using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.World;

public partial class ErasureManager : Node
{
    public enum ErasureZonePhase
    {
        Anchored = 0,
        Fragile = 1,
        Frayed = 2,
        Erased = 3,
        Void = 4,
    }

    private int _cellSize = 128;
    private float _seededMemory = 1f;
    private float _baseDecayPerMinute = 0.018f;
    private float _globalAccelerationPerMinute = 0.010f;
    private float _distanceDecayMultiplier = 0.11f;
    private float _playerPresenceFalloffCells = 2.5f;
    private float _playerDecayReduction = 0.72f;
    private float _spawnDensityAtZeroMemory = 2.2f;
    private float _enemySpeedBonusAtZeroMemory = 0.3f;
    private float _lateGameThreshold = 0.68f;
    private float _updateIntervalSec = 0.5f;
    private int _trackedRadiusCells = 14;
    private float _stabilizeRadiusCells = 2.5f;
    private float _stabilizeMemoryFloor = 0.72f;
    private float _globalErasurePercent;
    private float _updateTimer;
    private float _totalElapsed;

    private readonly Dictionary<Vector2I, float> _zoneMemory = new();
    private readonly Dictionary<Vector2I, ErasureZonePhase> _zonePhases = new();

    private EventBus _eventBus;
    private Player _player;
    private ErasureOverlay _overlay;

    public int CellSize => _cellSize;
    public float InitialMemory => _seededMemory;
    public float GlobalErasurePercent => _globalErasurePercent;
    public bool IsLateGame => _globalErasurePercent >= _lateGameThreshold;

    public override void _Ready()
    {
        LoadConfig();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.SouvenirDiscovered += OnSouvenirDiscovered;
        _eventBus.PoiDiscovered += OnPoiDiscovered;

        _overlay = new ErasureOverlay(this, _cellSize)
        {
            Name = "ErasureOverlay",
            ZIndex = -1,
        };
        CallDeferred(Node.MethodName.AddChild, _overlay);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.SouvenirDiscovered -= OnSouvenirDiscovered;
            _eventBus.PoiDiscovered -= OnPoiDiscovered;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _totalElapsed += dt;
        _updateTimer += dt;

        if (_updateTimer < _updateIntervalSec)
            return;

        _updateTimer = 0f;
        CachePlayer();
        if (_player == null || !IsInstanceValid(_player))
            return;

        SeedAroundPlayer();

        float elapsedMinutes = _totalElapsed / 60f;
        float decayPerMinute = _baseDecayPerMinute + _globalAccelerationPerMinute * elapsedMinutes;
        float decayAmount = decayPerMinute * (_updateIntervalSec / 60f);
        float previousGlobal = _globalErasurePercent;

        List<Vector2I> cells = new(_zoneMemory.Keys);
        foreach (Vector2I cell in cells)
        {
            Vector2 worldPos = CellCenterToWorld(cell);
            float playerDistCells = worldPos.DistanceTo(_player.GlobalPosition) / Mathf.Max(_cellSize, 1);
            float proximity = Mathf.Clamp(1f - (playerDistCells / Mathf.Max(_playerPresenceFalloffCells, 0.01f)), 0f, 1f);

            float localDecay = decayAmount * (1f + playerDistCells * _distanceDecayMultiplier);
            localDecay *= 1f - proximity * _playerDecayReduction;
            localDecay = Mathf.Max(0.0001f, localDecay);

            float next = Mathf.Clamp(_zoneMemory[cell] - localDecay, 0f, 1f);
            _zoneMemory[cell] = next;

            UpdateZonePhase(cell, next);
        }

        _globalErasurePercent = Mathf.Clamp(
            _globalErasurePercent + decayAmount * 0.85f,
            0f,
            1f);

        if (!Mathf.IsEqualApprox(previousGlobal, _globalErasurePercent))
            _eventBus?.EmitSignal(EventBus.SignalName.ErasureUpdated, _globalErasurePercent);

        _overlay?.QueueRedraw();
    }

