using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class BiomeData
{
    public string Id;
    public string Name;
    public Dictionary<string, float> TerrainWeights = new();
    public List<string> DayEnemyPool = new();
    public List<string> NightEnemyPool = new();
    public Dictionary<string, float> ResourceBias = new();
    public string AmbientColorDay;
    public string AmbientColorDusk;
    public int DangerLevel;
}

public static class BiomeDataLoader
{
    private static readonly List<BiomeData> _allBiomes = new();
    private static readonly Dictionary<string, BiomeData> _byId = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        string dirPath = "res://data/biomes";
        DirAccess dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            GD.PushError("[BiomeDataLoader] Cannot open data/biomes/");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (!fileName.EndsWith(".json") || fileName.StartsWith("_"))
            {
                fileName = dir.GetNext();
                continue;
            }

            LoadBiomeFile($"{dirPath}/{fileName}");
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        _loaded = true;
        GD.Print($"[BiomeDataLoader] Loaded {_allBiomes.Count} biomes");
    }

    public static BiomeData Get(string id)
    {
        if (!_loaded)
            Load();

        return _byId.GetValueOrDefault(id);
    }

    public static List<BiomeData> GetAll()
    {
        if (!_loaded)
            Load();

        return _allBiomes;
    }

    private static void LoadBiomeFile(string path)
    {
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning($"[BiomeDataLoader] Cannot open {path}");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[BiomeDataLoader] Parse error in {path}: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        BiomeData biome = ParseBiome(dict);
        if (biome == null || string.IsNullOrEmpty(biome.Id))
            return;

        _allBiomes.Add(biome);
        _byId[biome.Id] = biome;
    }

    private static BiomeData ParseBiome(Godot.Collections.Dictionary dict)
    {
        BiomeData biome = new()
        {
            Id = dict["id"].AsString(),
            Name = dict.ContainsKey("name") ? dict["name"].AsString() : "",
            DangerLevel = dict.ContainsKey("danger_level") ? (int)dict["danger_level"].AsDouble() : 1,
            AmbientColorDay = dict.ContainsKey("ambient_color_day") ? dict["ambient_color_day"].AsString() : "#FFFFFF",
            AmbientColorDusk = dict.ContainsKey("ambient_color_dusk") ? dict["ambient_color_dusk"].AsString() : "#8888AA"
        };

        if (dict.ContainsKey("terrain_weights"))
        {
            Godot.Collections.Dictionary weights = dict["terrain_weights"].AsGodotDictionary();
            foreach (Variant key in weights.Keys)
                biome.TerrainWeights[key.AsString()] = (float)weights[key].AsDouble();
        }

        if (dict.ContainsKey("day_enemy_pool"))
        {
            Godot.Collections.Array pool = dict["day_enemy_pool"].AsGodotArray();
            foreach (Variant item in pool)
                biome.DayEnemyPool.Add(item.AsString());
        }

        if (dict.ContainsKey("night_enemy_pool"))
        {
            Godot.Collections.Array pool = dict["night_enemy_pool"].AsGodotArray();
            foreach (Variant item in pool)
                biome.NightEnemyPool.Add(item.AsString());
        }

        if (dict.ContainsKey("resource_bias"))
        {
            Godot.Collections.Dictionary bias = dict["resource_bias"].AsGodotDictionary();
            foreach (Variant key in bias.Keys)
                biome.ResourceBias[key.AsString()] = (float)bias[key].AsDouble();
        }

        return biome;
    }
}
