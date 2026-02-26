using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class SouvenirData
{
    public string Id;
    public string Name;
    public string ConstellationId;
    public string Text;
    public string UnlockType;
    public string UnlockId;
}

public class ConstellationData
{
    public string Id;
    public string Name;
    public string Description;
    public Color Color;
    public int Order;
}

public static class SouvenirDataLoader
{
    private static readonly Dictionary<string, SouvenirData> _souvenirCache = new();
    private static readonly Dictionary<string, ConstellationData> _constellationCache = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        LoadConstellations();
        LoadSouvenirs();
        _loaded = true;

        GD.Print($"[SouvenirDataLoader] Loaded {_constellationCache.Count} constellations, {_souvenirCache.Count} souvenirs");
    }

    public static SouvenirData Get(string id)
    {
        if (!_loaded) Load();
        return _souvenirCache.TryGetValue(id, out SouvenirData data) ? data : null;
    }

    public static List<SouvenirData> GetAll()
    {
        if (!_loaded) Load();
        return new List<SouvenirData>(_souvenirCache.Values);
    }

    public static List<SouvenirData> GetByConstellation(string constellationId)
    {
        if (!_loaded) Load();
        List<SouvenirData> result = new();
        foreach (SouvenirData s in _souvenirCache.Values)
        {
            if (s.ConstellationId == constellationId)
                result.Add(s);
        }
        return result;
    }

    public static ConstellationData GetConstellation(string id)
    {
        if (!_loaded) Load();
        return _constellationCache.TryGetValue(id, out ConstellationData data) ? data : null;
    }

    public static List<ConstellationData> GetAllConstellations()
    {
        if (!_loaded) Load();
        List<ConstellationData> list = new(_constellationCache.Values);
        list.Sort((a, b) => a.Order.CompareTo(b.Order));
        return list;
    }

    private static void LoadConstellations()
    {
        FileAccess file = FileAccess.Open("res://data/souvenirs/constellations.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[SouvenirDataLoader] Cannot open constellations.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[SouvenirDataLoader] Parse error in constellations.json: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            ConstellationData data = new()
            {
                Id = dict["id"].AsString(),
                Name = dict["name"].AsString(),
                Description = dict["description"].AsString(),
                Color = new Color(dict["color"].AsString()),
                Order = (int)dict["order"].AsDouble()
            };
            _constellationCache[data.Id] = data;
        }
    }

    private static void LoadSouvenirs()
    {
        FileAccess file = FileAccess.Open("res://data/souvenirs/souvenirs.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[SouvenirDataLoader] Cannot open souvenirs.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[SouvenirDataLoader] Parse error in souvenirs.json: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            SouvenirData data = new()
            {
                Id = dict["id"].AsString(),
                Name = dict["name"].AsString(),
                ConstellationId = dict["constellation"].AsString(),
                Text = dict["text"].AsString(),
                UnlockType = dict.ContainsKey("unlock_type") ? dict["unlock_type"].AsString() : "",
                UnlockId = dict.ContainsKey("unlock_id") ? dict["unlock_id"].AsString() : ""
            };
            _souvenirCache[data.Id] = data;
        }
    }
}
