using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Place les structures urbaines à l'échelle de l'îlot : quelques masses de
/// bâtiments lisibles, puis un mobilier de rue secondaire.
/// </summary>
public static class UrbanPropPlacer
{
	private static readonly string[] BuildingMassSprites = {
		"assets/props/urban_ruins/prop_collapsed_building.png",
		"assets/props/urban_ruins/prop_building_facade_wide.png",
		"assets/props/urban_ruins/prop_building_corner_large.png",
		"assets/props/urban_ruins/prop_building_tower_chunk.png",
	};

	private static readonly string[] RoadSprites = {
		"assets/props/urban_ruins/prop_urban_car.png",
		"assets/props/urban_ruins/prop_concrete_debris.png",
		"assets/props/urban_ruins/prop_concrete_debris_v2.png",
	};

	private static readonly string[] SidewalkIntersectionSprites = {
		"assets/props/urban_ruins/prop_traffic_light.png",
		"assets/props/urban_ruins/prop_phone_booth.png",
		"assets/props/urban_ruins/prop_mailbox.png",
	};

	private static readonly string[] SidewalkEdgeSprites = {
		"assets/props/urban_ruins/prop_dumpster.png",
		"assets/props/urban_ruins/prop_dumpster_v2.png",
		"assets/props/urban_ruins/prop_chain_link_fence.png",
		"assets/props/urban_ruins/prop_torn_billboard.png",
		"assets/props/urban_ruins/prop_graffiti_wall.png",
	};

	private static readonly string[] InteriorDebrisSprites = {
		"assets/props/urban_ruins/prop_concrete_debris.png",
		"assets/props/urban_ruins/prop_concrete_debris_v2.png",
		"assets/props/urban_ruins/prop_concrete_debris_v3.png",
		"assets/props/urban_ruins/prop_steel_beam.png",
		"assets/props/urban_ruins/prop_steel_beam_diagonal.png",
	};

	private static readonly Dictionary<string, float> CollisionRadii = new()
	{
		{ "prop_collapsed_building.png", 18f },
		{ "prop_building_facade_wide.png", 24f },
		{ "prop_building_corner_large.png", 26f },
		{ "prop_building_tower_chunk.png", 20f },
		{ "prop_urban_car.png", 12f },
		{ "prop_concrete_debris.png", 0f },
		{ "prop_concrete_debris_v2.png", 0f },
		{ "prop_concrete_debris_v3.png", 0f },
		{ "prop_steel_beam.png", 0f },
		{ "prop_steel_beam_diagonal.png", 0f },
		{ "prop_traffic_light.png", 3f },
		{ "prop_phone_booth.png", 4f },
		{ "prop_mailbox.png", 0f },
		{ "prop_dumpster.png", 4f },
		{ "prop_dumpster_v2.png", 4f },
		{ "prop_chain_link_fence.png", 6f },
		{ "prop_torn_billboard.png", 5f },
		{ "prop_graffiti_wall.png", 6f },
	};

	public static void PlaceProps(
		UrbanLayout layout,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		ulong seed)
	{
		Dictionary<string, Texture2D> cache = new();
		HashSet<Vector2I> intersections = FindIntersections(layout);

		int buildingCount = PlaceBuildingMasses(layout, ground, container, usedCells, cache, seed);
		int roadCount = PlaceRoadProps(layout, intersections, ground, container, usedCells, cache, seed);
		int sidewalkCount = PlaceSidewalkProps(layout, intersections, ground, container, usedCells, cache, seed);

		GD.Print($"[UrbanPropPlacer] Placed {buildingCount} building masses, {roadCount} road props, {sidewalkCount} sidewalk props");
	}

	private static int PlaceBuildingMasses(
		UrbanLayout layout,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		ulong seed)
	{
		List<BuildingFootprint> buildings = new(layout.Buildings);
		buildings.Sort((left, right) =>
		{
			int leftArea = left.Size.X * left.Size.Y;
			int rightArea = right.Size.X * right.Size.Y;
			return rightArea.CompareTo(leftArea);
		});

		int placed = 0;
		foreach (BuildingFootprint building in buildings)
		{
			int area = building.Size.X * building.Size.Y;
			if (area < 48)
				continue;

			uint hash = HashCell(building.Origin, seed ^ (ulong)(area * 17));
			Vector2I primaryAnchor = GetPrimaryAnchor(building);
			string primarySprite = PickBuildingSprite(building, area, hash);

			if (TryPlaceProp(primarySprite, primaryAnchor, ground, container, usedCells, cache))
				placed++;

			if (area >= 128)
			{
				Vector2I secondaryAnchor = GetSecondaryAnchor(building, hash);
				string secondarySprite = PickSecondarySprite(building, area, hash);
				if (!usedCells.Contains(secondaryAnchor)
					&& TryPlaceProp(secondarySprite, secondaryAnchor, ground, container, usedCells, cache))
				{
					placed++;
				}
			}

			if (building.Integrity < 0.52f)
			{
				PlaceInteriorDebris(building, ground, container, usedCells, cache, seed ^ hash, ref placed);
			}

			ReserveBuildingFootprint(building, usedCells, inset: 1);
		}

		return placed;
	}

