using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

public class LootEntry
{
    public string Type;
    public string Item;
    public float Weight;
    public int MinAmount;
    public int MaxAmount;
    /// <summary>Essence donnée si l'entrée ne peut pas servir (niveaux d'objet sans objet à monter).</summary>
    public int FallbackEssence;
}

public class LootTableData
{
    public string Id;
    public List<LootEntry> Entries = new();
}

/// <summary>
/// Tables de butin des coffres et des lieux (data/loot_tables/, un fichier par table, « _ » en tête pour un modèle non
/// chargé), lues strictement une fois (plan 26 Q7g-1). Chaque sorte de butin a son article attendu (celui que lit
/// LootRewards) ; un catalogue refusé n'est pas publié et arrête le chargement de la run (GameBootstrap).
/// </summary>
public static class LootTableLoader
{
    public const string DirectoryPath = "res://data/loot_tables";
    private const int MaxAmount = 10000;

    /// <summary>Sorte de butin → article attendu ; le Souvenir admet aussi un identifiant du catalogue.</summary>
    private static readonly Dictionary<string, string> ItemByType = new()
    {
        ["essence"] = "essence", ["xp"] = "xp", ["object_level"] = "random_owned_object", ["object"] = "random_item",
        ["souvenir"] = "random_souvenir",
    };

    private static Dictionary<string, LootTableData> _cache = new();
    private static bool _loaded;
    private static string _loadError;

    /// <summary>Les tables lues et contrôlées ; faux, avec la raison, si le catalogue a été refusé (la run ne démarre pas).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        _loadError = $"{DirectoryPath} : lecture interrompue";
        List<(string Name, string Text)> files = new();
        using DirAccess dir = DirAccess.Open(DirectoryPath);
        if (dir == null)
            _loadError = $"{DirectoryPath} : dossier illisible";
        else
        {
            foreach (string fileName in dir.GetFiles())
            {
                if (!fileName.EndsWith(".json", StringComparison.Ordinal) || fileName.StartsWith('_'))
                    continue;
                using FileAccess file = FileAccess.Open($"{DirectoryPath}/{fileName}", FileAccess.ModeFlags.Read);
                files.Add((fileName, file?.GetAsText()));
            }
            _loadError = Apply(files, id => SouvenirDataLoader.Get(id) != null);
        }
        if (_loadError != null)
        {
            GD.PushError($"[LootTableLoader] {_loadError}");
            return;
        }
        GD.Print($"[LootTableLoader] Loaded {_cache.Count} loot tables");
    }

    /// <summary>
    /// Contrôle les fichiers (nom, texte) et ne publie que s'ils sont tous acceptés ; rend l'erreur, nulle si tout est
    /// publié. <paramref name="souvenirExists"/> reconnaît un Souvenir nommé.
    /// </summary>
    public static string Apply(IReadOnlyList<(string Name, string Text)> files, Func<string, bool> souvenirExists)
    {
        Dictionary<string, LootTableData> tables = new();
        foreach ((string name, string text) in files)
        {
            string where = $"{DirectoryPath}/{name}";
            if (text == null)
                return $"{where} : fichier illisible";
            try
            {
                using JsonDocument document = JsonDocument.Parse(text);
                JsonConfigReader reader = new(document.RootElement);
                LootTableData table = Parse(reader, reader.Root, souvenirExists);
                // L'id égal au nom du fichier le rend unique dans le dossier.
                if (reader.Error == null && table.Id + ".json" != name)
                    reader.Fail($"id « {table.Id} » différent du nom du fichier");
                if (reader.Error != null)
                    return $"{where} : {reader.Error}";
                tables[table.Id] = table;
            }
            catch (JsonException exception)
            {
                return $"{where} : JSON invalide : {exception.Message}";
            }
        }
        if (tables.Count == 0)
            return $"{DirectoryPath} : aucune table";
        _cache = tables;
        return null;
    }

    /// <summary>Une table de butin de ce nom existe (référence d'un coffre, plan 26 Q7c).</summary>
    public static bool Exists(string id)
    {
        Load();
        return id != null && _cache.ContainsKey(id);
    }

    public static LootTableData Get(string id)
    {
        Load();
        if (id != null && _cache.TryGetValue(id, out LootTableData data))
            return data;

        GD.PushWarning($"[LootTableLoader] Unknown loot table: {id}");
        return null;
    }

    private static LootTableData Parse(JsonConfigReader reader, JsonElement root, Func<string, bool> souvenirExists)
    {
        reader.AllowOnly(root, "table", "id", "entries");
        LootTableData table = new() { Id = reader.Text(root, "id") };
        int index = 0;
        foreach (JsonElement item in reader.List(root, "entries", 1))
        {
            string where = $"entries[{index++}]";
            if (item.ValueKind != JsonValueKind.Object)
            {
                reader.Fail($"{where} : objet attendu");
                break;
            }
            reader.AllowOnly(item, where, "type", "item", "weight", "min_amount", "max_amount", "fallback_essence");
            LootEntry entry = new()
            {
                Type = reader.OneOf(item, "type", ItemByType.Keys),
                Item = reader.Text(item, "item"),
                Weight = reader.Positive(item, "weight"),
                MinAmount = reader.Integer(item, "min_amount", 0, MaxAmount),
                MaxAmount = reader.Integer(item, "max_amount", 0, MaxAmount),
            };
            if (reader.Error != null)
                break;
            bool namedSouvenir = entry.Type == "souvenir" && souvenirExists(entry.Item);
            if (entry.Item != ItemByType[entry.Type] && !namedSouvenir)
                reader.Fail($"{where} : article « {entry.Item} » inattendu pour {entry.Type} (« {ItemByType[entry.Type]} »)");
            else if (entry.MinAmount > entry.MaxAmount)
                reader.Fail($"{where} : min_amount ≤ max_amount attendu ({entry.MinAmount} > {entry.MaxAmount})");
            else if (JsonConfigReader.Has(item, "fallback_essence"))
            {
                // Seuls les niveaux d'objet retombent en Essence quand aucun objet ne peut monter.
                if (entry.Type != "object_level")
                    reader.Fail($"{where} : fallback_essence réservé à object_level");
                else
                    entry.FallbackEssence = reader.Integer(item, "fallback_essence", 0, MaxAmount);
            }
            table.Entries.Add(entry);
        }
        return table;
    }
}
