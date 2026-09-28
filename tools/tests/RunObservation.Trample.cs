using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-trample : herbes qui plient (plan 10 lot E). Le joueur se place loin d'un petit décor traversable, puis à
/// sa gauche et à sa droite, puis une créature à sa gauche, le joueur à l'écart ; captures au zoom ×3 sur la touffe.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureTrample()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        await Frames(90);
        _player.AIInputOverride = Vector2.Zero;
        EnvironmentProp tuft = null;
        float best = float.MaxValue;
        foreach (Node node in _world.GetNode("PropContainer").FindChildren("*", "", true, false))
        {
            if (node is not EnvironmentProp prop || !Tramples(prop))
                continue;
            float distance = prop.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
            if (distance < best)
            {
                best = distance;
                tuft = prop;
            }
        }
        if (tuft == null)
        {
            GD.Print("[RunObservation] RESULT trample tufts=0");
            return;
        }
        Vector2 initialZoom = _camera.Zoom;
        _camera.Zoom = initialZoom * 3f;
        (string Name, Vector2 Offset)[] poses = { ("trample-0-far.png", new(0f, 90f)), ("trample-1-left.png", new(-12f, 4f)), ("trample-2-right.png", new(12f, 4f)) };
        foreach ((string name, Vector2 offset) in poses)
        {
            _player.GlobalPosition = tuft.GlobalPosition + offset;
            _camera.ResetSmoothing();
            await Frames(12);
            Save(name);
        }
        // Une créature passe à gauche de la touffe, le joueur à l'écart : l'herbe plie pour elle aussi.
        _player.GlobalPosition = tuft.GlobalPosition + new Vector2(0f, 50f);
        _world.GetNode<Vestiges.Spawn.SpawnManager>("SpawnManager").ForceSpawnEnemy("shade", tuft.GlobalPosition + new Vector2(-12f, 4f));
        Node2D creature = null;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy { IsActive: true } enemy && enemy.GlobalPosition.DistanceTo(tuft.GlobalPosition) < 40f)
                creature = enemy;
        for (int frame = 0; frame < 12 && creature != null; frame++)
        {
            creature.GlobalPosition = tuft.GlobalPosition + new Vector2(-12f, 4f);
            await Frames(1);
        }
        Save("trample-3-creature.png");
        _camera.Zoom = initialZoom;
        GD.Print($"[RunObservation] RESULT trample tuft={tuft.GlobalPosition} creature={creature != null}");
    }

    private static bool Tramples(EnvironmentProp prop)
    {
        foreach (Node child in prop.GetChildren())
            if (child is Sprite2D { Material: ShaderMaterial { Shader: { } shader } } && shader.ResourcePath.EndsWith("prop_trample.gdshader"))
                return true;
        return false;
    }
}
