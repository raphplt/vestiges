using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Place les props structurels du layout urbain :
/// murs de batiments, mobilier de rue, landmarks.
/// Appele une fois lors de la generation, apres PropSpawner.
/// </summary>
public static class UrbanPropPlacer
{
	// Chemins des sprites (relatifs a res://)
	private static readonly string[] WallSprites = {
		"assets/props/urban_ruins/prop_concrete_wall.png",
		"assets/props/urban_ruins/prop_concrete_wall_v2.png",
		"assets/props/urban_ruins/prop_concrete_wall_v3.png",
		"assets/props/urban_ruins/prop_brick_wall.png",
	};
	// Poids relatifs : intact, fissure, effondre, brique
	private static readonly float[] WallWeights = { 1.8f, 1.2f, 0.8f, 1.0f };

	private static readonly Dictionary<string, float> RoadProps = new()
	{
		{ "assets/props/urban_ruins/prop_urban_car.png", 0.04f },
		{ "assets/props/urban_ruins/prop_concrete_debris.png", 0.10f },
		{ "assets/props/urban_ruins/prop_concrete_debris_v2.png", 0.08f },
		{ "assets/props/urban_ruins/prop_concrete_debris_v3.png", 0.06f },
	};

	private static readonly Dictionary<string, float> SidewalkProps = new()
	{
		{ "assets/props/urban_ruins/prop_dumpster.png", 0.015f },
		{ "assets/props/urban_ruins/prop_mailbox.png", 0.015f },
		{ "assets/props/urban_ruins/prop_traffic_light.png", 0.02f },
		{ "assets/props/urban_ruins/prop_phone_booth.png", 0.008f },
		{ "assets/props/urban_ruins/prop_torn_billboard.png", 0.008f },
		{ "assets/props/urban_ruins/prop_graffiti_wall.png", 0.01f },
	};

	private static readonly string[] LandmarkSprites = {
		"assets/props/urban_ruins/prop_collapsed_building.png",
		"assets/props/urban_ruins/prop_collapsed_stairs.png",
		"assets/props/urban_ruins/prop_supermarket_shelves.png",
	};

	// Collision radii correspondant aux sprites
	private static readonly Dictionary<string, float> CollisionRadii = new()
	{
		{ "prop_concrete_wall.png", 10f },
		{ "prop_concrete_wall_v2.png", 10f },
		{ "prop_concrete_wall_v3.png", 10f },
		{ "prop_brick_wall.png", 8f },
		{ "prop_urban_car.png", 12f },
		{ "prop_traffic_light.png", 3f },
		{ "prop_dumpster.png", 4f },
		{ "prop_phone_booth.png", 4f },
		{ "prop_torn_billboard.png", 5f },
		{ "prop_graffiti_wall.png", 6f },
		{ "prop_collapsed_building.png", 16f },
		{ "prop_collapsed_stairs.png", 8f },
		{ "prop_supermarket_shelves.png", 8f },
	};

