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
    /// <summary>
    /// Saut du vide (lot E3) : longueur ajoutée au dash au-dessus du Néant, une valeur par palier, palier 0 compris.
    /// Une allonge nulle garde l'ancienne règle : le dash s'arrête au bord du vide.
    /// </summary>
    public IReadOnlyList<float> VoidLeapExtension { get; init; } = Array.Empty<float>();
    /// <summary>Vide franchi au minimum pour qu'un saut compte et nourrisse l'Élan.</summary>
    public float VoidLeapMinVoidPixels { get; init; }
    public float VoidLeapGain { get; init; }
    public float VoidLeapHeightPixels { get; init; }
    public string VoidLeapLandingAudio { get; init; } = "";

    /// <summary>Percussion (lot E4) : palier à partir duquel le dash projette les créatures qu'il traverse.</summary>
    public int PercussionMinTier { get; init; }
    /// <summary>Distance de projection, une valeur par palier, palier 0 compris.</summary>
    public IReadOnlyList<float> PercussionDistance { get; init; } = Array.Empty<float>();
    /// <summary>Distance au sol entre le trajet du dash et une créature touchée.</summary>
    public float PercussionReachPixels { get; init; }
    /// <summary>Part de la poussée dans le sens du dash, ajoutée à la poussée vers l'extérieur du trajet.</summary>
    public float PercussionForwardBias { get; init; }
    public float PercussionGainPerCreature { get; init; }
    public float PercussionMaxGainPerDash { get; init; }
    /// <summary>Vitesse de recul au-dessus de laquelle une créature qui entre dans le Néant y tombe au lieu de s'arrêter au bord.</summary>
    public float FallMinKnockSpeed { get; init; }
    public string PercussionAudio { get; init; } = "";
    public string FallAudio { get; init; } = "";

    public float PercussionDistanceAt(int tier) =>
        PercussionDistance.Count == 0 ? 0f : PercussionDistance[Mathf.Clamp(tier, 0, PercussionDistance.Count - 1)];

    /// <summary>Allonge du saut au palier donné ; nulle si l'Élan n'a pas de réglage de saut.</summary>
    public float VoidLeapExtensionAt(int tier) =>
        VoidLeapExtension.Count == 0 ? 0f : VoidLeapExtension[Mathf.Clamp(tier, 0, VoidLeapExtension.Count - 1)];

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
                "perfect_dodge", "void_leap", "percussion");
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
            JsonElement leap = reader.Section(root, "void_leap");
            reader.AllowOnly(leap, "void_leap", "extension_px", "min_void_px", "gain", "height_px", "landing_audio");
            List<float> extensions = ReadPerTier(reader, leap, "void_leap", "extension_px", tiers.Count);
            JsonElement percussion = reader.Section(root, "percussion");
            reader.AllowOnly(percussion, "percussion", "min_tier", "distance_px", "reach_px", "forward_bias", "gain_per_creature",
                "max_gain_per_dash", "fall_min_knock_speed", "audio", "fall_audio");
            List<float> distances = ReadPerTier(reader, percussion, "percussion", "distance_px", tiers.Count);
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
                VoidLeapExtension = extensions,
                VoidLeapMinVoidPixels = reader.NonNegative(leap, "min_void_px"),
                VoidLeapGain = reader.Chance(leap, "gain"),
                VoidLeapHeightPixels = reader.NonNegative(leap, "height_px"),
                VoidLeapLandingAudio = reader.Text(leap, "landing_audio"),
                PercussionMinTier = reader.Integer(percussion, "min_tier", 0, tiers.Count),
                PercussionDistance = distances,
                PercussionReachPixels = reader.Positive(percussion, "reach_px"),
                PercussionForwardBias = reader.NonNegative(percussion, "forward_bias"),
                PercussionGainPerCreature = reader.Chance(percussion, "gain_per_creature"),
                PercussionMaxGainPerDash = reader.Chance(percussion, "max_gain_per_dash"),
                FallMinKnockSpeed = reader.NonNegative(percussion, "fall_min_knock_speed"),
                PercussionAudio = reader.Text(percussion, "audio"),
                FallAudio = reader.Text(percussion, "fall_audio"),
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

    /// <summary>
    /// Une longueur par palier, palier 0 compris, positive et croissante : un palier de plus ne raccourcit jamais le saut
    /// ni la projection.
    /// </summary>
    private static List<float> ReadPerTier(JsonConfigReader reader, JsonElement owner, string section, string key, int tierCount)
    {
        List<float> values = new();
        float previous = 0f;
        foreach (JsonElement entry in reader.List(owner, key, 1))
        {
            if (entry.ValueKind != JsonValueKind.Number || !entry.TryGetSingle(out float value) || value < 0f)
            {
                reader.Fail($"{section}.{key} : longueurs positives ou nulles attendues");
                return values;
            }
            if (value < previous)
                reader.Fail($"{section}.{key} : longueurs croissantes attendues");
            previous = value;
            values.Add(value);
        }
        if (reader.Error == null && values.Count != tierCount + 1)
            reader.Fail($"{section}.{key} : {tierCount + 1} longueurs attendues (palier 0 compris), {values.Count} lues");
        return values;
    }
}
