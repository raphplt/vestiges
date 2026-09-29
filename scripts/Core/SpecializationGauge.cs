namespace Vestiges.Core;

/// <summary>
/// État lisible d'un perk : <paramref name="Value"/> sur <paramref name="Max"/> (PV, dégâts), et pour une fenêtre
/// limitée, le temps restant sur sa durée. <paramref name="Key"/> distingue les instances d'un même effet (arme).
/// </summary>
public readonly record struct SpecializationGauge(ulong PlayerId, string Effect, string Key, float Value, float Max,
    float Remaining = 0f, float Duration = 0f);
