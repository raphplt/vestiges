using Godot;
using System.Collections.Generic;

namespace Vestiges.World;

/// <summary>
/// Génère 16 tiles isométriques 64×32 de route pixel art directionnelles.
/// Chaque tile correspond à une combinaison de connectivité (L/R/U/D).
/// La route est fine (~11px de haut en bande horizontale) et ne couvre qu'une
/// partie du diamant ; le reste est transparent pour laisser voir le terrain.
/// </summary>
public static class RoadTileGenerator
{
	private const int TileW = 64;
	private const int TileH = 32;
	private const int CenterX = TileW / 2; // 32
	private const int CenterY = TileH / 2; // 16

	// Demi-largeurs de la route (en pixels) pour chaque axe
	// RoadHalfH = demi-hauteur de la bande horizontale (route L↔R)
	// RoadHalfV = demi-largeur de la bande verticale (route U↔D)
	// Le ratio 2:1 maintient une largeur visuelle égale en isométrique
	private const int RoadHalfH = 5;
	private const int RoadHalfV = 10;

	// Bitmask de connectivité
	public const int ConnLeft  = 8;
	public const int ConnRight = 4;
	public const int ConnUp    = 2;
	public const int ConnDown  = 1;
	public const int VariantCount = 16;

	// Palette asphalte — charte graphique urban_ruins
	private static readonly Color AsphaltDark  = new(0.22f, 0.22f, 0.24f);
	private static readonly Color AsphaltMid   = new(0.29f, 0.29f, 0.31f);
	private static readonly Color AsphaltLight = new(0.34f, 0.33f, 0.35f);
	private static readonly Color CurbDark     = new(0.38f, 0.36f, 0.33f);
	private static readonly Color CurbLight    = new(0.45f, 0.42f, 0.38f);
	private static readonly Color CrackDark    = new(0.14f, 0.13f, 0.15f);
	private static readonly Color CrackMid     = new(0.18f, 0.17f, 0.19f);
	private static readonly Color PotholeEdge  = new(0.16f, 0.15f, 0.17f);
	private static readonly Color PotholeFill  = new(0.11f, 0.10f, 0.12f);
	private static readonly Color MarkingYellow = new(0.76f, 0.66f, 0.19f, 0.35f);

	// Cache des textures générées par connectivité
	private static ImageTexture[] _roadTextures;

	/// <summary>
	/// Retourne les 16 textures de route (indexées par bitmask de connectivité).
	/// Index = (hasLeft?8:0) | (hasRight?4:0) | (hasUp?2:0) | (hasDown?1:0)
	/// </summary>
	public static ImageTexture[] GetOrGenerate()
	{
		if (_roadTextures != null)
			return _roadTextures;

		_roadTextures = new ImageTexture[VariantCount];
		for (int mask = 0; mask < VariantCount; mask++)
		{
			_roadTextures[mask] = GenerateRoadTile(mask, seed: 42 + mask * 137);
		}
		return _roadTextures;
	}

	/// <summary>
	/// Retourne la clé de source spéciale pour un bitmask donné (ex: "road_5").
	/// </summary>
	public static string GetRoadKey(int connectivity)
	{
		return $"road_{connectivity}";
	}

	// =====================================================================
	//  Génération d'une tile de route selon son bitmask de connectivité
	// =====================================================================

