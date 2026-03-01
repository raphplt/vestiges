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

    // Homing
    private float _homingStrength;
    private Node2D _homingTarget;

    // Ground fire (weapon special_effect)
    private bool _spawnsGroundFire;
    private float _groundDamage;
    private float _groundDuration;
    private float _groundRadius;

    // Weapon reference for on-hit effects
    public Infrastructure.WeaponData SourceWeapon { get; set; }

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

    public void SetHoming(float strength, Node2D target)
    {
        _homingStrength = strength;
        _homingTarget = target;
    }

    public void SetGroundFire(float damage, float duration, float radius)
    {
        _spawnsGroundFire = true;
        _groundDamage = damage;
        _groundDuration = duration;
        _groundRadius = radius;
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

        float dt = (float)delta;

        // Homing : ajuste la direction vers la cible
        if (_homingStrength > 0f && _homingTarget != null && IsInstanceValid(_homingTarget))
        {
            Vector2 toTarget = (_homingTarget.GlobalPosition - GlobalPosition).Normalized();
            _direction = _direction.Lerp(toTarget, _homingStrength * dt * 5f).Normalized();
            Rotation = _direction.Angle();
        }
        else if (_homingStrength > 0f && (_homingTarget == null || !IsInstanceValid(_homingTarget)))
        {
            // Cible perdue : chercher la plus proche
            _homingTarget = FindNearestEnemy();
        }

        Position += _direction * Speed * dt;
    }

    private Node2D FindNearestEnemy()
    {
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
        Node2D nearest = null;
        float nearestDist = 500f;

        foreach (Node node in enemies)
        {
            if (node is Node2D candidate && !candidate.IsQueuedForDeletion())
            {
                float dist = GlobalPosition.DistanceTo(candidate.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearest = candidate;
                    nearestDist = dist;
                }
            }
        }
        return nearest;
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

            // Ground fire : zone de dégâts persistante à l'impact
            if (_spawnsGroundFire)
            {
                SpawnGroundFire(enemy.GlobalPosition);
                _spawnsGroundFire = false;
            }

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

    private void SpawnGroundFire(Vector2 position)
    {
        Node2D fire = new() { Name = "GroundFire", GlobalPosition = position };

        int segments = 10;
        Vector2[] points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Tau * i / segments;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _groundRadius;
        }

        Polygon2D glow = new()
        {
            Polygon = points,
            Color = new Color(1f, 0.5f, 0.1f, 0.4f)
        };
        fire.AddChild(glow);

        PointLight2D light = new()
        {
            Color = new Color(1f, 0.6f, 0.2f),
            Energy = 0.6f,
            TextureScale = _groundRadius / 64f
        };
        light.Texture = GD.Load<Texture2D>("res://icon.svg");
        fire.AddChild(light);

        GetTree().CurrentScene.AddChild(fire);

        // Damage tick toutes les 0.5s
        float elapsed = 0f;
        float tickInterval = 0.5f;
        float tickAccumulator = 0f;
        float totalDuration = _groundDuration;
        float dmg = _groundDamage;
        float radius = _groundRadius;

        fire.SetProcess(true);
        fire.TreeExiting += () => { };

        SceneTreeTimer timer = GetTree().CreateTimer(totalDuration);
        SceneTreeTimer tickTimer = GetTree().CreateTimer(tickInterval);

        // Tick damage loop via recursive timers
        void DoTick()
        {
            if (!IsInstanceValid(fire))
                return;
            Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
            foreach (Node node in enemies)
            {
                if (node is Enemy e && IsInstanceValid(e) && !e.IsDying)
                {
                    if (e.GlobalPosition.DistanceTo(position) < radius)
                        e.TakeDamage(dmg);
                }
            }
            elapsed += tickInterval;
            if (elapsed < totalDuration && IsInstanceValid(fire))
                GetTree().CreateTimer(tickInterval).Timeout += DoTick;
        }
        GetTree().CreateTimer(tickInterval).Timeout += DoTick;

        // Fade out et destruction
        Tween tween = fire.CreateTween();
        tween.TweenProperty(glow, "modulate:a", 0f, totalDuration);
        tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(fire)) fire.QueueFree(); }));
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
        SetDeferred("monitoring", false);

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
