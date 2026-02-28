using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Vestiges.Simulation;

/// <summary>
/// Configuration d'un batch de simulations.
/// Chargée depuis un fichier JSON (data/simulation/*.json).
/// </summary>
public class SimulationBatchConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "unnamed_batch";

    [JsonPropertyName("runs_per_config")]
    public int RunsPerConfig { get; set; } = 10;

    [JsonPropertyName("configs")]
    public List<SimulationRunConfig> Configs { get; set; } = new();

    public static SimulationBatchConfig LoadFromFile(string path)
    {
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[SimulationConfig] Cannot open config: {path}");
            return null;
        }

        string json = file.GetAsText();
        file.Close();

        try
        {
            return JsonSerializer.Deserialize<SimulationBatchConfig>(json);
        }
        catch (JsonException ex)
        {
            GD.PushError($"[SimulationConfig] Parse error: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Configuration d'une variante de simulation.
/// Chaque config peut overrider les paramètres de scaling, le personnage, le profil AI, etc.
/// </summary>
public class SimulationRunConfig
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = "default";

    [JsonPropertyName("character_id")]
    public string CharacterId { get; set; } = "traqueur";

    [JsonPropertyName("profile")]
    public string ProfileName { get; set; } = "medium";

    [JsonPropertyName("perk_strategy")]
    public string PerkStrategyName { get; set; } = "random";

    [JsonPropertyName("time_scale")]
    public float TimeScale { get; set; } = 5f;

    [JsonPropertyName("max_duration_sec")]
    public float MaxDurationSec { get; set; } = 1800f;

    [JsonPropertyName("seed")]
    public ulong Seed { get; set; }

    [JsonPropertyName("scaling_overrides")]
    public Dictionary<string, float> ScalingOverrides { get; set; }
}
