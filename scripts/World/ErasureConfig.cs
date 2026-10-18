using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Réglages de l'Effacement (data/scaling/erasure.json), contrôlés en entier une fois (plan 26 Q7d-2) : tempo de
/// l'oubli, particules, échos, effets par phase ; la section <c>land_front</c> par <see cref="LandFrontConfig"/>. Un
/// fichier refusé arrête le chargement de la run (GameBootstrap). Un champ absent garde la valeur d'avant.
/// </summary>
public sealed class ErasureConfig
{
    public const string FilePath = "res://data/scaling/erasure.json";

    /// <summary>Éclats du Néant qui s'élèvent des décors hauts en zone Effacée (section <c>motes</c>, plan 16 O2).</summary>
    public sealed class MotesConfig
    {
        public bool Enabled { get; init; } = true;
        public float IntervalSec { get; init; } = 0.15f;
        public float MaxMemory { get; init; } = 0.25f;
        public int PropsPerTick { get; init; } = 2;
        public int Count { get; init; } = 3;
        public float SpeedMin { get; init; } = 40f;
        public float SpeedMax { get; init; } = 90f;
        public float LifeMin { get; init; } = 0.8f;
        public float LifeMax { get; init; } = 1.4f;
    }

    public int CellSize { get; init; } = 128;
    public float SeededMemory { get; init; } = 1f;
    public float BaseDecayPerMinute { get; init; } = 0.012f;
    public float AccelerationPerMinute { get; init; } = 0.0004f;
    public float DistanceDecayMultiplier { get; init; } = 0.11f;
    public float DistanceDecayMaxCells { get; init; } = 8f;
    public float GlobalDecayFactor { get; init; } = 2.2f;
    public float PlayerPresenceFalloffCells { get; init; } = 2.5f;
    public float PlayerDecayReduction { get; init; } = 0.72f;
    public float SpawnDensityAtZeroMemory { get; init; } = 2.2f;
    public float EnemySpeedBonusAtZeroMemory { get; init; } = 0.3f;
    public float LateGameThreshold { get; init; } = 0.68f;
    public float UpdateIntervalSec { get; init; } = 0.5f;
    public int TrackedRadiusCells { get; init; } = 14;
    public float SeedDistancePenalty { get; init; } = 0.005f;
    public float StabilizeRadiusCells { get; init; } = 2.5f;
    public float StabilizeMemoryFloor { get; init; } = 0.72f;
    public bool StabilizeOnChestOpen { get; init; } = true;
    public float VoidDamageRatioPerSecond { get; init; } = 0.06f;
    public float CrisisDecayMultiplier { get; init; } = 1.5f;
    /// <summary>Section absente : pas d'éclats (comportement d'avant).</summary>
    public MotesConfig Motes { get; init; }
    public EchoConfig Echoes { get; init; } = new();
    public IReadOnlyDictionary<ErasureManager.ErasureZonePhase, ErasureEffects.Effect> ZoneEffects { get; init; } =
        new Dictionary<ErasureManager.ErasureZonePhase, ErasureEffects.Effect>();

    private static readonly (string Key, ErasureManager.ErasureZonePhase Phase)[] Phases =
    {
        ("fragile", ErasureManager.ErasureZonePhase.Fragile), ("frayed", ErasureManager.ErasureZonePhase.Frayed),
        ("erased", ErasureManager.ErasureZonePhase.Erased), ("void", ErasureManager.ErasureZonePhase.Void),
    };

    private static ErasureConfig _loaded;
    private static string _loadError;
    private static bool _attempted;

