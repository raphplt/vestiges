using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Lot 6A : horloges indépendantes de la distance et de la cadence des images, sur les vrais systèmes.</summary>
public partial class TemporalRegression : Node
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private Player _player;
    private EventBus _bus;
    private int _failures;
    private readonly List<object> _results = new();
    private static object Field(object target, string name) => target.GetType().GetField(name, Private)!.GetValue(target);
    private static float Number(object target, string name) => Convert.ToSingle(Field(target, name));
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private)!.SetValue(target, value);
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private void Check(bool pass, string name)
    {
        if (!pass) _failures++;
        _results.Add(new { check = name, pass });
        GD.Print($"[TemporalRegression] {(pass ? "PASS" : "FAIL")} {name}");
    }

    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            GD.Seed(221092026);
            Vestiges.Core.RunRandom.Begin(221092026);
            _bus = GetNode<EventBus>("/root/EventBus");
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_player);
            _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
            _player.IsGodMode = true;
            _player.IsAIControlled = true;
            _player.SetPhysicsProcess(false);
            _player.SetProcess(false);
            foreach (Node child in _player.GetChildren()) if (child is Timer timer) timer.Stop();
            await Frame();
            await Enemies();
            await Erasure();
            string[] args = OS.GetCmdlineUserArgs();
            string output = args[Array.IndexOf(args, "--output") + 1];
            System.IO.File.WriteAllText(output, JsonSerializer.Serialize(new { seed = 221092026, failures = _failures, results = _results }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"[TemporalRegression] RESULT failures={_failures}");
            await GameExit.QuitAsync(GetTree(), _failures == 0 ? 0 : 1);
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(2); }
    }

    private async Task<Enemy> EnemyAt(float radius)
    {
        Enemy enemy = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn").Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("shade"), 1000f, 1f);
        enemy.ProcessMode = ProcessModeEnum.Pausable;
        enemy.SetTicking(false);
        enemy.SetProcess(false);
        enemy.Position = new Vector2(radius, 0);
        await Frame();
        return enemy;
    }

    private async Task Enemies()
    {
        float? nearHp = null;
        foreach (float radius in new[] { 100f, 1500f })
        {
            Enemy enemy = await EnemyAt(radius);
            enemy.ApplySlow(0.5f, 2f);
            enemy.ApplyDisorient(2f);
            enemy.ApplyKnockback(Vector2.Right, 40f);
            EnemyModifiers mods = (EnemyModifiers)Field(enemy, "_mods");
            mods.AddAffix(new EnemyAffixData { RegenRatioPerSec = 0.01f, RegenPauseAfterHitSec = 1f });
            Set(enemy, "_currentHp", Number(enemy, "_maxHp") * 0.5f);
            for (int tick = 0; tick < 600; tick++)
            {
                enemy.Position = new Vector2(radius, 0);
                enemy.PhysicsTick(1.0 / 60);
            }
            float hp = Number(enemy, "_currentHp");
            _results.Add(new { scenario = "enemy", radius, seconds = 10, slow = Number(enemy, "_slowTimer"), disorient = Number(enemy, "_disorientTimer"), hp });
            Check(Number(enemy, "_slowTimer") <= 0 && Number(enemy, "_slowFactor") == 1, $"slow expire à {radius} px");
            Check(Number(enemy, "_disorientTimer") <= 0, $"désorientation expire à {radius} px");
            Check((Vector2)Field(enemy, "_knockVelocity") == Vector2.Zero, $"recul expiré à {radius} px");
            if (nearHp.HasValue) Check(Mathf.IsEqualApprox(hp, nearHp.Value), "régénération proche/loin identique"); else nearHp = hp;
            enemy.Position = new Vector2(100, 0);
            enemy.PhysicsTick(1.0 / 60);
            Check(Number(enemy, "_slowFactor") == 1, "retour proche sans ancien slow");
            enemy.ApplySlow(0.5f, 2);
            enemy.SetTicking(true);
            GetTree().Paused = true;
            await Frame(); await Frame();
            Check(Number(enemy, "_slowTimer") == 2, "pause conserve la durée des statuts");
            enemy.SetTicking(false);
            GetTree().Paused = false;
            enemy.Reset();
            enemy.Initialize(EnemyDataLoader.Get("shade"), 1000f, 1f);
            enemy.SetTicking(false);
            Check(Number(enemy, "_slowTimer") == 0 && Number(enemy, "_disorientTimer") == 0 && (Vector2)Field(enemy, "_knockVelocity") == Vector2.Zero, "réutilisation nettoie les états");
            // Deux DOT létaux : le second ne doit pas retravailler ni déplacer un corps déjà mort.
            enemy.Position = new Vector2(radius, 0);
            Set(enemy, "_currentHp", 1f);
            enemy.ApplyIgnite(100f, 1f);
            enemy.ApplyBleed(100f, 1f);
            int damageSignals = 0;
            void Damaged(Node target, float amount) { if (target == enemy) damageSignals++; }
            _bus.EntityDamaged += Damaged;
            Vector2 origin = enemy.Position;
            enemy.PhysicsTick(1.0 / 60);
            _bus.EntityDamaged -= Damaged;
            Check(enemy.IsDying && damageSignals == 1 && enemy.Position == origin, "mort par DOT arrête le tick");
            enemy.QueueFree(); await Frame();
        }
    }

    private ErasureManager NewErasure(bool inVoid = false)
    {
        ErasureManager erasure = new() { ProcessMode = ProcessModeEnum.Pausable };
        AddChild(erasure);
        erasure.SetProcess(false);
        Set(erasure, "_player", _player);
        // Une seule cellule suffit à la comparaison des horloges ; la texture et les signaux restent réels.
        Set(erasure, "_trackedRadiusCells", 0);
        if (inVoid) erasure.OverrideMemory(Vector2I.Zero, 0f);
        return erasure;
    }

    private async Task Erasure()
    {
        float reference = 0;
        foreach (int hz in new[] { 60, 30, 144, 1 })
        {
            ErasureManager erasure = NewErasure();
            for (int i = 0; i < hz * 600; i++) erasure._Process(1.0 / hz);
            float global = erasure.GlobalErasurePercent;
            _results.Add(new { scenario = "erasure_clock", hz, seconds = 600, global });
            if (hz == 60) reference = global;
            else Check(Mathf.Abs(global - reference) < 0.0001f, $"600 s à {hz} Hz équivalent à 60 Hz");
            erasure.Free();
        }
        ErasureManager regular = NewErasure();
        for (int i = 0; i < 20; i++) regular._Process(0.5);
        float tenSeconds = regular.GlobalErasurePercent;
        regular.Free();
        ErasureManager stalled = NewErasure();
        int publications = 0;
        void Updated(float amount) => publications++;
        _bus.ErasureUpdated += Updated;
        stalled._Process(10);
        Check(publications > 0 && publications <= 4, "hitch de 10 s : au plus 4 pas par image");
        for (int i = 0; i < 10; i++) stalled._Process(0);
        Check(Mathf.Abs(stalled.GlobalErasurePercent - tenSeconds) < 0.000001f, "dette de 10 s rattrapée sans perte");
        _bus.ErasureUpdated -= Updated;
        stalled.Free();
        ErasureManager jitter = NewErasure();
        for (int i = 0; i < 50; i++) { jitter._Process(0.13); jitter._Process(0.07); }
        Check(Mathf.Abs(jitter.GlobalErasurePercent - tenSeconds) < 0.000001f, "reste temporel conservé sous jitter");
        double elapsedBefore = Convert.ToDouble(Field(jitter, "_totalElapsed"));
        jitter.SetProcess(true);
        GetTree().Paused = true;
        await Frame(); await Frame();
        Check(Convert.ToDouble(Field(jitter, "_totalElapsed")) == elapsedBefore, "pause : horloge d’Effacement figée");
        jitter.SetProcess(false); GetTree().Paused = false; jitter.Free();
        // Le delta fourni est déjà multiplié par Engine.TimeScale : simuler dix secondes à demi-vitesse.
        ErasureManager slowed = NewErasure();
        for (int i = 0; i < 600; i++) slowed._Process(0.5 / 60);
        ErasureManager fiveSeconds = NewErasure();
        for (int i = 0; i < 10; i++) fiveSeconds._Process(0.5);
        Check(Mathf.Abs(slowed.GlobalErasurePercent - fiveSeconds.GlobalErasurePercent) < 0.000001f, "ralenti : cinq secondes simulées, pas dix secondes murales");
        slowed.Free(); fiveSeconds.Free();
        // Signal des dégâts et perte de PV réelle, sans invulnérabilité ni bouclier sur ce chemin.
        foreach (double delta in new[] { 0.5, 1.0 })
        {
            ErasureManager erasure = NewErasure(true);
            float hp = _player.CurrentHp;
            _player.IsGodMode = false;
            int hits = 0;
            void Hit(string source, float amount) { if (source == "void") hits++; }
            _bus.PlayerHitBy += Hit;
            for (int i = 0; i < 2 / delta; i++) erasure._Process(delta);
            _bus.PlayerHitBy -= Hit;
            _player.IsGodMode = true;
            Check(hits == 4 && Mathf.Abs(hp - _player.CurrentHp - _player.EffectiveMaxHp * 0.12f) < 0.001f, $"Néant : 4 impacts et 12 % des PV en 2 s, delta={delta}");
            erasure.Free();
        }
        // Déclin accéléré du fixture : quatre changements de phase doivent survivre au rattrapage.
        ErasureManager transitions = NewErasure();
        Set(transitions, "_baseDecayPerMinute", 30f);
        Set(transitions, "_globalAccelerationPerMinute", 0f);
        Set(transitions, "_playerDecayReduction", 0f);
        Set(transitions, "_voidDamageRatioPerSecond", 0f);
        transitions.OverrideMemory(Vector2I.Zero, 1f);
        List<int> phases = new();
        void Phase(int x, int y, int phase) { if (x == 0 && y == 0) phases.Add(phase); }
        _bus.ZonePhaseChanged += Phase;
        transitions._Process(2);
        _bus.ZonePhaseChanged -= Phase;
        Check(phases.Count == 4 && phases[0] == 1 && phases[1] == 2 && phases[2] == 3 && phases[3] == 4, "rattrapage conserve les quatre transitions de phase");
        transitions.Free();
    }
}
