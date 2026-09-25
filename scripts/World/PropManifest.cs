using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Manifeste des décors procéduraux (props_manifest.json, écrit par tools/generate_props.py) :
/// point au sol dans le sprite et emprise projetée exacte. Absent pour les anciens décors dessinés,
/// qui gardent l'emprise déduite des pixels (PropFootprint).
/// </summary>
public static class PropManifest
{
    public const string FileName = "props_manifest.json";

    public readonly struct Entry
    {
        /// <summary>Point au sol, en pixels depuis le coin haut-gauche du sprite.</summary>
        public readonly Vector2 Pivot;
        /// <summary>Emprise au sol en pixels relatifs au pivot (vide si le décor n'en a pas).</summary>
        public readonly Vector2[] Footprint;

        public Entry(Vector2 pivot, Vector2[] footprint)
        {
            Pivot = pivot;
            Footprint = footprint;
        }
    }

    private static readonly Dictionary<string, Dictionary<string, Entry>> Folders = new();

    /// <summary>Noms (sans extension) des décors d'un dossier, par exemple res://assets/props/urban_ruins.</summary>
    public static IEnumerable<string> Stems(string folder)
    {
        if (!Folders.TryGetValue(folder, out Dictionary<string, Entry> entries))
            Folders[folder] = entries = Load(folder.PathJoin(FileName));
        return entries.Keys;
    }

    public static bool TryGet(Texture2D texture, out Entry entry)
    {
        entry = default;
        string path = texture.ResourcePath;
        if (string.IsNullOrEmpty(path))
            return false;
        string folder = path.GetBaseDir();
        if (!Folders.TryGetValue(folder, out Dictionary<string, Entry> entries))
            Folders[folder] = entries = Load(folder.PathJoin(FileName));
        return entries.TryGetValue(path.GetFile().GetBaseName(), out entry);
    }

    private static Dictionary<string, Entry> Load(string manifestPath)
    {
        Dictionary<string, Entry> entries = new();
        if (!FileAccess.FileExists(manifestPath))
            return entries;

        using FileAccess file = FileAccess.Open(manifestPath, FileAccess.ModeFlags.Read);
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[PropManifest] {manifestPath} : {json.GetErrorMessage()}");
            return entries;
        }

        foreach ((Variant key, Variant value) in json.Data.AsGodotDictionary())
        {
            Godot.Collections.Dictionary dict = value.AsGodotDictionary();
            Godot.Collections.Array pivot = dict["pivot"].AsGodotArray();
            Vector2[] footprint = System.Array.Empty<Vector2>();
            if (dict.ContainsKey("footprint"))
            {
                Godot.Collections.Array points = dict["footprint"].AsGodotArray();
                footprint = new Vector2[points.Count];
                for (int i = 0; i < points.Count; i++)
                {
                    Godot.Collections.Array point = points[i].AsGodotArray();
                    footprint[i] = new Vector2((float)point[0].AsDouble(), (float)point[1].AsDouble());
                }
            }
            entries[key.AsString()] = new Entry(new Vector2((float)pivot[0].AsDouble(), (float)pivot[1].AsDouble()), footprint);
        }
        GD.Print($"[PropManifest] {entries.Count} décors dans {manifestPath}");
        return entries;
    }
}
