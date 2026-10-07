using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Forme des Craies (plan 27 V2b) : étoile, cercle, maison ou soleil tracé à la craie sur le sol, à la taille réelle de
/// la zone touchée (un cercle à l'écran, comme le contrôle des dégâts). Pixels calculés une fois au lancement, tracé en
/// quelques poses, tenu, puis effacé par tramage ; le dessin n'est refait qu'au changement de pose. Recyclé par
/// CombatPools.
/// </summary>
public partial class ChalkDrawing : Node2D
{
    private const int FadePoses = 2;
    /// <summary>Pixels tracés au plus : au-delà (zone très agrandie), un pixel sur deux ou trois, comme un trait plus léger.</summary>
    private const int MaxDrawnPixels = 600;

    private readonly List<Vector2I> _pixels = new();
    private Action<ChalkDrawing> _release;
    private ChalkShapeConfig _config;
    private FxRamp _ramp;
    private float _elapsed;
    private int _pose = -1;
    private int _seed;

    public static ChalkDrawing Create(Action<ChalkDrawing> release)
    {
        ChalkDrawing drawing = new() { _release = release, Visible = false, ZIndex = -1 };
        drawing.SetProcess(false);
        return drawing;
    }

    public void Play(Vector2 center, float radius, string shape, FxFamily family, ChalkShapeConfig config, float opacity)
    {
        _config = config;
        _ramp = PixelPalette.Ramp(family);
        _seed = (int)(GD.Randi() & 0xFFFF);
        GlobalPosition = center.Round();
        Modulate = new Color(1f, 1f, 1f, opacity);
        _pixels.Clear();
        Trace(shape, Mathf.Max(radius, 4f));
        _elapsed = 0f;
        _pose = -1;
        Visible = true;
        SetProcess(true);
        Advance();
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        Advance();
    }

    public override void _Draw()
    {
        int drawPoses = _config.DrawPoses;
        int shown = _pose < drawPoses ? _pixels.Count * (_pose + 1) / drawPoses : _pixels.Count;
        int fade = Math.Max(0, _pose - drawPoses);
        int stride = (_pixels.Count + MaxDrawnPixels - 1) / MaxDrawnPixels;
        for (int index = 0; index < shown; index += stride)
        {
            Vector2I pixel = _pixels[index];
            float grain = Hash(pixel, _seed);
            if (grain < _config.GapChance)
                continue;
            // Effacement en damier irrégulier : la moitié des pixels, puis presque tous.
            float wear = Hash(pixel, _seed + 7919);
            if ((fade >= 1 && wear < 0.5f) || (fade >= 2 && wear < 0.85f))
                continue;
            DrawRect(new Rect2(pixel, Vector2.One), grain < _config.GapChance + 0.2f ? _ramp.Mid : _ramp.Light);
        }
    }

    private void Advance()
    {
        float drawStep = _config.DrawSeconds / _config.DrawPoses;
        float fadeStart = _config.DrawSeconds + _config.HoldSeconds;
        float fadeStep = _config.FadeSeconds / FadePoses;
        if (_elapsed >= fadeStart + _config.FadeSeconds)
        {
            Stop();
            return;
        }
        int pose = _elapsed < _config.DrawSeconds ? (int)(_elapsed / drawStep)
            : _elapsed < fadeStart ? _config.DrawPoses
            : _config.DrawPoses + 1 + (int)((_elapsed - fadeStart) / fadeStep);
        if (pose == _pose)
            return;
        _pose = pose;
        QueueRedraw();
    }

    private void Stop()
    {
        Visible = false;
        SetProcess(false);
        _release(this);
    }

    /// <summary>Contours de la forme, dans un cercle de rayon <paramref name="radius"/> centré sur l'impact.</summary>
    private void Trace(string shape, float radius)
    {
        switch (shape)
        {
            case "star":
                for (int point = 0; point < 10; point++)
                    Line(Polar(point, 10, point % 2 == 0 ? radius : radius * 0.42f), Polar(point + 1, 10, (point + 1) % 2 == 0 ? radius : radius * 0.42f));
                break;
            case "house":
                Polyline(radius, new Vector2(-0.55f, 0.75f), new Vector2(0.55f, 0.75f), new Vector2(0.55f, 0f), new Vector2(-0.55f, 0f), new Vector2(-0.55f, 0.75f));
                Polyline(radius, new Vector2(-0.7f, 0f), new Vector2(0f, -0.7f), new Vector2(0.7f, 0f));
                Polyline(radius, new Vector2(-0.15f, 0.75f), new Vector2(-0.15f, 0.35f), new Vector2(0.15f, 0.35f), new Vector2(0.15f, 0.75f));
                break;
            case "sun":
                Circle(radius * 0.45f, Segments(radius * 0.45f));
                for (int ray = 0; ray < 8; ray++)
                {
                    Line(Polar(ray, 8, radius * 0.6f), Polar(ray, 8, radius * 0.95f));
                    _pixels.Add((Vector2I)Polar(ray, 8, radius * 0.95f).Round());
                }
                break;
            default:
                Circle(radius, Segments(radius));
                break;
        }
    }

    /// <summary>Un segment tous les 4 px environ : le cercle reste rond à toutes les tailles.</summary>
    private static int Segments(float radius) => Mathf.Clamp(Mathf.RoundToInt(Mathf.Tau * radius / 4f), 16, 96);

    private void Circle(float radius, int segments)
    {
        for (int segment = 0; segment < segments; segment++)
            Line(Polar(segment, segments, radius), Polar(segment + 1, segments, radius));
    }

    private void Polyline(float radius, params Vector2[] points)
    {
        for (int point = 0; point + 1 < points.Length; point++)
            Line(points[point] * radius, points[point + 1] * radius);
        // Le dernier point d'un tracé ouvert n'est posé par aucun segment.
        _pixels.Add((Vector2I)(points[^1] * radius).Round());
    }

    /// <summary>Sommet <paramref name="index"/> sur <paramref name="count"/>, en partant du haut.</summary>
    private static Vector2 Polar(int index, int count, float radius) =>
        Vector2.FromAngle(-Mathf.Pi / 2f + index * Mathf.Tau / count) * radius;

    /// <summary>Segment au pixel (Bresenham), sans doubler l'extrémité partagée avec le segment suivant.</summary>
    private void Line(Vector2 from, Vector2 to)
    {
        Vector2I a = (Vector2I)from.Round();
        Vector2I b = (Vector2I)to.Round();
        int dx = Math.Abs(b.X - a.X), sx = a.X < b.X ? 1 : -1;
        int dy = -Math.Abs(b.Y - a.Y), sy = a.Y < b.Y ? 1 : -1;
        int error = dx + dy;
        while (a != b)
        {
            _pixels.Add(a);
            int doubled = 2 * error;
            if (doubled >= dy)
            {
                error += dy;
                a.X += sx;
            }
            if (doubled <= dx)
            {
                error += dx;
                a.Y += sy;
            }
        }
    }

    private static float Hash(Vector2I pixel, int seed)
    {
        uint h = (uint)(pixel.X * 73856093) ^ (uint)(pixel.Y * 19349663) ^ (uint)(seed * 83492791);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return (h & 1023) / 1024f;
    }
}
