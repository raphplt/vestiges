using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Allure des chemins de terre (plan 10 T3) : <c>path_style</c> d'un biome, ou <c>paths.default_style</c> de world_gen.json.
/// </summary>
public struct PathStyle
{
    /// <summary>Ton moyen du chemin ; le shader en tire ses quatre tons.</summary>
    public Color Tone;

    /// <summary>0 : piste pleine ; 1 : deux traces de roues, le sol visible au milieu.</summary>
    public float Ruts;

    /// <summary>Largeur au sol, en pixels.</summary>
    public float WidthPx;

    public static PathStyle Default => new() { Tone = new Color("#6f5d46"), Ruts = 0f, WidthPx = 28f };

    public static PathStyle Parse(Godot.Collections.Dictionary dict, PathStyle fallback)
    {
        return new PathStyle
        {
            Tone = dict.ContainsKey("tone") ? new Color(dict["tone"].AsString()) : fallback.Tone,
            Ruts = dict.ContainsKey("ruts") ? (float)dict["ruts"].AsDouble() : fallback.Ruts,
            WidthPx = dict.ContainsKey("width_px") ? (float)dict["width_px"].AsDouble() : fallback.WidthPx,
        };
    }
}