	private static int PlaceRoadProps(
		UrbanLayout layout,
		HashSet<Vector2I> intersections,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		ulong seed)
	{
		int placed = 0;
		foreach (Vector2I roadCell in layout.RoadCells)
		{
			if (usedCells.Contains(roadCell))
				continue;
			if (!IsStraightRoadCell(layout, roadCell))
				continue;
			if (IsNearIntersection(intersections, roadCell))
				continue;

			uint hash = HashCell(roadCell, seed ^ 0x1A2B3CUL);
			int roll = (int)(hash % 100);
			if (roll >= 8)
				continue;

			string sprite = roll < 3
				? RoadSprites[0]
				: RoadSprites[1 + (int)(hash % 2)];

			if (TryPlaceProp(sprite, roadCell, ground, container, usedCells, cache))
				placed++;
		}

		return placed;
	}

	private static int PlaceSidewalkProps(
		UrbanLayout layout,
		HashSet<Vector2I> intersections,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		ulong seed)
	{
		int placed = 0;
		foreach (Vector2I cell in layout.SidewalkCells)
		{
			if (usedCells.Contains(cell))
				continue;

			bool nearIntersection = IsNearIntersection(intersections, cell);
			bool nearBuilding = IsNearBuilding(layout, cell);
			if (!nearIntersection && !nearBuilding)
				continue;

			uint hash = HashCell(cell, seed ^ 0xCAFEBABEUL);
			int roll = (int)(hash % 100);

			string sprite = null;
			if (nearIntersection && roll < 14)
				sprite = SidewalkIntersectionSprites[(int)(hash % (uint)SidewalkIntersectionSprites.Length)];
			else if (nearBuilding && roll < 10)
				sprite = SidewalkEdgeSprites[(int)(hash % (uint)SidewalkEdgeSprites.Length)];

			if (sprite != null && TryPlaceProp(sprite, cell, ground, container, usedCells, cache))
				placed++;
		}

		return placed;
	}

	private static void PlaceInteriorDebris(
		BuildingFootprint building,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		ulong seed,
		ref int placed)
	{
		int quota = Mathf.Clamp((building.Size.X * building.Size.Y) / 45, 1, 2);
		for (int i = 0; i < quota; i++)
		{
			int x = building.Origin.X + 1 + (int)((seed + (ulong)(i * 37)) % (ulong)Mathf.Max(1, building.Size.X - 2));
			int y = building.Origin.Y + 1 + (int)(((seed >> 3) + (ulong)(i * 53)) % (ulong)Mathf.Max(1, building.Size.Y - 2));
			Vector2I cell = new(x, y);
			if (usedCells.Contains(cell))
				continue;

			string sprite = InteriorDebrisSprites[(int)((seed + (ulong)i) % (ulong)InteriorDebrisSprites.Length)];
			if (TryPlaceProp(sprite, cell, ground, container, usedCells, cache))
				placed++;
		}
	}

	private static string PickBuildingSprite(BuildingFootprint building, int area, uint hash)
	{
		bool wide = building.Size.X >= building.Size.Y + 2;
		bool tall = building.Size.Y >= building.Size.X + 2;

		if (area >= 110)
			return wide ? BuildingMassSprites[1] : BuildingMassSprites[2];
		if (area >= 72)
			return tall ? BuildingMassSprites[3] : BuildingMassSprites[1];
		if (area >= 48)
			return (hash % 100) < 35 ? BuildingMassSprites[2] : BuildingMassSprites[0];
		return BuildingMassSprites[0];
	}

	private static string PickSecondarySprite(BuildingFootprint building, int area, uint hash)
	{
		if (area >= 120 && building.Size.Y > building.Size.X)
			return BuildingMassSprites[3];
		return (hash % 100) < 50 ? BuildingMassSprites[2] : BuildingMassSprites[0];
	}

	private static Vector2I GetPrimaryAnchor(BuildingFootprint building)
	{
		int x = building.Origin.X + building.Size.X / 2;
		int y = building.Origin.Y + building.Size.Y - 1;
		return new Vector2I(x, y);
	}

	private static Vector2I GetSecondaryAnchor(BuildingFootprint building, uint hash)
	{
		bool favorRight = (hash % 100) < 50;
		if (favorRight)
		{
			int x = building.Origin.X + building.Size.X - 2;
			int y = building.Origin.Y + building.Size.Y / 2;
			return new Vector2I(x, y);
		}

		int altX = building.Origin.X + 1;
		int altY = building.Origin.Y + building.Size.Y / 2;
		return new Vector2I(altX, altY);
	}

