using System.Collections.Generic;
using System.Linq;

namespace Vestiges.Infrastructure;

/// <summary>
/// Agrégation de métriques depuis l'historique des runs.
/// Utilisé par le DebugOverlay et potentiellement le Hub pour afficher des tendances.
/// </summary>
public static class RunAnalytics
{
    public class CharacterRunStats
    {
        public float AvgScore { get; set; }
        public float AvgNights { get; set; }
        public int RunCount { get; set; }
        public int BestScore { get; set; }
    }

    public static float GetAverageNights()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        if (history.Count == 0) return 0f;
        return (float)history.Average(r => r.NightsSurvived);
    }

    public static float GetAverageScore()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        if (history.Count == 0) return 0f;
        return (float)history.Average(r => r.Score);
    }

    /// <summary>Distribution des nuits de mort (nuit → nombre de morts à cette nuit).</summary>
    public static Dictionary<int, int> GetDeathNightDistribution()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        Dictionary<int, int> distribution = new();
        foreach (RunRecord run in history)
        {
            int night = run.DeathNight;
            distribution[night] = distribution.GetValueOrDefault(night, 0) + 1;
        }
        return distribution;
    }

    /// <summary>Distribution des causes de mort (cause → nombre).</summary>
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

    /// <summary>Taux de sélection des perks (perkId → nombre de fois choisi), trié descendant.</summary>
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

    /// <summary>Stats par personnage.</summary>
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
                AvgNights = (float)group.Value.Average(r => r.NightsSurvived),
                RunCount = group.Value.Count,
                BestScore = group.Value.Max(r => r.Score)
            };
        }
        return stats;
    }

    /// <summary>Derniers N scores (du plus récent au plus ancien).</summary>
    public static List<int> GetScoreTrend(int count = 10)
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        return history.Take(count).Select(r => r.Score).ToList();
    }

    /// <summary>DPS moyen sur l'ensemble des runs (total_damage_dealt / run_duration_sec).</summary>
    public static float GetAverageDps()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withDuration = history.Where(r => r.RunDurationSec > 0f).ToList();
        if (withDuration.Count == 0) return 0f;
        return (float)withDuration.Average(r => r.TotalDamageDealt / r.RunDurationSec);
    }

    /// <summary>Nuit moyenne de mort.</summary>
    public static float GetAverageDeathNight()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withDeathNight = history.Where(r => r.DeathNight > 0).ToList();
        if (withDeathNight.Count == 0) return 0f;
        return (float)withDeathNight.Average(r => r.DeathNight);
    }

    /// <summary>Taux de survie par nuit (nuit → % de runs qui ont dépassé cette nuit).</summary>
    public static Dictionary<int, float> GetSurvivalRateByNight()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        if (history.Count == 0) return new Dictionary<int, float>();

        int maxNight = history.Max(r => r.NightsSurvived);
        Dictionary<int, float> rates = new();
        for (int n = 1; n <= maxNight; n++)
        {
            int survived = history.Count(r => r.NightsSurvived >= n);
            rates[n] = (float)survived / history.Count * 100f;
        }
        return rates;
    }

    /// <summary>Pression moyenne à la mort sur l'ensemble des runs.</summary>
    public static float GetAveragePressure()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withPressure = history.Where(r => r.AvgPressure > 0f).ToList();
        if (withPressure.Count == 0) return 0f;
        return (float)withPressure.Average(r => r.AvgPressure);
    }

    /// <summary>Pic d'ennemis moyen par run.</summary>
    public static float GetAveragePeakEnemies()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withPeak = history.Where(r => r.PeakEnemies > 0).ToList();
        if (withPeak.Count == 0) return 0f;
        return (float)withPeak.Average(r => r.PeakEnemies);
    }

    /// <summary>Kill rate moyen (kills / spawns) — efficacité du joueur.</summary>
    public static float GetAverageKillEfficiency()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withSpawns = history.Where(r => r.TotalSpawned > 0).ToList();
        if (withSpawns.Count == 0) return 0f;
        return (float)withSpawns.Average(r => (float)r.TotalKills / r.TotalSpawned);
    }

    /// <summary>Scaling HP moyen au moment de la mort — montre combien les ennemis sont buff.</summary>
    public static float GetAverageFinalHpScale()
    {
        List<RunRecord> history = RunHistoryManager.GetHistory();
        List<RunRecord> withScale = history.Where(r => r.FinalHpScale > 0f).ToList();
        if (withScale.Count == 0) return 1f;
        return (float)withScale.Average(r => r.FinalHpScale);
    }
}
