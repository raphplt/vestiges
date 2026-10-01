using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Composition d'une offre de level-up, lue dans data/progression/level_up_offer.json (plan 24 lot L3).</summary>
public sealed class LevelUpOfferConfig
{
    public int WeaponGuaranteeBelow { get; private init; }
    public float WeaponChanceAfter { get; private init; }
    public float TierBumpChance { get; private init; }
    public float TierBumpLuck { get; private init; }
    public float UpgradeWeight { get; private init; }
    public float MinWeight { get; private init; }

    private readonly List<int> _tierLevels = new();
    private readonly List<(int MinTier, float Base, float Luck)> _tierWeights = new();

    private static LevelUpOfferConfig _cached;

    /// <summary>Palier d'arme le plus haut offert à ce niveau du joueur, avant la chance d'un palier de plus.</summary>
    public int BaseTier(int playerLevel)
    {
        int tier = 1;
        foreach (int level in _tierLevels)
            if (playerLevel >= level)
                tier++;
        return tier;
    }

    public int MaxTier => _tierLevels.Count + 1;

    /// <summary>Poids d'une carte d'arme de ce palier : la Chance relève les paliers hauts.</summary>
    public float TierWeight(int tier, float luck)
    {
        foreach ((int minTier, float weightBase, float weightLuck) in _tierWeights)
            if (tier >= minTier)
                return weightBase + luck * weightLuck;
        return 1f;
    }

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

        LevelUpOfferConfig config = new()
        {
            WeaponGuaranteeBelow = (int)data["weapon_guarantee_below"].AsDouble(),
            WeaponChanceAfter = (float)data["weapon_chance_after"].AsDouble(),
            TierBumpChance = (float)data["tier_bump_chance"].AsDouble(),
            TierBumpLuck = (float)data["tier_bump_luck"].AsDouble(),
            UpgradeWeight = (float)data["upgrade_weight"].AsDouble(),
            MinWeight = (float)data["min_weight"].AsDouble(),
        };
        foreach (Variant level in data["tier_levels"].AsGodotArray())
            config._tierLevels.Add((int)level.AsDouble());
        foreach (Variant entry in data["tier_weights"].AsGodotArray())
        {
            Godot.Collections.Dictionary weight = entry.AsGodotDictionary();
            config._tierWeights.Add(((int)weight["min_tier"].AsDouble(), (float)weight["base"].AsDouble(), (float)weight["luck"].AsDouble()));
        }
        // Le palier le plus haut d'abord : TierWeight prend la première règle qui s'applique.
        config._tierWeights.Sort((a, b) => b.MinTier.CompareTo(a.MinTier));
        _cached = config;
        return _cached;
    }
}
