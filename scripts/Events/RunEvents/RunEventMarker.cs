using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Repère au sol d'un micro-événement : colonne de lumière visible de loin (la même que les coffres), anneau de zone
/// avec sa progression, vestige en chute puis posé. Détaché du porteur, au-dessus du sol. Tout est dessiné au pixel du
/// monde, comme les anneaux d'affixe (plan 37 M1) : bords pleins, intérieur tramé, respiration par poses de 0,2 s et
/// non en glissement continu ; le dessin ne se refait qu'au changement de pose ou de progression.
/// </summary>
public partial class RunEventMarker : Node2D
{
    public enum MarkerStyle { Beacon, Zone, Relic }

    private const float BeaconHeight = 90f;
    private const float FallHeight = 220f;
    private const float PoseSec = 0.2f;
    private const int ProgressGap = 4;
    private const int ProgressSteps = 64;
    private const int RelicGlowRadius = 18;
    private static readonly int[] PoseOffsets = { 0, 1, 2, 1 };
    // Losange du vestige, rangée par rangée : demi-largeur de chaque ligne, de la pointe haute à la pointe basse.
    private static readonly int[] CrystalRows = { 0, 1, 1, 2, 2, 3, 3, 4, 4, 3, 3, 2, 2, 1, 1, 0 };
    private static readonly Dictionary<int, (Vector2I Pixel, float Turn)[]> ProgressRims = new();

    private MarkerStyle _style;
    private int _radius;
    private Color _color;
    private float _progress;
    private int _shownProgress = -1;
    private float _fall;
    private float _poseTimer;
    private int _pose;
    private bool _active = true;
    private LightColumn _column;

    public RunEventMarker()
    {
        TopLevel = true;
        ZAsRelative = false;
        ZIndex = 1;
    }

    public void Setup(MarkerStyle style, Vector2 position, float radius, Color color)
    {
        _style = style;
        // Calé sur le pixel du monde : le bord tombe sur la grille des sprites.
        GlobalPosition = position.Round();
        _radius = Mathf.Max(6, Mathf.RoundToInt(radius));
        _color = color;
        _column ??= new LightColumn { Name = "LightColumn" };
        if (_column.GetParent() == null)
            AddChild(_column);
        _column.Configure(new RarityColors(color, color.Lightened(0.45f), color.Darkened(0.5f)), BeaconHeight, 1.5f, 5f);
        _column.Visible = style != MarkerStyle.Relic;
        QueueRedraw();
    }

    public void SetProgress(float progress)
    {
        _progress = Mathf.Clamp(progress, 0f, 1f);
        int shown = Mathf.FloorToInt(_progress * ProgressSteps);
        if (shown != _shownProgress)
        {
            _shownProgress = shown;
            QueueRedraw();
        }
    }

    /// <summary>Avancement de la chute du vestige (0 = dans le ciel, 1 = posé).</summary>
    public void SetFall(float fall)
    {
        _fall = Mathf.Clamp(fall, 0f, 1f);
        if (_column != null)
            _column.Visible = _fall >= 1f;
        if (_fall < 1f)
            QueueRedraw();
    }

    /// <summary>Zone franchie par le joueur : l'anneau s'intensifie.</summary>
    public void SetActive(bool active)
    {
        if (_active == active)
            return;
        _active = active;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _poseTimer += (float)delta;
        if (_poseTimer < PoseSec)
            return;
        _poseTimer -= PoseSec;
        _pose = (_pose + 1) % PoseOffsets.Length;
        QueueRedraw();
    }

    public override void _Draw()
    {
        switch (_style)
        {
            case MarkerStyle.Beacon:
                DrawBeacon();
                break;
            case MarkerStyle.Zone:
                DrawZone();
                break;
            case MarkerStyle.Relic:
                DrawRelic();
                break;
        }
    }

