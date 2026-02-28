using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

public partial class EnemyProjectile : Area2D
{
    [Export] public float Speed = 185f;
    [Export] public float MaxLifetime = 4f;

    private Vector2 _direction;
    private float _damage;
    private string _sourceEnemyId = "enemy_projectile";
    private Polygon2D _visual;
    private bool _isDespawning;

    public void Initialize(Vector2 direction, float damage, string sourceEnemyId = "enemy_projectile")
    {
        _direction = direction.Normalized();
        _damage = damage;
        _sourceEnemyId = sourceEnemyId;
        Rotation = _direction.Angle();
    }

    public override void _Ready()
    {
        _visual = GetNodeOrNull<Polygon2D>("Visual");
        BodyEntered += OnBodyEntered;
        GetTree().CreateTimer(MaxLifetime).Timeout += QueueFree;

        if (_visual != null)
        {
            _visual.Scale = new Vector2(0.75f, 0.75f);
            _visual.Modulate = new Color(1f, 1f, 1f, 0.92f);
            Tween spawnTween = CreateTween();
            spawnTween.TweenProperty(_visual, "scale", Vector2.One, 0.08f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDespawning)
            return;

        Position += _direction * Speed * (float)delta;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.PlayerHitBy, _sourceEnemyId, _damage);
            player.TakeDamage(_damage);
            StartDespawn();
        }
    }

    private void StartDespawn()
    {
        if (_isDespawning)
            return;

        _isDespawning = true;
        SetDeferred("monitoring", false);

        if (_visual == null)
        {
            QueueFree();
            return;
        }

        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_visual, "scale", new Vector2(0.2f, 0.2f), 0.08f);
        tween.TweenProperty(_visual, "modulate:a", 0f, 0.08f);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
