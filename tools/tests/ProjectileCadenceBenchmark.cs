using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Cadence isolée : six Hurleurs, deux Cracheurs fixes et une trajectoire périodique identique.
/// Capacités et projectiles réels ; pas de morts, choix de build ou renforts qui changeraient la composition.
/// Le cri est exécuté et compté ; sa création de renforts est couverte par EnemyAbilityRegression.
/// </summary>
public partial class ProjectileCadenceBenchmark : Node2D
{
    private const float Dt = 1f / 60f;
    private const int Ticks = 60 * 90;
    private readonly List<(Enemy Enemy, IEnemyAbility Shot, IEnemyAbility Cry)> _shooters = new();
    private readonly List<EnemyProjectile> _projectiles = new();
    private static readonly FieldInfo AbilityCache = typeof(Enemy).GetField("_abilityCache", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo Cooldown = typeof(AimedShotAbility).GetField("_cooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance);

    public override async void _Ready()
    {
        try
        {
            await Run();
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"[ProjectileCadenceBenchmark] {exception}");
            GetTree().Quit(1);
        }
    }

    private async Task Run()
    {
        GD.Seed(46);
        float multiplier = ReadNumber("--howler-cooldown", 1f);
        float aggression = ReadNumber("--aggression", 1f);
        GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        player.IsAIControlled = true;
        player.IsGodMode = true;
        player.SetPhysicsProcess(false);
        foreach (Node child in player.GetChildren())
            if (child is Timer timer)
                timer.Stop();
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        pools.ChildEnteredTree += child =>
        {
            if (child is EnemyProjectile projectile)
                _projectiles.Add(projectile);
        };
        PackedScene enemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");
        EnemyDataLoader.Get("hurleur").Abilities[EnemyAbilityKind.AimedShot].Numbers["cooldown_multiplier"] = multiplier;
        for (int i = 0; i < 8; i++)
        {
            Enemy enemy = enemyScene.Instantiate<Enemy>();
            AddChild(enemy);
            enemy.Initialize(EnemyDataLoader.Get(i < 6 ? "hurleur" : "fading_spitter"), 1000f, 1f);
            enemy.ApplySpawnTuning(1f, aggression);
            enemy.SetTicking(false);
            enemy.GlobalPosition = Vector2.FromAngle(Mathf.Tau * i / 8f) * 170f;
            Dictionary<EnemyAbilityKind, IEnemyAbility> abilities = (Dictionary<EnemyAbilityKind, IEnemyAbility>)AbilityCache.GetValue(enemy);
            IEnemyAbility shot = abilities[EnemyAbilityKind.AimedShot];
            Cooldown.SetValue(shot, 0.11f * i);
            _shooters.Add((enemy, shot, abilities.GetValueOrDefault(EnemyAbilityKind.Cry)));
        }
        int howlerShots = 0, spitterShots = 0, cries = 0, occupiedTicks = 0, peak = 0;
        double sum = 0, damage = 0;
        EventBus events = GetNode<EventBus>("/root/EventBus");
        EventBus.PlayerHitByEventHandler onHit = (_, amount) => damage += amount;
        events.PlayerHitBy += onHit;
        for (int tick = 0; tick < Ticks; tick++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            float time = tick * Dt;
            player.GlobalPosition = new Vector2(Mathf.Cos(time * 1.5f) * 65f, Mathf.Sin(time * 1.5f) * 40f);
            foreach ((Enemy enemy, IEnemyAbility shot, IEnemyAbility cry) in _shooters)
            {
                float distance = enemy.GlobalPosition.DistanceTo(player.GlobalPosition);
                bool aiming = shot.IsActive;
                shot.Process(enemy, player, distance, Dt);
                if (aiming && !shot.IsActive)
                {
                    if (enemy.EnemyId == "hurleur") howlerShots++;
                    else spitterShots++;
                }
                bool crying = cry?.IsActive ?? false;
                cry?.Process(enemy, player, distance, Dt);
                if (crying && !cry.IsActive)
                    cries++;
            }
            int active = 0;
            foreach (EnemyProjectile projectile in _projectiles)
                if (projectile.Visible)
                    active++;
            sum += active;
            peak = Math.Max(peak, active);
            if (active > 0)
                occupiedTicks++;
        }
        events.PlayerHitBy -= onHit;
        if (howlerShots == 0 || spitterShots == 0 || cries == 0)
            throw new InvalidOperationException("Le banc n'a pas exercé toutes les capacités.");
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[ProjectileCadenceBenchmark] RESULT multiplier={multiplier} aggression={aggression} seconds={Ticks * Dt:F0} howler_shots={howlerShots} spitter_shots={spitterShots} cries={cries} mean={sum / Ticks:F3} peak={peak} occupied_pct={100.0 * occupiedTicks / Ticks:F2} hit_damage={damage:F0}"));
    }

    private static float ReadNumber(string key, float fallback)
    {
        string[] args = OS.GetCmdlineUserArgs();
        int index = Array.IndexOf(args, key);
        float value = index >= 0 && index + 1 < args.Length
            ? float.Parse(args[index + 1], CultureInfo.InvariantCulture) : fallback;
        return float.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(key);
    }
}
