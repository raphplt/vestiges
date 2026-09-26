using System.Collections.Generic;
using Godot;

namespace Vestiges.Core;

/// <summary>
/// Ombre de contact posée au sol : ellipse iso (2:1) à bord net, en deux paliers d'opacité, texture générée une fois
/// par largeur (arrondie à 4 px). Sous les décors, le joueur, les créatures et les projectiles : c'est elle qui dit
/// qu'un corps touche le sol, ou qu'un projectile vole au-dessus.
/// </summary>
public static class GroundShadow
{
    private const int WidthStep = 4;
    private static readonly Color Core = new(0.05f, 0.04f, 0.08f, 0.34f);
    private static readonly Color Rim = new(0.05f, 0.04f, 0.08f, 0.18f);
    private static readonly Dictionary<int, ImageTexture> Cache = new();

    public static int SnapWidth(float width) => Mathf.Max(WidthStep * 2, Mathf.RoundToInt(width / WidthStep) * WidthStep);

    /// <summary>Ombre centrée sur (0, <paramref name="offsetY"/>) du parent, sous les corps (z −1).</summary>
    public static Sprite2D Create(float width, float offsetY = 0f)
    {
        return new Sprite2D
        {
            Name = "GroundShadow",
            Texture = TextureFor(SnapWidth(width)),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = new Vector2(0f, offsetY),
            ZIndex = -1,
        };
    }

    public static ImageTexture TextureFor(int width)
    {
        if (Cache.TryGetValue(width, out ImageTexture cached))
            return cached;

        int height = Mathf.Max(4, width / 2);
        using Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        float rx = width * 0.5f;
        float ry = height * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x + 0.5f - rx) / rx;
                float dy = (y + 0.5f - ry) / ry;
                float d = dx * dx + dy * dy;
                if (d <= 0.45f)
                    image.SetPixel(x, y, Core);
                else if (d <= 1f)
                    image.SetPixel(x, y, Rim);
            }
        }
        ImageTexture texture = ImageTexture.CreateFromImage(image);
        Cache[width] = texture;
        return texture;
    }
}
