using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation.MathSim;

public enum SimDayPhase { Day, Dusk, Night, Dawn }

/// <summary>
/// Modèle de combat agrégé pour la simulation mathématique.
/// Gère le spawning par cohortes, le cycle jour/nuit, et le scaling.
/// </summary>
public class SimCombatModel
{
    // Scaling config
    private float _baseSpawnInterval = 1.8f;
    private float _minSpawnInterval = 0.25f;
    private float _spawnIntervalDecay = 0.06f;
    private float _hpScalingPerMinute = 1.08f;
    private float _dmgScalingPerMinute = 1.05f;
    private int _maxEnemies = 140;
    private float _nightHpMultiplier = 1.2f;
    private float _nightDmgMultiplier = 1.1f;
    private float _nightSpawnRateMultiplier = 0.91f;

    // Cycle config
    private float _dayDuration = 120f;
    private float _duskDuration = 30f;
    private float _nightDuration = 90f;
    private float _dawnDuration = 10f;
    private float _totalCycle;

    // Enemy pools
    private static readonly string[] DayPool = { "shadow_crawler", "fading_spitter" };
    private static readonly string[] NightPool = { "shadow_crawler", "shade", "shade", "fading_spitter", "void_brute", "wailing_sentinel" };

    // Static data (loaded once on main thread via InitStaticData)
    private static Dictionary<string, SimEnemyType> _enemyTypes;
    private static ScalingDefaults _scalingDefaults;
    private static CycleDefaults _cycleDefaults;
    private static bool _staticDataLoaded;

    // Score points per enemy
    private static readonly Dictionary<string, int> ScorePoints = new()
    {
        ["shade"] = 5,
        ["shadow_crawler"] = 10,
        ["charognard"] = 10,
        ["fading_spitter"] = 15,
        ["wailing_sentinel"] = 25,
        ["void_brute"] = 30,
        ["treant_corrompu"] = 35
    };

    // Cohorts
    private readonly List<EnemyCohort> _cohorts = new();

    // Tracking
    public int TotalSpawned { get; private set; }
    public int PeakEnemies { get; private set; }
    public int ActiveCount { get; private set; }
    public float LastHpScale { get; private set; } = 1f;
    public float LastDmgScale { get; private set; } = 1f;
    private string _lastDamageSourceType = "unknown";

    /// <summary>
    /// Charge toutes les données statiques depuis les fichiers JSON.
    /// Doit être appelé une fois sur le main thread avant toute utilisation parallèle.
    /// </summary>
    public static void InitStaticData()
    {
        if (_staticDataLoaded) return;

        LoadEnemyTypes();
        LoadScalingDefaults();
        LoadCycleDefaults();
        _staticDataLoaded = true;
    }

    public SimCombatModel(Dictionary<string, float> scalingOverrides)
    {
        // Copy defaults from static cache
        _baseSpawnInterval = _scalingDefaults.BaseSpawnInterval;
        _minSpawnInterval = _scalingDefaults.MinSpawnInterval;
        _spawnIntervalDecay = _scalingDefaults.SpawnIntervalDecay;
        _hpScalingPerMinute = _scalingDefaults.HpScalingPerMinute;
        _dmgScalingPerMinute = _scalingDefaults.DmgScalingPerMinute;
        _maxEnemies = _scalingDefaults.MaxEnemies;
        _nightHpMultiplier = _scalingDefaults.NightHpMultiplier;
        _nightDmgMultiplier = _scalingDefaults.NightDmgMultiplier;
        _nightSpawnRateMultiplier = _scalingDefaults.NightSpawnRateMultiplier;

        _dayDuration = _cycleDefaults.DayDuration;
        _duskDuration = _cycleDefaults.DuskDuration;
        _nightDuration = _cycleDefaults.NightDuration;
        _dawnDuration = _cycleDefaults.DawnDuration;

        // Apply per-run overrides
        ApplyScalingOverrides(scalingOverrides);

        _totalCycle = _dayDuration + _duskDuration + _nightDuration + _dawnDuration;
    }

    // --- Phase System ---

    public SimDayPhase GetPhase(float simTime, out int nightNumber)
    {
        int cycleIndex = (int)(simTime / _totalCycle);
        float timeInCycle = simTime % _totalCycle;

        SimDayPhase phase;
        if (timeInCycle < _dayDuration)
            phase = SimDayPhase.Day;
        else if (timeInCycle < _dayDuration + _duskDuration)
            phase = SimDayPhase.Dusk;
        else if (timeInCycle < _dayDuration + _duskDuration + _nightDuration)
            phase = SimDayPhase.Night;
        else
            phase = SimDayPhase.Dawn;

        nightNumber = cycleIndex + (phase >= SimDayPhase.Night ? 1 : 0);
        return phase;
    }

