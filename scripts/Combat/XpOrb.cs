using System;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

public partial class XpOrb : Area2D, ITicked
{
    // Les orbes éveillées sont avancées par une seule boucle C#, pas par un rappel moteur chacune (plan 29).
    private static readonly TickRoster<XpOrb> Roster = new("XpOrbs");
    public int TickSlot { get; set; } = -1;

    private const float BaseAttractionRadius = 150f;
    private const float BaseDriftRadius = 250f;
    private const float MaxSpeed = 500f;
    private const float Acceleration = 800f;
    private const float DriftSpeed = 40f;
    // Saut du butin (plan 02 J2) : l'orbe jaillit du corps et retombe à côté, avant de pouvoir être attirée.
    private const float HopSec = 0.35f;
    private const float HopHeightPx = 14f;
    // Collecte (plan 02 J3) : chaque ramassage rapproché monte le son d'un cran ; l'orbe aspirée s'étire et traîne des éclats.
    private const ulong ChainWindowMs = 500;
    private const int ChainMax = 14;
    private const float ChainPitchStep = 0.035f;
    private const float TrailInterval = 0.04f;
    private const float TrailMinSpeed = 220f;
    // Au-delà, l'orbe est hors de l'écran et hors d'attraction : elle s'endort (ni physique, ni animation, ni lueur)
    // et CombatPools la réveille quand le joueur revient. Le nomade en laisse des centaines derrière lui.
    private const float SleepDistance = 750f;
    private const float SleepAttractionMargin = 150f;
    // Sillage : une orbe hors d'attraction consulte le couloir du joueur à ce rythme, pas à chaque pas physique.
    private const float TrailCheckInterval = 0.15f;

    private static ulong _lastCollectMs;
    private static int _chain;

    private float _xpValue;
    private float _currentSpeed;
    private Player _player;
    private bool _collected;
    private Node2D _visualRoot;
    private float _floatTime;
    private Vector2 _hopFrom;
    private Vector2 _hopTo;
    private float _hopElapsed = -1f;
    private float _trailTimer;
    private float _trailCheckTimer;
    private bool _trailBound;

    public bool IsAsleep { get; private set; }

    /// <summary>Change à chaque endormissement : une entrée de CombatPools d'un sommeil précédent est périmée.</summary>
    public int SleepToken { get; private set; }

    /// <summary>Distance au joueur au-delà de laquelle l'orbe dort, attraction du joueur comprise.</summary>
    /// <summary>Rayon dans lequel l'orbe fonce vers le joueur ; il donne aussi la demi-largeur du couloir de Sillage.</summary>
    public static float AttractionRadius(Player player) => BaseAttractionRadius * player.XpMagnetMultiplier;

    public static float SleepRadius(Player player) =>
        Mathf.Max(SleepDistance, BaseDriftRadius * player.XpMagnetMultiplier + SleepAttractionMargin);

    // Textures statiques pour l'animation 2 frames
    private static Texture2D _orbFrame1;
    private static Texture2D _orbFrame2;
    private static Texture2D OrbFrame1 => _orbFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_orb_xp.png");
    private static Texture2D OrbFrame2 => _orbFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_orb_xp_f2.png");

    private static SpriteFrames _frames;
    private static readonly StringName PulseAnimation = "pulse";

    private GpuParticles2D _glow;
    private AnimatedSprite2D _sprite;
    private Action<XpOrb> _release;

    public void SetRelease(Action<XpOrb> release)
    {
        _release = release;
    }

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

        if (_frames == null)
        {
            _frames = new SpriteFrames();
            _frames.AddAnimation(PulseAnimation);
            _frames.SetAnimationSpeed(PulseAnimation, 4);
            _frames.SetAnimationLoopMode(PulseAnimation, SpriteFrames.LoopMode.Linear);
            _frames.AddFrame(PulseAnimation, OrbFrame1);
            _frames.AddFrame(PulseAnimation, OrbFrame2);
        }

        _sprite = new AnimatedSprite2D
        {
            Name = "Visual",
            SpriteFrames = _frames,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        AddChild(_sprite);
        _visualRoot = _sprite;

        _glow = VfxFactory.CreateXpOrbGlow();
        AddChild(_glow);
    }

    /// <summary>
    /// Pose une orbe recyclée par CombatPools : tout l'état se réinitialise ici. Avec <paramref name="origin"/>,
    /// elle saute de ce point jusqu'à <paramref name="position"/>.
    /// </summary>
    public void Launch(Vector2 position, float xpValue, Vector2? origin = null)
    {
        _hopFrom = origin ?? position;
        _hopTo = position;
        _hopElapsed = origin.HasValue ? 0f : -1f;
        GlobalPosition = _hopFrom;
        _sprite.Scale = Vector2.One;
        _sprite.Rotation = 0f;
        _xpValue = xpValue;
        _currentSpeed = 0f;
        _collected = false;
        _trailBound = false;
        _trailCheckTimer = (float)GD.RandRange(0.0, TrailCheckInterval);
        IsAsleep = false;
        // Léger décalage pour désynchroniser le flottement des orbes entre elles
        _floatTime = (float)GD.RandRange(0, Mathf.Tau);
        _sprite.Play(PulseAnimation);
        bool glow = VfxFactory.CurrentParticleLevel != ParticleLevel.Off;
        _glow.Visible = glow;
        _glow.Emitting = glow;
        Visible = true;
        Roster.Add(this);
        // Pendant le saut, pas de ramassage : la détection s'allume à l'atterrissage (et signale alors un joueur déjà là).
        SetDeferred(Area2D.PropertyName.Monitoring, !origin.HasValue);
    }

