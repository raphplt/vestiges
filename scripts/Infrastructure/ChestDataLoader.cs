using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class ChestData
{
    public string Id;
    /// <summary>Clé de la palette de rareté (data/ui/rarities.json).</summary>
    public string Rarity;
    public string SpriteClosed;
    public string SpriteOpen;
    public float ColumnHeight;
    public float ColumnCore;
    public string FxFamily;
    public float OpenTime;
    public string LootTableId;
    public int LootRolls;
    public int ScorePoints;
}

/// <summary>Un groupe de coffres placés dans une bande de distance au départ (fraction du rayon de carte).</summary>
public readonly record struct ChestPlacementGroup(string ChestId, int Count, float BandMin, float BandMax);

/// <summary>Zone réservée autour d'un point, en pixels monde : côtés, nord (derrière) et sud (devant).</summary>
public readonly record struct PixelClearance(float Side, float North, float South);

/// <summary>Placement des coffres du monde (data/chests/chest_placement.json).</summary>
public class ChestPlacementData
{
    public float MinSpacingPx = 720f;
    public int CandidatesPerChest = 10;
    public int AttemptsPerChest = 80;
    public float PathBonus = 0.6f;
    public PixelClearance Clearance = new(72f, 32f, 124f);
    public PixelClearance BuildingClearance = new(176f, 48f, 176f);
    /// <summary>Repères de bord d'écran : coffres fermés hors du cadre à moins de cette distance du centre.</summary>
    public float PointerRangePx = 1100f;
    public int PointerMax = 3;
    public List<ChestPlacementGroup> Groups = new();
}

public static class ChestDataLoader
{
    private static readonly Dictionary<string, ChestData> _cache = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/chests/chests.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[ChestDataLoader] Cannot open data/chests/chests.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[ChestDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            if (!dict.ContainsKey("sprite_closed") || !dict.ContainsKey("sprite_open"))
            {
                GD.PushError($"[ChestDataLoader] {dict["id"]} : sprite_closed et sprite_open sont requis");
                continue;
            }
            ChestData data = new()
            {
                Id = dict["id"].AsString(),
                Rarity = dict.ContainsKey("rarity") ? dict["rarity"].AsString() : "common",
                SpriteClosed = dict["sprite_closed"].AsString(),
                SpriteOpen = dict["sprite_open"].AsString(),
                ColumnHeight = dict.ContainsKey("column_height") ? (float)dict["column_height"].AsDouble() : 96f,
                ColumnCore = dict.ContainsKey("column_core") ? (float)dict["column_core"].AsDouble() : 1f,
                FxFamily = dict.ContainsKey("fx_family") ? dict["fx_family"].AsString() : "silk",
                OpenTime = dict.ContainsKey("open_time") ? (float)dict["open_time"].AsDouble() : 0.5f,
                LootTableId = dict.ContainsKey("loot_table_id") ? dict["loot_table_id"].AsString() : "",
                LootRolls = dict.ContainsKey("loot_rolls") ? (int)dict["loot_rolls"].AsDouble() : 1,
                ScorePoints = dict.ContainsKey("score_points") ? (int)dict["score_points"].AsDouble() : 25
            };
            _cache[data.Id] = data;
        }

        _loaded = true;
        GD.Print($"[ChestDataLoader] Loaded {_cache.Count} chest definitions");
    }

    private static ChestPlacementData _placement;

    public static ChestPlacementData LoadPlacement()
    {
        if (_placement != null)
            return _placement;
        ChestPlacementData placement = _placement = new();
        using FileAccess file = FileAccess.Open("res://data/chests/chest_placement.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[ChestDataLoader] Cannot open data/chests/chest_placement.json");
            return placement;
        }

        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[ChestDataLoader] Placement parse error: {json.GetErrorMessage()}");
            return placement;
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        if (dict.ContainsKey("min_spacing_px"))
            placement.MinSpacingPx = (float)dict["min_spacing_px"].AsDouble();
        if (dict.ContainsKey("candidates_per_chest"))
            placement.CandidatesPerChest = (int)dict["candidates_per_chest"].AsDouble();
        if (dict.ContainsKey("attempts_per_chest"))
            placement.AttemptsPerChest = (int)dict["attempts_per_chest"].AsDouble();
        if (dict.ContainsKey("path_bonus"))
            placement.PathBonus = (float)dict["path_bonus"].AsDouble();
        if (dict.ContainsKey("clearance_px"))
            placement.Clearance = ParseClearance(dict["clearance_px"].AsGodotDictionary(), placement.Clearance);
        if (dict.ContainsKey("building_clearance_px"))
            placement.BuildingClearance = ParseClearance(dict["building_clearance_px"].AsGodotDictionary(), placement.BuildingClearance);

        if (dict.ContainsKey("edge_pointer"))
        {
            Godot.Collections.Dictionary pointer = dict["edge_pointer"].AsGodotDictionary();
            placement.PointerRangePx = (float)pointer["range_px"].AsDouble();
            placement.PointerMax = (int)pointer["max"].AsDouble();
        }

        foreach (Variant item in dict["groups"].AsGodotArray())
        {
            Godot.Collections.Dictionary group = item.AsGodotDictionary();
            Godot.Collections.Array band = group["band"].AsGodotArray();
            placement.Groups.Add(new ChestPlacementGroup(
                group["chest"].AsString(),
                (int)group["count"].AsDouble(),
                (float)band[0].AsDouble(),
                (float)band[1].AsDouble()));
        }
        return placement;
    }

    private static PixelClearance ParseClearance(Godot.Collections.Dictionary dict, PixelClearance fallback) => new(
        dict.ContainsKey("side") ? (float)dict["side"].AsDouble() : fallback.Side,
        dict.ContainsKey("north") ? (float)dict["north"].AsDouble() : fallback.North,
        dict.ContainsKey("south") ? (float)dict["south"].AsDouble() : fallback.South);

    public static ChestData Get(string id)
    {
        if (!_loaded)
            Load();

        if (_cache.TryGetValue(id, out ChestData data))
            return data;

        GD.PushWarning($"[ChestDataLoader] Unknown chest id: {id}");
        return null;
    }

    public static List<ChestData> GetAll()
    {
        if (!_loaded)
            Load();

        return new List<ChestData>(_cache.Values);
    }
}
