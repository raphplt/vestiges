using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Comment une stat se montre au joueur (plan 17 §4.2).</summary>
public enum StatDisplay
{
    /// <summary>Valeur avant → après, une décimale (dégâts, cadence).</summary>
    Value,
    /// <summary>Compte (projectiles, perçage, rebonds, notes), en fraction s'il le faut : 1,5 projectile (plan 23).</summary>
    Count,
    /// <summary>Seulement le gain en pourcentage (portée, zone, vitesses).</summary>
    Percent,
    /// <summary>Une part (0,05) montrée en pourcentage signé (critique).</summary>
    Fraction,
    /// <summary>Une part montrée en pourcentage, sans signe : ce n'est pas un bonus mais une proportion (explosion du Pétard mouillé).</summary>
    Share,
}

/// <summary>
/// Catalogue des stats affichées (data/ui/stats.json), commun au level-up et à la pause : libellé traduit,
/// forme d'affichage, unité. Nombres à la française : virgule décimale, espace avant « % ».
/// </summary>
public static class StatCatalog
{
    private static readonly Dictionary<string, (string NameKey, StatDisplay Display, string Unit)> _stats = new();
    private static readonly Dictionary<string, string> _properties = new();
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static bool _loaded;
    private const float UnlimitedCount = 999f;

    public static string Name(string stat) => TranslationServer.Translate(Entry(stat).NameKey);

    public static StatDisplay Display(string stat) => Entry(stat).Display;

    /// <summary>Propriété de la grammaire commune que la stat monte (plan 21 §7) ; null pour la survie ou la collecte.</summary>
    public static string Property(string stat)
    {
        if (!_loaded)
            Load();
        return _properties.GetValueOrDefault(stat);
    }

    /// <summary>Valeur formatée : une décimale pour une valeur, entier pour un compte, avec l'unité éventuelle.</summary>
    public static string Format(string stat, float value)
    {
        (string _, StatDisplay display, string unit) = Entry(stat);
        // Une perforation « illimitée » (Transpercer, Lentille de phare) vaut 999 dans les données.
        string number = display == StatDisplay.Count
            ? value >= UnlimitedCount ? "∞" : CountText(value)
            : value.ToString("0.0", French);
        return string.IsNullOrEmpty(unit) ? number : $"{number} {unit}";
    }

    /// <summary>
    /// Effet d'un bonus du joueur (passif, perk) : multiplicatif en pourcentage (« +12 % »), additif selon la stat
    /// (« +20 », « +5 % », « +0,5 PV/s »).
    /// </summary>
    public static string FormatBonus(string stat, float modifier, bool multiplicative)
    {
        if (multiplicative)
            return SignedPercent((modifier - 1f) * 100f);
        (string _, StatDisplay display, string unit) = Entry(stat);
        return display switch
        {
            StatDisplay.Count => (modifier >= 0f ? "+" : "") + CountText(modifier),
            StatDisplay.Fraction => SignedPercent(modifier * 100f),
            StatDisplay.Share => Percent(modifier * 100f),
            _ => (modifier >= 0f ? "+" : "") + modifier.ToString("0.0#", French) + (string.IsNullOrEmpty(unit) ? "" : $" {unit}"),
        };
    }

    /// <summary>Compte à la française, décimales seulement si elles existent : « 2 », « 1,5 », « 0,75 ».</summary>
    public static string CountText(float value) => (Mathf.Round(value * 100f) / 100f).ToString("0.##", French);

    /// <summary>Pourcentage signé, avec une décimale seulement si elle compte : un niveau d'objet qui ajoute 0,6 % doit se voir.</summary>
    private static string SignedPercent(float percent)
    {
        // Arrondi avant le signe : un multiplicateur de 0,9999999 ne doit pas afficher « -0 % ».
        float rounded = Mathf.Round(percent * 10f) / 10f;
        return (rounded >= 0f ? "+" : "") + Percent(rounded);
    }

    private static string Percent(float percent) => (Mathf.Round(percent * 10f) / 10f).ToString("0.#", French) + " %";

    /// <summary>Écart en pourcentage entier, signé : « +18 % ».</summary>
    public static string FormatGain(float before, float after)
    {
        if (Mathf.IsZeroApprox(before))
            return "";
        int percent = Mathf.RoundToInt((after / before - 1f) * 100f);
        return $"{(percent >= 0 ? "+" : "")}{percent} %";
    }

    private static (string NameKey, StatDisplay Display, string Unit) Entry(string stat)
    {
        if (!_loaded)
            Load();
        return _stats.TryGetValue(stat, out (string, StatDisplay, string) entry) ? entry : (stat, StatDisplay.Percent, "");
    }

    private static void Load()
    {
        _loaded = true;
        using FileAccess file = FileAccess.Open("res://data/ui/stats.json", FileAccess.ModeFlags.Read);
        Json json = new();
        if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError("[StatCatalog] Cannot read data/ui/stats.json");
            return;
        }

        foreach (Variant item in json.Data.AsGodotDictionary()["stats"].AsGodotArray())
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            StatDisplay display = dict["display"].AsString() switch
            {
                "value" => StatDisplay.Value,
                "count" => StatDisplay.Count,
                "fraction" => StatDisplay.Fraction,
                "share" => StatDisplay.Share,
                _ => StatDisplay.Percent,
            };
            string id = dict["id"].AsString();
            _stats[id] = (dict["name_key"].AsString(), display, dict.ContainsKey("unit") ? dict["unit"].AsString() : "");
            if (dict.ContainsKey("property"))
                _properties[id] = dict["property"].AsString();
        }
    }
}
