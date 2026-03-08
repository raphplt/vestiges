using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Charge les textures de tiles par biome et les enregistre dynamiquement
/// dans le TileSet. Fournit un sourceId par (biomeIndex, terrainType, cellule)
/// avec sélection déterministe de variante basée sur la position.
/// </summary>
public class BiomeTileMapper
{
	private readonly Dictionary<int, Dictionary<TerrainType, int[]>> _biomeSourceMap = new();
	private readonly Dictionary<int, Dictionary<string, int[]>> _biomeSpecialSourceMap = new();
	private readonly Dictionary<TerrainType, int[]> _fallbackSourceMap = new();
	private readonly Dictionary<int, string> _biomeIds = new();

	// Tiles d'eau communes (remplacement global du water générique)
	private int[] _commonWaterSources;

	// Tiles de dissolution pour le dégradé visuel en bordure de carte
	private int[] _dissolutionSources;

	private static readonly Dictionary<string, TerrainType> TerrainNameMap = new()
	{
		{ "grass", TerrainType.Grass },
		{ "concrete", TerrainType.Concrete },
		{ "water", TerrainType.Water },
		{ "forest", TerrainType.Forest }
	};

	public void Initialize(TileSet tileSet, List<BiomeData> activeBiomes)
	{
		_biomeSourceMap.Clear();
		_biomeSpecialSourceMap.Clear();
		_fallbackSourceMap.Clear();
		_biomeIds.Clear();

		// Charger les tiles d'eau communes
		_commonWaterSources = LoadTileGroup(tileSet, new List<string>
		{
			"commun/tile_eau_profonde_base",
			"commun/tile_eau_profonde_v2"
		});

		if (_commonWaterSources.Length > 0)
			_fallbackSourceMap[TerrainType.Water] = _commonWaterSources;

		// Charger les tiles de dissolution
		_dissolutionSources = LoadTileGroup(tileSet, new List<string>
		{
			"commun/tile_dissolution_n1",
			"commun/tile_dissolution_n2",
			"commun/tile_dissolution_n3"
		});

		for (int i = 0; i < activeBiomes.Count; i++)
		{
			BiomeData biome = activeBiomes[i];
			_biomeIds[i] = biome.Id;
			if (biome.TileSources.Count == 0)
				continue;

			Dictionary<TerrainType, int[]> terrainMap = new();
			Dictionary<string, int[]> specialMap = new();

			foreach (KeyValuePair<string, List<string>> kv in biome.TileSources)
			{
				int[] sources = LoadTileGroup(tileSet, kv.Value);
				if (sources.Length == 0)
					continue;

				if (TerrainNameMap.TryGetValue(kv.Key, out TerrainType terrainType))
				{
					terrainMap[terrainType] = sources;
					if (!_fallbackSourceMap.ContainsKey(terrainType))
						_fallbackSourceMap[terrainType] = sources;
				}
				else
				{
					specialMap[kv.Key] = sources;
				}
			}

			if (terrainMap.Count > 0)
			{
				_biomeSourceMap[i] = terrainMap;
				GD.Print($"[BiomeTileMapper] Biome '{biome.Id}' : {terrainMap.Count} terrain(s) mappé(s)");
			}

			if (specialMap.Count > 0)
			{
				_biomeSpecialSourceMap[i] = specialMap;
				GD.Print($"[BiomeTileMapper] Biome '{biome.Id}' : {specialMap.Count} groupe(s) spéciaux");
			}
		}

		GD.Print($"[BiomeTileMapper] Initialisé — {_biomeSourceMap.Count} biome(s), {_fallbackSourceMap.Count} fallback(s), {_commonWaterSources.Length} eau commune");
	}

