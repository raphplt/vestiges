using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de l'inventaire du joueur (data/progression/inventory.json, plan 35) : emplacements d'Écussons, rendement
/// des niveaux d'Écusson, sac d'objets et valeur des exemplaires. Contrôlés en entier ; un fichier refusé arrête le
/// chargement de la run.
/// </summary>
public static class InventoryConfig
{
    private const string ConfigPath = "res://data/progression/inventory.json";
    // Valeurs de départ du dépôt, le temps que le fichier soit lu ; un fichier refusé arrête la run (GameBootstrap).
    private static int _badgeSlots = 4;
    private static int _badgeSoftCapLevel = 30;
    private static float _badgeSoftCapFactor = 0.5f;
    private static int _bagSize = 10;
    private static int[] _itemCopyLevels = { 10, 15, 19, 22, 24, 25 };
    private static bool _loaded;
    private static string _loadError;

    /// <summary>Écussons portés à la fois.</summary>
    public static int BadgeSlots
    {
        get
        {
            Load();
            return _badgeSlots;
        }
    }

    /// <summary>Places du sac d'objets avant le Campement : une par exemplaire porté.</summary>
    public static int BagSize
    {
        get
        {
            Load();
            return _bagSize;
        }
    }

    /// <summary>Niveau d'effet d'un objet selon ses exemplaires : [0] pour un, [1] pour deux…</summary>
    public static IReadOnlyList<int> ItemCopyLevels
    {
        get
        {
            Load();
            return _itemCopyLevels;
        }
    }

    /// <summary>Part du pas que vaut le niveau <paramref name="level"/> d'un Écusson : 1, puis le facteur au-delà du seuil.</summary>
    public static float BadgeStepFactor(int level)
    {
        Load();
        return level > _badgeSoftCapLevel ? _badgeSoftCapFactor : 1f;
    }

    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    /// <summary>Contrôle un texte de réglages ; ne publie les valeurs que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "inventaire", "badge_slots", "badge_soft_cap_level", "badge_soft_cap_factor", "bag_size",
                "item_copy_levels");
            int badgeSlots = reader.Integer(reader.Root, "badge_slots", 1, 8);
            int softCapLevel = reader.Integer(reader.Root, "badge_soft_cap_level", 1, 1000);
            float softCapFactor = reader.Ratio(reader.Root, "badge_soft_cap_factor");
            int bagSize = reader.Integer(reader.Root, "bag_size", 1, 50);
            int[] copyLevels = ReadCopyLevels(reader, reader.Root, "item_copy_levels");
            if (reader.Error != null)
                return reader.Error;
            _badgeSlots = badgeSlots;
            _badgeSoftCapLevel = softCapLevel;
            _badgeSoftCapFactor = softCapFactor;
            _bagSize = bagSize;
            _itemCopyLevels = copyLevels;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }

    /// <summary>Niveaux d'exemplaires : entiers strictement croissants, de 1 à 100 ; lus aussi pour un objet qui a les siens.</summary>
    public static int[] ReadCopyLevels(JsonConfigReader reader, JsonElement owner, string key)
    {
        List<JsonElement> values = reader.List(owner, key, 1);
        int[] levels = new int[values.Count];
        for (int i = 0; i < values.Count && reader.Error == null; i++)
        {
            if (values[i].ValueKind != JsonValueKind.Number || !values[i].TryGetInt32(out int level) || level < 1 || level > 100)
                reader.Fail($"{key}[{i}] : entier de 1 à 100 attendu");
            else if (i > 0 && level <= levels[i - 1])
                reader.Fail($"{key}[{i}] : {level} pas plus haut que l'exemplaire précédent ({levels[i - 1]})");
            else
                levels[i] = level;
        }
        return levels;
    }

    private static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        string error = FileAccess.FileExists(ConfigPath) ? Apply(FileAccess.GetFileAsString(ConfigPath)) : "absent";
        if (error != null)
        {
            _loadError = $"{ConfigPath} : {error}";
            GD.PushError($"[InventoryConfig] {_loadError}");
        }
    }
}
