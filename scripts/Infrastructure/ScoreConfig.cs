using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Barème du score, lu dans data/scaling/score.json : les points de chaque élimination (DECISIONS §40).</summary>
public sealed class ScoreConfig
{
    private readonly Dictionary<string, int> _killPoints = new();
    private int _defaultKill;

    private static ScoreConfig _cached;

    public int KillPoints(string enemyId) => _killPoints.TryGetValue(enemyId, out int points) ? points : _defaultKill;

    public static ScoreConfig Load()
    {
        if (_cached != null)
            return _cached;

        using FileAccess file = FileAccess.Open("res://data/scaling/score.json", FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidOperationException("Barème du score absent.");
        using Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException("Barème du score invalide.");
        Godot.Collections.Dictionary data = json.Data.AsGodotDictionary();

        ScoreConfig config = new();
        config._defaultKill = ReadTable(data["kill_points"].AsGodotDictionary(), config._killPoints);
        _cached = config;
        return _cached;
    }

    /// <summary>Remplit la table et renvoie sa valeur « default ».</summary>
    private static int ReadTable(Godot.Collections.Dictionary table, Dictionary<string, int> target)
    {
        foreach (Variant key in table.Keys)
            target[key.AsString()] = (int)table[key].AsDouble();
        return target.TryGetValue("default", out int fallback) ? fallback : 0;
    }
}
