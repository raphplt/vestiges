using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Comment une stat se montre au joueur (plan 17 §4.2).</summary>
public enum StatDisplay
{
    /// <summary>Valeur avant → après, une décimale (dégâts, cadence).</summary>
    Value,
    /// <summary>Entier (projectiles, perçage, rebonds, notes).</summary>
    Count,
    /// <summary>Seulement le gain en pourcentage (portée, zone, vitesses).</summary>
    Percent,
    /// <summary>Une part (0,05) montrée en pourcentage (critique).</summary>
    Fraction,
}

/// <summary>
/// Catalogue des stats affichées (data/ui/stats.json), commun au level-up et à la pause : libellé traduit,
/// forme d'affichage, unité. Nombres à la française : virgule décimale, espace avant « % ».
/// </summary>
public static class StatCatalog
{
    private static readonly Dictionary<string, (string NameKey, StatDisplay Display, string Unit)> _stats = new();
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static bool _loaded;

    public static string Name(string stat) => TranslationServer.Translate(Entry(stat).NameKey);

    public static StatDisplay Display(string stat) => Entry(stat).Display;

    /// <summary>Valeur formatée : une décimale pour une valeur, entier pour un compte, avec l'unité éventuelle.</summary>
    public static string Format(string stat, float value)
    {
        (string _, StatDisplay display, string unit) = Entry(stat);
        string number = display == StatDisplay.Count
            ? Mathf.RoundToInt(value).ToString(French)
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
            StatDisplay.Count => Signed(Mathf.RoundToInt(modifier)),
            StatDisplay.Fraction => SignedPercent(modifier * 100f),
            _ => (modifier >= 0f ? "+" : "") + modifier.ToString("0.0#", French) + (string.IsNullOrEmpty(unit) ? "" : $" {unit}"),
        };
    }

    private static string Signed(int value) => (value >= 0 ? "+" : "") + value.ToString(French);

    /// <summary>Pourcentage signé, avec une décimale seulement si elle compte : un niveau d'objet qui ajoute 0,6 % doit se voir.</summary>
    private static string SignedPercent(float percent)
    {
        // Arrondi avant le signe : un multiplicateur de 0,9999999 ne doit pas afficher « -0 % ».
        float rounded = Mathf.Round(percent * 10f) / 10f;
        return (rounded >= 0f ? "+" : "") + rounded.ToString("0.#", French) + " %";
    }

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
                _ => StatDisplay.Percent,
            };
            _stats[dict["id"].AsString()] = (dict["name_key"].AsString(), display, dict.ContainsKey("unit") ? dict["unit"].AsString() : "");
        }
    }
}
