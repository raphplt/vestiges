using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Calendrier et composition des offres de perks (plan 05, B1). Chaque palier donne un droit ; un niveau sert au plus
/// un droit, à la place du choix ordinaire. Un droit passé ou sans candidat revient au niveau suivant, jamais au même.
/// </summary>
public sealed class PerkSpecializationOffers
{
    public const string OptionType = "specialization";

    private readonly PerkSpecializationConfig _config;
    private readonly HashSet<string> _banished = new();
    private int _earliestLevel;

    public PerkSpecializationOffers(PerkSpecializationConfig config)
    {
        _config = config;
    }

    public int Capacity => _config?.MaxEquipped ?? 0;

    /// <summary>Droits ouverts par les paliers atteints à ce niveau.</summary>
    public int RightsEarned(int level)
    {
        if (_config == null)
            return 0;
        int rights = 0;
        foreach (int offerLevel in _config.OfferLevels)
            if (offerLevel <= level)
                rights++;
        return Mathf.Min(rights, Capacity);
    }

    /// <summary>Ce niveau doit-il servir un droit de perk plutôt qu'un choix ordinaire ?</summary>
    public bool IsDue(int level, Player player) =>
        level >= _earliestLevel && RightsEarned(level) > player.Specializations.Count;

    /// <summary>Le niveau a servi un droit, ou tenté faute de candidat : le prochain essai attend le niveau suivant.</summary>
    public void Served(int level) => _earliestLevel = level + 1;

    /// <summary>Perks proposables maintenant : effet branché, ni acquis ni banni, conditions remplies par l'arsenal.</summary>
    public List<FragmentOption> Candidates(Player player, IReadOnlySet<string> banishedWeapons)
    {
        List<FragmentOption> candidates = new();
        foreach (PerkSpecializationData perk in PerkSpecializationDataLoader.GetAll())
        {
            if (IsAvailable(perk, player) && IsEligible(perk, player, banishedWeapons))
                candidates.Add(new FragmentOption(perk.Id, OptionType, perk.Name, 1));
        }
        return candidates;
    }

    /// <summary>
    /// Tant qu'aucun perk n'est acquis, une place par groupe de familles de la première offre (au moins un défensif,
    /// tiré uniformément pour alterner) ; le reste est tiré parmi tous les candidats. L'ordre affiché est mélangé.
    /// </summary>
    public List<FragmentOption> Pick(List<FragmentOption> candidates, bool firstOffer, RandomNumberGenerator rng)
    {
        List<FragmentOption> remaining = new(candidates);
        List<FragmentOption> picked = new();
        int size = _config?.OfferSize ?? 0;
        if (firstOffer && _config != null)
        {
            foreach (IReadOnlyList<string> families in _config.FirstOfferFamilies)
            {
                if (picked.Count >= size)
                    break;
                List<FragmentOption> matching = remaining.FindAll(option => families.Contains(FamilyOf(option)));
                if (matching.Count > 0)
                    Take(matching[rng.RandiRange(0, matching.Count - 1)], remaining, picked);
            }
        }
        while (picked.Count < size && remaining.Count > 0)
            Take(remaining[rng.RandiRange(0, remaining.Count - 1)], remaining, picked);

        for (int i = picked.Count - 1; i > 0; i--)
        {
            int j = rng.RandiRange(0, i);
            (picked[i], picked[j]) = (picked[j], picked[i]);
        }
        return picked;
    }

    /// <summary>
    /// Bannit un perk pour la run. Refusé s'il manquerait ensuite de perks du catalogue pour remplir les emplacements
    /// restants, ou si l'offre en cours n'aurait plus de carte : l'écran resterait ouvert sans choix.
    /// </summary>
    public bool TryBanish(string id, Player player, IReadOnlySet<string> banishedWeapons)
    {
        int available = 0;
        foreach (PerkSpecializationData perk in PerkSpecializationDataLoader.GetAll())
            if (IsAvailable(perk, player))
                available++;
        if (available <= Capacity - player.Specializations.Count || !_banished.Add(id))
            return false;
        if (Candidates(player, banishedWeapons).Count > 0)
            return true;
        _banished.Remove(id);
        return false;
    }

    /// <summary>Conditions d'offre de la fiche, évaluées sur l'arsenal courant (armes bannies non améliorables).</summary>
    public static bool IsEligible(PerkSpecializationData perk, Player player, IReadOnlySet<string> banishedWeapons)
    {
        PerkSpecializationEligibility conditions = perk.Eligibility;
        // Aucune récompense d'objets à choix n'existe en run avant le catalogue d'objets (B4).
        if (conditions.ItemChoiceRewards || conditions.OwnedItem)
            return false;

        bool targeting = false;
        bool directHits = false;
        bool nativeControl = false;
        int upgradeable = 0;
        foreach (WeaponInstance weapon in player.WeaponSlots)
        {
            targeting |= WeaponTraits.SearchesTarget(weapon.Base);
            directHits |= WeaponTraits.DealsDirectHits(weapon.Base);
            nativeControl |= WeaponTraits.AppliesNativeControl(weapon.Base);
            if (weapon.CanLevelUp && !banishedWeapons.Contains(weapon.Id))
                upgradeable++;
        }
        return (!conditions.TargetingWeapon || targeting)
            && (!conditions.DirectDamageWeapon || directHits)
            && (!conditions.NativeControlWeapon || nativeControl)
            && upgradeable >= conditions.MinUpgradeableWeapons;
    }

    private bool IsAvailable(PerkSpecializationData perk, Player player) =>
        PerkSpecializationEffects.IsOfferable(perk.Effect) && !_banished.Contains(perk.Id) && !player.HasSpecialization(perk.Id);

    private static string FamilyOf(FragmentOption option) => PerkSpecializationDataLoader.Get(option.Id)?.Family;

    private static void Take(FragmentOption option, List<FragmentOption> remaining, List<FragmentOption> picked)
    {
        remaining.Remove(option);
        picked.Add(option);
    }
}
