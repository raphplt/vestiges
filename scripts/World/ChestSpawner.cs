using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Place les coffres du monde sur toute la carte (data/chests/chest_placement.json), par groupes dans l'ordre du
/// fichier : les plus rares d'abord. Emplacements et dégagements : <see cref="SitePlacer"/>.
/// </summary>
public static class ChestSpawner
{
    public static void SpawnChests(SitePlacer placer, Node2D container)
    {
        PackedScene chestScene = GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
        ChestPlacementData placement = ChestDataLoader.LoadPlacement();
        int spawned = 0;

        foreach (ChestPlacementGroup group in placement.Groups)
        {
            ChestData data = ChestDataLoader.Get(group.ChestId);
            if (data == null)
                continue;

            for (int i = 0; i < group.Count; i++)
            {
                if (!placer.TryPlace(group.BandMin, group.BandMax, placement.MinSpacingPx, out Vector2 position))
                {
                    GD.PushWarning($"[ChestSpawner] No room for {group.ChestId} in band {group.BandMin}-{group.BandMax}");
                    continue;
                }

                Chest chest = chestScene.Instantiate<Chest>();
                chest.GlobalPosition = position;
                container.AddChild(chest);
                chest.Initialize(data);
                spawned++;
            }
        }

        GD.Print($"[ChestSpawner] Spawned {spawned} chests");
    }
}
