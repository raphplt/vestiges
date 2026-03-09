using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

public enum SwampZoneType
{
	None,
	DeepWater,
	ShallowWater,
	Bank,
	WetClearing,
	DeadGrove,
	FungalPocket
}

public struct SwampPropPlacement
{
	public Vector2I Cell;
	public string BasePath;
	public string CanopyPath;
	public float CanopyOffsetY;
	public float CollisionRadius;
	public float CollisionOffsetY;
}

public class SwampPropLayout
{
	public HashSet<Vector2I> ReservedCells = new();
	public List<SwampPropPlacement> Placements = new();
	public Dictionary<Vector2I, SwampZoneType> Zones = new();
}

/// <summary>
/// Compose le biome marécageux par zones lisibles :
/// eaux profondes, lisières, clairières humides, bosquets morts et poches fongiques.
/// </summary>
public static class SwampPropPlacer
{
	private static readonly Vector2I[] NearOffsets = {
		new(-2, -1), new(-1, -2), new(1, -2), new(2, -1),
		new(2, 1), new(1, 2), new(-1, 2), new(-2, 1),
		new(0, -3), new(3, 0), new(0, 3), new(-3, 0)
	};

	private static readonly Vector2I[] CloseOffsets = {
		new(-1, -1), new(0, -1), new(1, -1),
		new(-1, 0), new(1, 0),
		new(-1, 1), new(0, 1), new(1, 1)
	};

	private static readonly Dictionary<string, float> CollisionRadii = new()
	{
		{ "prop_dead_tree_large_base.png", 8f },
		{ "prop_dead_tree_mossy_large.png", 8f },
		{ "prop_dead_tree_small.png", 3f },
		{ "prop_aerial_roots.png", 6f },
		{ "prop_aerial_roots_twisted.png", 6f },
		{ "prop_root_mass_large.png", 7f },
		{ "prop_bound_tree.png", 7f },
		{ "prop_sunken_trunk_large.png", 8f },
		{ "prop_sunken_boat.png", 7f },
		{ "prop_drowned_cart.png", 6f },
		{ "prop_broken_walkway.png", 0f },
		{ "prop_bone_pile.png", 0f },
		{ "prop_collapsed_pontoon.png", 6f },
		{ "prop_sunken_shrine.png", 5f },
		{ "prop_reeds.png", 0f },
		{ "prop_lily_pads.png", 0f },
		{ "prop_fallen_log.png", 6f },
		{ "prop_rotten_stump.png", 3f },
		{ "prop_spore_patch.png", 0f },
		{ "prop_toxic_mushrooms.png", 0f },
		{ "prop_swamp_lantern.png", 0f },
		{ "prop_vine_curtain.png", 0f },
		{ "prop_hanging_moss.png", 0f },
		{ "prop_old_post.png", 0f },
	};

	public static SwampPropLayout BuildLayout(WorldGenerator generator, ulong seed)
	{
		SwampPropLayout layout = new();
		Dictionary<Vector2I, int> waterDistances = new();
		List<Vector2I> swampGround = new();
		List<Vector2I> bankCells = new();
		List<Vector2I> shallowWaterCells = new();
		List<Vector2I> deepWaterCells = new();
		List<Vector2I> wetClearingCells = new();

		ClassifyZones(layout, generator, seed, waterDistances, swampGround, bankCells, shallowWaterCells, deepWaterCells, wetClearingCells);

		List<Vector2I> deadGroveAnchors = CreateDeadGroves(layout, generator, seed, waterDistances);
		List<Vector2I> fungalAnchors = CreateFungalPockets(layout, generator, seed, waterDistances, deadGroveAnchors);

		PlaceDeadGroves(layout, generator, seed, deadGroveAnchors);
		PlaceBankScenes(layout, generator, seed, bankCells, shallowWaterCells);
		PlaceWetStructureScenes(layout, generator, seed, wetClearingCells, deadGroveAnchors, fungalAnchors);
		PlaceFungalScenes(layout, generator, seed, fungalAnchors);
		PlaceNarrativeProps(layout, generator, seed);

		return layout;
	}

