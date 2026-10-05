using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Un groupe de lieux : combien, et dans quelle couronne de distance au départ (fraction du rayon).</summary>
public readonly record struct LandmarkBand(int Count, float Min, float Max);

/// <summary>Réglages des Mémoriaux (section <c>memorial</c> de data/world/landmarks.json).</summary>
public class MemorialConfig
{
    public string SpriteDormant;
    public string SpriteAwake;
    public string SpriteShard;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx;
    public float HoldTime;
    public int Shards;
    public float ShardDistanceMin;
    public float ShardDistanceMax;
    public float ShardTime;
    public float ShardPickupPx;
    public int BlessingChoices;
    public string BlessingMinRarity;
    public int HealCost;
    public float HealPercent;
    public float CostGrowth;
    public int LiftOubliCost;
    /// <summary>Relancer les trois bénédictions d'un Mémorial qu'on vient de raviver (plan 17, 4.5).</summary>
    public int BlessingRerollCost;
}

/// <summary>Réglages des Ateliers (section <c>workshop</c> de data/world/landmarks.json, plan 22 C2).</summary>
public class WorkshopConfig
{
    public string Sprite;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx;
    public int WeaponCost;
    public string WeaponMinRarity;
    public int RetemperCost;
    public int TemperUpgrades;
    public float CostGrowth;
}

/// <summary>Réglages des Failles (section <c>rift</c> de data/world/landmarks.json).</summary>
public class RiftConfig
{
    public string SpriteOpen;
    public string SpriteClosed;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx;
    public float HoldTime;
    public int Offers;
    public string OfferMinRarity;
    public int PerilPerOffer;
    public int MaxOpen;
    public float SpawnChance;
    public float SpawnCooldown;
    public float SpawnDistanceMin;
    public float SpawnDistanceMax;
}

/// <summary>
/// Lieux du monde à trouver hors coffres : Mémoriaux, Ateliers et Failles (data/world/landmarks.json), contrôlés en
/// entier (plan 26 Q7c) : tous les champs sont obligatoires, les raretés minimales existent, les images aussi.
/// </summary>
public static class LandmarkDataLoader
{
    private const string ConfigPath = "res://data/world/landmarks.json";
    private static MemorialConfig _memorial;
    private static RiftConfig _rift;
    private static WorkshopConfig _workshop;
    private static string _loadError;

    public static MemorialConfig Memorial
    {
        get
        {
            Load();
            return _memorial;
        }
    }

    public static WorkshopConfig Workshop
    {
        get
        {
            Load();
            return _workshop;
        }
    }

    public static RiftConfig Rift
    {
        get
        {
            Load();
            return _rift;
        }
    }

    /// <summary>Réglages lus et contrôlés ; faux, avec la raison, s'ils ont été refusés (le chargement de la run s'arrête).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    private static void Load()
    {
        if (_memorial != null)
            return;
        // Refusés, les réglages restent vides : la run ne démarre pas (GameBootstrap).
        _memorial = new MemorialConfig();
        _rift = new RiftConfig();
        _workshop = new WorkshopConfig();
        string error = FileAccess.FileExists(ConfigPath)
            ? Apply(FileAccess.GetFileAsString(ConfigPath), path => ResourceLoader.Exists(path), RarityIds())
            : "absent";
        if (error != null)
        {
            _loadError = $"{ConfigPath} : {error}";
            GD.PushError($"[LandmarkDataLoader] {_loadError}");
        }
    }

    /// <summary>Raretés d'amélioration connues : les raretés minimales des lieux en font partie.</summary>
    private static HashSet<string> RarityIds() => DataKeySets.ListIds("res://data/progression/upgrade_rarities.json", "rarities");

    /// <summary>Contrôle un texte de réglages ; ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json, Func<string, bool> exists, IReadOnlyCollection<string> rarityIds)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "lieux", "memorial", "rift", "workshop");

