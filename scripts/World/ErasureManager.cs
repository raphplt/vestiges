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
    // Néant (mémoire nulle) : part des PV max perdue par seconde tant que le joueur y reste (Stratégie V2 §8).
    private float _voidDamageRatioPerSecond = 0.06f;
    private float _globalErasurePercent;
    private float _updateTimer;
    private float _totalElapsed;

    private readonly Dictionary<Vector2I, float> _zoneMemory = new();
    private readonly Dictionary<Vector2I, ErasureZonePhase> _zonePhases = new();
    private readonly List<Vector2I> _cellsToUpdate = new();

    // Fenêtre de mémoire autour du joueur, lue par le shader du sol (assets/shaders/ground.gdshader).
    private const int MemoryWindowCells = 32;
    private readonly byte[] _memoryBytes = new byte[MemoryWindowCells * MemoryWindowCells];
    private Image _memoryImage;
    private ImageTexture _memoryTexture;

    private EventBus _eventBus;
    private Player _player;
    private ErasureZonePhase _playerPhase = ErasureZonePhase.Anchored;

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

        _memoryImage = Image.CreateFromData(MemoryWindowCells, MemoryWindowCells, false, Image.Format.R8, _memoryBytes);
        _memoryTexture = ImageTexture.CreateFromImage(_memoryImage);
        RenderingServer.GlobalShaderParameterSet("erasure_memory", _memoryTexture);
        RenderingServer.GlobalShaderParameterSet("erasure_window", Vector4.Zero);
        RenderingServer.GlobalShaderParameterSet("erasure_far_memory", 1f);
    }

    public override void _ExitTree()
    {
        // Le Hub et les runs suivantes repartent d'un monde intact.
        RenderingServer.GlobalShaderParameterSet("erasure_window", Vector4.Zero);
        RenderingServer.GlobalShaderParameterSet("erasure_far_memory", 1f);
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

        _cellsToUpdate.Clear();
        _cellsToUpdate.AddRange(_zoneMemory.Keys);
        foreach (Vector2I cell in _cellsToUpdate)
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

        PublishGroundMemory();
        PublishPlayerPhase();
        HurtPlayerInVoid();
    }

    private void PublishPlayerPhase()
    {
        ErasureZonePhase phase = GetZonePhaseAt(_player.GlobalPosition);
        if (phase == _playerPhase)
            return;
        _playerPhase = phase;
        _eventBus?.EmitSignal(EventBus.SignalName.PlayerErasurePhaseChanged, (int)phase);
    }

    /// <summary>Le Néant se traverse mais consume : dégâts continus, proportionnels aux PV max, à chaque mise à jour.</summary>
    private void HurtPlayerInVoid()
    {
        if (_voidDamageRatioPerSecond <= 0f || GetZonePhaseAt(_player.GlobalPosition) != ErasureZonePhase.Void)
            return;
        float damage = _player.EffectiveMaxHp * _voidDamageRatioPerSecond * _updateIntervalSec;
        _eventBus?.EmitSignal(EventBus.SignalName.PlayerHitBy, "void", damage);
        _player.TakeDamage(damage);
    }

    /// <summary>Recopie la mémoire des zones autour du joueur dans la texture lue par le shader du sol.</summary>
    private void PublishGroundMemory()
    {
        if (_memoryImage == null || _player == null || !IsInstanceValid(_player))
            return;

        Vector2I origin = WorldToCell(_player.GlobalPosition) - new Vector2I(MemoryWindowCells / 2, MemoryWindowCells / 2);
        for (int y = 0; y < MemoryWindowCells; y++)
        {
            for (int x = 0; x < MemoryWindowCells; x++)
            {
                float memory = GetMemoryAtCell(new Vector2I(origin.X + x, origin.Y + y));
                _memoryBytes[y * MemoryWindowCells + x] = (byte)Mathf.RoundToInt(memory * 255f);
            }
        }
        _memoryImage.SetData(MemoryWindowCells, MemoryWindowCells, false, Image.Format.R8, _memoryBytes);
        _memoryTexture.Update(_memoryImage);

        float windowSize = MemoryWindowCells * _cellSize;
        RenderingServer.GlobalShaderParameterSet("erasure_window",
            new Vector4(origin.X * _cellSize, origin.Y * _cellSize, windowSize, windowSize));
        RenderingServer.GlobalShaderParameterSet("erasure_far_memory", GetMemoryAtCell(origin - Vector2I.One));
    }

    /// <summary>Impose la mémoire d'une zone (captures et tests de rendu de l'oubli).</summary>
    internal void OverrideMemory(Vector2I cell, float memory)
    {
        _zoneMemory[cell] = Mathf.Clamp(memory, 0f, 1f);
        UpdateZonePhase(cell, _zoneMemory[cell], true);
    }

    /// <summary>Republie la mémoire et la phase du joueur sans faire avancer l'Effacement (captures).</summary>
    internal void RefreshGroundMemory()
    {
        CachePlayer();
        PublishGroundMemory();
        if (_player != null && IsInstanceValid(_player))
            PublishPlayerPhase();
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

        PublishGroundMemory();
    }

    private Vector2 CellToWorld(Vector2I cell)
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
        _voidDamageRatioPerSecond = dict.ContainsKey("void_damage_ratio_per_second") ? (float)dict["void_damage_ratio_per_second"].AsDouble() : _voidDamageRatioPerSecond;
    }
}