	public static void PlaceProps(
		SwampPropLayout layout,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells)
	{
		Dictionary<string, Texture2D> cache = new();

		foreach (SwampPropPlacement placement in layout.Placements)
		{
			if (usedCells.Contains(placement.Cell))
				continue;

			Texture2D baseTexture = LoadCached(placement.BasePath, cache);
			if (baseTexture == null)
				continue;

			Texture2D canopyTexture = string.IsNullOrEmpty(placement.CanopyPath)
				? null
				: LoadCached(placement.CanopyPath, cache);

			EnvironmentProp prop = new();
			prop.GlobalPosition = ground.MapToLocal(placement.Cell);
			container.AddChild(prop);
			prop.Initialize(baseTexture, canopyTexture, placement.CanopyOffsetY, placement.CollisionRadius, placement.CollisionOffsetY);
			usedCells.Add(placement.Cell);
		}
	}

	private static void ClassifyZones(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		Dictionary<Vector2I, int> waterDistances,
		List<Vector2I> swampGround,
		List<Vector2I> bankCells,
		List<Vector2I> shallowWaterCells,
		List<Vector2I> deepWaterCells,
		List<Vector2I> wetClearingCells)
	{
		int radius = generator.MapRadius;
		for (int x = -radius; x <= radius; x++)
		{
			for (int y = -radius; y <= radius; y++)
			{
				if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
					continue;
				if (generator.GetBiomeId(x, y) != "swamp")
					continue;

				Vector2I cell = new(x, y);
				TerrainType terrain = generator.GetTerrain(x, y);
				if (terrain == TerrainType.Water)
				{
					SwampZoneType zone = IsShallowWater(generator, x, y)
						? SwampZoneType.ShallowWater
						: SwampZoneType.DeepWater;
					layout.Zones[cell] = zone;
					if (zone == SwampZoneType.ShallowWater)
						shallowWaterCells.Add(cell);
					else
						deepWaterCells.Add(cell);
					continue;
				}

				int waterDistance = GetNearestWaterDistance(generator, x, y, 4);
				waterDistances[cell] = waterDistance;
				swampGround.Add(cell);

				SwampZoneType groundZone = waterDistance == 1
					? SwampZoneType.Bank
					: SwampZoneType.WetClearing;
				layout.Zones[cell] = groundZone;

				if (groundZone == SwampZoneType.Bank)
					bankCells.Add(cell);
				else
					wetClearingCells.Add(cell);
			}
		}
	}

	private static List<Vector2I> CreateDeadGroves(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		Dictionary<Vector2I, int> waterDistances)
	{
		List<Vector2I> candidates = new();
		foreach (KeyValuePair<Vector2I, SwampZoneType> kv in layout.Zones)
		{
			if (kv.Value != SwampZoneType.WetClearing)
				continue;
			if (generator.GetTerrain(kv.Key.X, kv.Key.Y) != TerrainType.Forest)
				continue;
			if (!waterDistances.TryGetValue(kv.Key, out int waterDistance) || waterDistance < 2)
				continue;
			if (PatchRoll(kv.Key.X, kv.Key.Y, 7, seed ^ 0xA11DUL) >= 48)
				continue;

			candidates.Add(kv.Key);
		}

		SortCells(candidates, seed ^ 0x710FUL);
		int targetCount = Mathf.Clamp(candidates.Count / 72, 4, 6);
		List<Vector2I> anchors = PickSpacedCells(candidates, targetCount, 13, layout.ReservedCells);

		foreach (Vector2I anchor in anchors)
			MarkZoneRadius(layout.Zones, anchor, 3, SwampZoneType.DeadGrove, SwampZoneType.WetClearing);

		return anchors;
	}

