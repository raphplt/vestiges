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
	// biomeIndex → (TerrainType → sourceIds[])
	private readonly Dictionary<int, Dictionary<TerrainType, int[]>> _biomeSourceMap = new();

	// Tiles d'eau communes (remplacement global du water générique)
	private int[] _commonWaterSources;

	// Tiles de dissolution pour le dégradé visuel en bordure de carte
	// Index 0 = légère dissolution, dernier = quasi-vide
	private int[] _dissolutionSources;

	private static readonly Dictionary<TerrainType, int> GenericSources = new()
	{
		{ TerrainType.Grass, 0 },
		{ TerrainType.Concrete, 1 },
		{ TerrainType.Water, 2 },
		{ TerrainType.Forest, 3 }
	};

	private static readonly Dictionary<string, TerrainType> TerrainNameMap = new()
	{
		{ "grass", TerrainType.Grass },
		{ "concrete", TerrainType.Concrete },
		{ "water", TerrainType.Water },
		{ "forest", TerrainType.Forest }
	};

	public void Initialize(TileSet tileSet, List<BiomeData> activeBiomes)
	{
		// Charger les tiles d'eau communes (eau profonde par défaut)
		_commonWaterSources = LoadTileGroup(tileSet, new List<string>
		{
			"commun/tile_eau_profonde_base",
			"commun/tile_eau_profonde_v2"
		});

		// Charger les tiles de dissolution (n1 = léger → n3 = quasi-vide)
		_dissolutionSources = LoadTileGroup(tileSet, new List<string>
		{
			"commun/tile_dissolution_n1",
			"commun/tile_dissolution_n2",
			"commun/tile_dissolution_n3"
		});

		for (int i = 0; i < activeBiomes.Count; i++)
		{
			BiomeData biome = activeBiomes[i];
			if (biome.TileSources.Count == 0)
				continue;

			Dictionary<TerrainType, int[]> terrainMap = new();

			foreach (KeyValuePair<string, List<string>> kv in biome.TileSources)
			{
				if (!TerrainNameMap.TryGetValue(kv.Key, out TerrainType terrainType))
				{
					GD.PushWarning($"[BiomeTileMapper] Terrain inconnu '{kv.Key}' dans biome '{biome.Id}'");
					continue;
				}

				int[] sources = LoadTileGroup(tileSet, kv.Value);
				if (sources.Length > 0)
					terrainMap[terrainType] = sources;
			}

			if (terrainMap.Count > 0)
			{
				_biomeSourceMap[i] = terrainMap;
				GD.Print($"[BiomeTileMapper] Biome '{biome.Id}' : {terrainMap.Count} terrain(s) mappé(s)");
			}
		}

		GD.Print($"[BiomeTileMapper] Initialisé — {_biomeSourceMap.Count} biome(s), {_commonWaterSources.Length} eau commune");
	}

	/// <summary>
	/// Retourne le sourceId à utiliser pour une cellule donnée.
	/// Sélection déterministe de variante basée sur la position (même seed visuel).
	/// </summary>
	public int GetSourceId(int biomeIndex, TerrainType terrain, int x, int y)
	{
		// Tiles spécifiques au biome
		if (biomeIndex >= 0 && _biomeSourceMap.TryGetValue(biomeIndex, out Dictionary<TerrainType, int[]> terrainMap))
		{
			if (terrainMap.TryGetValue(terrain, out int[] sources))
			{
				int hash = HashCell(x, y);
				return sources[hash % sources.Length];
			}
		}

		// Fallback eau commune (remplace le water générique partout)
		if (terrain == TerrainType.Water && _commonWaterSources.Length > 0)
		{
			int hash = HashCell(x, y);
			return _commonWaterSources[hash % _commonWaterSources.Length];
		}

		// Fallback tiles génériques (sources 0-3 du tileset original)
		return GenericSources[terrain];
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
}