    public override void _ExitTree()
    {
        Roster.Remove(this);
    }

    /// <summary>Un tick de l'orbe éveillée, appelé par la boucle des orbes.</summary>
    public void PhysicsTick(double delta)
    {
        if (_collected)
            return;

        float dt = (float)delta;

        if (_hopElapsed >= 0f)
        {
            _hopElapsed += dt;
            float t = Mathf.Clamp(_hopElapsed / HopSec, 0f, 1f);
            GlobalPosition = _hopFrom.Lerp(_hopTo, 1f - (1f - t) * (1f - t));
            _visualRoot.Position = new Vector2(0f, -Mathf.Sin(t * Mathf.Pi) * HopHeightPx);
            // Pop : petite au départ, légèrement écrasée à l'atterrissage.
            _sprite.Scale = t < 1f ? Vector2.One * Mathf.Lerp(0.5f, 1f, Mathf.Min(1f, t * 2.5f)) : Vector2.One;
            if (t >= 1f)
            {
                _hopElapsed = -1f;
                SetDeferred(Area2D.PropertyName.Monitoring, true);
            }
            return;
        }

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
        if (!_trailBound && distSq >= attractionRadiusSq && XpTrail.Any)
        {
            _trailCheckTimer -= dt;
            if (_trailCheckTimer <= 0f)
            {
                _trailCheckTimer = TrailCheckInterval;
                _trailBound = XpTrail.Covers(GlobalPosition);
            }
        }
        float sleepRadius = SleepRadius(_player);
        if (!_trailBound && distSq > sleepRadius * sleepRadius)
        {
            Sleep();
            return;
        }

        // Une orbe prise dans le Sillage rejoint le joueur jusqu'au bout, même si le couloir vieillit entre-temps.
        if (distSq < attractionRadiusSq || _trailBound)
        {
            _currentSpeed = Mathf.Min(_currentSpeed + Acceleration * dt, MaxSpeed);
            Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += direction * _currentSpeed * dt;
            Stretch(direction, dt);
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

        if (body is Player player)
        {
            _collected = true;
            // Ramassée en dormant (le joueur marche dessus) : CombatPools l'oubliera à sa prochaine ronde.
            IsAsleep = false;

            CombatPools.Instance?.ShowXpCollect(GlobalPosition);

            ulong now = AudioManager.NowMsec;
            _chain = now - _lastCollectMs < ChainWindowMs ? Mathf.Min(_chain + 1, ChainMax) : 0;
            _lastCollectMs = now;
            AudioManager.Play("xp_gain", 0.01f, -1.5f, 1f + _chain * ChainPitchStep);

            EventBus eventBus = GetNode<EventBus>("/root/EventBus");
            eventBus.EmitSignal(EventBus.SignalName.XpGained, _xpValue);
            player.OnXpOrbCollected();

            // Retour au pool : invisible, inerte, prête pour la prochaine mort.
            Visible = false;
            _sprite.Stop();
            _glow.Emitting = false;
            Roster.Remove(this);
            SetDeferred(Area2D.PropertyName.Monitoring, false);
            _release(this);
        }
    }

    /// <summary>Aspirée, l'orbe s'étire dans sa course et sème des éclats d'Essence derrière elle.</summary>
    private void Stretch(Vector2 direction, float dt)
    {
        float k = _currentSpeed / MaxSpeed;
        _sprite.Rotation = direction.Angle();
        _sprite.Scale = new Vector2(1f + k * 0.7f, 1f - k * 0.3f);
        _trailTimer -= dt;
        if (_currentSpeed < TrailMinSpeed || _trailTimer > 0f || CombatPools.Instance == null)
            return;
        _trailTimer = TrailInterval;
        CombatPools.Instance.EmitSparks(GlobalPosition, new SparkBurst
        {
            Family = FxFamily.Essence,
            Owner = FxOwner.World,
            Count = 1,
            Direction = -direction,
            Spread = 0.6f,
            SpeedMin = 10f,
            SpeedMax = 30f,
            LifeMin = 0.12f,
            LifeMax = 0.22f,
            Size = 1,
            Decorative = true,
        });
    }

    /// <summary>Inerte loin du joueur ; la zone reste active, marcher dessus la ramasse quand même.</summary>
    private void Sleep()
    {
        IsAsleep = true;
        SleepToken++;
        Roster.Remove(this);
        _sprite.Pause();
        _glow.Emitting = false;
        CombatPools.Instance?.AddSleepingOrb(this);
    }

    /// <summary>Réveillée par la ronde ; <paramref name="trailBound"/> : le Sillage l'appelle depuis le couloir.</summary>
    public void Wake(bool trailBound = false)
    {
        IsAsleep = false;
        _trailBound = trailBound;
        _sprite.Play(PulseAnimation);
        // Le réglage des particules a pu changer pendant le sommeil.
        _glow.Visible = VfxFactory.CurrentParticleLevel != ParticleLevel.Off;
        _glow.Emitting = _glow.Visible;
        Roster.Add(this);
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
