using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Infrastructure.Analytics;
using Vestiges.Infrastructure.Steam;

namespace Vestiges.Score;

/// <summary>
/// Mesure de la run : les éliminations, une seule mesure pour le bilan, les records et les classements (DECISIONS §155).
/// Règle la fin de run : records du profil (global et par personnage), Vestiges gagnés, historique, analytics, Steam.
/// </summary>
public partial class ScoreManager : Node
{
    private ScoreConfig _config;
    private bool _endSettled;
    private bool _endedWithRecord;
    private bool _endedWithCharacterRecord;
    private int _previousBest;
    private int _previousCharacterBest;

    private int _totalKills;
    private EventBus _eventBus;
    private bool _bossDefeated;
    private bool _endgameReached;

    public int TotalKills => _totalKills;
    /// <summary>Record global avant cette run : figé par SaveEndOfRun, qui écrit ensuite le nouveau record.</summary>
    public int BestKills => _endSettled ? _previousBest : MetaSaveManager.GetBestKills();
    /// <summary>Record du personnage joué avant cette run, figé de même.</summary>
    public int CharacterBestKills => _endSettled ? _previousCharacterBest : MetaSaveManager.GetKillRecord(CharacterId)?.Kills ?? 0;
    /// <summary>Verdicts figés en fin de run, avant la sauvegarde qui écraserait la comparaison (plan 02 lot A).</summary>
    public bool IsNewRecord => _endSettled ? _endedWithRecord : _totalKills > BestKills;
    public bool IsNewCharacterRecord => _endSettled ? _endedWithCharacterRecord : _totalKills > CharacterBestKills;
    public int VestigesEarned { get; private set; }
    /// <summary>Multiplicateur des Vestiges de fin de run, posé au départ par le Campement.</summary>
    public float VestigeMultiplier { get; set; } = 1f;

    private RunTracker _runTracker;

    private string CharacterId => GetNode<GameManager>("/root/GameManager").SelectedCharacterId ?? "";

    public override void _Ready()
    {
        // Barème refusé : le chargement de la run s'arrête sur son message (GameBootstrap).
        ScoreConfig.TryLoad(out _config, out _);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.RunPhaseChanged += OnRunPhaseChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EnemyKilled -= OnEnemyKilled;
            _eventBus.RunPhaseChanged -= OnRunPhaseChanged;
        }
    }

    public void SetRunTracker(RunTracker runTracker)
    {
        _runTracker = runTracker;
    }

    /// <summary>
    /// Règle la run une seule fois : records, acquis méta et historique (plan 26 Q2b), puis analytics et Steam.
    /// Un second appel ne fait rien ; un échec d'écriture est remis au bilan par <see cref="GameManager.LastRunSaveError"/>.
    /// </summary>
    public void SaveEndOfRun()
    {
        if (_endSettled)
            return;

        _previousBest = MetaSaveManager.GetBestKills();
        _previousCharacterBest = MetaSaveManager.GetKillRecord(CharacterId)?.Kills ?? 0;
        _endedWithRecord = _totalKills > _previousBest;
        _endedWithCharacterRecord = _totalKills > _previousCharacterBest;
        _endSettled = true;

        RunRecord record = BuildRunRecord();
        record.RunId = System.Guid.NewGuid().ToString("N");
        VestigesEarned = _config?.VestigesFor(_totalKills, VestigeMultiplier) ?? 0;
        RunSettlement.Outcome outcome = RunSettlement.Settle(record, VestigesEarned);

        GameManager gm = GetNode<GameManager>("/root/GameManager");
        gm.LastRunData = record;
        gm.LastVestigesEarned = VestigesEarned;
        gm.LastRunSaveError = outcome.Saved ? "" : outcome.Error;
        gm.LastRunHistoryPending = outcome.HistoryPending;

        // Analytics : enregistrer les métriques de la run
        AnalyticsManager.Instance?.RecordRunEnd(record);

        if (record.Provenance == RunProvenance.Normal && SteamManager.CanSubmitResults)
            SubmitToSteam(gm.SelectedCharacterId);
    }

    // Hors SaveEndOfRun : ces types nomment Steamworks, dont l'assemblage ne se charge pas hors x86/x64.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SubmitToSteam(string characterId)
    {
        GetNodeOrNull<SteamAchievements>("../SteamAchievements")?.OnRunEnd(_totalKills, _runTracker?.CrisesSurvived ?? 0, characterId);
        SteamManager.SubmitRunKills(_totalKills, characterId);
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
            Provenance = DevelopmentMode.CurrentProvenance,
            CharacterId = characterId,
            CharacterName = charData?.Name ?? characterId,
            CrisesSurvived = _runTracker?.CrisesSurvived ?? 0,
            TotalKills = _totalKills,
            Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            WeaponId = weaponId == "unknown" ? null : weaponId,
            Seed = gm.EffectiveSeed,
            ActiveMutators = gm.ActiveMutators != null && gm.ActiveMutators.Count > 0
                ? new List<string>(gm.ActiveMutators)
                : null,
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
        if (enemyId == EnemyGrammar.FinalBossId)
            _bossDefeated = true;
    }

    private void OnRunPhaseChanged(string oldPhase, string newPhase)
    {
        if (newPhase == "Endgame")
            _endgameReached = true;
    }
}
