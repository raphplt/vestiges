using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

public class PoiData
{
    public string Id;
    public string Name;
    public string Type;
    public int Size;
    public int MinDistanceFromSpawn;
    public int MaxDistanceFromSpawn;
    public List<string> BiomeWhitelist = new();
    public List<string> BiomeBlacklist = new();
    public Color Color;
    public Color OutlineColor;
    public string Shape;
    public string InteractionType;
    public float SearchTime;
    public string LootTableId;
    public int LootRolls = 2;
    public List<string> EnemyGuards = new();
}

/// <summary>
/// Lieux à fouiller, garder ou activer (data/pois/pois.json), lus strictement une fois (plan 26 Q7g-2) : formes et
/// interactions que lit PointOfInterest, table de butin et gardiens existants. Un fichier refusé n'est pas publié et
/// arrête le chargement de la run (GameBootstrap). Le poids de tirage d'un lieu vit dans le biome (poi_pool).
/// </summary>
public static class PoiDataLoader
{
    public const string DataPath = "res://data/pois/pois.json";
    private static readonly string[] Shapes = { "building", "crate", "ruin", "chest_platform", "figure", "rift", "sanctuary" };
    private static readonly string[] Interactions = { "search", "guarded", "activate" };

    /// <summary>Catalogues que citent les lieux.</summary>
    public sealed record References(Func<string, bool> LootTableExists, Func<string, bool> EnemyExists);

    private static Dictionary<string, PoiData> _cache = new();
    private static bool _loaded;
    private static string _loadError;

