using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class PerkData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Stat { get; set; }
    public float Modifier { get; set; }
    public string ModifierType { get; set; }
    public int MaxStacks { get; set; }
}

public static class PerkDataLoader
{
    private static readonly List<PerkData> _allPerks = new();
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
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            PerkData perk = new()
            {
                Id = dict["id"].AsString(),
                Name = dict["name"].AsString(),
                Description = dict["description"].AsString(),
                Stat = dict["stat"].AsString(),
                Modifier = (float)dict["modifier"].AsDouble(),
                ModifierType = dict["modifier_type"].AsString(),
                MaxStacks = (int)dict["max_stacks"].AsDouble()
            };
            _allPerks.Add(perk);
            _byId[perk.Id] = perk;
        }

        _loaded = true;
        GD.Print($"[PerkDataLoader] Loaded {_allPerks.Count} perks");
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
}
