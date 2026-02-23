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
/// La carte est divisée en secteurs angulaires, chaque secteur assigné à un biome.
/// Les bords se décomposent progressivement : la réalité s'effiloche puis cesse.
/// Seed déterministe : même seed = même monde, mêmes biomes.
/// </summary>
public class WorldGenerator
{
    private readonly int _mapRadius;
    private readonly int _foyerClearance;
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
    private float[] _sectorBoundaries;

    private const float BoundaryNoiseAmplitude = 0.25f;
    private const float BoundaryNoiseFrequency = 3.0f;

    public int MapRadius => _mapRadius;
    public List<BiomeData> ActiveBiomes => _activeBiomes;

    public struct ZoneConfig
    {
        public float MaxRadius;
        public float GrassWeight;
        public float ConcreteWeight;
        public float WaterWeight;
        public float ForestWeight;
    }

    public WorldGenerator(int mapRadius, int foyerClearance, int caIterations, List<ZoneConfig> zones, ulong seed, int edgeFadeWidth = 5)
    {
        _mapRadius = mapRadius;
        _foyerClearance = foyerClearance;
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

        ClearFoyerArea();
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

        ClearFoyerArea();
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

        biomeCount = Mathf.Clamp(biomeCount, 2, Mathf.Min(availableBiomes.Count, 4));

        List<BiomeData> pool = new(availableBiomes);
        _activeBiomes.Clear();
        for (int i = 0; i < biomeCount && pool.Count > 0; i++)
        {
            int index = (int)(_rng.Randi() % pool.Count);
            _activeBiomes.Add(pool[index]);
            pool.RemoveAt(index);
        }

        float startAngle = _rng.Randf() * Mathf.Tau;
        float sectorSize = Mathf.Tau / biomeCount;

        _sectorBoundaries = new float[biomeCount];
        for (int i = 0; i < biomeCount; i++)
            _sectorBoundaries[i] = startAngle + sectorSize * i;

        float noisePhase = _rng.Randf() * Mathf.Tau;

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
                float dist = Mathf.Sqrt(x * x + y * y);

                if (dist < 0.5f)
                {
                    _biomeGrid[gx, gy] = 0;
                    continue;
                }

                float angle = Mathf.Atan2(y, x);
                if (angle < 0) angle += Mathf.Tau;

                float noise = Mathf.Sin(dist * BoundaryNoiseFrequency * 0.1f + noisePhase) * BoundaryNoiseAmplitude;
                float perturbedAngle = angle + noise;
                if (perturbedAngle < 0) perturbedAngle += Mathf.Tau;
                if (perturbedAngle >= Mathf.Tau) perturbedAngle -= Mathf.Tau;

                _biomeGrid[gx, gy] = GetSectorIndex(perturbedAngle);
            }
        }
    }

    private int GetSectorIndex(float angle)
    {
        int count = _activeBiomes.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            float boundary = _sectorBoundaries[i] % Mathf.Tau;
            float nextBoundary = _sectorBoundaries[(i + 1) % count] % Mathf.Tau;

            if (nextBoundary < boundary)
            {
                if (angle >= boundary || angle < nextBoundary)
                    return i;
            }
            else
            {
                if (angle >= boundary && angle < nextBoundary)
                    return i;
            }
        }
        return 0;
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
        float proximityFade = Mathf.Clamp(1.0f - (dist / (_foyerClearance * 2.5f)), 0f, 1f);

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

    private void ClearFoyerArea()
    {
        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                int x = gx - _mapRadius;
                int y = gy - _mapRadius;

                if (Mathf.Abs(x) <= _foyerClearance && Mathf.Abs(y) <= _foyerClearance)
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
