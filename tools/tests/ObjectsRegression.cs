using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Objets du joueur (plan 21 §4, lots G2a et G2a-2, plan 23 R3) sur un vrai joueur : six emplacements, trente
/// niveaux, valeur égale à la somme des gains, gain selon la rareté, objets retirés des offres, paliers au niveau 15,
/// projectiles en plus fractionnaires, Durée et renouvellement des statuts.
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
            CheckRarityGains();
            CheckOffers();
            CheckMilestoneActivation();
            CheckMilestoneCards();
            CheckExtraProjectiles();
            CheckStatusDuration();
            CheckStatusRenewal();
            CheckCombatMilestones();
            CheckSurvivalMilestones();
            CheckRewardMilestones();
            CheckImpactTriggers();
            CheckTargetBonuses();
            CheckKillTransmissions();
            CheckFragility();
            CheckKillRewards();
            CheckStrideAndLevels();
            CheckNamedProperties();
            CheckCritTriggers();
            CheckDashTrail();
            CheckStances();
            CheckChestStatBonus();
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
            wellFormed &= data.MaxLevel == 30 && data.Effects.Count > 0 && !string.IsNullOrEmpty(data.Name);
            anySurvival |= data.Survival;
            foreach (ObjectMilestoneData milestone in data.Milestones)
                wellFormed &= milestone.Level == 15;
        }
        Check(offered.Count == 31 && wellFormed && anySurvival, $"Catalogue : {offered.Count} objets proposés, niveau max 30, paliers au niveau 15, au moins un de survie");
        bool retired = true;
        foreach (string id in new[] { "flamme_interieure", "fragment_deternite" })
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
        bool one = Near(_player.AttackSpeedMultiplier / baseSpeed, 1.08f);
        _player.AddOrUpgradePassive("memoire_vive", 4);
        bool five = Near(_player.AttackSpeedMultiplier / baseSpeed, 1.4f) && _player.GetPassiveLevel("memoire_vive") == 5;
        _player.AddOrUpgradePassive("memoire_vive", 200);
        bool thirty = Near(_player.AttackSpeedMultiplier / baseSpeed, 3.4f) && _player.GetPassiveLevel("memoire_vive") == 30;
        bool capped = !_player.AddOrUpgradePassive("memoire_vive") && Near(_player.AttackSpeedMultiplier / baseSpeed, 3.4f);
        Check(one && five && thirty && capped, "Ressort de sommier : +8 % de cadence par carte commune, +240 % au niveau 30, plafonné");

        float hp = _player.EffectiveMaxHp;
        float current = _player.CurrentHp;
        _player.AddOrUpgradePassive("ancrage");
        _player.AddOrUpgradePassive("ancrage", 9);
        Check(Near(_player.EffectiveMaxHp, hp + 150f) && Near(_player.CurrentHp, current + 150f), "Bouton de manteau : +15 PV max par carte commune, PV courants suivis");
    }

    private void CheckMultipleEffects()
    {
        Setup();
        float chance = _player.CritChance;
        float multiplier = _player.CritMultiplier;
        _player.AddOrUpgradePassive("oeil_critique");
        _player.AddOrUpgradePassive("oeil_critique", 9);
        Check(Near(_player.CritChance, chance + 0.2f) && Near(_player.CritMultiplier, multiplier + 1f),
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
        Check(Near(progression.CurrentXp - before, 1.5f), "Photo de classe niveau 10 : +50 % d'XP, appliqué une fois au gain");

        _player.AddOrUpgradePassive("instinct");
        _player.AddOrUpgradePassive("instinct", 9);
        Check(Near(_player.Mobility.RechargeMultiplier, 1.5f) && Near(_player.SpeedMultiplier, 1.3f),
            "Lacet rouge niveau 10 : vitesse +30 %, dash rechargé 50 % plus vite");

        float luck = _player.LuckBonus;
        _player.AddOrUpgradePassive("jeton_de_fete");
        Check(Near(_player.LuckBonus, luck + 0.03f), "Jeton de fête foraine : +0,03 Chance par carte commune");
    }

    private void CheckRarityGains()
    {
        RandomNumberGenerator rng = new() { Seed = 4 };
        List<string> values = new();
        bool oneLevel = true;
        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
        {
            Setup();
            float baseAoe = _player.AoeMultiplier;
            _player.AddOrUpgradePassive("resonance");
            FragmentOption option = UpgradeRoller.RollGains(new FragmentOption("resonance", "passive_upgrade", "Rondelle de cuivre", 1), _player, rarity, rng);
            option.ApplyTo(_player);
            oneLevel &= _player.GetPassiveLevel("resonance") == 2;
            values.Add(((_player.AoeMultiplier / baseAoe - 1f) * 100f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
        }
        Check(oneLevel && string.Join(",", values) == "16,20,24,28,32",
            $"Rareté d'une carte d'objet : un niveau, gain du pas × 1 à × 3 (Rondelle au niveau 2 : +{string.Join(" / +", values)} %)");

        Setup();
        _player.AddOrUpgradePassive("resonance");
        _player.AddOrUpgradePassive("resonance", 1, 3f);
        _player.AddOrUpgradePassive("resonance", 1, 1.5f);
        Check(Near(_player.AoeMultiplier, 1f + 0.08f * 5.5f), "La valeur d'un objet est la somme de ses gains : 1 + 3 + 1,5 pas");
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
        bool noRetired = !pool.Exists(option => option.Id is "flamme_interieure" or "fragment_deternite");
        Check(fresh == 31 && noRetired, $"Offre de niveau : {fresh} objets neufs possibles, aucun objet retiré");

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
        bool one = Near(_player.BonusProjectiles, 0.5f) && _player.ObjectMilestones == null;
        _player.AddOrUpgradePassive("souffle_du_neant", 13);
        bool before = Near(_player.BonusProjectiles, 7f) && _player.GetPassiveLevel("souffle_du_neant") == 14 && _player.ObjectMilestones == null;
        _player.AddOrUpgradePassive("souffle_du_neant");
        bool reached = _player.ObjectMilestones is { SpreadsExtraProjectiles: true } && Near(_player.BonusProjectiles, 7.5f);
        Check(one && before && reached,
            "Papier carbone : +0,5 projectile par carte commune ; au palier 15, les projectiles en plus visent chacun leur cible");

        Setup();
        _player.AddOrUpgradePassive("reflet_brise");
        _player.AddOrUpgradePassive("reflet_brise", 14, 2f);
        Check(Near(_player.ProjectilePierce, 0.5f + 14f) && _player.ObjectMilestones is { } milestones && Near(milestones.PierceDamageRamp, 0.1f),
            "Reflet brisé : +0,5 perforation par carte commune (× 2 en rare) ; au palier 15, +10 % de dégâts par ennemi traversé");

        // Un tir qui perfore trois ennemis : le deuxième encaisse 110 %, le troisième 120 %.
        Projectile shot = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn").Instantiate<Projectile>();
        AddChild(shot);
        WeaponInstance bow = _player.WeaponSlots[0];
        shot.Launch(new Vector2(7000f, 7000f), Vector2.Right, 10f, 400f, 1f, 5, false, _player, bow.Base, bow,
            _player.BeginAttack(bow, 10f), pierceDamageRamp: 0.1f);
        MethodInfo enter = typeof(Projectile).GetMethod("OnBodyEntered", Private);
        List<float> losses = new();
        for (int i = 0; i < 3; i++)
        {
            Enemy target = SpawnEnemy();
            target.Position = new Vector2(7000f + i * 40f, 7000f);
            float hp = Hp(target);
            enter.Invoke(shot, new object[] { target });
            losses.Add(hp - Hp(target));
            target.QueueFree();
        }
        Check(Near(losses[1], losses[0] * 1.1f) && Near(losses[2], losses[0] * 1.2f),
            $"Reflet brisé palier 15 : dégâts d'un tir qui perfore {losses[0]:0.0} → {losses[1]:0.0} → {losses[2]:0.0}");
        shot.QueueFree();
    }

    private void CheckMilestoneCards()
    {
        Setup();
        _player.AddOrUpgradePassive("souffle_du_neant");
        _player.AddOrUpgradePassive("souffle_du_neant", 12);
        // Niveau 13 : même une légendaire ne mène qu'au niveau 14, sans palier.
        FragmentOption upcoming = new FragmentOption("souffle_du_neant", "passive_upgrade", "Papier carbone", 1)
            .WithPassiveUpgrade(UpgradeRoller.Get("legendary"));
        bool upcomingBadge = UpgradeText.ReachesMilestone(upcoming, _player);
        string upcomingText = CardText(upcoming);
        _player.AddOrUpgradePassive("souffle_du_neant");
        FragmentOption crossing = new FragmentOption("souffle_du_neant", "passive_upgrade", "Papier carbone", 1)
            .WithPassiveUpgrade(UpgradeRoller.Get("common"));
        FragmentOption fresh = new("persistance", "passive_new", "Pince à linge", 1);
        string freshText = CardText(fresh);
        Check(!upcomingBadge && UpgradeText.ReachesMilestone(crossing, _player)
            && !UpgradeText.ReachesMilestone(fresh, _player)
            && !upcomingText.Contains("Palier") && !CardText(crossing).Contains("Palier") && freshText.Contains("Durée"),
            "Cartes : un palier atteint est un badge, jamais une ligne de texte (plan 23 R2)");

        bool coded = true;
        foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
            foreach (ObjectMilestoneData milestone in data.Milestones)
                if (!ObjectMilestoneEffects.IsImplemented(milestone.Effect) && milestone.Level > 1)
                {
                    Setup();
                    _player.AddOrUpgradePassive(data.Id);
                    _player.AddOrUpgradePassive(data.Id, milestone.Level - 2);
                    coded &= !UpgradeText.ReachesMilestone(new FragmentOption(data.Id, "passive_upgrade", data.Name, 1)
                        .WithPassiveUpgrade(UpgradeRoller.Get("common")), _player);
                }
        Check(coded && !ObjectMilestoneEffects.IsImplemented("inconnu"), "Un palier non codé n'a pas de badge");
    }

    private void CheckExtraProjectiles()
    {
        bool rolls = FractionalCount.Roll(2.5f, 0.4f) == 3 && FractionalCount.Roll(2.5f, 0.6f) == 2 && FractionalCount.Roll(0f, 0f) == 0
            && FractionalCount.Roll(1f, 0.999f) == 1 && FractionalCount.Roll(0.75f, 0.74f) == 1 && FractionalCount.Roll(0.75f, 0.76f) == 0;
        Check(rolls, "Stat fractionnaire : la partie entière toujours, la décimale en chance");

        Setup();
        _player.AddOrUpgradePassive("souffle_du_neant", 5);
        MethodInfo roll = typeof(Player).GetMethod("RollBonusProjectiles", Private);
        WeaponInstance bow = _player.WeaponSlots[0];
        int total = 0, low = int.MaxValue, high = 0;
        const int attacks = 1000;
        for (int i = 0; i < attacks; i++)
        {
            int extra = (int)roll.Invoke(_player, new object[] { bow });
            total += extra;
            low = Math.Min(low, extra);
            high = Math.Max(high, extra);
        }
        float mean = total / (float)attacks;
        Check(low == 2 && high == 3 && mean > 2.42f && mean < 2.58f,
            $"Papier carbone niveau 5 : 2,5 projectiles en plus, soit 2 ou 3 par attaque ({mean:0.00} en moyenne sur {attacks})");
    }

    private void CheckStatusDuration()
    {
        Setup();
        _player.AddOrUpgradePassive("persistance");
        _player.AddOrUpgradePassive("persistance", 9);
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        WeaponInstance bell = _player.WeaponSlots[_player.WeaponSlots.Count - 1];
        Enemy enemy = SpawnEnemy();
        _player.OnProjectileHit(enemy, 1f, false, bell);
        float slow = (float)typeof(Enemy).GetField("_slowTimer", Private).GetValue(enemy);
        Check(Near(_player.StatusDurationMultiplier, 1.8f) && Near(slow, 2f * 1.8f),
            $"Pince à linge niveau 10 : statuts +80 % (ralentissement de la Cloche {slow:0.00} s au lieu de 2 s)");
        enemy.QueueFree();
    }

    private void CheckStatusRenewal()
    {
        Setup();
        _player.AddOrUpgradePassive("persistance");
        _player.AddOrUpgradePassive("persistance", 14);
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
            $"Pince à linge palier 15 : {share:P0} des ralentissements expirés renouvelés, même durée ; rien pour un statut sans joueur");
        enemy.QueueFree();
    }

    /// <summary>Monte un objet (neuf) jusqu'au niveau voulu.</summary>
    private void Raise(string id, int level)
    {
        _player.AddOrUpgradePassive(id);
        if (level > 1)
            _player.AddOrUpgradePassive(id, level - 1);
    }

    private void CheckCombatMilestones()
    {
        Setup();
        Enemy control = SpawnEnemy();
        Enemy echoed = SpawnEnemy();
        echoed.Position = new Vector2(500f, 0f);
        Enemy burst = SpawnEnemy();
        burst.Position = new Vector2(-500f, 0f);
        Enemy pushed = SpawnEnemy();
        pushed.Position = new Vector2(40f, 0f);
        // GroupCache garde sa liste pour la frame, et tout le banc tient dans la première : on la fait relire.
        typeof(GroupCache).GetField("_enemiesFrame", Private).SetValue(GetNode<GroupCache>("/root/GroupCache"), ulong.MaxValue);
        float hpBefore = Hp(control);
        control.TakeDamage(10f);
        float fullLoss = hpBefore - Hp(control);

        foreach (string id in new[] { "memoire_vive", "resonance", "portee_etendue", "oeil_critique", "carapace", "instinct" })
            Raise(id, 15);
        ObjectMilestones milestones = _player.ObjectMilestones;
        MethodInfo process = typeof(ObjectMilestones).GetMethod("_Process");
        System.Collections.IList repeats = (System.Collections.IList)typeof(ObjectMilestones).GetField("_pendingRepeats", Private).GetValue(milestones);
        WeaponInstance bow = _player.WeaponSlots[0];
        for (int i = 0; i < 9; i++)
            milestones.CountAttack(bow);
        bool ninth = repeats.Count == 0;
        milestones.CountAttack(bow);
        bool tenth = repeats.Count == 1;
        process.Invoke(milestones, new object[] { 0.2 });
        Check(ninth && tenth && repeats.Count == 0, "Ressort de sommier palier 15 : la 10ᵉ attaque d'une arme repart, puis la file se vide");
        for (int i = 0; i < 5; i++)
            milestones.CountAttack(bow);
        _player.UpgradeWeapon(bow.Id, UpgradeRoller.RollWeaponGains(bow, UpgradeRoller.Get("common"), new RandomNumberGenerator { Seed = 1 }));
        for (int i = 0; i < 5; i++)
            milestones.CountAttack(bow);
        Check(repeats.Count == 1, "Ressort de sommier : une montée de niveau de l'arme ne remet pas son compte à zéro");
        process.Invoke(milestones, new object[] { 0.2 });

        hpBefore = Hp(echoed);
        milestones.QueueCircleEcho(echoed.GlobalPosition, 30f, 10f, bow, _player.BeginAttack(bow, 10f));
        bool delayed = Near(Hp(echoed), hpBefore);
        process.Invoke(milestones, new object[] { 0.3 });
        Check(milestones.HasZoneEcho && delayed && Near(hpBefore - Hp(echoed), fullLoss * 0.3f),
            $"Rondelle de cuivre palier 15 : la zone refrappe après 0,25 s, à 30 % ({hpBefore - Hp(echoed):0.0} PV)");

        hpBefore = Hp(burst);
        _player.OnProjectileSpent(burst.GlobalPosition, 10f, _player.BeginAttack(bow, 10f));
        Check(Near(hpBefore - Hp(burst), fullLoss * 0.5f), $"Mètre pliant palier 15 : un projectile en bout de course éclate à 50 % ({hpBefore - Hp(burst):0.0} PV)");

        bool fullDouble = Near(_player.ResolveHitDamage(pushed, 10f, true), 20f);
        bool damagedSame = Near(_player.ResolveHitDamage(control, 10f, true), 10f);
        bool normal = Near(_player.ResolveHitDamage(pushed, 10f, false), 10f);
        Check(fullDouble && damagedSame && normal,
            "Lunettes de lecture palier 15 : un critique sur une cible à PV pleins compte double, pas sur une cible entamée");

        milestones.OnShieldBroken();
        FieldInfo knock = typeof(Enemy).GetField("_knockVelocity", Private);
        Check(((Vector2)knock.GetValue(pushed)).X > 0f && ((Vector2)knock.GetValue(control)) == Vector2.Zero,
            "Écusson de pompier palier 15 : le bouclier cassé repousse les ennemis proches, pas les lointains");
        Check(Near(_player.Mobility.DistanceMultiplier, 1.3f), "Lacet rouge palier 15 : dash 30 % plus long");
        foreach (Enemy enemy in new[] { control, echoed, burst, pushed })
            enemy.QueueFree();
    }

    private void CheckSurvivalMilestones()
    {
        Setup();
        _player.DisableDefenseForTests();
        foreach (string id in new[] { "ancrage", "regeneration", "peau_dure", "siphon_essence" })
            Raise(id, 15);
        ObjectMilestones milestones = _player.ObjectMilestones;
        float hp = _player.CurrentHp;
        float small = _player.EffectiveMaxHp * 0.03f - 0.5f;
        _player.TakeDamage(small);
        bool ignored = Near(_player.CurrentHp, hp);
        _player.TakeDamage(small + 1f);
        Check(ignored && _player.CurrentHp < hp, $"Bouton de manteau palier 15 : un coup sous 3 % des PV max ({small:0.0}) est ignoré, pas au-dessus");

        Check(Near(milestones.RegenMultiplier, 2f), "Bobine de fil palier 15 : régénération doublée après la blessure");
        typeof(ObjectMilestones).GetMethod("_Process").Invoke(milestones, new object[] { 3.1 });
        Check(Near(milestones.RegenMultiplier, 1f), "Bobine de fil palier 15 : retour à la normale après 3 s");

        PlayerMobility mobility = _player.Mobility;
        // Le coup encaissé plus haut laisse le joueur sonné : il s'en remet avant de dasher.
        mobility.Step(1f, Vector2.Zero, 200f, 1f, true);
        bool calm = Near(milestones.ArmorMultiplier(mobility), 1f);
        mobility.Request();
        mobility.Step(1f / 60f, Vector2.Right, 200f, 1f, true);
        bool dashing = mobility.IsDashing && Near(milestones.ArmorMultiplier(mobility), 2f);
        for (int i = 0; i < 30; i++)
            mobility.Step(1f / 60f, Vector2.Zero, 200f, 1f, true);
        bool after = !mobility.IsDashing && Near(milestones.ArmorMultiplier(mobility), 2f);
        for (int i = 0; i < 90; i++)
            mobility.Step(1f / 60f, Vector2.Zero, 200f, 1f, true);
        Check(calm && dashing && after && Near(milestones.ArmorMultiplier(mobility), 1f),
            "Genouillère palier 15 : armure doublée pendant le dash et la seconde qui suit");

        hp = _player.CurrentHp;
        _player.OnXpOrbCollected();
        Check(Near(_player.CurrentHp - hp, 0.2f), "Aimant de frigo palier 15 : une orbe ramassée rend 0,2 PV");
    }

    private void CheckRewardMilestones()
    {
        Setup();
        EssenceTracker essence = new() { Name = "EssenceTracker" };
        AddChild(essence);
        foreach (string id in new[] { "photo_de_classe", "jeton_de_fete" })
            Raise(id, 15);
        ObjectMilestones milestones = _player.ObjectMilestones;
        int before = essence.CurrentEssence;
        typeof(ObjectMilestones).GetMethod("OnLevelUp", Private).Invoke(milestones, new object[] { 26 });
        Check(essence.CurrentEssence - before == 3, "Photo de classe palier 15 : un niveau gagné donne 3 Essence");
        Check(milestones.GrantsRerollAt(15) && !milestones.GrantsRerollAt(16) && milestones.GrantsRerollAt(30),
            "Jeton de fête foraine palier 15 : une relance aux niveaux 15, 30…");
        // Retiré de l'arbre tout de suite : il ne doit plus écouter les butins des contrôles suivants.
        RemoveChild(essence);
        essence.QueueFree();

        bool allCoded = true;
        foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
            foreach (ObjectMilestoneData milestone in data.Milestones)
                allCoded &= ObjectMilestoneEffects.IsImplemented(milestone.Effect);
        Check(allCoded, "Les paliers des 31 objets proposés sont tous codés");
    }

    private static readonly FieldInfo IgniteTimer = typeof(Enemy).GetField("_igniteTimer", Private);
    private static readonly FieldInfo IgniteDps = typeof(Enemy).GetField("_igniteDps", Private);
    private static readonly FieldInfo SlowTimer = typeof(Enemy).GetField("_slowTimer", Private);
    private static readonly FieldInfo SlowFactor = typeof(Enemy).GetField("_slowFactor", Private);

    private void ClearStatuses(Enemy enemy)
    {
        IgniteTimer.SetValue(enemy, 0f);
        SlowTimer.SetValue(enemy, 0f);
        SlowFactor.SetValue(enemy, 1f);
    }

    /// <summary>Part des impacts d'une arme qui enflamment, sur <paramref name="trials"/> coups directs.</summary>
    private float BurnShare(Enemy enemy, WeaponInstance weapon, int trials, DamageKind kind = DamageKind.DirectWeapon)
    {
        int burns = 0;
        for (int i = 0; i < trials; i++)
        {
            ClearStatuses(enemy);
            _player.OnProjectileHit(enemy, 10f, false, weapon, _player.BeginAttack(weapon, 10f, kind));
            burns += enemy.IsBurning ? 1 : 0;
        }
        return burns / (float)trials;
    }

    private void CheckImpactTriggers()
    {
        Setup();
        Raise("allumette_humide", 30);
        _player.ObjectTriggers.Rng.Seed = 11;
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        _player.AddWeapon(WeaponDataLoader.Get("sling"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        WeaponInstance sling = _player.WeaponSlots[2];
        Enemy enemy = SpawnEnemy();
        float heavy = BurnShare(enemy, hammer, 2000);
        float light = BurnShare(enemy, sling, 2000);
        float passive = BurnShare(enemy, hammer, 300, DamageKind.Passive);
        Check(Near(_player.ObjectTriggers.BurnChance, 0.9f) && heavy > 0.87f && heavy < 0.93f && light > 0.37f && light < 0.44f && passive == 0f,
            $"Allumette humide niveau 30 : 90 % d'enflammer ; Marteau (coefficient 1) {heavy:P0}, Fronde (0,45) {light:P0}, effet déclenché 0 %");

        ClearStatuses(enemy);
        while (!enemy.IsBurning)
            _player.OnProjectileHit(enemy, 10f, false, hammer, _player.BeginAttack(hammer, 10f));
        float hammerHit = (float)typeof(Player).GetMethod("ComputeBaseAttackDamage", Private, new[] { typeof(WeaponInstance) }).Invoke(_player, new object[] { hammer });
        Check(Near((float)IgniteDps.GetValue(enemy), hammerHit * 0.25f) && Near((float)IgniteTimer.GetValue(enemy), 3f),
            $"Brûlure : 25 % du coup de base de l'arme par seconde ({hammerHit * 0.25f:0.0}), pendant 3 s");
        enemy.QueueFree();

        Setup();
        Raise("glacon", 15);
        _player.ObjectTriggers.Rng.Seed = 5;
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        hammer = _player.WeaponSlots[1];
        enemy = SpawnEnemy();
        ClearStatuses(enemy);
        while (!enemy.IsSlowed)
            _player.OnProjectileHit(enemy, 1f, false, hammer, _player.BeginAttack(hammer, 1f));
        bool chilled = Near((float)SlowFactor.GetValue(enemy), 0.6f) && Near((float)SlowTimer.GetValue(enemy), 1.5f);
        FieldInfo freeze = typeof(Enemy).GetField("_freezeTimer", Private);
        while ((float)freeze.GetValue(enemy) <= 0f)
            _player.OnProjectileHit(enemy, 1f, false, hammer, _player.BeginAttack(hammer, 1f));
        Check(chilled && Near((float)freeze.GetValue(enemy), 0.5f) && Near((float)SlowFactor.GetValue(enemy), 0.6f),
            "Glaçon : ralentit de 40 % pendant 1,5 s ; au palier 15, un ennemi ralenti deux fois est figé 0,5 s, sans toucher au ralentissement");
        enemy.QueueFree();
    }

    private void CheckTargetBonuses()
    {
        Setup();
        Raise("thermometre", 1);
        Raise("epingle_a_nourrice", 1);
        Enemy enemy = SpawnEnemy();
        float plain = _player.ResolveHitDamage(enemy, 10f, false);
        enemy.ApplyIgnite(1f, 5f);
        float burning = _player.ResolveHitDamage(enemy, 10f, false);
        enemy.ApplySlow(0.5f, 5f);
        float both = _player.ResolveHitDamage(enemy, 10f, false);
        Check(Near(plain, 10f) && Near(burning, 10.4f) && Near(both, 10.8f),
            "Thermomètre et Épingle à nourrice niveau 1 : +4 % contre une cible brûlée, autant contre une cible ralentie");

        Raise("allumette_humide", 1);
        _player.AddOrUpgradePassive("thermometre", 14);
        _player.ObjectTriggers.Rng.Seed = 2;
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        ClearStatuses(enemy);
        while (!enemy.IsBurning)
            _player.OnProjectileHit(enemy, 1f, false, hammer, _player.BeginAttack(hammer, 1f));
        Check(Near((float)SlowFactor.GetValue(enemy), 0.85f), "Thermomètre palier 15 : un ennemi enflammé est aussi ralenti de 15 %");
        enemy.QueueFree();
    }

    private void CheckKillTransmissions()
    {
        Setup();
        // Loin des ennemis des contrôles précédents, encore dans l'arbre jusqu'à la fin de la frame.
        Enemy victim = SpawnEnemy();
        victim.Position = new Vector2(3000f, 3000f);
        Enemy neighbour = SpawnEnemy();
        neighbour.Position = victim.Position + new Vector2(40f, 0f);
        Enemy slowedNeighbour = SpawnEnemy();
        slowedNeighbour.Position = victim.Position + new Vector2(0f, 30f);
        typeof(GroupCache).GetField("_enemiesFrame", Private).SetValue(GetNode<GroupCache>("/root/GroupCache"), ulong.MaxValue);
        Raise("allumette_humide", 15);
        Raise("epingle_a_nourrice", 15);
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        AttackContext attack = _player.BeginAttack(hammer, 1f);
        victim.ApplyIgnite(3f, 2f, attack);
        victim.ApplySlow(0.5f, 2f, attack);
        slowedNeighbour.ApplySlow(0.5f, 1f, attack);
        victim.TakeDamage(100000f, source: attack);
        Check(neighbour.IsBurning && Near((float)IgniteDps.GetValue(neighbour), 3f),
            "Allumette humide palier 15 : la Brûlure d'un ennemi tué passe à son plus proche voisin");
        bool extended = Near((float)SlowTimer.GetValue(slowedNeighbour), 2f);
        for (int i = 0; i < 5; i++)
            slowedNeighbour.ExtendSlow(1f, 4f);
        Check(extended && Near((float)SlowTimer.GetValue(slowedNeighbour), 4f),
            "Épingle à nourrice palier 15 : un ennemi ralenti tué prolonge de 1 s le ralentissement de ses voisins, 4 s restantes au plus");
        foreach (Enemy enemy in new[] { victim, neighbour, slowedNeighbour })
            enemy.QueueFree();
    }

    private void CheckFragility()
    {
        Setup();
        Enemy control = SpawnEnemy();
        Enemy fragile = SpawnEnemy();
        float before = Hp(control);
        control.TakeDamage(10f);
        float plain = before - Hp(control);
        fragile.ApplyFragile(0.2f, 2f);
        fragile.ApplyFragile(0.1f, 1f);
        before = Hp(fragile);
        fragile.TakeDamage(10f);
        bool stronger = Near(before - Hp(fragile), plain * 1.2f);
        typeof(Enemy).GetMethod("ProcessFragility", Private).Invoke(fragile, new object[] { 2.1f });
        before = Hp(fragile);
        fragile.TakeDamage(10f);
        Check(stronger && Near(before - Hp(fragile), plain),
            "Fragilité : +20 % de dégâts subis, la plus forte intensité garde la main, puis expire");
        control.QueueFree();
        fragile.QueueFree();
    }

    private void CheckKillRewards()
    {
        Setup();
        Enemy victim = SpawnEnemy();
        victim.Position = new Vector2(5000f, 5000f);
        Enemy neighbour = SpawnEnemy();
        neighbour.Position = victim.Position + new Vector2(30f, 0f);
        Enemy elite = SpawnEnemy();
        elite.Position = new Vector2(-5000f, 5000f);
        elite.ApplyVariant(EnemyVariantDataLoader.GetVariant("elite"), System.Array.Empty<EnemyAffixData>());
        typeof(GroupCache).GetField("_enemiesFrame", Private).SetValue(GetNode<GroupCache>("/root/GroupCache"), ulong.MaxValue);
        Raise("petard_mouille", 15);
        Raise("de_a_coudre", 15);
        _player.DisableDefenseForTests();
        _player.TakeDamage(40f);
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        WeaponInstance hammer = _player.WeaponSlots[1];

        // Le voisin encaisse les deux explosions sans mourir.
        typeof(Enemy).GetField("_currentHp", Private).SetValue(neighbour, 1e7f);
        float neighbourHp = Hp(neighbour);
        float playerHp = _player.CurrentHp;
        DamageResult fatal = victim.TakeDamage(100000f, source: _player.BeginAttack(hammer, 100000f));
        float firstBlast = neighbourHp - Hp(neighbour);
        float healed = _player.CurrentHp - playerHp;
        typeof(ObjectTriggers).GetMethod("_Process").Invoke(_player.ObjectTriggers, new object[] { 0.3 });
        float secondBlast = neighbourHp - Hp(neighbour) - firstBlast;
        Check(firstBlast > 0f && Mathf.Abs(firstBlast - secondBlast) < 1f && Mathf.Abs(firstBlast - fatal.NativeDamage * 1.5f) < 1f,
            $"Pétard mouillé : la victime explose ({firstBlast:0} PV au voisin, 150 % du coup fatal au niveau 15), deux fois au palier 15");
        Check(Near(healed, 1.5f), $"Dé à coudre niveau 15 : une élimination rend 1,5 PV ({healed:0.00})");

        playerHp = _player.CurrentHp;
        elite.TakeDamage(100000f, source: _player.BeginAttack(hammer, 100000f));
        Check(Near(_player.CurrentHp - playerHp, 1.5f + _player.EffectiveMaxHp * 0.05f), "Dé à coudre palier 15 : une élite tuée rend en plus 5 % des PV max");

        playerHp = _player.CurrentHp;
        Enemy byEffect = SpawnEnemy();
        byEffect.TakeDamage(100000f, source: _player.BeginAttack(hammer, 1f, DamageKind.Passive));
        Check(Near(_player.CurrentHp, playerHp), "Une élimination par un effet déclenché ne déclenche rien");
        foreach (Enemy enemy in new[] { victim, neighbour, elite, byEffect })
            enemy.QueueFree();
    }

    private void CheckStrideAndLevels()
    {
        Setup();
        Raise("semelle_usee", 1);
        ObjectTriggers triggers = _player.ObjectTriggers;
        MethodInfo process = typeof(ObjectTriggers).GetMethod("_Process");
        _player.Velocity = new Vector2(150f, 0f);
        process.Invoke(triggers, new object[] { 1.0 });
        bool notYet = triggers.StrideCharges == 0;
        process.Invoke(triggers, new object[] { 1.1 });
        process.Invoke(triggers, new object[] { 3.0 });
        float charged = triggers.ConsumeStride();
        float spent = triggers.ConsumeStride();
        process.Invoke(triggers, new object[] { 0.5 });
        bool noRefill = triggers.StrideCharges == 0;
        Check(notYet && Near(charged, 1.06f) && Near(spent, 1f) && noRefill,
            "Semelle usée niveau 1 : après 2 s de marche, la prochaine attaque fait +6 %, une seule fois ; la marche repart de zéro");
        _player.Velocity = Vector2.Zero;
        process.Invoke(triggers, new object[] { 1.5 });
        _player.Velocity = new Vector2(150f, 0f);
        process.Invoke(triggers, new object[] { 1.5 });
        bool reset = triggers.StrideCharges == 0;
        _player.AddOrUpgradePassive("semelle_usee", 14);
        process.Invoke(triggers, new object[] { 2.1 });
        Check(reset && triggers.StrideCharges == 2, "Semelle usée : un arrêt remet la marche à zéro ; au palier 15, deux attaques chargées");
        _player.Velocity = Vector2.Zero;

        Setup();
        _player.DisableDefenseForTests();
        Raise("boite_de_pansements", 15);
        _player.TakeDamage(50f);
        float hp = _player.CurrentHp;
        MethodInfo levelUp = typeof(ObjectTriggers).GetMethod("OnLevelUp", Private);
        levelUp.Invoke(_player.ObjectTriggers, new object[] { 30 });
        float heal = _player.CurrentHp - hp;
        PlayerDefense defense = (PlayerDefense)typeof(Player).GetField("_defense", Private).GetValue(_player);
        bool notYetInvulnerable = !defense.IsInvulnerable;
        levelUp.Invoke(_player.ObjectTriggers, new object[] { 31 });
        levelUp.Invoke(_player.ObjectTriggers, new object[] { 32 });
        Check(Near(heal, _player.EffectiveMaxHp * 0.045f) && notYetInvulnerable && defense.IsInvulnerable,
            "Boîte de pansements niveau 15 : un niveau soigne 4,5 % des PV max ; trois niveaux d'un coup rendent invulnérable");
    }

    private void CheckNamedProperties()
    {
        Check(StatCatalog.Property("attack_speed") == "frequency" && StatCatalog.Property("max_hp") == null
            && StatCatalog.Property("projectile_pierce") == "count",
            "Propriétés : chaque stat d'arme a la sienne, la survie n'en a pas");

        WeaponInstance bow = new(WeaponDataLoader.Get("makeshift_bow"));
        WeaponInstance bell = new(WeaponDataLoader.Get("teachers_bell"));
        WeaponInstance musicBox = new(WeaponDataLoader.Get("music_box"));
        WeaponInstance broadcast = new(WeaponDataLoader.Get("last_broadcast"));
        WeaponInstance chain = new(WeaponDataLoader.Get("chain_of_names"));
        bool rules = !WeaponProperties.Concerns(bow, "size") && WeaponProperties.Concerns(bow, "count")
            && WeaponProperties.Concerns(bell, "size") && WeaponProperties.Concerns(bell, "duration")
            && !WeaponProperties.Concerns(musicBox, "frequency") && WeaponProperties.Concerns(musicBox, "range") && WeaponProperties.Concerns(musicBox, "size")
            && !WeaponProperties.Concerns(bow, "duration") && WeaponProperties.Concerns(bow, "duration", objectStatuses: true)
            && WeaponProperties.Concerns(broadcast, "size") && !WeaponProperties.Concerns(broadcast, "count")
            && !WeaponProperties.Concerns(chain, "count") && WeaponProperties.Concerns(chain, "precision");
        Check(rules, "Armes concernées : Taille pour la mêlée en zone et le cône, Nombre pour tirs et frappes, rien d'autre que Portée et Force pour l'orbite");

        Setup();
        FragmentOption washer = new("resonance", "passive_new", "Rondelle de cuivre", 1);
        List<WeaponInstance> alone = UpgradeText.ConcernedWeapons(washer, _player);
        _player.AddWeapon(WeaponDataLoader.Get("chipped_blade"));
        List<WeaponInstance> withBlade = UpgradeText.ConcernedWeapons(washer, _player);
        List<WeaponInstance> survival = UpgradeText.ConcernedWeapons(new FragmentOption("ancrage", "passive_new", "Bouton de manteau", 1), _player);
        string card = CardText(washer);
        Check(alone is { Count: 0 } && withBlade is { Count: 1 } && withBlade[0].Id == "chipped_blade" && survival == null
            && !card.Contains("Pour :") && !card.Contains("Aucune de tes armes") && !card.Contains("Taille ·"),
            $"Carte d'objet : ni propriété ni armes sur la carte, armes concernées pour l'inventaire ({card})");

        // Une amélioration d'arme à trois stats tient en deux lignes : la première en valeur, la seconde regroupe.
        Setup();
        WeaponInstance equipped = _player.EquippedWeapon;
        FragmentOption legendary = new FragmentOption(equipped.Id, "weapon_upgrade", equipped.Name, 1)
            .WithWeaponUpgrade(UpgradeRoller.Get("legendary"), UpgradeRoller.RollWeaponGains(equipped, UpgradeRoller.Get("legendary"), new RandomNumberGenerator { Seed = 5 }));
        List<(string, Color)> lines = UpgradeText.Describe(legendary, _player);
        bool fits = lines.Count <= 2;
        foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
            fits &= UpgradeText.Describe(new FragmentOption(data.Id, "passive_new", data.Name, 1), _player).Count <= 2;
        Check(fits && lines.Count == 2 && lines[1].Item1.StartsWith("et "),
            $"Cartes : deux lignes de gain au plus ({string.Join(" | ", lines.ConvertAll(line => line.Item1))})");
    }

    /// <summary>Loin des ennemis des contrôles précédents, et la liste du GroupCache relue.</summary>
    private Enemy SpawnAt(Vector2 position)
    {
        Enemy enemy = SpawnEnemy();
        enemy.Position = position;
        typeof(GroupCache).GetField("_enemiesFrame", Private).SetValue(GetNode<GroupCache>("/root/GroupCache"), ulong.MaxValue);
        return enemy;
    }

    private static readonly FieldInfo FragileBonus = typeof(Enemy).GetField("_fragileBonus", Private);
    private static readonly FieldInfo FragileTimer = typeof(Enemy).GetField("_fragileTimer", Private);

    private void CheckCritTriggers()
    {
        Setup();
        Vector2 origin = new(9000f, 9000f);
        Raise("loupe_de_philateliste", 5);
        Raise("stylo_quatre_couleurs", 10);
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        Enemy target = SpawnAt(origin);
        Enemy near = SpawnAt(origin + new Vector2(50f, 0f));
        Enemy second = SpawnAt(origin + new Vector2(0f, 60f));
        Enemy far = SpawnAt(origin + new Vector2(600f, 0f));
        float nearHp = Hp(near), secondHp = Hp(second), farHp = Hp(far);
        _player.OnProjectileHit(target, 10f, false, hammer, _player.BeginAttack(hammer, 10f));
        bool noCrit = (float)FragileTimer.GetValue(target) <= 0f && Near(Hp(near), nearHp);
        _player.OnProjectileHit(target, 10f, true, hammer, _player.BeginAttack(hammer, 10f));
        bool fragile = Near((float)FragileBonus.GetValue(target), 0.25f) && Near((float)FragileTimer.GetValue(target), 2f);
        bool echoed = Near(nearHp - Hp(near), 6f) && Near(Hp(second), secondHp) && Near(Hp(far), farHp);
        Check(noCrit && fragile, "Loupe de philatéliste niveau 5 : un critique rend la cible Fragile (+25 %) 2 s ; un coup normal, rien");
        Check(echoed, $"Stylo à quatre couleurs niveau 10 : 60 % d'un critique repartent sur la cible la plus proche ({nearHp - Hp(near):0.0} PV), pas au-delà de 180 px");

        _player.AddOrUpgradePassive("loupe_de_philateliste", 10);
        _player.AddOrUpgradePassive("stylo_quatre_couleurs", 5);
        nearHp = Hp(near);
        secondHp = Hp(second);
        _player.OnProjectileHit(target, 10f, true, hammer, _player.BeginAttack(hammer, 10f));
        Check(Near((float)FragileTimer.GetValue(target), 4f) && nearHp > Hp(near) && secondHp > Hp(second),
            "Paliers 15 : Fragile dure 4 s ; le critique repart sur deux cibles");
        foreach (Enemy enemy in new[] { target, near, second, far })
            enemy.QueueFree();
    }

    private void CheckDashTrail()
    {
        Setup();
        Vector2 origin = new(12000f, 12000f);
        _player.GlobalPosition = origin;
        Raise("chewing_gum", 10);
        ObjectTriggers triggers = _player.ObjectTriggers;
        MethodInfo process = typeof(ObjectTriggers).GetMethod("_Process");
        System.Collections.IList patches = (System.Collections.IList)typeof(ObjectTriggers).GetField("_trailPatches", Private).GetValue(triggers);
        Enemy stuck = SpawnAt(origin + new Vector2(10f, 0f));
        PlayerMobility mobility = _player.Mobility;
        mobility.Request();
        mobility.Step(1f / 60f, Vector2.Right, 200f, 1f, true);
        bool dashing = mobility.IsDashing;
        process.Invoke(triggers, new object[] { 0.3 });
        bool laid = patches.Count >= 1;
        float slow = (float)SlowFactor.GetValue(stuck);
        for (int i = 0; i < 40; i++)
            mobility.Step(1f / 60f, Vector2.Zero, 200f, 1f, true);
        process.Invoke(triggers, new object[] { 2.1 });
        Check(dashing && laid && slow < 1f && patches.Count == 0,
            $"Chewing-gum niveau 10 : le dash pose une tache qui ralentit ({slow:0.00}), elle s'efface après 1 + 1 s");
        stuck.QueueFree();
    }

    /// <summary>
    /// Bonus de stat des coffres (DECISIONS §38, plan 23 R8) : tiré dans la table, jamais de +dégâts universel ;
    /// un niveau d'objet commun pour un coffre commun, le triple pour un épique, appliqué une fois.
    /// </summary>
    private void CheckChestStatBonus()
    {
        Setup();
        ChestStatBonusData table = ChestDataLoader.LoadStatBonus();
        HashSet<string> allowed = new();
        foreach (ChestStatBonus entry in table.Stats)
            allowed.Add(entry.Stat);
        HashSet<string> drawn = new();
        bool inTable = true;
        for (int i = 0; i < 400; i++)
        {
            ResolvedLoot loot = LootRewards.RollStatBonus("common").Value;
            drawn.Add(loot.ItemId);
            inTable &= allowed.Contains(loot.ItemId) && loot.ItemId != "damage" && loot.Label.Length > 0;
        }
        Check(inTable && drawn.Count == allowed.Count, $"Coffre : stat tirée dans la table, sans +dégâts ({drawn.Count}/{allowed.Count} stats vues sur 400 tirages)");

        ResolvedLoot Draw(string rarity, string stat)
        {
            for (int i = 0; i < 2000; i++)
                if (LootRewards.RollStatBonus(rarity) is { } loot && loot.ItemId == stat)
                    return loot;
            throw new InvalidOperationException($"{stat} jamais tiré");
        }
        EventBus events = GetNode<EventBus>("/root/EventBus");
        float before = _player.AttackSpeedMultiplier;
        ResolvedLoot common = Draw("common", "attack_speed");
        LootRewards.Apply(common, _player, events, Vector2.Zero);
        float afterCommon = _player.AttackSpeedMultiplier;
        ResolvedLoot epic = Draw("epic", "attack_speed");
        LootRewards.Apply(epic, _player, events, Vector2.Zero);
        float armor = _player.Armor;
        LootRewards.Apply(Draw("rare", "armor"), _player, events, Vector2.Zero);
        Check(Mathf.IsEqualApprox(afterCommon / before, 1.08f) && Mathf.IsEqualApprox(_player.AttackSpeedMultiplier / afterCommon, 1.24f)
              && Mathf.IsEqualApprox(_player.Armor - armor, 4f),
            $"Coffre : cadence ×1,08 (commun, « {common.Label} ») puis ×1,24 (épique), armure +4 (rare)");
    }

    private void CheckStances()
    {
        Setup();
        Vector2 origin = new(15000f, 15000f);
        _player.GlobalPosition = origin;
        _player.DisableDefenseForTests();
        foreach (string id in new[] { "tabouret_de_camping", "gilet_reflechissant", "thermos", "medaille_cabossee", "porte_monnaie_use" })
            Raise(id, 10);
        ObjectStances stances = _player.ObjectStances;
        MethodInfo process = typeof(ObjectStances).GetMethod("_Process");
        float speed = _player.AttackSpeedMultiplier;
        _player.Velocity = Vector2.Zero;
        process.Invoke(stances, new object[] { 0.5 });
        bool notYet = !stances.IsStill;
        process.Invoke(stances, new object[] { 0.6 });
        bool still = stances.IsStill && Near(_player.AttackSpeedMultiplier / speed, 1.3f);
        _player.Velocity = new Vector2(150f, 0f);
        process.Invoke(stances, new object[] { 0.1 });
        Check(notYet && still && Near(_player.AttackSpeedMultiplier, speed),
            "Tabouret de camping niveau 10 : immobile 1 s, cadence +30 % ; en mouvement, rien");
        _player.Velocity = Vector2.Zero;

        // Pleine forme (Thermos +20 %), trois ennemis proches (Gilet +2 % chacun), pas d'Essence.
        Enemy[] crowd = { SpawnAt(origin + new Vector2(30f, 0f)), SpawnAt(origin + new Vector2(-40f, 0f)), SpawnAt(origin + new Vector2(0f, 20f)) };
        Enemy distant = SpawnAt(origin + new Vector2(400f, 0f));
        process.Invoke(stances, new object[] { 0.2 });
        Check(stances.CrowdCount == 3 && Near(stances.DamageMultiplier, 1f + 0.2f + 0.06f),
            $"Gilet réfléchissant et Thermos niveau 10 : trois ennemis à moins de 120 px, PV pleins : dégâts ×{stances.DamageMultiplier:0.00}");

        _player.TakeDamage(_player.EffectiveMaxHp * 0.7f);
        process.Invoke(stances, new object[] { 0.2 });
        Check(Near(stances.DamageMultiplier, 1f + 0.06f + 0.3f), $"Médaille cabossée niveau 10 : sous 35 % des PV, dégâts +30 % et plus de Thermos (×{stances.DamageMultiplier:0.00})");

        EventBus events = GetNode<EventBus>("/root/EventBus");
        EssenceTracker wallet = new() { Name = "EssenceTracker" };
        AddChild(wallet);
        wallet.AddEssence(200);
        float withEssence = stances.DamageMultiplier;
        wallet.AddEssence(4800);
        Check(Near(withEssence, 1.36f + 0.2f) && Near(stances.DamageMultiplier, 1.36f + 0.3f),
            "Porte-monnaie usé niveau 10 : +1 % par 10 Essence gardées, plafond 30 %");

        float armor = _player.Armor;
        Raise("porte_monnaie_use", 15);
        _player.AddOrUpgradePassive("tabouret_de_camping", 5);
        _player.AddOrUpgradePassive("medaille_cabossee", 5);
        int refunded = 0;
        EventBus.LootReceivedEventHandler onLoot = (type, source, amount) => refunded += source == ObjectStances.EssenceRefundEffect ? amount : 0;
        events.LootReceived += onLoot;
        wallet.TrySpend(1000);
        // Le remboursement part en différé, après la dépense : le banc le déclenche lui-même.
        typeof(ObjectStances).GetMethod("FlushRefund", Private).Invoke(stances, null);
        events.LootReceived -= onLoot;
        bool refundedToWallet = wallet.CurrentEssence == 4200;
        RemoveChild(wallet);
        wallet.QueueFree();
        process.Invoke(stances, new object[] { 1.1 });
        Check(refunded == 200 && refundedToWallet && _player.Armor - armor >= 9.99f && Near(_player.SpeedMultiplier, 1.15f),
            $"Paliers 15 : 20 % de l'Essence dépensée rendue ({refunded}), armure +10 immobile, vitesse +15 % sous le seuil");
        foreach (Enemy enemy in crowd)
            enemy.QueueFree();
        distant.QueueFree();
    }

    private static float Hp(Enemy enemy) => (float)typeof(Enemy).GetField("_currentHp", Private).GetValue(enemy);

    private string CardText(FragmentOption option)
    {
        List<string> lines = new();
        foreach ((string line, Color _) in UpgradeText.Describe(option, _player))
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
