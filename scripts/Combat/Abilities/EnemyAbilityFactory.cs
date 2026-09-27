namespace Vestiges.Combat.Abilities;

public static class EnemyAbilityFactory
{
    /// <summary>Crée la capacité correspondant à une clé du bloc "abilities", ou null si elle est inconnue.</summary>
    public static IEnemyAbility Create(string id, Enemy owner) => id switch
    {
        "omen_strike" => new OmenStrikeAbility(owner),
        "pounce" => new PounceAbility(owner, "PounceMarker"),
        "charge" => new PounceAbility(owner, "ChargeMarker"),
        "burrow" => new BurrowAbility(owner),
        "cry" => new CryAbility(owner),
        "aimed_shot" => new AimedShotAbility(owner),
        _ => null
    };
}
