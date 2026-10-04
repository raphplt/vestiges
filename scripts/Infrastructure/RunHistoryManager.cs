using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Vestiges.Infrastructure;

public class RunRecord
{
    [JsonPropertyName("provenance")]
    [JsonConverter(typeof(JsonStringEnumConverter<RunProvenance>))]
    public RunProvenance Provenance { get; set; } = RunProvenance.Normal;

    [JsonPropertyName("version")]
    public int Version { get; set; } = RunHistoryManager.CurrentVersion;

    /// <summary>Identité de la fin de run, pour ne jamais la régler ni l'inscrire deux fois (absente avant Q2b).</summary>
    [JsonPropertyName("run_id")]
    public string RunId { get; set; }

    [JsonPropertyName("character_id")]
    public string CharacterId { get; set; }

    [JsonPropertyName("character_name")]
    public string CharacterName { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("total_kills")]
    public int TotalKills { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("run_phase")]
    public string RunPhase { get; set; }

    [JsonPropertyName("crises_survived")]
    public int CrisesSurvived { get; set; }

    [JsonPropertyName("death_cause")]
    public string DeathCause { get; set; }

    [JsonPropertyName("death_phase")]
    public string DeathPhase { get; set; }

    [JsonPropertyName("perk_ids")]
    public List<string> PerkIds { get; set; }

    [JsonPropertyName("weapon_id")]
    public string WeaponId { get; set; }

    [JsonPropertyName("total_damage_dealt")]
    public float TotalDamageDealt { get; set; }

    [JsonPropertyName("total_damage_taken")]
    public float TotalDamageTaken { get; set; }

    [JsonPropertyName("pois_explored")]
    public int PoisExplored { get; set; }

    [JsonPropertyName("chests_opened")]
    public int ChestsOpened { get; set; }

    [JsonPropertyName("max_level")]
    public int MaxLevel { get; set; }

    [JsonPropertyName("run_duration_sec")]
    public float RunDurationSec { get; set; }

    [JsonPropertyName("seed")]
    public ulong Seed { get; set; }

    [JsonPropertyName("combat_score")]
    public int CombatScoreDetail { get; set; }

    [JsonPropertyName("total_spawned")]
    public int TotalSpawned { get; set; }

    [JsonPropertyName("peak_enemies")]
    public int PeakEnemies { get; set; }

    [JsonPropertyName("avg_pressure")]
    public float AvgPressure { get; set; }

    [JsonPropertyName("final_hp_scale")]
    public float FinalHpScale { get; set; }

    [JsonPropertyName("final_dmg_scale")]
    public float FinalDmgScale { get; set; }

    [JsonPropertyName("active_mutators")]
    public List<string> ActiveMutators { get; set; }

    [JsonPropertyName("mutator_multiplier")]
    public float MutatorMultiplier { get; set; } = 1f;

    [JsonPropertyName("boss_defeated")]
    public bool BossDefeated { get; set; }

    [JsonPropertyName("endgame_reached")]
    public bool EndgameReached { get; set; }

    [JsonPropertyName("sim_label")]
    public string SimLabel { get; set; }

    [JsonPropertyName("sim_profile")]
    public string SimProfile { get; set; }

    [JsonPropertyName("sim_perk_strategy")]
    public string SimPerkStrategy { get; set; }

    // Relevés du bilan (version 3, plan 02 lot D M2). Absents des runs plus anciennes : null = non mesuré, jamais zéro.

    /// <summary>Plus grande distance au sol atteinte depuis le départ, en mètres.</summary>
    [JsonPropertyName("max_distance_m")]
    public float? MaxDistanceMeters { get; set; }

    [JsonPropertyName("travelled_m")]
    public float? TravelledMeters { get; set; }

    /// <summary>Armes en fin de run, dans l'ordre des emplacements.</summary>
    [JsonPropertyName("weapons")]
    public List<RunWeaponRecord> Weapons { get; set; }

    [JsonPropertyName("elites_killed")]
    public int? ElitesKilled { get; set; }

    [JsonPropertyName("sovereigns_killed")]
    public int? SovereignsKilled { get; set; }

    [JsonPropertyName("bosses_killed")]
    public int? BossesKilled { get; set; }
}

