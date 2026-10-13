using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de la mobilité, au même bouton pour tous (plan 01 E) : le dash commun, et des profils de personnage
/// (<c>profiles</c>) qui ne réécrivent que les valeurs qui changent. Un profil inconnu est refusé.
/// </summary>
public sealed class MobilityConfig
{
    public string Name { get; init; }
    public string Description { get; init; }
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
    /// <summary>Virage pendant le mouvement, en degrés par seconde ; 0 : la direction est figée au départ.</summary>
    public float TurnRateDegrees { get; init; }
    /// <summary>Longe un mur au lieu de s'y arrêter ; seul un choc de face l'interrompt.</summary>
    public bool SlidesAlongWalls { get; init; }
    /// <summary>Hauteur du sprite au sommet du saut ; corps et ombre restent au sol.</summary>
    public float VisualHeightPixels { get; init; }
    public bool IgnoreWaterWhileActive { get; init; }
    public string StartAudio { get; init; }
    public string EndAudio { get; init; }

    private static readonly Dictionary<string, MobilityConfig> _cached = new();

    /// <summary>Dash commun si <paramref name="profileId"/> est vide, sinon le profil nommé.</summary>
    public static MobilityConfig Load(string profileId = null)
    {
        string key = profileId ?? "";
        if (_cached.TryGetValue(key, out MobilityConfig cached))
            return cached;

        using FileAccess file = FileAccess.Open("res://data/movement/mobility.json", FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidOperationException("Configuration de mobilité absente.");
        using Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException("Configuration de mobilité invalide.");
        Godot.Collections.Dictionary data = json.Data.AsGodotDictionary();
        if (key != "")
        {
            Godot.Collections.Dictionary profiles = data.ContainsKey("profiles") ? data["profiles"].AsGodotDictionary() : new();
            if (!profiles.ContainsKey(key))
                throw new InvalidOperationException($"Profil de mobilité inconnu : {key}.");
            // Le profil hérite du dash commun et ne réécrit que ses propres valeurs.
            data = data.Duplicate();
            foreach ((Variant field, Variant value) in profiles[key].AsGodotDictionary())
                data[field] = value;
        }
        string response = data["response"].AsString();
        if (response != "direct" && response != "brief")
            throw new InvalidOperationException("Réponse de mobilité inconnue.");

        MobilityConfig config = new()
        {
            Name = data["name"].AsString(),
            Description = data["description"].AsString(),
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
            TurnRateDegrees = ReadNumber(data, "turn_rate_degrees", 0f, 720f),
            SlidesAlongWalls = data["slides_along_walls"].AsBool(),
            VisualHeightPixels = ReadNumber(data, "visual_height_px", 0f, 32f),
            IgnoreWaterWhileActive = ReadBool(data, "ignore_water_while_active"),
            StartAudio = data["start_audio"].AsString(),
            EndAudio = data["end_audio"].AsString()
        };
        _cached[key] = config;
        return config;
    }

    private static float ReadNumber(Godot.Collections.Dictionary data, string key, float minimum, float maximum)
    {
        if (!data.TryGetValue(key, out Variant item) || item.VariantType is not (Variant.Type.Float or Variant.Type.Int))
            throw new InvalidOperationException($"Paramètre de mobilité numérique requis : {key}.");
        float value = (float)item.AsDouble();
        if (!float.IsFinite(value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"Paramètre de mobilité hors limites : {key}.");
        return value;
    }

    private static bool ReadBool(Godot.Collections.Dictionary data, string key)
    {
        if (!data.TryGetValue(key, out Variant value) || value.VariantType != Variant.Type.Bool)
            throw new InvalidOperationException($"Paramètre de mobilité booléen requis : {key}.");
        return value.AsBool();
    }
}
