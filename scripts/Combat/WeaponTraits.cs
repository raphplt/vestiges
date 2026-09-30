using System.Collections.Generic;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Traits d'une arme portée, déduits de ses données et de sa voie d'ascension, pour décider si une Réminiscence a prise
/// sur l'arsenal (plan 05, B1). Ils suivent le code d'attaque de <c>Player</c> : un nouveau motif ou effet doit être
/// reporté ici.
/// </summary>
public static class WeaponTraits
{
    /// <summary>L'arme choisit une cible ; l'onde circulaire, l'orbite et le cône orienté par le regard n'en cherchent pas.</summary>
    public static bool SearchesTarget(WeaponInstance weapon) =>
        weapon.AttackPattern?.ToLowerInvariant() is not ("circular" or "orbital") && weapon.Base.SpecialEffect?.Type != "sustained_cone";

    /// <summary>L'arme inflige des impacts directs attribués au joueur.</summary>
    public static bool DealsDirectHits(WeaponInstance weapon) => weapon.GetStat("damage", 0f) > 0f;

    /// <summary>L'arme applique elle-même un ralentissement ou une désorientation.</summary>
    public static bool AppliesNativeControl(WeaponInstance weapon) =>
        weapon.OnHitEffect?.Type is "slow" or "disorient" || weapon.Base.SpecialEffect?.Type == "local_time_slow";
}
