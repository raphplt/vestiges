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
public partial class Projectile : Area2D
{

    public float Speed { get; private set; } = 400f;

    private Vector2 _direction;
    private float _damage;
    private float _lifetime;
    private float _age;
    private int _pierceRemaining;
    private bool _isCrit;
    private bool _isDespawning;
    private Player _owner;
    private AttackContext _context;
    private Sprite2D _sprite;
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
    private Node2D _homingTarget;

    // Ground fire (weapon special_effect)
    private bool _spawnsGroundFire;
    private float _groundDamage;
    private float _groundDuration;
    private float _groundRadius;

    private GroupCache _groupCache;

    /// <summary>Arme d'origine : effets à l'impact et apparence.</summary>
    public WeaponData SourceWeapon { get; private set; }
    /// <summary>Arme portée qui a tiré : ses effets au contact et son recul s'appliquent, pas ceux de la dernière arme.</summary>
    public WeaponInstance SourceInstance { get; private set; }

    public void SetRelease(Action<Projectile> release)
    {
        _release = release;
    }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Visual");
        _sprite.Position = new Vector2(0f, -Iso.FlightHeight);
        // Au sol sous le projectile : c'est l'écart entre l'ombre et le visuel qui dit qu'il vole.
        AddChild(GroundShadow.Create(8f));
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
        BodyEntered += OnBodyEntered;
    }

    public void Launch(Vector2 position, Vector2 direction, float damage, float speed, float lifetime, int pierce,
                       bool isCrit, Player owner, WeaponData weapon, WeaponInstance source, AttackContext context = default)
    {
        GlobalPosition = position;
        _direction = direction.Normalized();
        _damage = damage;
        Speed = speed;
        _lifetime = lifetime;
        _age = 0f;
        _pierceRemaining = pierce;
        _isCrit = isCrit;
        _owner = owner;
        _context = context.OwnerId != 0 || owner == null ? context
            : owner.BeginAttack(source, damage);
        SourceWeapon = weapon;
        SourceInstance = source;
        _isDespawning = false;
        _hitEnemies.Clear();
        _homingStrength = 0f;
        _homingTarget = null;
        _spawnsGroundFire = false;

        _family = isCrit ? FxFamily.Crit : PlayerAttackFx.FamilyOf(weapon);
        string spriteId = weapon?.Fx.Projectile ?? "arrow";
        if (spriteId == "orb")
            spriteId = _family == FxFamily.Fire ? "orb_fire" : "orb_essence";
        _glowTrail = spriteId is "orb_fire" or "orb_essence" or "shard" or "note";
        _spriteSet = ProjectileSprites.Get(spriteId) ?? ProjectileSprites.Get("arrow");
        _spriteDirection = -1;
        _spriteFrame = -1;
        _sprite.Visible = CombatFxSettings.PlayerProjectiles && _spriteSet != null;
        _sprite.Modulate = new Color(1f, 1f, 1f, CombatFxSettings.PlayerOpacity);
        UpdateSprite();

        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
        SetDeferred(Area2D.PropertyName.Monitoring, true);
    }

    public void SetHoming(float strength, Node2D target)
    {
        _homingStrength = strength;
        _homingTarget = target;
    }

    /// <summary>Copie du Papier carbone : même vol, teinte bleutée et plus pâle, pour la distinguer du tir plein.</summary>
    public void MarkAsCopy()
    {
        _sprite.Modulate = new Color(0.7f, 0.78f, 1f, CombatFxSettings.PlayerOpacity * 0.75f);
    }

    public void SetGroundFire(float damage, float duration, float radius)
    {
        _spawnsGroundFire = true;
        _groundDamage = damage;
        _groundDuration = duration;
        _groundRadius = radius;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDespawning)
            return;

        float dt = (float)delta;
        _age += dt;
        if (_age >= _lifetime)
        {
            if (_owner != null && IsInstanceValid(_owner))
                _owner.OnProjectileSpent(GlobalPosition, _damage, _context);
            // Différé comme à l'impact : le retour au pool et la désactivation doivent passer ensemble,
            // sinon une relance dans la même frame serait désactivée après coup.
            _isDespawning = true;
            CallDeferred(MethodName.Release);
            return;
        }

        if (_homingStrength > 0f && _homingTarget != null && IsInstanceValid(_homingTarget))
        {
            Vector2 toTarget = (_homingTarget.GlobalPosition - GlobalPosition).Normalized();
            _direction = _direction.Lerp(toTarget, _homingStrength * dt * 5f).Normalized();
        }
        else if (_homingStrength > 0f)
        {
            _homingTarget = FindNearestEnemy();
        }

        Position += _direction * Speed * dt;
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
        // monitoring n'est coupé qu'en différé : sans cette garde, tous les corps déjà superposés
        // au point de tir recevraient l'impact dans le même flush, perforation ou non.
        if (_isDespawning)
            return;

        if (body is Enemy enemy && !enemy.IsQueuedForDeletion())
        {
            ulong id = enemy.GetInstanceId();
            if (_hitEnemies.Contains(id))
                return;

            _hitEnemies.Add(id);
            bool ownerValid = _owner != null && IsInstanceValid(_owner);
            float damage = ownerValid ? _owner.ResolveHitDamage(enemy, _damage, _isCrit) : _damage;
            enemy.TakeDamage(damage, _isCrit, source: _context);

            // Notify owner for perk effects (vampirism, ignite, execution, ricochet)
            if (ownerValid)
                _owner.OnProjectileHit(enemy, damage, _isCrit, SourceInstance, _context);

            if (_spawnsGroundFire)
            {
                GroundFire.Spawn(enemy.GlobalPosition, _groundDamage, _groundDuration, _groundRadius, _context.As(DamageKind.DamageOverTime));
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
            }
        }
    }

    /// <summary>
    /// Impact sur une cible qui n'est pas une créature (l'Indicible) : donne ses dégâts et rentre au pool, sans
    /// perforation. Faux si le projectile a déjà touché ce qui l'arrête.
    /// </summary>
    public bool TryAbsorb(out float damage)
    {
        damage = _damage;
        if (_isDespawning)
            return false;
        _isDespawning = true;
        CallDeferred(MethodName.Release);
        return true;
    }

    private void Release()
    {
        _isDespawning = true;
        Visible = false;
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        // Hors traitement : retiré de la physique (DisableMode Remove) jusqu'au prochain Launch.
        SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
        _homingTarget = null;
        _owner = null;
        _context = default;
        SourceInstance = null;
        if (_release != null)
            _release(this);
        else
            QueueFree();
    }
}
