namespace Vestiges.Combat.Abilities;

public static class EnemyAbilityFactory
{
    /// <summary>Crée la capacité correspondant à une clé du bloc "abilities", ou null si elle est inconnue.</summary>
    public static IEnemyAbility Create(string id, Enemy owner) => id switch
    {
        "omen_strike" => new OmenStrikeAbility(owner),
        "pounce" => new PounceAbility(owner),
        _ => null
    };
}
