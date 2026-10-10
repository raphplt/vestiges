using System;
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

    [JsonPropertyName("stats")]
    public MetaStats Stats { get; set; } = new();

    [JsonPropertyName("discovered_souvenirs")]
    public List<string> DiscoveredSouvenirs { get; set; } = new();

    /// <summary>
    /// Quêtes de déblocage accomplies (plan 06 §9). Les pièces débloquées s'en déduisent : une pièce gardée par une
    /// quête est disponible dès que cette quête figure ici, même si le catalogue lui ajoute une pièce plus tard.
    /// </summary>
    [JsonPropertyName("completed_quests")]
    public List<string> CompletedQuests { get; set; } = new();

    /// <summary>
    /// Avancée de chaque quête non accomplie, une valeur par condition : la meilleure atteinte en une run, ou le cumul
    /// de toutes les runs pour une quête de cumul.
    /// </summary>
    [JsonPropertyName("quest_progress")]
    public Dictionary<string, List<float>> QuestProgress { get; set; } = new();

    /// <summary>Aides déjà montrées une fois au joueur (objectif d'un micro-événement, plan 24 A3).</summary>
    [JsonPropertyName("seen_hints")]
    public List<string> SeenHints { get; set; } = new();

    /// <summary>Dernières runs dont les acquis sont engagés : une même run ne se règle jamais deux fois (plan 26 Q2b).</summary>
    [JsonPropertyName("settled_runs")]
    public List<string> SettledRuns { get; set; } = new();

    /// <summary>Relevés de runs réglées dont l'historique n'est pas encore écrit ; chacun est retiré une fois inscrit.</summary>
    [JsonPropertyName("pending_history")]
    public List<RunRecord> PendingHistory { get; set; } = new();
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
    private static string SavePath => DevelopmentMode.GetSavePath("meta_save.json");
    private static string LegacyArchivePath => DevelopmentMode.GetSavePath("meta_save_legacy_v1.json");

    private static MetaSaveData _data = new();
    private static bool _loaded;
    private static string _writeBlockReason = "";

    /// <summary>Provenance du profil chargé (neuf, lu, repris sur la copie de secours, illisible, futur).</summary>
    public static SaveFile.ReadStatus LoadStatus { get; private set; } = SaveFile.ReadStatus.Missing;

    /// <summary>Faux quand le fichier vient d'une version plus récente ou n'a pas pu être lu : on ne le réécrit pas.</summary>
    public static bool CanWrite => _writeBlockReason.Length == 0;

    private readonly record struct ParsedMetaSave(MetaSaveData Data, string LegacyJson);

    /// <summary>Résultat du règlement d'une run : déjà réglée, issue de l'unique écriture.</summary>
    public readonly record struct Settlement(bool AlreadySettled, SaveFile.WriteResult Write);

    private const int SettledRunMemory = 20;
    private static int _batchDepth;
    private static bool _batchDirty;

    internal static void ReloadProfile()
    {
        _data = new MetaSaveData();
        _loaded = false;
        _writeBlockReason = "";
        LoadStatus = SaveFile.ReadStatus.Missing;
        _batchDepth = 0;
        _batchDirty = false;
    }

    public static void Load()
    {
        if (_loaded)
            return;

        _loaded = true;
        _writeBlockReason = "";

        SaveFile.ReadResult<ParsedMetaSave> read = SaveFile.Read<ParsedMetaSave>(SavePath, ParseSave);
        LoadStatus = read.Status;
        switch (read.Status)
        {
            case SaveFile.ReadStatus.Missing:
                _data = new MetaSaveData();
                NormalizeData();
                Save();
                GD.Print("[MetaSaveManager] Created new V2 meta save");
                return;
            case SaveFile.ReadStatus.Unreadable:
                GD.PushError($"[MetaSaveManager] Sauvegarde illisible, mise de côté ; profil vide ({read.Detail})");
                _data = new MetaSaveData();
                NormalizeData();
                return;
            case SaveFile.ReadStatus.FutureVersion:
            case SaveFile.ReadStatus.Inaccessible:
                _writeBlockReason = read.Detail;
                GD.PushError($"[MetaSaveManager] Sauvegarde laissée intacte, aucune écriture pendant cette session ({read.Detail})");
                _data = read.Value.Data ?? new MetaSaveData();
                NormalizeData();
                return;
        }

        _data = read.Value.Data;
        bool migrated = read.Value.LegacyJson != null;
        if (migrated && !ArchiveLegacyJson(read.Value.LegacyJson))
            _writeBlockReason = "archive de la sauvegarde V1 impossible";
        NormalizeData();
        if (migrated)
            Save();
        GD.Print($"[MetaSaveManager] Loaded — {_data.Vestiges} Vestiges, {_data.CompletedQuests.Count} quests completed");
    }

    /// <summary>
    /// Écriture du profil. Entre <see cref="BeginBatch"/> et <see cref="EndBatch"/>, elle est seulement notée :
    /// l'écriture réelle et son issue viennent de <see cref="EndBatch"/>.
    /// </summary>
    public static SaveFile.WriteResult Save()
    {
        Load();
        NormalizeData();
        if (_batchDepth > 0)
        {
            _batchDirty = true;
            return SaveFile.WriteResult.Ok;
        }
        if (!CanWrite)
        {
            GD.PushWarning($"[MetaSaveManager] Écriture refusée : {_writeBlockReason}");
            return SaveFile.WriteResult.Failed(_writeBlockReason);
        }

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
        SaveFile.WriteResult result = SaveFile.Write(SavePath, JsonSerializer.Serialize(_data, options));
        if (!result.Succeeded)
            GD.PushError($"[MetaSaveManager] Cannot save meta data: {result.Error}");
        return result;
    }

    /// <summary>Regroupe les changements d'une même attribution en une seule écriture.</summary>
    public static void BeginBatch()
    {
        Load();
        _batchDepth++;
    }

    /// <summary>Ferme un lot ; seul le lot le plus extérieur écrit, et lui seul rend l'issue réelle de l'écriture.</summary>
    public static SaveFile.WriteResult EndBatch()
    {
        if (_batchDepth == 0)
            return SaveFile.WriteResult.Ok;
        _batchDepth--;
        if (_batchDepth > 0 || !_batchDirty)
            return SaveFile.WriteResult.Ok;
        _batchDirty = false;
        return Save();
    }

    /// <summary>
    /// Engage les acquis d'une run en une écriture : Vestiges, statistiques, identité de la run et relevé en attente
    /// d'historique. Une run déjà réglée ne rapporte rien de plus. Les quêtes s'enregistrent en run, pas ici.
    /// </summary>
    public static Settlement SettleRun(RunRecord record, int vestiges)
    {
        Load();
        if (string.IsNullOrEmpty(record.RunId))
            return new Settlement(false, SaveFile.WriteResult.Failed("run sans identité"));
        if (_data.SettledRuns.Contains(record.RunId))
            return new Settlement(true, SaveFile.WriteResult.Ok);

        SaveFile.WriteResult write;
        BeginBatch();
        try
        {
            _data.Vestiges += vestiges;
            UpdateStats(record);
            _data.SettledRuns.Add(record.RunId);
            if (_data.SettledRuns.Count > SettledRunMemory)
                _data.SettledRuns.RemoveRange(0, _data.SettledRuns.Count - SettledRunMemory);
            _data.PendingHistory.Add(record);
            _batchDirty = true;
        }
        finally
        {
            write = EndBatch();
        }
        GD.Print($"[MetaSaveManager] Run {record.RunId} settled: +{vestiges} Vestiges (total: {_data.Vestiges})");
        return new Settlement(false, write);
    }

    public static List<RunRecord> GetPendingHistory()
    {
        Load();
        return new List<RunRecord>(_data.PendingHistory);
    }

    public static SaveFile.WriteResult ClearPendingHistory(string runId)
    {
        Load();
        if (_data.PendingHistory.RemoveAll(run => run.RunId == runId) == 0)
            return SaveFile.WriteResult.Ok;
        return Save();
    }

    /// <summary>Contrôle la forme avant toute désérialisation ; une version future n'est jamais ramenée à l'actuelle.</summary>
    private static SaveFile.Parse<ParsedMetaSave> ParseSave(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return SaveFile.Parse<ParsedMetaSave>.Invalid($"racine {root.ValueKind}, objet attendu");

            int version = 0;
            if (root.TryGetProperty("version", out JsonElement versionElement)
                && (versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out version) || version < 0))
                return SaveFile.Parse<ParsedMetaSave>.Invalid($"version invalide : {versionElement.GetRawText()}");

            if (version > CurrentVersion)
                return SaveFile.Parse<ParsedMetaSave>.Future(new ParsedMetaSave(TryDeserializeFuture(json), null),
                    $"version {version} plus récente que {CurrentVersion}");

            if (version < CurrentVersion)
                return SaveFile.Parse<ParsedMetaSave>.Valid(new ParsedMetaSave(MigrateLegacySave(root), json));

            MetaSaveData data = JsonSerializer.Deserialize<MetaSaveData>(json);
            return data == null
                ? SaveFile.Parse<ParsedMetaSave>.Invalid("contenu nul")
                : SaveFile.Parse<ParsedMetaSave>.Valid(new ParsedMetaSave(data, null));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or NotSupportedException)
        {
            return SaveFile.Parse<ParsedMetaSave>.Invalid(ex.Message);
        }
    }

    /// <summary>Lecture au mieux d'un profil plus récent, pour l'afficher sans jamais le réécrire.</summary>
    private static MetaSaveData TryDeserializeFuture(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MetaSaveData>(json) ?? new MetaSaveData();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or NotSupportedException)
        {
            return new MetaSaveData();
        }
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

    /// <summary>
    /// Règle unique d'accès (plan 06 §9) : une pièce qu'aucune quête ne garde est disponible dès le départ, les autres
    /// une fois leur quête accomplie. Le mode dev ouvre tout. Loot, offres de niveau, Collection et accueil la partagent.
    /// </summary>
    public static bool IsUnlocked(UnlockKind kind, string id)
    {
        Load();
        if (DevelopmentMode.IsEnabled)
            return true;
        QuestDefinition quest = QuestDataLoader.FindUnlocking(kind, id);
        return quest == null || _data.CompletedQuests.Contains(quest.Id);
    }

    public static bool IsCharacterUnlocked(string characterId) => IsUnlocked(UnlockKind.Character, characterId);
    public static bool IsWeaponUnlocked(string weaponId) => IsUnlocked(UnlockKind.Weapon, weaponId);
    public static bool IsObjectUnlocked(string objectId) => IsUnlocked(UnlockKind.Object, objectId);

    /// <summary>
    /// Accomplit une quête, ce qui débloque ses pièces : une seule écriture, aussitôt (plan 06 §9.8). Faux si elle
    /// l'était déjà ; <paramref name="saved"/> rend l'issue de l'écriture.
    /// </summary>
    public static bool ClaimQuest(QuestDefinition quest, out SaveFile.WriteResult saved)
    {
        Load();
        saved = SaveFile.WriteResult.Ok;
        if (quest == null || _data.CompletedQuests.Contains(quest.Id))
            return false;
        BeginBatch();
        try
        {
            _data.CompletedQuests.Add(quest.Id);
            _data.QuestProgress.Remove(quest.Id);
            _batchDirty = true;
        }
        finally
        {
            saved = EndBatch();
        }
        GD.Print($"[MetaSaveManager] Quest completed: {quest.Id}");
        return true;
    }

    /// <summary>Avancée retenue d'une quête, une valeur par condition (vide si rien n'est retenu).</summary>
    public static IReadOnlyList<float> GetQuestProgress(string questId)
    {
        Load();
        return _data.QuestProgress.TryGetValue(questId, out List<float> values) ? values : Array.Empty<float>();
    }

    /// <summary>
    /// Retient l'avancée d'une run pour une quête non accomplie : la meilleure valeur par condition, ou l'ajout au
    /// cumul. Seulement noté : l'appelant écrit par <see cref="Save"/> ou dans un lot.
    /// </summary>
    public static void RecordQuestProgress(QuestDefinition quest, IReadOnlyList<float> runValues)
    {
        Load();
        if (quest == null || _data.CompletedQuests.Contains(quest.Id))
            return;
        if (!_data.QuestProgress.TryGetValue(quest.Id, out List<float> values))
        {
            values = new List<float>();
            _data.QuestProgress[quest.Id] = values;
        }
        while (values.Count < quest.Conditions.Count)
            values.Add(0f);
        for (int i = 0; i < quest.Conditions.Count && i < runValues.Count; i++)
            values[i] = quest.Scope == QuestScope.Cumulative ? values[i] + runValues[i] : Mathf.Max(values[i], runValues[i]);
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

    /// <summary>Vrai la première fois que cette aide est demandée pour ce profil ; elle est alors retenue.</summary>
    public static bool MarkHintSeen(string hintId)
    {
        Load();
        if (string.IsNullOrWhiteSpace(hintId) || _data.SeenHints.Contains(hintId))
            return false;
        _data.SeenHints.Add(hintId);
        Save();
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
        _data.DiscoveredSouvenirs ??= new List<string>();
        _data.CompletedQuests ??= new List<string>();
        _data.QuestProgress ??= new Dictionary<string, List<float>>();
        _data.SeenHints ??= new List<string>();
        _data.SettledRuns ??= new List<string>();
        _data.PendingHistory ??= new List<RunRecord>();
        _data.PendingHistory.RemoveAll(run => run == null || string.IsNullOrEmpty(run.RunId));

        // Seules restent les quêtes du catalogue actuel : les anciennes quêtes de progression ne débloquent plus rien
        // (plan 06 §9.8). Un catalogue refusé ne fait rien oublier : le profil serait réécrit sans ses acquis.
        _data.CompletedQuests = _data.CompletedQuests.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (QuestDataLoader.TryLoad(out _))
        {
            _data.CompletedQuests.RemoveAll(id => QuestDataLoader.Get(id) == null);
            foreach (string questId in _data.QuestProgress.Keys.ToList())
            {
                if (QuestDataLoader.Get(questId) == null || _data.CompletedQuests.Contains(questId) || _data.QuestProgress[questId] == null)
                    _data.QuestProgress.Remove(questId);
            }
        }

        // Le Journal des Souvenirs se remplit en dev sans falsifier de quête ; les déblocages, eux, suivent IsUnlocked.
        if (DevelopmentMode.IsEnabled)
            _data.DiscoveredSouvenirs = SouvenirDataLoader.GetAll().Select(souvenir => souvenir.Id).ToList();

        _data.DiscoveredSouvenirs = _data.DiscoveredSouvenirs
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
    }

    private static MetaSaveData MigrateLegacySave(JsonElement root)
    {
        MetaSaveData migrated = new()
        {
            Vestiges = TryGetInt(root, "vestiges"),
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

        return migrated;
    }

    private static bool ArchiveLegacyJson(string rawJson)
    {
        SaveFile.WriteResult archived = SaveFile.Write(LegacyArchivePath, rawJson);
        if (archived.Succeeded)
            GD.Print("[MetaSaveManager] Legacy V1 meta save archived and migrated to V2");
        else
            GD.PushError($"[MetaSaveManager] Failed to archive legacy save to {LegacyArchivePath}: {archived.Error}");
        return archived.Succeeded;
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
