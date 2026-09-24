using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Présentation des micro-événements : bandeau (titre, objectif, temps restant), flèche au bord
/// de l'écran vers une cible hors champ, bilan de fin et annonce des variantes abattues.
/// Enfant de la racine mise à l'échelle du HUD : coordonnées en unités de référence 960×540.
/// </summary>
public partial class RunEventHud : Control
{
    private const float BannerWidth = 300f;
    private const float BannerTop = 66f;
    private const float ResultHoldSec = 3.5f;
    private const float ToastHoldSec = 2.2f;
    private const float PointerMargin = 26f;

    private static readonly Color Gold = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);
    private static readonly Color WhiteOff = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    private static readonly Color GrayLight = new(0x9E / 255f, 0x94 / 255f, 0x94 / 255f);
    private static readonly Color Success = new(0.55f, 0.85f, 0.45f);
    private static readonly Color PointerFill = new(0.95f, 0.78f, 0.3f);

    private EventBus _eventBus;
    private PanelContainer _banner;
    private Label _titleLabel;
    private Label _objectiveLabel;
    private Label _timeLabel;
    private ColorRect _timeFill;
    private ColorRect _progressFill;
    private Label _toastLabel;
    private Tween _bannerTween;
    private Tween _toastTween;

    private float _duration = 1f;
    private float _resultTimer;
    private bool _eventActive;
    private bool _hasTarget;
    private Vector2 _target;
    private int _shownSeconds = -1;
    private readonly Vector2[] _arrow = new Vector2[3];

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildBanner();
        BuildToast();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.RunEventStarted += OnEventStarted;
        _eventBus.RunEventProgress += OnEventProgress;
        _eventBus.RunEventEnded += OnEventEnded;
        _eventBus.VariantEnemyKilled += OnVariantKilled;
    }

    public override void _ExitTree()
    {
        _eventBus.RunEventStarted -= OnEventStarted;
        _eventBus.RunEventProgress -= OnEventProgress;
        _eventBus.RunEventEnded -= OnEventEnded;
        _eventBus.VariantEnemyKilled -= OnVariantKilled;
    }

    private void BuildBanner()
    {
        _banner = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        StyleBoxFlat style = new()
        {
            BgColor = new Color(0.06f, 0.05f, 0.04f, 0.88f),
            BorderColor = Gold with { A = 0.8f },
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1
        };
        _banner.AddThemeStyleboxOverride("panel", style);
        _banner.AnchorLeft = 0.5f;
        _banner.AnchorRight = 0.5f;
        _banner.OffsetLeft = -BannerWidth / 2f;
        _banner.OffsetRight = BannerWidth / 2f;
        _banner.OffsetTop = BannerTop;
        _banner.OffsetBottom = BannerTop + 46f;
        _banner.PivotOffset = new Vector2(BannerWidth / 2f, 0f);
        AddChild(_banner);

        Control content = new() { MouseFilter = MouseFilterEnum.Ignore };
        _banner.AddChild(content);

        _titleLabel = MakeLabel(15, Gold, 4);
        _titleLabel.Position = new Vector2(10, 1);
        _titleLabel.Size = new Vector2(BannerWidth - 70, 20);
        content.AddChild(_titleLabel);

        _timeLabel = MakeLabel(12, WhiteOff, 3);
        _timeLabel.Position = new Vector2(BannerWidth - 60, 2);
        _timeLabel.Size = new Vector2(50, 18);
        _timeLabel.HorizontalAlignment = HorizontalAlignment.Right;
        content.AddChild(_timeLabel);

        _objectiveLabel = MakeLabel(11, WhiteOff, 3);
        _objectiveLabel.Position = new Vector2(10, 20);
        _objectiveLabel.Size = new Vector2(BannerWidth - 20, 16);
        _objectiveLabel.ClipText = true;
        content.AddChild(_objectiveLabel);

        // Deux jauges superposées : progression de l'objectif (or) et temps restant (fin trait clair).
        ColorRect track = new() { Color = new Color(0f, 0f, 0f, 0.8f), Position = new Vector2(10, 38), Size = new Vector2(BannerWidth - 20, 4) };
        content.AddChild(track);
        _progressFill = new ColorRect { Color = Gold, Size = new Vector2(0, 3) };
        track.AddChild(_progressFill);
        _timeFill = new ColorRect { Color = WhiteOff with { A = 0.7f }, Position = new Vector2(0, 3), Size = new Vector2(BannerWidth - 20, 1) };
        track.AddChild(_timeFill);
    }

    private void BuildToast()
    {
        _toastLabel = MakeLabel(12, Gold, 4);
        _toastLabel.AnchorLeft = 0.5f;
        _toastLabel.AnchorRight = 0.5f;
        _toastLabel.AnchorTop = 1f;
        _toastLabel.AnchorBottom = 1f;
        _toastLabel.OffsetLeft = -160;
        _toastLabel.OffsetRight = 160;
        _toastLabel.OffsetTop = -80;
        _toastLabel.OffsetBottom = -62;
        _toastLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _toastLabel.Modulate = Colors.Transparent;
        AddChild(_toastLabel);
    }

    private static Label MakeLabel(int size, Color color, int outline)
    {
        Label label = new() { MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", outline);
        return label;
    }

    public override void _Process(double delta)
    {
        if (_resultTimer > 0f)
        {
            _resultTimer -= (float)delta;
            if (_resultTimer <= 0f)
                HideBanner();
        }
        if (_eventActive && _hasTarget)
            QueueRedraw();
    }

    private void OnEventStarted(string eventId, string title, string objective, float duration)
    {
        _eventActive = true;
        _resultTimer = 0f;
        _duration = Mathf.Max(1f, duration);
        _shownSeconds = -1;
        _hasTarget = false;
        _titleLabel.Text = title;
        _titleLabel.AddThemeColorOverride("font_color", Gold);
        _objectiveLabel.Text = objective;
        _objectiveLabel.AddThemeColorOverride("font_color", WhiteOff);
        _progressFill.Size = new Vector2(0, 3);
        UpdateTime(duration);

        _banner.Visible = true;
        _bannerTween?.Kill();
        _banner.Modulate = Colors.Transparent;
        _banner.Scale = new Vector2(1.15f, 1.15f);
        _bannerTween = CreateTween().SetParallel();
        _bannerTween.TweenProperty(_banner, "modulate", Colors.White, 0.2f);
        _bannerTween.TweenProperty(_banner, "scale", Vector2.One, 0.3f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    private void OnEventProgress(string objective, float progress, float timeRemaining, Vector2 target, bool hasTarget)
    {
        if (!_eventActive)
            return;
        if (_objectiveLabel.Text != objective)
            _objectiveLabel.Text = objective;
        _progressFill.Size = new Vector2((BannerWidth - 20) * Mathf.Clamp(progress, 0f, 1f), 3);
        UpdateTime(timeRemaining);
        _target = target;
        bool hadTarget = _hasTarget;
        _hasTarget = hasTarget;
        if (hadTarget && !hasTarget)
            QueueRedraw();
    }

    private void UpdateTime(float timeRemaining)
    {
        _timeFill.Size = new Vector2((BannerWidth - 20) * Mathf.Clamp(timeRemaining / _duration, 0f, 1f), 1);
        int seconds = Mathf.CeilToInt(timeRemaining);
        if (seconds == _shownSeconds)
            return;
        _shownSeconds = seconds;
        _timeLabel.Text = $"{seconds} s";
        _timeLabel.AddThemeColorOverride("font_color", seconds <= 10 ? new Color(0.95f, 0.45f, 0.3f) : WhiteOff);
    }

    private void OnEventEnded(string eventId, bool success, string summary)
    {
        _eventActive = false;
        _hasTarget = false;
        QueueRedraw();
        _titleLabel.Text = Tr(success ? "EVENT_RESULT_SUCCESS" : "EVENT_RESULT_FAIL");
        _titleLabel.AddThemeColorOverride("font_color", success ? Success : GrayLight);
        _objectiveLabel.Text = summary;
        _objectiveLabel.AddThemeColorOverride("font_color", success ? Gold : GrayLight);
        _timeLabel.Text = "";
        _progressFill.Size = new Vector2(success ? BannerWidth - 20 : 0, 3);
        _timeFill.Size = new Vector2(0, 1);
        _resultTimer = ResultHoldSec;
        if (success)
            Infrastructure.AudioManager.Play("sfx_souvenir_trouve", 0f, -4f);
    }

    private void HideBanner()
    {
        _bannerTween?.Kill();
        _bannerTween = CreateTween();
        _bannerTween.TweenProperty(_banner, "modulate", Colors.Transparent, 0.4f);
        _bannerTween.TweenCallback(Callable.From(() => _banner.Visible = _eventActive));
    }

    private void OnVariantKilled(string displayName, string variantId, Vector2 position)
    {
        _toastLabel.Text = string.Format(Tr("EVENT_VARIANT_SLAIN"), displayName);
        _toastTween?.Kill();
        _toastLabel.Modulate = Colors.White;
        _toastTween = CreateTween();
        _toastTween.TweenInterval(ToastHoldSec);
        _toastTween.TweenProperty(_toastLabel, "modulate", Colors.Transparent, 0.5f);
    }

    /// <summary>Flèche au bord de l'écran quand la cible de l'événement est hors du cadre.</summary>
    public override void _Draw()
    {
        if (!_eventActive || !_hasTarget)
            return;

        Viewport viewport = GetViewport();
        Vector2 screen = viewport.GetCanvasTransform() * _target;
        Vector2 hudScale = GetParent<Control>().Scale;
        Vector2 local = screen / hudScale;
        Vector2 size = Size;
        Rect2 inner = new(Vector2.One * PointerMargin, size - Vector2.One * PointerMargin * 2f);
        if (inner.HasPoint(local))
            return;

        Vector2 center = size / 2f;
        Vector2 direction = (local - center).Normalized();
        // Intersection du rayon centre→cible avec le cadre intérieur.
        float tx = direction.X != 0f ? (inner.Size.X / 2f) / Mathf.Abs(direction.X) : float.MaxValue;
        float ty = direction.Y != 0f ? (inner.Size.Y / 2f) / Mathf.Abs(direction.Y) : float.MaxValue;
        Vector2 tip = center + direction * Mathf.Min(tx, ty);

        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.GetTicksMsec() / 120f);
        DrawArrow(tip, direction, 1.35f * pulse, new Color(0f, 0f, 0f, 0.75f));
        DrawArrow(tip, direction, pulse, PointerFill);
    }

    private void DrawArrow(Vector2 tip, Vector2 direction, float scale, Color color)
    {
        Vector2 side = direction.Orthogonal();
        _arrow[0] = tip + direction * 8f * scale;
        _arrow[1] = tip - direction * 5f * scale + side * 7f * scale;
        _arrow[2] = tip - direction * 5f * scale - side * 7f * scale;
        DrawColoredPolygon(_arrow, color);
    }
}
