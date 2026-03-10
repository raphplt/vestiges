using Godot;
using System.Collections.Generic;

namespace Vestiges.World;

/// <summary>
/// Génère 16 tiles isométriques 64×32 de route pixel art directionnelles.
/// Chaque tile = combinaison de connectivité (L/R/U/D).
/// Palette issue de la charte graphique Ruines Urbaines.
/// La route est un overlay transparent en dehors de la chaussée,
/// dessiné au-dessus du sol urbain.
/// </summary>
public static class RoadTileGenerator
{
	private const int TileW = 64;
	private const int TileH = 32;
	private const int CenterX = TileW / 2; // 32
	private const int CenterY = TileH / 2; // 16

	// Demi-largeurs de la route (pixels) pour chaque axe
	// Le ratio 2:1 maintient une largeur visuelle égale en iso
	private const int RoadHalfH = 6;   // bande horizontale (L↔R)
	private const int RoadHalfV = 12;  // bande verticale (U↔D)

	// Bitmask de connectivité
	public const int ConnLeft  = 8;
	public const int ConnRight = 4;
	public const int ConnUp    = 2;
	public const int ConnDown  = 1;
	public const int VariantCount = 16;

	// ── Palette Ruines Urbaines (charte graphique) ──

	// Asphalte — surface principale de la route
	private static readonly Color AsphaltDark  = new(0.227f, 0.227f, 0.243f);  // #3A3A3E
	private static readonly Color AsphaltMid   = new(0.271f, 0.271f, 0.282f);  // #454548
	private static readonly Color AsphaltLight = new(0.314f, 0.314f, 0.333f);  // #505055

	// Caniveau — bande sombre le long des bordures
	private static readonly Color GutterColor = new(0.196f, 0.196f, 0.208f);   // #323235

	// Fissures
	private static readonly Color CrackDark  = new(0.176f, 0.176f, 0.184f);    // #2D2D2F
	private static readonly Color CrackLight = new(0.212f, 0.208f, 0.220f);    // #363538

	// Marquages jaunes (signalisation fanée) — charte: #C4A830
	private static readonly Color MarkingYellow = new(0.769f, 0.659f, 0.188f, 0.45f);

	// Rustine de réparation (patch d'asphalte plus récent)
	private static readonly Color PatchDark  = new(0.255f, 0.255f, 0.267f);    // #414144
	private static readonly Color PatchLight = new(0.294f, 0.290f, 0.302f);    // #4B4A4D

	// Nature reconquérante — #4A7A3A de la charte
	private static readonly Color GrassDark  = new(0.180f, 0.322f, 0.133f);    // #2E5222
	private static readonly Color GrassLight = new(0.290f, 0.478f, 0.227f);    // #4A7A3A

	// Débris/rouille — #6B3A24 de la charte
	private static readonly Color DebrisColor = new(0.420f, 0.227f, 0.141f);   // #6B3A24

	// Cache
	private static ImageTexture[] _roadTextures;

	/// <summary>
	/// Retourne les 16 textures de route (indexées par bitmask).
	/// </summary>
	public static ImageTexture[] GetOrGenerate()
	{
		if (_roadTextures != null)
			return _roadTextures;

		_roadTextures = new ImageTexture[VariantCount];
		for (int mask = 0; mask < VariantCount; mask++)
			_roadTextures[mask] = GenerateRoadTile(mask, seed: 42 + mask * 137);

		return _roadTextures;
	}

	/// <summary>Clé de source pour un bitmask (ex: "road_5").</summary>
	public static string GetRoadKey(int connectivity) => $"road_{connectivity}";

	// =====================================================================
	//  GÉNÉRATION PRINCIPALE
	// =====================================================================

