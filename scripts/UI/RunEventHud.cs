using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Présentation des micro-événements, sans bandeau (plan 24 A3) : l'objectif se lit dans le monde. Reste une flèche
/// au bord de l'écran vers une cible hors champ, et, la première fois qu'un profil croise un type d'événement, une ligne
/// d'aide (son objectif) quelques secondes sous le temps. La réussite se fait entendre.
/// Enfant de la racine mise à l'échelle du HUD : coordonnées en unités de référence 960×540.
/// </summary>
public partial class RunEventHud : Control
{
    private const float HintTop = 44f;
    private const float HintWidth = 320f;
    private const float HintHoldSec = 3.5f;
    private const float PointerMargin = 26f;

    private static readonly Color WhiteOff = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    private static readonly Color PointerFill = new(0.95f, 0.78f, 0.3f);

    private EventBus _eventBus;
    private Label _hintLabel;
    private Tween _hintTween;

    private bool _eventActive;
    private bool _hasTarget;
    private Vector2 _target;
    private readonly Vector2[] _arrow = new Vector2[3];

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildHint();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.RunEventStarted += OnEventStarted;
        _eventBus.RunEventProgress += OnEventProgress;
        _eventBus.RunEventEnded += OnEventEnded;
    }

    public override void _ExitTree()
    {
        _eventBus.RunEventStarted -= OnEventStarted;
        _eventBus.RunEventProgress -= OnEventProgress;
        _eventBus.RunEventEnded -= OnEventEnded;
    }

    private void BuildHint()
    {
        _hintLabel = new Label { MouseFilter = MouseFilterEnum.Ignore, Modulate = Colors.Transparent };
        _hintLabel.AddThemeFontSizeOverride("font_size", 11);
        _hintLabel.AddThemeColorOverride("font_color", WhiteOff);
        _hintLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        _hintLabel.AddThemeConstantOverride("outline_size", 3);
        _hintLabel.AnchorLeft = 0.5f;
        _hintLabel.AnchorRight = 0.5f;
        _hintLabel.OffsetLeft = -HintWidth / 2f;
        _hintLabel.OffsetRight = HintWidth / 2f;
        _hintLabel.OffsetTop = HintTop;
        _hintLabel.OffsetBottom = HintTop + 16f;
        _hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_hintLabel);
    }

    public override void _Process(double delta)
    {
        if (_eventActive && _hasTarget)
            QueueRedraw();
    }

    private void OnEventStarted(string eventId, string title, string objective, float duration)
    {
        _eventActive = true;
        _hasTarget = false;
        if (string.IsNullOrEmpty(objective) || !Infrastructure.MetaSaveManager.MarkHintSeen($"run_event:{eventId}"))
            return;
        _hintLabel.Text = objective;
        _hintTween?.Kill();
        _hintLabel.Modulate = Colors.Transparent;
        _hintTween = CreateTween();
        _hintTween.TweenProperty(_hintLabel, "modulate", Colors.White, 0.3f);
        _hintTween.TweenInterval(HintHoldSec);
        _hintTween.TweenProperty(_hintLabel, "modulate", Colors.Transparent, 0.6f);
    }

    private void OnEventProgress(string objective, float progress, float timeRemaining, Vector2 target, bool hasTarget)
    {
        if (!_eventActive)
            return;
        _target = target;
        bool hadTarget = _hasTarget;
        _hasTarget = hasTarget;
        if (hadTarget && !hasTarget)
            QueueRedraw();
    }

    private void OnEventEnded(string eventId, bool success, string summary)
    {
        _eventActive = false;
        _hasTarget = false;
        QueueRedraw();
        if (success)
            Infrastructure.AudioManager.Play("sfx_souvenir_trouve", 0f, -4f);
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
