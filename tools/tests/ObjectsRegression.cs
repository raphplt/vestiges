using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Objets du joueur (plan 21 §4, lots G2a et G2a-2) sur un vrai joueur : six emplacements, cinquante niveaux par
/// formule, effets multiples, niveaux gagnés selon la rareté, objets retirés des offres, paliers, copies d'attaque,
/// Durée et renouvellement des statuts.
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
            CheckMilestoneActivation();
            CheckMilestoneCards();
            CheckAttackCopies();
            CheckStatusDuration();
            CheckStatusRenewal();
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
        Check(offered.Count == 14 && wellFormed && anySurvival, $"Catalogue : {offered.Count} objets proposés, niveau max 50, au moins un de survie");
        bool retired = true;
        foreach (string id in new[] { "flamme_interieure", "reflet_brise", "fragment_deternite" })
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
        bool noRetired = !pool.Exists(option => option.Id is "flamme_interieure" or "reflet_brise");
        Check(fresh == 14 && noRetired, $"Offre de niveau : {fresh} objets neufs possibles, aucun objet retiré");

        foreach (string id in new[] { "memoire_vive", "resonance", "portee_etendue", "oeil_critique", "ancrage", "regeneration" })
            _player.AddOrUpgradePassive(id);
        pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);
        Check(!pool.Exists(option => option.Type == "passive_new") && pool.FindAll(option => option.Type == "passive_upgrade").Count == 6,
            "Six objets pris : plus d'objet neuf, six améliorations possibles");
    }

    private void CheckMilestoneActivation()
    {
        Setup();
        _player.AddOrUpgradePassive("souffle_du_neant");
        bool one = _player.AttackCopies == 1 && Near(_player.CopyDamageFactor, 0.314f) && _player.ObjectMilestones == null;
        _player.AddOrUpgradePassive("souffle_du_neant", 23);
        bool before = _player.AttackCopies == 1 && _player.GetPassiveLevel("souffle_du_neant") == 24;
        _player.AddOrUpgradePassive("souffle_du_neant");
        bool reached = _player.AttackCopies == 2 && _player.ObjectMilestones != null;
        _player.AddOrUpgradePassive("souffle_du_neant", 10);
        bool once = _player.AttackCopies == 2;
        _player.AddOrUpgradePassive("souffle_du_neant", 20);
        bool fifty = _player.AttackCopies == 3 && Near(_player.CopyDamageFactor, 1f);
        Check(one && before && reached && once && fifty,
            "Papier carbone : 1 copie à 31,4 % au niveau 1, 2 au palier 25 (une seule fois), 3 à 100 % au niveau 50");

        Setup();
        _player.AddOrUpgradePassive("souffle_du_neant");
        _player.AddOrUpgradePassive("souffle_du_neant", 49);
        Check(_player.AttackCopies == 3, "Une amélioration qui franchit deux paliers les active tous les deux");
    }

    private void CheckMilestoneCards()
    {
        Setup();
        _player.AddOrUpgradePassive("souffle_du_neant");
        _player.AddOrUpgradePassive("souffle_du_neant", 20);
        string upcoming = CardText(new FragmentOption("souffle_du_neant", "passive_upgrade", "Papier carbone", 1)
            .WithPassiveUpgrade(UpgradeRoller.Get("common"), 1));
        string crossing = CardText(new FragmentOption("souffle_du_neant", "passive_upgrade", "Papier carbone", 1)
            .WithPassiveUpgrade(UpgradeRoller.Get("legendary"), 5));
        string fresh = CardText(new FragmentOption("persistance", "passive_new", "Pince à linge", 1));
        Check(upcoming.Contains("Palier 25 :") && crossing.Contains("Palier 25 atteint") && !crossing.Contains("Palier 50")
            && fresh.Contains("Palier 25 :") && fresh.Contains("Durée"),
            "Cartes : le prochain palier annoncé, « atteint » quand l'amélioration le franchit");

        bool coded = true;
        foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
            foreach (ObjectMilestoneData milestone in data.Milestones)
                if (!ObjectMilestoneEffects.IsImplemented(milestone.Effect))
                {
                    string card = CardText(new FragmentOption(data.Id, "passive_new", data.Name, 1));
                    coded &= !card.Contains(milestone.Text);
                }
        Check(coded && !ObjectMilestoneEffects.IsImplemented("inconnu"), "Un palier non codé n'est pas annoncé");
    }

    private void CheckAttackCopies()
    {
        Setup();
        MethodInfo strikes = typeof(Player).GetMethod("StrikeMultiplierSum", Private);
        MethodInfo projectile = typeof(Player).GetMethod("ProjectileDamage", Private);
        float soloStrike = (float)strikes.Invoke(_player, new object[] { 0, 0 });
        _player.AddOrUpgradePassive("souffle_du_neant");
        float withCopy = (float)strikes.Invoke(_player, new object[] { 0, 1 });
        float copyOnly = (float)strikes.Invoke(_player, new object[] { 1, 1 });
        float copyShot = (float)projectile.Invoke(_player, new object[] { 10f, false, true });
        float fullShot = (float)projectile.Invoke(_player, new object[] { 10f, false, false });
        Check(Near(soloStrike, 1f) && Near(withCopy, 1.314f) && Near(copyOnly, 0.314f) && Near(copyShot, 3.14f) && Near(fullShot, 10f),
            "Copies : une frappe ou un tir de plus, à 31,4 % des dégâts au niveau 1");
    }

    private void CheckStatusDuration()
    {
        Setup();
        _player.AddOrUpgradePassive("persistance");
        _player.AddOrUpgradePassive("persistance", 9);
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        WeaponInstance bell = _player.WeaponSlots[_player.WeaponSlots.Count - 1];
        Enemy enemy = SpawnEnemy();
        _player.OnProjectileHit(enemy, 1f, false, false, bell);
        float slow = (float)typeof(Enemy).GetField("_slowTimer", Private).GetValue(enemy);
        Check(Near(_player.StatusDurationMultiplier, 1.15f) && Near(slow, 2f * 1.15f),
            $"Pince à linge niveau 10 : statuts +15 % (ralentissement de la Cloche {slow:0.00} s au lieu de 2 s)");
        enemy.QueueFree();
    }

    private void CheckStatusRenewal()
    {
        Setup();
        _player.AddOrUpgradePassive("persistance");
        _player.AddOrUpgradePassive("persistance", 24);
        _player.ObjectMilestones.Rng.Seed = 7;
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        WeaponInstance bell = _player.WeaponSlots[_player.WeaponSlots.Count - 1];
        Enemy enemy = SpawnEnemy();
        MethodInfo decay = typeof(Enemy).GetMethod("ProcessSlowDecay", Private);
        FieldInfo timer = typeof(Enemy).GetField("_slowTimer", Private);
        int renewed = 0;
        bool sameDuration = true;
        const int trials = 400;
        for (int i = 0; i < trials; i++)
        {
            enemy.ApplySlow(0.5f, 1f, _player.BeginAttack(bell, 1f));
            decay.Invoke(enemy, new object[] { 1.5f });
            float remaining = (float)timer.GetValue(enemy);
            if (remaining > 0f)
            {
                renewed++;
                sameDuration &= Near(remaining, 1f);
                decay.Invoke(enemy, new object[] { 1.5f });
                // Un renouvellement peut lui-même se renouveler : on vide jusqu'à l'expiration définitive.
                while ((float)timer.GetValue(enemy) > 0f)
                    decay.Invoke(enemy, new object[] { 1.5f });
            }
        }
        int propagatedRenewals = 0;
        bool stayedPropagated = true;
        for (int i = 0; i < 100; i++)
        {
            enemy.ApplySlow(0.5f, 1f, _player.BeginAttack(bell, 1f), ControlOrigin.Propagated);
            decay.Invoke(enemy, new object[] { 1.5f });
            if ((float)timer.GetValue(enemy) <= 0f)
                continue;
            propagatedRenewals++;
            stayedPropagated &= !enemy.SlowControl.CanPropagate(_player.GetInstanceId());
            while ((float)timer.GetValue(enemy) > 0f)
                decay.Invoke(enemy, new object[] { 1.5f });
        }
        Check(propagatedRenewals > 0 && stayedPropagated, $"Un ralentissement reçu par Propagation, renouvelé {propagatedRenewals} fois, ne redevient pas transmissible");

        enemy.ApplySlow(0.5f, 1f);
        int foreign = 0;
        for (int i = 0; i < 40; i++)
        {
            decay.Invoke(enemy, new object[] { 1.5f });
            foreign += (float)timer.GetValue(enemy) > 0f ? 1 : 0;
            enemy.ApplySlow(0.5f, 1f);
        }
        float share = renewed / (float)trials;
        Check(share > 0.18f && share < 0.32f && sameDuration && foreign == 0,
            $"Pince à linge palier 25 : {share:P0} des ralentissements expirés renouvelés, même durée ; rien pour un statut sans joueur");
        enemy.QueueFree();
    }

    private string CardText(FragmentOption option)
    {
        List<string> lines = new();
        foreach ((string line, Color _) in Vestiges.UI.UpgradeText.Describe(option, _player))
            lines.Add(line);
        return string.Join(" | ", lines);
    }

    private Enemy SpawnEnemy()
    {
        Enemy enemy = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn").Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
        enemy.SetPhysicsProcess(false);
        enemy.Position = new Vector2(1000f, 1000f);
        return enemy;
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
