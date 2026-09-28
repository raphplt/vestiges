using Godot;

namespace Vestiges.Combat;

public enum DamageKind { Unknown, DirectWeapon, SecondaryWeapon, DamageOverTime, Passive, Execution, EnemyExplosion }

/// <summary>
/// Origine conservée même si l'arme quitte l'inventaire. LaunchId regroupe l'émission entière ; ReferenceDamage
/// fige la contribution native de l'impact qui charge (critique inclus), avant défense. Un multi-impact garde
/// le même LaunchId et peut porter plusieurs références : le consommateur agrège sous un plafond unique.
/// </summary>
public readonly record struct AttackContext(ulong OwnerId, WeaponInstance Weapon, ulong LaunchId,
    DamageKind Kind, float ReferenceDamage = 0f)
{
    public bool IsPlayerOwned => OwnerId != 0;
    public bool IsDirectWeapon => IsPlayerOwned && Weapon != null && Kind == DamageKind.DirectWeapon;
    public AttackContext As(DamageKind kind) => this with { Kind = kind };
}

/// <summary>Le numéro de vie empêche un nœud recyclé de devenir accidentellement une ancienne cible.</summary>
public readonly record struct EnemyLife(ulong InstanceId, ulong Generation);

/// <summary>Dommages résolus après défense ; le report reste distinct du natif et ne peut engendrer un surkill natif.</summary>
public readonly record struct DamageResult(EnemyLife Target, AttackContext Source, float HpBefore,
    float NativeDamage, float CarriedDamage, float HpLost, bool Fatal, bool Applied)
{
    public float NativeOverkill => Applied && Fatal ? Mathf.Max(0f, NativeDamage - HpBefore) : 0f;

    public static DamageResult Resolve(EnemyLife target, AttackContext source, float hp,
        float native, float carried, float defenseMultiplier = 1f)
    {
        float resolvedNative = Mathf.Max(0f, native * defenseMultiplier);
        float resolvedCarried = Mathf.Max(0f, carried * defenseMultiplier);
        float total = resolvedNative + resolvedCarried;
        return new(target, source, hp, resolvedNative, resolvedCarried, Mathf.Min(hp, total), total >= hp, true);
    }
}

public enum ControlOrigin { Unknown, NativeWeapon, Propagated }

/// <summary>La provenance est propre à chaque contrôle, jamais déduite de la source du dernier coup.</summary>
public readonly record struct ControlState(float Strength, float Remaining, AttackContext Source, ControlOrigin Origin)
{
    public bool CanPropagate(ulong ownerId) => Remaining > 0f && Origin == ControlOrigin.NativeWeapon
        && Source.OwnerId == ownerId && Source.Weapon != null;
}

public readonly record struct EnemyKillResult(EnemyLife Target, string EnemyId, Vector2 Position,
    DamageResult Damage, ControlState Slow, ControlState Disorientation);
