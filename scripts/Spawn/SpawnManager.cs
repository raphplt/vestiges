using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Events;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Spawn;

public partial class SpawnManager : Node2D
{
    [Export] public float SpawnRadiusMin = 400f;
    [Export] public float SpawnRadiusMax = 600f;
    [Export] public float FoyerSafeRadius = 150f;
    [Export] public float NightSpawnRadius = 700f;

    private float _baseSpawnInterval;
    private float _minSpawnInterval;
    private float _spawnIntervalDecay;
    private float _hpScalingPerMinute;
    private float _dmgScalingPerMinute;
    private int _maxEnemies;
    private float _nightHpMultiplier;
    private float _nightDmgMultiplier;
    private float _nightSpawnRateMultiplier;
    private float _daySpawnRateMultiplier;
    private float _duskSpawnRateMultiplier;
    private float _nightBaseSpawnRateMultiplier;
    private float _flatHpMultiplier = 1f;
    private float _flatDmgMultiplier = 1f;

    private float _elapsedTime;
    private float _spawnTimer;
    private int _currentNight;

    private DayPhase _currentPhase = DayPhase.Day;
    private Vector2 _foyerPosition = Vector2.Zero;

    private EnemyPool _pool;
    private Player _player;
    private WorldSetup _worldSetup;
    private List<string> _enemyIds;
    private Node _enemyContainer;
    private EventBus _eventBus;

    // Fallback quand aucun biome n'est disponible
    private static readonly List<string> FallbackDayPool = new() { "shadow_crawler", "fading_spitter" };
    private static readonly List<string> FallbackNightPool = new() { "shadow_crawler", "shade", "shade", "fading_spitter", "void_brute", "wailing_sentinel" };

    // Mapping biome → colosse (mini-boss biome-spécifique, nuit 3+)
    private static readonly Dictionary<string, string> BiomeColosseMap = new()
    {
        { "forest_reclaimed", "colosse_forest" },
        { "urban_ruins", "colosse_urban" },
        { "swamp", "colosse_swamp" }
    };
    private const int ColosseMinNight = 3;
    private const string FallbackColosse = "colosse_forest";
    private const int AberrationMinNight = 7;
    private const float AberrationBaseChance = 0.15f;
    private const float AberrationChancePerNight = 0.05f;
    private const int IndicibleMinNight = 10;
    private bool _indicibleSpawned;
    private RandomEventManager _randomEventManager;

    public override void _Ready()
    {
        EnemyDataLoader.Load();
        BiomeDataLoader.Load();
        LoadScalingConfig();

        _enemyIds = EnemyDataLoader.GetAllIds();

        _pool = GetNode<EnemyPool>("../EnemyPool");
        _enemyContainer = GetNode("../EnemyContainer");

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.DayPhaseChanged += OnDayPhaseChanged;

        Node2D foyer = GetNodeOrNull<Node2D>("../Foyer");
        if (foyer != null)
            _foyerPosition = foyer.GlobalPosition;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.DayPhaseChanged -= OnDayPhaseChanged;
    }

    public override void _Process(double delta)
    {
        CachePlayer();
        if (_player == null || !IsInstanceValid(_player))
            return;

        if (_currentPhase == DayPhase.Dawn)
            return;

        float dt = (float)delta;
        _elapsedTime += dt;
        _spawnTimer += dt;

        float elapsedMinutes = _elapsedTime / 60f;
        float interval = GetCurrentInterval(elapsedMinutes);

        if (_spawnTimer >= interval)
        {
            _spawnTimer = 0f;
            TrySpawnEnemy(elapsedMinutes);
        }
    }

    private float GetCurrentInterval(float elapsedMinutes)
    {
        float baseInterval = Mathf.Max(
            _baseSpawnInterval - (_spawnIntervalDecay * elapsedMinutes),
            _minSpawnInterval
        );

        float nightFactor = _currentPhase switch
        {
            DayPhase.Day => _daySpawnRateMultiplier,
            DayPhase.Dusk => _duskSpawnRateMultiplier,
            DayPhase.Night => _nightBaseSpawnRateMultiplier * Mathf.Pow(_nightSpawnRateMultiplier, _currentNight - 1),
            _ => 1f
        };

        return Mathf.Max(baseInterval * nightFactor, _minSpawnInterval);
    }

