using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --capture-perks : Prévoyance et Reprise sur la barre de PV (plan 05, B2). Réserve pleine, coup absorbé en partie
/// par la réserve, part récupérable ouverte puis dans sa dernière seconde. Puis Débordement : l'arc surpuissant
/// achève des rôdeurs, la case d'arme s'éclaire et le coup suivant affiche son chiffre renforcé.
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
        GD.Print($"[Perks] RESULT impacts renforcés : {consumed}");
    }
}
