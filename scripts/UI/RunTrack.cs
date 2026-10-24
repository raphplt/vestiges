using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Piste de run sous le temps (plan 56 B3) : un losange par terre à traverser, puis un sommet pour la Montée. Terres
/// passées en or, terre en cours allumée, à venir en gris. Tant que la Montée n'existe pas, la terre du late game allume
/// le sommet. Une marque qui change d'état jette un éclat. Écoute <see cref="EventBus.LandObjectiveChanged"/> ; cachée
/// hors des terres. Au repos, ni dessin ni rappel. Coordonnées en unités de référence 960×540.
/// </summary>
public partial class RunTrack : Control
{
    /// <summary>Centre des marques, entre le temps et la Série.</summary>
    public const float Middle = 33f;
    /// <summary>Bas de la piste : la Série passe dessous.</summary>
    public const float Bottom = Middle + MarkRadius + 3f;

    private const float Spacing = 14f;
    private const float MarkRadius = 3f;
    private const float PeakHalfWidth = 4f;
    private const float FlashSeconds = 0.7f;
    private const float FlashRadius = 7f;

    private static readonly Color Outline = new(0.02f, 0.02f, 0.04f, 0.92f);
    private static readonly Color PassedColor = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);
    private static readonly Color CurrentColor = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    private static readonly Color AheadColor = new(0x6B / 255f, 0x61 / 255f, 0x61 / 255f);
    private static readonly Color BeyondColor = new(0xE0 / 255f, 0x7B / 255f, 0x39 / 255f);

    private EventBus _eventBus;
    private int _landsToWin;
    private int _land = -1;
    private bool _landDone;
    private int _flashMark = -1;
    private float _flash;
    private readonly Vector2[] _outlinePoints = new Vector2[4];
    private readonly Vector2[] _fillPoints = new Vector2[4];
    private readonly Vector2[] _peakOutline = new Vector2[3];
    private readonly Vector2[] _peakFill = new Vector2[3];

    private int MarkCount => _landsToWin + 1;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        Visible = false;
        SetProcess(false);
        GuardianConfig config = GuardianConfig.Load(out string error);
        if (config == null)
        {
            GD.PushError($"[RunTrack] {error} : pas de piste de run");
            return;
        }
        _landsToWin = config.LandsToWin;
        float half = (MarkCount - 1) * Spacing / 2f + PeakHalfWidth + 2f;
        OffsetLeft = -half;
        OffsetRight = half;
        OffsetTop = Middle - MarkRadius - 3f;
        OffsetBottom = Bottom;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LandObjectiveChanged += OnObjective;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.LandObjectiveChanged -= OnObjective;
    }

    private void OnObjective(int land, int lights, int needed, int stage)
    {
        // 4 : gardien vaincu, 7 : le dernier, la run est gagnée.
        bool done = stage is 4 or 7;
        if (land == _land && done == _landDone)
            return;
        bool first = _land < 0;
        _land = land;
        _landDone = done;
        Visible = true;
        QueueRedraw();
        if (first)
            return;
        _flashMark = Mathf.Min(land, MarkCount - 1);
        _flash = FlashSeconds;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _flash -= (float)delta;
        if (_flash <= 0f)
        {
            _flash = 0f;
            SetProcess(false);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_land < 0)
            return;
        float y = Middle - OffsetTop;
        float left = Size.X / 2f - (MarkCount - 1) * Spacing / 2f;
        for (int i = 0; i < MarkCount - 1; i++)
        {
            float from = left + i * Spacing + MarkRadius + 2f;
            float to = left + (i + 1) * Spacing - MarkRadius - 2f;
            Color line = i < _land ? PassedColor with { A = 0.7f } : AheadColor with { A = 0.6f };
            DrawRect(new Rect2(from, y - 1f, to - from, 3f), Outline with { A = 0.6f });
            DrawRect(new Rect2(from, y, to - from, 1f), line);
        }

        if (_flash > 0f && _flashMark >= 0)
        {
            float t = 1f - _flash / FlashSeconds;
            Color halo = ColorOf(_flashMark) with { A = (1f - t) * 0.55f };
            DrawArc(new Vector2(left + _flashMark * Spacing, y), MarkRadius + t * FlashRadius, 0f, Mathf.Tau, 16, halo, 1f);
        }

        for (int i = 0; i < MarkCount; i++)
        {
            Vector2 at = new(left + i * Spacing, y);
            if (i == MarkCount - 1)
                DrawPeak(at, ColorOf(i), i == ActiveMark);
            else
                DrawMark(at, ColorOf(i), i == ActiveMark || i < _land || (i == _land && _landDone));
        }
    }

    /// <summary>Marque allumée : la terre en cours, ou le sommet une fois les terres franchies.</summary>
    private int ActiveMark => _landDone ? -1 : Mathf.Min(_land, MarkCount - 1);

    private Color ColorOf(int mark)
    {
        if (mark == MarkCount - 1)
            return _land >= _landsToWin ? BeyondColor : AheadColor;
        if (mark < _land || (mark == _land && _landDone))
            return PassedColor;
        return mark == _land ? CurrentColor : AheadColor;
    }

    /// <summary>Un losange : plein s'il est franchi ou en cours, en creux s'il est à venir.</summary>
    private void DrawMark(Vector2 at, Color color, bool filled)
    {
        Diamond(_outlinePoints, at, MarkRadius + 1f);
        DrawColoredPolygon(_outlinePoints, Outline);
        Diamond(_fillPoints, at, MarkRadius);
        DrawColoredPolygon(_fillPoints, color);
        if (filled)
            return;
        Diamond(_fillPoints, at, MarkRadius - 1.5f);
        DrawColoredPolygon(_fillPoints, Outline);
    }

    /// <summary>Le sommet de la Montée, pointe en haut : plein s'il est allumé, en creux sinon.</summary>
    private void DrawPeak(Vector2 at, Color color, bool lit)
    {
        Peak(_peakOutline, at, -1f);
        DrawColoredPolygon(_peakOutline, Outline);
        Peak(_peakFill, at, 0f);
        DrawColoredPolygon(_peakFill, color);
        if (lit)
            return;
        Peak(_peakFill, at, 1.5f);
        DrawColoredPolygon(_peakFill, Outline);
    }

    private static void Diamond(Vector2[] points, Vector2 at, float radius)
    {
        points[0] = at + new Vector2(0f, -radius);
        points[1] = at + new Vector2(radius, 0f);
        points[2] = at + new Vector2(0f, radius);
        points[3] = at + new Vector2(-radius, 0f);
    }

    /// <summary>Triangle du sommet, rentré de <paramref name="inset"/> pixels sur chaque côté (négatif : élargi).</summary>
    private static void Peak(Vector2[] points, Vector2 at, float inset)
    {
        float top = -MarkRadius - 1f + inset * 2f;
        float bottom = MarkRadius - inset;
        float half = PeakHalfWidth - inset * 2f;
        points[0] = at + new Vector2(0f, top);
        points[1] = at + new Vector2(half, bottom);
        points[2] = at + new Vector2(-half, bottom);
    }
}
