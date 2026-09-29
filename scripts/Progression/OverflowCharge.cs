using System.Collections.Generic;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Débordement (plan 05 §3.2), côté joueur : l'excédent natif d'un coup direct fatal charge la réserve de l'arme,
/// plafonnée à la référence de l'attaque qui charge. Les reports, secondaires, DOT et exécutions ne chargent rien.
/// </summary>
public sealed class OverflowCharge
{
    private readonly ulong _ownerId;
    private readonly float _fraction;
    private readonly float _duration;
    private readonly float _capRatio;
    private readonly List<WeaponInstance> _ready = new();

    public OverflowCharge(ulong ownerId, PerkSpecializationData perk)
    {
        _ownerId = ownerId;
        _fraction = SpecializationRuntime.Parameter(perk, "carry_fraction");
        _duration = SpecializationRuntime.Parameter(perk, "duration_seconds");
        _capRatio = SpecializationRuntime.Parameter(perk, "attack_damage_cap_ratio");
    }

    /// <summary>Vrai si la réserve de l'arme de cet impact a changé (consommée ou chargée).</summary>
    public bool Resolve(in DamageResult result)
    {
        AttackContext source = result.Source;
        if (source.OwnerId != _ownerId || !source.IsDirectWeapon)
            return false;
        bool changed = result.CarriedDamage > 0f && _ready.Remove(source.Weapon);
        float overkill = result.NativeOverkill;
        if (overkill <= 0f)
            return changed;
        OverflowLedger.Charge(_ownerId, source.Weapon, source.LaunchId, overkill * _fraction, source.ReferenceDamage * _capRatio, _duration);
        if (!_ready.Contains(source.Weapon))
            _ready.Add(source.Weapon);
        return true;
    }

    public bool HasReserves => _ready.Count > 0;

    public float Amount(WeaponInstance weapon) => OverflowLedger.Amount(_ownerId, weapon);

    /// <summary>Fait vieillir les réserves ; les armes dont la réserve a expiré sont ajoutées à <paramref name="expired"/>.</summary>
    public void Advance(float delta, List<WeaponInstance> expired)
    {
        OverflowLedger.Advance(_ownerId, delta);
        Prune(expired);
    }

    public void DropMissing(IReadOnlyList<WeaponInstance> equipped, List<WeaponInstance> expired)
    {
        OverflowLedger.DropMissing(_ownerId, equipped);
        Prune(expired);
    }

    public void Clear() => OverflowLedger.Clear(_ownerId);

    private void Prune(List<WeaponInstance> expired)
    {
        for (int i = _ready.Count - 1; i >= 0; i--)
        {
            if (OverflowLedger.Amount(_ownerId, _ready[i]) > 0f)
                continue;
            expired.Add(_ready[i]);
            _ready.RemoveAt(i);
        }
    }
}
