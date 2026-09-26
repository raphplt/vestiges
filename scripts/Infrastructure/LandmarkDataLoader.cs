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
}

/// <summary>Lieux du monde à trouver hors coffres (data/world/landmarks.json).</summary>
public static class LandmarkDataLoader
{
    private static MemorialConfig _memorial;

    public static MemorialConfig Memorial
    {
        get
        {
            if (_memorial == null)
                Load();
            return _memorial;
        }
    }

    private static void Load()
    {
        _memorial = new MemorialConfig();
        using FileAccess file = FileAccess.Open("res://data/world/landmarks.json", FileAccess.ModeFlags.Read);
        Json json = new();
        if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError("[LandmarkDataLoader] Cannot read data/world/landmarks.json");
            return;
        }

        Godot.Collections.Dictionary memorial = json.Data.AsGodotDictionary()["memorial"].AsGodotDictionary();
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
