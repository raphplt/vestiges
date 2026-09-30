namespace Vestiges.Infrastructure;

/// <summary>
/// Un effet d'objet (plan 21 §4) : une stat du joueur qui monte avec le niveau de l'objet. Au niveau n, il vaut
/// 1 + base + per_level × n s'il est multiplicatif, base + per_level × n s'il est additif, plus un cran <c>step</c>
/// par niveau de <c>step_levels</c> atteint (Papier carbone : une copie de plus aux niveaux 25 et 50). Sans l'objet
/// (niveau 0), il est neutre.
/// </summary>
public sealed class PassiveEffectData
{
	public string Stat { get; init; }
	public string ModifierType { get; init; }
	public float Base { get; init; }
	public float PerLevel { get; init; }
	public float Step { get; init; }
	public int[] StepLevels { get; init; } = System.Array.Empty<int>();

	public bool Multiplicative => ModifierType == "multiplicative";

	public float ValueAt(int level)
	{
		float neutral = Multiplicative ? 1f : 0f;
		if (level <= 0)
			return neutral;
		float value = neutral + Base + PerLevel * level;
		foreach (int stepLevel in StepLevels)
			if (level >= stepLevel)
				value += Step;
		return value;
	}
}
