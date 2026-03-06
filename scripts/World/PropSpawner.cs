using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Données d'un type de prop chargées depuis JSON.
/// </summary>
public class PropDefinition
{
	public string Id;
	public string Name;
	public float Weight;
	public List<string> Terrain = new();
	public string SpriteBasePath;
	public string SpriteCanopyPath;
	public float CanopyOffsetY;
	public float CollisionRadius;
	public float CollisionOffsetY;
	public int MinDistance;
	public int Variants;
}

/// <summary>
/// Config d'un biome de props.
/// </summary>
public class BiomePropConfig
{
	public string BiomeId;
	public float Density;
	public List<PropDefinition> Props = new();
}

/// <summary>
/// Spawn procédural de décors d'environnement par biome.
/// Utilise un bruit de Perlin pour la densité + weighted random pour le type de prop.
/// Gère le fondu de canopée quand le joueur passe dessous.
/// </summary>
public partial class PropSpawner : Node2D
{
	// Tous les props assez grands pour cacher le joueur (arbres, rochers, ruines, voitures)
	private readonly List<EnvironmentProp> _solidProps = new();
	private Node2D _player;
	// Distance horizontale max pour détecter si le joueur est "derrière" un prop
	private const float OcclusionDistanceX = 20f;
	// Transparence minimum quand le joueur est complètement derrière
	private const float OccludedAlpha = 0.35f;

	/// <summary>
	/// Spawn tous les props pour la carte générée.
	/// Appelé par WorldSetup après la génération du terrain et le spawn des ressources.
	/// </summary>
	public void SpawnProps(
		WorldGenerator generator,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		ulong seed)
	{
		// Charger les configs de props par biome
		Dictionary<string, BiomePropConfig> configs = LoadPropConfigs();
		if (configs.Count == 0)
		{
			GD.PrintErr("[PropSpawner] No prop configs found — aborting spawn");
			return;
		}

		// Cache de textures pour éviter les chargements multiples
		Dictionary<string, Texture2D> textureCache = new();

		// Bruit de Perlin pour la distribution naturelle
		FastNoiseLite noise = new()
		{
			Seed = (int)(seed & 0x7FFFFFFF),
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = 0.08f,
			FractalOctaves = 2,
		};

		int radius = generator.MapRadius;
		int safeRadius = radius - 5;
		int foyerExclusion = generator.FoyerClearance + 2;
		int totalSpawned = 0;

		// Debug counters
		int cellsEvaluated = 0;
		int skipBiomeConfig = 0;
		int skipDensity = 0;
		int skipTerrain = 0;
		int skipDistance = 0;
		int skipTexture = 0;

		GD.Print($"[PropSpawner] Starting spawn: radius={radius} safe={safeRadius} foyer={foyerExclusion} seed={seed}");
		GD.Print($"[PropSpawner] Configs loaded for biomes: [{string.Join(", ", configs.Keys)}]");

		for (int x = -safeRadius; x <= safeRadius; x++)
		{
			for (int y = -safeRadius; y <= safeRadius; y++)
			{
				if (x * x + y * y > safeRadius * safeRadius)
					continue;

				if (Mathf.Abs(x) <= foyerExclusion && Mathf.Abs(y) <= foyerExclusion)
					continue;

				if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
					continue;

				TerrainType terrain = generator.GetTerrain(x, y);
				if (terrain == TerrainType.Water)
					continue;

				Vector2I cell = new(x, y);
				if (usedCells.Contains(cell))
					continue;

				cellsEvaluated++;

				BiomeData biome = generator.GetBiome(x, y);
				if (biome == null)
					continue;

				if (!configs.TryGetValue(biome.Id, out BiomePropConfig config))
				{
					skipBiomeConfig++;
					continue;
				}

				// Le bruit Perlin crée des zones denses et des zones vides.
				// spawnChance = density modulée par le bruit (zones de clustering naturel)
				float noiseVal = (noise.GetNoise2D(x, y) + 1f) * 0.5f; // 0..1
				float spawnChance = config.Density * (0.5f + noiseVal); // 0..density*1.5
				uint cellHash = (uint)((x * 48611) ^ (y * 96293)) & 0x7FFFFFFF;
				float roll = (cellHash % 10000) / 10000f; // 0..1
				if (roll > spawnChance)
				{
					skipDensity++;
					continue;
				}

				string terrainName = terrain.ToString().ToLowerInvariant();
				PropDefinition prop = PickProp(config, terrainName, x, y);
				if (prop == null)
				{
					skipTerrain++;
					continue;
				}

				if (prop.MinDistance > 1 && IsTooCloseToUsed(cell, usedCells, prop.MinDistance))
				{
					skipDistance++;
					continue;
				}

				usedCells.Add(cell);

				Texture2D baseTex = LoadTextureCached(prop.SpriteBasePath, textureCache);
				if (baseTex == null)
				{
					skipTexture++;
					continue;
				}

				Texture2D canopyTex = null;
				if (!string.IsNullOrEmpty(prop.SpriteCanopyPath))
					canopyTex = LoadTextureCached(prop.SpriteCanopyPath, textureCache);

				Vector2 worldPos = ground.MapToLocal(cell);
				EnvironmentProp envProp = new();
				envProp.GlobalPosition = worldPos;
				container.AddChild(envProp);
				envProp.Initialize(baseTex, canopyTex, prop.CanopyOffsetY, prop.CollisionRadius, prop.CollisionOffsetY);

				// Tracker tous les props assez hauts pour cacher le joueur
				if (envProp.BaseHeight >= 16 || envProp.HasCanopy)
					_solidProps.Add(envProp);

				totalSpawned++;
			}
		}

		GD.Print($"[PropSpawner] === SPAWN REPORT ===");
		GD.Print($"[PropSpawner] Cells evaluated: {cellsEvaluated}");
		GD.Print($"[PropSpawner] Skip no biome config: {skipBiomeConfig}");
		GD.Print($"[PropSpawner] Skip density filter: {skipDensity}");
		GD.Print($"[PropSpawner] Skip terrain mismatch: {skipTerrain}");
		GD.Print($"[PropSpawner] Skip too close: {skipDistance}");
		GD.Print($"[PropSpawner] Skip texture missing: {skipTexture}");
		GD.Print($"[PropSpawner] TOTAL SPAWNED: {totalSpawned} ({_solidProps.Count} with occlusion tracking)");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_solidProps.Count == 0)
			return;

