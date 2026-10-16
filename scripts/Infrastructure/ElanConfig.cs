using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de l'Élan (data/movement/elan.json, plan 52). Contrôlés en entier : un fichier invalide est refusé avec le
/// nom du champ fautif, comme la mobilité.
/// </summary>
public sealed class ElanConfig
{
    private const string ConfigPath = "res://data/movement/elan.json";
    private static ElanConfig _cached;

    public bool EnabledInRuns { get; init; }
    public IReadOnlyList<ElanTier> Tiers { get; init; } = Array.Empty<ElanTier>();
    public float SpeedMinRatio { get; init; }
    public float SpeedGainPerSecond { get; init; }
    /// <summary>Plafond de la course seule ; au-dessus, seuls les gestes nourrissent l'Élan.</summary>
    public float SpeedCap { get; init; }
    public float StillSpeedRatio { get; init; }
    public float StillGraceSeconds { get; init; }
    public float StillDecayPerSecond { get; init; }
    public float UpkeepDecayPerSecond { get; init; }
    public float HitLoss { get; init; }
    /// <summary>Esquive parfaite (lot E2) : un coup évité pendant le dash, compté une fois par dash.</summary>
    public float PerfectDodgeGain { get; init; }
    /// <summary>Part de la recharge du dash rendue par une esquive parfaite.</summary>
    public float PerfectDodgeCooldownRefund { get; init; }
    public string PerfectDodgeAudio { get; init; } = "";

    public static ElanConfig Load()
    {
        if (_cached != null)
            return _cached;
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[ElanConfig] {ConfigPath} absent ; Élan éteint.");
            return _cached = new ElanConfig();
        }
        ElanConfig config = Parse(file.GetAsText(), out string error);
        if (config != null)
            return _cached = config;
        // L'Élan est un ajout : un fichier fautif l'éteint au lieu d'empêcher la run de démarrer.
        GD.PushError($"[ElanConfig] {ConfigPath} : {error} ; Élan éteint.");
        return _cached = new ElanConfig();
    }

    /// <summary>Contrôle un texte de réglages ; rend null et l'erreur s'il est invalide.</summary>
    public static ElanConfig Parse(string json, out string error)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "élan", "enabled_in_runs", "tiers", "speed", "still", "upkeep_decay_per_second", "hit_loss",
                "perfect_dodge");
            List<ElanTier> tiers = new();
            float previous = 0f;
            foreach (JsonElement entry in reader.List(root, "tiers", 1))
            {
                reader.AllowOnly(entry, "tiers", "from", "speed", "damage");
                ElanTier tier = new(reader.Ratio(entry, "from"), reader.NonNegative(entry, "speed"), reader.NonNegative(entry, "damage"));
                if (reader.Error == null && tier.From <= previous)
                    reader.Fail("tiers : seuils strictement croissants attendus");
                previous = tier.From;
                tiers.Add(tier);
            }
            JsonElement speed = reader.Section(root, "speed");
            reader.AllowOnly(speed, "speed", "min_ratio", "per_second", "cap");
            JsonElement still = reader.Section(root, "still");
            reader.AllowOnly(still, "still", "speed_ratio", "grace_seconds", "decay_per_second");
            JsonElement dodge = reader.Section(root, "perfect_dodge");
            reader.AllowOnly(dodge, "perfect_dodge", "gain", "cooldown_refund", "audio");
            ElanConfig config = new()
            {
                EnabledInRuns = reader.Flag(root, "enabled_in_runs"),
                Tiers = tiers,
                SpeedMinRatio = reader.Ratio(speed, "min_ratio"),
                SpeedGainPerSecond = reader.NonNegative(speed, "per_second"),
                SpeedCap = reader.Ratio(speed, "cap"),
                StillSpeedRatio = reader.Ratio(still, "speed_ratio"),
                StillGraceSeconds = reader.NonNegative(still, "grace_seconds"),
                StillDecayPerSecond = reader.NonNegative(still, "decay_per_second"),
                UpkeepDecayPerSecond = reader.NonNegative(root, "upkeep_decay_per_second"),
                HitLoss = reader.Chance(root, "hit_loss"),
                PerfectDodgeGain = reader.Chance(dodge, "gain"),
                PerfectDodgeCooldownRefund = reader.Chance(dodge, "cooldown_refund"),
                PerfectDodgeAudio = reader.Text(dodge, "audio"),
            };
            if (reader.Error == null && config.SpeedCap < tiers[0].From - 0.0001f)
                reader.Fail("speed.cap sous le premier palier : la course n'en atteindrait aucun");
            if (reader.Error == null && config.StillSpeedRatio >= config.SpeedMinRatio)
                reader.Fail("still.speed_ratio doit rester sous speed.min_ratio");
            error = reader.Error;
            return error == null ? config : null;
        }
        catch (JsonException exception)
        {
            error = $"JSON illisible : {exception.Message}";
            return null;
        }
    }
}
