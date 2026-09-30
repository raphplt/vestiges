namespace Vestiges.Infrastructure;

/// <summary>
/// Un effet d'objet (plan 21 §4, plan 23 R3) : une stat du joueur qui monte d'un pas à chaque carte, multiplié par le
/// gain de la rareté de la carte. La valeur de l'effet est la somme de ses gains : 1 + somme s'il est multiplicatif,
/// la somme s'il est additif. Sans l'objet, il est neutre.
/// </summary>
public sealed class PassiveEffectData
{
	public string Stat { get; init; }
	public string ModifierType { get; init; }
	/// <summary>Gain d'une carte commune.</summary>
	public float Step { get; init; }

	public bool Multiplicative => ModifierType == "multiplicative";

	/// <summary>Valeur de l'effet sans aucun gain : 1 pour un facteur, 0 pour un ajout.</summary>
	public float Neutral => Multiplicative ? 1f : 0f;
}
