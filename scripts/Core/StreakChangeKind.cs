namespace Vestiges.Core;

/// <summary>Ce qui vient d'arriver à la Série (plan 52 §15).</summary>
public enum StreakChangeKind
{
    /// <summary>Une élimination, ou une chute dans le vide qui en vaut plusieurs.</summary>
    Gain,
    /// <summary>Un coup reçu l'a coupée en deux.</summary>
    Hit,
    /// <summary>Sa fenêtre s'est vidée : elle s'arrête et s'encaisse en Essence.</summary>
    CashOut,
    /// <summary>Éteinte : remise à zéro sans encaissement.</summary>
    Reset,
}
