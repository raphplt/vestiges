using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Events;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --capture-endgame (plan 03 lot E) : l'Indicible est forcé à côté du joueur, qui le combat avec un build à distance.
/// Captures pendant le combat ; s'il vit encore après --seconds, ses PV sont vidés pour dérouler la mort et le passage
/// en endgame. Ligne RESULT : PV perdus au combat, mort, phase de run, tempo des crises.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureEndgame(double fightSeconds)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        await Frames(60);
        foreach (string weapon in new[] { "crossbow", "throwing_axes", "sling" })
            _player.AddWeapon(WeaponDataLoader.Get(weapon));
        EndgameManager endgame = _world.GetNode<EndgameManager>("EndgameManager");
        typeof(EndgameManager).GetMethod("SpawnIndicible", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(endgame, null);
        Indicible boss = _world.GetNode<Indicible>("IndicibleBoss");
        FieldInfo hpField = typeof(Indicible).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance);
        float startHp = (float)hpField.GetValue(boss);
        int hits = 0;
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        EventBus.PlayerHitByEventHandler onHit = (source, _) => { if (source == "indicible") hits++; };
        eventBus.PlayerHitBy += onHit;

        double elapsed = 0;
        int shot = 0;
        while (elapsed < fightSeconds && endgame.IsBossSpawned)
        {
            // Le bot tourne autour du point d'arrivée : il esquive (ou non) les tentacules annoncés.
            _player.AIInputOverride = Vector2.FromAngle((float)elapsed * 0.9f);
            await Seconds(0.5);
            elapsed += 0.5;
            if (GetTree().Paused)
                AutoPickLevelUp();
            if (elapsed >= shot * 5.0)
                SaveFrame($"endgame-{shot++:00}");
        }
        float hpAfterFight = IsInstanceValid(boss) ? (float)hpField.GetValue(boss) : 0f;
        if (endgame.IsBossSpawned && IsInstanceValid(boss))
            boss.TakeDamage(hpAfterFight + 1f);
        await Seconds(1.5);
        SaveFrame("endgame-after");
        eventBus.PlayerHitBy -= onHit;
        GameManager manager = GetNode<GameManager>("/root/GameManager");
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT endgame hp={startHp:F0}->{hpAfterFight:F0} fight_s={elapsed:F0} player_hits={hits} defeated={endgame.IsBossDefeated} endgame={endgame.IsEndgameReached} phase={manager.CurrentRunPhase} boss_freed={!IsInstanceValid(boss) || boss.IsQueuedForDeletion()}"));
    }
}
