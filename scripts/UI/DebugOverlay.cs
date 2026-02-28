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
/// Affiche en temps réel les stats de la run + agrégats historiques.
/// ProcessMode.Always pour fonctionner même quand le jeu est en pause.
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

	private EventBus _eventBus;
	private RunTracker _runTracker;
	private ScoreManager _scoreManager;
	private DayNightCycle _dayNightCycle;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Layer = 100;

		_eventBus = GetNode<EventBus>("/root/EventBus");

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
		if (!_visible) return;

		_updateTimer -= (float)delta;
		if (_updateTimer <= 0f)
		{
			_updateTimer = UpdateInterval;
			RefreshDisplay();
		}
	}

	private void BuildUI()
	{
		_panel = new PanelContainer();
		_panel.AnchorLeft = 0f;
		_panel.AnchorTop = 0f;
		_panel.AnchorRight = 1f;
		_panel.AnchorBottom = 0f;
		_panel.OffsetBottom = 0;
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
		label.AddThemeFontSizeOverride("font_size", 11);
		label.AddThemeColorOverride("font_color", new Color(0.0f, 1.0f, 0.4f));
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		return label;
	}

	private void EnsureReferences()
	{
		if (_runTracker == null)
			_runTracker = GetTree().CurrentScene.GetNodeOrNull<RunTracker>("RunTracker");
		if (_scoreManager == null)
			_scoreManager = GetTree().CurrentScene.GetNodeOrNull<ScoreManager>("ScoreManager");
		if (_dayNightCycle == null)
			_dayNightCycle = GetTree().CurrentScene.GetNodeOrNull<DayNightCycle>("DayNightCycle");
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

		string phase = _dayNightCycle?.CurrentPhase.ToString() ?? "?";
		int night = _dayNightCycle?.CurrentNight ?? 0;
		float phaseProgress = _dayNightCycle?.PhaseProgress ?? 0f;

		int enemyCount = GetTree().GetNodesInGroup("enemies").Count;

		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		float hp = player?.CurrentHp ?? 0f;
		float maxHp = player?.EffectiveMaxHp ?? 1f;

		float dps = _runTracker?.RollingDps ?? 0f;
		float dmgPerMin = _runTracker?.DamageTakenPerMinute ?? 0f;
		int score = _scoreManager?.CurrentScore ?? 0;
		float duration = _runTracker?.RunDurationSeconds ?? 0f;
		float scorePerMin = duration > 60f ? score / (duration / 60f) : 0f;
		int level = _runTracker?.MaxLevel ?? 0;

		_leftColumn.Text =
			$"[DEBUG — F1]\n" +
			$"FPS: {fps:F0}\n" +
			$"Phase: {phase} (N{night}) {phaseProgress * 100f:F0}%\n" +
			$"Ennemis: {enemyCount}\n" +
			$"HP: {hp:F0}/{maxHp:F0} ({(hp / maxHp * 100f):F0}%)\n" +
			$"DPS (10s): {dps:F1}\n" +
			$"Dmg reçus/min: {dmgPerMin:F1}\n" +
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
			< 0.8f => "<<",  // joueur domine
			< 1.0f => "<",   // joueur gère
			< 1.2f => "=",   // équilibre
			< 1.5f => ">",   // pression
			_ => ">>"        // submergé
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

		float avgNights = RunAnalytics.GetAverageNights();
		float avgDeathNight = RunAnalytics.GetAverageDeathNight();

		// Top 3 causes de mort
		Dictionary<string, int> deathCauses = RunAnalytics.GetDeathCauseDistribution();
		string causesText = string.Join(", ",
			deathCauses.Take(3).Select(kv => $"{kv.Key}({kv.Value})"));
		if (string.IsNullOrEmpty(causesText)) causesText = "N/A";

		// Top 5 perks
		Dictionary<string, int> perkRates = RunAnalytics.GetPerkPickRates();
		string perksText = string.Join(", ",
			perkRates.Take(5).Select(kv => $"{kv.Key}({kv.Value})"));
		if (string.IsNullOrEmpty(perksText)) perksText = "N/A";

		// Trend des 5 derniers scores
		List<int> trend = RunAnalytics.GetScoreTrend(5);
		string trendText = string.Join(" → ", trend);

		// DPS moyen
		float avgDps = RunAnalytics.GetAverageDps();

		_rightColumn.Text =
			$"[HISTORIQUE — {history.Count} runs]\n" +
			$"Nuit moy. survécue: {avgNights:F1}\n" +
			$"Nuit moy. de mort: {avgDeathNight:F1}\n" +
			$"Causes de mort: {causesText}\n" +
			$"Top perks: {perksText}\n" +
			$"Scores récents: {trendText}\n" +
			$"DPS moyen: {avgDps:F1}";
	}

	private static string FormatDuration(float seconds)
	{
		int min = (int)(seconds / 60f);
		int sec = (int)(seconds % 60f);
		return $"{min}:{sec:D2}";
	}
}
