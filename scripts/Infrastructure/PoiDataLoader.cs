using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class PoiData
{
    public string Id;
    public string Name;
    public string Type;
    public int Size;
    public int MinDistanceFromFoyer;
    public int MaxDistanceFromFoyer;
    public List<string> BiomeWhitelist = new();
    public List<string> BiomeBlacklist = new();
    public float SpawnWeight;
    public Color Color;
    public Color OutlineColor;
    public string Shape;
    public string InteractionType;
    public float SearchTime;
    public string LootTableId;
    public List<string> EnemyGuards = new();
    public int ScorePoints;
}

public static class PoiDataLoader
{
    private static readonly Dictionary<string, PoiData> _cache = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/pois/pois.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[PoiDataLoader] Cannot open data/pois/pois.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[PoiDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            PoiData data = ParsePoi(dict);
            if (data != null)
                _cache[data.Id] = data;
        }

        _loaded = true;
        GD.Print($"[PoiDataLoader] Loaded {_cache.Count} POI definitions");
    }

    public static PoiData Get(string id)
    {
        if (!_loaded)
            Load();

        if (_cache.TryGetValue(id, out PoiData data))
            return data;

        GD.PushWarning($"[PoiDataLoader] Unknown POI id: {id}");
        return null;
    }

    public static List<PoiData> GetAll()
    {
        if (!_loaded)
            Load();

        return new List<PoiData>(_cache.Values);
    }

    private static PoiData ParsePoi(Godot.Collections.Dictionary dict)
    {
        string id = dict["id"].AsString();
        string colorHex = dict.ContainsKey("color") ? dict["color"].AsString() : "#888888";
        string outlineHex = dict.ContainsKey("outline_color") ? dict["outline_color"].AsString() : colorHex;

        PoiData data = new()
        {
            Id = id,
            Name = dict.ContainsKey("name") ? dict["name"].AsString() : id,
            Type = dict.ContainsKey("type") ? dict["type"].AsString() : "searchable",
            Size = dict.ContainsKey("size") ? (int)dict["size"].AsDouble() : 2,
            MinDistanceFromFoyer = dict.ContainsKey("min_distance_from_foyer") ? (int)dict["min_distance_from_foyer"].AsDouble() : 10,
            MaxDistanceFromFoyer = dict.ContainsKey("max_distance_from_foyer") ? (int)dict["max_distance_from_foyer"].AsDouble() : 55,
            SpawnWeight = dict.ContainsKey("spawn_weight") ? (float)dict["spawn_weight"].AsDouble() : 10f,
            Color = Color.FromHtml(colorHex),
            OutlineColor = Color.FromHtml(outlineHex),
            Shape = dict.ContainsKey("shape") ? dict["shape"].AsString() : "building",
            InteractionType = dict.ContainsKey("interaction_type") ? dict["interaction_type"].AsString() : "search",
            SearchTime = dict.ContainsKey("search_time") ? (float)dict["search_time"].AsDouble() : 1f,
            LootTableId = dict.ContainsKey("loot_table_id") ? dict["loot_table_id"].AsString() : "",
            ScorePoints = dict.ContainsKey("score_points") ? (int)dict["score_points"].AsDouble() : 50
        };

        if (dict.ContainsKey("biome_whitelist"))
        {
            Godot.Collections.Array list = dict["biome_whitelist"].AsGodotArray();
            foreach (Variant v in list)
                data.BiomeWhitelist.Add(v.AsString());
        }

        if (dict.ContainsKey("biome_blacklist"))
        {
            Godot.Collections.Array list = dict["biome_blacklist"].AsGodotArray();
            foreach (Variant v in list)
                data.BiomeBlacklist.Add(v.AsString());
        }

        if (dict.ContainsKey("enemy_guards"))
        {
            Godot.Collections.Array guards = dict["enemy_guards"].AsGodotArray();
            foreach (Variant v in guards)
                data.EnemyGuards.Add(v.AsString());
        }

        return data;
    }
}
