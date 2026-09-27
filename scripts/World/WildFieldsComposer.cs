using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Compose la campagne (plan 08 P4b) autour des fermes, que pose <see cref="SiteComposer"/> : haies et vergers aux
/// bords des parcelles, scènes-récits. Plan dans data/world/farms.json.
/// </summary>
public static class WildFieldsComposer
{
    public const string BiomeId = "wild_fields";
    public const string Folder = "res://assets/props/wild_fields/";

    /// <summary>
    /// Paysage des champs (plan 08 P4b-3) : haies, murets et clôtures aux bords des parcelles, le long des allées ;
    /// vergers en rangs dans une partie des prairies. Cellules marquées par WildFieldsLayoutGenerator ; les fermes,
    /// les chemins et les points d'intérêt gardent la priorité. Haies non bloquantes, troncs des vergers bloquants.
    /// </summary>
    public static int PlaceParcelProps(WildFieldsLayout layout, FarmConfig config, HashSet<Vector2I> usedCells,
                                       HashSet<Vector2I> blockedCells, TileMapLayer ground, Node2D container, ulong seed)
    {
        if (layout == null || !config.Site.Enabled)
            return 0;
        Dictionary<string, Texture2D> cache = new();
        int placed = 0;
        if (config.ParcelEdges.Length > 0)
        {
            foreach (Vector2I cell in layout.HedgeCells)
            {
                if (usedCells.Contains(cell) || (blockedCells != null && blockedCells.Contains(cell)))
                    continue;
                uint pick = CellHash.Of(cell.X, cell.Y, seed ^ 0x4ED6FUL);
                Texture2D texture = SiteComposer.Load(Folder, config.ParcelEdges[(int)(pick % (uint)config.ParcelEdges.Length)], cache);
                if (texture == null)
                    continue;
                EnvironmentProp prop = new();
                prop.GlobalPosition = ground.MapToLocal(cell);
                container.AddChild(prop);
                prop.Initialize(texture, null, 0f, false);
                usedCells.Add(cell);
                placed++;
            }
        }
        if (config.OrchardTrees.Count > 0)
        {
            foreach (Vector2I cell in layout.OrchardCells)
            {
                if (usedCells.Contains(cell) || (blockedCells != null && blockedCells.Contains(cell)))
                    continue;
                uint pick = CellHash.Of(cell.X, cell.Y, seed ^ 0x0C4A3UL);
                (string baseStem, string canopyStem) = config.OrchardTrees[(int)(pick % (uint)config.OrchardTrees.Count)];
                Texture2D trunk = SiteComposer.Load(Folder, baseStem, cache);
                Texture2D canopy = SiteComposer.Load(Folder, canopyStem, cache);
                if (trunk == null || canopy == null)
                    continue;
                EnvironmentProp prop = new();
                prop.GlobalPosition = ground.MapToLocal(cell);
                container.AddChild(prop);
                prop.Initialize(trunk, canopy, 0f, true);
                usedCells.Add(cell);
                placed++;
            }
        }
        GD.Print($"[WildFieldsComposer] {placed} haies, murets et arbres de verger");
        return placed;
    }

    /// <summary>
    /// Scènes-récits (plan 08 P4b-4) : une région de champs sur deux reçoit, près de son centre, une petite scène
    /// lisible d'un coup d'œil (pique-nique abandonné, linge encore étendu, épouvantail couronné de corbeaux).
    /// </summary>
    public static List<Vector2> PlaceScenes(WorldGenerator generator, FarmConfig config, HashSet<Vector2I> usedCells,
                                            HashSet<Vector2I> blockedCells, TileMapLayer ground, Node2D container, ulong seed)
    {
        List<Vector2> spots = new();
        if (!config.Site.Enabled || config.Scenes.Length == 0)
            return spots;
        Dictionary<string, Texture2D> cache = new();
        uint threshold = (uint)(Mathf.Clamp(config.SceneChancePerRegion, 0f, 1f) * 1000f);
        foreach (Vector2 center in generator.BiomeRegionCenters)
        {
            Vector2I origin = new(Mathf.RoundToInt(center.X), Mathf.RoundToInt(center.Y));
            if (generator.GetBiomeId(origin.X, origin.Y) != BiomeId || CellHash.Of(origin.X, origin.Y, seed ^ 0x5CE7EUL) % 1000 >= threshold)
                continue;
            if (!TryFindFreeCell(generator, origin, usedCells, blockedCells, seed, out Vector2I cell))
                continue;
            string sprite = config.Scenes[(int)(CellHash.Of(origin.X, origin.Y, seed ^ 0x5CE7FUL) % (uint)config.Scenes.Length)];
            Texture2D texture = SiteComposer.Load(Folder, sprite, cache);
            if (texture == null)
                continue;
            EnvironmentProp prop = new();
            prop.GlobalPosition = ground.MapToLocal(cell);
            container.AddChild(prop);
            prop.Initialize(texture, null, 0f, System.Array.IndexOf(config.BlockingScenes, sprite) >= 0);
            usedCells.Add(cell);
            // Le conteneur est à l'origine (et hors de l'arbre au chargement) : la position locale est la position monde.
            spots.Add(prop.Position);
        }
        GD.Print($"[WildFieldsComposer] {spots.Count} scènes-récits");
        return spots;
    }

    /// <summary>Une cellule de champs libre à quelques cases du centre de la région, tirée par graine.</summary>
    private static bool TryFindFreeCell(WorldGenerator generator, Vector2I origin, HashSet<Vector2I> usedCells,
                                        HashSet<Vector2I> blockedCells, ulong seed, out Vector2I cell)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            uint roll = CellHash.Of(origin.X * 31 + attempt, origin.Y, seed ^ 0x5CE80UL);
            cell = new Vector2I(origin.X + (int)(roll % 13) - 6, origin.Y + (int)((roll >> 8) % 25) - 12);
            if (!generator.IsWithinBounds(cell.X, cell.Y) || generator.IsErased(cell.X, cell.Y) || generator.GetBiomeId(cell.X, cell.Y) != BiomeId
                || generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water || usedCells.Contains(cell)
                || (blockedCells != null && blockedCells.Contains(cell)))
                continue;
            return true;
        }
        cell = default;
        return false;
    }
}
