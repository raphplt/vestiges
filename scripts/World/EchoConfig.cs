using System;
using System.Collections.Generic;
using System.Text.Json;
using Vestiges.Infrastructure;

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

    /// <summary>Réglages du jeu, contrôlés avec tout le fichier par <see cref="ErasureConfig"/> (plan 26 Q7d-2).</summary>
    public static EchoConfig Load() => ErasureConfig.Load().Echoes;

    /// <summary>Section <c>echoes</c> lue strictement ; un champ absent garde sa valeur d'avant.</summary>
    public static EchoConfig Read(JsonConfigReader reader, JsonElement section, Func<string, bool> characterExists,
        Func<string, bool> inhabitantExists)
    {
        reader.AllowOnly(section, "echoes", "enabled", "first_delay_sec", "interval_min_sec", "interval_max_sec", "min_memory",
            "max_memory", "min_distance_px", "max_distance_px", "lifetime_sec", "fade_sec", "dissolve_sec",
            "dissolve_distance_px", "walk_speed_px", "walk_chance", "whisper_count", "whisper_sec", "whisper_rise_px",
            "characters", "inhabitants");
        EchoConfig d = new();
        EchoConfig config = new()
        {
            Enabled = !JsonConfigReader.Has(section, "enabled") || reader.Flag(section, "enabled"),
            FirstDelaySec = ErasureConfig.NonNegative(reader, section, "first_delay_sec", d.FirstDelaySec),
            IntervalMinSec = ErasureConfig.Positive(reader, section, "interval_min_sec", d.IntervalMinSec),
            IntervalMaxSec = ErasureConfig.Positive(reader, section, "interval_max_sec", d.IntervalMaxSec),
            MinMemory = ErasureConfig.Chance(reader, section, "min_memory", d.MinMemory),
            MaxMemory = ErasureConfig.Chance(reader, section, "max_memory", d.MaxMemory),
            MinDistancePx = ErasureConfig.NonNegative(reader, section, "min_distance_px", d.MinDistancePx),
            MaxDistancePx = ErasureConfig.NonNegative(reader, section, "max_distance_px", d.MaxDistancePx),
            LifetimeSec = ErasureConfig.Positive(reader, section, "lifetime_sec", d.LifetimeSec),
            FadeSec = ErasureConfig.Positive(reader, section, "fade_sec", d.FadeSec),
            DissolveSec = ErasureConfig.Positive(reader, section, "dissolve_sec", d.DissolveSec),
            DissolveDistancePx = ErasureConfig.NonNegative(reader, section, "dissolve_distance_px", d.DissolveDistancePx),
            WalkSpeedPx = ErasureConfig.NonNegative(reader, section, "walk_speed_px", d.WalkSpeedPx),
            WalkChance = ErasureConfig.Chance(reader, section, "walk_chance", d.WalkChance),
            WhisperCount = JsonConfigReader.Has(section, "whisper_count") ? reader.Integer(section, "whisper_count", 0, 99) : d.WhisperCount,
            WhisperSec = ErasureConfig.Positive(reader, section, "whisper_sec", d.WhisperSec),
            WhisperRisePx = ErasureConfig.NonNegative(reader, section, "whisper_rise_px", d.WhisperRisePx),
        };
        if (reader.Error == null && (config.IntervalMinSec > config.IntervalMaxSec || config.MinMemory > config.MaxMemory
                || config.MinDistancePx > config.MaxDistancePx))
            reader.Fail("echoes : intervalles min ≤ max attendus (interval, memory, distance)");
        config.Characters = Ids(reader, section, "characters", characterExists, "personnage");
        config.Inhabitants = Ids(reader, section, "inhabitants", inhabitantExists, "habitant");
        return config;
    }

    private static List<string> Ids(JsonConfigReader reader, JsonElement section, string key, Func<string, bool> exists, string what)
    {
        List<string> ids = new();
        if (!JsonConfigReader.Has(section, key))
            return ids;
        foreach (JsonElement item in reader.List(section, key, 0))
        {
            string id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (string.IsNullOrEmpty(id) || id.Contains('/') || id.Contains('.') || !exists(id))
                reader.Fail($"echoes.{key} : {what} « {id} » inconnu");
            else if (ids.Contains(id))
                reader.Fail($"echoes.{key} : {what} « {id} » en double");
            else
                ids.Add(id);
        }
        return ids;
    }
}