            JsonElement m = reader.Section("memorial");
            reader.AllowOnly(m, "memorial", "sprite_dormant", "sprite_awake", "sprite_shard", "placement", "min_spacing_px",
                "hold_time", "shards", "shard_distance_px", "shard_time", "shard_pickup_px", "blessing_choices",
                "blessing_min_rarity", "services");
            JsonElement ms = reader.Section(m, "services");
            reader.AllowOnly(ms, "memorial.services", "heal_cost", "heal_percent", "cost_growth", "lift_oubli_cost", "blessing_reroll_cost");
            (float shardMin, float shardMax) = reader.Range(m, "shard_distance_px", 0f);
            MemorialConfig memorial = new()
            {
                SpriteDormant = reader.Resource(m, "sprite_dormant", exists),
                SpriteAwake = reader.Resource(m, "sprite_awake", exists),
                SpriteShard = reader.Resource(m, "sprite_shard", exists),
                Placement = Bands(reader, m),
                MinSpacingPx = reader.Positive(m, "min_spacing_px"),
                HoldTime = reader.Positive(m, "hold_time"),
                Shards = reader.Count(m, "shards", 12),
                ShardDistanceMin = shardMin,
                ShardDistanceMax = shardMax,
                ShardTime = reader.Positive(m, "shard_time"),
                ShardPickupPx = reader.Positive(m, "shard_pickup_px"),
                BlessingChoices = reader.Count(m, "blessing_choices", 6),
                BlessingMinRarity = reader.OneOf(m, "blessing_min_rarity", rarityIds),
                HealCost = reader.Integer(ms, "heal_cost", 0, 100_000),
                HealPercent = reader.Ratio(ms, "heal_percent"),
                CostGrowth = reader.NonNegative(ms, "cost_growth"),
                LiftOubliCost = reader.Integer(ms, "lift_oubli_cost", 0, 100_000),
                BlessingRerollCost = reader.Integer(ms, "blessing_reroll_cost", 0, 100_000),
            };

            JsonElement r = reader.Section("rift");
            reader.AllowOnly(r, "rift", "sprite_open", "sprite_closed", "placement", "min_spacing_px", "hold_time", "offers",
                "offer_min_rarity", "peril_per_offer", "erased_spawn");
            JsonElement spawn = reader.Section(r, "erased_spawn");
            reader.AllowOnly(spawn, "rift.erased_spawn", "max_open", "chance", "cooldown_s", "distance_px");
            (float spawnMin, float spawnMax) = reader.Range(spawn, "distance_px", 0f);
            RiftConfig rift = new()
            {
                SpriteOpen = reader.Resource(r, "sprite_open", exists),
                SpriteClosed = reader.Resource(r, "sprite_closed", exists),
                Placement = Bands(reader, r),
                MinSpacingPx = reader.Positive(r, "min_spacing_px"),
                HoldTime = reader.Positive(r, "hold_time"),
                Offers = reader.Count(r, "offers", 6),
                OfferMinRarity = reader.OneOf(r, "offer_min_rarity", rarityIds),
                PerilPerOffer = reader.Integer(r, "peril_per_offer", 0, 100),
                MaxOpen = reader.Integer(spawn, "max_open", 0, 100),
                SpawnChance = reader.Chance(spawn, "chance"),
                SpawnCooldown = reader.NonNegative(spawn, "cooldown_s"),
                SpawnDistanceMin = spawnMin,
                SpawnDistanceMax = spawnMax,
            };

            JsonElement w = reader.Section("workshop");
            reader.AllowOnly(w, "workshop", "sprite", "placement", "min_spacing_px", "services");
            JsonElement ws = reader.Section(w, "services");
            reader.AllowOnly(ws, "workshop.services", "weapon_cost", "weapon_min_rarity", "retemper_cost", "temper_upgrades", "cost_growth");
            WorkshopConfig workshop = new()
            {
                Sprite = reader.Resource(w, "sprite", exists),
                Placement = Bands(reader, w),
                MinSpacingPx = reader.Positive(w, "min_spacing_px"),
                WeaponCost = reader.Integer(ws, "weapon_cost", 0, 100_000),
                WeaponMinRarity = reader.OneOf(ws, "weapon_min_rarity", rarityIds),
                RetemperCost = reader.Integer(ws, "retemper_cost", 0, 100_000),
                TemperUpgrades = reader.Integer(ws, "temper_upgrades", 0, 100),
                CostGrowth = reader.NonNegative(ws, "cost_growth"),
            };
            if (reader.Error != null)
                return reader.Error;
            _memorial = memorial;
            _rift = rift;
            _workshop = workshop;
            _loadError = null;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }

    /// <summary>Groupes de placement : combien, et dans quelle couronne (fraction du rayon de carte, de 0 à 1).</summary>
    private static List<LandmarkBand> Bands(JsonConfigReader reader, JsonElement owner)
    {
        List<LandmarkBand> bands = new();
        foreach (JsonElement group in reader.List(owner, "placement", 1))
        {
            reader.AllowOnly(group, "placement", "count", "band");
            int count = reader.Integer(group, "count", 0, 1000);
            (float min, float max) = reader.Range(group, "band", 0f, 1f);
            bands.Add(new LandmarkBand(count, min, max));
        }
        return bands;
    }
}
