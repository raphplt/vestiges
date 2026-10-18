using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Teintes d'une rareté : principale, et tons clair et sombre pour les effets en pixel art.</summary>
public readonly struct RarityColors
{
    public readonly Color Main;
    public readonly Color Light;
    public readonly Color Dark;

    public RarityColors(Color main, Color light, Color dark)
    {
        Main = main;
        Light = light;
        Dark = dark;
    }
}

/// <summary>
/// Palette de rareté unique (data/ui/rarities.json) : coffres, level-up, écran de butin, HUD et armes au sol
/// y lisent leurs couleurs et leurs noms, pour qu'une rareté ait partout la même teinte.
/// </summary>
public static class RarityPalette
{
    public const string FilePath = "res://data/ui/rarities.json";

    private static Dictionary<string, (RarityColors Colors, string NameKey)> _byId = new();
    private static bool _loaded;
    private static string _loadError;

    public static RarityColors Colors(string rarityId) => Entry(rarityId).Colors;

    public static Color Main(string rarityId) => Entry(rarityId).Colors.Main;

    /// <summary>Nom traduit de la rareté.</summary>
    public static string DisplayName(string rarityId) => TranslationServer.Translate(Entry(rarityId).NameKey);

    private static (RarityColors Colors, string NameKey) Entry(string rarityId)
    {
        if (!_loaded)
            Load();
        if (!string.IsNullOrEmpty(rarityId) && _byId.TryGetValue(rarityId, out (RarityColors, string) entry))
            return entry;
        return _byId.TryGetValue("common", out (RarityColors, string) common)
            ? common
            : (new RarityColors(Godot.Colors.White, Godot.Colors.White, Godot.Colors.Gray), "RARITY_COMMON");
    }

    /// <summary>Palette contrôlée une fois (plan 26 Q7g-4a) ; refusée, la raison, et la run ne démarre pas (GameBootstrap).</summary>
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
            GD.PushError($"[RarityPalette] {_loadError}");
    }

    /// <summary>
    /// Contrôle <paramref name="text"/> et ne publie qu'un fichier accepté ; rend l'erreur, nulle si tout est publié.
    /// La rareté commune est obligatoire : elle sert de secours à une rareté inconnue. Ton clair et sombre facultatifs.
    /// </summary>
    public static string Apply(string text, Func<string, bool> translated)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "raretés", "rarities");
            Dictionary<string, (RarityColors, string)> byId = new();
            foreach (JsonElement item in reader.List(reader.Root, "rarities", 1))
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    reader.Fail("rarities : objet attendu");
                    break;
                }
                reader.AllowOnly(item, "rareté", "id", "name_key", "color", "light", "dark");
                string id = reader.Text(item, "id");
                string nameKey = reader.Text(item, "name_key");
                Color main = Html(reader, item, "color");
                Color light = JsonConfigReader.Has(item, "light") ? Html(reader, item, "light") : main.Lightened(0.4f);
                Color dark = JsonConfigReader.Has(item, "dark") ? Html(reader, item, "dark") : main.Darkened(0.5f);
                if (reader.Error != null)
                    break;
                if (byId.ContainsKey(id))
                    reader.Fail($"rareté « {id} » déjà déclarée");
                else if (!translated(nameKey))
                    reader.Fail($"rareté « {id} » : name_key « {nameKey} » sans traduction");
                byId[id] = (new RarityColors(main, light, dark), nameKey);
            }
            if (reader.Error == null && !byId.ContainsKey("common"))
                reader.Fail("rareté « common » attendue (secours d'une rareté inconnue)");
            if (reader.Error != null)
                return reader.Error;
            _byId = byId;
            return null;
        }
        catch (JsonException exception)
        {
            return $"JSON invalide : {exception.Message}";
        }
    }

    private static Color Html(JsonConfigReader reader, JsonElement owner, string key)
    {
        string html = reader.Text(owner, key);
        if (html != null && Color.HtmlIsValid(html))
            return Color.FromHtml(html);
        reader.Fail($"{key} : couleur « {html} » invalide");
        return default;
    }
}
