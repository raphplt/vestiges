using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

public partial class EnemyProjectile : Area2D
{
    [Export] public float Speed = 250f;
    [Export] public float MaxLifetime = 4f;

    private Vector2 _direction;
    private float _damage;

    public void Initialize(Vector2 direction, float damage)
    {
        _direction = direction.Normalized();
        _damage = damage;
        Rotation = _direction.Angle();
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        GetTree().CreateTimer(MaxLifetime).Timeout += QueueFree;
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += _direction * Speed * (float)delta;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.TakeDamage(_damage);
            QueueFree();
        }
    }
}
