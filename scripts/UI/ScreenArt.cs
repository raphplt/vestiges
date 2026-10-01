using Godot;

namespace Vestiges.UI;

/// <summary>Textures natives des écrans et sols de chargement, chargées une fois et partagées.</summary>
public static class ScreenArt
{
    public const string Folder = "res://assets/ui/screens/plan25/";
    private static Texture2D[] _backgrounds;
    private static Texture2D[] _grounds;
    public static float RotationDegreesPerSecond { get; private set; }

    private static void Load()
    {
        if (_backgrounds != null)
            return;
        using FileAccess file = FileAccess.Open(Folder + "screens_manifest.json", FileAccess.ModeFlags.Read);
        Godot.Collections.Dictionary data = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        Godot.Collections.Dictionary background = data["background"].AsGodotDictionary();
        RotationDegreesPerSecond = (float)background["rotation_degrees_per_second"].AsDouble();
        Godot.Collections.Array tints = background["tints"].AsGodotArray();
        _backgrounds = new Texture2D[tints.Count];
        for (int i = 0; i < tints.Count; i++)
            _backgrounds[i] = GD.Load<Texture2D>(Folder + $"choice_dust_{tints[i].AsString()}.png");
        Godot.Collections.Array biomes = data["loading_ground"].AsGodotDictionary()["biomes"].AsGodotArray();
        _grounds = new Texture2D[biomes.Count];
        for (int i = 0; i < biomes.Count; i++)
            _grounds[i] = GD.Load<Texture2D>(Folder + $"loading_ground_{biomes[i].AsString()}.png");
    }

    public static Texture2D Background(int tint) { Load(); return _backgrounds[tint]; }
    public static Texture2D[] Grounds { get { Load(); return _grounds; } }
}
