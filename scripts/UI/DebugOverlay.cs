using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Score;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Overlay de debug toggleable avec F1.
/// Affiche en temps réel les stats de la run V2 + agrégats historiques.
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    private const float UpdateInterval = 0.5f;

    private PanelContainer _panel;
    private Label _leftColumn;
    private Label _centerColumn;
    private Label _rightColumn;
    private float _updateTimer;
    private bool _visible;

    private RunTracker _runTracker;
    private ScoreManager _scoreManager;
    private GameManager _gameManager;
    private ErasureManager _erasureManager;
    private Events.CrisisManager _crisisManager;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 100;

        BuildUI();
        _panel.Visible = false;
        _visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.F1)
        {
            _visible = !_visible;
            _panel.Visible = _visible;

            if (_visible)
                RefreshDisplay();

            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!_visible)
            return;

        _updateTimer -= (float)delta;
        if (_updateTimer > 0f)
            return;

        _updateTimer = UpdateInterval;
        RefreshDisplay();
    }

    private void BuildUI()
    {
        _panel = new PanelContainer();
        _panel.AnchorLeft = 0f;
        _panel.AnchorTop = 0f;
        _panel.AnchorRight = 1f;
        _panel.AnchorBottom = 0f;
        _panel.SizeFlagsHorizontal = Control.SizeFlags.Fill;

        StyleBoxFlat style = new();
        style.BgColor = new Color(0f, 0f, 0f, 0.7f);
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        _panel.AddThemeStyleboxOverride("panel", style);

        HBoxContainer hbox = new();
        hbox.AddThemeConstantOverride("separation", 40);
        _panel.AddChild(hbox);

        _leftColumn = CreateLabel();
        hbox.AddChild(_leftColumn);

        _centerColumn = CreateLabel();
        hbox.AddChild(_centerColumn);

        _rightColumn = CreateLabel();
        hbox.AddChild(_rightColumn);

        AddChild(_panel);
    }

    private Label CreateLabel()
    {
        Label label = new();
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", new Color(0.0f, 1.0f, 0.4f));
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return label;
    }

    private void EnsureReferences()
    {
        Node currentScene = GetTree().CurrentScene;
        if (currentScene == null)
            return;

        _runTracker ??= currentScene.GetNodeOrNull<RunTracker>("RunTracker");
        _scoreManager ??= currentScene.GetNodeOrNull<ScoreManager>("ScoreManager");
        _erasureManager ??= currentScene.GetNodeOrNull<ErasureManager>("ErasureManager");
        _crisisManager ??= currentScene.GetNodeOrNull<Events.CrisisManager>("CrisisManager");
        _gameManager ??= GetNodeOrNull<GameManager>("/root/GameManager");
    }

    private void RefreshDisplay()
    {
        EnsureReferences();
        UpdateLeftColumn();
        UpdateCenterColumn();
        UpdateRightColumn();
    }

    private void UpdateLeftColumn()
    {
        double fps = Engine.GetFramesPerSecond();

        string runPhase = _gameManager?.CurrentRunPhase.ToString() ?? "?";
        float erasurePercent = (_erasureManager?.GlobalErasurePercent ?? 0f) * 100f;
        string crisisState = _crisisManager switch
        {
            { IsCrisisActive: true } manager => $"ACTIVE #{manager.CrisisNumber} x{manager.CurrentIntensity}",
            { IsWarningActive: true } manager => $"WARN #{manager.CrisisNumber + 1} ({Mathf.CeilToInt(manager.TimeUntilNextCrisis)}s)",
            not null => $"Next in {Mathf.CeilToInt(_crisisManager.TimeUntilNextCrisis)}s",
            _ => "N/A"
        };

        int enemyCount = GetTree().GetNodesInGroup("enemies").Count;

        Player player = GetTree().GetFirstNodeInGroup("player") as Player;
        float hp = player?.CurrentHp ?? 0f;
        float maxHp = player?.EffectiveMaxHp ?? 1f;

        float dps = _runTracker?.RollingDps ?? 0f;
        float damagePerMin = _runTracker?.DamageTakenPerMinute ?? 0f;
        int score = _scoreManager?.CurrentScore ?? 0;
        float duration = _runTracker?.RunDurationSeconds ?? 0f;
        float scorePerMin = duration > 60f ? score / (duration / 60f) : 0f;
        int level = _runTracker?.MaxLevel ?? 0;
        int crises = _runTracker?.CrisesSurvived ?? 0;

        _leftColumn.Text =
            $"[DEBUG — F1]\n" +
            $"FPS: {fps:F0}\n" +
            $"Phase: {runPhase}\n" +
            $"Effacement: {erasurePercent:F0}%\n" +
            $"Résurgence: {crisisState}\n" +
            $"Crises survécues: {crises}\n" +
            $"Ennemis: {enemyCount}\n" +
            $"HP: {hp:F0}/{maxHp:F0} ({(hp / maxHp * 100f):F0}%)\n" +
            $"DPS (10s): {dps:F1}\n" +
            $"Dmg reçus/min: {damagePerMin:F1}\n" +
            $"Score: {score} ({scorePerMin:F0}/min)\n" +
            $"Niveau: {level}\n" +
            $"Durée: {FormatDuration(duration)}";
    }

    private void UpdateCenterColumn()
    {
        int activeEnemies = GetTree().GetNodesInGroup("enemies").Count;
        float spawnsPerMin = _runTracker?.SpawnsPerMinute ?? 0f;
        float killsPerMin = _runTracker?.KillsPerMinute ?? 0f;
        float pressure = _runTracker?.PressureRatio ?? 0f;
        int peakEnemies = _runTracker?.PeakEnemies ?? 0;
        int totalSpawned = _runTracker?.TotalSpawned ?? 0;
        int totalKilled = _runTracker?.TotalKilled ?? 0;
        float hpScale = _runTracker?.LastHpScale ?? 1f;
        float dmgScale = _runTracker?.LastDmgScale ?? 1f;

        string pressureIcon = pressure switch
        {
            < 0.8f => "<<",
            < 1.0f => "<",
            < 1.2f => "=",
            < 1.5f => ">",
            _ => ">>"
        };

        _centerColumn.Text =
            $"[DIFFICULTÉ]\n" +
            $"Actifs: {activeEnemies} (pic: {peakEnemies})\n" +
            $"Spawn/min: {spawnsPerMin:F0}\n" +
            $"Kill/min: {killsPerMin:F0}\n" +
            $"Pression: {pressure:F2} {pressureIcon}\n" +
            $"Total: {totalSpawned} spawn / {totalKilled} kill\n" +
            $"HP scale: x{hpScale:F2}\n" +
            $"DMG scale: x{dmgScale:F2}";
    }

    private void UpdateRightColumn()
    {
        RunHistoryManager.Load();
        List<RunRecord> history = RunHistoryManager.GetHistory();

        if (history.Count == 0)
        {
            _rightColumn.Text = "[HISTORIQUE]\nAucune run enregistrée.";
            return;
        }

        float avgDuration = RunAnalytics.GetAverageRunDuration();
        float avgCrises = RunAnalytics.GetAverageCrises();
        float avgPressure = RunAnalytics.GetAveragePressure();

        Dictionary<string, int> deathCauses = RunAnalytics.GetDeathCauseDistribution();
        string causesText = string.Join(", ",
            deathCauses.Take(3).Select(kv => $"{kv.Key}({kv.Value})"));
        if (string.IsNullOrEmpty(causesText))
            causesText = "N/A";

        Dictionary<string, int> perkRates = RunAnalytics.GetPerkPickRates();
        string perksText = string.Join(", ",
            perkRates.Take(5).Select(kv => $"{kv.Key}({kv.Value})"));
        if (string.IsNullOrEmpty(perksText))
            perksText = "N/A";

        List<int> trend = RunAnalytics.GetScoreTrend(5);
        string trendText = string.Join(" -> ", trend);
        float avgDps = RunAnalytics.GetAverageDps();

        _rightColumn.Text =
            $"[HISTORIQUE — {history.Count} runs]\n" +
            $"Durée moy.: {FormatDuration(avgDuration)}\n" +
            $"Crises moy.: {avgCrises:F1}\n" +
            $"Pression moy.: {avgPressure:F2}\n" +
            $"Causes de mort: {causesText}\n" +
            $"Top perks: {perksText}\n" +
            $"Scores récents: {trendText}\n" +
            $"DPS moyen: {avgDps:F1}";
    }

    private static string FormatDuration(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int min = totalSeconds / 60;
        int sec = totalSeconds % 60;
        return $"{min}:{sec:D2}";
    }
}
