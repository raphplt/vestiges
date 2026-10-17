using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Tache au sol en pixels pour les dessins faits en code : ellipse 2:1 au bord rond et rugueux, comme les zones de
/// pixel_fx.gdshader (plan 25 S12). Une graine donne un grain ; les formes sont calculées une fois et partagées.
/// </summary>
public static class PixelBlob
{
    /// <summary>Pixels du bord, pixels intérieurs et, pour chacun, sa distance relative au centre (0 centre, 1 bord).</summary>
    public readonly record struct Shape(Vector2I[] Rim, Vector2I[] Inside, float[] Depth);

    /// <summary>Formes différentes par rayon : assez pour que deux créatures voisines ne se ressemblent pas.</summary>
    public const int Variants = 4;

    private static readonly Dictionary<(int Radius, int Variant, int Amp), Shape> Cache = new();

    /// <summary>Tache de demi-largeur <paramref name="radius"/> pixels ; <paramref name="amp"/> règle le grain du bord.</summary>
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

    /// <summary>Grain d'au plus 1,2 × amp pixels au sol, continu le long du bord : cercle exact, jamais une patate.</summary>
    private static float Relative(int x, int y, float radius, float phase, float amp)
    {
        float groundX = x + 0.5f;
        float groundY = (y + 0.5f) * Iso.GroundSquash;
        float theta = Mathf.Atan2(groundY, groundX);
        float rim = radius + amp * 1.2f * (RimNoise(theta, phase, radius) - 0.5f) * 2f;
        return Mathf.Sqrt(groundX * groundX + groundY * groundY) / rim;
    }

    /// <summary>Bruit de valeur périodique sur le tour du bord, une cellule tous les deux pixels de circonférence environ.</summary>
    private static float RimNoise(float theta, float phase, float radius)
    {
        int cells = Mathf.Max(8, Mathf.RoundToInt(radius * 2.8f));
        float u = (theta / Mathf.Tau + 0.5f) * cells;
        int index = Mathf.FloorToInt(u);
        float t = u - index;
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(Hash(Mathf.PosMod(index, cells), phase), Hash(Mathf.PosMod(index + 1, cells), phase), t);
    }

    private static float Hash(int index, float phase)
    {
        float value = Mathf.Sin(index * 12.9898f + phase * 78.233f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
