using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Infrastructure;

/// <summary>
/// Accumule les données brutes d'une run en écoutant l'EventBus.
/// Utilisé par ScoreManager.BuildRunRecord() pour enrichir le RunRecord à la mort.
/// </summary>
public partial class RunTracker : Node
{
    private EventBus _eventBus;
    private float _runStartTimeMsec;

    private float _totalDamageDealt;
    private float _totalDamageTaken;
    private float _previousHp;
    private float _previousMaxHp;
    private readonly Dictionary<string, int> _resourcesCollected = new();
    private int _structuresPlaced;
    private int _structuresLost;
    private int _poisExplored;
    private int _chestsOpened;
    private int _maxLevel;
    private readonly List<string> _perkIds = new();
    private string _lastHitByEnemyId = "unknown";
    private string _currentPhase = "Day";
    private int _currentNight;

    // DPS tracking (rolling window)
    private readonly List<(float time, float damage)> _damageEvents = new();
    private const float DpsWindowSeconds = 10f;

    // Difficulty / pressure tracking
    private int _totalSpawned;
    private int _totalKilled;
    private int _peakEnemies;
    private float _lastHpScale = 1f;
    private float _lastDmgScale = 1f;
    private readonly List<float> _spawnTimestamps = new();
    private readonly List<float> _killTimestamps = new();
    private const float RateWindowSeconds = 60f;

    public float TotalDamageDealt => _totalDamageDealt;
    public float TotalDamageTaken => _totalDamageTaken;
    public Dictionary<string, int> ResourcesCollected => _resourcesCollected;
    public int StructuresPlaced => _structuresPlaced;
    public int StructuresLost => _structuresLost;
    public int PoisExplored => _poisExplored;
    public int ChestsOpened => _chestsOpened;
    public int MaxLevel => _maxLevel;
    public List<string> PerkIds => _perkIds;
    public string LastHitByEnemyId => _lastHitByEnemyId;
    public string CurrentPhase => _currentPhase;
    public int CurrentNight => _currentNight;
    public float RunDurationSeconds => (Time.GetTicksMsec() - _runStartTimeMsec) / 1000f;

    // --- Difficulty metrics ---
    public int TotalSpawned => _totalSpawned;
    public int TotalKilled => _totalKilled;
    public int PeakEnemies => _peakEnemies;
    public float LastHpScale => _lastHpScale;
    public float LastDmgScale => _lastDmgScale;

    /// <summary>Spawns par minute sur une fenêtre glissante de 60s.</summary>
    public float SpawnsPerMinute
    {
        get
        {
            float now = Time.GetTicksMsec() / 1000f;
            float windowStart = now - RateWindowSeconds;
            int count = 0;
            foreach (float t in _spawnTimestamps)
            {
                if (t >= windowStart) count++;
            }
            return count;
        }
    }

