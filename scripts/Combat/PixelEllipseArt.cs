using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Ellipses au sol cuites en textures blanches au pixel du monde (plan 37 M1) : bord plein, ou intérieur tramé. Un
/// grand repère (zone de 96 px) se dessine en une texture au lieu de milliers de rectangles par image ; la couleur
/// vient de la modulation. Une texture par rayon et par sorte, gardée pour la session.
/// </summary>
public static class PixelEllipseArt
{
    private static readonly Dictionary<(int Radius, bool Fill), Texture2D> Cache = new();

    /// <summary>Texture du bord (<paramref name="fill"/> faux) ou de la trame intérieure, centrée par <see cref="Offset"/>.</summary>
    public static Texture2D Get(int radius, bool fill)
    {
        if (Cache.TryGetValue((radius, fill), out Texture2D texture))
            return texture;
        (Vector2I[] rim, Vector2I[] inside) = PixelGroundRing.Ellipse(radius);
        Vector2I offset = Offset(radius);
        using Image image = Image.CreateEmpty(radius * 2 + 1, -offset.Y * 2 + 1, false, Image.Format.Rgba8);
        foreach (Vector2I pixel in fill ? inside : rim)
            image.SetPixelv(pixel - offset, Colors.White);
        texture = ImageTexture.CreateFromImage(image);
        Cache[(radius, fill)] = texture;
        return texture;
    }

    /// <summary>Coin haut gauche de la texture par rapport au centre de l'ellipse.</summary>
    public static Vector2I Offset(int radius) => new(-radius, -Mathf.CeilToInt(radius / Core.Iso.GroundSquash));
}
