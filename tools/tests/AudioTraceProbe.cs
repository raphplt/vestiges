using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Trace d'écoute (plan 15, A0) : signaux de Résurgence, phases, clés musicales et chaque demande de son, horodatés
/// sur l'horloge sonore (<see cref="AudioManager.NowMsec"/>), qui est aussi la position dans l'enregistrement
/// du Movie Maker. Elle sert à retrouver un moment dans le fichier ; elle ne remplace pas l'écoute.
/// </summary>
internal sealed class AudioTraceProbe : IDisposable
{
    private readonly string _output;
    private readonly AudioManager _audio;
    private readonly EventBus _eventBus;
    private readonly Node _world;
    private readonly Player _player;
    private readonly List<string> _events = new() { "audio_s,game_s,kind,detail" };
    private readonly List<string> _states = new() { "audio_s,game_s,phase,music,intent,alive,near600" };
    private readonly Dictionary<string, int[]> _sounds = new();
    private string _phase = "";
    private string _music = "";
    private double _gameTime;
    private double _nextState;

    internal AudioTraceProbe(string output, Node world, Player player)
    {
        _output = output;
        _world = world;
        _player = player;
        _audio = AudioManager.Instance ?? throw new InvalidOperationException("AudioManager absent.");
        _eventBus = world.GetNode<EventBus>("/root/EventBus");
        _audio.SoundRequested += OnSound;
        _eventBus.RunPhaseChanged += OnPhase;
        _eventBus.CrisisWarning += OnWarning;
        _eventBus.CrisisStarted += OnStarted;
        _eventBus.CrisisEnded += OnEnded;
        _eventBus.GameStateChanged += OnState;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.ChestOpened += OnChest;
        _eventBus.RandomEventTriggered += OnRandomEvent;
        _eventBus.RunEventStarted += OnRunEvent;
        _phase = world.GetNode<GameManager>("/root/GameManager").CurrentRunPhase.ToString();
        Add("trace", "start");
    }

    private static double AudioSeconds => AudioManager.NowMsec / 1000.0;

    private void Add(string kind, string detail) =>
        _events.Add(string.Create(CultureInfo.InvariantCulture, $"{AudioSeconds:F2},{_gameTime:F2},{kind},{detail}"));

    private void OnSound(string key, AudioManager.SoundOutcome outcome)
    {
        if (!_sounds.TryGetValue(key, out int[] counts))
            _sounds[key] = counts = new int[5];
        counts[(int)outcome]++;
        if (outcome != AudioManager.SoundOutcome.Throttled)
            Add("sound", $"{key}:{outcome}");
    }

    private void OnPhase(string oldPhase, string newPhase)
    {
        _phase = newPhase;
        Add("phase", $"{oldPhase}>{newPhase}");
    }

    private void OnWarning(int crisis, float countdown) =>
        Add("crisis_warning", string.Create(CultureInfo.InvariantCulture, $"{crisis}:{countdown:F1}s"));
    private void OnStarted(int crisis, int intensity) => Add("crisis_started", $"{crisis}:{intensity}");
    private void OnEnded(int crisis) => Add("crisis_ended", $"{crisis}");
    private void OnState(string oldState, string newState) => Add("state", $"{oldState}>{newState}");
    private void OnLevelUp(int level) => Add("level_up", $"{level}");
    private void OnChest(string chestId, string rarity, Vector2 position) => Add("chest", $"{chestId}:{rarity}");
    private void OnRandomEvent(string eventId, string eventName) => Add("random_event", eventId);
    private void OnRunEvent(string eventId, string title, string objective, float duration) => Add("run_event", eventId);

    /// <summary>À chaque image, pauses comprises : relève les changements de musique, et l'état une fois par seconde.</summary>
    internal void Sample(double gameTime)
    {
        _gameTime = gameTime;
        if (_audio.CurrentMusicKey != _music)
        {
            _music = _audio.CurrentMusicKey;
            Add("music", _music.Length > 0 ? _music : "-");
        }
        if (AudioSeconds < _nextState)
            return;
        _nextState = Math.Floor(AudioSeconds) + 1.0;
        int alive = 0, near = 0;
        foreach (Node node in _world.GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                continue;
            alive++;
            if (enemy.GlobalPosition.DistanceTo(_player.GlobalPosition) <= 600f)
                near++;
        }
        _states.Add(string.Create(CultureInfo.InvariantCulture,
            $"{AudioSeconds:F2},{_gameTime:F2},{_phase},{(_music.Length > 0 ? _music : "-")},{_audio.CurrentMusicIntent},{alive},{near}"));
    }

    public void Dispose()
    {
        Add("trace", "end");
        _audio.SoundRequested -= OnSound;
        _eventBus.RunPhaseChanged -= OnPhase;
        _eventBus.CrisisWarning -= OnWarning;
        _eventBus.CrisisStarted -= OnStarted;
        _eventBus.CrisisEnded -= OnEnded;
        _eventBus.GameStateChanged -= OnState;
        _eventBus.LevelUp -= OnLevelUp;
        _eventBus.ChestOpened -= OnChest;
        _eventBus.RandomEventTriggered -= OnRandomEvent;
        _eventBus.RunEventStarted -= OnRunEvent;

        List<string> sounds = new() { "key,played,throttled,voice_stolen,interface,voice_limited" };
        foreach (KeyValuePair<string, int[]> entry in _sounds)
            sounds.Add($"{entry.Key},{entry.Value[0]},{entry.Value[1]},{entry.Value[2]},{entry.Value[3]},{entry.Value[4]}");
        Write("audio-events.csv", _events);
        Write("audio-states.csv", _states);
        Write("audio-sounds.csv", sounds);
        GD.Print($"[AudioTrace] {_events.Count - 1} événements, {_sounds.Count} sons distincts, écrits dans {_output}");
    }

    private void Write(string name, List<string> rows)
    {
        using FileAccess file = FileAccess.Open($"{_output}/{name}", FileAccess.ModeFlags.Write);
        file.StoreString(string.Join("\n", rows) + "\n");
    }
}