    /// <summary>Kills par minute sur une fenêtre glissante de 60s.</summary>
    public float KillsPerMinute
    {
        get
        {
            float now = Time.GetTicksMsec() / 1000f;
            float windowStart = now - RateWindowSeconds;
            int count = 0;
            foreach (float t in _killTimestamps)
            {
                if (t >= windowStart) count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Ratio de pression : spawns/min ÷ kills/min.
    /// &lt; 1 = le joueur gère. = 1 = équilibre. &gt; 1 = les ennemis s'accumulent.
    /// </summary>
    public float PressureRatio
    {
        get
        {
            float kpm = KillsPerMinute;
            if (kpm < 0.1f) return SpawnsPerMinute > 0 ? 99f : 0f;
            return SpawnsPerMinute / kpm;
        }
    }

    /// <summary>DPS sur une fenêtre glissante de 10s.</summary>
    public float RollingDps
    {
        get
        {
            float now = Time.GetTicksMsec() / 1000f;
            float windowStart = now - DpsWindowSeconds;
            float total = 0f;
            foreach ((float time, float damage) entry in _damageEvents)
            {
                if (entry.time >= windowStart)
                    total += entry.damage;
            }
            return total / DpsWindowSeconds;
        }
    }

    /// <summary>Dégâts reçus par minute sur toute la run.</summary>
    public float DamageTakenPerMinute
    {
        get
        {
            float duration = RunDurationSeconds;
            if (duration < 1f) return 0f;
            return _totalDamageTaken / (duration / 60f);
        }
    }

    public override void _Ready()
    {
        _runStartTimeMsec = Time.GetTicksMsec();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemySpawned += OnEnemySpawned;
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.PlayerHitBy += OnPlayerHitBy;
        _eventBus.DayPhaseChanged += OnDayPhaseChanged;
        _eventBus.ResourceCollected += OnResourceCollected;
        _eventBus.StructurePlaced += OnStructurePlaced;
        _eventBus.StructureDestroyed += OnStructureDestroyed;
        _eventBus.PoiExplored += OnPoiExplored;
        _eventBus.ChestOpened += OnChestOpened;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.PerkChosen += OnPerkChosen;
        _eventBus.EntityDamaged += OnEntityDamaged;

        GD.Print("[RunTracker] Tracking started");
    }

    public override void _ExitTree()
    {
        if (_eventBus == null) return;
        _eventBus.EnemySpawned -= OnEnemySpawned;
        _eventBus.EnemyKilled -= OnEnemyKilled;
        _eventBus.PlayerDamaged -= OnPlayerDamaged;
        _eventBus.PlayerHitBy -= OnPlayerHitBy;
        _eventBus.DayPhaseChanged -= OnDayPhaseChanged;
        _eventBus.ResourceCollected -= OnResourceCollected;
        _eventBus.StructurePlaced -= OnStructurePlaced;
        _eventBus.StructureDestroyed -= OnStructureDestroyed;
        _eventBus.PoiExplored -= OnPoiExplored;
        _eventBus.ChestOpened -= OnChestOpened;
        _eventBus.LevelUp -= OnLevelUp;
        _eventBus.PerkChosen -= OnPerkChosen;
        _eventBus.EntityDamaged -= OnEntityDamaged;
    }

    public override void _Process(double delta)
    {
        float now = Time.GetTicksMsec() / 1000f;

        // Purge old DPS entries
        float dpsWindowStart = now - DpsWindowSeconds;
        _damageEvents.RemoveAll(e => e.time < dpsWindowStart);

        // Purge old rate entries
        float rateWindowStart = now - RateWindowSeconds;
        _spawnTimestamps.RemoveAll(t => t < rateWindowStart);
        _killTimestamps.RemoveAll(t => t < rateWindowStart);

        // Track peak enemies
        int activeEnemies = GetTree().GetNodesInGroup("enemies").Count;
        if (activeEnemies > _peakEnemies)
            _peakEnemies = activeEnemies;
    }

    private void OnEnemySpawned(string enemyId, float hpScale, float dmgScale)
    {
        _totalSpawned++;
        _lastHpScale = hpScale;
        _lastDmgScale = dmgScale;
        float now = Time.GetTicksMsec() / 1000f;
        _spawnTimestamps.Add(now);
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        _totalKilled++;
        float now = Time.GetTicksMsec() / 1000f;
        _killTimestamps.Add(now);
    }

    private void OnEntityDamaged(Node entity, float amount)
    {
        // Track damage dealt to enemies for DPS calculation
        if (entity is not Player)
        {
            _totalDamageDealt += amount;
            float now = Time.GetTicksMsec() / 1000f;
            _damageEvents.Add((now, amount));
        }
    }

    private void OnPlayerDamaged(float currentHp, float maxHp)
    {
        // Detect damage taken (HP decreased)
        if (_previousMaxHp > 0f && currentHp < _previousHp)
        {
            float damageTaken = _previousHp - currentHp;
            _totalDamageTaken += damageTaken;
        }
        _previousHp = currentHp;
        _previousMaxHp = maxHp;
    }

    private void OnPlayerHitBy(string enemyId, float damage)
    {
        _lastHitByEnemyId = enemyId;
    }

    private void OnDayPhaseChanged(string phase)
    {
        _currentPhase = phase;
        if (phase == "Night")
            _currentNight++;
    }

    private void OnResourceCollected(string resourceId, int amount)
    {
        if (_resourcesCollected.ContainsKey(resourceId))
            _resourcesCollected[resourceId] += amount;
        else
            _resourcesCollected[resourceId] = amount;
    }

    private void OnStructurePlaced(string structureId, Vector2 position)
    {
        _structuresPlaced++;
    }

    private void OnStructureDestroyed(string structureId, Vector2 position)
    {
        _structuresLost++;
    }

    private void OnPoiExplored(string poiId, string poiType)
    {
        _poisExplored++;
    }

    private void OnChestOpened(string chestId, string rarity, Vector2 position)
    {
        _chestsOpened++;
    }

    private void OnLevelUp(int newLevel)
    {
        if (newLevel > _maxLevel)
            _maxLevel = newLevel;
    }

    private void OnPerkChosen(string perkId)
    {
        _perkIds.Add(perkId);
    }
}
