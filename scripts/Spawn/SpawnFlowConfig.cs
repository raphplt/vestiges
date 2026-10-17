using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Spawn;

/// <summary>
/// Flux d'apparition et montée des créatures (data/scaling/spawn_flow.json), lu strictement une fois (plan 26 Q7e-1).
/// Chaque réglage numérique est déclaré dans <see cref="Schema"/> avec sa règle et le secours qu'appliquait
/// SpawnManager ; une clé inconnue, une valeur hors règle, une créature de secours inconnue refusent le fichier, et le
/// chargement de la run s'arrête (GameBootstrap). Lu au chargement seulement : aucune recherche par nom en jeu.
/// </summary>
public sealed class SpawnFlowConfig
{
    public const string FilePath = "res://data/scaling/spawn_flow.json";

    private enum Rule { Positive, NonNegative, Chance, Integer, Angle }

    /// <summary>Secours nul : réglage obligatoire.</summary>
    private readonly record struct Entry(string Key, Rule Rule, float? Fallback, int Min = 0, int Max = 0);

    private static readonly Entry[] Schema =
    {
        new("base_spawn_interval", Rule.Positive, null),
        new("min_spawn_interval", Rule.Positive, null),
        new("spawn_interval_decay_per_minute", Rule.NonNegative, null),
        new("max_enemies_on_screen", Rule.Integer, null, 1, 5000),
        new("flat_hp_multiplier", Rule.Positive, 1f),
        new("flat_dmg_multiplier", Rule.Positive, 1f),
        new("ranged_damage_growth_share", Rule.NonNegative, 1f),
        new("xp_growth_per_minute", Rule.NonNegative, 0f),
        new("xp_oblivion_bonus", Rule.NonNegative, 0f),
        new("xp_crisis_multiplier", Rule.Positive, 1f),
        new("max_enemies_growth_per_minute", Rule.NonNegative, 0f),
        new("enemy_speed_base_multiplier", Rule.Positive, 1f),
        new("enemy_speed_growth_per_minute", Rule.NonNegative, 0f),
        new("enemy_speed_melee_bonus", Rule.Positive, 1f),
        new("enemy_speed_ranged_bonus", Rule.Positive, 1f),
        new("enemy_speed_elite_bonus", Rule.Positive, 1f),
        new("enemy_aggression_base_multiplier", Rule.Positive, 1f),
        new("enemy_aggression_growth_per_minute", Rule.NonNegative, 0f),
        new("enemy_aggression_melee_bonus", Rule.Positive, 1f),
        new("enemy_aggression_ranged_bonus", Rule.Positive, 1f),
        new("enemy_aggression_elite_bonus", Rule.Positive, 1f),
        new("day_enemy_despawn_distance", Rule.NonNegative, 1400f),
        new("local_enemy_radius", Rule.NonNegative, 900f),
        new("local_enemy_target_base", Rule.NonNegative, 24f),
        new("opening_grace_seconds", Rule.NonNegative, 0f),
        new("opening_ramp_seconds", Rule.NonNegative, 0f),
        // Secours : local_enemy_target_base (lu avant).
        new("opening_local_target_start", Rule.NonNegative, float.NaN),
        new("opening_interval_multiplier_start", Rule.Positive, 1f),
        new("early_density_multiplier", Rule.Positive, 1f),
        new("early_density_until_minute", Rule.NonNegative, 0f),
        new("early_density_ramp_minutes", Rule.NonNegative, 0f),
        new("early_xp_multiplier", Rule.Positive, 1f),
        new("local_enemy_target_growth_per_minute", Rule.NonNegative, 6f),
        new("local_spawn_burst_max", Rule.Integer, 4f, 0, 100),
        new("same_type_cluster_chance", Rule.Chance, 0.4f),
        new("same_type_cluster_min", Rule.Integer, 2f, 1, 50),
        new("same_type_cluster_max", Rule.Integer, 4f, 1, 50),
        new("same_type_cluster_spacing_min", Rule.NonNegative, 18f),
        new("same_type_cluster_spacing_max", Rule.NonNegative, 46f),
        new("crisis_spawn_multiplier", Rule.Positive, 1.65f),
        new("crisis_cap_multiplier", Rule.Positive, 1f),
        new("crisis_burst_base", Rule.Integer, 8f, 0, 500),
        new("crisis_burst_per_intensity", Rule.Integer, 4f, 0, 500),
        new("late_game_spawn_multiplier", Rule.Positive, 1.4f),
        new("endgame_spawn_multiplier", Rule.Positive, 1.85f),
        new("crisis_hp_multiplier", Rule.Positive, 1.18f),
        new("crisis_damage_multiplier", Rule.Positive, 1.12f),
        new("late_game_hp_multiplier", Rule.Positive, 1.35f),
        new("late_game_damage_multiplier", Rule.Positive, 1.22f),
        new("endgame_hp_multiplier", Rule.Positive, 1.55f),
        new("endgame_damage_multiplier", Rule.Positive, 1.32f),
        new("time_peril_from_minute", Rule.NonNegative, 0f),
        new("time_peril_per_minute", Rule.NonNegative, 0f),
        new("spawn_screen_margin_min", Rule.NonNegative, 40f),
        new("spawn_screen_margin_max", Rule.NonNegative, 140f),
        new("spawn_forward_bias", Rule.Chance, 0.5f),
        new("spawn_forward_arc_degrees", Rule.Angle, 140f),
        new("spawn_speed_lead_seconds", Rule.NonNegative, 0f),
        new("spawn_min_distance_px", Rule.NonNegative, 0f),
    };

