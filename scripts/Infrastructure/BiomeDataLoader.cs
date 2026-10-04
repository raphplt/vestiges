using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class BiomeData
{
    public string Id;
    public string Name;
    public string FootstepAudio;
    public Dictionary<string, float> TerrainWeights = new();
    public List<string> ExplorationEnemyPool = new();
    public List<string> ResurgenceEnemyPool = new();
    public int DangerLevel;
    public Dictionary<string, float> PoiPool = new();
    public int PoiCountMin = 3;
    public int PoiCountMax = 5;

    /// <summary>
    /// Mapping terrain_type → liste de chemins de textures (relatifs à assets/tiles/).
    /// Ex: "grass" → ["foret/tile_foret_sol_base", "foret/tile_foret_sol_v2"]
    /// </summary>
    public Dictionary<string, List<string>> TileSources = new();

    /// <summary>Groupes de tile_sources en tuiles de Wang (16 tuiles choisies par les arêtes de la cellule).</summary>
    public HashSet<string> WangTileGroups = new();

    /// <summary>Les matières du biome (herbe, terre, sous-bois…) se fondent entre elles comme deux biomes voisins.</summary>
    public bool BlendTerrains;

    /// <summary>Allure des chemins de terre dans ce biome (plan 10 T3) ; null : style par défaut de world_gen.json.</summary>
    public PathStyle? PathStyle;

    /// <summary>
    /// Poids relatif pour la taille du secteur angulaire sur la map.
    /// Plus le poids est élevé, plus le biome occupe d'espace.
    /// </summary>
    public float MapWeight = 1.0f;
}

public static class BiomeDataLoader
{
    private static readonly List<BiomeData> _allBiomes = new();
    private static readonly Dictionary<string, BiomeData> _byId = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        string dirPath = "res://data/biomes";
        DirAccess dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            GD.PushError("[BiomeDataLoader] Cannot open data/biomes/");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (!fileName.EndsWith(".json") || fileName.StartsWith("_"))
            {
                fileName = dir.GetNext();
                continue;
            }

            LoadBiomeFile($"{dirPath}/{fileName}");
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        // Groupes d'apparition contrôlés une fois : une créature écartée n'est pas cherchée à chaque tirage.
        foreach (BiomeData biome in _allBiomes)
        {
            foreach (string error in EnemyPools.KeepKnown(biome.ExplorationEnemyPool, $"biome {biome.Id}, exploration_enemy_pool"))
                GD.PushError($"[BiomeDataLoader] {error}");
            foreach (string error in EnemyPools.KeepKnown(biome.ResurgenceEnemyPool, $"biome {biome.Id}, resurgence_enemy_pool"))
                GD.PushError($"[BiomeDataLoader] {error}");
        }

        _loaded = true;
        GD.Print($"[BiomeDataLoader] Loaded {_allBiomes.Count} biomes");
    }

    public static BiomeData Get(string id)
    {
        if (!_loaded)
            Load();

        return _byId.GetValueOrDefault(id);
    }

    public static List<BiomeData> GetAll()
    {
        if (!_loaded)
            Load();

        return _allBiomes;
    }

    private static void LoadBiomeFile(string path)
    {
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning($"[BiomeDataLoader] Cannot open {path}");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[BiomeDataLoader] Parse error in {path}: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        BiomeData biome = ParseBiome(dict);
        if (biome == null || string.IsNullOrEmpty(biome.Id))
            return;

        _allBiomes.Add(biome);
        _byId[biome.Id] = biome;
    }

    private static BiomeData ParseBiome(Godot.Collections.Dictionary dict)
    {
        BiomeData biome = new()
        {
            Id = dict["id"].AsString(),
            FootstepAudio = dict.ContainsKey("footstep_audio") ? dict["footstep_audio"].AsString() : null,
            Name = dict.ContainsKey("name") ? dict["name"].AsString() : "",
            DangerLevel = dict.ContainsKey("danger_level") ? (int)dict["danger_level"].AsDouble() : 1
        };

        if (dict.ContainsKey("terrain_weights"))
        {
            Godot.Collections.Dictionary weights = dict["terrain_weights"].AsGodotDictionary();
            foreach (Variant key in weights.Keys)
                biome.TerrainWeights[key.AsString()] = (float)weights[key].AsDouble();
        }

        if (dict.ContainsKey("exploration_enemy_pool"))
        {
            Godot.Collections.Array pool = dict["exploration_enemy_pool"].AsGodotArray();
            foreach (Variant item in pool)
                biome.ExplorationEnemyPool.Add(item.AsString());
        }

        if (dict.ContainsKey("resurgence_enemy_pool"))
        {
            Godot.Collections.Array pool = dict["resurgence_enemy_pool"].AsGodotArray();
            foreach (Variant item in pool)
                biome.ResurgenceEnemyPool.Add(item.AsString());
        }

        if (dict.ContainsKey("poi_pool"))
        {
            Godot.Collections.Dictionary pool = dict["poi_pool"].AsGodotDictionary();
            foreach (Variant key in pool.Keys)
                biome.PoiPool[key.AsString()] = (float)pool[key].AsDouble();
        }

        if (dict.ContainsKey("poi_count_min"))
            biome.PoiCountMin = (int)dict["poi_count_min"].AsDouble();

        if (dict.ContainsKey("poi_count_max"))
            biome.PoiCountMax = (int)dict["poi_count_max"].AsDouble();

        if (dict.ContainsKey("map_weight"))
            biome.MapWeight = (float)dict["map_weight"].AsDouble();

        if (dict.ContainsKey("tile_sources"))
        {
            Godot.Collections.Dictionary srcDict = dict["tile_sources"].AsGodotDictionary();
            foreach (Variant key in srcDict.Keys)
            {
                string terrainName = key.AsString();
                Godot.Collections.Array paths = srcDict[key].AsGodotArray();
                List<string> pathList = new();
                foreach (Variant p in paths)
                    pathList.Add(p.AsString());
                biome.TileSources[terrainName] = pathList;
            }
        }

        if (dict.ContainsKey("blend_terrains"))
            biome.BlendTerrains = dict["blend_terrains"].AsBool();

        if (dict.ContainsKey("path_style"))
            biome.PathStyle = Infrastructure.PathStyle.Parse(dict["path_style"].AsGodotDictionary(), Infrastructure.PathStyle.Default);

        if (dict.ContainsKey("wang_tile_groups"))
        {
            foreach (Variant group in dict["wang_tile_groups"].AsGodotArray())
                biome.WangTileGroups.Add(group.AsString());
        }

        return biome;
    }
}
