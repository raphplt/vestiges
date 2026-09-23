using System;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Valeurs communes du prototype, sans profil ni attribution au casting.</summary>
public sealed class MobilityConfig
{
    public float DurationSeconds { get; init; }
    public float SpeedMultiplier { get; init; }
    public float CooldownSeconds { get; init; }
    public float InputBufferSeconds { get; init; }
    public bool BriefResponse { get; init; }
    public float AccelerationSeconds { get; init; }
    public float BrakingSeconds { get; init; }
    public float HurtRecoverySeconds { get; init; }
    public float InvulnerabilitySeconds { get; init; }
    public float TrialInvulnerabilitySeconds { get; init; }
    public float WaterSpeedFactor { get; init; }
    public string StartAudio { get; init; }
    public string EndAudio { get; init; }

    private static MobilityConfig _cached;

    public static MobilityConfig Load()
    {
        if (_cached != null)
            return _cached;

        using FileAccess file = FileAccess.Open("res://data/movement/mobility.json", FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidOperationException("Configuration de mobilité absente.");
        using Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException("Configuration de mobilité invalide.");
        Godot.Collections.Dictionary data = json.Data.AsGodotDictionary();
        string response = data["response"].AsString();
        if (response != "direct" && response != "brief")
            throw new InvalidOperationException("Réponse de mobilité inconnue.");

        _cached = new MobilityConfig
        {
            DurationSeconds = ReadNumber(data, "duration_seconds", 0.01f, 1f),
            SpeedMultiplier = ReadNumber(data, "speed_multiplier", 1f, 5f),
            CooldownSeconds = ReadNumber(data, "cooldown_seconds", 0.1f, 30f),
            InputBufferSeconds = ReadNumber(data, "input_buffer_seconds", 0f, 0.2f),
            BriefResponse = response == "brief",
            AccelerationSeconds = ReadNumber(data, "acceleration_seconds", 0.04f, 0.09f),
            BrakingSeconds = ReadNumber(data, "braking_seconds", 0.03f, 0.07f),
            HurtRecoverySeconds = ReadNumber(data, "hurt_recovery_seconds", 0f, 1f),
            InvulnerabilitySeconds = ReadNumber(data, "invulnerability_seconds", 0f, 0.15f),
            TrialInvulnerabilitySeconds = ReadNumber(data, "trial_invulnerability_seconds", 0f, 0.15f),
            WaterSpeedFactor = ReadNumber(data, "water_speed_factor", 0f, 1f),
            StartAudio = data["start_audio"].AsString(),
            EndAudio = data["end_audio"].AsString()
        };
        return _cached;
    }

    private static float ReadNumber(Godot.Collections.Dictionary data, string key, float minimum, float maximum)
    {
        float value = (float)data[key].AsDouble();
        if (!float.IsFinite(value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"Paramètre de mobilité hors limites : {key}.");
        return value;
    }
}
