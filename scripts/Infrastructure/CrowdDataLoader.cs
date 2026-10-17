using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de la foule (plan 29 F2), <c>data/scaling/crowd.json</c> : séparation entre créatures et distance où une
/// créature de mêlée s'arrête contre le joueur au lieu de viser son centre. Lus strictement une fois (plan 26 Q7e-2) ;
/// un fichier refusé n'est pas publié et arrête le chargement de la run (GameBootstrap).
/// </summary>
public static class CrowdDataLoader
{
    public const string DataPath = "res://data/scaling/crowd.json";

    public readonly record struct Settings(float SeparationRadius, float SeparationStrength, float SeparationMaxPush,
        float ContactDistance);

    private static bool _loaded;
    private static string _loadError;
    /// <summary>Réglages d'avant le fichier, gardés pour un champ absent.</summary>
    private static readonly Settings Defaults = new(26f, 0.9f, 1.5f, 22f);
    private static Settings _settings = Defaults;

    /// <summary>Distance en dessous de laquelle deux créatures se repoussent, en pixels.</summary>
    public static float SeparationRadius { get { Load(); return _settings.SeparationRadius; } }

    /// <summary>Part de sa vitesse qu'une créature consacre à s'écarter de ses voisines.</summary>
    public static float SeparationStrength { get { Load(); return _settings.SeparationStrength; } }

    /// <summary>Poussée maximale, en multiples d'une voisine collée.</summary>
    public static float SeparationMaxPush { get { Load(); return _settings.SeparationMaxPush; } }

    /// <summary>Distance au joueur où une créature de mêlée cesse d'avancer, en pixels.</summary>
    public static float ContactDistance { get { Load(); return _settings.ContactDistance; } }

    /// <summary>Le fichier lu et contrôlé ; faux, avec la raison, s'il a été refusé (la run ne démarre pas).</summary>
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
        _loadError = $"{DataPath} : lecture interrompue";
        using FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
        if (file == null)
            _loadError = $"{DataPath} : fichier illisible";
        else if (TryParse(file.GetAsText(), out Settings settings, out _loadError))
            _settings = settings;
        else
            _loadError = $"{DataPath} : {_loadError}";
        if (_loadError != null)
            GD.PushError($"[CrowdDataLoader] {_loadError}");
    }

    /// <summary>Un champ absent garde le réglage d'avant.</summary>
    public static bool TryParse(string text, out Settings settings, out string error)
    {
        settings = default;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "crowd", "separation_radius_px", "separation_strength", "separation_max_push", "contact_distance_px");
            Settings parsed = new(
                NonNegative(reader, root, "separation_radius_px", Defaults.SeparationRadius),
                NonNegative(reader, root, "separation_strength", Defaults.SeparationStrength),
                NonNegative(reader, root, "separation_max_push", Defaults.SeparationMaxPush),
                NonNegative(reader, root, "contact_distance_px", Defaults.ContactDistance));
            error = reader.Error;
            if (error != null)
                return false;
            settings = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static float NonNegative(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.NonNegative(owner, key) : fallback;
}
