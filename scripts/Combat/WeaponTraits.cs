using System.Collections.Generic;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Traits d'une arme déduits de ses données, pour décider si un perk a prise sur l'arsenal (plan 05, B1).
/// Ils suivent le code d'attaque de <c>Player</c> : un nouveau motif ou effet doit être reporté ici.
/// </summary>
public static class WeaponTraits
{
    /// <summary>L'arme choisit une cible ; l'onde circulaire, l'orbite et le cône orienté par le regard n'en cherchent pas.</summary>
    public static bool SearchesTarget(WeaponData weapon) =>
        weapon.AttackPattern?.ToLowerInvariant() is not ("circular" or "orbital") && weapon.SpecialEffect?.Type != "sustained_cone";

    /// <summary>L'arme inflige des impacts directs attribués au joueur.</summary>
    public static bool DealsDirectHits(WeaponData weapon) => weapon.Stats.GetValueOrDefault("damage") > 0f;

    /// <summary>L'arme applique elle-même un ralentissement ou une désorientation.</summary>
    public static bool AppliesNativeControl(WeaponData weapon) =>
        weapon.OnHitEffect?.Type is "slow" or "disorient" || weapon.SpecialEffect?.Type == "local_time_slow";
}
