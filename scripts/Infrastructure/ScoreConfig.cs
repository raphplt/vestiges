using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Barème du score, lu dans data/scaling/score.json : les points de chaque élimination (DECISIONS §40). Contrôlé en
/// entier (plan 26 Q7a) : « default » obligatoire, points entiers positifs ou nuls, chaque clé est une créature connue.
/// </summary>
public sealed class ScoreConfig
{
    private const string ConfigPath = "res://data/scaling/score.json";
    private const int MaxPoints = 1_000_000;

    private readonly Dictionary<string, int> _killPoints = new();
    private int _defaultKill;

    private static ScoreConfig _cached;

    public int KillPoints(string enemyId) => _killPoints.TryGetValue(enemyId, out int points) ? points : _defaultKill;

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
        EnemyDataLoader.Load();
        if (!TryParse(file.GetAsText(), EnemyDataLoader.Exists, out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        _cached = config;
        error = null;
        return true;
    }

    /// <param name="enemyExists">Une clé du barème doit nommer une créature du catalogue.</param>
    public static bool TryParse(string json, Func<string, bool> enemyExists, out ScoreConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "barème", "kill_points");
            JsonElement table = reader.Section("kill_points");
            ScoreConfig parsed = new();
            bool hasDefault = false;
            foreach ((string key, _) in reader.Entries(table, "kill_points"))
            {
                int points = reader.Integer(table, key, 0, MaxPoints);
                if (key == "default")
                {
                    parsed._defaultKill = points;
                    hasDefault = true;
                }
                else if (!enemyExists(key))
                {
                    reader.Fail($"kill_points : créature « {key} » inconnue");
                }
                else
                {
                    parsed._killPoints[key] = points;
                }
            }
            if (!hasDefault)
                reader.Fail("kill_points : default absent");
            error = reader.Error;
            config = error == null ? parsed : null;
            return error == null;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }
}