	/// <summary>
	/// Retourne le sourceId à utiliser pour une cellule donnée.
	/// Sélection déterministe de variante basée sur la position (même seed visuel).
	/// </summary>
	public int GetSourceId(
		int biomeIndex,
		TerrainType terrain,
		int x,
		int y,
		UrbanCellType urbanCellType = UrbanCellType.None,
		UrbanLayout urbanLayout = null)
	{
		if (biomeIndex >= 0
			&& _biomeIds.TryGetValue(biomeIndex, out string biomeId)
			&& biomeId == "urban_ruins"
			&& urbanCellType != UrbanCellType.None
			&& TryGetUrbanRuinsSourceId(biomeIndex, urbanCellType, x, y, urbanLayout, out int urbanSourceId))
		{
			return urbanSourceId;
		}

		// Tiles spécifiques au biome
		if (biomeIndex >= 0 && _biomeSourceMap.TryGetValue(biomeIndex, out Dictionary<TerrainType, int[]> terrainMap))
		{
			if (terrainMap.TryGetValue(terrain, out int[] sources))
			{
				if (_biomeIds.TryGetValue(biomeIndex, out string biomeKey))
				{
					return GetBiomeSpecificSourceId(biomeKey, terrain, sources, x, y);
				}

				int hash = HashCell(x, y);
				return sources[hash % sources.Length];
			}
		}

		// Fallback par terrain basé sur les tiles réellement chargées
		if (_fallbackSourceMap.TryGetValue(terrain, out int[] fallbackSources) && fallbackSources.Length > 0)
		{
			int hash = HashCell(x, y);
			return fallbackSources[hash % fallbackSources.Length];
		}

		GD.PushWarning($"[BiomeTileMapper] Aucun sourceId pour terrain {terrain} (biomeIndex={biomeIndex}, cell={x},{y})");
		return -1;
	}

	/// <summary>
	/// Retourne un sourceId de dissolution basé sur l'intensité du decay (0→1).
	/// Retourne -1 si aucune tile de dissolution n'est disponible.
	/// </summary>
	public int GetDissolutionSourceId(float decayIntensity)
	{
		if (_dissolutionSources.Length == 0)
			return -1;

		int index = (int)(decayIntensity * _dissolutionSources.Length);
		index = Mathf.Clamp(index, 0, _dissolutionSources.Length - 1);
		return _dissolutionSources[index];
	}

	public bool HasDissolutionTiles => _dissolutionSources.Length > 0;

	private static int[] LoadTileGroup(TileSet tileSet, List<string> relativePaths)
	{
		List<int> ids = new();

		foreach (string path in relativePaths)
		{
			string fullPath = $"res://assets/tiles/{path}.png";
			Texture2D texture = GD.Load<Texture2D>(fullPath);
			if (texture == null)
			{
				GD.PushWarning($"[BiomeTileMapper] Texture introuvable : {fullPath}");
				continue;
			}

			TileSetAtlasSource source = new();
			source.Texture = texture;
			source.TextureRegionSize = new Vector2I(64, 32);
			source.CreateTile(Vector2I.Zero);

			int sourceId = tileSet.AddSource(source);
			ids.Add(sourceId);
		}

		return ids.ToArray();
	}

	/// <summary>
	/// Hash déterministe d'une cellule pour choisir une variante.
	/// Donne un pattern visuellement aléatoire mais reproductible.
	/// </summary>
	private static int HashCell(int x, int y)
	{
		return ((x * 73856093) ^ (y * 19349663)) & 0x7FFFFFFF;
	}

	private static int GetBiomeSpecificSourceId(string biomeId, TerrainType terrain, int[] sources, int x, int y)
	{
		if (biomeId == "wild_fields" && terrain == TerrainType.Grass && sources.Length >= 8)
			return GetWildFieldsGrassSourceId(sources, x, y);

		int hash = HashCell(x, y);
		return sources[hash % sources.Length];
	}

	private static int GetWildFieldsGrassSourceId(int[] sources, int x, int y)
	{
		int patchX = Mathf.FloorToInt(x / 5.0f);
		int patchY = Mathf.FloorToInt(y / 5.0f);
		int patchRoll = HashCell(patchX, patchY) % 100;
		int variantRoll = HashCell(x, y);

		if (patchRoll < 18)
			return sources[6 + (variantRoll % 2)];

		if (patchRoll < 58)
			return sources[3 + (variantRoll % 3)];

		return sources[variantRoll % 3];
	}