    // --- Spawning ---

    public float GetSpawnInterval(float elapsedMinutes, SimDayPhase phase, int nightNumber)
    {
        if (phase == SimDayPhase.Dawn)
            return float.MaxValue;

        float baseInterval = Math.Max(_baseSpawnInterval - _spawnIntervalDecay * elapsedMinutes, _minSpawnInterval);

        float phaseFactor = phase switch
        {
            SimDayPhase.Day => 0.8f,
            SimDayPhase.Dusk => 0.65f,
            SimDayPhase.Night => 0.3f * MathF.Pow(_nightSpawnRateMultiplier, Math.Max(0, nightNumber - 1)),
            _ => 1f
        };

        return Math.Max(baseInterval * phaseFactor, _minSpawnInterval);
    }

    public (float hpScale, float dmgScale) GetEnemyScaling(float elapsedMinutes, SimDayPhase phase, int nightNumber)
    {
        float hpScale = MathF.Pow(_hpScalingPerMinute, elapsedMinutes);
        float dmgScale = MathF.Pow(_dmgScalingPerMinute, elapsedMinutes);

        if (phase == SimDayPhase.Night && nightNumber > 1)
        {
            hpScale *= MathF.Pow(_nightHpMultiplier, nightNumber - 1);
            dmgScale *= MathF.Pow(_nightDmgMultiplier, nightNumber - 1);
        }

        LastHpScale = hpScale;
        LastDmgScale = dmgScale;
        return (hpScale, dmgScale);
    }

    public string PickEnemy(SimDayPhase phase, Random rng)
    {
        string[] pool = phase == SimDayPhase.Night || phase == SimDayPhase.Dusk ? NightPool : DayPool;
        return pool[rng.Next(pool.Length)];
    }

    public void SpawnEnemy(string enemyId, float hpScale, float dmgScale, SimDayPhase phase)
    {
        if (!_enemyTypes.TryGetValue(enemyId, out SimEnemyType type))
            return;

        if (ActiveCount + GetPendingCount() >= _maxEnemies)
            return;

        float scaledHp = type.BaseHp * hpScale;
        float scaledDmg = type.BaseDamage * dmgScale;

        // Approach time based on spawn distance and speed
        float spawnDistance = phase == SimDayPhase.Night ? 700f : 500f;
        float approachDistance = type.IsRanged ? Math.Max(0, spawnDistance - type.AttackRange) : spawnDistance;
        float approachTime = type.Speed > 0 ? approachDistance / type.Speed : 0f;

        _cohorts.Add(new EnemyCohort
        {
            EnemyId = enemyId,
            IndividualHp = scaledHp,
            IndividualDamage = scaledDmg,
            AttackCooldown = type.AttackCooldown,
            Count = 1,
            ApproachTimeRemaining = approachTime,
            XpReward = type.XpReward,
            CurrentTargetHp = scaledHp
        });

        TotalSpawned++;
    }

    // --- Tick Processing ---

    public void AdvancePendingEnemies(float dt)
    {
        foreach (EnemyCohort cohort in _cohorts)
        {
            if (cohort.ApproachTimeRemaining > 0)
                cohort.ApproachTimeRemaining = Math.Max(0, cohort.ApproachTimeRemaining - dt);
        }

        // Update counts
        ActiveCount = _cohorts.Where(c => c.ApproachTimeRemaining <= 0).Sum(c => c.Count);
        int totalCount = _cohorts.Sum(c => c.Count);
        PeakEnemies = Math.Max(PeakEnemies, totalCount);
    }

    public float GetActiveEnemyDps()
    {
        float dps = 0f;
        foreach (EnemyCohort cohort in _cohorts)
        {
            if (cohort.ApproachTimeRemaining <= 0 && cohort.Count > 0)
                dps += cohort.Count * cohort.IndividualDamage / cohort.AttackCooldown;
        }
        return dps;
    }

    public float GetAverageHitDamage()
    {
        float totalDmg = 0f;
        int totalCount = 0;
        foreach (EnemyCohort cohort in _cohorts)
        {
            if (cohort.ApproachTimeRemaining <= 0 && cohort.Count > 0)
            {
                totalDmg += cohort.Count * cohort.IndividualDamage;
                totalCount += cohort.Count;
            }
        }
        return totalCount > 0 ? totalDmg / totalCount : 5f;
    }

