using System.Collections.Generic;
using System.Linq;

namespace Vestiges.Infrastructure;

/// <summary>
/// Agrégation de métriques V2 depuis l'historique des runs.
/// </summary>
public static class RunAnalytics
{
    public class CharacterRunStats
    {
        public float AvgScore { get; set; }
        public float AvgDurationSec { get; set; }
        public float AvgCrises { get; set; }
        public int RunCount { get; set; }
        public int BestScore { get; set; }
    }

    public static float GetAverageCrises()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        if (history.Count == 0) return 0f;
        return (float)history.Average(r => r.CrisesSurvived);
    }

    public static float GetAverageNights()
    {
        return GetAverageCrises();
    }

    public static float GetAverageScore()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        if (history.Count == 0) return 0f;
        return (float)history.Average(r => r.Score);
    }

    public static float GetAverageRunDuration()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withDuration = history.Where(r => r.RunDurationSec > 0f).ToList();
        if (withDuration.Count == 0) return 0f;
        return (float)withDuration.Average(r => r.RunDurationSec);
    }

    public static Dictionary<string, int> GetDeathCauseDistribution()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        Dictionary<string, int> distribution = new();
        foreach (RunRecord run in history)
        {
            string cause = run.DeathCause ?? "unknown";
            distribution[cause] = distribution.GetValueOrDefault(cause, 0) + 1;
        }
        return distribution;
    }

    public static Dictionary<string, int> GetPerkPickRates()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        Dictionary<string, int> rates = new();
        foreach (RunRecord run in history)
        {
            if (run.PerkIds == null) continue;
            foreach (string perkId in run.PerkIds)
                rates[perkId] = rates.GetValueOrDefault(perkId, 0) + 1;
        }
        return rates.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public static Dictionary<string, CharacterRunStats> GetCharacterStats()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        Dictionary<string, List<RunRecord>> grouped = new();
        foreach (RunRecord run in history)
        {
            string charId = run.CharacterId ?? "unknown";
            if (!grouped.ContainsKey(charId))
                grouped[charId] = new List<RunRecord>();
            grouped[charId].Add(run);
        }

        Dictionary<string, CharacterRunStats> stats = new();
        foreach (KeyValuePair<string, List<RunRecord>> group in grouped)
        {
            stats[group.Key] = new CharacterRunStats
            {
                AvgScore = (float)group.Value.Average(r => r.Score),
                AvgDurationSec = (float)group.Value.Average(r => r.RunDurationSec),
                AvgCrises = (float)group.Value.Average(r => r.CrisesSurvived),
                RunCount = group.Value.Count,
                BestScore = group.Value.Max(r => r.Score)
            };
        }
        return stats;
    }

    public static List<int> GetScoreTrend(int count = 10)
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        return history.Take(count).Select(r => r.Score).ToList();
    }

    public static float GetAverageDps()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withDuration = history.Where(r => r.RunDurationSec > 0f).ToList();
        if (withDuration.Count == 0) return 0f;
        return (float)withDuration.Average(r => r.TotalDamageDealt / r.RunDurationSec);
    }

    public static float GetAverageDeathNight()
    {
        return 0f;
    }

    public static float GetAveragePressure()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withPressure = history.Where(r => r.AvgPressure > 0f).ToList();
        if (withPressure.Count == 0) return 0f;
        return (float)withPressure.Average(r => r.AvgPressure);
    }

    public static float GetAveragePeakEnemies()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withPeak = history.Where(r => r.PeakEnemies > 0).ToList();
        if (withPeak.Count == 0) return 0f;
        return (float)withPeak.Average(r => r.PeakEnemies);
    }

    public static float GetAverageKillEfficiency()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withSpawns = history.Where(r => r.TotalSpawned > 0).ToList();
        if (withSpawns.Count == 0) return 0f;
        return (float)withSpawns.Average(r => (float)r.TotalKills / r.TotalSpawned);
    }

    public static float GetAverageFinalHpScale()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withScale = history.Where(r => r.FinalHpScale > 0f).ToList();
        if (withScale.Count == 0) return 1f;
        return (float)withScale.Average(r => r.FinalHpScale);
    }
}
