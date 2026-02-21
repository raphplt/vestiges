using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class EnemyStats
{
    public float Hp { get; set; }
    public float Speed { get; set; }
    public float Damage { get; set; }
    public float AttackRange { get; set; }
    public float XpReward { get; set; }
}

public class EnemyVisual
{
    public Color Color { get; set; }
    public string Shape { get; set; }
    public float Size { get; set; }
}

public class EnemyData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public EnemyStats Stats { get; set; }
    public EnemyVisual Visual { get; set; }
}

public static class EnemyDataLoader
{
    private static readonly Dictionary<string, EnemyData> _cache = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        string[] files = GetEnemyFiles();
        foreach (string path in files)
        {
            EnemyData data = ParseEnemyFile(path);
            if (data != null)
                _cache[data.Id] = data;
        }

        _loaded = true;
        GD.Print($"[EnemyDataLoader] Loaded {_cache.Count} enemy definitions");
    }

    public static EnemyData Get(string id)
    {
        if (!_loaded)
            Load();

        if (_cache.TryGetValue(id, out EnemyData data))
            return data;

        GD.PushError($"[EnemyDataLoader] Unknown enemy id: {id}");
        return null;
    }

    public static List<string> GetAllIds()
    {
        if (!_loaded)
            Load();

        return new List<string>(_cache.Keys);
    }

    private static string[] GetEnemyFiles()
    {
        List<string> files = new();
        DirAccess dir = DirAccess.Open("res://data/enemies/");
        if (dir == null)
        {
            GD.PushError("[EnemyDataLoader] Cannot open data/enemies/ directory");
            return files.ToArray();
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".json") && !fileName.StartsWith("_"))
                files.Add($"res://data/enemies/{fileName}");
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();
        return files.ToArray();
    }

    private static EnemyData ParseEnemyFile(string path)
    {
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[EnemyDataLoader] Cannot open {path}");
            return null;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        Error error = json.Parse(jsonText);
        if (error != Error.Ok)
        {
            GD.PushError($"[EnemyDataLoader] Parse error in {path}: {json.GetErrorMessage()}");
            return null;
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        Godot.Collections.Dictionary stats = dict["stats"].AsGodotDictionary();
        Godot.Collections.Dictionary visual = dict["visual"].AsGodotDictionary();

        return new EnemyData
        {
            Id = dict["id"].AsString(),
            Name = dict["name"].AsString(),
            Type = dict["type"].AsString(),
            Stats = new EnemyStats
            {
                Hp = (float)stats["hp"].AsDouble(),
                Speed = (float)stats["speed"].AsDouble(),
                Damage = (float)stats["damage"].AsDouble(),
                AttackRange = stats.ContainsKey("attack_range") ? (float)stats["attack_range"].AsDouble() : 0f,
                XpReward = (float)stats["xp_reward"].AsDouble()
            },
            Visual = new EnemyVisual
            {
                Color = Color.FromHtml(visual["color"].AsString()),
                Shape = visual["shape"].AsString(),
                Size = (float)visual["size"].AsDouble()
            }
        };
    }
}
