using Godot;
using Steamworks;
using Vestiges.Core;

namespace Vestiges.Infrastructure.Steam;

/// <summary>
/// Suivi automatique des achievements Steam via EventBus.
/// Chaque achievement est défini par un ID Steam (à configurer dans Steamworks App Admin).
/// Le tracking se fait en écoutant les signaux existants — zéro couplage avec les systèmes de gameplay.
/// </summary>
public partial class SteamAchievements : Node
{
	// --- Achievement IDs (à configurer dans Steamworks App Admin) ---
	// Survie
	public const string SurviveNight1 = "ACH_SURVIVE_NIGHT_1";
	public const string SurviveNight3 = "ACH_SURVIVE_NIGHT_3";
	public const string SurviveNight5 = "ACH_SURVIVE_NIGHT_5";
	public const string SurviveNight10 = "ACH_SURVIVE_NIGHT_10";

	// Combat
	public const string Kill100 = "ACH_KILL_100";
	public const string Kill500 = "ACH_KILL_500";
	public const string Kill1000 = "ACH_KILL_1000";
	public const string KillColosse = "ACH_KILL_COLOSSE";
	public const string KillIndicible = "ACH_KILL_INDICIBLE";
	public const string NoDamageNight = "ACH_NO_DAMAGE_NIGHT";

	// Exploration
	public const string Explore10Pois = "ACH_EXPLORE_10_POIS";
	public const string DiscoverAllBiomes = "ACH_DISCOVER_ALL_BIOMES";
	public const string Open50Chests = "ACH_OPEN_50_CHESTS";

	// Construction
	public const string Place50Structures = "ACH_PLACE_50_STRUCTURES";
	public const string SurviveNightAllStructures = "ACH_ALL_STRUCTURES_SURVIVE";

	// Personnages
	public const string UnlockForgeuse = "ACH_UNLOCK_FORGEUSE";
	public const string UnlockTraqueur = "ACH_UNLOCK_TRAQUEUR";
	public const string PlayAllCharacters = "ACH_PLAY_ALL_CHARACTERS";

	// Score
	public const string Score10000 = "ACH_SCORE_10000";
	public const string Score50000 = "ACH_SCORE_50000";
	public const string Score100000 = "ACH_SCORE_100000";

	// Lore
	public const string FirstSouvenir = "ACH_FIRST_SOUVENIR";
	public const string CompleteConstellation = "ACH_COMPLETE_CONSTELLATION";

	// --- Stat IDs pour les compteurs Steam ---
	public const string StatTotalKills = "STAT_TOTAL_KILLS";
	public const string StatTotalRuns = "STAT_TOTAL_RUNS";
	public const string StatTotalPois = "STAT_TOTAL_POIS";
	public const string StatTotalChests = "STAT_TOTAL_CHESTS";
	public const string StatTotalStructures = "STAT_TOTAL_STRUCTURES";
	public const string StatMaxNightsSurvived = "STAT_MAX_NIGHTS";
	public const string StatBestScore = "STAT_BEST_SCORE";

	// --- Compteurs de session ---
	private int _sessionKills;
	private int _sessionPois;
	private int _sessionChests;
	private int _sessionStructures;
	private int _sessionNightsSurvived;
	private bool _tookDamageThisNight;
	private int _sessionScore;

	private EventBus _eventBus;

	public override void _Ready()
	{
		if (!SteamManager.IsActive)
			return;

		_eventBus = GetNode<EventBus>("/root/EventBus");

		// Combat
		_eventBus.EnemyKilled += OnEnemyKilled;
		_eventBus.PlayerDamaged += OnPlayerDamaged;

		// Cycle
		_eventBus.DayPhaseChanged += OnDayPhaseChanged;

		// Exploration
		_eventBus.PoiExplored += OnPoiExplored;
		_eventBus.ChestOpened += OnChestOpened;
		_eventBus.ZoneDiscovered += OnZoneDiscovered;

		// Lore
		_eventBus.SouvenirDiscovered += OnSouvenirDiscovered;

		// Score
		_eventBus.ScoreChanged += OnScoreChanged;

		// Chargement des stats Steam accumulées
		SteamUserStats.RequestCurrentStats();
	}

	public override void _ExitTree()
	{
		if (_eventBus == null)
			return;

		_eventBus.EnemyKilled -= OnEnemyKilled;
		_eventBus.PlayerDamaged -= OnPlayerDamaged;
		_eventBus.DayPhaseChanged -= OnDayPhaseChanged;
		_eventBus.PoiExplored -= OnPoiExplored;
		_eventBus.ChestOpened -= OnChestOpened;
		_eventBus.ZoneDiscovered -= OnZoneDiscovered;
		_eventBus.SouvenirDiscovered -= OnSouvenirDiscovered;
		_eventBus.ScoreChanged -= OnScoreChanged;
	}