    /// <summary>
    /// Applique les dégâts du joueur aux cohortes. Retourne kills, XP, score, et le dernier type tué.
    /// Les dégâts s'accumulent sur l'ennemi courant de chaque cohorte entre les ticks.
    /// </summary>
    public (int kills, float xp, int score) ApplyPlayerDamage(float damage, float executionThreshold)
    {
        int totalKills = 0;
        float totalXp = 0f;
        int totalScore = 0;

        // Sort cohorts: active first, then by lowest current target HP (closest to dying first)
        List<EnemyCohort> active = _cohorts
            .Where(c => c.ApproachTimeRemaining <= 0 && c.Count > 0)
            .OrderBy(c => c.CurrentTargetHp)
            .ToList();

        float remainingDamage = damage;

        foreach (EnemyCohort cohort in active)
        {
            if (remainingDamage <= 0) break;

            // Execution threshold reduces effective HP needed to kill
            float effectiveMaxHp = cohort.IndividualHp;
            if (executionThreshold > 0f)
                effectiveMaxHp *= (1f - executionThreshold);

            // Initialize current target HP if needed
            if (cohort.CurrentTargetHp <= 0f)
                cohort.CurrentTargetHp = effectiveMaxHp;

            // Apply damage to current target, then overflow to next enemies
            while (remainingDamage > 0 && cohort.Count > 0)
            {
                if (remainingDamage >= cohort.CurrentTargetHp)
                {
                    remainingDamage -= cohort.CurrentTargetHp;
                    cohort.Count--;
                    totalKills++;
                    totalXp += cohort.XpReward;
                    totalScore += ScorePoints.GetValueOrDefault(cohort.EnemyId, 10);
                    cohort.CurrentTargetHp = effectiveMaxHp;
                }
                else
                {
                    cohort.CurrentTargetHp -= remainingDamage;
                    remainingDamage = 0;
                }
            }
        }

        // Clean up empty cohorts
        _cohorts.RemoveAll(c => c.Count <= 0);

        // Update active count
        ActiveCount = _cohorts.Where(c => c.ApproachTimeRemaining <= 0).Sum(c => c.Count);

        return (totalKills, totalXp, totalScore);
    }

    public string GetDominantEnemyType()
    {
        // Return the enemy type with most active enemies (likely death cause)
        EnemyCohort dominant = _cohorts
            .Where(c => c.ApproachTimeRemaining <= 0 && c.Count > 0)
            .OrderByDescending(c => c.Count * c.IndividualDamage)
            .FirstOrDefault();

        return dominant?.EnemyId ?? _lastDamageSourceType;
    }

    public void UpdateDamageSource()
    {
        string type = GetDominantEnemyType();
        if (type != null)
            _lastDamageSourceType = type;
    }

    private int GetPendingCount()
    {
        return _cohorts.Where(c => c.ApproachTimeRemaining > 0).Sum(c => c.Count);
    }

    // --- Data Loading (static, main thread only) ---

    private void ApplyScalingOverrides(Dictionary<string, float> overrides)
    {
        if (overrides == null) return;
        foreach (KeyValuePair<string, float> kv in overrides)
        {
            switch (kv.Key)
            {
                case "base_spawn_interval": _baseSpawnInterval = kv.Value; break;
                case "min_spawn_interval": _minSpawnInterval = kv.Value; break;
                case "spawn_interval_decay_per_minute": _spawnIntervalDecay = kv.Value; break;
                case "hp_scaling_per_minute": _hpScalingPerMinute = kv.Value; break;
                case "damage_scaling_per_minute": _dmgScalingPerMinute = kv.Value; break;
                case "max_enemies": _maxEnemies = (int)kv.Value; break;
                case "night_hp_multiplier": _nightHpMultiplier = kv.Value; break;
                case "night_damage_multiplier": _nightDmgMultiplier = kv.Value; break;
                case "night_spawn_rate_multiplier": _nightSpawnRateMultiplier = kv.Value; break;
            }
        }
    }

