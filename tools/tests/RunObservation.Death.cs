using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --capture-death : bilan de fin de run (plan 02 lot D). Le bot joue --seconds secondes pour se constituer un build,
/// puis meurt pour de bon ; captures pendant le blanchiment, les trois temps de la révélation et l'écran final.
/// </summary>
public partial class RunObservation
{
    private static readonly string[] PassiveIds = { "flamme_interieure", "memoire_vive", "instinct" };

    private async Task CaptureDeath(double playSeconds)
    {
        await Seconds(playSeconds);
        // Build fourni pour juger la mise en page d'une run riche : quatre armes, niveaux et raretés variés, souvenirs.
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"), "rare");
        _player.AddWeapon(WeaponDataLoader.Get("makeshift_bow"), "epic");
        _player.AddWeapon(WeaponDataLoader.Get("chain_of_names"));
        for (int i = 0; i < 3; i++)
            _player.UpgradeWeaponFragmentLevel("heavy_hammer");
        foreach (string passive in PassiveIds)
            _player.AddOrUpgradePassive(passive);
        _player.AddOrUpgradePassive(PassiveIds[0]);
        _player.IsGodMode = false;
        _player.TakeDamage(100000f);
        double[] moments = { 1.0, 2.1, 2.6, 3.2, 4.5 };
        double elapsed = 0.0;
        for (int shot = 0; shot < moments.Length; shot++)
        {
            await Seconds(moments[shot] - elapsed);
            elapsed = moments[shot];
            Save($"death-{shot}.png");
        }
        GD.Print("[RunObservation] RESULT death captured");
    }
}
