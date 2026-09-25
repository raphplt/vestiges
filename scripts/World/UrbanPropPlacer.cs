using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Place les structures urbaines avec une logique de ville réaliste :
/// bâtiments alignés sur les rues, coins marqués, landmarks rares,
/// dégradation progressive, antennes isolées.
/// </summary>
public static class UrbanPropPlacer
{
	// --- Sprite categories ---

	private static readonly string[] RoadDebrisSprites = {
		"assets/props/urban_ruins/prop_concrete_debris.png",
		"assets/props/urban_ruins/prop_concrete_debris_v2.png",
	};

	// Voitures alignées sur l'axe de leur route : x pour une rue est-ouest (cellules), y pour une rue nord-sud.
	private static readonly string[] CarSpritesAxisX = {
		"assets/props/urban_ruins/prop_urban_car_red_x.png",
		"assets/props/urban_ruins/prop_urban_car_teal_x.png",
		"assets/props/urban_ruins/prop_urban_car_cream_x.png",
	};

	private static readonly string[] CarSpritesAxisY = {
		"assets/props/urban_ruins/prop_urban_car_red_y.png",
		"assets/props/urban_ruins/prop_urban_car_teal_y.png",
		"assets/props/urban_ruins/prop_urban_car_cream_y.png",
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
	};

	private static readonly string[] InteriorDebrisSprites = {
		"assets/props/urban_ruins/prop_concrete_debris.png",
		"assets/props/urban_ruins/prop_concrete_debris_v2.png",
		"assets/props/urban_ruins/prop_concrete_debris_v3.png",
		"assets/props/urban_ruins/prop_steel_beam.png",
		"assets/props/urban_ruins/prop_steel_beam_diagonal.png",
	};

	// Décors qui bloquent le passage ; la forme vient de leur base visible (PropFootprint).
	private static readonly HashSet<string> BlockingSprites = new()
	{
		"prop_urban_car.png",
		"prop_urban_car_red_x.png",
		"prop_urban_car_teal_x.png",
		"prop_urban_car_cream_x.png",
		"prop_urban_car_red_y.png",
		"prop_urban_car_teal_y.png",
		"prop_urban_car_cream_y.png",
		"prop_traffic_light.png",
		"prop_phone_booth.png",
		"prop_dumpster.png",
		"prop_dumpster_v2.png",
		"prop_chain_link_fence.png",
		"prop_torn_billboard.png",
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

		int buildingCount = UrbanBuildingPlacer.Place(layout, ground, container, usedCells, cache, seed);
		// Décors de rue à l'échelle du personnage : au moins une cellule libre entre deux d'entre eux.
		HashSet<Vector2I> streetProps = new();
		int roadCount = PlaceRoadProps(layout, intersections, ground, container, usedCells, streetProps, cache, seed);
		int sidewalkCount = PlaceSidewalkProps(layout, intersections, ground, container, usedCells, streetProps, cache, seed);

		GD.Print($"[UrbanPropPlacer] Placed {buildingCount} building masses, {roadCount} road props, {sidewalkCount} sidewalk props");
	}

	// =========================================================================
	// ROAD PROPS
	// =========================================================================

	private static int PlaceRoadProps(
		UrbanLayout layout,
		HashSet<Vector2I> intersections,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		HashSet<Vector2I> streetProps,
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
			if (roll >= 15)
				continue;

			string sprite;
			if (roll < 3)
			{
				string[] cars = layout.RoadCells.Contains(roadCell + Vector2I.Right) ? CarSpritesAxisX : CarSpritesAxisY;
				sprite = cars[(int)((hash >> 8) % (uint)cars.Length)];
			}
			else
			{
				sprite = RoadDebrisSprites[(int)(hash % 2)];
			}

			if (IsCrowded(streetProps, roadCell))
				continue;
			if (TryPlaceProp(sprite, roadCell, ground, container, usedCells, cache))
			{
				streetProps.Add(roadCell);
				placed++;
			}
		}

		return placed;
	}

	// =========================================================================
	// SIDEWALK PROPS
	// =========================================================================

	private static int PlaceSidewalkProps(
		UrbanLayout layout,
		HashSet<Vector2I> intersections,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		HashSet<Vector2I> streetProps,
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
			if (nearIntersection && roll < 20)
				sprite = SidewalkIntersectionSprites[(int)(hash % (uint)SidewalkIntersectionSprites.Length)];
			else if (nearBuilding && roll < 9)
				sprite = SidewalkEdgeSprites[(int)(hash % (uint)SidewalkEdgeSprites.Length)];

			if (sprite == null || IsCrowded(streetProps, cell))
				continue;
			if (TryPlaceProp(sprite, cell, ground, container, usedCells, cache))
			{
				streetProps.Add(cell);
				placed++;
			}
		}

		return placed;
	}

	// =========================================================================
	// INTERIOR DEBRIS
	// =========================================================================

	internal static void PlaceInteriorDebris(
		BuildingFootprint building,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		ulong seed,
		ref int placed)
	{
		int area = building.Size.X * building.Size.Y;
		// Scale debris with area and damage
		int quota = Mathf.Clamp(area / 20, 1, 6);
		if (building.Integrity < 0.50f)
			quota += 2;
		if (building.Integrity < 0.35f)
			quota += 2;

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

	// =========================================================================
	// HELPERS
	// =========================================================================

	internal static void ReserveBuildingFootprint(BuildingFootprint building, HashSet<Vector2I> usedCells, int inset)
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

	internal static bool TryPlaceProp(
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
		prop.Initialize(texture, null, 0f, IsBlocking(spritePath));

		usedCells.Add(cell);
		return true;
	}

	private static bool IsCrowded(HashSet<Vector2I> streetProps, Vector2I cell)
	{
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				if (streetProps.Contains(new Vector2I(cell.X + dx, cell.Y + dy)))
					return true;
			}
		}
		return false;
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

	internal static UrbanCellType GetCellType(UrbanLayout layout, Vector2I cell)
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

	private static bool IsBlocking(string path)
	{
		int slash = path.LastIndexOf('/');
		string file = slash >= 0 ? path[(slash + 1)..] : path;
		return file.StartsWith(UrbanBuildingPlacer.SpritePrefix) || BlockingSprites.Contains(file);
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

	internal static uint HashCell(Vector2I cell, ulong salt)
	{
		return CellHash.Of(cell.X, cell.Y, salt);
	}
}
