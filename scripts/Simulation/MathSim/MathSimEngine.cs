using System;
using System.Collections.Generic;
using System.Linq;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation.MathSim;

/// <summary>
/// Moteur de simulation mathématique. Simule une run complète sans Godot (pas de Node, pas de physique).
/// Produit un RunRecord compatible avec le SimulationReport existant.
///
/// Progression :
/// - Level-up → Fragments de Mémoire (armes ou Souvenirs Passifs, 3 choix)
/// - World exploration → Perks de stats (coffres, POI) à intervalles réguliers
/// </summary>
public class MathSimEngine
{
	private const float Dt = 0.1f;

	// Skill factor: base kiting effectiveness per AI profile (lower = better kiter)
	private static readonly Dictionary<string, float> BaseSkillFactors = new()
	{
		["noob"] = 0.55f,
		["medium"] = 0.30f,
		["pro"] = 0.18f
	};

	private const float NightSkillPenalty = 1.25f;
	private const int KiteEasyThreshold = 5;
	private const int KiteHardThreshold = 25;
	private const float KiteOverwhelmMultiplier = 2.5f;

	// World perk interval (simulates chest/POI finds during exploration)
	private static readonly Dictionary<string, float> WorldPerkIntervals = new()
	{
		["noob"] = 75f,
		["medium"] = 55f,
		["pro"] = 40f
	};

	// Disabled perks for world perk pool
	private static readonly HashSet<string> DisabledPerks = new()
	{
		"channeling", "siphon", "instability", "essence_regen",
		"torch_bearer", "night_vision", "awakened_sight",
		"time_master", "memory_anchor", "last_stand", "salvager",
		"aoe_up", "forgeuse_overcharge",
		"vagabond_jack_of_all", "vagabond_nomad", "vagabond_scrounger",
		"forgeuse_recycler", "forgeuse_last_wall",
		"traqueur_ambush", "traqueur_marked"
	};

	// Fragment scoring: survival vs damage passives
	private static readonly HashSet<string> SurvivalPassiveStats = new()
		{ "max_hp", "regen_rate", "armor", "speed" };
	private static readonly HashSet<string> DamagePassiveStats = new()
		{ "damage", "attack_speed", "crit_chance", "projectile_count", "projectile_pierce", "cooldown_reduction" };

	private readonly SimulationRunConfig _config;
	private readonly Random _rng;

	public MathSimEngine(SimulationRunConfig config, Random rng)
	{
		_config = config;
		_rng = rng;
	}

