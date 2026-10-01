using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Catalogue visuel de Collection ; lire une image ne crée jamais une règle ni une offre en run.</summary>
public static class CollectionArtDataLoader
{
    public readonly record struct Entry(string Id, string Name, string Icon, bool World);
    private static IReadOnlyList<Entry> _items;
    private static IReadOnlyList<Entry> _perks;

    public static IReadOnlyList<Entry> Items => _items ??= Read("res://assets/items/icons/", "items_manifest.json");
    public static IReadOnlyList<Entry> Perks => _perks ??= Read("res://assets/perks/icons/", "reminiscences_manifest.json");

    private static IReadOnlyList<Entry> Read(string folder, string manifest)
    {
        using FileAccess file = FileAccess.Open(folder + manifest, FileAccess.ModeFlags.Read);
        Godot.Collections.Array entries = Json.ParseString(file.GetAsText()).AsGodotDictionary()["icons"].AsGodotArray();
        List<Entry> result = new();
        foreach (Variant item in entries)
        {
            Godot.Collections.Dictionary data = item.AsGodotDictionary();
            result.Add(new Entry(data["id"].AsString(), data["name"].AsString(), folder + data["icon"].AsString(),
                data.ContainsKey("world") && data["world"].AsBool()));
        }
        return result;
    }
}
