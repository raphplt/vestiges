using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

public enum TerrainType
{
    Grass = 0,
    Concrete = 1,
    Water = 2,
    Forest = 3
}

/// <summary>
/// Génère une carte procédurale circulaire à base de Cellular Automata.
/// Les biomes sont répartis en régions contiguës à partir de seeds aléatoires.
/// Les bords se décomposent progressivement : la réalité s'effiloche puis cesse.
/// Seed déterministe : même seed = même monde, mêmes biomes.
/// </summary>
public class WorldGenerator
{
    private readonly int _mapRadius;
    private readonly int _spawnClearance;
    private readonly int _caIterations;
    private readonly int _edgeFadeWidth;
    private readonly List<ZoneConfig> _zones;
    private readonly RandomNumberGenerator _rng;
    private TerrainType[,] _grid;
    private int[,] _biomeGrid;
    private bool[,] _withinBounds;
    private bool[,] _erasedGrid;
    private int _size;

    private List<BiomeData> _activeBiomes = new();
    private readonly List<Vector2> _regionCenters = new();
    private readonly List<int> _regionBiomes = new();
    private List<int>[,] _regionBuckets;
    private float _regionBucketSize;
    private int _regionBucketCount;
    private FastNoiseLite _biomeWarpNoiseX;
    private FastNoiseLite _biomeWarpNoiseY;
    private BiomeLayoutConfig _biomeLayout = BiomeLayoutConfig.Default;

    private const int RegionPlacementMaxFailures = 80;
    private const float RegionNeighbourFactor = 1.9f;
    private const float RegionSameNeighbourPenalty = 100f;

    public int MapRadius => _mapRadius;
    public int SpawnClearance => _spawnClearance;
    public List<BiomeData> ActiveBiomes => _activeBiomes;
    public int BiomeRegionCount => _regionCenters.Count;

    /// <summary>Taille et forme de la mosaïque de biomes (world_gen.json, bloc biome_layout).</summary>
    public struct BiomeLayoutConfig
    {
        public float RegionSpacing;
        public float WarpStrength;
        public float SpawnOffsetFactor;

        public static BiomeLayoutConfig Default => new() { RegionSpacing = 28f, WarpStrength = 9f, SpawnOffsetFactor = 0.4f };
    }

    public BiomeLayoutConfig BiomeLayout
    {
        get => _biomeLayout;
        set => _biomeLayout = value;
    }

    public struct ZoneConfig
    {
        public float MaxRadius;
        public float GrassWeight;
        public float ConcreteWeight;
        public float WaterWeight;
        public float ForestWeight;
    }

    public WorldGenerator(int mapRadius, int spawnClearance, int caIterations, List<ZoneConfig> zones, ulong seed, int edgeFadeWidth = 5)
    {
        _mapRadius = mapRadius;
        _spawnClearance = spawnClearance;
        _caIterations = caIterations;
        _edgeFadeWidth = edgeFadeWidth;
        _zones = zones;

        _rng = new RandomNumberGenerator();
        _rng.Seed = seed;

        _size = mapRadius * 2 + 1;
        _grid = new TerrainType[_size, _size];
        _biomeGrid = new int[_size, _size];
        _withinBounds = new bool[_size, _size];
        _erasedGrid = new bool[_size, _size];
    }

    public TerrainType[,] Generate(List<BiomeData> availableBiomes, int biomeCount)
    {
        ComputeCircularBounds();
        AssignBiomes(availableBiomes, biomeCount);
        SeedInitial();

        for (int i = 0; i < _caIterations; i++)
            SmoothPass();

        ClearSpawnArea();
        ApplyEdgeDecay();
        EnsureWaterConnectivity();

        string biomeNames = string.Join(", ", _activeBiomes.ConvertAll(b => b.Name));
        GD.Print($"[WorldGenerator] Generated circular map radius={_mapRadius} — biomes: {biomeNames}");
        return _grid;
    }

