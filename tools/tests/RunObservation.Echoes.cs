using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-echoes : échos de l'oubli (plan 16 O6). Mémoire Fragile imposée autour du joueur, trois échos forcés ;
/// pour chacun, une capture apparu (zoom ×2 centré entre le joueur et l'écho), puis le joueur s'approche :
/// capture pendant la dissolution et au murmure.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureEchoes()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        erasure.ProcessMode = ProcessModeEnum.Disabled;
        await Frames(90);
        _player.AIInputOverride = Vector2.Zero;

        ErasureEchoes echoes = _world.GetNodeOrNull<ErasureEchoes>("ErasureEchoes");
        if (echoes == null)
        {
            GD.PushError("[RunObservation] ErasureEchoes absent de la scène");
            return;
        }

        Vector2I center = new(Mathf.FloorToInt(_player.GlobalPosition.X / erasure.CellSize),
                              Mathf.FloorToInt(_player.GlobalPosition.Y / erasure.CellSize));
        const int radius = 12;
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                erasure.OverrideMemory(center + new Vector2I(x, y), 0.62f);
        erasure.RefreshGroundMemory();

        Vector2 initialZoom = _camera.Zoom;
        Vector2 home = _player.GlobalPosition;
        int shown = 0;
        for (int attempt = 0; attempt < 6 && shown < 3; attempt++)
        {
            _player.GlobalPosition = home;
            await Frames(5);
            if (!echoes.SpawnNow())
                continue;
            shown++;
            Vector2 echo = echoes.GlobalPosition;
            _camera.Zoom = initialZoom * 2f;
            _camera.GlobalPosition = (home + echo) * 0.5f;
            await Seconds(2.2);
            Save($"echo-{shown}-apparu.png");

            // Le joueur s'approche : dissolution puis murmure.
            _player.GlobalPosition = echoes.GlobalPosition + new Vector2(40f, 10f);
            await Seconds(0.3);
            Save($"echo-{shown}-dissolution.png");
            await Seconds(0.9);
            Save($"echo-{shown}-murmure.png");
            GD.Print($"[RunObservation] écho {shown} en {echo}");
            await Seconds(3.5);
        }
        _camera.Zoom = initialZoom;
        GD.Print($"[RunObservation] RESULT echoes shown={shown}");
    }

    private void Save(string name)
    {
        using Image image = GetViewport().GetTexture().GetImage();
        image.SavePng($"{_output}/{name}");
    }

    private async Task Seconds(double seconds)
    {
        ulong until = Time.GetTicksMsec() + (ulong)(seconds * 1000.0);
        while (Time.GetTicksMsec() < until)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
