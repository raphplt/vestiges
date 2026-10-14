namespace Vestiges.UI;

/// <summary>
/// Morceaux d'une carte d'amélioration (<see cref="UpgradeText.Parts"/>) : libellé de la stat qui bouge, gain court,
/// valeurs avant et après quand elles se lisent, autres stats regroupées, règle et ambiance. Chaîne vide = absent.
/// </summary>
public sealed class UpgradeParts
{
    public string Label { get; set; } = "";
    public string Gain { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string Others { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Flavor { get; set; } = "";
}
