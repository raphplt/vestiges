using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Base;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Génère le monde procédural au lancement : terrain via WorldGenerator,
/// puis place les nœuds de ressource en fonction du terrain.
/// Gère le respawn périodique des ressources épuisées.
/// </summary>
public partial class WorldSetup : Node2D
{
    private TileMapLayer _ground;
    private Node2D _resourceContainer;
    private PackedScene _resourceScene;
    private HashSet<Vector2I> _usedCells = new();
    private Timer _respawnTimer;

    private WorldGenerator _generator;
    private WorldGenConfig _config;

    /// <summary>Seed de la run, injectée par GameBootstrap.</summary>
    public ulong Seed { get; set; }

    /// <summary>Référence publique au générateur pour les autres systèmes.</summary>
    public WorldGenerator Generator => _generator;

    /// <summary>Checks if a world position is impassable (water or erased void).</summary>
    public bool IsWaterAt(Vector2 worldPos)
    {
        if (_generator == null || _ground == null)
            return false;
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(worldPos));
        if (_generator.IsErased(cell.X, cell.Y))
            return true;
        return _generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water;
    }

    /// <summary>Retourne le BiomeData à une position monde, ou null.</summary>
    public BiomeData GetBiomeAt(Vector2 worldPos)
    {
        if (_generator == null || _ground == null)
            return null;
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(worldPos));
        return _generator.GetBiome(cell.X, cell.Y);
    }

    public override void _Ready()
    {
        EnemyDataLoader.Load();
        ResourceDataLoader.Load();
        BiomeDataLoader.Load();

        _ground = GetNode<TileMapLayer>("Ground");
        _resourceContainer = GetNode<Node2D>("ResourceContainer");
        _resourceScene = GD.Load<PackedScene>("res://scenes/base/ResourceNode.tscn");

        _config = WorldGenConfig.Load();

        // Read seed from GameManager (set in Hub or 0 = random)
        GameManager gm = GetNodeOrNull<GameManager>("/root/GameManager");
        if (gm != null && gm.RunSeed != 0)
            Seed = gm.RunSeed;
        if (Seed == 0)
            Seed = GD.Randi();

        _generator = new WorldGenerator(
            _config.MapRadius,
            _config.FoyerClearance,
            _config.CaIterations,
            _config.Zones,
            Seed,
            _config.EdgeFadeWidth
        );

        // Charger les biomes disponibles et générer avec secteurs
        List<BiomeData> availableBiomes = LoadAvailableBiomes();
        TerrainType[,] terrain;

        if (availableBiomes.Count >= 2)
        {
            terrain = _generator.Generate(availableBiomes, _config.BiomeCount);
        }
        else
        {
            terrain = _generator.Generate();
        }

        CreateVoidBackground();
        ApplyTerrain(terrain);
        SpawnResources();

        _respawnTimer = new Timer();
        _respawnTimer.WaitTime = _config.RespawnInterval;
        _respawnTimer.Autostart = true;
        _respawnTimer.Timeout += OnRespawnTimer;
        AddChild(_respawnTimer);

        GD.Print($"[WorldSetup] World generated with seed {Seed}");
    }

    private List<BiomeData> LoadAvailableBiomes()
    {
        List<BiomeData> biomes = new();

        if (_config.AvailableBiomes != null && _config.AvailableBiomes.Count > 0)
        {
            foreach (string biomeId in _config.AvailableBiomes)
            {
                BiomeData biome = BiomeDataLoader.Get(biomeId);
                if (biome != null)
                    biomes.Add(biome);
                else
                    GD.PushWarning($"[WorldSetup] Biome '{biomeId}' not found in data");
            }
        }
        else
        {
            biomes = BiomeDataLoader.GetAll();
        }

        return biomes;
    }

    /// <summary>
    /// Fond noir derrière le TileMap : le vide, l'Effacé.
    /// Au-delà de la carte et dans les trous de décomposition, c'est le néant.
    /// </summary>
    private void CreateVoidBackground()
    {
        ColorRect voidBg = new();
        float worldSize = _config.MapRadius * 64f * 2f;
        voidBg.Size = new Vector2(worldSize, worldSize);
        voidBg.Position = new Vector2(-worldSize * 0.5f, -worldSize * 0.5f);
        voidBg.Color = new Color(0f, 0f, 0f, 1f);
        voidBg.ZIndex = -100;
        AddChild(voidBg);
        MoveChild(voidBg, 0);
    }

    private void ApplyTerrain(TerrainType[,] terrain)
    {
        int radius = _config.MapRadius;
        int size = radius * 2 + 1;

        for (int gx = 0; gx < size; gx++)
        {
            for (int gy = 0; gy < size; gy++)
            {
                int x = gx - radius;
                int y = gy - radius;

                // Hors limites ou effacé : pas de tile, le vide noir transparaît
                if (!_generator.IsWithinBounds(x, y) || _generator.IsErased(x, y))
                    continue;

                Vector2I cell = new(x, y);
                int sourceId = (int)terrain[gx, gy];
                _ground.SetCell(cell, sourceId, Vector2I.Zero);
            }
        }
    }

    private void SpawnResources()
    {
        Dictionary<string, int> spawnCounts = new();
        for (int i = 0; i < _config.ResourceCount; i++)
        {
            string spawned = SpawnSingleResource();
            if (spawned != null)
            {
                spawnCounts.TryGetValue(spawned, out int count);
                spawnCounts[spawned] = count + 1;
            }
        }

        string breakdown = string.Join(", ", spawnCounts.Select(kv => $"{kv.Key}={kv.Value}"));
        GD.Print($"[WorldSetup] Spawned {_usedCells.Count} resource nodes ({breakdown})");
    }

    private string SpawnSingleResource(float minDistFromPlayer = 0f)
    {
        Vector2I cell = PickResourceCell(_usedCells, minDistFromPlayer);
        if (cell == new Vector2I(int.MinValue, int.MinValue))
            return null;

        string resourceId = PickResourceForCell(cell);

        ResourceData data = ResourceDataLoader.Get(resourceId);
        if (data == null)
        {
            GD.PushWarning($"[WorldSetup] ResourceData null for '{resourceId}'");
            return null;
        }

        _usedCells.Add(cell);

        Vector2 worldPos = _ground.MapToLocal(cell);
        ResourceNode node = _resourceScene.Instantiate<ResourceNode>();
        node.GlobalPosition = worldPos;
        _resourceContainer.AddChild(node);
        node.Initialize(data);
        return resourceId;
    }

    private void OnRespawnTimer()
    {
        RefreshUsedCells();

        int activeCount = _resourceContainer.GetChildCount();
        if (activeCount >= _config.RespawnThreshold)
            return;

        int toSpawn = Mathf.Min(_config.ResourceCount - activeCount, 5);
        for (int i = 0; i < toSpawn; i++)
        {
            SpawnSingleResource(300f);
        }

        if (toSpawn > 0)
            GD.Print($"[WorldSetup] Respawned {toSpawn} resources (active: {activeCount + toSpawn})");
    }

    private void RefreshUsedCells()
    {
        _usedCells.Clear();
        foreach (Node child in _resourceContainer.GetChildren())
        {
            if (child is ResourceNode res && !res.IsExhausted)
            {
                Vector2I cell = _ground.LocalToMap(_ground.ToLocal(res.GlobalPosition));
                _usedCells.Add(cell);
            }
        }
    }

    private string PickResourceForCell(Vector2I cell)
    {
        // Priorité au biome s'il existe, sinon fallback terrain
        BiomeData biome = _generator.GetBiome(cell.X, cell.Y);

        Dictionary<string, float> bias;
        if (biome != null && biome.ResourceBias.Count > 0)
        {
            bias = biome.ResourceBias;
        }
        else
        {
            TerrainType terrain = _generator.GetTerrain(cell.X, cell.Y);
            bias = _config.GetTerrainBias(terrain);
        }

        float roll = (float)GD.Randf();
        float cumulative = 0f;

        foreach (KeyValuePair<string, float> kv in bias)
        {
            cumulative += kv.Value;
            if (roll < cumulative)
                return kv.Key;
        }

        return "wood";
    }

    private Vector2I PickResourceCell(HashSet<Vector2I> used, float minDistFromPlayer = 0f)
    {
        int radius = _config.MapRadius;
        int foyerExclusion = _config.FoyerClearance + 1;
        int safeRadius = radius - _config.EdgeFadeWidth;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        Vector2 playerPos = playerNode is Node2D player ? player.GlobalPosition : Vector2.Zero;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = (int)GD.RandRange(-safeRadius + 1, safeRadius);
            int y = (int)GD.RandRange(-safeRadius + 1, safeRadius);
            Vector2I cell = new(x, y);

            // Rester dans le cercle (hors zone de décomposition)
            if (x * x + y * y > safeRadius * safeRadius)
                continue;

            if (used.Contains(cell))
                continue;

            if (Mathf.Abs(x) <= foyerExclusion && Mathf.Abs(y) <= foyerExclusion)
                continue;

            if (_generator.GetTerrain(x, y) == TerrainType.Water)
                continue;

            if (minDistFromPlayer > 0f)
            {
                Vector2 worldPos = _ground.MapToLocal(cell);
                if (worldPos.DistanceTo(playerPos) < minDistFromPlayer)
                    continue;
            }

            return cell;
        }

        return new Vector2I(int.MinValue, int.MinValue);
    }
}

