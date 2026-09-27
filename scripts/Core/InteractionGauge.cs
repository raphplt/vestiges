using Godot;

namespace Vestiges.Core;

/// <summary>
/// Jauge d'une action qui prend du temps (ouvrir un coffre, fouiller) : rectangles pleins alignés sur les
/// texels du monde, contour sombre, remplissage à la couleur de ce qu'on ouvre. Au-dessus des entités.
/// </summary>
public partial class InteractionGauge : Node2D
{
    private const int Width = 24;
    private const int Height = 3;
    private static readonly Color Outline = new("1A1A2E");
    private static readonly Color Empty = new("3A3535");

    private float _ratio;
    private Color _fill = new("D4A843");

    public override void _Ready()
    {
        ZIndex = 30;
        Visible = false;
    }

    public void Begin(Color fill)
    {
        _fill = fill;
        _ratio = 0f;
        Visible = true;
        QueueRedraw();
    }

    public void SetRatio(float ratio)
    {
        float clamped = Mathf.Clamp(ratio, 0f, 1f);
        // Un texel de plus à la fois : pas de redessin tant que la largeur remplie ne change pas.
        if (Mathf.FloorToInt(clamped * Width) == Mathf.FloorToInt(_ratio * Width))
            return;
        _ratio = clamped;
        QueueRedraw();
    }

    public void End() => Visible = false;

    public override void _Draw()
    {
        Vector2 origin = new(-Width / 2, 0);
        DrawRect(new Rect2(origin - Vector2.One, new Vector2(Width + 2, Height + 2)), Outline);
        DrawRect(new Rect2(origin, new Vector2(Width, Height)), Empty);
        int filled = Mathf.FloorToInt(_ratio * Width);
        if (filled > 0)
            DrawRect(new Rect2(origin, new Vector2(filled, Height)), _fill);
    }
}