    // Anneau qui respire en dedans du bord, par poses : 90 à 100 % du rayon.
    private void DrawBeacon()
    {
        int breath = Mathf.RoundToInt(_radius * 0.05f) * PoseOffsets[_pose];
        DrawEllipse(_radius - Mathf.RoundToInt(_radius * 0.1f) + breath, false, _color with { A = 0.7f });
    }

    private void DrawZone()
    {
        float pulse = PoseOffsets[_pose] * 0.025f;
        DrawEllipse(_radius, true, _color with { A = (_active ? 0.3f : 0.16f) + pulse });
        DrawEllipse(_radius, false, _color with { A = _active ? 0.9f : 0.55f });
        if (_shownProgress > 0)
            DrawProgress(_radius + ProgressGap, _shownProgress / (float)ProgressSteps, _color.Lightened(0.35f));
        DrawBeacon();
    }

    private void DrawRelic()
    {
        if (_fall < 1f)
        {
            // Traînée qui descend du ciel vers la zone d'impact annoncée.
            int height = Mathf.RoundToInt(FallHeight * (1f - _fall));
            DrawRect(new Rect2(-1, -height - 40, 2, 40), _color with { A = 0.35f });
            DrawCrystal(new Vector2I(0, -height));
            return;
        }
        DrawEllipse(RelicGlowRadius + PoseOffsets[_pose] * 2, true, _color with { A = 0.35f });
        DrawCrystal(new Vector2I(0, -10 - PoseOffsets[_pose]));
        DrawBeacon();
    }

    private void DrawEllipse(int radius, bool fill, Color color) =>
        DrawTexture(PixelEllipseArt.Get(radius, fill), PixelEllipseArt.Offset(radius), color);

    /// <summary>Arc de progression : les pixels du bord, dans l'ordre des aiguilles depuis le haut.</summary>
    private void DrawProgress(int radius, float progress, Color color)
    {
        (Vector2I Pixel, float Turn)[] rim = ProgressRimOf(radius);
        for (int i = 0; i < rim.Length && rim[i].Turn <= progress; i++)
            DrawRect(new Rect2(rim[i].Pixel, Vector2.One), color);
    }

    private static (Vector2I Pixel, float Turn)[] ProgressRimOf(int radius)
    {
        if (ProgressRims.TryGetValue(radius, out (Vector2I Pixel, float Turn)[] rim))
            return rim;
        Vector2I[] pixels = PixelGroundRing.Ellipse(radius).Rim;
        rim = new (Vector2I, float)[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            // Angle au sol (l'ellipse redressée), compté depuis le haut dans le sens des aiguilles.
            Vector2 ground = new(pixels[i].X + 0.5f, (pixels[i].Y + 0.5f) * Iso.GroundSquash);
            float turn = Mathf.PosMod(Mathf.Atan2(ground.X, -ground.Y), Mathf.Tau) / Mathf.Tau;
            rim[i] = (pixels[i], turn);
        }
        System.Array.Sort(rim, (a, b) => a.Turn.CompareTo(b.Turn));
        ProgressRims[radius] = rim;
        return rim;
    }

    /// <summary>Vestige en losange au pixel : contour sombre, cœur clair, reflet blanc sur l'arête gauche.</summary>
    private void DrawCrystal(Vector2I tip)
    {
        Color outline = _color.Darkened(0.5f);
        Color body = _color.Lightened(0.3f);
        Color shine = Colors.White with { A = 0.75f };
        for (int row = 0; row < CrystalRows.Length; row++)
        {
            int half = CrystalRows[row];
            int y = tip.Y + row;
            DrawRect(new Rect2(tip.X - half, y, half * 2 + 1, 1), outline);
            if (half > 1)
                DrawRect(new Rect2(tip.X - half + 1, y, half * 2 - 1, 1), body);
            if (half > 1 && row < CrystalRows.Length / 2)
                DrawRect(new Rect2(tip.X - half + 1, y, 1, 1), shine);
        }
    }
}
