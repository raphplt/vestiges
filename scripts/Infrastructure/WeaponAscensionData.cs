using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Voie d'ascension d'une arme au niveau maximal (plan 21 §3) : une transformation définitive, décrite par des leviers
/// communs (motif, stats, effet à l'impact, projectiles en plus, drapeaux) plutôt que par du code propre à l'arme.
/// </summary>
public sealed class WeaponAscensionData
{
	/// <summary>Orbite qui s'éloigne et revient (Boîte à musique, Ronde).</summary>
	public const string OrbitPulseFlag = "orbit_pulse";

	public string Id { get; init; }
	public string Name { get; init; }
	public string Description { get; init; }
	/// <summary>Motif d'attaque remplacé ; null pour garder celui de l'arme.</summary>
	public AttackPatternKind? AttackPattern { get; init; }
	public Dictionary<string, float> StatMultipliers { get; init; } = new();
	public Dictionary<string, float> StatOverrides { get; init; } = new();
	/// <summary>Effet à l'impact remplacé ; null pour garder celui de l'arme.</summary>
	public WeaponOnHitEffect OnHitEffect { get; init; }
	/// <summary>Part des projectiles en plus du Papier carbone que l'arme reçoit (0 : aucun, 2 : le double).</summary>
	public float BonusProjectileMultiplier { get; init; } = 1f;
	/// <summary>Réglages de l'effet spécial de l'arme remplacés par la voie (Suture : soin tous les 3 coups).</summary>
	public Dictionary<string, float> SpecialOverrides { get; init; } = new();
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