	private static List<Vector2I> CreateFungalPockets(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		Dictionary<Vector2I, int> waterDistances,
		List<Vector2I> deadGroveAnchors)
	{
		List<Vector2I> candidates = new();
		foreach (KeyValuePair<Vector2I, SwampZoneType> kv in layout.Zones)
		{
			if (kv.Value != SwampZoneType.WetClearing)
				continue;
			if (!waterDistances.TryGetValue(kv.Key, out int waterDistance))
				continue;
			if (waterDistance > 3)
				continue;
			if (PatchRoll(kv.Key.X, kv.Key.Y, 8, seed ^ 0xF00DUL) >= 22)
				continue;
			if (IsNearAny(kv.Key, deadGroveAnchors, 10))
				continue;

			candidates.Add(kv.Key);
		}

		SortCells(candidates, seed ^ 0xFACEUL);
		int targetCount = Mathf.Clamp(candidates.Count / 75, 3, 5);
		List<Vector2I> anchors = PickSpacedCells(candidates, targetCount, 11, layout.ReservedCells);

		foreach (Vector2I anchor in anchors)
			MarkZoneRadius(layout.Zones, anchor, 2, SwampZoneType.FungalPocket, SwampZoneType.WetClearing);

		return anchors;
	}

	private static void PlaceDeadGroves(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> anchors)
	{
		if (anchors.Count == 0)
			return;

		bool boundTreePlaced = false;
		foreach (Vector2I anchor in anchors)
		{
			uint hash = HashCell(anchor, seed ^ 0xBADA55UL);
			bool placeBoundTree = !boundTreePlaced && (hash % 100 < 40 || anchors.Count == 1);
			bool mossy = !placeBoundTree && (hash % 100) < 55;

			string basePath = placeBoundTree
				? "assets/props/swamp/prop_bound_tree.png"
				: (mossy ? "assets/props/swamp/prop_dead_tree_mossy_large.png" : "assets/props/swamp/prop_dead_tree_large_base.png");
			string canopyPath = (!placeBoundTree && !mossy)
				? "assets/props/swamp/prop_dead_tree_large_canopy.png"
				: null;
			float canopyOffset = canopyPath == null ? 0f : -54f;
			float collisionRadius = placeBoundTree ? 7f : 8f;

			AddPlacement(layout, anchor, basePath, canopyPath, canopyOffset, collisionRadius, 0f, 4);
			boundTreePlaced |= placeBoundTree;

			List<string> usedSatellites = new();
			int satelliteCount = 3 + (int)(hash % 3);
			for (int i = 0; i < satelliteCount; i++)
			{
				Vector2I? candidate = FindNearbyZoneCell(layout, generator, anchor, seed ^ hash ^ (ulong)(i * 17), SwampZoneType.DeadGrove);
				if (candidate == null)
					continue;

				string sprite = PickDeadGroveSatellite(placeBoundTree, mossy, i, seed ^ hash, usedSatellites);
				AddPlacement(
					layout,
					candidate.Value,
					sprite,
					null,
					0f,
					GetCollisionRadius(sprite),
					0f,
					sprite.Contains("vine_curtain") || sprite.Contains("hanging_moss") || sprite.Contains("bone_pile") ? 1 : 2);
				usedSatellites.Add(sprite);
			}

			Vector2I? structureCell = FindNearbyZoneCell(layout, generator, anchor, seed ^ hash ^ 0x7171UL, SwampZoneType.DeadGrove);
			if (structureCell != null)
			{
				string structureSprite = PickDeadGroveStructure(placeBoundTree, mossy, seed ^ hash);
				float structureOffset = structureSprite.Contains("fallen_log") ? 2f : 0f;
				AddPlacement(
					layout,
					structureCell.Value,
					structureSprite,
					null,
					0f,
					GetCollisionRadius(structureSprite),
					structureOffset,
					2);
			}
		}
	}