	public RunRecord Run()
	{
		SimPlayerState player = SimPlayerState.FromConfig(_config.CharacterId ?? "traqueur");
		SimCombatModel combat = new(_config.ScalingOverrides);
		PerkStrategyType strategyType = ParsePerkStrategy(_config.PerkStrategyName);

		float baseSkillFactor = BaseSkillFactors.GetValueOrDefault(_config.ProfileName, 0.30f);
		float worldPerkInterval = WorldPerkIntervals.GetValueOrDefault(_config.ProfileName, 55f);

		// Tracking
		float simTime = 0f;
		float spawnAccumulator = 0f;
		float worldPerkTimer = worldPerkInterval;
		int totalKills = 0;
		float totalDamageDealt = 0f;
		float totalDamageTaken = 0f;
		int combatScore = 0;
		int survivalScore = 0;
		int bonusScore = 0;
		int nightsSurvived = 0;
		bool tookDamageThisNight = false;
		SimDayPhase previousPhase = SimDayPhase.Day;
		int previousNight = 0;
		float maxDuration = _config.MaxDurationSec > 0 ? _config.MaxDurationSec : 1800f;

		while (player.CurrentHp > 0 && simTime < maxDuration)
		{
			simTime += Dt;
			float elapsedMinutes = simTime / 60f;

			// 1. Phase determination
			SimDayPhase phase = combat.GetPhase(simTime, out int nightNumber);

			// Night transition tracking
			if (phase == SimDayPhase.Dawn && previousPhase == SimDayPhase.Night)
			{
				nightsSurvived++;
				survivalScore += (int)(100 * MathF.Pow(1.6f, nightsSurvived - 1));
				if (!tookDamageThisNight)
					bonusScore += 500;
			}
			if (phase == SimDayPhase.Night && previousPhase != SimDayPhase.Night)
				tookDamageThisNight = false;

			previousPhase = phase;
			previousNight = nightNumber;

			// 2. Spawning
			if (phase != SimDayPhase.Dawn)
			{
				float spawnInterval = combat.GetSpawnInterval(elapsedMinutes, phase, nightNumber);
				spawnAccumulator += Dt;

				while (spawnAccumulator >= spawnInterval)
				{
					spawnAccumulator -= spawnInterval;
					string enemyId = combat.PickEnemy(phase, _rng);
					(float hpScale, float dmgScale) = combat.GetEnemyScaling(elapsedMinutes, phase, nightNumber);
					combat.SpawnEnemy(enemyId, hpScale, dmgScale, phase);
				}
			}

			// 3. Advance approaching enemies
			combat.AdvancePendingEnemies(Dt);

			// 4. Player deals damage
			float playerDps = player.ComputeEffectiveDps();
			float damageThisTick = playerDps * Dt;
			totalDamageDealt += damageThisTick;

			(int kills, float xp, int score) = combat.ApplyPlayerDamage(damageThisTick, player.ExecutionThreshold);
			totalKills += kills;
			combatScore += score;

			for (int i = 0; i < kills; i++)
				player.OnKill();

			float vampHeal = damageThisTick * player.VampirismPercent;

			// 5. Enemies deal damage to player
			float enemyDps = combat.GetActiveEnemyDps();

			int activeEnemies = combat.ActiveCount;
			float crowdFactor = activeEnemies <= KiteEasyThreshold
				? 1f
				: 1f + (KiteOverwhelmMultiplier - 1f) *
				  Math.Min(1f, (float)(activeEnemies - KiteEasyThreshold) / (KiteHardThreshold - KiteEasyThreshold));
			float effectiveSkill = baseSkillFactor * crowdFactor;
			if (phase is SimDayPhase.Night or SimDayPhase.Dusk)
				effectiveSkill *= NightSkillPenalty;
			float rawDamage = enemyDps * Dt * effectiveSkill;

			if (player.DodgeChance > 0)
				rawDamage *= (1f - player.DodgeChance);

			float avgHit = combat.GetAverageHitDamage();
			if (player.Armor > 0 && avgHit > 0)
			{
				float armorReduction = Math.Min(player.Armor / avgHit, 0.8f);
				rawDamage *= (1f - armorReduction);
			}

			if (rawDamage > 0)
			{
				totalDamageTaken += rawDamage;
				player.CurrentHp -= rawDamage;
				tookDamageThisNight = true;
				combat.UpdateDamageSource();
			}

			// 6. Healing
			float healing = player.EffectiveRegenRate * Dt + vampHeal;
			player.CurrentHp = Math.Min(player.CurrentHp + healing, player.EffectiveMaxHp);

			// 7. Thorns
			if (player.ThornsPercent > 0 && rawDamage > 0)
			{
				float thornsDamage = rawDamage * player.ThornsPercent;
				combat.ApplyPlayerDamage(thornsDamage, 0f);
			}

			// 8. Second wind
			if (player.CurrentHp <= 0 && player.SecondWindAvailable)
			{
				player.CurrentHp = player.EffectiveMaxHp * player.SecondWindHealPercent;
				player.SecondWindAvailable = false;
			}

			// 9. Kill speed decay
			player.ProcessKillSpeedDecay(Dt);

			// 10. XP & Level up → Fragment selection
			if (xp > 0)
				player.GainXp(xp);

			while (player.HasPendingLevelUp)
			{
				player.HasPendingLevelUp = false;
				OfferFragments(player, strategyType);
			}

			// 11. World perks (simulate chest/POI finds)
			worldPerkTimer -= Dt;
			if (worldPerkTimer <= 0f)
			{
				worldPerkTimer = worldPerkInterval;
				ApplyWorldPerk(player);
			}
		}

		// Build RunRecord
		string deathCause = combat.GetDominantEnemyType();
		float pressure = totalKills > 0 ? (float)combat.TotalSpawned / totalKills : 99f;

		return new RunRecord
		{
			CharacterId = player.CharacterId,
			CharacterName = player.CharacterName,
			Score = (int)((combatScore + survivalScore + bonusScore) * player.ScoreMultiplier),
			NightsSurvived = nightsSurvived,
			TotalKills = totalKills,
			Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
			DeathCause = deathCause,
			DeathNight = previousNight,
			DeathPhase = previousPhase.ToString(),
			PerkIds = new List<string>(player.ChosenIds),
			WeaponId = player.Weapons.Count > 0 ? player.Weapons[0].Id : "unknown",
			TotalDamageDealt = totalDamageDealt,
			TotalDamageTaken = totalDamageTaken,
			ResourcesCollected = new Dictionary<string, int>(),
			MaxLevel = player.Level,
			RunDurationSec = simTime,
			Seed = 0,
			CombatScoreDetail = combatScore,
			SurvivalScoreDetail = survivalScore,
			BonusScoreDetail = bonusScore,
			TotalSpawned = combat.TotalSpawned,
			PeakEnemies = combat.PeakEnemies,
			AvgPressure = pressure,
			FinalHpScale = combat.LastHpScale,
			FinalDmgScale = combat.LastDmgScale,
			SimLabel = _config.Label,
			SimProfile = _config.ProfileName,
			SimPerkStrategy = _config.PerkStrategyName
		};
	}

