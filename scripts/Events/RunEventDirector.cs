using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Events.RunEvents;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Events;

/// <summary>
/// Orchestre les micro-événements entre les Résurgences : un temps fort toutes les deux à trois minutes,
/// jamais pendant une Résurgence, son annonce ou l'accalmie qui la suit. Tirage pondéré, sans répétition
/// immédiate, avec des événements débloqués au fil de la run. Relaie objectif et progression au HUD.
/// </summary>
public partial class RunEventDirector : Node
{
    private const float BlockedRetrySec = 5f;

    private readonly List<string> _recent = new();
    private readonly List<RunEventData> _candidates = new();

    private EventBus _eventBus;
    private GameManager _gameManager;
    private CrisisManager _crisisManager;
    private RunEventContext _context;
    private RunEventSchedule _schedule;
    private RunEvent _active;
    private RunEventData _activeData;
    private float _elapsed;
    private float _nextEventAt;
    private float _calmUntil;
    private float _progressTimer;
    private int _nextToken = 1;

    public bool IsEventActive => _active != null;

    /// <summary>Cible de l'événement en cours, si elle existe (bancs d'observation).</summary>
    public bool TryGetActiveTarget(out Vector2 target)
    {
        target = _active?.Target ?? Vector2.Zero;
        return _active != null && _active.HasTarget;
    }

    public override void _Ready()
    {
        RunEventDataLoader.Load();
        _schedule = RunEventDataLoader.Schedule;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _crisisManager = GetParent().GetNodeOrNull<CrisisManager>("CrisisManager");

        Node2D world = GetParent<Node2D>();
        _context = new RunEventContext(
            world.GetNode<Player>("Player"),
            world.GetNode<SpawnManager>("SpawnManager"),
            world,
            _eventBus,
            GetNode<GroupCache>("/root/GroupCache"));

        _nextEventAt = _context.Rng.RandfRange(_schedule.FirstEventMinSec, _schedule.FirstEventMaxSec);
        _eventBus.CrisisEnded += OnCrisisEnded;
        _eventBus.EventEnemyKilled += OnEventEnemyKilled;
    }

    public override void _ExitTree()
    {
        _eventBus.CrisisEnded -= OnCrisisEnded;
        _eventBus.EventEnemyKilled -= OnEventEnemyKilled;
        _active?.Cleanup();
        _active = null;
    }

    public override void _Process(double delta)
    {
        if (_gameManager.CurrentState != GameManager.GameState.Run || _context.Player.IsDead)
            return;

        float dt = (float)delta;
        _elapsed += dt;

        if (_active != null)
        {
            _active.Tick(dt);
            if (_active.IsFinished)
                EndActive();
            else
                EmitProgress(dt);
            return;
        }

        if (_elapsed < _nextEventAt)
            return;

        RunEventData next = IsBlockedByCrisis() ? null : PickEvent();
        if (next == null)
        {
            _nextEventAt = _elapsed + BlockedRetrySec;
            return;
        }
        StartEvent(next);
    }

    /// <summary>Démarre un événement précis (mode dev, bancs de capture).</summary>
    public bool ForceStart(string eventId)
    {
        if (_active != null)
            return false;
        foreach (RunEventData data in RunEventDataLoader.Events)
        {
            if (data.Id == eventId)
            {
                StartEvent(data);
                return true;
            }
        }
        return false;
    }

    /// <summary>La Résurgence reste le temps fort de sa fenêtre : pas d'événement pendant, pendant son annonce ni juste après.</summary>
    private bool IsBlockedByCrisis()
    {
        if (_elapsed < _calmUntil)
            return true;
        return _crisisManager != null && (_crisisManager.IsCrisisActive || _crisisManager.IsWarningActive);
    }

    /// <summary>Temps disponible avant la prochaine Résurgence : seuls les événements qui s'y terminent sont tirés.</summary>
    private float AvailableWindow() => _crisisManager == null
        ? float.MaxValue
        : _crisisManager.TimeUntilNextCrisis - _crisisManager.WarningDurationSec - _schedule.CrisisMarginSec;

