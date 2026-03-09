using Godot;

namespace Vestiges.Events;

/// <summary>
/// Fabrique centralisée de sprites pixel art pour les événements aléatoires.
/// Génère chaque sprite via Image pixel par pixel, cachés en static.
/// Remplace tous les Polygon2D procéduraux de RandomEventManager.
/// </summary>
public static class EventSpriteFactory
{
	// Cache des textures
	private static ImageTexture _merchantTex;
	private static ImageTexture _smokeBaseTex;
	private static ImageTexture[] _smokeFrames;
	private static ImageTexture _lureTex;
	private static ImageTexture[] _riftFrames;
	private static ImageTexture[] _ghostBuildingTex;
	private static ImageTexture _resonanceWaveTex;
	private static ImageTexture[] _flowerFrames;

	// =====================================================================
	//  Marchand ambulant (caravan) — 24×32
	// =====================================================================

	public static ImageTexture GetMerchantSprite()
	{
		if (_merchantTex != null) return _merchantTex;

		Image img = Image.CreateEmpty(24, 32, false, Image.Format.Rgba8);
		Color cloak = new(0.65f, 0.50f, 0.15f);
		Color cloakDark = new(0.50f, 0.38f, 0.10f);
		Color hat = new(0.45f, 0.32f, 0.08f);
		Color hatBand = new(0.75f, 0.60f, 0.18f);
		Color skin = new(0.72f, 0.58f, 0.42f);
		Color eye = new(0.15f, 0.12f, 0.10f);
		Color pack = new(0.55f, 0.42f, 0.22f);
		Color packDark = new(0.40f, 0.30f, 0.15f);
		Color outline = new(0.18f, 0.15f, 0.12f);

		// Corps (losange / robe large) — lignes 16-30
		for (int y = 16; y <= 30; y++)
		{
			int halfW = (y - 16) * 8 / 14 + 3;
			int cx = 12;
			for (int x = cx - halfW; x <= cx + halfW; x++)
			{
				if (x < 0 || x >= 24) continue;
				if (x == cx - halfW || x == cx + halfW)
					img.SetPixel(x, y, outline);
				else
					img.SetPixel(x, y, y % 3 == 0 ? cloakDark : cloak);
			}
		}
		// Bas du manteau
		for (int x = 4; x <= 20; x++)
			SetPixelSafe(img, x, 31, outline);

		// Tête / visage — lignes 8-15
		for (int y = 10; y <= 15; y++)
		{
			for (int x = 9; x <= 15; x++)
				img.SetPixel(x, y, skin);
		}
		// Yeux
		img.SetPixel(10, 12, eye);
		img.SetPixel(14, 12, eye);

		// Chapeau pointu — lignes 0-9
		for (int y = 2; y <= 9; y++)
		{
			int halfW = (y - 2) * 5 / 7;
			int cx = 12;
			for (int x = cx - halfW; x <= cx + halfW; x++)
			{
				if (x < 0 || x >= 24) continue;
				img.SetPixel(x, y, hat);
			}
		}
		// Bord du chapeau
		for (int x = 7; x <= 17; x++)
			SetPixelSafe(img, x, 10, hatBand);

		// Sac à dos (côté droit)
		for (int y = 14; y <= 24; y++)
		{
			for (int x = 17; x <= 21; x++)
			{
				if (x == 17 || x == 21 || y == 14 || y == 24)
					img.SetPixel(x, y, packDark);
				else
					img.SetPixel(x, y, pack);
			}
		}

		_merchantTex = ImageTexture.CreateFromImage(img);
		return _merchantTex;
	}

	// =====================================================================
	//  Colonne de fumée (smoke_signal) — 16×32, 3 frames
	// =====================================================================

	public static ImageTexture[] GetSmokeFrames()
	{
		if (_smokeFrames != null) return _smokeFrames;

		_smokeFrames = new ImageTexture[3];
		for (int f = 0; f < 3; f++)
			_smokeFrames[f] = GenerateSmokeFrame(f);
		return _smokeFrames;
	}

