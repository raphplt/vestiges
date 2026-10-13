using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Rôle d'un texte d'interface : sa taille vient de l'échelle commune, jamais d'un nombre écrit dans l'écran.</summary>
public enum TextRole { Caption, Small, Body, Lead, Subhead, Heading, Title, Banner, Display, Section }

/// <summary>Graisse de Pixel Operator : normale ou grasse pour les accents. Les titres en Alkhemikal l'ignorent.</summary>
public enum TextWeight { Regular, Strong, Bold }

/// <summary>
/// Système visuel commun des écrans d'interface (plan 04 lot B) : polices, échelle typographique, couleurs de la
/// charte, fabrique de libellés, NinePatch et styles de boutons.
/// </summary>
public static class UITheme
{
	// --- Typographie ---
	// Échelle du Campement approuvée, en base 1080p ; les petits textes du HUD ont leur grille native de 8 px.
	private static readonly int[] RoleSizes = { 24, 24, 32, 32, 32, 32, 48, 64, 64, 32 };
	private static Font _bodyFont;
	private static Font _strongFont;
	private static Font _smallFont;
	private static Font _smallStrongFont;
	private static Font _titleFont;

	public static Font BodyFont => _bodyFont ??= LoadFont("PixelOperator.ttf");
	public static Font StrongFont => _strongFont ??= LoadFont("PixelOperator-Bold.ttf");
	public static Font BoldFont => StrongFont;
	public static Font SmallFont => _smallFont ??= LoadFont("PixelOperator8.ttf");
	public static Font SmallStrongFont => _smallStrongFont ??= LoadFont("PixelOperator8-Bold.ttf");
	/// <summary>Alkhemikal (jeti, CC BY 4.0) : titres et en-têtes seulement, jamais les chiffres ; grille de 16 px, sans graisse grasse.</summary>
	public static Font TitleFont => _titleFont ??= LoadFont("Alkhemikal.ttf", "alkhemikal");

	private static Font LoadFont(string name, string family = "pixel-operator")
	{
		// Même ressource que le thème global : les textes hérités gardent aussi leurs pixels sans lissage.
		FontFile font = GD.Load<FontFile>($"res://assets/fonts/{family}/{name}");
		font.Antialiasing = TextServer.FontAntialiasing.None;
		return font;
	}

	/// <summary>Police native adaptée au texte du HUD ou du monde, sans réduire les glyphes de 16 px.</summary>
	public static Font CompactFont(int size, TextWeight weight = TextWeight.Regular) => size < 16
		? weight == TextWeight.Regular ? SmallFont : SmallStrongFont
		: FontFor(weight);

	/// <summary>Taille du HUD ou du monde sur sa grille de 8 px.</summary>
	public static int CompactSize(int size) => Mathf.Max(8, Mathf.RoundToInt(size / 8f) * 8);

	public static void SetCompactText(Control control, int size, TextWeight weight = TextWeight.Regular)
	{
		int nativeSize = CompactSize(size);
		control.AddThemeFontOverride("font", CompactFont(nativeSize, weight));
		control.AddThemeFontSizeOverride("font_size", nativeSize);
	}

	/// <summary>Rôles écrits en Alkhemikal : titres, grands affichages et en-têtes de section.</summary>
	public static bool IsTitleRole(TextRole role) => role is TextRole.Title or TextRole.Banner or TextRole.Display or TextRole.Section;

	public static Font FontFor(TextRole role, TextWeight weight) => IsTitleRole(role) ? TitleFont : FontFor(weight);

	/// <summary>Titre compact du HUD ou du monde : Alkhemikal sur sa grille de 16 px.</summary>
	public static void SetCompactTitle(Control control, int size)
	{
		control.AddThemeFontOverride("font", TitleFont);
		control.AddThemeFontSizeOverride("font_size", Mathf.Max(16, Mathf.RoundToInt(size / 16f) * 16));
	}

	public static Font FontFor(TextWeight weight) => weight switch
	{
		TextWeight.Strong => StrongFont,
		TextWeight.Bold => BoldFont,
		_ => BodyFont,
	};

	/// <summary>Taille d'un rôle en base 1080p.</summary>
	public static int FontSize(TextRole role) => RoleSizes[(int)role];

	/// <summary>Donne à un contrôle la taille d'un rôle.</summary>
	public static void SetTextRole(Control control, TextRole role)
	{
		if (!control.HasThemeFontOverride("font"))
			control.AddThemeFontOverride("font", IsTitleRole(role) ? TitleFont : BodyFont);
		control.AddThemeFontSizeOverride("font_size", FontSize(role));
	}

