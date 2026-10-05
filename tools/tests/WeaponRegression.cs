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
            await CheckTargetLockOneLife();
            await CheckHitEffectsFollowSource();
            CheckSingleLevel();
            CheckBanishUpgrade();
            CheckRarityDistribution();
            CheckOubliEffects();
            CheckUpgradeGains();
            CheckRangeAndZone();
            await CheckGroundFireOnGround();
            CheckAscensions();
            CheckGrammarRejectsUnknownKeys();
            CheckContractRejectsBadData();
            await CheckCountForAllWeapons();
            await CheckVolleyOnSingleTarget();
            CheckTemper();

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

    /// <summary>
    /// Q8a (plan 26) : un tir guidé ou un tir de rafale en attente verrouillé sur une créature qui meurt puis revient du
    /// pool, ailleurs, ne suit pas sa nouvelle vie ; il se tourne vers la créature restante. Contre-épreuve : sans mort,
    /// le verrou tient.
    /// </summary>
    private async Task CheckTargetLockOneLife()
    {
        PackedScene projectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");
        WeaponData compass = WeaponDataLoader.Get("compass_needle");
        Enemy locked = SpawnStill(new Vector2(120f, 0f));
        Enemy other = SpawnStill(new Vector2(0f, 150f));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Projectile homing = projectileScene.Instantiate<Projectile>();
        AddChild(homing);
        homing.SetPhysicsProcess(false);
        homing.Launch(Vector2.Zero, Vector2.Right, 1f, 100f, 10f, 0, false, _player, compass, null);
        homing.SetHoming(1f, locked);
        homing._PhysicsProcess(1f / 60f);
        Check(LockedTarget(homing, "_homingTarget") == locked, "Verrou : tir guidé sur sa cible tant qu'elle vit");

        Recycle(locked, new Vector2(5000f, 5000f));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        homing._PhysicsProcess(1f / 60f);
        Check(LockedTarget(homing, "_homingTarget") == other,
            "Verrou : la cible morte puis revenue du pool n'est plus suivie, le tir guidé prend la créature restante");

        Projectile burst = projectileScene.Instantiate<Projectile>();
        AddChild(burst);
        burst.SetPhysicsProcess(false);
        burst.Launch(Vector2.Zero, Vector2.Right, 1f, 100f, 10f, 0, false, _player, compass, null, launchDelay: 0.05f);
        burst.AimAtDeparture(locked);
        Recycle(locked, new Vector2(-5000f, 5000f));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        burst._PhysicsProcess(0.1f);
        Vector2 direction = (Vector2)typeof(Projectile).GetField("_direction", Private).GetValue(burst);
        Vector2 toOther = (other.GlobalPosition - _player.GlobalPosition).Normalized();
        Check(direction.Dot(toOther) > 0.99f,
            $"Verrou : le tir de rafale dont la cible est revenue du pool part vers la créature restante ({direction})");

        homing.QueueFree();
        burst.QueueFree();
        locked.QueueFree();
        other.QueueFree();
    }

    private Enemy SpawnStill(Vector2 offset)
    {
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        enemy.SetPhysicsProcess(false);
        enemy.GlobalPosition = _player.GlobalPosition + offset;
        return enemy;
    }

    /// <summary>La créature meurt et revient du pool comme une autre vie, loin d'ici.</summary>
    private void Recycle(Enemy enemy, Vector2 offset)
    {
        enemy.Reset();
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        enemy.SetPhysicsProcess(false);
        enemy.GlobalPosition = _player.GlobalPosition + offset;
    }

    private static Node2D LockedTarget(Projectile projectile, string field) =>
        ((TargetLock)typeof(Projectile).GetField(field, Private).GetValue(projectile)).TryGet(out Node2D target) ? target : null;

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

    /// <summary>
    /// 10 000 tirages : les poids de base sont respectés ; l'oubli de la zone fait un peu monter les raretés hautes, la
    /// Chance beaucoup (DECISIONS §53).
    /// </summary>
    private void CheckRarityDistribution()
    {
        RandomNumberGenerator rng = new() { Seed = 17 };
        const int draws = 10000;
        Dictionary<string, int> anchored = new();
        Dictionary<string, int> erased = new();
        Dictionary<string, int> lucky = new();
        float luckySteps = UpgradeRoller.BumpSteps(1f, Vestiges.World.ErasureManager.ErasureZonePhase.Anchored, 0);
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
            string l = UpgradeRoller.RollRarity(luckySteps, rng).Id;
            lucky[l] = lucky.GetValueOrDefault(l) + 1;
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
        int highLucky = lucky.GetValueOrDefault("epic") + lucky.GetValueOrDefault("legendary");
        Check(highErased > highAnchored * 1.3f && highLucky > highAnchored * 5,
            $"raretés : Épique et Légendaire sur {draws} tirages, {highAnchored} sans rien, {highErased} en zone Effacée, {highLucky} avec 1 de Chance");
    }

    /// <summary>
    /// Plan 23 R4 : une Légendaire touche trois stats, chacune au double de son pas relevé, ou +3 pour une stat
    /// entière ; une stat entière se tire dès la commune (+0,5) ; la décimale se joue à chaque attaque, sauf pour les
    /// orbes, qui ne comptent que la partie entière.
    /// </summary>
    private void CheckUpgradeGains()
    {
        WeaponInstance crossbow = new(WeaponDataLoader.Get("crossbow"));
        RandomNumberGenerator rng = new() { Seed = 5 };
        List<StatGain> gains = UpgradeRoller.RollWeaponGains(crossbow, UpgradeRoller.Get("legendary"), rng);
        bool amounts = gains.Count == 3;
        foreach (StatGain gain in gains)
        {
            WeaponUpgradeStatConfig config = WeaponUpgradeDataLoader.GetStatConfig(gain.Stat);
            amounts &= config.Integer ? Mathf.IsEqualApprox(gain.Amount, 3f) : Mathf.IsEqualApprox(gain.Amount, config.Step * 2f);
        }
        float damageBefore = crossbow.GetStat("damage");
        crossbow.ApplyUpgrade(gains);
        StatGain damageGain = gains.Find(g => g.Stat == "damage");
        float expectedDamage = damageGain.Stat == null ? damageBefore : damageBefore * (1f + damageGain.Amount);
        Check(amounts && Mathf.IsEqualApprox(crossbow.GetStat("damage"), expectedDamage) && crossbow.Level == 2
              && Mathf.IsEqualApprox(WeaponUpgradeDataLoader.GetStatConfig("damage").Step, 0.18f),
            $"Légendaire sur la Cloueuse : {gains.Count} stats au double du pas (dégâts +18 % par commune), +3 pour une stat entière");

        // Une commune finit par tirer la perforation, stat entière de la Cloueuse : +0,5.
        WeaponInstance nailer = new(WeaponDataLoader.Get("crossbow"));
        float pierceBefore = nailer.GetStat("projectile_pierce");
        StatGain pierce = default;
        for (int i = 0; i < 200 && pierce.Stat == null; i++)
            pierce = UpgradeRoller.RollWeaponGains(nailer, UpgradeRoller.Get("common"), rng).Find(g => g.Stat == "projectile_pierce");
        nailer.ApplyUpgrade(new[] { pierce });
        Check(pierce.Stat != null && Mathf.IsEqualApprox(nailer.GetStat("projectile_pierce"), pierceBefore + 0.5f)
              && StatCatalog.Format("projectile_pierce", nailer.GetStat("projectile_pierce")) == "3,5",
            $"Commune sur la Cloueuse : perforation {pierceBefore} → {StatCatalog.Format("projectile_pierce", nailer.GetStat("projectile_pierce"))}");

        // Lance-billes à 3,5 projectiles : 3 ou 4 par salve, la moitié du temps chacun, sur 1 000 attaques.
        WeaponInstance sling = new(WeaponDataLoader.Get("sling"));
        sling.ApplyUpgrade(new[] { new StatGain("projectile_count", 0.5f) });
        RandomNumberGenerator attacks = new() { Seed = 9 };
        int total = 0, low = int.MaxValue, high = 0;
        for (int i = 0; i < 1000; i++)
        {
            int count = FractionalCount.Roll(sling.GetStat("projectile_count"), attacks.Randf());
            total += count;
            low = Mathf.Min(low, count);
            high = Mathf.Max(high, count);
        }
        Check(low == 3 && high == 4 && total > 3420 && total < 3580,
            $"Lance-billes à 3,5 projectiles : {low} à {high} par salve, {total / 1000f:0.00} en moyenne sur 1 000 attaques");

        // Boîte à musique à 3,5 puis 4 orbes : 3 orbes, puis 4, sans perte ni doublon.
        WeaponInstance box = FindSlot("music_box") ?? AddAndFind("music_box");
        MethodInfo setup = typeof(Player).GetMethod("SetupOrbitalWeapon", Private);
        List<Node2D> orbs = (List<Node2D>)typeof(Player).GetField("_orbitalProjectiles", Private).GetValue(_player);
        box.ApplyUpgrade(new[] { new StatGain("orbital_count", 0.5f) });
        setup.Invoke(_player, new object[] { box });
        int half = orbs.Count;
        box.ApplyUpgrade(new[] { new StatGain("orbital_count", 0.5f) });
        setup.Invoke(_player, new object[] { box });
        int whole = orbs.Count;
        HashSet<Node2D> distinct = new(orbs);
        Check(half == 3 && whole == 4 && distinct.Count == 4,
            $"Boîte à musique : 3,5 orbes en font {half}, 4 en font {whole}, toutes distinctes");
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
        // Toutes les armes ont leurs deux voies (DECISIONS §40), dans un motif que leur famille sait jouer.
        int withPaths = 0, total = 0;
        List<string> badPatterns = new();
        foreach (WeaponData data in WeaponDataLoader.GetAll())
        {
            total++;
            withPaths += data.Ascensions.Count == 2 ? 1 : 0;
            foreach (WeaponAscensionData path in data.Ascensions)
            {
                AttackPatternKind pattern = path.AttackPattern ?? data.AttackPattern;
                bool known = data.Category == WeaponCategory.Melee
                    ? pattern is AttackPatternKind.Arc or AttackPatternKind.Linear or AttackPatternKind.Circular or AttackPatternKind.Chain
                    : pattern is AttackPatternKind.Linear or AttackPatternKind.Burst or AttackPatternKind.Homing or AttackPatternKind.Orbital;
                if (!known)
                    badPatterns.Add($"{data.Id}:{path.Id}:{pattern}");
            }
        }
        Check(withPaths == total && badPatterns.Count == 0,
            $"Ascensions : {withPaths}/{total} armes ont leurs deux voies, motifs inconnus [{string.Join(" ", badPatterns)}]");

        WeaponInstance bow = MaxedWeapon("makeshift_bow");
        bool ready = bow.CanAscend && !new WeaponInstance(WeaponDataLoader.Get("heavy_hammer")).CanAscend;
        bool chosen = bow.Ascend("volley");
        Check(ready && chosen && !bow.Ascend("pierce_through") && bow.AttackPattern == AttackPatternKind.Burst
            && bow.GetStat("projectile_count") >= 2f && Mathf.IsEqualApprox(bow.GetStat("spread_angle"), 40f) && Mathf.IsEqualApprox(bow.BonusProjectileMultiplier, 2f),
            "Volée : éventail, deux fois plus de flèches et de projectiles en plus ; la voie est définitive");
        WeaponInstance piercing = MaxedWeapon("makeshift_bow");
        float before = piercing.GetStat("damage");
        piercing.Ascend("pierce_through");
        WeaponInstance thrust = MaxedWeapon("chipped_blade");
        thrust.Ascend("thrust");
        WeaponInstance harvest = MaxedWeapon("chipped_blade");
        harvest.Ascend("harvest");
        Check(!WeaponProperties.Concerns(thrust, "size") && WeaponProperties.Concerns(harvest, "size") && !WeaponTraits.SearchesTarget(harvest)
            && StatCatalog.Format("projectile_pierce", 999f) == "∞",
            "La voie choisie compte partout : Estoc ne grandit plus par la Taille, Moisson ne cherche plus de cible ; perforation « ∞ »");
        Check(Mathf.IsEqualApprox(piercing.GetStat("damage"), before * 1.5f) && piercing.GetStat("projectile_pierce") >= 999f
            && Mathf.IsEqualApprox(piercing.GetStat("projectile_count"), 1f) && piercing.BonusProjectileMultiplier == 0f,
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

        // Réglages d'effet spécial remplacés par la voie, sans toucher à l'arme de base (G3 étape 2).
        WeaponInstance suture = MaxedWeapon("surgeons_scalpel");
        suture.Ascend("suture");
        WeaponInstance combination = MaxedWeapon("echo_gauntlets");
        combination.Ascend("combination");
        Check(Mathf.IsEqualApprox(suture.SpecialEffect.Get(SpecialEffectParam.HitsPerHeal), 3f) && Mathf.IsEqualApprox(suture.Base.SpecialEffect.Get(SpecialEffectParam.HitsPerHeal), 5f)
            && Mathf.IsEqualApprox(combination.SpecialEffect.Get(SpecialEffectParam.EchoCount), 2f),
            "Suture soigne tous les 3 coups et Enchaînement fait deux échos ; le Scalpel de base reste à 5");

        player.AddWeapon(WeaponDataLoader.Get("clock_hand"));
        WeaponInstance clock = player.WeaponSlots[^1];
        while (clock.CanLevelUp)
            clock.ApplyUpgrade(Array.Empty<StatGain>());
        player.AscendWeapon(clock.Id, "freeze_frame");
        Enemy frozenTarget = EnemyScene.Instantiate<Enemy>();
        AddChild(frozenTarget);
        frozenTarget.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
        frozenTarget.SetPhysicsProcess(false);
        frozenTarget.Position = new Vector2(6000f, 6000f);
        player.OnProjectileHit(frozenTarget, 1f, false, clock);
        float stopped = (float)typeof(Enemy).GetField("_freezeTimer", Private).GetValue(frozenTarget);
        Check(stopped > 0.3f, $"Arrêt sur image : le champ du Chronomètre fige ({stopped:0.00} s)");
        frozenTarget.QueueFree();

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

    /// <summary>
    /// Grammaire d'attaque (plan 26 Q5) : une faute de clé écarte l'arme avec un diagnostic qui nomme l'arme, la voie
    /// et le champ, au lieu de la jouer avec le motif ou la famille par défaut.
    /// </summary>
    private void CheckGrammarRejectsUnknownKeys()
    {
        Godot.Collections.Dictionary Weapon(string type, string pattern, string pathPattern)
        {
            Godot.Collections.Dictionary path = new() { ["id"] = "voie_test" };
            if (pathPattern != null)
                path["attack_pattern"] = pathPattern;
            return new Godot.Collections.Dictionary
            {
                ["id"] = "arme_test",
                ["type"] = type,
                ["attack_pattern"] = pattern,
                ["stats"] = new Godot.Collections.Dictionary { ["damage"] = 10, ["attack_speed"] = 1, ["range"] = 70 },
                ["ascensions"] = new Godot.Collections.Array { path, new Godot.Collections.Dictionary { ["id"] = "autre_voie" } },
            };
        }

        bool valid = WeaponDataLoader.TryParseWeapon(Weapon("melee", "circular", "chain"), out WeaponData parsed, out _)
            && parsed.Category == WeaponCategory.Melee && parsed.AttackPattern == AttackPatternKind.Circular
            && parsed.Ascensions[0].AttackPattern == AttackPatternKind.Chain && parsed.Ascensions[1].AttackPattern == null;
        bool badPattern = !WeaponDataLoader.TryParseWeapon(Weapon("ranged", "homming", null), out WeaponData rejected, out string patternError)
            && rejected == null && patternError.Contains("arme_test") && patternError.Contains("attack_pattern") && patternError.Contains("homming");
        bool badCategory = !WeaponDataLoader.TryParseWeapon(Weapon("Melee", "arc", null), out _, out string categoryError)
            && categoryError.Contains("arme_test") && categoryError.Contains("type") && categoryError.Contains("Melee");
        bool badPath = !WeaponDataLoader.TryParseWeapon(Weapon("melee", "arc", "spiral"), out _, out string pathError)
            && pathError.Contains("voie_test") && pathError.Contains("spiral");
        Check(valid && badPattern && badCategory && badPath,
            $"Grammaire : clés connues lues en types, motif/famille/voie inconnus refusés [{patternError} | {categoryError} | {pathError}]");
    }

    /// <summary>
    /// Contrat des armes (plan 26 Q6a) : chaque faute de données écarte l'arme avec un message qui nomme l'arme, la voie
    /// et le champ ; une arme conforme reçoit les secours du contrat pour les réglages qu'elle ne déclare pas.
    /// </summary>
    private void CheckContractRejectsBadData()
    {
        Godot.Collections.Dictionary Valid() => new()
        {
            ["id"] = "arme_test",
            ["type"] = "ranged",
            ["attack_pattern"] = "linear",
            ["stats"] = new Godot.Collections.Dictionary { ["damage"] = 10, ["attack_speed"] = 1, ["range"] = 200 },
            ["growth"] = new Godot.Collections.Dictionary { ["damage"] = 3 },
            ["special_effect"] = new Godot.Collections.Dictionary { ["type"] = "delayed_echo", ["echo_delay"] = 0.3, ["echo_damage_percent"] = 0.6 },
            ["ascensions"] = new Godot.Collections.Array
            {
                new Godot.Collections.Dictionary { ["id"] = "voie_test" },
                new Godot.Collections.Dictionary { ["id"] = "autre_voie" },
            },
        };
        Godot.Collections.Dictionary Path(Godot.Collections.Dictionary weapon) => weapon["ascensions"].AsGodotArray()[0].AsGodotDictionary();
        Godot.Collections.Dictionary Table(Godot.Collections.Dictionary owner, string key) => owner[key].AsGodotDictionary();

        bool valid = WeaponDataLoader.TryParseWeapon(Valid(), out WeaponData parsed, out string validError)
            && Mathf.IsEqualApprox(parsed.SpecialEffect.Get(SpecialEffectParam.EchoRadius), 40f)
            && Mathf.IsEqualApprox(parsed.SpecialEffect.Get(SpecialEffectParam.EchoCount), 1f);
        Check(valid, $"Contrat : arme conforme acceptée, secours du contrat résolus (écho 40 px, 1 répétition) {validError}");

        (string Label, System.Action<Godot.Collections.Dictionary> Mutate, string Expected)[] cases =
        {
            ("stat inconnue", w => Table(w, "stats")["damge"] = 5, "damge"),
            ("stat non numérique", w => Table(w, "stats")["damage"] = "dix", "nombre attendu"),
            ("stat non finie", w => Table(w, "stats")["damage"] = float.PositiveInfinity, "non finie"),
            ("stat nulle (NaN écrit en null)", w => Table(w, "stats")["damage"] = float.NaN, "nombre attendu"),
            ("stat hors bornes", w => Table(w, "stats")["arc_angle"] = 400, "hors de"),
            ("stat obligatoire absente", w => Table(w, "stats").Remove("range"), "range obligatoire"),
            ("croissance inconnue", w => Table(w, "growth")["rang"] = 1, "rang"),
            ("croissance sans réglage d'amélioration", w => Table(w, "growth")["spread_angle"] = 1, "réglage d'amélioration"),
            ("effet à l'impact inconnu", w => w["on_hit_effect"] = new Godot.Collections.Dictionary { ["type"] = "bleed", ["duration"] = 1 }, "bleed"),
            ("réglage d'impact absent", w => w["on_hit_effect"] = new Godot.Collections.Dictionary { ["type"] = "slow", ["duration"] = 1 }, "value obligatoire"),
            ("réglage d'impact inconnu", w => w["on_hit_effect"] = new Godot.Collections.Dictionary { ["type"] = "slow", ["value"] = 0.5, ["duration"] = 1, ["valeur"] = 1 }, "valeur"),
            ("effet spécial inconnu", w => Table(w, "special_effect")["type"] = "delayed_ecco", "delayed_ecco"),
            ("réglage spécial absent", w => Table(w, "special_effect").Remove("echo_delay"), "echo_delay obligatoire"),
            ("réglage spécial non entier", w => Table(w, "special_effect")["echo_count"] = 1.5, "pas entier"),
            ("champ d'arme inconnu", w => w["dammage_type"] = "physical", "dammage_type"),
            ("arme en main retirée (§65)", w => w["held_sprite"] = "assets/weapons/held/weapon_held_sickle.png", "held_sprite"),
            ("champ de voie inconnu", w => Path(w)["stat_multiplier"] = new Godot.Collections.Dictionary(), "stat_multiplier"),
            ("multiplicateur nul", w => Path(w)["stat_multipliers"] = new Godot.Collections.Dictionary { ["damage"] = 0 }, "stat_multipliers.damage"),
            ("remplacement de stat inconnue", w => Path(w)["stat_overrides"] = new Godot.Collections.Dictionary { ["piercing"] = 1 }, "piercing"),
            ("réglage de voie hors de l'effet", w => Path(w)["special_overrides"] = new Godot.Collections.Dictionary { ["echo_radiuss"] = 1 }, "echo_radiuss"),
            ("réglage de voie sans effet spécial", w => { w.Remove("special_effect"); Path(w)["special_overrides"] = new Godot.Collections.Dictionary { ["n"] = 3 }; }, "pas d'effet spécial"),
            ("drapeau inconnu", w => Path(w)["flags"] = new Godot.Collections.Array { "orbit_pluse" }, "orbit_pluse"),
            ("réglage de drapeau absent", w => { Path(w)["flags"] = new Godot.Collections.Array { "orbit_pulse" }; Path(w)["params"] = new Godot.Collections.Dictionary { ["pulse_min"] = 0.6, ["pulse_max"] = 1.6 }; }, "pulse_period obligatoire"),
            ("son introuvable", w => w["attack_audio"] = "sfx_inexistant", "sfx_inexistant"),
            ("image introuvable", w => w["sprite"] = "assets/weapons/icons/inexistant.png", "inexistant.png"),
            ("style inconnu", w => w["fx"] = new Godot.Collections.Dictionary { ["style"] = "spin" }, "spin"),
            ("famille inconnue", w => w["fx"] = new Godot.Collections.Dictionary { ["family"] = "gold" }, "gold"),
            ("projectile introuvable", w => w["fx"] = new Godot.Collections.Dictionary { ["projectile"] = "comet" }, "comet"),
        };
        foreach ((string label, System.Action<Godot.Collections.Dictionary> mutate, string expected) in cases)
        {
            Godot.Collections.Dictionary weapon = Valid();
            mutate(weapon);
            bool rejected = !WeaponDataLoader.TryParseWeapon(weapon, out WeaponData result, out string error);
            Check(rejected && result == null && error.Contains("arme_test") && error.Contains(expected)
                && (!label.Contains("voie") || error.Contains("voie_test")),
                $"Contrat : {label} refusé [{error}]");
        }
    }

    /// <summary>
    /// Rafale (plan 21 G6g, DECISIONS §59) : trois flèches sur un ennemi seul ne se superposent plus, deux attendent
    /// avant de partir, et toutes touchent leur cible.
    /// </summary>
    private async Task CheckVolleyOnSingleTarget()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        // Un joueur neuf : celui du banc porte les effets des contrôles précédents (échos, déclencheurs). Leurs
        // minuteurs d'arme sont suspendus pour que seuls les tirs commandés comptent.
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        player.SetPhysicsProcess(false);
        player.IsAIControlled = true;
        player.ProcessMode = ProcessModeEnum.Disabled;
        _player.ProcessMode = ProcessModeEnum.Disabled;
        typeof(Player).GetField("_critChance", Private).SetValue(player, 0f);
        FieldInfo equipped = typeof(Player).GetField("_equippedWeapon", Private);
        MethodInfo ranged = typeof(Player).GetMethod("PerformRangedAttack", Private);
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 100000f, 1f);
        enemy.SetPhysicsProcess(false);
        enemy.Position = player.Position + new Vector2(90f, 0f);
        int hits = 0;
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        EventBus.EntityDamagedEventHandler onDamaged = (target, _) => hits += target == enemy ? 1 : 0;
        bus.EntityDamaged += onDamaged;
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            async Task<(int Hits, int Flying, int Waiting)> Fire(int count)
            {
                WeaponInstance bow = new(WeaponDataLoader.Get("makeshift_bow"));
                if (count > 1)
                    bow.ApplyUpgrade(new[] { new StatGain("projectile_count", count - 1f) });
                equipped.SetValue(player, bow);
                hits = 0;
                ranged.Invoke(player, new object[] { AttackPatternKind.Linear });
                int flying = 0, waiting = 0;
                foreach (Node child in pools.GetChildren())
                {
                    if (child is not Projectile shot || shot.ProcessMode == ProcessModeEnum.Disabled)
                        continue;
                    flying += shot.Visible ? 1 : 0;
                    waiting += shot.Visible ? 0 : 1;
                }
                for (int frame = 0; frame < 90; frame++)
                    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                return (hits, flying, waiting);
            }

            (int singleHits, _, _) = await Fire(1);
            (int volleyHits, int flying, int waiting) = await Fire(3);
            Check(singleHits > 0 && flying == 1 && waiting == 2 && volleyHits == 3 * singleHits,
                $"Rafale : 3 flèches sur un ennemi seul, {flying} part, {waiting} attendent leur tour ; {volleyHits} impacts pour {singleHits} avec une flèche");
        }
        finally
        {
            bus.EntityDamaged -= onDamaged;
            enemy.QueueFree();
            player.QueueFree();
            pools.QueueFree();
            _player.ProcessMode = ProcessModeEnum.Inherit;
        }
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    /// <summary>
    /// Plan 21 G6a : les 24 armes montent leur nombre (tirs, frappes, ondes, orbes, cibles de chaîne) à un poids franc ;
    /// une frappe de mêlée en plus touche à pleins dégâts ; une onde de plus double le cône ; Papier carbone favorisé.
    /// </summary>
    private async Task CheckCountForAllWeapons()
    {
        int missing = 0;
        float lowestShare = 1f;
        foreach (WeaponData data in WeaponDataLoader.GetAll())
        {
            string count = data.AttackPattern switch
            {
                AttackPatternKind.Orbital => "orbital_count",
                AttackPatternKind.Chain => "chain_targets",
                _ => "projectile_count",
            };
            float total = 0f;
            foreach (float weight in data.Growth.Values)
                total += weight;
            if (!data.Growth.TryGetValue(count, out float own))
            {
                missing++;
                continue;
            }
            lowestShare = Mathf.Min(lowestShare, own / total);
        }
        Check(missing == 0 && lowestShare >= 0.12f,
            $"Nombre pour toutes les armes : {missing} sans, part la plus faible {lowestShare:P0} des tirages de stat");

        FieldInfo equipped = typeof(Player).GetField("_equippedWeapon", Private);
        FieldInfo crit = typeof(Player).GetField("_critChance", Private);
        float critBefore = (float)crit.GetValue(_player);
        crit.SetValue(_player, 0f);
        WeaponInstance blade = new(WeaponDataLoader.Get("chipped_blade"));
        equipped.SetValue(_player, blade);
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 100000f, 1f);
        enemy.SetPhysicsProcess(false);
        enemy.Position = _player.Position + new Vector2(40f, 0f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        MethodInfo melee = typeof(Player).GetMethod("PerformMeleeAttack", Private);
        melee.Invoke(_player, new object[] { AttackPatternKind.Arc });
        float single = _player.GetDamageDealt("chipped_blade");
        blade.ApplyUpgrade(new[] { new StatGain("projectile_count", 1f) });
        melee.Invoke(_player, new object[] { AttackPatternKind.Arc });
        float twice = _player.GetDamageDealt("chipped_blade") - single;
        Check(single > 0f && twice > single * 1.8f && twice < single * 2.2f
              && StatCatalog.Name("projectile_count", blade.Base) != StatCatalog.Name("projectile_count"),
            $"Lame ébréchée à 2 frappes : {single:0.#} puis {twice:0.#} dégâts, libellé « {StatCatalog.Name("projectile_count", blade.Base)} »");
        enemy.QueueFree();

        WeaponInstance radio = new(WeaponDataLoader.Get("last_broadcast"));
        equipped.SetValue(_player, radio);
        MethodInfo activate = typeof(Player).GetMethod("ActivateSustainedCone", Private);
        MethodInfo deactivate = typeof(Player).GetMethod("DeactivateSustainedCone", Private);
        FieldInfo coneDamage = typeof(Player).GetField("_coneBaseDamage", Private);
        activate.Invoke(_player, new object[] { radio.SpecialEffect });
        float oneWave = (float)coneDamage.GetValue(_player);
        deactivate.Invoke(_player, null);
        radio.ApplyUpgrade(new[] { new StatGain("projectile_count", 1f) });
        equipped.SetValue(_player, radio);
        activate.Invoke(_player, new object[] { radio.SpecialEffect });
        float twoWaves = (float)coneDamage.GetValue(_player);
        deactivate.Invoke(_player, null);
        Check(oneWave > 0f && Mathf.IsEqualApprox(twoWaves, oneWave * 2f),
            $"Transistor à 2 ondes : cône de {oneWave:0.#} à {twoWaves:0.#} dégâts de base");
        crit.SetValue(_player, critBefore);

        Check(Mathf.IsEqualApprox(PassiveSouvenirDataLoader.Get("souffle_du_neant").OfferWeight, 2f)
              && Mathf.IsEqualApprox(PassiveSouvenirDataLoader.Get("ancrage").OfferWeight, 1f),
            "Papier carbone pèse 2 dans les offres, les autres objets 1");

        WeaponVisualConfig visuals = WeaponVisualConfig.Load();
        Check(Mathf.IsEqualApprox(visuals.ScaleFor(1f), 1f) && Mathf.IsEqualApprox(visuals.ScaleFor(1.5f), 1.5f)
              && Mathf.IsEqualApprox(visuals.ScaleFor(3f), visuals.MaxScale) && Mathf.IsEqualApprox(visuals.ScaleFor(0.6f), 1f),
            $"Visuels d'arme (G6e) : taille ×1,5 → échelle {visuals.ScaleFor(1.5f):0.0#}, plafond {visuals.MaxScale:0.0#}, jamais sous 1");
    }

    /// <summary>
    /// Plan 22 C2a : la Trempe ajoute une stat aux 5 améliorations suivantes, décomptée à l'application ; la Retrempe
    /// remplace les gains de la dernière amélioration sans changer le niveau.
    /// </summary>
    private void CheckTemper()
    {
        WeaponInstance blade = FindSlot("chipped_blade") ?? AddAndFind("chipped_blade");
        RandomNumberGenerator rng = new() { Seed = 21 };
        UpgradeRarity common = UpgradeRoller.Get("common");
        FragmentOption Offer() => UpgradeRoller.RollGains(new FragmentOption(blade.Id, "weapon_upgrade", blade.Name), _player, common, rng);

        _player.GrantTemper(5);
        bool tempered = true;
        for (int i = 0; i < 5; i++)
        {
            FragmentOption option = Offer();
            tempered &= option.WeaponGains.Count == 2 && option.ApplyTo(_player);
        }
        FragmentOption plain = Offer();
        Check(tempered && _player.TemperCharges == 0 && plain.WeaponGains.Count == 1,
            $"Trempe : 5 améliorations communes à 2 stats, charges {_player.TemperCharges}, puis {plain.WeaponGains.Count} stat");

        // Amélioration U1, puis Retrempe en U2 : l'arme doit valoir celle qui aurait reçu U2 à la place de U1.
        List<StatGain> first = UpgradeRoller.RollWeaponGains(blade, common, rng);
        List<StatGain> second = UpgradeRoller.RollWeaponGains(blade, UpgradeRoller.Get("rare"), rng);
        WeaponInstance expected = blade.PreviewWith(second);
        _player.UpgradeWeapon(blade.Id, first, "common");
        int level = blade.Level;
        bool retempered = _player.RetemperWeapon(blade.Id, second, "rare");
        bool same = true;
        foreach (string stat in blade.Base.Growth.Keys)
            same &= Mathf.IsEqualApprox(blade.GetStat(stat), expected.GetStat(stat));
        Check(retempered && same && blade.Level == level && blade.LastRarityId == "rare" && blade.LastGains.Count == second.Count,
            $"Retrempe : gains de la dernière amélioration remplacés ({first.Count} → {second.Count} stats, rare), niveau {level} inchangé");
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
