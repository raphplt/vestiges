using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// --capture-farms : fermes des Champs Sauvages (plan 08 P4b). Les quatre plus proches du départ, au zoom normal
/// puis dézoomées (×0,6) pour juger la composition avec les champs et le chemin qui les rejoint ; puis les trois
/// scènes-récits les plus proches, au zoom ×2.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureFarms()
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

        List<Vector2> farms = new(_world.FarmAnchors);
        farms.Sort((a, b) => a.LengthSquared().CompareTo(b.LengthSquared()));
        GD.Print($"[RunObservation] RESULT farms count={farms.Count}");
        Vector2 initialZoom = _camera.Zoom;
        for (int i = 0; i < farms.Count && i < 4; i++)
        {
            // Le joueur se tient dans la cour, un peu au sud, pour ne pas masquer la maison.
            _player.GlobalPosition = farms[i] + new Vector2(0f, 60f);
            _camera.Zoom = initialZoom;
            _camera.ResetSmoothing();
            await Frames(20);
            Save($"farm-{i + 1}.png");
            _camera.Zoom = initialZoom * 0.6f;
            await Frames(10);
            Save($"farm-{i + 1}-large.png");
            GD.Print($"[RunObservation] ferme {i + 1} en {farms[i]}");
        }

        // Scènes-récits (P4b-4) : les six plus proches du départ, au zoom ×2.
        List<Vector2> scenes = new(_world.StoryScenes);
        scenes.Sort((a, b) => a.LengthSquared().CompareTo(b.LengthSquared()));
        GD.Print($"[RunObservation] RESULT scenes count={scenes.Count}");
        for (int i = 0; i < scenes.Count && i < 6; i++)
        {
            _player.GlobalPosition = scenes[i] + new Vector2(-40f, 20f);
            _camera.Zoom = initialZoom * 2f;
            _camera.ResetSmoothing();
            await Frames(20);
            Save($"scene-{i + 1}.png");
        }
        _camera.Zoom = initialZoom;
    }
}
