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
    public string SpriteFolder { get; set; }
    /// <summary>Distance du centre du cadre aux pieds, en pixels. Non nul : sprite à la densité du monde, sans mise à l'échelle.</summary>
    public float SpriteFeetOffset { get; set; }
}

/// <summary>Paramètres d'une capacité composée (bloc "abilities" du JSON ennemi).</summary>
public class EnemyAbilityData
{
    public Dictionary<string, float> Numbers { get; } = new();
    public Dictionary<string, string> Texts { get; } = new();

    public float GetNumber(string key, float fallback) => Numbers.TryGetValue(key, out float value) ? value : fallback;
    public string GetText(string key, string fallback) => Texts.TryGetValue(key, out string value) ? value : fallback;
}

public class EnemyData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Behavior { get; set; } = "default";
    public string Tier { get; set; } = "normal";
    /// <summary>Accord des titres de variante (« Tisseuse Enragée »).</summary>
    public bool IsFeminine { get; set; }
    public EnemyStats Stats { get; set; }
    public EnemyVisual Visual { get; set; }
    public Dictionary<string, float> ExtraStats { get; set; } = new();
    public Dictionary<string, EnemyAbilityData> Abilities { get; set; } = new();
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

        EnemyData data = new()
        {
            Id = dict["id"].AsString(),
            Name = dict["name"].AsString(),
            Type = dict["type"].AsString(),
            Behavior = dict.ContainsKey("behavior") ? dict["behavior"].AsString() : "default",
            Tier = dict.ContainsKey("tier") ? dict["tier"].AsString() : "normal",
            IsFeminine = dict.ContainsKey("grammatical_gender") && dict["grammatical_gender"].AsString() == "f",
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
                Size = (float)visual["size"].AsDouble(),
                SpriteFolder = visual.ContainsKey("sprite_folder") ? visual["sprite_folder"].AsString() : null,
                SpriteFeetOffset = visual.ContainsKey("sprite_feet_offset") ? (float)visual["sprite_feet_offset"].AsDouble() : 0f
            }
        };

        // Extra stats non-standard (pack_bonus, charge_speed, etc.)
        string[] coreStats = { "hp", "speed", "damage", "attack_range", "xp_reward" };
        HashSet<string> coreSet = new(coreStats);
        foreach (Variant key in stats.Keys)
        {
            string k = key.AsString();
            if (!coreSet.Contains(k))
            {
                Variant val = stats[key];
                if (val.VariantType is Variant.Type.Int or Variant.Type.Float)
                    data.ExtraStats[k] = (float)val.AsDouble();
            }
        }

        if (dict.ContainsKey("abilities"))
            ParseAbilities(dict["abilities"].AsGodotDictionary(), data);

        return data;
    }

    private static void ParseAbilities(Godot.Collections.Dictionary abilities, EnemyData data)
    {
        foreach (Variant abilityKey in abilities.Keys)
        {
            EnemyAbilityData ability = new();
            Godot.Collections.Dictionary parameters = abilities[abilityKey].AsGodotDictionary();
            foreach (Variant paramKey in parameters.Keys)
            {
                Variant value = parameters[paramKey];
                if (value.VariantType is Variant.Type.Int or Variant.Type.Float)
                    ability.Numbers[paramKey.AsString()] = (float)value.AsDouble();
                else if (value.VariantType == Variant.Type.String)
                    ability.Texts[paramKey.AsString()] = value.AsString();
            }
            data.Abilities[abilityKey.AsString()] = ability;
        }
    }
}
