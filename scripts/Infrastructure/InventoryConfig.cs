using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de l'inventaire du joueur (data/progression/inventory.json, plan 35) : emplacements d'Écussons et rendement
/// des niveaux d'Écusson. Contrôlés en entier ; un fichier refusé arrête le chargement de la run.
/// </summary>
public static class InventoryConfig
{
    private const string ConfigPath = "res://data/progression/inventory.json";
    // Valeurs de départ du dépôt, le temps que le fichier soit lu ; un fichier refusé arrête la run (GameBootstrap).
    private static int _badgeSlots = 4;
    private static int _badgeSoftCapLevel = 30;
    private static float _badgeSoftCapFactor = 0.5f;
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
            reader.AllowOnly(reader.Root, "inventaire", "badge_slots", "badge_soft_cap_level", "badge_soft_cap_factor");
            int badgeSlots = reader.Integer(reader.Root, "badge_slots", 1, 8);
            int softCapLevel = reader.Integer(reader.Root, "badge_soft_cap_level", 1, 1000);
            float softCapFactor = reader.Ratio(reader.Root, "badge_soft_cap_factor");
            if (reader.Error != null)
                return reader.Error;
            _badgeSlots = badgeSlots;
            _badgeSoftCapLevel = softCapLevel;
            _badgeSoftCapFactor = softCapFactor;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
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
