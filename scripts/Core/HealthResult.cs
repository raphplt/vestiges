using Godot;

namespace Vestiges.Core;

public enum HealingKind { Normal, Regeneration, PerkRecovery, Revival }
public enum PlayerDamageKind { Combat, Erasure }

/// <summary>Les PV réellement rendus et l'excédent restent séparés ; une restitution ne doit pas se recharger.</summary>
public readonly record struct HealingResult(ulong PlayerId, HealingKind Kind, float Requested,
    float HpRestored, float Excess, bool Applied)
{
    public bool CanStoreExcess => Applied && Kind is HealingKind.Normal or HealingKind.Regeneration;
    public static HealingResult Resolve(ulong playerId, HealingKind kind, float amount, float hp, float maxHp)
    {
        float restored = Mathf.Clamp(amount, 0f, Mathf.Max(0f, maxHp - hp));
        return new(playerId, kind, amount, restored, Mathf.Max(0f, amount - restored), true);
    }
}

/// <summary>Fatal décrit le coup avant toute résurrection existante ; le Néant est toujours identifiable.</summary>
public readonly record struct PlayerDamageResult(ulong PlayerId, PlayerDamageKind Kind, float HpBefore,
    float HpLost, bool Fatal, bool ShieldAbsorbed, bool Applied)
{
    public bool CanRecover => Applied && HpLost > 0f && !Fatal && Kind == PlayerDamageKind.Combat;
}
