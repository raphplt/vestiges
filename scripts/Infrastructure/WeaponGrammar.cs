namespace Vestiges.Infrastructure;

/// <summary>Motif d'attaque d'une arme ou d'une voie d'ascension, lu une fois depuis <c>attack_pattern</c>.</summary>
public enum AttackPatternKind
{
	Linear,
	Arc,
	Burst,
	Chain,
	Circular,
	Homing,
	Orbital,
}

/// <summary>Famille d'une arme, lue une fois depuis <c>type</c> : la mêlée frappe sans projectile.</summary>
public enum WeaponCategory
{
	Melee,
	Ranged,
	Special,
}

/// <summary>
/// Frontière entre les clés JSON de la grammaire d'attaque et ses types (plan 26 Q5) : le combat, les propriétés et
/// l'interface comparent des types, jamais des chaînes. Une clé inconnue est refusée au chargement.
/// </summary>
public static class WeaponGrammar
{
	public const string PatternKeys = "linear, arc, burst, chain, circular, homing, orbital";
	public const string CategoryKeys = "melee, ranged, special";

	public static bool TryParsePattern(string key, out AttackPatternKind pattern)
	{
		(bool known, pattern) = key switch
		{
			"linear" => (true, AttackPatternKind.Linear),
			"arc" => (true, AttackPatternKind.Arc),
			"burst" => (true, AttackPatternKind.Burst),
			"chain" => (true, AttackPatternKind.Chain),
			"circular" => (true, AttackPatternKind.Circular),
			"homing" => (true, AttackPatternKind.Homing),
			"orbital" => (true, AttackPatternKind.Orbital),
			_ => (false, AttackPatternKind.Linear),
		};
		return known;
	}

	public static bool TryParseCategory(string key, out WeaponCategory category)
	{
		(bool known, category) = key switch
		{
			"melee" => (true, WeaponCategory.Melee),
			"ranged" => (true, WeaponCategory.Ranged),
			"special" => (true, WeaponCategory.Special),
			_ => (false, WeaponCategory.Ranged),
		};
		return known;
	}

	/// <summary>Clé de traduction du nom du motif (Collection).</summary>
	public static string LabelKey(AttackPatternKind pattern) => pattern switch
	{
		AttackPatternKind.Arc => "WEAPON_PATTERN_ARC",
		AttackPatternKind.Burst => "WEAPON_PATTERN_BURST",
		AttackPatternKind.Chain => "WEAPON_PATTERN_CHAIN",
		AttackPatternKind.Circular => "WEAPON_PATTERN_CIRCULAR",
		AttackPatternKind.Homing => "WEAPON_PATTERN_HOMING",
		AttackPatternKind.Orbital => "WEAPON_PATTERN_ORBITAL",
		_ => "WEAPON_PATTERN_LINEAR",
	};

	/// <summary>Clé de traduction de la famille (Collection, carte « Nouvelle » du level-up).</summary>
	public static string LabelKey(WeaponCategory category) => category switch
	{
		WeaponCategory.Melee => "LEVELUP_MELEE",
		WeaponCategory.Special => "WEAPON_CATEGORY_SPECIAL",
		_ => "LEVELUP_RANGED",
	};
}
