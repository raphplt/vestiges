using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Quelles propriétés de la grammaire commune (plan 21 §7) agissent sur une arme, selon son motif : une seule règle,
/// lue par les cartes d'objets (« Pour : … ») et, plus tard, par les affinités des personnages.
/// </summary>
public static class WeaponProperties
{
    /// <param name="objectStatuses">Le joueur porte un objet qui pose des statuts à l'impact (Allumette, Glaçon) : la
    /// Durée vaut alors pour toute arme qui frappe.</param>
    public static bool Concerns(WeaponData weapon, string property, bool objectStatuses = false)
    {
        if (weapon == null || property == null)
            return false;
        string pattern = weapon.AttackPattern?.ToLowerInvariant();
        string special = weapon.SpecialEffect?.Type;
        bool continuous = special == "sustained_cone";
        bool strikes = pattern != "orbital" && !continuous;
        return property switch
        {
            "force" or "range" => true,
            "frequency" or "precision" => strikes,
            "count" => strikes && pattern != "chain",
            "size" => continuous || pattern == "orbital"
                || (weapon.Type?.ToLowerInvariant() == "melee" && pattern is "arc" or "circular")
                || special is "delayed_echo" or "random_shape" or "local_time_slow" or "ground_fire",
            "duration" => weapon.OnHitEffect != null || special is "ground_fire" or "local_time_slow" || objectStatuses,
            _ => false,
        };
    }
}
