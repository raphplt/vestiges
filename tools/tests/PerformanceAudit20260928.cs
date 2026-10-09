using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Diagnostics isolés du 28 septembre : appels réels, aucun remplacement du code de production.</summary>
public partial class PerformanceAudit20260928 : Node
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private WorldSetup _world;
    private Player _player;
    private readonly List<object> _results = new();
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, Private)
        ?? throw new MissingFieldException(target.GetType().Name, name);
    private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static T Get<T>(object target, string name) => (T)Field(target, name).GetValue(target);
    private static T Method<T>(object target, string name) where T : Delegate =>
        target.GetType().GetMethod(name, Private)!.CreateDelegate<T>(target);
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            await Frame();
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--cycles") >= 0) { await Cycles(); return; }
            GD.Seed(221092026);
            GameManager gm = GetNode<GameManager>("/root/GameManager");
            gm.RunSeed = 221092026;
            gm.SelectedCharacterId = "traqueur";
            _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(_world);
            GetTree().CurrentScene = _world;
            while (!_world.IsWorldReady || GetTree().Paused || gm.CurrentState != GameManager.GameState.Run)
                await Frame();
            // Tout le monde est figé ; seul le recyclage des retours visuels reste vivant pendant le test cône.
            KeepBodiesActive(_world);
            _world.ProcessMode = ProcessModeEnum.Disabled;
            _player = _world.GetNode<Player>("Player");
            _player.IsGodMode = true;
            _player.GlobalPosition = Vector2.Zero;
            foreach (Node enemy in GetTree().GetNodesInGroup("enemies")) enemy.QueueFree();
            await Frame();
            Census();

            Erasure();
            await ConeAndCrowd();
            string[] args = OS.GetCmdlineUserArgs();
            int outputIndex = Array.IndexOf(args, "--output");
            string output = outputIndex >= 0 ? args[outputIndex + 1] : "/tmp/vestiges-audit-systems.json";
            System.IO.File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                seed = 221092026, engine = Engine.GetVersionInfo()["string"].AsString(),
                display = DisplayServer.GetName(), fixed_delta = 1.0 / 60,
                evidence = "reproduit isolément ; Main figée, méthodes de production appelées par délégués ; durées non FPS",
                results = _results
            }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print("[PerformanceAudit] RESULT " + output);
            _world.QueueFree();
            await Frame();
            await Frame();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Frame();
            GetTree().Quit();
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task Cycles()
    {
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        Node hub = GD.Load<PackedScene>("res://scenes/Hub.tscn").Instantiate();
        GetTree().Root.AddChild(hub);
        GetTree().CurrentScene = hub;
        for (int cycle = 0; cycle <= 20; cycle++)
        {
            for (int frame = 0; frame < 10; frame++) await Frame();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            await Frame();
            _results.Add(new { cycle, nodes = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
                orphans = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
                objects = Performance.GetMonitor(Performance.Monitor.ObjectCount),
                resources = Performance.GetMonitor(Performance.Monitor.ObjectResourceCount),
                native_bytes = Performance.GetMonitor(Performance.Monitor.MemoryStatic),
                managed_bytes = GC.GetTotalMemory(true), rss_bytes = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 });
            GD.Print("[PerformanceAudit] cycle " + cycle);
            if (cycle == 20) break;
            hub.QueueFree();
            await Frame();
            gm.RunSeed = 221092026;
            gm.SelectedCharacterId = "traqueur";
            GD.Seed(221092026);
            _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(_world);
            GetTree().CurrentScene = _world;
            while (!_world.IsWorldReady || GetTree().Paused || gm.CurrentState != GameManager.GameState.Run) await Frame();
            _world.GetNode<Player>("Player").IsGodMode = true;
            for (int frame = 0; frame < 60; frame++) await Frame();
            GetTree().Paused = false;
            _world.QueueFree();
            await Frame(); await Frame();
            _world = null;
            gm.ChangeState(GameManager.GameState.Hub);
            hub = GD.Load<PackedScene>("res://scenes/Hub.tscn").Instantiate();
            GetTree().Root.AddChild(hub);
            GetTree().CurrentScene = hub;
        }
        string[] args = OS.GetCmdlineUserArgs();
        string output = args[Array.IndexOf(args, "--output") + 1];
        System.IO.File.WriteAllText(output, JsonSerializer.Serialize(new { seed = 221092026, cycles = _results }, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print("[PerformanceAudit] RESULT " + output);
        hub.QueueFree(); await Frame(); await Frame();
        GC.Collect(); GC.WaitForPendingFinalizers(); await Frame(); GetTree().Quit();
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void KeepBodiesActive(Node root)
    {
        foreach (CollisionObject2D body in Descendants(root).OfType<CollisionObject2D>())
            body.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
    }

    private void Census()
    {
        List<EnvironmentProp> props = Descendants(_world).OfType<EnvironmentProp>().ToList();
        int shapes = 0, shapeless = 0, layerZero = 0, zeroWithShapes = 0;
        foreach (EnvironmentProp prop in props)
        {
            int count = PhysicsServer2D.BodyGetShapeCount(prop.GetRid());
            shapes += count;
            if (count == 0) shapeless++;
            if (prop.CollisionLayer == 0) { layerZero++; if (count > 0) zeroWithShapes++; }
        }
        _results.Add(new { kind = "map_census", props = props.Count, shapes, shapeless, layerZero, zeroWithShapes,
            nodes = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount) });
    }

    private ErasureManager NewErasure()
    {
        ErasureManager erasure = new() { ProcessMode = ProcessModeEnum.Disabled };
        AddChild(erasure);
        Set(erasure, "_player", _player);
        // Les abonnés EventBus restent branchés : le coût inclut la propagation réelle des signaux.
        // _Ready a connecté les événements ; ils sont déconnectés normalement par _ExitTree.
        return erasure;
    }

    private void Erasure()
    {
        foreach (int hz in new[] { 60, 30, 144, 1 })
        {
            ErasureManager erasure = NewErasure();
            _player.GlobalPosition = Vector2.Zero;
            int updates = 0;
            EventBus bus = GetNode<EventBus>("/root/EventBus");
            // Dans ce scénario stationnaire de 10 min, chaque pas modifie encore la mémoire globale.
            void Updated(float amount) => updates++;
            bus.ErasureUpdated += Updated;
            for (int i = 0; i < hz * 600; i++)
                erasure._Process(1.0 / hz);
            bus.ErasureUpdated -= Updated;
            _results.Add(new { kind = "erasure_clock", hz, seconds = 600, updates,
                global = erasure.GlobalErasurePercent, origin_memory = Get<Dictionary<Vector2I, float>>(erasure, "_zoneMemory")[Vector2I.Zero] });
            erasure.Free();
        }
        // Trajet synthétique sur une carte finie : traversées rectangulaires, aucune téléportation hors monde.
        ErasureManager roaming = NewErasure();
        List<object> checkpoints = new();
        double[] intervals = new double[3600];
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long visits = 0, zeroVisits = 0;
        for (int i = 0; i < intervals.Length; i++)
        {
            float t = i * 0.5f;
            _player.GlobalPosition = new Vector2(5000f * Mathf.Sin(t / 50f), 2200f * Mathf.Sin(t / 91f));
            long start = Stopwatch.GetTimestamp();
            roaming._Process(0.5);
            intervals[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            Dictionary<Vector2I, float> memory = Get<Dictionary<Vector2I, float>>(roaming, "_zoneMemory");
            visits += roaming.LastAdvancedCellCount;
            int zeros = memory.Values.Count(v => v == 0f);
            zeroVisits += zeros;
            if ((i + 1) % 600 == 0) checkpoints.Add(new { seconds = (i + 1) / 2, cells = memory.Count, zeros });
        }
        // Allocations de l'observation incluses ici : ne pas attribuer ce compteur à ErasureManager seul.
        _results.Add(new { kind = "erasure_roaming", checkpoints, visits, zero_cells_after_update = zeroVisits,
            intervals_ms = intervals, allocation_including_observer = GC.GetAllocatedBytesForCurrentThread() - allocated });
        roaming.Free();
        _player.GlobalPosition = Vector2.Zero;
        // Coût strict des appels, listes déjà dimensionnées ; ordres ABBA, cellules déjà à zéro.
        foreach (int cells in new[] { 841, 6000, 6000, 841 })
        {
            ErasureManager erasure = NewErasure();
            Dictionary<Vector2I, float> memory = Get<Dictionary<Vector2I, float>>(erasure, "_zoneMemory");
            for (int i = 0; i < cells; i++) erasure.OverrideMemory(new Vector2I(i % 29 - 14, i / 29 - 14), 0f);
            erasure._Process(0.5);
            double[] times = new double[100];
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < times.Length; i++)
            {
                long start = Stopwatch.GetTimestamp(); erasure._Process(0.5);
                times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            }
            _results.Add(new { kind = "erasure_zero_cells", cells = memory.Count, intervals_ms = times,
                allocated_bytes = GC.GetAllocatedBytesForCurrentThread() - bytes });
            erasure.Free();
        }
    }

    private async Task ConeAndCrowd()
    {
        CombatPools.Instance.ProcessMode = ProcessModeEnum.Always;
        EnemyPool pool = _world.GetNode<EnemyPool>("EnemyPool");
        Node container = _world.GetNode("EnemyContainer");
        Enemy[] enemies = new Enemy[240];
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = pool.Get(); container.AddChild(enemy);
            enemy.Initialize(EnemyDataLoader.Get("shade"), 100000f, 1f);
            enemy.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
            enemy.SetTicking(false);
            enemy.ProcessMode = ProcessModeEnum.Disabled;
            enemy.Position = new Vector2(80 + i % 20, (i % 3 - 1) * 2);
            enemies[i] = enemy;
        }
        await Frame();
        // Appels directs des vraies méthodes : attribution isolée, pas parts exclusives de la frame.
        foreach (int count in new[] { 60, 240, 240, 60 })
        {
            Action<float>[] animations = enemies.Take(count).Select(e => Method<Action<float>>(e, "UpdateSpriteAnimation")).ToArray();
            Action<float>[] moves = enemies.Take(count).Select(e => Method<Action<float>>(e, "MoveWithKnockback")).ToArray();
            Action<float>[] ai = enemies.Take(count).Select(e => Method<Action<float>>(e, "ProcessBehaviorAbilities")).ToArray();
            foreach (string part in new[] { "observer", "animation", "move", "behavior", "physics_total" })
            {
                double[] times = new double[100];
                long bytes = GC.GetAllocatedBytesForCurrentThread();
                for (int sample = 0; sample < times.Length; sample++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        enemies[i].Position = Vector2.FromAngle(i * 2.3999632f) * (40 + 100 * Mathf.Sqrt((i + 1f) / count));
                        enemies[i].Velocity = new Vector2(-60, 0);
                    }
                    long start = Stopwatch.GetTimestamp();
                    for (int i = 0; i < count; i++)
                    {
                        Enemy enemy = enemies[i];
                        switch (part)
                        {
                            case "observer": if (IsInstanceValid(enemy) && enemy.IsActive && !enemy.IsDying && enemy.HpRatio > 0)
                                _ = enemy.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) <= 360000; break;
                            case "animation": animations[i](1f / 60); break;
                            case "move": moves[i](1f / 60); break;
                            case "behavior": ai[i](1f / 60); break;
                            case "physics_total": enemy.PhysicsTick(1.0 / 60); break;
                        }
                    }
                    times[sample] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                }
                _results.Add(new { kind = "crowd_method", count, part, intervals_ms = times,
                    allocated_bytes = GC.GetAllocatedBytesForCurrentThread() - bytes });
            }
        }
        foreach (int radius in new[] { 100, 1500 })
        {
            Enemy enemy = enemies[0];
            enemy.ApplySlow(0.5f, 2f);
            enemy.ApplyDisorient(2f);
            for (int tick = 0; tick < 600; tick++)
            {
                enemy.GlobalPosition = new Vector2(radius, 0);
                enemy.PhysicsTick(1.0 / 60);
            }
            _results.Add(new { kind = "far_status", radius, simulated_seconds = 10,
                slow_remaining = Get<float>(enemy, "_slowTimer"), slow_factor = Get<float>(enemy, "_slowFactor"),
                disorient_remaining = Get<float>(enemy, "_disorientTimer") });
        }
        WeaponInstance weapon = new(WeaponDataLoader.Get("last_broadcast"));
        Set(_player, "_equippedWeapon", weapon);
        Set(_player, "_facingDirection", Vector2.Right);
        Action<WeaponSpecialEffect> activate = Method<Action<WeaponSpecialEffect>>(_player, "ActivateSustainedCone");
        Action<float> cone = Method<Action<float>>(_player, "ProcessSustainedCone");
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        int hits = 0; double damage = 0;
        void OnDamage(Node target, float amount) { if (target is Enemy) { hits++; damage += amount; } }
        bus.EntityDamaged += OnDamage;
        foreach (int count in new[] { 0, 50, 50, 0 })
        {
            GD.Seed(221092026);
            for (int i = 0; i < enemies.Length; i++)
                enemies[i].Position = i < count ? new Vector2(80 + i % 20, (i % 3 - 1) * 2) : new Vector2(-900, 0);
            // Une activation complète chauffe les pools avant de compter deux activations identiques.
            double[] times = new double[240];
            long bytes = 0;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                activate(weapon.Base.SpecialEffect);
                if (cycle == 1) { hits = 0; damage = 0; }
                for (int tick = 0; tick < 120; tick++)
                {
                    await Frame();
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    long start = Stopwatch.GetTimestamp();
                    cone(1f / 60);
                    double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    if (cycle > 0) { times[(cycle - 1) * 120 + tick] = ms; bytes += GC.GetAllocatedBytesForCurrentThread() - before; }
                }
            }
            _results.Add(new { kind = "cone", targets = count, measured_ticks = 240, hits, damage,
                allocated_bytes_in_cone_call = bytes, intervals_ms = times });
        }
        bus.EntityDamaged -= OnDamage;
    }
}
