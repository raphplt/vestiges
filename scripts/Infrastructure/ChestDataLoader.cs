using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

public class ChestData
{
    public string Id;
    /// <summary>Clé de la palette de rareté (data/ui/rarities.json).</summary>
    public string Rarity;
    public string SpriteClosed;
    public string SpriteOpen;
    public float ColumnHeight;
    public float ColumnCore;
    public string FxFamily;
    /// <summary>Distance d'ouverture (px) : il faut être collé au coffre, qui s'ouvre à l'appui, sans jauge.</summary>
    public float OpenRangePx;
    public string LootTableId;
    public int LootRolls;
    /// <summary>Coffre d'un rang de rareté en dessous (Oubli du trésor), ou rien.</summary>
    public string DowngradeTo;
}

/// <summary>Un groupe de coffres placés dans une bande de distance au départ (fraction du rayon de carte).</summary>
public readonly record struct ChestPlacementGroup(string ChestId, int Count, float BandMin, float BandMax);

/// <summary>Zone réservée autour d'un point, en pixels monde : côtés, nord (derrière) et sud (devant).</summary>
public readonly record struct PixelClearance(float Side, float North, float South);

/// <summary>Placement des coffres du monde (data/chests/chest_placement.json).</summary>
public class ChestPlacementData
{
    public float MinSpacingPx;
    public int CandidatesPerChest;
    public int AttemptsPerChest;
    public float PathBonus;
    public PixelClearance Clearance;
    public PixelClearance BuildingClearance;
    /// <summary>Repères de bord d'écran : coffres fermés hors du cadre à moins de cette distance du centre.</summary>
    public float PointerRangePx;
    public int PointerMax;
    public List<ChestPlacementGroup> Groups = new();
}

/// <summary>Coffres et placement, contrôlés ensemble avant publication des deux catalogues.</summary>
public static class ChestDataLoader
{
    private const string ChestsPath = "res://data/chests/chests.json";
    private const string PlacementPath = "res://data/chests/chest_placement.json";

    private static readonly Dictionary<string, ChestData> _cache = new();
    private static ChestPlacementData _placement = new();
    private static bool _loaded;
    private static string _loadError;