	public static void ApplyTypography(Control control, TextRole role, TextWeight weight = TextWeight.Regular)
	{
		control.AddThemeFontOverride("font", FontFor(role, weight));
		SetTextRole(control, role);
		control.AddThemeConstantOverride("outline_size", 2);
		control.AddThemeColorOverride("font_outline_color", OutlineColor);
	}

	/// <summary>Libellé d'interface : rôle, couleur, graisse, contour facultatif pour les textes posés sur le décor.</summary>
	public static Label MakeLabel(string text, TextRole role, Color color, TextWeight weight = TextWeight.Regular,
		HorizontalAlignment align = HorizontalAlignment.Left, int outline = 2)
	{
		Label label = new() { Text = text, HorizontalAlignment = align, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontOverride("font", FontFor(role, weight));
		SetTextRole(label, role);
		label.AddThemeColorOverride("font_color", color);
		if (outline > 0)
		{
			label.AddThemeConstantOverride("outline_size", outline);
			label.AddThemeColorOverride("font_outline_color", OutlineColor);
		}
		return label;
	}

	// --- Paths ---
	public const string UiPath = "res://assets/ui/";
	public const string MenusPath = UiPath + "menus/";
	public const string IconsPath = UiPath + "icons/";

	// --- Couleurs charte graphique ---
	public static readonly Color GoldColor = new(0.83f, 0.66f, 0.26f);
	public static readonly Color GoldBright = new(0.9f, 0.78f, 0.39f);
	public static readonly Color GoldDim = new(0.63f, 0.47f, 0.16f);
	public static readonly Color TextLight = new(0.92f, 0.9f, 0.85f);
	public static readonly Color TextColor = new(0.72f, 0.7f, 0.66f);
	public static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);
	public static readonly Color TextVeryDim = new(0.35f, 0.35f, 0.4f);
	public static readonly Color BgDark = new(0.04f, 0.05f, 0.09f);
	public static readonly Color CyanEssence = new(0.37f, 0.77f, 0.77f);
	public static readonly Color GreenKit = new(0.4f, 0.6f, 0.4f);
	/// <summary>Contour des textes posés sur une image ou sur le jeu.</summary>
	public static readonly Color OutlineColor = new(0.02f, 0.02f, 0.04f, 0.9f);

	/// <summary>Charge une texture, retourne null si absente.</summary>
	public static Texture2D LoadTex(string path)
	{
		if (path.StartsWith(MenusPath, System.StringComparison.Ordinal))
		{
			Texture2D stone = MenuSkin.TextureFor(path[MenusPath.Length..]);
			if (stone != null) return stone;
			string skin = ScreenArt.Folder + path[MenusPath.Length..];
			if (ResourceLoader.Exists(skin))
				path = skin;
		}
		if (ResourceLoader.Exists(path))
			return GD.Load<Texture2D>(path);
		GD.PushWarning($"[UITheme] Missing texture: {path}");
		return null;
	}

	/// <summary>Neuf régions raccordables : les textures M6 gardent leur grain et leurs tranches à toutes les tailles.</summary>
	public static StyleBoxTexture CreateNinePatch(Texture2D texture, int left, int top, int right, int bottom)
	{
		int textureMargin = MenuStoneTextures.IsFramed(texture) ? MenuStoneTextures.CornerMargin(texture) : 0;
		StyleBoxTexture sbt = new()
		{
			Texture = texture,
			RegionRect = new Rect2(0, 0, texture.GetWidth(), texture.GetHeight()),
			AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile,
			AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile
		};

		sbt.TextureMarginLeft = textureMargin > 0 ? textureMargin : left;
		sbt.TextureMarginTop = textureMargin > 0 ? textureMargin : top;
		sbt.TextureMarginRight = textureMargin > 0 ? textureMargin : right;
		sbt.TextureMarginBottom = textureMargin > 0 ? textureMargin : bottom;
		// La découpe irrégulière des coins n'impose pas davantage de vide entre les contrôles.
		if (textureMargin > 0) left = top = right = bottom = MenuStoneTextures.BorderSize;

		sbt.ContentMarginLeft = left + 2;
		sbt.ContentMarginTop = top + 2;
		sbt.ContentMarginRight = right + 2;
		sbt.ContentMarginBottom = bottom + 2;

		return sbt;
	}

