using System.Collections.Generic;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>Gain d'une amélioration sur une stat d'arme : fraction de la base (multiplicative) ou valeur (additive, paliers).</summary>
public readonly record struct StatGain(string Stat, float Amount, bool Milestone);

/// <summary>
/// Arme portée en run (plan 17, décision 4.3 : pas de rareté d'arme). Un niveau, et les gains accumulés de chaque
/// amélioration : une arme grandit par les stats qu'elle déclare (growth), chacune à son rythme.
/// </summary>
public class WeaponInstance
{
	public WeaponData Base { get; }
	private readonly Dictionary<string, float> _bonuses = new();
	private int _level = 1;

	public string Id => Base.Id;
	public string Name => Base.Name;
	public string Description => Base.Description;
	public int Tier => Base.Tier;
	public string Type => Base.Type;
	public string DamageType => Base.DamageType;
	/// <summary>Motif d'attaque : celui de la voie d'ascension choisie, sinon celui de l'arme.</summary>
	public string AttackPattern => Ascension?.AttackPattern ?? Base.AttackPattern;
	/// <summary>Effet à l'impact : celui de la voie d'ascension, sinon celui de l'arme.</summary>
	public WeaponOnHitEffect OnHitEffect => Ascension?.OnHitEffect ?? Base.OnHitEffect;
	/// <summary>Voie choisie au niveau maximal (plan 21 §3) ; null avant l'ascension.</summary>
	public WeaponAscensionData Ascension { get; private set; }
	/// <summary>L'arme est au niveau maximal, n'a pas encore choisi et a deux voies à proposer.</summary>
	public bool CanAscend => !CanLevelUp && Ascension == null && Base.Ascensions.Count == 2;
	/// <summary>Part des projectiles en plus du Papier carbone que reçoit l'arme (Volée : double, Transpercer : aucun).</summary>
	public float BonusProjectileMultiplier => Ascension?.BonusProjectileMultiplier ?? 1f;
	public string Sprite => Base.Sprite;
	public string DefaultFor => Base.DefaultFor;

	public int Level => _level;
	public int MaxLevel => WeaponUpgradeDataLoader.GetWeaponMaxLevel();
	public bool CanLevelUp => _level < MaxLevel;

	public WeaponInstance(WeaponData baseData)
	{
		Base = baseData;
	}

	/// <summary>Applique une amélioration : ses gains s'ajoutent, l'arme monte d'un niveau.</summary>
	public bool ApplyUpgrade(IReadOnlyList<StatGain> gains)
	{
		if (!CanLevelUp)
			return false;
		foreach (StatGain gain in gains)
			_bonuses[gain.Stat] = _bonuses.GetValueOrDefault(gain.Stat) + gain.Amount;
		_level++;
		return true;
	}

	/// <summary>Choisit une voie d'ascension, pour de bon ; faux si l'arme ne peut pas encore ou plus choisir.</summary>
	public bool Ascend(string ascensionId)
	{
		if (!CanAscend)
			return false;
		foreach (WeaponAscensionData ascension in Base.Ascensions)
		{
			if (ascension.Id != ascensionId)
				continue;
			Ascension = ascension;
			return true;
		}
		return false;
	}

	public bool HasFlag(string flag) => Ascension != null && Ascension.Flags.Contains(flag);

	/// <summary>Copie de l'arme avec une amélioration appliquée : sert à montrer « avant → après ».</summary>
	public WeaponInstance PreviewWith(IReadOnlyList<StatGain> gains)
	{
		WeaponInstance preview = new(Base) { _level = _level };
		foreach ((string stat, float bonus) in _bonuses)
			preview._bonuses[stat] = bonus;
		preview.ApplyUpgrade(gains);
		return preview;
	}

	/// <summary>
	/// Stat de l'arme : base des données, plus les gains accumulés (en pourcentage de la base, ou ajoutés), puis la voie
	/// d'ascension (valeur fixée, ou multipliée).
	/// </summary>
	public float GetStat(string key, float fallback)
	{
		if (Ascension != null && Ascension.StatOverrides.TryGetValue(key, out float overridden))
			return overridden;
		float value = LeveledStat(key, fallback);
		return Ascension != null && Ascension.StatMultipliers.TryGetValue(key, out float multiplier) ? value * multiplier : value;
	}

	/// <summary>Stat avant l'ascension : base des données et gains de niveau, plafonnée selon sa configuration.</summary>
	private float LeveledStat(string key, float fallback)
	{
		float baseValue = Base.Stats.TryGetValue(key, out float v) ? v : fallback;
		if (!_bonuses.TryGetValue(key, out float bonus))
			return baseValue;

		WeaponUpgradeStatConfig config = WeaponUpgradeDataLoader.GetStatConfig(key);
		float value = config != null && config.Additive ? baseValue + bonus : baseValue * (1f + bonus);
		return config != null && config.Max > 0f && value > config.Max ? config.Max : value;
	}

	public float GetComparisonScore()
	{
		float damage = GetStat("damage", 1f);
		float attackSpeed = GetStat("attack_speed", 1f);
		float range = Godot.Mathf.Max(24f, GetStat("range", 60f));
		return damage * attackSpeed * Godot.Mathf.Sqrt(range / 60f);
	}

	public float GetDamageValue() => GetStat("damage", 1f);
	public float GetAttackSpeedValue() => GetStat("attack_speed", 1f);
	public float GetRangeValue() => GetStat("range", 60f);
}
