using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

public enum WildFieldCellType
{
	None = 0,
	Meadow,
	Wheat,
	Fallow,
	Path,
}

public class WildFieldsLayout
{
	public WildFieldCellType[,] CellGrid;
	public HashSet<Vector2I> PathCells = new();
	public HashSet<Vector2I> WheatCells = new();
	public HashSet<Vector2I> FallowCells = new();
	public HashSet<Vector2I> MeadowCells = new();
	public int MapRadius;
}

/// <summary>
/// Structure le biome des champs en grandes parcelles lisibles :
/// allées de terre, blé dense, jachères et prairies de lisière.
/// Le terrain est muté in-place pour que les chemins existent réellement.
/// </summary>
public class WildFieldsLayoutGenerator
{
	private readonly int _mapRadius;
	private readonly int _size;
	private readonly ulong _seed;

	private const int ParcelWidth = 13;
	private const int ParcelHeight = 10;
	private const int PrimaryLaneModulo = 13;
	private const int SecondaryLaneModulo = 21;

	public WildFieldsLayoutGenerator(ulong seed, int mapRadius)
	{
		_seed = seed ^ 0x71A1D5UL;
		_mapRadius = mapRadius;
		_size = mapRadius * 2 + 1;
	}

	public WildFieldsLayout Apply(TerrainType[,] terrain, WorldGenerator generator, string biomeId)
	{
		WildFieldsLayout layout = new()
		{
			CellGrid = new WildFieldCellType[_size, _size],
			MapRadius = _mapRadius,
		};
		TerrainType[,] baseTerrain = (TerrainType[,])terrain.Clone();

		int wildCellCount = 0;
		for (int x = -_mapRadius; x <= _mapRadius; x++)
		{
			for (int y = -_mapRadius; y <= _mapRadius; y++)
			{
				if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
					continue;
				if (generator.GetBiomeId(x, y) != biomeId)
					continue;
				if (terrain[x + _mapRadius, y + _mapRadius] == TerrainType.Water)
					continue;

				wildCellCount++;
			}
		}

		if (wildCellCount < 80)
		{
			GD.Print($"[WildFieldsLayout] Too few wild field cells ({wildCellCount}), skipping layout");
			return layout;
		}

		for (int x = -_mapRadius; x <= _mapRadius; x++)
		{
			for (int y = -_mapRadius; y <= _mapRadius; y++)
			{
				if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
					continue;
				if (generator.GetBiomeId(x, y) != biomeId)
					continue;

				int gx = x + _mapRadius;
				int gy = y + _mapRadius;
				TerrainType current = terrain[gx, gy];
				if (current == TerrainType.Water)
					continue;

				if (current == TerrainType.Forest)
				{
					layout.CellGrid[gx, gy] = WildFieldCellType.None;
					continue;
				}

				if (IsPathCell(x, y, generator, baseTerrain))
				{
					terrain[gx, gy] = TerrainType.Concrete;
					layout.PathCells.Add(new Vector2I(x, y));
					layout.CellGrid[gx, gy] = WildFieldCellType.Path;
					continue;
				}

				WildFieldCellType fieldType = PickFieldType(x, y, generator, baseTerrain);
				layout.CellGrid[gx, gy] = fieldType;

				switch (fieldType)
				{
					case WildFieldCellType.Wheat:
						terrain[gx, gy] = TerrainType.Grass;
						layout.WheatCells.Add(new Vector2I(x, y));
						break;
					case WildFieldCellType.Fallow:
						terrain[gx, gy] = TerrainType.Grass;
						layout.FallowCells.Add(new Vector2I(x, y));
						break;
					default:
						terrain[gx, gy] = TerrainType.Grass;
						layout.MeadowCells.Add(new Vector2I(x, y));
						break;
				}
			}
		}

		GD.Print($"[WildFieldsLayout] Layout complete: wheat={layout.WheatCells.Count}, fallow={layout.FallowCells.Count}, meadow={layout.MeadowCells.Count}, paths={layout.PathCells.Count}");
		return layout;
	}

