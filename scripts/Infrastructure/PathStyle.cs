using System.Text.Json;
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

    /// <summary>
    /// Style lu strictement (<c>path_style</c> d'un biome, <c>paths.default_style</c>) : un champ absent garde la valeur
    /// de <see cref="Default"/>.
    /// </summary>
    public static PathStyle Read(JsonConfigReader reader, JsonElement section, string where)
    {
        reader.AllowOnly(section, where, "tone", "ruts", "width_px");
        PathStyle style = Default;
        if (JsonConfigReader.Has(section, "tone"))
        {
            string tone = reader.Text(section, "tone");
            if (tone != null && !Color.HtmlIsValid(tone))
                reader.Fail($"{where}.tone : couleur HTML attendue");
            else if (tone != null)
                style.Tone = new Color(tone);
        }
        if (JsonConfigReader.Has(section, "ruts"))
            style.Ruts = reader.Chance(section, "ruts");
        if (JsonConfigReader.Has(section, "width_px"))
            style.WidthPx = reader.Positive(section, "width_px");
        return style;
    }
}