	private static ImageTexture GenerateSmokeFrame(int frame)
	{
		Image img = Image.CreateEmpty(16, 32, false, Image.Format.Rgba8);
		Color fireOrange = new(0.90f, 0.50f, 0.10f);
		Color fireYellow = new(0.95f, 0.75f, 0.20f);
		Color smokeLight = new(0.60f, 0.58f, 0.55f);
		Color smokeMid = new(0.45f, 0.43f, 0.40f);
		Color smokeDark = new(0.32f, 0.30f, 0.28f);

		int offset = frame; // frame shift pour l'animation

		// Feu (base, lignes 26-31)
		for (int y = 26; y <= 31; y++)
		{
			int halfW = (31 - y) + 1;
			int cx = 8;
			for (int x = cx - halfW; x <= cx + halfW; x++)
			{
				if (x < 0 || x >= 16) continue;
				img.SetPixel(x, y, y < 29 ? fireYellow : fireOrange);
			}
		}

		// Fumée (bouffées empilées, lignes 4-25)
		int[] puffCenters = { 8, 7 + offset % 2, 9 - offset % 2, 8 + (offset + 1) % 3 - 1 };
		int[] puffY = { 22, 16, 10, 5 };
		int[] puffR = { 3, 4, 3, 2 };
		float[] puffAlpha = { 0.6f, 0.5f, 0.35f, 0.2f };

		for (int p = 0; p < 4; p++)
		{
			int cx = puffCenters[p];
			int cy = puffY[p] - offset;
			int r = puffR[p];
			float alpha = puffAlpha[p];

			for (int dy = -r; dy <= r; dy++)
			{
				for (int dx = -r; dx <= r; dx++)
				{
					if (dx * dx + dy * dy > r * r) continue;
					int px = cx + dx;
					int py = cy + dy;
					if (px < 0 || px >= 16 || py < 0 || py >= 32) continue;

					Color smokeColor = (dx * dx + dy * dy < (r - 1) * (r - 1))
						? smokeMid : smokeLight;
					smokeColor.A = alpha;
					Color existing = img.GetPixel(px, py);
					if (existing.A > smokeColor.A)
						continue;
					img.SetPixel(px, py, smokeColor);
				}
			}
		}

		return ImageTexture.CreateFromImage(img);
	}

	// =====================================================================
	//  Leurre (the_call) — 16×16, lueur verte pulsante
	// =====================================================================

	public static ImageTexture GetLureSprite()
	{
		if (_lureTex != null) return _lureTex;

		Image img = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
		Color coreGreen = new(0.40f, 0.90f, 0.50f, 0.85f);
		Color midGreen = new(0.30f, 0.80f, 0.40f, 0.55f);
		Color outerGreen = new(0.25f, 0.70f, 0.35f, 0.25f);

		int cx = 8, cy = 8;
		for (int y = 0; y < 16; y++)
		{
			for (int x = 0; x < 16; x++)
			{
				int dx = x - cx;
				int dy = y - cy;
				int distSq = dx * dx + dy * dy;

				if (distSq <= 4)
					img.SetPixel(x, y, coreGreen);
				else if (distSq <= 16)
					img.SetPixel(x, y, midGreen);
				else if (distSq <= 36)
					img.SetPixel(x, y, outerGreen);
			}
		}

		// Étoile intérieure
		img.SetPixel(cx, cy - 3, coreGreen);
		img.SetPixel(cx, cy + 3, coreGreen);
		img.SetPixel(cx - 3, cy, coreGreen);
		img.SetPixel(cx + 3, cy, coreGreen);

		_lureTex = ImageTexture.CreateFromImage(img);
		return _lureTex;
	}

	// =====================================================================
	//  Faille temporelle (temporal_rift) — 32×32, 4 frames
	// =====================================================================

	public static ImageTexture[] GetRiftFrames()
	{
		if (_riftFrames != null) return _riftFrames;

		_riftFrames = new ImageTexture[4];
		for (int f = 0; f < 4; f++)
			_riftFrames[f] = GenerateRiftFrame(f);
		return _riftFrames;
	}

