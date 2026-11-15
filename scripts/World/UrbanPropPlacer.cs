using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Infrastructure;

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

	private const string ChainLinkFenceSprite = "assets/props/urban_ruins/prop_chain_link_fence.png";

	private static readonly string[] SidewalkEdgeSprites = {
		"assets/props/urban_ruins/prop_dumpster.png",
		"assets/props/urban_ruins/prop_dumpster_v2.png",
		ChainLinkFenceSprite,
		"assets/props/urban_ruins/prop_torn_billboard.png",
		// Abribus (plan 23 R7) : petit lieu de vitesse, le long des trottoirs.
		"assets/props/urban_ruins/prop_bus_shelter.png",
	};

	private static readonly string[] InteriorDebrisSprites = {
		"assets/props/urban_ruins/prop_concrete_debris.png",
		"assets/props/urban_ruins/prop_concrete_debris_v2.png",
		"assets/props/urban_ruins/prop_concrete_debris_v3.png",
		"assets/props/urban_ruins/prop_steel_beam.png",
		"assets/props/urban_ruins/prop_steel_beam_diagonal.png",
	};

	// Arbres d'une avenue : de part et d'autre de la chaussée, sur le trottoir (une rue verticale est plus large à l'écran).
	private const float AvenueTreeOffsetXPx = 56f;
	private const float AvenueTreeOffsetYPx = 26f;
	private static readonly Vector2 DiagonalSeTreeNormal = new(42f, -21f);
	private static readonly Vector2 DiagonalSwTreeNormal = new(42f, 21f);

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
		"prop_bus_shelter.png",
		"prop_radio_mast.png",
	};

	/// <summary>Pose la ville et renvoie les scènes de l'exode (plan 39 B3).</summary>
	public static List<ExodusScenePlacer.Scene> PlaceProps(
		UrbanLayout layout,
		CityGrid city,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		ulong seed)
	{
		Dictionary<string, Texture2D> cache = new();
		HashSet<Vector2I> intersections = FindIntersections(layout);

		int buildingCount = UrbanBuildingPlacer.Place(layout, ground, container, usedCells, cache, seed);
		usedCells.UnionWith(layout.ParkPondCells);
		// Places, étoiles, parcs, arbres des avenues et gravats des cratères : avant l'exode et le semis des rues, qui s'en écartent.
		int cityCount = PlaceSquares(layout, city, ground, container, usedCells, cache, seed)
			+ PlaceStarPlazas(layout, city, ground, container, usedCells, cache, seed)
			+ PlaceParks(layout, city, ground, container, usedCells, cache, seed)
			+ PlaceAvenueTrees(layout, city, ground, container, usedCells, cache, seed)
			+ PlaceCraters(layout, city, ground, container, usedCells, cache, seed);
		// Décors de rue à l'échelle du personnage : au moins une cellule libre entre deux d'entre eux.
		HashSet<Vector2I> streetProps = new();
		List<ExodusScenePlacer.Scene> exodusScenes = new();
		int roadCount = 0;
		// Les scènes de l'exode passent avant le semis, qui leur laisse un vide autour et compense leur nombre.
		if (ExodusConfig.TryLoad(out ExodusConfig exodus, out string error))
		{
			exodusScenes = ExodusScenePlacer.Place(layout, intersections, exodus, ground, container, usedCells, streetProps, cache, seed);
			roadCount = PlaceRoadProps(layout, intersections, exodus, ground, container, usedCells, streetProps, cache, seed);
		}
		else
		{
			GD.PushError($"[UrbanPropPlacer] {error} : rues sans exode ni décors semés");
		}
		int sidewalkCount = PlaceSidewalkProps(layout, intersections, ground, container, usedCells, streetProps, cache, seed);

		GD.Print($"[UrbanPropPlacer] Placed {buildingCount} building masses, {cityCount} square, avenue and crater props, "
			+ $"{roadCount} road props, {sidewalkCount} sidewalk props");
		return exodusScenes;
	}

	// =========================================================================
	// ROAD PROPS
	// =========================================================================

	private static int PlaceRoadProps(
		UrbanLayout layout,
		HashSet<Vector2I> intersections,
		ExodusConfig config,
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
			bool diagonal = layout.DiagonalCells.Contains(roadCell);
			if (!diagonal && !IsStraightRoadCell(layout, roadCell))
				continue;
			if (IsNearIntersection(intersections, roadCell))
				continue;

			uint hash = HashCell(roadCell, seed ^ 0x1A2B3CUL);
			float roll = (hash % 10000u) / 10000f;
			if (roll >= (diagonal ? config.ScatterChance * 0.35f : config.ScatterChance))
				continue;

			string sprite;
			if (!diagonal && roll < config.ScatterChance * config.ScatterCarShare)
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
			Vector2 offset = StreetPoint(layout, ground, roadCell) - ground.MapToLocal(roadCell);
			if (TryPlaceProp(sprite, roadCell, ground, container, usedCells, cache, offset))
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
			// Un grillage sur deux laisse sa place vide : la ville en portait trop (DECISIONS §150).
			if (sprite == ChainLinkFenceSprite && (hash >> 16) % 2 == 0)
				sprite = null;

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
	// PLACES, AVENUES, CRATÈRES (plan 58 T3a)
	// =========================================================================

	/// <summary>
	/// Place plantée : des arbres en quinconce, et entre eux bancs, réverbères, colonne ou borne-fontaine. Une marge d'une
	/// cellule reste libre au bord, côté trottoir.
	/// </summary>
	private static int PlaceSquares(UrbanLayout layout, CityGrid city, TileMapLayer ground, Node2D container,
		HashSet<Vector2I> usedCells, Dictionary<string, Texture2D> cache, ulong seed)
	{
		int placed = 0;
		Vector2I every = city.SquareTreeEvery;
		foreach (Rect2I square in layout.Squares)
		{
			for (int y = square.Position.Y + 1; y < square.End.Y - 1; y++)
			{
				for (int x = square.Position.X + 1; x < square.End.X - 1; x++)
				{
					Vector2I cell = new(x, y);
					if (GetCellType(layout, cell) != UrbanCellType.Plaza)
						continue;
					int column = x - square.Position.X - 1;
					int row = y - square.Position.Y - 1;
					// Quinconce : une rangée sur deux décale ses arbres d'un demi-pas.
					bool tree = row % every.Y == 0 && (column + (row / every.Y % 2) * (every.X / 2)) % every.X == 0;
					uint hash = HashCell(cell, seed ^ 0x5C0A2EUL);
					CityGrid.Dressing dressing = tree ? CityGrid.Pick(city.SquareTrees, hash)
						: (hash >> 12) % 10000u / 10000f < city.SquareDensity ? CityGrid.Pick(city.SquareProps, hash) : null;
					if (dressing != null && TryPlaceDressing(dressing, cell, ground, container, usedCells, cache, default))
						placed++;
				}
			}
		}
		return placed;
	}

	/// <summary>Arbres d'alignement des avenues (orthogonales et diagonales), de part et d'autre de la chaussée.</summary>
	private static int PlaceAvenueTrees(UrbanLayout layout, CityGrid city, TileMapLayer ground, Node2D container,
		HashSet<Vector2I> usedCells, Dictionary<string, Texture2D> cache, ulong seed)
	{
		int placed = 0;
		// Pas en cellules : 64 px par colonne le long d'une rue horizontale, 16 px par rang le long d'une verticale,
		// en nombre pair pour qu'un arbre sur deux ne saute pas de ±32 px avec la parité du rang.
		int stepX = Mathf.Max(1, Mathf.RoundToInt(city.AvenueTreeSpacingPx / 64f));
		int stepY = Mathf.Max(2, Mathf.RoundToInt(city.AvenueTreeSpacingPx / 32f) * 2);
		int stepDiag = Mathf.Max(2, Mathf.RoundToInt(city.AvenueTreeSpacingPx / 32f));
		foreach (Vector2I cell in layout.AvenueCells)
		{
			if (layout.DiagonalCells.Contains(cell))
			{
				RoadDir dir = layout.GetRoadDir(cell.X, cell.Y);
				if ((dir & RoadDir.Orthogonal) != 0 || ((dir & RoadDir.DiagSe) != 0 && (dir & RoadDir.DiagSw) != 0))
					continue;
				if (Mathf.PosMod(cell.Y, stepDiag) != 0)
					continue;
				Vector2 normal = (dir & RoadDir.DiagSe) != 0 ? DiagonalSeTreeNormal : DiagonalSwTreeNormal;
				Vector2 diagOrigin = ground.MapToLocal(cell);
				foreach (int side in new[] { -1, 1 })
				{
					Vector2 at = diagOrigin + normal * side;
					Vector2I treeCell = ground.LocalToMap(at);
					if (!layout.SidewalkCells.Contains(treeCell) || layout.RoadCells.Contains(treeCell))
						continue;
					CityGrid.Dressing tree = CityGrid.Pick(city.AvenueTrees, HashCell(treeCell, seed ^ 0xA7E1UL));
					if (tree != null && TryPlaceDressing(tree, treeCell, ground, container, usedCells, cache, at - ground.MapToLocal(treeCell)))
						placed++;
				}
				continue;
			}

			if (!IsStraightRoadCell(layout, cell))
				continue;
			bool vertical = layout.RoadCells.Contains(cell + Vector2I.Up);
			if (vertical ? Mathf.PosMod(cell.Y, stepY) != 0 : Mathf.PosMod(cell.X, stepX) != 0)
				continue;
			Vector2 street = StreetPoint(layout, ground, cell);
			foreach (int side in new[] { -1, 1 })
			{
				Vector2 at = street + (vertical ? new Vector2(side * AvenueTreeOffsetXPx, 0f) : new Vector2(0f, side * AvenueTreeOffsetYPx));
				Vector2I treeCell = ground.LocalToMap(at);
				if (!layout.SidewalkCells.Contains(treeCell))
					continue;
				CityGrid.Dressing tree = CityGrid.Pick(city.AvenueTrees, HashCell(treeCell, seed ^ 0xA7E1UL));
				if (tree != null && TryPlaceDressing(tree, treeCell, ground, container, usedCells, cache, at - ground.MapToLocal(treeCell)))
					placed++;
			}
		}
		return placed;
	}

	/// <summary>Places en étoile (plan 58 T3e) : repère au centre et couronne de tilleuls et de bancs entre les rues.</summary>
	private static int PlaceStarPlazas(UrbanLayout layout, CityGrid city, TileMapLayer ground, Node2D container,
		HashSet<Vector2I> usedCells, Dictionary<string, Texture2D> cache, ulong seed)
	{
		int placed = 0;
		foreach (StarPlaza star in layout.StarPlazas)
		{
			if (GetCellType(layout, star.CenterCell) == UrbanCellType.Plaza)
			{
				uint centerHash = HashCell(star.CenterCell, seed ^ 0x57A8UL);
				CityGrid.Dressing monument = CityGrid.Pick(city.StarCenterProps, centerHash);
				if (monument != null && TryPlaceDressing(monument, star.CenterCell, ground, container, usedCells, cache, default))
					placed++;
			}

			float ringRadius = star.RadiusPx * 0.52f;
			for (int k = 0; k < 8; k++)
			{
				float angle = (k + 0.5f) * Mathf.Pi / 4f;
				Vector2 at = star.CenterWorld + new Vector2(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius * 0.5f);
				Vector2I cell = ground.LocalToMap(at);
				if (GetCellType(layout, cell) != UrbanCellType.Plaza || IsNearRoad(layout, cell))
					continue;
				uint hash = HashCell(cell, seed ^ (ulong)(0x57A9 + k));
				CityGrid.Dressing dressing = k % 2 == 0
					? CityGrid.Pick(city.AvenueTrees, hash)
					: CityGrid.Pick(city.SquareProps, hash);
				if (dressing != null && TryPlaceDressing(dressing, cell, ground, container, usedCells, cache, default))
					placed++;
			}
		}
		return placed;
	}

	/// <summary>Parcs verts urbains (plan 58 T3e) : tilleuls en lisière et en bosquets, clôtures, bancs et réverbères le long des allées.</summary>
	private static int PlaceParks(UrbanLayout layout, CityGrid city, TileMapLayer ground, Node2D container,
		HashSet<Vector2I> usedCells, Dictionary<string, Texture2D> cache, ulong seed)
	{
		int placed = 0;
		foreach (UrbanPark park in layout.Parks)
		{
			List<Vector2> parkTrees = new();
			List<Vector2> parkFurniture = new();
			HashSet<Vector2I> alleySet = new(park.AlleyCells);
			HashSet<Vector2I> lawnOrPond = new(park.LawnCells);
			lawnOrPond.UnionWith(park.PondCells);

			foreach (Vector2I cell in park.PerimeterCells)
			{
				if (AdjacentToSet(cell, alleySet))
					continue;
				Vector2 world = ground.MapToLocal(cell);
				uint hash = HashCell(cell, seed ^ 0x9A8C1UL);
				if (FarEnoughFromAll(world, parkTrees, 100f))
				{
					CityGrid.Dressing tree = CityGrid.Pick(city.ParkTrees, hash);
					if (tree != null && TryPlaceDressing(tree, cell, ground, container, usedCells, cache, default))
					{
						parkTrees.Add(world);
						placed++;
						continue;
					}
				}
				if (hash % 100 < 12 && FarEnoughFromAll(world, parkFurniture, 96f)
					&& TryPlaceProp(ChainLinkFenceSprite, cell, ground, container, usedCells, cache))
				{
					parkFurniture.Add(world);
					placed++;
				}
			}

			foreach (Vector2I cell in park.AlleyCells)
			{
				if (!AdjacentToSet(cell, lawnOrPond))
					continue;
				Vector2 world = ground.MapToLocal(cell);
				if (world.DistanceSquaredTo(park.CenterWorld) < 36f * 36f)
					continue;
				uint hash = HashCell(cell, seed ^ 0x9A8C2UL);
				if (hash % 100 >= 20 || !FarEnoughFromAll(world, parkFurniture, 90f) || !FarEnoughFromAll(world, parkTrees, 72f))
					continue;
				CityGrid.Dressing prop = CityGrid.Pick(city.ParkProps, hash >> 8);
				if (prop != null && TryPlaceDressing(prop, cell, ground, container, usedCells, cache, default))
				{
					parkFurniture.Add(world);
					placed++;
				}
			}

			foreach (Vector2I cell in park.LawnCells)
			{
				Vector2 world = ground.MapToLocal(cell);
				uint hash = HashCell(cell, seed ^ 0x9A8C3UL);
				if (hash % 100 < 7 && FarEnoughFromAll(world, parkTrees, 110f))
				{
					CityGrid.Dressing tree = CityGrid.Pick(city.ParkTrees, hash >> 8);
					if (tree != null && TryPlaceDressing(tree, cell, ground, container, usedCells, cache, default))
					{
						parkTrees.Add(world);
						placed++;
					}
				}
			}
		}
		return placed;
	}

	private static bool AdjacentToSet(Vector2I cell, HashSet<Vector2I> set)
	{
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				if (set.Contains(new Vector2I(cell.X + dx, cell.Y + dy)))
					return true;
			}
		}
		return false;
	}

	private static bool FarEnoughFromAll(Vector2 point, List<Vector2> others, float minDistancePx)
	{
		float sq = minDistancePx * minDistancePx;
		foreach (Vector2 other in others)
		{
			if (other.DistanceSquaredTo(point) < sq)
				return false;
		}
		return true;
	}

	private static bool IsNearRoad(UrbanLayout layout, Vector2I cell)
	{
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				if (layout.RoadCells.Contains(new Vector2I(cell.X + dx, cell.Y + dy)))
					return true;
			}
		}
		return false;
	}

	/// <summary>Gravats d'une rue effondrée : la chaussée ouverte se couvre de blocs, de dalles et de poutrelles.</summary>
	private static int PlaceCraters(UrbanLayout layout, CityGrid city, TileMapLayer ground, Node2D container,
		HashSet<Vector2I> usedCells, Dictionary<string, Texture2D> cache, ulong seed)
	{
		int placed = 0;
		foreach (Vector2I cell in layout.CraterCells)
		{
			CityGrid.Dressing dressing = CityGrid.Pick(city.CraterProps, HashCell(cell, seed ^ 0xC8A7E2UL));
			if (dressing != null && TryPlaceDressing(dressing, cell, ground, container, usedCells, cache, default))
				placed++;
		}
		return placed;
	}

	// =========================================================================
	// INTERIOR DEBRIS
	// =========================================================================

	internal static void PlaceInteriorDebris(
		UrbanLayout layout,
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
			UrbanCellType cellType = GetCellType(layout, cell);
			if (cellType != UrbanCellType.BuildingInterior && cellType != UrbanCellType.BuildingWall)
				continue;

			string sprite = InteriorDebrisSprites[(int)((seed + (ulong)i) % (ulong)InteriorDebrisSprites.Length)];
			if (TryPlaceProp(sprite, cell, ground, container, usedCells, cache))
				placed++;
		}
	}

	// =========================================================================
	// HELPERS
	// =========================================================================

	internal static void ReserveBuildingFootprint(UrbanLayout layout, BuildingFootprint building, HashSet<Vector2I> usedCells, int inset)
	{
		int minX = building.Origin.X + inset;
		int maxX = building.Origin.X + building.Size.X - 1 - inset;
		int minY = building.Origin.Y + inset;
		int maxY = building.Origin.Y + building.Size.Y - 1 - inset;

		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
			{
				Vector2I cell = new(x, y);
				UrbanCellType cellType = GetCellType(layout, cell);
				if (cellType == UrbanCellType.BuildingInterior || cellType == UrbanCellType.BuildingWall)
					usedCells.Add(cell);
			}
		}
	}

	/// <summary>
	/// Milieu de la chaussée sur une cellule de rue. Une rue verticale passe au milieu de sa colonne, à ±16 px du centre de
	/// la cellule (RoadTileGenerator).
	/// </summary>
	internal static Vector2 StreetPoint(UrbanLayout layout, TileMapLayer ground, Vector2I roadCell)
	{
		bool vertical = layout.RoadCells.Contains(roadCell + Vector2I.Up);
		Vector2 offset = vertical ? new Vector2((roadCell.Y & 1) != 0 ? -16f : 16f, 0f) : Vector2.Zero;
		return ground.MapToLocal(roadCell) + offset;
	}

	/// <summary>Décor de la grille (plan 58 T3a), canopée comprise, sur une cellule libre.</summary>
	private static bool TryPlaceDressing(CityGrid.Dressing dressing, Vector2I cell, TileMapLayer ground, Node2D container,
		HashSet<Vector2I> usedCells, Dictionary<string, Texture2D> cache, Vector2 offset)
	{
		if (usedCells.Contains(cell))
			return false;
		Texture2D texture = LoadCached(dressing.Sprite, cache);
		if (texture == null)
			return false;
		EnvironmentProp prop = new();
		prop.GlobalPosition = ground.MapToLocal(cell) + offset;
		container.AddChild(prop);
		Texture2D canopy = dressing.Canopy != null ? LoadCached(dressing.Canopy, cache) : null;
		prop.Initialize(texture, canopy, dressing.CanopyOffsetY, dressing.Blocking);
		usedCells.Add(cell);
		return true;
	}

	internal static bool TryPlaceProp(
		string spritePath,
		Vector2I cell,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		Vector2 offset = default)
	{
		if (usedCells.Contains(cell))
			return false;

		Texture2D texture = LoadCached(spritePath, cache);
		if (texture == null)
			return false;

		EnvironmentProp prop = new();
		prop.GlobalPosition = ground.MapToLocal(cell) + offset;
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
		int gy = cell.Y + layout.MapRadiusY;
		if (gx < 0 || gy < 0 || gx >= layout.CellGrid.GetLength(0) || gy >= layout.CellGrid.GetLength(1))
			return UrbanCellType.None;

		return layout.CellGrid[gx, gy];
	}

	internal static bool IsStraightRoadCell(UrbanLayout layout, Vector2I cell)
	{
		if (layout.DiagonalCells.Contains(cell))
			return false;
		bool north = layout.RoadCells.Contains(cell + Vector2I.Up);
		bool south = layout.RoadCells.Contains(cell + Vector2I.Down);
		bool west = layout.RoadCells.Contains(cell + Vector2I.Left);
		bool east = layout.RoadCells.Contains(cell + Vector2I.Right);

		bool straightVertical = north && south && !west && !east;
		bool straightHorizontal = west && east && !north && !south;
		return straightVertical || straightHorizontal;
	}

	internal static bool IsNearIntersection(HashSet<Vector2I> intersections, Vector2I cell)
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

			RoadDir roadDir = layout.GetRoadDir(roadCell.X, roadCell.Y);
			int dirCount = ((roadDir & RoadDir.Orthogonal) != 0 ? 1 : 0)
				+ ((roadDir & RoadDir.DiagSe) != 0 ? 1 : 0)
				+ ((roadDir & RoadDir.DiagSw) != 0 ? 1 : 0);
			if (adjacent >= 3 || dirCount >= 2)
				intersections.Add(roadCell);
		}

		return intersections;
	}

	private static bool IsBlocking(string path)
	{
		int slash = path.LastIndexOf('/');
		string file = slash >= 0 ? path[(slash + 1)..] : path;
		return file.StartsWith(UrbanBuildingPlacer.SpritePrefix) || BlockingSprites.Contains(file)
			|| file.StartsWith("prop_exodus_car_") || file.StartsWith("prop_exodus_barrier_") || file.StartsWith("prop_exodus_sign_");
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
