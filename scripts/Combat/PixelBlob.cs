using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Tache au sol en pixels pour les dessins faits en code : ellipse 2:1 au bord en lobes, mêmes harmoniques que les
/// zones de pixel_fx.gdshader (plan 25 S10). Une graine donne une forme ; les formes sont calculées une fois et partagées.
/// </summary>
public static class PixelBlob
{
    /// <summary>Pixels du bord, pixels intérieurs et, pour chacun, sa distance relative au centre (0 centre, 1 bord).</summary>
    public readonly record struct Shape(Vector2I[] Rim, Vector2I[] Inside, float[] Depth);

    /// <summary>Formes différentes par rayon : assez pour que deux créatures voisines ne se ressemblent pas.</summary>
    public const int Variants = 4;

    private static readonly Dictionary<(int Radius, int Variant, int Amp), Shape> Cache = new();

    /// <summary>Tache de demi-largeur <paramref name="radius"/> pixels ; <paramref name="amp"/> règle la force des lobes.</summary>
    public static Shape Get(int radius, int variant, float amp)
    {
        (int, int, int) key = (radius, variant % Variants, Mathf.RoundToInt(amp * 100f));
        if (Cache.TryGetValue(key, out Shape shape))
            return shape;
        float phase = 1.7f + variant % Variants * 2.9f;
        List<Vector2I> rim = new();
        List<Vector2I> inside = new();
        List<float> depth = new();
        int maxY = Mathf.CeilToInt(radius * 1.2f / Iso.GroundSquash);
        int maxX = Mathf.CeilToInt(radius * 1.2f);
        for (int y = -maxY; y <= maxY; y++)
        {
            for (int x = -maxX; x <= maxX; x++)
            {
                float relative = Relative(x, y, radius, phase, amp);
                if (relative > 1f)
                    continue;
                bool edge = Relative(x + 1, y, radius, phase, amp) > 1f || Relative(x - 1, y, radius, phase, amp) > 1f
                    || Relative(x, y + 1, radius, phase, amp) > 1f || Relative(x, y - 1, radius, phase, amp) > 1f;
                if (edge)
                {
                    rim.Add(new Vector2I(x, y));
                    continue;
                }
                inside.Add(new Vector2I(x, y));
                depth.Add(relative);
            }
        }
        shape = new Shape(rim.ToArray(), inside.ToArray(), depth.ToArray());
        Cache[key] = shape;
        return shape;
    }

    /// <summary>Trame ordonnée de Bayer 4×4, entre 0 et 1, comme celle du shader.</summary>
    public static float Bayer(Vector2I pixel)
    {
        int x = pixel.X & 3;
        int y = pixel.Y & 3;
        int value = 4 * Bayer2(x & 1, y & 1) + Bayer2(x >> 1, y >> 1);
        return (value + 0.5f) / 16f;
    }

    private static int Bayer2(int x, int y) => ((x ^ y) << 1) | y;

    private static float Relative(int x, int y, float radius, float phase, float amp)
    {
        float groundX = x + 0.5f;
        float groundY = (y + 0.5f) * Iso.GroundSquash;
        float theta = Mathf.Atan2(groundY, groundX);
        float lobes = 1f + amp * (0.07f * Mathf.Sin(3f * theta + phase) + 0.05f * Mathf.Sin(5f * theta + phase * 2.3f)
            + 0.03f * Mathf.Sin(9f * theta + phase * 4.1f));
        return Mathf.Sqrt(groundX * groundX + groundY * groundY) / (radius * lobes);
    }
}
