using Godot;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Annonce au sol d'une attaque ennemie : zone circulaire ou trajectoire, remplie selon l'avancement.
/// Détachée du transform de son propriétaire pour rester fixe pendant que l'ennemi bouge.
/// </summary>
public partial class GroundTelegraph : Node2D
{
    private enum MarkerShape { Circle, Line }

    private const int ArcSegments = 32;
    private const float OutlineWidth = 1.5f;

    private MarkerShape _shape;
    private float _radius;
    private Vector2 _lineEnd;
    private float _lineWidth;
    private Color _color;
    private float _progress;
    private float _flash;

    public GroundTelegraph()
    {
        TopLevel = true;
        ZIndex = -2;
        Visible = false;
    }

    public void ShowCircle(Vector2 center, float radius, Color color)
    {
        _shape = MarkerShape.Circle;
        _radius = radius;
        Begin(center, color);
    }

    public void ShowLine(Vector2 from, Vector2 to, float width, Color color)
    {
        _shape = MarkerShape.Line;
        _lineEnd = to - from;
        _lineWidth = width;
        Begin(from, color);
    }

    public void SetProgress(float progress)
    {
        _progress = Mathf.Clamp(progress, 0f, 1f);
        QueueRedraw();
    }

    /// <summary>Intensité 1 → 0 de l'éclat d'impact, pilotée par la capacité après résolution.</summary>
    public void SetFlash(float intensity)
    {
        _flash = Mathf.Clamp(intensity, 0f, 1f);
        QueueRedraw();
    }

    public void HideMarker()
    {
        Visible = false;
        _flash = 0f;
        _progress = 0f;
    }

    private void Begin(Vector2 origin, Color color)
    {
        GlobalPosition = origin;
        _color = color;
        _progress = 0f;
        _flash = 0f;
        Visible = true;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color outline = new(_color, 0.55f + 0.45f * _progress);
        Color area = new(_color, 0.08f + 0.14f * _progress);
        Color fill = new(_color, 0.3f);

        if (_shape == MarkerShape.Circle)
        {
            DrawCircle(Vector2.Zero, _radius, area);
            // Le disque intérieur qui grandit donne le délai sans dépendre de la seule couleur.
            DrawCircle(Vector2.Zero, _radius * _progress, fill);
            DrawArc(Vector2.Zero, _radius, 0f, Mathf.Tau, ArcSegments, outline, OutlineWidth);
            if (_flash > 0f)
                DrawCircle(Vector2.Zero, _radius * (1f + 0.25f * (1f - _flash)), new Color(1f, 1f, 0.85f, 0.6f * _flash));
            return;
        }

        DrawLine(Vector2.Zero, _lineEnd, area, _lineWidth);
        DrawLine(Vector2.Zero, _lineEnd * _progress, fill, _lineWidth * 0.6f);
        DrawCircle(_lineEnd, _lineWidth * 0.5f, outline);
    }
}
