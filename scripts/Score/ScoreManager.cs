using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

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
    private const int NoDamageNightBonus = 500;
    private const int PointsPerStructurePlaced = 20;
    private const int PointsPerStructureSurvived = 50;
    private const string HighScorePath = "user://highscore.save";

    private int _combatScore;
    private int _survivalScore;
    private int _bonusScore;
    private int _buildScore;
    private int _totalKills;
    private int _nightKills;
    private int _nightScore;
    private int _nightsSurvived;
    private bool _tookDamageThisNight;
    private int _noDamageNights;
    private int _bestScore;
    private float _scoreMultiplier = 1f;
    private EventBus _eventBus;

    public int CurrentScore => (int)((_combatScore + _survivalScore + _bonusScore + _buildScore) * _scoreMultiplier);
    public int CombatScore => _combatScore;
    public int SurvivalScore => _survivalScore;
    public int BonusScore => _bonusScore;
    public int TotalKills => _totalKills;
    public int NightsSurvived => _nightsSurvived;
    public int NoDamageNights => _noDamageNights;
    public int BestScore => _bestScore;
    public bool IsNewRecord => CurrentScore > _bestScore;
    public int VestigesEarned { get; private set; }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.DayPhaseChanged += OnDayPhaseChanged;
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.StructurePlaced += OnStructurePlaced;

        LoadBestScore();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EnemyKilled -= OnEnemyKilled;
            _eventBus.DayPhaseChanged -= OnDayPhaseChanged;
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
            _eventBus.StructurePlaced -= OnStructurePlaced;
        }
    }

    public void SetCharacterMultiplier(float multiplier)
    {
        _scoreMultiplier = multiplier;
        GD.Print($"[ScoreManager] Score multiplier set to x{multiplier}");
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
    }

    /// <summary>Construit un RunRecord depuis l'état courant de la run.</summary>
    public RunRecord BuildRunRecord()
    {
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        string characterId = gm.SelectedCharacterId ?? "unknown";
        CharacterData charData = CharacterDataLoader.Get(characterId);

        return new RunRecord
        {
            CharacterId = characterId,
            CharacterName = charData?.Name ?? characterId,
            Score = CurrentScore,
            NightsSurvived = _nightsSurvived,
            TotalKills = _totalKills,
            Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        _totalKills++;
        _nightKills++;

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
            _ => PointsPerMeleeKill
        };
    }

    private void OnStructurePlaced(string structureId, Vector2 position)
    {
        _buildScore += PointsPerStructurePlaced;
        _eventBus.EmitSignal(EventBus.SignalName.ScoreChanged, CurrentScore);
    }

    private void OnDayPhaseChanged(string phase)
    {
        if (phase == "Dawn")
        {
            _nightsSurvived++;

            int survivalPoints = GetSurvivalPoints(_nightsSurvived);
            _survivalScore += survivalPoints;

            int structureBonus = CountSurvivingStructures() * PointsPerStructureSurvived;
            _buildScore += structureBonus;

            if (!_tookDamageThisNight && _nightsSurvived > 0)
            {
                _bonusScore += NoDamageNightBonus;
                _noDamageNights++;
            }

            _eventBus.EmitSignal(EventBus.SignalName.ScoreChanged, CurrentScore);
            _eventBus.EmitSignal(EventBus.SignalName.NightSummary, _nightsSurvived, _nightKills, _nightScore);
            GD.Print($"[ScoreManager] Night {_nightsSurvived} — Kills: {_nightKills}, Score: +{_nightScore}, Survival: +{survivalPoints}, Structures: +{structureBonus}, NoDmg: {!_tookDamageThisNight}");

            _nightKills = 0;
            _nightScore = 0;
            _tookDamageThisNight = false;
        }
        else if (phase == "Night")
        {
            _tookDamageThisNight = false;
        }
    }

    private int CountSurvivingStructures()
    {
        Godot.Collections.Array<Node> structures = GetTree().GetNodesInGroup("structures");
        return structures.Count;
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
