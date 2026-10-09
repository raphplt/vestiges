using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Events;

namespace Vestiges.Tests;

/// <summary>
/// Chronologie d'une vraie run (plan 30, --timeline) : heure de chaque temps fort (Résurgence annoncée, commencée,
/// finie ; accalmie ; micro-événement ; élite, Souverain ou aberration apparus ; coffres, lieux, niveaux) et, chaque
/// seconde, l'état de la run vu du joueur (phase, événement en cours, variantes en vie, foule et PV autour de lui).
/// Lecture seule : rien n'est modifié dans la run.
/// </summary>
internal sealed class RunTimelineProbe : IDisposable
{
    private const float NearRadius = 600f;
    private const float CloseRadius = 300f;
    private const double VariantScanSeconds = 0.25;

    private readonly string _path;
    private readonly string _beatsPath;
    private readonly SceneTree _tree;
    private readonly Player _player;
    private readonly EventBus _bus;
    private readonly GameManager _game;
    private readonly CrisisManager _crisis;
    private readonly List<string> _events = new() { "t,kind,id,detail" };
    private readonly List<string> _beats = new() { "t,phase,crisis,event,elites,elites_near,champions,champions_near,aberrations,close300,near_hp,near_variant_hp" };
    private readonly Dictionary<ulong, string> _variants = new();
    private readonly HashSet<ulong> _seen = new();
    private string _activeEvent = "";
    private bool _calm;
    private double _time;
    private double _nextScan;
    private double _nextBeat = 1.0;

    public RunTimelineProbe(string output, ulong seed, SceneTree tree, Node world, Player player, EventBus bus, GameManager game)
    {
        _path = Path.Combine(output, $"timeline-{seed}.csv");
        _beatsPath = Path.Combine(output, $"beats-{seed}.csv");
        _tree = tree;
        _player = player;
        _bus = bus;
        _game = game;
        _crisis = world.GetNode<CrisisManager>("CrisisManager");
        _bus.CrisisWarning += OnCrisisWarning;
        _bus.CrisisStarted += OnCrisisStarted;
        _bus.CrisisEnded += OnCrisisEnded;
        _bus.CrisisCalmChanged += OnCalm;
        _bus.RunEventStarted += OnEventStarted;
        _bus.RunEventEnded += OnEventEnded;
        _bus.VariantEnemyKilled += OnVariantKilled;
        _bus.ChestOpened += OnChest;
        _bus.SmallPlaceUsed += OnSmallPlace;
        _bus.MemorialAwakened += OnMemorial;
        _bus.RiftUsed += OnRift;
        _bus.WorkshopVisited += OnWorkshop;
        _bus.LevelUp += OnLevelUp;
        _bus.RunPhaseChanged += OnPhase;
        _bus.PerilChanged += OnPeril;
    }

    public void Sample(double time)
    {
        _time = time;
        if (time >= _nextScan)
        {
            _nextScan += VariantScanSeconds;
            ScanVariants();
        }
        if (time < _nextBeat)
            return;
        _nextBeat += 1.0;
        WriteBeat(time);
    }

