using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Progression;

public enum AirReserveState { Full, Consuming, Empty, Recharging, HeldInVoid }

/// <summary>
/// Réserve d'air de la Scaphandrière (plan 45 N2a). Seuls les malus de vitesse et de dégâts des zones
/// effilochées/effacées sont suspendus. Le Néant conserve ses dégâts, ses malus et l'air déjà en bouteille.
/// </summary>
public partial class AirReserve : Node2D
{
    private const int GaugeWidth = 16;
    private static readonly Color Brass = new(0.8f, 0.66f, 0.38f);
    private static readonly Color Breath = new(0.57f, 0.83f, 0.86f);
    private static readonly Color EmptyColor = new(0.65f, 0.35f, 0.3f);
    private static readonly Color Back = new(0.08f, 0.09f, 0.12f, 0.9f);
    private Player _player;
    private PerkAir _config;
    private GameManager _manager;
    private EventBus _events;
    private SceneTree _tree;
    private int _drawnFill = -1;
    private AirReserveState _drawnState;
    private float _audioCooldown;

    public float RemainingSeconds { get; private set; }
    public float Fraction => RemainingSeconds / _config.Seconds;
    public AirReserveState State { get; private set; }
    public bool IsProtecting { get; private set; }

    public void Initialize(Player player, PerkAir config)
    {
        _player = player;
        _config = config;
        RemainingSeconds = config.Seconds;
    }

    public override void _Ready()
    {
        _tree = GetTree();
        _manager = GetNode<GameManager>("/root/GameManager");
        _events = GetNode<EventBus>("/root/EventBus");
        _player.SynchronizeErasurePhase();
        _player.ErasurePhaseChanged += OnPhaseChanged;
        _events.GameStateChanged += OnGameStateChanged;
        _events.PlayerDamageResolved += OnDamage;
        // À côté du casque, loin des jauges de PV et de mobilité sous les pieds.
        Position = new Vector2(0f, -28f);
        Refresh();
    }

    public override void _ExitTree()
    {
        if (_player != null && IsInstanceValid(_player))
        {
            _player.ErasurePhaseChanged -= OnPhaseChanged;
            _player.SetErasurePenaltyProtection(false);
        }
        if (_events != null && IsInstanceValid(_events))
        {
            _events.GameStateChanged -= OnGameStateChanged;
            _events.PlayerDamageResolved -= OnDamage;
        }
        IsProtecting = false;
    }

    public override void _Process(double delta) => Advance((float)delta);

    /// <summary>Un pas de temps de jeu ; les tests passent aussi ici, y compris quand l'arbre est en pause.</summary>
    public void Advance(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0f || _tree.Paused || _manager.CurrentState != GameManager.GameState.Run
            || _player.IsDead)
            return;
        _audioCooldown = Mathf.Max(0f, _audioCooldown - dt);
        switch (_player.ErasurePhase)
        {
            case ErasureManager.ErasureZonePhase.Frayed:
            case ErasureManager.ErasureZonePhase.Erased:
                RemainingSeconds = Mathf.Max(0f, RemainingSeconds - dt);
                break;
            case ErasureManager.ErasureZonePhase.Anchored:
            case ErasureManager.ErasureZonePhase.Fragile:
                RemainingSeconds = Mathf.Min(_config.Seconds, RemainingSeconds + dt * (_config.Seconds / _config.RechargeSeconds));
                break;
        }
        Refresh(audible: true);
    }

    private void OnPhaseChanged(ErasureManager.ErasureZonePhase phase) => Refresh(audible: true);
    private void OnGameStateChanged(string oldState, string newState) => Refresh();

    private void OnDamage(PlayerDamageResult result)
    {
        if (result.PlayerId == _player.GetInstanceId() && _player.IsDead)
            Refresh();
    }

    private void Refresh(bool audible = false)
    {
        AirReserveState previous = State;
        ErasureManager.ErasureZonePhase phase = _player.ErasurePhase;
        bool exposed = phase is ErasureManager.ErasureZonePhase.Frayed or ErasureManager.ErasureZonePhase.Erased;
        bool inRun = !_player.IsDead && _manager.CurrentState == GameManager.GameState.Run;
        IsProtecting = inRun && exposed && RemainingSeconds > 0f;
        _player.SetErasurePenaltyProtection(IsProtecting);
        State = phase == ErasureManager.ErasureZonePhase.Void ? AirReserveState.HeldInVoid
            : RemainingSeconds <= 0f && exposed ? AirReserveState.Empty
            : exposed ? AirReserveState.Consuming
            : RemainingSeconds >= _config.Seconds ? AirReserveState.Full : AirReserveState.Recharging;
        Visible = inRun;
        // Le délai commun aux deux sons évite le cliquetis aux frontières ; aucun rattrapage après une pause.
        if (audible && inRun && !_tree.Paused && State != previous && _config.Audio != null && _audioCooldown <= 0f)
        {
            string sound = State == AirReserveState.Consuming ? _config.Audio.Consume
                : State == AirReserveState.Empty && previous == AirReserveState.Consuming ? _config.Audio.Empty : null;
            if (sound != null)
            {
                AudioManager.PlayAt(sound, _player.GlobalPosition, 0f);
                _audioCooldown = _config.Audio.MinIntervalSeconds;
            }
        }
        int fill = Mathf.Clamp(Mathf.RoundToInt(Fraction * GaugeWidth), 0, GaugeWidth);
        if (fill == _drawnFill && State == _drawnState)
            return;
        _drawnFill = fill;
        _drawnState = State;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color color = State switch
        {
            AirReserveState.Consuming => Breath,
            AirReserveState.Empty => EmptyColor,
            AirReserveState.HeldInVoid => new Color(0.4f, 0.4f, 0.45f),
            _ => Brass,
        };
        DrawRect(new Rect2(-9f, -1f, GaugeWidth + 2f, 4f), Back);
        if (_drawnFill > 0)
            DrawRect(new Rect2(-8f, 0f, _drawnFill, 2f), color);
        // Un signe statique lisible distingue les états sans animation permanente ni particules.
        if (State == AirReserveState.Recharging)
        {
            DrawRect(new Rect2(11f, -1f, 1f, 3f), color);
            DrawRect(new Rect2(10f, 0f, 3f, 1f), color);
        }
        else if (State is AirReserveState.Empty or AirReserveState.HeldInVoid)
            DrawRect(new Rect2(10f, 0f, 3f, 1f), color);
        else if (State == AirReserveState.Consuming)
            DrawRect(new Rect2(11f, -2f, 1f, 1f), color);
    }
}
