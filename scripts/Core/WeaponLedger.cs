using System.Collections.Generic;

namespace Vestiges.Core;

/// <summary>
/// Ce que chaque arme a fait pendant la run : dégâts infligés (pause, plan 17 lot 1C) et créatures achevées
/// (bilan, plan 02 lot D M2). Les dégâts secondaires (brûlure, explosion) n'y sont pas attribués.
/// </summary>
public sealed class WeaponLedger
{
    private readonly Dictionary<string, float> _damage = new();
    private readonly Dictionary<string, int> _kills = new();

    public void AddDamage(string weaponId, float damage) =>
        _damage[weaponId] = _damage.GetValueOrDefault(weaponId) + damage;

    public void AddKill(string weaponId) => _kills[weaponId] = _kills.GetValueOrDefault(weaponId) + 1;

    public float DamageOf(string weaponId) => _damage.GetValueOrDefault(weaponId);

    public int KillsOf(string weaponId) => _kills.GetValueOrDefault(weaponId);
}
