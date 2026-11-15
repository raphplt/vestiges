using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
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
	public bool Blocking;
	public float FootprintScale = 1f;
	public int MinDistance;
}

/// <summary>
/// Config d'un biome de props.
/// </summary>
public class BiomePropConfig
{
	public string BiomeId;
	public float Density;
	public List<PropDefinition> Props = new();
	/// <summary>Décors remarquables posés une fois chacun sur une terre de ce biome, loin les uns des autres (plan 58).</summary>
	public List<string> Remarkables = new();
	/// <summary>Terrain rythmé du biome (plan 58 T1a), nul si le semis reste uniforme.</summary>
	public TerrainRhythm Rhythm;
	/// <summary>Grille de la ville (plan 58 T3a), nulle hors de la Haute-Ville.</summary>
	public CityGrid City;
}

/// <summary>
/// Spawn procédural de décors d'environnement par biome.
/// Utilise un bruit de Perlin pour la densité + weighted random pour le type de prop.
/// La transparence d'occlusion est gérée par PropOcclusion pour tous les placeurs.
/// </summary>
public partial class PropSpawner : Node2D
{

	/// <summary>
	/// Spawn tous les props pour la carte générée.
	/// Appelé par WorldSetup après la génération du terrain.
	/// </summary>
	/// <param name="sliceSpent">Vrai quand la tranche de temps de chargement est épuisée : le placeur rend alors la main.</param>
	/// <param name="yieldSlice">Rend la main au moteur pour une image et ouvre une nouvelle tranche.</param>
	public async Task SpawnPropsAsync(
		WorldGenerator generator,
		TerrainType[,] terrainGrid,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		ulong seed,
		Func<bool> sliceSpent,
		Func<Task> yieldSlice,
		HashSet<Vector2I> blockedCells = null,
		WildFieldsLayout wildFieldsLayout = null,
		byte[,] rhythmZones = null,
		UrbanLayout urbanLayout = null,
		HashSet<Vector2I> pathCells = null)
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
		using FastNoiseLite noise = new()
		{
			Seed = (int)(seed & 0x7FFFFFFF),
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = 0.08f,
			FractalOctaves = 2,
		};

		int radius = generator.MapRadiusX;
		int safeRadius = radius - 5;
		int safeRadiusY = generator.MapRadiusY - 5;
		int spawnExclusion = generator.SpawnClearance + 2;
		int totalSpawned = 0;

		// Debug counters
		int cellsEvaluated = 0;
		int skipBiomeConfig = 0;
		int skipDensity = 0;
		int skipTerrain = 0;
		int skipDistance = 0;
		int skipTexture = 0;

		GD.Print($"[PropSpawner] Starting spawn: radius={radius} safe={safeRadius} spawn={spawnExclusion} seed={seed}");
		GD.Print($"[PropSpawner] Configs loaded for biomes: [{string.Join(", ", configs.Keys)}]");

		for (int x = -safeRadius; x <= safeRadius; x++)
		{
			for (int y = -safeRadiusY; y <= safeRadiusY; y++)
			{
				// Le premier chargement d'une texture de décor peut coûter cher : la tranche se vérifie à chaque cellule.
				if (sliceSpent())
					await yieldSlice();
				if (generator.EllipseDistance(x, y) > safeRadius)
					continue;

				if (Mathf.Abs(x) <= spawnExclusion && Mathf.Abs(y) <= spawnExclusion)
					continue;

				if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
					continue;

				TerrainType terrain = GetTerrainAtCell(generator, terrainGrid, x, y);
				if (terrain == TerrainType.Water)
					continue;

				Vector2I cell = new(x, y);
				if (usedCells.Contains(cell))
					continue;
				if (blockedCells != null && blockedCells.Contains(cell))
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
				byte packedZone = rhythmZones != null && config.Rhythm != null
					? rhythmZones[x + generator.MapRadiusX, y + generator.MapRadiusY]
					: TerrainRhythm.NoZone;
				TerrainRhythm.Zone zone = packedZone == TerrainRhythm.NoZone ? null : config.Rhythm.Zones[TerrainRhythm.ZoneIndex(packedZone)];
				if (zone != null)
					spawnChance *= zone.DensityScale;
				float roll = CellHash.Unit(x, y, 0x5EED);
				if (roll > spawnChance)
				{
					skipDensity++;
					continue;
				}

				string terrainName = terrain.ToString().ToLowerInvariant();
				PropDefinition prop = PickProp(config, terrainName, x, y, zone, TerrainRhythm.IsLane(packedZone));
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

				if (!TrySpawnPropInstance(prop, cell, ground, container, usedCells, textureCache))
				{
					skipTexture++;
					continue;
				}

				totalSpawned++;
			}
		}

