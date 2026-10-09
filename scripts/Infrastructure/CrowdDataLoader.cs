using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de la foule (plan 29 F2), <c>data/scaling/crowd.json</c> : séparation entre créatures et distance où une
/// créature de mêlée s'arrête contre le joueur au lieu de viser son centre.
/// </summary>
public static class CrowdDataLoader
{
    private const string Path = "res://data/scaling/crowd.json";
    private static bool _loaded;
    private static float _separationRadius = 26f;
    private static float _separationStrength = 0.9f;
    private static float _separationMaxPush = 1.5f;
    private static float _contactDistance = 22f;

    /// <summary>Distance en dessous de laquelle deux créatures se repoussent, en pixels.</summary>
    public static float SeparationRadius { get { Load(); return _separationRadius; } }

    /// <summary>Part de sa vitesse qu'une créature consacre à s'écarter de ses voisines.</summary>
    public static float SeparationStrength { get { Load(); return _separationStrength; } }

    /// <summary>Poussée maximale, en multiples d'une voisine collée.</summary>
    public static float SeparationMaxPush { get { Load(); return _separationMaxPush; } }

    /// <summary>Distance au joueur où une créature de mêlée cesse d'avancer, en pixels.</summary>
    public static float ContactDistance { get { Load(); return _contactDistance; } }

    private static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        using FileAccess file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[CrowdDataLoader] {Path} introuvable : réglages par défaut");
            return;
        }
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[CrowdDataLoader] {Path} invalide : {json.GetErrorMessage()}");
            return;
        }
        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        _separationRadius = Read(dict, "separation_radius_px", _separationRadius);
        _separationStrength = Read(dict, "separation_strength", _separationStrength);
        _separationMaxPush = Read(dict, "separation_max_push", _separationMaxPush);
        _contactDistance = Read(dict, "contact_distance_px", _contactDistance);
    }

    private static float Read(Godot.Collections.Dictionary dict, string key, float fallback) =>
        dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;
}