    public TerrainType[,] Generate()
    {
        ComputeCircularBounds();
        SeedInitial();

        for (int i = 0; i < _caIterations; i++)
            SmoothPass();

        ClearSpawnArea();
        ApplyEdgeDecay();
        EnsureWaterConnectivity();

        GD.Print($"[WorldGenerator] Generated circular map radius={_mapRadius} (no biomes)");
        return _grid;
    }

    public TerrainType GetTerrain(int x, int y)
    {
        int gx = x + _mapRadius;
        int gy = y + _mapRadius;
        if (gx < 0 || gy < 0 || gx >= _size || gy >= _size)
            return TerrainType.Water;
        if (!_withinBounds[gx, gy])
            return TerrainType.Water;
        return _grid[gx, gy];
    }

    public BiomeData GetBiome(int x, int y)
    {
        if (_activeBiomes.Count == 0)
            return null;

        int gx = x + _mapRadius;
        int gy = y + _mapRadius;
        if (gx < 0 || gy < 0 || gx >= _size || gy >= _size)
            return _activeBiomes[0];

        int index = _biomeGrid[gx, gy];
        return _activeBiomes[index];
    }

    public int GetBiomeIndex(int x, int y)
    {
        if (_activeBiomes.Count == 0)
            return -1;

        int gx = x + _mapRadius;
        int gy = y + _mapRadius;
        if (gx < 0 || gy < 0 || gx >= _size || gy >= _size)
            return 0;

        return _biomeGrid[gx, gy];
    }

    public string GetBiomeId(int x, int y)
    {
        BiomeData biome = GetBiome(x, y);
        return biome?.Id;
    }

    public bool IsWalkable(int x, int y)
    {
        return GetTerrain(x, y) != TerrainType.Water;
    }

    /// <summary>
    /// Indique si une cellule est dans les limites circulaires de la carte.
    /// </summary>
    public bool IsWithinBounds(int x, int y)
    {
        int gx = x + _mapRadius;
        int gy = y + _mapRadius;
        if (gx < 0 || gy < 0 || gx >= _size || gy >= _size)
            return false;
        return _withinBounds[gx, gy];
    }

    /// <summary>
    /// Indique si une cellule a été effacée (décomposée en bordure).
    /// Ces cellules n'ont pas de tile : c'est le vide noir, l'Effacé.
    /// </summary>
    public bool IsErased(int x, int y)
    {
        int gx = x + _mapRadius;
        int gy = y + _mapRadius;
        if (gx < 0 || gy < 0 || gx >= _size || gy >= _size)
            return true;
        if (!_withinBounds[gx, gy])
            return true;
        return _erasedGrid[gx, gy];
    }

