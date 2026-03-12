using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Infrastructure.Analytics;
using Vestiges.Infrastructure.Steam;

namespace Vestiges.Score;

/// <summary>
/// Score complet : combat + survie + bonus nuit sans dégât.
/// Sauvegarde du meilleur score en local.
/// </summary>
public partial class ScoreManager : Node
{
    private const int PointsPerMeleeKill = 10;
    private const int PointsPerRangedKill = 15;
    private const int PointsPerBruteKill = 30;
    private const int PointsPerShadeKill = 5;
    private const int PointsPerSentinelKill = 25;
    private const int PointsPerHurleurKill = 30;
    private const int PointsPerTisseuseKill = 20;
    private const int PointsPerRampantKill = 15;
    private const int PointsPerRodeurKill = 15;
    private const int PointsPerColosseKill = 200;
    private const int PointsPerIndicibleKill = 5000;
    private const int NoDamageNightBonus = 500;
    private const int PointsPerPoiExplored = 50;
    private const string HighScorePath = "user://highscore.save";

    private int _combatScore;
    private int _survivalScore;
    private int _bonusScore;
    private int _explorationScore;
    private int _totalKills;
    private int _nightScore;
    private bool _tookDamageThisNight;
    private int _noDamageNights;
    private int _bestScore;
    private float _scoreMultiplier = 1f;
    private float _mutatorMultiplier = 1f;
    private EventBus _eventBus;

    public int CurrentScore => (int)((_combatScore + _survivalScore + _bonusScore + _explorationScore) * _scoreMultiplier * _mutatorMultiplier);
    public int CombatScore => _combatScore;
    public int SurvivalScore => _survivalScore;
    public int BonusScore => _bonusScore;
    public int ExplorationScore => _explorationScore;
    public int TotalKills => _totalKills;
    public int NoDamageNights => _noDamageNights;
    public int BestScore => _bestScore;
    public bool IsNewRecord => CurrentScore > _bestScore;
    public int VestigesEarned { get; private set; }
    public float CharacterMultiplier => _scoreMultiplier;
    public float MutatorMultiplier => _mutatorMultiplier;

    private RunTracker _runTracker;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.PoiExplored += OnPoiExplored;
        _eventBus.ChestOpened += OnChestOpened;

