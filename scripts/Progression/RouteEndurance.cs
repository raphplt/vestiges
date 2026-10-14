using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>La découverte existante du monde recharge l'endurance ; aucun second relevé du terrain par image.</summary>
public partial class RouteEndurance : Node2D
{
    private Player _player;
    private PerkRouteEndurance _config;
    private EventBus _events;
    private GameManager _manager;
    private Vector2 _lastDiscoveryPosition;
    private int _cells;
    public int Charges { get; private set; }
    public int CellsUntilCharge => _config.CellsPerCharge - _cells;

    public void Initialize(Player player, PerkRouteEndurance config)
    {
        config.Validate();
        _player = player;
        _config = config;
        player.RouteEndurance = this;
    }

    public override void _Ready()
    {
        SetProcess(false);
        _lastDiscoveryPosition = _player.GlobalPosition;
        _events = GetNode<EventBus>("/root/EventBus");
        _manager = GetNode<GameManager>("/root/GameManager");
        _events.ZoneDiscovered += OnDiscovery;
        _events.PlayerDamageResolved += OnDamage;
        _events.GameStateChanged += OnStateChanged;
        Position = new Vector2(0f, -42f);
        ZIndex = 20;
    }

    public override void _ExitTree()
    {
        if (_events != null)
        {
            _events.ZoneDiscovered -= OnDiscovery;
            _events.PlayerDamageResolved -= OnDamage;
            _events.GameStateChanged -= OnStateChanged;
        }
        if (_player != null && IsInstanceValid(_player) && _player.RouteEndurance == this)
            _player.RouteEndurance = null;
    }

    private void OnDiscovery(int x, int y, int count)
    {
        Vector2 position = _player.GlobalPosition;
        bool moved = position.DistanceSquaredTo(_lastDiscoveryPosition) > 0.01f;
        _lastDiscoveryPosition = position;
        // La révélation initiale et les événements hors run n'accordent aucune réserve gratuite.
        if (!moved || count <= 0 || _player.IsDead || _manager.CurrentState != GameManager.GameState.Run || GetTree().Paused)
            return;
        if (Charges == _config.MaxCharges)
            return;
        long cells = (long)_cells + count;
        Charges = Mathf.Min(_config.MaxCharges, Charges + (int)(cells / _config.CellsPerCharge));
        _cells = Charges == _config.MaxCharges ? 0 : (int)(cells % _config.CellsPerCharge);
        QueueRedraw();
    }

    internal float ReduceDamage(float damage)
    {
        if (damage <= 0f || Charges == 0 || _player.IsDead)
            return damage;
        Charges--;
        QueueRedraw();
        _player.AttackFx.PlayObjectRing(_player.GlobalPosition, FxFamily.Brass, 22f);
        return damage * (1f - _config.DamageReduction);
    }

    private void OnDamage(PlayerDamageResult result)
    {
        if (result.PlayerId == _player.GetInstanceId() && result.Fatal)
            Clear();
    }

    private void OnStateChanged(string oldState, string newState)
    {
        if (_manager.CurrentState != GameManager.GameState.Run)
            Clear();
        Visible = _manager.CurrentState == GameManager.GameState.Run;
    }

    private void Clear()
    {
        Charges = 0;
        _cells = 0;
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (int i = 0; i < _config.MaxCharges; i++)
        {
            Vector2 point = new((i - (_config.MaxCharges - 1) * 0.5f) * 6f, 0f);
            DrawCircle(point, 2.5f, new Color(0.08f, 0.07f, 0.05f));
            DrawCircle(point, 1.5f, i < Charges ? new Color(0.95f, 0.79f, 0.45f) : new Color(0.35f, 0.32f, 0.27f));
        }
        float progress = _cells / (float)_config.CellsPerCharge;
        if (progress > 0f)
            DrawLine(new Vector2(-8f, 5f), new Vector2(-8f + progress * 16f, 5f), new Color(0.95f, 0.79f, 0.45f), 1f);
    }
}
