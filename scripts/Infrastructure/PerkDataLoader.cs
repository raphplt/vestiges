using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class PerkEffect
{
    public string Stat { get; set; }
    public float Modifier { get; set; }
    public string ModifierType { get; set; }
}

public class ComplexEffect
{
    public string Trigger { get; set; }
    public string Action { get; set; }
    public float Value { get; set; }
    public float Chance { get; set; }
    public string Condition { get; set; }
    public float ConditionValue { get; set; }
    public string Stat { get; set; }
    public float Modifier { get; set; }
    public string ModifierType { get; set; }
    public float DotDamage { get; set; }
    public float DotDuration { get; set; }
    public float BounceRange { get; set; }
    public float Duration { get; set; }
    public int MaxBuffStacks { get; set; }
    public float HealPercent { get; set; }
    public int Uses { get; set; }
}

public class PerkData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Rarity { get; set; }
    public string Stat { get; set; }
    public float Modifier { get; set; }
    public string ModifierType { get; set; }
    public int MaxStacks { get; set; }
    public bool IsPassive { get; set; }
    public string CharacterId { get; set; }
    public float Weight { get; set; } = 1.0f;
    public List<string> Tags { get; set; }
    public List<PerkEffect> Effects { get; set; }
    public ComplexEffect Effect { get; set; }

    // Synergy-specific
    public string Type { get; set; }
    public List<string> RequiredPerks { get; set; }
    public string Notification { get; set; }
}

