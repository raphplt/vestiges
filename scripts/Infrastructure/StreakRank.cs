using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Un rang de la Série (plan 52 §15) : nom (clé de traduction), seuil en éliminations, couleur, fenêtre sans
/// élimination avant l'encaissement, bonus multiplicatifs de vitesse et de dégâts (0,08 = +8 %), Chance ajoutée
/// (0,05 = +5 %) et taux d'encaissement en Essence (Série × taux).
/// </summary>
public readonly record struct StreakRank(
    string Name, int From, Color Color, float WindowSeconds, float Speed, float Damage, float Luck, float CashOut);
