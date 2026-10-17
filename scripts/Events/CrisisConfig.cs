using System;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Events;

/// <summary>
/// Calendrier des Résurgences, seuils du late game et de l'Indicible, accalmie après une crise
/// (data/scaling/crises.json), lus strictement une fois (plan 26 Q7d-1). Un fichier refusé arrête le chargement de la
/// run (GameBootstrap). Un champ absent garde la valeur qu'avaient les lecteurs d'avant.
/// </summary>
public sealed class CrisisConfig
{
    public const string FilePath = "res://data/scaling/crises.json";

    public float FirstCrisisDelaySec { get; private init; } = 240f;
    public float IntervalSec { get; private init; } = 240f;
    public float IntervalVarianceSec { get; private init; } = 45f;
    public float WarningDurationSec { get; private init; } = 20f;
    public float CrisisDurationSec { get; private init; } = 70f;
    public float EndgameIntervalMultiplier { get; private init; } = 0.7f;
    public float EndgameDurationMultiplier { get; private init; } = 1.15f;
    public float LateGameErasureThreshold { get; private init; } = 0.68f;
    public int LateGameCrisisThreshold { get; private init; } = 4;
    public int BossCrisisThreshold { get; private init; } = 5;
    public float BossTimeThresholdSec { get; private init; } = 30f * 60f;
    public float BossHpScale { get; private init; } = 3.2f;
    public float BossDmgScale { get; private init; } = 1.65f;
    public float CalmEssenceMultiplier { get; private init; } = 2f;
    public float CalmEssenceSeconds { get; private init; } = 30f;
    public string CalmChest { get; private init; } = "chest_rare";
    /// <summary>Distance du coffre d'accalmie devant le joueur, en pixels : [min, max].</summary>
    public Vector2 CalmChestDistance { get; private init; } = new(220f, 340f);

    private static CrisisConfig _loaded;
    private static string _loadError;
    private static bool _attempted;

    /// <summary>Les réglages du jeu ; refusés, une exception qui nomme le champ (le bootstrap les a déjà contrôlés).</summary>
    public static CrisisConfig Load()
    {
        if (!TryLoad(out CrisisConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    public static bool TryLoad(out CrisisConfig config, out string error)
    {
        if (!_attempted)
        {
            _attempted = true;
            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            if (file == null)
                _loadError = $"{FilePath} : fichier illisible";
            else if (ChestDataLoader.TryLoad(out string chestError)
                     && TryParse(file.GetAsText(), id => ChestDataLoader.Get(id) != null, out _loaded, out _loadError))
                _loadError = null;
            else
                _loadError = $"{FilePath} : {_loadError ?? chestError}";
        }
        config = _loaded;
        error = _loadError;
        return error == null;
    }

    /// <summary><paramref name="chestExists"/> reconnaît un coffre du catalogue (coffre d'accalmie).</summary>
    public static bool TryParse(string text, Func<string, bool> chestExists, out CrisisConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "crises", "first_crisis_delay_sec", "interval_sec", "interval_variance_sec",
                "warning_duration_sec", "crisis_duration_sec", "late_game_erasure_threshold", "late_game_crisis_threshold",
                "boss_crisis_threshold", "boss_time_threshold_sec", "boss_hp_scale", "boss_dmg_scale",
                "endgame_interval_multiplier", "endgame_duration_multiplier", "calm_essence_multiplier",
                "calm_essence_seconds", "calm_chest", "calm_chest_distance");
            CrisisConfig defaults = new();
            float intervalSec = Positive(reader, root, "interval_sec", defaults.IntervalSec);
            string chest = JsonConfigReader.Has(root, "calm_chest") ? reader.Text(root, "calm_chest") : defaults.CalmChest;
            if (chest != null && !chestExists(chest))
                reader.Fail($"calm_chest : coffre « {chest} » inconnu");
            Vector2 distance = defaults.CalmChestDistance;
            if (JsonConfigReader.Has(root, "calm_chest_distance"))
            {
                (float min, float max) = reader.Range(root, "calm_chest_distance", 0f);
                distance = new Vector2(min, max);
            }
            CrisisConfig parsed = new()
            {
                FirstCrisisDelaySec = Positive(reader, root, "first_crisis_delay_sec", defaults.FirstCrisisDelaySec),
                IntervalSec = intervalSec,
                IntervalVarianceSec = NonNegative(reader, root, "interval_variance_sec", defaults.IntervalVarianceSec),
                WarningDurationSec = NonNegative(reader, root, "warning_duration_sec", defaults.WarningDurationSec),
                CrisisDurationSec = Positive(reader, root, "crisis_duration_sec", defaults.CrisisDurationSec),
                EndgameIntervalMultiplier = Positive(reader, root, "endgame_interval_multiplier", defaults.EndgameIntervalMultiplier),
                EndgameDurationMultiplier = Positive(reader, root, "endgame_duration_multiplier", defaults.EndgameDurationMultiplier),
                LateGameErasureThreshold = JsonConfigReader.Has(root, "late_game_erasure_threshold")
                    ? reader.Chance(root, "late_game_erasure_threshold") : defaults.LateGameErasureThreshold,
                LateGameCrisisThreshold = Count(reader, root, "late_game_crisis_threshold", defaults.LateGameCrisisThreshold),
                BossCrisisThreshold = Count(reader, root, "boss_crisis_threshold", defaults.BossCrisisThreshold),
                BossTimeThresholdSec = Positive(reader, root, "boss_time_threshold_sec", defaults.BossTimeThresholdSec),
                BossHpScale = Positive(reader, root, "boss_hp_scale", defaults.BossHpScale),
                BossDmgScale = Positive(reader, root, "boss_dmg_scale", defaults.BossDmgScale),
                CalmEssenceMultiplier = Positive(reader, root, "calm_essence_multiplier", defaults.CalmEssenceMultiplier),
                CalmEssenceSeconds = NonNegative(reader, root, "calm_essence_seconds", defaults.CalmEssenceSeconds),
                CalmChest = chest,
                CalmChestDistance = distance,
            };
            error = reader.Error;
            if (error != null)
                return false;
            config = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static float Positive(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.Positive(owner, key) : fallback;

    private static float NonNegative(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.NonNegative(owner, key) : fallback;

    private static int Count(JsonConfigReader reader, JsonElement owner, string key, int fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.Count(owner, key, 100) : fallback;
}
