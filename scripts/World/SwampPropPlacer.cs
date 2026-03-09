using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

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
}

/// <summary>
/// Compose le biome marais à partir de zones locales : lisières d'eau,
/// bosquets morts, clairières humides et poches fongiques.
/// </summary>
public static class SwampPropPlacer
{
	private static readonly Vector2I[] RingOffsets = {
		new(-2, -1), new(-1, -2), new(1, -2), new(2, -1),
		new(-3, 1), new(3, 1), new(-2, 2), new(2, 2),
		new(0, 3), new(-1, 4), new(1, 4)
	};

	private static readonly Dictionary<string, float> CollisionRadii = new()
	{
		{ "prop_dead_tree_large_base.png", 8f },
		{ "prop_dead_tree_mossy_large.png", 8f },
		{ "prop_aerial_roots.png", 6f },
		{ "prop_root_mass_large.png", 7f },
		{ "prop_sunken_trunk_large.png", 8f },
		{ "prop_sunken_boat.png", 7f },
		{ "prop_drowned_cart.png", 6f },
		{ "prop_broken_walkway.png", 0f },
		{ "prop_bone_pile.png", 0f },
		{ "prop_reeds.png", 0f },
		{ "prop_lily_pads.png", 0f },
		{ "prop_fallen_log.png", 6f },
		{ "prop_spore_patch.png", 0f },
		{ "prop_toxic_mushrooms.png", 0f },
		{ "prop_swamp_lantern.png", 0f },
		{ "prop_vine_curtain.png", 0f },
		{ "prop_hanging_moss.png", 0f },
	};

