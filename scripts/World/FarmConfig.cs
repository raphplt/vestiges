using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>Plan d'une ferme des Champs Sauvages (data/world/farms.json, plan 08 P4b).</summary>
public sealed class FarmConfig
{
    /// <summary>Un élément de la ferme : un décor tiré parmi <see cref="Sprites"/>, ou une rangée de décors de At à To.</summary>
    public sealed class Element
    {
        public string[] Sprites;
        public Vector2 At;
        public Vector2? To;
        public float Step;
        public bool Blocking;
        public float Chance = 1f;
    }

    public bool Enabled;
    public int SearchRadiusCells = 10;
    /// <summary>Demi-emprise de la ferme en pixels écran : toutes ses cellules doivent être des champs libres.</summary>
    public Vector2 HalfExtentPx = new(470f, 150f);
    /// <summary>Un chemin doit passer à moins de cette distance de la cour ; un embranchement l'y relie.</summary>
    public float PathReachPx = 360f;
    public Vector2 SpurStart = new(0f, 70f);
    public float SpurWidthFactor = 0.75f;
    public readonly List<Element> Elements = new();

    public static FarmConfig Load()
    {
        FarmConfig config = new();
        using FileAccess file = FileAccess.Open("res://data/world/farms.json", FileAccess.ModeFlags.Read);
        if (file == null)
            return config;
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[FarmConfig] farms.json invalide : {json.GetErrorMessage()}");
            return config;
        }

        Godot.Collections.Dictionary d = json.Data.AsGodotDictionary();
        config.Enabled = d.GetValueOrDefault("enabled", false).AsBool();
        config.SearchRadiusCells = (int)d.GetValueOrDefault("search_radius_cells", config.SearchRadiusCells).AsDouble();
        config.HalfExtentPx = ReadVector(d, "half_extent_px", config.HalfExtentPx);
        config.PathReachPx = (float)d.GetValueOrDefault("path_reach_px", config.PathReachPx).AsDouble();
        config.SpurStart = ReadVector(d, "spur_start", config.SpurStart);
        config.SpurWidthFactor = (float)d.GetValueOrDefault("spur_width_factor", config.SpurWidthFactor).AsDouble();
        if (d.ContainsKey("elements"))
        {
            foreach (Variant item in d["elements"].AsGodotArray())
            {
                Godot.Collections.Dictionary e = item.AsGodotDictionary();
                Element element = new()
                {
                    Sprites = e["sprites"].AsStringArray(),
                    At = ReadVector(e, "at", Vector2.Zero),
                    To = e.ContainsKey("to") ? ReadVector(e, "to", Vector2.Zero) : null,
                    Step = (float)e.GetValueOrDefault("step", 0f).AsDouble(),
                    Blocking = e.GetValueOrDefault("blocking", false).AsBool(),
                    Chance = (float)e.GetValueOrDefault("chance", 1f).AsDouble(),
                };
                if (element.Sprites.Length > 0)
                    config.Elements.Add(element);
            }
        }
        return config;
    }

    private static Vector2 ReadVector(Godot.Collections.Dictionary d, string key, Vector2 fallback)
    {
        if (!d.ContainsKey(key))
            return fallback;
        Godot.Collections.Array values = d[key].AsGodotArray();
        return values.Count >= 2 ? new Vector2((float)values[0].AsDouble(), (float)values[1].AsDouble()) : fallback;
    }
}
