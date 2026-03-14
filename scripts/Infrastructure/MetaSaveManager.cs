using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Vestiges.Infrastructure;

public class MetaSaveData
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 2;

    [JsonPropertyName("vestiges")]
    public int Vestiges { get; set; }

    [JsonPropertyName("unlocked_characters")]
    public List<string> UnlockedCharacters { get; set; } = new() { "traqueur" };

    [JsonPropertyName("stats")]
    public MetaStats Stats { get; set; } = new();

    [JsonPropertyName("discovered_souvenirs")]
    public List<string> DiscoveredSouvenirs { get; set; } = new();

    [JsonPropertyName("completed_quests")]
    public List<string> CompletedQuests { get; set; } = new();
}

public class MetaStats
{
    [JsonPropertyName("best_run_duration_sec")]
    public float BestRunDurationSec { get; set; }

    [JsonPropertyName("max_kills_in_run")]
    public int MaxKillsInRun { get; set; }

    [JsonPropertyName("total_runs")]
    public int TotalRuns { get; set; }

    [JsonPropertyName("total_crises_survived")]
    public int TotalCrisesSurvived { get; set; }

    [JsonPropertyName("best_score")]
    public int BestScore { get; set; }
}

/// <summary>
/// Gestionnaire statique de la sauvegarde méta V2.
/// Conserve uniquement les données encore valides après le pivot.
/// </summary>
public static class MetaSaveManager
{
    private const int CurrentVersion = 2;
    private const string SavePath = "user://meta_save.json";
    private const string LegacyArchivePath = "user://meta_save_legacy_v1.json";
    private const float VagabondUnlockDurationSec = 12f * 60f;

    private static MetaSaveData _data = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        _loaded = true;

        if (!FileAccess.FileExists(SavePath))
        {
            _data = new MetaSaveData();
            NormalizeData();
            Save();
            GD.Print("[MetaSaveManager] Created new V2 meta save");
            return;
        }

        FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            _data = new MetaSaveData();
            NormalizeData();
            return;
        }

        string json = file.GetAsText();
        file.Close();

        bool migrated = false;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            int version = root.TryGetProperty("version", out JsonElement versionElement)
                && versionElement.ValueKind == JsonValueKind.Number
                ? versionElement.GetInt32()
                : 0;

            _data = version >= CurrentVersion
                ? JsonSerializer.Deserialize<MetaSaveData>(json) ?? new MetaSaveData()
                : MigrateLegacySave(root, json);
            migrated = version < CurrentVersion;
        }
        catch (JsonException ex)
        {
            GD.PushWarning($"[MetaSaveManager] Failed to parse save: {ex.Message}");
            _data = new MetaSaveData();
        }

        NormalizeData();
        if (migrated)
            Save();
        GD.Print($"[MetaSaveManager] Loaded — {_data.Vestiges} Vestiges, {_data.UnlockedCharacters.Count} characters unlocked");
    }

    public static void Save()
    {
        NormalizeData();

        FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError("[MetaSaveManager] Cannot save meta data");
            return;
        }

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
        string json = JsonSerializer.Serialize(_data, options);
        file.StoreString(json);
        file.Close();
    }

    public static int GetVestiges()
    {
        Load();
        return _data.Vestiges;
    }

    public static void AddVestiges(int amount)
    {
        Load();
        _data.Vestiges += amount;
        Save();
        GD.Print($"[MetaSaveManager] +{amount} Vestiges (total: {_data.Vestiges})");
    }

    public static bool SpendVestiges(int amount)
    {
        Load();
        if (_data.Vestiges < amount)
            return false;

        _data.Vestiges -= amount;
        Save();
        return true;
    }

    public static bool IsCharacterUnlocked(string characterId)
    {
        Load();
        return _data.UnlockedCharacters.Contains(characterId);
    }

    public static void UnlockCharacter(string characterId)
    {
        Load();
        if (_data.UnlockedCharacters.Contains(characterId))
            return;

        _data.UnlockedCharacters.Add(characterId);
        Save();
        GD.Print($"[MetaSaveManager] Character unlocked: {characterId}");
    }

    public static List<string> GetUnlockedCharacters()
    {
        Load();
        return new List<string>(_data.UnlockedCharacters);
    }

    public static List<string> CheckUnlocks()
    {
        Load();
        CharacterDataLoader.Load();

        List<string> newUnlocks = new();
        foreach (CharacterData character in CharacterDataLoader.GetAll())
        {
            if (_data.UnlockedCharacters.Contains(character.Id))
                continue;

            bool shouldUnlock = character.UnlockCondition switch
            {
                "default" => true,
                "survive_3_nights" => _data.Stats.BestRunDurationSec >= VagabondUnlockDurationSec,
                "kill_200_in_run" => _data.Stats.MaxKillsInRun >= 200,
                _ => false
            };

            if (!shouldUnlock)
                continue;

            _data.UnlockedCharacters.Add(character.Id);
            newUnlocks.Add(character.Id);
            GD.Print($"[MetaSaveManager] New unlock: {character.Name} ({character.UnlockCondition})");
        }

        if (newUnlocks.Count > 0)
            Save();

        return newUnlocks;
    }

    public static void UpdateStats(RunRecord record)
    {
        Load();
        _data.Stats.TotalRuns++;
        _data.Stats.MaxKillsInRun = Mathf.Max(_data.Stats.MaxKillsInRun, record.TotalKills);
        _data.Stats.BestRunDurationSec = Mathf.Max(_data.Stats.BestRunDurationSec, record.RunDurationSec);
        _data.Stats.TotalCrisesSurvived += record.CrisesSurvived;
        _data.Stats.BestScore = Mathf.Max(_data.Stats.BestScore, record.Score);
        Save();
    }

    public static MetaStats GetStats()
    {
        Load();
        return _data.Stats;
    }

    public static bool IsSouvenirDiscovered(string souvenirId)
    {
        Load();
        return _data.DiscoveredSouvenirs.Contains(souvenirId);
    }

    public static bool HasSouvenir(string souvenirId)
    {
        return IsSouvenirDiscovered(souvenirId);
    }

    public static void DiscoverSouvenir(string souvenirId)
    {
        Load();
        if (_data.DiscoveredSouvenirs.Contains(souvenirId))
            return;

        _data.DiscoveredSouvenirs.Add(souvenirId);
        Save();
        GD.Print($"[MetaSaveManager] Souvenir discovered: {souvenirId} (total: {_data.DiscoveredSouvenirs.Count})");
    }

    public static List<string> GetDiscoveredSouvenirs()
    {
        Load();
        return new List<string>(_data.DiscoveredSouvenirs);
    }

    public static bool HasCompletedQuest(string questId)
    {
        Load();
        return _data.CompletedQuests.Contains(questId);
    }

    public static bool CompleteQuest(string questId)
    {
        Load();
        if (string.IsNullOrWhiteSpace(questId) || _data.CompletedQuests.Contains(questId))
            return false;

        _data.CompletedQuests.Add(questId);
        Save();
        GD.Print($"[MetaSaveManager] Quest completed: {questId}");
        return true;
    }

    public static List<string> GetCompletedQuests()
    {
        Load();
        return new List<string>(_data.CompletedQuests);
    }

    public static int GetDiscoveredSouvenirCount()
    {
        Load();
        return _data.DiscoveredSouvenirs.Count;
    }

    // Compatibilite legacy : les kits et mutateurs sont desactives en V2.
    public static HashSet<string> GetPurchasedKits() => new();
    public static bool PurchaseKit(string kitId, int cost) => false;
    public static string GetSelectedKit() => "";
    public static void SelectKit(string kitId) { }
    public static bool IsMutatorUnlocked(string mutatorId) => false;
    public static List<string> GetUnlockedMutators() => new();
    public static List<string> GetActiveMutators() => new();
    public static void SetActiveMutators(List<string> mutatorIds) { }
    public static void ToggleMutator(string mutatorId) { }
    public static List<string> CheckMutatorUnlocks() => new();

    private static void NormalizeData()
    {
        _data ??= new MetaSaveData();
        _data.Version = CurrentVersion;
        _data.Stats ??= new MetaStats();
        _data.UnlockedCharacters ??= new List<string>();
        _data.DiscoveredSouvenirs ??= new List<string>();
        _data.CompletedQuests ??= new List<string>();

        CharacterDataLoader.Load();
        HashSet<string> supportedCharacters = CharacterDataLoader.GetAll()
            .Select(character => character.Id)
            .ToHashSet();

        _data.UnlockedCharacters = _data.UnlockedCharacters
            .Where(id => supportedCharacters.Contains(id))
            .Distinct()
            .ToList();

        if (!_data.UnlockedCharacters.Contains("traqueur"))
            _data.UnlockedCharacters.Insert(0, "traqueur");

        _data.DiscoveredSouvenirs = _data.DiscoveredSouvenirs
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        _data.CompletedQuests = _data.CompletedQuests
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
    }

    private static MetaSaveData MigrateLegacySave(JsonElement root, string rawJson)
    {
        MetaSaveData migrated = new()
        {
            Vestiges = TryGetInt(root, "vestiges"),
            UnlockedCharacters = ReadStringList(root, "unlocked_characters"),
            DiscoveredSouvenirs = ReadStringList(root, "discovered_souvenirs"),
            Stats = new MetaStats()
        };

        if (root.TryGetProperty("stats", out JsonElement stats) && stats.ValueKind == JsonValueKind.Object)
        {
            int legacyMaxNights = TryGetInt(stats, "max_nights_survived");
            migrated.Stats.MaxKillsInRun = TryGetInt(stats, "max_kills_in_run");
            migrated.Stats.TotalRuns = TryGetInt(stats, "total_runs");
            migrated.Stats.TotalCrisesSurvived = TryGetInt(stats, "total_crises_survived");
            migrated.Stats.BestScore = TryGetInt(stats, "best_score");
            migrated.Stats.BestRunDurationSec = Mathf.Max(
                TryGetFloat(stats, "best_run_duration_sec"),
                legacyMaxNights * 240f);
        }

        ArchiveLegacyJson(rawJson);
        GD.Print("[MetaSaveManager] Legacy V1 meta save archived and migrated to V2");
        return migrated;
    }

    private static void ArchiveLegacyJson(string rawJson)
    {
        FileAccess archive = FileAccess.Open(LegacyArchivePath, FileAccess.ModeFlags.Write);
        if (archive == null)
        {
            GD.PushWarning($"[MetaSaveManager] Failed to archive legacy save to {LegacyArchivePath}");
            return;
        }

        archive.StoreString(rawJson);
        archive.Close();
    }

    private static List<string> ReadStringList(JsonElement root, string propertyName)
    {
        List<string> result = new();
        if (!root.TryGetProperty(propertyName, out JsonElement arrayElement) || arrayElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (JsonElement item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                continue;

            string value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                result.Add(value);
        }

        return result;
    }

    private static int TryGetInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement element))
            return 0;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetInt32(),
            JsonValueKind.String when int.TryParse(element.GetString(), out int value) => value,
            _ => 0
        };
    }

    private static float TryGetFloat(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement element))
            return 0f;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetSingle(),
            JsonValueKind.String when float.TryParse(element.GetString(), out float value) => value,
            _ => 0f
        };
    }
}
