using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-micro : micro-interactions (plan 02 J6). Le joueur s'arrête près du coffre le plus proche (frémissement,
/// trois instants), puis marche en ligne droite (poussière de pas), enfin se tient près d'un point d'intérêt (reflets),
/// capturé en gros plan.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureMicro()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        await Frames(90);
        _player.AIInputOverride = Vector2.Zero;

        Chest nearest = null;
        float best = float.MaxValue;
        foreach (Node node in GetTree().GetNodesInGroup("chests"))
        {
            if (node is not Chest chest || chest.IsOpened)
                continue;
            float distance = chest.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
            if (distance < best)
            {
                best = distance;
                nearest = chest;
            }
        }
        GD.Print($"[RunObservation] RESULT micro chest={(nearest != null ? nearest.ChestId : "none")}");
        if (nearest != null)
        {
            _player.GlobalPosition = nearest.GlobalPosition + new Vector2(-40f, 20f);
            _camera.ResetSmoothing();
            float maxTilt = 0f;
            for (int shot = 0; shot < 3; shot++)
            {
                for (int frame = 0; frame < 4; frame++)
                {
                    await Frames(1);
                    foreach (Node child in nearest.GetChildren())
                        if (child is Node2D { Visible: true } shown && child is Sprite2D or Polygon2D)
                            maxTilt = Mathf.Max(maxTilt, Mathf.Abs(shown.Rotation));
                }
                SavePlayerCloseUp($"{_output}/micro-chest-{shot}.png", new Vector2(110f, 70f));
            }
            GD.Print($"[RunObservation] RESULT micro chest tilt max={Mathf.RadToDeg(maxTilt):F2}° à {nearest.GlobalPosition.DistanceTo(_player.GlobalPosition):F0} px");
        }

        _player.AIInputOverride = Vector2.Right;
        for (int shot = 0; shot < 4; shot++)
        {
            await Frames(4);
            SavePlayerCloseUp($"{_output}/micro-steps-{shot}.png", new Vector2(110f, 70f));
        }
        _player.AIInputOverride = Vector2.Zero;

        // Reflets : le point d'intérêt inexploré le plus proche, observé quelques secondes.
        PointOfInterest poi = null;
        best = float.MaxValue;
        foreach (Node node in GetTree().GetNodesInGroup("pois"))
        {
            if (node is not PointOfInterest candidate || candidate.IsExplored)
                continue;
            float distance = candidate.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
            if (distance < best)
            {
                best = distance;
                poi = candidate;
            }
        }
        if (poi == null)
            return;
        _player.GlobalPosition = poi.GlobalPosition + new Vector2(70f, 25f);
        _camera.ResetSmoothing();
        for (int shot = 0; shot < 4; shot++)
        {
            await Seconds(0.45);
            SavePlayerCloseUp($"{_output}/micro-poi-{shot}.png", new Vector2(130f, 90f));
        }
    }
}
