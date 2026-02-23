using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class CharacterStats
{
    public float Speed { get; set; }
    public float AttackDamage { get; set; }
    public float AttackSpeed { get; set; }
    public float AttackRange { get; set; }
    public float MaxHp { get; set; }
    public float RegenRate { get; set; }
    public float InteractRange { get; set; }
}

public class CharacterData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public CharacterStats BaseStats { get; set; }
    public string PassivePerk { get; set; }
    public List<string> ExclusivePerks { get; set; } = new();
    public float ScoreMultiplier { get; set; } = 1f;
    public Color VisualColor { get; set; }
    public string UnlockCondition { get; set; }
}

public static class CharacterDataLoader
{
    private static readonly List<CharacterData> _allCharacters = new();
    private static readonly Dictionary<string, CharacterData> _byId = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/characters/characters.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[CharacterDataLoader] Cannot open characters.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[CharacterDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            Godot.Collections.Dictionary statsDict = dict["base_stats"].AsGodotDictionary();
            Godot.Collections.Array colorArr = dict["visual_color"].AsGodotArray();

            CharacterStats stats = new()
            {
                Speed = (float)statsDict["speed"].AsDouble(),
                AttackDamage = (float)statsDict["attack_damage"].AsDouble(),
                AttackSpeed = (float)statsDict["attack_speed"].AsDouble(),
                AttackRange = (float)statsDict["attack_range"].AsDouble(),
                MaxHp = (float)statsDict["max_hp"].AsDouble(),
                RegenRate = (float)statsDict["regen_rate"].AsDouble(),
                InteractRange = (float)statsDict["interact_range"].AsDouble()
            };

            List<string> exclusivePerks = new();
            Godot.Collections.Array perksArr = dict["exclusive_perks"].AsGodotArray();
            foreach (Variant perkId in perksArr)
                exclusivePerks.Add(perkId.AsString());

            CharacterData character = new()
            {
                Id = dict["id"].AsString(),
                Name = dict["name"].AsString(),
                Description = dict["description"].AsString(),
                BaseStats = stats,
                PassivePerk = dict["passive_perk"].AsString(),
                ExclusivePerks = exclusivePerks,
                ScoreMultiplier = (float)dict["score_multiplier"].AsDouble(),
                VisualColor = new Color(
                    (float)colorArr[0].AsDouble(),
                    (float)colorArr[1].AsDouble(),
                    (float)colorArr[2].AsDouble(),
                    (float)colorArr[3].AsDouble()
                ),
                UnlockCondition = dict["unlock_condition"].AsString()
            };

            _allCharacters.Add(character);
            _byId[character.Id] = character;
        }

        _loaded = true;
        GD.Print($"[CharacterDataLoader] Loaded {_allCharacters.Count} characters");
    }

    public static CharacterData Get(string id)
    {
        if (!_loaded)
            Load();

        return _byId.GetValueOrDefault(id);
    }

    public static List<CharacterData> GetAll()
    {
        if (!_loaded)
            Load();

        return _allCharacters;
    }
}
