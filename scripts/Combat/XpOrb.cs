using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

public partial class XpOrb : Area2D
{
    private const float AttractionRadius = 150f;
    private const float DriftRadius = 250f;
    private const float MaxSpeed = 500f;
    private const float Acceleration = 800f;
    private const float DriftSpeed = 40f;

    private float _xpValue;
    private float _currentSpeed;
    private Player _player;
    private bool _collected;

    public void Initialize(float xpValue)
    {
        _xpValue = xpValue;
        _currentSpeed = 0f;
        _collected = false;
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_collected)
            return;

        CachePlayer();
        if (_player == null || !IsInstanceValid(_player))
            return;

        float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);

        if (dist < AttractionRadius)
        {
            _currentSpeed = Mathf.Min(_currentSpeed + Acceleration * (float)delta, MaxSpeed);
            Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += direction * _currentSpeed * (float)delta;
        }
        else if (dist < DriftRadius)
        {
            Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += direction * DriftSpeed * (float)delta;
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_collected)
            return;

        if (body is Player)
        {
            _collected = true;
            EventBus eventBus = GetNode<EventBus>("/root/EventBus");
            eventBus.EmitSignal(EventBus.SignalName.XpGained, _xpValue);
            CallDeferred(MethodName.QueueFree);
        }
    }

    private void CachePlayer()
    {
        if (_player != null && IsInstanceValid(_player))
            return;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Player p)
            _player = p;
    }
}
