using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Plan d'un lieu composé (ferme des champs, chantier de la carrière, pièce d'une terre) : où le chercher et quoi y
/// poser. Positions en pixels écran depuis le centre du lieu (y vers le bas, le nord en haut), retourné d'est en ouest
/// une fois sur deux. Lu dans un JSON de data/world/ (plan 08 P4b, composition de la carrière, plan 58).
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
        /// <summary>Variantes d'une même place : de tous les éléments d'un groupe, un seul est posé par lieu.</summary>
        public string Group;
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

    /// <summary>Clés communes à tous les plans, à admettre par un fichier qui en porte d'autres (fermes).</summary>
    public static readonly string[] CommonKeys =
        { "enabled", "search_radius_cells", "half_extent_px", "path_reach_px", "spur_start", "spur_width_factor", "elements" };

    /// <summary>Plan seul dans son fichier (chantiers de la carrière), valide, ou une exception qui nomme le champ fautif.</summary>
    public static SitePlan Load(string path, string biomeId, string folder, string logName)
    {
        if (!TryLoad(path, biomeId, folder, logName, out SitePlan plan, out string error))
            throw new InvalidOperationException(error);
        return plan;
    }

    /// <summary>Plan seul dans son fichier, lu strictement (plan 26 Q7c-4c) : décors du dossier présents.</summary>
    public static bool TryLoad(string path, string biomeId, string folder, string logName, out SitePlan plan, out string error)
    {
        plan = null;
        if (!ReadText(path, out string text, out error))
            return false;
        if (!TryParse(text, biomeId, folder, logName, stem => ResourceLoader.Exists($"{folder}{stem}.png"), out plan, out error))
        {
            error = $"{path} : {error}";
            return false;
        }
        return true;
    }

    public static bool TryParse(string text, string biomeId, string folder, string logName, Func<string, bool> spriteExists,
        out SitePlan plan, out string error)
    {
        plan = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, logName, CommonKeys);
            SitePlan parsed = Read(reader, reader.Root, biomeId, folder, logName, spriteExists);
            error = reader.Error;
            if (error != null)
                return false;
            plan = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    /// <summary>
    /// Clés communes (<see cref="CommonKeys"/>) lues dans <paramref name="owner"/>, sans refuser les autres : le lecteur
    /// d'un fichier plus large (fermes) les admet lui-même. Un champ absent garde sa valeur d'avant.
    /// </summary>
    public static SitePlan Read(JsonConfigReader reader, JsonElement owner, string biomeId, string folder, string logName,
        Func<string, bool> spriteExists)
    {
        SitePlan plan = new() { BiomeId = biomeId, Folder = folder, LogName = logName };
        plan.Enabled = reader.Flag(owner, "enabled");
        if (JsonConfigReader.Has(owner, "search_radius_cells"))
            plan.SearchRadiusCells = reader.Integer(owner, "search_radius_cells", 1, 256);
        if (JsonConfigReader.Has(owner, "half_extent_px"))
        {
            plan.HalfExtentPx = Vector(reader, owner, "half_extent_px");
            if (reader.Error == null && (plan.HalfExtentPx.X <= 0f || plan.HalfExtentPx.Y <= 0f))
                reader.Fail("half_extent_px : deux demi-dimensions strictement positives attendues");
        }
        if (JsonConfigReader.Has(owner, "path_reach_px"))
            plan.PathReachPx = reader.NonNegative(owner, "path_reach_px");
        if (JsonConfigReader.Has(owner, "spur_start"))
            plan.SpurStart = Vector(reader, owner, "spur_start");
        if (JsonConfigReader.Has(owner, "spur_width_factor"))
            plan.SpurWidthFactor = reader.Positive(owner, "spur_width_factor");
        if (!JsonConfigReader.Has(owner, "elements"))
            return plan;
        foreach (JsonElement item in reader.List(owner, "elements", 0))
        {
            reader.AllowOnly(item, "elements", "sprites", "at", "to", "step", "blocking", "chance", "group");
            Element element = new()
            {
                Sprites = Sprites(reader, item, "sprites", 1, spriteExists).ToArray(),
                At = Vector(reader, item, "at"),
                Blocking = reader.Flag(item, "blocking"),
            };
            // Une rangée va de at à to, un décor tous les step pixels : l'un sans l'autre ne pose rien de plus.
            bool line = JsonConfigReader.Has(item, "to");
            if (line != JsonConfigReader.Has(item, "step"))
                reader.Fail("elements : to et step vont ensemble (rangée de at à to)");
            if (line)
            {
                element.To = Vector(reader, item, "to");
                element.Step = reader.Positive(item, "step");
            }
            if (JsonConfigReader.Has(item, "chance"))
                element.Chance = reader.Chance(item, "chance");
            if (JsonConfigReader.Has(item, "group"))
                element.Group = reader.Text(item, "group");
            plan.Elements.Add(element);
        }
        return plan;
    }

    /// <summary>Liste de noms de décors du dossier, présents, d'au moins <paramref name="minCount"/> éléments.</summary>
    public static List<string> Sprites(JsonConfigReader reader, JsonElement owner, string key, int minCount,
        Func<string, bool> spriteExists)
    {
        List<string> sprites = new();
        foreach (JsonElement item in reader.List(owner, key, minCount))
        {
            string stem = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (string.IsNullOrEmpty(stem) || stem.Contains('/') || stem.Contains('.') || !spriteExists(stem))
                reader.Fail($"{key} : décor « {stem} » introuvable");
            else
                sprites.Add(stem);
        }
        return sprites;
    }

    /// <summary>Texte d'un fichier de data/world/, ou un diagnostic.</summary>
    public static bool ReadText(string path, out string text, out string error)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        text = file?.GetAsText();
        error = file == null ? $"{path} : fichier illisible" : null;
        return file != null;
    }

    /// <summary>Position [x, y] en pixels écran : deux nombres finis.</summary>
    public static Vector2 Vector(JsonConfigReader reader, JsonElement owner, string key)
    {
        List<JsonElement> values = reader.List(owner, key, 2);
        if (reader.Error != null)
            return Vector2.Zero;
        if (values.Count != 2 || values[0].ValueKind != JsonValueKind.Number || values[1].ValueKind != JsonValueKind.Number
            || !float.IsFinite((float)values[0].GetDouble()) || !float.IsFinite((float)values[1].GetDouble()))
        {
            reader.Fail($"{key} : position [x, y] attendue");
            return Vector2.Zero;
        }
        return new Vector2((float)values[0].GetDouble(), (float)values[1].GetDouble());
    }
}