    private void TrySpawnEnemy(float elapsedMinutes)
    {
        if (_pool.ActiveCount >= _maxEnemies)
            return;

        // Position d'abord, puis on interroge le biome à cet endroit
        Vector2 spawnPos = GetSpawnPosition();
        string enemyId = PickEnemyForPosition(spawnPos);
        EnemyData data = EnemyDataLoader.Get(enemyId);
        if (data == null)
            return;

        float hpScale = Mathf.Pow(_hpScalingPerMinute, elapsedMinutes) * _flatHpMultiplier;
        float dmgScale = Mathf.Pow(_dmgScalingPerMinute, elapsedMinutes) * _flatDmgMultiplier;

        if (_currentPhase == DayPhase.Night && _currentNight > 1)
        {
            float nightBonus = Mathf.Pow(_nightHpMultiplier, _currentNight - 1);
            float dmgBonus = Mathf.Pow(_nightDmgMultiplier, _currentNight - 1);
            hpScale *= nightBonus;
            dmgScale *= dmgBonus;
        }

        // Résurgence : buff ennemis pendant l'événement
        CacheRandomEventManager();
        if (_randomEventManager != null && _randomEventManager.ResurgenceActive)
        {
            hpScale *= _randomEventManager.ResurgenceHpMultiplier;
            dmgScale *= _randomEventManager.ResurgenceDmgMultiplier;
        }

        Enemy enemy = _pool.Get();
        enemy.GlobalPosition = spawnPos;
        _enemyContainer.AddChild(enemy);
        enemy.Initialize(data, hpScale, dmgScale);
        enemy.SetNightTarget(_currentPhase == DayPhase.Night, _foyerPosition);

        // Aberration : nuit 7+, chance croissante de corrompre l'ennemi
        if (_currentPhase == DayPhase.Night && _currentNight >= AberrationMinNight && data.Tier == "normal")
        {
            float chance = AberrationBaseChance + AberrationChancePerNight * (_currentNight - AberrationMinNight);
            if (GD.Randf() < chance)
                enemy.Aberrate();
        }

        // Wave modifiers : nuit 7+, chance d'appliquer enragé/régénérant/explosif
        if (_currentPhase == DayPhase.Night && _currentNight >= AberrationMinNight && data.Tier == "normal")
        {
            float modChance = 0.1f + 0.03f * (_currentNight - AberrationMinNight);
            if (GD.Randf() < modChance)
            {
                string[] modifiers = { "enraged", "regenerant", "explosive" };
                string modifier = modifiers[(int)(GD.Randi() % modifiers.Length)];
                enemy.ApplyWaveModifier(modifier);
            }
        }

        _eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, enemyId, hpScale, dmgScale);
    }

    /// <summary>
    /// Sélectionne un ennemi en fonction du biome à la position donnée.
    /// Jour : pool diurne du biome. Nuit/Crépuscule : pool nocturne.
    /// </summary>
    private string PickEnemyForPosition(Vector2 worldPos)
    {
        bool isNight = _currentPhase == DayPhase.Night || _currentPhase == DayPhase.Dusk;

        CacheWorldSetup();
        BiomeData biome = _worldSetup?.GetBiomeAt(worldPos);

        List<string> pool;
        if (biome != null)
        {
            pool = isNight ? biome.NightEnemyPool : biome.DayEnemyPool;
            if (pool == null || pool.Count == 0)
                pool = isNight ? FallbackNightPool : FallbackDayPool;
        }
        else
        {
            pool = isNight ? FallbackNightPool : FallbackDayPool;
        }

        int index = (int)(GD.Randi() % pool.Count);
        return pool[index];
    }

    private Vector2 GetSpawnPosition()
    {
        if (_currentPhase == DayPhase.Night || _currentPhase == DayPhase.Dusk)
            return GetNightSpawnPosition();

        return GetDaySpawnPosition();
    }

    /// <summary>Jour : spawn autour du joueur, en dehors du rayon de sécurité du Foyer et not on water.</summary>
    private Vector2 GetDaySpawnPosition()
    {
        for (int attempt = 0; attempt < 15; attempt++)
        {
            float angle = (float)GD.RandRange(0, Mathf.Tau);
            float radius = (float)GD.RandRange(SpawnRadiusMin, SpawnRadiusMax);
            Vector2 position = _player.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            if (position.DistanceTo(_foyerPosition) <= FoyerSafeRadius)
                continue;

            if (IsWaterAt(position))
                continue;

            return position;
        }

        float fallbackAngle = (float)GD.RandRange(0, Mathf.Tau);
        float fallbackRadius = (float)GD.RandRange(SpawnRadiusMin, SpawnRadiusMax);
        return _player.GlobalPosition + new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * fallbackRadius;
    }

    /// <summary>Nuit : spawn depuis les bords, loin du Foyer, convergent ensuite vers lui.</summary>
    private Vector2 GetNightSpawnPosition()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = (float)GD.RandRange(0, Mathf.Tau);
            float radius = NightSpawnRadius;
            Vector2 position = _foyerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            if (!IsWaterAt(position))
                return position;
        }

        float fallbackAngle = (float)GD.RandRange(0, Mathf.Tau);
        return _foyerPosition + new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * NightSpawnRadius;
    }

    private bool IsWaterAt(Vector2 worldPos)
    {
        if (_worldSetup == null)
            CacheWorldSetup();
        return _worldSetup != null && _worldSetup.IsWaterAt(worldPos);
    }

    private void OnDayPhaseChanged(string phase)
    {
        _currentPhase = phase switch
        {
            "Day" => DayPhase.Day,
            "Dusk" => DayPhase.Dusk,
            "Night" => DayPhase.Night,
            "Dawn" => DayPhase.Dawn,
            _ => DayPhase.Day
        };

        if (_currentPhase == DayPhase.Night)
        {
            _currentNight++;
            if (_currentNight >= ColosseMinNight)
                SpawnColosse();
            if (_currentNight >= IndicibleMinNight && !_indicibleSpawned)
                SpawnIndicible();
        }

        if (_currentPhase == DayPhase.Dawn)
            _spawnTimer = 0f;

        GD.Print($"[SpawnManager] Phase → {_currentPhase} (Night #{_currentNight})");
    }

    /// <summary>Spawn un Colosse biome-spécifique au début de la nuit.</summary>
    private void SpawnColosse()
    {
        CachePlayer();
        if (_player == null || !IsInstanceValid(_player))
            return;

        // Trouver le biome au Foyer pour déterminer le type de colosse
        CacheWorldSetup();
        BiomeData biome = _worldSetup?.GetBiomeAt(_foyerPosition);
        string colosseId = FallbackColosse;
        if (biome != null && BiomeColosseMap.TryGetValue(biome.Id, out string mapped))
            colosseId = mapped;

        EnemyData data = EnemyDataLoader.Get(colosseId);
        if (data == null)
            return;

        // Scaling basé sur le temps écoulé + bonus nuit
        float elapsedMinutes = _elapsedTime / 60f;
        float hpScale = Mathf.Pow(_hpScalingPerMinute, elapsedMinutes) * _flatHpMultiplier;
        float dmgScale = Mathf.Pow(_dmgScalingPerMinute, elapsedMinutes) * _flatDmgMultiplier;
        float nightBonus = Mathf.Pow(_nightHpMultiplier, _currentNight - 1);
        float dmgBonus = Mathf.Pow(_nightDmgMultiplier, _currentNight - 1);
        hpScale *= nightBonus;
        dmgScale *= dmgBonus;

        // Position de spawn : bord opposé au joueur par rapport au foyer
        Vector2 dirFromPlayer = (_foyerPosition - _player.GlobalPosition).Normalized();
        if (dirFromPlayer == Vector2.Zero)
            dirFromPlayer = Vector2.Right;
        Vector2 spawnPos = _foyerPosition + dirFromPlayer * NightSpawnRadius;

        Enemy enemy = _pool.Get();
        enemy.GlobalPosition = spawnPos;
        _enemyContainer.AddChild(enemy);
        enemy.Initialize(data, hpScale, dmgScale);
        enemy.SetNightTarget(true, _foyerPosition);

        _eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, colosseId, hpScale, dmgScale);
        GD.Print($"[SpawnManager] Colosse spawned: {colosseId} (Night #{_currentNight}, HP scale: {hpScale:F1}x)");
    }

    /// <summary>Fait émerger L'Indicible au début de la nuit 10.</summary>
    private void SpawnIndicible()
    {
        _indicibleSpawned = true;

        float elapsedMinutes = _elapsedTime / 60f;
        float hpScale = Mathf.Pow(_hpScalingPerMinute, elapsedMinutes) * Mathf.Pow(_nightHpMultiplier, _currentNight - 1) * _flatHpMultiplier;
        float dmgScale = Mathf.Pow(_dmgScalingPerMinute, elapsedMinutes) * Mathf.Pow(_nightDmgMultiplier, _currentNight - 1) * _flatDmgMultiplier;

        Indicible boss = new();
        boss.Name = "Indicible";
        _enemyContainer.AddChild(boss);
        boss.Initialize(hpScale, dmgScale, _foyerPosition);

        GD.Print($"[SpawnManager] L'Indicible émerge... (Night #{_currentNight})");
    }

    private void LoadScalingConfig()
    {
        FileAccess file = FileAccess.Open("res://data/scaling/wave_scaling.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[SpawnManager] Cannot open wave_scaling.json, using defaults");
            SetDefaults();
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[SpawnManager] Parse error: {json.GetErrorMessage()}");
            SetDefaults();
            return;
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        _baseSpawnInterval = (float)dict["base_spawn_interval"].AsDouble();
        _minSpawnInterval = (float)dict["min_spawn_interval"].AsDouble();
        _spawnIntervalDecay = (float)dict["spawn_interval_decay_per_minute"].AsDouble();
        _hpScalingPerMinute = (float)dict["hp_scaling_per_minute"].AsDouble();
        _dmgScalingPerMinute = (float)dict["damage_scaling_per_minute"].AsDouble();
        _maxEnemies = (int)dict["max_enemies_on_screen"].AsDouble();
        _nightHpMultiplier = dict.ContainsKey("night_hp_multiplier") ? (float)dict["night_hp_multiplier"].AsDouble() : 1.25f;
        _nightDmgMultiplier = dict.ContainsKey("night_damage_multiplier") ? (float)dict["night_damage_multiplier"].AsDouble() : 1.15f;
        _nightSpawnRateMultiplier = dict.ContainsKey("night_spawn_rate_multiplier") ? (float)dict["night_spawn_rate_multiplier"].AsDouble() : 0.85f;
        _daySpawnRateMultiplier = dict.ContainsKey("day_spawn_rate_multiplier") ? (float)dict["day_spawn_rate_multiplier"].AsDouble() : 0.8f;
        _duskSpawnRateMultiplier = dict.ContainsKey("dusk_spawn_rate_multiplier") ? (float)dict["dusk_spawn_rate_multiplier"].AsDouble() : 0.65f;
        _nightBaseSpawnRateMultiplier = dict.ContainsKey("night_base_spawn_rate_multiplier") ? (float)dict["night_base_spawn_rate_multiplier"].AsDouble() : 0.3f;

        GD.Print($"[SpawnManager] Config loaded — interval: {_baseSpawnInterval}s, max: {_maxEnemies}, nightHP: x{_nightHpMultiplier}");
    }

    private void SetDefaults()
    {
        _baseSpawnInterval = 2.0f;
        _minSpawnInterval = 0.3f;
        _spawnIntervalDecay = 0.05f;
        _hpScalingPerMinute = 1.08f;
        _dmgScalingPerMinute = 1.05f;
        _maxEnemies = 150;
        _nightHpMultiplier = 1.25f;
        _nightDmgMultiplier = 1.15f;
        _nightSpawnRateMultiplier = 0.85f;
        _daySpawnRateMultiplier = 0.8f;
        _duskSpawnRateMultiplier = 0.65f;
        _nightBaseSpawnRateMultiplier = 0.3f;
    }

    /// <summary>
    /// Override scaling values at runtime for simulation.
    /// Only overrides keys present in the dictionary. Does NOT modify the JSON file on disk.
    /// </summary>
    public void ApplyScalingOverrides(Dictionary<string, float> overrides)
    {
        foreach (KeyValuePair<string, float> kv in overrides)
        {
            switch (kv.Key)
            {
                case "base_spawn_interval": _baseSpawnInterval = kv.Value; break;
                case "min_spawn_interval": _minSpawnInterval = kv.Value; break;
                case "spawn_interval_decay_per_minute": _spawnIntervalDecay = kv.Value; break;
                case "hp_scaling_per_minute": _hpScalingPerMinute = kv.Value; break;
                case "damage_scaling_per_minute": _dmgScalingPerMinute = kv.Value; break;
                case "max_enemies_on_screen": _maxEnemies = (int)kv.Value; break;
                case "night_hp_multiplier": _nightHpMultiplier = kv.Value; break;
                case "night_damage_multiplier": _nightDmgMultiplier = kv.Value; break;
                case "night_spawn_rate_multiplier": _nightSpawnRateMultiplier = kv.Value; break;
                case "day_spawn_rate_multiplier": _daySpawnRateMultiplier = kv.Value; break;
                case "dusk_spawn_rate_multiplier": _duskSpawnRateMultiplier = kv.Value; break;
                case "night_base_spawn_rate_multiplier": _nightBaseSpawnRateMultiplier = kv.Value; break;
                case "flat_hp_multiplier": _flatHpMultiplier = kv.Value; break;
                case "flat_dmg_multiplier": _flatDmgMultiplier = kv.Value; break;
            }
        }
        GD.Print($"[SpawnManager] Applied {overrides.Count} scaling override(s)");
    }

    private void CachePlayer()
    {
        if (_player != null && IsInstanceValid(_player))
            return;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Player p)
            _player = p;
    }

    private void CacheWorldSetup()
    {
        if (_worldSetup != null && IsInstanceValid(_worldSetup))
            return;

        _worldSetup = GetNodeOrNull<WorldSetup>("/root/Main");
    }

    private void CacheRandomEventManager()
    {
        if (_randomEventManager != null && IsInstanceValid(_randomEventManager))
            return;

        _randomEventManager = GetNodeOrNull<RandomEventManager>("../RandomEventManager");
    }
}
