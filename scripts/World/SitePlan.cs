using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Plan d'un lieu composé (ferme des champs, chantier de la carrière) : où le chercher et quoi y poser. Positions en
/// pixels écran depuis le centre du lieu (y vers le bas, le nord en haut) ; un lieu par région du biome au plus,
/// retourné d'est en ouest une fois sur deux. Lu dans un JSON de data/world/ (plan 08 P4b et composition de la carrière).
/// </summary>
public sealed class SitePlan
{
    /// <summary>Un élément du lieu : un décor tiré parmi <see cref="Sprites"/>, ou une rangée de décors de At à To.</summary>
    public sealed class Element
    {
        public string[] Sprites;
        public Vector2 At;
        public Vector2? To;
        public float Step;
        public bool Blocking;
        public float Chance = 1f;
    }

    public string BiomeId = "";
    /// <summary>Dossier des décors du biome, avec la barre finale.</summary>
    public string Folder = "";
    /// <summary>Nom du lieu dans les journaux (« fermes », « chantiers »).</summary>
    public string LogName = "lieux";
    public bool Enabled;
    public int SearchRadiusCells = 10;
    /// <summary>Demi-emprise du lieu en pixels écran : toutes ses cellules doivent être libres dans le biome.</summary>
    public Vector2 HalfExtentPx = new(470f, 150f);
    /// <summary>Un chemin doit passer à moins de cette distance ; un embranchement l'y relie.</summary>
    public float PathReachPx = 360f;
    public Vector2 SpurStart = new(0f, 70f);
    public float SpurWidthFactor = 0.75f;
    public readonly List<Element> Elements = new();

    /// <summary>Plan seul dans son fichier (chantiers de la carrière) ; un plan désactivé si le fichier manque.</summary>
    public static SitePlan Load(string path, string biomeId, string folder, string logName)
    {
        Godot.Collections.Dictionary data = ReadJson(path);
        return data == null ? new SitePlan { BiomeId = biomeId, Folder = folder, LogName = logName } : Read(data, biomeId, folder, logName);
    }

    /// <summary>Clés communes à tous les plans : enabled, search_radius_cells, half_extent_px, path_reach_px, spur_*, elements.</summary>
    public static SitePlan Read(Godot.Collections.Dictionary d, string biomeId, string folder, string logName)
    {
        SitePlan plan = new() { BiomeId = biomeId, Folder = folder, LogName = logName };
        plan.Enabled = d.GetValueOrDefault("enabled", false).AsBool();
        plan.SearchRadiusCells = (int)d.GetValueOrDefault("search_radius_cells", plan.SearchRadiusCells).AsDouble();
        plan.HalfExtentPx = ReadVector(d, "half_extent_px", plan.HalfExtentPx);
        plan.PathReachPx = (float)d.GetValueOrDefault("path_reach_px", plan.PathReachPx).AsDouble();
        plan.SpurStart = ReadVector(d, "spur_start", plan.SpurStart);
        plan.SpurWidthFactor = (float)d.GetValueOrDefault("spur_width_factor", plan.SpurWidthFactor).AsDouble();
        if (!d.ContainsKey("elements"))
            return plan;
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
                plan.Elements.Add(element);
        }
        return plan;
    }

    public static Godot.Collections.Dictionary ReadJson(string path)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            return null;
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[SitePlan] {path} invalide : {json.GetErrorMessage()}");
            return null;
        }
        return json.Data.AsGodotDictionary();
    }

    public static Vector2 ReadVector(Godot.Collections.Dictionary d, string key, Vector2 fallback)
    {
        if (!d.ContainsKey(key))
            return fallback;
        Godot.Collections.Array values = d[key].AsGodotArray();
        return values.Count >= 2 ? new Vector2((float)values[0].AsDouble(), (float)values[1].AsDouble()) : fallback;
    }
}
