using System.Collections.Generic;

namespace Vestiges.Infrastructure;

/// <summary>
/// Couples statistique / modificateur que <see cref="Core.Player.ApplyPerkModifier"/> applique et qu'un bonus permanent
/// peut donner (Campement, signatures). Sous-ensemble volontaire : le bouclier et le Péril, que la méthode applique aussi,
/// n'en sont pas. Le contrat des objets ne convient pas : il n'admet que l'additif pour les PV, alors que le Campement
/// les donne en pourcentage.
/// </summary>
public static class PlayerModifiers
{
    private static readonly HashSet<(string Stat, string Modifier)> Pairs = new()
    {
        ("max_hp", "additive"), ("max_hp", "multiplicative"), ("damage", "multiplicative"), ("speed", "multiplicative"),
        ("attack_speed", "multiplicative"), ("aoe_radius", "multiplicative"), ("attack_range", "multiplicative"),
        ("regen_rate", "additive"), ("lifesteal", "additive"), ("armor", "additive"), ("crit_chance", "additive"),
        ("crit_multiplier", "additive"), ("projectile_pierce", "additive"), ("xp_magnet_radius", "multiplicative"),
        ("luck", "additive"), ("xp_gain", "multiplicative"), ("dash_recharge", "multiplicative"),
        ("projectile_bonus", "additive"), ("status_duration", "multiplicative"),
    };

    public static bool Applies(string stat, string modifier) => Pairs.Contains((stat, modifier));
}