	public static SwampPropLayout BuildLayout(WorldGenerator generator, ulong seed)
	{
		SwampPropLayout layout = new();
		List<Vector2I> groveCandidates = new();
		List<Vector2I> waterEdgeGround = new();
		List<Vector2I> shallowWater = new();
		List<Vector2I> fungalCandidates = new();
		List<Vector2I> wetOpenCandidates = new();
		List<Vector2I> swampGround = new();

		int radius = generator.MapRadius;
		for (int x = -radius; x <= radius; x++)
		{
			for (int y = -radius; y <= radius; y++)
			{
				if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
					continue;
				if (generator.GetBiomeId(x, y) != "swamp")
					continue;

				TerrainType terrain = generator.GetTerrain(x, y);
				Vector2I cell = new(x, y);

				if (terrain == TerrainType.Water)
				{
					if (IsShallowWater(generator, x, y))
						shallowWater.Add(cell);
					continue;
				}

				swampGround.Add(cell);

				bool edge = IsAdjacentToWater(generator, x, y, 1);
				if (edge)
					waterEdgeGround.Add(cell);

				if (terrain == TerrainType.Forest && !edge && PatchRoll(x, y, 6, seed ^ 0xA11DUL) < 58)
					groveCandidates.Add(cell);

				if (!edge && PatchRoll(x, y, 7, seed ^ 0xF00DUL) < 18)
					fungalCandidates.Add(cell);

				if (terrain == TerrainType.Grass && !edge && PatchRoll(x, y, 6, seed ^ 0x0BEEFUL) < 45)
					wetOpenCandidates.Add(cell);
			}
		}

		PlaceHeroTrees(layout, generator, seed, swampGround, groveCandidates);
		PlaceWaterlineLandmarks(layout, generator, seed, waterEdgeGround, shallowWater);
		PlaceFungalPockets(layout, generator, seed, fungalCandidates, wetOpenCandidates);
		PlaceWaterEdgeClusters(layout, generator, seed, waterEdgeGround, shallowWater);
		PlaceRareNarrativeProps(layout, generator, seed, waterEdgeGround, wetOpenCandidates, shallowWater);

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

	private static void PlaceHeroTrees(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> swampGround,
		List<Vector2I> groveCandidates)
	{
		List<Vector2I> anchors = groveCandidates.Count > 0 ? new(groveCandidates) : new(swampGround);
		SortCells(anchors, seed ^ 0x710FUL);

		int heroCount = Mathf.Clamp(swampGround.Count / 550, 2, 4);
		List<Vector2I> chosen = PickSpacedCells(anchors, heroCount, 18, layout.ReservedCells);

		foreach (Vector2I anchor in chosen)
		{
			uint hash = HashCell(anchor, seed ^ 0xBADA55UL);
			bool mossy = (hash % 100) < 45;
			AddPlacement(
				layout,
				anchor,
				mossy ? "assets/props/swamp/prop_dead_tree_mossy_large.png" : "assets/props/swamp/prop_dead_tree_large_base.png",
				mossy ? null : "assets/props/swamp/prop_dead_tree_large_canopy.png",
				mossy ? 0f : -54f,
				mossy ? 8f : 8f,
				0f,
				4);

			int satelliteCount = mossy ? 2 : 1 + (int)(hash % 2);
			for (int i = 0; i < satelliteCount; i++)
			{
				Vector2I? candidate = FindSatelliteCell(generator, layout.ReservedCells, anchor, seed ^ hash ^ (ulong)i);
				if (candidate == null)
					continue;

				string sprite = PickTreeSatelliteSprite(seed ^ hash ^ (ulong)(i * 17), mossy);
				AddPlacement(
					layout,
					candidate.Value,
					sprite,
					null,
					0f,
					GetCollisionRadius(sprite),
					0f,
					sprite.Contains("vine_curtain") || sprite.Contains("hanging_moss") ? 0 : 1);
			}
		}
	}

	private static void PlaceWaterlineLandmarks(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> waterEdgeGround,
		List<Vector2I> shallowWater)
	{
		List<Vector2I> shoreline = new(shallowWater);
		shoreline.AddRange(waterEdgeGround);
		SortCells(shoreline, seed ^ 0x5511UL);

		List<Vector2I> trunks = PickSpacedCells(shoreline, 3, 16, layout.ReservedCells);
		foreach (Vector2I cell in trunks)
		{
			AddPlacement(
				layout,
				cell,
				"assets/props/swamp/prop_sunken_trunk_large.png",
				null,
				0f,
				8f,
				0f,
				3);
		}
	}

	private static void PlaceFungalPockets(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> fungalCandidates,
		List<Vector2I> wetOpenCandidates)
	{
		List<Vector2I> pockets = fungalCandidates.Count > 0 ? new(fungalCandidates) : new(wetOpenCandidates);
		SortCells(pockets, seed ^ 0xFACEUL);

		List<Vector2I> centers = PickSpacedCells(pockets, 4, 14, layout.ReservedCells);
		foreach (Vector2I center in centers)
		{
			AddPlacement(layout, center, "assets/props/swamp/prop_spore_patch.png", null, 0f, 0f, 0f, 1);

			for (int i = 0; i < 3; i++)
			{
				Vector2I? candidate = FindSatelliteCell(generator, layout.ReservedCells, center, seed ^ (ulong)(center.X * 33 + center.Y * 19 + i));
				if (candidate == null)
					continue;

				string sprite = i == 2 && PatchRoll(center.X, center.Y, 4, seed ^ 0xCC44UL) < 28
					? "assets/props/swamp/prop_swamp_lantern.png"
					: "assets/props/swamp/prop_toxic_mushrooms.png";

				AddPlacement(layout, candidate.Value, sprite, null, 0f, GetCollisionRadius(sprite), 0f, 1);
			}
		}
	}

	private static void PlaceWaterEdgeClusters(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> waterEdgeGround,
		List<Vector2I> shallowWater)
	{
		List<Vector2I> centers = new(waterEdgeGround);
		SortCells(centers, seed ^ 0x1234UL);
		List<Vector2I> chosen = PickSpacedCells(centers, Mathf.Clamp(waterEdgeGround.Count / 120, 4, 8), 10, new HashSet<Vector2I>());

		foreach (Vector2I center in chosen)
		{
			for (int i = 0; i < 4; i++)
			{
				Vector2I offset = RingOffsets[(i + (int)(HashCell(center, seed) % (uint)RingOffsets.Length)) % RingOffsets.Length];
				Vector2I target = center + offset;
				if (!generator.IsWithinBounds(target.X, target.Y) || generator.IsErased(target.X, target.Y))
					continue;
				if (generator.GetBiomeId(target.X, target.Y) != "swamp")
					continue;
				if (layout.ReservedCells.Contains(target))
					continue;

				TerrainType terrain = generator.GetTerrain(target.X, target.Y);
				string sprite = terrain == TerrainType.Water
					? "assets/props/swamp/prop_lily_pads.png"
					: (i % 3 == 0 ? "assets/props/swamp/prop_fallen_log.png" : "assets/props/swamp/prop_reeds.png");

				AddPlacement(layout, target, sprite, null, 0f, GetCollisionRadius(sprite), sprite.Contains("fallen_log") ? 2f : 0f, 1);
			}
		}
	}

	private static void PlaceRareNarrativeProps(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> waterEdgeGround,
		List<Vector2I> wetOpenCandidates,
		List<Vector2I> shallowWater)
	{
		PlaceRareProp(layout, generator, seed ^ 0x9911UL, shallowWater, "assets/props/swamp/prop_sunken_boat.png", 3);
		PlaceRareProp(layout, generator, seed ^ 0x8822UL, waterEdgeGround, "assets/props/swamp/prop_broken_walkway.png", 2);
		PlaceRareProp(layout, generator, seed ^ 0x7733UL, wetOpenCandidates, "assets/props/swamp/prop_drowned_cart.png", 2);
		PlaceRareProp(layout, generator, seed ^ 0x6644UL, wetOpenCandidates, "assets/props/swamp/prop_bone_pile.png", 1);
	}

	private static void PlaceRareProp(
		SwampPropLayout layout,
		WorldGenerator generator,
		ulong seed,
		List<Vector2I> candidates,
		string sprite,
		int reserveRadius)
	{
		if (candidates.Count == 0)
			return;

		List<Vector2I> sorted = new(candidates);
		SortCells(sorted, seed);
		foreach (Vector2I cell in sorted)
		{
			if (layout.ReservedCells.Contains(cell))
				continue;
			AddPlacement(layout, cell, sprite, null, 0f, GetCollisionRadius(sprite), sprite.Contains("fallen_log") ? 2f : 0f, reserveRadius);
			return;
		}
	}

	private static Vector2I? FindSatelliteCell(WorldGenerator generator, HashSet<Vector2I> reservedCells, Vector2I center, ulong seed)
	{
		int start = (int)(HashCell(center, seed) % (uint)RingOffsets.Length);
		for (int i = 0; i < RingOffsets.Length; i++)
		{
			Vector2I candidate = center + RingOffsets[(start + i) % RingOffsets.Length];
			if (!generator.IsWithinBounds(candidate.X, candidate.Y) || generator.IsErased(candidate.X, candidate.Y))
				continue;
			if (generator.GetBiomeId(candidate.X, candidate.Y) != "swamp")
				continue;
			if (reservedCells.Contains(candidate))
				continue;
			if (generator.GetTerrain(candidate.X, candidate.Y) == TerrainType.Water)
				continue;

			return candidate;
		}

		return null;
	}

	private static string PickTreeSatelliteSprite(ulong seed, bool mossy)
	{
		string[] drySet = {
			"assets/props/swamp/prop_aerial_roots.png",
			"assets/props/swamp/prop_root_mass_large.png",
			"assets/props/swamp/prop_vine_curtain.png"
		};

		string[] mossySet = {
			"assets/props/swamp/prop_root_mass_large.png",
			"assets/props/swamp/prop_hanging_moss.png",
			"assets/props/swamp/prop_vine_curtain.png"
		};

		string[] pool = mossy ? mossySet : drySet;
		return pool[seed % (ulong)pool.Length];
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
				if (existing.DistanceSquaredTo(cell) < minDistance * minDistance)
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

	private static void SortCells(List<Vector2I> cells, ulong seed)
	{
		cells.Sort((left, right) =>
		{
			uint leftHash = HashCell(left, seed);
			uint rightHash = HashCell(right, seed);
			return leftHash.CompareTo(rightHash);
		});
	}

	private static bool IsAdjacentToWater(WorldGenerator generator, int x, int y, int radius)
	{
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				if (dx == 0 && dy == 0)
					continue;
				if (generator.GetTerrain(x + dx, y + dy) == TerrainType.Water)
					return true;
			}
		}

		return false;
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
