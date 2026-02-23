using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class WeaponData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Tier { get; set; }
    public string Type { get; set; }
    public string DamageType { get; set; }
    public string AttackPattern { get; set; }
    public string DefaultFor { get; set; }
    public Dictionary<string, float> Stats { get; set; } = new();
}

public static class WeaponDataLoader
{
    private static readonly List<WeaponData> _allWeapons = new();
    private static readonly Dictionary<string, WeaponData> _byId = new();
    private static readonly Dictionary<string, WeaponData> _defaultByCharacter = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/weapons/weapons.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[WeaponDataLoader] Cannot open weapons.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[WeaponDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;

            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            if (!dict.ContainsKey("id"))
                continue;

            WeaponData weapon = ParseWeapon(dict);
            if (weapon == null || string.IsNullOrEmpty(weapon.Id))
                continue;

            _allWeapons.Add(weapon);
            _byId[weapon.Id] = weapon;

            if (!string.IsNullOrEmpty(weapon.DefaultFor))
                _defaultByCharacter[weapon.DefaultFor] = weapon;
        }

        _loaded = true;
        GD.Print($"[WeaponDataLoader] Loaded {_allWeapons.Count} weapons");
    }

    public static WeaponData Get(string id)
    {
        if (!_loaded)
            Load();

        if (string.IsNullOrEmpty(id))
            return null;

        return _byId.GetValueOrDefault(id);
    }

    public static WeaponData GetDefaultForCharacter(string characterId)
    {
        if (!_loaded)
            Load();

        if (string.IsNullOrEmpty(characterId))
            return null;

        return _defaultByCharacter.GetValueOrDefault(characterId);
    }

    public static List<WeaponData> GetAll()
    {
        if (!_loaded)
            Load();

        return _allWeapons;
    }

    private static WeaponData ParseWeapon(Godot.Collections.Dictionary dict)
    {
        WeaponData weapon = new()
        {
            Id = dict["id"].AsString(),
            Name = dict.ContainsKey("name") ? dict["name"].AsString() : "",
            Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
            Tier = dict.ContainsKey("tier") ? (int)dict["tier"].AsDouble() : 1,
            Type = dict.ContainsKey("type") ? dict["type"].AsString() : "ranged",
            DamageType = dict.ContainsKey("damage_type") ? dict["damage_type"].AsString() : "physical",
            AttackPattern = dict.ContainsKey("attack_pattern") ? dict["attack_pattern"].AsString() : "linear",
            DefaultFor = dict.ContainsKey("default_for") ? dict["default_for"].AsString() : null
        };

        if (dict.ContainsKey("stats"))
        {
            Godot.Collections.Dictionary statsDict = dict["stats"].AsGodotDictionary();
            foreach (Variant key in statsDict.Keys)
            {
                string statKey = key.AsString();
                Variant value = statsDict[key];
                if (value.VariantType is Variant.Type.Int or Variant.Type.Float)
                    weapon.Stats[statKey] = (float)value.AsDouble();
            }
        }

        return weapon;
    }
}
