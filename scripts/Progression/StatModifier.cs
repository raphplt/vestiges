using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Modification durable d'une stat du joueur (bénédiction, Oubli), appliquée comme un perk. Réversible :
/// <see cref="Inverse"/> la retire exactement.
/// </summary>
public readonly record struct StatModifier(string Stat, string ModifierType, float Value)
{
    public bool IsMultiplicative => ModifierType == "multiplicative";

    public StatModifier Inverse() => this with { Value = IsMultiplicative ? 1f / Value : -Value };

    public void ApplyTo(Player player) => player.ApplyPerkModifier(Stat, Value, ModifierType);

    /// <summary>« Dégâts  +10 % », « PV max  +20 ».</summary>
    public string Describe() => $"{StatCatalog.Name(Stat)}  {StatCatalog.FormatBonus(Stat, Value, IsMultiplicative)}";

    /// <summary>
    /// Modificateur d'un effet de données : <paramref name="amount"/> × <paramref name="gain"/>, autour de 1 si
    /// multiplicatif. Un <paramref name="amount"/> négatif donne un malus.
    /// </summary>
    public static StatModifier Scaled(string stat, string modifierType, float amount, float gain) =>
        new(stat, modifierType, modifierType == "multiplicative" ? 1f + amount * gain : amount * gain);
}
