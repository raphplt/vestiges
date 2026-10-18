using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Halo au sol en pixel art : bord rond au grain propre à chaque porteur (plan 25 S12), lueur tramée plus dense près du
/// bord, qui respire par poses (trois rayons) plutôt qu'en glissement continu ; la trame se décale à chaque pose et
/// scintille, des tirets plus vifs tournent sur le bord.
/// Aura des créatures à affixe (une par créature, réutilisée par le pool), repère des armes au sol.
/// Le dessin ne se refait qu'au changement de pose.
/// </summary>
public partial class PixelGroundRing : Node2D
{
    private const float PoseSec = 0.2f;
    public float RimAlpha { get; set; } = 0.7f;
    public float FillAlpha { get; set; } = 0.2f;
    private static readonly int[] PoseOffsets = { 0, 1, 2, 1 };
    private static readonly Dictionary<int, (Vector2I[] Rim, Vector2I[] Fill)> Shapes = new();

    private readonly List<Color> _colors = new();
    private int _colorIndex;
    private int _baseRadius;
    private int _variant;
    private int _pose;
    private float _poseTimer;

    public PixelGroundRing()
    {
        // Au sol, sous le corps de la créature.
        ZIndex = -1;
        Visible = false;
        SetProcess(false);
    }

    /// <summary>Allume l'anneau, de demi-largeur <paramref name="radius"/> pixels au sol.</summary>
    public void Show(Color color, float radius)
    {
        _colors.Clear();
        _colors.Add(color);
        Begin(radius);
    }

    /// <summary>Plusieurs couleurs (plusieurs affixes) : l'anneau passe de l'une à l'autre à chaque respiration.</summary>
    public void Show(IReadOnlyList<Color> colors, float radius)
    {
        _colors.Clear();
        for (int i = 0; i < colors.Count; i++)
            _colors.Add(colors[i]);
        if (_colors.Count == 0)
            _colors.Add(Colors.White);
        Begin(radius);
    }

    private void Begin(float radius)
    {
        _colorIndex = 0;
        _baseRadius = Mathf.Max(6, Mathf.RoundToInt(radius));
        _variant = (int)(GetInstanceId() % PixelBlob.Variants);
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
        if (_pose == 0)
            _colorIndex = (_colorIndex + 1) % _colors.Count;
        QueueRedraw();
    }

    public override void _Draw()
    {
        PixelBlob.Shape shape = PixelBlob.Get(_baseRadius + PoseOffsets[_pose], _variant, 1f);
        Color color = _colors[_colorIndex];
        Color fillColor = color with { A = FillAlpha };
        Vector2I shift = new(_pose, _pose * 2);
        for (int i = 0; i < shape.Inside.Length; i++)
        {
            // Lueur plus dense près du bord, quelques pixels épars au centre.
            float density = shape.Depth[i] > 0.7f ? 0.5f : 0.2f;
            if (PixelBlob.Bayer(shape.Inside[i] + shift) < density)
                DrawRect(new Rect2(shape.Inside[i], Vector2.One), fillColor);
        }
        Color rimColor = color with { A = RimAlpha };
        Color rimDim = color with { A = RimAlpha * 0.55f };
        foreach (Vector2I pixel in shape.Rim)
        {
            // Douze tirets qui avancent d'un quart de pas à chaque pose : l'aura tourne autour du porteur.
            float turn = Mathf.Atan2((pixel.Y + 0.5f) * Iso.GroundSquash, pixel.X + 0.5f) / Mathf.Tau * 12f + _pose * 0.25f;
            DrawRect(new Rect2(pixel, Vector2.One), turn - Mathf.Floor(turn) < 0.5f ? rimColor : rimDim);
        }
    }

    /// <summary>
    /// Ellipse au sol exacte (deux fois plus large que haute) : pixels du bord et trame d'un pixel sur deux dedans, pour
    /// les repères des micro-événements (<see cref="PixelEllipseArt"/>, <c>RunEventMarker</c>).
    /// </summary>
    public static (Vector2I[] Rim, Vector2I[] Fill) Ellipse(int radius)
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
