using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Crédits de l'accueil (data/ui/credits.json) : contributeurs, ressources tierces et textes de licence à livrer avec
/// le jeu. Les notices du moteur ne sont pas recopiées ici : Godot les fournit lui-même.
/// </summary>
public static class CreditsDataLoader
{
    private const string DataPath = "res://data/ui/credits.json";

    public readonly record struct Entry(string Name, string Detail);
    public readonly record struct Section(string TitleKey, string NoteKey, IReadOnlyList<Entry> Entries);
    public readonly record struct License(string Name, string Text);
    public sealed record Credits(string CreatedBy, IReadOnlyList<Section> Sections, IReadOnlyList<License> Licenses);

    private static Credits _credits;

    public static Credits Get() => _credits ??= Read();

    private static Credits Read()
    {
        using FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[CreditsDataLoader] Lecture impossible de {DataPath} : {FileAccess.GetOpenError()}");
            return new Credits("", new List<Section>(), new List<License>());
        }

        Godot.Collections.Dictionary root = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        List<Section> sections = new();
        foreach (Variant item in root["sections"].AsGodotArray())
        {
            Godot.Collections.Dictionary data = item.AsGodotDictionary();
            List<Entry> entries = new();
            foreach (Variant entry in data["entries"].AsGodotArray())
            {
                Godot.Collections.Dictionary fields = entry.AsGodotDictionary();
                entries.Add(new Entry(fields["name"].AsString(), fields["detail"].AsString()));
            }
            string note = data.ContainsKey("note") ? data["note"].AsString() : "";
            sections.Add(new Section(data["title"].AsString(), note, entries));
        }

        List<License> licenses = new();
        foreach (Variant item in root["licenses"].AsGodotArray())
        {
            Godot.Collections.Dictionary data = item.AsGodotDictionary();
            licenses.Add(new License(data["name"].AsString(), data["text"].AsString()));
        }
        return new Credits(root["created_by"].AsString(), sections, licenses);
    }
}
