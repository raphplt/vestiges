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
    private Sprite2D _sprite;
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

    // Performance cache
    private GroupCache _groupCache;

    // Weapon reference for on-hit effects
    public Infrastructure.WeaponData SourceWeapon { get; set; }

    // --- Textures des projectiles (chargées une seule fois, cachées en static) ---
    private static Texture2D _arrowTexture;
    private static Texture2D _boltTexture;
    private static Texture2D _stoneTexture;
    private static Texture2D _impactTexture;

    private static Texture2D ArrowTexture => _arrowTexture ??= GD.Load<Texture2D>("res://assets/vfx/vfx_arrow.png");
    private static Texture2D BoltTexture => _boltTexture ??= GD.Load<Texture2D>("res://assets/vfx/vfx_bolt.png");
    private static Texture2D StoneTexture => _stoneTexture ??= GD.Load<Texture2D>("res://assets/vfx/vfx_stone_projectile.png");
    private static Texture2D ImpactTexture => _impactTexture ??= GD.Load<Texture2D>("res://assets/vfx/vfx_impact_projectile.png");

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
        _sprite = GetNodeOrNull<Sprite2D>("Visual");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
        BodyEntered += OnBodyEntered;
        GetTree().CreateTimer(MaxLifetime).Timeout += QueueFree;

        if (_sprite != null)
        {
            // Assigner le bon sprite selon l'arme source
            AssignProjectileSprite();

            _sprite.Scale = _isCrit ? new Vector2(1.35f, 1.35f) : Vector2.One;
            _sprite.Modulate = new Color(1f, 1f, 1f, 0.92f);
            Tween spawnTween = CreateTween();
            spawnTween.TweenProperty(_sprite, "scale", _isCrit ? new Vector2(1.2f, 1.2f) : Vector2.One, 0.08f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);

            // Trail de particules derrière le projectile
            Color trailColor = _sprite.SelfModulate;
            GpuParticles2D trail = VfxFactory.CreateProjectileTrail(trailColor, _isCrit);
            if (trail != null)
                AddChild(trail);
        }
    }

    /// <summary>
    /// Assigne le sprite de projectile correspondant à l'arme équipée.
    /// Arc → flèche, Arbalète → carreau, Fronde → pierre, autre → flèche par défaut.
    /// Applique la teinte DamageType via SelfModulate.
    /// </summary>
    private void AssignProjectileSprite()
    {
        string weaponId = SourceWeapon?.Id ?? "";

        // Sélection du sprite basée sur l'ID de l'arme
        Texture2D texture;
        if (weaponId.Contains("bow") || weaponId.Contains("arc"))
            texture = ArrowTexture;
        else if (weaponId.Contains("crossbow") || weaponId.Contains("arbalete"))
            texture = BoltTexture;
        else if (weaponId.Contains("sling") || weaponId.Contains("fronde") || weaponId.Contains("throwing"))
            texture = StoneTexture;
        else
            texture = ArrowTexture; // Projectile par défaut

        _sprite.Texture = texture;

        // Appliquer la teinte DamageType
        string damageType = SourceWeapon?.DamageType ?? "physical";
        _sprite.SelfModulate = damageType switch
        {
            "essence" => new Color(0.45f, 0.85f, 1f),
            "hybrid" => new Color(0.85f, 0.65f, 1f),
            _ => new Color(1f, 0.85f, 0.2f)
        };
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
        Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
        Node2D nearest = null;
        float nearestDistSq = 500f * 500f;

        foreach (Node node in enemies)
        {
            if (node is Node2D candidate && !candidate.IsQueuedForDeletion())
            {
                float distSq = GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
                if (distSq < nearestDistSq)
                {
                    nearest = candidate;
                    nearestDistSq = distSq;
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

    private static Texture2D _groundFireTexture;

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

        _groundFireTexture ??= GD.Load<Texture2D>("res://icon.svg");
        PointLight2D light = new()
        {
            Color = new Color(1f, 0.6f, 0.2f),
            Energy = 0.6f,
            TextureScale = _groundRadius / 64f,
            Texture = _groundFireTexture
        };
        fire.AddChild(light);

        GetTree().CurrentScene.AddChild(fire);

        float totalDuration = _groundDuration;
        float dmg = _groundDamage;
        float radius = _groundRadius;
        float radiusSq = radius * radius;
        GroupCache groupCache = _groupCache;

        // Single timer with repeat instead of recursive timer creation
        Timer tickTimer = new() { WaitTime = 0.5f, Autostart = true };
        float elapsed = 0f;
        tickTimer.Timeout += () =>
        {
            if (!IsInstanceValid(fire))
            {
                tickTimer.QueueFree();
                return;
            }
            Godot.Collections.Array<Node> enemies = groupCache.GetEnemies();
            foreach (Node node in enemies)
            {
                if (node is Enemy e && IsInstanceValid(e) && !e.IsDying)
                {
                    if (e.GlobalPosition.DistanceSquaredTo(position) < radiusSq)
                        e.TakeDamage(dmg);
                }
            }
            elapsed += 0.5f;
            if (elapsed >= totalDuration)
                tickTimer.Stop();
        };
        fire.AddChild(tickTimer);

        // Fade out et destruction
        Tween tween = fire.CreateTween();
        tween.TweenProperty(glow, "modulate:a", 0f, totalDuration);
        tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(fire)) fire.QueueFree(); }));
    }

    private void PulseOnHit()
    {
        if (_sprite == null || _isDespawning)
            return;

        Tween pulse = CreateTween();
        pulse.SetParallel();
        pulse.TweenProperty(_sprite, "scale", new Vector2(1.25f, 0.85f), 0.04f);
        pulse.TweenProperty(_sprite, "modulate:a", 0.8f, 0.04f);
        pulse.Chain().SetParallel();
        pulse.TweenProperty(_sprite, "scale", Vector2.One, 0.05f);
        pulse.TweenProperty(_sprite, "modulate:a", 0.95f, 0.05f);
    }

    private void StartDespawn()
    {
        if (_isDespawning)
            return;

        _isDespawning = true;
        SetDeferred("monitoring", false);

        // Impact animé 3 frames (remplace l'ancien sprite statique)
        Color impactColor = _sprite?.SelfModulate ?? new Color(1f, 0.85f, 0.2f);
        Node2D animatedImpact = VfxFactory.CreateAnimatedImpactVfx(GlobalPosition, impactColor);
        if (animatedImpact != null)
            GetTree().CurrentScene.CallDeferred("add_child", animatedImpact);

        // Particules d'impact complémentaires
        Node2D impact = VfxFactory.CreateProjectileImpact(GlobalPosition, impactColor);
        if (impact != null)
            GetTree().CurrentScene.CallDeferred("add_child", impact);

        if (_sprite == null)
        {
            CallDeferred(MethodName.QueueFree);
            return;
        }

        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_sprite, "scale", new Vector2(0.25f, 0.25f), 0.08f);
        tween.TweenProperty(_sprite, "modulate:a", 0f, 0.08f);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    /// <summary>
    /// Affiche le sprite d'impact (vfx_impact_projectile) à la position de collision.
    /// </summary>
    private void SpawnImpactSprite()
    {
        Texture2D impactTex = ImpactTexture;
        if (impactTex == null)
            return;

        Sprite2D impactSprite = new()
        {
            GlobalPosition = GlobalPosition,
            Texture = impactTex,
            TextureFilter = TextureFilterEnum.Nearest,
            SelfModulate = _sprite?.SelfModulate ?? new Color(1f, 0.85f, 0.2f),
        };

        GetTree().CurrentScene.CallDeferred("add_child", impactSprite);

        // Appeler le tween après ajout à l'arbre
        impactSprite.TreeEntered += () =>
        {
            Tween tween = impactSprite.CreateTween();
            tween.SetParallel();
            tween.TweenProperty(impactSprite, "scale", new Vector2(1.8f, 1.8f), 0.12f);
            tween.TweenProperty(impactSprite, "modulate:a", 0f, 0.15f);
            tween.Chain().TweenCallback(Callable.From(impactSprite.QueueFree));
        };
    }
}
