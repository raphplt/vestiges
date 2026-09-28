using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Vestiges.World;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Infrastructure.Analytics;
using Vestiges.Infrastructure.Steam;

namespace Vestiges.Score;

/// <summary>
/// Score V2 : combat + temps de survie + crises + exploration.
/// Sauvegarde du meilleur score en local.
/// </summary>
public partial class ScoreManager : Node
{
    // Le score de survie croît chaque seconde : notifié à cadence bornée, et seulement s'il a changé (plan 02 lot A).
    private const float NotifyInterval = 0.25f;
    private ScoreConfig _config;
    private float _notifyTimer;
    private int _notifiedScore = -1;
    private bool _endSettled;
    private bool _endedWithRecord;
    private int _previousBest;
    private static string HighScorePath => DevelopmentMode.GetSavePath("highscore.save");

    private int _combatScore;
    private int _survivalScore;
    private int _bonusScore;
    private int _explorationScore;
    private int _totalKills;
    private int _bestScore;
    private float _scoreMultiplier = 1f;
    private float _mutatorMultiplier = 1f;
    private float _perilMultiplier = 1f;
    private EventBus _eventBus;
    private bool _bossDefeated;
    private bool _endgameReached;

    public int CurrentScore => (int)((_combatScore + SurvivalScore + BonusScore + _explorationScore) * _scoreMultiplier * _mutatorMultiplier * _perilMultiplier);
    public int CombatScore => _combatScore;
    public int SurvivalScore
    {
        get
        {
            if (_runTracker == null)
                return _survivalScore;
            return Mathf.RoundToInt(_runTracker.RunDurationSeconds * _config.PointsPerSecond);
        }
    }
    public int BonusScore
    {
        get
        {
            if (_runTracker == null)
                return _bonusScore;
            return _bonusScore + _runTracker.CrisesSurvived * _config.PointsPerCrisis;
        }
    }
    public int ExplorationScore => _explorationScore;
    public int TotalKills => _totalKills;
    /// <summary>Meilleur score avant cette run : figé par SaveEndOfRun, qui écrit ensuite le nouveau record.</summary>
    public int BestScore => _endSettled ? _previousBest : _bestScore;
    /// <summary>Verdict figé en fin de run, avant la sauvegarde qui écraserait la comparaison (plan 02 lot A).</summary>
    public bool IsNewRecord => _endSettled ? _endedWithRecord : CurrentScore > _bestScore;
    public int VestigesEarned { get; private set; }
    public float CharacterMultiplier => _scoreMultiplier;
    public float MutatorMultiplier => _mutatorMultiplier;
    public float PerilMultiplier => _perilMultiplier;

    private RunTracker _runTracker;

    public override void _Ready()
    {
        _config = ScoreConfig.Load();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.PoiExplored += OnPoiExplored;
        _eventBus.ChestOpened += OnChestOpened;
        _eventBus.RunPhaseChanged += OnRunPhaseChanged;
        _eventBus.PerilChanged += OnPerilChanged;

        LoadBestScore();
    }

    public override void _Process(double delta)
    {
        _notifyTimer -= (float)delta;
        if (_notifyTimer > 0f)
            return;
        _notifyTimer = NotifyInterval;
        if (CurrentScore != _notifiedScore)
            NotifyScore();
    }

