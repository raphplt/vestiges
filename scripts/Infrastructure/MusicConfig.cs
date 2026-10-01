using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Pilotage de la musique (plan 15, A1), lu dans data/audio/music.json.</summary>
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

    public IReadOnlyDictionary<MusicIntent, Cue> Cues { get; private init; } = new Dictionary<MusicIntent, Cue>();
    public float CombatRadiusPx { get; private init; } = 600f;
    public float CombatSampleSeconds { get; private init; } = 0.5f;
    public int CombatEnterEnemies { get; private init; } = 4;
    public float CombatEnterHoldSeconds { get; private init; } = 1f;
    public int CombatExitEnemies { get; private init; } = 1;
    public float CombatExitHoldSeconds { get; private init; } = 6f;

    public const string DefaultPath = "res://data/audio/music.json";

    /// <summary>Réglages du jeu ; un autre chemin sert aux variantes d'écoute (tools/record_run_audio.sh).</summary>
    public static MusicConfig Load(string path = DefaultPath)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[MusicConfig] {path} introuvable");
            return new MusicConfig();
        }
        using Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[MusicConfig] {path} invalide : {json.GetErrorMessage()}");
            return new MusicConfig();
        }

        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        MusicConfig defaults = new();
        Dictionary<MusicIntent, Cue> cues = new();
        foreach (KeyValuePair<Variant, Variant> entry in Section(root, "intents"))
        {
            if (!IntentIds.TryGetValue(entry.Key.AsString(), out MusicIntent intent))
            {
                GD.PushError($"[MusicConfig] Intention inconnue : {entry.Key.AsString()}");
                continue;
            }
            Godot.Collections.Dictionary cue = entry.Value.AsGodotDictionary();
            if (!cue.ContainsKey("music"))
            {
                GD.PushError($"[MusicConfig] Intention sans morceau : {entry.Key.AsString()}");
                continue;
            }
            cues[intent] = new Cue(cue["music"].AsString(), Mathf.Max(0.01f, Read(cue, "fade_sec", 2f)),
                !cue.ContainsKey("loop") || cue["loop"].AsBool(), Mathf.Max(0f, Read(cue, "start_sec", 0f)));
        }

        Godot.Collections.Dictionary combat = Section(root, "combat");
        return new MusicConfig
        {
            Cues = cues,
            CombatRadiusPx = Read(combat, "radius_px", defaults.CombatRadiusPx),
            CombatSampleSeconds = Mathf.Max(0.05f, Read(combat, "sample_sec", defaults.CombatSampleSeconds)),
            CombatEnterEnemies = (int)Read(combat, "enter_enemies", defaults.CombatEnterEnemies),
            CombatEnterHoldSeconds = Read(combat, "enter_hold_sec", defaults.CombatEnterHoldSeconds),
            CombatExitEnemies = (int)Read(combat, "exit_enemies", defaults.CombatExitEnemies),
            CombatExitHoldSeconds = Read(combat, "exit_hold_sec", defaults.CombatExitHoldSeconds),
        };
    }

    private static Godot.Collections.Dictionary Section(Godot.Collections.Dictionary root, string key) =>
        root.ContainsKey(key) ? root[key].AsGodotDictionary() : new Godot.Collections.Dictionary();

    private static float Read(Godot.Collections.Dictionary dict, string key, float fallback) =>
        dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;
}