    public float GetMemoryAt(Vector2 worldPos)
    {
        return GetMemoryAtCell(WorldToCell(worldPos));
    }

    public ErasureZonePhase GetZonePhaseAt(Vector2 worldPos)
    {
        Vector2I cell = WorldToCell(worldPos);
        if (_zonePhases.TryGetValue(cell, out ErasureZonePhase phase))
            return phase;
        return ResolvePhase(GetMemoryAtCell(cell));
    }

    public float GetSpawnDensityMultiplier(Vector2 worldPos)
    {
        float memory = GetMemoryAt(worldPos);
        return Mathf.Lerp(_spawnDensityAtZeroMemory, 1f, memory);
    }

    public float GetEnemySpeedBonus(Vector2 worldPos)
    {
        float memory = GetMemoryAt(worldPos);
        return Mathf.Lerp(_enemySpeedBonusAtZeroMemory, 0f, memory);
    }

    public void StabilizeZone(Vector2 worldPos)
    {
        Vector2I center = WorldToCell(worldPos);
        int radius = Mathf.CeilToInt(_stabilizeRadiusCells);
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2I cell = new(center.X + dx, center.Y + dy);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > _stabilizeRadiusCells)
                    continue;

                float current = GetMemoryAtCell(cell);
                float stabilized = Mathf.Max(current, _stabilizeMemoryFloor);
                _zoneMemory[cell] = stabilized;
                UpdateZonePhase(cell, stabilized);
            }
        }

        _overlay?.QueueRedraw();
    }

    public void GetCellsInRect(Rect2 worldRect, List<(Vector2I cell, float memory)> output)
    {
        output.Clear();
        Vector2I min = WorldToCell(worldRect.Position);
        Vector2I max = WorldToCell(worldRect.Position + worldRect.Size);

        for (int x = min.X - 1; x <= max.X + 1; x++)
        {
            for (int y = min.Y - 1; y <= max.Y + 1; y++)
            {
                Vector2I cell = new(x, y);
                output.Add((cell, GetMemoryAtCell(cell)));
            }
        }
    }

    public Vector2 CellToWorld(Vector2I cell)
    {
        return new Vector2(cell.X * _cellSize, cell.Y * _cellSize);
    }

    private Vector2 CellCenterToWorld(Vector2I cell)
    {
        return CellToWorld(cell) + new Vector2(_cellSize * 0.5f, _cellSize * 0.5f);
    }

    private void SeedAroundPlayer()
    {
        Vector2I playerCell = WorldToCell(_player.GlobalPosition);
        for (int dx = -_trackedRadiusCells; dx <= _trackedRadiusCells; dx++)
        {
            for (int dy = -_trackedRadiusCells; dy <= _trackedRadiusCells; dy++)
            {
                Vector2I cell = new(playerCell.X + dx, playerCell.Y + dy);
                if (_zoneMemory.ContainsKey(cell))
                    continue;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float seeded = Mathf.Clamp(_seededMemory - _globalErasurePercent * 0.85f - dist * 0.012f, 0.08f, 1f);
                _zoneMemory[cell] = seeded;
                UpdateZonePhase(cell, seeded, true);
            }
        }
    }

    private float GetMemoryAtCell(Vector2I cell)
    {
        if (_zoneMemory.TryGetValue(cell, out float value))
            return value;
        return Mathf.Clamp(_seededMemory - _globalErasurePercent * 0.85f, 0f, 1f);
    }

    private void UpdateZonePhase(Vector2I cell, float memory, bool silent = false)
    {
        ErasureZonePhase phase = ResolvePhase(memory);
        if (_zonePhases.TryGetValue(cell, out ErasureZonePhase previous) && previous == phase)
            return;

        _zonePhases[cell] = phase;
        if (!silent)
            _eventBus?.EmitSignal(EventBus.SignalName.ZonePhaseChanged, cell.X, cell.Y, (int)phase);
    }

    private static ErasureZonePhase ResolvePhase(float memory)
    {
        if (memory <= 0f)
            return ErasureZonePhase.Void;
        if (memory <= 0.25f)
            return ErasureZonePhase.Erased;
        if (memory <= 0.50f)
            return ErasureZonePhase.Frayed;
        if (memory <= 0.75f)
            return ErasureZonePhase.Fragile;
        return ErasureZonePhase.Anchored;
    }

    private Vector2I WorldToCell(Vector2 worldPos)
    {
        return new Vector2I(
            Mathf.FloorToInt(worldPos.X / _cellSize),
            Mathf.FloorToInt(worldPos.Y / _cellSize));
    }

    private void CachePlayer()
    {
        if (_player != null && IsInstanceValid(_player))
            return;

        _player = GetTree().GetFirstNodeInGroup("player") as Player;
    }

    private void OnSouvenirDiscovered(string souvenirId, string souvenirName, string constellationId)
    {
        CachePlayer();
        if (_player != null)
            StabilizeZone(_player.GlobalPosition);
    }

    private void OnPoiDiscovered(string poiId, string poiType, Vector2 position)
    {
        StabilizeZone(position);
    }

    private void LoadConfig()
    {
        FileAccess file = FileAccess.Open("res://data/scaling/erasure.json", FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
            return;

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        _cellSize = dict.ContainsKey("cell_size") ? (int)dict["cell_size"].AsDouble() : _cellSize;
        _seededMemory = dict.ContainsKey("seeded_memory") ? (float)dict["seeded_memory"].AsDouble() : _seededMemory;
        _baseDecayPerMinute = dict.ContainsKey("base_decay_per_minute") ? (float)dict["base_decay_per_minute"].AsDouble() : _baseDecayPerMinute;
        _globalAccelerationPerMinute = dict.ContainsKey("acceleration_per_minute") ? (float)dict["acceleration_per_minute"].AsDouble() : _globalAccelerationPerMinute;
        _distanceDecayMultiplier = dict.ContainsKey("distance_decay_multiplier") ? (float)dict["distance_decay_multiplier"].AsDouble() : _distanceDecayMultiplier;
        _playerPresenceFalloffCells = dict.ContainsKey("player_presence_falloff_cells") ? (float)dict["player_presence_falloff_cells"].AsDouble() : _playerPresenceFalloffCells;
        _playerDecayReduction = dict.ContainsKey("player_decay_reduction") ? (float)dict["player_decay_reduction"].AsDouble() : _playerDecayReduction;
        _spawnDensityAtZeroMemory = dict.ContainsKey("spawn_density_at_zero_memory") ? (float)dict["spawn_density_at_zero_memory"].AsDouble() : _spawnDensityAtZeroMemory;
        _enemySpeedBonusAtZeroMemory = dict.ContainsKey("enemy_speed_bonus_at_zero_memory") ? (float)dict["enemy_speed_bonus_at_zero_memory"].AsDouble() : _enemySpeedBonusAtZeroMemory;
        _lateGameThreshold = dict.ContainsKey("late_game_threshold") ? (float)dict["late_game_threshold"].AsDouble() : _lateGameThreshold;
        _updateIntervalSec = dict.ContainsKey("update_interval_sec") ? (float)dict["update_interval_sec"].AsDouble() : _updateIntervalSec;
        _trackedRadiusCells = dict.ContainsKey("tracked_radius_cells") ? (int)dict["tracked_radius_cells"].AsDouble() : _trackedRadiusCells;
        _stabilizeRadiusCells = dict.ContainsKey("stabilize_radius_cells") ? (float)dict["stabilize_radius_cells"].AsDouble() : _stabilizeRadiusCells;
        _stabilizeMemoryFloor = dict.ContainsKey("stabilize_memory_floor") ? (float)dict["stabilize_memory_floor"].AsDouble() : _stabilizeMemoryFloor;
    }
}