		LoadProfiler.Mark("décors semés sur la carte");
		await yieldSlice();
		if (wildFieldsLayout != null && configs.TryGetValue("wild_fields", out BiomePropConfig wildFieldsConfig))
			totalSpawned += await PlaceWildFieldsHeroPropsAsync(generator, terrainGrid, ground, container, usedCells, blockedCells, textureCache, wildFieldsConfig, wildFieldsLayout, seed, yieldSlice);
		foreach (BiomeData active in generator.ActiveBiomes)
		{
			if (configs.TryGetValue(active.Id, out BiomePropConfig activeConfig) && activeConfig.Remarkables.Count > 0)
				totalSpawned += await PlaceRemarkablesAsync(generator, terrainGrid, ground, container, usedCells, blockedCells, textureCache,
					activeConfig, seed, yieldSlice, urbanLayout, pathCells);
		}

		GD.Print($"[PropSpawner] === SPAWN REPORT ===");
		GD.Print($"[PropSpawner] Cells evaluated: {cellsEvaluated}");
		GD.Print($"[PropSpawner] Skip no biome config: {skipBiomeConfig}");
		GD.Print($"[PropSpawner] Skip density filter: {skipDensity}");
		GD.Print($"[PropSpawner] Skip terrain mismatch: {skipTerrain}");
		GD.Print($"[PropSpawner] Skip too close: {skipDistance}");
		GD.Print($"[PropSpawner] Skip texture missing: {skipTexture}");
		GD.Print($"[PropSpawner] TOTAL SPAWNED: {totalSpawned}");
	}

	/// <summary>Décor tiré au poids parmi ceux du terrain ; la zone du rythme repondère, une trouée refuse ce qui bloque.</summary>
	private PropDefinition PickProp(BiomePropConfig config, string terrainName, int x, int y, TerrainRhythm.Zone zone, bool lane)
	{
		float totalWeight = 0f;
		foreach (PropDefinition prop in config.Props)
		{
			if (prop.Terrain.Contains(terrainName))
				totalWeight += WeightIn(prop, zone, lane);
		}

		if (totalWeight <= 0f)
			return null;

		float roll = CellHash.Unit(x, y, 0x9C0F) * totalWeight;

		float cumulative = 0f;
		PropDefinition last = null;
		foreach (PropDefinition prop in config.Props)
		{
			if (!prop.Terrain.Contains(terrainName))
				continue;
			float weight = WeightIn(prop, zone, lane);
			if (weight <= 0f)
				continue;
			last = prop;
			cumulative += weight;
			if (roll < cumulative)
				return prop;
		}

		return last;
	}

	private static float WeightIn(PropDefinition prop, TerrainRhythm.Zone zone, bool lane)
	{
		if (zone == null)
			return prop.Weight;
		if (lane && prop.Blocking)
			return 0f;
		return prop.Weight * (zone.Weights.TryGetValue(prop.Id, out float scale) ? scale : zone.OtherWeightScale);
	}

	/// <summary>Cinq grands décors des champs, un par image : chacun cherche sa place dans de longues listes de cellules.</summary>
	private async Task<int> PlaceWildFieldsHeroPropsAsync(
		WorldGenerator generator,
		TerrainType[,] terrainGrid,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		HashSet<Vector2I> blockedCells,
		Dictionary<string, Texture2D> textureCache,
		BiomePropConfig config,
		WildFieldsLayout layout,
		ulong seed,
		Func<Task> yieldSlice)
	{
		if (layout.MapRadius <= 0)
			return 0;

		Dictionary<string, PropDefinition> propsById = new();
		foreach (PropDefinition prop in config.Props)
			propsById[prop.Id] = prop;

		(string Id, HashSet<Vector2I>[] Sources, ulong Salt)[] heroes =
		{
			("rusted_plow", new[] { layout.FallowCells, layout.PathCells, layout.WheatCells }, 0x1A31UL),
			("tractor_remains", new[] { layout.FallowCells, layout.PathCells, layout.MeadowCells }, 0x2B52UL),
			("tractor_husk", new[] { layout.PathCells, layout.FallowCells, layout.WheatCells }, 0x3C73UL),
			("windmill_ruin", new[] { layout.MeadowCells, layout.WheatCells }, 0x4D94UL),
			("silo_ruin", new[] { layout.PathCells, layout.FallowCells, layout.MeadowCells }, 0x5EB5UL),
		};
		int placed = 0;
		foreach ((string id, HashSet<Vector2I>[] sources, ulong salt) in heroes)
		{
			placed += TryPlaceWildFieldsHeroProp(id, BuildWildFieldCandidateCells(layout, sources), salt, propsById, generator, terrainGrid,
				ground, container, usedCells, blockedCells, textureCache, seed) ? 1 : 0;
			await yieldSlice();
		}

		if (placed > 0)
			GD.Print($"[PropSpawner] Wild fields hero props placed: {placed}");

		return placed;
	}

	/// <summary>
	/// Décors remarquables du biome (plan 58), un chacun : cellules du biome mêlées une fois par la graine, parcourues
	/// depuis un point tiré pour chaque décor, à <see cref="PropRules.RemarkableSpacingCells"/> colonnes les uns des
	/// autres au moins pour qu'ils se trouvent en marchant.
	/// </summary>
	private async Task<int> PlaceRemarkablesAsync(
		WorldGenerator generator,
		TerrainType[,] terrainGrid,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		HashSet<Vector2I> blockedCells,
		Dictionary<string, Texture2D> textureCache,
		BiomePropConfig config,
		ulong seed,
		Func<Task> yieldSlice,
		UrbanLayout urbanLayout,
		HashSet<Vector2I> pathCells)
	{
		Dictionary<string, PropDefinition> propsById = new();
		foreach (PropDefinition prop in config.Props)
			propsById[prop.Id] = prop;
		bool urbanCity = urbanLayout != null && config.BiomeId == "urban_ruins";
		List<Vector2I> cells = new();
		for (int x = -generator.MapRadiusX; x <= generator.MapRadiusX; x++)
		{
			for (int y = -generator.MapRadiusY; y <= generator.MapRadiusY; y++)
			{
				if (!generator.IsWithinBounds(x, y) || generator.GetBiomeId(x, y) != config.BiomeId)
					continue;
				Vector2I cell = new(x, y);
				if (urbanCity && !IsUrbanRemarkableCell(urbanLayout, cell))
					continue;
				cells.Add(cell);
			}
		}
		HashSet<Vector2I> remarkableBlocked = urbanCity ? pathCells : blockedCells;
		RandomNumberGenerator rng = new() { Seed = seed ^ 0x8E3A0UL };
		for (int i = cells.Count - 1; i > 0; i--)
		{
			int j = rng.RandiRange(0, i);
			(cells[i], cells[j]) = (cells[j], cells[i]);
		}
		await yieldSlice();
		List<Vector2I> placedCells = new();
		int placed = 0;
		foreach (string id in config.Remarkables)
		{
			int start = cells.Count > 0 ? rng.RandiRange(0, cells.Count - 1) : 0;
			if (TryPlaceRemarkable(propsById[id], cells, start, placedCells, generator, terrainGrid, ground, container,
					usedCells, remarkableBlocked, textureCache))
				placed++;
			else
				GD.PushWarning($"[PropSpawner] {config.BiomeId} : pas de place pour le décor remarquable {id}");
			await yieldSlice();
		}
		GD.Print($"[PropSpawner] {config.BiomeId} : {placed}/{config.Remarkables.Count} décors remarquables");
		return placed;
	}

	private static bool IsUrbanRemarkableCell(UrbanLayout layout, Vector2I cell)
	{
		if (layout.ParkPondCells.Contains(cell))
			return false;
		UrbanCellType center = UrbanPropPlacer.GetCellType(layout, cell);
		if (center is not (UrbanCellType.Plaza or UrbanCellType.Park))
			return false;
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				UrbanCellType neighbor = UrbanPropPlacer.GetCellType(layout, cell + new Vector2I(dx, dy));
				if (neighbor is UrbanCellType.Road or UrbanCellType.BuildingWall or UrbanCellType.BuildingInterior or UrbanCellType.None)
					return false;
			}
		}
		return true;
	}

	private bool TryPlaceRemarkable(
		PropDefinition prop,
		List<Vector2I> cells,
		int start,
		List<Vector2I> placedCells,
		WorldGenerator generator,
		TerrainType[,] terrainGrid,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		HashSet<Vector2I> blockedCells,
		Dictionary<string, Texture2D> textureCache)
	{
		PropRules rules = PropRules.Current;
		int spacing = rules.RemarkableSpacingCells;
		int spawnExclusion = generator.SpawnClearance + 2;
		for (int step = 0; step < cells.Count; step++)
		{
			Vector2I cell = cells[(start + step) % cells.Count];
			if (generator.IsErased(cell.X, cell.Y) || (Mathf.Abs(cell.X) <= spawnExclusion && Mathf.Abs(cell.Y) <= spawnExclusion))
				continue;
			if (usedCells.Contains(cell) || (blockedCells != null && blockedCells.Contains(cell)))
				continue;
			string terrainName = GetTerrainAtCell(generator, terrainGrid, cell.X, cell.Y).ToString().ToLowerInvariant();
			if (!prop.Terrain.Contains(terrainName) || IsTooCloseToUsed(cell, usedCells, rules.RemarkableClearanceCells))
				continue;
			bool crowded = false;
			foreach (Vector2I other in placedCells)
			{
				// Rangs d'un demi-pas : une colonne vaut deux rangs au sol.
				Vector2I delta = cell - other;
				if (delta.X * delta.X * 4 + delta.Y * delta.Y < spacing * spacing * 4)
				{
					crowded = true;
					break;
				}
			}
			if (crowded || !TrySpawnPropInstance(prop, cell, ground, container, usedCells, textureCache))
				continue;
			placedCells.Add(cell);
			return true;
		}
		return false;
	}

	private bool TryPlaceWildFieldsHeroProp(
		string propId,
		List<Vector2I> candidates,
		ulong salt,
		Dictionary<string, PropDefinition> propsById,
		WorldGenerator generator,
		TerrainType[,] terrainGrid,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		HashSet<Vector2I> blockedCells,
		Dictionary<string, Texture2D> textureCache,
		ulong seed)
	{
		if (!propsById.TryGetValue(propId, out PropDefinition prop))
			return false;

		SortCellsDeterministically(candidates, seed ^ salt);

		foreach (Vector2I cell in candidates)
		{
			if (!generator.IsWithinBounds(cell.X, cell.Y) || generator.IsErased(cell.X, cell.Y))
				continue;
			if (generator.GetBiomeId(cell.X, cell.Y) != "wild_fields")
				continue;
			if (Mathf.Abs(cell.X) <= generator.SpawnClearance + 2 && Mathf.Abs(cell.Y) <= generator.SpawnClearance + 2)
				continue;
			if (usedCells.Contains(cell))
				continue;
			if (blockedCells != null && blockedCells.Contains(cell))
				continue;

			TerrainType terrain = GetTerrainAtCell(generator, terrainGrid, cell.X, cell.Y);
			if (terrain == TerrainType.Water)
				continue;

			string terrainName = terrain.ToString().ToLowerInvariant();
			if (!prop.Terrain.Contains(terrainName))
				continue;
			if (prop.MinDistance > 1 && IsTooCloseToUsed(cell, usedCells, prop.MinDistance))
				continue;

			if (TrySpawnPropInstance(prop, cell, ground, container, usedCells, textureCache))
				return true;
		}

		return false;
	}

	private static List<Vector2I> BuildWildFieldCandidateCells(WildFieldsLayout layout, params HashSet<Vector2I>[] groups)
	{
		List<Vector2I> cells = new();
		HashSet<Vector2I> unique = new();

		foreach (HashSet<Vector2I> group in groups)
		{
			if (group == null)
				continue;

			foreach (Vector2I cell in group)
			{
				if (unique.Add(cell))
					cells.Add(cell);
			}
		}

		return cells;
	}

	private static void SortCellsDeterministically(List<Vector2I> cells, ulong salt)
	{
		cells.Sort((a, b) =>
		{
			int hashA = HashCell(a.X, a.Y, salt);
			int hashB = HashCell(b.X, b.Y, salt);
			if (hashA != hashB)
				return hashA.CompareTo(hashB);
			if (a.X != b.X)
				return a.X.CompareTo(b.X);
			return a.Y.CompareTo(b.Y);
		});
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

	private bool TrySpawnPropInstance(
		PropDefinition prop,
		Vector2I cell,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> textureCache)
	{
		Texture2D baseTex = LoadTextureCached(prop.SpriteBasePath, textureCache);
		if (baseTex == null)
			return false;

		Texture2D canopyTex = null;
		if (!string.IsNullOrEmpty(prop.SpriteCanopyPath))
			canopyTex = LoadTextureCached(prop.SpriteCanopyPath, textureCache);

		Vector2 worldPos = ground.MapToLocal(cell);
		// Petits décors non bloquants : léger décalage dans le losange de la case, sinon ils dessinent la trame
		// de la grille. Les décors bloquants restent centrés (collision et occupation des cases).
		if (!prop.Blocking)
			worldPos += new Vector2((CellHash.Unit(cell.X, cell.Y, 0xA11) - 0.5f) * 32f, (CellHash.Unit(cell.X, cell.Y, 0xB22) - 0.5f) * 12f);
		EnvironmentProp envProp = new();
		envProp.GlobalPosition = worldPos;
		container.AddChild(envProp);
		envProp.Initialize(baseTex, canopyTex, prop.CanopyOffsetY, prop.Blocking, prop.FootprintScale);
		usedCells.Add(cell);

		return true;
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

	private static TerrainType GetTerrainAtCell(WorldGenerator generator, TerrainType[,] terrainGrid, int x, int y)
	{
		if (terrainGrid != null)
		{
			int gx = x + terrainGrid.GetLength(0) / 2;
			int gy = y + terrainGrid.GetLength(1) / 2;
			if (gx >= 0 && gy >= 0 && gx < terrainGrid.GetLength(0) && gy < terrainGrid.GetLength(1))
				return terrainGrid[gx, gy];
		}

		return generator.GetTerrain(x, y);
	}

	private static int HashCell(int x, int y, ulong salt)
	{
		return (int)CellHash.Of(x, y, salt);
	}

	// =========================================================================
	// === Chargement des configs JSON ===
	// =========================================================================

	public const string PropsDirectory = "res://data/props";
	// Écart minimal entre deux décors remarquables, en colonnes de cellules.
	private static readonly string[] TerrainNames = Array.ConvertAll(Enum.GetNames<TerrainType>(), name => name.ToLowerInvariant());
	private static Dictionary<string, BiomePropConfig> _configs;
	private static string _configsError;

	/// <summary>Décors par biome contrôlés une fois (plan 26 Q7h-2) ; refusés, la raison, et la run ne démarre pas (GameBootstrap).</summary>
	public static bool TryLoadConfigs(out string error)
	{
		LoadPropConfigs();
		error = _configsError;
		return error == null;
	}

	/// <summary>Terrains rythmés par biome, pour la génération (plan 58 T1a) ; vide si le catalogue est refusé.</summary>
	public static Dictionary<string, TerrainRhythm> LoadRhythms()
	{
		Dictionary<string, TerrainRhythm> rhythms = new();
		foreach (KeyValuePair<string, BiomePropConfig> entry in LoadPropConfigs())
		{
			if (entry.Value.Rhythm != null)
				rhythms[entry.Key] = entry.Value.Rhythm;
		}
		return rhythms;
	}

	public const string CityBiomeId = "urban_ruins";

	/// <summary>Grille de la ville d'un biome, nulle s'il n'en règle pas ou si le catalogue est refusé.</summary>
	public static CityGrid LoadCity(string biomeId) =>
		LoadPropConfigs().TryGetValue(biomeId, out BiomePropConfig config) ? config.City : null;

	/// <summary>Les décors acceptés ; un catalogue refusé n'en rend aucun (la génération s'arrête sur l'erreur journalisée).</summary>
	private static Dictionary<string, BiomePropConfig> LoadPropConfigs()
	{
		if (_configs != null || _configsError != null)
			return _configs ?? new Dictionary<string, BiomePropConfig>();
		List<(string Name, string Text)> files = new();
		using DirAccess dir = DirAccess.Open(PropsDirectory);
		if (dir == null)
			_configsError = $"{PropsDirectory} : dossier illisible";
		else
		{
			foreach (string fileName in dir.GetFiles())
			{
				if (!fileName.EndsWith(".json", StringComparison.Ordinal) || fileName.StartsWith('_'))
					continue;
				using FileAccess file = FileAccess.Open($"{PropsDirectory}/{fileName}", FileAccess.ModeFlags.Read);
				files.Add((fileName, file?.GetAsText()));
			}
			_configsError = TryParseConfigs(files, id => BiomeDataLoader.Get(id) != null, path => ResourceLoader.Exists($"res://{path}"),
				out _configs);
		}
		if (_configsError != null)
		{
			GD.PushError($"[PropSpawner] {_configsError}");
			return new Dictionary<string, BiomePropConfig>();
		}
		GD.Print($"[PropSpawner] {_configs.Count} biome prop configs loaded");
		return _configs;
	}

	/// <summary>
	/// Un fichier par biome connu, nommé comme lui ; décors aux identifiants uniques, sur des terrains de la génération,
	/// sprites présents. Rend l'erreur, nulle si tout est accepté.
	/// </summary>
	public static string TryParseConfigs(IReadOnlyList<(string Name, string Text)> files, Func<string, bool> biomeExists,
		Func<string, bool> spriteExists, out Dictionary<string, BiomePropConfig> configs)
	{
		configs = null;
		Dictionary<string, BiomePropConfig> parsed = new();
		foreach ((string name, string text) in files)
		{
			string where = $"{PropsDirectory}/{name}";
			if (text == null)
				return $"{where} : fichier illisible";
			try
			{
				using JsonDocument document = JsonDocument.Parse(text);
				JsonConfigReader reader = new(document.RootElement);
				BiomePropConfig config = ParseConfig(reader, reader.Root, biomeExists, spriteExists);
				if (reader.Error == null && config.BiomeId + ".json" != name)
					reader.Fail($"biome_id « {config.BiomeId} » différent du nom du fichier");
				// La Haute-Ville sans grille ne serait qu'un sol nu : sa grille est exigée (plan 58 T3a).
				if (reader.Error == null && config.BiomeId == CityBiomeId && config.City == null)
					reader.Fail($"{CityBiomeId} : city exigé, la grille de la ville");
				if (reader.Error != null)
					return $"{where} : {reader.Error}";
				parsed[config.BiomeId] = config;
			}
			catch (JsonException exception)
			{
				return $"{where} : JSON invalide : {exception.Message}";
			}
		}
		if (parsed.Count == 0)
			return $"{PropsDirectory} : aucun fichier de décors";
		configs = parsed;
		return null;
	}

	private static BiomePropConfig ParseConfig(JsonConfigReader reader, JsonElement root, Func<string, bool> biomeExists,
		Func<string, bool> spriteExists)
	{
		reader.AllowOnly(root, "décors", "biome_id", "density", "props", "remarkables", "rhythm", "city");
		BiomePropConfig config = new() { BiomeId = reader.Text(root, "biome_id"), Density = reader.Chance(root, "density") };
		if (reader.Error == null && !biomeExists(config.BiomeId))
			reader.Fail($"biome_id « {config.BiomeId} » inconnu");
		HashSet<string> ids = new();
		foreach (JsonElement item in reader.List(root, "props", 1))
		{
			if (item.ValueKind != JsonValueKind.Object)
			{
				reader.Fail("props : objet attendu");
				break;
			}
			reader.AllowOnly(item, "décor", "id", "name", "weight", "terrain", "sprite_base", "sprite_canopy", "canopy_offset_y", "blocking",
				"footprint_scale", "min_distance");
			PropDefinition prop = new()
			{
				Id = reader.Text(item, "id"),
				Name = JsonConfigReader.Has(item, "name") ? reader.Text(item, "name") : "",
				Weight = reader.Positive(item, "weight"),
				SpriteBasePath = Sprite(reader, item, "sprite_base", spriteExists),
				SpriteCanopyPath = JsonConfigReader.Has(item, "sprite_canopy") ? Sprite(reader, item, "sprite_canopy", spriteExists) : null,
				CanopyOffsetY = JsonConfigReader.Has(item, "canopy_offset_y") ? reader.Number(item, "canopy_offset_y") : 0f,
				Blocking = reader.Flag(item, "blocking"),
				FootprintScale = JsonConfigReader.Has(item, "footprint_scale") ? reader.Positive(item, "footprint_scale") : 1f,
				MinDistance = JsonConfigReader.Has(item, "min_distance") ? reader.Integer(item, "min_distance", 1, 64) : 1,
			};
			foreach (JsonElement terrain in reader.List(item, "terrain", 1))
			{
				string terrainName = terrain.ValueKind == JsonValueKind.String ? terrain.GetString() : null;
				if (terrainName == null || Array.IndexOf(TerrainNames, terrainName) < 0)
					reader.Fail($"décor {prop.Id} : terrain « {terrainName} » inconnu ({string.Join(", ", TerrainNames)})");
				else if (prop.Terrain.Contains(terrainName))
					reader.Fail($"décor {prop.Id} : terrain « {terrainName} » en double");
				else
					prop.Terrain.Add(terrainName);
			}
			if (reader.Error == null && !ids.Add(prop.Id))
				reader.Fail($"décor « {prop.Id} » déjà déclaré");
			if (reader.Error != null)
				break;
			config.Props.Add(prop);
		}
		if (JsonConfigReader.Has(root, "remarkables"))
		{
			foreach (JsonElement item in reader.List(root, "remarkables", 0))
			{
				string id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
				if (id == null || !ids.Contains(id))
					reader.Fail($"remarkables : décor « {id} » absent de props");
				else if (config.Remarkables.Contains(id))
					reader.Fail($"remarkables : décor « {id} » en double");
				else
					config.Remarkables.Add(id);
			}
		}
		if (JsonConfigReader.Has(root, "rhythm"))
			config.Rhythm = TerrainRhythm.Parse(reader, reader.Section(root, "rhythm"), ids, spriteExists);
		if (JsonConfigReader.Has(root, "city") && reader.Error == null)
		{
			Dictionary<string, PropDefinition> byId = new();
			foreach (PropDefinition prop in config.Props)
				byId[prop.Id] = prop;
			config.City = CityGrid.Parse(reader, reader.Section(root, "city"), byId);
		}
		return config;
	}

	/// <summary>Chemin d'image sans « res:// », qui doit exister.</summary>
	private static string Sprite(JsonConfigReader reader, JsonElement item, string key, Func<string, bool> spriteExists)
	{
		string path = reader.Text(item, key);
		if (path != null && !spriteExists(path))
			reader.Fail($"{key} : image « {path} » introuvable");
		return path;
	}
}
