using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Events;

/// <summary>
/// Orchestration du late game V2.
/// Déclenche le passage en late game, force l'apparition de l'Indicible,
/// puis bascule la run en endgame après sa mort. Sur les terres (plan 51 §16), c'est le dernier gardien vaincu qui gagne
/// la run et l'ouvre.
/// </summary>
public partial class EndgameManager : Node
{
	private float _lateGameErasureThreshold = 0.68f;
	private int _lateGameCrisisThreshold = 4;
	private int _bossCrisisThreshold = 5;
	private float _bossTimeThresholdSec = 30f * 60f;
	private float _bossHpScale = 3.2f;
	private float _bossDmgScale = 1.65f;

	private EventBus _eventBus;
	private GameManager _gameManager;
	private CrisisManager _crisisManager;
	private ErasureManager _erasureManager;
	private Player _player;

	private RunClock _clock;
	private bool _landsRun;
	private bool _lateGameReached;
	private bool _bossSpawned;
	private bool _bossDefeated;
	private bool _endgameReached;
	/// <summary>Raison pour laquelle le boss ne peut pas apparaître (réglages ou fiche invalides) ; null s'il le peut.</summary>
	private string _bossUnavailable;
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
		_clock = GetNode<RunClock>("../RunClock");
		// Sur les terres (plan 51), chaque terre a son gardien : l'Indicible ne vient pas à l'heure.
		_landsRun = GetParentOrNull<World.WorldSetup>()?.Land?.IsLand == true;
		_crisisManager = GetParent().GetNodeOrNull<CrisisManager>("CrisisManager");
		_erasureManager = GetParent().GetNodeOrNull<ErasureManager>("ErasureManager");

		_eventBus.EnemyKilled += OnEnemyKilled;
		_eventBus.CrisisStarted += OnCrisisStarted;
		_eventBus.GuardianDefeated += OnGuardianDefeated;

		// Contrôlés dès le début de la run : une faute de réglage du boss se voit au chargement, pas à 30 minutes.
		// Le reste de la run se joue ; seuls le boss et la fin de partie qu'il ouvre sont retirés.
		if (!IndicibleConfig.TryLoad(out _, out string error))
			_bossUnavailable = error;
		else if (!EnemyDataLoader.Exists(EnemyGrammar.FinalBossId))
			_bossUnavailable = "fiche data/enemies/indicible.json absente ou écartée";
		if (_bossUnavailable != null)
			GD.PushError($"[EndgameManager] L'Indicible n'apparaîtra pas pendant cette run : {_bossUnavailable}.");
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
		{
			_eventBus.EnemyKilled -= OnEnemyKilled;
			_eventBus.CrisisStarted -= OnCrisisStarted;
			_eventBus.GuardianDefeated -= OnGuardianDefeated;
		}
	}

	public override void _Process(double delta)
	{
		if (_gameManager == null || _gameManager.CurrentState != GameManager.GameState.Run)
			return;

		CachePlayer();
		UpdateLateGameState();

		if (!_bossSpawned && !_bossDefeated && _bossUnavailable == null && ShouldForceBoss())
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

		ReachLateGame();
		GD.Print("[EndgameManager] Late game reached");
	}

	private bool ShouldForceBoss()
	{
		if (_landsRun || _player == null || !IsInstanceValid(_player))
			return false;

		if (_crisisManager is { IsCrisisActive: true } && _crisisManager.CrisisNumber >= _bossCrisisThreshold)
			return true;

		return _clock.Seconds >= _bossTimeThresholdSec;
	}

	private void SpawnIndicible()
	{
		if (_player == null || !IsInstanceValid(_player))
			return;

		_indicible = new Indicible { Name = "IndicibleBoss" };
		GetParent().AddChild(_indicible);
		Spawn.SpawnManager spawner = GetParent().GetNode<Spawn.SpawnManager>("SpawnManager");
		if (!_indicible.Initialize(_bossHpScale * spawner.Difficulty.EnemyHp, _bossDmgScale * spawner.Difficulty.EnemyDamage, _player, spawner))
		{
			_indicible.QueueFree();
			_bossUnavailable = "initialisation refusée";
			return;
		}

		_bossSpawned = true;
		ReachLateGame();

		GD.Print("[EndgameManager] Indicible spawned");
	}

	private void OnCrisisStarted(int crisisNumber, int intensity)
	{
		if (_endgameReached || _bossDefeated)
			return;

		if (crisisNumber >= _lateGameCrisisThreshold)
			ReachLateGame();
	}

	/// <summary>
	/// Late game atteint, pour toute la suite de la run : la phase revient en late game à la fin de la Résurgence en
	/// cours, même si l'Effacement est sous son seuil (plan 26 Q8d, constat F08).
	/// </summary>
	private void ReachLateGame()
	{
		_lateGameReached = true;
		_gameManager?.ReportLateGame();
	}

	private void OnEnemyKilled(string enemyId, Vector2 position)
	{
		if (enemyId != EnemyGrammar.FinalBossId)
			return;

		_bossSpawned = false;
		ReachEndgame();
		GD.Print("[EndgameManager] Indicible defeated, endgame reached");
	}

	private void OnGuardianDefeated(int land, bool victory)
	{
		if (!victory || _bossDefeated)
			return;

		ReachLateGame();
		ReachEndgame();
		GD.Print($"[EndgameManager] Dernier gardien vaincu (terre {land}) : run gagnée, endgame atteint");
	}

	/// <summary>Le boss de la run est tombé : la run est gagnée, et ce qui suit se joue en endgame.</summary>
	private void ReachEndgame()
	{
		_bossDefeated = true;
		_endgameReached = true;
		_gameManager?.ReportEndgame();
		_crisisManager?.EnableEndgameTempo();
	}

	private void CachePlayer()
	{
		if (_player != null && IsInstanceValid(_player))
			return;

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
	}

	/// <summary>Réglages contrôlés par <see cref="CrisisConfig"/> (plan 26 Q7d-1).</summary>
	private void LoadConfig()
	{
		CrisisConfig config = CrisisConfig.Load();
		_lateGameErasureThreshold = config.LateGameErasureThreshold;
		_lateGameCrisisThreshold = config.LateGameCrisisThreshold;
		_bossCrisisThreshold = config.BossCrisisThreshold;
		_bossTimeThresholdSec = config.BossTimeThresholdSec;
		_bossHpScale = config.BossHpScale;
		_bossDmgScale = config.BossDmgScale;
	}
}