	private static void PlaceBankScenes(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> bankCells,
		List<Vector2I> shallowWaterCells)
	{
		List<Vector2I> sceneAnchors = new(bankCells);
		SortCells(sceneAnchors, seed ^ 0x5511UL);
		List<Vector2I> chosen = PickSpacedCells(sceneAnchors, 5, 13, layout.ReservedCells);

		if (chosen.Count == 0)
			return;

		AddPlacement(layout, chosen[0], "assets/props/swamp/prop_collapsed_pontoon.png", null, 0f, 6f, 2f, 3);
		DecorateBankCluster(layout, generator, chosen[0], seed ^ 0xAB12UL, true);

		if (chosen.Count > 1)
		{
			Vector2I shrineCell = FindNearbyPreferredCell(layout, generator, chosen[1], seed ^ 0xBC23UL, SwampZoneType.ShallowWater, SwampZoneType.Bank) ?? chosen[1];
			AddPlacement(layout, shrineCell, "assets/props/swamp/prop_sunken_shrine.png", null, 0f, 5f, 0f, 3);
			DecorateBankCluster(layout, generator, chosen[1], seed ^ 0xCC34UL, false);
		}

		if (chosen.Count > 2)
		{
			Vector2I trunkCell = FindNearbyPreferredCell(layout, generator, chosen[2], seed ^ 0xCD45UL, SwampZoneType.ShallowWater, SwampZoneType.Bank) ?? chosen[2];
			AddPlacement(layout, trunkCell, "assets/props/swamp/prop_sunken_trunk_large.png", null, 0f, 8f, 0f, 3);
			DecorateBankCluster(layout, generator, chosen[2], seed ^ 0xDE56UL, false);
		}

		if (chosen.Count > 3)
		{
			Vector2I logCell = FindNearbyPreferredCell(layout, generator, chosen[3], seed ^ 0xEF67UL, SwampZoneType.Bank, SwampZoneType.ShallowWater) ?? chosen[3];
			AddPlacement(layout, logCell, "assets/props/swamp/prop_fallen_log.png", null, 0f, 6f, 2f, 2);
			DecorateBankCluster(layout, generator, chosen[3], seed ^ 0xF078UL, false);
		}

		if (chosen.Count > 4)
		{
			Vector2I rootCell = FindNearbyPreferredCell(layout, generator, chosen[4], seed ^ 0xA099UL, SwampZoneType.Bank, SwampZoneType.ShallowWater) ?? chosen[4];
			AddPlacement(layout, rootCell, "assets/props/swamp/prop_aerial_roots_twisted.png", null, 0f, 6f, 0f, 2);
			DecorateBankCluster(layout, generator, chosen[4], seed ^ 0xB0AAUL, false);
		}

		List<Vector2I> walkwayCandidates = new(bankCells);
		SortCells(walkwayCandidates, seed ^ 0x8888UL);
		List<Vector2I?> avoidCells = new();
		foreach (Vector2I chosenCell in chosen)
			avoidCells.Add(chosenCell);
		Vector2I? walkwayCell = PickSingleCell(walkwayCandidates, layout.ReservedCells, 12, avoidCells.ToArray());
		if (walkwayCell != null)
			AddPlacement(layout, walkwayCell.Value, "assets/props/swamp/prop_broken_walkway.png", null, 0f, 0f, 0f, 2);
	}

	private static void PlaceFungalScenes(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> anchors)
	{
		foreach (Vector2I center in anchors)
		{
			AddPlacement(layout, center, "assets/props/swamp/prop_spore_patch.png", null, 0f, 0f, 0f, 2);

			Vector2I? lanternCell = FindNearbyZoneCell(layout, generator, center, seed ^ 0xCC44UL, SwampZoneType.FungalPocket);
			if (lanternCell != null && (HashCell(center, seed ^ 0x44CCUL) % 100) < 55)
				AddPlacement(layout, lanternCell.Value, "assets/props/swamp/prop_swamp_lantern.png", null, 0f, 0f, 0f, 1);

			for (int i = 0; i < 3; i++)
			{
				Vector2I? candidate = FindNearbyZoneCell(layout, generator, center, seed ^ (ulong)(center.X * 33 + center.Y * 19 + i), SwampZoneType.FungalPocket);
				if (candidate == null)
					continue;
				AddPlacement(layout, candidate.Value, "assets/props/swamp/prop_toxic_mushrooms.png", null, 0f, 0f, 0f, 1);
			}
		}
	}

