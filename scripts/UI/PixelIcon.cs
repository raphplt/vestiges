using System.Collections.Generic;
using Godot;

namespace Vestiges.UI;

/// <summary>
/// Petite icône dessinée pixel par pixel depuis un motif de caractères, avec un contour sombre d'un pixel, en unités du
/// contrôle (à l'échelle de l'écran qui la porte). Pour les signes d'interface qui n'ont pas encore de sprite (plan 25).
/// </summary>
public partial class PixelIcon : Control
{
    private static readonly Color Outline = new(0.03f, 0.02f, 0.05f, 0.9f);

    private readonly string[] _pattern;
    private readonly Dictionary<char, Color> _palette;
    private readonly float _pixel;

    public PixelIcon() : this(new[] { "#" }, new Dictionary<char, Color> { ['#'] = Colors.White }, 1f)
    {
    }

    public PixelIcon(string[] pattern, Dictionary<char, Color> palette, float pixel)
    {
        _pattern = pattern;
        _palette = palette;
        _pixel = pixel;
        MouseFilter = MouseFilterEnum.Ignore;
        int width = 0;
        foreach (string row in pattern)
            width = Mathf.Max(width, row.Length);
        CustomMinimumSize = new Vector2(width + 2, pattern.Length + 2) * pixel;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
    }

    public override void _Draw()
    {
        for (int y = 0; y < _pattern.Length; y++)
            for (int x = 0; x < _pattern[y].Length; x++)
                if (_palette.ContainsKey(_pattern[y][x]))
                    DrawRect(new Rect2(new Vector2(x, y) * _pixel, Vector2.One * 3f * _pixel), Outline);
        for (int y = 0; y < _pattern.Length; y++)
            for (int x = 0; x < _pattern[y].Length; x++)
                if (_palette.TryGetValue(_pattern[y][x], out Color color))
                    DrawRect(new Rect2(new Vector2(x + 1, y + 1) * _pixel, Vector2.One * _pixel), color);
    }

    /// <summary>Trèfle à quatre feuilles : la Chance a joué (plan 24 D2).</summary>
    public static PixelIcon Clover(float pixel) => new(new[]
    {
        " ## ## ",
        "#ll#ll#",
        "#l####d",
        " ##### ",
        "#l####d",
        "#ld#dd#",
        " ## ## ",
        "   d   ",
        "  d    ",
    }, new Dictionary<char, Color>
    {
        ['#'] = new(0.36f, 0.72f, 0.36f),
        ['l'] = new(0.62f, 0.90f, 0.52f),
        ['d'] = new(0.20f, 0.46f, 0.24f),
    }, pixel);
}
