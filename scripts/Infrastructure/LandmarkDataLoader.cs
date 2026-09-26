using System.Collections.Generic;
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
    public float MinSpacingPx = 900f;
    public float HoldTime = 0.6f;
    public int Shards = 3;
    public float ShardDistanceMin = 200f;
    public float ShardDistanceMax = 380f;
    public float ShardTime = 20f;
    public float ShardPickupPx = 30f;
    public int BlessingChoices = 3;
    public string BlessingMinRarity = "uncommon";
    public int WeaponCost = 30;
    public string WeaponMinRarity = "rare";
    public int HealCost = 20;
    public float HealPercent = 0.4f;
    public float CostGrowth = 0.5f;
    public int LiftOubliCost = 40;
}

/// <summary>Réglages des Failles (section <c>rift</c> de data/world/landmarks.json).</summary>
public class RiftConfig
{
    public string SpriteOpen;
    public string SpriteClosed;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx = 700f;
    public float HoldTime = 0.8f;
    public int Offers = 3;
    public string OfferMinRarity = "epic";
    public int PerilPerOffer = 1;
    public int MaxOpen = 6;
    public float SpawnChance = 0.08f;
    public float SpawnCooldown = 45f;
    public float SpawnDistanceMin = 500f;
    public float SpawnDistanceMax = 1400f;
}

/// <summary>Lieux du monde à trouver hors coffres : Mémoriaux et Failles (data/world/landmarks.json).</summary>
public static class LandmarkDataLoader
{
    private static MemorialConfig _memorial;
    private static RiftConfig _rift;

    public static MemorialConfig Memorial
    {
        get
        {
            if (_memorial == null)
                Load();
            return _memorial;
        }
    }

    public static RiftConfig Rift
    {
        get
        {
            if (_rift == null)
                Load();
            return _rift;
        }
    }

    private static void Load()
    {
        _memorial = new MemorialConfig();
        _rift = new RiftConfig();
        using FileAccess file = FileAccess.Open("res://data/world/landmarks.json", FileAccess.ModeFlags.Read);
        Json json = new();
        if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError("[LandmarkDataLoader] Cannot read data/world/landmarks.json");
            return;
        }

        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        Godot.Collections.Dictionary memorial = root["memorial"].AsGodotDictionary();
        MemorialConfig c = _memorial;
        c.SpriteDormant = memorial["sprite_dormant"].AsString();
        c.SpriteAwake = memorial["sprite_awake"].AsString();
        c.SpriteShard = memorial["sprite_shard"].AsString();
        c.Placement = ReadBands(memorial["placement"].AsGodotArray());
        c.MinSpacingPx = Float(memorial, "min_spacing_px", c.MinSpacingPx);
        c.HoldTime = Float(memorial, "hold_time", c.HoldTime);
        c.Shards = (int)Float(memorial, "shards", c.Shards);
        if (memorial.ContainsKey("shard_distance_px"))
        {
            Godot.Collections.Array shardDistance = memorial["shard_distance_px"].AsGodotArray();
            c.ShardDistanceMin = (float)shardDistance[0].AsDouble();
            c.ShardDistanceMax = (float)shardDistance[1].AsDouble();
        }
        c.ShardTime = Float(memorial, "shard_time", c.ShardTime);
        c.ShardPickupPx = Float(memorial, "shard_pickup_px", c.ShardPickupPx);
        c.BlessingChoices = (int)Float(memorial, "blessing_choices", c.BlessingChoices);
        c.BlessingMinRarity = memorial.ContainsKey("blessing_min_rarity") ? memorial["blessing_min_rarity"].AsString() : c.BlessingMinRarity;
        Godot.Collections.Dictionary services = memorial.ContainsKey("services") ? memorial["services"].AsGodotDictionary() : new();
        c.WeaponCost = (int)Float(services, "weapon_cost", c.WeaponCost);
        c.WeaponMinRarity = services.ContainsKey("weapon_min_rarity") ? services["weapon_min_rarity"].AsString() : c.WeaponMinRarity;
        c.HealCost = (int)Float(services, "heal_cost", c.HealCost);
        c.HealPercent = Float(services, "heal_percent", c.HealPercent);
        c.CostGrowth = Float(services, "cost_growth", c.CostGrowth);
        c.LiftOubliCost = (int)Float(services, "lift_oubli_cost", c.LiftOubliCost);

        Godot.Collections.Dictionary rift = root["rift"].AsGodotDictionary();
        RiftConfig r = _rift;
        r.SpriteOpen = rift["sprite_open"].AsString();
        r.SpriteClosed = rift["sprite_closed"].AsString();
        r.Placement = ReadBands(rift["placement"].AsGodotArray());
        r.MinSpacingPx = Float(rift, "min_spacing_px", r.MinSpacingPx);
        r.HoldTime = Float(rift, "hold_time", r.HoldTime);
        r.Offers = (int)Float(rift, "offers", r.Offers);
        r.OfferMinRarity = rift.ContainsKey("offer_min_rarity") ? rift["offer_min_rarity"].AsString() : r.OfferMinRarity;
        r.PerilPerOffer = (int)Float(rift, "peril_per_offer", r.PerilPerOffer);
        Godot.Collections.Dictionary spawn = rift.ContainsKey("erased_spawn") ? rift["erased_spawn"].AsGodotDictionary() : new();
        r.MaxOpen = (int)Float(spawn, "max_open", r.MaxOpen);
        r.SpawnChance = Float(spawn, "chance", r.SpawnChance);
        r.SpawnCooldown = Float(spawn, "cooldown_s", r.SpawnCooldown);
        if (spawn.ContainsKey("distance_px"))
        {
            Godot.Collections.Array distance = spawn["distance_px"].AsGodotArray();
            r.SpawnDistanceMin = (float)distance[0].AsDouble();
            r.SpawnDistanceMax = (float)distance[1].AsDouble();
        }
    }

    /// <summary>Réglage optionnel : la valeur par défaut du modèle quand la clé manque.</summary>
    private static float Float(Godot.Collections.Dictionary dict, string key, float fallback) =>
        dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;

    private static List<LandmarkBand> ReadBands(Godot.Collections.Array groups)
    {
        List<LandmarkBand> bands = new();
        foreach (Variant item in groups)
        {
            Godot.Collections.Dictionary group = item.AsGodotDictionary();
            Godot.Collections.Array band = group["band"].AsGodotArray();
            bands.Add(new LandmarkBand((int)group["count"].AsDouble(), (float)band[0].AsDouble(), (float)band[1].AsDouble()));
        }
        return bands;
    }
}
