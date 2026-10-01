using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Éclats et cadres du plan 25, dans l'ordre de leur manifeste.</summary>
public static class RarityArt
{
    private const string Folder = "res://assets/ui/rarities/";
    private static string[] _ids;
    public static int Fps { get; private set; }
    public static int Margin { get; private set; }

    private static void Load()
    {
        if (_ids != null)
            return;
        using FileAccess file = FileAccess.Open(Folder + "rarities_manifest.json", FileAccess.ModeFlags.Read);
        Godot.Collections.Dictionary data = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        Godot.Collections.Array ids = data["rarities"].AsGodotArray();
        _ids = new string[ids.Count];
        for (int i = 0; i < ids.Count; i++)
            _ids[i] = ids[i].AsString();
        Fps = data["fps"].AsInt32();
        Margin = data["card_margin"].AsInt32();
    }

    public static int Rank(string id)
    {
        Load();
        for (int i = 0; i < _ids.Length; i++)
            if (_ids[i] == id)
                return i;
        return -1;
    }

    private static string Id(int rank) { Load(); return _ids[Mathf.Clamp(rank, 0, _ids.Length - 1)]; }
    public static Texture2D[] Icons(int rank, int size) => SpriteAtlas.Horizontal($"{Folder}rarity_{Id(rank)}_{size}.png", size, size);
    public static Texture2D[] Cards(int rank) => SpriteAtlas.Horizontal($"{Folder}ui_card_{Id(rank)}.png", 20, 20);
    public static Texture2D[] Jump(int rank) => SpriteAtlas.Horizontal($"{Folder}rarity_jump_{Id(rank)}.png", 32, 32);
}
