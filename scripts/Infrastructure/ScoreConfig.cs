using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Barème du score, lu dans data/scaling/score.json (plan 02 lot A).</summary>
public sealed class ScoreConfig
{
    public int PointsPerSecond { get; init; }
    public int PointsPerCrisis { get; init; }
    public int PointsPerPoi { get; init; }
    public int PointsBossDefeated { get; init; }
    public int PointsEndgameReached { get; init; }

    private readonly Dictionary<string, int> _killPoints = new();
    private readonly Dictionary<string, int> _chestPoints = new();
    private int _defaultKill;
    private int _defaultChest;

    private static ScoreConfig _cached;

    public int KillPoints(string enemyId) => _killPoints.TryGetValue(enemyId, out int points) ? points : _defaultKill;

    public int ChestPoints(string rarity) => _chestPoints.TryGetValue(rarity, out int points) ? points : _defaultChest;

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

        ScoreConfig config = new()
        {
            PointsPerSecond = (int)data["points_per_second"].AsDouble(),
            PointsPerCrisis = (int)data["points_per_crisis"].AsDouble(),
            PointsPerPoi = (int)data["points_per_poi"].AsDouble(),
            PointsBossDefeated = (int)data["points_boss_defeated"].AsDouble(),
            PointsEndgameReached = (int)data["points_endgame_reached"].AsDouble(),
        };
        config._defaultKill = ReadTable(data["kill_points"].AsGodotDictionary(), config._killPoints);
        config._defaultChest = ReadTable(data["chest_points"].AsGodotDictionary(), config._chestPoints);
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
