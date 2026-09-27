using Godot;
using System.Collections.Generic;

namespace Vestiges.World;

/// <summary>
/// Génère les tiles isométriques 64×32 de route pixel art directionnelles.
/// Chaque tile = combinaison de connectivité (L/R/U/D) × parité du rang.
/// Palette issue de la charte graphique Ruines Urbaines.
/// La route est un overlay transparent en dehors de la chaussée,
/// dessiné au-dessus du sol urbain.
/// Grille « stacked » : une colonne de cellules zigzague de ±16 px d'un rang à l'autre. La bande verticale est donc
/// décalée vers le milieu de la colonne selon la parité du rang, et chaque tile ne dessine que sa tranche de 16 px
/// de haut (le pas entre deux rangs) : une rue verticale forme une seule chaussée droite, sans joint.
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
	private const int ColumnShift = 16; // décalage de la bande verticale vers le milieu de sa colonne
	private const int SlotTop = CenterY - 8;
	private const int SlotBottom = CenterY + 8;

	// Bitmask de connectivité
	public const int ConnLeft  = 8;
	public const int ConnRight = 4;
	public const int ConnUp    = 2;
	public const int ConnDown  = 1;
	public const int MaskCount = 16;
	public const int VariantCount = MaskCount * 2;

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

	// Bordure de trottoir usée : pierre claire côté lumière (nord, ouest), ombrée côté sud et est
	private static readonly Color CurbLight = new(0.541f, 0.537f, 0.510f);     // #8A8982
	private static readonly Color CurbShade = new(0.408f, 0.404f, 0.384f);     // #686762

	// Cache
	private static ImageTexture[] _roadTextures;

	/// <summary>
	/// Retourne les 32 textures de route, indexées par <see cref="VariantIndex"/>.
	/// </summary>
	public static ImageTexture[] GetOrGenerate()
	{
		if (_roadTextures != null)
			return _roadTextures;

		_roadTextures = new ImageTexture[VariantCount];
		for (int variant = 0; variant < VariantCount; variant++)
		{
			int mask = variant % MaskCount;
			int shift = variant < MaskCount ? ColumnShift : -ColumnShift;
			_roadTextures[variant] = GenerateRoadTile(mask, shift, seed: 42 + variant * 137);
		}

		return _roadTextures;
	}

	/// <summary>Variante pour un bitmask et le rang de la cellule (pair : bande verticale décalée vers la droite).</summary>
	public static int VariantIndex(int connectivity, int row) => connectivity + ((row & 1) != 0 ? MaskCount : 0);

	/// <summary>Clé de source d'une variante (ex: "road_21").</summary>
	public static string GetRoadKey(int variant) => $"road_{variant}";

	// =====================================================================
	//  GÉNÉRATION PRINCIPALE
	// =====================================================================

	private static ImageTexture GenerateRoadTile(int connectivity, int shift, int seed)
	{
		bool hasLeft  = (connectivity & ConnLeft) != 0;
		bool hasRight = (connectivity & ConnRight) != 0;
		bool hasUp    = (connectivity & ConnUp) != 0;
		bool hasDown  = (connectivity & ConnDown) != 0;

		Image img = Image.CreateEmpty(TileW, TileH, false, Image.Format.Rgba8);
		img.Fill(new Color(0, 0, 0, 0));

		uint rng = (uint)seed;

		// ── Passe 1 : Base asphalte avec bruit directionnel ──
		FillAsphalt(img, ref rng, hasLeft, hasRight, hasUp, hasDown, shift);

		// ── Passe 2 : Caniveau (bande sombre le long des bords) ──
		DrawGutters(img, hasLeft, hasRight, hasUp, hasDown, shift);

		// ── Passe 2b : Bordures de trottoir, usées par endroits ──
		DrawCurbs(img, hasLeft, hasRight, hasUp, hasDown, shift, seed);

		// ── Passe 3 : Fissures organiques ──
		int connCount = (hasLeft ? 1 : 0) + (hasRight ? 1 : 0)
					  + (hasUp ? 1 : 0) + (hasDown ? 1 : 0);
		int crackCount = connCount >= 3 ? 4 : connCount >= 2 ? 3 : 2;
		DrawCracks(img, ref rng, hasLeft, hasRight, hasUp, hasDown, shift, crackCount);

		// ── Passe 4 : Rustines d'asphalte (réparations) ──
		if (connCount >= 2)
			DrawPatch(img, ref rng, hasLeft, hasRight, hasUp, hasDown, shift);

		// ── Passe 5 : Marquages centraux (lignes droites seulement) ──
		if (hasLeft && hasRight && !hasUp && !hasDown)
			DrawHorizontalMarking(img, ref rng);
		if (hasUp && hasDown && !hasLeft && !hasRight)
			DrawVerticalMarking(img, ref rng, CenterX + shift);

		// ── Passe 6 : Nature reconquérante (herbe dans les fissures) ──
		DrawGrassInCracks(img, ref rng, hasLeft, hasRight, hasUp, hasDown, shift);

		// ── Passe 7 : Débris près des bordures ──
		DrawDebris(img, ref rng, hasLeft, hasRight, hasUp, hasDown, shift);

		return ImageTexture.CreateFromImage(img);
	}

	// =====================================================================
	//  ASPHALTE — surface avec bruit subtil
	// =====================================================================

	private static void FillAsphalt(Image img, ref uint rng,
		bool left, bool right, bool up, bool down, int shift)
	{
		for (int y = 0; y < TileH; y++)
		{
			for (int x = 0; x < TileW; x++)
			{
				if (!IsRoadPixel(x, y, left, right, up, down, shift))
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

	private static void DrawGutters(Image img, bool left, bool right, bool up, bool down, int shift)
	{
		for (int y = 0; y < TileH; y++)
		{
			for (int x = 0; x < TileW; x++)
			{
				if (!IsRoadPixel(x, y, left, right, up, down, shift))
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
						// Hors de la tranche du rang, la chaussée continue dans la tile voisine : pas de bord.
						if (nx < 0 || nx >= TileW || ny < SlotTop || ny >= SlotBottom)
							continue;
						if (!IsRoadPixel(nx, ny, left, right, up, down, shift))
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
	//  BORDURES — pierre de 1 px autour de la chaussée, manquante par tronçons
	// =====================================================================

	private static void DrawCurbs(Image img, bool left, bool right, bool up, bool down, int shift, int seed)
	{
		for (int y = SlotTop; y < SlotBottom; y++)
		{
			for (int x = 0; x < TileW; x++)
			{
				if (IsRoadPixel(x, y, left, right, up, down, shift))
					continue;

				// Côté de la chaussée : la bordure au nord et à l'ouest prend la lumière.
				bool roadBelow = IsRoadPixel(x, y + 1, left, right, up, down, shift);
				bool roadRight = IsRoadPixel(x + 1, y, left, right, up, down, shift);
				bool roadAbove = IsRoadPixel(x, y - 1, left, right, up, down, shift);
				bool roadLeft = IsRoadPixel(x - 1, y, left, right, up, down, shift);
				if (!roadBelow && !roadRight && !roadAbove && !roadLeft)
					continue;

				// Usure par tronçons de 3 px : une bordure sur quatre a disparu.
				uint wear = Xorshift((uint)(seed * 7919 + (x / 3) * 131 + (y / 3) * 977 + 1));
				if (wear % 4 == 0)
					continue;

				img.SetPixel(x, y, roadBelow || roadRight ? CurbLight : CurbShade);
			}
		}
	}

	// =====================================================================
	//  FISSURES — marche aléatoire organique
	// =====================================================================

	private static void DrawCracks(Image img, ref uint rng,
		bool left, bool right, bool up, bool down, int shift, int count)
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
					&& IsRoadPixel(cx, cy, left, right, up, down, shift))
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
							&& IsRoadPixel(bx, by, left, right, up, down, shift))
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
		bool left, bool right, bool up, bool down, int shift)
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
				if (!IsRoadPixel(tx, ty, left, right, up, down, shift)) continue;

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

	private static void DrawVerticalMarking(Image img, ref uint rng, int column)
	{
		int dashLen = 0;
		bool drawing = true;
		rng = Xorshift(rng);
		int nextSwitch = (int)(rng % 2) + 3;

		for (int y = SlotTop; y < SlotBottom; y++)
		{
			if (drawing)
			{
				Color existing = img.GetPixel(column, y);
				if (existing.A > 0.5f)
					img.SetPixel(column, y, existing.Lerp(MarkingYellow, MarkingYellow.A));
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
		bool left, bool right, bool up, bool down, int shift)
	{
		rng = Xorshift(rng);
		int grassCount = (int)(rng % 4) + 1;

		for (int i = 0; i < grassCount; i++)
		{
			rng = Xorshift(rng);
			int gx = (int)(rng % (uint)TileW);
			rng = Xorshift(rng);
			int gy = (int)(rng % (uint)TileH);
			if (!IsRoadPixel(gx, gy, left, right, up, down, shift)) continue;

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
						&& !IsRoadPixel(nx, ny, left, right, up, down, shift))
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
				if (nx >= 0 && nx < TileW
					&& IsRoadPixel(nx, gy, left, right, up, down, shift))
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
		bool left, bool right, bool up, bool down, int shift)
	{
		rng = Xorshift(rng);
		int debrisCount = (int)(rng % 3) + 1;

		for (int i = 0; i < debrisCount; i++)
		{
			rng = Xorshift(rng);
			int dx = (int)(rng % (uint)TileW);
			rng = Xorshift(rng);
			int dy = (int)(rng % (uint)TileH);
			if (!IsRoadPixel(dx, dy, left, right, up, down, shift)) continue;

			// Doit être près du bord de la route
			bool nearEdge = false;
			for (int ny = -2; ny <= 2 && !nearEdge; ny++)
			{
				for (int nx = -2; nx <= 2 && !nearEdge; nx++)
				{
					int tx = dx + nx;
					int ty = dy + ny;
					if (tx >= 0 && tx < TileW && ty >= 0 && ty < TileH
						&& !IsRoadPixel(tx, ty, left, right, up, down, shift))
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
	/// Le pixel (x,y) fait-il partie de la chaussée ? Seule la tranche du rang (16 px de haut) est dessinée :
	/// les rangs voisins couvrent le reste, sans recouvrement.
	/// </summary>
	private static bool IsRoadPixel(int x, int y, bool left, bool right, bool up, bool down, int shift)
	{
		if (x < 0 || x >= TileW || y < SlotTop || y >= SlotBottom)
			return false;

		int column = CenterX + shift;
		bool inHStrip = Mathf.Abs(y - CenterY) <= RoadHalfH;
		bool inVStrip = Mathf.Abs(x - column) <= RoadHalfV;

		// Bande horizontale (L↔R), jusqu'à la bande verticale
		if (inHStrip)
		{
			if (left && x <= column + RoadHalfV) return true;
			if (right && x >= column - RoadHalfV) return true;
		}

		// Bande verticale (U↔D)
		if (inVStrip)
		{
			if (up && y <= CenterY + RoadHalfH) return true;
			if (down && y >= CenterY - RoadHalfH) return true;
		}

		return false;
	}

	private static uint Xorshift(uint state)
	{
		state ^= state << 13;
		state ^= state >> 17;
		state ^= state << 5;
		return state == 0 ? 1 : state;
	}
}
