using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Palier d'un objet (plan 21 §4) : au niveau <see cref="Level"/>, l'objet gagne un effet propre, nommé par
/// <see cref="Effect"/> et réglé par ses paramètres. <see cref="Text"/> est ce que la carte et la pause en disent.
/// </summary>
public sealed class ObjectMilestoneData
{
	public int Level { get; init; }
	public string Effect { get; init; }
	public string Text { get; init; }
	public Dictionary<string, float> Parameters { get; init; } = new();

	/// <summary>Coefficient du palier ; son absence est une erreur de données, pas une valeur par défaut silencieuse.</summary>
	public float Parameter(string name)
	{
		if (Parameters.TryGetValue(name, out float value))
			return value;
		GD.PushError($"[ObjectMilestoneData] Paramètre {name} absent du palier {Effect}.");
		return 0f;
	}
}
