using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.Events;

/// <summary>
/// Orchestration du late game V2.
/// Déclenche le passage en late game, force l'apparition de l'Indicible,
/// puis bascule la run en endgame après sa mort.
/// </summary>
public partial class EndgameManager : Node
{
	private float _lateGameErasureThreshold = 0.68f;
	private int _lateGameCrisisThreshold = 4;
	private int _bossCrisisThreshold = 5;
	private float _bossTimeThresholdSec = 22f * 60f;
	private float _bossHpScale = 3.2f;
	private float _bossDmgScale = 1.65f;

	private EventBus _eventBus;
	private GameManager _gameManager;
	private CrisisManager _crisisManager;
	private ErasureManager _erasureManager;
	private Player _player;

	private float _elapsed;
	private bool _lateGameReached;
	private bool _bossSpawned;
	private bool _bossDefeated;
	private bool _endgameReached;
	private Indicible _indicible;

	public bool IsLateGameReached => _lateGameReached;
	public bool IsBossSpawned => _bossSpawned;
	public bool IsBossDefeated => _bossDefeated;
	public bool IsEndgameReached => _endgameReached;

	public override void _Ready()
	{
		LoadConfig();

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_crisisManager = GetParent().GetNodeOrNull<CrisisManager>("CrisisManager");
		_erasureManager = GetParent().GetNodeOrNull<ErasureManager>("ErasureManager");

		_eventBus.EnemyKilled += OnEnemyKilled;
		_eventBus.CrisisStarted += OnCrisisStarted;
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
		{
			_eventBus.EnemyKilled -= OnEnemyKilled;
			_eventBus.CrisisStarted -= OnCrisisStarted;
		}
	}

	public override void _Process(double delta)
	{
		if (_gameManager == null || _gameManager.CurrentState != GameManager.GameState.Run)
			return;

		_elapsed += (float)delta;
		CachePlayer();
		UpdateLateGameState();

		if (!_bossSpawned && !_bossDefeated && ShouldForceBoss())
			SpawnIndicible();
	}

	private void UpdateLateGameState()
	{
		if (_lateGameReached)
			return;

		bool erasureReached = _erasureManager != null && _erasureManager.GlobalErasurePercent >= _lateGameErasureThreshold;
		bool crisisReached = _crisisManager != null && _crisisManager.CrisisNumber >= _lateGameCrisisThreshold;
		if (!erasureReached && !crisisReached)
			return;

		_lateGameReached = true;
		if (_gameManager.CurrentRunPhase == GameManager.RunPhase.Exploration)
			_gameManager.SetRunPhase(GameManager.RunPhase.LateGame);

		GD.Print("[EndgameManager] Late game reached");
	}

	private bool ShouldForceBoss()
	{
		if (_player == null || !IsInstanceValid(_player))
			return false;

		if (_crisisManager is { IsCrisisActive: true } && _crisisManager.CrisisNumber >= _bossCrisisThreshold)
			return true;

		return _elapsed >= _bossTimeThresholdSec;
	}

	private void SpawnIndicible()
	{
		if (_player == null || !IsInstanceValid(_player))
			return;

		_indicible = new Indicible { Name = "IndicibleBoss" };
		GetParent().AddChild(_indicible);
		_indicible.Initialize(_bossHpScale, _bossDmgScale, _player.GlobalPosition);

		_bossSpawned = true;
		_lateGameReached = true;
		if (_gameManager.CurrentRunPhase != GameManager.RunPhase.Endgame)
			_gameManager.SetRunPhase(GameManager.RunPhase.LateGame);

		GD.Print("[EndgameManager] Indicible spawned");
	}

	private void OnCrisisStarted(int crisisNumber, int intensity)
	{
		if (_endgameReached || _bossDefeated)
			return;

		if (crisisNumber >= _lateGameCrisisThreshold)
			_lateGameReached = true;
	}

	private void OnEnemyKilled(string enemyId, Vector2 position)
	{
		if (enemyId != "indicible")
			return;

		_bossDefeated = true;
		_bossSpawned = false;
		_endgameReached = true;
		_gameManager?.SetRunPhase(GameManager.RunPhase.Endgame);
		_crisisManager?.EnableEndgameTempo();
		GD.Print("[EndgameManager] Indicible defeated, endgame reached");
	}

	private void CachePlayer()
	{
		if (_player != null && IsInstanceValid(_player))
			return;

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
	}

	private void LoadConfig()
	{
		FileAccess file = FileAccess.Open("res://data/scaling/crises.json", FileAccess.ModeFlags.Read);
		if (file == null)
			return;

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
			return;

		Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
		_lateGameErasureThreshold = dict.ContainsKey("late_game_erasure_threshold")
			? (float)dict["late_game_erasure_threshold"].AsDouble()
			: _lateGameErasureThreshold;
		_lateGameCrisisThreshold = dict.ContainsKey("late_game_crisis_threshold")
			? (int)dict["late_game_crisis_threshold"].AsDouble()
			: _lateGameCrisisThreshold;
		_bossCrisisThreshold = dict.ContainsKey("boss_crisis_threshold")
			? (int)dict["boss_crisis_threshold"].AsDouble()
			: _bossCrisisThreshold;
		_bossTimeThresholdSec = dict.ContainsKey("boss_time_threshold_sec")
			? (float)dict["boss_time_threshold_sec"].AsDouble()
			: _bossTimeThresholdSec;
		_bossHpScale = dict.ContainsKey("boss_hp_scale")
			? (float)dict["boss_hp_scale"].AsDouble()
			: _bossHpScale;
		_bossDmgScale = dict.ContainsKey("boss_dmg_scale")
			? (float)dict["boss_dmg_scale"].AsDouble()
			: _bossDmgScale;
	}
}
