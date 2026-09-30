namespace Vestiges.Infrastructure;

/// <summary>
/// Un effet d'objet (plan 21 §4) : une stat du joueur qui monte avec le niveau de l'objet. Au niveau n, il vaut
/// 1 + per_level × n s'il est multiplicatif, per_level × n s'il est additif.
/// </summary>
public sealed class PassiveEffectData
{
	public string Stat { get; init; }
	public string ModifierType { get; init; }
	public float PerLevel { get; init; }

	public bool Multiplicative => ModifierType == "multiplicative";

	public float ValueAt(int level) => Multiplicative ? 1f + PerLevel * level : PerLevel * level;
}
