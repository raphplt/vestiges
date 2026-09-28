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
    private float _baseDecayPerMinute = 0.012f;
    // Oubli du chemin (plan 17 lot 3D) : l'oubli avance plus vite.
    private float _decayMultiplier = 1f;
    private float _globalAccelerationPerMinute = 0.0004f;
    private float _distanceDecayMultiplier = 0.11f;
    // La carte est finie et se retraverse en une minute : sans plafond, une zone quittée s'oubliait jusqu'à ×6,5 plus vite
    // et la carte entière était Néant vers 10 min (mesure du 28 septembre, plan 03 §8).
    private float _distanceDecayMaxCells = 8f;
    // Rythme de l'Effacement global (late game, zones neuves, HUD) rapporté à l'oubli d'une zone.
    private float _globalDecayFactor = 2.2f;
    private float _playerPresenceFalloffCells = 2.5f;
    private float _playerDecayReduction = 0.72f;
    private float _spawnDensityAtZeroMemory = 2.2f;
    private float _enemySpeedBonusAtZeroMemory = 0.3f;
    private float _lateGameThreshold = 0.68f;
    private float _updateIntervalSec = 0.5f;
    private int _trackedRadiusCells = 14;
    // Une zone naît d'autant moins ancrée qu'elle est loin du joueur quand il l'atteint : lisière pâle, sans zones mortes-nées.
    private float _seedDistancePenalty = 0.005f;
    private float _stabilizeRadiusCells = 2.5f;
    private float _stabilizeMemoryFloor = 0.72f;
    // Sans POI (plan 17 lot 0B), ouvrir un coffre est le geste d'exploration qui ravive la mémoire alentour.
    private bool _stabilizeOnChestOpen = true;
    // Néant (mémoire nulle) : part des PV max perdue par seconde tant que le joueur y reste (Stratégie V2 §8).
    private float _voidDamageRatioPerSecond = 0.06f;
    // Résurgence (V2 §8, plan 03 lot C) : pendant une crise, l'oubli s'accélère partout ; il revient au rythme normal après.
    private float _crisisDecayMultiplier = 1.5f;
    private bool _crisisActive;
    private float _globalErasurePercent;
    // Budget de travail, pas règle d'équilibrage : conserver la dette au-delà évite de bloquer une image.
    private const int MaxUpdatesPerFrame = 4;
    private double _updateTimer;
    private double _totalElapsed;

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
        _eventBus.ChestOpened += OnChestOpened;
        _eventBus.MemorialAwakened += OnMemorialAwakened;
        _eventBus.OubliEffectChanged += OnOubliEffectChanged;
        _eventBus.CrisisStarted += OnCrisisStarted;
        _eventBus.CrisisEnded += OnCrisisEnded;

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
            _eventBus.ChestOpened -= OnChestOpened;
            _eventBus.MemorialAwakened -= OnMemorialAwakened;
            _eventBus.OubliEffectChanged -= OnOubliEffectChanged;
            _eventBus.CrisisStarted -= OnCrisisStarted;
            _eventBus.CrisisEnded -= OnCrisisEnded;
        }
    }

    public override void _Process(double delta)
    {
        _totalElapsed += delta;
        _updateTimer += delta;
        // Tolérance au bruit d'addition des deltas, sans faire perdre le reste de temps à chaque pas.
        if (_updateTimer + 1e-9 < _updateIntervalSec)
            return;

        CachePlayer();
        if (_player == null || !IsInstanceValid(_player) || _player.IsDead)
            return;

        SeedAroundPlayer();
        int updates = 0;
        while (_updateTimer + 1e-9 >= _updateIntervalSec && updates < MaxUpdatesPerFrame)
        {
            _updateTimer = System.Math.Max(0, _updateTimer - _updateIntervalSec);
            updates++;
            // Évaluer la montée du déclin à la date de ce pas, pas quatre fois à la fin du hitch.
            AdvanceMemory((float)((_totalElapsed - _updateTimer) / 60.0));
            PublishPlayerPhase();
            HurtPlayerInVoid();
            if (_player.IsDead || GetTree().Paused)
                break;
        }
        // Les phases et dégâts restent séquentiels ; seul l'upload de leur résultat est mutualisé.
        PublishGroundMemory();
    }

    private void AdvanceMemory(float elapsedMinutes)
    {
        float decayPerMinute = (_baseDecayPerMinute + _globalAccelerationPerMinute * elapsedMinutes) * _decayMultiplier;
        float decayAmount = decayPerMinute * (_updateIntervalSec / 60f) * (_crisisActive ? _crisisDecayMultiplier : 1f);
        float previousGlobal = _globalErasurePercent;

        _cellsToUpdate.Clear();
        _cellsToUpdate.AddRange(_zoneMemory.Keys);
        foreach (Vector2I cell in _cellsToUpdate)
        {
            Vector2 worldPos = CellCenterToWorld(cell);
            float playerDistCells = worldPos.DistanceTo(_player.GlobalPosition) / Mathf.Max(_cellSize, 1);
            float proximity = Mathf.Clamp(1f - (playerDistCells / Mathf.Max(_playerPresenceFalloffCells, 0.01f)), 0f, 1f);

            float localDecay = decayAmount * (1f + Mathf.Min(playerDistCells, _distanceDecayMaxCells) * _distanceDecayMultiplier);
            localDecay *= 1f - proximity * _playerDecayReduction;
            localDecay = Mathf.Max(0.0001f, localDecay);

            float next = Mathf.Clamp(_zoneMemory[cell] - localDecay, 0f, 1f);
            _zoneMemory[cell] = next;

            UpdateZonePhase(cell, next);
        }

        _globalErasurePercent = Mathf.Clamp(
            _globalErasurePercent + decayAmount * _globalDecayFactor,
            0f,
            1f);

        if (!Mathf.IsEqualApprox(previousGlobal, _globalErasurePercent))
            _eventBus?.EmitSignal(EventBus.SignalName.ErasureUpdated, _globalErasurePercent);
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
        _player.TakeErasureDamage(damage);
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

    public Vector2 CellCenterToWorld(Vector2I cell)
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
                float seeded = Mathf.Clamp(_seededMemory - _globalErasurePercent * 0.85f - dist * _seedDistancePenalty, 0.08f, 1f);
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

    private void OnOubliEffectChanged(string effect, float total)
    {
        if (effect == "erasure_speed")
            _decayMultiplier = 1f + total;
    }

    private void OnCrisisStarted(int crisisNumber, int intensity) => _crisisActive = true;

    private void OnCrisisEnded(int crisisNumber) => _crisisActive = false;

    private void OnMemorialAwakened(Vector2 position)
    {
        StabilizeZone(position);
    }

    private void OnChestOpened(string chestId, string rarity, Vector2 position)
    {
        if (_stabilizeOnChestOpen)
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
        _distanceDecayMaxCells = dict.ContainsKey("distance_decay_max_cells") ? (float)dict["distance_decay_max_cells"].AsDouble() : _distanceDecayMaxCells;
        _globalDecayFactor = dict.ContainsKey("global_decay_factor") ? (float)dict["global_decay_factor"].AsDouble() : _globalDecayFactor;
        _playerPresenceFalloffCells = dict.ContainsKey("player_presence_falloff_cells") ? (float)dict["player_presence_falloff_cells"].AsDouble() : _playerPresenceFalloffCells;
        _playerDecayReduction = dict.ContainsKey("player_decay_reduction") ? (float)dict["player_decay_reduction"].AsDouble() : _playerDecayReduction;
        _spawnDensityAtZeroMemory = dict.ContainsKey("spawn_density_at_zero_memory") ? (float)dict["spawn_density_at_zero_memory"].AsDouble() : _spawnDensityAtZeroMemory;
        _enemySpeedBonusAtZeroMemory = dict.ContainsKey("enemy_speed_bonus_at_zero_memory") ? (float)dict["enemy_speed_bonus_at_zero_memory"].AsDouble() : _enemySpeedBonusAtZeroMemory;
        _lateGameThreshold = dict.ContainsKey("late_game_threshold") ? (float)dict["late_game_threshold"].AsDouble() : _lateGameThreshold;
        _updateIntervalSec = dict.ContainsKey("update_interval_sec") ? (float)dict["update_interval_sec"].AsDouble() : _updateIntervalSec;
        _trackedRadiusCells = dict.ContainsKey("tracked_radius_cells") ? (int)dict["tracked_radius_cells"].AsDouble() : _trackedRadiusCells;
        _seedDistancePenalty = dict.ContainsKey("seed_distance_penalty") ? (float)dict["seed_distance_penalty"].AsDouble() : _seedDistancePenalty;
        _stabilizeRadiusCells = dict.ContainsKey("stabilize_radius_cells") ? (float)dict["stabilize_radius_cells"].AsDouble() : _stabilizeRadiusCells;
        _stabilizeMemoryFloor = dict.ContainsKey("stabilize_memory_floor") ? (float)dict["stabilize_memory_floor"].AsDouble() : _stabilizeMemoryFloor;
        _stabilizeOnChestOpen = !dict.ContainsKey("stabilize_on_chest_open") || dict["stabilize_on_chest_open"].AsBool();
        _voidDamageRatioPerSecond = dict.ContainsKey("void_damage_ratio_per_second") ? (float)dict["void_damage_ratio_per_second"].AsDouble() : _voidDamageRatioPerSecond;
        _crisisDecayMultiplier = dict.ContainsKey("crisis_decay_multiplier") ? (float)dict["crisis_decay_multiplier"].AsDouble() : _crisisDecayMultiplier;
    }
}
