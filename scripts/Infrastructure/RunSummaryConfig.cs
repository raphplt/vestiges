using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Réglages des relevés de run pour le bilan (data/ui/run_summary.json, plan 02 lot D M2).</summary>
public sealed class RunSummaryConfig
{
    public float PixelsPerMeter { get; private init; } = 16f;
    public float DistanceStepSeconds { get; private init; } = 0.25f;
    public float TimelineSampleSeconds { get; private init; } = 10f;

    public static RunSummaryConfig Load()
    {
        using FileAccess file = FileAccess.Open("res://data/ui/run_summary.json", FileAccess.ModeFlags.Read);
        Json json = new();
        if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError("[RunSummaryConfig] Cannot read data/ui/run_summary.json");
            return new RunSummaryConfig();
        }

        Godot.Collections.Dictionary d = json.Data.AsGodotDictionary();
        RunSummaryConfig defaults = new();
        return new RunSummaryConfig
        {
            PixelsPerMeter = Read(d, "pixels_per_meter", defaults.PixelsPerMeter),
            DistanceStepSeconds = Read(d, "distance_step_seconds", defaults.DistanceStepSeconds),
            TimelineSampleSeconds = Read(d, "timeline_sample_seconds", defaults.TimelineSampleSeconds),
        };
    }

    private static float Read(Godot.Collections.Dictionary d, string key, float fallback) =>
        d.ContainsKey(key) ? (float)d[key].AsDouble() : fallback;
}
