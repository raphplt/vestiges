using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Lot 6C : trace exacte avant/après, réactivation et travail utile de l'Effacement.</summary>
public partial class ErasureActiveRegression : Node
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private Player _player;
    private EventBus _bus;
    private int _checks;
    private int _failures;
    private readonly List<object> _results = new();
    private readonly List<string> _events = new();
    private int _step;
    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private)!.GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private)!.SetValue(target, value);
    private void Check(bool condition, string description)
    {
        _checks++;
        if (!condition) _failures++;
        GD.Print($"[ErasureActiveRegression] {(condition ? "PASS" : "FAIL")} {description}");
    }

    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            GD.Seed(221092026);
            _bus = GetNode<EventBus>("/root/EventBus");
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_player);
            _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
            _player.IsGodMode = true;
            _player.IsAIControlled = true;
            _player.ProcessMode = ProcessModeEnum.Disabled;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Reactivation();
            ReentrantSignals();
            Roaming();
            ZeroCells();
            string[] args = OS.GetCmdlineUserArgs();
            string output = args[Array.IndexOf(args, "--output") + 1];
            System.IO.File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                seed = 221092026, checks = _checks, failures = _failures,
                results = _results, events = _events,
            }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"[ErasureActiveRegression] RESULT checks={_checks} failures={_failures}");
            await GameExit.QuitAsync(GetTree(), _failures == 0 ? 0 : 1);
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(2); }
    }

    private ErasureManager NewErasure(int radius = 0)
    {
        ErasureManager erasure = new() { ProcessMode = ProcessModeEnum.Disabled };
        AddChild(erasure);
        Set(erasure, "_player", _player);
        Set(erasure, "_trackedRadiusCells", radius);
        Set(erasure, "_voidDamageRatioPerSecond", 0f);
        _player.Position = Vector2.Zero;
        return erasure;
    }

    private static int Visits(ErasureManager erasure)
    {
        // Même instrument sur l'ancienne boucle et la nouvelle, sans recompter les zéros après le pas.
        PropertyInfo counter = typeof(ErasureManager).GetProperty("LastAdvancedCellCount", Private);
        return counter != null ? (int)counter.GetValue(erasure)
            : Get<List<Vector2I>>(erasure, "_cellsToUpdate").Count;
    }

    private void Reactivation()
    {
        ErasureManager erasure = NewErasure();
        for (int i = 0; i < 130; i++) erasure.OverrideMemory(new Vector2I(i - 65, -3), 0f);
        erasure.OverrideMemory(Vector2I.Zero, 0f);
        erasure._Process(0.5);
        Check(erasure.GetMemoryAt(Vector2.Zero) == 0, "Néant conservé sans recréation à l'approche");
        foreach (int index in new[] { 0, 63, 64, 65, 127, 129 })
        {
            Vector2I cell = new(index - 65, -3);
            erasure.OverrideMemory(cell, 0.4f);
            erasure._Process(0.5);
            float memory = erasure.GetMemoryAt(erasure.CellCenterToWorld(cell));
            Check(memory > 0 && memory < 0.4f, $"réactivation à l'indice {index}, coordonnées négatives et limites 64 bits");
            erasure.OverrideMemory(cell, 0f);
        }
        foreach (string source in new[] { "memorial", "chest", "poi", "souvenir" })
        {
            erasure.OverrideMemory(Vector2I.Zero, 0f);
            if (source == "memorial") _bus.EmitSignal(EventBus.SignalName.MemorialAwakened, Vector2.Zero);
            if (source == "chest") _bus.EmitSignal(EventBus.SignalName.ChestOpened, "fixture", "common", Vector2.Zero);
            if (source == "poi") _bus.EmitSignal(EventBus.SignalName.PoiDiscovered, "fixture", "fixture", Vector2.Zero);
            if (source == "souvenir") _bus.EmitSignal(EventBus.SignalName.SouvenirDiscovered, "fixture", "fixture", "fixture");
            float revived = erasure.GetMemoryAt(Vector2.Zero);
            erasure._Process(0.5);
            Check(revived == 0.72f && erasure.GetMemoryAt(Vector2.Zero) < revived,
                $"{source} : zone nulle ravivée puis déclin repris");
        }
        erasure.OverrideMemory(Vector2I.Zero, 2f);
        Check(erasure.GetMemoryAt(Vector2.Zero) == 1, "surcharge positive bornée à un");
        erasure.OverrideMemory(Vector2I.Zero, -1f);
        Check(erasure.GetMemoryAt(Vector2.Zero) == 0, "surcharge négative bornée à zéro");
        erasure.Free();
    }

    private void ReentrantSignals()
    {
        ErasureManager erasure = NewErasure();
        // Ordre historique : A, B, C. Au passage de A au Néant, un abonné ravive B, A et une zone neuve D.
        Vector2I a = Vector2I.Zero, b = new(1, 0), c = new(2, 0), d = new(3, 0);
        erasure.OverrideMemory(a, 0.00001f);
        erasure.OverrideMemory(b, 0f);
        erasure.OverrideMemory(c, 0.8f);
        bool revived = false;
        void Changed(int x, int y, int phase)
        {
            if (revived || x != 0 || y != 0 || phase != 4) return;
            revived = true;
            erasure.OverrideMemory(a, 0.5f);
            erasure.OverrideMemory(b, 0.5f);
            erasure.OverrideMemory(d, 0.5f);
        }
        _bus.ZonePhaseChanged += Changed;
        erasure._Process(0.5);
        _bus.ZonePhaseChanged -= Changed;
        Check(revived && erasure.GetMemoryAt(erasure.CellCenterToWorld(b)) < 0.5f,
            "signal réentrant : une ancienne zone encore à venir est traitée au même pas");
        Check(erasure.GetMemoryAt(Vector2.Zero) == 0.5f, "signal réentrant : zone déjà traitée attend le pas suivant");
        Check(erasure.GetMemoryAt(erasure.CellCenterToWorld(d)) == 0.5f, "signal réentrant : nouvelle zone attend le pas suivant");
        erasure._Process(0.5);
        Check(erasure.GetMemoryAt(Vector2.Zero) < 0.5f && erasure.GetMemoryAt(erasure.CellCenterToWorld(d)) < 0.5f,
            "pas suivant : les deux zones différées reprennent leur déclin");
        erasure.Free();
    }

    private void Roaming()
    {
        ErasureManager erasure = NewErasure(14);
        void Zone(int x, int y, int phase) => _events.Add($"{_step}:zone:{x}:{y}:{phase}");
        void PlayerPhase(int phase) => _events.Add($"{_step}:player:{phase}");
        _bus.ZonePhaseChanged += Zone;
        _bus.PlayerErasurePhaseChanged += PlayerPhase;
        List<object> trace = new();
        long totalVisits = 0;
        for (_step = 0; _step < 3600; _step++)
        {
            float time = _step * 0.5f;
            _player.Position = new Vector2(5000f * Mathf.Sin(time / 50f), 2200f * Mathf.Sin(time / 91f));
            if (_step % 480 == 360) _bus.EmitSignal(EventBus.SignalName.CrisisStarted, _step / 480 + 1, 1);
            if (_step % 480 == 40) _bus.EmitSignal(EventBus.SignalName.CrisisEnded, _step / 480);
            if (_step == 900) _bus.EmitSignal(EventBus.SignalName.OubliEffectChanged, "erasure_speed", 0.4f);
            if (_step == 1500) _bus.EmitSignal(EventBus.SignalName.OubliEffectChanged, "erasure_speed", 0f);
            if (_step % 300 == 200) _bus.EmitSignal(EventBus.SignalName.MemorialAwakened, _player.Position);
            if (_step % 400 == 250) _bus.EmitSignal(EventBus.SignalName.ChestOpened, "fixture", "common", Vector2.Zero);
            erasure._Process(0.5);
            Dictionary<Vector2I, float> memory = Get<Dictionary<Vector2I, float>>(erasure, "_zoneMemory");
            Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases = Get<Dictionary<Vector2I, ErasureManager.ErasureZonePhase>>(erasure, "_zonePhases");
            ulong hash = 14695981039346656037UL;
            int active = 0;
            foreach (KeyValuePair<Vector2I, float> entry in memory)
            {
                Hash(ref hash, (uint)entry.Key.X); Hash(ref hash, (uint)entry.Key.Y);
                Hash(ref hash, BitConverter.SingleToUInt32Bits(entry.Value)); Hash(ref hash, (uint)phases[entry.Key]);
                if (entry.Value > 0) active++;
            }
            foreach (byte value in Get<byte[]>(erasure, "_memoryBytes")) Hash(ref hash, value);
            Hash(ref hash, BitConverter.SingleToUInt32Bits(erasure.GlobalErasurePercent));
            totalVisits += Visits(erasure);
            trace.Add(new { step = _step, tracked = memory.Count, active, state_hash = hash.ToString("x16"), visits = Visits(erasure) });
            if ((_step + 1) % 600 == 0)
                GD.Print($"[ErasureActiveRegression] {_step / 2 + 1}s zones={memory.Count} actives={active} visites={Visits(erasure)}");
        }
        _bus.ZonePhaseChanged -= Zone;
        _bus.PlayerErasurePhaseChanged -= PlayerPhase;
        Check(_events.Count > 1000 && erasure.GlobalErasurePercent == 1f, "parcours 30 min : nombreuses transitions, Effacement global complet");
        _results.Add(new { scenario = "roaming", seconds = 1800, visits = totalVisits, trace });
        erasure.Free();
    }

    private static void Hash(ref ulong hash, uint value) => hash = unchecked((hash ^ value) * 1099511628211UL);

    private void ZeroCells()
    {
        foreach (int count in new[] { 841, 6000 })
        {
            ErasureManager erasure = NewErasure();
            for (int i = 0; i < count; i++) erasure.OverrideMemory(new Vector2I(i, 0), 0f);
            for (int warm = 0; warm < 10; warm++) erasure._Process(0.5);
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) erasure._Process(0.5);
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            _results.Add(new { scenario = "zero_cells", cells = count, updates = 100, visits_per_update = Visits(erasure), allocated_bytes = allocated });
            Check(Get<Dictionary<Vector2I, float>>(erasure, "_zoneMemory").Count == count,
                $"{count} zones nulles : mémoire historique conservée");
            erasure.Free();
        }
    }
}
