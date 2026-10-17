using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Cadence des micro-événements autour des Résurgences.</summary>
public class RunEventSchedule
{
    public float FirstEventMinSec = 70f;
    public float FirstEventMaxSec = 95f;
    public float GapMinSec = 75f;
    public float GapMaxSec = 115f;
    public float CrisisMarginSec = 15f;
    public float AfterCrisisCalmSec = 40f;
    public int RecentMemory = 2;
    public float RecentWeightFactor = 0.35f;
    public float ProgressEmitIntervalSec = 0.1f;
    /// <summary>Rendez-vous fixes (plan 30 T2), triés par heure : l'événement part à son heure, hors tirage.</summary>
    public readonly List<FixedRunEvent> FixedEvents = new();
}

/// <summary>Un micro-événement à heure fixe : il part dès que la run le permet après <see cref="AtSec"/>.</summary>
public readonly record struct FixedRunEvent(float AtSec, string EventId);

/// <summary>
/// Définition d'un micro-événement. Les champs communs sont typés ; les réglages propres
/// à chaque type (<see cref="Kind"/>) sont lus par nom avec une valeur par défaut.
/// </summary>
public class RunEventData
{
    public string Id;
    public string Kind;
    public string TitleKey;
    public string ObjectiveKey;
    public float MinSec;
    public float Weight = 1f;
    public float DurationSec = 45f;
    public readonly Dictionary<string, float> Numbers = new();
    public readonly Dictionary<string, string> Texts = new();
    public readonly Dictionary<string, List<string>> Lists = new();

    public float Number(string key, float fallback) => Numbers.TryGetValue(key, out float value) ? value : fallback;
    public string Text(string key, string fallback = "") => Texts.TryGetValue(key, out string value) ? value : fallback;
    public List<string> List(string key) => Lists.TryGetValue(key, out List<string> value) ? value : new List<string>();
}

public static class RunEventDataLoader
{
    public const string DataPath = "res://data/events/run_events.json";
    private static readonly List<RunEventData> _events = new();
    private static bool _attempted;
    private static string _loadError;

    /// <summary>
    /// Réglages propres à chaque sorte (plan 26 Q7d-3), calqués sur les lectures de scripts/Events/RunEvents : nombres
    /// positifs ou nuls, coffres du catalogue, listes de créatures connues. Un réglage absent garde le secours du code.
    /// </summary>
    private static readonly Dictionary<string, (string[] Numbers, string[] Chests, string[] Enemies)> Kinds = new()
    {
        ["hunt"] = (new[] { "escort_base", "escort_max", "escort_per_minute", "late_reward_after_sec", "reward_essence",
            "spawn_distance_min", "spawn_distance_max" }, new[] { "late_reward_chest" }, Array.Empty<string>()),
        ["stampede"] = (new[] { "band_width", "count_base", "count_max", "count_per_minute", "reward_essence", "start_distance",
            "success_ratio", "travel_speed_mult", "xp_mult" }, new[] { "reward_chest" }, new[] { "preferred_enemies" }),
        ["fallen_relic"] = (new[] { "elite_guardian_after_sec", "fall_delay_sec", "guardians_base", "guardians_max",
            "guardians_per_minute", "impact_enemy_damage", "impact_player_damage_ratio", "impact_radius", "pickup_delay_sec",
            "pickup_radius", "reward_essence", "reward_heal_ratio", "reward_level_ratio", "spawn_distance_min",
            "spawn_distance_max" }, Array.Empty<string>(), Array.Empty<string>()),
        ["vigil"] = (new[] { "drain_per_sec", "hold_sec", "pressure_count", "pressure_interval_sec", "radius", "reward_essence",
            "reward_heal_ratio", "spawn_distance_min", "spawn_distance_max" }, new[] { "reward_chest" }, Array.Empty<string>()),
        ["shard_rain"] = (new[] { "enemy_damage_base", "enemy_damage_growth_per_minute", "essence_per_kills",
            "player_damage_ratio", "strike_delay_sec", "strike_interval_sec", "strike_radius", "strike_range",
            "target_enemy_ratio", "target_player_ratio" }, Array.Empty<string>(), Array.Empty<string>()),
    };
    private static readonly string[] CommonKeys = { "id", "kind", "title_key", "objective_key", "min_sec", "weight", "duration_sec" };

    public static RunEventSchedule Schedule { get; private set; } = new();
    public static IReadOnlyList<RunEventData> Events
    {
        get
        {
            Load();
            return _events;
        }
    }

