using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation;

/// <summary>
/// Génère un rapport comparatif à partir des résultats d'un batch de simulation.
/// Produit un résumé console + un fichier JSON détaillé dans user://simulation_results/.
/// </summary>
public class SimulationReport
{
    private readonly SimulationBatchConfig _config;
    private readonly List<List<RunRecord>> _results;

    public SimulationReport(SimulationBatchConfig config, List<List<RunRecord>> results)
    {
        _config = config;
        _results = results;
    }

    public class ConfigSummary
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("run_count")]
        public int RunCount { get; set; }

        [JsonPropertyName("avg_nights_survived")]
        public float AvgNightsSurvived { get; set; }

        [JsonPropertyName("median_nights_survived")]
        public float MedianNightsSurvived { get; set; }

        [JsonPropertyName("max_nights_survived")]
        public int MaxNightsSurvived { get; set; }

        [JsonPropertyName("min_nights_survived")]
        public int MinNightsSurvived { get; set; }

        [JsonPropertyName("avg_score")]
        public float AvgScore { get; set; }

        [JsonPropertyName("max_score")]
        public int MaxScore { get; set; }

        [JsonPropertyName("min_score")]
        public int MinScore { get; set; }

        [JsonPropertyName("avg_kills")]
        public float AvgKills { get; set; }

        [JsonPropertyName("avg_dps")]
        public float AvgDps { get; set; }

        [JsonPropertyName("avg_damage_taken")]
        public float AvgDamageTaken { get; set; }

        [JsonPropertyName("avg_final_hp_scale")]
        public float AvgFinalHpScale { get; set; }

        [JsonPropertyName("avg_final_dmg_scale")]
        public float AvgFinalDmgScale { get; set; }

        [JsonPropertyName("avg_pressure")]
        public float AvgPressure { get; set; }

        [JsonPropertyName("avg_peak_enemies")]
        public float AvgPeakEnemies { get; set; }

        [JsonPropertyName("avg_run_duration_sec")]
        public float AvgRunDurationSec { get; set; }

        [JsonPropertyName("death_night_distribution")]
        public Dictionary<int, int> DeathNightDistribution { get; set; } = new();

        [JsonPropertyName("death_cause_distribution")]
        public Dictionary<string, int> DeathCauseDistribution { get; set; } = new();

        [JsonPropertyName("perk_pick_rates")]
        public Dictionary<string, int> PerkPickRates { get; set; } = new();
    }

    public void PrintSummary()
    {
        List<ConfigSummary> summaries = BuildSummaries();

        GD.Print("\n==========================================");
        GD.Print($"  SIMULATION REPORT: {_config.Name}");
        GD.Print($"  {summaries.Sum(s => s.RunCount)} total runs across {summaries.Count} config(s)");
        GD.Print("==========================================\n");

        foreach (ConfigSummary s in summaries)
        {
            GD.Print($"--- {s.Label} ({s.RunCount} runs) ---");
            GD.Print($"  Nights:   avg={s.AvgNightsSurvived:F1}, median={s.MedianNightsSurvived:F1}, range=[{s.MinNightsSurvived}-{s.MaxNightsSurvived}]");
            GD.Print($"  Score:    avg={s.AvgScore:F0}, range=[{s.MinScore}-{s.MaxScore}]");
            GD.Print($"  Kills:    avg={s.AvgKills:F0}");
            GD.Print($"  DPS:      avg={s.AvgDps:F1}");
            GD.Print($"  Pressure: avg={s.AvgPressure:F2}");
            GD.Print($"  HP scale: x{s.AvgFinalHpScale:F2}");
            GD.Print($"  Duration: avg={s.AvgRunDurationSec / 60f:F1} min");
            GD.Print($"  Deaths:   {FormatDistribution(s.DeathNightDistribution)}");
            GD.Print($"  Causes:   {FormatStringDistribution(s.DeathCauseDistribution, 3)}");
            GD.Print($"  Top perks:{FormatStringDistribution(s.PerkPickRates, 5)}");
            GD.Print();
        }

        if (summaries.Count >= 2)
        {
            GD.Print("--- COMPARISON (vs first config) ---");
            ConfigSummary baseline = summaries[0];
            for (int i = 1; i < summaries.Count; i++)
            {
                ConfigSummary comp = summaries[i];
                float nightsDiff = comp.AvgNightsSurvived - baseline.AvgNightsSurvived;
                float scoreDiff = comp.AvgScore - baseline.AvgScore;
                float pressureDiff = comp.AvgPressure - baseline.AvgPressure;
                GD.Print($"  {comp.Label}: nights {nightsDiff:+0.0;-0.0}, " +
                         $"score {scoreDiff:+0;-0}, pressure {pressureDiff:+0.00;-0.00}");
            }
            GD.Print();
        }
    }

    public void SaveToFile()
    {
        DirAccess dir = DirAccess.Open("user://");
        if (dir != null && !DirAccess.DirExistsAbsolute("user://simulation_results"))
            dir.MakeDir("simulation_results");

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"user://simulation_results/{_config.Name}_{timestamp}.json";

        List<ConfigSummary> summaries = BuildSummaries();

        ReportOutput output = new()
        {
            BatchName = _config.Name,
            Timestamp = timestamp,
            TotalRuns = _results.Sum(r => r.Count),
            RunsPerConfig = _config.RunsPerConfig,
            Summaries = summaries
        };

        JsonSerializerOptions options = new() { WriteIndented = true };
        string json = JsonSerializer.Serialize(output, options);

        FileAccess file = FileAccess.Open(filename, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError($"[SimulationReport] Cannot write to {filename}");
            return;
        }

        file.StoreString(json);
        file.Close();

        string absolutePath = ProjectSettings.GlobalizePath(filename);
        if (string.IsNullOrWhiteSpace(absolutePath))
            absolutePath = filename;

        GD.Print($"[SimulationReport] Saved to {absolutePath}");
    }

    private List<ConfigSummary> BuildSummaries()
    {
        List<ConfigSummary> summaries = new();

        for (int i = 0; i < _results.Count && i < _config.Configs.Count; i++)
        {
            List<RunRecord> records = _results[i];
            SimulationRunConfig config = _config.Configs[i];

            if (records.Count == 0)
            {
                summaries.Add(new ConfigSummary { Label = config.Label, RunCount = 0 });
                continue;
            }

            List<int> nights = records.Select(r => r.NightsSurvived).OrderBy(n => n).ToList();

            ConfigSummary summary = new()
            {
                Label = config.Label,
                RunCount = records.Count,
                AvgNightsSurvived = (float)records.Average(r => r.NightsSurvived),
                MedianNightsSurvived = GetMedian(nights),
                MaxNightsSurvived = records.Max(r => r.NightsSurvived),
                MinNightsSurvived = records.Min(r => r.NightsSurvived),
                AvgScore = (float)records.Average(r => r.Score),
                MaxScore = records.Max(r => r.Score),
                MinScore = records.Min(r => r.Score),
                AvgKills = (float)records.Average(r => r.TotalKills),
                AvgDps = records.Where(r => r.RunDurationSec > 0).Select(r => r.TotalDamageDealt / r.RunDurationSec).DefaultIfEmpty(0f).Average(),
                AvgDamageTaken = (float)records.Average(r => r.TotalDamageTaken),
                AvgFinalHpScale = records.Where(r => r.FinalHpScale > 0).Select(r => r.FinalHpScale).DefaultIfEmpty(1f).Average(),
                AvgFinalDmgScale = records.Where(r => r.FinalDmgScale > 0).Select(r => r.FinalDmgScale).DefaultIfEmpty(1f).Average(),
                AvgPressure = records.Where(r => r.AvgPressure > 0).Select(r => r.AvgPressure).DefaultIfEmpty(0f).Average(),
                AvgPeakEnemies = (float)records.Average(r => r.PeakEnemies),
                AvgRunDurationSec = (float)records.Average(r => r.RunDurationSec),
                DeathNightDistribution = BuildIntDistribution(records, r => r.DeathNight),
                DeathCauseDistribution = BuildStringDistribution(records, r => r.DeathCause ?? "unknown"),
                PerkPickRates = BuildPerkPickRates(records)
            };

            summaries.Add(summary);
        }

        return summaries;
    }

    private static float GetMedian(List<int> sorted)
    {
        if (sorted.Count == 0) return 0f;
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2f
            : sorted[mid];
    }

    private static Dictionary<int, int> BuildIntDistribution(List<RunRecord> records, Func<RunRecord, int> selector)
    {
        Dictionary<int, int> dist = new();
        foreach (RunRecord r in records)
        {
            int key = selector(r);
            dist[key] = dist.GetValueOrDefault(key, 0) + 1;
        }
        return dist.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static Dictionary<string, int> BuildStringDistribution(List<RunRecord> records, Func<RunRecord, string> selector)
    {
        Dictionary<string, int> dist = new();
        foreach (RunRecord r in records)
        {
            string key = selector(r);
            dist[key] = dist.GetValueOrDefault(key, 0) + 1;
        }
        return dist.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static Dictionary<string, int> BuildPerkPickRates(List<RunRecord> records)
    {
        Dictionary<string, int> rates = new();
        foreach (RunRecord r in records)
        {
            if (r.PerkIds == null) continue;
            foreach (string perkId in r.PerkIds)
                rates[perkId] = rates.GetValueOrDefault(perkId, 0) + 1;
        }
        return rates.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static string FormatDistribution(Dictionary<int, int> dist)
    {
        if (dist == null || dist.Count == 0) return "N/A";
        return string.Join(", ", dist.Select(kv => $"N{kv.Key}({kv.Value})"));
    }

    private static string FormatStringDistribution(Dictionary<string, int> dist, int max)
    {
        if (dist == null || dist.Count == 0) return "N/A";
        return string.Join(", ", dist.Take(max).Select(kv => $"{kv.Key}({kv.Value})"));
    }

    private class ReportOutput
    {
        [JsonPropertyName("batch_name")]
        public string BatchName { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("total_runs")]
        public int TotalRuns { get; set; }

        [JsonPropertyName("runs_per_config")]
        public int RunsPerConfig { get; set; }

        [JsonPropertyName("summaries")]
        public List<ConfigSummary> Summaries { get; set; }
    }
}
