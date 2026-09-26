using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-erasure : mémoire des zones imposée autour du joueur, une capture par phase de l'oubli
/// (Ancrée, Fragile, Effilochée, Effacée, Néant), puis un dégradé d'ouest en est qui les montre côte à côte.
/// </summary>
public partial class RunObservation
{
    private static readonly (string Name, float Memory)[] ErasurePhases =
    {
        ("ancree", 1f), ("fragile", 0.62f), ("effilochee", 0.38f), ("effacee", 0.14f), ("neant", 0f),
    };

    private async Task CaptureErasure()
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

        Vector2I center = new(Mathf.FloorToInt(_player.GlobalPosition.X / erasure.CellSize),
                              Mathf.FloorToInt(_player.GlobalPosition.Y / erasure.CellSize));
        const int radius = 12;
        foreach ((string name, float memory) in ErasurePhases)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    erasure.OverrideMemory(center + new Vector2I(x, y), memory);
            erasure.RefreshGroundMemory();
            // Le voile d'écran suit la phase en 0,8 s.
            await Frames(60);
            using Image image = GetViewport().GetTexture().GetImage();
            image.SavePng($"{_output}/erasure-{name}.png");
        }

        // Dégradé à l'écran : Fragile à l'ouest, lisière des 25 % près du joueur, Néant à l'est.
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                float memory = Mathf.Clamp(0.3f - x / 12f, 0f, 1f);
                erasure.OverrideMemory(center + new Vector2I(x, y), memory);
            }
        }
        erasure.RefreshGroundMemory();
        await Frames(10);
        using (Image gradient = GetViewport().GetTexture().GetImage())
            gradient.SavePng($"{_output}/erasure-degrade.png");
        GD.Print($"[RunObservation] oubli capturé autour de la zone {center.ToString()} ({ErasurePhases.Length.ToString(CultureInfo.InvariantCulture)} phases + dégradé)");
    }
}
