using System.Collections.Generic;
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
}

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
    private const string DataPath = "res://data/events/run_events.json";
    private static readonly List<RunEventData> _events = new();
    private static bool _loaded;

    public static RunEventSchedule Schedule { get; private set; } = new();
    public static IReadOnlyList<RunEventData> Events
    {
        get
        {
            Load();
            return _events;
        }
    }

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;

        FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[RunEventDataLoader] Cannot open {DataPath}");
            return;
        }

        Json json = new();
        Error error = json.Parse(file.GetAsText());
        file.Close();
        if (error != Error.Ok)
        {
            GD.PushError($"[RunEventDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        Godot.Collections.Dictionary schedule = root["schedule"].AsGodotDictionary();
        Schedule = new RunEventSchedule
        {
            FirstEventMinSec = Float(schedule, "first_event_min_sec", 70f),
            FirstEventMaxSec = Float(schedule, "first_event_max_sec", 95f),
            GapMinSec = Float(schedule, "gap_min_sec", 75f),
            GapMaxSec = Float(schedule, "gap_max_sec", 115f),
            CrisisMarginSec = Float(schedule, "crisis_margin_sec", 15f),
            AfterCrisisCalmSec = Float(schedule, "after_crisis_calm_sec", 40f),
            RecentMemory = (int)Float(schedule, "recent_memory", 2f),
            RecentWeightFactor = Float(schedule, "recent_weight_factor", 0.35f),
            ProgressEmitIntervalSec = Float(schedule, "progress_emit_interval_sec", 0.1f)
        };

        foreach (Variant item in root["events"].AsGodotArray())
            _events.Add(ParseEvent(item.AsGodotDictionary()));

        GD.Print($"[RunEventDataLoader] Loaded {_events.Count} run events");
    }

    private static RunEventData ParseEvent(Godot.Collections.Dictionary dict)
    {
        RunEventData data = new()
        {
            Id = dict["id"].AsString(),
            Kind = dict["kind"].AsString(),
            TitleKey = dict["title_key"].AsString(),
            ObjectiveKey = dict["objective_key"].AsString(),
            MinSec = Float(dict, "min_sec", 0f),
            Weight = Float(dict, "weight", 1f),
            DurationSec = Float(dict, "duration_sec", 45f)
        };

        foreach (KeyValuePair<Variant, Variant> entry in dict)
        {
            string key = entry.Key.AsString();
            switch (entry.Value.VariantType)
            {
                case Variant.Type.Float:
                case Variant.Type.Int:
                    data.Numbers[key] = (float)entry.Value.AsDouble();
                    break;
                case Variant.Type.String:
                    data.Texts[key] = entry.Value.AsString();
                    break;
                case Variant.Type.Array:
                    List<string> values = new();
                    foreach (Variant value in entry.Value.AsGodotArray())
                        values.Add(value.AsString());
                    data.Lists[key] = values;
                    break;
            }
        }
        return data;
    }

    private static float Float(Godot.Collections.Dictionary dict, string key, float fallback) =>
        dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;
}