	// --- Fragment Selection (level-up: weapons + passive souvenirs) ---

	private void OfferFragments(SimPlayerState player, PerkStrategyType strategy)
	{
		List<FragmentChoice> pool = BuildFragmentPool(player);
		if (pool.Count == 0) return;

		// Pick 3 random from pool
		List<FragmentChoice> choices = new();
		List<FragmentChoice> available = new(pool);
		for (int i = 0; i < 3 && available.Count > 0; i++)
		{
			int idx = _rng.Next(available.Count);
			choices.Add(available[idx]);
			available.RemoveAt(idx);
		}

		// Score and select best
		FragmentChoice best = choices[0];
		float bestScore = -1f;

		foreach (FragmentChoice choice in choices)
		{
			float score = ScoreFragment(choice, player, strategy);
			if (score > bestScore)
			{
				bestScore = score;
				best = choice;
			}
		}

		// Apply
		switch (best.Type)
		{
			case "weapon_new":
				WeaponData weaponData = WeaponDataLoader.Get(best.Id);
				player.AddWeapon(weaponData);
				break;
			case "weapon_upgrade":
				player.UpgradeWeapon(best.Id);
				break;
			case "passive_new":
			case "passive_upgrade":
				player.AddOrUpgradePassive(best.Id);
				break;
		}
	}

	private List<FragmentChoice> BuildFragmentPool(SimPlayerState player)
	{
		List<FragmentChoice> pool = new();
		bool weaponSlotsFull = player.Weapons.Count >= SimPlayerState.MaxWeaponSlots;
		bool passiveSlotsFull = player.Passives.Count >= SimPlayerState.MaxPassiveSlots;

		HashSet<string> equippedWeaponIds = new();
		foreach (SimWeapon w in player.Weapons)
			equippedWeaponIds.Add(w.Id);

		// New weapons (if slots available)
		if (!weaponSlotsFull)
		{
			foreach (WeaponData weapon in WeaponDataLoader.GetAll())
			{
				if (equippedWeaponIds.Contains(weapon.Id)) continue;
				if (weapon.Source == "craft") continue;
				if (!string.IsNullOrEmpty(weapon.DefaultFor) && weapon.DefaultFor != player.CharacterId) continue;

				pool.Add(new FragmentChoice(weapon.Id, "weapon_new"));
			}
		}

		// Weapon upgrades
		foreach (SimWeapon w in player.Weapons)
		{
			if (w.CanLevelUp)
				pool.Add(new FragmentChoice(w.Id, "weapon_upgrade"));
		}

		HashSet<string> equippedPassiveIds = new();
		foreach (SimPassive p in player.Passives)
			equippedPassiveIds.Add(p.Id);

		// New passives (if slots available)
		if (!passiveSlotsFull)
		{
			foreach (PassiveSouvenirData passive in PassiveSouvenirDataLoader.GetAll())
			{
				if (equippedPassiveIds.Contains(passive.Id)) continue;
				pool.Add(new FragmentChoice(passive.Id, "passive_new"));
			}
		}

		// Passive upgrades
		foreach (SimPassive p in player.Passives)
		{
			if (!p.IsMaxLevel)
				pool.Add(new FragmentChoice(p.Id, "passive_upgrade"));
		}

		return pool;
	}

