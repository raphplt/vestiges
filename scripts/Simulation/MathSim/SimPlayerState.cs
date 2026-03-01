using System;
using System.Collections.Generic;
using System.Linq;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation.MathSim;

/// <summary>
/// Arme simulée — miroir de WeaponInstance.
/// Gère les stats de base + scaling par weapon_upgrades.json.
/// </summary>
public class SimWeapon
{
	public string Id;
	public string Name;
	public string Type;
	public Dictionary<string, float> BaseStats;
	public int Level = 1;

	public int MaxLevel => WeaponUpgradeDataLoader.GetWeaponMaxLevel();
	public bool CanLevelUp => Level < MaxLevel;

	public SimWeapon(WeaponData data)
	{
		Id = data.Id;
		Name = data.Name;
		Type = data.Type;
		BaseStats = new Dictionary<string, float>(data.Stats);
	}

	public bool LevelUp()
	{
		if (!CanLevelUp) return false;
		Level++;
		return true;
	}

	public float GetStat(string key, float fallback)
	{
		float baseValue = BaseStats.TryGetValue(key, out float v) ? v : fallback;
		int effectiveLevel = Level - 1;
		if (effectiveLevel <= 0) return baseValue;

		WeaponUpgradeStatConfig config = WeaponUpgradeDataLoader.GetStatConfig(key);
		if (config == null) return baseValue;

		if (config.Types != null && config.Types.Count > 0
			&& !config.Types.Contains(Type?.ToLower() ?? ""))
			return baseValue;

		int clampedLevel = Math.Min(effectiveLevel, config.MaxLevel);

		if (config.Mode == "additive")
			return baseValue + config.PerLevel * clampedLevel;

		return baseValue * (1f + config.PerLevel * clampedLevel);
	}
}

/// <summary>
/// Souvenir Passif simulé — miroir de ActivePassiveSouvenir.
/// </summary>
public class SimPassive
{
	public PassiveSouvenirData Data;
	public int Level = 1;

	public string Id => Data.Id;
	public bool IsMaxLevel => Level >= Data.MaxLevel;

	public float GetCurrentModifier()
	{
		if (Data.PerLevel == null || Data.PerLevel.Length == 0) return 0f;
		int idx = Math.Clamp(Level - 1, 0, Data.PerLevel.Length - 1);
		return Data.PerLevel[idx];
	}

	public bool Upgrade()
	{
		if (IsMaxLevel) return false;
		Level++;
		return true;
	}
}

/// <summary>
/// État joueur pur C# sans dépendance Node.
/// Miroir des stats combat de Player.cs pour la simulation mathématique.
/// Supporte multi-armes (4 slots) et Souvenirs Passifs (4 slots).
/// </summary>
public class SimPlayerState
{
	public const int MaxWeaponSlots = 4;
	public const int MaxPassiveSlots = 4;

	// Base stats (from CharacterData)
	public string CharacterId;
	public string CharacterName;
	public float MaxHp;
	public float CurrentHp;
	public float BaseDamage;
	public float BaseAttackSpeed;
	public float BaseAttackRange;
	public float BaseRegenRate;
	public float ScoreMultiplier = 1f;

	// Weapon & passive slots
	public List<SimWeapon> Weapons = new();
	public List<SimPassive> Passives = new();

	// Global stat modifiers (applied by passives, world perks, character passive)
	public float DamageMultiplier = 1f;
	public float SpeedMultiplier = 1f;
	public float AttackSpeedMultiplier = 1f;
	public float BonusMaxHp;
	public int ExtraProjectiles;
	public float AoeMultiplier = 1f;
	public float AttackRangeMultiplier = 1f;
	public float BonusRegenRate;
	public float Armor;
	public float CritChance;
	public float CritMultiplier = 2f;
	public int ProjectilePierce;

	// Complex perk effects (from world perks)
	public float VampirismPercent;
	public float BerserkerThreshold;
	public float BerserkerDamageMult = 1f;
	public float ThornsPercent;
	public float ExecutionThreshold;
	public float DodgeChance;
	public bool SecondWindAvailable;
	public float SecondWindHealPercent;
	public float IgniteChance;
	public float IgniteDamage;
	public float IgniteDuration;
	public float RicochetChance;
	public float KillSpeedBonusPerKill;
	public float KillSpeedDuration;
	public int KillSpeedMaxStacks;

	// Kill speed runtime state
	public int KillSpeedActiveStacks;
	public float KillSpeedTimer;

	// Progression
	public int Level = 1;
	public float CurrentXp;
	public float XpToNextLevel;
	public bool HasPendingLevelUp;
	public List<string> ChosenIds = new();
	public Dictionary<string, int> PerkStacks = new();

	public float EffectiveMaxHp => MaxHp + BonusMaxHp;
	public float EffectiveRegenRate => BaseRegenRate + BonusRegenRate;
	public float HpRatio => EffectiveMaxHp > 0 ? CurrentHp / EffectiveMaxHp : 0f;