    /// <summary>Seul point d'émission de ScoreChanged : un gain immédiat n'est pas réémis au tick suivant.</summary>
    private void NotifyScore()
    {
        _notifiedScore = CurrentScore;
        _eventBus.EmitSignal(EventBus.SignalName.ScoreChanged, _notifiedScore);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EnemyKilled -= OnEnemyKilled;
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
            _eventBus.PoiExplored -= OnPoiExplored;
            _eventBus.ChestOpened -= OnChestOpened;
            _eventBus.RunPhaseChanged -= OnRunPhaseChanged;
            _eventBus.PerilChanged -= OnPerilChanged;
        }
    }

    private void OnPerilChanged(int peril)
    {
        _perilMultiplier = PerilDataLoader.ScoreMultiplier(peril);
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
        _previousBest = _bestScore;
        _endedWithRecord = CurrentScore > _bestScore;
        _endSettled = true;
        if (_endedWithRecord)
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

        if (SteamManager.IsActive)
            SubmitToSteam(gm.SelectedCharacterId);
    }

    // Hors SaveEndOfRun : ces types nomment Steamworks, dont l'assemblage ne se charge pas hors x86/x64.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SubmitToSteam(string characterId)
    {
        int crisesSurvived = _runTracker?.CrisesSurvived ?? 0;
        GetNodeOrNull<SteamAchievements>("../SteamAchievements")?.OnRunEnd(CurrentScore, crisesSurvived, characterId);
        GetNodeOrNull<SteamLeaderboards>("../SteamLeaderboards")?.UploadScore(CurrentScore, crisesSurvived, characterId);
    }

    /// <summary>Construit un RunRecord enrichi depuis l'état courant + RunTracker.</summary>
    public RunRecord BuildRunRecord()
    {
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        string characterId = gm.SelectedCharacterId ?? "unknown";
        CharacterData charData = CharacterDataLoader.Get(characterId);

        // À la mort, le joueur a déjà quitté le groupe « player » : on le prend dans la scène de run.
        Player player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
        string weaponId = player?.EquippedWeapon?.Id ?? "unknown";

        RunRecord record = new()
        {
            CharacterId = characterId,
            CharacterName = charData?.Name ?? characterId,
            Score = CurrentScore,
            CrisesSurvived = _runTracker?.CrisesSurvived ?? 0,
            TotalKills = _totalKills,
            Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            WeaponId = weaponId == "unknown" ? null : weaponId,
            CombatScoreDetail = _combatScore,
            SurvivalScoreDetail = SurvivalScore,
            BonusScoreDetail = BonusScore,
            ExplorationScoreDetail = _explorationScore,
            Seed = gm.RunSeed,
            ActiveMutators = gm.ActiveMutators != null && gm.ActiveMutators.Count > 0
                ? new List<string>(gm.ActiveMutators)
                : null,
            MutatorMultiplier = _mutatorMultiplier,
            RunPhase = _runTracker?.CurrentPhase ?? GameManager.RunPhase.Exploration.ToString(),
            RunDurationSec = _runTracker?.RunDurationSeconds ?? 0f,
            BossDefeated = _bossDefeated,
            EndgameReached = _endgameReached
        };

        if (_runTracker != null)
        {
            record.DeathCause = _runTracker.LastHitByEnemyId;
            record.DeathPhase = _runTracker.CurrentPhase;
            record.PerkIds = _runTracker.PerkIds.Count > 0
                ? new List<string>(_runTracker.PerkIds)
                : null;
            record.TotalDamageDealt = _runTracker.TotalDamageDealt;
            record.TotalDamageTaken = _runTracker.TotalDamageTaken;
            record.PoisExplored = _runTracker.PoisExplored;
            record.ChestsOpened = _runTracker.ChestsOpened;
            record.MaxLevel = _runTracker.MaxLevel;
            record.RunDurationSec = _runTracker.RunDurationSeconds;
            record.TotalSpawned = _runTracker.TotalSpawned;
            record.PeakEnemies = _runTracker.PeakEnemies;
            record.AvgPressure = _runTracker.PressureRatio;
            record.FinalHpScale = _runTracker.LastHpScale;
            record.FinalDmgScale = _runTracker.LastDmgScale;
            record.MaxDistanceMeters = _runTracker.Journey.MaxDistanceMeters;
            record.TravelledMeters = _runTracker.Journey.TravelledMeters;
            record.ElitesKilled = _runTracker.ElitesKilled;
            record.SovereignsKilled = _runTracker.SovereignsKilled;
            record.BossesKilled = _runTracker.BossesKilled;
        }

        if (player != null)
        {
            record.Weapons = new List<RunWeaponRecord>();
            foreach (WeaponInstance weapon in player.WeaponSlots)
            {
                record.Weapons.Add(new RunWeaponRecord
                {
                    Id = weapon.Id,
                    Level = weapon.Level,
                    Damage = player.GetDamageDealt(weapon.Id),
                    Kills = player.GetKills(weapon.Id),
                    HeldSeconds = _runTracker?.WeaponHeldSeconds(weapon.Id),
                });
            }
        }

        return record;
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        _totalKills++;

        int points = _config.KillPoints(enemyId);
        // Combattre là où le monde s'oublie rapporte davantage.
        points = Mathf.RoundToInt(points * (1f + ErasureEffectAt(position).ScoreBonus));
        _combatScore += points;

        if (enemyId == "indicible" && !_bossDefeated)
        {
            _bossDefeated = true;
            _bonusScore += _config.PointsBossDefeated;
        }

        NotifyScore();
    }

    private void OnPlayerDamaged(float currentHp, float maxHp)
    {
    }

    private ErasureManager _erasureManager;

    /// <summary>Ce que l'oubli offre là où la créature est tombée (plan 16 O5).</summary>
    private ErasureEffects.Effect ErasureEffectAt(Vector2 position)
    {
        if (_erasureManager == null || !IsInstanceValid(_erasureManager))
            _erasureManager = GetTree().CurrentScene?.GetNodeOrNull<ErasureManager>("ErasureManager");
        return _erasureManager == null ? ErasureEffects.Effect.None : ErasureEffects.For(_erasureManager.GetZonePhaseAt(position));
    }

    private void OnPoiExplored(string poiId, string poiType)
    {
        _explorationScore += _config.PointsPerPoi;
        NotifyScore();
        GD.Print($"[ScoreManager] POI explored: {poiId} ({poiType}) +{_config.PointsPerPoi}pts");
    }

    private void OnChestOpened(string chestId, string rarity, Vector2 position)
    {
        int points = _config.ChestPoints(rarity);
        _explorationScore += points;
        NotifyScore();
        GD.Print($"[ScoreManager] Chest opened: {chestId} ({rarity}) +{points}pts");
    }

    private void OnRunPhaseChanged(string oldPhase, string newPhase)
    {
        if (newPhase == "Endgame" && !_endgameReached)
        {
            _endgameReached = true;
            _bonusScore += _config.PointsEndgameReached;
            NotifyScore();
        }
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