	private static ImageTexture GenerateRiftFrame(int frame)
	{
		Image img = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
		Color gold = new(1f, 0.85f, 0.30f, 0.70f);
		Color goldMid = new(1f, 0.75f, 0.20f, 0.45f);
		Color goldOuter = new(1f, 0.65f, 0.15f, 0.15f);
		Color spiralBright = new(1f, 0.95f, 0.60f, 0.65f);

		int cx = 16, cy = 16;
		float rotOffset = frame * Mathf.Pi * 0.5f;

		// Cercles concentriques avec variation par frame
		for (int y = 0; y < 32; y++)
		{
			for (int x = 0; x < 32; x++)
			{
				int dx = x - cx;
				int dy = y - cy;
				int distSq = dx * dx + dy * dy;

				if (distSq <= 9)
					img.SetPixel(x, y, gold);
				else if (distSq <= 36)
					img.SetPixel(x, y, goldMid);
				else if (distSq <= 100)
					img.SetPixel(x, y, goldOuter);
			}
		}

		// Bras spiraux (3 bras qui tournent)
		for (int arm = 0; arm < 3; arm++)
		{
			float baseAngle = rotOffset + arm * Mathf.Tau / 3f;
			for (int t = 2; t < 12; t++)
			{
				float angle = baseAngle + t * 0.15f;
				int px = cx + Mathf.RoundToInt(Mathf.Cos(angle) * t);
				int py = cy + Mathf.RoundToInt(Mathf.Sin(angle) * t);

				SetPixelSafe(img, px, py, spiralBright);
				SetPixelSafe(img, px + 1, py, spiralBright);
			}
		}

		// Bord du portail (anneau)
		for (int a = 0; a < 32; a++)
		{
			float angle = Mathf.Tau * a / 32f;
			int r = 10 + ((a + frame) % 3 == 0 ? 1 : 0); // légère irrégularité
			int px = cx + Mathf.RoundToInt(Mathf.Cos(angle) * r);
			int py = cy + Mathf.RoundToInt(Mathf.Sin(angle) * r);
			SetPixelSafe(img, px, py, gold);
		}

		return ImageTexture.CreateFromImage(img);
	}

	// =====================================================================
	//  Bâtiment fantôme (world_echo) — 32×48, 4 variantes
	// =====================================================================

	public static ImageTexture[] GetGhostBuildingSprites()
	{
		if (_ghostBuildingTex != null) return _ghostBuildingTex;

		_ghostBuildingTex = new ImageTexture[4];
		_ghostBuildingTex[0] = GenerateGhostBuilding(0); // maison
		_ghostBuildingTex[1] = GenerateGhostBuilding(1); // tour
		_ghostBuildingTex[2] = GenerateGhostBuilding(2); // église
		_ghostBuildingTex[3] = GenerateGhostBuilding(3); // immeuble
		return _ghostBuildingTex;
	}

