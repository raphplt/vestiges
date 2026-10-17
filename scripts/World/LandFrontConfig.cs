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
                "heart_radius", "noise_seconds", "noise_frequency");
            LandFrontConfig config = new()
            {
                FirstFallSeconds = reader.Positive(front, "first_fall_seconds"),
                HeartReachedSeconds = reader.Positive(front, "heart_reached_seconds"),
                WarningSeconds = reader.Positive(front, "warning_seconds"),
                HeartRadius = reader.Ratio(front, "heart_radius"),
                NoiseSeconds = reader.NonNegative(front, "noise_seconds"),
                NoiseFrequency = reader.Positive(front, "noise_frequency"),
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