        LoadBestScore();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EnemyKilled -= OnEnemyKilled;
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
            _eventBus.PoiExplored -= OnPoiExplored;
            _eventBus.ChestOpened -= OnChestOpened;
        }
    }

    public void SetRunTracker(RunTracker runTracker)
    {
        _runTracker = runTracker;
    }

    public void SetCharacterMultiplier(float multiplier)
    {
        _scoreMultiplier = multiplier;
        GD.Print($"[ScoreManager] Character multiplier set to x{multiplier}");
    }

    public void SetMutatorMultiplier(float multiplier)
    {
        _mutatorMultiplier = multiplier;
        GD.Print($"[ScoreManager] Mutator multiplier set to x{multiplier:F2}");
    }

    /// <summary>Sauvegarde le score, enregistre la run, calcule les Vestiges, vérifie les déblocages.</summary>
    public void SaveEndOfRun()
    {
        if (CurrentScore > _bestScore)
        {
            _bestScore = CurrentScore;
            SaveBestScore();
        }

        RunRecord record = BuildRunRecord();
        RunHistoryManager.SaveRun(record);

        // Vestiges = score / 10
        VestigesEarned = CurrentScore / 10;
        MetaSaveManager.AddVestiges(VestigesEarned);

        // Update meta stats and check unlocks
        MetaSaveManager.UpdateStats(record);
        System.Collections.Generic.List<string> newUnlocks = MetaSaveManager.CheckUnlocks();

        GameManager gm = GetNode<GameManager>("/root/GameManager");
        gm.LastRunData = record;
        gm.LastVestigesEarned = VestigesEarned;
        gm.LastUnlocks = newUnlocks;

        // Analytics : enregistrer les métriques de la run
        AnalyticsManager.Instance?.RecordRunEnd(record);

        // Steam : upload score + achievements
        if (SteamManager.IsActive)
        {
            SteamAchievements achievements = GetNodeOrNull<SteamAchievements>("../SteamAchievements");
            int nightsSurvived = _runTracker?.CurrentNight ?? 0;
            achievements?.OnRunEnd(CurrentScore, nightsSurvived, gm.SelectedCharacterId);

            SteamLeaderboards leaderboards = GetNodeOrNull<SteamLeaderboards>("../SteamLeaderboards");
            leaderboards?.UploadScore(CurrentScore, nightsSurvived, gm.SelectedCharacterId);
        }
    }

    /// <summary>Construit un RunRecord enrichi depuis l'état courant + RunTracker.</summary>
    public RunRecord BuildRunRecord()
    {
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        string characterId = gm.SelectedCharacterId ?? "unknown";
        CharacterData charData = CharacterDataLoader.Get(characterId);

        Player player = GetTree().GetFirstNodeInGroup("player") as Player;
        string weaponId = player?.EquippedWeapon?.Id ?? "unknown";

        RunRecord record = new()
        {
            CharacterId = characterId,
            CharacterName = charData?.Name ?? characterId,
            Score = CurrentScore,
            NightsSurvived = _runTracker?.CurrentNight ?? 0,
            TotalKills = _totalKills,
            Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            WeaponId = weaponId,
            CombatScoreDetail = _combatScore,
            SurvivalScoreDetail = _survivalScore,
            BonusScoreDetail = _bonusScore,
            BuildScoreDetail = 0,
            ExplorationScoreDetail = _explorationScore,
            Seed = gm.RunSeed,
            ActiveMutators = new System.Collections.Generic.List<string>(gm.ActiveMutators),
            MutatorMultiplier = _mutatorMultiplier
        };

        if (_runTracker != null)
        {
            record.DeathCause = _runTracker.LastHitByEnemyId;
            record.DeathNight = _runTracker.CurrentNight;
            record.DeathPhase = _runTracker.CurrentPhase;
            record.PerkIds = new System.Collections.Generic.List<string>(_runTracker.PerkIds);
            record.TotalDamageDealt = _runTracker.TotalDamageDealt;
            record.TotalDamageTaken = _runTracker.TotalDamageTaken;
            record.ResourcesCollected = new System.Collections.Generic.Dictionary<string, int>(_runTracker.ResourcesCollected);
            record.StructuresPlaced = _runTracker.StructuresPlaced;
            record.StructuresLost = _runTracker.StructuresLost;
            record.PoisExplored = _runTracker.PoisExplored;
            record.ChestsOpened = _runTracker.ChestsOpened;
            record.MaxLevel = _runTracker.MaxLevel;
            record.RunDurationSec = _runTracker.RunDurationSeconds;
            record.TotalSpawned = _runTracker.TotalSpawned;
            record.PeakEnemies = _runTracker.PeakEnemies;
            record.AvgPressure = _runTracker.PressureRatio;
            record.FinalHpScale = _runTracker.LastHpScale;
            record.FinalDmgScale = _runTracker.LastDmgScale;
        }

        return record;
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        _totalKills++;

        int points = GetPointsForEnemy(enemyId);
        _combatScore += points;
        _nightScore += points;

        _eventBus.EmitSignal(EventBus.SignalName.ScoreChanged, CurrentScore);
    }

    private void OnPlayerDamaged(float currentHp, float maxHp)
    {
        _tookDamageThisNight = true;
    }

    private int GetPointsForEnemy(string enemyId)
    {
        return enemyId switch
        {
            "shadow_crawler" => PointsPerMeleeKill,
            "fading_spitter" => PointsPerRangedKill,
            "void_brute" => PointsPerBruteKill,
            "shade" => PointsPerShadeKill,
            "wailing_sentinel" => PointsPerSentinelKill,
            "hurleur" => PointsPerHurleurKill,
            "tisseuse" => PointsPerTisseuseKill,
            "rampant" => PointsPerRampantKill,
            "rodeur" => PointsPerRodeurKill,
            "colosse_forest" or "colosse_urban" or "colosse_swamp" => PointsPerColosseKill,
            "indicible" => PointsPerIndicibleKill,
            _ => PointsPerMeleeKill
        };
    }

    private void OnPoiExplored(string poiId, string poiType)
    {
        _explorationScore += PointsPerPoiExplored;
        _eventBus.EmitSignal(EventBus.SignalName.ScoreChanged, CurrentScore);
        GD.Print($"[ScoreManager] POI explored: {poiId} ({poiType}) +{PointsPerPoiExplored}pts");
    }

    private void OnChestOpened(string chestId, string rarity, Vector2 position)
    {
        int points = rarity switch
        {
            "common" => 25,
            "rare" => 75,
            "epic" => 200,
            "lore" => 100,
            _ => 25
        };
        _explorationScore += points;
        _eventBus.EmitSignal(EventBus.SignalName.ScoreChanged, CurrentScore);
        GD.Print($"[ScoreManager] Chest opened: {chestId} ({rarity}) +{points}pts");
    }

    /// <summary>Score de survie exponentiel par nuit (GDD : nuit1=100, nuit2=250, nuit5=1500, nuit10=8000).</summary>
    private int GetSurvivalPoints(int nightNumber)
    {
        return (int)(100 * Mathf.Pow(1.6f, nightNumber - 1));
    }

    private void LoadBestScore()
    {
        if (!FileAccess.FileExists(HighScorePath))
        {
            _bestScore = 0;
            return;
        }

        FileAccess file = FileAccess.Open(HighScorePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            _bestScore = 0;
            return;
        }

        string content = file.GetAsText().StripEdges();
        file.Close();

        if (int.TryParse(content, out int score))
            _bestScore = score;
    }

    private void SaveBestScore()
    {
        FileAccess file = FileAccess.Open(HighScorePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError("[ScoreManager] Cannot save high score");
            return;
        }

        file.StoreString(_bestScore.ToString());
        file.Close();
        GD.Print($"[ScoreManager] New record saved: {_bestScore}");
    }
}
