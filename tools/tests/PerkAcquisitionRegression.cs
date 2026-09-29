using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Acquisition des fragments (plan 21 §16, lot G1) sur un vrai joueur et le vrai gestionnaire de niveaux : un droit par
/// Résurgence survécue, place dans la file, report au niveau suivant, première offre, bannissements gratuits puis payés
/// en Péril, offres courtes, éligibilité selon l'arsenal, quatre emplacements.
/// </summary>
public partial class PerkAcquisitionRegression : Node2D
{
    private static readonly HashSet<string> NoBanishedWeapons = new();
    private int _failures;
    private Player _player;
    private FragmentManager _fragments;
    private readonly List<string> _acquired = new();
    private EventBus _bus;
    private PerilManager _peril;

    public override void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            GetNode<EventBus>("/root/EventBus").SpecializationAcquired += id => _acquired.Add(id);
            PerkSpecializationDataLoader.Load();

            _bus = GetNode<EventBus>("/root/EventBus");
            CheckLevelsAloneOfferNoFragment();
            CheckResurgenceOffers();
            CheckQueueAndDeferral();
            CheckNoCandidate();
            CheckFirstOffer();
            CheckBanishAndShortOffers();
            CheckBanishCostsPeril();
            CheckCatalogueBanishLimit();
            CheckEligibility();
            CheckFourSlots();
            GD.Print($"[PerkAcquisitionRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    /// <summary>Sans Résurgence, les niveaux ne proposent jamais de fragment ; les choix ordinaires continuent.</summary>
    private void CheckLevelsAloneOfferNoFragment()
    {
        Setup(preview: false);
        bool anyFragment = false;
        bool allOffered = true;
        for (int level = 2; level <= 30; level++)
        {
            LevelUp(level);
            anyFragment |= _fragments.IsSpecializationChoice || HasPerkCard();
            allOffered &= _fragments.PendingChoices.Count > 0;
            _fragments.SkipChoice();
        }
        Check(!anyFragment && allOffered && _player.Specializations.Count == 0, "Niveaux 2 à 30 sans Résurgence : aucun fragment, choix ordinaires intacts");
    }

    /// <summary>Une Résurgence survécue ouvre aussitôt une offre de trois fragments, puis les niveaux redeviennent ordinaires.</summary>
    private void CheckResurgenceOffers()
    {
        Setup(preview: false);
        _acquired.Clear();
        Resurgence(1);
        bool offered = _fragments.IsSpecializationChoice && _fragments.PendingChoices.Count == 3;
        bool onlyImplemented = true;
        foreach (FragmentOption option in _fragments.PendingChoices)
            onlyImplemented &= PerkSpecializationEffects.IsImplemented(PerkSpecializationDataLoader.Get(option.Id).Effect);
        string chosen = _fragments.PendingChoices[0].Id;
        Select(0);
        Check(offered && onlyImplemented && _acquired.Count == 1 && _acquired[0] == chosen && !_fragments.IsChoiceActive,
            "Résurgence survécue : offre de trois fragments branchés, acquisition annoncée, écran refermé");
        LevelUp(5);
        Check(!_fragments.IsSpecializationChoice && !HasPerkCard(), "Niveau suivant : choix ordinaire");
        _fragments.SkipChoice();
    }

    /// <summary>Pendant un choix de niveau, le fragment attend son tour et passe avant les niveaux en file ; passé, il revient au niveau suivant.</summary>
    private void CheckQueueAndDeferral()
    {
        Setup(preview: false);
        LevelUp(3);
        LevelUp(4);
        Resurgence(1);
        bool waited = !_fragments.IsSpecializationChoice && _fragments.QueuedLevels == 1;
        _fragments.SkipChoice();
        bool before = _fragments.IsSpecializationChoice && _fragments.QueuedLevels == 1;
        _fragments.SkipChoice();
        bool levelAfter = _fragments.IsChoiceActive && !_fragments.IsSpecializationChoice;
        _fragments.SkipChoice();
        Check(waited && before && levelAfter && !_fragments.IsChoiceActive,
            "Résurgence pendant un choix : fragment après le choix courant, avant le niveau en file");

        LevelUp(5);
        bool reoffered = _fragments.IsSpecializationChoice && _fragments.QueuedLevels == 1;
        Select(0);
        bool thenLevel = _fragments.IsChoiceActive && !_fragments.IsSpecializationChoice;
        _fragments.SkipChoice();
        Check(reoffered && thenLevel && _player.Specializations.Count == 1,
            "Fragment passé : il revient au niveau gagné suivant, puis ce niveau est proposé");
    }

    /// <summary>Sans candidat, le droit attend ; une arme compatible et un niveau plus tard, il est servi.</summary>
    private void CheckNoCandidate()
    {
        Setup(preview: false);
        while (_player.WeaponSlots.Count > 0)
            _player.RemoveWeapon(0);
        foreach (FragmentOption option in new PerkSpecializationOffers(PerkSpecializationDataLoader.Config).Candidates(_player, NoBanishedWeapons))
            if (_player.Specializations.Count < 3)
                _player.AcquireSpecialization(PerkSpecializationDataLoader.Get(option.Id));
        Resurgence(1);
        bool waiting = !_fragments.IsChoiceActive;
        _player.AddWeapon(WeaponDataLoader.Get("makeshift_bow"));
        LevelUp(6);
        bool served = _fragments.IsSpecializationChoice;
        Select(0);
        _fragments.SkipChoice();
        Check(waiting && served && _player.Specializations.Count == 4, "Aucun fragment proposable : droit gardé, servi au niveau suivant");
    }

    /// <summary>Première offre : un défensif, un combat, une collecte ou récompense ; les deux défensifs alternent.</summary>
    private void CheckFirstOffer()
    {
        Setup(preview: true);
        _fragments.AddRerolls(60);
        int rerolls = _fragments.RerollsRemaining;
        Resurgence(1);
        bool composed = true;
        bool distinct = true;
        HashSet<string> survival = new();
        for (int offer = 0; offer < 60; offer++)
        {
            HashSet<string> families = new();
            HashSet<string> ids = new();
            foreach (FragmentOption option in _fragments.PendingChoices)
            {
                PerkSpecializationData perk = PerkSpecializationDataLoader.Get(option.Id);
                families.Add(perk.Family);
                distinct &= ids.Add(option.Id);
                if (perk.Family == "survival")
                    survival.Add(perk.Id);
            }
            composed &= _fragments.IsSpecializationChoice && _fragments.PendingChoices.Count == 3
                && families.Contains("survival") && families.Contains("combat")
                && (families.Contains("collection") || families.Contains("rewards"));
            _fragments.Reroll();
        }
        Check(composed && distinct, "Première offre : trois perks distincts, survie + combat + collecte/récompenses, à chaque relance");
        Check(survival.Contains("overheal_reserve") && survival.Contains("rally"), "Première offre : Prévoyance et Reprise alternent");
        Check(_fragments.RerollsRemaining == rerolls - 60 && _fragments.IsSpecializationChoice, "Relancer des fragments consomme une relance et propose encore des perks");
    }

    /// <summary>Bannir raccourcit l'offre ; le bannissement qui viderait l'offre est refusé sans être consommé.</summary>
    private void CheckBanishAndShortOffers()
    {
        Setup(preview: true);
        foreach (string id in new[] { "overheal_reserve", "rally", "xp_trail" })
            _player.AcquireSpecialization(PerkSpecializationDataLoader.Get(id));
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        _fragments.AddBanishes(10);
        Resurgence(4);
        // Candidats : Convergence, Débordement, Propagation, Seconde lecture.
        List<int> sizes = new() { _fragments.PendingChoices.Count };
        for (int i = 0; i < 3; i++)
        {
            _fragments.BanishFragment(_fragments.PendingChoices[0].Id);
            sizes.Add(_fragments.PendingChoices.Count);
        }
        int banishes = _fragments.BanishesRemaining;
        string last = _fragments.PendingChoices[0].Id;
        _fragments.BanishFragment(last);
        Check(string.Join(",", sizes) == "3,3,2,1", $"Offres courtes après bannissements : {string.Join(",", sizes)}");
        Check(_fragments.BanishesRemaining == banishes && _fragments.IsSpecializationChoice && Offers(last),
            "Bannir la dernière carte de fragment : refusé, non consommé");
        Select(0);
        Check(_player.Specializations.Count == 4, "Offre d'une carte : le quatrième perk s'acquiert");
    }

    /// <summary>Bannir, c'est oublier : trois gratuits, puis +⅓, +⅔, +1 Péril… réglés par points entiers.</summary>
    private void CheckBanishCostsPeril()
    {
        Setup(preview: false);
        LevelUp(8);
        List<float> costs = new();
        List<int> peril = new();
        for (int i = 0; i < 6; i++)
        {
            costs.Add(_fragments.NextBanishPerilCost);
            _fragments.BanishFragment(_fragments.PendingChoices[0].Id);
            peril.Add(_peril.Peril);
        }
        _fragments.SkipChoice();
        string costText = string.Join(" ", costs.ConvertAll(cost => cost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
        Check(costText == "0.00 0.00 0.00 0.33 0.67 1.00" && string.Join(",", peril) == "0,0,0,0,1,2" && _fragments.BanishesRemaining == 0,
            $"Bannissements : trois gratuits, puis coût croissant en Péril ({costText} ; Péril {string.Join(",", peril)})");
    }

    /// <summary>Le catalogue doit toujours pouvoir remplir les emplacements restants.</summary>
    private void CheckCatalogueBanishLimit()
    {
        Setup(preview: true);
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        PerkSpecializationOffers offers = new(PerkSpecializationDataLoader.Config);
        bool allowed = true;
        foreach (string id in new[] { "salvage_xp", "familiar_loot", "carried_choice", "carry_control", "priority_targeting" })
            allowed &= offers.TryBanish(id, _player, NoBanishedWeapons);
        bool refused = !offers.TryBanish("overflow", _player, NoBanishedWeapons);
        Check(allowed && refused && offers.Candidates(_player, NoBanishedWeapons).Count == 4,
            "Bannissements : refusés dès que quatre perks restent pour quatre emplacements");
    }

    /// <summary>Les conditions suivent l'arsenal : ciblage, contrôle natif, deux armes améliorables, objets absents.</summary>
    private void CheckEligibility()
    {
        Setup(preview: true);
        PerkSpecializationOffers offers = new(PerkSpecializationDataLoader.Config);
        HashSet<string> bow = Ids(offers.Candidates(_player, NoBanishedWeapons));
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        HashSet<string> withBell = Ids(offers.Candidates(_player, NoBanishedWeapons));
        HashSet<string> bellBanished = Ids(offers.Candidates(_player, new HashSet<string> { "teachers_bell" }));
        _player.RemoveWeapon(0);
        HashSet<string> bellOnly = Ids(offers.Candidates(_player, NoBanishedWeapons));

        Check(bow.Contains("priority_targeting") && bow.Contains("overflow") && !bow.Contains("carry_control") && !bow.Contains("carried_choice"),
            "Arc seul : Convergence et Débordement, ni Propagation ni Seconde lecture");
        Check(withBell.Contains("carry_control") && withBell.Contains("carried_choice"),
            "Arc + Cloche : Propagation (ralentissement natif) et Seconde lecture (deux armes améliorables)");
        Check(!bellBanished.Contains("carried_choice"), "Arme bannie : ne compte plus comme améliorable");
        Check(!bellOnly.Contains("priority_targeting") && bellOnly.Contains("carry_control"),
            "Cloche seule (onde) : pas de Convergence, Propagation conservée");
        Check(!withBell.Contains("salvage_xp") && !withBell.Contains("familiar_loot"),
            "Délestage et Habitude absents tant que les objets à choix n'existent pas");
        PerkSpecializationEffects.PreviewInactive = false;
        bool implementedOnly = true;
        foreach (FragmentOption option in offers.Candidates(_player, NoBanishedWeapons))
            implementedOnly &= PerkSpecializationEffects.IsImplemented(PerkSpecializationDataLoader.Get(option.Id).Effect);
        Check(implementedOnly, "Sans aperçu : seuls les effets branchés sont candidats");
    }

    /// <summary>Quatre fragments distincts au plus ; les Résurgences suivantes n'en offrent plus.</summary>
    private void CheckFourSlots()
    {
        Setup(preview: true);
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        bool eachOffered = true;
        for (int crisis = 1; crisis <= 4; crisis++)
        {
            Resurgence(crisis);
            eachOffered &= _fragments.IsSpecializationChoice;
            Select(0);
        }
        Resurgence(5);
        Resurgence(6);
        bool noMore = !_fragments.IsChoiceActive;
        HashSet<string> ids = new();
        foreach (PerkSpecializationData perk in _player.Specializations)
            ids.Add(perk.Id);
        Check(eachOffered && noMore && _player.Specializations.Count == 4 && ids.Count == 4,
            "Quatre Résurgences, quatre fragments distincts ; les suivantes n'en offrent plus");
        PerkSpecializationData fifth = null;
        foreach (PerkSpecializationData perk in PerkSpecializationDataLoader.GetAll())
            if (!ids.Contains(perk.Id))
                fifth = perk;
        Check(!_player.AcquireSpecialization(fifth) && !_player.AcquireSpecialization(_player.Specializations[0])
            && _player.Specializations.Count == 4, "Cinquième fragment et doublon refusés");
    }

    private void Setup(bool preview)
    {
        PerkSpecializationEffects.PreviewInactive = preview;
        if (_fragments != null)
        {
            RemoveChild(_fragments);
            _fragments.QueueFree();
        }
        if (_player != null)
        {
            RemoveChild(_player);
            _player.QueueFree();
        }
        _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(_player);
        _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        _player.IsAIControlled = true;
        _player.SetPhysicsProcess(false);
        if (_peril != null)
        {
            RemoveChild(_peril);
            _peril.QueueFree();
        }
        _peril = new PerilManager { Name = "PerilManager" };
        AddChild(_peril);
        _fragments = new FragmentManager { Name = "FragmentManager" };
        AddChild(_fragments);
    }

    /// <summary>Niveau gagné tel que le jeu l'émet ; une retenue de la réserve est levée aussitôt, comme à son expiration.</summary>
    private void LevelUp(int level)
    {
        const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        _bus.EmitSignal(EventBus.SignalName.LevelUp, level);
        Timer hold = (Timer)typeof(FragmentManager).GetField("_holdTimer", Private).GetValue(_fragments);
        if (hold.IsStopped())
            return;
        hold.Stop();
        typeof(FragmentManager).GetMethod("ProcessNextInQueue", Private).Invoke(_fragments, null);
    }

    private void Resurgence(int number) => _bus.EmitSignal(EventBus.SignalName.CrisisEnded, number);

    private void Select(int index) => _fragments.SelectFragment(_fragments.PendingChoices[index]);

    private bool HasPerkCard()
    {
        foreach (FragmentOption option in _fragments.PendingChoices)
            if (option.Type == PerkSpecializationOffers.OptionType)
                return true;
        return false;
    }

    private bool Offers(string id)
    {
        foreach (FragmentOption option in _fragments.PendingChoices)
            if (option.Id == id)
                return true;
        return false;
    }

    private static HashSet<string> Ids(List<FragmentOption> options)
    {
        HashSet<string> ids = new();
        foreach (FragmentOption option in options)
            ids.Add(option.Id);
        return ids;
    }

    private void Check(bool ok, string label)
    {
        if (!ok)
            _failures++;
        GD.Print($"[PerkAcquisitionRegression] {(ok ? "PASS" : "FAIL")} {label}");
    }
}
