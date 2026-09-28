using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Réserve de niveaux (plan 20 §6.7, R1-G), lue dans le bloc level_reserve de data/scaling/progression.json.</summary>
public sealed class LevelReserveConfig
{
    public float WindowSeconds { get; private init; } = 3f;
    public float HoldSeconds { get; private init; } = 3f;
    public int Crowd { get; private init; } = 60;
    public float CrowdRadius { get; private init; } = 600f;

    public static LevelReserveConfig Load()
    {
        FileAccess file = FileAccess.Open("res://data/scaling/progression.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[LevelReserveConfig] Cannot open progression.json");
            return new LevelReserveConfig();
        }

        Json json = new();
        Error error = json.Parse(file.GetAsText());
        file.Close();
        if (error != Error.Ok || !json.Data.AsGodotDictionary().ContainsKey("level_reserve"))
        {
            GD.PushError("[LevelReserveConfig] level_reserve absent ou illisible");
            return new LevelReserveConfig();
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary()["level_reserve"].AsGodotDictionary();
        LevelReserveConfig defaults = new();
        return new LevelReserveConfig
        {
            WindowSeconds = Read(dict, "window_seconds", defaults.WindowSeconds),
            HoldSeconds = Read(dict, "hold_seconds", defaults.HoldSeconds),
            Crowd = (int)Read(dict, "crowd", defaults.Crowd),
            CrowdRadius = Read(dict, "crowd_radius", defaults.CrowdRadius),
        };
    }

    private static float Read(Godot.Collections.Dictionary dict, string key, float fallback) =>
        dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;
}