	private static ImageTexture GenerateRoadTile(int connectivity, int seed)
	{
		bool hasLeft  = (connectivity & ConnLeft) != 0;
		bool hasRight = (connectivity & ConnRight) != 0;
		bool hasUp    = (connectivity & ConnUp) != 0;
		bool hasDown  = (connectivity & ConnDown) != 0;

		Image img = Image.CreateEmpty(TileW, TileH, false, Image.Format.Rgba8);
		uint rng = (uint)seed;

		// Passe 1 : remplir les pixels de route (asphalte)
		for (int y = 0; y < TileH; y++)
		{
			for (int x = 0; x < TileW; x++)
			{
				if (!IsInsideDiamond(x, y))
					continue;

				if (!IsRoadPixel(x, y, hasLeft, hasRight, hasUp, hasDown))
					continue;

				// Bruit d'asphalte
				rng = Xorshift(rng);
				int noise = (int)(rng % 100);
				Color baseColor;
				if (noise < 50)
					baseColor = AsphaltMid;
				else if (noise < 80)
					baseColor = AsphaltDark;
				else
					baseColor = AsphaltLight;

				// Petite variation par pixel
				rng = Xorshift(rng);
				float variation = ((rng % 20) - 10) / 255f;
				baseColor.R += variation;
				baseColor.G += variation;
				baseColor.B += variation;

				img.SetPixel(x, y, baseColor);
			}
		}

		// Passe 2 : bordures de trottoir (curb) le long des bords de la route
		DrawCurbs(img, hasLeft, hasRight, hasUp, hasDown);

		// Passe 3 : détails (fissures, nids de poule, marquages)
		int connCount = (hasLeft ? 1 : 0) + (hasRight ? 1 : 0) + (hasUp ? 1 : 0) + (hasDown ? 1 : 0);
		DrawCracks(img, seed, hasLeft, hasRight, hasUp, hasDown, density: connCount >= 3 ? 3 : 2);

		// Marquages centraux pour les lignes droites
		if (hasLeft && hasRight && !hasUp && !hasDown)
			DrawHorizontalMarking(img, seed);
		if (hasUp && hasDown && !hasLeft && !hasRight)
			DrawVerticalMarking(img, seed);

		// Nid de poule occasionnel sur les intersections
		if (connCount >= 3)
			DrawPothole(img, seed);

		return ImageTexture.CreateFromImage(img);
	}

	// =====================================================================
	//  Test : est-ce qu'un pixel fait partie de la route ?
	// =====================================================================

	/// <summary>
	/// Détermine si le pixel (x,y) fait partie de la bande de route
	/// selon la connectivité aux 4 côtés du diamant.
	/// </summary>
	private static bool IsRoadPixel(int x, int y, bool left, bool right, bool up, bool down)
	{
		int distH = Mathf.Abs(y - CenterY); // distance à la ligne horizontale centrale
		int distV = Mathf.Abs(x - CenterX); // distance à la ligne verticale centrale

		bool inHStrip = distH <= RoadHalfH;
		bool inVStrip = distV <= RoadHalfV;

		// Bande horizontale (route L↔R), étendue au centre pour la jonction
		if (inHStrip)
		{
			if (left && x <= CenterX + RoadHalfV) return true;
			if (right && x >= CenterX - RoadHalfV) return true;
		}

		// Bande verticale (route U↔D), étendue au centre pour la jonction
		if (inVStrip)
		{
			if (up && y <= CenterY + RoadHalfH) return true;
			if (down && y >= CenterY - RoadHalfH) return true;
		}

		return false;
	}

