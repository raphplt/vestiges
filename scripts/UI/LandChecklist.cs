using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Objectif de la terre en cases à cocher, en haut à gauche sous les PV (retour de Raphaël du 15 novembre) : raviver les
/// Mémoriaux, puis réveiller et vaincre le gardien. Les icônes de la minimap ne suffisaient pas à un nouveau joueur pour
/// savoir quoi faire. Deux lignes courtes, sans fond : la ligne en cours en clair, la suivante en gris, une case cochée
/// en or. Le passage coche tout puis s'efface ; cachée hors des terres et sur la terre du late game. Rien ne tourne par
/// image. Coordonnées en unités de référence 960×540.
/// </summary>
public partial class LandChecklist : Control
{
    /// <summary>Haut de la liste : sous la plaque des PV.</summary>
    private const float Top = 54f;
    private const float Left = 10f;
    private const float Box = 7f;
    private const float BoxGap = 5f;
    private const float LineHeight = 14f;
    private const float Width = 220f;
    private const float FlashSeconds = 0.8f;
    private const float DoneHoldSeconds = 3f;
    private const float FadeSeconds = 0.6f;
    private static readonly Color CurrentColor = new(0.93f, 0.9f, 0.84f);
    private static readonly Color PendingColor = new(0.62f, 0.6f, 0.62f, 0.85f);
    private static readonly Color DoneColor = new(0.98f, 0.82f, 0.45f);
    private static readonly Color Ink = new(0.04f, 0.04f, 0.08f, 0.85f);
    private static readonly Color FlashModulate = new(1.7f, 1.7f, 1.7f);

    private EventBus _eventBus;
    private Label _lightsLine;
    private Label _guardianLine;
    private Tween _tween;
    private bool _lightsDone;
    private bool _guardianDone;
    private int _stage = -1;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Position = new Vector2(Left, Top);
        Size = new Vector2(Width, LineHeight * 2f);
        _lightsLine = CreateLine("LightsLine", 0);
        _guardianLine = CreateLine("GuardianLine", 1);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LandObjectiveChanged += OnObjective;
        _eventBus.LandChanging += OnLandChanging;
        Visible = false;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.LandObjectiveChanged -= OnObjective;
            _eventBus.LandChanging -= OnLandChanging;
        }
    }

    private Label CreateLine(string name, int row)
    {
        Label line = new()
        {
            Name = name,
            MouseFilter = MouseFilterEnum.Ignore,
            Position = new Vector2(Box + BoxGap, row * LineHeight - 4f),
            Size = new Vector2(Width - Box - BoxGap, LineHeight + 4f),
        };
        UITheme.SetCompactText(line, 16);
        line.AddThemeColorOverride("font_outline_color", UITheme.OutlineColor);
        line.AddThemeConstantOverride("outline_size", 4);
        AddChild(line);
        return line;
    }

    private void OnObjective(int land, int lights, int needed, int stage)
    {
        // Late game (échos) : pas de gardien à réveiller, la liste n'a rien à dire.
        if (needed <= 0 || stage is 5 or 6)
        {
            Conceal(false);
            _stage = stage;
            return;
        }
        int previous = _stage;
        _stage = stage;
        bool lightsDone = stage >= 1;
        bool guardianDone = stage is 4 or 7;
        bool justChecked = previous >= 0 && (lightsDone && !_lightsDone || guardianDone && !_guardianDone);
        _lightsDone = lightsDone;
        _guardianDone = guardianDone;

        _lightsLine.Text = string.Format(Tr("LAND_CHECK_LIGHTS"), Mathf.Min(lights, needed), needed);
        _guardianLine.Text = Tr(stage switch
        {
            0 => "LAND_CHECK_WAKE",
            1 => "LAND_CHECK_WAKE_LAIR",
            _ => "LAND_CHECK_DEFEAT",
        });
        _lightsLine.AddThemeColorOverride("font_color", lightsDone ? DoneColor : CurrentColor);
        _guardianLine.AddThemeColorOverride("font_color", guardianDone ? DoneColor : lightsDone ? CurrentColor : PendingColor);
        Visible = true;
        QueueRedraw();

        if (justChecked || previous < 0 || guardianDone)
            Flash();
        // Terre franchie : tout est coché, la liste s'efface jusqu'à la terre suivante.
        if (guardianDone)
        {
            _tween.TweenInterval(DoneHoldSeconds);
            _tween.TweenProperty(this, "modulate:a", 0f, FadeSeconds);
            _tween.TweenCallback(Callable.From(() => Conceal(false)));
        }
    }

    private void OnLandChanging(int toIndex)
    {
        Conceal(true);
        _stage = -1;
    }

    private void Flash()
    {
        _tween?.Kill();
        Modulate = FlashModulate;
        _tween = CreateTween();
        _tween.TweenProperty(this, "modulate", Colors.White, FlashSeconds);
    }

    private void Conceal(bool killTween)
    {
        if (killTween)
            _tween?.Kill();
        Visible = false;
        Modulate = Colors.White;
    }

    public override void _Draw()
    {
        DrawCheckbox(0, _lightsDone, _lightsDone ? DoneColor : CurrentColor);
        DrawCheckbox(1, _guardianDone, _guardianDone ? DoneColor : _lightsDone ? CurrentColor : PendingColor);
    }

    private void DrawCheckbox(int row, bool done, Color color)
    {
        Rect2 box = new(new Vector2(0f, row * LineHeight + (LineHeight - Box) / 2f - 1f).Round(), new Vector2(Box, Box));
        DrawRect(box.Grow(1f), Ink);
        DrawRect(box, color, false, 1f);
        if (!done)
            return;
        Vector2 a = box.Position + new Vector2(1.5f, 3.5f);
        Vector2 b = box.Position + new Vector2(3f, 5.5f);
        Vector2 c = box.Position + new Vector2(6f, 1.5f);
        DrawLine(a, b, color, 1.5f);
        DrawLine(b, c, color, 1.5f);
    }
}
