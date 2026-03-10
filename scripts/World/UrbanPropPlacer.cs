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

	// Large/tall buildings for street-facing facades
	private static readonly string[] FacadeSprites = {
		"assets/props/urban_ruins/prop_building_facade_wide.png",     // 0 - wide facade
		"assets/props/urban_ruins/prop_building_apartment_block.png", // 1 - long apartment
		"assets/props/urban_ruins/prop_building_shopfront_row.png",   // 2 - shopfront row
	};

	// Corner buildings (placed at block corners facing intersections)
	private static readonly string[] CornerSprites = {
		"assets/props/urban_ruins/prop_building_corner_large.png",    // 0 - corner block
		"assets/props/urban_ruins/prop_building_tower_chunk.png",     // 1 - tower fragment
	};

	// Collapsed/damaged fill (interior or low-integrity blocks)
	private static readonly string[] RubbleSprites = {
		"assets/props/urban_ruins/prop_collapsed_building.png",
	};

	// Rare landmarks (max 1 per map)
	private static readonly string[] LandmarkSprites = {
		"assets/props/urban_ruins/prop_building_church_ruin.png",
	};

	// Standalone tall structure (max 1-2 per map, placed on sidewalk near large buildings)
	private const string AntennaSprite = "assets/props/urban_ruins/prop_radio_antenna_tower.png";

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
		{ "prop_building_apartment_block.png", 28f },
		{ "prop_building_church_ruin.png", 22f },
		{ "prop_building_shopfront_row.png", 26f },
		{ "prop_radio_antenna_tower.png", 8f },
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
	};

	// WallMask bits
	private const int WallTop = 1;
	private const int WallRight = 2;
	private const int WallBottom = 4;
	private const int WallLeft = 8;

	public static void PlaceProps(
		UrbanLayout layout,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		ulong seed)
	{
		Dictionary<string, Texture2D> cache = new();
		HashSet<Vector2I> intersections = FindIntersections(layout);

		int buildingCount = PlaceBuildingMasses(layout, intersections, ground, container, usedCells, cache, seed);
		int roadCount = PlaceRoadProps(layout, intersections, ground, container, usedCells, cache, seed);
		int sidewalkCount = PlaceSidewalkProps(layout, intersections, ground, container, usedCells, cache, seed);

		GD.Print($"[UrbanPropPlacer] Placed {buildingCount} building masses, {roadCount} road props, {sidewalkCount} sidewalk props");
	}

	// =========================================================================
	// BUILDING MASSES — street-aligned placement
	// =========================================================================

	private static int PlaceBuildingMasses(
		UrbanLayout layout,
		HashSet<Vector2I> intersections,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		ulong seed)
	{
		// Sort buildings: largest first so they get priority placement
		List<BuildingFootprint> buildings = new(layout.Buildings);
		buildings.Sort((a, b) =>
		{
			int areaA = a.Size.X * a.Size.Y;
			int areaB = b.Size.X * b.Size.Y;
			return areaB.CompareTo(areaA);
		});

		int placed = 0;
		bool landmarkPlaced = false;
		int antennaCount = 0;
		const int maxAntennas = 2;

		foreach (BuildingFootprint building in buildings)
		{
			int area = building.Size.X * building.Size.Y;
			if (area < 9)
				continue;

			uint hash = HashCell(building.Origin, seed ^ (ulong)(area * 17));
			int roll = (int)(hash % 100);

			// --- Landmark (church): rare, only on large intact buildings ---
			if (!landmarkPlaced && area >= 80 && building.Integrity >= 0.50f && roll < 20)
			{
				Vector2I anchor = GetCenterAnchor(building);
				if (TryPlaceProp(LandmarkSprites[0], anchor, ground, container, usedCells, cache))
				{
					placed++;
					landmarkPlaced = true;
					ReserveBuildingFootprint(building, usedCells, inset: 0);
					continue;
				}
			}

			// --- Very damaged buildings: rubble pile + debris ---
			if (building.Integrity < 0.40f)
			{
				Vector2I anchor = GetCenterAnchor(building);
				if (TryPlaceProp(RubbleSprites[0], anchor, ground, container, usedCells, cache))
					placed++;

				PlaceInteriorDebris(building, ground, container, usedCells, cache, seed ^ hash, ref placed);
				ReserveBuildingFootprint(building, usedCells, inset: 0);
				continue;
			}

			// --- Normal buildings: place facades along street-facing edges ---
			List<int> streetSides = GetStreetFacingSides(building, layout);

			if (streetSides.Count == 0)
			{
				// Interior block with no street access: always rubble
				Vector2I anchor = GetCenterAnchor(building);
				if (TryPlaceProp(RubbleSprites[0], anchor, ground, container, usedCells, cache))
					placed++;
				if (building.Integrity < 0.60f)
					PlaceInteriorDebris(building, ground, container, usedCells, cache, seed ^ hash, ref placed);
				ReserveBuildingFootprint(building, usedCells, inset: 0);
				continue;
			}

			// Check if this building is on a corner (2+ street-facing sides)
			bool isCorner = streetSides.Count >= 2;

			if (isCorner)
			{
				// Corner building at the intersection vertex
				Vector2I cornerAnchor = GetCornerAnchor(building, streetSides);
				string cornerSprite = CornerSprites[(int)(hash % (uint)CornerSprites.Length)];
				if (TryPlaceProp(cornerSprite, cornerAnchor, ground, container, usedCells, cache))
					placed++;

				// Also add a facade on the longest remaining street side
				if (area >= 50)
				{
					int bestSide = GetLongestStreetSide(building, streetSides);
					Vector2I facadeAnchor = GetFacadeAnchor(building, bestSide);
					string facadeSprite = PickFacadeSprite(building, area, hash >> 3);
					if (!usedCells.Contains(facadeAnchor)
						&& TryPlaceProp(facadeSprite, facadeAnchor, ground, container, usedCells, cache))
					{
						placed++;
					}
				}
			}
			else
			{
				// Single street side: facade along it
				int bestSide = GetLongestStreetSide(building, streetSides);
				Vector2I facadeAnchor = GetFacadeAnchor(building, bestSide);
				string facadeSprite = PickFacadeSprite(building, area, hash);
				if (TryPlaceProp(facadeSprite, facadeAnchor, ground, container, usedCells, cache))
					placed++;
			}

			// Multi-street buildings: facade on EVERY additional street side
			if (streetSides.Count >= 2 && area >= 50)
			{
				int primarySide = GetLongestStreetSide(building, streetSides);
				foreach (int side in streetSides)
				{
					if (side == primarySide)
						continue;
					Vector2I extraAnchor = GetFacadeAnchor(building, side);
					if (usedCells.Contains(extraAnchor))
						continue;
					uint sideHash = hash ^ (uint)(side * 7919);
					string extraSprite = PickFacadeSprite(building, area, sideHash);
					if (TryPlaceProp(extraSprite, extraAnchor, ground, container, usedCells, cache))
						placed++;
				}
			}

			// Interior debris for all buildings (more for damaged ones)
			if (area >= 30)
				PlaceInteriorDebris(building, ground, container, usedCells, cache, seed ^ hash, ref placed);

			// Antenna: rare, only near large buildings
			if (antennaCount < maxAntennas && area >= 70 && roll < 12)
			{
				Vector2I antennaCell = FindAdjacentSidewalkCell(building, layout, usedCells, hash);
				if (antennaCell != Vector2I.MinValue
					&& TryPlaceProp(AntennaSprite, antennaCell, ground, container, usedCells, cache))
				{
					antennaCount++;
					placed++;
				}
			}

			ReserveBuildingFootprint(building, usedCells, inset: 0);
		}

		return placed;
	}

	/// <summary>
	/// Determines which sides of a building face a road/sidewalk.
	/// Returns list of side flags (WallTop, WallRight, WallBottom, WallLeft).
	/// </summary>
	private static List<int> GetStreetFacingSides(BuildingFootprint building, UrbanLayout layout)
	{
		List<int> sides = new();

		// Check each side: sample 3 points along the edge, 1 cell outside the building
		// Top edge
		if (SideHasStreetAccess(building, layout, WallTop))
			sides.Add(WallTop);
		// Bottom edge
		if (SideHasStreetAccess(building, layout, WallBottom))
			sides.Add(WallBottom);
		// Left edge
		if (SideHasStreetAccess(building, layout, WallLeft))
			sides.Add(WallLeft);
		// Right edge
		if (SideHasStreetAccess(building, layout, WallRight))
			sides.Add(WallRight);

		return sides;
	}

	private static bool SideHasStreetAccess(BuildingFootprint building, UrbanLayout layout, int side)
	{
		int samples = 3;
		int hits = 0;

		for (int i = 0; i < samples; i++)
		{
			Vector2I probe;
			float t = (samples == 1) ? 0.5f : (float)i / (samples - 1);

			switch (side)
			{
				case WallTop:
				{
					int x = building.Origin.X + (int)(building.Size.X * t);
					probe = new Vector2I(x, building.Origin.Y - 1);
					break;
				}
				case WallBottom:
				{
					int x = building.Origin.X + (int)(building.Size.X * t);
					probe = new Vector2I(x, building.Origin.Y + building.Size.Y);
					break;
				}
				case WallLeft:
				{
					int y = building.Origin.Y + (int)(building.Size.Y * t);
					probe = new Vector2I(building.Origin.X - 1, y);
					break;
				}
				case WallRight:
				{
					int y = building.Origin.Y + (int)(building.Size.Y * t);
					probe = new Vector2I(building.Origin.X + building.Size.X, y);
					break;
				}
				default:
					continue;
			}

			UrbanCellType type = GetCellType(layout, probe);
			if (type == UrbanCellType.Road || type == UrbanCellType.Sidewalk || type == UrbanCellType.Plaza)
				hits++;
		}

		return hits >= 2; // Majority of samples must face street
	}

	private static int GetLongestStreetSide(BuildingFootprint building, List<int> streetSides)
	{
		int best = streetSides[0];
		int bestLen = 0;

		foreach (int side in streetSides)
		{
			int len = (side == WallTop || side == WallBottom) ? building.Size.X : building.Size.Y;
			if (len > bestLen)
			{
				bestLen = len;
				best = side;
			}
		}

		return best;
	}

	/// <summary>
	/// Anchor point along a facade edge (middle of the street-facing side).
	/// </summary>
	private static Vector2I GetFacadeAnchor(BuildingFootprint building, int side)
	{
		return side switch
		{
			WallTop => new Vector2I(
				building.Origin.X + building.Size.X / 2,
				building.Origin.Y),
			WallBottom => new Vector2I(
				building.Origin.X + building.Size.X / 2,
				building.Origin.Y + building.Size.Y - 1),
			WallLeft => new Vector2I(
				building.Origin.X,
				building.Origin.Y + building.Size.Y / 2),
			WallRight => new Vector2I(
				building.Origin.X + building.Size.X - 1,
				building.Origin.Y + building.Size.Y / 2),
			_ => GetCenterAnchor(building),
		};
	}

	/// <summary>
	/// Anchor for a corner building: placed at the corner vertex where two street sides meet.
	/// </summary>
	private static Vector2I GetCornerAnchor(BuildingFootprint building, List<int> streetSides)
	{
		bool hasTop = streetSides.Contains(WallTop);
		bool hasBottom = streetSides.Contains(WallBottom);
		bool hasLeft = streetSides.Contains(WallLeft);
		bool hasRight = streetSides.Contains(WallRight);

		// Pick the actual corner where two street sides meet
		if (hasBottom && hasRight)
			return new Vector2I(building.Origin.X + building.Size.X - 1, building.Origin.Y + building.Size.Y - 1);
		if (hasBottom && hasLeft)
			return new Vector2I(building.Origin.X, building.Origin.Y + building.Size.Y - 1);
		if (hasTop && hasRight)
			return new Vector2I(building.Origin.X + building.Size.X - 1, building.Origin.Y);
		if (hasTop && hasLeft)
			return new Vector2I(building.Origin.X, building.Origin.Y);

		// Fallback: center
		return GetCenterAnchor(building);
	}

	private static Vector2I GetCenterAnchor(BuildingFootprint building)
	{
		return new Vector2I(
			building.Origin.X + building.Size.X / 2,
			building.Origin.Y + building.Size.Y / 2);
	}

	/// <summary>
	/// Find a sidewalk cell adjacent to the building for antenna placement.
	/// </summary>
	private static Vector2I FindAdjacentSidewalkCell(
		BuildingFootprint building, UrbanLayout layout, HashSet<Vector2I> usedCells, uint hash)
	{
		// Walk around the building perimeter looking for a sidewalk cell
		List<Vector2I> candidates = new();

		for (int x = building.Origin.X - 1; x <= building.Origin.X + building.Size.X; x++)
		{
			Vector2I top = new(x, building.Origin.Y - 1);
			Vector2I bottom = new(x, building.Origin.Y + building.Size.Y);
			if (layout.SidewalkCells.Contains(top) && !usedCells.Contains(top))
				candidates.Add(top);
			if (layout.SidewalkCells.Contains(bottom) && !usedCells.Contains(bottom))
				candidates.Add(bottom);
		}
		for (int y = building.Origin.Y; y < building.Origin.Y + building.Size.Y; y++)
		{
			Vector2I left = new(building.Origin.X - 1, y);
			Vector2I right = new(building.Origin.X + building.Size.X, y);
			if (layout.SidewalkCells.Contains(left) && !usedCells.Contains(left))
				candidates.Add(left);
			if (layout.SidewalkCells.Contains(right) && !usedCells.Contains(right))
				candidates.Add(right);
		}

		if (candidates.Count == 0)
			return Vector2I.MinValue;

		return candidates[(int)(hash % (uint)candidates.Count)];
	}

	private static string PickFacadeSprite(BuildingFootprint building, int area, uint hash)
	{
		int roll = (int)(hash % 100);
		bool wide = building.Size.X >= building.Size.Y;

		// Large wide buildings → apartment block or wide facade
		if (area >= 100 && wide)
			return roll < 45 ? FacadeSprites[1] : FacadeSprites[0]; // apartment or facade

		// Medium buildings → shopfront or facade
		if (area >= 60)
			return roll < 40 ? FacadeSprites[2] : FacadeSprites[0]; // shopfront or facade

		// Smaller buildings → shopfront
		return FacadeSprites[2];
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

			string sprite = roll < 3
				? RoadSprites[0]
				: RoadSprites[1 + (int)(hash % 2)];

			if (TryPlaceProp(sprite, roadCell, ground, container, usedCells, cache))
				placed++;
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
			if (nearIntersection && roll < 25)
				sprite = SidewalkIntersectionSprites[(int)(hash % (uint)SidewalkIntersectionSprites.Length)];
			else if (nearBuilding && roll < 18)
				sprite = SidewalkEdgeSprites[(int)(hash % (uint)SidewalkEdgeSprites.Length)];

			if (sprite != null && TryPlaceProp(sprite, cell, ground, container, usedCells, cache))
				placed++;
		}

		return placed;
	}

	// =========================================================================
	// INTERIOR DEBRIS
	// =========================================================================

	private static void PlaceInteriorDebris(
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
