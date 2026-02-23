using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

public partial class Projectile : Area2D
{
    [Export] public float Speed = 400f;
    [Export] public float MaxLifetime = 3f;

    private Vector2 _direction;
    private float _damage;
    private int _pierceRemaining;
    private bool _isCrit;
    private bool _isRicochet;
    private Player _owner;
    private Polygon2D _visual;
    private bool _isDespawning;
    private readonly HashSet<ulong> _hitEnemies = new();

    public void Initialize(Vector2 direction, float damage, int pierce = 0, bool isCrit = false, Player owner = null, bool isRicochet = false)
    {
        _direction = direction.Normalized();
        _damage = damage;
        _pierceRemaining = pierce;
        _isCrit = isCrit;
        _owner = owner;
        _isRicochet = isRicochet;
        Rotation = _direction.Angle();
    }

    public override void _Ready()
    {
        _visual = GetNodeOrNull<Polygon2D>("Visual");
        BodyEntered += OnBodyEntered;
        GetTree().CreateTimer(MaxLifetime).Timeout += QueueFree;

        if (_visual != null)
        {
            _visual.Scale = _isCrit ? new Vector2(1.35f, 1.35f) : new Vector2(0.75f, 0.75f);
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
        if (body is Enemy enemy && !enemy.IsQueuedForDeletion())
        {
            ulong id = enemy.GetInstanceId();
            if (_hitEnemies.Contains(id))
                return;

            _hitEnemies.Add(id);
            enemy.TakeDamage(_damage, _isCrit);

            // Notify owner for perk effects (vampirism, ignite, execution, ricochet)
            if (_owner != null && IsInstanceValid(_owner))
                _owner.OnProjectileHit(enemy, _damage, _isCrit, _isRicochet);

            if (_pierceRemaining <= 0)
            {
                StartDespawn();
            }
            else
            {
                _pierceRemaining--;
                PulseOnHit();
            }
        }
    }

    private void PulseOnHit()
    {
        if (_visual == null || _isDespawning)
            return;

        Tween pulse = CreateTween();
        pulse.SetParallel();
        pulse.TweenProperty(_visual, "scale", new Vector2(1.25f, 0.85f), 0.04f);
        pulse.TweenProperty(_visual, "modulate:a", 0.8f, 0.04f);
        pulse.Chain().SetParallel();
        pulse.TweenProperty(_visual, "scale", Vector2.One, 0.05f);
        pulse.TweenProperty(_visual, "modulate:a", 0.95f, 0.05f);
    }

    private void StartDespawn()
    {
        if (_isDespawning)
            return;

        _isDespawning = true;
        Monitoring = false;

        if (_visual == null)
        {
            QueueFree();
            return;
        }

        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_visual, "scale", new Vector2(0.25f, 0.25f), 0.08f);
        tween.TweenProperty(_visual, "modulate:a", 0f, 0.08f);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
