using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
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
    public const string FilePath = "res://data/ui/stats.json";

    private static readonly Dictionary<string, StatDisplay> Displays = new()
    {
        ["value"] = StatDisplay.Value, ["count"] = StatDisplay.Count, ["percent"] = StatDisplay.Percent,
        ["fraction"] = StatDisplay.Fraction, ["share"] = StatDisplay.Share,
    };

    private static Dictionary<string, (string NameKey, StatDisplay Display, string Unit)> _stats = new();
    private static Dictionary<string, string> _properties = new();
    private static string _loadError;
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static bool _loaded;
    private const float UnlimitedCount = 999f;

    public static string Name(string stat) => TranslationServer.Translate(Entry(stat).NameKey);

    /// <summary>Libellé d'une stat d'arme : le nombre d'une arme de mêlée se lit en frappes, celui d'un cône en ondes.</summary>
    public static string Name(string stat, WeaponData weapon) =>
        stat == "projectile_count" && !string.IsNullOrEmpty(weapon?.CountNameKey)
            ? TranslationServer.Translate(weapon.CountNameKey)
            : Name(stat);

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
        if (stat == "regen_rate") value *= 60f;
        // Une perforation « illimitée » (Transpercer, Lentille de phare) vaut 999 dans les données.
        string number = display == StatDisplay.Count
            ? value >= UnlimitedCount ? "∞" : CountText(value)
            : value.ToString("0.0", French);
        return string.IsNullOrEmpty(unit) ? number : $"{number} {unit}";
    }

    /// <summary>
    /// Effet d'un bonus du joueur (passif, perk) : multiplicatif en pourcentage (« +12 % »), additif selon la stat
    /// (« +20 », « +5 % », « +9 PV/min »).
    /// </summary>
    public static string FormatBonus(string stat, float modifier, bool multiplicative)
    {
        if (stat == "regen_rate" && !multiplicative)
            return (modifier >= 0f ? "+" : "") + CountText(modifier * 60f) + " PV/min";
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

    /// <summary>Catalogue contrôlé une fois (plan 26 Q7g-4b) ; refusé, la raison, et la run ne démarre pas (GameBootstrap).</summary>
    public static bool TryLoad(out string error)
    {
        if (!_loaded)
            Load();
        error = _loadError;
        return error == null;
    }

    private static void Load()
    {
        _loaded = true;
        _loadError = $"{FilePath} : lecture interrompue";
        using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
        _loadError = file == null ? $"{FilePath} : fichier illisible"
            : Apply(file.GetAsText(), key => TranslationServer.Translate(key) != key) is string error ? $"{FilePath} : {error}" : null;
        if (_loadError != null)
            GD.PushError($"[StatCatalog] {_loadError}");
    }

    /// <summary>
    /// Contrôle <paramref name="text"/> et ne publie qu'un fichier accepté ; rend l'erreur, nulle si tout est publié.
    /// Libellé traduit, forme d'affichage connue, propriété de la grammaire commune traduite (PROPERTY_…).
    /// </summary>
    public static string Apply(string text, Func<string, bool> translated)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "stats", "stats");
            Dictionary<string, (string, StatDisplay, string)> stats = new();
            Dictionary<string, string> properties = new();
            foreach (JsonElement item in reader.List(reader.Root, "stats", 1))
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    reader.Fail("stats : objet attendu");
                    break;
                }
                reader.AllowOnly(item, "stat", "id", "name_key", "display", "unit", "property");
                string id = reader.Text(item, "id");
                string nameKey = reader.Text(item, "name_key");
                string display = reader.OneOf(item, "display", Displays.Keys);
                string unit = JsonConfigReader.Has(item, "unit") ? reader.Text(item, "unit") : "";
                string property = JsonConfigReader.Has(item, "property") ? reader.Text(item, "property") : null;
                if (reader.Error != null)
                    break;
                if (stats.ContainsKey(id))
                    reader.Fail($"stat « {id} » déjà déclarée");
                else if (!translated(nameKey))
                    reader.Fail($"stat « {id} » : name_key « {nameKey} » sans traduction");
                else if (property != null && !translated("PROPERTY_" + property.ToUpperInvariant()))
                    reader.Fail($"stat « {id} » : propriété « {property} » inconnue (sans PROPERTY_{property.ToUpperInvariant()})");
                stats[id] = (nameKey, Displays[display], unit);
                if (property != null)
                    properties[id] = property;
            }
            if (reader.Error != null)
                return reader.Error;
            _stats = stats;
            _properties = properties;
            return null;
        }
        catch (JsonException exception)
        {
            return $"JSON invalide : {exception.Message}";
        }
    }
}
