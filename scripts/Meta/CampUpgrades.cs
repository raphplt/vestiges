using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Meta;

/// <summary>
/// Le Campement en run (plan 36) : les rangs achetés au Hub s'appliquent une fois, au début de la run, après
/// l'initialisation du personnage. Emplacements d'armes, places du sac, relances, bannissements, Essence de départ et
/// stats.
/// </summary>
public static class CampUpgrades
{
    /// <summary>Ce que valent des rangs achetés, sans rien appliquer : la run, l'écran du Campement et les tests le lisent.</summary>
    public readonly record struct Totals(int WeaponSlots, int BagSlots, int Rerolls, int Banishes, int StartEssence);

    /// <summary>
    /// Totaux des capacités pour ces rangs. En mode dev, les emplacements d'armes restent à quatre : bancs et builds de
    /// référence portent souvent quatre armes (plan 36 §9).
    /// </summary>
    public static Totals Compute(IReadOnlyDictionary<string, int> ranks, bool development)
    {
        int slots = CampUpgradeDataLoader.BaseWeaponSlots, bag = InventoryConfig.BagSize, rerolls = 0, banishes = 0, essence = 0;
        foreach (CampUpgradeData upgrade in CampUpgradeDataLoader.All)
        {
            int rank = RankOf(upgrade, ranks);
            int total = Mathf.RoundToInt(upgrade.TotalAt(rank));
            switch (upgrade.Effect)
            {
                case CampEffect.WeaponSlots: slots += total; break;
                case CampEffect.BagSlots: bag += total; break;
                case CampEffect.Rerolls: rerolls += total; break;
                case CampEffect.Banishes: banishes += total; break;
                case CampEffect.StartEssence: essence += total; break;
            }
        }
        if (development)
            slots = Player.MaxWeaponSlots;
        return new Totals(Mathf.Clamp(slots, 1, Player.MaxWeaponSlots), bag, rerolls, banishes, essence);
    }

    /// <summary>Applique les rangs achetés au joueur et aux systèmes de la run ; rend les totaux appliqués.</summary>
    public static Totals Apply(Player player, FragmentManager fragments, EssenceTracker essence,
        IReadOnlyDictionary<string, int> ranks, bool development)
    {
        Totals totals = Compute(ranks, development);
        player.SetWeaponSlotLimit(totals.WeaponSlots);
        player.SetBagCapacity(totals.BagSlots);
        fragments?.AddRerolls(totals.Rerolls);
        fragments?.AddBanishes(totals.Banishes);
        essence?.AddEssence(totals.StartEssence);
        foreach (CampUpgradeData upgrade in CampUpgradeDataLoader.All)
        {
            int rank = RankOf(upgrade, ranks);
            if (upgrade.Effect != CampEffect.Stat || rank == 0)
                continue;
            float total = upgrade.TotalAt(rank);
            player.ApplyPerkModifier(upgrade.Stat, upgrade.IsMultiplicative ? 1f + total : total, upgrade.ModifierType);
        }
        GD.Print($"[CampUpgrades] {totals.WeaponSlots} emplacements d'armes, sac de {totals.BagSlots}, +{totals.Rerolls} relances, "
            + $"+{totals.Banishes} bannissements, +{totals.StartEssence} Essence");
        return totals;
    }

    private static int RankOf(CampUpgradeData upgrade, IReadOnlyDictionary<string, int> ranks)
        => ranks != null && ranks.TryGetValue(upgrade.Id, out int rank) ? Mathf.Clamp(rank, 0, upgrade.MaxRank) : 0;
}
