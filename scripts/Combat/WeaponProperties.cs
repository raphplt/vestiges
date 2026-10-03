using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Quelles propriétés de la grammaire commune (plan 21 §7) agissent sur une arme portée, selon son motif et son effet
/// à l'impact après une éventuelle ascension : une seule règle, que liront les affinités des personnages (plan 21 §6).
/// Le jeu n'explicite pas les synergies au joueur (DECISIONS §39).
/// </summary>
public static class WeaponProperties
{
    /// <param name="objectStatuses">Le joueur porte un objet qui pose des statuts à l'impact (Allumette, Glaçon) : la
    /// Durée vaut alors pour toute arme qui frappe.</param>
    public static bool Concerns(WeaponInstance weapon, string property, bool objectStatuses = false)
    {
        if (weapon == null || property == null)
            return false;
        AttackPatternKind pattern = weapon.AttackPattern;
        string special = weapon.SpecialEffect?.Type;
        bool continuous = special == "sustained_cone";
        bool strikes = pattern != AttackPatternKind.Orbital && !continuous;
        return property switch
        {
            "force" or "range" => true,
            "frequency" or "precision" => strikes,
            "count" => strikes && pattern != AttackPatternKind.Chain,
            "size" => continuous || pattern == AttackPatternKind.Orbital
                || (weapon.Category == WeaponCategory.Melee && pattern is AttackPatternKind.Arc or AttackPatternKind.Circular)
                || special is "delayed_echo" or "random_shape" or "local_time_slow" or "ground_fire",
            "duration" => weapon.OnHitEffect != null || special is "ground_fire" or "local_time_slow" || objectStatuses,
            _ => false,
        };
    }
}
