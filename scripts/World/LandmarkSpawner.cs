using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Place les Mémoriaux, les Failles, les Ateliers, les stèles du Péril et les Puits du départ (data/world/landmarks.json), après les coffres et par le même
/// <see cref="SitePlacer"/> : ils s'écartent des coffres et réservent leur dégagement avant les décors.
/// </summary>
public static class LandmarkSpawner
{
    // Ateliers, stèles et Puits en réserve (plan 56 B2) : aucun posé. Placés après les autres lieux, les sauter ne
    // déplace ni coffre, ni Mémorial, ni Faille.
    private static readonly List<LandmarkBand> NoBands = new();

    /// <param name="memorialSlots">Places déjà réservées aux Mémoriaux des pièces de la terre (plan 58) : prises d'abord.</param>
    public static void SpawnLandmarks(SitePlacer placer, Node2D container, IReadOnlyList<Vector2> memorialSlots)
    {
        MemorialConfig memorial = LandmarkDataLoader.Memorial;
        int spawned = 0;
        int slotted = 0;
        foreach (LandmarkBand band in memorial.Placement)
        {
            int count = placer.Scaled("memorial", band.Count);
            for (int i = 0; i < count; i++)
            {
                Vector2 ring = placer.MemorialBand ?? new Vector2(band.Min, band.Max);
                Vector2 position;
                if (slotted < memorialSlots.Count)
                    position = memorialSlots[slotted++];
                else if (!placer.TryPlace(ring.X, ring.Y, memorial.MinSpacingPx, out position))
                {
                    GD.PushWarning($"[LandmarkSpawner] No room for a memorial in band {band.Min}-{band.Max}");
                    continue;
                }
                Memorial node = new() { Name = $"Memorial{++spawned}", GlobalPosition = position };
                node.Initialize(memorial);
                container.AddChild(node);
            }
        }
        // Une place de pièce sans Mémorial reste réservée et vide : la terre en pose moins que ses pièces n'en prévoient.
        if (slotted < memorialSlots.Count)
            GD.PushWarning($"[LandmarkSpawner] {memorialSlots.Count - slotted} memorial slot(s) of land pieces left empty");
        GD.Print($"[LandmarkSpawner] Spawned {spawned} memorials, {slotted} in land pieces");

        RiftConfig rift = LandmarkDataLoader.Rift;
        int rifts = 0;
        foreach (LandmarkBand band in rift.Placement)
        {
            int count = placer.Scaled("rift", band.Count);
            for (int i = 0; i < count; i++)
            {
                if (!placer.TryPlace(band.Min, band.Max, rift.MinSpacingPx, out Vector2 position))
                {
                    GD.PushWarning($"[LandmarkSpawner] No room for a rift in band {band.Min}-{band.Max}");
                    continue;
                }
                Rift node = new() { Name = $"Rift{++rifts}", GlobalPosition = position };
                node.Initialize(rift);
                container.AddChild(node);
            }
        }
        GD.Print($"[LandmarkSpawner] Spawned {rifts} rifts");

        // Ateliers (plan 22 C2) : placés en dernier, pour que les tirages des lieux plus anciens ne bougent pas.
        WorkshopConfig workshop = LandmarkDataLoader.Workshop;
        int workshops = 0;
        foreach (LandmarkBand band in ReserveSwitch.IsOn(workshop.Enabled) ? workshop.Placement : NoBands)
        {
            int count = placer.Scaled("workshop", band.Count);
            for (int i = 0; i < count; i++)
            {
                if (!placer.TryPlace(band.Min, band.Max, workshop.MinSpacingPx, out Vector2 position))
                {
                    GD.PushWarning($"[LandmarkSpawner] No room for a workshop in band {band.Min}-{band.Max}");
                    continue;
                }
                Workshop node = new() { Name = $"Workshop{++workshops}", GlobalPosition = position };
                node.Initialize(workshop);
                container.AddChild(node);
            }
        }
        GD.Print($"[LandmarkSpawner] Spawned {workshops} workshops");

        // Stèles du Péril (plan 28 P2) : après les Ateliers, pour la même raison.
        PerilSteleConfig stele = LandmarkDataLoader.PerilStele;
        int steles = 0;
        foreach (LandmarkBand band in ReserveSwitch.IsOn(stele.Enabled) ? stele.Placement : NoBands)
        {
            int count = placer.Scaled("peril_stele", band.Count);
            for (int i = 0; i < count; i++)
            {
                if (!placer.TryPlace(band.Min, band.Max, stele.MinSpacingPx, out Vector2 position))
                {
                    GD.PushWarning($"[LandmarkSpawner] No room for a peril stele in band {band.Min}-{band.Max}");
                    continue;
                }
                PerilStele node = new() { Name = $"PerilStele{++steles}", GlobalPosition = position };
                node.Initialize(stele);
                container.AddChild(node);
            }
        }
        GD.Print($"[LandmarkSpawner] Spawned {steles} peril steles");

        // Puits de l'oubli (plan 44) : placés en dernier, pour la même raison.
        WellConfig well = LandmarkDataLoader.Well;
        int wells = 0;
        foreach (LandmarkBand band in ReserveSwitch.IsOn(well.Enabled) ? well.Placement : NoBands)
        {
            int count = placer.Scaled("well", band.Count);
            for (int i = 0; i < count; i++)
            {
                if (!placer.TryPlace(band.Min, band.Max, well.MinSpacingPx, out Vector2 position))
                {
                    GD.PushWarning($"[LandmarkSpawner] No room for a well in band {band.Min}-{band.Max}");
                    continue;
                }
                Well node = new() { Name = $"Well{++wells}", GlobalPosition = position };
                node.Initialize(well);
                container.AddChild(node);
            }
        }
        GD.Print($"[LandmarkSpawner] Spawned {wells} wells");
    }
}
