using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

public partial class Enemy : CharacterBody2D
{
    [Export] public float MaxHp = 30f;

    private float _currentHp;
    private Polygon2D _visual;
    private Color _originalColor;
    private static PackedScene _damageNumberScene;

    public override void _Ready()
    {
        _currentHp = MaxHp;
        _visual = GetNode<Polygon2D>("Visual");
        _originalColor = _visual.Color;
        AddToGroup("enemies");
        _damageNumberScene ??= GD.Load<PackedScene>("res://scenes/combat/DamageNumber.tscn");
    }

    public void TakeDamage(float damage)
    {
        if (_currentHp <= 0)
            return;

        _currentHp -= damage;
        HitFlash();
        SpawnDamageNumber(damage);

        if (_currentHp <= 0)
            Die();
    }

    private void HitFlash()
    {
        _visual.Color = Colors.White;
        Tween tween = CreateTween();
        tween.TweenProperty(_visual, "color", _originalColor, 0.15f)
            .SetDelay(0.05f);
    }

    private void SpawnDamageNumber(float damage)
    {
        DamageNumber dmgNum = _damageNumberScene.Instantiate<DamageNumber>();
        dmgNum.GlobalPosition = GlobalPosition + new Vector2(0, -20);
        dmgNum.SetDamage(damage);
        GetTree().CurrentScene.AddChild(dmgNum);
    }

    private void Die()
    {
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        eventBus.EmitSignal(EventBus.SignalName.EntityDied, this);
        QueueFree();
    }
}
