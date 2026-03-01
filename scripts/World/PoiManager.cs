using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.World;

/// <summary>
/// Gère le spawn procédural des Points d'Intérêt sur la map.
/// Chaque biome a son propre pool de POI et un nombre min/max.
/// Les POI sont placés en respectant des distances minimales entre eux et par rapport au Foyer.
/// Spawn aussi les ennemis gardes autour des POI gardés.
/// </summary>
public partial class PoiManager : Node
{
    private const float GuardSpawnRadius = 60f;
    private const float GuardHpScale = 1.5f;
    private const float GuardDmgScale = 1.2f;

    private readonly HashSet<Vector2I> _usedPoiCells = new();
    private readonly List<PointOfInterest> _spawnedPois = new();
    private PackedScene _poiScene;
    private WorldGenerator _generator;
    private TileMapLayer _ground;
    private int _minDistanceBetween = 8;
    private EnemyPool _enemyPool;
    private Node _enemyContainer;

    public List<PointOfInterest> SpawnedPois => _spawnedPois;

    /// <summary>
    /// Spawn les POI pour tous les biomes actifs de la run.
    /// Appelé par WorldSetup après la génération du terrain et des ressources.
    /// </summary>
    public void SpawnPois(
        WorldGenerator generator,
        TileMapLayer ground,
        Node2D container,
        HashSet<Vector2I> occupiedCells,
        int minDistanceBetween,
        EnemyPool enemyPool = null,
        Node enemyContainer = null)
    {
        _generator = generator;
        _ground = ground;
        _minDistanceBetween = minDistanceBetween;
        _enemyPool = enemyPool;
        _enemyContainer = enemyContainer;
        _poiScene = GD.Load<PackedScene>("res://scenes/world/PointOfInterest.tscn");

        // Copier les cellules occupées pour éviter le chevauchement avec les ressources
        foreach (Vector2I cell in occupiedCells)
            _usedPoiCells.Add(cell);

        List<BiomeData> activeBiomes = generator.ActiveBiomes;
        if (activeBiomes == null || activeBiomes.Count == 0)
        {
            GD.PushWarning("[PoiManager] No active biomes, skipping POI generation");
            return;
        }

        int totalSpawned = 0;
        foreach (BiomeData biome in activeBiomes)
        {
            int count = SpawnPoisForBiome(biome, container);
            totalSpawned += count;
        }

        // Ajouter les cellules POI aux cellules occupées globales
        foreach (Vector2I cell in _usedPoiCells)
            occupiedCells.Add(cell);

        GD.Print($"[PoiManager] Spawned {totalSpawned} POIs across {activeBiomes.Count} biomes");
    }

    private int SpawnPoisForBiome(BiomeData biome, Node2D container)
    {
        if (biome.PoiPool.Count == 0)
            return 0;

        int targetCount = (int)GD.RandRange(biome.PoiCountMin, biome.PoiCountMax + 1);
        int spawned = 0;

        for (int i = 0; i < targetCount; i++)
        {
            string poiId = PickPoiForBiome(biome);
            if (poiId == null)
                continue;

            PoiData data = PoiDataLoader.Get(poiId);
            if (data == null)
                continue;

            // Vérifier les restrictions de biome
            if (data.BiomeBlacklist.Contains(biome.Id))
                continue;
            if (data.BiomeWhitelist.Count > 0 && !data.BiomeWhitelist.Contains(biome.Id))
                continue;

            Vector2I cell = PickPoiCell(biome.Id, data);
            if (cell == new Vector2I(int.MinValue, int.MinValue))
                continue;

            SpawnPoiAt(cell, data, container);
            spawned++;
        }

        return spawned;
    }

