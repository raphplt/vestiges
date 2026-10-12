using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Écussons et objets sur un vrai joueur : progression des Écussons, copies directes, raretés fixes,
/// déclencheurs probabilistes et budgets communs aux armes, sans bonus caché au doublon ;
/// projectiles en plus fractionnaires, Durée et renouvellement des statuts.
/// </summary>
public partial class ObjectsRegression : Node2D
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private int _failures;
    private Player _player;

    public override async void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            CheckCatalogue();
            CheckSlots();
            CheckLevels();
            CheckMultipleEffects();
            CheckNewStats();
            CheckPerilObject();
            CheckRarityGains();
            CheckOffers();
            CheckMilestoneActivation();
            CheckMilestoneCards();
            CheckExtraProjectiles();
            CheckStatusDuration();
            CheckStatusRenewal();
            CheckCombatMilestones();
            CheckSurvivalMilestones();
            CheckLifesteal();
            CheckObjectProcs();
            CheckMilestoneProcIcons();
            CheckRewardMilestones();
            CheckImpactTriggers();
            CheckSharedItemBudgets();
            CheckChillAndEliteBudgets();
            CheckPoisonDoses();
            CheckStatItemExchange();
            CheckCatalogueStatItems();
            CheckTargetBonuses();
            CheckKillTransmissions();
            CheckFragility();
            CheckKillRewards();
            CheckSecondaryHits();
            CheckWeaponKills();
            CheckGroundFireBurns();
            CheckFragileDamageOverTime();
            CheckHinderedTargets();
            CheckFrozenCreatures();
            CheckBurnStacking();
            CheckDamageOverTimeRules();
            CheckFrozenMiniboss();
            CheckPropagationOffer();
            CheckScalpelCounterReset();
            CheckSteadyDisorientation();
            CheckStrideAndLevels();
            CheckOrbitalStride();
            CheckNamedProperties();
            CheckCritTriggers();
            CheckDashTrail();
            CheckStances();
            CheckItemPresentation();
            await CheckItemPause();
            GD.Print($"[ObjectsRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GetTree().Paused = false;
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    private void CheckCatalogue()
    {
        List<PassiveSouvenirData> offered = PassiveSouvenirDataLoader.GetAll();
        bool wellFormed = true;
        bool anySurvival = false;
        int badges = 0;
        foreach (PassiveSouvenirData data in offered)
        {
            badges += data.IsBadge ? 1 : 0;
            wellFormed &= data.MaxLevel == (data.IsBadge ? 99 : 30) && data.Effects.Count > 0 && !string.IsNullOrEmpty(data.Name)
                && data.IsBadge == data.Name.StartsWith("Écusson", StringComparison.Ordinal) && data.IsBadge == (data.Subtitle.Length > 0);
            anySurvival |= data.Survival;
            foreach (ObjectMilestoneData milestone in data.Milestones)
                wellFormed &= milestone.Level == 15;
        }
        Check(offered.Count == 72 && badges == 16 && wellFormed && anySurvival,
            $"Catalogue : {offered.Count} fiches proposées, {badges} Écussons (niveau max 99) et objets (niveau max 30), paliers au niveau 15, au moins une de survie");
        bool retired = true;
        foreach (string id in new[] { "flamme_interieure", "fragment_deternite" })
            retired &= PassiveSouvenirDataLoader.Get(id) != null && !offered.Exists(data => data.Id == id);
        Check(retired, "Objets retirés : identifiants gardés, hors des offres");
        List<string> unresolved = new();
        foreach (PassiveSouvenirData data in offered)
            if (data.RuleText().Contains('{'))
                unresolved.Add(data.Id);
        string medal = PassiveSouvenirDataLoader.Get("medaille_cabossee").RuleText();
        Check(unresolved.Count == 0 && medal.Contains("35 %"),
            $"Règles des objets : chiffres remplis ({medal}), paramètres manquants : {string.Join(", ", unresolved)}");
    }

    private void CheckSlots()
    {
        Setup();
        int badges = 0;
        foreach (string id in new[] { "memoire_vive", "resonance", "portee_etendue", "oeil_critique", "ancrage" })
            badges += _player.AddOrUpgradePassive(id) ? 1 : 0;
        Check(InventoryConfig.BadgeSlots == 4 && badges == 4 && _player.BadgeSlots.Count == 4 && _player.PassiveSlots.Count == 0,
            "Quatre emplacements d'Écussons, à part des objets ; le cinquième Écusson est refusé");
        int items = 0;
        foreach (string id in new[] { "allumette_humide", "glacon", "thermometre", "epingle_a_nourrice", "petard_mouille", "de_a_coudre",
            "semelle_usee", "boite_de_pansements", "loupe_de_philateliste", "stylo_quatre_couleurs", "tabouret_de_camping" })
            items += _player.AddOrUpgradePassive(id) ? 1 : 0;
        Check(_player.BagCapacity == 10 && items == 10 && _player.BagCount == 10, "Sac de dix places ; le onzième objet est refusé");
        Check(_player.AddOrUpgradePassive("memoire_vive") && _player.GetPassiveLevel("memoire_vive") == 2
            && !_player.AddOrUpgradePassive("glacon") && _player.GetPassiveLevel("glacon") == 1,
            "Sac plein : une carte améliore un Écusson, mais une copie exige une place libre");
    }

    private void CheckLevels()
    {
        Setup();
        float baseSpeed = _player.AttackSpeedMultiplier;
        _player.AddOrUpgradePassive("memoire_vive");
        bool one = Near(_player.AttackSpeedMultiplier / baseSpeed, 1.08f);
        _player.AddOrUpgradePassive("memoire_vive", 4);
        bool five = Near(_player.AttackSpeedMultiplier / baseSpeed, 1.4f) && _player.GetPassiveLevel("memoire_vive") == 5;
        _player.AddOrUpgradePassive("memoire_vive", 25);
        bool thirty = Near(_player.AttackSpeedMultiplier / baseSpeed, 3.4f) && _player.GetPassiveLevel("memoire_vive") == 30;
        _player.AddOrUpgradePassive("memoire_vive");
        bool halfStep = Near(_player.AttackSpeedMultiplier / baseSpeed, 3.44f);
        _player.AddOrUpgradePassive("memoire_vive", 200);
        bool capped = _player.GetPassiveLevel("memoire_vive") == 99 && !_player.AddOrUpgradePassive("memoire_vive")
            && Near(_player.AttackSpeedMultiplier / baseSpeed, 6.16f);
        Check(one && five && thirty && halfStep && capped,
            "Écusson du batteur : +8 % de cadence par carte commune, +240 % au niveau 30, puis un demi-pas par niveau jusqu'au 99");

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

    /// <summary>Sifflet d'arbitre (plan 28 P3) : un point de Péril par niveau, les fractions des cartes rares cumulées.</summary>
    private void CheckPerilObject()
    {
        Setup();
        PerilManager peril = new() { Name = "PerilManager" };
        AddChild(peril);
        _player.AddOrUpgradePassive("sifflet_d_arbitre");
        int first = peril.Peril;
        _player.AddOrUpgradePassive("sifflet_d_arbitre", 2);
        int third = peril.Peril;
        _player.AddOrUpgradePassive("sifflet_d_arbitre", 1, 1.5f);
        int half = peril.Peril;
        _player.AddOrUpgradePassive("sifflet_d_arbitre", 1, 1.5f);
        int whole = peril.Peril;
        Check(first == 1 && third == 3 && half == 4 && whole == 5,
            $"Sifflet d'arbitre : Péril {first}, {third} après trois copies, puis {half} et {whole} : aucun gain de rareté appliqué aux copies");
        RemoveChild(peril);
        peril.QueueFree();
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
            FragmentOption option = UpgradeRoller.RollGains(new FragmentOption("resonance", "passive_upgrade", "Rondelle de cuivre"), _player, rarity, rng);
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
        // Profil neuf : seuls les objets de la réserve de départ sont offerts (plan 06 §9.2).
        int fresh = pool.FindAll(option => option.Type == "passive_new").Count;
        int unlocked = PassiveSouvenirDataLoader.GetAll().FindAll(item => item.IsBadge && MetaSaveManager.IsObjectUnlocked(item.Id)).Count;
        bool noItem = !pool.Exists(option => option.Type.StartsWith("passive", StringComparison.Ordinal) && !PassiveSouvenirDataLoader.Get(option.Id).IsBadge);
        Check(fresh == unlocked && fresh == 10 && noItem,
            $"Offre de niveau : {fresh} Écussons neufs possibles (réserve de départ), aucun objet (ils se trouvent dans le monde)");

        // Plus de paliers par niveau (DECISIONS §52) : dès le niveau 2, toutes les armes débloquées sont tirables.
        typeof(FragmentManager).GetField("_currentLevel", Private).SetValue(fragments, 2);
        pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);
        HashSet<int> tiers = new();
        HashSet<int> unlockedTiers = new();
        foreach (FragmentOption option in pool)
            if (option.Type == "weapon_new")
                tiers.Add(WeaponDataLoader.Get(option.Id).Tier);
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
            if (MetaSaveManager.IsWeaponUnlocked(weapon.Id) && weapon.Id != _player.WeaponSlots[0].Id)
                unlockedTiers.Add(weapon.Tier);
        Check(tiers.SetEquals(unlockedTiers) && tiers.Count > 1, $"Offre au niveau 2 : armes neuves de tous les paliers débloqués ({string.Join(", ", tiers)})");

        foreach (string id in new[] { "memoire_vive", "resonance", "portee_etendue", "oeil_critique" })
            _player.AddOrUpgradePassive(id);
        pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);
        _player.AddItemCopy("glacon");
        pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);
        Check(!pool.Exists(option => option.Type == "passive_new") && pool.FindAll(option => option.Type == "passive_upgrade").Count == 4
            && !pool.Exists(option => option.Id == "glacon"),
            "Quatre Écussons pris : plus d'Écusson neuf, quatre améliorations ; un objet porté ne s'offre jamais au level-up");
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
        MethodInfo hit = typeof(Projectile).GetMethod("Hit", Private);
        List<float> losses = new();
        for (int i = 0; i < 3; i++)
        {
            Enemy target = SpawnEnemy();
            target.Position = new Vector2(7000f + i * 40f, 7000f);
            float hp = Hp(target);
            hit.Invoke(shot, new object[] { target });
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
        FragmentOption upcoming = new FragmentOption("souffle_du_neant", "passive_upgrade", "Papier carbone")
            .WithPassiveUpgrade(UpgradeRoller.Get("legendary"));
        bool upcomingBadge = UpgradeText.ReachesMilestone(upcoming, _player);
        string upcomingText = CardText(upcoming);
        _player.AddOrUpgradePassive("souffle_du_neant");
        FragmentOption crossing = new FragmentOption("souffle_du_neant", "passive_upgrade", "Papier carbone")
            .WithPassiveUpgrade(UpgradeRoller.Get("common"));
        FragmentOption fresh = new("persistance", "passive_new", "Pince à linge");
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
                    coded &= !UpgradeText.ReachesMilestone(new FragmentOption(data.Id, "passive_upgrade", data.Name)
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
        // Les créatures viennent d'être placées hors de leur tick : l'index doit relire leurs positions.
        CrowdIndex.MarkMoved();
        float hpBefore = Hp(control);
        control.TakeDamage(10f);
        float fullLoss = hpBefore - Hp(control);

        // Six Écussons à la fois pour éprouver tous leurs paliers : la règle des quatre emplacements est vérifiée ailleurs.
        string inventory = Godot.FileAccess.GetFileAsString("res://data/progression/inventory.json");
        System.Text.Json.Nodes.JsonObject wide = System.Text.Json.Nodes.JsonNode.Parse(inventory)!.AsObject();
        wide["badge_slots"] = 8;
        Check(InventoryConfig.Apply(wide.ToJsonString()) == null, "réglages d'essai : huit Écussons");
        foreach (string id in new[] { "memoire_vive", "resonance", "portee_etendue", "oeil_critique", "carapace", "instinct" })
            Raise(id, 15);
        InventoryConfig.Apply(inventory);
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
        _player.OnProjectileSpent(burst.GlobalPosition, 10f, _player.BeginAttack(bow, 10f), FxFamily.Physical, Vector2.Right);
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

    /// <summary>Paille tordue (plan 21 G6b) : 0,5 % des dégâts rendus, plafond par seconde (defense.json), doublé sous la moitié des PV au palier.</summary>
    private void CheckLifesteal()
    {
        Setup();
        _player.DisableDefenseForTests();
        Raise("paille_tordue", 1);
        Enemy enemy = SpawnEnemy();
        CurrentHp.SetValue(enemy, 100000f);
        MethodInfo step = typeof(Player).GetMethod("StepLifesteal", Private);
        float maxHp = _player.EffectiveMaxHp;
        _player.TakeDamage(maxHp * 0.6f);
        float hp = _player.CurrentHp;
        AttackContext attack = _player.BeginAttack(_player.EquippedWeapon, 20f);
        enemy.TakeDamage(20f, source: attack);
        step.Invoke(_player, new object[] { 0.25f });
        float small = _player.CurrentHp - hp;
        hp = _player.CurrentHp;
        enemy.TakeDamage(5000f, source: _player.BeginAttack(_player.EquippedWeapon, 5000f));
        step.Invoke(_player, new object[] { 0.25f });
        float capped = _player.CurrentHp - hp;
        float cap = DefenseConfig.Load().LifestealMaxHpPerSecond;
        Check(Near(_player.Lifesteal, 0.005f) && Near(small, 0.1f) && Near(cap, 0.08f)
            && Near(capped, maxHp * cap * 0.25f),
            $"Vol de vie réel : 20 PV perdus rendent {small:0.00} ; plafond relevé à 8 % des PV max/s ({capped:0.00} en 0,25 s)");
        _player.AddOrUpgradePassive("paille_tordue", 14);
        hp = _player.CurrentHp;
        enemy.TakeDamage(5000f, source: _player.BeginAttack(_player.EquippedWeapon, 5000f));
        step.Invoke(_player, new object[] { 0.25f });
        Check(hp < maxHp * 0.5f && Near(_player.CurrentHp - hp, maxHp * cap * 2f * 0.25f),
            "Palier de l’Écusson du barbier conservé : plafond de vol de vie doublé sous la moitié des PV");
        // Aucun crédit supplémentaire provenant de la notification visuelle ou du surplus au-delà des PV.
        step.Invoke(_player, new object[] { 100f });
        _player.DisableDefenseForTests();
        _player.TakeDamage(maxHp * 0.4f);
        Enemy almostDead = SpawnAt(new Vector2(-70000f, 10000f));
        CurrentHp.SetValue(almostDead, 1f);
        hp = _player.CurrentHp;
        almostDead.TakeDamage(5000f, source: _player.BeginAttack(_player.EquippedWeapon, 5000f));
        _player.OnProjectileHit(almostDead, 5000f, false, _player.EquippedWeapon);
        step.Invoke(_player, new object[] { 1f });
        Check(Near(_player.CurrentHp - hp, _player.Lifesteal), "Vol de vie : un surkill de 5 000 sur 1 PV ne crédite que 1 PV infligé, sans double crédit");
        hp = _player.CurrentHp;
        enemy.TakeDamage(20f, source: attack.As(DamageKind.Passive));
        enemy.TakeDamage(20f, source: attack.As(DamageKind.DamageOverTime));
        step.Invoke(_player, new object[] { 1f });
        Check(Near(_player.CurrentHp, hp), "Dégâts passifs et tics : aucun vol de vie récursif");
        enemy.QueueFree();
        almostDead.QueueFree();
    }

    /// <summary>Plan 21 F4 : l'icône d'un objet porté s'élève quand il agit, une fois par seconde au plus ; jamais celle d'un objet absent.</summary>
    private void CheckObjectProcs()
    {
        Setup();
        Raise("petard_mouille", 1);
        ObjectProcs procs = _player.ObjectProcs;
        procs.Show("kill_explosion");
        int first = procs.ActiveCount;
        procs.Show("kill_explosion");
        int repeated = procs.ActiveCount;
        procs.Show("lifesteal");
        Check(first == 1 && repeated == 1 && procs.ActiveCount == 1,
            $"Icônes d'objets : Pétard affiché ({first}), pas répété dans la seconde ({repeated}), Paille absente ignorée ({procs.ActiveCount})");
    }

    /// <summary>Plan 27 V2d : les paliers du Mètre pliant et de l'Écusson font s'élever leur icône quand ils agissent.</summary>
    private void CheckMilestoneProcIcons()
    {
        Setup();
        Raise("portee_etendue", 15);
        Raise("carapace", 15);
        ObjectProcs procs = _player.ObjectProcs;
        int before = procs.ActiveCount;
        _player.OnProjectileSpent(_player.GlobalPosition + new Vector2(200f, 0f), 10f, default, FxFamily.Physical, Vector2.Right);
        int afterBurst = procs.ActiveCount;
        _player.ObjectMilestones.OnShieldBroken();
        Check(afterBurst == before + 1 && procs.ActiveCount == before + 2,
            $"Icônes des paliers : Mètre ({afterBurst - before}) puis Écusson ({procs.ActiveCount - afterBurst})");
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
        Check(allCoded, "Les paliers des 32 objets proposés sont tous codés");
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
            _player.ObjectTriggers.AdvanceClock(1f);
            ClearStatuses(enemy);
            _player.ObjectTriggers.OnWeaponImpact(enemy, 10f, weapon, 1, _player.BeginAttack(weapon, 10f, kind), dealt: 10f);
            burns += enemy.IsBurning ? 1 : 0;
        }
        return burns / (float)trials;
    }

    private void CheckImpactTriggers()
    {
        Setup();
        Raise("allumette_humide", 1);
        _player.ObjectTriggers.Rng.Seed = 11;
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        _player.AddWeapon(WeaponDataLoader.Get("sling"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        WeaponInstance sling = _player.WeaponSlots[2];
        Enemy enemy = SpawnEnemy();
        float heavy = BurnShare(enemy, hammer, 4000);
        float light = BurnShare(enemy, sling, 4000);
        float passive = BurnShare(enemy, hammer, 300, DamageKind.Passive);
        float secondary = BurnShare(enemy, hammer, 300, DamageKind.SecondaryWeapon);
        Check(Near(_player.ObjectTriggers.BurnChance, 0.08f) && heavy > 0.06f && heavy < 0.10f
            && light > 0.02f && light < 0.05f && passive == 0f && secondary == 0f,
            $"Allumette : 8 % × coefficient ; Marteau {heavy:P1}, Fronde {light:P1}, aucun impact passif ou secondaire");
        ClearStatuses(enemy);
        for (int i = 0; i < 500 && !enemy.IsBurning; i++)
        {
            _player.ObjectTriggers.AdvanceClock(1f);
            _player.ObjectTriggers.OnWeaponImpact(enemy, 40f, hammer, 1, _player.BeginAttack(hammer, 40f), dealt: 40f);
        }
        Check(enemy.IsBurning && Near((float)IgniteDps.GetValue(enemy), 10f) && Near((float)IgniteTimer.GetValue(enemy), 3f),
            "Brûlure : 25 % du coup de référence par seconde pendant 3 s");
        enemy.QueueFree();

        Setup();
        Raise("glacon", 2);
        _player.ObjectTriggers.Rng.Seed = 5;
        WeaponInstance weapon = _player.WeaponSlots[0];
        enemy = SpawnEnemy();
        for (int i = 0; i < 500 && !enemy.IsSlowed; i++)
            _player.ObjectTriggers.OnWeaponImpact(enemy, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        float initial = (float)SlowTimer.GetValue(enemy);
        bool chilled = Near((float)SlowFactor.GetValue(enemy), 0.6f) && Near(initial, 1.5f);
        SlowTimer.SetValue(enemy, 0.5f);
        for (int i = 0; i < 500; i++)
            _player.ObjectTriggers.OnWeaponImpact(enemy, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        Check(chilled && Near((float)SlowTimer.GetValue(enemy), 0.5f) && Near((float)FreezeTimer.GetValue(enemy), 0f),
            "Glaçon : ralentit de 40 % pendant 1,5 s ; aucun gel ni renouvellement au doublon");
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
        Check(Near(plain, 10f) && Near(burning, 13.5f) && Near(both, 17f),
            "Thermomètre et Épingle : chacun +35 % par copie, selon le statut de la cible");
        _player.AddItemCopy("thermometre");
        _player.AddItemCopy("epingle_a_nourrice");
        Check(Near(_player.ResolveHitDamage(enemy, 10f, false), 24f), "Copies des bonus de cible : cumul additif, sans palier");
        ClearStatuses(enemy);
        Raise("allumette_humide", 1);
        WeaponInstance weapon = _player.WeaponSlots[0];
        for (int i = 0; i < 500 && !enemy.IsBurning; i++)
            _player.ObjectTriggers.OnWeaponImpact(enemy, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        Check(enemy.IsBurning && !enemy.IsSlowed, "Thermomètre doublonné : aucune lenteur ajoutée à la Brûlure");
        enemy.QueueFree();
    }

    private void CheckKillTransmissions()
    {
        Setup();
        Enemy victim = SpawnAt(new Vector2(3000f, 3000f));
        Enemy neighbour = SpawnAt(victim.Position + new Vector2(40f, 0f));
        Enemy slowedNeighbour = SpawnAt(victim.Position + new Vector2(0f, 30f));
        Raise("allumette_humide", 2);
        Raise("epingle_a_nourrice", 2);
        AttackContext attack = _player.BeginAttack(_player.WeaponSlots[0], 10f);
        victim.ApplyIgnite(3f, 2f, attack);
        victim.ApplySlow(0.5f, 2f, attack);
        slowedNeighbour.ApplySlow(0.5f, 1f, attack);
        victim.TakeDamage(100000f, source: attack);
        Check(!neighbour.IsBurning && Near((float)SlowTimer.GetValue(slowedNeighbour), 1f),
            "Copies ordinaires : aucune propagation de brûlure ni extension des ralentissements à la mort");
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
        _player.DisableDefenseForTests();
        _player.TakeDamage(50f);
        Raise("petard_mouille", 2);
        Raise("de_a_coudre", 2);
        Enemy victim = SpawnAt(new Vector2(5000f, 5000f));
        Enemy neighbour = SpawnAt(victim.Position + new Vector2(30f, 0f));
        CurrentHp.SetValue(victim, 1f);
        CurrentHp.SetValue(neighbour, 1000f);
        float hp = _player.CurrentHp;
        DamageResult fatal = victim.TakeDamage(10f, source: _player.BeginAttack(_player.WeaponSlots[0], 10f));
        float blast = 1000f - Hp(neighbour);
        _player.ObjectTriggers.AdvanceClock(0.3f);
        Check(Near(blast, fatal.NativeDamage * 2f) && Near(1000f - Hp(neighbour), blast),
            "Deux Pétards : une explosion à 200 % du coup fatal, aucune seconde explosion retardée");
        Check(Near(_player.CurrentHp - hp, 1f), "Dé : une élimination soigne 1 PV, les copies renforcent le débit seulement");
        float before = _player.CurrentHp;
        for (int i = 0; i < 100; i++)
        {
            Enemy killed = SpawnAt(new Vector2(-10000f - i * 500f, 5000f));
            killed.TakeDamage(100000f, source: _player.BeginAttack(_player.WeaponSlots[0], 1f));
            killed.QueueFree();
        }
        Check(_player.CurrentHp - before <= 2.001f, "Cent morts simultanées : soin limité au budget commun de deux copies");
        before = _player.CurrentHp;
        Enemy passive = SpawnEnemy();
        passive.TakeDamage(100000f, source: _player.BeginAttack(_player.WeaponSlots[0], 1f, DamageKind.Passive));
        Check(Near(_player.CurrentHp, before), "Mort passive : aucun soin du Dé");
        foreach (Enemy enemy in new[] { victim, neighbour, passive })
            enemy.QueueFree();
    }

    private void CheckStrideAndLevels()
    {
        Setup();
        Raise("semelle_usee", 2);
        ObjectTriggers triggers = _player.ObjectTriggers;
        _player.Velocity = new Vector2(150f, 0f);
        triggers.AdvanceClock(10f);
        Check(triggers.StrideCharges == 0, "Semelle : marcher ne charge aucune attaque");
        PublishTestDash();
        float charged = triggers.ConsumeStride();
        Check(Near(charged, 1.4f) && Near(triggers.ConsumeStride(), 1f),
            "Deux Semelles : une seule attaque +40 % après dash, pas deux charges");
        PublishTestDash();
        PublishTestDash();
        Check(triggers.StrideCharges == 1, "Dashes répétés : la Semelle garde au plus une charge");
        triggers.AdvanceClock(3.1f);
        Check(Near(triggers.ConsumeStride(), 1f), "Charge de Semelle expirée après trois secondes");
        PublishTestDash();
        _player.ForgetItemCopy("semelle_usee");
        _player.ForgetItemCopy("semelle_usee");
        Check(Near(triggers.ConsumeStride(), 1f), "Dernière Semelle oubliée : aucune charge résiduelle");

        Setup();
        _player.DisableDefenseForTests();
        Raise("boite_de_pansements", 2);
        _player.TakeDamage(50f);
        float hp = _player.CurrentHp;
        EventBus events = GetNode<EventBus>("/root/EventBus");
        for (int level = 2; level <= 4; level++)
            events.EmitSignal(EventBus.SignalName.LevelUp, level);
        PlayerDefense defense = (PlayerDefense)typeof(Player).GetField("_defense", Private).GetValue(_player);
        Check(Near(_player.CurrentHp - hp, _player.EffectiveMaxHp * 0.18f) && !defense.IsInvulnerable,
            "Deux Pansements : 6 % des PV max à chacun des trois niveaux, sans invulnérabilité");
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
        FragmentOption washer = new("resonance", "passive_new", "Rondelle de cuivre");
        string card = CardText(washer);
        Check(!card.Contains("Pour :") && !card.Contains("Aucune de tes armes") && !card.Contains("Taille ·"),
            $"Carte d'objet : ni propriété ni armes concernées, le jeu n'explicite pas les synergies ({card})");

        // Une amélioration d'arme à trois stats tient en deux lignes : la première en valeur, la seconde regroupe.
        Setup();
        WeaponInstance equipped = _player.EquippedWeapon;
        FragmentOption legendary = new FragmentOption(equipped.Id, "weapon_upgrade", equipped.Name)
            .WithWeaponUpgrade(UpgradeRoller.Get("legendary"), UpgradeRoller.RollWeaponGains(equipped, UpgradeRoller.Get("legendary"), new RandomNumberGenerator { Seed = 5 }));
        List<(string, Color)> lines = UpgradeText.Describe(legendary, _player);
        bool fits = lines.Count <= 2;
        foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
            fits &= UpgradeText.Describe(new FragmentOption(data.Id, "passive_new", data.Name), _player).Count <= 3;
        Check(fits && lines.Count == 2 && lines[1].Item1.StartsWith("et "),
            $"Cartes : deux lignes de gain au plus ({string.Join(" | ", lines.ConvertAll(line => line.Item1))})");
    }

    /// <summary>Loin des ennemis des contrôles précédents, et la liste du GroupCache relue.</summary>
    private Enemy SpawnAt(Vector2 position)
    {
        Enemy enemy = SpawnEnemy();
        enemy.Position = position;
        CrowdIndex.MarkMoved();
        return enemy;
    }

    private static readonly FieldInfo FragileBonus = typeof(Enemy).GetField("_fragileBonus", Private);
    private static readonly FieldInfo FragileTimer = typeof(Enemy).GetField("_fragileTimer", Private);

    private void CheckCritTriggers()
    {
        Setup();
        Raise("loupe_de_philateliste", 1);
        Raise("stylo_quatre_couleurs", 1);
        Enemy target = SpawnAt(new Vector2(9000f, 9000f));
        Enemy near = SpawnAt(target.Position + new Vector2(40f, 0f));
        Enemy second = SpawnAt(target.Position + new Vector2(0f, 80f));
        CurrentHp.SetValue(near, 1000f);
        CurrentHp.SetValue(second, 1000f);
        WeaponInstance weapon = _player.WeaponSlots[0];
        _player.OnProjectileHit(target, 10f, false, weapon, _player.BeginAttack(weapon, 10f));
        bool ordinary = Near((float)FragileTimer.GetValue(target), 0f) && Near(Hp(near), 1000f);
        _player.OnProjectileHit(target, 10f, true, weapon, _player.BeginAttack(weapon, 10f));
        Check(ordinary && Near((float)FragileBonus.GetValue(target), 0.5f) && Near((float)FragileTimer.GetValue(target), 2f)
            && Near(1000f - Hp(near), 6f) && Near(Hp(second), 1000f),
            "Critique : Fragile +50 % pendant 2 s ; écho de 60 % vers une seule cible");
        _player.AddItemCopy("loupe_de_philateliste");
        _player.AddItemCopy("stylo_quatre_couleurs");
        float before = Hp(near);
        _player.OnProjectileHit(target, 10f, true, weapon, _player.BeginAttack(weapon, 10f));
        Check(Near((float)FragileBonus.GetValue(target), 1f) && Near((float)FragileTimer.GetValue(target), 2f)
            && Near(before - Hp(near), 12f) && Near(Hp(second), 1000f),
            "Copies Loupe/Stylo : puissance doublée, durée et nombre de cibles inchangés");
        foreach (Enemy enemy in new[] { target, near, second })
            enemy.QueueFree();
    }

    private void CheckDashTrail()
    {
        Setup();
        _player.GlobalPosition = new Vector2(12000f, 12000f);
        Raise("chewing_gum", 1);
        ObjectTriggers triggers = _player.ObjectTriggers;
        Enemy stuck = SpawnAt(_player.Position + new Vector2(10f, 0f));
        EventBus events = GetNode<EventBus>("/root/EventBus");
        events.PublishDashMoved(new(_player.GetInstanceId(), _player.Position, _player.Position + new Vector2(100f, 0f), false));
        System.Collections.IList initialPatches = (System.Collections.IList)typeof(ObjectTriggers).GetField("_trailPatches", Private).GetValue(triggers);
        Check(initialPatches.Count == 0, "Chewing-gum acquis en cours de dash : aucun trajet inventé depuis l’origine du monde");
        PublishTestDash();
        triggers.AdvanceClock(0.1f);
        bool slowed = Near((float)SlowFactor.GetValue(stuck), 0.6f);
        System.Collections.IList patches = (System.Collections.IList)typeof(ObjectTriggers).GetField("_trailPatches", Private).GetValue(triggers);
        bool laid = patches.Count > 0;
        triggers.AdvanceClock(2.1f);
        Check(laid && slowed && patches.Count == 0 && Near((float)FragileBonus.GetValue(stuck), 0f),
            "Chewing-gum : traînée de deux secondes, ralentissement de 40 %, sans Fragilité");
        for (int i = 0; i < 7; i++)
            _player.AddItemCopy("chewing_gum");
        PublishTestDash();
        triggers.AdvanceClock(6.1f);
        Check(_player.FindObject("chewing_gum").Copies == 8 && patches.Count == 0,
            "Chewing-gum : huit copies acceptées ; la durée garde son plafond de sécurité à six secondes");
        stuck.QueueFree();
    }

    private void CheckStances()
    {
        Setup();
        _player.GlobalPosition = new Vector2(15000f, 15000f);
        _player.DisableDefenseForTests();
        Raise("tabouret_de_camping", 1);
        ObjectStances stances = _player.ObjectStances;
        float speed = _player.AttackSpeedMultiplier;
        float armor = _player.Armor;
        _player.Velocity = Vector2.Zero;
        stances._Process(1.0);
        float first = stances.DamageMultiplier;
        stances._Process(1.5);
        float half = stances.DamageMultiplier;
        stances._Process(2.5);
        Check(Near(first, 1.2f) && Near(half, 1.5f) && Near(stances.DamageMultiplier, 2f)
            && Near(_player.AttackSpeedMultiplier, speed) && Near(_player.Armor, armor),
            "Tabouret : dégâts +20/+50/+100 % après 1/2,5/5 s, cadence et armure inchangées");
        _player.Velocity = new Vector2(150f, 0f);
        stances._Process(0.1);
        bool reset = Near(stances.DamageMultiplier, 1f);
        _player.Velocity = Vector2.Zero;
        _player.AddItemCopy("tabouret_de_camping");
        stances._Process(2.5);
        Check(reset && Near(stances.DamageMultiplier, 2f), "Deux Tabourets : plafond +100 % atteint en 2,5 s, mouvement remet à zéro");
        PublishTestDash();
        Check(Near(stances.DamageMultiplier, 1f), "Le dash remet le Tabouret à zéro, saut instantané compris");
        _player.ForgetItemCopy("tabouret_de_camping");
        _player.ForgetItemCopy("tabouret_de_camping");
        foreach (string id in new[] { "gilet_reflechissant", "thermos", "medaille_cabossee", "porte_monnaie_use" })
            Raise(id, 1);
        Enemy[] crowd = { SpawnAt(_player.Position + new Vector2(30f, 0f)), SpawnAt(_player.Position + new Vector2(-40f, 0f)), SpawnAt(_player.Position + new Vector2(0f, 20f)) };
        stances._Process(0.2);
        Check(stances.CrowdCount == 3 && Near(stances.DamageMultiplier, 1.26f), "Gilet et Thermos : +6 % pour trois voisins, +20 % au-dessus de 90 % PV");
        _player.TakeDamage(_player.EffectiveMaxHp * 0.7f);
        stances._Process(0.2);
        Check(Near(stances.DamageMultiplier, 1.36f), "Médaille : +30 % sous 35 % PV ; Thermos éteint");
        EssenceTracker wallet = new() { Name = "EssenceTracker" };
        AddChild(wallet);
        wallet.AddEssence(200);
        float twoHundred = stances.DamageMultiplier;
        wallet.AddEssence(4800);
        Check(Near(twoHundred, 1.56f) && Near(stances.DamageMultiplier, 1.66f), "Porte-monnaie : +1 %/10 Essence, plafond +30 % par copie");
        _player.AddItemCopy("porte_monnaie_use");
        _player.AddItemCopy("medaille_cabossee");
        wallet.TrySpend(1000);
        stances._Process(1.1);
        Check(wallet.CurrentEssence == 4000 && Near(_player.Armor, armor) && Near(_player.SpeedMultiplier, 1f),
            "Doublons : aucun remboursement d’Essence, aucune armure ou vitesse supplémentaire");
        RemoveChild(wallet);
        wallet.QueueFree();
        foreach (Enemy enemy in crowd)
            enemy.QueueFree();
    }

    private void CheckOrbitalStride()
    {
        Setup();
        Raise("semelle_usee", 2);
        _player.AddWeapon(WeaponDataLoader.Get("music_box"));
        typeof(Player).GetField("_orbitalWeapon", Private).SetValue(_player, _player.WeaponSlots[1]);
        Enemy enemy = SpawnAt(new Vector2(-101000f, 15000f));
        CurrentHp.SetValue(enemy, 1000f);
        MethodInfo hit = typeof(Player).GetMethod("OrbitalHit", Private);
        hit.Invoke(_player, new object[] { enemy });
        float ordinary = 1000f - Hp(enemy);
        float before = Hp(enemy);
        PublishTestDash(blink: true);
        hit.Invoke(_player, new object[] { enemy });
        float boosted = before - Hp(enemy);
        before = Hp(enemy);
        hit.Invoke(_player, new object[] { enemy });
        Check(ordinary > 0f && Mathf.Abs(boosted - ordinary * 1.4f) < 0.01f && Near(before - Hp(enemy), ordinary),
            "Semelle et orbitale : le premier contact après dash reçoit +40 %, le contact suivant redevient normal");
        enemy.QueueFree();
    }

    private void PublishTestDash(bool blink = false)
    {
        EventBus events = GetNode<EventBus>("/root/EventBus");
        PlayerDashEvent dash = new(_player.GetInstanceId(), _player.GlobalPosition, _player.GlobalPosition + new Vector2(100f, 0f), blink);
        events.PublishDashStarted(dash);
        events.PublishDashMoved(dash);
        events.PublishDashFinished(dash);
    }

    private void CheckSharedItemBudgets()
    {
        Setup();
        Raise("allumette_humide", 1);
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        ObjectTriggers triggers = _player.ObjectTriggers;
        triggers.Rng.Seed = 17;
        Enemy enemy = SpawnEnemy();
        int burns = 0;
        for (int i = 0; i < 1000; i++)
        {
            ClearStatuses(enemy);
            WeaponInstance weapon = _player.WeaponSlots[i % 2];
            triggers.OnWeaponImpact(enemy, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
            burns += enemy.IsBurning ? 1 : 0;
        }
        Check(burns == 2, $"Budget Allumette partagé entre deux armes : {burns}/2 brûlures dans la même fenêtre");
        _player.ForgetItemCopy("allumette_humide");
        _player.AddItemCopy("allumette_humide");
        int afterExchange = 0;
        for (int i = 0; i < 500; i++)
        {
            ClearStatuses(enemy);
            WeaponInstance weapon = _player.WeaponSlots[0];
            triggers.OnWeaponImpact(enemy, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
            afterExchange += enemy.IsBurning ? 1 : 0;
        }
        Check(afterExchange == 0, "Oubli puis reprise : aucun rechargement du budget de déclenchement");
        triggers.AdvanceClock(1.01f);
        _player.AddItemCopy("allumette_humide");
        burns = 0;
        for (int i = 0; i < 1000; i++)
        {
            ClearStatuses(enemy);
            WeaponInstance weapon = _player.WeaponSlots[i % 2];
            triggers.OnWeaponImpact(enemy, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
            burns += enemy.IsBurning ? 1 : 0;
        }
        Check(burns == 4 && Near(triggers.BurnChance, 0.08f), "Deux Allumettes : débit 4/s, probabilité toujours 8 %");
        triggers.AdvanceClock(1.01f);
        AttackContext sameAttack = _player.BeginAttack(_player.WeaponSlots[0], 10f);
        burns = 0;
        for (int i = 0; i < 1000; i++)
        {
            ClearStatuses(enemy);
            triggers.OnWeaponImpact(enemy, 10f, _player.WeaponSlots[0], 1, sameAttack, dealt: 10f);
            burns += enemy.IsBurning ? 1 : 0;
        }
        Check(burns <= 1, "Même attaque et même cible : un seul jet, même après mille notifications");
        AttackContext[] longLived = new AttackContext[200];
        for (int i = 0; i < longLived.Length; i++)
        {
            longLived[i] = _player.BeginAttack(_player.WeaponSlots[0], 10f);
            triggers.OnWeaponImpact(enemy, 10f, _player.WeaponSlots[0], 1, longLived[i], dealt: 10f);
        }
        triggers.AdvanceClock(121f);
        burns = 0;
        foreach (AttackContext attack in longLived)
        {
            ClearStatuses(enemy);
            triggers.OnWeaponImpact(enemy, 10f, _player.WeaponSlots[0], 1, attack, dealt: 10f);
            burns += enemy.IsBurning ? 1 : 0;
        }
        Check(burns == 0, "Attaques persistantes : un jet par vie de cible, même après deux minutes");
        enemy.QueueFree();
    }

    private void CheckChillAndEliteBudgets()
    {
        Setup();
        Raise("glacon", 1);
        _player.ObjectTriggers.Rng.Seed = 203;
        Enemy[] targets = new Enemy[20];
        for (int i = 0; i < targets.Length; i++)
            targets[i] = SpawnAt(new Vector2(-90000f + i * 100f, 13000f));
        WeaponInstance weapon = _player.WeaponSlots[0];
        for (int i = 0; i < 2000; i++)
            _player.ObjectTriggers.OnWeaponImpact(targets[i % targets.Length], 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        int chilled = 0;
        Enemy first = null;
        foreach (Enemy target in targets)
            if (target.IsSlowed) { chilled++; first = target; }
        Check(chilled == 1, "Glaçon : une seule application/s même avec vingt cibles et deux mille impacts");
        _player.ObjectTriggers.AdvanceClock(1.6f);
        typeof(Enemy).GetMethod("ProcessSlowDecay", Private).Invoke(first, new object[] { 1.6f });
        for (int i = 0; i < 1000; i++)
            _player.ObjectTriggers.OnWeaponImpact(first, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        Check(!first.IsSlowed, "Glaçon : une cible libérée bénéficie d’une seconde de répit malgré les impacts");
        _player.ObjectTriggers.AdvanceClock(1.0f);
        for (int i = 0; i < 1000 && !first.IsSlowed; i++)
            _player.ObjectTriggers.OnWeaponImpact(first, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        Check(first.IsSlowed, "Glaçon : la cible redevient éligible après son répit");
        foreach (Enemy target in targets) target.QueueFree();

        Setup();
        Raise("montre_arretee", 1);
        _player.ObjectTriggers.Rng.Seed = 204;
        Enemy ordinary = SpawnAt(new Vector2(-96000f, 13000f));
        Enemy elite = SpawnAt(ordinary.Position + new Vector2(150f, 0f));
        elite.ApplyVariant(EnemyVariantDataLoader.GetVariant("elite"), Array.Empty<EnemyAffixData>());
        weapon = _player.WeaponSlots[0];
        for (int i = 0; i < 1000; i++)
        {
            _player.ObjectTriggers.OnWeaponImpact(ordinary, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
            _player.ObjectTriggers.OnWeaponImpact(elite, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        }
        Check(!ordinary.IsSlowed && elite.IsSlowed && Near((float)SlowFactor.GetValue(elite), 0.65f),
            "Montre : ralentissement de 35 % sur une élite, jamais sur une créature ordinaire");
        _player.ObjectTriggers.AdvanceClock(3f);
        typeof(Enemy).GetMethod("ProcessSlowDecay", Private).Invoke(elite, new object[] { 3f });
        _player.ForgetItemCopy("montre_arretee");
        _player.AddItemCopy("montre_arretee");
        for (int i = 0; i < 1000; i++)
            _player.ObjectTriggers.OnWeaponImpact(elite, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        Check(!elite.IsSlowed, "Montre : rééquiper ne contourne pas les huit secondes entre deux ralentissements d’une élite");
        _player.ObjectTriggers.AdvanceClock(5.1f);
        for (int i = 0; i < 1000 && !elite.IsSlowed; i++)
            _player.ObjectTriggers.OnWeaponImpact(elite, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
        Check(elite.IsSlowed, "Montre : nouvelle activation possible après huit secondes");
        ordinary.QueueFree(); elite.QueueFree();
    }

    private void CheckPoisonDoses()
    {
        Setup();
        Enemy enemy = SpawnAt(new Vector2(-98000f, 15000f));
        CurrentHp.SetValue(enemy, 1000f);
        MethodInfo process = typeof(Enemy).GetMethod("ProcessPoison", Private);
        enemy.ApplyPoison(30f, 3f, maxDoses: 2);
        process.Invoke(enemy, new object[] { 2f });
        enemy.ApplyPoison(30f, 3f, maxDoses: 2);
        enemy.ApplyPoison(300f, 3f, maxDoses: 2);
        Check(enemy.PoisonDoseCount == 2, "Poison : une dose de plus que le plafond est refusée");
        process.Invoke(enemy, new object[] { 1f });
        Check(enemy.PoisonDoseCount == 1 && Near(Hp(enemy), 960f),
            "Poison : la première dose expire sans être prolongée par la deuxième (40 dégâts après trois secondes)");
        process.Invoke(enemy, new object[] { 2.1f });
        Check(!enemy.IsPoisoned && Near(Hp(enemy), 940f), "Poison : chaque dose inflige son budget exact, sans réserve relancée");
        Raise("flacon_de_pharmacie", 1);
        _player.ObjectTriggers.Rng.Seed = 205;
        WeaponInstance weapon = _player.WeaponSlots[0];
        MethodInfo clear = typeof(Enemy).GetMethod("ClearPoison", Private);
        int poisoned = 0;
        const int trials = 4000;
        for (int i = 0; i < trials; i++)
        {
            _player.ObjectTriggers.AdvanceClock(1.01f);
            clear.Invoke(enemy, null);
            _player.ObjectTriggers.OnWeaponImpact(enemy, 10f, weapon, 1, _player.BeginAttack(weapon, 10f), dealt: 10f);
            if (enemy.IsPoisoned) poisoned++;
        }
        float expected = 0.10f * Mathf.Clamp(weapon.Base.TriggerCoefficient, 0f, 1f);
        Check(Mathf.Abs(poisoned / (float)trials - expected) < 0.025f,
            $"Flacon : probabilité mesurée {poisoned / (float)trials:P1}, cible {expected:P1} (aucun poison garanti)");
        enemy.QueueFree();
    }

    private void CheckStatItemExchange()
    {
        Setup();
        _player.DisableDefenseForTests();
        _player.TakeDamage(30f);
        float hp = _player.CurrentHp, max = _player.EffectiveMaxHp;
        // Un objet factice reprend le circuit réel du sac, avant livraison des nouvelles fiches de stats en I11.
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get("semelle_usee");
        List<PassiveEffectData> original = data.Effects;
        try
        {
            data.Effects = new() { new() { Stat = "max_hp", ModifierType = "additive", Step = 12f } };
            _player.SetBagCapacity(20);
            for (int i = 0; i < 8; i++) _player.AddItemCopy(data.Id);
            bool eight = Near(_player.EffectiveMaxHp, max + 96f) && Near(_player.CurrentHp, hp);
            for (int i = 0; i < 8; i++) _player.ForgetItemCopy(data.Id);
            _player.AddItemCopy(data.Id);
            Check(eight && Near(_player.EffectiveMaxHp, max + 12f) && Near(_player.CurrentHp, hp),
                "PV max : huit copies linéaires, oubli puis reprise sans rendre de PV");
            _player.ForgetItemCopy(data.Id);
            data.Effects = new() { new() { Stat = "shield", ModifierType = "additive", Step = 15f } };
            _player.AddItemCopy(data.Id);
            _player.AddItemCopy(data.Id);
            bool empty = Near(_player.MaxShield, 30f) && Near(_player.Shield, 0f);
            PlayerDefense defense = (PlayerDefense)typeof(Player).GetField("_defense", Private).GetValue(_player);
            defense.Step(100f);
            defense.Absorb(10f, 0f);
            _player.ForgetItemCopy(data.Id);
            _player.AddItemCopy(data.Id);
            Check(empty && Near(_player.MaxShield, 30f) && Near(_player.Shield, 15f),
                "Bouclier : agrandir ne recharge pas ; une copie retirée puis reprise ne rend pas la charge perdue");
            _player.ForgetItemCopy(data.Id);
            _player.ForgetItemCopy(data.Id);
            Check(Near(_player.MaxShield, 0f) && Near(_player.Shield, 0f), "Dernier objet de bouclier retiré : aucun point résiduel");
        }
        finally { data.Effects = original; }
    }

    private void CheckItemPresentation()
    {
        Setup();
        List<PlayerSheet.StatLine> rows = PlayerSheet.StatLines(_player);
        ObjectDataValidator.TryParseContract(Godot.FileAccess.GetFileAsString(ObjectDataValidator.ContractPath), out ObjectDataValidator.Contract contract, out _);
        HashSet<string> shown = new();
        foreach (PlayerSheet.StatLine row in rows)
            shown.Add(row.Id);
        Check(rows.Count == 19 && shown.SetEquals(contract.PlayerStats), "Pause : les dix-neuf statistiques globales du contrat sont présentes, même à zéro");
        Check(rows.Find(row => row.Id == "lifesteal").Value == "0 %"
            && rows.Find(row => row.Id == "shield").Value == "0 / 0"
            && rows.Find(row => row.Id == "xp_gain").Value == "100 %"
            && rows.Find(row => row.Id == "dash_recharge").Value.EndsWith(" s", StringComparison.Ordinal),
            "Pause : vol de vie, bouclier, XP et recharge du dash visibles sans objet");
        Check(StatCatalog.FormatBonus("regen_rate", 0.15f, false).Contains("9 PV/min", StringComparison.Ordinal)
            && rows.Find(row => row.Id == "regen_rate").Value.Contains("PV/min", StringComparison.Ordinal),
            "Régénération : 0,15 PV/s internes sont annoncés +9 PV/min partout");
        PassiveSouvenirData match = PassiveSouvenirDataLoader.Get("allumette_humide");
        string one = ItemPresentation.Describe(match, 1), eight = ItemPresentation.Describe(match, 8);
        Check(one.Contains("8 %", StringComparison.Ordinal) && eight.Contains("8 %", StringComparison.Ordinal)
            && one.Contains("2 applications/s", StringComparison.Ordinal) && eight.Contains("16 applications/s", StringComparison.Ordinal),
            "Fiche Allumette : la chance reste 8 %, les copies augmentent seulement le budget montré");
        ChoiceCard card = new();
        ItemPresentation.ConfigureCard(card, match);
        Check(card.Rank == 1 && card.Frame == RarityPalette.Main("uncommon") && card.Tag == RarityPalette.DisplayName("uncommon"),
            "Cartes d’objet : rang, couleur et nom viennent de sa rareté fixe");
        _player.AddOrUpgradePassive("souffle_du_neant", 5);
        WeaponInstance bow = _player.WeaponSlots[0];
        Check(Near(_player.GetWeaponStatForDisplay(bow, "projectile_count"), bow.GetStat("projectile_count") + _player.BonusProjectiles),
            "Fiche d’arme : les projectiles du build sont inclus dans la quantité finale");
        WeaponInstance volley = new(WeaponDataLoader.Get("makeshift_bow"));
        WeaponInstance piercing = new(WeaponDataLoader.Get("makeshift_bow"));
        foreach (WeaponInstance variant in new[] { volley, piercing })
            while (variant.CanLevelUp) variant.ApplyUpgrade(Array.Empty<StatGain>());
        volley.Ascend("volley");
        piercing.Ascend("pierce_through");
        Check(Near(_player.GetWeaponStatForDisplay(volley, "projectile_count"), volley.GetStat("projectile_count") + 2f * _player.BonusProjectiles)
            && Near(_player.GetWeaponStatForDisplay(piercing, "projectile_count"), 1f),
            "Fiches d’ascension : Volée double le bonus de projectiles, Transpercer reste à un tir");
        Check(ItemPresentation.Describe(PassiveSouvenirDataLoader.Get("plan_de_metro"), 8).Contains("×4,5", StringComparison.Ordinal)
            && ItemPresentation.Describe(PassiveSouvenirDataLoader.Get("chewing_gum"), 8).Contains("durée 6 s", StringComparison.Ordinal),
            "Fiches à huit copies : distance réelle du Métro et plafond de traînée respectés");
    }

    private async Task CheckItemPause()
    {
        Setup();
        Raise("semelle_usee", 1);
        Raise("tabouret_de_camping", 1);
        _player.Velocity = Vector2.Zero;
        PublishTestDash(blink: true);
        FieldInfo clock = typeof(ObjectTriggers).GetField("_clock", Private);
        GetTree().Paused = true;
        float before = (float)clock.GetValue(_player.ObjectTriggers);
        float still = _player.ObjectStances.StillDamageBonus;
        for (int i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        bool paused = Near((float)clock.GetValue(_player.ObjectTriggers), before)
            && Near(_player.ObjectStances.StillDamageBonus, still) && _player.ObjectTriggers.StrideCharges == 1;
        GetTree().Paused = false;
        for (int i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(paused && (float)clock.GetValue(_player.ObjectTriggers) > before && _player.ObjectStances.StillDamageBonus > still,
            "Pause moteur réelle : budgets, charge et immobilité suspendus ; horloges reprises après fermeture");
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
        enemy.SetTicking(false);
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
