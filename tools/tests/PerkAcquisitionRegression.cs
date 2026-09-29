using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Acquisition des perks B1 (plan 05 §8.2) sur un vrai joueur et le vrai gestionnaire de niveaux : paliers, report,
/// cascade, première offre, bannissement, offres courtes, éligibilité selon l'arsenal, quatre emplacements.
/// </summary>
public partial class PerkAcquisitionRegression : Node2D
{
    private static readonly HashSet<string> NoBanishedWeapons = new();
    private int _failures;
    private Player _player;
    private FragmentManager _fragments;
    private readonly List<string> _acquired = new();

    public override void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            GetNode<EventBus>("/root/EventBus").SpecializationAcquired += id => _acquired.Add(id);
            PerkSpecializationDataLoader.Load();

            CheckNormalRunOffersNothing();
            CheckDeferredRight();
            CheckFirstOffer();
            CheckCalendarAndCascade();
            CheckBanishAndShortOffers();
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

    /// <summary>Run normale en B1 : aucun effet branché, donc aucune carte de perk, et les choix ordinaires continuent.</summary>
    private void CheckNormalRunOffersNothing()
    {
        Setup(preview: false);
        bool anyPerk = false;
        bool allOrdinary = true;
        for (int level = 2; level <= 30; level++)
        {
            _fragments.TriggerLevelUp(level);
            anyPerk |= _fragments.IsSpecializationChoice || HasPerkCard();
            allOrdinary &= _fragments.PendingChoices.Count > 0;
            _fragments.SkipChoice();
        }
        Check(!anyPerk && allOrdinary && _player.Specializations.Count == 0,
            "Run normale : aucun perk proposé tant qu'aucun effet n'est branché, choix ordinaires intacts");
    }

    /// <summary>Un droit sans candidat laisse un choix ordinaire et revient au niveau suivant.</summary>
    private void CheckDeferredRight()
    {
        Setup(preview: false);
        _fragments.TriggerLevelUp(2);
        bool ordinary = !_fragments.IsSpecializationChoice;
        _fragments.SkipChoice();
        PerkSpecializationEffects.PreviewInactive = true;
        _fragments.TriggerLevelUp(3);
        bool offered = _fragments.IsSpecializationChoice && _fragments.PendingChoices.Count == 3;
        Select(0);
        _fragments.TriggerLevelUp(4);
        Check(ordinary && offered && _player.Specializations.Count == 1 && !_fragments.IsSpecializationChoice,
            "Droit sans candidat au palier 2 : choix ordinaire, puis perk au niveau 3, puis retour à l'ordinaire");
        _fragments.SkipChoice();
    }

    /// <summary>Première offre : un défensif, un combat, une collecte ou récompense ; les deux défensifs alternent.</summary>
    private void CheckFirstOffer()
    {
        Setup(preview: true);
        _fragments.AddRerolls(60);
        int rerolls = _fragments.RerollsRemaining;
        _fragments.TriggerLevelUp(2);
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
        Check(_fragments.RerollsRemaining == rerolls - 60 && _fragments.IsSpecializationChoice, "Relancer un perk consomme une relance et propose encore des perks");
    }

    /// <summary>Paliers 2/6/12, passage reporté au niveau suivant, place du perk conservée dans une cascade.</summary>
    private void CheckCalendarAndCascade()
    {
        Setup(preview: true);
        _acquired.Clear();
        _fragments.TriggerLevelUp(2);
        string first = _fragments.PendingChoices[0].Id;
        Select(0);
        Check(_player.Specializations.Count == 1 && _acquired.Count == 1 && _acquired[0] == first,
            "Perk choisi au palier 2 : acquis et annoncé sur l'EventBus");

        bool ordinary = true;
        for (int level = 3; level <= 5; level++)
        {
            _fragments.TriggerLevelUp(level);
            ordinary &= !_fragments.IsSpecializationChoice && !HasPerkCard();
            _fragments.SkipChoice();
        }
        Check(ordinary, "Niveaux 3 à 5 : choix ordinaires");

        _fragments.TriggerLevelUp(6);
        bool secondOffer = _fragments.IsSpecializationChoice && !Offers(first);
        _fragments.SkipChoice();
        _fragments.TriggerLevelUp(7);
        bool reported = _fragments.IsSpecializationChoice;
        Select(0);
        Check(secondOffer && reported && _player.Specializations.Count == 2,
            "Palier 6 sans doublon ; passé, il revient au niveau 7");

        EventBus bus = GetNode<EventBus>("/root/EventBus");
        _fragments.TriggerLevelUp(11);
        bus.EmitSignal(EventBus.SignalName.LevelUp, 12);
        bus.EmitSignal(EventBus.SignalName.LevelUp, 13);
        bool queued = !_fragments.IsSpecializationChoice && _fragments.QueuedLevels == 2;
        _fragments.SkipChoice();
        bool twelve = _fragments.IsSpecializationChoice;
        Select(0);
        bool thirteen = _fragments.IsChoiceActive && !_fragments.IsSpecializationChoice && !HasPerkCard();
        _fragments.SkipChoice();
        Check(queued && twelve && thirteen && !_fragments.IsChoiceActive && _player.Specializations.Count == 3,
            "Cascade 11→13 : le palier 12 garde sa place, un seul droit servi, 13 ordinaire");
    }

    /// <summary>Bannir raccourcit l'offre ; le bannissement qui viderait l'offre est refusé sans être consommé.</summary>
    private void CheckBanishAndShortOffers()
    {
        Setup(preview: true);
        foreach (string id in new[] { "overheal_reserve", "rally", "xp_trail" })
            _player.AcquireSpecialization(PerkSpecializationDataLoader.Get(id));
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        _fragments.AddBanishes(10);
        _fragments.TriggerLevelUp(20);
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
            "Bannir la dernière carte de perk : refusé, non consommé");
        Select(0);
        Check(_player.Specializations.Count == 4, "Offre d'une carte : le quatrième perk s'acquiert");
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
        Check(offers.Candidates(_player, NoBanishedWeapons).Count == 0, "Sans aperçu : aucun candidat en B1");
    }

    /// <summary>Quatre perks distincts au plus ; ensuite plus aucune offre de perk.</summary>
    private void CheckFourSlots()
    {
        Setup(preview: true);
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        bool perksAtThresholds = true;
        bool neverAfter = true;
        for (int level = 2; level <= 60; level++)
        {
            _fragments.TriggerLevelUp(level);
            bool threshold = level is 2 or 6 or 12 or 20;
            if (threshold)
                perksAtThresholds &= _fragments.IsSpecializationChoice;
            else
                neverAfter &= !_fragments.IsSpecializationChoice;
            if (threshold)
                Select(0);
            else
                _fragments.SkipChoice();
        }
        HashSet<string> ids = new();
        foreach (PerkSpecializationData perk in _player.Specializations)
            ids.Add(perk.Id);
        Check(perksAtThresholds && neverAfter && _player.Specializations.Count == 4 && ids.Count == 4,
            "Paliers 2/6/12/20 servis, quatre perks distincts, aucune offre ensuite");
        PerkSpecializationData fifth = null;
        foreach (PerkSpecializationData perk in PerkSpecializationDataLoader.GetAll())
            if (!ids.Contains(perk.Id))
                fifth = perk;
        Check(!_player.AcquireSpecialization(fifth) && !_player.AcquireSpecialization(_player.Specializations[0])
            && _player.Specializations.Count == 4, "Cinquième perk et doublon refusés");
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
        _fragments = new FragmentManager { Name = "FragmentManager" };
        AddChild(_fragments);
    }

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
