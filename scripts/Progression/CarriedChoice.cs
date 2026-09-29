using System.Collections.Generic;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.Progression;

/// <summary>
/// Seconde lecture (plan 05 §3.8) : après une sélection ordinaire, la plus rare des améliorations d'arme non choisies
/// (la plus à gauche à égalité) revient telle quelle au prochain choix ordinaire, une seule fois. Elle n'est jamais
/// reportée deux fois ; passer l'offre l'efface ; une carte devenue impossible est libérée.
/// </summary>
public sealed class CarriedChoice
{
    public const string UpgradeType = "weapon_upgrade";

    private FragmentOption _carried;

    public FragmentOption Pending => _carried;

    /// <summary>Carte reportée encore applicable à l'offre qui s'ouvre, ou null (elle est alors libérée).</summary>
    public FragmentOption Validate(Player player, IReadOnlySet<string> banishedIds)
    {
        if (_carried == null)
            return null;
        bool applicable = false;
        foreach (WeaponInstance weapon in player.WeaponSlots)
            applicable |= weapon.Id == _carried.Id && weapon.CanLevelUp;
        if (applicable && !banishedIds.Contains(_carried.Id))
            return _carried;
        _carried = null;
        return null;
    }

    /// <summary>Une sélection ordinaire consomme le report présenté et prépare le suivant parmi les cartes neuves.</summary>
    public void Resolve(IReadOnlyList<FragmentOption> offered, FragmentOption selected, bool active)
    {
        _carried = null;
        if (!active)
            return;
        FragmentOption best = null;
        foreach (FragmentOption option in offered)
        {
            if (option == selected || option.IsCarried || option.Type != UpgradeType || option.Rarity == null)
                continue;
            if (best == null || option.Rarity.Rank > best.Rarity.Rank)
                best = option;
        }
        _carried = best?.AsCarried();
    }

    public void Clear() => _carried = null;
}
