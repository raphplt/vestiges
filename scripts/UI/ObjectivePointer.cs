using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Flèche d'objectif d'une terre (plan 57 C1) : vers le Mémorial endormi le plus proche tant qu'il manque des lumières,
/// puis vers le repaire du gardien. Hors du cadre, une flèche au bord de l'écran, l'icône du but dans un losange ; dans
/// le cadre, un chevron qui flotte au-dessus du but et s'efface quand le joueur l'atteint. Rien pendant un cercle tenu,
/// le combat ou le passage, ni hors des terres. Les Mémoriaux se montrent même dans le brouillard (DECISIONS §198).
/// Enfant de la racine mise à l'échelle du HUD (unités de référence 960×540), comme les flèches des coffres.
/// </summary>
public partial class ObjectivePointer : Control
{
    // Un peu en retrait des flèches des coffres, qui longent le même bord.
    private const float Margin = 30f;
    private const float TopMargin = 70f;
    private const float BottomMargin = 82f;
    // Distance au but où l'invite d'interaction prend le relais.
    private const float ReachedPx = 64f;
    private const float ChevronLift = 10f;
    private const float BobPixels = 3f;
    private const float IconScale = 3f;

    private static readonly Color MemorialColor = new(0.95f, 0.82f, 0.45f);
    private static readonly Color LairColor = new(0.86f, 0.55f, 0.95f);
    private static readonly Color Ink = new(0.04f, 0.04f, 0.08f, 0.95f);

    private enum Goal { None, Memorial, Lair }

    private readonly Vector2[] _triangle = new Vector2[3];
    private readonly Vector2[] _diamond = new Vector2[4];
    private EventBus _eventBus;
    private GroupCache _groupCache;
    private Texture2D _icon;
    private Goal _goal;
    private bool _hasTarget;
    private Vector2 _target;
    private bool _drawn;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _icon = GD.Load<Texture2D>("res://assets/ui/hud/plan25/minimap_memorial.png");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LandObjectiveChanged += OnObjective;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.LandObjectiveChanged -= OnObjective;
    }

    private void OnObjective(int land, int lights, int needed, int stage)
    {
        _goal = stage switch
        {
            0 => Goal.Memorial,
            1 => Goal.Lair,
            _ => Goal.None,
        };
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _hasTarget = _goal switch
        {
            Goal.Memorial => NearestDormantMemorial(out _target),
            Goal.Lair => LairTarget(out _target),
            _ => false,
        };
        if (_hasTarget && _groupCache.GetPlayer() is Node2D player
            && player.GlobalPosition.DistanceSquaredTo(_target) <= ReachedPx * ReachedPx)
            _hasTarget = false;
        if (_hasTarget || _drawn)
            QueueRedraw();
        if (!_hasTarget && !_drawn && _goal == Goal.None)
            SetProcess(false);
    }

    /// <summary>Le Mémorial endormi le plus proche du joueur ; aucun pendant un cercle tenu, le joueur y est occupé.</summary>
    private bool NearestDormantMemorial(out Vector2 target)
    {
        target = Vector2.Zero;
        if (_groupCache.GetPlayer() is not Node2D player)
            return false;
        IReadOnlyList<Memorial> all = Memorial.All;
        float best = float.MaxValue;
        for (int i = 0; i < all.Count; i++)
        {
            Memorial memorial = all[i];
            if (memorial.State == Memorial.MemorialState.Gathering)
                return false;
            if (memorial.State != Memorial.MemorialState.Dormant)
                continue;
            float distanceSq = player.GlobalPosition.DistanceSquaredTo(memorial.GlobalPosition);
            if (distanceSq >= best)
                continue;
            best = distanceSq;
            target = memorial.PromptPosition;
        }
        return best < float.MaxValue;
    }

    private static bool LairTarget(out Vector2 target)
    {
        GuardianLair lair = GuardianLair.Current;
        target = lair?.PromptPosition ?? Vector2.Zero;
        return lair is { CanInteract: true };
    }

    public override void _Draw()
    {
        _drawn = _hasTarget;
        if (!_hasTarget)
            return;
        Camera2D camera = GetViewport().GetCamera2D();
        if (camera == null)
            return;

        Transform2D canvas = GetViewport().GetCanvasTransform();
        Vector2 hudScale = GetParent<Control>().Scale;
        Vector2 local = canvas * _target / hudScale;
        Color color = _goal == Goal.Lair ? LairColor : MemorialColor;
        float time = Time.GetTicksMsec() / 1000f;
        Rect2 inner = new(new Vector2(Margin, TopMargin), Size - new Vector2(Margin * 2f, TopMargin + BottomMargin));
        if (inner.HasPoint(local))
        {
            float bob = Mathf.Round(Mathf.Sin(time * 4f) * BobPixels);
            DrawChevron(local + new Vector2(0f, -ChevronLift + bob), color);
            return;
        }

        Vector2 middle = inner.GetCenter();
        Vector2 direction = (local - middle).Normalized();
        float tx = direction.X != 0f ? (inner.Size.X / 2f) / Mathf.Abs(direction.X) : float.MaxValue;
        float ty = direction.Y != 0f ? (inner.Size.Y / 2f) / Mathf.Abs(direction.Y) : float.MaxValue;
        Vector2 badge = (middle + direction * Mathf.Min(tx, ty)).Round();
        float pulse = 1f + 0.12f * Mathf.Sin(time * 6f);
        DrawArrowHead(badge + direction * 12f, direction, 3.2f * pulse + 0.6f, Ink);
        DrawArrowHead(badge + direction * 12f, direction, 3.2f * pulse, color);
        DrawBadge(badge, color);
    }

    /// <summary>Losange au bord de l'écran, l'icône des Mémoriaux de la minimap dedans, à la couleur du but.</summary>
    private void DrawBadge(Vector2 at, Color color)
    {
        FillDiamond(at, 13f);
        DrawColoredPolygon(_diamond, Ink);
        FillDiamond(at, 11f);
        DrawColoredPolygon(_diamond, color.Darkened(0.55f));
        Vector2 size = _icon.GetSize() * IconScale;
        DrawTextureRect(_icon, new Rect2((at - size / 2f).Round(), size), false, color);
    }

    private void DrawArrowHead(Vector2 tip, Vector2 direction, float scale, Color color)
    {
        Vector2 side = direction.Orthogonal();
        _triangle[0] = tip + direction * 4f * scale;
        _triangle[1] = tip - direction * 1f * scale + side * 3.5f * scale;
        _triangle[2] = tip - direction * 1f * scale - side * 3.5f * scale;
        DrawColoredPolygon(_triangle, color);
    }

    /// <summary>Chevron pointé vers le bas, au-dessus du but, avec son contour d'encre.</summary>
    private void DrawChevron(Vector2 tip, Color color)
    {
        tip = tip.Round();
        _triangle[0] = tip + new Vector2(0f, 2f);
        _triangle[1] = tip + new Vector2(-9f, -10f);
        _triangle[2] = tip + new Vector2(9f, -10f);
        DrawColoredPolygon(_triangle, Ink);
        _triangle[0] = tip;
        _triangle[1] = tip + new Vector2(-6f, -8f);
        _triangle[2] = tip + new Vector2(6f, -8f);
        DrawColoredPolygon(_triangle, color);
    }

    private void FillDiamond(Vector2 at, float radius)
    {
        _diamond[0] = at + new Vector2(0f, -radius);
        _diamond[1] = at + new Vector2(radius, 0f);
        _diamond[2] = at + new Vector2(0f, radius);
        _diamond[3] = at + new Vector2(-radius, 0f);
    }
}
