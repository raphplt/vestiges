using Godot;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Repère au sol d'un micro-événement : colonne de lumière visible de loin, anneau de zone
/// avec sa progression, vestige en chute puis posé. Détaché du porteur, au-dessus du sol.
/// </summary>
public partial class RunEventMarker : Node2D
{
    public enum MarkerStyle { Beacon, Zone, Relic }

    private const int ArcSegments = 48;
    private const float BeaconHeight = 90f;
    private const float FallHeight = 220f;

    private MarkerStyle _style;
    private float _radius;
    private Color _color;
    private float _progress;
    private float _fall;
    private float _time;
    private bool _active = true;
    private readonly Vector2[] _fillPoints = new Vector2[ArcSegments];
    private readonly Vector2[] _crystalPoints = new Vector2[4];
    private readonly Vector2[] _crystalOutline = new Vector2[5];

    public RunEventMarker()
    {
        TopLevel = true;
        ZAsRelative = false;
        ZIndex = 1;
    }

    public void Setup(MarkerStyle style, Vector2 position, float radius, Color color)
    {
        _style = style;
        GlobalPosition = position;
        _radius = radius;
        _color = color;
        QueueRedraw();
    }

    public void SetProgress(float progress) => _progress = Mathf.Clamp(progress, 0f, 1f);

    /// <summary>Avancement de la chute du vestige (0 = dans le ciel, 1 = posé).</summary>
    public void SetFall(float fall) => _fall = Mathf.Clamp(fall, 0f, 1f);

    /// <summary>Zone franchie par le joueur : l'anneau s'intensifie.</summary>
    public void SetActive(bool active) => _active = active;

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);
        switch (_style)
        {
            case MarkerStyle.Beacon:
                DrawBeacon(pulse);
                break;
            case MarkerStyle.Zone:
                DrawZone(pulse);
                break;
            case MarkerStyle.Relic:
                DrawRelic(pulse);
                break;
        }
    }

    private void DrawBeacon(float pulse)
    {
        // Colonne de lumière : se lit au bord de l'écran avant que la cible n'y entre.
        for (int i = 0; i < 6; i++)
        {
            float t = i / 6f;
            float width = Mathf.Lerp(7f, 2f, t);
            DrawRect(new Rect2(-width / 2f, -BeaconHeight * (t + 1f / 6f), width, BeaconHeight / 6f + 1f),
                _color with { A = (0.45f - t * 0.35f) * (0.7f + 0.3f * pulse) });
        }
        DrawEllipseRing(_radius * (0.9f + 0.1f * pulse), _color with { A = 0.7f }, 1.5f);
    }

    private void DrawZone(float pulse)
    {
        float alpha = _active ? 0.9f : 0.55f;
        DrawEllipseFilled(_radius, _color with { A = (_active ? 0.16f : 0.08f) + 0.05f * pulse });
        DrawEllipseRing(_radius, _color with { A = alpha }, 2f);
        if (_progress > 0f)
            DrawEllipseArc(_radius + 5f, _progress, _color.Lightened(0.35f), 3f);
        DrawBeacon(pulse);
    }

    private void DrawRelic(float pulse)
    {
        if (_fall < 1f)
        {
            // Traînée qui descend du ciel vers la zone d'impact annoncée.
            float height = FallHeight * (1f - _fall);
            DrawLine(new Vector2(0f, -height - 40f), new Vector2(0f, -height), _color with { A = 0.35f }, 3f);
            DrawCrystal(new Vector2(0f, -height), 1f + 0.2f * pulse);
            return;
        }
        DrawEllipseFilled(18f + 4f * pulse, _color with { A = 0.18f });
        DrawCrystal(new Vector2(0f, -10f - 2f * pulse), 1f);
        DrawBeacon(pulse);
    }

    private void DrawCrystal(Vector2 center, float scale)
    {
        _crystalPoints[0] = center + new Vector2(0f, -9f) * scale;
        _crystalPoints[1] = center + new Vector2(5f, 0f) * scale;
        _crystalPoints[2] = center + new Vector2(0f, 7f) * scale;
        _crystalPoints[3] = center + new Vector2(-5f, 0f) * scale;
        for (int i = 0; i < 5; i++)
            _crystalOutline[i] = _crystalPoints[i % 4];
        DrawColoredPolygon(_crystalPoints, _color.Lightened(0.3f));
        DrawPolyline(_crystalOutline, _color.Darkened(0.5f), 1f);
        DrawLine(_crystalPoints[0], _crystalPoints[2], Colors.White with { A = 0.6f }, 1f);
    }

    // Ellipses aplaties 2:1 pour coller au sol isométrique.
    private void DrawEllipseRing(float radius, Color color, float width) => DrawEllipseArc(radius, 1f, color, width);

    private void DrawEllipseArc(float radius, float ratio, Color color, float width)
    {
        int segments = Mathf.Max(1, Mathf.CeilToInt(ArcSegments * ratio));
        Vector2 previous = EllipsePoint(radius, -Mathf.Pi / 2f);
        for (int i = 1; i <= segments; i++)
        {
            Vector2 next = EllipsePoint(radius, -Mathf.Pi / 2f + Mathf.Tau * ratio * i / segments);
            DrawLine(previous, next, color, width);
            previous = next;
        }
    }

    private void DrawEllipseFilled(float radius, Color color)
    {
        for (int i = 0; i < ArcSegments; i++)
            _fillPoints[i] = EllipsePoint(radius, Mathf.Tau * i / ArcSegments);
        DrawColoredPolygon(_fillPoints, color);
    }

    private static Vector2 EllipsePoint(float radius, float angle) =>
        new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.5f);
}
