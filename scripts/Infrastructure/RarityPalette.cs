using System.Collections.Generic;
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
    private static readonly Dictionary<string, (RarityColors Colors, string NameKey)> _byId = new();
    private static bool _loaded;

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

    private static void Load()
    {
        _loaded = true;
        using FileAccess file = FileAccess.Open("res://data/ui/rarities.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[RarityPalette] Cannot open data/ui/rarities.json");
            return;
        }

        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[RarityPalette] Parse error: {json.GetErrorMessage()}");
            return;
        }

        foreach (Variant item in json.Data.AsGodotDictionary()["rarities"].AsGodotArray())
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            Color main = Color.FromHtml(dict["color"].AsString());
            RarityColors colors = new(
                main,
                dict.ContainsKey("light") ? Color.FromHtml(dict["light"].AsString()) : main.Lightened(0.4f),
                dict.ContainsKey("dark") ? Color.FromHtml(dict["dark"].AsString()) : main.Darkened(0.5f));
            _byId[dict["id"].AsString()] = (colors, dict["name_key"].AsString());
        }
    }
}
