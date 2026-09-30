using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Voie d'ascension d'une arme au niveau maximal (plan 21 §3) : une transformation définitive, décrite par des leviers
/// communs (motif, stats, effet à l'impact, copies, drapeaux) plutôt que par du code propre à l'arme.
/// </summary>
public sealed class WeaponAscensionData
{
	/// <summary>Orbite qui s'éloigne et revient (Boîte à musique, Ronde).</summary>
	public const string OrbitPulseFlag = "orbit_pulse";

	public string Id { get; init; }
	public string Name { get; init; }
	public string Description { get; init; }
	/// <summary>Motif d'attaque remplacé ; null pour garder celui de l'arme.</summary>
	public string AttackPattern { get; init; }
	public Dictionary<string, float> StatMultipliers { get; init; } = new();
	public Dictionary<string, float> StatOverrides { get; init; } = new();
	/// <summary>Effet à l'impact remplacé ; null pour garder celui de l'arme.</summary>
	public WeaponOnHitEffect OnHitEffect { get; init; }
	/// <summary>Part des copies du Papier carbone que l'arme reçoit (0 : aucune, 2 : le double).</summary>
	public float CopiesMultiplier { get; init; } = 1f;
	public HashSet<string> Flags { get; init; } = new();
	public Dictionary<string, float> Parameters { get; init; } = new();

	/// <summary>Réglage d'un drapeau ; son absence est une erreur de données, pas une valeur par défaut silencieuse.</summary>
	public float Parameter(string name)
	{
		if (Parameters.TryGetValue(name, out float value))
			return value;
		GD.PushError($"[WeaponAscensionData] Paramètre {name} absent de la voie {Id}.");
		return 0f;
	}
}