	private float ScoreFragment(FragmentChoice choice, SimPlayerState player, PerkStrategyType strategy)
	{
		float score = 1f;

		switch (choice.Type)
		{
			case "weapon_new":
			{
				// New weapon = big DPS boost (new attack source)
				WeaponData data = WeaponDataLoader.Get(choice.Id);
				if (data == null) return 0f;
				float weaponDmg = data.Stats.GetValueOrDefault("damage", 10f);
				float weaponSpd = data.Stats.GetValueOrDefault("attack_speed", 1f);
				score = weaponDmg * weaponSpd * 2f;

				if (strategy == PerkStrategyType.Damage) score *= 1.5f;
				break;
			}
			case "weapon_upgrade":
			{
				// Upgrade = moderate boost (+15% dmg, +10% atk speed per level)
				SimWeapon w = player.Weapons.FirstOrDefault(w => w.Id == choice.Id);
				if (w == null) return 0f;
				score = 8f + w.Level * 2f;

				if (strategy == PerkStrategyType.Damage) score *= 1.3f;
				break;
			}
			case "passive_new":
			{
				PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(choice.Id);
				if (data == null) return 0f;
				bool isDmg = DamagePassiveStats.Contains(data.Stat);
				bool isSurv = SurvivalPassiveStats.Contains(data.Stat);

				score = 6f;
				if (strategy == PerkStrategyType.Damage && isDmg) score *= 2f;
				else if (strategy == PerkStrategyType.Survival && isSurv) score *= 2f;
				else if (strategy == PerkStrategyType.Balanced) score *= 1.3f;

				if (player.HpRatio < 0.5f && isSurv) score *= 1.5f;
				break;
			}
			case "passive_upgrade":
			{
				SimPassive p = player.Passives.FirstOrDefault(p => p.Id == choice.Id);
				if (p == null) return 0f;
				bool isDmg = DamagePassiveStats.Contains(p.Data.Stat);
				bool isSurv = SurvivalPassiveStats.Contains(p.Data.Stat);

				score = 5f + p.Level;
				if (strategy == PerkStrategyType.Damage && isDmg) score *= 1.5f;
				else if (strategy == PerkStrategyType.Survival && isSurv) score *= 1.5f;
				break;
			}
		}

		return score;
	}

	// --- World Perks (chests/POI simulation) ---

	private void ApplyWorldPerk(SimPlayerState player)
	{
		List<PerkData> available = PerkDataLoader.GetAll()
			.Where(p =>
				!p.IsPassive
				&& !DisabledPerks.Contains(p.Id)
				&& player.PerkStacks.GetValueOrDefault(p.Id, 0) < p.MaxStacks
				&& (p.CharacterId == null || p.CharacterId == player.CharacterId))
			.ToList();

		if (available.Count == 0) return;

		PerkData selected = WeightedRandom(available);
		if (selected != null)
			player.ApplyPerk(selected);
	}

	private PerkData WeightedRandom(List<PerkData> perks)
	{
		float totalWeight = 0f;
		foreach (PerkData perk in perks)
			totalWeight += perk.Weight;

		float roll = (float)(_rng.NextDouble() * totalWeight);
		float cumulative = 0f;

		foreach (PerkData perk in perks)
		{
			cumulative += perk.Weight;
			if (roll <= cumulative)
				return perk;
		}

		return perks[^1];
	}

	private static PerkStrategyType ParsePerkStrategy(string name) => name switch
	{
		"random" => PerkStrategyType.Random,
		"survival" => PerkStrategyType.Survival,
		"damage" => PerkStrategyType.Damage,
		"balanced" => PerkStrategyType.Balanced,
		_ => PerkStrategyType.Random
	};
}

public class FragmentChoice
{
	public string Id;
	public string Type;

	public FragmentChoice(string id, string type)
	{
		Id = id;
		Type = type;
	}
}
