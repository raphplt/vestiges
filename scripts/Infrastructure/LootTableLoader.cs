using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class LootEntry
{
    public string Type;
    public string Item;
    public float Weight;
    public int MinAmount;
    public int MaxAmount;
}

public class LootTableData
{
    public string Id;
    public List<LootEntry> Entries = new();
}

public static class LootTableLoader
{
    private static readonly Dictionary<string, LootTableData> _cache = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        string dirPath = "res://data/loot_tables";
        DirAccess dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            GD.PushError("[LootTableLoader] Cannot open data/loot_tables/");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (fileName.EndsWith(".json") && !fileName.StartsWith("_"))
                LoadFile($"{dirPath}/{fileName}");

            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        _loaded = true;
        GD.Print($"[LootTableLoader] Loaded {_cache.Count} loot tables");
    }

    public static LootTableData Get(string id)
    {
        if (!_loaded)
            Load();

        if (_cache.TryGetValue(id, out LootTableData data))
            return data;

        GD.PushWarning($"[LootTableLoader] Unknown loot table: {id}");
        return null;
    }

    private static void LoadFile(string path)
    {
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[LootTableLoader] Parse error in {path}: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        LootTableData table = new()
        {
            Id = dict["id"].AsString()
        };

        if (dict.ContainsKey("entries"))
        {
            Godot.Collections.Array entries = dict["entries"].AsGodotArray();
            foreach (Variant entryVar in entries)
            {
                Godot.Collections.Dictionary entryDict = entryVar.AsGodotDictionary();
                LootEntry entry = new()
                {
                    Type = entryDict["type"].AsString(),
                    Item = entryDict["item"].AsString(),
                    Weight = (float)entryDict["weight"].AsDouble(),
                    MinAmount = (int)entryDict["min_amount"].AsDouble(),
                    MaxAmount = (int)entryDict["max_amount"].AsDouble()
                };
                table.Entries.Add(entry);
            }
        }

        _cache[table.Id] = table;
    }
}
