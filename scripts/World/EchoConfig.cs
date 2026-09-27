using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>Réglages des échos de l'oubli (<c>echoes</c> dans data/scaling/erasure.json, plan 16 O6).</summary>
public sealed class EchoConfig
{
    public bool Enabled = true;
    public float FirstDelaySec = 60f;
    public float IntervalMinSec = 45f;
    public float IntervalMaxSec = 90f;
    /// <summary>Un écho n'apparaît qu'entre ces deux mémoires : zones Fragiles et Effilochées.</summary>
    public float MinMemory = 0.25f;
    public float MaxMemory = 0.75f;
    public float MinDistancePx = 130f;
    public float MaxDistancePx = 300f;
    public float LifetimeSec = 10f;
    public float FadeSec = 1.6f;
    public float DissolveSec = 0.7f;
    /// <summary>L'écho se dissout quand le joueur approche à moins de cette distance.</summary>
    public float DissolveDistancePx = 70f;
    public float WalkSpeedPx = 14f;
    public float WalkChance = 0.5f;
    /// <summary>Nombre de murmures ECHO_WHISPER_01… dans les traductions.</summary>
    public int WhisperCount = 12;
    public float WhisperSec = 3.2f;
    public float WhisperRisePx = 18f;
    public List<string> Characters = new();
    /// <summary>Habitants dessinés pour les échos (dossiers assets/characters/&lt;id&gt;) ; les personnages ne servent qu'à défaut.</summary>
    public List<string> Inhabitants = new();

    public static EchoConfig Load()
    {
        EchoConfig config = new();
        using FileAccess file = FileAccess.Open("res://data/scaling/erasure.json", FileAccess.ModeFlags.Read);
        if (file == null)
            return config;
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[EchoConfig] erasure.json invalide : {json.GetErrorMessage()}");
            return config;
        }
        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        if (!root.ContainsKey("echoes"))
            return config;

        Godot.Collections.Dictionary d = root["echoes"].AsGodotDictionary();
        config.Enabled = d.GetValueOrDefault("enabled", config.Enabled).AsBool();
        config.FirstDelaySec = (float)d.GetValueOrDefault("first_delay_sec", config.FirstDelaySec).AsDouble();
        config.IntervalMinSec = (float)d.GetValueOrDefault("interval_min_sec", config.IntervalMinSec).AsDouble();
        config.IntervalMaxSec = (float)d.GetValueOrDefault("interval_max_sec", config.IntervalMaxSec).AsDouble();
        config.MinMemory = (float)d.GetValueOrDefault("min_memory", config.MinMemory).AsDouble();
        config.MaxMemory = (float)d.GetValueOrDefault("max_memory", config.MaxMemory).AsDouble();
        config.MinDistancePx = (float)d.GetValueOrDefault("min_distance_px", config.MinDistancePx).AsDouble();
        config.MaxDistancePx = (float)d.GetValueOrDefault("max_distance_px", config.MaxDistancePx).AsDouble();
        config.LifetimeSec = (float)d.GetValueOrDefault("lifetime_sec", config.LifetimeSec).AsDouble();
        config.FadeSec = (float)d.GetValueOrDefault("fade_sec", config.FadeSec).AsDouble();
        config.DissolveSec = (float)d.GetValueOrDefault("dissolve_sec", config.DissolveSec).AsDouble();
        config.DissolveDistancePx = (float)d.GetValueOrDefault("dissolve_distance_px", config.DissolveDistancePx).AsDouble();
        config.WalkSpeedPx = (float)d.GetValueOrDefault("walk_speed_px", config.WalkSpeedPx).AsDouble();
        config.WalkChance = (float)d.GetValueOrDefault("walk_chance", config.WalkChance).AsDouble();
        config.WhisperCount = (int)d.GetValueOrDefault("whisper_count", config.WhisperCount).AsDouble();
        config.WhisperSec = (float)d.GetValueOrDefault("whisper_sec", config.WhisperSec).AsDouble();
        config.WhisperRisePx = (float)d.GetValueOrDefault("whisper_rise_px", config.WhisperRisePx).AsDouble();
        if (d.ContainsKey("characters"))
        {
            foreach (Variant id in d["characters"].AsGodotArray())
                config.Characters.Add(id.AsString());
        }
        if (d.ContainsKey("inhabitants"))
        {
            foreach (Variant id in d["inhabitants"].AsGodotArray())
                config.Inhabitants.Add(id.AsString());
        }
        return config;
    }
}