    /// <summary>
    /// Calcule les cellules qui font partie du monde circulaire.
    /// </summary>
    private void ComputeCircularBounds()
    {
        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                int x = gx - _mapRadius;
                int y = gy - _mapRadius;
                float dist = Mathf.Sqrt(x * x + y * y);
                _withinBounds[gx, gy] = dist <= _mapRadius;
            }
        }
    }

    /// <summary>
    /// Zone de décomposition en bordure : la réalité s'effiloche puis cesse.
    /// Les tiles disparaissent progressivement dans le noir — le vide, l'Effacé.
    /// Lore : "les tiles se décomposent, les couleurs fuient, les formes perdent leur netteté.
    ///         Comme une aquarelle qui n'est pas finie."
    /// </summary>
    private void ApplyEdgeDecay()
    {
        float fadeStart = _mapRadius - _edgeFadeWidth;

        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                if (!_withinBounds[gx, gy])
                {
                    _erasedGrid[gx, gy] = true;
                    continue;
                }

                int x = gx - _mapRadius;
                int y = gy - _mapRadius;
                float dist = Mathf.Sqrt(x * x + y * y);

                if (dist <= fadeStart)
                    continue;

                float decay = (dist - fadeStart) / _edgeFadeWidth;
                decay = Mathf.Clamp(decay, 0f, 1f);

                // Le bord extérieur est toujours effacé
                if (decay > 0.8f)
                {
                    _erasedGrid[gx, gy] = true;
                    continue;
                }

                // Zone effilochée : les tiles disparaissent progressivement dans le vide
                if (_rng.Randf() < decay * 0.65f)
                    _erasedGrid[gx, gy] = true;
            }
        }
    }

    private void AssignBiomes(List<BiomeData> availableBiomes, int biomeCount)
    {
        if (availableBiomes == null || availableBiomes.Count == 0)
            return;

        biomeCount = Mathf.Clamp(biomeCount, 1, availableBiomes.Count);

        List<BiomeData> pool = new(availableBiomes);
        _activeBiomes.Clear();
        for (int i = 0; i < biomeCount && pool.Count > 0; i++)
        {
            int index = (int)(_rng.Randi() % pool.Count);
            _activeBiomes.Add(pool[index]);
            pool.RemoveAt(index);
        }

        CreateBiomeRegions();
        _biomeWarpNoiseX = CreateBiomeWarpNoise((int)_rng.Randi());
        _biomeWarpNoiseY = CreateBiomeWarpNoise((int)_rng.Randi());

        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                if (!_withinBounds[gx, gy])
                {
                    _biomeGrid[gx, gy] = 0;
                    continue;
                }

                int x = gx - _mapRadius;
                int y = gy - _mapRadius;
                Vector2 samplePoint = GetWarpedBiomeSample(new Vector2(x, y));
                _biomeGrid[gx, gy] = FindClosestRegionBiome(samplePoint);
            }
        }
    }

    /// <summary>
    /// Mosaïque de régions : centres espacés d'au moins RegionSpacing (tirage de Poisson),
    /// chaque biome revient plusieurs fois sans jamais toucher une région du même biome.
    /// Le spawn tombe près d'une frontière pour qu'on voie plusieurs biomes dès le départ.
    /// </summary>
    private void CreateBiomeRegions()
    {
        float spacing = Mathf.Max(8f, _biomeLayout.RegionSpacing);
        _regionBucketSize = spacing;
        _regionBucketCount = Mathf.CeilToInt(_size / spacing) + 1;
        _regionBuckets = new List<int>[_regionBucketCount, _regionBucketCount];
        _regionCenters.Clear();
        _regionBiomes.Clear();

        float spawnOffset = spacing * Mathf.Clamp(_biomeLayout.SpawnOffsetFactor, 0f, 0.5f);
        AddRegion(SampleBiomeSeed(spawnOffset * 0.8f, spawnOffset));

        float spacingSq = spacing * spacing;
        int failures = 0;
        while (failures < RegionPlacementMaxFailures)
        {
            Vector2 candidate = SampleBiomeSeed(0f, _mapRadius + spacing * 0.5f);
            if (NearestRegionDistanceSquared(candidate, 1) < spacingSq)
            {
                failures++;
                continue;
            }
            AddRegion(candidate);
            failures = 0;
        }

        int[] regionCounts = new int[_activeBiomes.Count];
        float neighbourRadiusSq = spacingSq * RegionNeighbourFactor * RegionNeighbourFactor;
        List<int> neighbourBiomes = new();
        for (int region = 0; region < _regionCenters.Count; region++)
        {
            neighbourBiomes.Clear();
            for (int other = 0; other < region; other++)
            {
                if (_regionCenters[region].DistanceSquaredTo(_regionCenters[other]) <= neighbourRadiusSq)
                    neighbourBiomes.Add(_regionBiomes[other]);
            }

            int bestBiome = 0;
            float bestScore = float.MaxValue;
            for (int biome = 0; biome < _activeBiomes.Count; biome++)
            {
                float weight = Mathf.Max(0.35f, _activeBiomes[biome].MapWeight);
                // Un voisin du même biome est fortement pénalisé ; à égalité, le biome le moins présent l'emporte.
                float score = (regionCounts[biome] + 1) / weight
                    + (neighbourBiomes.Contains(biome) ? RegionSameNeighbourPenalty : 0f)
                    + _rng.Randf() * 0.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestBiome = biome;
                }
            }
            _regionBiomes[region] = bestBiome;
            regionCounts[bestBiome]++;
        }
    }

    private void AddRegion(Vector2 center)
    {
        Vector2I bucket = RegionBucket(center);
        _regionBuckets[bucket.X, bucket.Y] ??= new List<int>();
        _regionBuckets[bucket.X, bucket.Y].Add(_regionCenters.Count);
        _regionCenters.Add(center);
        _regionBiomes.Add(0);
    }

    private Vector2I RegionBucket(Vector2 point)
    {
        int bx = Mathf.Clamp(Mathf.FloorToInt((point.X + _mapRadius) / _regionBucketSize), 0, _regionBucketCount - 1);
        int by = Mathf.Clamp(Mathf.FloorToInt((point.Y + _mapRadius) / _regionBucketSize), 0, _regionBucketCount - 1);
        return new Vector2I(bx, by);
    }

    private float NearestRegionDistanceSquared(Vector2 point, int bucketRange)
    {
        return NearestRegion(point, bucketRange, out float distanceSq) < 0 ? float.MaxValue : distanceSq;
    }

    private int NearestRegion(Vector2 point, int bucketRange, out float bestDistanceSq)
    {
        Vector2I bucket = RegionBucket(point);
        int bestRegion = -1;
        bestDistanceSq = float.MaxValue;
        for (int bx = bucket.X - bucketRange; bx <= bucket.X + bucketRange; bx++)
        {
            for (int by = bucket.Y - bucketRange; by <= bucket.Y + bucketRange; by++)
            {
                if (bx < 0 || by < 0 || bx >= _regionBucketCount || by >= _regionBucketCount)
                    continue;
                List<int> regions = _regionBuckets[bx, by];
                if (regions == null)
                    continue;
                foreach (int region in regions)
                {
                    float distanceSq = point.DistanceSquaredTo(_regionCenters[region]);
                    if (distanceSq < bestDistanceSq)
                    {
                        bestDistanceSq = distanceSq;
                        bestRegion = region;
                    }
                }
            }
        }
        return bestRegion;
    }

    private FastNoiseLite CreateBiomeWarpNoise(int seed)
    {
        FastNoiseLite noise = new();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = 0.018f;
        noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
        noise.FractalOctaves = 3;
        noise.Seed = seed;
        return noise;
    }

    private Vector2 SampleBiomeSeed(float minRadius, float maxRadius)
    {
        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float radius = Mathf.Lerp(minRadius, maxRadius, Mathf.Sqrt(_rng.Randf()));
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private Vector2 GetWarpedBiomeSample(Vector2 cellPosition)
    {
        float strength = _biomeLayout.WarpStrength;
        float warpX = _biomeWarpNoiseX.GetNoise2D(cellPosition.X, cellPosition.Y) * strength;
        float warpY = _biomeWarpNoiseY.GetNoise2D(cellPosition.X + 137f, cellPosition.Y - 211f) * strength;
        return new Vector2(cellPosition.X + warpX, cellPosition.Y + warpY);
    }

    private int FindClosestRegionBiome(Vector2 samplePoint)
    {
        if (_regionCenters.Count == 0)
            return 0;

        // Le tirage de Poisson laisse des trous inférieurs à deux espacements : deux cases de voisinage suffisent.
        int region = NearestRegion(samplePoint, 2, out _);
        if (region < 0)
            region = NearestRegion(samplePoint, _regionBucketCount, out _);
        return _regionBiomes[region];
    }

    private void SeedInitial()
    {
        bool hasBiomes = _activeBiomes.Count > 0;

        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                if (!_withinBounds[gx, gy])
                {
                    _grid[gx, gy] = TerrainType.Water;
                    continue;
                }

                int x = gx - _mapRadius;
                int y = gy - _mapRadius;
                float dist = Mathf.Sqrt(x * x + y * y);

                if (hasBiomes)
                {
                    BiomeData biome = _activeBiomes[_biomeGrid[gx, gy]];
                    _grid[gx, gy] = PickTerrainFromBiome(biome, dist);
                }
                else
                {
                    ZoneConfig zone = GetZoneForDistance(dist);
                    _grid[gx, gy] = PickTerrainWeighted(zone);
                }
            }
        }
    }

    private TerrainType PickTerrainFromBiome(BiomeData biome, float dist)
    {
        float proximityFade = Mathf.Clamp(1.0f - (dist / (_spawnClearance * 2.5f)), 0f, 1f);

        float grassW = biome.TerrainWeights.GetValueOrDefault("grass", 0.25f);
        float concreteW = biome.TerrainWeights.GetValueOrDefault("concrete", 0.25f);
        float waterW = biome.TerrainWeights.GetValueOrDefault("water", 0.1f);
        float forestW = biome.TerrainWeights.GetValueOrDefault("forest", 0.25f);

        grassW = Mathf.Lerp(grassW, 0.8f, proximityFade);
        waterW = Mathf.Lerp(waterW, 0f, proximityFade);

        float total = grassW + concreteW + waterW + forestW;
        if (total <= 0f) return TerrainType.Grass;

        float roll = _rng.Randf() * total;
        float cumulative = 0f;

        cumulative += grassW;
        if (roll < cumulative) return TerrainType.Grass;

        cumulative += concreteW;
        if (roll < cumulative) return TerrainType.Concrete;

        cumulative += waterW;
        if (roll < cumulative) return TerrainType.Water;

        return TerrainType.Forest;
    }

    private void SmoothPass()
    {
        TerrainType[,] next = new TerrainType[_size, _size];

        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                if (!_withinBounds[gx, gy])
                {
                    next[gx, gy] = TerrainType.Water;
                    continue;
                }

                int[] counts = new int[4];
                counts[(int)_grid[gx, gy]] += 2;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int nx = gx + dx;
                        int ny = gy + dy;

                        if (nx < 0 || ny < 0 || nx >= _size || ny >= _size)
                            continue;

                        if (!_withinBounds[nx, ny])
                        {
                            counts[(int)TerrainType.Water]++;
                            continue;
                        }

                        counts[(int)_grid[nx, ny]]++;
                    }
                }

                int bestType = 0;
                int bestCount = counts[0];
                for (int t = 1; t < 4; t++)
                {
                    if (counts[t] > bestCount)
                    {
                        bestType = t;
                        bestCount = counts[t];
                    }
                }

                next[gx, gy] = (TerrainType)bestType;
            }
        }

        _grid = next;
    }

    private void ClearSpawnArea()
    {
        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                int x = gx - _mapRadius;
                int y = gy - _mapRadius;

                if (Mathf.Abs(x) <= _spawnClearance && Mathf.Abs(y) <= _spawnClearance)
                    _grid[gx, gy] = TerrainType.Grass;
            }
        }
    }

    private void EnsureWaterConnectivity()
    {
        float fadeStart = _mapRadius - _edgeFadeWidth;

        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                if (!_withinBounds[gx, gy])
                    continue;

                if (_grid[gx, gy] != TerrainType.Water)
                    continue;

                int x = gx - _mapRadius;
                int y = gy - _mapRadius;
                float dist = Mathf.Sqrt(x * x + y * y);

                // Ne pas retirer l'eau de la zone de décomposition
                if (dist >= fadeStart)
                    continue;

                int waterNeighbors = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int nx = gx + dx;
                        int ny = gy + dy;

                        if (nx >= 0 && ny >= 0 && nx < _size && ny < _size
                            && _grid[nx, ny] == TerrainType.Water)
                        {
                            waterNeighbors++;
                        }
                    }
                }

                if (waterNeighbors < 2)
                    _grid[gx, gy] = TerrainType.Grass;
            }
        }
    }

    private ZoneConfig GetZoneForDistance(float dist)
    {
        foreach (ZoneConfig zone in _zones)
        {
            if (dist <= zone.MaxRadius)
                return zone;
        }
        return _zones[_zones.Count - 1];
    }

    private TerrainType PickTerrainWeighted(ZoneConfig zone)
    {
        float roll = _rng.Randf();
        float cumulative = 0f;

        cumulative += zone.GrassWeight;
        if (roll < cumulative) return TerrainType.Grass;

        cumulative += zone.ConcreteWeight;
        if (roll < cumulative) return TerrainType.Concrete;

        cumulative += zone.WaterWeight;
        if (roll < cumulative) return TerrainType.Water;

        return TerrainType.Forest;
    }
}
