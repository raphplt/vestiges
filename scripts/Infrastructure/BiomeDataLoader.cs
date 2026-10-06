using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Définition partagée d'un biome, publiée en lecture seule après validation du catalogue entier.</summary>
public sealed class BiomeData
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string FootstepAudio { get; init; }
    public IReadOnlyDictionary<string, float> TerrainWeights { get; init; }
    // Les répétitions sont des poids : ne pas les dédupliquer.
    public IReadOnlyList<string> ExplorationEnemyPool { get; init; }
    public IReadOnlyList<string> ResurgenceEnemyPool { get; init; }
    public int DangerLevel { get; init; }
    public IReadOnlyDictionary<string, float> PoiPool { get; init; }
    public int PoiCountMin { get; init; }
    public int PoiCountMax { get; init; }
    /// <summary>Terrain → chemins relatifs à assets/tiles/, sans extension.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> TileSources { get; init; }
    /// <summary>Groupes de Wang complets : seize tuiles par matière.</summary>
    public IReadOnlySet<string> WangTileGroups { get; init; }
    public bool BlendTerrains { get; init; }
    /// <summary>Null : style de chemins de world_gen.json.</summary>
    public PathStyle? PathStyle { get; init; }
    /// <summary>Poids relatif de la surface occupée par le biome.</summary>
    public float MapWeight { get; init; }
}

public static class BiomeDataLoader
{
    private const string DirectoryPath = "res://data/biomes";
    private static readonly string[] Terrains = { "grass", "concrete", "water", "forest" };
    // Groupes spéciaux consommés par BiomeTileMapper, en plus des quatre terrains du générateur.
    private static readonly string[] TileGroups =
    {
        "grass", "concrete", "water", "forest", "sidewalk", "building_interior", "building_edge", "plaza",
        "wet_mid_ground", "mud_transition", "bank_dirty", "sludge_dark",
        "bank_edge_nw", "bank_edge_ne", "bank_edge_se", "bank_edge_sw",
        "bank_inner_corner_nw", "bank_inner_corner_ne", "bank_inner_corner_se", "bank_inner_corner_sw",
        "bank_outer_corner_nw", "bank_outer_corner_ne", "bank_outer_corner_se", "bank_outer_corner_sw"
    };
    private static List<BiomeData> _allBiomes = new();
    private static Dictionary<string, BiomeData> _byId = new();
    private static bool _attempted;
    private static string _loadError;

    public static void Load()
    {
        if (!TryLoad(out string error))
            GD.PushError($"[BiomeDataLoader] {error}");
    }

