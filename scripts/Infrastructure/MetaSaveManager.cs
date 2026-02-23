using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Vestiges.Infrastructure;

public class MetaSaveData
{
    [JsonPropertyName("vestiges")]
    public int Vestiges { get; set; }

    [JsonPropertyName("unlocked_characters")]
    public List<string> UnlockedCharacters { get; set; } = new() { "vagabond" };

    [JsonPropertyName("purchased_kits")]
    public List<string> PurchasedKits { get; set; } = new();

    [JsonPropertyName("selected_kit")]
    public string SelectedKit { get; set; } = "";

    [JsonPropertyName("stats")]
    public MetaStats Stats { get; set; } = new();
}

public class MetaStats
{
    [JsonPropertyName("max_nights_survived")]
    public int MaxNightsSurvived { get; set; }

    [JsonPropertyName("max_kills_in_run")]
    public int MaxKillsInRun { get; set; }

    [JsonPropertyName("total_runs")]
    public int TotalRuns { get; set; }
}

/// <summary>
/// Gestionnaire statique de la sauvegarde méta (progression persistante entre runs).
/// Centralise : Vestiges (monnaie), personnages débloqués, kits de départ, stats globales.
/// Sauvegarde dans user://meta_save.json.
/// </summary>
public static class MetaSaveManager
{
    private const string SavePath = "user://meta_save.json";

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
            Save();
            GD.Print("[MetaSaveManager] Created new meta save");
            return;
        }

        FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            _data = new MetaSaveData();
            return;
        }

        string json = file.GetAsText();
        file.Close();

        try
        {
            _data = JsonSerializer.Deserialize<MetaSaveData>(json) ?? new MetaSaveData();
        }
        catch (JsonException ex)
        {
            GD.PushWarning($"[MetaSaveManager] Failed to parse save: {ex.Message}");
            _data = new MetaSaveData();
        }

        // Vagabond is always unlocked
        if (!_data.UnlockedCharacters.Contains("vagabond"))
            _data.UnlockedCharacters.Add("vagabond");

        GD.Print($"[MetaSaveManager] Loaded — {_data.Vestiges} Vestiges, {_data.UnlockedCharacters.Count} characters unlocked");
    }

    public static void Save()
    {
        FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError("[MetaSaveManager] Cannot save meta data");
            return;
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(_data, options);
        file.StoreString(json);
        file.Close();
    }

    // --- Vestiges ---

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

    // --- Character Unlocks ---

    public static bool IsCharacterUnlocked(string characterId)
    {
        Load();
        return _data.UnlockedCharacters.Contains(characterId);
    }

    public static void UnlockCharacter(string characterId)
    {
        Load();
        if (!_data.UnlockedCharacters.Contains(characterId))
        {
            _data.UnlockedCharacters.Add(characterId);
            Save();
            GD.Print($"[MetaSaveManager] Character unlocked: {characterId}");
        }
    }

    public static List<string> GetUnlockedCharacters()
    {
        Load();
        return new List<string>(_data.UnlockedCharacters);
    }

    /// <summary>
    /// Vérifie les conditions de déblocage de tous les personnages.
    /// Retourne la liste des personnages nouvellement débloqués.
    /// </summary>
    public static List<string> CheckUnlocks()
    {
        Load();
        List<string> newUnlocks = new();

        List<CharacterData> allCharacters = CharacterDataLoader.GetAll();
        foreach (CharacterData character in allCharacters)
        {
            if (_data.UnlockedCharacters.Contains(character.Id))
                continue;

            bool shouldUnlock = character.UnlockCondition switch
            {
                "default" => true,
                "survive_3_nights" => _data.Stats.MaxNightsSurvived >= 3,
                "kill_200_in_run" => _data.Stats.MaxKillsInRun >= 200,
                _ => false
            };

            if (shouldUnlock)
            {
                _data.UnlockedCharacters.Add(character.Id);
                newUnlocks.Add(character.Id);
                GD.Print($"[MetaSaveManager] New unlock: {character.Name} ({character.UnlockCondition})");
            }
        }

        if (newUnlocks.Count > 0)
            Save();

        return newUnlocks;
    }

    // --- Stats ---

    public static void UpdateStats(RunRecord record)
    {
        Load();
        _data.Stats.TotalRuns++;

        if (record.NightsSurvived > _data.Stats.MaxNightsSurvived)
            _data.Stats.MaxNightsSurvived = record.NightsSurvived;

        if (record.TotalKills > _data.Stats.MaxKillsInRun)
            _data.Stats.MaxKillsInRun = record.TotalKills;

        Save();
    }

    public static MetaStats GetStats()
    {
        Load();
        return _data.Stats;
    }

    // --- Starting Kits ---

    public static HashSet<string> GetPurchasedKits()
    {
        Load();
        return new HashSet<string>(_data.PurchasedKits);
    }

    public static bool PurchaseKit(string kitId, int cost)
    {
        Load();
        if (_data.PurchasedKits.Contains(kitId))
            return false;

        if (!SpendVestiges(cost))
            return false;

        _data.PurchasedKits.Add(kitId);
        Save();
        GD.Print($"[MetaSaveManager] Kit purchased: {kitId}");
        return true;
    }

    public static string GetSelectedKit()
    {
        Load();
        return _data.SelectedKit;
    }

    public static void SelectKit(string kitId)
    {
        Load();
        _data.SelectedKit = kitId;
        Save();
    }
}
