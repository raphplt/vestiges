namespace Vestiges.Core;

public partial class Player
{
    internal Vestiges.Progression.RouteEndurance RouteEndurance { get; set; }
    /// <summary>Gain de Pression, distinct des bonus permanents et des malus de l'Effacement.</summary>
    internal float PressureDamageMultiplier { get; set; } = 1f;
}
