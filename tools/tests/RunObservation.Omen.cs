using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// --capture-omen : présage d'une Résurgence (plan 03 lot C). La prochaine crise est avancée pour que l'avertissement
/// commence tout de suite ; captures avant, pendant l'avertissement (début, milieu, fin) et pendant la crise.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureOmen()
    {
        // Ni apparitions ni morts : aucune montée de niveau ne vient figer la capture.
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        await Frames(90);
        _player.AIInputOverride = Vector2.Zero;
        Vestiges.Events.CrisisManager crisis = _world.GetNode<Vestiges.Events.CrisisManager>("CrisisManager");
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        float elapsed = (float)typeof(Vestiges.Events.CrisisManager).GetField("_elapsed", flags).GetValue(crisis);
        float warning = crisis.WarningDurationSec;
        typeof(Vestiges.Events.CrisisManager).GetField("_nextCrisisAtSec", flags).SetValue(crisis, elapsed + warning + 1f);
        Save("omen-0-before.png");
        double[] moments = { 1.5, warning * 0.5, warning * 0.95, warning + 3.0 };
        string[] names = { "omen-1-warning-start.png", "omen-2-warning-mid.png", "omen-3-warning-end.png", "omen-4-crisis.png" };
        double waited = 0;
        for (int i = 0; i < moments.Length; i++)
        {
            await Seconds(moments[i] - waited);
            waited = moments[i];
            Save(names[i]);
        }
        GD.Print($"[RunObservation] RESULT omen warning={warning:F0}s");
    }
}