	public static void PlaceProps(
		UrbanLayout layout,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		ulong seed)
	{
		RandomNumberGenerator rng = new() { Seed = seed ^ 0xBEEFCAFE };
		Dictionary<string, Texture2D> cache = new();
		int wallCount = 0;
		int furnitureCount = 0;

		// 1. Murs de batiments
		foreach (Vector2I wallCell in layout.WallCells)
		{
			if (usedCells.Contains(wallCell))
				continue;

			// Choisir le sprite de mur (weighted random biaise par integrity)
			int gx = wallCell.X + layout.MapRadius;
			int gy = wallCell.Y + layout.MapRadius;
			uint hash = (uint)((wallCell.X * 73856) ^ (wallCell.Y * 19349)) & 0x7FFFFFFF;
			string sprite = PickWallSprite(hash);

			Texture2D tex = LoadCached(sprite, cache);
			if (tex == null) continue;

			float radius = GetCollisionRadius(sprite);
			Vector2 worldPos = ground.MapToLocal(wallCell);

			EnvironmentProp prop = new();
			prop.GlobalPosition = worldPos;
			container.AddChild(prop);
			prop.Initialize(tex, null, 0f, radius, 0f);

			usedCells.Add(wallCell);
			wallCount++;
		}

		// 2. Mobilier de rue
		foreach (Vector2I roadCell in layout.RoadCells)
		{
			if (usedCells.Contains(roadCell))
				continue;

			foreach (KeyValuePair<string, float> kv in RoadProps)
			{
				uint hash = (uint)((roadCell.X * 48611) ^ (roadCell.Y * 96293) ^ kv.Key.GetHashCode()) & 0x7FFFFFFF;
				float roll = (hash % 10000) / 10000f;
				if (roll > kv.Value)
					continue;

				Texture2D tex = LoadCached(kv.Key, cache);
				if (tex == null) continue;

				float radius = GetCollisionRadius(kv.Key);
				Vector2 worldPos = ground.MapToLocal(roadCell);

				EnvironmentProp prop = new();
				prop.GlobalPosition = worldPos;
				container.AddChild(prop);
				prop.Initialize(tex, null, 0f, radius, 0f);

				usedCells.Add(roadCell);
				furnitureCount++;
				break; // Un seul prop par cell de route
			}
		}

		// 3. Mobilier de trottoir
		foreach (Vector2I swCell in layout.SidewalkCells)
		{
			if (usedCells.Contains(swCell))
				continue;

			foreach (KeyValuePair<string, float> kv in SidewalkProps)
			{
				uint hash = (uint)((swCell.X * 12345) ^ (swCell.Y * 67891) ^ kv.Key.GetHashCode()) & 0x7FFFFFFF;
				float roll = (hash % 10000) / 10000f;
				if (roll > kv.Value)
					continue;

				Texture2D tex = LoadCached(kv.Key, cache);
				if (tex == null) continue;

				float radius = GetCollisionRadius(kv.Key);
				Vector2 worldPos = ground.MapToLocal(swCell);

				EnvironmentProp prop = new();
				prop.GlobalPosition = worldPos;
				container.AddChild(prop);
				prop.Initialize(tex, null, 0f, radius, 0f);

				usedCells.Add(swCell);
				furnitureCount++;
				break;
			}
		}

		// 4. Landmarks aux intersections (cells qui sont route et adjacentes a 3+ routes)
		int landmarkCount = 0;
		List<Vector2I> intersections = FindIntersections(layout);
		foreach (Vector2I inter in intersections)
		{
			uint hash = (uint)((inter.X * 99991) ^ (inter.Y * 77773)) & 0x7FFFFFFF;
			float roll = (hash % 10000) / 10000f;
			if (roll > 0.20f)
				continue;

			// Placer le landmark sur une cell adjacente non occupee
			Vector2I? target = FindAdjacentFree(inter, usedCells, layout);
			if (target == null) continue;

			string sprite = LandmarkSprites[hash % (uint)LandmarkSprites.Length];
			Texture2D tex = LoadCached(sprite, cache);
			if (tex == null) continue;

			float radius = GetCollisionRadius(sprite);
			Vector2 worldPos = ground.MapToLocal(target.Value);

			EnvironmentProp prop = new();
			prop.GlobalPosition = worldPos;
			container.AddChild(prop);
			prop.Initialize(tex, null, 0f, radius, 0f);

			usedCells.Add(target.Value);
			landmarkCount++;
		}

		GD.Print($"[UrbanPropPlacer] Placed {wallCount} walls, {furnitureCount} road/sidewalk props, {landmarkCount} landmarks");
	}

	private static string PickWallSprite(uint hash)
	{
		float totalWeight = 0f;
		foreach (float w in WallWeights) totalWeight += w;

		float roll = (hash % 10000) / 10000f * totalWeight;
		float cumulative = 0f;
		for (int i = 0; i < WallSprites.Length; i++)
		{
			cumulative += WallWeights[i];
			if (roll < cumulative)
				return WallSprites[i];
		}
		return WallSprites[0];
	}

	private static float GetCollisionRadius(string path)
	{
		// Extraire le nom de fichier
		int lastSlash = path.LastIndexOf('/');
		string filename = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
		return CollisionRadii.TryGetValue(filename, out float r) ? r : 0f;
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

		Texture2D tex = GD.Load<Texture2D>(resPath);
		cache[path] = tex;
		return tex;
	}

	private static List<Vector2I> FindIntersections(UrbanLayout layout)
	{
		List<Vector2I> intersections = new();
		foreach (Vector2I cell in layout.RoadCells)
		{
			int adjacentRoads = 0;
			foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
			{
				if (layout.RoadCells.Contains(cell + dir))
					adjacentRoads++;
			}
			// Une intersection = cell de route avec routes dans les 4 directions
			if (adjacentRoads >= 3)
				intersections.Add(cell);
		}
		return intersections;
	}

	private static Vector2I? FindAdjacentFree(Vector2I center, HashSet<Vector2I> usedCells, UrbanLayout layout)
	{
		// Chercher une cell adjacente non occupee, de preference interieur batiment ou sidewalk
		foreach (Vector2I dir in new[] { new Vector2I(1, 1), new Vector2I(-1, 1), new Vector2I(1, -1), new Vector2I(-1, -1) })
		{
			Vector2I candidate = center + dir;
			if (!usedCells.Contains(candidate) && !layout.RoadCells.Contains(candidate))
				return candidate;
		}
		return null;
	}
}