    /// <summary>Les deux fichiers lus et contrôlés ; faux, avec la raison, s'ils ont été refusés (la run ne démarre pas).</summary>
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
        string error = Apply(Read(ChestsPath), Read(PlacementPath), new References(
            path => ResourceLoader.Exists(path),
            DataKeySets.ListIds("res://data/ui/rarities.json", "rarities"),
            DataKeySets.StringList("res://data/enemies/_contract.json", "families"),
            LootTableLoader.Exists));
        if (error != null)
        {
            _loadError = error;
            GD.PushError($"[ChestDataLoader] {error}");
            return;
        }
        GD.Print($"[ChestDataLoader] Loaded {_cache.Count} chest definitions");
    }

    private static string Read(string path) => FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : null;

    /// <summary>Références qu'un coffre peut nommer : images, raretés de la palette, familles d'effets, tables de butin.</summary>
    public sealed record References(Func<string, bool> ResourceExists, IReadOnlyCollection<string> Rarities,
        IReadOnlyCollection<string> Families, Func<string, bool> LootTableExists);

    /// <summary>Contrôle les deux textes et ne les publie que s'ils sont tous valides. Rend l'erreur, ou null.</summary>
    public static string Apply(string chestsJson, string placementJson, References refs)
    {
        if (chestsJson == null || placementJson == null)
            return "un des fichiers de coffres est absent";
        try
        {
            Dictionary<string, ChestData> chests = new();
            string error = ParseChests(chestsJson, refs, chests);
            if (error != null)
                return $"{ChestsPath} : {error}";
            ChestPlacementData placement = new();
            error = ParsePlacement(placementJson, chests, placement);
            if (error != null)
                return $"{PlacementPath} : {error}";
            _cache.Clear();
            foreach ((string id, ChestData chest) in chests)
                _cache[id] = chest;
            _placement = placement;
            _loadError = null;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }

    private static string ParseChests(string json, References refs, Dictionary<string, ChestData> chests)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
            return "liste non vide de coffres attendue";
        foreach (JsonElement entry in document.RootElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
                return "chaque coffre doit être un objet";
            JsonConfigReader reader = new(entry);
            string id = reader.Text(entry, "id");
            reader.AllowOnly(entry, "clés", "id", "rarity", "sprite_closed", "sprite_open", "column_height", "column_core",
                "fx_family", "open_range_px", "loot_table_id", "loot_rolls", "downgrade_to");
            ChestData chest = new()
            {
                Id = id,
                Rarity = reader.OneOf(entry, "rarity", refs.Rarities),
                SpriteClosed = reader.Resource(entry, "sprite_closed", refs.ResourceExists),
                SpriteOpen = reader.Resource(entry, "sprite_open", refs.ResourceExists),
                ColumnHeight = reader.Positive(entry, "column_height"),
                ColumnCore = reader.Positive(entry, "column_core"),
                FxFamily = reader.OneOf(entry, "fx_family", refs.Families),
                OpenRangePx = reader.Positive(entry, "open_range_px"),
                LootTableId = reader.Text(entry, "loot_table_id"),
                LootRolls = reader.Count(entry, "loot_rolls", 10),
                DowngradeTo = entry.TryGetProperty("downgrade_to", out _) ? reader.Text(entry, "downgrade_to") : null,
            };
            if (reader.Error == null && !refs.LootTableExists(chest.LootTableId))
                reader.Fail($"loot_table_id : table « {chest.LootTableId} » inconnue");
            if (reader.Error == null && !chests.TryAdd(id, chest))
                reader.Fail("identifiant en double");
            if (reader.Error != null)
                return $"coffre {id ?? "?"} : {reader.Error}";
        }
        foreach (ChestData chest in chests.Values)
        {
            if (chest.DowngradeTo != null && (chest.DowngradeTo == chest.Id || !chests.ContainsKey(chest.DowngradeTo)))
                return $"coffre {chest.Id} : downgrade_to : coffre « {chest.DowngradeTo} » inconnu";
        }
        return null;
    }

    private static string ParsePlacement(string json, Dictionary<string, ChestData> chests, ChestPlacementData placement)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonConfigReader reader = new(document.RootElement);
        JsonElement root = reader.Root;
        reader.AllowOnly(root, "placement", "min_spacing_px", "candidates_per_chest", "attempts_per_chest", "path_bonus",
            "clearance_px", "building_clearance_px", "edge_pointer", "groups");
        placement.MinSpacingPx = reader.Positive(root, "min_spacing_px");
        placement.CandidatesPerChest = reader.Count(root, "candidates_per_chest", 1000);
        placement.AttemptsPerChest = reader.Count(root, "attempts_per_chest", 10000);
        placement.PathBonus = reader.NonNegative(root, "path_bonus");
        placement.Clearance = Clearance(reader, reader.Section("clearance_px"));
        placement.BuildingClearance = Clearance(reader, reader.Section("building_clearance_px"));
        JsonElement pointer = reader.Section("edge_pointer");
        reader.AllowOnly(pointer, "edge_pointer", "range_px", "max");
        placement.PointerRangePx = reader.Positive(pointer, "range_px");
        placement.PointerMax = reader.Integer(pointer, "max", 0, 20);
        foreach (JsonElement group in reader.List(root, "groups", 1))
        {
            reader.AllowOnly(group, "groups", "chest", "count", "band");
            string chest = reader.OneOf(group, "chest", chests.Keys);
            int count = reader.Integer(group, "count", 0, 1000);
            (float min, float max) = reader.Range(group, "band", 0f, 1f);
            placement.Groups.Add(new ChestPlacementGroup(chest, count, min, max));
        }
        return reader.Error;
    }

    private static PixelClearance Clearance(JsonConfigReader reader, JsonElement owner)
    {
        reader.AllowOnly(owner, "dégagement", "side", "north", "south");
        return new PixelClearance(reader.NonNegative(owner, "side"), reader.NonNegative(owner, "north"), reader.NonNegative(owner, "south"));
    }

    public static ChestPlacementData LoadPlacement()
    {
        Load();
        return _placement;
    }

    public static ChestData Get(string id)
    {
        Load();
        if (_cache.TryGetValue(id, out ChestData data))
            return data;

        GD.PushWarning($"[ChestDataLoader] Unknown chest id: {id}");
        return null;
    }

    public static List<ChestData> GetAll()
    {
        Load();
        return new List<ChestData>(_cache.Values);
    }
}
