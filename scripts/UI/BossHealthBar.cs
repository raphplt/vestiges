using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Barre de boss en haut de l'écran (plan 07 B1b, DECISIONS §85) : nom et PV du Souverain, de la Barrière ou de
/// l'Indicible, un cran par partie à PV propres (battant). Sous le temps, le nom du biome et la ligne d'aide des
/// événements, qu'elle ne recouvre pas. La traînée claire montre le coup qui vient d'être porté.
/// Enfant de la racine mise à l'échelle du HUD : coordonnées en unités de référence 960×540.
/// </summary>
public partial class BossHealthBar : Control
{
	private const float Top = 62f;
	private const float Width = 300f;
	private const float NameHeight = 14f;
	private const float BarHeight = 7f;
	private const float FadeInSec = 0.25f;
	private const float FadeOutSec = 0.6f;
	private const float ChipHoldSec = 0.35f;
	private const float ChipSpeed = 0.6f;
	private const float DefeatFlashSec = 0.25f;

	private static readonly Color Ink = new("1a1a2e");
	private static readonly Color Metal = new("6b6161");
	private static readonly Color Paper = new("e8e0d4");
	private static readonly Color Gold = new("d4a843");
	private static readonly Color Rust = new("c4432b");

	private EventBus _eventBus;
	private Label _name;
	private Rect2 _bar;
	private int _encounterId;
	private int _notches;
	private float _ratio;
	private float _chipRatio;
	private float _chipHold;
	private float _flash;
	private Tween _fade;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		AnchorLeft = 0.5f;
		AnchorRight = 0.5f;
		OffsetLeft = -Width / 2f;
		OffsetRight = Width / 2f;
		OffsetTop = Top;
		OffsetBottom = Top + NameHeight + BarHeight + 6f;
		Modulate = Colors.Transparent;
		Visible = false;
		_bar = new Rect2(3f, NameHeight + 2f, Width - 6f, BarHeight);

		_name = new Label { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Center };
		_name.AddThemeFontSizeOverride("font_size", 11);
		_name.AddThemeColorOverride("font_color", Paper);
		_name.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
		_name.AddThemeConstantOverride("outline_size", 3);
		_name.Position = Vector2.Zero;
		_name.Size = new Vector2(Width, NameHeight);
		AddChild(_name);

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.BossEncounterStarted += OnStarted;
		_eventBus.BossHealthChanged += OnHealthChanged;
		_eventBus.BossEncounterEnded += OnEnded;
		SetProcess(false);
	}

	public override void _ExitTree()
	{
		_eventBus.BossEncounterStarted -= OnStarted;
		_eventBus.BossHealthChanged -= OnHealthChanged;
		_eventBus.BossEncounterEnded -= OnEnded;
	}

	/// <summary>Rencontre affichée, 0 si aucune (bancs).</summary>
	public int EncounterId => Visible ? _encounterId : 0;
	public float ShownRatio => _ratio;

	private void OnStarted(int encounterId, string bossName, float maxHp, int notches)
	{
		_encounterId = encounterId;
		_notches = notches;
		_ratio = _chipRatio = 1f;
		_flash = 0f;
		_name.Text = bossName;
		Visible = true;
		_fade?.Kill();
		_fade = CreateTween();
		_fade.TweenProperty(this, "modulate", Colors.White, FadeInSec);
		QueueRedraw();
	}

	private void OnHealthChanged(int encounterId, float currentHp, float maxHp)
	{
		if (encounterId != _encounterId)
			return;
		float ratio = maxHp > 0f ? Mathf.Clamp(currentHp / maxHp, 0f, 1f) : 0f;
		if (ratio < _ratio)
			_chipHold = ChipHoldSec;
		_ratio = ratio;
		_chipRatio = Mathf.Max(_chipRatio, _ratio);
		SetProcess(true);
		QueueRedraw();
	}

	private void OnEnded(int encounterId, bool defeated)
	{
		if (encounterId != _encounterId)
			return;
		_encounterId = 0;
		if (defeated)
		{
			_ratio = 0f;
			_flash = DefeatFlashSec;
			SetProcess(true);
		}
		_fade?.Kill();
		_fade = CreateTween();
		if (defeated)
			_fade.TweenInterval(DefeatFlashSec);
		_fade.TweenProperty(this, "modulate", Colors.Transparent, FadeOutSec);
		_fade.TweenCallback(Callable.From(() => Visible = _encounterId != 0));
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_flash = Mathf.Max(0f, _flash - dt);
		if (_chipHold > 0f)
			_chipHold -= dt;
		else
			_chipRatio = Mathf.Max(_ratio, _chipRatio - dt * ChipSpeed);
		QueueRedraw();
		SetProcess(_chipRatio > _ratio || _flash > 0f);
	}

	public override void _Draw()
	{
		Rect2 plate = _bar.Grow(3f);
		Color border = Metal.Lerp(Gold, 0.25f);
		// Plaque aux coins coupés et ombre courte, comme les PV du joueur.
		DrawRect(new Rect2(plate.Position + new Vector2(2f, 2f), plate.Size), Ink.Darkened(0.65f) with { A = 0.55f });
		DrawRect(new Rect2(plate.Position.X + 2f, plate.Position.Y, plate.Size.X - 4f, plate.Size.Y), border);
		DrawRect(new Rect2(plate.Position.X, plate.Position.Y + 2f, plate.Size.X, plate.Size.Y - 4f), border);
		DrawRect(plate.Grow(-1f), Ink.Darkened(0.4f) with { A = 0.97f });
		DrawRect(_bar, Ink.Darkened(0.6f));

		float chipWidth = Mathf.Floor(_bar.Size.X * _chipRatio);
		if (chipWidth > 0f)
			DrawRect(new Rect2(_bar.Position, new Vector2(chipWidth, _bar.Size.Y)), Paper.Darkened(0.2f));
		float fillWidth = Mathf.Floor(_bar.Size.X * _ratio);
		if (fillWidth > 0f)
		{
			DrawRect(new Rect2(_bar.Position, new Vector2(fillWidth, _bar.Size.Y)), Rust.Darkened(0.16f));
			DrawRect(new Rect2(_bar.Position, new Vector2(fillWidth, 1f)), Rust.Lerp(Paper, 0.4f));
			DrawRect(new Rect2(_bar.Position + new Vector2(0f, _bar.Size.Y - 2f), new Vector2(fillWidth, 2f)), Rust.Darkened(0.45f));
		}
		if (_flash > 0f)
			DrawRect(_bar, Paper with { A = _flash / DefeatFlashSec });

		// Un cran par battant : un filet sombre à travers la barre et une goupille dorée dessous.
		for (int notch = 1; notch < _notches; notch++)
		{
			float x = _bar.Position.X + Mathf.Floor(_bar.Size.X * notch / _notches);
			DrawRect(new Rect2(x, _bar.Position.Y, 1f, _bar.Size.Y), Ink);
			DrawRect(new Rect2(x - 1f, _bar.End.Y + 1f, 3f, 2f), Gold.Darkened(0.3f));
		}
		DrawRect(new Rect2(plate.Position.X + 4f, plate.Position.Y, 7f, 1f), Gold.Darkened(0.2f));
		DrawRect(new Rect2(plate.End.X - 11f, plate.End.Y - 1f, 7f, 1f), Gold.Darkened(0.2f));
	}
}
