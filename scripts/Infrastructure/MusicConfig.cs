using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Pilotage de la musique (plan 15, A1), lu strictement dans data/audio/music.json (plan 26 Q7g-4b).</summary>
public sealed class MusicConfig
{
    /// <summary>Morceau joué pour une intention : fondu d'entrée et point de départ dans le fichier.</summary>
    public readonly record struct Cue(string Key, float FadeSeconds, bool Loop, float StartSeconds);

    private static readonly Dictionary<string, MusicIntent> IntentIds = new()
    {
        ["hub"] = MusicIntent.Hub,
        ["exploration"] = MusicIntent.Exploration,
        ["combat"] = MusicIntent.Combat,
        ["calm"] = MusicIntent.Calm,
        ["late_game"] = MusicIntent.LateGame,
        ["endgame"] = MusicIntent.Endgame,
        ["warning"] = MusicIntent.Warning,
        ["resurgence"] = MusicIntent.Resurgence,
        ["death"] = MusicIntent.Death,
    };

    private const float MinSampleSeconds = 0.05f;

    public IReadOnlyDictionary<MusicIntent, Cue> Cues { get; private init; } = new Dictionary<MusicIntent, Cue>();
    public float CombatRadiusPx { get; private init; } = 600f;
    public float CombatSampleSeconds { get; private init; } = 0.5f;
    public int CombatEnterEnemies { get; private init; } = 4;
    /// <summary>Variantes qui font à elles seules un pic de combat ; mini-boss et boss en font toujours un.</summary>
    public IReadOnlySet<string> CombatPeakVariants { get; private init; } = new HashSet<string> { "champion" };
    public float CombatEnterHoldSeconds { get; private init; } = 1f;
    public int CombatExitEnemies { get; private init; } = 1;
    public float CombatExitHoldSeconds { get; private init; } = 6f;

    public const string DefaultPath = "res://data/audio/music.json";

    /// <summary>
    /// Réglages du jeu ; un autre chemin sert aux variantes d'écoute (tools/record_run_audio.sh). Refusés, la raison est
    /// journalisée et la musique se tait plutôt que de bloquer l'accueil (la run, elle, ne démarre pas : GameBootstrap).
    /// </summary>
    public static MusicConfig Load(string path = DefaultPath)
    {
        if (TryLoad(path, out MusicConfig config, out string error))
            return config;
        GD.PushError($"[MusicConfig] {error}");
        return new MusicConfig();
    }

    /// <summary>Lu strictement (plan 26 Q7g-4b) : morceaux de la banque de sons, variantes connues, bornes.</summary>
    public static bool TryLoad(string path, out MusicConfig config, out string error)
    {
        config = null;
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            error = $"{path} : fichier illisible";
            return false;
        }
        HashSet<string> sounds = DataKeySets.TopLevelKeys(AudioManager.SoundBankPath);
        if (TryParse(file.GetAsText(), sounds.Contains, id => Array.IndexOf(EnemyVariantDataLoader.VariantIds, id) >= 0, out config, out error))
            return true;
        error = $"{path} : {error}";
        return false;
    }

    /// <summary>Une intention absente n'a pas de morceau ; un réglage de combat absent garde sa valeur d'avant.</summary>
    public static bool TryParse(string text, Func<string, bool> soundExists, Func<string, bool> variantExists, out MusicConfig config,
        out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "musique", "intents", "combat");
            MusicConfig defaults = new();
            Dictionary<MusicIntent, Cue> cues = new();
            foreach ((string id, JsonElement cue) in reader.Entries(reader.Section(root, "intents"), "intents"))
            {
                if (!IntentIds.TryGetValue(id, out MusicIntent intent))
                {
                    reader.Fail($"intents : intention « {id} » inconnue ({string.Join(", ", IntentIds.Keys)})");
                    break;
                }
                if (cue.ValueKind != JsonValueKind.Object)
                {
                    reader.Fail($"intents.{id} : objet attendu");
                    break;
                }
                reader.AllowOnly(cue, $"intents.{id}", "music", "fade_sec", "loop", "start_sec");
                string music = reader.Text(cue, "music");
                if (reader.Error == null && !soundExists(music))
                    reader.Fail($"intents.{id} : morceau « {music} » absent de la banque de sons");
                cues[intent] = new Cue(music, JsonConfigReader.Has(cue, "fade_sec") ? reader.Positive(cue, "fade_sec") : 2f,
                    !JsonConfigReader.Has(cue, "loop") || reader.Flag(cue, "loop"),
                    JsonConfigReader.Has(cue, "start_sec") ? reader.NonNegative(cue, "start_sec") : 0f);
            }
            JsonElement combat = JsonConfigReader.Has(root, "combat") ? reader.Section(root, "combat") : default;
            reader.AllowOnly(combat, "combat", "radius_px", "sample_sec", "peak_variants", "enter_enemies", "enter_hold_sec",
                "exit_enemies", "exit_hold_sec");
            MusicConfig parsed = new()
            {
                Cues = cues,
                CombatRadiusPx = JsonConfigReader.Has(combat, "radius_px") ? reader.Positive(combat, "radius_px") : defaults.CombatRadiusPx,
                CombatSampleSeconds = JsonConfigReader.Has(combat, "sample_sec") ? reader.Positive(combat, "sample_sec") : defaults.CombatSampleSeconds,
                CombatEnterEnemies = JsonConfigReader.Has(combat, "enter_enemies") ? reader.Integer(combat, "enter_enemies", 0, 1000) : defaults.CombatEnterEnemies,
                CombatPeakVariants = JsonConfigReader.Has(combat, "peak_variants") ? Variants(reader, combat, variantExists) : defaults.CombatPeakVariants,
                CombatEnterHoldSeconds = JsonConfigReader.Has(combat, "enter_hold_sec") ? reader.NonNegative(combat, "enter_hold_sec") : defaults.CombatEnterHoldSeconds,
                CombatExitEnemies = JsonConfigReader.Has(combat, "exit_enemies") ? reader.Integer(combat, "exit_enemies", 0, 1000) : defaults.CombatExitEnemies,
                CombatExitHoldSeconds = JsonConfigReader.Has(combat, "exit_hold_sec") ? reader.NonNegative(combat, "exit_hold_sec") : defaults.CombatExitHoldSeconds,
            };
            // En dessous, l'échantillonnage tournerait à chaque image.
            if (reader.Error == null && parsed.CombatSampleSeconds < MinSampleSeconds)
                reader.Fail(FormattableString.Invariant($"combat.sample_sec : {parsed.CombatSampleSeconds} (au moins {MinSampleSeconds} s)"));
            if (reader.Error == null && parsed.CombatExitEnemies > parsed.CombatEnterEnemies)
                reader.Fail($"combat : exit_enemies ≤ enter_enemies attendu ({parsed.CombatExitEnemies} > {parsed.CombatEnterEnemies})");
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

    private static HashSet<string> Variants(JsonConfigReader reader, JsonElement combat, Func<string, bool> variantExists)
    {
        HashSet<string> variants = new();
        foreach (JsonElement item in reader.List(combat, "peak_variants", 0))
        {
            string id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (id == null)
                reader.Fail("combat.peak_variants : texte attendu");
            else if (!variantExists(id))
                reader.Fail($"combat.peak_variants : variante « {id} » inconnue");
            else if (!variants.Add(id))
                reader.Fail($"combat.peak_variants : variante « {id} » en double");
        }
        return variants;
    }
}
