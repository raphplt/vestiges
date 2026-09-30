using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.Progression;

/// <summary>
/// Rondelle de cuivre, palier 25 : une zone d'arme refrappe peu après, à une part de ses dégâts. Un arc de mêlée
/// refrappe autour du joueur, là où il se tient ; une zone posée (écho, éclat) refrappe à sa place. La seconde frappe
/// ne déclenche aucun effet à l'impact (plan 21 §7, pas de récursion).
/// </summary>
public sealed class ZoneEchoes
{
    private struct Echo
    {
        public bool AroundPlayer;
        public Vector2 Center;
        public Vector2 Direction;
        public float Radius;
        public float ArcAngle;
        public float Damage;
        public float Remaining;
        public WeaponInstance Weapon;
        public AttackContext Source;
    }

    private readonly Player _player;
    private readonly GroupCache _groupCache;
    private readonly float _ratio;
    private readonly float _delay;
    private readonly List<Echo> _pending = new();

    public ZoneEchoes(Player player, GroupCache groupCache, float ratio, float delay)
    {
        _player = player;
        _groupCache = groupCache;
        _ratio = ratio;
        _delay = delay;
    }

    public bool HasPending => _pending.Count > 0;

    /// <summary>Arc ou cercle de mêlée centré sur le joueur ; <paramref name="damage"/> est le coup plein d'une cible.</summary>
    public void QueueArc(Vector2 direction, float range, float arcAngle, float damage, WeaponInstance weapon, AttackContext source)
    {
        _pending.Add(new Echo
        {
            AroundPlayer = true,
            Direction = direction,
            Radius = range,
            ArcAngle = arcAngle,
            Damage = damage * _ratio,
            Remaining = _delay,
            Weapon = weapon,
            Source = source.As(DamageKind.Passive),
        });
    }

    /// <summary>Zone circulaire posée au sol par une arme (écho des Gantelets, éclat du Dessin).</summary>
    public void QueueCircle(Vector2 center, float radius, float damage, WeaponInstance weapon, AttackContext source)
    {
        _pending.Add(new Echo
        {
            Center = center,
            Radius = radius,
            ArcAngle = 360f,
            Damage = damage * _ratio,
            Remaining = _delay,
            Weapon = weapon,
            Source = source.As(DamageKind.Passive),
        });
    }

    public void Advance(float delta)
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            Echo echo = _pending[i];
            echo.Remaining -= delta;
            if (echo.Remaining > 0f)
            {
                _pending[i] = echo;
                continue;
            }
            _pending[i] = _pending[^1];
            _pending.RemoveAt(_pending.Count - 1);
            Strike(echo);
        }
    }

    public void Clear() => _pending.Clear();

    private void Strike(in Echo echo)
    {
        Vector2 center = echo.AroundPlayer ? _player.GlobalPosition : echo.Center;
        bool fullCircle = echo.ArcAngle >= 359f;
        float dotThreshold = fullCircle ? -1f : Mathf.Cos(Mathf.DegToRad(echo.ArcAngle * 0.5f));
        float radiusSq = echo.Radius * echo.Radius;
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy)
                continue;
            Vector2 toEnemy = enemy.GlobalPosition - center;
            float distanceSq = toEnemy.LengthSquared();
            if (distanceSq > radiusSq || (!fullCircle && (distanceSq <= 0.0001f || echo.Direction.Dot(toEnemy.Normalized()) < dotThreshold)))
                continue;
            enemy.TakeDamage(echo.Damage, source: echo.Source);
        }
        if (echo.AroundPlayer)
            _player.AttackFx.PlayMelee(echo.Weapon?.Base, echo.Direction, echo.Radius, echo.ArcAngle);
        else
            _player.AttackFx.PlayObjectRing(center, PlayerAttackFx.FamilyOf(echo.Weapon?.Base), echo.Radius);
    }
}
