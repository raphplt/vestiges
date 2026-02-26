using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class ChestData
{
    public string Id;
    public string Name;
    public string Rarity;
    public Color Color;
    public Color OutlineColor;
    public float Size;
    public float OpenTime;
    public string LootTableId;
    public int LootRolls;
    public int ScorePoints;
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
            string outlineHex = dict.ContainsKey("outline_color")
                ? dict["outline_color"].AsString()
                : dict["color"].AsString();

            ChestData data = new()
            {
                Id = dict["id"].AsString(),
                Name = dict.ContainsKey("name") ? dict["name"].AsString() : "",
                Rarity = dict.ContainsKey("rarity") ? dict["rarity"].AsString() : "common",
                Color = Color.FromHtml(dict["color"].AsString()),
                OutlineColor = Color.FromHtml(outlineHex),
                Size = dict.ContainsKey("size") ? (float)dict["size"].AsDouble() : 12f,
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
