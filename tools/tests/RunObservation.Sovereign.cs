using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Events;

namespace Vestiges.Tests;

/// <summary>
/// --capture-sovereign : un Souverain forcé face à un joueur désarmé, immobile et invincible ; captures à son arrivée
/// puis 100 s plus tard, après la fin de sa chasse (90 s). Il ne disparaît pas (DECISIONS §157) mais ne bloque pas la
/// suite (§165) : la chasse est close et un autre événement peut partir. RESULT : présence du Souverain avant et après,
/// même créature, PV restants, chasse close, événement suivant lancé.
/// </summary>
public partial class RunObservation
{
    private const double SovereignWaitSeconds = 100.0;

    private async Task CaptureSovereign()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        ProcessMode = ProcessModeEnum.Always;
        while (_player.WeaponSlots.Count > 0)
            _player.RemoveWeapon(0);
        await ToSignal(GetTree().CreateTimer(3.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);

        RunEventDirector director = _world.GetNode<RunEventDirector>("RunEventDirector");
        bool started = director.ForceStart("hunt");
        await Frames(30);
        Enemy champion = FindSovereign();
        SaveFrame("sovereign-0-arrival");
        await ToSignal(GetTree().CreateTimer(SovereignWaitSeconds), SceneTreeTimer.SignalName.Timeout);
        Enemy after = FindSovereign();
        SaveFrame("sovereign-1-after-100s");
        bool huntClosed = !director.IsEventActive;
        bool nextStarted = director.ForceStart("stampede");
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT sovereign started={started} present_start={champion != null} present_after={after != null} same={champion != null && champion == after} hp={after?.CurrentHp ?? 0f:F0}/{after?.MaxHp ?? 0f:F0} hunt_closed={huntClosed} next_started={nextStarted} dossier={_output}"));
    }

    private Enemy FindSovereign()
    {
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy { IsActive: true, IsDying: false, VariantId: "champion" } enemy)
                return enemy;
        return null;
    }
}
