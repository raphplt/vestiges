using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Place les Mémoriaux (data/world/landmarks.json), après les coffres et par le même <see cref="SitePlacer"/> :
/// ils s'écartent des coffres et réservent leur dégagement avant les décors.
/// </summary>
public static class LandmarkSpawner
{
    public static void SpawnLandmarks(SitePlacer placer, Node2D container)
    {
        MemorialConfig memorial = LandmarkDataLoader.Memorial;
        int spawned = 0;
        foreach (LandmarkBand band in memorial.Placement)
        {
            for (int i = 0; i < band.Count; i++)
            {
                if (!placer.TryPlace(band.Min, band.Max, memorial.MinSpacingPx, out Vector2 position))
                {
                    GD.PushWarning($"[LandmarkSpawner] No room for a memorial in band {band.Min}-{band.Max}");
                    continue;
                }
                Memorial node = new() { Name = $"Memorial{++spawned}", GlobalPosition = position };
                node.Initialize(memorial);
                container.AddChild(node);
            }
        }
        GD.Print($"[LandmarkSpawner] Spawned {spawned} memorials");
    }
}