	/// <summary>
	/// Retourne true si le pixel est sur le bord de la route (1px de curb).
	/// </summary>
	private static bool IsRoadEdge(int x, int y, bool left, bool right, bool up, bool down)
	{
		if (!IsInsideDiamond(x, y)) return false;
		if (IsRoadPixel(x, y, left, right, up, down)) return false;

		// Vérifier si un voisin immédiat est sur la route
		for (int dy = -1; dy <= 1; dy++)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				if (dx == 0 && dy == 0) continue;
				int nx = x + dx;
				int ny = y + dy;
				if (nx >= 0 && nx < TileW && ny >= 0 && ny < TileH
					&& IsInsideDiamond(nx, ny)
					&& IsRoadPixel(nx, ny, left, right, up, down))
				{
					return true;
				}
			}
		}
		return false;
	}

	// =====================================================================
	//  Dessin des bordures de trottoir
	// =====================================================================

	private static void DrawCurbs(Image img, bool left, bool right, bool up, bool down)
	{
		for (int y = 0; y < TileH; y++)
		{
			for (int x = 0; x < TileW; x++)
			{
				if (!IsRoadEdge(x, y, left, right, up, down))
					continue;

				// Alternance claire/sombre pour le curb
				Color curbColor = ((x + y) % 2 == 0) ? CurbDark : CurbLight;
				img.SetPixel(x, y, curbColor);
			}
		}
	}

	// =====================================================================
	//  Détails : fissures dans l'asphalte (limitées à la zone route)
	// =====================================================================

	private static void DrawCracks(Image img, int seed, bool left, bool right, bool up, bool down, int density)
	{
		uint rng = (uint)(seed * 31 + 7);

		for (int i = 0; i < density; i++)
		{
			rng = Xorshift(rng);
			int startX = (int)(rng % (uint)(TileW - 16)) + 8;
			rng = Xorshift(rng);
			int startY = (int)(rng % (uint)(TileH - 8)) + 4;

			int cx = startX;
			int cy = startY;
			rng = Xorshift(rng);
			int length = (int)(rng % 8) + 4;

			for (int step = 0; step < length; step++)
			{
				if (IsInsideDiamond(cx, cy) && IsRoadPixel(cx, cy, left, right, up, down))
				{
					Color crackColor = step % 3 == 0 ? CrackDark : CrackMid;
					img.SetPixel(cx, cy, crackColor);

					rng = Xorshift(rng);
					if (rng % 3 == 0)
					{
						int nx = cx + 1;
						if (nx < TileW && IsInsideDiamond(nx, cy) && IsRoadPixel(nx, cy, left, right, up, down))
							img.SetPixel(nx, cy, CrackMid);
					}
				}

				rng = Xorshift(rng);
				int dir = (int)(rng % 5);
				switch (dir)
				{
					case 0: cx++; break;
					case 1: cx--; break;
					case 2: cy++; break;
					case 3: cx++; cy++; break;
					default: cx--; cy++; break;
				}
				cx = Mathf.Clamp(cx, 0, TileW - 1);
				cy = Mathf.Clamp(cy, 0, TileH - 1);
			}
		}
	}

	// =====================================================================
	//  Marquage central (ligne jaune effacée) pour routes droites
	// =====================================================================

	private static void DrawHorizontalMarking(Image img, int seed)
	{
		uint rng = (uint)(seed * 37);
		// Ligne horizontale au centre (y = CenterY), avec des gaps
		for (int x = 8; x < TileW - 8; x++)
		{
			rng = Xorshift(rng);
			if (rng % 4 == 0) continue; // gap

			if (!IsInsideDiamond(x, CenterY)) continue;

			Color existing = img.GetPixel(x, CenterY);
			if (existing.A > 0.5f)
				img.SetPixel(x, CenterY, existing.Lerp(MarkingYellow, MarkingYellow.A));
		}
	}

	private static void DrawVerticalMarking(Image img, int seed)
	{
		uint rng = (uint)(seed * 43);
		// Ligne verticale au centre (x = CenterX), avec des gaps
		for (int y = 4; y < TileH - 4; y++)
		{
			rng = Xorshift(rng);
			if (rng % 4 == 0) continue;

			if (!IsInsideDiamond(CenterX, y)) continue;

			Color existing = img.GetPixel(CenterX, y);
			if (existing.A > 0.5f)
				img.SetPixel(CenterX, y, existing.Lerp(MarkingYellow, MarkingYellow.A));
		}
	}

	// =====================================================================
	//  Nid de poule (petit cercle sombre au centre, pour intersections)
	// =====================================================================

	private static void DrawPothole(Image img, int seed)
	{
		uint rng = (uint)(seed * 67 + 11);
		rng = Xorshift(rng);
		int ox = (int)(rng % 7) - 3; // offset -3..+3
		rng = Xorshift(rng);
		int oy = (int)(rng % 5) - 2;
		int pcx = CenterX + ox;
		int pcy = CenterY + oy;
		int radius = 2;

		for (int dy = -radius - 1; dy <= radius + 1; dy++)
		{
			for (int dx = -radius - 1; dx <= radius + 1; dx++)
			{
				int px = pcx + dx;
				int py = pcy + dy;
				if (px < 0 || px >= TileW || py < 0 || py >= TileH) continue;
				if (!IsInsideDiamond(px, py)) continue;

				Color existing = img.GetPixel(px, py);
				if (existing.A < 0.5f) continue;

				int distSq = dx * dx + dy * dy;
				if (distSq <= radius * radius)
					img.SetPixel(px, py, PotholeFill);
				else if (distSq <= (radius + 1) * (radius + 1))
					img.SetPixel(px, py, PotholeEdge);
			}
		}
	}

	// =====================================================================
	//  Utilitaires
	// =====================================================================

	private static bool IsInsideDiamond(int x, int y)
	{
		if (x < 0 || x >= TileW || y < 0 || y >= TileH)
			return false;

		int dx = x - CenterX;
		int dy = y - CenterY;
		return Mathf.Abs(dx) * TileH + Mathf.Abs(dy) * TileW <= TileW * TileH / 2;
	}

	private static ImageTexture CreateTexture(Image img)
	{
		return ImageTexture.CreateFromImage(img);
	}

	private static uint Xorshift(uint state)
	{
		state ^= state << 13;
		state ^= state >> 17;
		state ^= state << 5;
		return state == 0 ? 1 : state;
	}
}
