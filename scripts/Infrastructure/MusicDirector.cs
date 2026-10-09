using System;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.Infrastructure;

/// <summary>
/// Musique du jeu (plan 15, A1). L'intention musicale (exploration, combat, annonce, Résurgence, accalmie…) se résout
/// depuis l'état du jeu et les signaux de Résurgence, indépendamment de la phase de gameplay : une annonce garde
/// la main jusqu'au début réel de la crise. Les signaux d'une même image ne donnent qu'une résolution, en fin d'image,
/// donc un seul fondu. Réglages : data/audio/music.json.
/// </summary>
public partial class MusicDirector : Node
{
    private const float SilentDb = -80f;
    private MusicConfig _config;
    private Func<string, AudioStream> _streams;
    private Func<string, float> _volumes;
    private float _incomingGain = 1f;
    private AudioStreamPlayer _playerA;
    private AudioStreamPlayer _playerB;
    private bool _usingA = true;
    private Tween _fadeTween;
    private AudioStreamPlayer _incoming;
    private AudioStreamPlayer _outgoing;
    private float _outgoingFrom;

    private EventBus _eventBus;
    private GameManager _gameManager;
    private GroupCache _groups;

    private bool _warningActive;
    private bool _crisisActive;
    private bool _calmActive;
    private bool _inCombat;
    private float _sampleTimer;
    private float _combatHold;
    private bool _resolvePending;

    public string CurrentKey { get; private set; } = "";
    public MusicIntent CurrentIntent { get; private set; } = MusicIntent.None;

    public void Initialize(MusicConfig config, Func<string, AudioStream> streams, Func<string, float> volumes = null)
    {
        _config = config;
        _streams = streams;
        _volumes = volumes;
    }

    /// <summary>Remplace les réglages ; le morceau en cours change à la prochaine intention.</summary>
    public void Configure(MusicConfig config) => _config = config;

    public override void _Ready()
    {
        _playerA = new AudioStreamPlayer { Bus = "Music", Name = "MusicA", VolumeDb = SilentDb };
        _playerB = new AudioStreamPlayer { Bus = "Music", Name = "MusicB", VolumeDb = SilentDb };
        AddChild(_playerA);
        AddChild(_playerB);

        _gameManager = GetNode<GameManager>("/root/GameManager");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.GameStateChanged += OnGameStateChanged;
        _eventBus.RunPhaseChanged += OnRunPhaseChanged;
        _eventBus.CrisisWarning += OnCrisisWarning;
        _eventBus.CrisisStarted += OnCrisisStarted;
        _eventBus.CrisisEnded += OnCrisisEnded;
        _eventBus.CrisisCalmChanged += OnCrisisCalmChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.GameStateChanged -= OnGameStateChanged;
        _eventBus.RunPhaseChanged -= OnRunPhaseChanged;
        _eventBus.CrisisWarning -= OnCrisisWarning;
        _eventBus.CrisisStarted -= OnCrisisStarted;
        _eventBus.CrisisEnded -= OnCrisisEnded;
        _eventBus.CrisisCalmChanged -= OnCrisisCalmChanged;
    }

    /// <summary>Résout de nouveau l'intention (ouverture du Hub au démarrage, où aucun changement d'état n'est émis).</summary>
    public void Refresh() => RequestResolve();

    public override void _Process(double delta)
    {
        if (_gameManager.CurrentState != GameManager.GameState.Run)
            return;
        // Temps de jeu : un écran qui fige la run ne fait ni entrer ni sortir le combat.
        _sampleTimer -= (float)delta;
        if (_sampleTimer > 0f)
            return;
        _sampleTimer += _config.CombatSampleSeconds;
        SampleEnemies();
    }

    /// <summary>
    /// Créatures actives près du joueur, relevées à fréquence bornée. Un retrait lointain au pool compte comme
    /// un départ, ce que ne faisait pas un compteur tenu par les apparitions et les morts.
    /// </summary>
    private void SampleEnemies()
    {
        _groups ??= GetNode<GroupCache>("/root/GroupCache");
        if (_groups.GetPlayer() is not Node2D player)
            return;
        Vector2 center = player.GlobalPosition;
        float radiusSq = _config.CombatRadiusPx * _config.CombatRadiusPx;
        int near = 0;
        using CrowdQuery crowd = CrowdIndex.Near(center, _config.CombatRadiusPx);
        foreach (Node node in crowd.Targets)
        {
            if (node is Enemy { IsActive: true, IsDying: false } enemy && enemy.GlobalPosition.DistanceSquaredTo(center) <= radiusSq)
                near++;
        }

        bool holding = _inCombat ? near <= _config.CombatExitEnemies : near >= _config.CombatEnterEnemies;
        _combatHold = holding ? _combatHold + _config.CombatSampleSeconds : 0f;
        float hold = _inCombat ? _config.CombatExitHoldSeconds : _config.CombatEnterHoldSeconds;
        if (_combatHold + 0.001f < hold)
            return;
        _inCombat = !_inCombat;
        _combatHold = 0f;
        RequestResolve();
    }

