using Godot;
using Vestiges.Combat;

namespace Vestiges.World;

/// <summary>
/// Cercle à tenir d'un Mémorial sur une terre (plan 51 P2b) : son bord au sol, et une trame qui le remplit du centre vers
/// le bord à mesure qu'on le tient. Au pixel du monde, sous les entités ; les rayons de remplissage sont cuits une fois.
/// </summary>
public partial class MemorialCircle : Node2D
{
    private const int FillSteps = 8;
    private int _radius;
    private Color _color;
    private float _progress;

    public void Setup(float radiusPx, Color color)
    {
        _radius = Mathf.RoundToInt(radiusPx);
        _color = color;
        ZIndex = -1;
        PixelEllipseArt.Get(_radius, false);
        for (int step = 1; step <= FillSteps; step++)
            PixelEllipseArt.Get(FillRadius(step), true);
    }

    /// <summary>Part tenue, de 0 à 1 : le remplissage avance par crans, pour des pixels francs.</summary>
    public void SetProgress(float progress)
    {
        int before = Mathf.FloorToInt(_progress * FillSteps);
        _progress = Mathf.Clamp(progress, 0f, 1f);
        if (Mathf.FloorToInt(_progress * FillSteps) != before)
            QueueRedraw();
    }

    public override void _Draw()
    {
        DrawTexture(PixelEllipseArt.Get(_radius, false), PixelEllipseArt.Offset(_radius), _color);
        int step = Mathf.FloorToInt(_progress * FillSteps);
        if (step > 0)
        {
            int fill = FillRadius(step);
            DrawTexture(PixelEllipseArt.Get(fill, true), PixelEllipseArt.Offset(fill), _color with { A = 0.35f });
        }
    }

    private int FillRadius(int step) => Mathf.Max(4, _radius * step / FillSteps);
}
