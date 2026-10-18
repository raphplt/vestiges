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

    public const string FilePath = "res://data/movement/mobility.json";

    /// <summary>Réglages d'une mobilité ; un profil ne réécrit que certains d'entre eux.</summary>
    private static readonly string[] Keys =
    {
        "name", "description", "duration_seconds", "speed_multiplier", "cooldown_seconds", "input_buffer_seconds", "response",
        "acceleration_seconds", "braking_seconds", "hurt_recovery_seconds", "invulnerability_seconds",
        "trial_invulnerability_seconds", "water_speed_factor", "turn_rate_degrees", "slides_along_walls", "visual_height_px",
        "ignore_water_while_active", "start_audio", "end_audio",
    };

    private static readonly Dictionary<string, MobilityConfig> _cached = new();

    /// <summary>Dash commun si <paramref name="profileId"/> est vide, sinon le profil nommé.</summary>
    public static MobilityConfig Load(string profileId = null)
    {
        string key = profileId ?? "";
        if (_cached.TryGetValue(key, out MobilityConfig cached))
            return cached;

        using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidOperationException("Configuration de mobilité absente.");
        using Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException("Configuration de mobilité invalide.");
        Godot.Collections.Dictionary data = json.Data.AsGodotDictionary();
        RefuseUnknownKeys(data, "mobilité", allowProfiles: true);
        if (key != "")
        {
            Godot.Collections.Dictionary profiles = data.ContainsKey("profiles") ? data["profiles"].AsGodotDictionary() : new();
            if (!profiles.ContainsKey(key))
                throw new InvalidOperationException($"Profil de mobilité inconnu : {key}.");
            RefuseUnknownKeys(profiles[key].AsGodotDictionary(), $"profil {key}", allowProfiles: false);
            // Le profil hérite du dash commun et ne réécrit que ses propres valeurs.
            data = data.Duplicate();
            foreach ((Variant field, Variant value) in profiles[key].AsGodotDictionary())
                data[field] = value;
        }
        string response = ReadText(data, "response");
        if (response != "direct" && response != "brief")
            throw new InvalidOperationException("Réponse de mobilité inconnue.");

        MobilityConfig config = new()
        {
            Name = ReadText(data, "name"),
            Description = ReadText(data, "description"),
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
            SlidesAlongWalls = ReadBool(data, "slides_along_walls"),
            VisualHeightPixels = ReadNumber(data, "visual_height_px", 0f, 32f),
            IgnoreWaterWhileActive = ReadBool(data, "ignore_water_while_active"),
            StartAudio = ReadText(data, "start_audio"),
            EndAudio = ReadText(data, "end_audio")
        };
        _cached[key] = config;
        return config;
    }

    /// <summary>
    /// Le dash commun et chacun de ses profils, contrôlés au chargement de la run (plan 26 Q7g-4b) : un profil refusé
    /// l'est avant qu'un personnage ne le demande.
    /// </summary>
    public static bool TryLoad(out string error)
    {
        try
        {
            Load();
            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            using Json json = new();
            json.Parse(file.GetAsText());
            Godot.Collections.Dictionary data = json.Data.AsGodotDictionary();
            if (data.TryGetValue("profiles", out Variant profiles))
            {
                foreach (Variant profile in profiles.AsGodotDictionary().Keys)
                    Load(profile.AsString());
            }
            error = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            error = $"{FilePath} : {exception.Message}";
            return false;
        }
    }

    private static void RefuseUnknownKeys(Godot.Collections.Dictionary data, string where, bool allowProfiles)
    {
        foreach (Variant field in data.Keys)
        {
            string name = field.AsString();
            if (!name.StartsWith('_') && Array.IndexOf(Keys, name) < 0 && !(allowProfiles && name == "profiles"))
                throw new InvalidOperationException($"{where} : clé « {name} » inconnue.");
        }
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

    private static string ReadText(Godot.Collections.Dictionary data, string key)
    {
        if (!data.TryGetValue(key, out Variant value) || value.VariantType != Variant.Type.String)
            throw new InvalidOperationException($"Paramètre de mobilité texte requis : {key}.");
        return value.AsString();
    }

    private static bool ReadBool(Godot.Collections.Dictionary data, string key)
    {
        if (!data.TryGetValue(key, out Variant value) || value.VariantType != Variant.Type.Bool)
            throw new InvalidOperationException($"Paramètre de mobilité booléen requis : {key}.");
        return value.AsBool();
    }
}