	private static ImageTexture GenerateGhostBuilding(int type)
	{
		Image img = Image.CreateEmpty(32, 48, false, Image.Format.Rgba8);
		Color wall = new(0.70f, 0.75f, 0.90f, 0.30f);
		Color wallDark = new(0.55f, 0.60f, 0.75f, 0.25f);
		Color wallOutline = new(0.80f, 0.85f, 0.95f, 0.40f);
		Color windowGlow = new(1f, 0.90f, 0.60f, 0.45f);

		switch (type)
		{
			case 0: // Maison avec toit pointu
				// Murs
				FillRect(img, 4, 24, 28, 46, wall, wallDark);
				// Toit
				for (int y = 8; y < 24; y++)
				{
					int halfW = (y - 8) * 12 / 16;
					for (int x = 16 - halfW; x <= 16 + halfW; x++)
						SetPixelSafe(img, x, y, wallOutline);
				}
				// Fenêtres
				FillRect(img, 8, 28, 12, 32, windowGlow, windowGlow);
				FillRect(img, 20, 28, 24, 32, windowGlow, windowGlow);
				// Porte
				FillRect(img, 13, 38, 19, 46, wallDark, wallDark);
				break;

			case 1: // Tour haute
				FillRect(img, 10, 4, 22, 46, wall, wallDark);
				// Créneaux
				for (int x = 10; x <= 22; x += 3)
					FillRect(img, x, 2, x + 1, 4, wallOutline, wallOutline);
				// Fenêtres étagées
				FillRect(img, 14, 10, 18, 13, windowGlow, windowGlow);
				FillRect(img, 14, 20, 18, 23, windowGlow, windowGlow);
				FillRect(img, 14, 30, 18, 33, windowGlow, windowGlow);
				break;

			case 2: // Église avec clocher
				// Nef
				FillRect(img, 2, 24, 30, 46, wall, wallDark);
				// Clocher central
				FillRect(img, 12, 4, 20, 24, wallOutline, wall);
				// Pointe
				for (int y = 0; y < 4; y++)
				{
					int halfW = (3 - y);
					SetPixelSafe(img, 16 - halfW, y, wallOutline);
					SetPixelSafe(img, 16 + halfW, y, wallOutline);
				}
				// Rosace
				FillRect(img, 13, 28, 19, 34, windowGlow, windowGlow);
				break;

			default: // Immeuble rectangulaire
				FillRect(img, 2, 8, 30, 46, wall, wallDark);
				// Étages de fenêtres
				for (int floor = 0; floor < 4; floor++)
				{
					int fy = 12 + floor * 9;
					FillRect(img, 6, fy, 10, fy + 3, windowGlow, windowGlow);
					FillRect(img, 14, fy, 18, fy + 3, windowGlow, windowGlow);
					FillRect(img, 22, fy, 26, fy + 3, windowGlow, windowGlow);
				}
				break;
		}

		// Contour global (scan des bords de pixels opaques)
		DrawSpriteOutline(img, wallOutline);

		return ImageTexture.CreateFromImage(img);
	}

	// =====================================================================
	//  Onde de résonance (foyer_resonance) — 48×48
	// =====================================================================

	public static ImageTexture GetResonanceWaveSprite()
	{
		if (_resonanceWaveTex != null) return _resonanceWaveTex;

		Image img = Image.CreateEmpty(48, 48, false, Image.Format.Rgba8);
		Color waveGold = new(1f, 0.85f, 0.40f, 0.50f);
		Color waveInner = new(1f, 0.90f, 0.55f, 0.30f);

		int cx = 24, cy = 24;

		// Anneau principal
		for (int a = 0; a < 64; a++)
		{
			float angle = Mathf.Tau * a / 64f;
			for (int r = 18; r <= 22; r++)
			{
				int px = cx + Mathf.RoundToInt(Mathf.Cos(angle) * r);
				int py = cy + Mathf.RoundToInt(Mathf.Sin(angle) * r);
				SetPixelSafe(img, px, py, r == 20 ? waveGold : waveInner);
			}
		}

		// Rayons
		for (int ray = 0; ray < 8; ray++)
		{
			float angle = Mathf.Tau * ray / 8f;
			for (int t = 8; t < 18; t++)
			{
				int px = cx + Mathf.RoundToInt(Mathf.Cos(angle) * t);
				int py = cy + Mathf.RoundToInt(Mathf.Sin(angle) * t);
				Color rayColor = waveGold;
				rayColor.A = 0.3f - (t - 8) * 0.02f;
				SetPixelSafe(img, px, py, rayColor);
			}
		}

		// Centre lumineux
		for (int y = cy - 3; y <= cy + 3; y++)
			for (int x = cx - 3; x <= cx + 3; x++)
			{
				int dx = x - cx, dy = y - cy;
				if (dx * dx + dy * dy <= 9)
					img.SetPixel(x, y, new Color(1f, 0.95f, 0.70f, 0.60f));
			}

		_resonanceWaveTex = ImageTexture.CreateFromImage(img);
		return _resonanceWaveTex;
	}

	// =====================================================================
	//  Fleur de mémoire (spontaneous_bloom) — 12×20, 2 frames
	// =====================================================================

	public static ImageTexture[] GetFlowerFrames(Color petalColor)
	{
		// On ne cache pas par couleur pour simplifier — les fleurs sont peu nombreuses
		ImageTexture[] frames = new ImageTexture[2];
		frames[0] = GenerateFlowerFrame(petalColor, 0);
		frames[1] = GenerateFlowerFrame(petalColor, 1);
		return frames;
	}

