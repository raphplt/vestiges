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
            CheckUpgradeGains();
            CheckRangeAndZone();

            GD.Print($"[WeaponRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
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

        _player.OnProjectileHit(enemy, 1f, false, false, bow);
        float bleed = (float)typeof(Enemy).GetField("_bleedTimer", Private).GetValue(enemy);
        Check(bleed <= 0f, $"flèche de l'arc après un coup de masse : pas de saignement emprunté (timer {bleed:F1} s)");

        _player.OnProjectileHit(enemy, 1f, false, false, mace);
        bleed = (float)typeof(Enemy).GetField("_bleedTimer", Private).GetValue(enemy);
        Check(bleed > 0f, $"coup de la masse : saignement appliqué (timer {bleed:F1} s)");
        enemy.QueueFree();
    }

    private void CheckSingleLevel()
    {
        WeaponInstance equipped = _player.EquippedWeapon;
        int before = _player.GetWeaponFragmentLevel(equipped.Id);
        bool upgraded = _player.UpgradeEquippedWeaponAtAltar();
        int after = _player.GetWeaponFragmentLevel(equipped.Id);
        Check(upgraded && after == before + 1 && after == _player.EquippedWeapon.Level,
            $"Autel : badge et arme montent ensemble ({before} → {after}, arme {_player.EquippedWeapon.Level})");
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

    private void Check(bool passed, string message)
    {
        GD.Print($"[WeaponRegression] {(passed ? "PASS" : "FAIL")} {message}");
        if (!passed) _failures++;
    }
}
