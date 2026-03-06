using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

public partial class XpOrb : Area2D
{
    private const float BaseAttractionRadius = 150f;
    private const float BaseDriftRadius = 250f;
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

    private GpuParticles2D _glow;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        _glow = VfxFactory.CreateXpOrbGlow();
        if (_glow != null)
            AddChild(_glow);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_collected)
            return;

        CachePlayer();
        if (_player == null || !IsInstanceValid(_player))
            return;

        float magnetMult = _player.XpMagnetMultiplier;
        float attractionRadiusSq = BaseAttractionRadius * magnetMult * BaseAttractionRadius * magnetMult;
        float driftRadiusSq = BaseDriftRadius * magnetMult * BaseDriftRadius * magnetMult;
        float distSq = GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);

        if (distSq < attractionRadiusSq)
        {
            _currentSpeed = Mathf.Min(_currentSpeed + Acceleration * (float)delta, MaxSpeed);
            Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += direction * _currentSpeed * (float)delta;
        }
        else if (distSq < driftRadiusSq)
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

            // Burst doré à la collecte
            Node2D burst = VfxFactory.CreateXpCollectBurst(GlobalPosition);
            if (burst != null)
                GetTree().CurrentScene.AddChild(burst);

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
