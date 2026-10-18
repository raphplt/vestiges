namespace Vestiges.Infrastructure;

/// <summary>
/// Un palier de la Série (plan 52 §13) : seuil en éliminations, bonus multiplicatifs de vitesse et de dégâts
/// (0,08 = +8 %) et Chance ajoutée (0,05 = +5 %).
/// </summary>
public readonly record struct ElanTier(int From, float Speed, float Damage, float Luck);
