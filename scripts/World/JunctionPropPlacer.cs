using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Décors de transition le long des frontières de biomes (plan 10 T2) : herbes et buissons entre forêt et champs,
/// gravats entre ville et carrière… Tirés dans la liste de la paire de biomes, posés sur les cellules proches
/// de la frontière avant les décors génériques, jamais bloquants. Réglages : junction_props dans world_gen.json.
/// </summary>
public static class JunctionPropPlacer
{
    public sealed class Config
    {
        public bool Enabled;
        public int BandCells = 2;
        public float Chance = 0.07f;
        public readonly Dictionary<string, string[]> Pairs = new();

        public static Config Parse(Godot.Collections.Dictionary dict)
        {
            Config config = new()
            {
                Enabled = dict.GetValueOrDefault("enabled", false).AsBool(),
                BandCells = (int)dict.GetValueOrDefault("band_cells", 2).AsDouble(),
                Chance = (float)dict.GetValueOrDefault("chance", 0.07f).AsDouble(),
            };
            if (dict.ContainsKey("pairs"))
            {
                Godot.Collections.Dictionary pairs = dict["pairs"].AsGodotDictionary();
                foreach (Variant key in pairs.Keys)
                    config.Pairs[key.AsString()] = pairs[key].AsStringArray();
            }
            return config;
        }
    }

    public static int Place(WorldGenerator generator, Config config, HashSet<Vector2I> usedCells, HashSet<Vector2I> blockedCells,
                            TileMapLayer ground, Node2D container, ulong seed)
    {
        if (config == null || !config.Enabled || config.Pairs.Count == 0)
            return 0;

        uint threshold = (uint)(Mathf.Clamp(config.Chance, 0f, 1f) * 1000f);
        Dictionary<string, Texture2D> cache = new();
        int radius = generator.MapRadius;
        int band = Mathf.Max(1, config.BandCells);
        int placed = 0;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y) || generator.GetTerrain(x, y) == TerrainType.Water)
                    continue;
                Vector2I cell = new(x, y);
                if (CellHash.Of(x, y, seed ^ 0x7C0DUL) % 1000 >= threshold || usedCells.Contains(cell)
                    || (blockedCells != null && blockedCells.Contains(cell)))
                    continue;
                string own = generator.GetBiomeId(x, y);
                string other = NeighbourBiome(generator, x, y, own, band);
                if (other == null)
                    continue;
                string key = string.CompareOrdinal(own, other) < 0 ? $"{own}+{other}" : $"{other}+{own}";
                if (!config.Pairs.TryGetValue(key, out string[] sprites) || sprites.Length == 0)
                    continue;

                string path = sprites[(int)(CellHash.Of(x, y, seed ^ 0x7C0EUL) % (uint)sprites.Length)];
                Texture2D texture = Load(path, cache);
                if (texture == null)
                    continue;
                // Décalés dans leur case : les cellules de la bande forment des lignes le long de la frontière.
                uint jitter = CellHash.Of(x, y, seed ^ 0x7C0FUL);
                Vector2 offset = new((jitter % 29) - 14f, ((jitter >> 8) % 13) - 6f);
                EnvironmentProp prop = new();
                prop.GlobalPosition = ground.MapToLocal(cell) + offset;
                container.AddChild(prop);
                prop.Initialize(texture, null, 0f, false);
                usedCells.Add(cell);
                placed++;
            }
        }
        GD.Print($"[JunctionPropPlacer] {placed} décors de transition");
        return placed;
    }

    /// <summary>Un autre biome à moins de <paramref name="band"/> cases (grille « stacked » : ±band colonnes, ±2·band rangs).</summary>
    private static string NeighbourBiome(WorldGenerator generator, int x, int y, string own, int band)
    {
        for (int dy = -band * 2; dy <= band * 2; dy++)
        {
            for (int dx = -band; dx <= band; dx++)
            {
                if (!generator.IsWithinBounds(x + dx, y + dy) || generator.IsErased(x + dx, y + dy))
                    continue;
                string other = generator.GetBiomeId(x + dx, y + dy);
                if (other != null && other != own)
                    return other;
            }
        }
        return null;
    }

    private static Texture2D Load(string path, Dictionary<string, Texture2D> cache)
    {
        if (cache.TryGetValue(path, out Texture2D texture))
            return texture;
        string resource = $"res://assets/props/{path}.png";
        texture = ResourceLoader.Exists(resource) ? GD.Load<Texture2D>(resource) : null;
        if (texture == null)
            GD.PushWarning($"[JunctionPropPlacer] décor introuvable : {resource}");
        cache[path] = texture;
        return texture;
    }
}
