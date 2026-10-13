using System;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Barème de fin de run, lu dans data/scaling/score.json : les Vestiges gagnés pour un nombre d'éliminations
/// (DECISIONS §155). Contrôlé en entier (plan 26 Q7a) : un taux strictement positif, aucune autre clé.
/// </summary>
public sealed class ScoreConfig
{
    private const string ConfigPath = "res://data/scaling/score.json";

    private static ScoreConfig _cached;

    public float KillsPerVestige { get; private init; }

    /// <summary>Vestiges d'une run de <paramref name="kills"/> éliminations, arrondis en dessous.</summary>
    public int VestigesFor(int kills) => Mathf.FloorToInt(kills / KillsPerVestige);

    /// <summary>Le barème du jeu, lu une fois ; un barème invalide arrête le chargement de la run (GameBootstrap).</summary>
    public static ScoreConfig Load() =>
        TryLoad(out ScoreConfig config, out string error) ? config : throw new InvalidOperationException(error);

    public static bool TryLoad(out ScoreConfig config, out string error)
    {
        if (_cached != null)
        {
            config = _cached;
            error = null;
            return true;
        }
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            config = null;
            error = $"{ConfigPath} absent";
            return false;
        }
        if (!TryParse(file.GetAsText(), out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        _cached = config;
        error = null;
        return true;
    }

    public static bool TryParse(string json, out ScoreConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "barème", "kills_per_vestige");
            float rate = reader.Positive(reader.Root, "kills_per_vestige");
            error = reader.Error;
            config = error == null ? new ScoreConfig { KillsPerVestige = rate } : null;
            return error == null;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }
}
