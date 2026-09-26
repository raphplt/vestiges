using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Entrée du menu de l'accueil : texte seul, sans cadre. Au focus (souris ou manette),
/// une braise s'allume devant le texte et le décale. Survol et focus ne font qu'un : un seul choix brille.
/// </summary>
public partial class HubMenuButton : Button
{
	private const float RestIndent = 36f;
	private const float FocusIndent = 52f;
	private const int PixelSize = 4;

	private static readonly Color Idle = new(0.72f, 0.68f, 0.62f);
	private static readonly Color Lit = new(0.96f, 0.84f, 0.5f);
	private static readonly Color DisabledText = new(0.36f, 0.35f, 0.4f);
	private static readonly Color EmberCore = new(1f, 0.86f, 0.5f);
	private static readonly Color EmberEdge = new(0.88f, 0.48f, 0.22f);

	private readonly StyleBoxEmpty _style = new();
	private float _glow;
	private bool _silentFocus;
	private Tween _tween;

	public void Setup(string text, int fontSize, Font font)
	{
		Text = text;
		Flat = true;
		Alignment = HorizontalAlignment.Left;
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		CustomMinimumSize = new Vector2(420f, fontSize + 16f);
		AddThemeFontOverride("font", font);
		AddThemeFontSizeOverride("font_size", fontSize);
		AddThemeConstantOverride("outline_size", 8);
		AddThemeColorOverride("font_outline_color", new Color(0.03f, 0.03f, 0.06f, 0.85f));
		ApplyColors(Idle);
		AddThemeColorOverride("font_disabled_color", DisabledText);
		_style.ContentMarginLeft = RestIndent;
		foreach (string state in new[] { "normal", "hover", "pressed", "focus", "hover_pressed", "disabled" })
			AddThemeStyleboxOverride(state, _style);
	}

	public override void _Ready()
	{
		MouseEntered += () =>
		{
			if (!Disabled)
				GrabFocus();
		};
		FocusEntered += () =>
		{
			if (!_silentFocus)
				AudioManager.PlayUI("sfx_menu_survol");
			Animate(1f);
		};
		FocusExited += () => Animate(0f);
		ButtonDown += () => AudioManager.PlayUI("sfx_menu_clic");
	}

	/// <summary>Donne le focus sans son de survol (arrivée sur l'écran, retour d'un sous-écran).</summary>
	public void GrabFocusSilently()
	{
		_silentFocus = true;
		GrabFocus();
		_silentFocus = false;
	}

	public override void _Draw()
	{
		if (_glow <= 0.01f || Disabled)
			return;
		// Braise en losange de pixels de 4, centrée sur la ligne de texte.
		float y = Mathf.Round(Size.Y * 0.5f / PixelSize) * PixelSize - PixelSize * 0.5f;
		float x = 4f + (1f - _glow) * -8f;
		Color core = new(EmberCore, _glow);
		Color edge = new(EmberEdge, _glow);
		for (int row = -2; row <= 2; row++)
		{
			int half = 2 - Mathf.Abs(row);
			for (int column = -half; column <= half; column++)
			{
				bool inner = Mathf.Abs(row) + Mathf.Abs(column) <= 1;
				DrawRect(new Rect2(x + (column + 2) * PixelSize, y + row * PixelSize, PixelSize, PixelSize), inner ? core : edge);
			}
		}
	}

	private void Animate(float target)
	{
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenMethod(Callable.From<float>(SetGlow), _glow, target, 0.14f)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
	}

	private void SetGlow(float glow)
	{
		_glow = glow;
		_style.ContentMarginLeft = Mathf.Round(Mathf.Lerp(RestIndent, FocusIndent, glow));
		ApplyColors(Idle.Lerp(Lit, glow));
		QueueRedraw();
	}

	private void ApplyColors(Color color)
	{
		AddThemeColorOverride("font_color", color);
		AddThemeColorOverride("font_hover_color", color);
		AddThemeColorOverride("font_focus_color", color);
		AddThemeColorOverride("font_pressed_color", Lit);
		AddThemeColorOverride("font_hover_pressed_color", Lit);
	}
}