    /// <summary>Hors run (bilan), un fichier refusé laisse la liste vide ; en run, le bootstrap l'exige (<see cref="TryLoad"/>).</summary>
    public static void Load()
    {
        if (!TryLoad(out string error))
            GD.PushError($"[RunEventDataLoader] {error}");
    }

    /// <summary>Tout le catalogue publié, ou un diagnostic fichier/champ sans publication.</summary>
    public static bool TryLoad(out string error)
    {
        if (!_attempted)
        {
            _attempted = true;
            _loadError = $"{DataPath} : lecture interrompue";
            using FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
            EnemyDataLoader.Load();
            if (file == null)
                _loadError = $"{DataPath} : fichier illisible";
            else if (ChestDataLoader.TryLoad(out string chestError))
            {
                _loadError = Apply(file.GetAsText(), id => ChestDataLoader.Get(id) != null, EnemyDataLoader.Exists,
                    key => TranslationServer.Translate(key) != key);
                if (_loadError != null)
                    _loadError = $"{DataPath} : {_loadError}";
                else
                    GD.Print($"[RunEventDataLoader] Loaded {_events.Count} run events, {Schedule.FixedEvents.Count} fixed");
            }
            else
                _loadError = $"{DataPath} : {chestError}";
        }
        error = _loadError;
        return error == null;
    }

    /// <summary>Lit et publie le catalogue ; un refus ne publie rien et rend le diagnostic.</summary>
    public static string Apply(string text, Func<string, bool> chestExists, Func<string, bool> enemyExists,
        Func<string, bool> translated)
    {
        if (!TryParse(text, chestExists, enemyExists, translated, out RunEventSchedule schedule, out List<RunEventData> events,
                out string error))
            return error;
        Schedule = schedule;
        _events.Clear();
        _events.AddRange(events);
        return null;
    }

    public static bool TryParse(string text, Func<string, bool> chestExists, Func<string, bool> enemyExists,
        Func<string, bool> translated, out RunEventSchedule schedule, out List<RunEventData> events, out string error)
    {
        schedule = null;
        events = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "run_events", "schedule", "events");
            List<RunEventData> parsed = new();
            foreach (JsonElement item in reader.List(reader.Root, "events", 1))
            {
                RunEventData data = ReadEvent(reader, item, chestExists, enemyExists, translated);
                if (data == null)
                    break;
                if (parsed.Exists(other => other.Id == data.Id))
                    reader.Fail($"events : id « {data.Id} » en double");
                parsed.Add(data);
            }
            RunEventSchedule cadence = ReadSchedule(reader, reader.Section("schedule"), id => parsed.Exists(data => data.Id == id));
            error = reader.Error;
            if (error != null)
                return false;
            schedule = cadence;
            events = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    public static RunEventData Find(string eventId)
    {
        foreach (RunEventData data in Events)
            if (data.Id == eventId)
                return data;
        return null;
    }

    private static RunEventSchedule ReadSchedule(JsonConfigReader reader, JsonElement section, Func<string, bool> eventExists)
    {
        reader.AllowOnly(section, "schedule", "first_event_min_sec", "first_event_max_sec", "gap_min_sec", "gap_max_sec",
            "crisis_margin_sec", "after_crisis_calm_sec", "recent_memory", "recent_weight_factor", "progress_emit_interval_sec",
            "fixed_events");
        RunEventSchedule d = new();
        RunEventSchedule schedule = new()
        {
            FirstEventMinSec = NonNegative(reader, section, "first_event_min_sec", d.FirstEventMinSec),
            FirstEventMaxSec = NonNegative(reader, section, "first_event_max_sec", d.FirstEventMaxSec),
            GapMinSec = NonNegative(reader, section, "gap_min_sec", d.GapMinSec),
            GapMaxSec = NonNegative(reader, section, "gap_max_sec", d.GapMaxSec),
            CrisisMarginSec = NonNegative(reader, section, "crisis_margin_sec", d.CrisisMarginSec),
            AfterCrisisCalmSec = NonNegative(reader, section, "after_crisis_calm_sec", d.AfterCrisisCalmSec),
            RecentMemory = JsonConfigReader.Has(section, "recent_memory") ? reader.Integer(section, "recent_memory", 0, 10) : d.RecentMemory,
            RecentWeightFactor = JsonConfigReader.Has(section, "recent_weight_factor")
                ? reader.Chance(section, "recent_weight_factor") : d.RecentWeightFactor,
            ProgressEmitIntervalSec = JsonConfigReader.Has(section, "progress_emit_interval_sec")
                ? reader.Positive(section, "progress_emit_interval_sec") : d.ProgressEmitIntervalSec,
        };
        if (reader.Error == null && (schedule.FirstEventMinSec > schedule.FirstEventMaxSec || schedule.GapMinSec > schedule.GapMaxSec))
            reader.Fail("schedule : first_event_min_sec ≤ first_event_max_sec et gap_min_sec ≤ gap_max_sec attendus");
        if (!JsonConfigReader.Has(section, "fixed_events"))
            return schedule;
        foreach (JsonElement entry in reader.List(section, "fixed_events", 0))
        {
            reader.AllowOnly(entry, "fixed_events", "at_sec", "id");
            float at = reader.Positive(entry, "at_sec");
            string id = reader.Text(entry, "id");
            if (id != null && !eventExists(id))
                reader.Fail($"fixed_events : événement « {id} » inconnu");
            schedule.FixedEvents.Add(new FixedRunEvent(at, id));
        }
        schedule.FixedEvents.Sort((a, b) => a.AtSec.CompareTo(b.AtSec));
        return schedule;
    }

