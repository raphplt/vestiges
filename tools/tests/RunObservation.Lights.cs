using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-lights : fausses lumières (plan 39 B4a). La lampe allumée la plus proche du départ pour chaque sorte, en
/// plein jour (normal et ×2), puis sous l'orage, fenêtres éclairées comprises (B4b) ; enfin une lampe dont la zone
/// s'oublie, qui vacille puis meurt.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureLights()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        await Frames(300);
        _player.AIInputOverride = Vector2.Zero;
        FalseLightDirector lights = _world.GetNode<FalseLightDirector>("FalseLightDirector");

        Dictionary<string, Vector2> nearest = new();
        foreach ((string sprite, Vector2 position) in lights.LivingLights())
            if (!nearest.TryGetValue(sprite, out Vector2 best) || position.LengthSquared() < best.LengthSquared())
                nearest[sprite] = position;
        GD.Print($"[RunObservation] RESULT lights allumées={lights.LitCount} sortes={nearest.Count}");

        Vector2 initialZoom = _camera.Zoom;
        await ShootLights(nearest, initialZoom, "jour");
        _world.GetNode<WeatherDirector>("WeatherDirector").ForceStart("storm");
        await Frames(600);
        GD.Print($"[RunObservation] RESULT lights orage obscurité={lights.Darkness:0.00}");
        await ShootLights(nearest, initialZoom, "orage");

        // Zone qui s'oublie : la lampe vacille, puis meurt sous le seuil.
        if (nearest.Count > 0)
        {
            ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
            Dictionary<Vector2I, float> memory = (Dictionary<Vector2I, float>)typeof(ErasureManager)
                .GetField("_zoneMemory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(erasure);
            // Une lampe, pas une fenêtre : la position rendue pour une fenêtre est le pied de l'immeuble, dans une autre zone.
            Vector2 target = default;
            foreach ((string sprite, Vector2 position) in nearest)
            {
                if (sprite.StartsWith(Vestiges.Infrastructure.FalseLightsConfig.WindowSpritePrefix, System.StringComparison.Ordinal))
                    continue;
                target = position;
                break;
            }
            Vector2I cell = new(Mathf.FloorToInt(target.X / erasure.CellSize), Mathf.FloorToInt(target.Y / erasure.CellSize));
            _player.GlobalPosition = target + new Vector2(-60f, 50f);
            _camera.ResetSmoothing();
            memory[cell] = 0.4f;
            await Frames(60);
            int died = lights.DiedCount;
            for (int i = 0; i < 4; i++)
            {
                _camera.Zoom = initialZoom * 2f;
                await Frames(7);
                Save($"lights-oubli-{i + 1}.png");
            }
            memory[cell] = 0.1f;
            await Frames(90);
            _camera.Zoom = initialZoom * 2f;
            await Frames(10);
            Save("lights-morte.png");
            GD.Print($"[RunObservation] RESULT lights oubli mortes={lights.DiedCount - died}");
        }
        _camera.Zoom = initialZoom;
    }

    private async Task ShootLights(Dictionary<string, Vector2> nearest, Vector2 initialZoom, string label)
    {
        foreach ((string sprite, Vector2 position) in nearest)
        {
            _player.GlobalPosition = position + new Vector2(-60f, 50f);
            _camera.Zoom = initialZoom;
            _camera.ResetSmoothing();
            await Frames(30);
            Save($"lights-{label}-{sprite}.png");
            _camera.Zoom = initialZoom * 2f;
            await Frames(10);
            Save($"lights-{label}-{sprite}-zoom.png");
            GD.Print($"[RunObservation] lampe {sprite} ({label}) en {position}");
        }
    }
}
