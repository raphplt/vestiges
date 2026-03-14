using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Instance d'arme en run : wrapper mutable autour de WeaponData immutable.
/// Niveau global (1 à MaxLevel), toutes les stats scalent automatiquement via GetStat().
/// </summary>
public class WeaponInstance
{
	public WeaponData Base { get; }
	private WeaponRarityData _rarityData;
	private int _level = 1;

	public string Id => Base.Id;
	public string Name => Base.Name;
	public string Description => Base.Description;
	public int Tier => Base.Tier;
	public string Type => Base.Type;
	public string DamageType => Base.DamageType;
	public string AttackPattern => Base.AttackPattern;
	public string Sprite => Base.Sprite;
	public string DefaultFor => Base.DefaultFor;
	public string Rarity => _rarityData?.Id ?? "common";
	public string RarityDisplayName => _rarityData?.DisplayName ?? "Commun";
	public Godot.Color RarityColor => _rarityData?.Color ?? Godot.Colors.White;

	public int Level => _level;
	public int MaxLevel => WeaponUpgradeDataLoader.GetWeaponMaxLevel();
	public bool CanLevelUp => _level < MaxLevel;

	public WeaponInstance(WeaponData baseData, string rarity = "common")
	{
		Base = baseData;
		_rarityData = WeaponRarityDataLoader.Get(rarity);
	}

	public void SetRarity(string rarity)
	{
		_rarityData = WeaponRarityDataLoader.Get(rarity);
	}

	public WeaponInstance CloneWithRarity(string rarity)
	{
		WeaponInstance clone = new(Base, rarity);
		clone._level = _level;
		return clone;
	}

	/// <summary>
	/// Monte le niveau global de l'arme. Toutes les stats augmentent via GetStat().
	/// </summary>
	public bool LevelUp()
	{
		if (!CanLevelUp)
			return false;
		_level++;
		return true;
	}

	/// <summary>
	/// Retourne la stat effective après application du scaling de niveau.
	/// Le scaling par stat est défini dans weapon_upgrades.json.
	/// Le niveau effectif pour chaque stat est min(_level - 1, config.MaxLevel).
	/// Mode multiplicatif : base × (1 + perLevel × effectiveLevel)
	/// Mode additif : base + perLevel × effectiveLevel
	/// </summary>
	public float GetStat(string key, float fallback)
	{
		float baseValue = Base.Stats.TryGetValue(key, out float v) ? v : fallback;

		int effectiveLevel = _level - 1; // Niveau 1 = pas de bonus
		if (effectiveLevel <= 0)
			return baseValue;

		WeaponUpgradeStatConfig config = WeaponUpgradeDataLoader.GetStatConfig(key);
		if (config == null)
			return ApplyRarityMultiplier(key, baseValue);

		// Vérifier si ce stat est applicable à ce type d'arme
		if (config.Types != null && config.Types.Count > 0
			&& !config.Types.Contains(Base.Type?.ToLower() ?? ""))
			return ApplyRarityMultiplier(key, baseValue);

		// Clamp au max level de ce stat spécifique
		int clampedLevel = effectiveLevel < config.MaxLevel ? effectiveLevel : config.MaxLevel;

		if (config.Mode == "additive")
			return ApplyRarityMultiplier(key, baseValue + config.PerLevel * clampedLevel);

		return ApplyRarityMultiplier(key, baseValue * (1f + config.PerLevel * clampedLevel));
	}

	public float GetComparisonScore()
	{
		float damage = GetStat("damage", 1f);
		float attackSpeed = GetStat("attack_speed", 1f);
		float range = Mathf.Max(24f, GetStat("range", 60f));
		float coverage = Mathf.Sqrt(range / 60f);
		return damage * attackSpeed * coverage;
	}

	public float GetDamageValue() => GetStat("damage", 1f);
	public float GetAttackSpeedValue() => GetStat("attack_speed", 1f);
	public float GetRangeValue() => GetStat("range", 60f);

	private float ApplyRarityMultiplier(string key, float value)
	{
		if (_rarityData == null)
			return value;

		float multiplier = key switch
		{
			"damage" => _rarityData.DamageMultiplier,
			"attack_speed" => _rarityData.AttackSpeedMultiplier,
			"range" => _rarityData.RangeMultiplier,
			"heal_on_hit" => _rarityData.HealMultiplier,
			_ => _rarityData.GlobalMultiplier
		};

		return value * multiplier;
	}
}
