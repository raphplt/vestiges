using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class ResourceData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Color Color { get; set; }
    public Color OutlineColor { get; set; }
    public float HarvestTime { get; set; }
    public int AmountMin { get; set; }
    public int AmountMax { get; set; }
    public float Size { get; set; }
    public string Shape { get; set; }
    public int Harvests { get; set; }
}

public static class ResourceDataLoader
{
    private static readonly Dictionary<string, ResourceData> _cache = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/resources/resources.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[ResourceDataLoader] Cannot open data/resources/resources.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        Error error = json.Parse(jsonText);
        if (error != Error.Ok)
        {
            GD.PushError($"[ResourceDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            string outlineHex = dict.ContainsKey("outline_color")
                ? dict["outline_color"].AsString()
                : dict["color"].AsString();

            ResourceData data = new()
            {
                Id = dict["id"].AsString(),
                Name = dict["name"].AsString(),
                Color = Color.FromHtml(dict["color"].AsString()),
                OutlineColor = Color.FromHtml(outlineHex),
                HarvestTime = (float)dict["harvest_time"].AsDouble(),
                AmountMin = (int)dict["amount_min"].AsDouble(),
                AmountMax = (int)dict["amount_max"].AsDouble(),
                Size = (float)dict["size"].AsDouble(),
                Shape = dict["shape"].AsString(),
                Harvests = (int)dict["harvests"].AsDouble()
            };
            _cache[data.Id] = data;
        }

        _loaded = true;
        GD.Print($"[ResourceDataLoader] Loaded {_cache.Count} resource definitions");
    }

    public static ResourceData Get(string id)
    {
        if (!_loaded)
            Load();

        if (_cache.TryGetValue(id, out ResourceData data))
            return data;

        GD.PushError($"[ResourceDataLoader] Unknown resource id: {id}");
        return null;
    }

    public static List<ResourceData> GetAll()
    {
        if (!_loaded)
            Load();

        return new List<ResourceData>(_cache.Values);
    }

    public static List<string> GetAllIds()
    {
        if (!_loaded)
            Load();

        return new List<string>(_cache.Keys);
    }
}
