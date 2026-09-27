using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Anneau au sol d'une créature à affixe, en pixel art : bord plein et intérieur tramé à la couleur de l'affixe, qui
/// respire par poses (trois rayons) plutôt qu'en glissement continu. Un par créature, réutilisé par le pool : le
/// dessin ne se refait qu'au changement de pose.
/// </summary>
public partial class AffixAura : Node2D
{
    private const float PoseSec = 0.2f;
    private const float RimAlpha = 0.7f;
    private const float FillAlpha = 0.2f;
    private static readonly int[] PoseOffsets = { 0, 1, 2, 1 };
    private static readonly Dictionary<int, (Vector2I[] Rim, Vector2I[] Fill)> Shapes = new();

    private Color _color;
    private int _baseRadius;
    private int _pose;
    private float _poseTimer;

    public AffixAura()
    {
        // Au sol, sous le corps de la créature.
        ZIndex = -1;
        Visible = false;
        SetProcess(false);
    }

    /// <summary>Allume l'anneau autour d'une créature de demi-largeur <paramref name="radius"/> pixels.</summary>
    public void Show(Color color, float radius)
    {
        _color = color;
        _baseRadius = Mathf.Max(6, Mathf.RoundToInt(radius));
        _pose = 0;
        _poseTimer = PoseSec;
        Visible = true;
        SetProcess(true);
        QueueRedraw();
    }

    public void HideAura()
    {
        Visible = false;
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        _poseTimer -= (float)delta;
        if (_poseTimer > 0f)
            return;
        _poseTimer += PoseSec;
        _pose = (_pose + 1) % PoseOffsets.Length;
        QueueRedraw();
    }

    public override void _Draw()
    {
        (Vector2I[] rim, Vector2I[] fill) = ShapeOf(_baseRadius + PoseOffsets[_pose]);
        Color fillColor = _color with { A = FillAlpha };
        foreach (Vector2I pixel in fill)
            DrawRect(new Rect2(pixel, Vector2.One), fillColor);
        Color rimColor = _color with { A = RimAlpha };
        foreach (Vector2I pixel in rim)
            DrawRect(new Rect2(pixel, Vector2.One), rimColor);
    }

    /// <summary>Ellipse au sol (deux fois plus large que haute) : pixels du bord et trame d'un pixel sur deux dedans.</summary>
    private static (Vector2I[] Rim, Vector2I[] Fill) ShapeOf(int radius)
    {
        if (Shapes.TryGetValue(radius, out (Vector2I[] Rim, Vector2I[] Fill) shape))
            return shape;
        float rx = radius;
        float ry = radius / Iso.GroundSquash;
        List<Vector2I> rim = new();
        List<Vector2I> fill = new();
        int maxY = Mathf.CeilToInt(ry);
        for (int y = -maxY; y <= maxY; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                float outer = Normalized(x, y, rx, ry);
                if (outer > 1f)
                    continue;
                // Bord : dans l'ellipse, mais un voisin direct en dehors.
                bool edge = Normalized(x + 1, y, rx, ry) > 1f || Normalized(x - 1, y, rx, ry) > 1f
                    || Normalized(x, y + 1, rx, ry) > 1f || Normalized(x, y - 1, rx, ry) > 1f;
                if (edge)
                    rim.Add(new Vector2I(x, y));
                else if (((x + y) & 1) == 0)
                    fill.Add(new Vector2I(x, y));
            }
        }
        shape = (rim.ToArray(), fill.ToArray());
        Shapes[radius] = shape;
        return shape;
    }

    private static float Normalized(float x, float y, float rx, float ry) =>
        (x + 0.5f) * (x + 0.5f) / (rx * rx) + (y + 0.5f) * (y + 0.5f) / (ry * ry);
}