/// <summary>Une arme du build à la fin de la run : niveau, dégâts infligés et créatures achevées.</summary>
public class RunWeaponRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("damage")]
    public float Damage { get; set; }

    [JsonPropertyName("kills")]
    public int Kills { get; set; }

    /// <summary>Temps passé dans le build, pour les dégâts par seconde (absent avant le bilan dense).</summary>
    [JsonPropertyName("held_sec")]
    public float? HeldSeconds { get; set; }
}

/// <summary>
/// Historique de runs V2.
/// Les historiques V1 sont archives separement puis l'historique V2 repart proprement.
/// Les versions suivantes (3 : relevés du bilan) ne font qu'ajouter des champs facultatifs : une run plus ancienne
/// se relit telle quelle, ses champs absents valent « non mesuré ».
/// </summary>
public static class RunHistoryManager
{
    public const int CurrentVersion = 3;
    // Première version V2 : en dessous, c'est un historique V1 (archivé). Une hausse de CurrentVersion ne doit
    // jamais y envoyer les runs V2.
    private const int FirstV2Version = 2;
    private static string HistoryPath => DevelopmentMode.GetSavePath("run_history.json");
    private static string LegacyHistoryPath => DevelopmentMode.GetSavePath("run_history_legacy_v1.json");
    private const int MaxEntries = 50;

    private static List<RunRecord> _history = new();
    private static bool _loaded;
    private static string _writeBlockReason = "";

    public static SaveFile.ReadStatus LoadStatus { get; private set; } = SaveFile.ReadStatus.Missing;
    public static bool CanWrite => _writeBlockReason.Length == 0;

    private readonly record struct ParsedHistory(List<RunRecord> Runs, string LegacyJson);

    public static void Load()
    {
        if (_loaded)
            return;

        _loaded = true;
        _writeBlockReason = "";

        SaveFile.ReadResult<ParsedHistory> read = SaveFile.Read<ParsedHistory>(HistoryPath, ParseHistory);
        LoadStatus = read.Status;
        _history = read.Value.Runs ?? new List<RunRecord>();
        switch (read.Status)
        {
            case SaveFile.ReadStatus.Unreadable:
                GD.PushError($"[RunHistoryManager] Historique illisible, mis de côté ; historique vide ({read.Detail})");
                return;
            case SaveFile.ReadStatus.FutureVersion:
            case SaveFile.ReadStatus.Inaccessible:
                _writeBlockReason = read.Detail;
                GD.PushError($"[RunHistoryManager] Historique laissé intact, aucune écriture pendant cette session ({read.Detail})");
                return;
        }

        if (read.Value.LegacyJson != null)
        {
            if (!ArchiveLegacyHistory(read.Value.LegacyJson))
            {
                _writeBlockReason = "archive de l'historique V1 impossible";
                return;
            }
            Save();
            GD.Print("[RunHistoryManager] Legacy V1 history archived; V2 history reset");
            return;
        }

        GD.Print($"[RunHistoryManager] Loaded {_history.Count} run(s)");
    }

    public static SaveFile.WriteResult SaveRun(RunRecord record)
    {
        Load();

        record.Version = CurrentVersion;
        List<RunRecord> previous = new(_history);
        _history.Insert(0, record);

        if (_history.Count > MaxEntries)
            _history.RemoveRange(MaxEntries, _history.Count - MaxEntries);

        SaveFile.WriteResult result = Save();
        if (!result.Succeeded)
        {
            // La mémoire reste fidèle au disque : une run non écrite n'est pas « déjà inscrite » pour la réparation.
            _history = previous;
            return result;
        }
        GD.Print($"[RunHistoryManager] Saved run: {record.CharacterName} — Score {record.Score}, {record.RunDurationSec:F0}s, {record.CrisesSurvived} crises");
        return result;
    }

    public static bool Contains(string runId)
    {
        Load();
        if (string.IsNullOrEmpty(runId))
            return false;
        foreach (RunRecord run in _history)
        {
            if (run.RunId == runId)
                return true;
        }
        return false;
    }

    public static List<RunRecord> GetHistory()
    {
        Load();
        return new List<RunRecord>(_history);
    }

    public static int GetBestScore()
    {
        Load();
        int best = 0;
        foreach (RunRecord run in _history)
        {
            if (run.Score > best)
                best = run.Score;
        }
        return best;
    }

    public static int GetMaxCrises()
    {
        Load();
        int max = 0;
        foreach (RunRecord run in _history)
        {
            if (run.CrisesSurvived > max)
                max = run.CrisesSurvived;
        }
        return max;
    }

