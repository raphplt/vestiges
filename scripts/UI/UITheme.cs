using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Constantes et helpers UI partages entre HubScreen et SettingsScreen.
/// Couleurs de la charte graphique, NinePatch factory, styles boutons.
/// </summary>
public static class UITheme
{
	// --- Paths ---
	public const string UiPath = "res://assets/ui/";
	public const string MenusPath = UiPath + "menus/";
	public const string IconsPath = UiPath + "icons/";

	// --- Couleurs charte graphique ---
	public static readonly Color GoldColor = new(0.83f, 0.66f, 0.26f);
	public static readonly Color GoldBright = new(0.9f, 0.78f, 0.39f);
	public static readonly Color GoldDim = new(0.63f, 0.47f, 0.16f);
	public static readonly Color TextColor = new(0.72f, 0.7f, 0.66f);
	public static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);
	public static readonly Color TextVeryDim = new(0.35f, 0.35f, 0.4f);
	public static readonly Color BgDark = new(0.04f, 0.05f, 0.09f);
	public static readonly Color CyanEssence = new(0.37f, 0.77f, 0.77f);
	public static readonly Color GreenKit = new(0.4f, 0.6f, 0.4f);

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

	/// <summary>
	/// Applique le style NinePatch + hover anime (scale + tint).
	/// A utiliser sur les boutons du menu principal (dans CenterContainer).
	/// </summary>
	public static void ApplyAnimatedButtonStyle(
		Button btn,
		Texture2D normalTex,
		Texture2D hoverTex,
		Texture2D pressedTex,
		Texture2D disabledTex)
	{
		ApplyButtonStyle(btn, normalTex, hoverTex, pressedTex, disabledTex);

		btn.PivotOffset = btn.Size / 2;
		btn.Resized += () => btn.PivotOffset = btn.Size / 2;

		btn.MouseEntered += () =>
		{
			Tween tween = btn.CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(btn, "scale", new Vector2(1.05f, 1.05f), 0.12f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(btn, "modulate", new Color(1.15f, 1.1f, 1.0f), 0.12f)
				.SetTrans(Tween.TransitionType.Sine);
		};

		btn.MouseExited += () =>
		{
			Tween tween = btn.CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(btn, "scale", Vector2.One, 0.10f)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.In);
			tween.TweenProperty(btn, "modulate", Colors.White, 0.10f)
				.SetTrans(Tween.TransitionType.Sine);
		};

		btn.ButtonDown += () =>
		{
			Tween tween = btn.CreateTween();
			tween.TweenProperty(btn, "scale", new Vector2(0.95f, 0.95f), 0.05f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(btn, "scale", Vector2.One, 0.08f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		};
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
