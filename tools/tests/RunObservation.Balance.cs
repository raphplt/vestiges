using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Relevés d'équilibrage pendant --density (DECISIONS §50) : à chaque écran de niveau, la Chance du joueur, les crans
/// de montée de rareté (Chance, oubli de la zone, Péril) et la rareté des améliorations offertes, par tranche de 5 min.
/// </summary>
public partial class RunObservation
{
    private const double BalanceBandSeconds = 300.0;

    private readonly Dictionary<int, Dictionary<string, int>> _offeredRarities = new();
    private readonly Dictionary<int, (double Luck, double Steps, int Count)> _offerContext = new();
    private double _balanceTime;

    private void RecordOffer(FragmentManager fragments)
    {
        int band = (int)(_balanceTime / BalanceBandSeconds);
        if (!_offeredRarities.TryGetValue(band, out Dictionary<string, int> rarities))
            _offeredRarities[band] = rarities = new Dictionary<string, int>();
        foreach (FragmentOption option in fragments.PendingChoices)
            if (option.Rarity != null)
                rarities[option.Rarity.Id] = rarities.GetValueOrDefault(option.Rarity.Id) + 1;

        ErasureManager erasure = _world.GetNodeOrNull<ErasureManager>("ErasureManager");
        ErasureManager.ErasureZonePhase phase = erasure?.GetZonePhaseAt(_player.GlobalPosition) ?? ErasureManager.ErasureZonePhase.Anchored;
        int peril = _world.GetNodeOrNull<PerilManager>("PerilManager")?.Peril ?? 0;
        float steps = UpgradeRoller.BumpSteps(_player.LuckBonus, phase, peril);
        (double luck, double total, int count) = _offerContext.GetValueOrDefault(band);
        _offerContext[band] = (luck + _player.LuckBonus, total + steps, count + 1);
    }

    private void AppendBalance(StringBuilder summary)
    {
        List<int> bands = new(_offeredRarities.Keys);
        bands.Sort();
        foreach (int band in bands)
        {
            Dictionary<string, int> rarities = _offeredRarities[band];
            int total = 0;
            foreach (int count in rarities.Values)
                total += count;
            (double luck, double steps, int offers) = _offerContext.GetValueOrDefault(band);
            summary.Append(CultureInfo.InvariantCulture,
                $" | offers_{band * 5}-{band * 5 + 5}min n={total} luck={(offers > 0 ? luck / offers : 0):F2} steps={(offers > 0 ? steps / offers : 0):F2}");
            foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
                summary.Append(CultureInfo.InvariantCulture,
                    $" {rarity.Id}={(total > 0 ? 100.0 * rarities.GetValueOrDefault(rarity.Id) / total : 0):F0}%");
        }
    }
}