	private static void PlaceWetStructureScenes(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> wetClearingCells,
		List<Vector2I> deadGroveAnchors,
		List<Vector2I> fungalAnchors)
	{
		List<Vector2I> candidates = new();
		foreach (Vector2I cell in wetClearingCells)
		{
			if (layout.ReservedCells.Contains(cell))
				continue;
			if (IsNearAny(cell, deadGroveAnchors, 9))
				continue;
			if (IsNearAny(cell, fungalAnchors, 7))
				continue;
			if (PatchRoll(cell.X, cell.Y, 8, seed ^ 0x5EEDUL) >= 34)
				continue;

			candidates.Add(cell);
		}

		SortCells(candidates, seed ^ 0xC1EAUL);
		List<Vector2I> anchors = PickSpacedCells(candidates, 5, 14, layout.ReservedCells);
		foreach (Vector2I anchor in anchors)
		{
			string structureSprite = PickWetStructureSprite(anchor, seed);
			float collisionOffset = structureSprite.Contains("fallen_log") ? 2f : 0f;
			AddPlacement(
				layout,
				anchor,
				structureSprite,
				null,
				0f,
				GetCollisionRadius(structureSprite),
				collisionOffset,
				2);

			DecorateWetStructureCluster(layout, generator, anchor, seed ^ HashCell(anchor, seed));
		}
	}

	private static void PlaceNarrativeProps(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed)
	{
		List<Vector2I> shallowCandidates = GetCellsForZones(layout, SwampZoneType.ShallowWater, SwampZoneType.Bank);
		List<Vector2I> wetCandidates = GetCellsForZones(layout, SwampZoneType.WetClearing, SwampZoneType.Bank);
		List<Vector2I> groveCandidates = GetCellsForZones(layout, SwampZoneType.DeadGrove);

		SortCells(shallowCandidates, seed ^ 0x9911UL);
		SortCells(wetCandidates, seed ^ 0x7733UL);
		SortCells(groveCandidates, seed ^ 0x6644UL);

		Vector2I? boatCell = PickSingleCell(shallowCandidates, layout.ReservedCells, 18);
		if (boatCell != null)
			AddPlacement(layout, boatCell.Value, "assets/props/swamp/prop_sunken_boat.png", null, 0f, 7f, 0f, 3);

		Vector2I? cartCell = PickSingleCell(wetCandidates, layout.ReservedCells, 16, boatCell);
		if (cartCell != null)
			AddPlacement(layout, cartCell.Value, "assets/props/swamp/prop_drowned_cart.png", null, 0f, 6f, 0f, 2);

		Vector2I? boneCell = PickSingleCell(groveCandidates, layout.ReservedCells, 10, cartCell);
		if (boneCell != null)
			AddPlacement(layout, boneCell.Value, "assets/props/swamp/prop_bone_pile.png", null, 0f, 0f, 0f, 2);
	}

	private static void DecorateBankCluster(
		SwampPropLayout layout,
		WorldGenerator generator,
		Vector2I center,
		ulong seed,
		bool preferPontoonDressing)
	{
		string[] bankSprites = preferPontoonDressing
			? new[]
			{
				"assets/props/swamp/prop_reeds.png",
				"assets/props/swamp/prop_fallen_log.png",
				"assets/props/swamp/prop_lily_pads.png"
			}
			: new[]
		{
				"assets/props/swamp/prop_reeds.png",
				"assets/props/swamp/prop_lily_pads.png",
				"assets/props/swamp/prop_reeds.png"
			};

		string[] accentSprites = preferPontoonDressing
			? new[]
			{
				"assets/props/swamp/prop_old_post.png",
				"assets/props/swamp/prop_bone_pile.png"
			}
			: new[]
			{
				"assets/props/swamp/prop_old_post.png",
				"assets/props/swamp/prop_vine_curtain.png"
			};

		for (int i = 0; i < 5; i++)
		{
			Vector2I? candidate = FindNearbyPreferredCell(layout, generator, center, seed ^ (ulong)(i * 99), SwampZoneType.Bank, SwampZoneType.ShallowWater);
			if (candidate == null)
				continue;
			string sprite = i < 3
				? bankSprites[i % bankSprites.Length]
				: accentSprites[(i - 3) % accentSprites.Length];
			float collisionOffset = sprite.Contains("fallen_log") ? 2f : 0f;
			AddPlacement(layout, candidate.Value, sprite, null, 0f, GetCollisionRadius(sprite), collisionOffset, 1);
		}
	}

