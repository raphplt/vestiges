using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
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

    // Day enemies (basic types)
    private static readonly string[] DayEnemyPool = { "shadow_crawler", "fading_spitter" };
    // Night enemies (all types, including stronger ones)
    private static readonly string[] NightEnemyPool = { "shadow_crawler", "shade", "shade", "fading_spitter", "void_brute", "wailing_sentinel" };

    public override void _Ready()
    {
        EnemyDataLoader.Load();
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
            DayPhase.Day => 0.8f,
            DayPhase.Dusk => 0.55f,
            DayPhase.Night => 0.3f * Mathf.Pow(_nightSpawnRateMultiplier, _currentNight - 1),
            _ => 1f
        };

        return Mathf.Max(baseInterval * nightFactor, _minSpawnInterval);
    }

    private void TrySpawnEnemy(float elapsedMinutes)
    {
        if (_pool.ActiveCount >= _maxEnemies)
            return;

        string enemyId = PickEnemyType();
        EnemyData data = EnemyDataLoader.Get(enemyId);
        if (data == null)
            return;

        float hpScale = Mathf.Pow(_hpScalingPerMinute, elapsedMinutes);
        float dmgScale = Mathf.Pow(_dmgScalingPerMinute, elapsedMinutes);

        if (_currentPhase == DayPhase.Night && _currentNight > 1)
        {
            float nightBonus = Mathf.Pow(_nightHpMultiplier, _currentNight - 1);
            float dmgBonus = Mathf.Pow(_nightDmgMultiplier, _currentNight - 1);
            hpScale *= nightBonus;
            dmgScale *= dmgBonus;
        }

        Enemy enemy = _pool.Get();
        enemy.GlobalPosition = GetSpawnPosition();
        _enemyContainer.AddChild(enemy);
        enemy.Initialize(data, hpScale, dmgScale);
        enemy.SetNightTarget(_currentPhase == DayPhase.Night, _foyerPosition);
    }

    private string PickEnemyType()
    {
        string[] pool = (_currentPhase == DayPhase.Night || _currentPhase == DayPhase.Dusk)
            ? NightEnemyPool
            : DayEnemyPool;

        int index = (int)(GD.Randi() % pool.Length);
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
            _currentNight++;

        if (_currentPhase == DayPhase.Dawn)
            _spawnTimer = 0f;

        GD.Print($"[SpawnManager] Phase → {_currentPhase} (Night #{_currentNight})");
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
        _worldSetup = GetNodeOrNull<WorldSetup>("/root/Main");
    }
}
