using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de l'Élan (data/movement/elan.json, plan 52) : la Série et ses rangs, l'esquive parfaite, le saut du vide
/// et la percussion. Contrôlés en entier : un fichier invalide est refusé avec le nom du champ fautif, comme la mobilité.
/// </summary>
public sealed class ElanConfig
{
    private const string ConfigPath = "res://data/movement/elan.json";
    private static ElanConfig _cached;

    public bool EnabledInRuns { get; init; }
    /// <summary>Rangs de la Série dans l'ordre ; le rang 0 (aucun) n'y figure pas.</summary>
    public IReadOnlyList<StreakRank> Ranks { get; init; } = Array.Empty<StreakRank>();
    /// <summary>Fenêtre sans élimination avant l'encaissement, au rang 0 ; chaque rang a la sienne.</summary>
    public float StreakWindowSeconds { get; init; }
    /// <summary>Couleur de la Série au rang 0, avant ÉVEIL.</summary>
    public Color StreakColor { get; init; } = Colors.White;
    /// <summary>Ce que vaut une chute dans le vide, en éliminations.</summary>
    public int StreakFallWeight { get; init; } = 1;
    /// <summary>Part de la recharge du dash rendue par une esquive parfaite.</summary>
    public float PerfectDodgeCooldownRefund { get; init; }
    public string PerfectDodgeAudio { get; init; } = "";
    /// <summary>
    /// Saut du vide (lot E3) : longueur ajoutée au dash au-dessus du Néant, une valeur par rang, rang 0 compris.
    /// Une allonge nulle garde l'ancienne règle : le dash s'arrête au bord du vide.
    /// </summary>
    public IReadOnlyList<float> VoidLeapExtension { get; init; } = Array.Empty<float>();
    /// <summary>Vide franchi au minimum pour qu'un saut compte.</summary>
    public float VoidLeapMinVoidPixels { get; init; }
    public float VoidLeapHeightPixels { get; init; }
    public string VoidLeapLandingAudio { get; init; } = "";
    /// <summary>Percussion (lot E4) : rang à partir duquel le dash projette les créatures qu'il traverse.</summary>
    public int PercussionMinRank { get; init; }
    /// <summary>Distance de projection, une valeur par rang, rang 0 compris.</summary>
    public IReadOnlyList<float> PercussionDistance { get; init; } = Array.Empty<float>();
    /// <summary>Distance au sol entre le trajet du dash et une créature touchée.</summary>
    public float PercussionReachPixels { get; init; }
    /// <summary>Part de la poussée dans le sens du dash, ajoutée à la poussée vers l'extérieur du trajet.</summary>
    public float PercussionForwardBias { get; init; }
    /// <summary>Vitesse de recul au-dessus de laquelle une créature qui entre dans le Néant y tombe au lieu de s'arrêter au bord.</summary>
    public float FallMinKnockSpeed { get; init; }
    public string PercussionAudio { get; init; } = "";
    public string FallAudio { get; init; } = "";

    public float PercussionDistanceAt(int rank) =>
        PercussionDistance.Count == 0 ? 0f : PercussionDistance[Mathf.Clamp(rank, 0, PercussionDistance.Count - 1)];

    /// <summary>Allonge du saut au rang donné ; nulle si l'Élan n'a pas de réglage de saut.</summary>
    public float VoidLeapExtensionAt(int rank) =>
        VoidLeapExtension.Count == 0 ? 0f : VoidLeapExtension[Mathf.Clamp(rank, 0, VoidLeapExtension.Count - 1)];

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
            reader.AllowOnly(root, "élan", "enabled_in_runs", "streak", "ranks", "perfect_dodge", "void_leap", "percussion");
            JsonElement streak = reader.Section(root, "streak");
            reader.AllowOnly(streak, "streak", "window_seconds", "fall_weight", "color");
            List<StreakRank> ranks = new();
            int previous = 0;
            foreach (JsonElement entry in reader.List(root, "ranks", 1))
            {
                reader.AllowOnly(entry, "ranks", "name", "from", "color", "window_seconds", "speed", "damage", "luck", "cashout");
                StreakRank rank = new(reader.Text(entry, "name"), reader.Integer(entry, "from", 1, 100000), reader.Rgb(entry, "color"),
                    reader.Positive(entry, "window_seconds"), reader.NonNegative(entry, "speed"), reader.NonNegative(entry, "damage"),
                    reader.NonNegative(entry, "luck"), reader.NonNegative(entry, "cashout"));
                if (reader.Error == null && rank.From <= previous)
                    reader.Fail("ranks : seuils strictement croissants attendus");
                previous = rank.From;
                ranks.Add(rank);
            }
            JsonElement dodge = reader.Section(root, "perfect_dodge");
            reader.AllowOnly(dodge, "perfect_dodge", "cooldown_refund", "audio");
            JsonElement leap = reader.Section(root, "void_leap");
            reader.AllowOnly(leap, "void_leap", "extension_px", "min_void_px", "height_px", "landing_audio");
            List<float> extensions = ReadPerRank(reader, leap, "void_leap", "extension_px", ranks.Count);
            JsonElement percussion = reader.Section(root, "percussion");
            reader.AllowOnly(percussion, "percussion", "min_rank", "distance_px", "reach_px", "forward_bias",
                "fall_min_knock_speed", "audio", "fall_audio");
            List<float> distances = ReadPerRank(reader, percussion, "percussion", "distance_px", ranks.Count);
            ElanConfig config = new()
            {
                EnabledInRuns = reader.Flag(root, "enabled_in_runs"),
                Ranks = ranks,
                StreakWindowSeconds = reader.Positive(streak, "window_seconds"),
                StreakColor = reader.Rgb(streak, "color"),
                StreakFallWeight = reader.Integer(streak, "fall_weight", 1, 100),
                PerfectDodgeCooldownRefund = reader.Chance(dodge, "cooldown_refund"),
                PerfectDodgeAudio = reader.Text(dodge, "audio"),
                VoidLeapExtension = extensions,
                VoidLeapMinVoidPixels = reader.NonNegative(leap, "min_void_px"),
                VoidLeapHeightPixels = reader.NonNegative(leap, "height_px"),
                VoidLeapLandingAudio = reader.Text(leap, "landing_audio"),
                PercussionMinRank = reader.Integer(percussion, "min_rank", 0, ranks.Count),
                PercussionDistance = distances,
                PercussionReachPixels = reader.Positive(percussion, "reach_px"),
                PercussionForwardBias = reader.NonNegative(percussion, "forward_bias"),
                FallMinKnockSpeed = reader.NonNegative(percussion, "fall_min_knock_speed"),
                PercussionAudio = reader.Text(percussion, "audio"),
                FallAudio = reader.Text(percussion, "fall_audio"),
            };
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
    /// Une longueur par rang, rang 0 compris, positive et croissante : un rang de plus ne raccourcit jamais le saut ni
    /// la projection.
    /// </summary>
    private static List<float> ReadPerRank(JsonConfigReader reader, JsonElement owner, string section, string key, int rankCount)
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
        if (reader.Error == null && values.Count != rankCount + 1)
            reader.Fail($"{section}.{key} : {rankCount + 1} longueurs attendues (rang 0 compris), {values.Count} lues");
        return values;
    }
}
