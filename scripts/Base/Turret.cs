using Godot;
using Vestiges.Combat;

namespace Vestiges.Base;

/// <summary>
/// Tourelle auto-attaque. Tire sur l'ennemi le plus proche dans son rayon.
/// Hérite de Structure pour les HP et la destruction.
/// </summary>
public partial class Turret : Structure
{
    private float _damage = 8f;
    private float _attackSpeed = 1.5f;
    private float _range = 200f;
    private float _attackTimer;
    private PackedScene _projectileScene;
    private Polygon2D _dirIndicator;

    public override void _Ready()
    {
        base._Ready();
        CollisionLayer = 0;
        CollisionMask = 0;

        _projectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");
        CreateDirectionIndicator();
    }

    public override void Initialize(string recipeId, string structureId, float maxHp, Vector2I gridPos, Color color)
    {
        base.Initialize(recipeId, structureId, maxHp, gridPos, color);

        float s = 10f;
        Visual.Polygon = new Vector2[]
        {
            new(-s, 0), new(-s * 0.5f, -s * 0.6f),
            new(s * 0.5f, -s * 0.6f), new(s, 0),
            new(s * 0.5f, s * 0.6f), new(-s * 0.5f, s * 0.6f)
        };
    }

    public void SetTurretStats(float damage, float attackSpeed, float range)
    {
        _damage = damage;
        _attackSpeed = attackSpeed;
        _range = range;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDestroyed)
            return;

        _attackTimer -= (float)delta;
        if (_attackTimer > 0)
            return;

        Node2D target = FindNearestEnemy();
        if (target == null)
        {
            _dirIndicator.Visible = false;
            return;
        }

        Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
        _dirIndicator.Visible = true;
        _dirIndicator.Rotation = direction.Angle();

        Shoot(direction);
        _attackTimer = 1f / _attackSpeed;
    }

    private void Shoot(Vector2 direction)
    {
        Projectile projectile = _projectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GlobalPosition;
        projectile.Initialize(direction, _damage);
        GetTree().CurrentScene.AddChild(projectile);
    }

    private Node2D FindNearestEnemy()
    {
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
        Node2D nearest = null;
        float nearestDist = _range;

        foreach (Node node in enemies)
        {
            if (node is Node2D enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearest = enemy;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }

    private void CreateDirectionIndicator()
    {
        _dirIndicator = new Polygon2D();
        _dirIndicator.Polygon = new Vector2[] { new(0, -2), new(12, 0), new(0, 2) };
        _dirIndicator.Color = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        _dirIndicator.Visible = false;
        AddChild(_dirIndicator);
    }
}
