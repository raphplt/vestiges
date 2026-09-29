using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --capture-perks : Prévoyance et Reprise sur la barre de PV (plan 05, B2). Réserve pleine, coup absorbé en partie
/// par la réserve, part récupérable ouverte puis dans sa dernière seconde, crédit par élimination.
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
        GD.Print($"[Perks] RESULT fenêtre ouverte après 4,3 s : {_player.SpecializationRuntime.Rally.IsOpen}");
    }
}
