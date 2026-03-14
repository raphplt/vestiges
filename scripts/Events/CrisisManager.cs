using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.Events;

/// <summary>
/// Gere les Resurgences de la V2 : annonce, crise active, accalmie.
/// </summary>
public partial class CrisisManager : Node
{
    private float _firstCrisisDelaySec = 240f;
    private float _intervalSec = 240f;
    private float _intervalVarianceSec = 45f;
    private float _warningDurationSec = 20f;
    private float _crisisDurationSec = 70f;
    private float _lateGameErasureThreshold = 0.68f;
    private float _endgameIntervalMultiplier = 0.7f;
    private float _endgameDurationMultiplier = 1.15f;

    private EventBus _eventBus;
    private GameManager _gameManager;
    private ErasureManager _erasureManager;
    private RandomNumberGenerator _rng = new();

    private float _elapsed;
    private float _nextCrisisAtSec;
    private bool _warningIssued;
    private bool _isCrisisActive;
    private float _crisisTimeRemaining;
    private int _crisisNumber;
    private int _currentIntensity = 1;
    private bool _endgameMode;

    public bool IsCrisisActive => _isCrisisActive;
    public bool IsWarningActive => !_isCrisisActive && _warningIssued && TimeUntilNextCrisis > 0f;
    public float TimeUntilNextCrisis => Mathf.Max(0f, _nextCrisisAtSec - _elapsed);
    public float CrisisTimeRemaining => Mathf.Max(0f, _crisisTimeRemaining);
    public int CrisisNumber => _crisisNumber;
    public int CurrentIntensity => _currentIntensity;
    public bool IsEndgameMode => _endgameMode;

    public override void _Ready()
    {
        LoadConfig();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _erasureManager = GetParent().GetNodeOrNull<ErasureManager>("ErasureManager");

        _rng.Randomize();
        ScheduleNextCrisis(_firstCrisisDelaySec);
    }

    public override void _Process(double delta)
    {
        if (_gameManager == null || _gameManager.CurrentState != GameManager.GameState.Run)
            return;

        float dt = (float)delta;
        _elapsed += dt;

        bool isEndgamePhase = _gameManager.CurrentRunPhase == GameManager.RunPhase.Endgame;

        if (_isCrisisActive)
        {
            _crisisTimeRemaining -= dt;
            if (_crisisTimeRemaining <= 0f)
                EndCrisis();
            return;
        }

        float timeUntilCrisis = TimeUntilNextCrisis;
        if (!_warningIssued && timeUntilCrisis <= _warningDurationSec)
        {
            _warningIssued = true;
            _eventBus.EmitSignal(EventBus.SignalName.CrisisWarning, _crisisNumber + 1, timeUntilCrisis);
        }

        if (_elapsed >= _nextCrisisAtSec)
            StartCrisis();
        else if (isEndgamePhase)
            return;
        else if (_erasureManager != null && _erasureManager.GlobalErasurePercent >= _lateGameErasureThreshold)
            _gameManager.SetRunPhase(GameManager.RunPhase.LateGame);
        else
            _gameManager.SetRunPhase(GameManager.RunPhase.Exploration);
    }

    private void StartCrisis()
    {
        _isCrisisActive = true;
        _warningIssued = false;
        _crisisNumber++;
        _currentIntensity = 1 + (_crisisNumber - 1) / 2;
        _crisisTimeRemaining = _crisisDurationSec * (_endgameMode ? _endgameDurationMultiplier : 1f);

        if (_gameManager?.CurrentRunPhase != GameManager.RunPhase.Endgame)
            _gameManager?.SetRunPhase(GameManager.RunPhase.Crisis);
        _eventBus?.EmitSignal(EventBus.SignalName.CrisisStarted, _crisisNumber, _currentIntensity);
    }

    private void EndCrisis()
    {
        _isCrisisActive = false;
        _eventBus?.EmitSignal(EventBus.SignalName.CrisisEnded, _crisisNumber);

        if (_gameManager?.CurrentRunPhase == GameManager.RunPhase.Endgame)
        {
            ScheduleNextCrisis(_intervalSec * _endgameIntervalMultiplier);
            return;
        }

        if (_erasureManager != null && _erasureManager.GlobalErasurePercent >= _lateGameErasureThreshold)
            _gameManager?.SetRunPhase(GameManager.RunPhase.LateGame);
        else
            _gameManager?.SetRunPhase(GameManager.RunPhase.Exploration);

        float baseDelay = _intervalSec + _rng.RandfRange(-_intervalVarianceSec, _intervalVarianceSec);
        if (_endgameMode)
            baseDelay *= _endgameIntervalMultiplier;
        ScheduleNextCrisis(baseDelay);
    }

    private void ScheduleNextCrisis(float delaySec)
    {
        _nextCrisisAtSec = _elapsed + Mathf.Max(20f, delaySec);
    }

    private void LoadConfig()
    {
        FileAccess file = FileAccess.Open("res://data/scaling/crises.json", FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
            return;

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        _firstCrisisDelaySec = dict.ContainsKey("first_crisis_delay_sec") ? (float)dict["first_crisis_delay_sec"].AsDouble() : _firstCrisisDelaySec;
        _intervalSec = dict.ContainsKey("interval_sec") ? (float)dict["interval_sec"].AsDouble() : _intervalSec;
        _intervalVarianceSec = dict.ContainsKey("interval_variance_sec") ? (float)dict["interval_variance_sec"].AsDouble() : _intervalVarianceSec;
        _warningDurationSec = dict.ContainsKey("warning_duration_sec") ? (float)dict["warning_duration_sec"].AsDouble() : _warningDurationSec;
        _crisisDurationSec = dict.ContainsKey("crisis_duration_sec") ? (float)dict["crisis_duration_sec"].AsDouble() : _crisisDurationSec;
        _lateGameErasureThreshold = dict.ContainsKey("late_game_erasure_threshold") ? (float)dict["late_game_erasure_threshold"].AsDouble() : _lateGameErasureThreshold;
        _endgameIntervalMultiplier = dict.ContainsKey("endgame_interval_multiplier") ? (float)dict["endgame_interval_multiplier"].AsDouble() : _endgameIntervalMultiplier;
        _endgameDurationMultiplier = dict.ContainsKey("endgame_duration_multiplier") ? (float)dict["endgame_duration_multiplier"].AsDouble() : _endgameDurationMultiplier;
    }

    public void EnableEndgameTempo()
    {
        _endgameMode = true;
        if (!_isCrisisActive)
            ScheduleNextCrisis(_intervalSec * _endgameIntervalMultiplier);
    }
}
