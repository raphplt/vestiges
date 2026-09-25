using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Ombre de contact des décors : ellipse iso (2:1) à bord net, en deux paliers d'opacité,
/// générée une fois par largeur (arrondie à 4 px) pour garder des pixels de taille unique.
/// </summary>
public static class PropShadow
{
    private const int WidthStep = 4;
    private static readonly Color Core = new(0.05f, 0.04f, 0.08f, 0.34f);
    private static readonly Color Rim = new(0.05f, 0.04f, 0.08f, 0.18f);
    private static readonly Dictionary<int, ImageTexture> Cache = new();

    /// <summary>Ombre sous une emprise au sol (pixels relatifs au nœud), un peu plus large qu'elle.</summary>
    public static Sprite2D Create(Vector2[] ground)
    {
        float minX = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue, sumY = 0f;
        foreach (Vector2 point in ground)
        {
            minX = Mathf.Min(minX, point.X);
            maxX = Mathf.Max(maxX, point.X);
            maxY = Mathf.Max(maxY, point.Y);
            sumY += point.Y;
        }
        int width = Mathf.Max(WidthStep * 2, Mathf.RoundToInt((maxX - minX) * 1.15f / WidthStep) * WidthStep);
        ImageTexture texture = TextureFor(width);
        // Centrée sur l'emprise, sans dépasser nettement devant le décor.
        float centerY = Mathf.Min(sumY / ground.Length, maxY - texture.GetHeight() * 0.35f);
        return new Sprite2D
        {
            Texture = texture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = new Vector2(Mathf.Round((minX + maxX) * 0.5f), Mathf.Round(centerY)),
            ZIndex = -1,
        };
    }

    private static ImageTexture TextureFor(int width)
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
