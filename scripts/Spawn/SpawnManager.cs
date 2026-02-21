using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Spawn;

public partial class SpawnManager : Node2D
{
    [Export] public float SpawnRadiusMin = 400f;
    [Export] public float SpawnRadiusMax = 600f;
    [Export] public float MeleeWeight = 0.7f;

    private float _baseSpawnInterval;
    private float _minSpawnInterval;
    private float _spawnIntervalDecay;
    private float _hpScalingPerMinute;
    private float _dmgScalingPerMinute;
    private int _maxEnemies;

    private float _elapsedTime;
    private float _spawnTimer;
    private float _currentInterval;

    private EnemyPool _pool;
    private Player _player;
    private List<string> _enemyIds;
    private Node _enemyContainer;

    public override void _Ready()
    {
        EnemyDataLoader.Load();
        LoadScalingConfig();

        _enemyIds = EnemyDataLoader.GetAllIds();
        _currentInterval = _baseSpawnInterval;

        _pool = GetNode<EnemyPool>("../EnemyPool");
        _enemyContainer = GetNode("../EnemyContainer");
    }

    public override void _Process(double delta)
    {
        CachePlayer();
        if (_player == null || !IsInstanceValid(_player))
            return;

        float dt = (float)delta;
        _elapsedTime += dt;
        _spawnTimer += dt;

        float elapsedMinutes = _elapsedTime / 60f;
        _currentInterval = Mathf.Max(
            _baseSpawnInterval - (_spawnIntervalDecay * elapsedMinutes),
            _minSpawnInterval
        );

        if (_spawnTimer >= _currentInterval)
        {
            _spawnTimer = 0f;
            TrySpawnEnemy(elapsedMinutes);
        }
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

        Enemy enemy = _pool.Get();
        enemy.GlobalPosition = GetSpawnPosition();
        _enemyContainer.AddChild(enemy);
        enemy.Initialize(data, hpScale, dmgScale);
    }

    private string PickEnemyType()
    {
        float roll = (float)GD.RandRange(0, 1);

        foreach (string id in _enemyIds)
        {
            EnemyData data = EnemyDataLoader.Get(id);
            if (data == null)
                continue;

            if (data.Type == "melee" && roll < MeleeWeight)
                return id;
            if (data.Type == "ranged" && roll >= MeleeWeight)
                return id;
        }

        return _enemyIds[0];
    }

    private Vector2 GetSpawnPosition()
    {
        float angle = (float)GD.RandRange(0, Mathf.Tau);
        float radius = (float)GD.RandRange(SpawnRadiusMin, SpawnRadiusMax);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        return _player.GlobalPosition + offset;
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

        GD.Print($"[SpawnManager] Config loaded — interval: {_baseSpawnInterval}s, max: {_maxEnemies}");
    }

    private void SetDefaults()
    {
        _baseSpawnInterval = 2.0f;
        _minSpawnInterval = 0.3f;
        _spawnIntervalDecay = 0.05f;
        _hpScalingPerMinute = 1.08f;
        _dmgScalingPerMinute = 1.05f;
        _maxEnemies = 150;
    }

    private void CachePlayer()
    {
        if (_player != null && IsInstanceValid(_player))
            return;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Player p)
            _player = p;
    }
}
