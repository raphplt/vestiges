using System;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Courbe d'XP du joueur, lue dans data/scaling/progression.json (plan 20 §6.6) et contrôlée en entier (plan 26 Q7a) :
/// tous les champs sont obligatoires et un coût nul, qui rendrait la montée de niveau sans fin, est refusé.
/// </summary>
public sealed class XpCurveConfig
{
    private const string ConfigPath = "res://data/scaling/progression.json";
    private static XpCurveConfig _cached;

    public float BaseXp { get; private init; }
    public float Exponent { get; private init; }
    public int EarlyLevels { get; private init; }
    public float EarlyMultiplierStart { get; private init; }
    public float EarlyMultiplierEnd { get; private init; }
    public float MaxXpPerLevel { get; private init; }

    /// <summary>XP pour passer du niveau <paramref name="level"/> au suivant.</summary>
    public float CostOf(int level)
    {
        float xp = BaseXp * Mathf.Pow(level, Exponent);
        if (level <= EarlyLevels)
        {
            float t = EarlyLevels > 1 ? (level - 1) / (float)(EarlyLevels - 1) : 0f;
            xp *= Mathf.Lerp(EarlyMultiplierStart, EarlyMultiplierEnd, t);
        }
        return Mathf.Min(xp, MaxXpPerLevel);
    }

    /// <summary>La courbe du jeu, lue une fois ; une courbe invalide arrête le chargement de la run (GameBootstrap).</summary>
    public static XpCurveConfig Load() =>
        TryLoad(out XpCurveConfig config, out string error) ? config : throw new InvalidOperationException(error);

    public static bool TryLoad(out XpCurveConfig config, out string error)
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

    public static bool TryParse(string json, out XpCurveConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "courbe d'XP", "base_xp", "exponent", "early_levels", "early_multiplier_start",
                "early_multiplier_end", "max_xp_per_level");
            XpCurveConfig parsed = new()
            {
                BaseXp = reader.Positive(root, "base_xp"),
                Exponent = reader.Positive(root, "exponent"),
                EarlyLevels = reader.Integer(root, "early_levels", 0, 100),
                EarlyMultiplierStart = reader.Positive(root, "early_multiplier_start"),
                EarlyMultiplierEnd = reader.Positive(root, "early_multiplier_end"),
                MaxXpPerLevel = reader.Positive(root, "max_xp_per_level"),
            };
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
