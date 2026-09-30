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
        ObjectMilestones.StatStepEffect,
        ObjectMilestones.StatusRenewEffect,
    };

    public static bool IsImplemented(string effect) => Implemented.Contains(effect);
}