	public static SimPlayerState FromConfig(string characterId)
	{
		CharacterData data = CharacterDataLoader.Get(characterId)
			?? CharacterDataLoader.Get("traqueur");

		SimPlayerState state = new()
		{
			CharacterId = data.Id,
			CharacterName = data.Name,
			MaxHp = data.BaseStats.MaxHp,
			BaseDamage = data.BaseStats.AttackDamage,
			BaseAttackSpeed = data.BaseStats.AttackSpeed,
			BaseAttackRange = data.BaseStats.AttackRange,
			BaseRegenRate = data.BaseStats.RegenRate,
			ScoreMultiplier = data.ScoreMultiplier
		};
		state.CurrentHp = state.MaxHp;
		state.XpToNextLevel = CalculateXpForLevel(1);

		// Starting weapon
		WeaponData weapon = WeaponDataLoader.Get(data.StartingWeaponId)
			?? WeaponDataLoader.Get("makeshift_bow");
		if (weapon != null)
			state.Weapons.Add(new SimWeapon(weapon));

		// Apply character passive perk
		if (!string.IsNullOrEmpty(data.PassivePerk))
		{
			PerkData passive = PerkDataLoader.Get(data.PassivePerk);
			if (passive != null)
				state.ApplyPerk(passive);
		}

		return state;
	}

	// --- Multi-weapon DPS ---

	public float ComputeWeaponDps(SimWeapon weapon)
	{
		float weaponDamage = weapon.GetStat("damage", BaseDamage);
		float characterFactor = BaseDamage / 10f;
		float damage = weaponDamage * characterFactor * DamageMultiplier;

		if (BerserkerThreshold > 0f && HpRatio < BerserkerThreshold)
			damage *= BerserkerDamageMult;

		float atkSpeedMult = AttackSpeedMultiplier;
		if (KillSpeedActiveStacks > 0 && KillSpeedBonusPerKill > 0f)
			atkSpeedMult *= 1f + KillSpeedBonusPerKill * KillSpeedActiveStacks;

		float attacksPerSec = BaseAttackSpeed * weapon.GetStat("attack_speed", 1f) * atkSpeedMult;

		int projectiles = (int)weapon.GetStat("projectile_count", 1f) + ExtraProjectiles;
		int pierce = (int)weapon.GetStat("projectile_pierce", 0f) + ProjectilePierce;
		float pierceFactor = 1f + pierce * 0.3f;
		float ricochetFactor = 1f + RicochetChance * 0.5f;

		return damage * attacksPerSec * projectiles * pierceFactor * ricochetFactor;
	}

	public float ComputeEffectiveDps()
	{
		float totalDps = 0f;
		foreach (SimWeapon w in Weapons)
			totalDps += ComputeWeaponDps(w);

		float critFactor = 1f + CritChance * (CritMultiplier - 1f);
		totalDps *= critFactor;

		totalDps += IgniteChance * IgniteDamage;

		return totalDps;
	}

	// --- Fragment actions ---

	public bool AddWeapon(WeaponData data)
	{
		if (data == null || Weapons.Count >= MaxWeaponSlots) return false;
		if (Weapons.Any(w => w.Id == data.Id)) return false;

		Weapons.Add(new SimWeapon(data));
		ChosenIds.Add(data.Id);
		return true;
	}

	public bool UpgradeWeapon(string weaponId)
	{
		SimWeapon weapon = Weapons.FirstOrDefault(w => w.Id == weaponId);
		if (weapon == null || !weapon.CanLevelUp) return false;

		weapon.LevelUp();
		ChosenIds.Add(weaponId);
		return true;
	}

	public bool AddOrUpgradePassive(string passiveId)
	{
		SimPassive existing = Passives.FirstOrDefault(p => p.Id == passiveId);

		if (existing != null)
		{
			if (existing.IsMaxLevel) return false;

			float prevMod = existing.GetCurrentModifier();
			existing.Upgrade();
			float newMod = existing.GetCurrentModifier();
			ApplyPassiveModifierDelta(existing.Data, prevMod, newMod);

			ChosenIds.Add(passiveId);
			return true;
		}

		if (Passives.Count >= MaxPassiveSlots) return false;

		PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(passiveId);
		if (data == null) return false;

		SimPassive passive = new() { Data = data };
		Passives.Add(passive);
		ApplyPassiveModifier(data, passive.GetCurrentModifier());

		ChosenIds.Add(passiveId);
		return true;
	}

	// --- Passive modifier application (mirrors Player.cs) ---

	private void ApplyPassiveModifier(PassiveSouvenirData data, float value)
	{
		ApplyStatModifier(data.Stat, value, data.ModifierType);
	}

	private void ApplyPassiveModifierDelta(PassiveSouvenirData data, float oldValue, float newValue)
	{
		if (data.ModifierType == "multiplicative")
		{
			if (oldValue > 0f)
				ApplyStatModifier(data.Stat, newValue / oldValue, data.ModifierType);
		}
		else
		{
			ApplyStatModifier(data.Stat, newValue - oldValue, data.ModifierType);
		}
	}

	// --- XP & Level ---

