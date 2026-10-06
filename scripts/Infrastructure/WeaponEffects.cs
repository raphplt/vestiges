using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Effet à l'impact d'une arme ou d'une voie, lu une fois depuis <c>on_hit_effect.type</c>.</summary>
public enum OnHitEffectKind
{
	Bleed,
	Slow,
	Disorient,
	Freeze,
}

/// <summary>Effet spécial d'une arme, lu une fois depuis <c>special_effect.type</c>.</summary>
public enum SpecialEffectKind
{
	HealEveryNHits,
	InstantDisintegrate,
	DelayedEcho,
	GroundFire,
	LocalTimeSlow,
	RandomShape,
	SustainedCone,
}

/// <summary>Effet à l'impact (saignement, ralentissement, désorientation, gel) ; ses réglages sont validés au chargement.</summary>
public sealed class WeaponOnHitEffect
{
	public OnHitEffectKind Kind { get; init; }
	public float Value { get; init; }
	public float Damage { get; init; }
	public float Duration { get; init; }
}

/// <summary>
/// Effet spécial d'une arme (soin tous les N coups, écho, feu au sol…). Ses réglages sont résolus au chargement,
/// secours du contrat compris : <see cref="Get"/> ne lit jamais de valeur inventée par le code.
/// </summary>
public sealed class WeaponSpecialEffect
{
	public SpecialEffectKind Kind { get; init; }
	public IReadOnlyDictionary<string, float> Params { get; init; } = new Dictionary<string, float>();

	public float Get(string key)
	{
		if (Params.TryGetValue(key, out float value))
			return value;
		GD.PushError($"[WeaponSpecialEffect] Réglage {key} absent de l'effet {Kind}.");
		return 0f;
	}

	/// <summary>Copie aux réglages remplacés par une voie d'ascension.</summary>
	public WeaponSpecialEffect With(IReadOnlyDictionary<string, float> overrides)
	{
		Dictionary<string, float> values = new(Params);
		foreach ((string key, float value) in overrides)
			values[key] = value;
		return new WeaponSpecialEffect { Kind = Kind, Params = values };
	}
}

/// <summary>Noms des réglages d'effets spéciaux lus par le combat, tels que déclarés dans le contrat des armes.</summary>
public static class SpecialEffectParam
{
	public const string HitsPerHeal = "n";
	public const string HealAmount = "heal_amount";
	public const string EchoDelay = "echo_delay";
	public const string EchoDamagePercent = "echo_damage_percent";
	public const string EchoRadius = "echo_radius";
	public const string EchoCount = "echo_count";
	public const string GroundDamage = "ground_damage";
	public const string GroundDuration = "ground_duration";
	public const string GroundRadius = "ground_radius";
	public const string GroundBurnSeconds = "burn_seconds";
	public const string SlowRadius = "slow_radius";
	public const string SlowFactor = "slow_factor";
	public const string SlowDuration = "slow_duration";
	public const string FreezeSeconds = "freeze_seconds";
	public const string ShapeRadius = "shape_aoe_on_impact";
	public const string ShapeDamageRatio = "shape_damage_ratio";
	public const string ConeDuration = "duration";
	public const string ConeDamageRamp = "damage_ramp_per_sec";
}
