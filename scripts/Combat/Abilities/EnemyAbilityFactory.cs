using Vestiges.Infrastructure;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Registre des capacités composées : une sorte, une classe. Les sortes viennent de <see cref="EnemyGrammar"/> ; une clé
/// inconnue est refusée au chargement de la fiche, jamais ici.
/// </summary>
public static class EnemyAbilityFactory
{
    public static IEnemyAbility Create(EnemyAbilityKind kind, Enemy owner) => kind switch
    {
        EnemyAbilityKind.OmenStrike => new OmenStrikeAbility(owner),
        EnemyAbilityKind.Pounce => new PounceAbility(owner, "PounceMarker"),
        EnemyAbilityKind.Charge => new PounceAbility(owner, "ChargeMarker"),
        EnemyAbilityKind.Burrow => new BurrowAbility(owner),
        EnemyAbilityKind.Cry => new CryAbility(owner),
        EnemyAbilityKind.AimedShot => new AimedShotAbility(owner),
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "capacité sans classe"),
    };
}
