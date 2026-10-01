using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Spawn;
using Vestiges.UI;

namespace Vestiges.Tests;

public partial class RunObservation
{
    /// <summary>Parcours identique pour comparer le radar et la carte ; aucun ennemi ni combat ne déplace le joueur.</summary>
    private async Task CaptureCartography()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy enemy && enemy.IsActive)
                EnemyPool.Instance.Return(enemy);
        _player.SetPhysicsProcess(false);
        _player.AIInputOverride = Vector2.Zero;
        Vector2 origin = _player.GlobalPosition;
        await Frames(90);
        SaveFrame("map-00-radar-start");
        for (int i = 0; i < 24; i++)
        {
            float angle = Mathf.Tau * i / 24f;
            _player.GlobalPosition = origin + new Vector2(Mathf.Cos(angle) * 2100f, Mathf.Sin(angle) * 1200f);
            await Seconds(0.3);
        }
        _player.GlobalPosition = origin;
        _camera.ResetSmoothing();
        await Seconds(0.5);
        SaveFrame("map-01-radar-explored");
        Input.ActionPress("show_map");
        await Seconds(0.5);
        SaveFrame("map-02-full");
        // Zone connue devenue dangereuse : le fond doit se mettre à jour sans découvrir les cases voisines.
        Minimap map = _world.GetNode<Minimap>("HUD/HudRoot/Minimap");
        MethodInfo onPhase = typeof(Minimap).GetMethod("OnZonePhaseChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int x = -8; x <= 8; x++)
            for (int y = -4; y <= 4; y++)
                onPhase.Invoke(map, new object[] { x, y, 3 });
        await Seconds(0.5);
        SaveFrame("map-03-erasure");
        Input.ActionRelease("show_map");
        await Seconds(0.5);
        SaveFrame("map-04-radar-return");
        onPhase.Invoke(map, new object[] { 0, 0, 4 });
        await Seconds(0.3);
        SaveFrame("map-05-known-void");
        // La même découverte au bord du rectangle n'invente pas de terrain hors du monde elliptique.
        _player.GlobalPosition = new Vector2(_world.WorldBounds.End.X - 128f, 0f);
        _camera.ResetSmoothing();
        await Seconds(0.5);
        SaveFrame("map-06-edge");
        _player.GlobalPosition = origin;
        _camera.ResetSmoothing();
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Inherit;
        await Seconds(3);
        Input.ActionPress("show_map");
        await Seconds(0.3);
        SaveFrame("map-07-full-combat");
        Input.ActionRelease("show_map");
        GD.Print($"[RunObservation] RESULT cartography output={_output}");
    }
}
