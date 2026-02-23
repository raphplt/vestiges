using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Vestiges.Infrastructure;

public class StartingKitData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    [JsonPropertyName("contents")]
    public Dictionary<string, int> Contents { get; set; } = new();
}

/// <summary>
/// Chargeur statique des kits de départ depuis data/meta/starting_kits.json.
/// </summary>
public static class StartingKitDataLoader
{
    private const string DataPath = "res://data/meta/starting_kits.json";

    private static readonly Dictionary<string, StartingKitData> _kits = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        _loaded = true;

        FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[StartingKitDataLoader] Cannot open {DataPath}");
            return;
        }

        string json = file.GetAsText();
        file.Close();

        try
        {
            List<StartingKitData> list = JsonSerializer.Deserialize<List<StartingKitData>>(json);
            if (list != null)
            {
                foreach (StartingKitData kit in list)
                    _kits[kit.Id] = kit;
            }
        }
        catch (JsonException ex)
        {
            GD.PushError($"[StartingKitDataLoader] Parse error: {ex.Message}");
        }

        GD.Print($"[StartingKitDataLoader] Loaded {_kits.Count} starting kit(s)");
    }

    public static StartingKitData Get(string id)
    {
        Load();
        return _kits.TryGetValue(id, out StartingKitData kit) ? kit : null;
    }

    public static List<StartingKitData> GetAll()
    {
        Load();
        return new List<StartingKitData>(_kits.Values);
    }
}
