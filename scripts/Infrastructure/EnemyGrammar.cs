using System.Collections.Generic;
using System.Linq;

namespace Vestiges.Infrastructure;

/// <summary>Attaque de base d'une créature : contact, projectile, ou boss piloté par son propre module.</summary>
public enum EnemyCombatType
{
	Melee,
	Ranged,
	Boss,
}

/// <summary>Comportement qui s'ajoute à l'attaque de base.</summary>
public enum EnemyBehavior
{
	Default,
	Pack,
	Sentinel,
	Weaver,
	/// <summary>Partie d'un boss (battant, main) : immobile, sans attaque ni récompense, ses PV vont au boss (plan 07 B1).</summary>
	BossPart,
}

public enum EnemyTier
{
	Normal,
	Elite,
	Miniboss,
	Boss,
}

/// <summary>Capacités composées du bloc « abilities » ; chacune a sa classe dans <c>Combat/Abilities</c>.</summary>
public enum EnemyAbilityKind
{
	OmenStrike,
	Pounce,
	Charge,
	Burrow,
	Cry,
	AimedShot,
}

/// <summary>
/// Clés JSON des fiches de créatures ↔ types du domaine (plan 26 Q6c). Les fiches gardent leurs clés lisibles ; le
/// lecteur les convertit une fois et refuse une clé inconnue.
/// </summary>
public static class EnemyGrammar
{
	/// <summary>Boss final, entité unique : seule créature nommée par le code.</summary>
	public const string FinalBossId = "indicible";
	/// <summary>Fiche commune des parties de boss : le boss leur donne PV et rayon de touche.</summary>
	public const string BossPartId = "boss_part";
	/// <summary>Cause d'un coup porté par un boss fait de parties : préfixe suivi de la clé de traduction de son nom.</summary>
	public const string BossCausePrefix = "boss:";

	private static readonly (string Key, EnemyCombatType Value)[] CombatTypes =
	{
		("melee", EnemyCombatType.Melee), ("ranged", EnemyCombatType.Ranged), ("boss", EnemyCombatType.Boss),
	};

	private static readonly (string Key, EnemyBehavior Value)[] Behaviors =
	{
		("default", EnemyBehavior.Default), ("pack", EnemyBehavior.Pack), ("sentinel", EnemyBehavior.Sentinel),
		("weaver", EnemyBehavior.Weaver),
		("boss_part", EnemyBehavior.BossPart),
	};

	private static readonly (string Key, EnemyTier Value)[] Tiers =
	{
		("normal", EnemyTier.Normal), ("elite", EnemyTier.Elite), ("miniboss", EnemyTier.Miniboss), ("boss", EnemyTier.Boss),
	};

	private static readonly (string Key, EnemyAbilityKind Value)[] Abilities =
	{
		("omen_strike", EnemyAbilityKind.OmenStrike), ("pounce", EnemyAbilityKind.Pounce), ("charge", EnemyAbilityKind.Charge),
		("burrow", EnemyAbilityKind.Burrow), ("cry", EnemyAbilityKind.Cry), ("aimed_shot", EnemyAbilityKind.AimedShot),
	};

	public static bool TryParseCombatType(string key, out EnemyCombatType value) => TryParse(CombatTypes, key, out value);
	public static bool TryParseBehavior(string key, out EnemyBehavior value) => TryParse(Behaviors, key, out value);
	public static bool TryParseTier(string key, out EnemyTier value) => TryParse(Tiers, key, out value);
	public static bool TryParseAbility(string key, out EnemyAbilityKind value) => TryParse(Abilities, key, out value);

	public static string CombatTypeKeys => Keys(CombatTypes);
	public static string BehaviorKeys => Keys(Behaviors);
	public static string TierKeys => Keys(Tiers);
	public static string AbilityKeys => Keys(Abilities);

	public static IEnumerable<string> AllAbilityKeys => Abilities.Select(entry => entry.Key);

	public static string Key(EnemyAbilityKind kind)
	{
		foreach ((string key, EnemyAbilityKind value) in Abilities)
		{
			if (value == kind)
				return key;
		}
		return kind.ToString();
	}

	private static bool TryParse<T>((string Key, T Value)[] table, string key, out T value)
	{
		foreach ((string entryKey, T entryValue) in table)
		{
			if (entryKey == key)
			{
				value = entryValue;
				return true;
			}
		}
		value = default;
		return false;
	}

	private static string Keys<T>((string Key, T Value)[] table) => string.Join(", ", table.Select(entry => entry.Key));
}
