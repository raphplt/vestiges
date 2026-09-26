using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Effet de stat décrit en données : bénédiction d'un Mémorial.</summary>
public class StatEffectData
{
    public string Id;
    public string NameKey;
    public string Stat;
    public string ModifierType;
    public float Amount;
}

/// <summary>Lecture d'une liste d'effets de stat (bénédictions).</summary>
internal static class StatEffectReader
{
    public static void Read(string path, string key, List<StatEffectData> into)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        Json json = new();
        if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[StatEffectReader] Cannot read {path}");
            return;
        }

        foreach (Variant item in json.Data.AsGodotDictionary()[key].AsGodotArray())
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            into.Add(new StatEffectData
            {
                Id = dict["id"].AsString(),
                NameKey = dict["name_key"].AsString(),
                Stat = dict["stat"].AsString(),
                ModifierType = dict["modifier_type"].AsString(),
                Amount = (float)dict["amount"].AsDouble(),
            });
        }
    }
}
