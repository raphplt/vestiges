using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --capture-levelup-fx : temps fort de la montée de niveau (plan 02 J4). L'effet seul d'abord, au ralenti (×0,25) au milieu
/// d'un cercle de créatures, pour voir onde, colonne et poussée ; puis une vraie montée de niveau par XP, pour l'entrée
/// de l'écran de choix.
/// </summary>
public partial class RunObservation
{
    private static readonly int[] LevelUpFxCaptureFrames = { 1, 3, 6, 10, 15 };

    private async Task CaptureLevelUpFx()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        await Frames(90);
        _player.AIInputOverride = Vector2.Zero;
        Vector2 origin = _player.GlobalPosition;
        for (int index = 0; index < 6; index++)
            spawner.ForceSpawnEnemy("rodeur", origin + Vector2.FromAngle(index * Mathf.Tau / 6f) * (50f + index % 2 * 40f));
        await Frames(2);

        Engine.TimeScale = 0.25;
        try
        {
            LevelUpFx.Play(_player, GetNode<GroupCache>("/root/GroupCache"));
            int elapsed = 0;
            for (int shot = 0; shot < LevelUpFxCaptureFrames.Length; shot++)
            {
                await Frames(LevelUpFxCaptureFrames[shot] - elapsed);
                elapsed = LevelUpFxCaptureFrames[shot];
                SavePlayerCloseUp($"{_output}/levelup-fx-{shot}.png", new Vector2(160f, 110f));
            }
        }
        finally
        {
            Engine.TimeScale = 1.0;
        }

        // Vraie montée de niveau : l'écran de choix met en pause, son entrée se joue quand même.
        GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.XpGained, 10000f);
        for (int shot = 0; shot < 4; shot++)
        {
            await Frames(shot == 0 ? 1 : 3);
            Save($"levelup-screen-{shot}.png");
        }
        GD.Print("[RunObservation] RESULT levelup captured");
    }
}
