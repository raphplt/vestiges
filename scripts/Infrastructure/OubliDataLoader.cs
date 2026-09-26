using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Oubli d'une Faille : un malus sur la carte, nommé par l'effet qu'il nourrit.</summary>
public class OubliData
{
    public string Id;
    public string NameKey;
    public string DescriptionKey;
    public string Effect;
    public float Amount;
    /// <summary>Effet immédiat et définitif : un Mémorial ne peut pas le lever.</summary>
    public bool Permanent;

    /// <summary>« Les zones que tu quittes s'effacent 20 % plus vite. »</summary>
    public string Describe() =>
        string.Format(TranslationServer.Translate(DescriptionKey), Mathf.RoundToInt(Mathf.Abs(Amount) < 1f ? Amount * 100f : Amount));
}

/// <summary>Oublis des Failles (data/progression/oublis.json).</summary>
public static class OubliDataLoader
{
    private static readonly List<OubliData> _oublis = new();
    private static bool _loaded;

    public static IReadOnlyList<OubliData> All
    {
        get
        {
            if (!_loaded)
                Load();
            return _oublis;
        }
    }

    private static void Load()
    {
        _loaded = true;
        using FileAccess file = FileAccess.Open("res://data/progression/oublis.json", FileAccess.ModeFlags.Read);
        Json json = new();
        if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError("[OubliDataLoader] Cannot read data/progression/oublis.json");
            return;
        }

        foreach (Variant item in json.Data.AsGodotDictionary()["oublis"].AsGodotArray())
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            _oublis.Add(new OubliData
            {
                Id = dict["id"].AsString(),
                NameKey = dict["name_key"].AsString(),
                DescriptionKey = dict["description_key"].AsString(),
                Effect = dict["effect"].AsString(),
                Amount = (float)dict["amount"].AsDouble(),
                Permanent = dict.ContainsKey("permanent") && dict["permanent"].AsBool(),
            });
        }
    }
}