		if (_player == null || !IsInstanceValid(_player))
		{
			Node playerNode = GetTree().GetFirstNodeInGroup("player");
			if (playerNode is Node2D p)
				_player = p;
			else
				return;
		}

		Vector2 playerPos = _player.GlobalPosition;

		// Technique iso classique : le prop devient transparent quand le joueur
		// est "derrière" lui (= position Y du joueur > position Y du prop,
		// et assez proche horizontalement pour être caché visuellement).
		for (int i = _solidProps.Count - 1; i >= 0; i--)
		{
			EnvironmentProp prop = _solidProps[i];
			if (!IsInstanceValid(prop))
			{
				_solidProps.RemoveAt(i);
				continue;
			}

			Vector2 propPos = prop.GlobalPosition;
			float dx = Mathf.Abs(playerPos.X - propPos.X);
			// dy négatif = joueur plus haut sur l'écran = "derrière" en iso
			float dy = playerPos.Y - propPos.Y;

			bool playerBehind = dy < 8f && dy > -prop.BaseHeight && dx < OcclusionDistanceX;

			if (playerBehind)
			{
				float tX = 1f - (dx / OcclusionDistanceX);
				float alpha = Mathf.Lerp(1f, OccludedAlpha, tX);
				prop.SetOverallTransparency(alpha);
			}
			else
			{
				prop.SetOverallTransparency(1f);
			}
		}
	}

	private PropDefinition PickProp(BiomePropConfig config, string terrainName, int x, int y)
	{
		float totalWeight = 0f;
		List<PropDefinition> candidates = new();

		foreach (PropDefinition prop in config.Props)
		{
			if (prop.Terrain.Contains(terrainName))
			{
				candidates.Add(prop);
				totalWeight += prop.Weight;
			}
		}

		if (candidates.Count == 0 || totalWeight <= 0f)
			return null;

		uint hash = (uint)((x * 73856093) ^ (y * 19349663)) & 0x7FFFFFFF;
		float roll = (hash % 10000) / 10000f * totalWeight;

		float cumulative = 0f;
		foreach (PropDefinition prop in candidates)
		{
			cumulative += prop.Weight;
			if (roll < cumulative)
				return prop;
		}

		return candidates[candidates.Count - 1];
	}

	private static bool IsTooCloseToUsed(Vector2I cell, HashSet<Vector2I> usedCells, int minDist)
	{
		for (int dx = -minDist; dx <= minDist; dx++)
		{
			for (int dy = -minDist; dy <= minDist; dy++)
			{
				if (dx == 0 && dy == 0)
					continue;
				if (usedCells.Contains(new Vector2I(cell.X + dx, cell.Y + dy)))
					return true;
			}
		}
		return false;
	}

	private static Texture2D LoadTextureCached(string path, Dictionary<string, Texture2D> cache)
	{
		if (string.IsNullOrEmpty(path))
			return null;

		if (cache.TryGetValue(path, out Texture2D cached))
			return cached;

		string resPath = path.StartsWith("res://") ? path : $"res://{path}";
		if (!ResourceLoader.Exists(resPath))
		{
			GD.PrintErr($"[PropSpawner] Texture NOT FOUND: {resPath}");
			cache[path] = null;
			return null;
		}

		Texture2D tex = GD.Load<Texture2D>(resPath);
		cache[path] = tex;
		return tex;
	}

	// =========================================================================
	// === Chargement des configs JSON ===
	// =========================================================================

	private static Dictionary<string, BiomePropConfig> LoadPropConfigs()
	{
		Dictionary<string, BiomePropConfig> configs = new();

		DirAccess dir = DirAccess.Open("res://data/props");
		if (dir == null)
		{
			GD.PrintErr("[PropSpawner] Cannot open data/props/ directory!");
			return configs;
		}

		dir.ListDirBegin();
		string fileName = dir.GetNext();
		int fileCount = 0;
		while (!string.IsNullOrEmpty(fileName))
		{
			GD.Print($"[PropSpawner] Found in data/props/: '{fileName}'");
			if (fileName.EndsWith(".json") && !fileName.StartsWith("_"))
			{
				BiomePropConfig config = LoadPropConfig($"res://data/props/{fileName}");
				if (config != null)
				{
					configs[config.BiomeId] = config;
					GD.Print($"[PropSpawner] => Registered biome '{config.BiomeId}' ({config.Props.Count} prop types, density={config.Density})");
				}
				else
				{
					GD.PrintErr($"[PropSpawner] => FAILED to load {fileName}");
				}
			}
			fileCount++;
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();

		GD.Print($"[PropSpawner] Directory scan done: {fileCount} entries, {configs.Count} configs loaded");
		return configs;
	}

	private static BiomePropConfig LoadPropConfig(string path)
	{
		FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"[PropSpawner] Cannot open file: {path}");
			return null;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PrintErr($"[PropSpawner] JSON parse error in {path}: {json.GetErrorMessage()}");
			return null;
		}

		Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
		BiomePropConfig config = new()
		{
			BiomeId = dict["biome_id"].AsString(),
			Density = (float)dict["density"].AsDouble(),
		};

		Godot.Collections.Array propsArray = dict["props"].AsGodotArray();
		foreach (Variant propVar in propsArray)
		{
			Godot.Collections.Dictionary propDict = propVar.AsGodotDictionary();
			PropDefinition prop = new()
			{
				Id = propDict["id"].AsString(),
				Name = propDict.ContainsKey("name") ? propDict["name"].AsString() : "",
				Weight = (float)propDict["weight"].AsDouble(),
				SpriteBasePath = propDict["sprite_base"].AsString(),
				CollisionRadius = (float)propDict["collision_radius"].AsDouble(),
				CollisionOffsetY = propDict.ContainsKey("collision_offset_y") ? (float)propDict["collision_offset_y"].AsDouble() : 0f,
				CanopyOffsetY = propDict.ContainsKey("canopy_offset_y") ? (float)propDict["canopy_offset_y"].AsDouble() : 0f,
				MinDistance = propDict.ContainsKey("min_distance") ? (int)propDict["min_distance"].AsDouble() : 1,
				Variants = propDict.ContainsKey("variants") ? (int)propDict["variants"].AsDouble() : 1,
			};

			if (propDict.ContainsKey("sprite_canopy"))
				prop.SpriteCanopyPath = propDict["sprite_canopy"].AsString();

			if (propDict.ContainsKey("terrain"))
			{
				Godot.Collections.Array terrainArray = propDict["terrain"].AsGodotArray();
				foreach (Variant t in terrainArray)
					prop.Terrain.Add(t.AsString());
			}

			config.Props.Add(prop);
		}

		return config;
	}
}
