using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Flèches au bord de l'écran vers les coffres fermés proches mais hors du cadre, à la couleur de leur rareté, et vers
/// les petits lieux qu'une cabine téléphonique a révélés (plan 22 C4), à la couleur du lieu, plus petites.
/// Enfant de la racine mise à l'échelle du HUD (unités de référence 960×540), comme la flèche des événements.
/// </summary>
public partial class ChestPointers : Control
{
    private const float Margin = 18f;
    // Vitalité, progression et score occupent le haut du HUD, armes et passifs le bas : les flèches restent entre.
    private const float TopMargin = 58f;
    private const float BottomMargin = 70f;
    private const int RevealedMax = 4;

    private readonly Vector2[] _arrow = new Vector2[3];
    private readonly Chest[] _shown;
    private readonly float[] _distances;
    private readonly float _rangeSq;
    private int _count;
    private readonly SmallPlace[] _places = new SmallPlace[RevealedMax];
    private readonly float[] _placeDistances = new float[RevealedMax];
    private int _placeCount;
    private bool _drawn;
    private EventBus _eventBus;

    public ChestPointers()
    {
        ChestPlacementData placement = ChestDataLoader.LoadPlacement();
        _shown = new Chest[Mathf.Max(placement.PointerMax, 0)];
        _distances = new float[_shown.Length];
        _rangeSq = placement.PointerRangePx * placement.PointerRangePx;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.OubliEffectChanged += OnOubliEffectChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.OubliEffectChanged -= OnOubliEffectChanged;
    }

    /// <summary>Oubli des repères (plan 17 lot 3D) : plus de flèches tant qu'il est porté.</summary>
    private void OnOubliEffectChanged(string effect, float total)
    {
        if (effect != "chest_signals")
            return;
        Visible = total <= 0f;
        SetProcess(Visible);
    }

    public override void _Process(double delta)
    {
        Camera2D camera = GetViewport().GetCamera2D();
        _count = 0;
        _placeCount = 0;
        if (camera != null && _shown.Length > 0)
            CollectNearest(camera);
        if (camera != null && SmallPlace.Revealed.Count > 0)
            CollectRevealed(camera);
        // Redessin tant qu'une flèche vit, et une fois de plus pour effacer la dernière.
        if (_count > 0 || _placeCount > 0 || _drawn)
            QueueRedraw();
    }

    /// <summary>Les coffres hors du cadre les plus proches du centre de la vue, sans allocation.</summary>
    private void CollectNearest(Camera2D camera)
    {
        Vector2 center = camera.GetScreenCenterPosition();
        Vector2 half = GetViewport().GetVisibleRect().Size / (2f * camera.Zoom);
        Rect2 view = new(center - half, half * 2f);
        IReadOnlyList<Chest> chests = Chest.Closed;
        for (int i = 0; i < chests.Count; i++)
        {
            Chest chest = chests[i];
            if (!chest.CanOpen || view.HasPoint(chest.GlobalPosition))
                continue;
            float distanceSq = center.DistanceSquaredTo(chest.GlobalPosition);
            if (distanceSq > _rangeSq)
                continue;
            int slot;
            if (_count < _shown.Length)
                slot = _count++;
            else if (distanceSq < _distances[_count - 1])
                slot = _count - 1;
            else
                continue;
            _shown[slot] = chest;
            _distances[slot] = distanceSq;
            // Tri par insertion : le plus lointain reste en dernière place, remplacé en premier.
            for (int j = slot; j > 0 && _distances[j] < _distances[j - 1]; j--)
            {
                (_shown[j], _shown[j - 1]) = (_shown[j - 1], _shown[j]);
                (_distances[j], _distances[j - 1]) = (_distances[j - 1], _distances[j]);
            }
        }
    }

