using Godot;
using Vestiges.Combat;

namespace Vestiges.Base;

/// <summary>
/// Piège au sol. Inflige des dégâts aux ennemis qui marchent dessus.
/// Nombre d'utilisations limité.
/// </summary>
public partial class Trap : Structure
{
    private float _damage;
    private int _usesRemaining;
    private float _hitCooldown = 0.5f;
    private float _cooldownTimer;
    private Area2D _detectionArea;

    public override void _Ready()
    {
        base._Ready();
        CollisionLayer = 0;
        CollisionMask = 0;

        CreateDetectionArea();
    }

    public override void Initialize(string recipeId, string structureId, float maxHp, Vector2I gridPos, Color color)
    {
        base.Initialize(recipeId, structureId, maxHp, gridPos, color);

        float s = 12f;
        Visual.Polygon = new Vector2[]
        {
            new(-s, 0), new(-s * 0.5f, -s * 0.25f),
            new(0, -s * 0.5f), new(s * 0.5f, -s * 0.25f),
            new(s, 0), new(s * 0.5f, s * 0.25f),
            new(0, s * 0.5f), new(-s * 0.5f, s * 0.25f)
        };
    }

    public void SetTrapStats(float damage, int uses)
    {
        _damage = damage;
        _usesRemaining = uses;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_cooldownTimer > 0)
            _cooldownTimer -= (float)delta;
    }

    private void CreateDetectionArea()
    {
        _detectionArea = new Area2D();
        _detectionArea.CollisionLayer = 0;
        _detectionArea.CollisionMask = 2; // Detect enemies (layer 2)

        CircleShape2D shape = new();
        shape.Radius = 16f;
        CollisionShape2D collider = new();
        collider.Shape = shape;
        _detectionArea.AddChild(collider);

        _detectionArea.BodyEntered += OnBodyEntered;
        AddChild(_detectionArea);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_usesRemaining <= 0 || _cooldownTimer > 0)
            return;

        if (body is Enemy enemy)
        {
            enemy.TakeDamage(_damage);
            _usesRemaining--;
            _cooldownTimer = _hitCooldown;

            TriggerFlash();

            if (_usesRemaining <= 0)
                Exhaust();
        }
    }

    private void TriggerFlash()
    {
        Visual.Color = new Color(1f, 0.5f, 0.1f);
        Tween tween = CreateTween();
        tween.TweenProperty(Visual, "color", OriginalColor, 0.2f)
            .SetDelay(0.05f);
    }

    private void Exhaust()
    {
        Visual.Color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
        OriginalColor = Visual.Color;
        RemoveFromGroup("structures");

        Tween tween = CreateTween();
        tween.TweenInterval(2f);
        tween.TweenProperty(this, "modulate:a", 0f, 1f);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
}
