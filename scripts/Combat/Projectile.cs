using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Projectile du joueur, recyclé par CombatPools : lancé par Launch, rendu au pool à l'impact ou en fin de course.
/// Le sprite est prérendu dans la direction de vol (jamais tourné) et vole à hauteur de buste ;
/// la collision reste au sol, là où sont les corps.
/// </summary>
public partial class Projectile : Area2D, ITicked
{
    // Godot n'appelle plus les projectiles un par un : une seule boucle C# les avance tous (plan 29).
    private static readonly TickRoster<Projectile> Roster = new("PlayerProjectiles");
    public int TickSlot { get; set; } = -1;


    public float Speed { get; private set; } = 400f;

    private Vector2 _direction;
    private float _damage;
    private float _lifetime;
    private float _age;
    // Rafale (plan 21 G6g) : le tir attend, caché et sans contact, avant de partir de la position du joueur.
    private float _launchDelay;
    private TargetLock _departureTarget;
    private int _pierceRemaining;
    // Reflet brisé, palier 15 : chaque ennemi traversé ajoute cette part des dégâts de départ.
    private float _pierceDamageRamp;
    private float _launchDamage;
    private bool _isCrit;
    private bool _isDespawning;
    private Player _owner;
    private AttackContext _context;
    private Sprite2D _sprite;
    private CollisionShape2D _collision;
    // Ombre au sol dessinée en lot par GroundShadowLayer (plan 29 C2) ; seulement une fois parti, comme le tir.
    private readonly ShadowCaster _shadow;
    // Rayon de touche des créatures, taille comprise : elles n'ont plus de corps physique (plan 29 B).
    private float _baseHitRadius;
    private float _hitRadius;
    private readonly List<(float along, Enemy enemy)> _crossed = new();
    private ProjectileSprites.SpriteSet _spriteSet;
    private int _spriteDirection = -1;
    private int _spriteFrame = -1;
    private FxFamily _family;
    private bool _glowTrail;
    private ulong _trailFrame;
    private Action<Projectile> _release;
    private readonly HashSet<ulong> _hitEnemies = new();

    // Homing
    private float _homingStrength;
    private TargetLock _homingTarget;

    // Ground fire (weapon special_effect)
    private bool _spawnsGroundFire;
    private float _groundDamage;
    private float _groundDuration;
    private float _groundRadius;
    private float _groundBurnSeconds;

    /// <summary>Arme d'origine : effets à l'impact et apparence.</summary>
    public WeaponData SourceWeapon { get; private set; }
    /// <summary>Arme portée qui a tiré : ses effets au contact et son recul s'appliquent, pas ceux de la dernière arme.</summary>
    public WeaponInstance SourceInstance { get; private set; }

    public Projectile()
    {
        _shadow = new ShadowCaster(this) { Width = GroundShadow.SnapWidth(8f) };
    }

