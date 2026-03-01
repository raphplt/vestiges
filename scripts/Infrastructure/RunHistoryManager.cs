using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Vestiges.Infrastructure;

public class RunRecord
{
    [JsonPropertyName("character_id")]
    public string CharacterId { get; set; }

    [JsonPropertyName("character_name")]
    public string CharacterName { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("nights_survived")]
    public int NightsSurvived { get; set; }

    [JsonPropertyName("total_kills")]
    public int TotalKills { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    // --- Analytics enrichis ---

    [JsonPropertyName("death_cause")]
    public string DeathCause { get; set; }

    [JsonPropertyName("death_night")]
    public int DeathNight { get; set; }

    [JsonPropertyName("death_phase")]
    public string DeathPhase { get; set; }

    [JsonPropertyName("perk_ids")]
    public List<string> PerkIds { get; set; }

    [JsonPropertyName("weapon_id")]
    public string WeaponId { get; set; }

    [JsonPropertyName("total_damage_dealt")]
    public float TotalDamageDealt { get; set; }

    [JsonPropertyName("total_damage_taken")]
    public float TotalDamageTaken { get; set; }

    [JsonPropertyName("resources_collected")]
    public Dictionary<string, int> ResourcesCollected { get; set; }

    [JsonPropertyName("structures_placed")]
    public int StructuresPlaced { get; set; }

    [JsonPropertyName("structures_lost")]
    public int StructuresLost { get; set; }

    [JsonPropertyName("pois_explored")]
    public int PoisExplored { get; set; }

    [JsonPropertyName("chests_opened")]
    public int ChestsOpened { get; set; }

    [JsonPropertyName("max_level")]
    public int MaxLevel { get; set; }

    [JsonPropertyName("run_duration_sec")]
    public float RunDurationSec { get; set; }

    [JsonPropertyName("seed")]
    public ulong Seed { get; set; }

    [JsonPropertyName("combat_score")]
    public int CombatScoreDetail { get; set; }

    [JsonPropertyName("survival_score")]
    public int SurvivalScoreDetail { get; set; }

    [JsonPropertyName("bonus_score")]
    public int BonusScoreDetail { get; set; }

    [JsonPropertyName("build_score")]
    public int BuildScoreDetail { get; set; }

    [JsonPropertyName("exploration_score")]
    public int ExplorationScoreDetail { get; set; }

    // --- Métriques de difficulté ---

    [JsonPropertyName("total_spawned")]
    public int TotalSpawned { get; set; }

    [JsonPropertyName("peak_enemies")]
    public int PeakEnemies { get; set; }

    [JsonPropertyName("avg_pressure")]
    public float AvgPressure { get; set; }

    [JsonPropertyName("final_hp_scale")]
    public float FinalHpScale { get; set; }

    [JsonPropertyName("final_dmg_scale")]
    public float FinalDmgScale { get; set; }

    // --- Mutators ---

    [JsonPropertyName("active_mutators")]
    public List<string> ActiveMutators { get; set; }

    [JsonPropertyName("mutator_multiplier")]
    public float MutatorMultiplier { get; set; } = 1f;

    // --- Simulation metadata (null for normal runs) ---

    [JsonPropertyName("sim_label")]
    public string SimLabel { get; set; }

    [JsonPropertyName("sim_profile")]
    public string SimProfile { get; set; }

    [JsonPropertyName("sim_perk_strategy")]
    public string SimPerkStrategy { get; set; }
}

/// <summary>
/// Gestionnaire statique de l'historique des runs.
/// Sauvegarde/charge depuis user://run_history.json.
/// Garde les 50 dernières runs.
/// </summary>
public static class RunHistoryManager
{
    private const string HistoryPath = "user://run_history.json";
    private const int MaxEntries = 50;

    private static List<RunRecord> _history = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        _loaded = true;

        if (!FileAccess.FileExists(HistoryPath))
        {
            _history = new List<RunRecord>();
            return;
        }

        FileAccess file = FileAccess.Open(HistoryPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            _history = new List<RunRecord>();
            return;
        }

        string json = file.GetAsText();
        file.Close();

        try
        {
            _history = JsonSerializer.Deserialize<List<RunRecord>>(json) ?? new List<RunRecord>();
        }
        catch (JsonException ex)
        {
            GD.PushWarning($"[RunHistoryManager] Failed to parse history: {ex.Message}");
            _history = new List<RunRecord>();
        }

        GD.Print($"[RunHistoryManager] Loaded {_history.Count} run(s)");
    }

    public static void SaveRun(RunRecord record)
    {
        Load();

        _history.Insert(0, record);

        if (_history.Count > MaxEntries)
            _history.RemoveRange(MaxEntries, _history.Count - MaxEntries);

        Save();
        GD.Print($"[RunHistoryManager] Saved run: {record.CharacterName} — Score {record.Score}, {record.NightsSurvived} nuits");
    }

    public static List<RunRecord> GetHistory()
    {
        Load();
        return new List<RunRecord>(_history);
    }

    public static int GetBestScore()
    {
        Load();
        int best = 0;
        foreach (RunRecord run in _history)
        {
            if (run.Score > best)
                best = run.Score;
        }
        return best;
    }

    public static int GetMaxNights()
    {
        Load();
        int max = 0;
        foreach (RunRecord run in _history)
        {
            if (run.NightsSurvived > max)
                max = run.NightsSurvived;
        }
        return max;
    }

    /// <summary>Top N runs triées par score décroissant.</summary>
    public static List<RunRecord> GetTopByScore(int count = 10)
    {
        Load();
        List<RunRecord> sorted = new(_history);
        sorted.Sort((a, b) => b.Score.CompareTo(a.Score));
        return sorted.GetRange(0, System.Math.Min(count, sorted.Count));
    }

    /// <summary>Top N runs triées par nuits survivées décroissantes.</summary>
    public static List<RunRecord> GetTopByNights(int count = 10)
    {
        Load();
        List<RunRecord> sorted = new(_history);
        sorted.Sort((a, b) =>
        {
            int cmp = b.NightsSurvived.CompareTo(a.NightsSurvived);
            return cmp != 0 ? cmp : b.Score.CompareTo(a.Score);
        });
        return sorted.GetRange(0, System.Math.Min(count, sorted.Count));
    }

    /// <summary>Force un rechargement au prochain Load() (utile entre runs de simulation).</summary>
    public static void ForceReload()
    {
        _loaded = false;
    }

    private static void Save()
    {
        FileAccess file = FileAccess.Open(HistoryPath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError("[RunHistoryManager] Cannot save history");
            return;
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(_history, options);
        file.StoreString(json);
        file.Close();
    }
}
