using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Frise de la run au bilan (plan 02 lot D, M3). En abscisse le temps de jeu ; la courbe est la distance au départ
/// (jusqu'où, à chaque instant), remplie dessous. Les Résurgences sont des traits verticaux, les élites, Souverains et
/// boss des losanges au-dessus, les coffres des points tout en haut ; les montées de niveau, trop nombreuses pour un
/// repère chacune, font une bande dont l'intensité suit leur densité. <see cref="Reveal"/> dévoile la frise de
/// gauche à droite.
/// </summary>
public partial class RunTimelineStrip : Control
{
    private const float PlotTop = 22f;
    private const float PlotBottom = 30f;
    private const float LevelBand = 8f;
    private const int LevelBuckets = 96;
    private static readonly Color TrackColor = new(0.08f, 0.07f, 0.12f, 0.9f);
    private static readonly Color CurveColor = UITheme.CyanEssence;
    private static readonly Color FillColor = new(UITheme.CyanEssence, 0.16f);
    private static readonly Color CrisisColor = new(0.88f, 0.48f, 0.22f);
    private static readonly Color EliteColor = new(0.62f, 0.45f, 0.85f);
    private static readonly Color SovereignColor = UITheme.GoldBright;
    private static readonly Color BossColor = new(0.77f, 0.26f, 0.17f);
    private static readonly Color ChestColor = UITheme.TextLight;

    private readonly List<Vector2> _curve = new();
    private readonly List<RunMarker> _markers = new();
    private readonly int[] _levelDensity = new int[LevelBuckets];
    private float _duration = 1f;
    private float _maxDistance = 1f;
    private float _reveal = 1f;
    private string _startLabel = "";
    private string _endLabel = "";

    /// <summary>Part dévoilée, de 0 (rien) à 1 (toute la run).</summary>
    public float Reveal
    {
        get => _reveal;
        set
        {
            float clamped = Mathf.Clamp(value, 0f, 1f);
            if (Mathf.IsEqualApprox(clamped, _reveal))
                return;
            _reveal = clamped;
            QueueRedraw();
        }
    }

    public void SetData(IReadOnlyList<RunSample> samples, IReadOnlyList<RunMarker> markers, float durationSec, string startLabel, string endLabel)
    {
        _duration = Mathf.Max(1f, durationSec);
        _startLabel = startLabel;
        _endLabel = endLabel;
        _maxDistance = 1f;
        foreach (RunSample sample in samples)
            _maxDistance = Mathf.Max(_maxDistance, sample.DistanceMeters);
        _curve.Clear();
        foreach (RunSample sample in samples)
            _curve.Add(new Vector2(sample.Time / _duration, sample.DistanceMeters / _maxDistance));
        _markers.Clear();
        System.Array.Clear(_levelDensity);
        foreach (RunMarker marker in markers)
        {
            if (marker.Kind == RunMarkerKind.LevelUp)
                _levelDensity[Mathf.Clamp((int)(marker.Time / _duration * LevelBuckets), 0, LevelBuckets - 1)]++;
            else
                _markers.Add(marker);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        Rect2 plot = new(0f, PlotTop, Size.X, Size.Y - PlotTop - PlotBottom);
        DrawRect(new Rect2(0f, 0f, Size.X, Size.Y - PlotBottom + LevelBand + 4f), TrackColor);
        float revealX = plot.Size.X * _reveal;

        DrawLevelBand(plot, revealX);
        DrawCurve(plot, revealX);
        foreach (RunMarker marker in _markers)
        {
            float x = plot.Position.X + marker.Time / _duration * plot.Size.X;
            if (x > revealX)
                continue;
            DrawMarker(marker.Kind, x, plot);
        }

        Font font = UITheme.BodyFont;
        int size = UITheme.FontSize(TextRole.Caption);
        float baseline = Size.Y - 6f;
        DrawString(font, new Vector2(0f, baseline), _startLabel, HorizontalAlignment.Left, -1, size, UITheme.TextDim);
        if (_reveal >= 1f)
            DrawString(font, new Vector2(0f, baseline), _endLabel, HorizontalAlignment.Right, Size.X, size, UITheme.TextDim);
    }

    private void DrawCurve(Rect2 plot, float revealX)
    {
        if (_curve.Count < 2)
            return;
        List<Vector2> line = new(_curve.Count + 1);
        foreach (Vector2 point in _curve)
        {
            Vector2 p = ToPlot(point, plot);
            if (p.X > revealX)
            {
                // Coupe au bord dévoilé, à la hauteur interpolée.
                Vector2 previous = line.Count > 0 ? line[^1] : p;
                float t = Mathf.IsEqualApprox(p.X, previous.X) ? 1f : (revealX - previous.X) / (p.X - previous.X);
                line.Add(previous.Lerp(p, Mathf.Clamp(t, 0f, 1f)));
                break;
            }
            line.Add(p);
        }
        if (line.Count < 2)
            return;
        Vector2[] fill = new Vector2[line.Count + 2];
        for (int i = 0; i < line.Count; i++)
            fill[i] = line[i];
        fill[line.Count] = new Vector2(line[^1].X, plot.End.Y);
        fill[line.Count + 1] = new Vector2(line[0].X, plot.End.Y);
        DrawColoredPolygon(fill, FillColor);
        DrawPolyline(line.ToArray(), CurveColor, 2f);
    }

    private void DrawLevelBand(Rect2 plot, float revealX)
    {
        int peak = 1;
        foreach (int count in _levelDensity)
            peak = Mathf.Max(peak, count);
        float width = plot.Size.X / LevelBuckets;
        for (int i = 0; i < LevelBuckets; i++)
        {
            if (_levelDensity[i] == 0 || i * width > revealX)
                continue;
            float strength = 0.25f + 0.75f * _levelDensity[i] / peak;
            DrawRect(new Rect2(plot.Position.X + i * width, plot.End.Y + 2f, Mathf.Ceil(width), LevelBand), new Color(UITheme.GoldColor, strength));
        }
    }

    private void DrawMarker(RunMarkerKind kind, float x, Rect2 plot)
    {
        switch (kind)
        {
            case RunMarkerKind.Crisis:
                DrawRect(new Rect2(x - 1f, plot.Position.Y, 2f, plot.Size.Y), new Color(CrisisColor, 0.85f));
                break;
            case RunMarkerKind.Chest:
                DrawRect(new Rect2(x - 1f, plot.Position.Y - 21f, 3f, 3f), new Color(ChestColor, 0.7f));
                break;
            default:
                Color color = kind switch
                {
                    RunMarkerKind.Sovereign => SovereignColor,
                    RunMarkerKind.Boss => BossColor,
                    _ => EliteColor,
                };
                float r = kind == RunMarkerKind.Elite ? 4f : 6f;
                Vector2 c = new(x, plot.Position.Y - 10f);
                DrawColoredPolygon(new[] { c + new Vector2(0f, -r), c + new Vector2(r, 0f), c + new Vector2(0f, r), c + new Vector2(-r, 0f) }, color);
                break;
        }
    }

    private static Vector2 ToPlot(Vector2 normalized, Rect2 plot) =>
        new(plot.Position.X + normalized.X * plot.Size.X, plot.End.Y - normalized.Y * plot.Size.Y);
}
