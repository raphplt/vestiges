using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Banc des armes portées (plan 17, lot 1A, corrections) sur un vrai Player : notes de la Boîte à musique dès
/// l'équipement, effets au contact de l'arme qui a frappé (pas de la dernière qui a tiré), niveau unique après
/// l'Autel, bannissement qui écarte aussi les améliorations. Minuteurs d'armes coupés : le banc pilote les coups.
/// </summary>
public partial class WeaponRegression : Node2D
{
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private Player _player;
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_player);
            _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
            _player.SetPhysicsProcess(false);
            _player.IsAIControlled = true;
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            CheckOrbitalOnEquip();
            await CheckOrbitalHits();
            await CheckHitEffectsFollowSource();
            CheckSingleLevel();
            CheckBanishUpgrade();
            CheckRarityDistribution();
            CheckOubliEffects();
            CheckUpgradeGains();
            CheckRangeAndZone();
            await CheckGroundFireOnGround();
            CheckAscensions();

            GD.Print($"[WeaponRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    /// <summary>
    /// Flaque de feu de la Lanterne : dégâts mesurés au sol, comme l'ellipse dessinée. À 0,8 rayon à l'horizontale une
    /// créature brûle ; à 0,8 rayon à la verticale de l'écran (1,6 au sol), elle est hors de la flaque.
    /// </summary>
    private async Task CheckGroundFireOnGround()
    {
        const float radius = 40f;
        Vector2 center = _player.Position + new Vector2(0f, 300f);
        Enemy beside = EnemyScene.Instantiate<Enemy>();
        Enemy below = EnemyScene.Instantiate<Enemy>();
        foreach (Enemy enemy in new[] { beside, below })
        {
            AddChild(enemy);
            enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
            enemy.SetPhysicsProcess(false);
        }
        beside.Position = center + new Vector2(radius * 0.8f, 0f);
        below.Position = center + new Vector2(0f, radius * 0.8f);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        float besideBefore = beside.HpRatio;
        float belowBefore = below.HpRatio;
        GroundFire fire = new(GetNode<GroupCache>("/root/GroupCache"));
        fire.Add(center, 50f, 1f, radius);
        fire.Process(0.6f);
        Check(beside.HpRatio < besideBefore && Mathf.IsEqualApprox(below.HpRatio, belowBefore),
            $"Flaque de feu mesurée au sol : à côté {besideBefore:0.00} → {beside.HpRatio:0.00}, en dessous {belowBefore:0.00} → {below.HpRatio:0.00}");
        beside.QueueFree();
        below.QueueFree();
    }

    private void CheckOrbitalOnEquip()
    {
        _player.AddWeapon(WeaponDataLoader.Get("music_box"));
        List<Node2D> orbs = (List<Node2D>)typeof(Player).GetField("_orbitalProjectiles", Private).GetValue(_player);
        Check(orbs.Count > 0, $"Boîte à musique : {orbs.Count} notes dès l'équipement, sans attendre le minuteur");
    }

    /// <summary>Un ennemi posé sur l'orbite est touché par les notes : leurs dégâts comptent pour la Boîte à musique.</summary>
    private async Task CheckOrbitalHits()
    {
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        enemy.SetPhysicsProcess(false);
        float orbit = _player.GetWeaponStatForDisplay(FindSlot("music_box"), "range");
        enemy.Position = _player.Position + Iso.ToScreen(new Vector2(orbit, 0f));
        for (int frame = 0; frame < 180 && _player.GetDamageDealt("music_box") <= 0f; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            _player._PhysicsProcess(1f / 60f);
        }
        Check(_player.GetDamageDealt("music_box") > 0f, $"Boîte à musique : les notes touchent ({_player.GetDamageDealt("music_box"):0} dégâts)");
        enemy.QueueFree();
    }

    private async Task CheckHitEffectsFollowSource()
    {
        _player.AddWeapon(WeaponDataLoader.Get("nail_mace"));
        WeaponInstance bow = FindSlot("makeshift_bow") ?? AddAndFind("makeshift_bow");
        WeaponInstance mace = FindSlot("nail_mace");
        // La masse vient de frapper : c'est elle l'« arme courante » quand la flèche touche.
        typeof(Player).GetField("_equippedWeapon", Private).SetValue(_player, mace);

        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        enemy.SetPhysicsProcess(false);
        enemy.Position = _player.Position + new Vector2(80f, 0f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        _player.OnProjectileHit(enemy, 1f, false, bow);
        float bleed = (float)typeof(Enemy).GetField("_bleedTimer", Private).GetValue(enemy);
        Check(bleed <= 0f, $"flèche de l'arc après un coup de masse : pas de saignement emprunté (timer {bleed:F1} s)");

        _player.OnProjectileHit(enemy, 1f, false, mace);
        bleed = (float)typeof(Enemy).GetField("_bleedTimer", Private).GetValue(enemy);
        Check(bleed > 0f, $"coup de la masse : saignement appliqué (timer {bleed:F1} s)");
        enemy.QueueFree();
    }

    private void CheckSingleLevel()
    {
        WeaponInstance equipped = _player.EquippedWeapon;
        int before = _player.GetWeaponFragmentLevel(equipped.Id);
        RandomNumberGenerator rng = new() { Seed = 3 };
        bool rareOrBetter = true;
        for (int i = 0; i < 200; i++)
            rareOrBetter &= UpgradeRoller.RollRarityAtLeast(0f, "rare", rng).Rank >= UpgradeRoller.Get("rare").Rank;
        UpgradeRarity rarity = UpgradeRoller.RollRarityAtLeast(0f, "rare", rng);
        bool upgraded = _player.UpgradeWeapon(equipped.Id, UpgradeRoller.RollWeaponGains(equipped, rarity, rng));
        int after = _player.GetWeaponFragmentLevel(equipped.Id);
        Check(rareOrBetter && upgraded && after == before + 1 && after == _player.EquippedWeapon.Level,
            $"Mémorial : arme ravivée Rare au moins, badge et arme montent ensemble ({before} → {after}, arme {_player.EquippedWeapon.Level})");
    }

    private void CheckBanishUpgrade()
    {
        FragmentManager fragments = new() { Name = "FragmentManager" };
        AddChild(fragments);
        typeof(FragmentManager).GetMethod("CachePlayer", Private)?.Invoke(fragments, null);
        fragments.AddBanishes(1);
        int banishesBefore = fragments.BanishesRemaining;
        string banished = FindSlot("nail_mace").Id;
        typeof(FragmentManager).GetField("_currentLevel", Private)?.SetValue(fragments, 5);
        fragments.BanishFragment(banished);
        List<FragmentOption> pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);
        bool offered = pool.Exists(option => option.Id == banished);
        Check(!offered && fragments.BanishesRemaining == banishesBefore - 1,
            $"bannir l'amélioration de {banished} : absente de l'offre, bannissement consommé");
    }

    /// <summary>Oublis de carte : les effets du même nom s'additionnent et se publient ; un Oubli définitif ne se lève pas.</summary>
    private void CheckOubliEffects()
    {
        PerilManager peril = new();
        AddChild(peril);
        Dictionary<string, float> published = new();
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        EventBus.OubliEffectChangedEventHandler handler = (effect, total) => published[effect] = total;
        bus.OubliEffectChanged += handler;
        OubliData path = OubliDataLoader.All[0];
        OubliData permanent = OubliDataLoader.All[0];
        foreach (OubliData oubli in OubliDataLoader.All)
        {
            if (oubli.Effect == "erasure_speed")
                path = oubli;
            if (oubli.Permanent)
                permanent = oubli;
        }

        peril.AddOubli(path);
        peril.AddOubli(path);
        float twice = published.GetValueOrDefault(path.Effect);
        bool lifted = peril.LiftOubli(peril.Oublis[0]);
        float once = published.GetValueOrDefault(path.Effect);
        peril.AddOubli(permanent);
        bool permanentLifted = peril.LiftOubli(peril.Oublis[^1]);
        bus.OubliEffectChanged -= handler;
        Check(Mathf.IsEqualApprox(twice, 2f * path.Amount) && lifted && Mathf.IsEqualApprox(once, path.Amount)
              && !permanentLifted && peril.Oublis.Count == 2,
            $"Oublis : {path.Id} ×2 = {twice:0.##}, levé → {once:0.##} ; {permanent.Id} définitif, levée refusée");
        peril.QueueFree();
    }

    /// <summary>10 000 tirages : les poids de base sont respectés, et l'oubli de la zone fait monter les raretés.</summary>
    private void CheckRarityDistribution()
    {
        RandomNumberGenerator rng = new() { Seed = 17 };
        const int draws = 10000;
        Dictionary<string, int> anchored = new();
        Dictionary<string, int> erased = new();
        float erasedSteps = UpgradeRoller.BumpSteps(0f, Vestiges.World.ErasureManager.ErasureZonePhase.Erased, 0);
        float perilSteps = UpgradeRoller.BumpSteps(0f, Vestiges.World.ErasureManager.ErasureZonePhase.Anchored, 4)
            - UpgradeRoller.BumpSteps(0f, Vestiges.World.ErasureManager.ErasureZonePhase.Anchored, 0);
        Check(Mathf.IsEqualApprox(perilSteps, PerilDataLoader.RaritySteps(4)) && perilSteps > 0f,
            $"raretés : 4 points de Péril ajoutent {perilSteps:0.##} crans de montée");
        for (int i = 0; i < draws; i++)
        {
            string a = UpgradeRoller.RollRarity(0f, rng).Id;
            anchored[a] = anchored.GetValueOrDefault(a) + 1;
            string e = UpgradeRoller.RollRarity(erasedSteps, rng).Id;
            erased[e] = erased.GetValueOrDefault(e) + 1;
        }

        float totalWeight = 0f;
        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
            totalWeight += rarity.Weight;
        bool matches = true;
        List<string> shares = new();
        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
        {
            float expected = rarity.Weight / totalWeight;
            float measured = anchored.GetValueOrDefault(rarity.Id) / (float)draws;
            matches &= Mathf.Abs(measured - expected) < 0.015f;
            shares.Add($"{rarity.Id} {measured * 100f:0.0} % (attendu {expected * 100f:0.0}) / oubli {erased.GetValueOrDefault(rarity.Id) * 100f / draws:0.0} %");
        }
        int highAnchored = anchored.GetValueOrDefault("epic") + anchored.GetValueOrDefault("legendary");
        int highErased = erased.GetValueOrDefault("epic") + erased.GetValueOrDefault("legendary");
        GD.Print($"[WeaponRegression] raretés sur {draws} tirages : {string.Join(" ; ", shares)}");
        Check(matches, "raretés : poids de base respectés à 1,5 point près");
        Check(highErased > highAnchored * 2, $"raretés : zone Effacée, Épique et Légendaire plus fréquents ({highAnchored} → {highErased})");
    }

    /// <summary>Une amélioration Légendaire de l'arbalète : trois stats à ×2, et un palier de perçage.</summary>
    private void CheckUpgradeGains()
    {
        WeaponInstance crossbow = new(WeaponDataLoader.Get("crossbow"));
        RandomNumberGenerator rng = new() { Seed = 5 };
        List<StatGain> gains = UpgradeRoller.RollWeaponGains(crossbow, UpgradeRoller.Get("legendary"), rng);
        int stats = gains.FindAll(g => !g.Milestone).Count;
        bool milestone = gains.Exists(g => g.Milestone && g.Stat == "projectile_pierce");
        float pierceBefore = crossbow.GetStat("projectile_pierce", 0f);
        float damageBefore = crossbow.GetStat("damage", 0f);
        crossbow.ApplyUpgrade(gains);
        StatGain damageGain = gains.Find(g => g.Stat == "damage");
        float expectedDamage = damageGain.Stat == null ? damageBefore : damageBefore * (1f + damageGain.Amount);
        Check(stats == 3 && milestone && Mathf.IsEqualApprox(crossbow.GetStat("projectile_pierce", 0f), pierceBefore + 1f)
              && Mathf.IsEqualApprox(crossbow.GetStat("damage", 0f), expectedDamage) && crossbow.Level == 2,
            $"Légendaire sur l'arbalète : {stats} stats, palier de perçage {pierceBefore} → {crossbow.GetStat("projectile_pierce", 0f)}, niveau {crossbow.Level}");
    }

    /// <summary>Un bonus de zone ouvre l'arc sans allonger la portée (plan 05 §4).</summary>
    private void CheckRangeAndZone()
    {
        WeaponInstance mace = FindSlot("nail_mace");
        float range = _player.GetWeaponStatForDisplay(mace, "range");
        float arc = _player.GetWeaponStatForDisplay(mace, "arc_angle");
        _player.ApplyPerkModifier("aoe_radius", 1.2f, "multiplicative");
        float rangeAfter = _player.GetWeaponStatForDisplay(mace, "range");
        float arcAfter = _player.GetWeaponStatForDisplay(mace, "arc_angle");
        Check(Mathf.IsEqualApprox(range, rangeAfter) && arcAfter > arc * 1.19f,
            $"zone +20 % : portée inchangée ({range:0} → {rangeAfter:0}), arc {arc:0}° → {arcAfter:0}°");
    }

    private WeaponInstance FindSlot(string id)
    {
        foreach (WeaponInstance weapon in _player.WeaponSlots)
            if (weapon.Id == id)
                return weapon;
        return null;
    }

    private WeaponInstance AddAndFind(string id)
    {
        _player.AddWeapon(WeaponDataLoader.Get(id));
        return FindSlot(id);
    }

    /// <summary>
    /// Ascensions (plan 21 §3, lot G3) : deux voies au niveau 50, offertes ensemble, choisies pour de bon ; leurs
    /// leviers (motif, stats, projectiles en plus, effet à l'impact, orbite qui va et vient). Un joueur à part, pour ne pas toucher
    /// aux armes des autres contrôles.
    /// </summary>
    private void CheckAscensions()
    {
        int withPaths = 0;
        foreach (WeaponData data in WeaponDataLoader.GetAll())
            withPaths += data.Ascensions.Count == 2 ? 1 : 0;
        Check(withPaths >= 4, $"Ascensions : {withPaths} armes ont leurs deux voies");

        WeaponInstance bow = MaxedWeapon("makeshift_bow");
        bool ready = bow.CanAscend && !MaxedWeapon("heavy_hammer").CanAscend;
        bool chosen = bow.Ascend("volley");
        Check(ready && chosen && !bow.Ascend("pierce_through") && bow.AttackPattern == "burst"
            && bow.GetStat("projectile_count", 1f) >= 2f && Mathf.IsEqualApprox(bow.GetStat("spread_angle", 20f), 40f) && Mathf.IsEqualApprox(bow.BonusProjectileMultiplier, 2f),
            "Volée : éventail, deux fois plus de flèches et de projectiles en plus ; la voie est définitive");
        WeaponInstance piercing = MaxedWeapon("makeshift_bow");
        float before = piercing.GetStat("damage", 1f);
        piercing.Ascend("pierce_through");
        WeaponInstance thrust = MaxedWeapon("chipped_blade");
        thrust.Ascend("thrust");
        WeaponInstance harvest = MaxedWeapon("chipped_blade");
        harvest.Ascend("harvest");
        Check(!WeaponProperties.Concerns(thrust, "size") && WeaponProperties.Concerns(harvest, "size") && !WeaponTraits.SearchesTarget(harvest)
            && StatCatalog.Format("projectile_pierce", 999f) == "∞",
            "La voie choisie compte partout : Estoc ne grandit plus par la Taille, Moisson ne cherche plus de cible ; perforation « ∞ »");
        Check(Mathf.IsEqualApprox(piercing.GetStat("damage", 1f), before * 1.5f) && piercing.GetStat("projectile_pierce", 0f) >= 999f
            && Mathf.IsEqualApprox(piercing.GetStat("projectile_count", 1f), 1f) && piercing.BonusProjectileMultiplier == 0f,
            "Transpercer : une flèche, dégâts × 1,5, perforation illimitée, aucun projectile en plus");

        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        player.SetPhysicsProcess(false);
        player.IsAIControlled = true;
        WeaponInstance starting = player.WeaponSlots[0];
        while (starting.CanLevelUp)
            starting.ApplyUpgrade(Array.Empty<StatGain>());
        player.AddWeapon(WeaponDataLoader.Get("music_box"));
        WeaponInstance box = player.WeaponSlots[1];
        while (box.CanLevelUp)
            box.ApplyUpgrade(Array.Empty<StatGain>());

        FragmentManager fragments = new() { Name = "AscensionFragments" };
        AddChild(fragments);
        typeof(FragmentManager).GetField("_player", Private).SetValue(fragments, player);
        typeof(FragmentManager).GetMethod("OfferFragments", Private).Invoke(fragments, new object[] { 60 });
        List<FragmentOption> offer = new(fragments.PendingChoices);
        bool paired = offer.Count == 3 && offer[0].Type == FragmentOption.AscensionType && offer[1].Type == FragmentOption.AscensionType
            && offer[0].Id == offer[1].Id && offer[0].Ascension.Id != offer[1].Ascension.Id && offer[2].Type != FragmentOption.AscensionType;
        fragments.SelectFragment(offer[0]);
        WeaponInstance ascended = player.WeaponSlots[0].Id == offer[0].Id ? player.WeaponSlots[0] : player.WeaponSlots[1];
        Check(paired && ascended.Ascension?.Id == offer[0].Ascension.Id,
            $"Offre : les deux voies de {offer[0].DisplayName.Split(':')[0].Trim()} ensemble et une autre carte ; le choix transforme l'arme");

        box = player.WeaponSlots[1];
        bool lullaby = player.AscendWeapon(box.Id, "lullaby");
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
        enemy.SetPhysicsProcess(false);
        enemy.Position = new Vector2(4000f, 4000f);
        player.OnProjectileHit(enemy, 1f, false, box);
        float frozen = (float)typeof(Enemy).GetField("_freezeTimer", Private).GetValue(enemy);
        Check(lullaby && frozen > 0.5f, "Berceuse : une note fige un instant l'ennemi qu'elle touche");

        WeaponInstance round = MaxedWeapon("music_box");
        round.Ascend("round");
        MethodInfo pulse = typeof(Player).GetMethod("OrbitPulse", Private);
        float low = 10f, high = 0f;
        for (int i = 0; i < 40; i++)
        {
            float factor = (float)pulse.Invoke(player, new object[] { round, 0.05f });
            low = Mathf.Min(low, factor);
            high = Mathf.Max(high, factor);
        }
        Check(low < 0.7f && high > 1.5f, $"Ronde : l'orbite va de {low:0.00} à {high:0.00} fois la portée en 2 s");
        enemy.QueueFree();
        fragments.QueueFree();
        player.QueueFree();
    }

    private static WeaponInstance MaxedWeapon(string id)
    {
        WeaponInstance weapon = new(WeaponDataLoader.Get(id));
        while (weapon.CanLevelUp)
            weapon.ApplyUpgrade(Array.Empty<StatGain>());
        return weapon;
    }

    private void Check(bool passed, string message)
    {
        GD.Print($"[WeaponRegression] {(passed ? "PASS" : "FAIL")} {message}");
        if (!passed) _failures++;
    }
}