    public void SetRelease(Action<Projectile> release)
    {
        _release = release;
    }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Visual");
        _sprite.Material = PlayerAttackFx.ProjectileMaterial;
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
        _sprite.Position = new Vector2(0f, -Iso.FlightHeight);
        // Au sol sous le projectile : c'est l'écart entre l'ombre et le visuel qui dit qu'il vole.
        _baseHitRadius = ((CircleShape2D)_collision.Shape).Radius;
    }

    public void Launch(Vector2 position, Vector2 direction, float damage, float speed, float lifetime, int pierce,
                       bool isCrit, Player owner, WeaponData weapon, WeaponInstance source, AttackContext context = default,
                       float pierceDamageRamp = 0f, float sizeScale = 1f, float launchDelay = 0f)
    {
        GlobalPosition = position;
        _launchDelay = launchDelay;
        _departureTarget = default;
        // Taille du joueur (plan 21 G6e) : la zone de contact grandit avec le visuel, remise à chaque tir du pool.
        _collision.Scale = Vector2.One * sizeScale;
        _hitRadius = _baseHitRadius * sizeScale;
        _direction = direction.Normalized();
        _damage = damage;
        Speed = speed;
        _lifetime = lifetime;
        _age = 0f;
        _pierceRemaining = pierce;
        _pierceDamageRamp = pierceDamageRamp;
        _launchDamage = damage;
        _isCrit = isCrit;
        _owner = owner;
        _context = context.OwnerId != 0 || owner == null ? context
            : owner.BeginAttack(source, damage);
        SourceWeapon = weapon;
        SourceInstance = source;
        _isDespawning = false;
        _hitEnemies.Clear();
        _homingStrength = 0f;
        _homingTarget = default;
        _spawnsGroundFire = false;

        _family = isCrit ? FxFamily.Crit : PlayerAttackFx.FamilyOf(weapon);
        string spriteId = weapon?.Fx.Projectile ?? "arrow";
        if (spriteId == "orb")
            spriteId = _family == FxFamily.Fire ? "orb_fire" : "orb_essence";
        _glowTrail = spriteId is "orb_fire" or "orb_essence" or "shard" or "note" or "flash";
        _spriteSet = ProjectileSprites.Get(spriteId) ?? ProjectileSprites.Get("arrow");
        // Les petits sprites (notes, billes, orbes) ont une échelle de base, sans toucher à la zone de contact (§53).
        _sprite.Scale = Vector2.One * sizeScale * WeaponVisualConfig.Load().ProjectileBaseScale(spriteId);
        _spriteDirection = -1;
        _spriteFrame = -1;
        _sprite.Visible = CombatFxSettings.PlayerProjectiles && _spriteSet != null;
        _sprite.Modulate = new Color(1f, 1f, 1f, CombatFxSettings.PlayerOpacity);
        UpdateSprite();

        Visible = _launchDelay <= 0f;
        if (Visible)
            GroundShadowLayer.Add(_shadow);
        ProcessMode = ProcessModeEnum.Inherit;
        Roster.Add(this);
    }

    /// <summary>Tir de rafale : au départ, il vise de nouveau cette cible si elle vit encore, comme un tir neuf.</summary>
    public void AimAtDeparture(Node2D target) => _departureTarget = TargetLock.On(target);

    /// <summary>Fin de l'attente d'une rafale : le tir part du joueur, là où il se trouve maintenant.</summary>
    private void Depart()
    {
        if (_owner != null && IsInstanceValid(_owner))
            GlobalPosition = _owner.GlobalPosition;
        if (_departureTarget.IsSet)
        {
            // Cible tombée ou revenue du pool pendant l'attente : la plus proche, comme le ferait un tir neuf.
            Node2D target = _departureTarget.TryGet(out Node2D locked) ? locked : FindNearestEnemy();
            Vector2 toTarget = target != null ? target.GlobalPosition - GlobalPosition : Vector2.Zero;
            if (toTarget.LengthSquared() > 0.0001f)
                _direction = toTarget.Normalized();
        }
        _departureTarget = default;
        Visible = true;
        GroundShadowLayer.Add(_shadow);
    }

    /// <summary>Retire le projectile de la boucle : un test le fait alors avancer lui-même.</summary>
    public void StopTicking() => Roster.Remove(this);

    public void SetHoming(float strength, Node2D target)
    {
        _homingStrength = strength;
        _homingTarget = TargetLock.On(target);
    }

    public void SetGroundFire(float damage, float duration, float radius, float burnSeconds)
    {
        _spawnsGroundFire = true;
        _groundDamage = damage;
        _groundDuration = duration;
        _groundRadius = radius;
        _groundBurnSeconds = burnSeconds;
    }

    public override void _ExitTree()
    {
        Roster.Remove(this);
        GroundShadowLayer.Remove(_shadow);
    }

    /// <summary>Un tick du projectile, appelé par la boucle des projectiles (ou directement par un test).</summary>
    public void PhysicsTick(double delta)
    {
        if (_isDespawning)
            return;

        float dt = (float)delta;
        if (_launchDelay > 0f)
        {
            _launchDelay -= dt;
            if (_launchDelay > 0f)
                return;
            Depart();
        }
        _age += dt;
        if (_age >= _lifetime)
        {
            if (_owner != null && IsInstanceValid(_owner))
                _owner.OnProjectileSpent(GlobalPosition, _damage, _context, _family, _direction);
            // Différé comme à l'impact : le retour au pool et la désactivation doivent passer ensemble,
            // sinon une relance dans la même frame serait désactivée après coup.
            _isDespawning = true;
            CallDeferred(MethodName.Release);
            return;
        }

        if (_homingStrength > 0f && _homingTarget.TryGet(out Node2D homingTarget))
        {
            Vector2 toTarget = (homingTarget.GlobalPosition - GlobalPosition).Normalized();
            _direction = _direction.Lerp(toTarget, _homingStrength * dt * 5f).Normalized();
        }
        else if (_homingStrength > 0f)
        {
            _homingTarget = TargetLock.On(FindNearestEnemy());
        }

        Vector2 from = GlobalPosition;
        Position += _direction * Speed * dt;
        HitAlong(from, GlobalPosition);
        if (_isDespawning)
            return;
        UpdateSprite();
        EmitTrail();
    }

    private void UpdateSprite()
    {
        if (_spriteSet == null)
            return;
        int direction = _spriteSet.DirectionIndex(_direction);
        int frame = _spriteSet.Fps > 0f ? (int)(_age * _spriteSet.Fps) % _spriteSet.Frames : 0;
        if (direction == _spriteDirection && frame == _spriteFrame)
            return;
        _spriteDirection = direction;
        _spriteFrame = frame;
        _sprite.Texture = _spriteSet.Get(direction, frame);
    }

    /// <summary>Un pixel de traînée toutes les deux frames, pour les projectiles lumineux et les critiques.</summary>
    private void EmitTrail()
    {
        if (!_sprite.Visible || (!_glowTrail && !_isCrit) || CombatPools.Instance == null)
            return;
        ulong frame = Engine.GetPhysicsFrames();
        if (frame - _trailFrame < 2)
            return;
        _trailFrame = frame;
        CombatPools.Instance.EmitSparks(GlobalPosition + new Vector2(0f, -Iso.FlightHeight), new SparkBurst
        {
            Family = _family,
            Owner = FxOwner.Player,
            Count = 1,
            Direction = -_direction,
            Spread = 0.6f,
            SpeedMin = 10f,
            SpeedMax = 30f,
            LifeMin = 0.12f,
            LifeMax = 0.2f,
            Size = 1,
        });
    }

    private Node2D FindNearestEnemy()
    {
        using CrowdQuery crowd = CrowdIndex.Near(GlobalPosition, 500f);
        Node2D nearest = null;
        float nearestDistSq = 500f * 500f;

        foreach (Node node in crowd.Targets)
        {
            // Garde de sûreté : une créature libérée ou mourante n'est jamais une cible.
            if (node is not Node2D candidate || candidate.IsQueuedForDeletion() || candidate is Enemy { IsActive: false } or Enemy { IsDying: true })
                continue;
            float distSq = GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
            if (distSq < nearestDistSq)
            {
                nearest = candidate;
                nearestDistSq = distSq;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Créatures touchées sur le trajet du tick, dans l'ordre où le projectile les croise : un segment plutôt qu'un
    /// point, pour qu'un tir rapide ne traverse pas une créature entre deux ticks. Remplace la zone physique, les
    /// créatures n'ayant plus de corps (plan 29 B).
    /// </summary>
    private void HitAlong(Vector2 from, Vector2 to)
    {
        Vector2 path = to - from;
        float length = path.Length();
        Vector2 middle = (from + to) * 0.5f;
        using CrowdQuery crowd = CrowdIndex.Near(middle, length * 0.5f + _hitRadius + Enemy.LargestBodyRadius);
        _crossed.Clear();
        foreach (Node2D node in crowd.Targets)
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying || enemy.IsBurrowed)
                continue;
            Vector2 center = enemy.GlobalPosition;
            float reach = _hitRadius + enemy.BodyRadius;
            if (Geometry2D.GetClosestPointToSegment(center, from, to).DistanceSquaredTo(center) > reach * reach)
                continue;
            float along = length > 0.0001f ? (center - from).Dot(path) / length : 0f;
            _crossed.Add((along, enemy));
        }
        if (_crossed.Count > 1)
            _crossed.Sort(static (a, b) => a.along.CompareTo(b.along));
        foreach ((float _, Enemy enemy) in _crossed)
        {
            if (_isDespawning)
                break;
            Hit(enemy);
        }
    }

    private void Hit(Enemy enemy)
    {
        // Un impact précédent du même tick a pu la tuer ou la rendre au pool (ricochet, explosion, chaîne).
        if (!enemy.IsActive || enemy.IsDying || enemy.IsBurrowed)
            return;
        ulong id = enemy.GetInstanceId();
        if (_hitEnemies.Contains(id))
            return;

        _hitEnemies.Add(id);
        bool ownerValid = _owner != null && IsInstanceValid(_owner);
        float damage = ownerValid ? _owner.ResolveHitDamage(enemy, _damage, _isCrit) : _damage;
        enemy.TakeDamage(damage, _isCrit, source: _context);

        // Notify owner for perk effects (vampirism, ignite, execution, ricochet)
        if (ownerValid)
            _owner.OnProjectileHit(enemy, damage, _isCrit, SourceInstance, _context, _damage);

        if (_spawnsGroundFire)
        {
            GroundFire.Spawn(enemy.GlobalPosition, _groundDamage, _groundDuration, _groundRadius, _groundBurnSeconds,
                _context.As(DamageKind.DamageOverTime));
            _spawnsGroundFire = false;
        }

        if (_pierceRemaining <= 0)
        {
            _isDespawning = true;
            CallDeferred(MethodName.Release);
        }
        else
        {
            _pierceRemaining--;
            _damage += _launchDamage * _pierceDamageRamp;
        }
    }

    private void Release()
    {
        Roster.Remove(this);
        GroundShadowLayer.Remove(_shadow);
        _isDespawning = true;
        Visible = false;
        // Hors traitement : retiré de la physique (DisableMode Remove) jusqu'au prochain Launch.
        SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
        _homingTarget = default;
        _departureTarget = default;
        _owner = null;
        _context = default;
        SourceInstance = null;
        if (_release != null)
            _release(this);
        else
            QueueFree();
    }
}
