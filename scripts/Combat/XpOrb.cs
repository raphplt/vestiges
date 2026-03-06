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
    private Node2D _visualRoot;
    private float _floatTime;

    // Textures statiques pour l'animation 2 frames
    private static Texture2D _orbFrame1;
    private static Texture2D _orbFrame2;
    private static Texture2D OrbFrame1 => _orbFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_orb_xp.png");
    private static Texture2D OrbFrame2 => _orbFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_orb_xp_f2.png");

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

        // Remplacer le Sprite2D statique (défini dans la scène) par un AnimatedSprite2D 2 frames
        Sprite2D oldSprite = GetNodeOrNull<Sprite2D>("Visual");
        if (oldSprite != null)
        {
            // RemoveChild immédiat pour libérer le nom "Visual" avant d'ajouter le nouveau
            RemoveChild(oldSprite);
            oldSprite.QueueFree();
        }

        SpriteFrames frames = new();
        frames.AddAnimation("pulse");
        frames.SetAnimationSpeed("pulse", 4);
        frames.SetAnimationLoop("pulse", true);
        frames.AddFrame("pulse", OrbFrame1);
        frames.AddFrame("pulse", OrbFrame2);

        AnimatedSprite2D animSprite = new()
        {
            Name = "Visual",
            SpriteFrames = frames,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        animSprite.Play("pulse");
        AddChild(animSprite);
        _visualRoot = animSprite;

        _glow = VfxFactory.CreateXpOrbGlow();
        if (_glow != null)
            AddChild(_glow);

        // Léger offset aléatoire pour désynchroniser les orbes entre elles
        _floatTime = (float)GD.RandRange(0, Mathf.Tau);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_collected)
            return;

        float dt = (float)delta;

        // Animation de flottement subtile (sub-pixel bobbing)
        _floatTime += dt * 3f;
        if (_visualRoot != null)
            _visualRoot.Position = new Vector2(0, Mathf.Sin(_floatTime) * 1.5f);

        CachePlayer();
        if (_player == null || !IsInstanceValid(_player))
            return;

        float magnetMult = _player.XpMagnetMultiplier;
        float attractionRadiusSq = BaseAttractionRadius * magnetMult * BaseAttractionRadius * magnetMult;
        float driftRadiusSq = BaseDriftRadius * magnetMult * BaseDriftRadius * magnetMult;
        float distSq = GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);

        if (distSq < attractionRadiusSq)
        {
            _currentSpeed = Mathf.Min(_currentSpeed + Acceleration * dt, MaxSpeed);
            Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += direction * _currentSpeed * dt;
        }
        else if (distSq < driftRadiusSq)
        {
            Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += direction * DriftSpeed * dt;
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
