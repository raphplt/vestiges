using System;
using System.Collections.Generic;

namespace Vestiges.Progression;

/// <summary>
/// Effets de perk réellement branchés en run. Un perk n'est proposé que si son effet y figure : aucune carte
/// n'annonce une règle inactive (plan 05 §8). Chaque étape de B2 y ajoute les effets qu'elle livre, une fois vérifiés.
/// </summary>
public static class PerkSpecializationEffects
{
    private static readonly HashSet<string> Implemented = new(StringComparer.Ordinal)
    {
        SpecializationRuntime.OverhealReserveEffect,
        SpecializationRuntime.RallyEffect,
        SpecializationRuntime.OverflowEffect,
        SpecializationRuntime.PriorityTargetingEffect,
    };

    public static bool IsImplemented(string effect) => Implemented.Contains(effect);

    public static bool IsOfferable(string effect) => PreviewInactive || IsImplemented(effect);

#if TOOLS
    /// <summary>Bancs et captures seulement : propose aussi les effets non branchés, pour éprouver l'acquisition.</summary>
    public static bool PreviewInactive { get; set; }
#else
    public static bool PreviewInactive { get => false; set { } }
#endif
}
