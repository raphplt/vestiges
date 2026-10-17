using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Réglages du front de chute d'une terre (plan 51 P2a, section <c>land_front</c> de data/scaling/erasure.json),
/// contrôlés en entier : une section invalide est refusée avec le nom du champ fautif.
/// </summary>
public sealed class LandFrontConfig
{
    private const string ConfigPath = "res://data/scaling/erasure.json";

    /// <summary>Le bord le plus exposé tombe à cette date (secondes de terre).</summary>
    public float FirstFallSeconds { get; init; }
    /// <summary>Le dernier sol hors du cœur tombe à cette date : la terre est alors réduite à son cœur.</summary>
    public float HeartReachedSeconds { get; init; }
    /// <summary>Préavis entier avant la chute : trois états visibles (fragile, effiloché, effacé) d'un tiers chacun.</summary>
    public float WarningSeconds { get; init; }
    /// <summary>Rayon du cœur, en part du rayon de la terre : il ne tombe jamais.</summary>
    public float HeartRadius { get; init; }
    /// <summary>Écart de date dessiné par le bruit (langues et baies), en secondes de part et d'autre.</summary>
    public float NoiseSeconds { get; init; }
    /// <summary>Fréquence du bruit, par cellule d'Effacement.</summary>
    public float NoiseFrequency { get; init; }
    /// <summary>Rayon ancré par un Mémorial ravivé, en cellules d'Effacement (P2b).</summary>
    public float AnchorRadiusCells { get; init; }
    /// <summary>Essence prise par seconde dans le Néant d'une terre, avant les PV (P2d).</summary>
    public float VoidEssencePerSecond { get; init; }
    /// <summary>Durée de la recousure à l'arrivée sur une terre (§16 R1) : le sol monte du Néant, du plus proche au plus lointain.</summary>
    public float StitchSeconds { get; init; }

    public static LandFrontConfig Load(out string error)
    {
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            error = $"{ConfigPath} absent";
            return null;
        }
        return Parse(file.GetAsText(), out error);
    }

    public static LandFrontConfig Parse(string json, out string error)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement front = reader.Section("land_front");
            reader.AllowOnly(front, "land_front", "_doc", "first_fall_seconds", "heart_reached_seconds", "warning_seconds",
                "heart_radius", "noise_seconds", "noise_frequency", "anchor_radius_cells",
                "void_essence_per_second", "stitch_seconds");
            LandFrontConfig config = new()
            {
                FirstFallSeconds = reader.Positive(front, "first_fall_seconds"),
                HeartReachedSeconds = reader.Positive(front, "heart_reached_seconds"),
                WarningSeconds = reader.Positive(front, "warning_seconds"),
                HeartRadius = reader.Ratio(front, "heart_radius"),
                NoiseSeconds = reader.NonNegative(front, "noise_seconds"),
                NoiseFrequency = reader.Positive(front, "noise_frequency"),
                AnchorRadiusCells = reader.Positive(front, "anchor_radius_cells"),
                VoidEssencePerSecond = reader.NonNegative(front, "void_essence_per_second"),
                StitchSeconds = reader.NonNegative(front, "stitch_seconds"),
            };
            if (reader.Error == null && config.HeartReachedSeconds <= config.FirstFallSeconds)
                reader.Fail("land_front : heart_reached_seconds doit suivre first_fall_seconds");
            error = reader.Error;
            return error == null ? config : null;
        }
        catch (JsonException exception)
        {
            error = $"JSON illisible : {exception.Message}";
            return null;
        }
    }
}
