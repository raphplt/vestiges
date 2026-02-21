using Godot;
using Vestiges.Combat;

namespace Vestiges.Core;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 200f;
    [Export] public float AttackDamage = 10f;
    [Export] public float AttackSpeed = 1.0f;
    [Export] public float AttackRange = 300f;

    private PackedScene _projectileScene;
    private Timer _attackTimer;

    public override void _Ready()
    {
        _projectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");

        _attackTimer = new Timer();
        _attackTimer.WaitTime = 1.0 / AttackSpeed;
        _attackTimer.Autostart = true;
        _attackTimer.Timeout += OnAttackTimerTimeout;
        AddChild(_attackTimer);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (inputDir != Vector2.Zero)
        {
            Vector2 isoDir = CartesianToIsometric(inputDir);
            Velocity = isoDir * Speed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
    }

    private void OnAttackTimerTimeout()
    {
        Node2D nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null)
            return;

        Vector2 direction = (nearestEnemy.GlobalPosition - GlobalPosition).Normalized();

        Projectile projectile = _projectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GlobalPosition;
        projectile.Initialize(direction, AttackDamage);
        GetTree().CurrentScene.AddChild(projectile);
    }

    private Node2D FindNearestEnemy()
    {
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
        Node2D nearest = null;
        float nearestDist = AttackRange;

        foreach (Node node in enemies)
        {
            if (node is Node2D enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }

    private static Vector2 CartesianToIsometric(Vector2 cartesian)
    {
        Vector2 iso = new Vector2(
            cartesian.X - cartesian.Y,
            (cartesian.X + cartesian.Y) * 0.5f
        );
        return iso.Normalized();
    }
}