	private static void DecorateWetStructureCluster(
		SwampPropLayout layout,
		WorldGenerator generator,
		Vector2I center,
		ulong seed)
	{
		string[] decorations =
		{
			"assets/props/swamp/prop_reeds.png",
			"assets/props/swamp/prop_old_post.png",
			"assets/props/swamp/prop_hanging_moss.png",
			"assets/props/swamp/prop_rotten_stump.png",
			"assets/props/swamp/prop_vine_curtain.png"
		};

		for (int i = 0; i < 3; i++)
		{
			Vector2I? candidate = FindNearbyPreferredCell(layout, generator, center, seed ^ (ulong)(i * 71), SwampZoneType.WetClearing, SwampZoneType.Bank);
			if (candidate == null)
				continue;

			string sprite = decorations[(int)((HashCell(candidate.Value, seed) + (uint)i) % (uint)decorations.Length)];
			AddPlacement(layout, candidate.Value, sprite, null, 0f, GetCollisionRadius(sprite), 0f, 1);
		}
	}

	private static string PickDeadGroveSatellite(bool boundTree, bool mossy, int index, ulong seed, List<string> usedSatellites)
	{
		List<string> pool = new();
		if (boundTree)
		{
			pool.Add("assets/props/swamp/prop_root_mass_large.png");
			pool.Add("assets/props/swamp/prop_hanging_moss.png");
			pool.Add("assets/props/swamp/prop_bone_pile.png");
		}
		else if (mossy)
		{
			pool.Add("assets/props/swamp/prop_root_mass_large.png");
			pool.Add("assets/props/swamp/prop_hanging_moss.png");
			pool.Add("assets/props/swamp/prop_vine_curtain.png");
		}
		else
		{
			pool.Add("assets/props/swamp/prop_aerial_roots.png");
			pool.Add("assets/props/swamp/prop_aerial_roots_twisted.png");
			pool.Add("assets/props/swamp/prop_root_mass_large.png");
			pool.Add("assets/props/swamp/prop_vine_curtain.png");
		}

		for (int attempt = 0; attempt < pool.Count; attempt++)
		{
			string candidate = pool[(int)((seed + (ulong)(index * 7 + attempt)) % (ulong)pool.Count)];
			if (!usedSatellites.Contains(candidate))
				return candidate;
		}

		return pool[0];
	}

	private static string PickDeadGroveStructure(bool boundTree, bool mossy, ulong seed)
	{
		string[] pool = boundTree
			? new[]
			{
				"assets/props/swamp/prop_dead_tree_small.png",
				"assets/props/swamp/prop_fallen_log.png",
				"assets/props/swamp/prop_root_mass_large.png"
			}
			: (mossy
				? new[]
				{
					"assets/props/swamp/prop_dead_tree_small.png",
					"assets/props/swamp/prop_fallen_log.png",
					"assets/props/swamp/prop_vine_curtain.png"
				}
				: new[]
				{
					"assets/props/swamp/prop_dead_tree_small.png",
					"assets/props/swamp/prop_aerial_roots_twisted.png",
					"assets/props/swamp/prop_fallen_log.png"
				});

		return pool[(int)(seed % (ulong)pool.Length)];
	}

	private static string PickWetStructureSprite(Vector2I anchor, ulong seed)
	{
		string[] pool =
		{
			"assets/props/swamp/prop_fallen_log.png",
			"assets/props/swamp/prop_dead_tree_small.png",
			"assets/props/swamp/prop_aerial_roots_twisted.png",
			"assets/props/swamp/prop_root_mass_large.png",
			"assets/props/swamp/prop_old_post.png"
		};

		return pool[(int)(HashCell(anchor, seed ^ 0x3131UL) % (uint)pool.Length)];
	}

