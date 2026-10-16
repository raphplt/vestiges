namespace Vestiges.Infrastructure;

/// <summary>Un palier d'Élan (plan 52) : seuil et bonus multiplicatifs (0,08 = +8 %).</summary>
public readonly record struct ElanTier(float From, float Speed, float Damage);
