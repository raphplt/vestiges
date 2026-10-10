using System;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Rémanence (Double de l'Éveillée, plan 06 P3b) : armée par le module du passif après une mobilité, elle répète le
/// prochain tir de l'arme qui se déclenche, depuis son point et à une part des dégâts. Les tirs seulement : la mêlée et
/// la chaîne se dessinent autour du corps.
/// </summary>
public partial class Player
{
    private Vector2? _attackOriginOverride;
    private float _attackDamageScale = 1f;
    private bool _isEchoAttack;
    private Vector2 _echoOrigin;
    private float _echoDamageRatio;

    /// <summary>Une rémanence attend le prochain tir.</summary>
    public bool HasAttackEcho { get; private set; }

    /// <summary>La rémanence a répété un tir, depuis ce point.</summary>
    public event Action<Vector2> AttackEchoed;

    /// <summary>Point d'où partent les tirs : le joueur, ou la rémanence pendant le tir qu'elle répète.</summary>
    private Vector2 AttackOrigin => _attackOriginOverride ?? GlobalPosition;

    public void ArmAttackEcho(Vector2 origin, float damageRatio)
    {
        _echoOrigin = origin;
        _echoDamageRatio = damageRatio;
        HasAttackEcho = true;
    }

    public void DisarmAttackEcho() => HasAttackEcho = false;

    private void RepeatFromEcho(WeaponCategory category, AttackPatternKind pattern)
    {
        if (!HasAttackEcho || category == WeaponCategory.Melee || pattern == AttackPatternKind.Chain)
            return;
        _attackOriginOverride = _echoOrigin;
        _attackDamageScale = _echoDamageRatio;
        _isEchoAttack = true;
        bool fired;
        try
        {
            fired = PerformRangedAttack(pattern);
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
        HasAttackEcho = false;
        AttackEchoed?.Invoke(_echoOrigin);
    }
}
