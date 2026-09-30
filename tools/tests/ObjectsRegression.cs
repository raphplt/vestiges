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
            CheckCombatMilestones();
            CheckSurvivalMilestones();
            CheckRewardMilestones();
            CheckImpactTriggers();
            CheckTargetBonuses();
            CheckKillTransmissions();
            CheckFragility();
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
        Check(offered.Count == 18 && wellFormed && anySurvival, $"Catalogue : {offered.Count} objets proposés, niveau max 50, au moins un de survie");
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
        Check(fresh == 18 && noRetired, $"Offre de niveau : {fresh} objets neufs possibles, aucun objet retiré");

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
        _player.OnProjectileHit(enemy, 1f, false, bell);
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
            Raise(id, 25);
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
        Check(ninth && tenth && repeats.Count == 0, "Ressort de sommier palier 25 : la 10ᵉ attaque d'une arme repart, puis la file se vide");
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
            $"Rondelle de cuivre palier 25 : la zone refrappe après 0,25 s, à 30 % ({hpBefore - Hp(echoed):0.0} PV)");

        hpBefore = Hp(burst);
        _player.OnProjectileSpent(burst.GlobalPosition, 10f, _player.BeginAttack(bow, 10f));
        Check(Near(hpBefore - Hp(burst), fullLoss * 0.5f), $"Mètre pliant palier 25 : un projectile en bout de course éclate à 50 % ({hpBefore - Hp(burst):0.0} PV)");

        bool fullDouble = Near(_player.ResolveHitDamage(pushed, 10f, true), 20f);
        bool damagedSame = Near(_player.ResolveHitDamage(control, 10f, true), 10f);
        bool normal = Near(_player.ResolveHitDamage(pushed, 10f, false), 10f);
        Check(fullDouble && damagedSame && normal,
            "Lunettes de lecture palier 25 : un critique sur une cible à PV pleins compte double, pas sur une cible entamée");

        milestones.OnShieldBroken();
        FieldInfo knock = typeof(Enemy).GetField("_knockVelocity", Private);
        Check(((Vector2)knock.GetValue(pushed)).X > 0f && ((Vector2)knock.GetValue(control)) == Vector2.Zero,
            "Écusson de pompier palier 25 : le bouclier cassé repousse les ennemis proches, pas les lointains");
        Check(Near(_player.Mobility.DistanceMultiplier, 1.3f), "Lacet rouge palier 25 : dash 30 % plus long");
        foreach (Enemy enemy in new[] { control, echoed, burst, pushed })
            enemy.QueueFree();
    }

    private void CheckSurvivalMilestones()
    {
        Setup();
        _player.DisableDefenseForTests();
        foreach (string id in new[] { "ancrage", "regeneration", "peau_dure", "siphon_essence" })
            Raise(id, 25);
        ObjectMilestones milestones = _player.ObjectMilestones;
        float hp = _player.CurrentHp;
        float small = _player.EffectiveMaxHp * 0.03f - 0.5f;
        _player.TakeDamage(small);
        bool ignored = Near(_player.CurrentHp, hp);
        _player.TakeDamage(small + 1f);
        Check(ignored && _player.CurrentHp < hp, $"Bouton de manteau palier 25 : un coup sous 3 % des PV max ({small:0.0}) est ignoré, pas au-dessus");

        Check(Near(milestones.RegenMultiplier, 2f), "Bobine de fil palier 25 : régénération doublée après la blessure");
        typeof(ObjectMilestones).GetMethod("_Process").Invoke(milestones, new object[] { 3.1 });
        Check(Near(milestones.RegenMultiplier, 1f), "Bobine de fil palier 25 : retour à la normale après 3 s");

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
            "Genouillère palier 25 : armure doublée pendant le dash et la seconde qui suit");

        hp = _player.CurrentHp;
        _player.OnXpOrbCollected();
        Check(Near(_player.CurrentHp - hp, 0.2f), "Aimant de frigo palier 25 : une orbe ramassée rend 0,2 PV");
    }

    private void CheckRewardMilestones()
    {
        Setup();
        EssenceTracker essence = new() { Name = "EssenceTracker" };
        AddChild(essence);
        foreach (string id in new[] { "photo_de_classe", "jeton_de_fete" })
            Raise(id, 25);
        ObjectMilestones milestones = _player.ObjectMilestones;
        int before = essence.CurrentEssence;
        typeof(ObjectMilestones).GetMethod("OnLevelUp", Private).Invoke(milestones, new object[] { 26 });
        Check(essence.CurrentEssence - before == 3, "Photo de classe palier 25 : un niveau gagné donne 3 Essence");
        Check(milestones.GrantsRerollAt(15) && !milestones.GrantsRerollAt(16) && milestones.GrantsRerollAt(30),
            "Jeton de fête foraine palier 25 : une relance aux niveaux 15, 30…");
        essence.QueueFree();

        bool allCoded = true;
        foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
            foreach (ObjectMilestoneData milestone in data.Milestones)
                allCoded &= ObjectMilestoneEffects.IsImplemented(milestone.Effect);
        Check(allCoded, "Les paliers des 18 objets proposés sont tous codés");
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
        Raise("allumette_humide", 50);
        _player.ObjectTriggers.Rng.Seed = 11;
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        _player.AddWeapon(WeaponDataLoader.Get("sling"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        WeaponInstance sling = _player.WeaponSlots[2];
        Enemy enemy = SpawnEnemy();
        float heavy = BurnShare(enemy, hammer, 2000);
        float light = BurnShare(enemy, sling, 2000);
        float passive = BurnShare(enemy, hammer, 300, DamageKind.Passive);
        Check(Near(_player.ObjectTriggers.BurnChance, 0.26f) && heavy > 0.22f && heavy < 0.30f && light > 0.09f && light < 0.15f && passive == 0f,
            $"Allumette humide niveau 50 : 26 % d'enflammer ; Marteau (coefficient 1) {heavy:P0}, Fronde (0,45) {light:P0}, effet déclenché 0 %");

        ClearStatuses(enemy);
        while (!enemy.IsBurning)
            _player.OnProjectileHit(enemy, 10f, false, hammer, _player.BeginAttack(hammer, 10f));
        float hammerHit = (float)typeof(Player).GetMethod("ComputeBaseAttackDamage", Private, new[] { typeof(WeaponInstance) }).Invoke(_player, new object[] { hammer });
        Check(Near((float)IgniteDps.GetValue(enemy), hammerHit * 0.25f) && Near((float)IgniteTimer.GetValue(enemy), 3f),
            $"Brûlure : 25 % du coup de base de l'arme par seconde ({hammerHit * 0.25f:0.0}), pendant 3 s");
        enemy.QueueFree();

        Setup();
        Raise("glacon", 25);
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
            "Glaçon : ralentit de 40 % pendant 1,5 s ; au palier 25, un ennemi ralenti deux fois est figé 0,5 s, sans toucher au ralentissement");
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
        Check(Near(plain, 10f) && Near(burning, 11.08f) && Near(both, 12.16f),
            "Thermomètre et Épingle à nourrice niveau 1 : +10,8 % contre une cible brûlée, autant contre une cible ralentie");

        Raise("allumette_humide", 1);
        _player.AddOrUpgradePassive("thermometre", 24);
        _player.ObjectTriggers.Rng.Seed = 2;
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        ClearStatuses(enemy);
        while (!enemy.IsBurning)
            _player.OnProjectileHit(enemy, 1f, false, hammer, _player.BeginAttack(hammer, 1f));
        Check(Near((float)SlowFactor.GetValue(enemy), 0.85f), "Thermomètre palier 25 : un ennemi enflammé est aussi ralenti de 15 %");
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
        Raise("allumette_humide", 25);
        Raise("epingle_a_nourrice", 25);
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        AttackContext attack = _player.BeginAttack(hammer, 1f);
        victim.ApplyIgnite(3f, 2f, attack);
        victim.ApplySlow(0.5f, 2f, attack);
        slowedNeighbour.ApplySlow(0.5f, 1f, attack);
        victim.TakeDamage(100000f, source: attack);
        Check(neighbour.IsBurning && Near((float)IgniteDps.GetValue(neighbour), 3f),
            "Allumette humide palier 25 : la Brûlure d'un ennemi tué passe à son plus proche voisin");
        bool extended = Near((float)SlowTimer.GetValue(slowedNeighbour), 2f);
        for (int i = 0; i < 5; i++)
            slowedNeighbour.ExtendSlow(1f, 4f);
        Check(extended && Near((float)SlowTimer.GetValue(slowedNeighbour), 4f),
            "Épingle à nourrice palier 25 : un ennemi ralenti tué prolonge de 1 s le ralentissement de ses voisins, 4 s restantes au plus");
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

    private static float Hp(Enemy enemy) => (float)typeof(Enemy).GetField("_currentHp", Private).GetValue(enemy);

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
