using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Passerelles des chenaux (plan 58 T2a) : sur chaque cellule de chenal que l'axe d'un chemin franchit, un caillebotis
/// tourné selon la traversée (<see cref="BridgeHeading"/>). Jamais bloquant, plaqué au sol : on marche dessus.
/// </summary>
public static class ChannelBridgePlacer
{
    public static int Place(WorldGenerator generator, PathNetwork paths, IReadOnlyDictionary<string, TerrainRhythm> rhythms,
        Node2D container)
    {
        if (paths == null || paths.Bridges.Count == 0)
            return 0;
        Dictionary<string, Texture2D> cache = new();
        int placed = 0;
        foreach (ChannelBridge bridge in paths.Bridges)
        {
            Vector2I cell = PathNetworkGenerator.CellAt(bridge.Position);
            if (!rhythms.TryGetValue(generator.GetBiomeId(cell.X, cell.Y) ?? "", out TerrainRhythm rhythm)
                || rhythm.ChannelSet is not TerrainRhythm.Channels channels)
                continue;
            string sprite = channels.Bridges[(int)bridge.Heading];
            Texture2D texture = Load(sprite, cache);
            if (texture == null)
                continue;
            EnvironmentProp prop = new();
            prop.GlobalPosition = bridge.Position;
            container.AddChild(prop);
            prop.Initialize(texture, null, 0f, false, flat: true);
            placed++;
        }
        GD.Print($"[ChannelBridgePlacer] {placed} passerelles");
        return placed;
    }

    private static Texture2D Load(string path, Dictionary<string, Texture2D> cache)
    {
        if (cache.TryGetValue(path, out Texture2D texture))
            return texture;
        string resource = $"res://{path}";
        texture = ResourceLoader.Exists(resource) ? GD.Load<Texture2D>(resource) : null;
        if (texture == null)
            GD.PushWarning($"[ChannelBridgePlacer] passerelle introuvable : {resource}");
        cache[path] = texture;
        return texture;
    }
}
