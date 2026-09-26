using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-junctions : pour chaque paire de biomes voisins, la frontière la plus proche du départ,
/// capturée avec les décors puis sol seul (zoom ×2). Avant/après : basculer <c>ground_blend.enabled</c>.
/// </summary>
public partial class RunObservation
{
    private const int JunctionSearchRadius = 90;

    private async Task CaptureJunctions()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        await Frames(90);

        _player.AIInputOverride = Vector2.Zero;
        Node2D props = _world.GetNode<Node2D>("PropContainer");
        Node2D decals = _world.GetNodeOrNull<Node2D>("GroundDecals");
        Vector2 initialZoom = _camera.Zoom;
        foreach ((string pair, Vector2 point) in FindJunctions())
        {
            _player.GlobalPosition = point;
            _camera.Zoom = initialZoom;
            _camera.ResetSmoothing();
            await Frames(20);
            using (Image image = GetViewport().GetTexture().GetImage())
                image.SavePng($"{_output}/junction-{pair}.png");

            props.Visible = false;
            if (decals != null)
                decals.Visible = false;
            _camera.Zoom = initialZoom * 2f;
            await Frames(10);
            using (Image ground = GetViewport().GetTexture().GetImage())
                ground.SavePng($"{_output}/junction-{pair}-sol.png");
            props.Visible = true;
            if (decals != null)
                decals.Visible = true;
            GD.Print($"[RunObservation] jonction {pair} en {point}");
        }
        _camera.Zoom = initialZoom;
    }

    /// <summary>Par paire de biomes : la cellule frontière (voisin de droite ou du dessous) la plus proche du départ.</summary>
    private List<(string Pair, Vector2 Point)> FindJunctions()
    {
        TileMapLayer ground = _world.GetNode<TileMapLayer>("Ground");
        Dictionary<string, (Vector2 Point, float Distance)> best = new();
        for (int y = -JunctionSearchRadius; y <= JunctionSearchRadius; y++)
        {
            for (int x = -JunctionSearchRadius; x <= JunctionSearchRadius; x++)
            {
                Vector2 here = ground.MapToLocal(new Vector2I(x, y));
                string biome = _world.GetBiomeAt(here)?.Id;
                if (biome == null)
                    continue;
                foreach (Vector2I neighbour in new[] { new Vector2I(x + 1, y), new Vector2I(x, y + 2) })
                {
                    Vector2 there = ground.MapToLocal(neighbour);
                    string other = _world.GetBiomeAt(there)?.Id;
                    if (other == null || other == biome)
                        continue;
                    string pair = string.CompareOrdinal(biome, other) < 0 ? $"{biome}-{other}" : $"{other}-{biome}";
                    Vector2 middle = (here + there) * 0.5f;
                    float distance = middle.LengthSquared();
                    if (!best.TryGetValue(pair, out (Vector2 Point, float Distance) current) || distance < current.Distance)
                        best[pair] = (middle, distance);
                }
            }
        }

        List<(string, Vector2)> result = new();
        foreach ((string pair, (Vector2 point, float _)) in best)
            result.Add((pair, point));
        result.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return result;
    }
}