    /// <summary>Les réglages du jeu ; refusés, une exception qui nomme le champ (le bootstrap les a déjà contrôlés).</summary>
    public static ErasureConfig Load()
    {
        if (!TryLoad(out ErasureConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    public static bool TryLoad(out ErasureConfig config, out string error)
    {
        if (!_attempted)
        {
            _attempted = true;
            // Une exception imprévue laisse ce diagnostic plutôt qu'un réglage nul sans erreur.
            _loadError = $"{FilePath} : lecture interrompue";
            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            CharacterDataLoader.Load();
            if (file == null)
                _loadError = $"{FilePath} : fichier illisible";
            else if (TryParse(file.GetAsText(), id => CharacterDataLoader.Get(id) != null,
                         id => ResourceLoader.Exists($"res://assets/characters/{id}/char_{id}_S_idle_01.png"), out _loaded, out _loadError))
                _loadError = null;
            else
                _loadError = $"{FilePath} : {_loadError}";
        }
        config = _loaded;
        error = _loadError;
        return error == null;
    }

    /// <summary>
    /// <paramref name="characterExists"/> reconnaît un personnage jouable ; <paramref name="inhabitantExists"/> un habitant
    /// dessiné (dossier assets/characters/&lt;id&gt;), pour les échos.
    /// </summary>
    public static bool TryParse(string text, Func<string, bool> characterExists, Func<string, bool> inhabitantExists,
        out ErasureConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "erasure", "cell_size", "seeded_memory", "base_decay_per_minute", "acceleration_per_minute",
                "distance_decay_multiplier", "distance_decay_max_cells", "global_decay_factor", "player_presence_falloff_cells",
                "player_decay_reduction", "spawn_density_at_zero_memory", "enemy_speed_bonus_at_zero_memory",
                "late_game_threshold", "update_interval_sec", "tracked_radius_cells", "seed_distance_penalty",
                "stabilize_radius_cells", "stabilize_memory_floor", "stabilize_on_chest_open", "void_damage_ratio_per_second",
                "crisis_decay_multiplier", "land_front", "motes", "echoes", "zone_effects");
            if (JsonConfigReader.Has(root, "land_front"))
            {
                LandFrontConfig.Parse(text, out string frontError);
                if (frontError != null)
                    reader.Fail(frontError);
            }
            ErasureConfig defaults = new();
            ErasureConfig parsed = new()
            {
                CellSize = JsonConfigReader.Has(root, "cell_size") ? reader.Integer(root, "cell_size", 8, 1024) : defaults.CellSize,
                SeededMemory = Chance(reader, root, "seeded_memory", defaults.SeededMemory),
                BaseDecayPerMinute = NonNegative(reader, root, "base_decay_per_minute", defaults.BaseDecayPerMinute),
                AccelerationPerMinute = NonNegative(reader, root, "acceleration_per_minute", defaults.AccelerationPerMinute),
                DistanceDecayMultiplier = NonNegative(reader, root, "distance_decay_multiplier", defaults.DistanceDecayMultiplier),
                DistanceDecayMaxCells = NonNegative(reader, root, "distance_decay_max_cells", defaults.DistanceDecayMaxCells),
                GlobalDecayFactor = NonNegative(reader, root, "global_decay_factor", defaults.GlobalDecayFactor),
                PlayerPresenceFalloffCells = Positive(reader, root, "player_presence_falloff_cells", defaults.PlayerPresenceFalloffCells),
                PlayerDecayReduction = Chance(reader, root, "player_decay_reduction", defaults.PlayerDecayReduction),
                SpawnDensityAtZeroMemory = NonNegative(reader, root, "spawn_density_at_zero_memory", defaults.SpawnDensityAtZeroMemory),
                EnemySpeedBonusAtZeroMemory = NonNegative(reader, root, "enemy_speed_bonus_at_zero_memory", defaults.EnemySpeedBonusAtZeroMemory),
                LateGameThreshold = Chance(reader, root, "late_game_threshold", defaults.LateGameThreshold),
                UpdateIntervalSec = Positive(reader, root, "update_interval_sec", defaults.UpdateIntervalSec),
                TrackedRadiusCells = JsonConfigReader.Has(root, "tracked_radius_cells")
                    ? reader.Integer(root, "tracked_radius_cells", 1, 256) : defaults.TrackedRadiusCells,
                SeedDistancePenalty = NonNegative(reader, root, "seed_distance_penalty", defaults.SeedDistancePenalty),
                StabilizeRadiusCells = NonNegative(reader, root, "stabilize_radius_cells", defaults.StabilizeRadiusCells),
                StabilizeMemoryFloor = Chance(reader, root, "stabilize_memory_floor", defaults.StabilizeMemoryFloor),
                StabilizeOnChestOpen = !JsonConfigReader.Has(root, "stabilize_on_chest_open") || reader.Flag(root, "stabilize_on_chest_open"),
                VoidDamageRatioPerSecond = NonNegative(reader, root, "void_damage_ratio_per_second", defaults.VoidDamageRatioPerSecond),
                CrisisDecayMultiplier = Positive(reader, root, "crisis_decay_multiplier", defaults.CrisisDecayMultiplier),
                Motes = JsonConfigReader.Has(root, "motes") ? ReadMotes(reader, reader.Section("motes")) : null,
                Echoes = JsonConfigReader.Has(root, "echoes")
                    ? EchoConfig.Read(reader, reader.Section("echoes"), characterExists, inhabitantExists) : new EchoConfig(),
                ZoneEffects = JsonConfigReader.Has(root, "zone_effects") ? ReadEffects(reader, reader.Section("zone_effects")) : defaults.ZoneEffects,
            };
            error = reader.Error;
            if (error != null)
                return false;
            config = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static MotesConfig ReadMotes(JsonConfigReader reader, JsonElement section)
    {
        reader.AllowOnly(section, "motes", "enabled", "interval_sec", "max_memory", "props_per_tick", "count", "speed_min",
            "speed_max", "life_min", "life_max");
        MotesConfig d = new();
        MotesConfig motes = new()
        {
            Enabled = !JsonConfigReader.Has(section, "enabled") || reader.Flag(section, "enabled"),
            IntervalSec = Positive(reader, section, "interval_sec", d.IntervalSec),
            MaxMemory = Chance(reader, section, "max_memory", d.MaxMemory),
            PropsPerTick = JsonConfigReader.Has(section, "props_per_tick") ? reader.Integer(section, "props_per_tick", 0, 10_000) : d.PropsPerTick,
            Count = JsonConfigReader.Has(section, "count") ? reader.Integer(section, "count", 0, 64) : d.Count,
            SpeedMin = NonNegative(reader, section, "speed_min", d.SpeedMin),
            SpeedMax = NonNegative(reader, section, "speed_max", d.SpeedMax),
            LifeMin = Positive(reader, section, "life_min", d.LifeMin),
            LifeMax = Positive(reader, section, "life_max", d.LifeMax),
        };
        if (reader.Error == null && (motes.SpeedMin > motes.SpeedMax || motes.LifeMin > motes.LifeMax))
            reader.Fail("motes : speed_min ≤ speed_max et life_min ≤ life_max attendus");
        return motes;
    }

    private static Dictionary<ErasureManager.ErasureZonePhase, ErasureEffects.Effect> ReadEffects(JsonConfigReader reader, JsonElement section)
    {
        reader.AllowOnly(section, "zone_effects", "fragile", "frayed", "erased", "void");
        Dictionary<ErasureManager.ErasureZonePhase, ErasureEffects.Effect> effects = new();
        foreach ((string key, ErasureManager.ErasureZonePhase phase) in Phases)
        {
            if (!JsonConfigReader.Has(section, key))
                continue;
            JsonElement entry = reader.Section(section, key);
            reader.AllowOnly(entry, $"zone_effects.{key}", "speed", "damage", "veil", "essence_chance");
            effects[phase] = new ErasureEffects.Effect(
                Positive(reader, entry, "speed", 1f),
                Positive(reader, entry, "damage", 1f),
                Chance(reader, entry, "veil", 0f),
                Chance(reader, entry, "essence_chance", 0f));
        }
        return effects;
    }

    internal static float Positive(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.Positive(owner, key) : fallback;

    internal static float NonNegative(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.NonNegative(owner, key) : fallback;

    internal static float Chance(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.Chance(owner, key) : fallback;
}
