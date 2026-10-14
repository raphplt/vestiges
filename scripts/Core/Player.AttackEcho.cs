using System;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Rémanence (Double de l'Éveillée, plan 06 P3b) : armée par le module du passif après une mobilité, elle répète la
/// prochaine attaque de l'arme qui se déclenche, depuis son point et à une part des dégâts. Les orbites continues
/// ne consomment pas la rémanence.
/// </summary>
public partial class Player
{
    private Vector2? _attackOriginOverride;
    private float _attackDamageScale = 1f;
    private bool _isEchoAttack;
    private Vector2 _echoOrigin;
    private float _echoDamageRatio;
    private PlayerAttackFx _echoAttackFx;
    private bool _isEchoConeActive;
    private float _echoConeElapsed;
    private float _echoConeDuration;
    private float _echoConeRamp;
    private float _echoConeAngleStart;
    private float _echoConeAngleEnd;
    private float _echoConeRange;
    private float _echoConeDamage;
    private int _echoConeWaves;
    private Vector2 _echoConeOrigin;
    private Vector2 _echoConeDirection;
    private WeaponInstance _echoConeWeapon;
    private AttackContext _echoConeContext;

    /// <summary>Une rémanence attend la prochaine attaque.</summary>
    public bool HasAttackEcho { get; private set; }

    /// <summary>La rémanence a répété une attaque, depuis ce point.</summary>
    public event Action<Vector2> AttackEchoed;

    /// <summary>Point d'où partent les attaques : le joueur, ou la rémanence pendant sa répétition.</summary>
    private Vector2 AttackOrigin => _attackOriginOverride ?? GlobalPosition;

    public void ArmAttackEcho(Vector2 origin, float damageRatio)
    {
        if (_isDead || !float.IsFinite(damageRatio) || damageRatio <= 0f || !origin.IsFinite())
            return;
        _echoOrigin = origin;
        _echoDamageRatio = damageRatio;
        HasAttackEcho = true;
    }

    public void DisarmAttackEcho() => HasAttackEcho = false;

    private void RepeatFromEcho(WeaponCategory category, AttackPatternKind pattern)
    {
        if (!HasAttackEcho || _isDead || pattern == AttackPatternKind.Orbital)
            return;
        _attackOriginOverride = _echoOrigin;
        _attackDamageScale = _echoDamageRatio;
        _isEchoAttack = true;
        bool fired;
        try
        {
            fired = PerformDiscreteAttack(category, pattern);
        }
        finally
        {
            _attackOriginOverride = null;
            _attackDamageScale = 1f;
            _isEchoAttack = false;
        }
        // Sans cible à portée de la rémanence, l'occasion reste ouverte jusqu'à la fin de sa durée.
        if (!fired)
            return;
        ConsumeAttackEcho();
    }

    private void ConsumeAttackEcho()
    {
        HasAttackEcho = false;
        AttackEchoed?.Invoke(_echoOrigin);
    }

    /// <summary>Le cône répété garde son origine, sa direction et ses dégâts pendant toute l'émission.</summary>
    private void RepeatConeFromEcho()
    {
        if (!HasAttackEcho || _isDead)
            return;
        _attackOriginOverride = _echoOrigin;
        try
        {
            System.Collections.Generic.List<Enemy> targets = FindEnemiesInArc(_coneRange, 360f, squash: 1f / Iso.GroundSquash);
            if (targets.Count == 0)
                return;
            _echoAttackFx ??= new PlayerAttackFx(this, _sprite);
            _echoAttackFx.StopCone();
            _echoConeOrigin = _echoOrigin;
            _echoConeDirection = (targets[0].GlobalPosition - _echoOrigin).Normalized();
            _echoConeElapsed = 0f;
            _echoConeDuration = _coneDuration;
            _echoConeRamp = _coneDamageRampPerSec;
            _echoConeAngleStart = _coneAngleStart;
            _echoConeAngleEnd = _coneAngleEnd;
            _echoConeRange = _coneRange;
            _echoConeDamage = ComputeBaseAttackDamage(_coneWeapon) * _coneWaves * _echoDamageRatio;
            _echoConeWaves = _coneWaves;
            _echoConeWeapon = _coneWeapon;
            _echoConeContext = _coneContext with
            {
                Origin = _echoOrigin, IsEcho = true, LaunchId = ++_attackSequence,
                ReferenceDamage = _echoConeDamage * _echoConeDuration * (1f + _echoConeRamp * _echoConeDuration * 0.5f),
            };
            _isEchoConeActive = true;
            UpdateEchoConeVisual();
            ConsumeAttackEcho();
        }
        finally
        {
            _attackOriginOverride = null;
        }
    }

    private void UpdateEchoConeVisual()
    {
        float progress = Mathf.Clamp(_echoConeElapsed / _echoConeDuration, 0f, 1f);
        float angle = Mathf.Lerp(_echoConeAngleStart, _echoConeAngleEnd, progress);
        _echoAttackFx.UpdateCone(_echoConeWeapon.Base, _echoConeDirection, _echoConeRange,
            Mathf.DegToRad(angle * 0.5f), _echoConeWaves, _echoConeOrigin);
    }

    private void ProcessEchoCone(float dt)
    {
        if (!_isEchoConeActive || _isDead || GetTree().Paused)
            return;
        _echoConeElapsed += dt;
        if (_echoConeElapsed >= _echoConeDuration)
        {
            _isEchoConeActive = false;
            _echoConeWeapon = null;
            _echoAttackFx.StopCone();
            return;
        }
        UpdateEchoConeVisual();
        float progress = Mathf.Clamp(_echoConeElapsed / _echoConeDuration, 0f, 1f);
        float angle = Mathf.Lerp(_echoConeAngleStart, _echoConeAngleEnd, progress);
        float threshold = Mathf.Cos(Mathf.DegToRad(angle * 0.5f));
        float damage = _echoConeDamage * (1f + _echoConeElapsed * _echoConeRamp) * dt;
        Vector2 forward = Iso.ToGround(_echoConeDirection).Normalized();
        using CrowdQuery crowd = CrowdIndex.Near(_echoConeOrigin, _echoConeRange + Enemy.LargestPartRadius);
        foreach (Node node in crowd.Targets)
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy || !IsInstanceValid(enemy))
                continue;
            Vector2 toEnemy = Iso.ToGround(enemy.GlobalPosition - _echoConeOrigin);
            float distance = toEnemy.Length();
            if (distance <= 0.001f || distance - enemy.StrikeMargin > _echoConeRange
                || forward.Dot(toEnemy / distance) < threshold)
                continue;
            float hitDamage = ResolveHitDamage(enemy, damage, false);
            bool showImpact = enemy.TakeContinuousDamage(hitDamage, dt, _echoConeContext);
            OnAttackHit(enemy, hitDamage, damage, false, _echoConeWeapon, showImpact: showImpact, context: _echoConeContext);
        }
    }

    /// <summary>Une mort ou un changement de personnage retire aussi les attaques continues répétées.</summary>
    public void ResetAttackEcho()
    {
        DisarmAttackEcho();
        _isEchoConeActive = false;
        _echoConeWeapon = null;
        _echoAttackFx?.StopCone();
    }
}
