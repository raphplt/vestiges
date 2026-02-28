using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation;

/// <summary>
/// Orchestrateur de simulation multi-runs.
/// Persiste en tant qu'enfant de /root/ pour survivre aux changements de scène.
/// Itère les configs, collecte les RunRecords, génère le rapport final.
/// </summary>
public partial class BatchRunner : Node
{
    [Signal]
    public delegate void BatchCompletedEventHandler();

    private SimulationBatchConfig _batchConfig;
    private int _configIndex;
    private int _runIndex;
    private readonly List<List<RunRecord>> _allResults = new();
    private List<RunRecord> _currentConfigResults = new();
    private bool _isRunning;

    public SimulationRunConfig CurrentRunConfig =>
        _configIndex < _batchConfig.Configs.Count ? _batchConfig.Configs[_configIndex] : null;

    public bool IsRunning => _isRunning;
    public int CurrentConfigIndex => _configIndex;
    public int CurrentRunIndex => _runIndex;
    public int TotalConfigs => _batchConfig?.Configs.Count ?? 0;
    public int RunsPerConfig => _batchConfig?.RunsPerConfig ?? 0;

    /// <summary>
    /// Point d'entrée statique pour lancer un batch de simulation.
    /// Crée le BatchRunner, l'ajoute à /root/, et charge la scène principale.
    /// </summary>
    public static void StartBatch(string configPath)
    {
        SimulationBatchConfig config = SimulationBatchConfig.LoadFromFile(configPath);
        if (config == null)
        {
            GD.PushError($"[BatchRunner] Failed to load config from {configPath}");
            return;
        }

        StartBatch(config);
    }

    /// <summary>Lance un batch depuis une config déjà chargée.</summary>
    public static void StartBatch(SimulationBatchConfig config)
    {
        SceneTree tree = (SceneTree)Engine.GetMainLoop();

        // Nettoyer un éventuel BatchRunner précédent
        Node existing = tree.Root.GetNodeOrNull("BatchRunner");
        existing?.QueueFree();

        BatchRunner runner = new() { Name = "BatchRunner" };
        tree.Root.AddChild(runner);
        runner.Initialize(config);

        // Charger la scène de jeu (GameBootstrap détectera le BatchRunner)
        tree.ChangeSceneToFile("res://scenes/Main.tscn");
    }

    private void Initialize(SimulationBatchConfig config)
    {
        _batchConfig = config;
        _configIndex = 0;
        _runIndex = 0;
        _isRunning = true;
        _currentConfigResults = new List<RunRecord>();
        ProcessMode = ProcessModeEnum.Always;

        GD.Print($"[BatchRunner] === BATCH STARTED: {config.Name} ===");
        GD.Print($"[BatchRunner] {config.Configs.Count} config(s), {config.RunsPerConfig} run(s) each = {config.Configs.Count * config.RunsPerConfig} total runs");
    }

    /// <summary>Appelé par AIController quand une run se termine.</summary>
    public void OnRunCompleted(RunRecord record)
    {
        if (!_isRunning) return;

        _currentConfigResults.Add(record);
        _runIndex++;

        SimulationRunConfig config = CurrentRunConfig;
        GD.Print($"[BatchRunner] Run {_runIndex}/{_batchConfig.RunsPerConfig} " +
                 $"for '{config?.Label ?? "?"}' — Night {record.NightsSurvived}, " +
                 $"Score {record.Score}, Kills {record.TotalKills}");

        if (_runIndex >= _batchConfig.RunsPerConfig)
        {
            _allResults.Add(new List<RunRecord>(_currentConfigResults));
            _currentConfigResults.Clear();
            _configIndex++;
            _runIndex = 0;

            if (_configIndex >= _batchConfig.Configs.Count)
            {
                FinalizeBatch();
                return;
            }

            GD.Print($"[BatchRunner] Config '{_batchConfig.Configs[_configIndex].Label}' starting...");
        }

        CallDeferred(MethodName.StartNextRun);
    }

    private void StartNextRun()
    {
        Engine.TimeScale = 1.0;
        GetTree().Paused = false;

        // Reset static state that persists across scene reloads
        RunHistoryManager.ForceReload();

        GetTree().ReloadCurrentScene();
    }

    private void FinalizeBatch()
    {
        _isRunning = false;
        Engine.TimeScale = 1.0;

        SimulationReport report = new(_batchConfig, _allResults);
        report.PrintSummary();
        report.SaveToFile();

        EmitSignal(SignalName.BatchCompleted);

        GD.Print("[BatchRunner] === BATCH COMPLETE ===");

        // Nettoyage et retour au Hub
        GetTree().Paused = false;
        CallDeferred(MethodName.CleanupAndReturn);
    }

    private void CleanupAndReturn()
    {
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
        QueueFree();
    }
}