	/// <summary>
	/// Appelé en fin de run par ScoreManager.SaveEndOfRun() pour flush les stats et
	/// vérifier les achievements basés sur les cumuls cross-run.
	/// </summary>
	public void OnRunEnd(int finalScore, int nightsSurvived, string characterId)
	{
		if (!SteamManager.IsActive)
			return;

		// Mise à jour des stats cumulées Steam
		IncrementStat(StatTotalKills, _sessionKills);
		IncrementStat(StatTotalRuns, 1);
		IncrementStat(StatTotalPois, _sessionPois);
		IncrementStat(StatTotalChests, _sessionChests);
		IncrementStat(StatTotalStructures, _sessionStructures);
		SetStatIfHigher(StatMaxNightsSurvived, nightsSurvived);
		SetStatIfHigher(StatBestScore, finalScore);

		// Achievements de survie
		if (nightsSurvived >= 1) TryUnlock(SurviveNight1);
		if (nightsSurvived >= 3) TryUnlock(SurviveNight3);
		if (nightsSurvived >= 5) TryUnlock(SurviveNight5);
		if (nightsSurvived >= 10) TryUnlock(SurviveNight10);

		// Achievements de score
		if (finalScore >= 10_000) TryUnlock(Score10000);
		if (finalScore >= 50_000) TryUnlock(Score50000);
		if (finalScore >= 100_000) TryUnlock(Score100000);

		// Achievements de cumul (basés sur les stats Steam)
		CheckCumulativeAchievements();

		SteamUserStats.StoreStats();

		// Reset compteurs de session
		_sessionKills = 0;
		_sessionPois = 0;
		_sessionChests = 0;
		_sessionStructures = 0;
		_sessionNightsSurvived = 0;
		_sessionScore = 0;
	}

	/// <summary>Signale un unlock de personnage (appelé par MetaSaveManager).</summary>
	public void OnCharacterUnlocked(string characterId)
	{
		if (!SteamManager.IsActive)
			return;

		switch (characterId)
		{
			case "forgeuse":
				TryUnlock(UnlockForgeuse);
				break;
			case "traqueur":
				TryUnlock(UnlockTraqueur);
				break;
		}
	}

	private void OnEnemyKilled(string enemyId, Vector2 position)
	{
		_sessionKills++;

		if (enemyId.StartsWith("colosse")) TryUnlock(KillColosse);
		if (enemyId == "indicible") TryUnlock(KillIndicible);
	}

	private void OnPlayerDamaged(float currentHp, float maxHp)
	{
		_tookDamageThisNight = true;
	}

	private void OnDayPhaseChanged(string phase)
	{
		if (phase == "Night")
			_tookDamageThisNight = false;
	}

	private void OnPoiExplored(string poiId, string poiType)
	{
		_sessionPois++;
	}

	private void OnChestOpened(string chestId, string rarity, Vector2 position)
	{
		_sessionChests++;
	}

	private void OnZoneDiscovered(int cellX, int cellY, int cellCount)
	{
		// Utilisé pour tracker la découverte de biomes (via cellCount ou zone mapping)
	}

	private void OnSouvenirDiscovered(string souvenirId, string souvenirName, string constellationId)
	{
		TryUnlock(FirstSouvenir);
	}

	private void OnScoreChanged(int newScore)
	{
		_sessionScore = newScore;
	}

	private void CheckCumulativeAchievements()
	{
		if (GetStat(StatTotalKills) >= 100) TryUnlock(Kill100);
		if (GetStat(StatTotalKills) >= 500) TryUnlock(Kill500);
		if (GetStat(StatTotalKills) >= 1000) TryUnlock(Kill1000);
		if (GetStat(StatTotalPois) >= 10) TryUnlock(Explore10Pois);
		if (GetStat(StatTotalChests) >= 50) TryUnlock(Open50Chests);
		if (GetStat(StatTotalStructures) >= 50) TryUnlock(Place50Structures);
	}

	// --- Helpers Steam ---

	private static void TryUnlock(string achievementId)
	{
		if (!SteamManager.IsActive)
			return;

		SteamUserStats.GetAchievement(achievementId, out bool alreadyUnlocked);
		if (alreadyUnlocked)
			return;

		SteamUserStats.SetAchievement(achievementId);
		GD.Print($"[SteamAchievements] Unlocked: {achievementId}");
	}

	private static void IncrementStat(string statId, int amount)
	{
		if (!SteamManager.IsActive || amount <= 0)
			return;

		SteamUserStats.GetStat(statId, out int current);
		SteamUserStats.SetStat(statId, current + amount);
	}

	private static void SetStatIfHigher(string statId, int value)
	{
		if (!SteamManager.IsActive)
			return;

		SteamUserStats.GetStat(statId, out int current);
		if (value > current)
			SteamUserStats.SetStat(statId, value);
	}

	private static int GetStat(string statId)
	{
		if (!SteamManager.IsActive)
			return 0;

		SteamUserStats.GetStat(statId, out int value);
		return value;
	}
}