	private static ImageTexture GenerateRoadTile(int connectivity, int seed)
	{
		bool hasLeft  = (connectivity & ConnLeft) != 0;
		bool hasRight = (connectivity & ConnRight) != 0;
		bool hasUp    = (connectivity & ConnUp) != 0;
		bool hasDown  = (connectivity & ConnDown) != 0;

		Image img = Image.CreateEmpty(TileW, TileH, false, Image.Format.Rgba8);
		img.Fill(new Color(0, 0, 0, 0));

		uint rng = (uint)seed;

		// ── Passe 1 : Base asphalte avec bruit directionnel ──
		FillAsphalt(img, ref rng, hasLeft, hasRight, hasUp, hasDown);

		// ── Passe 2 : Caniveau (bande sombre le long des bords) ──
		DrawGutters(img, hasLeft, hasRight, hasUp, hasDown);

		// ── Passe 3 : Fissures organiques ──
		int connCount = (hasLeft ? 1 : 0) + (hasRight ? 1 : 0)
					  + (hasUp ? 1 : 0) + (hasDown ? 1 : 0);
		int crackCount = connCount >= 3 ? 4 : connCount >= 2 ? 3 : 2;
		DrawCracks(img, ref rng, hasLeft, hasRight, hasUp, hasDown, crackCount);

		// ── Passe 4 : Rustines d'asphalte (réparations) ──
		if (connCount >= 2)
			DrawPatch(img, ref rng, hasLeft, hasRight, hasUp, hasDown);

		// ── Passe 5 : Marquages centraux (lignes droites seulement) ──
		if (hasLeft && hasRight && !hasUp && !hasDown)
			DrawHorizontalMarking(img, ref rng);
		if (hasUp && hasDown && !hasLeft && !hasRight)
			DrawVerticalMarking(img, ref rng);

		// ── Passe 6 : Nature reconquérante (herbe dans les fissures) ──
		DrawGrassInCracks(img, ref rng, hasLeft, hasRight, hasUp, hasDown);

		// ── Passe 7 : Débris près des bordures ──
		DrawDebris(img, ref rng, hasLeft, hasRight, hasUp, hasDown);

		return ImageTexture.CreateFromImage(img);
	}

	// =====================================================================
	//  ASPHALTE — surface avec bruit subtil
	// =====================================================================

	private static void FillAsphalt(Image img, ref uint rng,
		bool left, bool right, bool up, bool down)
	{
		for (int y = 0; y < TileH; y++)
		{
			for (int x = 0; x < TileW; x++)
			{
				if (!IsVisibleRoadPixel(x, y, left, right, up, down))
					continue;

				// Bruit d'asphalte (variation granuleuse)
				rng = Xorshift(rng);
				int noise = (int)(rng % 100);

				Color c;
				if (noise < 45)
					c = AsphaltMid;
				else if (noise < 75)
					c = AsphaltDark;
				else if (noise < 92)
					c = AsphaltLight;
				else
					c = AsphaltMid; // pixel neutre pour casser les patterns

				// Micro-variation par pixel (±2/255)
				// Cast (int) obligatoire : rng est uint, sans cast la soustraction wrappe
				rng = Xorshift(rng);
				float v = ((int)(rng % 5) - 2) / 255f;
				c.R = Mathf.Clamp(c.R + v, 0f, 1f);
				c.G = Mathf.Clamp(c.G + v, 0f, 1f);
				c.B = Mathf.Clamp(c.B + v, 0f, 1f);

				img.SetPixel(x, y, c);
			}
		}
	}

	// =====================================================================
	//  CANIVEAU — bande sombre de 1px le long des bords intérieurs
	// =====================================================================

	private static void DrawGutters(Image img, bool left, bool right, bool up, bool down)
	{
		for (int y = 0; y < TileH; y++)
		{
			for (int x = 0; x < TileW; x++)
			{
				// Caniveau uniquement dans le losange (pas dans les extensions)
				if (!IsInsideDiamond(x, y))
					continue;
				if (!IsRoadPixel(x, y, left, right, up, down))
					continue;

				// Pixel de route adjacent à un pixel non-route
				bool adjacentToEdge = false;
				for (int dy = -1; dy <= 1; dy++)
				{
					for (int dx = -1; dx <= 1; dx++)
					{
						if (dx == 0 && dy == 0) continue;
						int nx = x + dx;
						int ny = y + dy;
						if (nx < 0 || nx >= TileW || ny < 0 || ny >= TileH)
							continue;
						if (!IsVisibleRoadPixel(nx, ny, left, right, up, down))
						{
							adjacentToEdge = true;
							break;
						}
					}
					if (adjacentToEdge) break;
				}

				if (adjacentToEdge)
					img.SetPixel(x, y, GutterColor);
			}
		}
	}

