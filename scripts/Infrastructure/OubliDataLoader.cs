using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Oubli d'une Faille : un malus sur la carte, nommé par l'effet qu'il nourrit.</summary>
public class OubliData
{
    public string Id;
    public string NameKey;
    public string DescriptionKey;
    public string Effect;
    public float Amount;
    /// <summary>Effet immédiat et définitif : un Mémorial ne peut pas le lever.</summary>
    public bool Permanent;

    /// <summary>« Les zones que tu quittes s'effacent 20 % plus vite. »</summary>
    public string Describe() =>
        string.Format(TranslationServer.Translate(DescriptionKey), Mathf.RoundToInt(Mathf.Abs(Amount) < 1f ? Amount * 100f : Amount));
}

/// <summary>
/// Oublis des Failles (data/progression/oublis.json), contrôlés en entier (plan 26 Q7c) : identifiant unique, clés
/// traduites, effet connu, valeur strictement positive.
/// </summary>
public static class OubliDataLoader
{
    private const string ConfigPath = "res://data/progression/oublis.json";

    /// <summary>
    /// Effets qu'un Oubli peut nourrir, chacun avec son lecteur : apparitions (extra_elite, affix_chance,
    /// enemy_detection), Effacement (erasure_speed), Résurgences (resurgence_interval), coffres et carte
    /// (chest_signals, chest_rarity_down), brouillard (fog_reveal), Mémoriaux (collapse_memorial).
    /// </summary>
    public static readonly IReadOnlyList<string> KnownEffects = new[]
    {
        "extra_elite", "erasure_speed", "resurgence_interval", "chest_signals", "fog_reveal", "collapse_memorial",
        "affix_chance", "chest_rarity_down", "enemy_detection",
    };

    private static readonly List<OubliData> _oublis = new();
    private static bool _loaded;
    private static string _loadError;

    public static IReadOnlyList<OubliData> All
    {
        get
        {
            Load();
            return _oublis;
        }
    }

    /// <summary>Liste lue et contrôlée ; faux, avec la raison, si elle a été refusée (le chargement de la run s'arrête).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    private static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        string error = FileAccess.FileExists(ConfigPath)
            ? Apply(FileAccess.GetFileAsString(ConfigPath), key => TranslationServer.Translate(key) != key)
            : "absent";
        if (error != null)
        {
            _loadError = $"{ConfigPath} : {error}";
            GD.PushError($"[OubliDataLoader] {_loadError}");
        }
    }

    /// <summary>Contrôle un texte d'Oublis et ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json, Func<string, bool> translated)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "Oublis", "oublis");
            List<OubliData> oublis = new();
            HashSet<string> ids = new();
            foreach (JsonElement entry in reader.List(reader.Root, "oublis", 1))
            {
                JsonConfigReader item = new(entry);
                string id = item.Text(entry, "id");
                item.AllowOnly(entry, "clés", "id", "name_key", "description_key", "effect", "amount", "permanent");
                OubliData oubli = new()
                {
                    Id = id,
                    NameKey = item.Text(entry, "name_key"),
                    DescriptionKey = item.Text(entry, "description_key"),
                    Effect = item.OneOf(entry, "effect", KnownEffects),
                    Amount = item.Positive(entry, "amount"),
                    Permanent = item.Flag(entry, "permanent"),
                };
                if (item.Error == null && !ids.Add(id))
                    item.Fail("identifiant en double");
                foreach (string key in new[] { oubli.NameKey, oubli.DescriptionKey })
                {
                    if (item.Error == null && !translated(key))
                        item.Fail($"clé « {key} » absente des traductions");
                }
                if (item.Error != null)
                    return $"Oubli {id ?? "?"} : {item.Error}";
                oublis.Add(oubli);
            }
            if (reader.Error != null)
                return reader.Error;
            _oublis.Clear();
            _oublis.AddRange(oublis);
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }
}
