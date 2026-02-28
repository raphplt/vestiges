using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation.MathSim;

/// <summary>
/// Orchestrateur de batch pour la simulation mathématique.
/// Exécute les runs en parallèle et produit un rapport via SimulationReport.
/// </summary>
public static class MathSimBatchRunner
{
    public static void RunBatch(string configPath)
    {
        SimulationBatchConfig config = SimulationBatchConfig.LoadFromFile(configPath);
        if (config == null)
        {
            GD.PushError($"[MathSim] Failed to load config from {configPath}");
            return;
        }

        RunBatch(config);
    }

    public static void RunBatch(SimulationBatchConfig config)
    {
        // Ensure data loaders are ready (main thread only — Godot FileAccess not thread-safe)
        CharacterDataLoader.Load();
        WeaponDataLoader.Load();
        PerkDataLoader.Load();
        EnemyDataLoader.Load();
        SimCombatModel.InitStaticData();

        Stopwatch sw = Stopwatch.StartNew();

        int totalRuns = config.Configs.Count * config.RunsPerConfig;
        GD.Print($"[MathSim] === BATCH STARTED: {config.Name} ===");
        GD.Print($"[MathSim] {config.Configs.Count} config(s), {config.RunsPerConfig} run(s) each = {totalRuns} total runs");

        // Master seed
        int masterSeed = System.Environment.TickCount;

        List<List<RunRecord>> allResults = new();

        for (int configIdx = 0; configIdx < config.Configs.Count; configIdx++)
        {
            SimulationRunConfig runConfig = config.Configs[configIdx];
            RunRecord[] results = new RunRecord[config.RunsPerConfig];

            int capturedConfigIdx = configIdx;

            // Parallel execution — each run is independent
            Parallel.For(0, config.RunsPerConfig, runIdx =>
            {
                int seed = masterSeed + capturedConfigIdx * 10000 + runIdx;
                Random rng = new(seed);

                MathSimEngine engine = new(runConfig, rng);
                results[runIdx] = engine.Run();
                results[runIdx].Seed = (ulong)seed;
            });

            allResults.Add(new List<RunRecord>(results));

            // Summary for this config
            float avgNights = results.Length > 0
                ? (float)results.Average(r => r.NightsSurvived)
                : 0f;
            float avgScore = results.Length > 0
                ? (float)results.Average(r => r.Score)
                : 0f;

            GD.Print($"[MathSim] Config '{runConfig.Label}': {config.RunsPerConfig} runs, " +
                     $"avg nights={avgNights:F1}, avg score={avgScore:F0}");
        }

        sw.Stop();

        // Generate report using existing SimulationReport
        SimulationReport report = new(config, allResults);
        report.PrintSummary();
        report.SaveToFile();

        float runsPerSec = totalRuns / (float)Math.Max(1, sw.ElapsedMilliseconds) * 1000f;
        GD.Print($"[MathSim] === BATCH COMPLETE in {sw.ElapsedMilliseconds}ms ({runsPerSec:F0} runs/sec) ===");
    }
}