	// =====================================================================
	//  FISSURES — marche aléatoire organique
	// =====================================================================

	private static void DrawCracks(Image img, ref uint rng,
		bool left, bool right, bool up, bool down, int count)
	{
		for (int i = 0; i < count; i++)
		{
			rng = Xorshift(rng);
			int cx = (int)(rng % (uint)(TileW - 20)) + 10;
			rng = Xorshift(rng);
			int cy = (int)(rng % (uint)(TileH - 10)) + 5;

			rng = Xorshift(rng);
			int length = (int)(rng % 10) + 5;

			// Direction principale de la fissure
			rng = Xorshift(rng);
			bool horizontal = (rng % 2) == 0;

			for (int step = 0; step < length; step++)
			{
				if (cx >= 0 && cx < TileW && cy >= 0 && cy < TileH
					&& IsInsideDiamond(cx, cy)
					&& IsRoadPixel(cx, cy, left, right, up, down))
				{
					Color crackColor = (step % 3 == 0) ? CrackDark : CrackLight;
					img.SetPixel(cx, cy, crackColor);

					// Branchement occasionnel (1px perpendiculaire)
					rng = Xorshift(rng);
					if (rng % 4 == 0)
					{
						int bx = horizontal ? cx : cx + ((rng % 2 == 0) ? 1 : -1);
						int by = horizontal ? cy + ((rng % 2 == 0) ? 1 : -1) : cy;
						if (bx >= 0 && bx < TileW && by >= 0 && by < TileH
							&& IsInsideDiamond(bx, by)
							&& IsRoadPixel(bx, by, left, right, up, down))
						{
							img.SetPixel(bx, by, CrackLight);
						}
					}
				}

				// Avancer avec déviation
				rng = Xorshift(rng);
				int dir = (int)(rng % 6);
				if (horizontal)
				{
					switch (dir)
					{
						case 0: case 1: case 2: cx++; break;
						case 3: cx++; cy++; break;
						case 4: cx++; cy--; break;
						default: cy += (rng % 2 == 0) ? 1 : -1; break;
					}
				}
				else
				{
					switch (dir)
					{
						case 0: case 1: case 2: cy++; break;
						case 3: cx++; cy++; break;
						case 4: cx--; cy++; break;
						default: cx += (rng % 2 == 0) ? 1 : -1; break;
					}
				}
			}
		}
	}

	// =====================================================================
	//  RUSTINE — patch d'asphalte plus récent (rectangle irrégulier)
	// =====================================================================

	private static void DrawPatch(Image img, ref uint rng,
		bool left, bool right, bool up, bool down)
	{
		rng = Xorshift(rng);
		if (rng % 3 != 0) return; // ~33% chance d'avoir un patch

		rng = Xorshift(rng);
		int px = (int)(rng % (uint)(TileW - 20)) + 10;
		rng = Xorshift(rng);
		int py = (int)(rng % (uint)(TileH - 10)) + 5;
		rng = Xorshift(rng);
		int pw = (int)(rng % 5) + 3;
		rng = Xorshift(rng);
		int ph = (int)(rng % 3) + 2;

		for (int dy = 0; dy < ph; dy++)
		{
			for (int dx = 0; dx < pw; dx++)
			{
				int tx = px + dx;
				int ty = py + dy;
				if (tx >= TileW || ty >= TileH) continue;
				if (!IsInsideDiamond(tx, ty)) continue;
				if (!IsRoadPixel(tx, ty, left, right, up, down)) continue;

				rng = Xorshift(rng);
				Color pc = (rng % 3 == 0) ? PatchLight : PatchDark;
				img.SetPixel(tx, ty, pc);
			}
		}
	}