	private bool TryGetUrbanRuinsSourceId(
		int biomeIndex,
		UrbanCellType urbanCellType,
		int x,
		int y,
		UrbanLayout urbanLayout,
		out int sourceId)
	{
		sourceId = -1;

		_biomeSpecialSourceMap.TryGetValue(biomeIndex, out Dictionary<string, int[]> specialMap);
		_biomeSourceMap.TryGetValue(biomeIndex, out Dictionary<TerrainType, int[]> terrainMap);

		switch (urbanCellType)
		{
			case UrbanCellType.Road:
				string roadKey = GetUrbanRoadKey(urbanLayout, x, y);
				if (TryPickSpecialSource(specialMap, roadKey, x, y, out sourceId))
					return true;
				break;
			case UrbanCellType.Sidewalk:
				if (TryPickSpecialSource(specialMap, "sidewalk", x, y, out sourceId))
					return true;
				if (terrainMap != null && terrainMap.TryGetValue(TerrainType.Grass, out int[] sidewalkFallback))
				{
					sourceId = sidewalkFallback[HashCell(x, y) % sidewalkFallback.Length];
					return true;
				}
				break;
			case UrbanCellType.BuildingInterior:
				if (TryPickSpecialSource(specialMap, "building_interior", x, y, out sourceId))
					return true;
				break;
			case UrbanCellType.BuildingWall:
				if (TryPickSpecialSource(specialMap, "building_edge", x, y, out sourceId))
					return true;
				break;
			case UrbanCellType.Plaza:
				if (TryPickSpecialSource(specialMap, "plaza", x, y, out sourceId))
					return true;
				break;
		}

		return false;
	}

	private static bool TryPickSpecialSource(Dictionary<string, int[]> specialMap, string key, int x, int y, out int sourceId)
	{
		sourceId = -1;
		if (specialMap == null || !specialMap.TryGetValue(key, out int[] sources) || sources.Length == 0)
			return false;

		sourceId = PickUrbanSpecialSourceId(key, sources, x, y);
		return true;
	}

	private static string GetUrbanRoadKey(UrbanLayout urbanLayout, int x, int y)
	{
		if (urbanLayout == null)
			return "road_straight_ns";

		Vector2I cell = new(x, y);
		bool north = urbanLayout.RoadCells.Contains(cell + Vector2I.Up);
		bool east = urbanLayout.RoadCells.Contains(cell + Vector2I.Right);
		bool south = urbanLayout.RoadCells.Contains(cell + Vector2I.Down);
		bool west = urbanLayout.RoadCells.Contains(cell + Vector2I.Left);

		int connections = 0;
		if (north) connections++;
		if (east) connections++;
		if (south) connections++;
		if (west) connections++;

		if (connections >= 4) return "road_cross";
		if (connections == 3)
		{
			if (!north) return "road_t_n";
			if (!east) return "road_t_e";
			if (!south) return "road_t_s";
			return "road_t_w";
		}

		if (connections == 2)
		{
			if (north && south) return "road_straight_ns";
			if (east && west) return "road_straight_ew";
			if (north && east) return "road_corner_ne";
			if (north && west) return "road_corner_nw";
			if (south && east) return "road_corner_se";
			return "road_corner_sw";
		}

		if (connections == 1)
		{
			if (north) return "road_end_n";
			if (east) return "road_end_e";
			if (south) return "road_end_s";
			return "road_end_w";
		}

		return "road_straight_ns";
	}

	private static int PickUrbanSpecialSourceId(string key, int[] sources, int x, int y)
	{
		int patchX = Mathf.FloorToInt(x / 4.0f);
		int patchY = Mathf.FloorToInt(y / 4.0f);
		int patchRoll = HashCell(patchX, patchY) % 100;
		int variantRoll = HashCell(x, y);

		if ((key == "building_edge" || key == "plaza") && sources.Length >= 3)
		{
			if (patchRoll < 25)
				return sources[2];
			if (patchRoll < 58)
				return sources[1];
			return sources[0];
		}

		if (key == "building_interior" && sources.Length >= 2)
		{
			return sources[(patchRoll < 45 ? 0 : 1) % sources.Length];
		}

		if (key == "sidewalk" && sources.Length >= 2)
		{
			return sources[(patchRoll < 50 ? 0 : 1) % sources.Length];
		}

		return sources[variantRoll % sources.Length];
	}
}
