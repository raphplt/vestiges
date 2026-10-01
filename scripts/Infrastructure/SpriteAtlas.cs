using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Découpe horizontale mise en cache : aucune création de texture pendant une animation.</summary>
public static class SpriteAtlas
{
    private static readonly Dictionary<(string, int, int), Texture2D[]> Cache = new();

    public static Texture2D[] Horizontal(string path, int width, int height)
    {
        if (Cache.TryGetValue((path, width, height), out Texture2D[] cached))
            return cached;
        Texture2D sheet = GD.Load<Texture2D>(path);
        int count = sheet.GetWidth() / width;
        Texture2D[] frames = new Texture2D[count];
        for (int i = 0; i < count; i++)
            frames[i] = new AtlasTexture { Atlas = sheet, Region = new Rect2(i * width, 0, width, height), FilterClip = true };
        Cache.Add((path, width, height), frames);
        return frames;
    }
}