    /// <summary>Tout le catalogue est disponible, ou un diagnostic fichier/champ est rendu sans publication.</summary>
    public static bool TryLoad(out string error)
    {
        if (_attempted)
        {
            error = _loadError;
            return error == null;
        }
        _attempted = true;
        using DirAccess dir = DirAccess.Open(DirectoryPath);
        if (dir == null)
        {
            error = _loadError = $"{DirectoryPath} : dossier introuvable";
            return false;
        }
        Dictionary<string, string> files = new();
        // Garder l'ordre de l'ancien lecteur : il influence le choix des biomes à seed fixée.
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".json", StringComparison.Ordinal) && !fileName.StartsWith('_'))
            {
                string path = $"{DirectoryPath}/{fileName}";
                using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    dir.ListDirEnd();
                    error = _loadError = $"{path} : fichier illisible";
                    return false;
                }
                files.Add(path, file.GetAsText());
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();
        EnemyDataLoader.Load();
        HashSet<string> pois = new();
        foreach (PoiData poi in PoiDataLoader.GetAll())
            pois.Add(poi.Id);
        error = _loadError = Apply(files, EnemyDataLoader.Exists, pois.Contains,
            DataKeySets.TopLevelKeys(AudioManager.SoundBankPath).Contains, path => ResourceLoader.Exists(path));
        if (error == null)
            GD.Print($"[BiomeDataLoader] Loaded {_allBiomes.Count} biomes");
        return error == null;
    }

    public static BiomeData Get(string id)
    {
        if (!_attempted)
            Load();
        return _byId.GetValueOrDefault(id);
    }

    /// <summary>Copie de la liste : le catalogue et ses définitions partagées ne peuvent pas être modifiés.</summary>
    public static List<BiomeData> GetAll()
    {
        if (!_attempted)
            Load();
        return new List<BiomeData>(_allBiomes);
    }

    /// <summary>Publie ensemble des fiches déjà lues ; tout refus préserve intégralement le catalogue précédent.</summary>
    public static string Apply(IReadOnlyDictionary<string, string> files, Func<string, bool> enemyExists,
        Func<string, bool> poiExists, Func<string, bool> audioExists, Func<string, bool> resourceExists)
    {
        if (files.Count == 0)
            return $"{DirectoryPath} : au moins un biome attendu";
        List<BiomeData> biomes = new();
        Dictionary<string, BiomeData> byId = new();
        foreach ((string path, string text) in files)
        {
            if (!TryParse(text, enemyExists, poiExists, audioExists, resourceExists, out BiomeData biome, out string error))
                return $"{path} : {error}";
            if (!byId.TryAdd(biome.Id, biome))
                return $"{path} : biome « {biome.Id} », id en double";
            biomes.Add(biome);
        }
        _allBiomes = biomes;
        _byId = byId;
        _loadError = null;
        _attempted = true;
        return null;
    }

    public static bool TryParse(string text, Func<string, bool> enemyExists, Func<string, bool> poiExists,
        Func<string, bool> audioExists, Func<string, bool> resourceExists, out BiomeData biome, out string error)
    {
        biome = null;
        error = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonElement root = document.RootElement;
            JsonConfigReader reader = new(root);
            reader.AllowOnly(root, "biome", "id", "name", "footstep_audio", "terrain_weights", "exploration_enemy_pool",
                "resurgence_enemy_pool", "danger_level", "poi_pool", "poi_count_min", "poi_count_max", "map_weight",
                "tile_sources", "wang_tile_groups", "blend_terrains", "path_style");
            string id = reader.Text(root, "id");
            string name = OptionalText(reader, root, "name", "");
            string audio = OptionalText(reader, root, "footstep_audio", null);
            if (audio != null && !audioExists(audio))
                reader.Fail($"footstep_audio : son « {audio} » inconnu");
            Dictionary<string, float> weights = Weights(reader, reader.Section("terrain_weights"), "terrain_weights", key => Array.IndexOf(Terrains, key) >= 0);
            List<string> exploration = TextList(reader, root, "exploration_enemy_pool", enemyExists, "créature", 1);
            List<string> resurgence = TextList(reader, root, "resurgence_enemy_pool", enemyExists, "créature", 1);
            int danger = OptionalInteger(reader, root, "danger_level", 1);
            Dictionary<string, float> pois = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("poi_pool", out _)
                ? Weights(reader, reader.Section("poi_pool"), "poi_pool", poiExists) : new();
            int poiMin = OptionalInteger(reader, root, "poi_count_min", 3);
            int poiMax = OptionalInteger(reader, root, "poi_count_max", 5);
            if (poiMin > poiMax)
                reader.Fail("poi_count_min/poi_count_max : intervalle croissant attendu");
            float mapWeight = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("map_weight", out _)
                ? reader.Positive(root, "map_weight") : 1f;
            JsonElement tileSection = reader.Section("tile_sources");
            Dictionary<string, IReadOnlyList<string>> tiles = new();
            foreach ((string key, JsonElement _) in reader.Entries(tileSection, "tile_sources"))
            {
                if (Array.IndexOf(TileGroups, key) < 0)
                    reader.Fail($"tile_sources.{key} : groupe inconnu");
                List<string> paths = TextList(reader, tileSection, key,
                    path => !path.StartsWith('/') && !path.Contains("..", StringComparison.Ordinal)
                        && !path.Contains(':') && resourceExists($"res://assets/tiles/{path}.png"), "tuile", 1);
                tiles.Add(key, paths.AsReadOnly());
            }
            if (tiles.Count == 0)
                reader.Fail("tile_sources : au moins un groupe attendu");
            List<string> wang = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("wang_tile_groups", out _)
                ? TextList(reader, root, "wang_tile_groups", tiles.ContainsKey, "groupe de tuiles", 0) : new();
            HashSet<string> wangSet = new();
            foreach (string group in wang)
            {
                if (!wangSet.Add(group))
                    reader.Fail($"wang_tile_groups : groupe « {group} » en double");
                if (tiles[group].Count % 16 != 0)
                    reader.Fail($"wang_tile_groups.{group} : nombre de tuiles multiple de 16 attendu");
            }
            bool blend = reader.Flag(root, "blend_terrains");
            PathStyle? style = null;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("path_style", out _))
            {
                JsonElement path = reader.Section("path_style");
                reader.AllowOnly(path, "path_style", "tone", "ruts", "width_px");
                string tone = OptionalText(reader, path, "tone", "#6f5d46");
                if (tone != null && !Color.HtmlIsValid(tone))
                    reader.Fail("path_style.tone : couleur HTML attendue");
                float ruts = path.ValueKind == JsonValueKind.Object && path.TryGetProperty("ruts", out _)
                    ? reader.Chance(path, "ruts") : PathStyle.Default.Ruts;
                float width = path.ValueKind == JsonValueKind.Object && path.TryGetProperty("width_px", out _)
                    ? reader.Positive(path, "width_px") : PathStyle.Default.WidthPx;
                style = new PathStyle { Tone = reader.Error == null ? new Color(tone) : default, Ruts = ruts, WidthPx = width };
            }
            if (reader.Error != null)
            {
                error = $"biome « {id ?? "?"} », {reader.Error}";
                return false;
            }
            biome = new BiomeData
            {
                Id = id, Name = name, FootstepAudio = audio, TerrainWeights = new ReadOnlyDictionary<string, float>(weights),
                ExplorationEnemyPool = exploration.AsReadOnly(), ResurgenceEnemyPool = resurgence.AsReadOnly(),
                DangerLevel = danger, PoiPool = new ReadOnlyDictionary<string, float>(pois), PoiCountMin = poiMin, PoiCountMax = poiMax,
                MapWeight = mapWeight, TileSources = new ReadOnlyDictionary<string, IReadOnlyList<string>>(tiles),
                WangTileGroups = new ReadOnlySet<string>(wangSet), BlendTerrains = blend, PathStyle = style
            };
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON illisible : {exception.Message}";
            return false;
        }
    }

    private static Dictionary<string, float> Weights(JsonConfigReader reader, JsonElement section, string where, Func<string, bool> known)
    {
        Dictionary<string, float> weights = new();
        double total = 0;
        foreach ((string key, JsonElement _) in reader.Entries(section, where))
        {
            if (!known(key))
                reader.Fail($"{where}.{key} : référence inconnue");
            float weight = reader.NonNegative(section, key);
            if (!float.IsFinite(weight))
                reader.Fail($"{where}.{key} : nombre fini attendu");
            weights.Add(key, weight);
            total += weight;
        }
        if (total <= 0 || total > float.MaxValue)
            reader.Fail($"{where} : total fini strictement positif attendu");
        return weights;
    }

    private static List<string> TextList(JsonConfigReader reader, JsonElement owner, string key, Func<string, bool> known, string kind, int minCount)
    {
        List<string> values = new();
        int index = 0;
        foreach (JsonElement item in reader.List(owner, key, minCount))
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                reader.Fail($"{key}[{index}] : texte non vide attendu");
            else if (!known(item.GetString()))
                reader.Fail($"{key}[{index}] : {kind} « {item.GetString()} » introuvable");
            else
                values.Add(item.GetString());
            index++;
        }
        return values;
    }

    private static string OptionalText(JsonConfigReader reader, JsonElement owner, string key, string fallback)
    {
        if (reader.Error != null || owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(key, out _))
            return fallback;
        return reader.Text(owner, key);
    }

    private static int OptionalInteger(JsonConfigReader reader, JsonElement owner, string key, int fallback)
    {
        if (reader.Error != null || owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(key, out JsonElement value))
            return fallback;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int count) || count < 0)
        {
            reader.Fail($"{key} : entier positif ou nul attendu");
            return fallback;
        }
        return count;
    }
}