	private bool IsPathCell(int x, int y, WorldGenerator generator, TerrainType[,] terrain)
	{
		bool primaryLane;
		bool secondaryLane;
		ComputePathAxes(x, y, out primaryLane, out secondaryLane);

		if (!primaryLane && !secondaryLane)
			return false;

		int edgeFactor = CountNearbyTerrain(generator, terrain, x, y, TerrainType.Forest, 1)
			+ CountNearbyTerrain(generator, terrain, x, y, TerrainType.Water, 1) * 2;

		if (edgeFactor >= 4 && !primaryLane)
			return false;

		return true;
	}

	private WildFieldCellType PickFieldType(int x, int y, WorldGenerator generator, TerrainType[,] terrain)
	{
		if (TouchesPath(x, y, generator, terrain))
			return WildFieldCellType.Meadow;

		int roughness = CountNearbyTerrain(generator, terrain, x, y, TerrainType.Forest, 1)
			+ CountNearbyTerrain(generator, terrain, x, y, TerrainType.Water, 2);
		if (roughness >= 3)
			return WildFieldCellType.Meadow;

		float u = x + y * 0.55f;
		float v = y - x * 0.35f;
		int parcelU = Mathf.FloorToInt(u / ParcelWidth);
		int parcelV = Mathf.FloorToInt(v / ParcelHeight);
		uint parcelHash = HashCell(parcelU, parcelV, _seed);
		int roll = (int)(parcelHash % 100);

		if (roll < 50)
			return WildFieldCellType.Wheat;
		if (roll < 72)
			return WildFieldCellType.Fallow;
		return WildFieldCellType.Meadow;
	}

	private bool TouchesPath(int x, int y, WorldGenerator generator, TerrainType[,] terrain)
	{
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				if (dx == 0 && dy == 0)
					continue;
				if (IsPathCell(x + dx, y + dy, generator, terrain))
					return true;
			}
		}

		return false;
	}

	private void ComputePathAxes(int x, int y, out bool primaryLane, out bool secondaryLane)
	{
		float u = x + y * 0.55f;
		float v = y - x * 0.35f;
		int iu = Mathf.FloorToInt(u);
		int iv = Mathf.FloorToInt(v);
		int parcelU = Mathf.FloorToInt(u / ParcelWidth);
		int parcelV = Mathf.FloorToInt(v / ParcelHeight);

		int localPrimary = PositiveMod(iu, PrimaryLaneModulo);
		primaryLane = localPrimary == 0;

		uint parcelHash = HashCell(parcelU, parcelV, _seed ^ 0x1445UL);
		bool allowSecondary = parcelHash % 100 < 62;
		int localSecondary = PositiveMod(iv + (int)(parcelHash % 3), SecondaryLaneModulo);
		secondaryLane = allowSecondary && localSecondary == 0;
	}

	private int CountNearbyTerrain(WorldGenerator generator, TerrainType[,] terrainGrid, int x, int y, TerrainType terrain, int radius)
	{
		int count = 0;
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				if (dx == 0 && dy == 0)
					continue;
				int nx = x + dx;
				int ny = y + dy;
				if (!generator.IsWithinBounds(nx, ny) || generator.IsErased(nx, ny))
					continue;
				if (terrainGrid[nx + _mapRadius, ny + _mapRadius] == terrain)
					count++;
			}
		}

		return count;
	}

	private static int PositiveMod(int value, int modulo)
	{
		int result = value % modulo;
		return result < 0 ? result + modulo : result;
	}

	private static uint HashCell(int x, int y, ulong salt)
	{
		ulong value = (ulong)((x * 73856093) ^ (y * 19349663));
		value ^= salt;
		return (uint)(value & 0x7FFFFFFF);
	}

}
