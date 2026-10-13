using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-exodus : scènes de l'exode dans les rues (plan 39 B3). Trois embouteillages et trois barrages, au zoom normal
/// pour juger leur poids dans la rue, puis au zoom ×2 pour lire les décors.
/// --listen-voices : cabines qui sonnent (plan 39 C1). Le joueur s'approche de la cabine qui sonne la plus proche
/// jusqu'à ce qu'elle se taise, puis s'en éloigne ; journal par étape.
/// Avec tools/record_run_audio.sh (RECORD_EXTRA_ARGS=--listen-voices), le son s'écoute dans run.mp4.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureExodus()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        // Le fondu d'entrée de la run assombrit encore l'écran pendant les premières secondes.
        await Frames(300);
        _player.AIInputOverride = Vector2.Zero;

        // Trois embouteillages et trois barrages, les plus proches du départ.
        List<Vector2> jams = new();
        List<Vector2> barricades = new();
        foreach (Vestiges.World.ExodusScenePlacer.Scene scene in _world.ExodusScenes)
            (scene.Jam ? jams : barricades).Add(scene.Position);
        jams.Sort((a, b) => a.LengthSquared().CompareTo(b.LengthSquared()));
        barricades.Sort((a, b) => a.LengthSquared().CompareTo(b.LengthSquared()));
        GD.Print($"[RunObservation] RESULT exodus_scenes jams={jams.Count} barricades={barricades.Count}");
        List<Vector2> scenes = new();
        scenes.AddRange(jams.GetRange(0, Mathf.Min(3, jams.Count)));
        scenes.AddRange(barricades.GetRange(0, Mathf.Min(3, barricades.Count)));
        Vector2 initialZoom = _camera.Zoom;
        for (int i = 0; i < scenes.Count && i < 6; i++)
        {
            // Le joueur se tient à l'écart, au sud-ouest, pour ne masquer ni voitures ni panneau.
            _player.GlobalPosition = scenes[i] + new Vector2(-70f, 50f);
            _camera.Zoom = initialZoom;
            _camera.ResetSmoothing();
            await Frames(20);
            Save($"exodus-{i + 1}.png");
            _camera.Zoom = initialZoom * 2f;
            await Frames(10);
            Save($"exodus-{i + 1}-zoom.png");
            GD.Print($"[RunObservation] scène de l'exode {i + 1} en {scenes[i]}");
        }
        _camera.Zoom = initialZoom;
    }

    private async Task ListenVoices()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        await Frames(300);
        _player.AIInputOverride = Vector2.Zero;
        AbandonedVoicesDirector voices = _world.GetNode<AbandonedVoicesDirector>("AbandonedVoicesDirector");
        GD.Print($"[RunObservation] RESULT voices booths={voices.RingingBooths.Count}");
        if (voices.RingingBooths.Count == 0)
            return;
        Vector2 booth = voices.RingingBooths[0];
        foreach (Vector2 other in voices.RingingBooths)
            if (other.LengthSquared() < booth.LengthSquared())
                booth = other;
        foreach (float distance in new[] { 900f, 600f, 300f, 60f, 300f })
        {
            _player.GlobalPosition = booth + new Vector2(-distance * 0.8f, distance * 0.6f);
            _camera.ResetSmoothing();
            await Frames(480);
            GD.Print($"[RunObservation] RESULT voices cabine à {distance} px : entendues={voices.Heard} cabines tues={voices.PhonesSilenced}");
        }
    }
}