	/// <summary>Applique le style NinePatch standard sur un bouton.</summary>
	public static void ApplyButtonStyle(
		Button btn,
		Texture2D normalTex,
		Texture2D hoverTex,
		Texture2D pressedTex,
		Texture2D disabledTex)
	{
		btn.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		if (normalTex != null)
			btn.AddThemeStyleboxOverride("normal", CreateNinePatch(normalTex, 4, 4, 4, 4));
		if (hoverTex != null)
			btn.AddThemeStyleboxOverride("hover", CreateNinePatch(hoverTex, 4, 4, 4, 4));
		if (pressedTex != null)
		{
			btn.AddThemeStyleboxOverride("pressed", CreateNinePatch(pressedTex, 4, 4, 4, 4));
			btn.AddThemeStyleboxOverride("hover_pressed", CreateNinePatch(pressedTex, 4, 4, 4, 4));
		}
		if (disabledTex != null)
			btn.AddThemeStyleboxOverride("disabled", CreateNinePatch(disabledTex, 4, 4, 4, 4));

		btn.AddThemeColorOverride("font_color", TextColor);
		btn.AddThemeColorOverride("font_hover_color", TextLight);
		btn.AddThemeColorOverride("font_focus_color", TextLight);
		btn.AddThemeColorOverride("font_pressed_color", GoldBright);
		btn.AddThemeColorOverride("font_hover_pressed_color", GoldBright);
		btn.AddThemeColorOverride("font_disabled_color", TextVeryDim);
		// Clavier et manette : le bouton choisi s'éclaire comme au survol, pas de liseré à côté (DECISIONS §150).
		if (hoverTex != null)
			btn.AddThemeStyleboxOverride("focus", CreateNinePatch(hoverTex, 4, 4, 4, 4));
		else
			ApplyFocusStyle(btn);
		WireButtonAudio(btn);
	}

	/// <summary>Repère de focus des contrôles sans pierre éclairée (défilements, curseurs) : un liseré latéral.</summary>
	public static void ApplyFocusStyle(Control control)
	{
		StyleBoxFlat focus = new()
		{
			DrawCenter = false,
			BorderWidthLeft = 3,
			BorderColor = GoldBright,
		};
		control.AddThemeStyleboxOverride("focus", focus);
	}

	/// <summary>Applique le style NinePatch pour un onglet.</summary>
	public static void ApplyTabStyle(
		Button btn, bool active,
		Texture2D normalTex, Texture2D hoverTex, Texture2D activeTex)
	{
		Texture2D tex = active ? activeTex : normalTex;
		if (tex != null)
		{
			StyleBoxTexture style = CreateNinePatch(tex, 4, 4, 4, 4);
			btn.AddThemeStyleboxOverride("normal", style);
			btn.AddThemeStyleboxOverride("hover", CreateNinePatch(hoverTex ?? tex, 4, 4, 4, 4));
			btn.AddThemeStyleboxOverride("pressed", CreateNinePatch(activeTex ?? tex, 4, 4, 4, 4));
		}

		Color fontColor = active ? GoldBright : TextDim;
		btn.AddThemeColorOverride("font_color", fontColor);
		btn.AddThemeColorOverride("font_hover_color", GoldColor);
		btn.AddThemeColorOverride("font_pressed_color", GoldBright);
		if (hoverTex != null)
			btn.AddThemeStyleboxOverride("focus", CreateNinePatch(hoverTex, 4, 4, 4, 4));
		else
			ApplyFocusStyle(btn);
		WireButtonAudio(btn);
	}

	/// <summary>Branche les SFX de survol/clic une seule fois par bouton.</summary>
	public static void WireButtonAudio(Button btn, bool hoverOnlyIfEnabled = true)
	{
		if (btn == null || btn.HasMeta("ui_sfx_wired"))
			return;

		btn.SetMeta("ui_sfx_wired", true);

		void PlayFocusSound()
		{
			if (hoverOnlyIfEnabled && btn.Disabled)
				return;
			AudioManager.PlayUI("sfx_menu_survol");
		}
		btn.MouseEntered += PlayFocusSound;
		// Même repère au clavier/manette ; la limitation UI absorbe un survol et un focus simultanés.
		btn.FocusEntered += PlayFocusSound;

		btn.ButtonDown += () =>
		{
			if (btn.Disabled)
				return;
			AudioManager.PlayUI("sfx_menu_clic");
		};
	}
}
