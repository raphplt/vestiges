using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de l'inventaire du joueur (data/progression/inventory.json, plan 35) : emplacements d'Écussons, rendement
/// des niveaux d'Écusson, sac d'objets, valeur des exemplaires et objets lâchés dans le monde. Contrôlés en entier ; un fichier refusé arrête le
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
    private static float _eliteItemChance = 0.25f;
    private static int _sovereignItems = 1;
    private static int _usageWideHitTargets = 3;
    private static float _usageFarKillDistance = 250f;
    private static float _usageContactRadius = 200f;
    private static float _usageLevelScale = 0.1f;
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

    /// <summary>Chance qu'une élite lâche un objet au sol (plan 35 I3).</summary>
    public static float EliteItemChance
    {
        get
        {
            Load();
            return _eliteItemChance;
        }
    }

    /// <summary>Objets qu'un Souverain lâche à sa mort.</summary>
    public static int SovereignItems
    {
        get
        {
            Load();
            return _sovereignItems;
        }
    }

    /// <summary>Fil d'usage (plan 35 I7) : créatures qu'une même attaque doit toucher pour un « coup large ».</summary>
    public static int UsageWideHitTargets
    {
        get
        {
            Load();
            return _usageWideHitTargets;
        }
    }

    /// <summary>Fil d'usage : distance en px au-delà de laquelle une élimination est « lointaine ».</summary>
    public static float UsageFarKillDistance
    {
        get
        {
            Load();
            return _usageFarKillDistance;
        }
    }

    /// <summary>Fil d'usage : rayon en px où une créature met le joueur « au contact ».</summary>
    public static float UsageContactRadius
    {
        get
        {
            Load();
            return _usageContactRadius;
        }
    }

    /// <summary>Fil d'usage : allongement du seuil par niveau d'Écusson au-delà du premier (0,1 : double au niveau 11).</summary>
    public static float UsageLevelScale
    {
        get
        {
            Load();
            return _usageLevelScale;
        }
    }

    /// <summary>Niveau d'Écusson au-delà duquel chaque niveau ne vaut plus qu'une part du pas.</summary>
    public static int BadgeSoftCapLevel
    {
        get
        {
            Load();
            return _badgeSoftCapLevel;
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
                "item_copy_levels", "elite_item_chance", "sovereign_items", "usage");
            int badgeSlots = reader.Integer(reader.Root, "badge_slots", 1, 8);
            int softCapLevel = reader.Integer(reader.Root, "badge_soft_cap_level", 1, 1000);
            float softCapFactor = reader.Ratio(reader.Root, "badge_soft_cap_factor");
            int bagSize = reader.Integer(reader.Root, "bag_size", 1, 50);
            int[] copyLevels = ReadCopyLevels(reader, reader.Root, "item_copy_levels");
            float eliteChance = reader.Chance(reader.Root, "elite_item_chance");
            int sovereignItems = reader.Integer(reader.Root, "sovereign_items", 0, 5);
            JsonElement usage = reader.Section("usage");
            reader.AllowOnly(usage, "usage", "wide_hit_targets", "far_kill_distance", "contact_radius", "level_scale");
            int wideHitTargets = reader.Integer(usage, "wide_hit_targets", 2, 20);
            float farKillDistance = reader.Positive(usage, "far_kill_distance");
            float contactRadius = reader.Positive(usage, "contact_radius");
            float levelScale = reader.Ratio(usage, "level_scale");
            if (reader.Error != null)
                return reader.Error;
            _badgeSlots = badgeSlots;
            _badgeSoftCapLevel = softCapLevel;
            _badgeSoftCapFactor = softCapFactor;
            _bagSize = bagSize;
            _itemCopyLevels = copyLevels;
            _eliteItemChance = eliteChance;
            _sovereignItems = sovereignItems;
            _usageWideHitTargets = wideHitTargets;
            _usageFarKillDistance = farKillDistance;
            _usageContactRadius = contactRadius;
            _usageLevelScale = levelScale;
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
