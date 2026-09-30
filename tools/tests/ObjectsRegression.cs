using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Objets du joueur (plan 21 §4, lot G2a) sur un vrai joueur : six emplacements, cinquante niveaux par formule, effets
/// multiples, niveaux gagnés selon la rareté, objets retirés des offres.
/// </summary>
public partial class ObjectsRegression : Node2D
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private int _failures;
    private Player _player;

    public override void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            CheckCatalogue();
            CheckSlots();
            CheckLevels();
            CheckMultipleEffects();
            CheckNewStats();
            CheckRarityLevels();
            CheckOffers();
            GD.Print($"[ObjectsRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    private void CheckCatalogue()
    {
        List<PassiveSouvenirData> offered = PassiveSouvenirDataLoader.GetAll();
        bool wellFormed = true;
        bool anySurvival = false;
        foreach (PassiveSouvenirData data in offered)
        {
            wellFormed &= data.MaxLevel == 50 && data.Effects.Count > 0 && !string.IsNullOrEmpty(data.Name);
            anySurvival |= data.Survival;
        }
        Check(offered.Count == 12 && wellFormed && anySurvival, $"Catalogue : {offered.Count} objets proposés, niveau max 50, au moins un de survie");
        bool retired = true;
        foreach (string id in new[] { "flamme_interieure", "reflet_brise", "souffle_du_neant", "fragment_deternite" })
            retired &= PassiveSouvenirDataLoader.Get(id) != null && !offered.Exists(data => data.Id == id);
        Check(retired, "Objets retirés : identifiants gardés, hors des offres");
    }

    private void CheckSlots()
    {
        Setup();
        string[] ids = { "memoire_vive", "resonance", "portee_etendue", "oeil_critique", "ancrage", "regeneration", "peau_dure" };
        int added = 0;
        foreach (string id in ids)
            added += _player.AddOrUpgradePassive(id) ? 1 : 0;
        Check(Player.MaxPassiveSlots == 6 && added == 6 && _player.PassiveSlots.Count == 6, "Six emplacements d'objets ; le septième objet est refusé");
        Check(_player.AddOrUpgradePassive("memoire_vive") && _player.GetPassiveLevel("memoire_vive") == 2, "Emplacements pleins : un objet possédé monte encore de niveau");
    }

    private void CheckLevels()
    {
        Setup();
        float baseSpeed = _player.AttackSpeedMultiplier;
        _player.AddOrUpgradePassive("memoire_vive");
        bool one = Near(_player.AttackSpeedMultiplier / baseSpeed, 1.012f);
        _player.AddOrUpgradePassive("memoire_vive", 4);
        bool five = Near(_player.AttackSpeedMultiplier / baseSpeed, 1.06f) && _player.GetPassiveLevel("memoire_vive") == 5;
        _player.AddOrUpgradePassive("memoire_vive", 200);
        bool fifty = Near(_player.AttackSpeedMultiplier / baseSpeed, 1.6f) && _player.GetPassiveLevel("memoire_vive") == 50;
        bool capped = !_player.AddOrUpgradePassive("memoire_vive") && Near(_player.AttackSpeedMultiplier / baseSpeed, 1.6f);
        Check(one && five && fifty && capped, "Ressort de sommier : +1,2 % de cadence par niveau, +60 % au niveau 50, plafonné");

        float hp = _player.EffectiveMaxHp;
        float current = _player.CurrentHp;
        _player.AddOrUpgradePassive("ancrage");
        _player.AddOrUpgradePassive("ancrage", 9);
        Check(Near(_player.EffectiveMaxHp, hp + 40f) && Near(_player.CurrentHp, current + 40f), "Bouton de manteau : +4 PV max par niveau, PV courants suivis");
    }

    private void CheckMultipleEffects()
    {
        Setup();
        float chance = _player.CritChance;
        float multiplier = _player.CritMultiplier;
        _player.AddOrUpgradePassive("oeil_critique");
        _player.AddOrUpgradePassive("oeil_critique", 9);
        Check(Near(_player.CritChance, chance + 0.06f) && Near(_player.CritMultiplier, multiplier + 0.1f),
            "Lunettes de lecture : deux effets montés ensemble (chance et dégâts critiques)");
    }

    private void CheckNewStats()
    {
        Setup();
        // En run, GameBootstrap attache la progression au joueur ; le banc fait de même.
        PlayerProgression progression = new() { Name = "PlayerProgression" };
        _player.AddChild(progression);
        _player.AddOrUpgradePassive("photo_de_classe");
        _player.AddOrUpgradePassive("photo_de_classe", 9);
        float before = progression.CurrentXp;
        GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.XpGained, 1f);
        Check(Near(progression.CurrentXp - before, 1.1f), "Photo de classe niveau 10 : +10 % d'XP, appliqué une fois au gain");

        _player.AddOrUpgradePassive("instinct");
        _player.AddOrUpgradePassive("instinct", 9);
        Check(Near(_player.Mobility.RechargeMultiplier, 1.1f) && Near(_player.SpeedMultiplier, 1.06f),
            "Lacet rouge niveau 10 : vitesse +6 %, dash rechargé 10 % plus vite");

        float luck = _player.LuckBonus;
        _player.AddOrUpgradePassive("jeton_de_fete");
        Check(Near(_player.LuckBonus, luck + 0.01f), "Jeton de fête foraine : +0,01 Chance par niveau");
    }

    private void CheckRarityLevels()
    {
        Setup();
        _player.AddOrUpgradePassive("resonance");
        RandomNumberGenerator rng = new() { Seed = 4 };
        List<int> levels = new();
        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
        {
            FragmentOption option = UpgradeRoller.RollGains(new FragmentOption("resonance", "passive_upgrade", "Rondelle de cuivre", 1), _player, rarity, rng);
            levels.Add(option.PassiveLevels);
        }
        Check(string.Join(",", levels) == "1,2,3,4,5", $"Rareté d'une amélioration d'objet : {string.Join(",", levels)} niveaux");
    }

    private void CheckOffers()
    {
        Setup();
        FragmentManager fragments = new() { Name = "FragmentManager" };
        AddChild(fragments);
        typeof(FragmentManager).GetMethod("CachePlayer", Private).Invoke(fragments, null);
        typeof(FragmentManager).GetField("_currentLevel", Private).SetValue(fragments, 5);
        List<FragmentOption> pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);
        int fresh = pool.FindAll(option => option.Type == "passive_new").Count;
        bool noRetired = !pool.Exists(option => option.Id is "flamme_interieure" or "reflet_brise" or "souffle_du_neant");
        Check(fresh == 12 && noRetired, $"Offre de niveau : {fresh} objets neufs possibles, aucun objet retiré");

        foreach (string id in new[] { "memoire_vive", "resonance", "portee_etendue", "oeil_critique", "ancrage", "regeneration" })
            _player.AddOrUpgradePassive(id);
        pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);
        Check(!pool.Exists(option => option.Type == "passive_new") && pool.FindAll(option => option.Type == "passive_upgrade").Count == 6,
            "Six objets pris : plus d'objet neuf, six améliorations possibles");
    }

    private void Setup()
    {
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
    }

    private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.002f;

    private void Check(bool ok, string label)
    {
        if (!ok)
            _failures++;
        GD.Print($"[ObjectsRegression] {(ok ? "PASS" : "FAIL")} {label}");
    }
}
