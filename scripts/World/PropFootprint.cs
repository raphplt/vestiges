using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Emprise au sol d'un décor, déduite des pixels de son sprite (une fois par texture).
/// Sert à caler la collision et le point de tri en profondeur sur ce que le joueur voit,
/// au lieu d'un cercle posé au pied du sprite.
/// </summary>
public readonly struct PropFootprint
{
    /// <summary>Largeur des pixels opaques dans le cinquième inférieur de la silhouette.</summary>
    public readonly float BaseWidth;
    /// <summary>Centre horizontal de cette base, relatif au centre du sprite.</summary>
    public readonly float BaseCenterX;
    /// <summary>Bord bas de la silhouette, relatif au bas du sprite (≤ 0).</summary>
    public readonly float VisibleBottom;
    public readonly float VisibleHeight;
    public readonly float VisibleWidth;
    public readonly int OpaquePixels;

    public PropFootprint(float baseWidth, float baseCenterX, float visibleBottom, float visibleHeight, float visibleWidth, int opaquePixels)
    {
        BaseWidth = baseWidth;
        BaseCenterX = baseCenterX;
        VisibleBottom = visibleBottom;
        VisibleHeight = visibleHeight;
        VisibleWidth = visibleWidth;
        OpaquePixels = opaquePixels;
    }

    /// <summary>Demi-hauteur du losange iso (2:1) qui couvre la base.</summary>
    public float DiamondHalfHeight(float scale) => Mathf.Max(2f, BaseWidth * scale * 0.25f);

    private const byte OpaqueAlpha = 128;
    private static readonly Dictionary<Texture2D, PropFootprint> Cache = new();

    public static PropFootprint Of(Texture2D texture)
    {
        if (Cache.TryGetValue(texture, out PropFootprint cached))
            return cached;

        PropFootprint footprint = Measure(texture);
        Cache[texture] = footprint;
        return footprint;
    }

    private static PropFootprint Measure(Texture2D texture)
    {
        using Image image = texture.GetImage();
        if (image == null)
            return new PropFootprint(texture.GetWidth(), 0f, 0f, texture.GetHeight(), texture.GetWidth(), texture.GetWidth() * texture.GetHeight());
        if (image.IsCompressed())
            image.Decompress();
        if (image.GetFormat() != Image.Format.Rgba8)
            image.Convert(Image.Format.Rgba8);

        int width = image.GetWidth();
        int height = image.GetHeight();
        byte[] data = image.GetData();
        int minX = width, maxX = -1, minY = height, maxY = -1, opaque = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (data[(y * width + x) * 4 + 3] < OpaqueAlpha)
                    continue;
                opaque++;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }
        if (opaque == 0)
            return new PropFootprint(0f, 0f, 0f, 0f, 0f, 0);

        int visibleHeight = maxY - minY + 1;
        int band = Mathf.Max(3, visibleHeight / 5);
        int baseMin = width, baseMax = -1;
        for (int y = maxY - band + 1; y <= maxY; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (data[(y * width + x) * 4 + 3] < OpaqueAlpha)
                    continue;
                baseMin = Mathf.Min(baseMin, x);
                baseMax = Mathf.Max(baseMax, x);
            }
        }

        float baseWidth = baseMax - baseMin + 1;
        float baseCenterX = (baseMin + baseMax + 1) * 0.5f - width * 0.5f;
        float visibleBottom = maxY + 1 - height;
        return new PropFootprint(baseWidth, baseCenterX, visibleBottom, visibleHeight, maxX - minX + 1, opaque);
    }
}
