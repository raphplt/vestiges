using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Progression;

/// <summary>
/// Convergence (plan 05 §3.1) : dans une recherche de cible déjà triée par distance, une arme qui vise place en tête
/// l'élite ou le Souverain à portée qu'elle suivait déjà, sinon le plus proche. Portée, obstacles et motif de l'arme
/// restent ceux de la recherche d'origine ; aucun dégât ni guidage ajouté.
/// </summary>
public sealed class PriorityTargeting
{
    private readonly Dictionary<WeaponInstance, EnemyLife> _tracked = new();
    private readonly PriorityTargetMarker _marker;

    public PriorityTargeting(PriorityTargetMarker marker)
    {
        _marker = marker;
    }

    /// <summary>Cible prioritaire effectivement suivie, ou null.</summary>
    public Enemy Current => _marker.Target;

    public void Promote<T>(WeaponInstance weapon, List<T> sortedByDistance, Func<T, Node2D> enemyOf)
    {
        if (weapon == null || !WeaponTraits.SearchesTarget(weapon))
            return;
        bool hasTracked = _tracked.TryGetValue(weapon, out EnemyLife tracked);
        int chosen = -1;
        for (int i = 0; i < sortedByDistance.Count; i++)
        {
            if (enemyOf(sortedByDistance[i]) is not Enemy { IsPriorityTarget: true, IsDying: false } enemy)
                continue;
            if (chosen < 0)
                chosen = i;
            if (hasTracked && enemy.Life == tracked)
            {
                chosen = i;
                break;
            }
        }

        if (chosen < 0)
        {
            _tracked.Remove(weapon);
            if (_marker.Target != null && !IsTracked(_marker.Target.Life))
                _marker.Release();
            return;
        }

        Enemy target = (Enemy)enemyOf(sortedByDistance[chosen]);
        _tracked[weapon] = target.Life;
        if (_marker.Target != target)
            _marker.Track(target);
        if (chosen == 0)
            return;
        T item = sortedByDistance[chosen];
        sortedByDistance.RemoveAt(chosen);
        sortedByDistance.Insert(0, item);
    }

    /// <summary>Une arme retirée cesse de suivre sa cible.</summary>
    public void Forget(IReadOnlyList<WeaponInstance> equipped)
    {
        List<WeaponInstance> removed = null;
        foreach (WeaponInstance weapon in _tracked.Keys)
        {
            bool kept = false;
            foreach (WeaponInstance owned in equipped)
                kept |= owned == weapon;
            if (!kept)
                (removed ??= new List<WeaponInstance>()).Add(weapon);
        }
        if (removed == null)
            return;
        foreach (WeaponInstance weapon in removed)
            _tracked.Remove(weapon);
        if (_marker.Target != null && !IsTracked(_marker.Target.Life))
            _marker.Release();
    }

    private bool IsTracked(EnemyLife life)
    {
        foreach (EnemyLife tracked in _tracked.Values)
            if (tracked == life)
                return true;
        return false;
    }
}