    private static void LoadScalingDefaults()
    {
        _scalingDefaults = new ScalingDefaults();

        FileAccess file = FileAccess.Open("res://data/scaling/wave_scaling.json", FileAccess.ModeFlags.Read);
        if (file == null) return;

        string json = file.GetAsText();
        file.Close();

        Godot.Collections.Dictionary parsed = (Godot.Collections.Dictionary)Json.ParseString(json);
        if (parsed == null) return;

        _scalingDefaults.BaseSpawnInterval = parsed.ContainsKey("base_spawn_interval") ? (float)parsed["base_spawn_interval"] : 1.8f;
        _scalingDefaults.MinSpawnInterval = parsed.ContainsKey("min_spawn_interval") ? (float)parsed["min_spawn_interval"] : 0.25f;
        _scalingDefaults.SpawnIntervalDecay = parsed.ContainsKey("spawn_interval_decay_per_minute") ? (float)parsed["spawn_interval_decay_per_minute"] : 0.06f;
        _scalingDefaults.HpScalingPerMinute = parsed.ContainsKey("hp_scaling_per_minute") ? (float)parsed["hp_scaling_per_minute"] : 1.08f;
        _scalingDefaults.DmgScalingPerMinute = parsed.ContainsKey("damage_scaling_per_minute") ? (float)parsed["damage_scaling_per_minute"] : 1.05f;
        _scalingDefaults.MaxEnemies = parsed.ContainsKey("max_enemies") ? (int)parsed["max_enemies"] : 140;
        _scalingDefaults.NightHpMultiplier = parsed.ContainsKey("night_hp_multiplier") ? (float)parsed["night_hp_multiplier"] : 1.2f;
        _scalingDefaults.NightDmgMultiplier = parsed.ContainsKey("night_damage_multiplier") ? (float)parsed["night_damage_multiplier"] : 1.1f;
        _scalingDefaults.NightSpawnRateMultiplier = parsed.ContainsKey("night_spawn_rate_multiplier") ? (float)parsed["night_spawn_rate_multiplier"] : 0.91f;
    }

    private static void LoadCycleDefaults()
    {
        _cycleDefaults = new CycleDefaults();

        FileAccess file = FileAccess.Open("res://data/scaling/day_night_cycle.json", FileAccess.ModeFlags.Read);
        if (file == null) return;

        string json = file.GetAsText();
        file.Close();

        Godot.Collections.Dictionary parsed = (Godot.Collections.Dictionary)Json.ParseString(json);
        if (parsed == null) return;

        _cycleDefaults.DayDuration = parsed.ContainsKey("day_duration") ? (float)parsed["day_duration"] : 120f;
        _cycleDefaults.DuskDuration = parsed.ContainsKey("dusk_duration") ? (float)parsed["dusk_duration"] : 30f;
        _cycleDefaults.NightDuration = parsed.ContainsKey("night_duration") ? (float)parsed["night_duration"] : 90f;
        _cycleDefaults.DawnDuration = parsed.ContainsKey("dawn_duration") ? (float)parsed["dawn_duration"] : 10f;
    }

    private static void LoadEnemyTypes()
    {
        _enemyTypes = new Dictionary<string, SimEnemyType>();
        EnemyDataLoader.Load();

        foreach (string id in EnemyDataLoader.GetAllIds())
        {
            EnemyData data = EnemyDataLoader.Get(id);
            if (data == null) continue;

            bool isRanged = data.Type == "ranged";
            _enemyTypes[data.Id] = new SimEnemyType
            {
                Id = data.Id,
                BaseHp = data.Stats.Hp,
                Speed = data.Stats.Speed,
                BaseDamage = data.Stats.Damage,
                AttackRange = isRanged ? data.Stats.AttackRange : 25f,
                AttackCooldown = isRanged ? 1.5f : 1.0f,
                XpReward = data.Stats.XpReward,
                IsRanged = isRanged
            };
        }
    }
}

public class EnemyCohort
{
    public string EnemyId;
    public float IndividualHp;
    public float IndividualDamage;
    public float AttackCooldown;
    public int Count;
    public float ApproachTimeRemaining;
    public float XpReward;
    public float CurrentTargetHp;
}

public class SimEnemyType
{
    public string Id;
    public float BaseHp;
    public float Speed;
    public float BaseDamage;
    public float AttackRange;
    public float AttackCooldown;
    public float XpReward;
    public bool IsRanged;
}

public class ScalingDefaults
{
    public float BaseSpawnInterval = 1.8f;
    public float MinSpawnInterval = 0.25f;
    public float SpawnIntervalDecay = 0.06f;
    public float HpScalingPerMinute = 1.08f;
    public float DmgScalingPerMinute = 1.05f;
    public int MaxEnemies = 140;
    public float NightHpMultiplier = 1.2f;
    public float NightDmgMultiplier = 1.1f;
    public float NightSpawnRateMultiplier = 0.91f;
}

public class CycleDefaults
{
    public float DayDuration = 120f;
    public float DuskDuration = 30f;
    public float NightDuration = 90f;
    public float DawnDuration = 10f;
}
