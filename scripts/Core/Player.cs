using Godot;
using Vestiges.Combat;

namespace Vestiges.Core;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 200f;
    [Export] public float AttackDamage = 10f;
    [Export] public float AttackSpeed = 1.0f;
    [Export] public float AttackRange = 300f;
    [Export] public float MaxHp = 100f;

    private float _currentHp;
    private bool _isDead;
    private PackedScene _projectileScene;
    private Timer _attackTimer;
    private Polygon2D _visual;
    private Color _originalColor;

    // Perk modifiers
    private float _damageMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _attackSpeedMultiplier = 1f;
    private float _bonusMaxHp;
    private int _extraProjectiles;
    private float _aoeMultiplier = 1f;

    public float CurrentHp => _currentHp;
    public float EffectiveMaxHp => MaxHp + _bonusMaxHp;
    public bool IsDead => _isDead;

    public override void _Ready()
    {
        _currentHp = MaxHp;
        _visual = GetNode<Polygon2D>("Visual");
        _originalColor = _visual.Color;

        AddToGroup("player");

        _projectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");

        _attackTimer = new Timer();
        _attackTimer.WaitTime = 1.0 / AttackSpeed;
        _attackTimer.Autostart = true;
        _attackTimer.Timeout += OnAttackTimerTimeout;
        AddChild(_attackTimer);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead)
            return;

        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (inputDir != Vector2.Zero)
        {
            Vector2 isoDir = CartesianToIsometric(inputDir);
            Velocity = isoDir * (Speed * _speedMultiplier);
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
    }

    public void ApplyPerkModifier(string stat, float value, string modifierType)
    {
        switch (stat)
        {
            case "damage":
                if (modifierType == "multiplicative") _damageMultiplier *= value;
                break;
            case "speed":
                if (modifierType == "multiplicative") _speedMultiplier *= value;
                break;
            case "max_hp":
                if (modifierType == "additive")
                {
                    _bonusMaxHp += value;
                    _currentHp += value;
                }
                break;
            case "attack_speed":
                if (modifierType == "multiplicative")
                {
                    _attackSpeedMultiplier *= value;
                    _attackTimer.WaitTime = 1.0 / (AttackSpeed * _attackSpeedMultiplier);
                }
                break;
            case "projectile_count":
                if (modifierType == "additive") _extraProjectiles += (int)value;
                break;
            case "aoe_radius":
                if (modifierType == "multiplicative") _aoeMultiplier *= value;
                break;
        }
    }

    public void TakeDamage(float damage)
    {
        if (_currentHp <= 0)
            return;

        _currentHp -= damage;
        HitFlash();

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;
        Velocity = Vector2.Zero;
        _attackTimer.Stop();
        RemoveFromGroup("player");

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        eventBus.EmitSignal(EventBus.SignalName.EntityDied, this);

        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_visual, "modulate:a", 0.3f, 0.8f);
        tween.TweenProperty(this, "scale", Vector2.One * 0.5f, 0.8f);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            GetTree().Paused = true;
        }));
    }

    private void HitFlash()
    {
        _visual.Color = new Color(1f, 0.3f, 0.3f);
        Tween tween = CreateTween();
        tween.TweenProperty(_visual, "color", _originalColor, 0.2f)
            .SetDelay(0.05f);
    }

    private void OnAttackTimerTimeout()
    {
        if (_isDead)
            return;

        int totalProjectiles = 1 + _extraProjectiles;
        System.Collections.Generic.List<Node2D> targets = FindNearestEnemies(totalProjectiles);
        if (targets.Count == 0)
            return;

        float effectiveDamage = AttackDamage * _damageMultiplier;

        for (int i = 0; i < totalProjectiles; i++)
        {
            Node2D target = targets[i % targets.Count];
            Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();

            Projectile projectile = _projectileScene.Instantiate<Projectile>();
            projectile.GlobalPosition = GlobalPosition;
            projectile.Initialize(direction, effectiveDamage);
            GetTree().CurrentScene.AddChild(projectile);
        }
    }

    private System.Collections.Generic.List<Node2D> FindNearestEnemies(int count)
    {
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
        System.Collections.Generic.List<(Node2D enemy, float dist)> inRange = new();

        foreach (Node node in enemies)
        {
            if (node is Node2D enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < AttackRange)
                    inRange.Add((enemy, dist));
            }
        }

        inRange.Sort((a, b) => a.dist.CompareTo(b.dist));

        System.Collections.Generic.List<Node2D> result = new();
        int limit = System.Math.Min(count, inRange.Count);
        for (int i = 0; i < limit; i++)
            result.Add(inRange[i].enemy);

        return result;
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
