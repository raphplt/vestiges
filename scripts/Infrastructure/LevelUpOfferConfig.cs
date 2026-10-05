using System;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Poids des cartes d'une offre de level-up, lus dans data/progression/level_up_offer.json (plan 24 lot L3) et
/// contrôlés (plan 26 Q7b) : deux poids strictement positifs, aucune autre clé.
/// </summary>
public sealed class LevelUpOfferConfig
{
    private const string ConfigPath = "res://data/progression/level_up_offer.json";

    public float UpgradeWeight { get; private init; }
    public float MinWeight { get; private init; }

    private static LevelUpOfferConfig _cached;

    /// <summary>La composition du jeu ; invalide, elle arrête le chargement de la run (GameBootstrap).</summary>
    public static LevelUpOfferConfig Load() =>
        TryLoad(out LevelUpOfferConfig config, out string error) ? config : throw new InvalidOperationException(error);

    public static bool TryLoad(out LevelUpOfferConfig config, out string error)
    {
        if (_cached != null)
        {
            config = _cached;
            error = null;
            return true;
        }
        if (!FileAccess.FileExists(ConfigPath))
        {
            config = null;
            error = $"{ConfigPath} absent";
            return false;
        }
        if (!TryParse(FileAccess.GetFileAsString(ConfigPath), out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        _cached = config;
        error = null;
        return true;
    }

    public static bool TryParse(string json, out LevelUpOfferConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "offre de niveau", "upgrade_weight", "min_weight");
            LevelUpOfferConfig parsed = new()
            {
                UpgradeWeight = reader.Positive(reader.Root, "upgrade_weight"),
                MinWeight = reader.Positive(reader.Root, "min_weight"),
            };
            error = reader.Error;
            config = error == null ? parsed : null;
            return error == null;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }
}
