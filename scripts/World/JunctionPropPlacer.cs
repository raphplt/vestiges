using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

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

        /// <summary>
        /// Section <c>junction_props</c> lue strictement : chaque paire « a+b » réunit deux biomes connus dans l'ordre
        /// des identifiants, et ne propose que des décors présents (chemins relatifs à assets/props, sans extension).
        /// </summary>
        public static Config Read(JsonConfigReader reader, JsonElement section, Func<string, bool> biomeExists,
            Func<string, bool> propExists)
        {
            reader.AllowOnly(section, "junction_props", "enabled", "band_cells", "chance", "pairs");
            Config config = new() { Enabled = reader.Flag(section, "enabled") };
            if (JsonConfigReader.Has(section, "band_cells"))
                config.BandCells = reader.Integer(section, "band_cells", 0, 16);
            if (JsonConfigReader.Has(section, "chance"))
                config.Chance = reader.Chance(section, "chance");
            if (!JsonConfigReader.Has(section, "pairs"))
                return config;
            JsonElement pairs = reader.Section(section, "pairs");
            foreach ((string key, JsonElement _) in reader.Entries(pairs, "junction_props.pairs"))
            {
                string[] biomes = key.Split('+');
                if (biomes.Length != 2 || !biomeExists(biomes[0]) || !biomeExists(biomes[1])
                    || string.CompareOrdinal(biomes[0], biomes[1]) >= 0)
                {
                    reader.Fail($"junction_props.pairs : « {key} » (deux biomes connus, triés, joints par +, attendus)");
                    continue;
                }
                List<string> sprites = new();
                foreach (JsonElement item in reader.List(pairs, key, 1))
                {
                    string path = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                    if (string.IsNullOrEmpty(path) || path.Contains("..", StringComparison.Ordinal) || !propExists(path))
                        reader.Fail($"junction_props.pairs.{key} : décor « {path} » introuvable");
                    else
                        sprites.Add(path);
                }
                config.Pairs[key] = sprites.ToArray();
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
        int band = Mathf.Max(1, config.BandCells);
        int placed = 0;
        for (int y = -generator.MapRadiusY; y <= generator.MapRadiusY; y++)
        {
            for (int x = -generator.MapRadiusX; x <= generator.MapRadiusX; x++)
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