/// <summary>
/// Données de configuration chargées depuis world_gen.json.
/// </summary>
public class WorldGenConfig
{
    public int MapRadius;
    public int FoyerClearance;
    public int CaIterations;
    public List<WorldGenerator.ZoneConfig> Zones = new();
    public int ResourceCount;
    public float RespawnInterval;
    public int RespawnThreshold;
    public int BiomeCount = 3;
    public int EdgeFadeWidth = 5;
    public List<string> AvailableBiomes = new();

    private readonly Dictionary<string, Dictionary<string, float>> _terrainBias = new();

    private static readonly Dictionary<string, float> DefaultBias = new()
    {
        { "wood", 0.50f }, { "stone", 0.35f }, { "metal", 0.15f }
    };

    public Dictionary<string, float> GetTerrainBias(TerrainType terrain)
    {
        string key = terrain.ToString().ToLowerInvariant();
        return _terrainBias.TryGetValue(key, out Dictionary<string, float> bias) ? bias : DefaultBias;
    }

    public static WorldGenConfig Load()
    {
        WorldGenConfig config = new();

        FileAccess file = FileAccess.Open("res://data/world/world_gen.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[WorldGenConfig] Cannot open world_gen.json, using defaults");
            config.SetDefaults();
            return config;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[WorldGenConfig] Parse error: {json.GetErrorMessage()}");
            config.SetDefaults();
            return config;
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();

        config.MapRadius = (int)dict["map_radius"].AsDouble();
        config.FoyerClearance = (int)dict["foyer_clearance"].AsDouble();
        config.CaIterations = (int)dict["ca_iterations"].AsDouble();
        config.ResourceCount = (int)dict["resource_count"].AsDouble();
        config.RespawnInterval = (float)dict["respawn_interval"].AsDouble();
        config.RespawnThreshold = (int)dict["respawn_threshold"].AsDouble();

        if (dict.ContainsKey("biome_count"))
            config.BiomeCount = (int)dict["biome_count"].AsDouble();

        if (dict.ContainsKey("edge_fade_width"))
            config.EdgeFadeWidth = (int)dict["edge_fade_width"].AsDouble();

        if (dict.ContainsKey("available_biomes"))
        {
            Godot.Collections.Array biomesArray = dict["available_biomes"].AsGodotArray();
            foreach (Variant biomeItem in biomesArray)
                config.AvailableBiomes.Add(biomeItem.AsString());
        }

        Godot.Collections.Array zonesArray = dict["zones"].AsGodotArray();
        foreach (Variant zoneVar in zonesArray)
        {
            Godot.Collections.Dictionary zoneDict = zoneVar.AsGodotDictionary();
            Godot.Collections.Dictionary weights = zoneDict["weights"].AsGodotDictionary();

            WorldGenerator.ZoneConfig zone = new()
            {
                MaxRadius = (float)zoneDict["max_radius"].AsDouble(),
                GrassWeight = (float)weights["grass"].AsDouble(),
                ConcreteWeight = (float)weights["concrete"].AsDouble(),
                WaterWeight = (float)weights["water"].AsDouble(),
                ForestWeight = (float)weights["forest"].AsDouble()
            };
            config.Zones.Add(zone);
        }

        if (dict.ContainsKey("resource_terrain_bias"))
        {
            Godot.Collections.Dictionary biasDict = dict["resource_terrain_bias"].AsGodotDictionary();
            foreach (Variant terrainKey in biasDict.Keys)
            {
                string terrainName = terrainKey.AsString();
                Godot.Collections.Dictionary resWeights = biasDict[terrainKey].AsGodotDictionary();
                Dictionary<string, float> bias = new();
                foreach (Variant resKey in resWeights.Keys)
                    bias[resKey.AsString()] = (float)resWeights[resKey].AsDouble();
                config._terrainBias[terrainName] = bias;
            }
        }

        GD.Print($"[WorldGenConfig] Loaded — radius={config.MapRadius}, zones={config.Zones.Count}, resources={config.ResourceCount}");
        return config;
    }

    private void SetDefaults()
    {
        MapRadius = 30;
        FoyerClearance = 4;
        CaIterations = 4;
        ResourceCount = 50;
        RespawnInterval = 30f;
        RespawnThreshold = 35;

        Zones.Add(new WorldGenerator.ZoneConfig
        {
            MaxRadius = 10, GrassWeight = 0.75f, ConcreteWeight = 0.20f,
            WaterWeight = 0f, ForestWeight = 0.05f
        });
        Zones.Add(new WorldGenerator.ZoneConfig
        {
            MaxRadius = 20, GrassWeight = 0.45f, ConcreteWeight = 0.20f,
            WaterWeight = 0.10f, ForestWeight = 0.25f
        });
        Zones.Add(new WorldGenerator.ZoneConfig
        {
            MaxRadius = 999, GrassWeight = 0.25f, ConcreteWeight = 0.15f,
            WaterWeight = 0.20f, ForestWeight = 0.40f
        });
    }
}
