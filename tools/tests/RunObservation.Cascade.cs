using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// --capture-cascade : réserve de niveaux (plan 20 §6.7, R1-G). Cinq niveaux gagnés d'un coup s'enchaînent dans un
/// écran qui reste ouvert, avec le compteur ; un niveau reçu juste après la fermeture est retenu, un niveau isolé
/// ouvre l'écran tout de suite. Une capture par choix, et une ligne PASS/FAIL par règle.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureCascade()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        ProcessMode = ProcessModeEnum.Always;
        await Frames(90);

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        PlayerProgression progression = _player.GetNode<PlayerProgression>("PlayerProgression");
        FragmentManager fragments = _world.GetNode<FragmentManager>("FragmentManager");
        Node screen = _world.GetNode("LevelUpScreen");
        MethodInfo activate = screen.GetType().GetMethod("Activate", BindingFlags.NonPublic | BindingFlags.Instance);
        int failures = 0;
        void Check(bool ok, string rule)
        {
            if (!ok)
                failures++;
            GD.Print($"[Cascade] {(ok ? "PASS" : "FAIL")} {rule}");
        }
        bool ScreenOpen() => screen.Get("visible").AsBool();

        eventBus.EmitSignal(EventBus.SignalName.XpGained, XpForLevels(progression, 5));
        await Frames(20);
        Check(ScreenOpen() && fragments.QueuedLevels == 4, $"cinq niveaux d'un coup : écran ouvert, 4 en attente ({fragments.QueuedLevels})");
        for (int choice = 0; choice < 5; choice++)
        {
            SaveFrame($"cascade-{choice}");
            activate.Invoke(screen, new object[] { 0 });
            await Frames(6);
            if (choice < 4)
                Check(ScreenOpen() && GetTree().Paused, $"choix {choice + 1} : l'écran reste ouvert, jeu en pause");
        }
        Check(!ScreenOpen() && !GetTree().Paused, "file vide : écran fermé, jeu repris");

        eventBus.EmitSignal(EventBus.SignalName.XpGained, XpForLevels(progression, 1));
        await Frames(6);
        Check(!ScreenOpen(), "niveau reçu juste après la fermeture : retenu");
        LevelReserveConfig reserve = LevelReserveConfig.Load();
        await Seconds(reserve.HoldSeconds + 0.3f);
        Check(ScreenOpen(), "après la retenue : l'écran s'ouvre");
        SaveFrame("cascade-held");
        activate.Invoke(screen, new object[] { 0 });
        await Seconds(reserve.WindowSeconds + 0.5f);

        eventBus.EmitSignal(EventBus.SignalName.XpGained, XpForLevels(progression, 1));
        await Frames(6);
        Check(ScreenOpen(), "niveau isolé : l'écran s'ouvre tout de suite");
        activate.Invoke(screen, new object[] { 0 });
        await Frames(6);
        GD.Print($"[Cascade] RESULT failures={failures} ; captures dans {_output}");
    }

    /// <summary>Attente en temps de jeu : la fenêtre de capture tourne bien au-delà de 60 images par seconde.</summary>
    private async Task Seconds(float seconds) =>
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    /// <summary>XP exacte pour gagner <paramref name="levels"/> niveaux depuis l'état courant, avec une marge d'un point.</summary>
    private static float XpForLevels(PlayerProgression progression, int levels)
    {
        XpCurveConfig curve = XpCurveConfig.Load();
        float xp = -progression.CurrentXp + 1f;
        for (int i = 0; i < levels; i++)
            xp += curve.CostOf(progression.CurrentLevel + i);
        return xp;
    }
}