    private void OnGameStateChanged(string oldState, string newState)
    {
        ResetRunState();
        RequestResolve();
    }

    private void OnRunPhaseChanged(string oldPhase, string newPhase) => RequestResolve();

    private void OnCrisisWarning(int crisisNumber, float countdown)
    {
        _warningActive = true;
        RequestResolve();
    }

    private void OnCrisisStarted(int crisisNumber, int intensity)
    {
        _warningActive = false;
        _crisisActive = true;
        RequestResolve();
    }

    private void OnCrisisEnded(int crisisNumber)
    {
        _crisisActive = false;
        RequestResolve();
    }

    private void OnCrisisCalmChanged(bool active)
    {
        _calmActive = active;
        RequestResolve();
    }

    private void ResetRunState()
    {
        _warningActive = false;
        _crisisActive = false;
        _calmActive = false;
        _inCombat = false;
        _combatHold = 0f;
        _sampleTimer = 0f;
    }

    private void RequestResolve()
    {
        if (_resolvePending)
            return;
        _resolvePending = true;
        Callable.From(Resolve).CallDeferred();
    }

    private void Resolve()
    {
        _resolvePending = false;
        MusicIntent intent = ResolveIntent();
        if (intent == CurrentIntent || !_config.Cues.TryGetValue(intent, out MusicConfig.Cue cue))
            return;
        CurrentIntent = intent;
        Play(cue);
    }

    private MusicIntent ResolveIntent()
    {
        if (_gameManager.CurrentState == GameManager.GameState.Hub)
            return MusicIntent.Hub;
        GameManager.RunPhase phase = _gameManager.CurrentRunPhase;
        if (_gameManager.CurrentState == GameManager.GameState.Death || phase == GameManager.RunPhase.Death)
            return MusicIntent.Death;
        if (_crisisActive || phase == GameManager.RunPhase.Crisis)
            return MusicIntent.Resurgence;
        if (_warningActive)
            return MusicIntent.Warning;
        if (phase == GameManager.RunPhase.Endgame)
            return MusicIntent.Endgame;
        if (phase == GameManager.RunPhase.LateGame)
            return MusicIntent.LateGame;
        if (_calmActive)
            return MusicIntent.Calm;
        return _inCombat ? MusicIntent.Combat : MusicIntent.Exploration;
    }

    /// <summary>Fondu enchaîné A/B ; deux intentions qui partagent un morceau ne le relancent pas.</summary>
    private void Play(MusicConfig.Cue cue)
    {
        if (CurrentKey == cue.Key)
            return;
        AudioStream stream = _streams(cue.Key);
        if (stream == null)
        {
            GD.PushWarning($"[MusicDirector] Musique introuvable : {cue.Key}");
            return;
        }
        CurrentKey = cue.Key;

        _incoming = _usingA ? _playerA : _playerB;
        _outgoing = _usingA ? _playerB : _playerA;
        _usingA = !_usingA;
        // Un fondu interrompu repart du niveau atteint par le morceau sortant.
        _outgoingFrom = _outgoing.Playing ? Mathf.DbToLinear(_outgoing.VolumeDb) : 0f;

        if (stream is AudioStreamOggVorbis ogg)
            ogg.Loop = cue.Loop;

        _incomingGain = Mathf.DbToLinear(_volumes?.Invoke(cue.Key) ?? 0f);
        _incoming.Stream = stream;
        _incoming.VolumeDb = SilentDb;
        _incoming.Play(cue.StartSeconds);

        _fadeTween?.Kill();
        // Les lecteurs se figent avec la run (écran de choix, pause) et le fondu avec eux ; le ralenti de la mort,
        // lui, ne doit pas étirer l'entrée de sa musique.
        _fadeTween = CreateTween().SetIgnoreTimeScale();
        _fadeTween.TweenMethod(Callable.From<float>(SetCrossfade), 0f, 1f, Mathf.Max(cue.FadeSeconds, 0.01f));
        _fadeTween.TweenCallback(Callable.From(_outgoing.Stop));
    }

    /// <summary>
    /// Fondu à puissance constante, en amplitude : interpoler les décibels de −80 à 0 laissait les deux morceaux
    /// vers −40 dB à mi-parcours, un creux audible au milieu de chaque transition.
    /// </summary>
    private void SetCrossfade(float progress)
    {
        float angle = progress * Mathf.Pi * 0.5f;
        _incoming.VolumeDb = ToDb(_incomingGain * Mathf.Sin(angle));
        _outgoing.VolumeDb = ToDb(_outgoingFrom * Mathf.Cos(angle));
    }

    private static float ToDb(float amplitude) => amplitude <= 0.0001f ? SilentDb : Mathf.LinearToDb(amplitude);
}
