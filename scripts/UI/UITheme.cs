using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Rôle d'un texte d'interface : sa taille vient de l'échelle commune, jamais d'un nombre écrit dans l'écran.</summary>
public enum TextRole { Caption, Small, Body, Lead, Subhead, Heading, Title, Banner, Display }

/// <summary>Graisse de Saira Semi Condensed : Medium (police du thème), SemiBold, Bold.</summary>
public enum TextWeight { Regular, Strong, Bold }

/// <summary>
/// Système visuel commun des écrans d'interface (plan 04 lot B) : polices, échelle typographique, couleurs de la
/// charte, fabrique de libellés, NinePatch et styles de boutons.
/// </summary>
public static class UITheme
{
	// --- Typographie ---
	// Échelle « confort » en base 1080p : rien sous 14 px, soit environ 9 px physiques en 1280×720.
	private static readonly int[] RoleSizes = { 14, 15, 16, 18, 20, 24, 30, 36, 46 };
	private static readonly StringName TextBaseMeta = "ui_text_base";
	private static Font _bodyFont;
	private static Font _strongFont;
	private static Font _boldFont;

	public static Font BodyFont => _bodyFont ??= GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-Medium.ttf");
	public static Font StrongFont => _strongFont ??= GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf");
	public static Font BoldFont => _boldFont ??= GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-Bold.ttf");

	public static Font FontFor(TextWeight weight) => weight switch
	{
		TextWeight.Strong => StrongFont,
		TextWeight.Bold => BoldFont,
		_ => BodyFont,
	};

	/// <summary>Taille d'un rôle en base 1080p, taille du texte du joueur comprise.</summary>
	public static int FontSize(TextRole role) => Scaled(RoleSizes[(int)role]);

	/// <summary>Taille hors échelle (score géant du bilan, compteurs animés), agrandie comme le reste.</summary>
	public static int Scaled(int basePixels) => Mathf.RoundToInt(basePixels * TextSettings.Scale);

	/// <summary>Donne à un contrôle la taille d'un rôle, retenue pour suivre un changement de taille du texte.</summary>
	public static void SetTextRole(Control control, TextRole role) => SetTextSize(control, RoleSizes[(int)role]);

	/// <summary>Comme <see cref="SetTextRole"/> pour une taille hors échelle.</summary>
	public static void SetTextSize(Control control, int basePixels)
	{
		control.SetMeta(TextBaseMeta, basePixels);
		control.AddThemeFontSizeOverride("font_size", Scaled(basePixels));
	}

	/// <summary>Taille figée, insensible à la taille du texte : réservée aux textes placés au pixel près.</summary>
	public static void SetFixedTextSize(Control control, int pixels)
	{
		if (control.HasMeta(TextBaseMeta))
			control.RemoveMeta(TextBaseMeta);
		control.AddThemeFontSizeOverride("font_size", pixels);
	}

	/// <summary>
	/// Réapplique la taille du texte à l'interface déjà construite sous <paramref name="root"/>. Ne descend pas dans
	/// le monde : sous un Node2D, seuls les CanvasLayer sont visités, jamais les milliers de décors et de créatures.
	/// </summary>
	public static void RefreshTextScale(Node root)
	{
		if (root is Control control && control.HasMeta(TextBaseMeta))
			control.AddThemeFontSizeOverride("font_size", Scaled(control.GetMeta(TextBaseMeta).AsInt32()));
		bool world = root is Node2D;
		foreach (Node child in root.GetChildren())
		{
			if (!world || child is CanvasLayer)
				RefreshTextScale(child);
		}
	}

	/// <summary>Libellé d'interface : rôle, couleur, graisse, contour facultatif pour les textes posés sur le décor.</summary>
	public static Label MakeLabel(string text, TextRole role, Color color, TextWeight weight = TextWeight.Regular,
		HorizontalAlignment align = HorizontalAlignment.Left, int outline = 0)
	{
		Label label = new() { Text = text, HorizontalAlignment = align, MouseFilter = Control.MouseFilterEnum.Ignore };
		if (weight != TextWeight.Regular)
			label.AddThemeFontOverride("font", FontFor(weight));
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
		if (ResourceLoader.Exists(path))
			return GD.Load<Texture2D>(path);
		GD.PushWarning($"[UITheme] Missing texture: {path}");
		return null;
	}

	/// <summary>Cree un StyleBoxTexture NinePatch depuis une texture.</summary>
	public static StyleBoxTexture CreateNinePatch(Texture2D texture, int left, int top, int right, int bottom)
	{
		StyleBoxTexture sbt = new()
		{
			Texture = texture,
			RegionRect = new Rect2(0, 0, texture.GetWidth(), texture.GetHeight()),
			AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile,
			AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile
		};

		sbt.TextureMarginLeft = left;
		sbt.TextureMarginTop = top;
		sbt.TextureMarginRight = right;
		sbt.TextureMarginBottom = bottom;

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
		if (normalTex != null)
			btn.AddThemeStyleboxOverride("normal", CreateNinePatch(normalTex, 4, 4, 4, 4));
		if (hoverTex != null)
			btn.AddThemeStyleboxOverride("hover", CreateNinePatch(hoverTex, 4, 4, 4, 4));
		if (pressedTex != null)
			btn.AddThemeStyleboxOverride("pressed", CreateNinePatch(pressedTex, 4, 4, 4, 4));
		if (disabledTex != null)
			btn.AddThemeStyleboxOverride("disabled", CreateNinePatch(disabledTex, 4, 4, 4, 4));

		btn.AddThemeColorOverride("font_color", GoldColor);
		btn.AddThemeColorOverride("font_hover_color", GoldBright);
		btn.AddThemeColorOverride("font_pressed_color", GoldBright);
		btn.AddThemeColorOverride("font_disabled_color", TextVeryDim);
		WireButtonAudio(btn);
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
		WireButtonAudio(btn);
	}

	/// <summary>Branche les SFX de survol/clic une seule fois par bouton.</summary>
	public static void WireButtonAudio(Button btn, bool hoverOnlyIfEnabled = true)
	{
		if (btn == null || btn.HasMeta("ui_sfx_wired"))
			return;

		btn.SetMeta("ui_sfx_wired", true);

		btn.MouseEntered += () =>
		{
			if (hoverOnlyIfEnabled && btn.Disabled)
				return;
			AudioManager.PlayUI("sfx_menu_survol");
		};

		btn.ButtonDown += () =>
		{
			if (btn.Disabled)
				return;
			AudioManager.PlayUI("sfx_menu_clic");
		};
	}
}
