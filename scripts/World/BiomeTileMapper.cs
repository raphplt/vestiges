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

	// Layout urbain pour la sélection directionnelle des tiles de route
	private UrbanLayout _urbanLayout;
	private WildFieldsLayout _wildFieldsLayout;

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

	private TileSet _tileSet;

	/// <summary>
	/// Enregistre un groupe de textures runtime (générées en mémoire) comme source spéciale
	/// pour un biome donné. Utilisé par RoadTileGenerator pour les tiles de route.
	/// </summary>
	public void RegisterRuntimeTileGroup(int biomeIndex, string specialKey, ImageTexture[] textures)
	{
		if (_tileSet == null || textures.Length == 0)
			return;

		List<int> ids = new();
		foreach (ImageTexture tex in textures)
		{
			TileSetAtlasSource source = new();
			source.Texture = tex;
			source.TextureRegionSize = new Vector2I(64, 32);
			source.CreateTile(Vector2I.Zero);
			int sourceId = _tileSet.AddSource(source);
			ids.Add(sourceId);
		}

		if (!_biomeSpecialSourceMap.ContainsKey(biomeIndex))
			_biomeSpecialSourceMap[biomeIndex] = new Dictionary<string, int[]>();

		_biomeSpecialSourceMap[biomeIndex][specialKey] = ids.ToArray();
		GD.Print($"[BiomeTileMapper] Runtime tiles '{specialKey}' : {ids.Count} source(s) enregistrée(s)");
	}

	/// <summary>Retourne l'index du biome par son ID, ou -1.</summary>
	public int GetBiomeIndex(string biomeId)
	{
		foreach (KeyValuePair<int, string> kv in _biomeIds)
		{
			if (kv.Value == biomeId)
				return kv.Key;
		}
		return -1;
	}

	/// <summary>
	/// Injecte le layout urbain pour permettre la sélection directionnelle des tiles de route.
	/// Doit être appelé après Initialize et avant ApplyTerrain.
	/// </summary>
	public void SetUrbanLayout(UrbanLayout layout)
	{
		_urbanLayout = layout;
	}

	public void SetWildFieldsLayout(WildFieldsLayout layout)
	{
		_wildFieldsLayout = layout;
	}

	public bool TryGetUrbanRoadOverlaySourceId(int biomeIndex, int x, int y, out int sourceId)
	{
		sourceId = -1;
		if (!_biomeIds.TryGetValue(biomeIndex, out string biomeId) || biomeId != "urban_ruins")
			return false;
		if (!_biomeSpecialSourceMap.TryGetValue(biomeIndex, out Dictionary<string, int[]> specialMap))
			return false;
		return TryGetDirectionalRoadSource(specialMap, x, y, out sourceId);
	}

	public void Initialize(TileSet tileSet, List<BiomeData> activeBiomes)
	{
		_biomeSourceMap.Clear();
		_biomeSpecialSourceMap.Clear();
		_fallbackSourceMap.Clear();
		_biomeIds.Clear();
		_tileSet = tileSet;

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
		WorldGenerator generator = null)
	{
		if (biomeIndex >= 0
			&& _biomeIds.TryGetValue(biomeIndex, out string biomeId)
			&& biomeId == "urban_ruins"
			&& urbanCellType != UrbanCellType.None
			&& TryGetUrbanRuinsSourceId(biomeIndex, urbanCellType, x, y, out int urbanSourceId))
		{
			return urbanSourceId;
		}

		if (biomeIndex >= 0
			&& _biomeIds.TryGetValue(biomeIndex, out string swampBiomeId)
			&& swampBiomeId == "swamp"
			&& generator != null
			&& TryGetSwampSourceId(biomeIndex, terrain, x, y, generator, out int swampSourceId))
		{
			return swampSourceId;
		}

		if (biomeIndex >= 0
			&& _biomeIds.TryGetValue(biomeIndex, out string wildBiomeId)
			&& wildBiomeId == "wild_fields"
			&& TryGetWildFieldsSourceId(biomeIndex, terrain, x, y, out int wildFieldsSourceId))
		{
			return wildFieldsSourceId;
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

		if (sources.Length >= 14)
		{
			if (patchRoll < 18)
				return sources[12 + (variantRoll % 2)];

			if (patchRoll < 42)
				return sources[10 + (variantRoll % 2)];

			if (patchRoll < 74)
				return sources[3 + (variantRoll % 3)];

			return sources[8 + (variantRoll % 2)];
		}

		if (patchRoll < 18)
			return sources[6 + (variantRoll % 2)];

		if (patchRoll < 58)
			return sources[3 + (variantRoll % 3)];

		return sources[variantRoll % 3];
	}

	private bool TryGetWildFieldsSourceId(
		int biomeIndex,
		TerrainType terrain,
		int x,
		int y,
		out int sourceId)
	{
		sourceId = -1;
		if (_wildFieldsLayout == null)
			return false;
		if (!_biomeSourceMap.TryGetValue(biomeIndex, out Dictionary<TerrainType, int[]> terrainMap))
			return false;

		int gx = x + _wildFieldsLayout.MapRadius;
		int gy = y + _wildFieldsLayout.MapRadius;
		if (gx < 0 || gy < 0 || gx >= _wildFieldsLayout.CellGrid.GetLength(0) || gy >= _wildFieldsLayout.CellGrid.GetLength(1))
			return false;

		WildFieldCellType cellType = _wildFieldsLayout.CellGrid[gx, gy];
		int hash = HashCell(x, y);

		if (terrain == TerrainType.Grass && terrainMap.TryGetValue(TerrainType.Grass, out int[] grassSources) && grassSources.Length >= 8)
		{
			switch (cellType)
			{
				case WildFieldCellType.Wheat:
					if (grassSources.Length >= 14 && (HashCell(x / 2, y / 2) & 1) == 0)
					{
						sourceId = grassSources[12 + (hash % 2)];
						return true;
					}

					sourceId = grassSources[6 + (hash % 2)];
					return true;
				case WildFieldCellType.Fallow:
					if (grassSources.Length >= 10)
					{
						sourceId = grassSources[8 + (hash % 2)];
						return true;
					}

					sourceId = grassSources[hash % 3];
					return true;
				case WildFieldCellType.Meadow:
					if (grassSources.Length >= 12 && (HashCell(x + 9, y - 7) % 3) == 0)
					{
						sourceId = grassSources[10 + (hash % 2)];
						return true;
					}

					sourceId = grassSources[3 + (hash % 3)];
					return true;
				case WildFieldCellType.Path:
					sourceId = grassSources[3 + (hash % 3)];
					return true;
			}
		}

		if (terrain == TerrainType.Concrete && terrainMap.TryGetValue(TerrainType.Concrete, out int[] pathSources) && pathSources.Length > 0)
		{
			sourceId = pathSources[hash % pathSources.Length];
			return true;
		}

		if (terrain == TerrainType.Forest && terrainMap.TryGetValue(TerrainType.Forest, out int[] groveSources) && groveSources.Length > 0)
		{
			sourceId = groveSources[hash % groveSources.Length];
			return true;
		}

		return false;
	}

	private bool TryGetUrbanRuinsSourceId(
		int biomeIndex,
		UrbanCellType urbanCellType,
		int x,
		int y,
		out int sourceId)
	{
		sourceId = -1;

		_biomeSpecialSourceMap.TryGetValue(biomeIndex, out Dictionary<string, int[]> specialMap);
		_biomeSourceMap.TryGetValue(biomeIndex, out Dictionary<TerrainType, int[]> terrainMap);

		switch (urbanCellType)
		{
			case UrbanCellType.Road:
				if (terrainMap != null && terrainMap.TryGetValue(TerrainType.Concrete, out int[] roadFallback))
				{
					sourceId = PickUrbanRoadBaseSourceId(roadFallback, x, y);
					return true;
				}
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

	/// <summary>
	/// Sélectionne la tile de route directionnelle selon la connectivité aux 4 voisins cardinaux.
	/// Calcule un bitmask (L=8|R=4|U=2|D=1) et cherche la clé "road_{mask}".
	/// </summary>
	private bool TryGetDirectionalRoadSource(
		Dictionary<string, int[]> specialMap,
		int x,
		int y,
		out int sourceId)
	{
		sourceId = -1;
		if (_urbanLayout == null || specialMap == null)
			return false;

		int radius = _urbanLayout.MapRadius;
		int gridSize = radius * 2 + 1;
		int gx = x + radius;
		int gy = y + radius;

		bool hasLeft  = IsUrbanRoad(gx - 1, gy, gridSize);
		bool hasRight = IsUrbanRoad(gx + 1, gy, gridSize);
		bool hasUp    = IsUrbanRoad(gx, gy - 1, gridSize);
		bool hasDown  = IsUrbanRoad(gx, gy + 1, gridSize);

		int mask = (hasLeft ? RoadTileGenerator.ConnLeft : 0)
				 | (hasRight ? RoadTileGenerator.ConnRight : 0)
				 | (hasUp ? RoadTileGenerator.ConnUp : 0)
				 | (hasDown ? RoadTileGenerator.ConnDown : 0);

		string key = RoadTileGenerator.GetRoadKey(mask);
		return TryPickSpecialSource(specialMap, key, x, y, out sourceId);
	}

	private bool IsUrbanRoad(int gx, int gy, int gridSize)
	{
		if (gx < 0 || gx >= gridSize || gy < 0 || gy >= gridSize)
			return false;
		return _urbanLayout.CellGrid[gx, gy] == UrbanCellType.Road;
	}

	private bool TryGetSwampSourceId(
		int biomeIndex,
		TerrainType terrain,
		int x,
		int y,
		WorldGenerator generator,
		out int sourceId)
	{
		sourceId = -1;
		_biomeSpecialSourceMap.TryGetValue(biomeIndex, out Dictionary<string, int[]> specialMap);
		_biomeSourceMap.TryGetValue(biomeIndex, out Dictionary<TerrainType, int[]> terrainMap);

		if (terrain == TerrainType.Water)
		{
			if (terrainMap != null && terrainMap.TryGetValue(TerrainType.Water, out int[] waterSources) && waterSources.Length > 0)
			{
				sourceId = GetSwampWaterSourceId(waterSources, x, y, generator);
				return true;
			}

			return false;
		}

		int waterDistance = GetSwampWaterDistance(generator, x, y, 3);
		int moistureBand = HashCell(Mathf.FloorToInt(x / 8.0f) + 733, Mathf.FloorToInt(y / 8.0f) - 733) % 100;
		int openGroundNoise = HashCell(Mathf.FloorToInt(x / 9.0f) + 411, Mathf.FloorToInt(y / 9.0f) - 411) % 100;
		int sludgeRoll = HashCell(Mathf.FloorToInt(x / 11.0f) + 199, Mathf.FloorToInt(y / 11.0f) - 199) % 100;

		if (waterDistance == 1)
		{
			if (TryGetDirectionalSwampBankSource(specialMap, generator, x, y, out sourceId))
				return true;
			if (openGroundNoise < 18 && TryPickSpecialSource(specialMap, "bank_dirty", x, y, out sourceId))
				return true;
			if (openGroundNoise < 72 && TryPickSpecialSource(specialMap, "mud_transition", x, y, out sourceId))
				return true;
			if (TryPickSpecialSource(specialMap, "wet_mid_ground", x, y, out sourceId))
				return true;
		}

		if (waterDistance == 2)
		{
			if (sludgeRoll < 4 && moistureBand < 35 && generator.GetTerrain(x, y) == TerrainType.Forest && TryPickSpecialSource(specialMap, "sludge_dark", x, y, out sourceId))
				return true;
			if (openGroundNoise < 22 && TryPickSpecialSource(specialMap, "mud_transition", x, y, out sourceId))
				return true;
			if (moistureBand < 58 && TryPickSpecialSource(specialMap, "wet_mid_ground", x, y, out sourceId))
				return true;
		}

		if (waterDistance == 3)
		{
			if (terrain == TerrainType.Forest && moistureBand < 26 && TryPickSpecialSource(specialMap, "wet_mid_ground", x, y, out sourceId))
				return true;
			if (terrain == TerrainType.Grass && openGroundNoise < 12 && TryPickSpecialSource(specialMap, "wet_mid_ground", x, y, out sourceId))
				return true;
		}

		if (terrain == TerrainType.Forest && moistureBand < 12 && TryPickSpecialSource(specialMap, "wet_mid_ground", x, y, out sourceId))
			return true;

		if (terrain == TerrainType.Grass && openGroundNoise < 8 && TryPickSpecialSource(specialMap, "wet_mid_ground", x, y, out sourceId))
			return true;

		if (terrainMap != null && terrainMap.TryGetValue(terrain, out int[] groundSources) && groundSources.Length > 0)
		{
			sourceId = GetSwampGroundSourceId(groundSources, x, y, waterDistance);
			return true;
		}

		return false;
	}

	private static bool TryGetDirectionalSwampBankSource(
		Dictionary<string, int[]> specialMap,
		WorldGenerator generator,
		int x,
		int y,
		out int sourceId)
	{
		sourceId = -1;
		if (specialMap == null)
			return false;

		bool waterUp = IsSwampWater(generator, x, y - 1);
		bool waterRight = IsSwampWater(generator, x + 1, y);
		bool waterDown = IsSwampWater(generator, x, y + 1);
		bool waterLeft = IsSwampWater(generator, x - 1, y);
		int cardinalCount = (waterUp ? 1 : 0) + (waterRight ? 1 : 0) + (waterDown ? 1 : 0) + (waterLeft ? 1 : 0);

		if (cardinalCount == 2 && waterLeft && waterUp && TryPickSpecialSource(specialMap, "bank_inner_corner_nw", x, y, out sourceId))
			return true;
		if (cardinalCount == 2 && waterUp && waterRight && TryPickSpecialSource(specialMap, "bank_inner_corner_ne", x, y, out sourceId))
			return true;
		if (cardinalCount == 2 && waterRight && waterDown && TryPickSpecialSource(specialMap, "bank_inner_corner_se", x, y, out sourceId))
			return true;
		if (cardinalCount == 2 && waterDown && waterLeft && TryPickSpecialSource(specialMap, "bank_inner_corner_sw", x, y, out sourceId))
			return true;

		if (cardinalCount == 1 && waterLeft && TryPickSpecialSource(specialMap, "bank_edge_nw", x, y, out sourceId))
			return true;
		if (cardinalCount == 1 && waterUp && TryPickSpecialSource(specialMap, "bank_edge_ne", x, y, out sourceId))
			return true;
		if (cardinalCount == 1 && waterRight && TryPickSpecialSource(specialMap, "bank_edge_se", x, y, out sourceId))
			return true;
		if (cardinalCount == 1 && waterDown && TryPickSpecialSource(specialMap, "bank_edge_sw", x, y, out sourceId))
			return true;

		bool waterUpLeft = IsSwampWater(generator, x - 1, y - 1);
		bool waterUpRight = IsSwampWater(generator, x + 1, y - 1);
		bool waterDownRight = IsSwampWater(generator, x + 1, y + 1);
		bool waterDownLeft = IsSwampWater(generator, x - 1, y + 1);

		if (cardinalCount == 0 && waterUpLeft && TryPickSpecialSource(specialMap, "bank_outer_corner_nw", x, y, out sourceId))
			return true;
		if (cardinalCount == 0 && waterUpRight && TryPickSpecialSource(specialMap, "bank_outer_corner_ne", x, y, out sourceId))
			return true;
		if (cardinalCount == 0 && waterDownRight && TryPickSpecialSource(specialMap, "bank_outer_corner_se", x, y, out sourceId))
			return true;
		if (cardinalCount == 0 && waterDownLeft && TryPickSpecialSource(specialMap, "bank_outer_corner_sw", x, y, out sourceId))
			return true;

		return false;
	}

	private static int GetSwampGroundSourceId(int[] sources, int x, int y, int waterDistance)
	{
		if (sources.Length == 1)
			return sources[0];

		int patchX = Mathf.FloorToInt(x / 8.0f);
		int patchY = Mathf.FloorToInt(y / 8.0f);
		int patchRoll = HashCell(patchX + 177, patchY - 177) % 100;

		if (sources.Length >= 3)
		{
			if (waterDistance >= 3 && patchRoll < 18)
				return sources[2];
			if (patchRoll < 6)
				return sources[1];
			return sources[0];
		}

		return patchRoll < 22 ? sources[1] : sources[0];
	}

	private static int GetSwampWaterSourceId(int[] sources, int x, int y, WorldGenerator generator)
	{
		if (sources.Length == 1)
			return sources[0];

		int patchX = Mathf.FloorToInt(x / 6.0f);
		int patchY = Mathf.FloorToInt(y / 6.0f);
		int patchRoll = HashCell(patchX + 911, patchY - 911) % 100;
		int deepNeighbors = CountNeighborTerrain(generator, x, y, TerrainType.Water, 1);

		if (sources.Length >= 3)
		{
			if (deepNeighbors >= 6)
				return patchRoll < 24 ? sources[2] : sources[0];
			return patchRoll < 12 ? sources[1] : sources[0];
		}

		return patchRoll < 18 ? sources[1] : sources[0];
	}

	private static int GetSwampWaterDistance(WorldGenerator generator, int x, int y, int maxDistance)
	{
		for (int radius = 1; radius <= maxDistance; radius++)
		{
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
						continue;
					if (IsSwampWater(generator, x + dx, y + dy))
						return radius;
				}
			}
		}

		return maxDistance + 1;
	}

	private static bool IsSwampWater(WorldGenerator generator, int x, int y)
	{
		if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
			return false;
		return generator.GetTerrain(x, y) == TerrainType.Water;
	}

	private static int CountNeighborTerrain(WorldGenerator generator, int x, int y, TerrainType target, int radius)
	{
		int count = 0;
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				if (dx == 0 && dy == 0)
					continue;
				if (!generator.IsWithinBounds(x + dx, y + dy) || generator.IsErased(x + dx, y + dy))
					continue;
				if (generator.GetTerrain(x + dx, y + dy) == target)
					count++;
			}
		}

		return count;
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

	private static int PickUrbanRoadBaseSourceId(int[] sources, int x, int y)
	{
		if (sources.Length == 0)
			return -1;

		// Les trois premières textures du groupe "concrete" sont les sols routiers
		// sombres. Les variantes carrelées sont réservées aux intérieurs/plazas.
		int usableCount = Mathf.Min(3, sources.Length);
		int hash = HashCell(x, y);
		return sources[hash % usableCount];
	}
}