    private static RunEventData ReadEvent(JsonConfigReader reader, JsonElement item, Func<string, bool> chestExists,
        Func<string, bool> enemyExists, Func<string, bool> translated)
    {
        string kind = reader.OneOf(item, "kind", Kinds.Keys);
        if (kind == null)
            return null;
        (string[] numbers, string[] chests, string[] enemies) = Kinds[kind];
        List<string> allowed = new(CommonKeys);
        allowed.AddRange(numbers);
        allowed.AddRange(chests);
        allowed.AddRange(enemies);
        reader.AllowOnly(item, $"events.{kind}", allowed.ToArray());
        RunEventData data = new()
        {
            Id = reader.Text(item, "id"),
            Kind = kind,
            TitleKey = reader.Text(item, "title_key"),
            ObjectiveKey = reader.Text(item, "objective_key"),
            MinSec = NonNegative(reader, item, "min_sec", 0f),
            Weight = NonNegative(reader, item, "weight", 1f),
            DurationSec = JsonConfigReader.Has(item, "duration_sec") ? reader.Positive(item, "duration_sec") : 45f,
        };
        foreach (string key in new[] { "title_key", "objective_key" })
        {
            string translation = key == "title_key" ? data.TitleKey : data.ObjectiveKey;
            if (translation != null && !translated(translation))
                reader.Fail($"events.{data.Id}.{key} : clé « {translation} » sans traduction");
        }
        // Mêmes tables qu'avant : chaque nombre, texte ou liste présent, réglages communs compris.
        foreach (string key in CommonKeys)
        {
            if (!JsonConfigReader.Has(item, key))
                continue;
            if (key is "id" or "kind" or "title_key" or "objective_key")
                data.Texts[key] = item.GetProperty(key).GetString();
        }
        foreach (string key in new[] { "min_sec", "weight", "duration_sec" })
            if (JsonConfigReader.Has(item, key))
                data.Numbers[key] = key switch { "min_sec" => data.MinSec, "weight" => data.Weight, _ => data.DurationSec };
        foreach (string key in numbers)
            if (JsonConfigReader.Has(item, key))
                data.Numbers[key] = reader.NonNegative(item, key);
        foreach (string key in chests)
        {
            if (!JsonConfigReader.Has(item, key))
                continue;
            string chest = reader.Text(item, key);
            if (chest != null && !chestExists(chest))
                reader.Fail($"events.{data.Id}.{key} : coffre « {chest} » inconnu");
            data.Texts[key] = chest;
        }
        foreach (string key in enemies)
        {
            if (!JsonConfigReader.Has(item, key))
                continue;
            List<string> ids = new();
            foreach (JsonElement enemy in reader.List(item, key, 0))
            {
                string id = enemy.ValueKind == JsonValueKind.String ? enemy.GetString() : null;
                if (string.IsNullOrEmpty(id) || !enemyExists(id))
                    reader.Fail($"events.{data.Id}.{key} : créature « {id} » inconnue");
                else
                    ids.Add(id);
            }
            data.Lists[key] = ids;
        }
        if (reader.Error == null && data.Numbers.TryGetValue("spawn_distance_min", out float near)
            && data.Numbers.TryGetValue("spawn_distance_max", out float far) && near > far)
            reader.Fail($"events.{data.Id} : spawn_distance_min ≤ spawn_distance_max attendu");
        return data;
    }

    private static float NonNegative(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.NonNegative(owner, key) : fallback;
}