    /// <summary>Une variante est « apparue » la première fois qu'une instance active la porte ; l'instance du pool qui la perd est oubliée.</summary>
    private void ScanVariants()
    {
        _seen.Clear();
        Vector2 origin = _player.GlobalPosition;
        foreach (Node node in _tree.GetNodesInGroup("enemies"))
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy || enemy.Modifiers.Variant == null)
                continue;
            ulong id = enemy.GetInstanceId();
            string variant = enemy.Modifiers.Variant.Id;
            _seen.Add(id);
            if (_variants.TryGetValue(id, out string known) && known == variant)
                continue;
            _variants[id] = variant;
            Add(variant, enemy.EnemyId, string.Create(CultureInfo.InvariantCulture, $"distance={enemy.GlobalPosition.DistanceTo(origin):F0}"));
        }
        List<ulong> gone = null;
        foreach (ulong id in _variants.Keys)
            if (!_seen.Contains(id))
                (gone ??= new List<ulong>()).Add(id);
        if (gone != null)
            foreach (ulong id in gone)
                _variants.Remove(id);
    }

    private void WriteBeat(double time)
    {
        int elites = 0, elitesNear = 0, champions = 0, championsNear = 0, aberrations = 0, close = 0;
        double nearHp = 0, nearVariantHp = 0;
        Vector2 origin = _player.GlobalPosition;
        foreach (Node node in _tree.GetNodesInGroup("enemies"))
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy)
                continue;
            float distance = enemy.GlobalPosition.DistanceTo(origin);
            bool near = distance <= NearRadius;
            if (distance <= CloseRadius)
                close++;
            double hp = enemy.HpRatio * enemy.MaxHp;
            if (near)
                nearHp += hp;
            switch (enemy.Modifiers.Variant?.Id)
            {
                case "elite":
                    elites++;
                    if (near) { elitesNear++; nearVariantHp += hp; }
                    break;
                case "champion":
                    champions++;
                    if (near) { championsNear++; nearVariantHp += hp; }
                    break;
                case "aberration":
                    aberrations++;
                    break;
            }
        }
        string crisis = _crisis.IsCrisisActive ? "active" : _crisis.IsWarningActive ? "warning" : _calm ? "calm" : "";
        _beats.Add(string.Create(CultureInfo.InvariantCulture,
            $"{time:F0},{_game.CurrentRunPhase},{crisis},{_activeEvent},{elites},{elitesNear},{champions},{championsNear},{aberrations},{close},{nearHp:F0},{nearVariantHp:F0}"));
    }

    private void Add(string kind, string id, string detail) => _events.Add(string.Create(CultureInfo.InvariantCulture,
        $"{_time:F1},{kind},{id},{detail}"));

    private void OnCrisisWarning(int number, float countdown) => Add("crisis_warning", number.ToString(CultureInfo.InvariantCulture),
        string.Create(CultureInfo.InvariantCulture, $"countdown={countdown:F0}"));
    private void OnCrisisStarted(int number, int intensity) => Add("crisis_start", number.ToString(CultureInfo.InvariantCulture),
        string.Create(CultureInfo.InvariantCulture, $"intensity={intensity}"));
    private void OnCrisisEnded(int number) => Add("crisis_end", number.ToString(CultureInfo.InvariantCulture), "");
    private void OnCalm(bool active)
    {
        _calm = active;
        Add(active ? "calm_start" : "calm_end", "", "");
    }
    private void OnEventStarted(string id, string title, string objective, float duration)
    {
        _activeEvent = id;
        Add("event_start", id, string.Create(CultureInfo.InvariantCulture, $"duration={duration:F0}"));
    }
    private void OnEventEnded(string id, bool success, string summary)
    {
        _activeEvent = "";
        Add("event_end", id, success ? "success" : "failure");
    }
    private void OnVariantKilled(string name, string variant, Vector2 position) => Add("variant_killed", variant, "");
    private void OnChest(string id, string rarity, Vector2 position) => Add("chest", id, rarity);
    private void OnSmallPlace(string id, Vector2 position) => Add("small_place", id, "");
    private void OnMemorial(Vector2 position) => Add("memorial", "", "");
    private void OnRift(Vector2 position) => Add("rift", "", "");
    private void OnWorkshop(Vector2 position) => Add("workshop", "", "");
    private void OnLevelUp(int level) => Add("level", level.ToString(CultureInfo.InvariantCulture), "");
    private void OnPhase(string from, string to) => Add("phase", to, from);
    private void OnPeril(int peril) => Add("peril", peril.ToString(CultureInfo.InvariantCulture), "");

    public void Dispose()
    {
        _bus.CrisisWarning -= OnCrisisWarning;
        _bus.CrisisStarted -= OnCrisisStarted;
        _bus.CrisisEnded -= OnCrisisEnded;
        _bus.CrisisCalmChanged -= OnCalm;
        _bus.RunEventStarted -= OnEventStarted;
        _bus.RunEventEnded -= OnEventEnded;
        _bus.VariantEnemyKilled -= OnVariantKilled;
        _bus.ChestOpened -= OnChest;
        _bus.SmallPlaceUsed -= OnSmallPlace;
        _bus.MemorialAwakened -= OnMemorial;
        _bus.RiftUsed -= OnRift;
        _bus.WorkshopVisited -= OnWorkshop;
        _bus.LevelUp -= OnLevelUp;
        _bus.RunPhaseChanged -= OnPhase;
        _bus.PerilChanged -= OnPeril;
        File.WriteAllLines(_path, _events);
        File.WriteAllLines(_beatsPath, _beats);
    }
}