	// =====================================================================
	//  MARQUAGES CENTRAUX — lignes jaunes tiretées (fanées)
	// =====================================================================

	private static void DrawHorizontalMarking(Image img, ref uint rng)
	{
		int dashLen = 0;
		bool drawing = true;
		rng = Xorshift(rng);
		int nextSwitch = (int)(rng % 2) + 3;

		for (int x = 6; x < TileW - 6; x++)
		{
			if (!IsInsideDiamond(x, CenterY)) continue;

			if (drawing)
			{
				Color existing = img.GetPixel(x, CenterY);
				if (existing.A > 0.5f)
					img.SetPixel(x, CenterY, existing.Lerp(MarkingYellow, MarkingYellow.A));
			}

			dashLen++;
			if (dashLen >= nextSwitch)
			{
				dashLen = 0;
				drawing = !drawing;
				rng = Xorshift(rng);
				nextSwitch = drawing ? (int)(rng % 2) + 3 : (int)(rng % 2) + 2;
			}
		}
	}

	private static void DrawVerticalMarking(Image img, ref uint rng)
	{
		int dashLen = 0;
		bool drawing = true;
		rng = Xorshift(rng);
		int nextSwitch = (int)(rng % 2) + 3;

		for (int y = 3; y < TileH - 3; y++)
		{
			if (!IsInsideDiamond(CenterX, y)) continue;

			if (drawing)
			{
				Color existing = img.GetPixel(CenterX, y);
				if (existing.A > 0.5f)
					img.SetPixel(CenterX, y, existing.Lerp(MarkingYellow, MarkingYellow.A));
			}

			dashLen++;
			if (dashLen >= nextSwitch)
			{
				dashLen = 0;
				drawing = !drawing;
				rng = Xorshift(rng);
				nextSwitch = drawing ? (int)(rng % 2) + 2 : (int)(rng % 2) + 1;
			}
		}
	}

	// =====================================================================
	//  NATURE RECONQUÉRANTE — pixels d'herbe dans les fissures
	// =====================================================================

	private static void DrawGrassInCracks(Image img, ref uint rng,
		bool left, bool right, bool up, bool down)
	{
		rng = Xorshift(rng);
		int grassCount = (int)(rng % 4) + 1;

		for (int i = 0; i < grassCount; i++)
		{
			rng = Xorshift(rng);
			int gx = (int)(rng % (uint)TileW);
			rng = Xorshift(rng);
			int gy = (int)(rng % (uint)TileH);

			if (!IsInsideDiamond(gx, gy)) continue;
			if (!IsRoadPixel(gx, gy, left, right, up, down)) continue;

			// Vérifier qu'on est près d'une fissure (pixel sombre)
			Color existing = img.GetPixel(gx, gy);
			bool nearCrack = existing.R < 0.22f;

			// Ou près du bord de la route
			bool nearEdge = false;
			for (int dy = -1; dy <= 1 && !nearEdge; dy++)
			{
				for (int dx = -1; dx <= 1 && !nearEdge; dx++)
				{
					int nx = gx + dx;
					int ny = gy + dy;
					if (nx >= 0 && nx < TileW && ny >= 0 && ny < TileH
						&& IsInsideDiamond(nx, ny)
						&& !IsRoadPixel(nx, ny, left, right, up, down))
					{
						nearEdge = true;
					}
				}
			}

			if (!nearCrack && !nearEdge) continue;

			// Placer 1-3 pixels de verdure
			rng = Xorshift(rng);
			Color grassColor = (rng % 2 == 0) ? GrassDark : GrassLight;
			img.SetPixel(gx, gy, grassColor);

			rng = Xorshift(rng);
			if (rng % 2 == 0)
			{
				int nx = gx + ((rng % 3 == 0) ? 1 : -1);
				if (nx >= 0 && nx < TileW && IsInsideDiamond(nx, gy)
					&& IsRoadPixel(nx, gy, left, right, up, down))
				{
					rng = Xorshift(rng);
					img.SetPixel(nx, gy, (rng % 2 == 0) ? GrassDark : GrassLight);
				}
			}
		}
	}

