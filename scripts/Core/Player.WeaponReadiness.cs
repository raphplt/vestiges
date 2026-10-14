using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Arme prête (DECISIONS §176) : une recharge qui s'achève sans cible à portée ne perd pas son tour. L'arme reste prête,
/// frappe dès qu'une créature entre à portée, et sa recharge repart de ce coup. Sans cela, une créature qui arrive au
/// contact attendait jusqu'à une recharge entière avant le premier coup.
/// </summary>
public partial class Player
{
    // Un bit par emplacement d'arme prêt, minuteur arrêté, en attente d'une cible.
    private int _readyWeaponSlots;

    /// <summary>La recharge est finie mais rien n'était à portée : le minuteur s'arrête et l'arme guette sa cible.</summary>
    private void HoldWeaponReady(int slotIndex)
    {
        _weaponTimers[slotIndex].Stop();
        _readyWeaponSlots |= 1 << slotIndex;
    }

    /// <summary>Chaque image, une arme prête frappe si une créature est à portée, puis reprend sa recharge.</summary>
    private void TickReadyWeapons()
    {
        if (_readyWeaponSlots == 0)
            return;
        for (int slot = 0; slot < _weaponSlots.Count && slot < _weaponTimers.Count; slot++)
        {
            int bit = 1 << slot;
            // Un minuteur suspendu (banc, scénario) garde la main sur ses coups.
            if ((_readyWeaponSlots & bit) == 0 || _weaponTimers[slot].Paused || !HasReadyTarget(_weaponSlots[slot]))
                continue;
            WeaponInstance previous = _equippedWeapon;
            _equippedWeapon = _weaponSlots[slot];
            AttackPatternKind pattern = _equippedWeapon.AttackPattern;
            if (PerformDiscreteAttack(_equippedWeapon.Category, pattern))
            {
                _readyWeaponSlots &= ~bit;
                _weaponTimers[slot].Start();
                RepeatFromEcho(_equippedWeapon.Category, pattern);
                _objectMilestones?.CountAttack(_equippedWeapon);
            }
            else
            {
                _equippedWeapon = previous;
            }
        }
    }

    /// <summary>
    /// Une créature attaquable à portée de l'arme, mesurée comme son attaque la mesure (ellipse du coup de mêlée, cercle
    /// de mêlée agrandi par la stat de taille), sans rien allouer : le test que l'arme prête refait chaque image.
    /// </summary>
    private bool HasReadyTarget(WeaponInstance weapon)
    {
        AttackPatternKind pattern = weapon.AttackPattern;
        bool melee = weapon.Category == WeaponCategory.Melee && pattern != AttackPatternKind.Chain;
        float range = GetEffectiveWeaponRange(weapon);
        if (melee && pattern == AttackPatternKind.Circular)
            range = ZoneScale(range);
        float squash = melee ? MeleeStrikeShape.Squash(pattern) : 1f;
        using CrowdQuery crowd = CrowdIndex.Near(AttackOrigin, range + Enemy.LargestPartRadius);
        foreach (Node2D node in crowd.Targets)
        {
            Enemy creature = node as Enemy;
            if (creature is { IsActive: false } or { IsDying: true } || creature is { IsBurrowed: true, Boss: not null })
                continue;
            Vector2 toTarget = node.GlobalPosition - AttackOrigin;
            if (new Vector2(toTarget.X, toTarget.Y * squash).Length() - (creature?.StrikeMargin ?? 0f) < range)
                return true;
        }
        return false;
    }

    /// <summary>Retire l'emplacement <paramref name="slotIndex"/> des armes prêtes ; les suivants descendent d'un rang.</summary>
    private void RemoveReadySlot(int slotIndex)
    {
        int below = _readyWeaponSlots & ((1 << slotIndex) - 1);
        int above = (_readyWeaponSlots >> (slotIndex + 1)) << slotIndex;
        _readyWeaponSlots = below | above;
    }
}