	private static void ReserveBuildingFootprint(BuildingFootprint building, HashSet<Vector2I> usedCells, int inset)
	{
		int minX = building.Origin.X + inset;
		int maxX = building.Origin.X + building.Size.X - 1 - inset;
		int minY = building.Origin.Y + inset;
		int maxY = building.Origin.Y + building.Size.Y - 1 - inset;

		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
				usedCells.Add(new Vector2I(x, y));
		}
	}

	private static bool TryPlaceProp(
		string spritePath,
		Vector2I cell,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache)
	{
		if (usedCells.Contains(cell))
			return false;

		Texture2D texture = LoadCached(spritePath, cache);
		if (texture == null)
			return false;

		EnvironmentProp prop = new();
		prop.GlobalPosition = ground.MapToLocal(cell);
		container.AddChild(prop);
		prop.Initialize(texture, null, 0f, GetCollisionRadius(spritePath), 0f);

		usedCells.Add(cell);
		return true;
	}

	private static bool IsNearBuilding(UrbanLayout layout, Vector2I cell)
	{
		foreach (Vector2I dir in new[] {
			Vector2I.Up,
			Vector2I.Down,
			Vector2I.Left,
			Vector2I.Right,
			new Vector2I(1, 1),
			new Vector2I(1, -1),
			new Vector2I(-1, 1),
			new Vector2I(-1, -1)
		})
		{
			Vector2I neighbor = cell + dir;
			if (layout.WallCells.Contains(neighbor))
				return true;

			UrbanCellType neighborType = GetCellType(layout, neighbor);
			if (neighborType == UrbanCellType.BuildingInterior || neighborType == UrbanCellType.BuildingWall)
				return true;
		}

		return false;
	}

	private static UrbanCellType GetCellType(UrbanLayout layout, Vector2I cell)
	{
		int gx = cell.X + layout.MapRadius;
		int gy = cell.Y + layout.MapRadius;
		if (gx < 0 || gy < 0 || gx >= layout.CellGrid.GetLength(0) || gy >= layout.CellGrid.GetLength(1))
			return UrbanCellType.None;

		return layout.CellGrid[gx, gy];
	}

	private static bool IsStraightRoadCell(UrbanLayout layout, Vector2I cell)
	{
		bool north = layout.RoadCells.Contains(cell + Vector2I.Up);
		bool south = layout.RoadCells.Contains(cell + Vector2I.Down);
		bool west = layout.RoadCells.Contains(cell + Vector2I.Left);
		bool east = layout.RoadCells.Contains(cell + Vector2I.Right);

		bool straightVertical = north && south && !west && !east;
		bool straightHorizontal = west && east && !north && !south;
		return straightVertical || straightHorizontal;
	}

	private static bool IsNearIntersection(HashSet<Vector2I> intersections, Vector2I cell)
	{
		foreach (Vector2I dir in new[] {
			Vector2I.Zero,
			Vector2I.Up,
			Vector2I.Down,
			Vector2I.Left,
			Vector2I.Right,
			new Vector2I(1, 1),
			new Vector2I(1, -1),
			new Vector2I(-1, 1),
			new Vector2I(-1, -1)
		})
		{
			if (intersections.Contains(cell + dir))
				return true;
		}

		return false;
	}

	private static HashSet<Vector2I> FindIntersections(UrbanLayout layout)
	{
		HashSet<Vector2I> intersections = new();
		foreach (Vector2I roadCell in layout.RoadCells)
		{
			int adjacent = 0;
			foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
			{
				if (layout.RoadCells.Contains(roadCell + dir))
					adjacent++;
			}

			if (adjacent >= 3)
				intersections.Add(roadCell);
		}

		return intersections;
	}

	private static float GetCollisionRadius(string path)
	{
		int lastSlash = path.LastIndexOf('/');
		string filename = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
		return CollisionRadii.TryGetValue(filename, out float radius) ? radius : 0f;
	}

	private static Texture2D LoadCached(string path, Dictionary<string, Texture2D> cache)
	{
		if (cache.TryGetValue(path, out Texture2D cached))
			return cached;

		string resPath = path.StartsWith("res://") ? path : $"res://{path}";
		if (!ResourceLoader.Exists(resPath))
		{
			GD.PrintErr($"[UrbanPropPlacer] Texture NOT FOUND: {resPath}");
			cache[path] = null;
			return null;
		}

		Texture2D texture = GD.Load<Texture2D>(resPath);
		cache[path] = texture;
		return texture;
	}

	private static uint HashCell(Vector2I cell, ulong salt)
	{
		ulong value = (ulong)((cell.X * 73856093) ^ (cell.Y * 19349663));
		value ^= salt;
		return (uint)(value & 0x7FFFFFFF);
	}
}
