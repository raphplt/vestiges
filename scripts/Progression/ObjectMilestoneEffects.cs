using System;
using System.Collections.Generic;

namespace Vestiges.Progression;

/// <summary>
/// Paliers d'objets réellement branchés en run (plan 21 §4). Une carte ou la pause n'annoncent un palier que s'il
/// figure ici, et le joueur ne l'active qu'à cette condition : aucun texte ne promet un effet absent.
/// </summary>
public static class ObjectMilestoneEffects
{
    private static readonly HashSet<string> Implemented = new(StringComparer.Ordinal)
    {
        ObjectMilestones.SpreadTargetsEffect,
        ObjectMilestones.PierceDamageRampEffect,
        ObjectMilestones.StatusRenewEffect,
        ObjectMilestones.RepeatAttackEffect,
        ObjectMilestones.ZoneEchoEffect,
        ObjectMilestones.RangeEndBurstEffect,
        ObjectMilestones.FullHpCritEffect,
        ObjectMilestones.IgnoreSmallHitsEffect,
        ObjectMilestones.WoundRegenEffect,
        ObjectMilestones.DashArmorEffect,
        ObjectMilestones.ShieldBreakWaveEffect,
        ObjectMilestones.DashDistanceEffect,
        ObjectMilestones.OrbHealEffect,
        ObjectMilestones.LevelEssenceEffect,
        ObjectMilestones.LevelRerollEffect,
        ObjectTriggers.BurnSpreadEffect,
        ObjectTriggers.DoubleSlowFreezeEffect,
        ObjectTriggers.BurnSlowsEffect,
        ObjectTriggers.SlowKillExtendsEffect,
        ObjectTriggers.DoubleExplosionEffect,
        ObjectTriggers.EliteKillHealEffect,
        ObjectTriggers.DoubleStrideEffect,
        ObjectTriggers.CascadeInvulnerabilityEffect,
    };

    public static bool IsImplemented(string effect) => Implemented.Contains(effect);
}
