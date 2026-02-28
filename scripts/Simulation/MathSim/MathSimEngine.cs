using System;
using System.Collections.Generic;
using System.Linq;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation.MathSim;

/// <summary>
/// Moteur de simulation mathématique. Simule une run complète sans Godot (pas de Node, pas de physique).
/// Produit un RunRecord compatible avec le SimulationReport existant.
/// </summary>
public class MathSimEngine
{
    private const float Dt = 0.1f;

    // Skill factor: base kiting effectiveness per AI profile (lower = better kiter)
    // Actual damage taken scales up with enemy count (harder to kite when surrounded)
    private static readonly Dictionary<string, float> BaseSkillFactors = new()
    {
        ["noob"] = 0.55f,
        ["medium"] = 0.30f,
        ["pro"] = 0.18f
    };

    private const float NightSkillPenalty = 1.25f;

    // Enemy count thresholds for skill factor scaling
    private const int KiteEasyThreshold = 5;
    private const int KiteHardThreshold = 25;
    private const float KiteOverwhelmMultiplier = 2.5f;

    // Disabled perks (same as PerkManager._disabledPerks)
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

    // Perk strategy stat categories
    private static readonly HashSet<string> SurvivalStats = new()
        { "max_hp", "regen_rate", "armor", "speed" };
    private static readonly HashSet<string> DamageStats = new()
        { "damage", "attack_speed", "crit_chance", "crit_multiplier", "projectile_count", "projectile_pierce" };

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

        // Tracking
        float simTime = 0f;
        float spawnAccumulator = 0f;
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

            // Kill speed buff
            for (int i = 0; i < kills; i++)
                player.OnKill();

            // Vampirism healing
            float vampHeal = damageThisTick * player.VampirismPercent;

            // 5. Enemies deal damage to player
            float enemyDps = combat.GetActiveEnemyDps();

            // Dynamic skill factor: kiting is easy with few enemies, hard when surrounded
            int activeEnemies = combat.ActiveCount;
            float crowdFactor = activeEnemies <= KiteEasyThreshold
                ? 1f
                : 1f + (KiteOverwhelmMultiplier - 1f) *
                  Math.Min(1f, (float)(activeEnemies - KiteEasyThreshold) / (KiteHardThreshold - KiteEasyThreshold));
            float effectiveSkill = baseSkillFactor * crowdFactor;
            if (phase is SimDayPhase.Night or SimDayPhase.Dusk)
                effectiveSkill *= NightSkillPenalty;
            float rawDamage = enemyDps * Dt * effectiveSkill;

            // Dodge
            if (player.DodgeChance > 0)
                rawDamage *= (1f - player.DodgeChance);

            // Armor reduction
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

            // 10. XP & Level up
            if (xp > 0)
                player.GainXp(xp);

            while (player.HasPendingLevelUp)
            {
                player.HasPendingLevelUp = false;
                OfferPerks(player, strategyType);
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
            PerkIds = new List<string>(player.PerkIds),
            WeaponId = player.WeaponId,
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

    // --- Perk Selection (self-contained, no Player node dependency) ---

    private void OfferPerks(SimPlayerState player, PerkStrategyType strategy)
    {
        string[] choices = PickRandomPerks(3, player);
        if (choices.Length == 0) return;

        string selected;
        if (strategy == PerkStrategyType.Random || choices.Length == 1)
        {
            selected = choices[_rng.Next(choices.Length)];
        }
        else
        {
            selected = ScoreAndSelectPerk(choices, player, strategy);
        }

        PerkData perk = PerkDataLoader.Get(selected);
        if (perk != null)
            player.ApplyPerk(perk);
    }

    private string[] PickRandomPerks(int count, SimPlayerState player)
    {
        List<PerkData> available = PerkDataLoader.GetAll()
            .Where(p =>
                !p.IsPassive
                && !DisabledPerks.Contains(p.Id)
                && player.PerkStacks.GetValueOrDefault(p.Id, 0) < p.MaxStacks
                && (p.CharacterId == null || p.CharacterId == player.CharacterId))
            .ToList();

        if (available.Count == 0)
            return Array.Empty<string>();

        List<string> picked = new();
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            PerkData selected = WeightedRandom(available);
            picked.Add(selected.Id);
            available.Remove(selected);
        }

        return picked.ToArray();
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

    private string ScoreAndSelectPerk(string[] choices, SimPlayerState player, PerkStrategyType strategy)
    {
        string best = choices[0];
        float bestScore = -1f;

        foreach (string perkId in choices)
        {
            PerkData data = PerkDataLoader.Get(perkId);
            if (data == null) continue;

            float score = ScorePerk(data, player, strategy);
            if (score > bestScore)
            {
                bestScore = score;
                best = perkId;
            }
        }
        return best;
    }

    private static float ScorePerk(PerkData data, SimPlayerState player, PerkStrategyType strategy)
    {
        float score = 1f;

        string stat = data.Stat ?? "";
        bool isSurvival = SurvivalStats.Contains(stat);
        bool isDamage = DamageStats.Contains(stat);

        if (data.Effect != null)
        {
            string action = data.Effect.Action ?? "";
            isSurvival = isSurvival || action is "revive" or "dodge" or "reflect_damage_percent" or "heal_percent_of_damage";
            isDamage = isDamage || action is "apply_dot" or "bounce_to_nearby" or "execute_below_percent" or "temporary_buff";
        }

        switch (strategy)
        {
            case PerkStrategyType.Survival:
                score = isSurvival ? 10f : isDamage ? 3f : 5f;
                if (player.HpRatio < 0.5f && stat is "regen_rate" or "max_hp")
                    score *= 2f;
                break;
            case PerkStrategyType.Damage:
                score = isDamage ? 10f : isSurvival ? 3f : 5f;
                break;
            case PerkStrategyType.Balanced:
                score = data.Weight * 5f;
                if (isSurvival) score *= 1.5f;
                if (isDamage) score *= 1.5f;
                break;
        }

        score *= data.Rarity switch
        {
            "rare" => 1.5f,
            "uncommon" => 1.2f,
            _ => 1f
        };

        return score;
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