    /// <summary>Les lieux révélés hors du cadre, les plus proches d'abord, sans allocation.</summary>
    private void CollectRevealed(Camera2D camera)
    {
        Vector2 center = camera.GetScreenCenterPosition();
        Vector2 half = GetViewport().GetVisibleRect().Size / (2f * camera.Zoom);
        Rect2 view = new(center - half, half * 2f);
        IReadOnlyList<SmallPlace> revealed = SmallPlace.Revealed;
        for (int i = 0; i < revealed.Count; i++)
        {
            SmallPlace place = revealed[i];
            if (view.HasPoint(place.GlobalPosition))
                continue;
            float distanceSq = center.DistanceSquaredTo(place.GlobalPosition);
            int slot;
            if (_placeCount < RevealedMax)
                slot = _placeCount++;
            else if (distanceSq < _placeDistances[_placeCount - 1])
                slot = _placeCount - 1;
            else
                continue;
            _places[slot] = place;
            _placeDistances[slot] = distanceSq;
            for (int j = slot; j > 0 && _placeDistances[j] < _placeDistances[j - 1]; j--)
            {
                (_places[j], _places[j - 1]) = (_places[j - 1], _places[j]);
                (_placeDistances[j], _placeDistances[j - 1]) = (_placeDistances[j - 1], _placeDistances[j]);
            }
        }
    }

    public override void _Draw()
    {
        _drawn = _count > 0 || _placeCount > 0;
        if (!_drawn)
            return;

        Transform2D canvas = GetViewport().GetCanvasTransform();
        Vector2 hudScale = GetParent<Control>().Scale;
        Vector2 size = Size;
        Rect2 inner = new(new Vector2(Margin, TopMargin), size - new Vector2(Margin * 2f, TopMargin + BottomMargin));
        Vector2 middle = inner.GetCenter();
        float pulse = 0.9f + 0.1f * Mathf.Sin(Time.GetTicksMsec() / 160f);
        for (int i = 0; i < _count; i++)
        {
            Vector2 local = canvas * _shown[i].GlobalPosition / hudScale;
            Vector2 direction = (local - middle).Normalized();
            float tx = direction.X != 0f ? (inner.Size.X / 2f) / Mathf.Abs(direction.X) : float.MaxValue;
            float ty = direction.Y != 0f ? (inner.Size.Y / 2f) / Mathf.Abs(direction.Y) : float.MaxValue;
            Vector2 tip = (middle + direction * Mathf.Min(tx, ty)).Round();
            // Plus le coffre est proche, plus la flèche est franche.
            float closeness = 1f - Mathf.Sqrt(_distances[i] / _rangeSq);
            Color color = RarityPalette.Main(_shown[i].Rarity) with { A = 0.55f + 0.45f * closeness };
            DrawArrow(tip, direction, 1.9f * pulse, new Color(0.1f, 0.1f, 0.18f, color.A));
            DrawArrow(tip, direction, 1.5f * pulse, color);
        }
        for (int i = 0; i < _placeCount; i++)
        {
            Vector2 local = canvas * _places[i].GlobalPosition / hudScale;
            Vector2 direction = (local - middle).Normalized();
            float tx = direction.X != 0f ? (inner.Size.X / 2f) / Mathf.Abs(direction.X) : float.MaxValue;
            float ty = direction.Y != 0f ? (inner.Size.Y / 2f) / Mathf.Abs(direction.Y) : float.MaxValue;
            Vector2 tip = (middle + direction * Mathf.Min(tx, ty)).Round();
            Color color = _places[i].Data.Color with { A = 0.85f };
            DrawArrow(tip, direction, 1.4f * pulse, new Color(0.1f, 0.1f, 0.18f, color.A));
            DrawArrow(tip, direction, 1.1f * pulse, color);
        }
    }

    private void DrawArrow(Vector2 tip, Vector2 direction, float scale, Color color)
    {
        Vector2 side = direction.Orthogonal();
        _arrow[0] = tip + direction * 5f * scale;
        _arrow[1] = tip - direction * 3f * scale + side * 4f * scale;
        _arrow[2] = tip - direction * 3f * scale - side * 4f * scale;
        DrawColoredPolygon(_arrow, color);
    }
}