    public static float GetLongestRunDurationSec()
    {
        Load();
        float max = 0f;
        foreach (RunRecord run in _history)
        {
            if (run.RunDurationSec > max)
                max = run.RunDurationSec;
        }
        return max;
    }

    public static List<RunRecord> GetTopByScore(int count = 10)
    {
        Load();
        List<RunRecord> sorted = new(_history);
        sorted.Sort((a, b) => b.Score.CompareTo(a.Score));
        return sorted.GetRange(0, System.Math.Min(count, sorted.Count));
    }

    public static List<RunRecord> GetTopByCrises(int count = 10)
    {
        Load();
        List<RunRecord> sorted = new(_history);
        sorted.Sort((a, b) =>
        {
            int cmp = b.CrisesSurvived.CompareTo(a.CrisesSurvived);
            return cmp != 0 ? cmp : b.Score.CompareTo(a.Score);
        });
        return sorted.GetRange(0, System.Math.Min(count, sorted.Count));
    }

    public static List<RunRecord> GetTopByDuration(int count = 10)
    {
        Load();
        List<RunRecord> sorted = new(_history);
        sorted.Sort((a, b) =>
        {
            int cmp = b.RunDurationSec.CompareTo(a.RunDurationSec);
            return cmp != 0 ? cmp : b.Score.CompareTo(a.Score);
        });
        return sorted.GetRange(0, System.Math.Min(count, sorted.Count));
    }

    public static void ForceReload()
    {
        _loaded = false;
        _writeBlockReason = "";
        LoadStatus = SaveFile.ReadStatus.Missing;
    }

    private static SaveFile.WriteResult Save()
    {
        if (!CanWrite)
        {
            GD.PushWarning($"[RunHistoryManager] Écriture refusée : {_writeBlockReason}");
            return SaveFile.WriteResult.Failed(_writeBlockReason);
        }

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
        SaveFile.WriteResult result = SaveFile.Write(HistoryPath, JsonSerializer.Serialize(_history, options));
        if (!result.Succeeded)
            GD.PushError($"[RunHistoryManager] Cannot save history: {result.Error}");
        return result;
    }

    /// <summary>
    /// Racine tableau d'objets versionnés. Une entrée sans version ou sous la V2 désigne un historique V1, à archiver ;
    /// une entrée plus récente que le jeu protège tout le fichier.
    /// </summary>
    private static SaveFile.Parse<ParsedHistory> ParseHistory(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return SaveFile.Parse<ParsedHistory>.Invalid($"racine {root.ValueKind}, tableau attendu");

            bool legacy = false;
            int newest = 0;
            foreach (JsonElement item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    return SaveFile.Parse<ParsedHistory>.Invalid($"entrée {item.ValueKind}, objet attendu");
                if (!item.TryGetProperty("version", out JsonElement versionElement))
                {
                    legacy = true;
                    continue;
                }
                if (versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out int version))
                    return SaveFile.Parse<ParsedHistory>.Invalid($"version invalide : {versionElement.GetRawText()}");
                legacy |= version < FirstV2Version;
                newest = Math.Max(newest, version);
            }

            if (newest > CurrentVersion)
                return SaveFile.Parse<ParsedHistory>.Future(new ParsedHistory(TryDeserialize(json), null),
                    $"version {newest} plus récente que {CurrentVersion}");
            if (legacy)
                return SaveFile.Parse<ParsedHistory>.Valid(new ParsedHistory(new List<RunRecord>(), json));

            List<RunRecord> runs = JsonSerializer.Deserialize<List<RunRecord>>(json);
            return runs == null
                ? SaveFile.Parse<ParsedHistory>.Invalid("contenu nul")
                : SaveFile.Parse<ParsedHistory>.Valid(new ParsedHistory(runs, null));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or NotSupportedException)
        {
            return SaveFile.Parse<ParsedHistory>.Invalid(ex.Message);
        }
    }

    private static List<RunRecord> TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<RunRecord>>(json) ?? new List<RunRecord>();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or NotSupportedException)
        {
            return new List<RunRecord>();
        }
    }

    private static bool ArchiveLegacyHistory(string rawJson)
    {
        SaveFile.WriteResult archived = SaveFile.Write(LegacyHistoryPath, rawJson);
        if (!archived.Succeeded)
            GD.PushError($"[RunHistoryManager] Failed to archive legacy history to {LegacyHistoryPath}: {archived.Error}");
        return archived.Succeeded;
    }
}