	private static void MarkZoneRadius(
		Dictionary<Vector2I, SwampZoneType> zones,
		Vector2I center,
		int radius,
		SwampZoneType zone,
		SwampZoneType onlyReplace)
	{
		foreach (KeyValuePair<Vector2I, SwampZoneType> kv in new Dictionary<Vector2I, SwampZoneType>(zones))
		{
			Vector2I cell = kv.Key;
			if ((cell - center).LengthSquared() > radius * radius)
				continue;
			if (zones[cell] != onlyReplace)
				continue;
			zones[cell] = zone;
		}
	}

	private static List<Vector2I> GetCellsForZones(SwampPropLayout layout, params SwampZoneType[] zones)
	{
		HashSet<SwampZoneType> allowed = new(zones);
		List<Vector2I> result = new();
		foreach (KeyValuePair<Vector2I, SwampZoneType> kv in layout.Zones)
		{
			if (allowed.Contains(kv.Value))
				result.Add(kv.Key);
		}

		return result;
	}

	private static Vector2I? PickSingleCell(
		List<Vector2I> candidates,
		HashSet<Vector2I> reservedCells,
		int minDistance,
		params Vector2I?[] avoidCells)
	{
		foreach (Vector2I cell in candidates)
		{
			if (reservedCells.Contains(cell))
				continue;

			bool tooClose = false;
			foreach (Vector2I? avoid in avoidCells)
			{
				if (avoid == null)
					continue;
				Vector2I other = avoid.Value;
				int dx = other.X - cell.X;
				int dy = other.Y - cell.Y;
				if (dx * dx + dy * dy < minDistance * minDistance)
				{
					tooClose = true;
					break;
				}
			}

			if (!tooClose)
				return cell;
		}

		return null;
	}

	private static Vector2I? FindNearbyPreferredCell(
		SwampPropLayout layout,
		WorldGenerator generator,
		Vector2I center,
		ulong seed,
		params SwampZoneType[] preferredZones)
	{
		HashSet<SwampZoneType> allowed = new(preferredZones);
		int start = (int)(HashCell(center, seed) % (uint)NearOffsets.Length);
		for (int i = 0; i < NearOffsets.Length; i++)
		{
			Vector2I candidate = center + NearOffsets[(start + i) % NearOffsets.Length];
			if (!layout.Zones.TryGetValue(candidate, out SwampZoneType zone) || !allowed.Contains(zone))
				continue;
			if (!generator.IsWithinBounds(candidate.X, candidate.Y) || generator.IsErased(candidate.X, candidate.Y))
				continue;
			if (layout.ReservedCells.Contains(candidate))
				continue;
			return candidate;
		}

		return null;
	}

	private static Vector2I? FindNearbyZoneCell(
		SwampPropLayout layout,
		WorldGenerator generator,
		Vector2I center,
		ulong seed,
		SwampZoneType zone)
	{
		int start = (int)(HashCell(center, seed) % (uint)NearOffsets.Length);
		for (int i = 0; i < NearOffsets.Length; i++)
		{
			Vector2I candidate = center + NearOffsets[(start + i) % NearOffsets.Length];
			if (!layout.Zones.TryGetValue(candidate, out SwampZoneType candidateZone) || candidateZone != zone)
				continue;
			if (!generator.IsWithinBounds(candidate.X, candidate.Y) || generator.IsErased(candidate.X, candidate.Y))
				continue;
			if (layout.ReservedCells.Contains(candidate))
				continue;
			if (generator.GetTerrain(candidate.X, candidate.Y) == TerrainType.Water && zone != SwampZoneType.ShallowWater)
				continue;
			return candidate;
		}

		return null;
	}

