using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Niveaux de difficulté choisis avant la run (data/scaling/difficulty.json, plan 50 T3), dans l'ordre du plus simple
/// au plus dur. Contrôlé en entier : un fichier invalide arrête le chargement de la run (GameBootstrap).
/// </summary>
public static class DifficultyDataLoader
{
    private const string ConfigPath = "res://data/scaling/difficulty.json";
    private static readonly DifficultyLevel Neutral = new() { Id = "normal", NameKey = "DIFFICULTY_NORMAL", DescKey = "DIFFICULTY_NORMAL_DESC", Open = true };
    private static List<DifficultyLevel> _levels = new() { Neutral };
    private static string _defaultId = Neutral.Id;
    private static bool _loaded;

    public static IReadOnlyList<DifficultyLevel> All
    {
        get
        {
            Load();
            return _levels;
        }
    }

    public static string DefaultId
    {
        get
        {
            Load();
            return _defaultId;
        }
    }

    /// <summary>Le niveau de cet identifiant ; un identifiant inconnu ou vide retombe sur le niveau par défaut.</summary>
    public static DifficultyLevel Get(string id)
    {
        Load();
        int index = IndexOf(id);
        return _levels[index >= 0 ? index : IndexOf(_defaultId)];
    }

    /// <summary>Rang dans l'ordre des niveaux, ou -1.</summary>
    public static int IndexOf(string id)
    {
        Load();
        for (int i = 0; i < _levels.Count; i++)
            if (_levels[i].Id == id)
                return i;
        return -1;
    }

    private static void Load()
    {
        if (_loaded)
            return;
        if (!TryLoad(out string error))
            GD.PushError($"[DifficultyDataLoader] {error}");
    }

    public static bool TryLoad(out string error)
    {
        _loaded = true;
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            error = $"{ConfigPath} absent";
            return false;
        }
        error = Apply(file.GetAsText());
        if (error != null)
            error = $"{ConfigPath} : {error}";
        return error == null;
    }

    /// <summary>Contrôle un texte de réglages ; ne publie les niveaux que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "difficulté", "default", "levels");
            string defaultId = reader.Text(reader.Root, "default");
            List<DifficultyLevel> levels = new();
            HashSet<string> ids = new();
            foreach (JsonElement entry in reader.List(reader.Root, "levels", 1))
            {
                reader.AllowOnly(entry, "levels", "id", "name_key", "desc_key", "open", "enemy_count", "enemy_hp", "enemy_damage",
                    "elite_frequency", "erasure_speed", "start_peril", "vestiges");
                DifficultyLevel level = new()
                {
                    Id = reader.Text(entry, "id"),
                    NameKey = reader.Text(entry, "name_key"),
                    DescKey = reader.Text(entry, "desc_key"),
                    Open = reader.Flag(entry, "open"),
                    EnemyCount = reader.Positive(entry, "enemy_count"),
                    EnemyHp = reader.Positive(entry, "enemy_hp"),
                    EnemyDamage = reader.Positive(entry, "enemy_damage"),
                    EliteFrequency = reader.Positive(entry, "elite_frequency"),
                    ErasureSpeed = reader.Positive(entry, "erasure_speed"),
                    StartPeril = reader.Integer(entry, "start_peril", 0, 100),
                    Vestiges = reader.Positive(entry, "vestiges"),
                };
                if (reader.Error == null && !ids.Add(level.Id))
                    reader.Fail($"niveau « {level.Id} » en double");
                levels.Add(level);
            }
            if (reader.Error == null && !ids.Contains(defaultId))
                reader.Fail($"default : niveau « {defaultId} » inconnu");
            if (reader.Error == null && !levels[0].Open)
                reader.Fail($"le premier niveau ({levels[0].Id}) doit être ouvert d'office");
            if (reader.Error != null)
                return reader.Error;
            _levels = levels;
            _defaultId = defaultId;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }
}