    private string PickPoiForBiome(BiomeData biome)
    {
        float totalWeight = 0f;
        foreach (KeyValuePair<string, float> kv in biome.PoiPool)
        {
            if (kv.Value > 0f)
                totalWeight += kv.Value;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = (float)GD.Randf() * totalWeight;
        float cumulative = 0f;

        foreach (KeyValuePair<string, float> kv in biome.PoiPool)
        {
            if (kv.Value <= 0f)
                continue;

            cumulative += kv.Value;
            if (roll < cumulative)
                return kv.Key;
        }

        // Fallback : premier POI du pool
        foreach (KeyValuePair<string, float> kv in biome.PoiPool)
        {
            if (kv.Value > 0f)
                return kv.Key;
        }

        return null;
    }

    private Vector2I PickPoiCell(string biomeId, PoiData data)
    {
        int mapRadius = _generator.MapRadius;
        int safeRadius = mapRadius - 5; // Pas dans la zone de décomposition

        for (int attempt = 0; attempt < 50; attempt++)
        {
            int x = (int)GD.RandRange(-safeRadius + 1, safeRadius);
            int y = (int)GD.RandRange(-safeRadius + 1, safeRadius);

            float distFromCenter = Mathf.Sqrt(x * x + y * y);

            // Hors du cercle de la map
            if (distFromCenter > safeRadius)
                continue;

            // Respecter la distance min/max par rapport au Foyer
            if (distFromCenter < data.MinDistanceFromFoyer)
                continue;
            if (distFromCenter > data.MaxDistanceFromFoyer)
                continue;

            // Doit être dans le bon biome
            BiomeData cellBiome = _generator.GetBiome(x, y);
            if (cellBiome == null || cellBiome.Id != biomeId)
                continue;

            // Pas sur l'eau
            if (_generator.GetTerrain(x, y) == TerrainType.Water)
                continue;

            // Pas effacé
            if (_generator.IsErased(x, y))
                continue;

            Vector2I cell = new(x, y);

            // Pas sur une cellule déjà occupée
            if (_usedPoiCells.Contains(cell))
                continue;

            // Distance minimale avec les autres POI
            if (!CheckMinDistance(cell))
                continue;

            return cell;
        }

        return new Vector2I(int.MinValue, int.MinValue);
    }

    private bool CheckMinDistance(Vector2I cell)
    {
        foreach (Vector2I existing in _usedPoiCells)
        {
            int dx = cell.X - existing.X;
            int dy = cell.Y - existing.Y;
            if (dx * dx + dy * dy < _minDistanceBetween * _minDistanceBetween)
                return false;
        }

        return true;
    }

    private void SpawnPoiAt(Vector2I cell, PoiData data, Node2D container)
    {
        // Réserver les cellules autour du POI (son footprint)
        for (int dx = -data.Size; dx <= data.Size; dx++)
        {
            for (int dy = -data.Size; dy <= data.Size; dy++)
                _usedPoiCells.Add(new Vector2I(cell.X + dx, cell.Y + dy));
        }

        Vector2 worldPos = _ground.MapToLocal(cell);
        PointOfInterest poi = _poiScene.Instantiate<PointOfInterest>();
        poi.GlobalPosition = worldPos;
        container.AddChild(poi);
        poi.Initialize(data);

        // Spawn des gardes autour des POI gardés
        if (data.EnemyGuards.Count > 0 && _enemyPool != null && _enemyContainer != null)
            SpawnGuards(poi, data.EnemyGuards, worldPos);

        _spawnedPois.Add(poi);
    }

    /// <summary>Spawn les ennemis gardes autour d'un POI gardé.</summary>
    private void SpawnGuards(PointOfInterest poi, List<string> guardIds, Vector2 poiPos)
    {
        EnemyDataLoader.Load();
        int spawnedCount = 0;

        for (int i = 0; i < guardIds.Count; i++)
        {
            EnemyData data = EnemyDataLoader.Get(guardIds[i]);
            if (data == null)
                continue;

            float angle = Mathf.Tau * i / guardIds.Count;
            Vector2 offset = new(Mathf.Cos(angle) * GuardSpawnRadius, Mathf.Sin(angle) * GuardSpawnRadius * 0.5f);
            Vector2 guardPos = poiPos + offset;

            Enemy guard = _enemyPool.Get();
            guard.GlobalPosition = guardPos;
            _enemyContainer.AddChild(guard);
            guard.Initialize(data, GuardHpScale, GuardDmgScale);
            guard.SetGuardTarget(poi);
            spawnedCount++;
        }

        poi.SetGuardCount(spawnedCount);
        GD.Print($"[PoiManager] Spawned {spawnedCount} guards for {poi.PoiId}");
    }
}