	private static ImageTexture GenerateFlowerFrame(Color petalColor, int frame)
	{
		Image img = Image.CreateEmpty(12, 20, false, Image.Format.Rgba8);
		Color stem = new(0.30f, 0.55f, 0.20f);
		Color stemDark = new(0.22f, 0.42f, 0.15f);
		Color center = new(0.95f, 0.85f, 0.30f);

		// Tige (pixel art, 2px de large, légèrement courbe)
		for (int y = 10; y < 20; y++)
		{
			int xOff = y > 16 ? 0 : (y < 13 ? -1 : 0);
			img.SetPixel(5 + xOff, y, stem);
			img.SetPixel(6 + xOff, y, stemDark);
		}

		// Petite feuille
		img.SetPixel(7, 15, stem);
		img.SetPixel(8, 14, stem);

		// Pétales (frame 0 = normal, frame 1 = légèrement ouvert)
		int petalR = frame == 0 ? 3 : 4;
		int cx = 6, cy = 7;

		for (int dy = -petalR; dy <= petalR; dy++)
		{
			for (int dx = -petalR; dx <= petalR; dx++)
			{
				if (dx * dx + dy * dy > petalR * petalR) continue;
				int px = cx + dx, py = cy + dy;
				if (px < 0 || px >= 12 || py < 0 || py >= 20) continue;

				if (dx * dx + dy * dy <= 1)
					img.SetPixel(px, py, center); // Centre de la fleur
				else
					img.SetPixel(px, py, petalColor);
			}
		}

		// Glow subtil autour
		Color glow = new(petalColor.R, petalColor.G, petalColor.B, 0.15f);
		for (int dy = -(petalR + 2); dy <= petalR + 2; dy++)
		{
			for (int dx = -(petalR + 2); dx <= petalR + 2; dx++)
			{
				int distSq = dx * dx + dy * dy;
				if (distSq <= petalR * petalR || distSq > (petalR + 2) * (petalR + 2))
					continue;
				int px = cx + dx, py = cy + dy;
				if (px < 0 || px >= 12 || py < 0 || py >= 20) continue;
				if (img.GetPixel(px, py).A > 0.1f) continue;
				img.SetPixel(px, py, glow);
			}
		}

		return ImageTexture.CreateFromImage(img);
	}

	// =====================================================================
	//  Helpers de dessin
	// =====================================================================

	private static void SetPixelSafe(Image img, int x, int y, Color color)
	{
		if (x >= 0 && x < img.GetWidth() && y >= 0 && y < img.GetHeight())
		{
			if (color.A < 0.99f)
			{
				Color existing = img.GetPixel(x, y);
				if (existing.A > 0.01f)
					img.SetPixel(x, y, existing.Lerp(color, color.A));
				else
					img.SetPixel(x, y, color);
			}
			else
			{
				img.SetPixel(x, y, color);
			}
		}
	}

	private static void FillRect(Image img, int x1, int y1, int x2, int y2, Color fill, Color alt)
	{
		for (int y = y1; y < y2; y++)
			for (int x = x1; x < x2; x++)
				SetPixelSafe(img, x, y, (x + y) % 2 == 0 ? fill : alt);
	}

	private static void DrawSpriteOutline(Image img, Color outlineColor)
	{
		int w = img.GetWidth();
		int h = img.GetHeight();
		Image copy = (Image)img.Duplicate();

		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				if (copy.GetPixel(x, y).A > 0.1f) continue;

				bool hasNeighbor = false;
				if (x > 0 && copy.GetPixel(x - 1, y).A > 0.1f) hasNeighbor = true;
				if (x < w - 1 && copy.GetPixel(x + 1, y).A > 0.1f) hasNeighbor = true;
				if (y > 0 && copy.GetPixel(x, y - 1).A > 0.1f) hasNeighbor = true;
				if (y < h - 1 && copy.GetPixel(x, y + 1).A > 0.1f) hasNeighbor = true;

				if (hasNeighbor)
					img.SetPixel(x, y, outlineColor);
			}
		}
	}
}
