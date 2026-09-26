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
            await CheckHitEffectsFollowSource();
            CheckSingleLevel();
            CheckBanishUpgrade();

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