	// =====================================================================
	//  DÉBRIS — petits pixels de rouille/gravats près des bordures
	// =====================================================================

	private static void DrawDebris(Image img, ref uint rng,
		bool left, bool right, bool up, bool down)
	{
		rng = Xorshift(rng);
		int debrisCount = (int)(rng % 3) + 1;

		for (int i = 0; i < debrisCount; i++)
		{
			rng = Xorshift(rng);
			int dx = (int)(rng % (uint)TileW);
			rng = Xorshift(rng);
			int dy = (int)(rng % (uint)TileH);

			if (!IsInsideDiamond(dx, dy)) continue;
			if (!IsRoadPixel(dx, dy, left, right, up, down)) continue;

			// Doit être près du bord de la route
			bool nearEdge = false;
			for (int ny = -2; ny <= 2 && !nearEdge; ny++)
			{
				for (int nx = -2; nx <= 2 && !nearEdge; nx++)
				{
					int tx = dx + nx;
					int ty = dy + ny;
					if (tx >= 0 && tx < TileW && ty >= 0 && ty < TileH
						&& IsInsideDiamond(tx, ty)
						&& !IsRoadPixel(tx, ty, left, right, up, down))
					{
						nearEdge = true;
					}
				}
			}

			if (!nearEdge) continue;

			img.SetPixel(dx, dy, DebrisColor);
		}
	}

	// =====================================================================
	//  TEST DE GÉOMÉTRIE
	// =====================================================================

	/// <summary>
	/// Le pixel (x,y) fait-il partie de la bande de route ?
	/// </summary>
	private static bool IsRoadPixel(int x, int y, bool left, bool right, bool up, bool down)
	{
		int distH = Mathf.Abs(y - CenterY);
		int distV = Mathf.Abs(x - CenterX);

		bool inHStrip = distH <= RoadHalfH;
		bool inVStrip = distV <= RoadHalfV;

		// Bande horizontale (L↔R)
		if (inHStrip)
		{
			if (left && x <= CenterX + RoadHalfV) return true;
			if (right && x >= CenterX - RoadHalfV) return true;
		}

		// Bande verticale (U↔D)
		if (inVStrip)
		{
			if (up && y <= CenterY + RoadHalfH) return true;
			if (down && y >= CenterY - RoadHalfH) return true;
		}

		return false;
	}

	/// <summary>
	/// Le pixel fait-il partie de la zone de route visible ?
	/// Inclut les extensions hors losange aux bords connectés pour éviter
	/// le rétrécissement de la route aux jonctions de tiles.
	/// </summary>
	private static bool IsVisibleRoadPixel(int x, int y, bool left, bool right, bool up, bool down)
	{
		if (x < 0 || x >= TileW || y < 0 || y >= TileH)
			return false;
		if (!IsRoadPixel(x, y, left, right, up, down))
			return false;
		if (IsInsideDiamond(x, y))
			return true;

		// Hors losange : autoriser l'extension vers les bords connectés
		int dx = x - CenterX;
		int dy = y - CenterY;
		if (left && dx < 0) return true;
		if (right && dx > 0) return true;
		if (up && dy < 0) return true;
		if (down && dy > 0) return true;

		return false;
	}

	private static bool IsInsideDiamond(int x, int y)
	{
		if (x < 0 || x >= TileW || y < 0 || y >= TileH)
			return false;

		int dx = x - CenterX;
		int dy = y - CenterY;
		return Mathf.Abs(dx) * TileH + Mathf.Abs(dy) * TileW <= TileW * TileH / 2;
	}

	private static uint Xorshift(uint state)
	{
		state ^= state << 13;
		state ^= state >> 17;
		state ^= state << 5;
		return state == 0 ? 1 : state;
	}
}
