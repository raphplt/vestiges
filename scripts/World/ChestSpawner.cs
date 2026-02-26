using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Place des coffres standalone sur la map, en dehors des POI.
/// La rareté dépend de la distance au Foyer et du danger du biome.
/// </summary>
public static class ChestSpawner
{
    private const int CommonChestCount = 8;
    private const int RareChestCount = 4;
    private const int EpicChestCount = 1;
    private const int LoreChestCount = 2;

    public static void SpawnChests(
        WorldGenerator generator,
        TileMapLayer ground,
        Node2D container,
        HashSet<Vector2I> usedCells)
    {
        PackedScene chestScene = GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
        if (chestScene == null)
        {
            GD.PushError("[ChestSpawner] Cannot load Chest.tscn");
            return;
        }

        int total = 0;
        total += SpawnChestsOfType("chest_common", CommonChestCount, 6, 50, generator, ground, container, usedCells, chestScene);
        total += SpawnChestsOfType("chest_rare", RareChestCount, 15, 55, generator, ground, container, usedCells, chestScene);
        total += SpawnChestsOfType("chest_epic", EpicChestCount, 30, 55, generator, ground, container, usedCells, chestScene);
        total += SpawnChestsOfType("chest_lore", LoreChestCount, 18, 55, generator, ground, container, usedCells, chestScene);

        GD.Print($"[ChestSpawner] Spawned {total} chests");
    }

    private static int SpawnChestsOfType(
        string chestId,
        int count,
        int minDist,
        int maxDist,
        WorldGenerator generator,
        TileMapLayer ground,
        Node2D container,
        HashSet<Vector2I> usedCells,
        PackedScene chestScene)
    {
        ChestData data = ChestDataLoader.Get(chestId);
        if (data == null)
            return 0;

        int spawned = 0;
        int safeRadius = generator.MapRadius - 5;

        for (int i = 0; i < count; i++)
        {
            Vector2I cell = PickChestCell(generator, usedCells, safeRadius, minDist, maxDist);
            if (cell == new Vector2I(int.MinValue, int.MinValue))
                continue;

            usedCells.Add(cell);

            Vector2 worldPos = ground.MapToLocal(cell);
            Chest chest = chestScene.Instantiate<Chest>();
            chest.GlobalPosition = worldPos;
            container.AddChild(chest);
            chest.Initialize(data);
            spawned++;
        }

        return spawned;
    }

    private static Vector2I PickChestCell(
        WorldGenerator generator,
        HashSet<Vector2I> usedCells,
        int safeRadius,
        int minDist,
        int maxDist)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = (int)GD.RandRange(-safeRadius + 1, safeRadius);
            int y = (int)GD.RandRange(-safeRadius + 1, safeRadius);

            float distFromCenter = Mathf.Sqrt(x * x + y * y);
            if (distFromCenter > safeRadius || distFromCenter < minDist || distFromCenter > maxDist)
                continue;

            if (generator.GetTerrain(x, y) == TerrainType.Water)
                continue;

            if (generator.IsErased(x, y))
                continue;

            Vector2I cell = new(x, y);
            if (usedCells.Contains(cell))
                continue;

            return cell;
        }

        return new Vector2I(int.MinValue, int.MinValue);
    }
}
