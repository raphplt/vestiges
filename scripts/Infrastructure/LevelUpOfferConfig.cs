using System;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Poids des cartes d'une offre de level-up, lus dans data/progression/level_up_offer.json (plan 24 lot L3).</summary>
public sealed class LevelUpOfferConfig
{
    public float UpgradeWeight { get; private init; }
    public float MinWeight { get; private init; }

    private static LevelUpOfferConfig _cached;

    public static LevelUpOfferConfig Load()
    {
        if (_cached != null)
            return _cached;

        using FileAccess file = FileAccess.Open("res://data/progression/level_up_offer.json", FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidOperationException("Composition du level-up absente.");
        using Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException("Composition du level-up invalide.");
        Godot.Collections.Dictionary data = json.Data.AsGodotDictionary();

        _cached = new LevelUpOfferConfig
        {
            UpgradeWeight = (float)data["upgrade_weight"].AsDouble(),
            MinWeight = (float)data["min_weight"].AsDouble(),
        };
        return _cached;
    }
}
