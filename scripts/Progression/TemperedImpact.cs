using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>Une seule charge par émission, malgré les cibles multiples et l'ordre d'arrivée des projectiles.</summary>
public partial class TemperedImpact : Node2D
{
    private const int HistorySize = 4096;
    private readonly ulong[] _launches = new ulong[HistorySize];
    private ulong _latestLaunch;
    private Player _player;
    private PerkShockwave _config;
    private EventBus _events;
    private GameManager _manager;
    public int SuccessfulAttacks { get; private set; }
    public int WavesTriggered { get; private set; }

    public void Initialize(Player player, PerkShockwave config)
    {
        config.Validate();
        _player = player;
        _config = config;
    }

    public override void _Ready()
    {
        SetProcess(false);
        _events = GetNode<EventBus>("/root/EventBus");
        _manager = GetNode<GameManager>("/root/GameManager");
        _events.EnemyDamageResolved += OnImpact;
        _events.GameStateChanged += OnStateChanged;
        Position = new Vector2(0f, -42f);
        ZIndex = 20;
    }

    public override void _ExitTree()
    {
        if (_events != null)
        {
            _events.EnemyDamageResolved -= OnImpact;
            _events.GameStateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged(string oldState, string newState)
    {
        Visible = _manager.CurrentState == GameManager.GameState.Run;
        if (Visible)
            return;
        SuccessfulAttacks = 0;
        WavesTriggered = 0;
        _latestLaunch = 0;
        System.Array.Clear(_launches);
        QueueRedraw();
    }

    private void OnImpact(DamageResult result)
    {
        AttackContext source = result.Source;
        if (_player.IsDead || _manager.CurrentState != GameManager.GameState.Run || !result.Applied || result.HpLost <= 0f || !source.IsDirectWeapon || source.IsEcho
            || source.OwnerId != _player.GetInstanceId() || source.LaunchId == 0 || source.ReferenceDamage <= 0f)
            return;
        // Fenêtre fixe : aucune collection croissante ni allocation au passage d'une salve.
        if (_latestLaunch >= HistorySize && source.LaunchId <= _latestLaunch - HistorySize)
            return;
        int slot = (int)(source.LaunchId % HistorySize);
        if (_launches[slot] == source.LaunchId)
            return;
        _latestLaunch = System.Math.Max(_latestLaunch, source.LaunchId);
        _launches[slot] = source.LaunchId;
        if (++SuccessfulAttacks < _config.AttacksPerWave)
        {
            QueueRedraw();
            return;
        }
        SuccessfulAttacks = 0;
        QueueRedraw();
        if (GodotObject.InstanceFromId(result.Target.InstanceId) is not Enemy target || target.Life != result.Target)
            return;
        Vector2 center = target.GlobalPosition;
        float radius = _config.Radius * _player.AoeMultiplier;
        float radiusSq = radius * radius;
        float damage = source.ReferenceDamage * _config.DamageRatio;
        AttackContext wave = source.As(DamageKind.Passive);
        WavesTriggered++;
        using CrowdQuery crowd = CrowdIndex.Near(center, radius);
        foreach (Node2D node in crowd.Targets)
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy
                || Iso.GroundDistanceSquared(center, enemy.GlobalPosition) > radiusSq)
                continue;
            DamageResult hit = enemy.TakeDamage(damage, source: wave);
            if (hit.Applied && !hit.Fatal)
            {
                Vector2 away = enemy.GlobalPosition - center;
                if (away == Vector2.Zero)
                    away = enemy.GlobalPosition - _player.GlobalPosition;
                enemy.ApplyKnockback(away == Vector2.Zero ? Vector2.Right : away, _config.Knockback);
            }
        }
        _player.AttackFx.PlayBurst(center, FxFamily.Brass, radius);
    }

    public override void _Draw()
    {
        for (int i = 0; i < _config.AttacksPerWave; i++)
        {
            Vector2 point = new((i - (_config.AttacksPerWave - 1) * 0.5f) * 5f, 0f);
            DrawRect(new Rect2(point - Vector2.One * 2f, Vector2.One * 4f), new Color(0.08f, 0.07f, 0.05f));
            DrawRect(new Rect2(point - Vector2.One, Vector2.One * 2f),
                i < SuccessfulAttacks ? new Color(1f, 0.64f, 0.32f) : new Color(0.35f, 0.3f, 0.25f));
        }
    }
}
