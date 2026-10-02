using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Droits et composition des offres de fragments (plan 21 §16, lot G1). Chaque Résurgence survécue ouvre un droit,
/// dans la limite des emplacements. Un droit passé, ou sans candidat, attend le prochain niveau gagné ou la prochaine
/// Résurgence ; il ne se rouvre jamais aussitôt.
/// </summary>
public sealed class PerkSpecializationOffers
{
    public const string OptionType = "specialization";

    private static readonly HashSet<string> NoBanishedWeapons = new();
    // Arsenal simulé d'un échange, réutilisé : l'invite d'échange le recalcule à chaque frame.
    private static readonly List<WeaponInstance> SwapPreview = new();

    private readonly PerkSpecializationConfig _config;
    private readonly HashSet<string> _banished = new();
    private int _pendingMoments;
    private bool _deferred;

    public PerkSpecializationOffers(PerkSpecializationConfig config)
    {
        _config = config;
    }

    public int Capacity => _config?.MaxEquipped ?? 0;

    /// <summary>Droits ouverts par les Résurgences et pas encore servis.</summary>
    public int PendingMoments => _pendingMoments;

    /// <summary>Une Résurgence survécue ouvre un droit, sans dépasser les emplacements encore libres.</summary>
    public bool GrantMoment(Player player)
    {
        if (_pendingMoments + player.Specializations.Count >= Capacity)
            return false;
        _pendingMoments++;
        _deferred = false;
        return true;
    }

    /// <summary>Un droit peut être servi maintenant : il en reste un, un emplacement est libre, il n'est pas reporté.</summary>
    public bool IsReady(Player player) => _pendingMoments > 0 && !_deferred && player.Specializations.Count < Capacity;

    /// <summary>Fragment choisi : le droit est servi.</summary>
    public void Consume() => _pendingMoments = Mathf.Max(0, _pendingMoments - 1);

    /// <summary>Passé ou sans candidat : le droit attend le prochain niveau gagné ou la prochaine Résurgence.</summary>
    public void Defer() => _deferred = true;

    /// <summary>Un nouveau niveau gagné redonne une occasion au droit reporté.</summary>
    public void Resume() => _deferred = false;

    /// <summary>Perks proposables maintenant : effet branché, ni acquis ni banni, conditions remplies par l'arsenal.</summary>
    public List<FragmentOption> Candidates(Player player, IReadOnlySet<string> banishedWeapons)
    {
        List<FragmentOption> candidates = new();
        foreach (PerkSpecializationData perk in PerkSpecializationDataLoader.GetAll())
        {
            if (IsAvailable(perk, player) && IsEligible(perk, player, banishedWeapons))
                candidates.Add(new FragmentOption(perk.Id, OptionType, perk.Name));
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
    public static bool IsEligible(PerkSpecializationData perk, Player player, IReadOnlySet<string> banishedWeapons) =>
        IsEligible(perk, player.WeaponSlots, banishedWeapons, true);

    /// <summary>
    /// Un perk acquis reste sans effet tant que l'arsenal ne remplit plus sa condition (arme support échangée). Le
    /// nombre d'armes améliorables ne compte que pour l'offre : des armes au maximum ne rendent rien inactif.
    /// </summary>
    public static bool IsActive(PerkSpecializationData perk, Player player) => IsActive(perk, player.WeaponSlots);

    private static bool IsActive(PerkSpecializationData perk, IReadOnlyList<WeaponInstance> weapons) =>
        IsEligible(perk, weapons, NoBanishedWeapons, false);

    /// <summary>
    /// Perks aujourd'hui actifs qu'un échange de l'arme du premier emplacement contre <paramref name="incoming"/>
    /// rendrait inactifs, séparés par des virgules ; vide sinon.
    /// </summary>
    public static string DeactivatedBySwap(Player player, WeaponInstance incoming)
    {
        if (player.Specializations.Count == 0 || player.WeaponSlots.Count == 0)
            return "";
        SwapPreview.Clear();
        for (int i = 1; i < player.WeaponSlots.Count; i++)
            SwapPreview.Add(player.WeaponSlots[i]);
        SwapPreview.Add(incoming);
        string names = "";
        foreach (PerkSpecializationData perk in player.Specializations)
        {
            if (IsActive(perk, player) && !IsActive(perk, SwapPreview))
                names = names.Length == 0 ? perk.Name : $"{names}, {perk.Name}";
        }
        return names;
    }

    private static bool IsEligible(PerkSpecializationData perk, IReadOnlyList<WeaponInstance> weapons, IReadOnlySet<string> banishedWeapons,
        bool countUpgradeable)
    {
        PerkSpecializationEligibility conditions = perk.Eligibility;
        // Aucune récompense d'objets à choix n'existe en run avant le catalogue d'objets (B4).
        if (conditions.ItemChoiceRewards || conditions.OwnedItem)
            return false;

        bool targeting = false;
        bool directHits = false;
        bool nativeControl = false;
        int upgradeable = 0;
        foreach (WeaponInstance weapon in weapons)
        {
            targeting |= WeaponTraits.SearchesTarget(weapon);
            directHits |= WeaponTraits.DealsDirectHits(weapon);
            nativeControl |= WeaponTraits.AppliesNativeControl(weapon);
            if (weapon.CanLevelUp && !banishedWeapons.Contains(weapon.Id))
                upgradeable++;
        }
        return (!conditions.TargetingWeapon || targeting)
            && (!conditions.DirectDamageWeapon || directHits)
            && (!conditions.NativeControlWeapon || nativeControl)
            && (!countUpgradeable || upgradeable >= conditions.MinUpgradeableWeapons);
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
