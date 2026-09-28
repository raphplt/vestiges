using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Courbe d'XP du joueur, lue dans data/scaling/progression.json (plan 20 §6.6).</summary>
public sealed class XpCurveConfig
{
    public float BaseXp { get; private init; } = 20f;
    public float Exponent { get; private init; } = 1.35f;
    public int EarlyLevels { get; private init; } = 5;
    public float EarlyMultiplierStart { get; private init; } = 1.65f;
    public float EarlyMultiplierEnd { get; private init; } = 1.2f;
    public float MaxXpPerLevel { get; private init; } = float.MaxValue;

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

    public static XpCurveConfig Load()
    {
        FileAccess file = FileAccess.Open("res://data/scaling/progression.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[XpCurveConfig] Cannot open progression.json");
            return new XpCurveConfig();
        }

        Json json = new();
        Error error = json.Parse(file.GetAsText());
        file.Close();
        if (error != Error.Ok)
        {
            GD.PushError($"[XpCurveConfig] Parse error: {json.GetErrorMessage()}");
            return new XpCurveConfig();
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        XpCurveConfig defaults = new();
        return new XpCurveConfig
        {
            BaseXp = Read(dict, "base_xp", defaults.BaseXp),
            Exponent = Read(dict, "exponent", defaults.Exponent),
            EarlyLevels = (int)Read(dict, "early_levels", defaults.EarlyLevels),
            EarlyMultiplierStart = Read(dict, "early_multiplier_start", defaults.EarlyMultiplierStart),
            EarlyMultiplierEnd = Read(dict, "early_multiplier_end", defaults.EarlyMultiplierEnd),
            MaxXpPerLevel = Read(dict, "max_xp_per_level", defaults.MaxXpPerLevel),
        };
    }

    private static float Read(Godot.Collections.Dictionary dict, string key, float fallback) =>
        dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;
}