    private RunEventData PickEvent()
    {
        _candidates.Clear();
        float window = AvailableWindow();
        float total = 0f;
        foreach (RunEventData data in RunEventDataLoader.Events)
        {
            if (_elapsed < data.MinSec || data.DurationSec > window || (_recent.Count > 0 && _recent[^1] == data.Id))
                continue;
            _candidates.Add(data);
            total += Weight(data);
        }
        if (_candidates.Count == 0)
            return null;

        float roll = _context.Rng.RandfRange(0f, total);
        foreach (RunEventData data in _candidates)
        {
            roll -= Weight(data);
            if (roll <= 0f)
                return data;
        }
        return _candidates[^1];
    }

    private float Weight(RunEventData data) => _recent.Contains(data.Id) ? data.Weight * _schedule.RecentWeightFactor : data.Weight;

    private void StartEvent(RunEventData data)
    {
        _activeData = data;
        _active = CreateEvent(data.Kind);
        if (_active == null)
        {
            GD.PushWarning($"[RunEventDirector] Unknown event kind: {data.Kind}");
            _nextEventAt = _elapsed + BlockedRetrySec;
            return;
        }

        int token = _nextToken++;
        _active.Begin(_context, data, token);
        if (_active.IsFinished)
        {
            // Impossible à mettre en place ici (aucun sol praticable) : abandon silencieux, nouvel essai bientôt.
            GD.Print($"[RunEventDirector] {data.Id} abandonné à la mise en place ({_elapsed:F0} s)");
            _active.Cleanup();
            _active = null;
            _activeData = null;
            _nextEventAt = _elapsed + BlockedRetrySec;
            return;
        }

        _recent.Add(data.Id);
        while (_recent.Count > _schedule.RecentMemory)
            _recent.RemoveAt(0);
        GD.Print($"[RunEventDirector] {data.Id} à {_elapsed:F0} s");
        _eventBus.EmitSignal(EventBus.SignalName.RandomEventTriggered, data.Id, _active.Title);
        _eventBus.EmitSignal(EventBus.SignalName.RunEventStarted, data.Id, _active.Title, _active.Objective, data.DurationSec);
        Infrastructure.AudioManager.Play("sfx_danger_building", 0f, -6f);
        _progressTimer = 0f;
    }

    private static RunEvent CreateEvent(string kind) => kind switch
    {
        "hunt" => new HuntEvent(),
        "stampede" => new StampedeEvent(),
        "fallen_relic" => new FallenRelicEvent(),
        "vigil" => new VigilEvent(),
        "shard_rain" => new ShardRainEvent(),
        _ => null
    };

    private void EmitProgress(float delta)
    {
        _progressTimer -= delta;
        if (_progressTimer > 0f)
            return;
        _progressTimer = _schedule.ProgressEmitIntervalSec;
        _eventBus.EmitSignal(EventBus.SignalName.RunEventProgress,
            _active.Objective, _active.Progress, Mathf.Max(0f, _active.TimeRemaining), _active.Target, _active.HasTarget);
    }

    private void EndActive()
    {
        RunEvent ended = _active;
        _active = null;
        ended.Cleanup();
        GD.Print($"[RunEventDirector] {_activeData.Id} {(ended.Succeeded ? "réussi" : "échoué")} à {_elapsed:F0} s");
        _eventBus.EmitSignal(EventBus.SignalName.RunEventEnded, _activeData.Id, ended.Succeeded, ended.Summary);
        _eventBus.EmitSignal(EventBus.SignalName.RandomEventEnded, _activeData.Id);
        _nextEventAt = _elapsed + _context.Rng.RandfRange(_schedule.GapMinSec, _schedule.GapMaxSec);
        _activeData = null;
    }

    private void OnCrisisEnded(int crisisNumber)
    {
        _calmUntil = _elapsed + _schedule.AfterCrisisCalmSec;
        _nextEventAt = Mathf.Max(_nextEventAt, _calmUntil);
    }

    private void OnEventEnemyKilled(int eventToken, Vector2 position)
    {
        if (_active != null && eventToken == _active.Token)
            _active.OnEnemyKilled(position);
    }
}
