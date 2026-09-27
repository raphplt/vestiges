using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-landmarks : repères rares des Ruines Urbaines (plan 08 P2), église et pylône. Les décors sont retrouvés
/// par leur texture ; chacun est capturé au zoom normal, joueur dans la rue au sud, puis dézoomé (×0,6).
/// </summary>
public partial class RunObservation
{
    private static readonly string[] LandmarkStems = { "prop_bld_church", "prop_radio_mast" };

    private async Task CaptureLandmarks()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        await Frames(90);
        _player.AIInputOverride = Vector2.Zero;

        List<(string Stem, Vector2 Position)> found = new();
        CollectLandmarks(_world, found);
        GD.Print($"[RunObservation] RESULT landmarks count={found.Count}");
        Vector2 initialZoom = _camera.Zoom;
        for (int i = 0; i < found.Count && i < 6; i++)
        {
            (string stem, Vector2 position) = found[i];
            GD.Print($"[RunObservation] repère {i + 1} : {stem} en {position}");
            _player.GlobalPosition = position + new Vector2(0f, 70f);
            _camera.Zoom = initialZoom;
            _camera.ResetSmoothing();
            await Frames(20);
            Save($"landmark-{i + 1}-{stem}.png");
            _camera.Zoom = initialZoom * 0.6f;
            await Frames(10);
            Save($"landmark-{i + 1}-{stem}-large.png");
        }
        _camera.Zoom = initialZoom;
    }

    private static void CollectLandmarks(Node node, List<(string, Vector2)> found)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is EnvironmentProp prop)
            {
                foreach (Node part in prop.GetChildren())
                {
                    if (part is not Sprite2D { Texture: not null } sprite)
                        continue;
                    string file = sprite.Texture.ResourcePath.GetFile();
                    foreach (string stem in LandmarkStems)
                        if (file.StartsWith(stem))
                            found.Add((file.GetBaseName(), prop.GlobalPosition));
                    break;
                }
                continue;
            }
            CollectLandmarks(child, found);
        }
    }
}
