using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Nature du gain d'un coffre, indépendante de sa rareté. Textures partagées entre les ouvertures.</summary>
public static class LootIconResolver
{
    private static readonly Dictionary<string, Texture2D> Icons = new();
    private static bool _loaded;

    public static Texture2D Get(string type, string itemId = "")
    {
        if (type == "object_level")
        {
            string path = PassiveSouvenirDataLoader.Get(itemId)?.Icon;
            return string.IsNullOrEmpty(path) ? null : GD.Load<Texture2D>(path);
        }
        if (!_loaded)
        {
            using FileAccess file = FileAccess.Open("res://data/ui/loot_icons.json", FileAccess.ModeFlags.Read);
            Godot.Collections.Dictionary data = Json.ParseString(file.GetAsText()).AsGodotDictionary();
            foreach (KeyValuePair<Variant, Variant> entry in data)
                Icons[entry.Key.AsString()] = GD.Load<Texture2D>(entry.Value.AsString());
            _loaded = true;
        }
        string key = type == "stat" ? $"stat:{itemId}" : type;
        if (Icons.TryGetValue(key, out Texture2D texture))
            return texture;
        GD.PushWarning($"[LootIconResolver] Icône absente : {key}");
        return null;
    }
}
