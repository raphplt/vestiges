using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// --capture-paths : chemins de terre (plan 10 T3). Pour chaque biome, le point de chemin le plus proche du départ,
/// avec les décors puis sol seul (zoom ×2) ; un raccord à une rue de la ville ; une vue dézoomée du départ.
/// </summary>
public partial class RunObservation
{
    private async Task CapturePaths()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        await Frames(90);

        Node2D paths = _world.GetNodeOrNull<Node2D>("Paths");
        if (paths == null)
        {
            GD.PushError("[RunObservation] aucun chemin dans la scène");
            return;
        }

        _player.AIInputOverride = Vector2.Zero;
        Vector2 initialZoom = _camera.Zoom;
        List<(string Name, Vector2 Point)> spots = FindPathSpots(paths);
        GD.Print($"[RunObservation] RESULT paths strokes={paths.GetChildCount()} spots={spots.Count}");
        Node2D props = _world.GetNode<Node2D>("PropContainer");
        Node2D decals = _world.GetNodeOrNull<Node2D>("GroundDecals");
        foreach ((string name, Vector2 point) in spots)
        {
            _player.GlobalPosition = point;
            _camera.Zoom = initialZoom;
            _camera.ResetSmoothing();
            await Frames(20);
            using (Image image = GetViewport().GetTexture().GetImage())
                image.SavePng($"{_output}/path-{name}.png");

            props.Visible = false;
            if (decals != null)
                decals.Visible = false;
            _camera.Zoom = initialZoom * 2f;
            await Frames(10);
            using (Image ground = GetViewport().GetTexture().GetImage())
                ground.SavePng($"{_output}/path-{name}-sol.png");
            props.Visible = true;
            if (decals != null)
                decals.Visible = true;
            GD.Print($"[RunObservation] chemin {name} en {point}");
        }

        _player.GlobalPosition = Vector2.Zero;
        _camera.Zoom = initialZoom * 0.3f;
        _camera.ResetSmoothing();
        await Frames(30);
        using (Image overview = GetViewport().GetTexture().GetImage())
            overview.SavePng($"{_output}/path-overview.png");
        _camera.Zoom = initialZoom;
    }

    /// <summary>Par biome, le sommet de chemin le plus proche du départ (hors du départ lui-même) ; plus le premier raccord en ville.</summary>
    private List<(string, Vector2)> FindPathSpots(Node2D paths)
    {
        Dictionary<string, (Vector2 Point, float Distance)> best = new();
        foreach (Node child in paths.GetChildren())
        {
            if (child is not MeshInstance2D { Mesh: ArrayMesh mesh } || mesh.GetSurfaceCount() == 0)
                continue;
            Vector2[] vertices = mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector2Array();
            for (int i = 0; i + 1 < vertices.Length; i += 2)
            {
                Vector2 middle = (vertices[i] + vertices[i + 1]) * 0.5f;
                float distance = middle.LengthSquared();
                if (distance < 400f * 400f)
                    continue;
                string biome = _world.GetBiomeAt(middle)?.Id;
                if (biome == null)
                    continue;
                bool atEnd = i == 0 || i + 2 >= vertices.Length;
                string key = atEnd && biome == "urban_ruins" ? "raccord-ville" : biome;
                if (!best.TryGetValue(key, out (Vector2 Point, float Distance) current) || distance < current.Distance)
                    best[key] = (middle, distance);
            }
        }

        List<(string, Vector2)> result = new();
        foreach ((string name, (Vector2 point, float _)) in best)
            result.Add((name, point));
        result.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return result;
    }
}