    /// <summary>Le fichier lu et contrôlé ; faux, avec la raison, s'il a été refusé (la run ne démarre pas).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        _loadError = $"{DataPath} : lecture interrompue";
        using FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
        _loadError = file == null ? $"{DataPath} : fichier illisible"
            : Apply(file.GetAsText(), new References(LootTableLoader.Exists, EnemyDataLoader.Exists));
        if (_loadError != null)
        {
            GD.PushError($"[PoiDataLoader] {_loadError}");
            return;
        }
        GD.Print($"[PoiDataLoader] Loaded {_cache.Count} POI definitions");
    }

    /// <summary>Contrôle <paramref name="text"/> et ne publie qu'un fichier accepté ; rend l'erreur, nulle si tout est publié.</summary>
    public static string Apply(string text, References references)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                return "liste de lieux attendue à la racine";
            Dictionary<string, PoiData> pois = new();
            int index = 0;
            foreach (JsonElement entry in document.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    return $"lieu {index} : objet attendu";
                JsonConfigReader reader = new(entry);
                PoiData poi = Parse(reader, entry, references);
                if (reader.Error == null && pois.ContainsKey(poi.Id))
                    reader.Fail("identifiant déjà déclaré");
                if (reader.Error != null)
                    return $"lieu {(poi.Id != null ? $"« {poi.Id} »" : index.ToString())} : {reader.Error}";
                pois[poi.Id] = poi;
                index++;
            }
            _cache = pois;
            return null;
        }
        catch (JsonException exception)
        {
            return $"JSON invalide : {exception.Message}";
        }
    }

    public static PoiData Get(string id)
    {
        Load();
        if (id != null && _cache.TryGetValue(id, out PoiData data))
            return data;

        GD.PushWarning($"[PoiDataLoader] Unknown POI id: {id}");
        return null;
    }

    public static List<PoiData> GetAll()
    {
        Load();
        return new List<PoiData>(_cache.Values);
    }

    private static PoiData Parse(JsonConfigReader reader, JsonElement entry, References references)
    {
        reader.AllowOnly(entry, "lieu", "id", "name", "type", "size", "min_distance_from_spawn", "max_distance_from_spawn",
            "biome_whitelist", "biome_blacklist", "color", "outline_color", "shape", "interaction_type", "search_time",
            "loot_table_id", "loot_rolls", "enemy_guards");
        string id = reader.Text(entry, "id");
        Color color = HtmlColor(reader, entry, "color", "#888888");
        PoiData data = new()
        {
            Id = id,
            Name = JsonConfigReader.Has(entry, "name") ? reader.Text(entry, "name") : id,
            Type = JsonConfigReader.Has(entry, "type") ? reader.Text(entry, "type") : "searchable",
            Size = JsonConfigReader.Has(entry, "size") ? reader.Integer(entry, "size", 1, 20) : 2,
            MinDistanceFromSpawn = JsonConfigReader.Has(entry, "min_distance_from_spawn")
                ? reader.Integer(entry, "min_distance_from_spawn", 0, 10000) : 10,
            MaxDistanceFromSpawn = JsonConfigReader.Has(entry, "max_distance_from_spawn")
                ? reader.Integer(entry, "max_distance_from_spawn", 0, 10000) : 55,
            Color = color,
            OutlineColor = JsonConfigReader.Has(entry, "outline_color") ? HtmlColor(reader, entry, "outline_color", "#888888") : color,
            Shape = JsonConfigReader.Has(entry, "shape") ? reader.OneOf(entry, "shape", Shapes) : "building",
            InteractionType = JsonConfigReader.Has(entry, "interaction_type") ? reader.OneOf(entry, "interaction_type", Interactions) : "search",
            SearchTime = JsonConfigReader.Has(entry, "search_time") ? reader.NonNegative(entry, "search_time") : 1f,
            LootTableId = JsonConfigReader.Has(entry, "loot_table_id") ? reader.Text(entry, "loot_table_id") : "",
            LootRolls = JsonConfigReader.Has(entry, "loot_rolls") ? reader.Integer(entry, "loot_rolls", 0, 20) : 2,
            BiomeWhitelist = Ids(reader, entry, "biome_whitelist", "biome", _ => true, false),
            BiomeBlacklist = Ids(reader, entry, "biome_blacklist", "biome", _ => true, false),
            EnemyGuards = Ids(reader, entry, "enemy_guards", "créature", references.EnemyExists, true),
        };
        if (reader.Error == null && data.MinDistanceFromSpawn > data.MaxDistanceFromSpawn)
            reader.Fail($"min_distance_from_spawn ≤ max_distance_from_spawn attendu ({data.MinDistanceFromSpawn} > {data.MaxDistanceFromSpawn})");
        string both = data.BiomeWhitelist.Find(data.BiomeBlacklist.Contains);
        if (reader.Error == null && both != null)
            reader.Fail($"biome « {both} » à la fois autorisé et exclu");
        if (reader.Error == null && data.LootTableId.Length > 0 && !references.LootTableExists(data.LootTableId))
            reader.Fail($"loot_table_id : table « {data.LootTableId} » inconnue");
        return data;
    }

    /// <summary>
    /// Identifiants d'une liste facultative ; <paramref name="repeatsAllowed"/> pour les gardiens, dont chaque mention
    /// fait apparaître une créature.
    /// </summary>
    private static List<string> Ids(JsonConfigReader reader, JsonElement entry, string key, string what, Func<string, bool> exists,
        bool repeatsAllowed)
    {
        List<string> ids = new();
        if (!JsonConfigReader.Has(entry, key))
            return ids;
        foreach (JsonElement item in reader.List(entry, key, 0))
        {
            string id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (string.IsNullOrEmpty(id))
                reader.Fail($"{key} : texte non vide attendu");
            else if (!exists(id))
                reader.Fail($"{key} : {what} « {id} » inconnu{(what == "créature" ? "e" : "")}");
            else if (!repeatsAllowed && ids.Contains(id))
                reader.Fail($"{key} : {what} « {id} » en double");
            else
                ids.Add(id);
        }
        return ids;
    }

    private static Color HtmlColor(JsonConfigReader reader, JsonElement entry, string key, string fallback)
    {
        string html = JsonConfigReader.Has(entry, key) ? reader.Text(entry, key) : fallback;
        if (html != null && Color.HtmlIsValid(html))
            return Color.FromHtml(html);
        reader.Fail($"{key} : couleur « {html} » invalide");
        return Color.FromHtml(fallback);
    }
}