	public void GainXp(float amount)
	{
		CurrentXp += amount;
		while (CurrentXp >= XpToNextLevel)
		{
			CurrentXp -= XpToNextLevel;
			Level++;
			XpToNextLevel = CalculateXpForLevel(Level);
			HasPendingLevelUp = true;
		}
	}

	public void ProcessKillSpeedDecay(float dt)
	{
		if (KillSpeedActiveStacks <= 0) return;

		KillSpeedTimer -= dt;
		if (KillSpeedTimer <= 0f)
		{
			KillSpeedActiveStacks = 0;
			KillSpeedTimer = 0f;
		}
	}

	public void OnKill()
	{
		if (KillSpeedBonusPerKill <= 0f) return;
		KillSpeedActiveStacks = Math.Min(KillSpeedActiveStacks + 1, KillSpeedMaxStacks);
		KillSpeedTimer = KillSpeedDuration;
	}

	// --- World perks (chests/POI/memorial) ---

	public void ApplyPerk(PerkData data)
	{
		PerkStacks[data.Id] = PerkStacks.GetValueOrDefault(data.Id, 0) + 1;
		ChosenIds.Add(data.Id);

		if (data.Effects != null)
		{
			foreach (PerkEffect effect in data.Effects)
				ApplyStatModifier(effect.Stat, effect.Modifier, effect.ModifierType);
			return;
		}

		if (!string.IsNullOrEmpty(data.Stat))
			ApplyStatModifier(data.Stat, data.Modifier, data.ModifierType);

		if (data.Effect != null)
			ApplyComplexEffect(data);
	}

	private void ApplyStatModifier(string stat, float value, string modifierType)
	{
		switch (stat)
		{
			case "damage":
				if (modifierType == "multiplicative") DamageMultiplier *= value;
				break;
			case "speed":
				if (modifierType == "multiplicative") SpeedMultiplier *= value;
				break;
			case "max_hp":
				if (modifierType == "additive") { BonusMaxHp += value; CurrentHp += value; }
				else if (modifierType == "multiplicative")
				{
					float oldMax = EffectiveMaxHp;
					MaxHp *= value;
					CurrentHp = Math.Max(1f, CurrentHp + (EffectiveMaxHp - oldMax));
				}
				break;
			case "attack_speed":
				if (modifierType == "multiplicative") AttackSpeedMultiplier *= value;
				break;
			case "projectile_count":
				if (modifierType == "additive") ExtraProjectiles += (int)value;
				break;
			case "aoe_radius":
				if (modifierType == "multiplicative") AoeMultiplier *= value;
				break;
			case "attack_range":
				if (modifierType == "multiplicative") AttackRangeMultiplier *= value;
				break;
			case "regen_rate":
				if (modifierType == "additive") BonusRegenRate += value;
				break;
			case "armor":
				if (modifierType == "additive") Armor += value;
				break;
			case "crit_chance":
				if (modifierType == "additive") CritChance += value;
				break;
			case "crit_multiplier":
				if (modifierType == "additive") CritMultiplier += value;
				break;
			case "projectile_pierce":
				if (modifierType == "additive") ProjectilePierce += (int)value;
				break;
			case "cooldown_reduction":
				if (modifierType == "multiplicative") AttackSpeedMultiplier *= (1f / value);
				break;
			case "xp_magnet_radius":
				break;
		}
	}

	private void ApplyComplexEffect(PerkData data)
	{
		ComplexEffect fx = data.Effect;

		if (fx.Action == "modify_stat" && fx.Stat != null && fx.Trigger == "passive")
		{
			ApplyStatModifier(fx.Stat, fx.Modifier, fx.ModifierType);
			return;
		}

		if (fx.Trigger == "passive_conditional" && fx.Condition == "hp_percent_below" && fx.Action == "modify_stat")
		{
			BerserkerThreshold = fx.ConditionValue;
			BerserkerDamageMult += (fx.Modifier - 1f);
			return;
		}

		switch (fx.Action)
		{
			case "heal_percent_of_damage": VampirismPercent += fx.Value; break;
			case "apply_dot":
				IgniteChance += fx.Chance;
				IgniteDamage = Math.Max(IgniteDamage, fx.DotDamage);
				IgniteDuration = Math.Max(IgniteDuration, fx.DotDuration);
				break;
			case "bounce_to_nearby": RicochetChance += fx.Chance; break;
			case "reflect_damage_percent": ThornsPercent += fx.Value; break;
			case "execute_below_percent": ExecutionThreshold += fx.Value; break;
			case "revive":
				SecondWindAvailable = true;
				SecondWindHealPercent = fx.HealPercent;
				break;
			case "temporary_buff":
				KillSpeedBonusPerKill += (fx.Modifier - 1f);
				KillSpeedDuration = Math.Max(KillSpeedDuration, fx.Duration);
				KillSpeedMaxStacks = Math.Max(KillSpeedMaxStacks, fx.MaxBuffStacks);
				break;
			case "dodge": DodgeChance += fx.Chance; break;
		}
	}

	private static float CalculateXpForLevel(int level)
	{
		return 20f * MathF.Pow(level, 1.35f);
	}
}