public static class PerkDataLoader
{
    private static readonly List<PerkData> _allPerks = new();
    private static readonly List<PerkData> _synergies = new();
    private static readonly Dictionary<string, PerkData> _byId = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/perks/perks.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[PerkDataLoader] Cannot open perks.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[PerkDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            if (item.VariantType == Variant.Type.String)
                continue;

            if (item.VariantType != Variant.Type.Dictionary)
                continue;

            Godot.Collections.Dictionary dict = item.AsGodotDictionary();

            List<PerkEffect> effects = null;
            if (dict.ContainsKey("effects"))
            {
                effects = new List<PerkEffect>();
                Godot.Collections.Array effectsArr = dict["effects"].AsGodotArray();
                foreach (Variant effectItem in effectsArr)
                {
                    Godot.Collections.Dictionary effectDict = effectItem.AsGodotDictionary();
                    effects.Add(new PerkEffect
                    {
                        Stat = effectDict.ContainsKey("stat") ? effectDict["stat"].AsString() : null,
                        Modifier = effectDict.ContainsKey("modifier") ? (float)effectDict["modifier"].AsDouble() : 0f,
                        ModifierType = effectDict.ContainsKey("modifier_type") ? effectDict["modifier_type"].AsString() : null
                    });
                }
            }

            ComplexEffect complexEffect = null;
            if (dict.ContainsKey("effect"))
            {
                Godot.Collections.Dictionary fxDict = dict["effect"].AsGodotDictionary();
                complexEffect = new ComplexEffect
                {
                    Trigger = fxDict.ContainsKey("trigger") ? fxDict["trigger"].AsString() : null,
                    Action = fxDict.ContainsKey("action") ? fxDict["action"].AsString() : null,
                    Value = fxDict.ContainsKey("value") ? (float)fxDict["value"].AsDouble() : 0f,
                    Chance = fxDict.ContainsKey("chance") ? (float)fxDict["chance"].AsDouble() : 0f,
                    Condition = fxDict.ContainsKey("condition") ? fxDict["condition"].AsString() : null,
                    ConditionValue = fxDict.ContainsKey("condition_value") ? (float)fxDict["condition_value"].AsDouble() : 0f,
                    Stat = fxDict.ContainsKey("stat") ? fxDict["stat"].AsString() : null,
                    Modifier = fxDict.ContainsKey("modifier") ? (float)fxDict["modifier"].AsDouble() : 0f,
                    ModifierType = fxDict.ContainsKey("modifier_type") ? fxDict["modifier_type"].AsString() : null,
                    DotDamage = fxDict.ContainsKey("dot_damage") ? (float)fxDict["dot_damage"].AsDouble() : 0f,
                    DotDuration = fxDict.ContainsKey("dot_duration") ? (float)fxDict["dot_duration"].AsDouble() : 0f,
                    BounceRange = fxDict.ContainsKey("bounce_range") ? (float)fxDict["bounce_range"].AsDouble() : 120f,
                    Duration = fxDict.ContainsKey("duration") ? (float)fxDict["duration"].AsDouble() : 0f,
                    MaxBuffStacks = fxDict.ContainsKey("max_buff_stacks") ? (int)fxDict["max_buff_stacks"].AsDouble() : 10,
                    HealPercent = fxDict.ContainsKey("heal_percent") ? (float)fxDict["heal_percent"].AsDouble() : 0f,
                    Uses = fxDict.ContainsKey("uses") ? (int)fxDict["uses"].AsDouble() : 0
                };
            }

            List<string> tags = null;
            if (dict.ContainsKey("tags"))
            {
                tags = new List<string>();
                Godot.Collections.Array tagsArr = dict["tags"].AsGodotArray();
                foreach (Variant tagItem in tagsArr)
                    tags.Add(tagItem.AsString());
            }

            List<string> requiredPerks = null;
            if (dict.ContainsKey("required_perks"))
            {
                requiredPerks = new List<string>();
                Godot.Collections.Array reqArr = dict["required_perks"].AsGodotArray();
                foreach (Variant reqItem in reqArr)
                    requiredPerks.Add(reqItem.AsString());
            }

            string type = dict.ContainsKey("type") ? dict["type"].AsString() : null;
            string rarity = dict.ContainsKey("rarity") ? dict["rarity"].AsString() : "common";

            float defaultWeight = rarity switch
            {
                "common" => 1.0f,
                "uncommon" => 0.5f,
                "rare" => 0.25f,
                "passive" => 0f,
                _ => 1.0f
            };

            PerkData perk = new()
            {
                Id = dict["id"].AsString(),
                Name = dict["name"].AsString(),
                Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
                Category = dict.ContainsKey("category") ? dict["category"].AsString() : null,
                Rarity = rarity,
                Stat = dict.ContainsKey("stat") ? dict["stat"].AsString() : null,
                Modifier = dict.ContainsKey("modifier") ? (float)dict["modifier"].AsDouble() : 0f,
                ModifierType = dict.ContainsKey("modifier_type") ? dict["modifier_type"].AsString() : null,
                MaxStacks = dict.ContainsKey("max_stacks") ? (int)dict["max_stacks"].AsDouble() : 1,
                IsPassive = dict.ContainsKey("is_passive") && dict["is_passive"].AsBool(),
                CharacterId = dict.ContainsKey("character_id") ? dict["character_id"].AsString() : null,
                Weight = dict.ContainsKey("weight") ? (float)dict["weight"].AsDouble() : defaultWeight,
                Tags = tags,
                Effects = effects,
                Effect = complexEffect,
                Type = type,
                RequiredPerks = requiredPerks,
                Notification = dict.ContainsKey("notification") ? dict["notification"].AsString() : null
            };

            if (type == "synergy")
            {
                _synergies.Add(perk);
            }
            else
            {
                _allPerks.Add(perk);
            }

            _byId[perk.Id] = perk;
        }

        _loaded = true;
        GD.Print($"[PerkDataLoader] Loaded {_allPerks.Count} perks + {_synergies.Count} synergies");
    }

    public static PerkData Get(string id)
    {
        if (!_loaded)
            Load();

        return _byId.GetValueOrDefault(id);
    }

    public static List<PerkData> GetAll()
    {
        if (!_loaded)
            Load();

        return _allPerks;
    }

    public static List<PerkData> GetSynergies()
    {
        if (!_loaded)
            Load();

        return _synergies;
    }
}