    /// <summary>Paires qui doivent rester ordonnées : (bas, haut).</summary>
    private static readonly (string Low, string High)[] Ordered =
    {
        ("min_spawn_interval", "base_spawn_interval"), ("same_type_cluster_min", "same_type_cluster_max"),
        ("same_type_cluster_spacing_min", "same_type_cluster_spacing_max"), ("spawn_screen_margin_min", "spawn_screen_margin_max"),
    };

    private readonly Dictionary<string, float> _values = new();

    public IReadOnlyList<string> FallbackExplorationPool { get; private init; } = Array.Empty<string>();
    public IReadOnlyList<string> FallbackResurgencePool { get; private init; } = Array.Empty<string>();
    /// <summary>Segments de montée des PV et des dégâts : (à partir de la minute, facteur par minute), dans l'ordre du fichier.</summary>
    public IReadOnlyList<(float FromMinute, float PerMinute)> HpSegments { get; private init; } = Array.Empty<(float, float)>();
    public IReadOnlyList<(float FromMinute, float PerMinute)> DamageSegments { get; private init; } = Array.Empty<(float, float)>();

    public float Number(string key) => _values[key];
    public int Integer(string key) => (int)_values[key];

    private static SpawnFlowConfig _loaded;
    private static string _loadError;
    private static bool _attempted;

    /// <summary>Les réglages du jeu ; refusés, une exception qui nomme le champ (le bootstrap les a déjà contrôlés).</summary>
    public static SpawnFlowConfig Load()
    {
        if (!TryLoad(out SpawnFlowConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    public static bool TryLoad(out SpawnFlowConfig config, out string error)
    {
        if (!_attempted)
        {
            _attempted = true;
            _loadError = $"{FilePath} : lecture interrompue";
            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            if (file == null)
                _loadError = $"{FilePath} : fichier illisible";
            else if (TryParse(file.GetAsText(), EnemyDataLoader.Exists, out _loaded, out _loadError))
                _loadError = null;
            else
                _loadError = $"{FilePath} : {_loadError}";
        }
        config = _loaded;
        error = _loadError;
        return error == null;
    }

    /// <summary><paramref name="enemyExists"/> reconnaît une créature du catalogue (groupes de secours, répétitions admises).</summary>
    public static bool TryParse(string text, Func<string, bool> enemyExists, out SpawnFlowConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            List<string> keys = new() { "fallback_exploration_pool", "fallback_resurgence_pool", "hp_scaling_segments", "damage_scaling_segments" };
            foreach (Entry entry in Schema)
                keys.Add(entry.Key);
            reader.AllowOnly(root, "spawn_flow", keys.ToArray());
            SpawnFlowConfig parsed = new()
            {
                FallbackExplorationPool = Pool(reader, root, "fallback_exploration_pool", enemyExists),
                FallbackResurgencePool = Pool(reader, root, "fallback_resurgence_pool", enemyExists),
                HpSegments = Segments(reader, root, "hp_scaling_segments"),
                DamageSegments = Segments(reader, root, "damage_scaling_segments"),
            };
            foreach (Entry entry in Schema)
                parsed._values[entry.Key] = Read(reader, root, entry, parsed._values);
            foreach ((string low, string high) in Ordered)
            {
                if (reader.Error == null && parsed._values[low] > parsed._values[high])
                    reader.Fail(FormattableString.Invariant($"{low} ≤ {high} attendu ({parsed._values[low]} > {parsed._values[high]})"));
            }
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

    private static float Read(JsonConfigReader reader, JsonElement root, Entry entry, Dictionary<string, float> values)
    {
        if (!JsonConfigReader.Has(root, entry.Key) && entry.Fallback is float fallback)
            return float.IsNaN(fallback) ? values["local_enemy_target_base"] : fallback;
        return entry.Rule switch
        {
            Rule.Positive => reader.Positive(root, entry.Key),
            Rule.NonNegative => reader.NonNegative(root, entry.Key),
            Rule.Chance => reader.Chance(root, entry.Key),
            Rule.Integer => reader.Integer(root, entry.Key, entry.Min, entry.Max),
            _ => Angle(reader, root, entry.Key),
        };
    }

    private static float Angle(JsonConfigReader reader, JsonElement root, string key)
    {
        float degrees = reader.NonNegative(root, key);
        if (reader.Error == null && degrees > 360f)
            reader.Fail(FormattableString.Invariant($"{key} : {degrees} (angle de 0 à 360 degrés attendu)"));
        return degrees;
    }

    /// <summary>Groupe de secours hors biome : créatures connues, répétées pour pondérer le tirage.</summary>
    private static List<string> Pool(JsonConfigReader reader, JsonElement root, string key, Func<string, bool> enemyExists)
    {
        List<string> pool = new();
        foreach (JsonElement item in reader.List(root, key, 1))
        {
            string id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (string.IsNullOrEmpty(id) || !enemyExists(id))
                reader.Fail($"{key} : créature « {id} » inconnue");
            else
                pool.Add(id);
        }
        return pool;
    }

    /// <summary>Segments absents ou vides : pas de croissance avec le temps (facteur 1 dès la minute 0), comme avant.</summary>
    private static List<(float, float)> Segments(JsonConfigReader reader, JsonElement root, string key)
    {
        List<(float, float)> segments = new();
        if (JsonConfigReader.Has(root, key))
        {
            foreach (JsonElement item in reader.List(root, key, 0))
            {
                reader.AllowOnly(item, key, "from_minute", "per_minute");
                segments.Add((reader.NonNegative(item, "from_minute"), reader.Positive(item, "per_minute")));
            }
        }
        if (segments.Count == 0)
            segments.Add((0f, 1f));
        return segments;
    }
}
