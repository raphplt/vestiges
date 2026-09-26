using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Place les coffres du monde sur toute la carte (data/chests/chest_placement.json), avant les décors :
/// chaque coffre réserve autour de lui un dégagement que les placeurs de décors respectent, pour qu'aucun
/// arbre ni immeuble posé devant ne le masque. Tirage reproductible par la graine du monde.
/// </summary>
public static class ChestSpawner
{
    private const float CellWidth = 64f;
    private const float RowHeight = 16f;

    public static void SpawnChests(
        WorldGenerator generator,
        TileMapLayer ground,
        Node2D container,
        HashSet<Vector2I> usedCells,
        ulong seed,
        UrbanLayout urbanLayout,
        WildFieldsLayout wildFieldsLayout)
    {
        PackedScene chestScene = GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
        ChestPlacementData placement = ChestDataLoader.LoadPlacement();
        RandomNumberGenerator rng = new() { Seed = seed ^ 0xC4E57UL };
        PlacementContext context = new(generator, ground, usedCells, urbanLayout, wildFieldsLayout, placement);
        List<Vector2> placed = new();

        foreach (ChestPlacementGroup group in placement.Groups)
        {
            ChestData data = ChestDataLoader.Get(group.ChestId);
            if (data == null)
                continue;

            for (int i = 0; i < group.Count; i++)
            {
                if (!TryPickCell(context, group, placed, rng, out Vector2I cell))
                {
                    GD.PushWarning($"[ChestSpawner] No room for {group.ChestId} in band {group.BandMin}-{group.BandMax}");
                    continue;
                }

                Vector2 worldPos = ground.MapToLocal(cell);
                ReserveClearance(context, cell);
                placed.Add(worldPos);

                Chest chest = chestScene.Instantiate<Chest>();
                chest.GlobalPosition = worldPos;
                container.AddChild(chest);
                chest.Initialize(data);
            }
        }

        GD.Print($"[ChestSpawner] Spawned {placed.Count} chests");
    }

    private readonly record struct PlacementContext(
        WorldGenerator Generator,
        TileMapLayer Ground,
        HashSet<Vector2I> UsedCells,
        UrbanLayout Urban,
        WildFieldsLayout Fields,
        ChestPlacementData Placement);

    /// <summary>Plusieurs candidats valides par coffre ; garde celui qui est sur un chemin et loin des autres coffres.</summary>
    private static bool TryPickCell(PlacementContext context, ChestPlacementGroup group, List<Vector2> placed,
        RandomNumberGenerator rng, out Vector2I best)
    {
        best = default;
        float bestScore = float.MinValue;
        int radius = context.Generator.MapRadius;
        float innerSq = group.BandMin * group.BandMin;
        float outerSq = group.BandMax * group.BandMax;
        int valid = 0;

        for (int attempt = 0; attempt < context.Placement.AttemptsPerChest && valid < context.Placement.CandidatesPerChest; attempt++)
        {
            // Uniforme en surface dans la couronne : sans la racine, les coffres s'entasseraient au bord intérieur.
            float distance = radius * Mathf.Sqrt(rng.RandfRange(innerSq, outerSq));
            float angle = rng.RandfRange(0f, Mathf.Tau);
            Vector2I cell = new(Mathf.RoundToInt(Mathf.Cos(angle) * distance), Mathf.RoundToInt(Mathf.Sin(angle) * distance));
            if (!IsCellAllowed(context, cell))
                continue;

            Vector2 worldPos = context.Ground.MapToLocal(cell);
            float nearest = NearestDistance(worldPos, placed);
            if (nearest < context.Placement.MinSpacingPx)
                continue;

            valid++;
            float score = Mathf.Min(nearest / context.Placement.MinSpacingPx, 2f);
            if (IsOnPath(context, cell))
                score += context.Placement.PathBonus;
            if (score > bestScore)
            {
                bestScore = score;
                best = cell;
            }
        }

        return valid > 0;
    }

    private static bool IsCellAllowed(PlacementContext context, Vector2I cell)
    {
        WorldGenerator generator = context.Generator;
        if (!generator.IsWithinBounds(cell.X, cell.Y) || generator.IsErased(cell.X, cell.Y))
            return false;
        if (generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water)
            return false;

        bool free = true;
        ForEachCellInClearance(context.Ground, cell, context.Placement.Clearance, c =>
        {
            if (context.UsedCells.Contains(c))
                free = false;
        });
        if (!free || context.Urban == null)
            return free;

        // Un immeuble posé devant le coffre, ou le coffre dans un îlot, le rendrait invisible.
        ForEachCellInClearance(context.Ground, cell, context.Placement.BuildingClearance, c =>
        {
            UrbanCellType type = UrbanCell(context.Urban, c);
            if (type == UrbanCellType.BuildingInterior || type == UrbanCellType.BuildingWall)
                free = false;
        });
        return free;
    }

    private static bool IsOnPath(PlacementContext context, Vector2I cell)
    {
        if (context.Fields != null && context.Fields.PathCells.Contains(cell))
            return true;
        if (context.Urban == null)
            return false;
        UrbanCellType type = UrbanCell(context.Urban, cell);
        return type == UrbanCellType.Road || type == UrbanCellType.Sidewalk || type == UrbanCellType.Plaza;
    }

    private static UrbanCellType UrbanCell(UrbanLayout layout, Vector2I cell)
    {
        int gx = cell.X + layout.MapRadius;
        int gy = cell.Y + layout.MapRadius;
        if (gx < 0 || gy < 0 || gx >= layout.CellGrid.GetLength(0) || gy >= layout.CellGrid.GetLength(1))
            return UrbanCellType.None;
        return layout.CellGrid[gx, gy];
    }

    private static void ReserveClearance(PlacementContext context, Vector2I cell)
    {
        HashSet<Vector2I> used = context.UsedCells;
        ForEachCellInClearance(context.Ground, cell, context.Placement.Clearance, c => used.Add(c));
    }

    /// <summary>
    /// Cellules dont le centre tombe dans la zone en pixels autour de <paramref name="cell"/>. Grille « stacked » :
    /// une colonne décale de 64 px, un rang de 16 px en quinconce, d'où la zone en pixels et non en cellules.
    /// </summary>
    private static void ForEachCellInClearance(TileMapLayer ground, Vector2I cell, PixelClearance clearance, System.Action<Vector2I> visit)
    {
        Vector2 origin = ground.MapToLocal(cell);
        int rowMin = cell.Y - Mathf.CeilToInt(clearance.North / RowHeight);
        int rowMax = cell.Y + Mathf.CeilToInt(clearance.South / RowHeight);
        int columnSpan = Mathf.CeilToInt(clearance.Side / CellWidth) + 1;
        for (int y = rowMin; y <= rowMax; y++)
        {
            for (int x = cell.X - columnSpan; x <= cell.X + columnSpan; x++)
            {
                Vector2I candidate = new(x, y);
                if (Mathf.Abs(ground.MapToLocal(candidate).X - origin.X) <= clearance.Side)
                    visit(candidate);
            }
        }
    }

    private static float NearestDistance(Vector2 position, List<Vector2> placed)
    {
        float nearest = float.MaxValue;
        foreach (Vector2 other in placed)
            nearest = Mathf.Min(nearest, position.DistanceTo(other));
        return nearest;
    }
}
