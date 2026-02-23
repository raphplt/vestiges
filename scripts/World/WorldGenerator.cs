using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

public enum TerrainType
{
    Grass = 0,
    Concrete = 1,
    Water = 2,
    Forest = 3
}

/// <summary>
/// Génère une carte procédurale à base de Cellular Automata.
/// Zones concentriques autour du Foyer influencent la distribution des terrains.
/// Seed déterministe : même seed = même monde.
/// </summary>
public class WorldGenerator
{
    private readonly int _mapRadius;
    private readonly int _foyerClearance;
    private readonly int _caIterations;
    private readonly List<ZoneConfig> _zones;
    private readonly RandomNumberGenerator _rng;
    private TerrainType[,] _grid;
    private int _size;

    public int MapRadius => _mapRadius;

    public struct ZoneConfig
    {
        public float MaxRadius;
        public float GrassWeight;
        public float ConcreteWeight;
        public float WaterWeight;
        public float ForestWeight;
    }

    public WorldGenerator(int mapRadius, int foyerClearance, int caIterations, List<ZoneConfig> zones, ulong seed)
    {
        _mapRadius = mapRadius;
        _foyerClearance = foyerClearance;
        _caIterations = caIterations;
        _zones = zones;

        _rng = new RandomNumberGenerator();
        _rng.Seed = seed;

        _size = mapRadius * 2 + 1;
        _grid = new TerrainType[_size, _size];
    }

    public TerrainType[,] Generate()
    {
        SeedInitial();

        for (int i = 0; i < _caIterations; i++)
            SmoothPass();

        ClearFoyerArea();
        EnsureWaterConnectivity();

        GD.Print($"[WorldGenerator] Generated {_size}x{_size} map with {_caIterations} CA passes");
        return _grid;
    }

    public TerrainType GetTerrain(int x, int y)
    {
        int gx = x + _mapRadius;
        int gy = y + _mapRadius;
        if (gx < 0 || gy < 0 || gx >= _size || gy >= _size)
            return TerrainType.Grass;
        return _grid[gx, gy];
    }

    /// <summary>
    /// Checks if a map cell is walkable (not water).
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        return GetTerrain(x, y) != TerrainType.Water;
    }

    private void SeedInitial()
    {
        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                int x = gx - _mapRadius;
                int y = gy - _mapRadius;
                float dist = Mathf.Sqrt(x * x + y * y);

                ZoneConfig zone = GetZoneForDistance(dist);
                _grid[gx, gy] = PickTerrainWeighted(zone);
            }
        }
    }

    private void SmoothPass()
    {
        TerrainType[,] next = new TerrainType[_size, _size];

        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                int[] counts = new int[4];
                counts[(int)_grid[gx, gy]] += 2; // self counts double

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

                        counts[(int)_grid[nx, ny]]++;
                    }
                }

                // Pick the type with the highest neighbor count
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

    /// <summary>
    /// Remove isolated single water tiles that look unnatural.
    /// A water cell with fewer than 2 water neighbors becomes grass.
    /// </summary>
    private void EnsureWaterConnectivity()
    {
        for (int gx = 0; gx < _size; gx++)
        {
            for (int gy = 0; gy < _size; gy++)
            {
                if (_grid[gx, gy] != TerrainType.Water)
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
