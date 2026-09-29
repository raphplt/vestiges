using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --capture-perks : Prévoyance et Reprise sur la barre de PV (plan 05, B2). Réserve pleine, coup absorbé en partie
/// par la réserve, part récupérable ouverte puis dans sa dernière seconde. Puis Débordement : l'arc surpuissant
/// achève des rôdeurs, la case d'arme s'éclaire et le coup suivant affiche son chiffre renforcé. Enfin Convergence :
/// une élite plus lointaine que les rôdeurs est visée et entourée de son repère.
/// </summary>
public partial class RunObservation
{
    private async Task CapturePerks()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        await Frames(90);
        _player.AcquireSpecialization(PerkSpecializationDataLoader.Get("overheal_reserve"));
        _player.AcquireSpecialization(PerkSpecializationDataLoader.Get("rally"));
        await Frames(10);
        SaveFrame("perks-0-reserve-full");

        _player.IsGodMode = false;
        _player.DisableDefenseForTests();
        _player.TakeDamage(_player.EffectiveMaxHp * 0.55f);
        _player.IsGodMode = true;
        await Frames(10);
        SaveFrame("perks-1-wounded");
        GD.Print($"[Perks] après le coup : PV {_player.CurrentHp:0.#}, réserve {_player.SpecializationRuntime.Reserve.Stock:0.#}, "
            + $"récupérable {_player.SpecializationRuntime.Rally.Recoverable:0.#}");

        await Seconds(2.8f);
        SaveFrame("perks-2-rally-ending");
        await Seconds(1.5f);
        SaveFrame("perks-3-rally-expired");
        GD.Print($"[Perks] fenêtre ouverte après 4,3 s : {_player.SpecializationRuntime.Rally.IsOpen}");

        _player.AcquireSpecialization(PerkSpecializationDataLoader.Get("overflow"));
        _player.ApplyPerkModifier("damage", 6f, "multiplicative");
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Vector2 origin = _player.GlobalPosition;
        int consumed = 0;
        GetNode<EventBus>("/root/EventBus").EnemyDamageResolved += result => consumed += result.CarriedDamage > 0f ? 1 : 0;
        for (int wave = 0; wave < 4; wave++)
        {
            for (int index = 0; index < 5; index++)
                spawner.ForceSpawnEnemy("rodeur", origin + Vector2.FromAngle(index * Mathf.Tau / 5f + wave) * (90f + index * 12f));
            for (int shot = 0; shot < 4; shot++)
            {
                await Frames(8);
                SavePlayerCloseUp($"{_output}/perks-overflow-{wave}-{shot}.png", new Vector2(220f, 140f));
            }
            SaveFrame($"perks-overflow-hud-{wave}");
        }
        GD.Print($"[Perks] impacts renforcés : {consumed}");

        // Convergence : une élite au loin parmi des rôdeurs proches, l'arc la vise et le repère l'entoure.
        _player.AcquireSpecialization(PerkSpecializationDataLoader.Get("priority_targeting"));
        // Arc inoffensif : aucune élimination, donc aucun choix de niveau ne vient masquer la scène.
        _player.ApplyPerkModifier("damage", 1e-4f, "multiplicative");
        Enemy elite = spawner.SpawnEventEnemy("rodeur", _player.GlobalPosition + new Vector2(-170f, 40f), "elite");
        for (int index = 0; index < 3; index++)
            spawner.ForceSpawnEnemy("rodeur", _player.GlobalPosition + Vector2.FromAngle(index * 0.9f) * 70f);
        for (int shot = 0; shot < 3; shot++)
        {
            await Frames(20);
            await SkipLevelChoices();
            SavePlayerCloseUp($"{_output}/perks-priority-{shot}.png", new Vector2(240f, 150f));
        }
        Node marker = _player.SpecializationRuntime.GetNode("PriorityTargetMarker");
        GD.Print($"[Perks] RESULT repère sur l'élite : {marker.Get("visible").AsBool() && IsInstanceValid(elite)}");
    }

    /// <summary>Les éliminations précédentes ont pu ouvrir des choix de niveau : ils sont passés pour laisser voir le monde.</summary>
    private async Task SkipLevelChoices()
    {
        Node screen = _world.GetNode("LevelUpScreen");
        System.Reflection.MethodInfo skip = screen.GetType().GetMethod("Skip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        for (int guard = 0; guard < 40 && screen.Get("visible").AsBool(); guard++)
        {
            skip.Invoke(screen, null);
            await Frames(2);
        }
    }
}
