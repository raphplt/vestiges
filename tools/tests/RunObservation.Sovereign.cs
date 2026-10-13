using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Events;

namespace Vestiges.Tests;

/// <summary>
/// --capture-sovereign : un Souverain forcé face à un joueur désarmé, immobile et invincible ; captures à son arrivée
/// puis 75 s plus tard, au-delà de l'ancienne limite de 60 s (DECISIONS §157 : il ne disparaît plus). RESULT : présence
/// du Souverain avant et après, même créature, PV restants.
/// </summary>
public partial class RunObservation
{
    private const double SovereignWaitSeconds = 75.0;

    private async Task CaptureSovereign()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        ProcessMode = ProcessModeEnum.Always;
        while (_player.WeaponSlots.Count > 0)
            _player.RemoveWeapon(0);
        await ToSignal(GetTree().CreateTimer(3.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);

        bool started = _world.GetNode<RunEventDirector>("RunEventDirector").ForceStart("hunt");
        await Frames(30);
        Enemy champion = FindSovereign();
        SaveFrame("sovereign-0-arrival");
        await ToSignal(GetTree().CreateTimer(SovereignWaitSeconds), SceneTreeTimer.SignalName.Timeout);
        Enemy after = FindSovereign();
        SaveFrame("sovereign-1-after-75s");
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT sovereign started={started} present_start={champion != null} present_after={after != null} same={champion != null && champion == after} hp={after?.CurrentHp ?? 0f:F0}/{after?.MaxHp ?? 0f:F0} dossier={_output}"));
    }

    private Enemy FindSovereign()
    {
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy { IsActive: true, IsDying: false, VariantId: "champion" } enemy)
                return enemy;
        return null;
    }
}
