using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Manifestes visuels des ramassables : chargés une fois, partagés entre les objets du pool.</summary>
public static class FieldBonusArt
{
    public sealed record Sprites(Texture2D[] Idle, Texture2D[] Disappear, Texture2D Glow, float IdleFps, float DisappearFps);
    private static readonly Dictionary<string, Sprites> Cache = new();
    private const string Folder = "res://assets/vfx/pickups/";

    public static Sprites Get(string id)
    {
        if (Cache.Count == 0)
        {
            using FileAccess file = FileAccess.Open(Folder + "pickups_manifest.json", FileAccess.ModeFlags.Read);
            Godot.Collections.Dictionary data = Json.ParseString(file.GetAsText()).AsGodotDictionary();
            Godot.Collections.Array size = data["frame_size"].AsGodotArray();
            foreach ((Variant key, Variant value) in data["pickups"].AsGodotDictionary())
            {
                Godot.Collections.Dictionary entry = value.AsGodotDictionary();
                Cache[key.AsString()] = new Sprites(
                    SpriteAtlas.Horizontal(Folder + entry["idle"].AsString(), size[0].AsInt32(), size[1].AsInt32()),
                    SpriteAtlas.Horizontal(Folder + entry["disappear"].AsString(), size[0].AsInt32(), size[1].AsInt32()),
                    GD.Load<Texture2D>(Folder + entry["glow"].AsString()),
                    (float)data["idle_fps"].AsDouble(), (float)data["disappear_fps"].AsDouble());
            }
        }
        return Cache[id];
    }
}