	private static void AddPlacement(
		SwampPropLayout layout,
		Vector2I cell,
		string basePath,
		string canopyPath,
		float canopyOffsetY,
		float collisionRadius,
		float collisionOffsetY,
		int reserveRadius)
	{
		layout.Placements.Add(new SwampPropPlacement
		{
			Cell = cell,
			BasePath = basePath,
			CanopyPath = canopyPath,
			CanopyOffsetY = canopyOffsetY,
			CollisionRadius = collisionRadius,
			CollisionOffsetY = collisionOffsetY,
		});

		Reserve(layout.ReservedCells, cell, reserveRadius);
	}

	private static void Reserve(HashSet<Vector2I> reservedCells, Vector2I center, int radius)
	{
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				if (dx * dx + dy * dy > radius * radius)
					continue;
				reservedCells.Add(new Vector2I(center.X + dx, center.Y + dy));
			}
		}
	}

	private static List<Vector2I> PickSpacedCells(List<Vector2I> cells, int count, int minDistance, HashSet<Vector2I> reservedCells)
	{
		List<Vector2I> chosen = new();
		foreach (Vector2I cell in cells)
		{
			if (chosen.Count >= count)
				break;
			if (reservedCells.Contains(cell))
				continue;

			bool tooClose = false;
			foreach (Vector2I existing in chosen)
			{
				int dx = existing.X - cell.X;
				int dy = existing.Y - cell.Y;
				if (dx * dx + dy * dy < minDistance * minDistance)
				{
					tooClose = true;
					break;
				}
			}

			if (!tooClose)
				chosen.Add(cell);
		}

		return chosen;
	}

	private static bool IsNearAny(Vector2I cell, List<Vector2I> others, int minDistance)
	{
		foreach (Vector2I other in others)
		{
			int dx = other.X - cell.X;
			int dy = other.Y - cell.Y;
			if (dx * dx + dy * dy < minDistance * minDistance)
				return true;
		}

		return false;
	}

	private static void SortCells(List<Vector2I> cells, ulong seed)
	{
		cells.Sort((left, right) =>
		{
			uint leftHash = HashCell(left, seed);
			uint rightHash = HashCell(right, seed);
			return leftHash.CompareTo(rightHash);
		});
	}

	private static bool IsShallowWater(WorldGenerator generator, int x, int y)
	{
		foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
		{
			int nx = x + dir.X;
			int ny = y + dir.Y;
			if (!generator.IsWithinBounds(nx, ny) || generator.IsErased(nx, ny))
				continue;
			if (generator.GetBiomeId(nx, ny) != "swamp")
				continue;
			if (generator.GetTerrain(nx, ny) != TerrainType.Water)
				return true;
		}

		return false;
	}

	private static int GetNearestWaterDistance(WorldGenerator generator, int x, int y, int maxDistance)
	{
		for (int radius = 1; radius <= maxDistance; radius++)
		{
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
						continue;
					int nx = x + dx;
					int ny = y + dy;
					if (!generator.IsWithinBounds(nx, ny) || generator.IsErased(nx, ny))
						continue;
					if (generator.GetBiomeId(nx, ny) != "swamp")
						continue;
					if (generator.GetTerrain(nx, ny) == TerrainType.Water)
						return radius;
				}
			}
		}

		return maxDistance + 1;
	}

	private static int PatchRoll(int x, int y, int size, ulong seed)
	{
		int patchX = Mathf.FloorToInt(x / (float)size);
		int patchY = Mathf.FloorToInt(y / (float)size);
		return (int)(HashCell(new Vector2I(patchX, patchY), seed) % 100);
	}

	private static float GetCollisionRadius(string path)
	{
		int slash = path.LastIndexOf('/');
		string filename = slash >= 0 ? path[(slash + 1)..] : path;
		return CollisionRadii.TryGetValue(filename, out float radius) ? radius : 0f;
	}

	private static Texture2D LoadCached(string path, Dictionary<string, Texture2D> cache)
	{
		if (cache.TryGetValue(path, out Texture2D cached))
			return cached;

		string resPath = path.StartsWith("res://") ? path : $"res://{path}";
		if (!ResourceLoader.Exists(resPath))
		{
			GD.PrintErr($"[SwampPropPlacer] Texture NOT FOUND: {resPath}");
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
