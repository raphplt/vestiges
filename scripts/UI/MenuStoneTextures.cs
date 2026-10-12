using System.Collections.Generic;
using Godot;

namespace Vestiges.UI;

/// <summary>Matière raccordable des panneaux : pierre mate M6 et tranches taillées M7, créées une fois.</summary>
public static class MenuStoneTextures
{
    private const int TileSize = 64;
    private const int CenterSize = 128;
    private const int NativeBorder = 6;
    private const int PixelScale = 2;
    public const int BorderSize = NativeBorder * PixelScale;
    private static readonly Dictionary<Texture2D, int> FrameMargins = new();
    private static readonly Color[] Grain = CreateGrain();

    public static bool IsFramed(Texture2D texture) => texture != null && FrameMargins.ContainsKey(texture);
    public static int CornerMargin(Texture2D texture) => FrameMargins[texture];

    /// <summary>Centre raccordable et coins conservés à leur taille ; la tranche suit les découpes.</summary>
    public static Texture2D Create(Color body, Color rim, Color shadow, bool pressed, int cornerSize = NativeBorder)
    {
        int size = CenterSize + cornerSize * 2;
        Color reference = new("24212d");
        using Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Color grain = Grain[((y - cornerSize) & 63) * TileSize + ((x - cornerSize) & 63)];
            Color color = new(Mathf.Clamp(body.R + grain.R - reference.R, 0f, 1f),
                Mathf.Clamp(body.G + grain.G - reference.G, 0f, 1f),
                Mathf.Clamp(body.B + grain.B - reference.B, 0f, 1f));
            color = BorderPixel(x, y, size, cornerSize, color, body, rim, shadow);
            if (pressed) color = color.Darkened(0.18f);
            image.SetPixel(x, y, color);
        }
        image.Resize(size * PixelScale, size * PixelScale, Image.Interpolation.Nearest);
        Texture2D texture = ImageTexture.CreateFromImage(image);
        FrameMargins.Add(texture, cornerSize * PixelScale);
        return texture;
    }

    private static Color BorderPixel(int x, int y, int size, int cornerSize, Color surface, Color body, Color rim, Color shadow)
    {
        int nearX = Mathf.Min(x, size - 1 - x);
        int nearY = Mathf.Min(y, size - 1 - y);
        int edge = Mathf.Min(nearX, nearY);
        bool corner = nearX < cornerSize && nearY < cornerSize;
        bool horizontal = nearY <= nearX;
        bool lit = horizontal ? y < size / 2 : x < size / 2;
        int along = (horizontal ? x : y) - cornerSize;
        int run = along & (CenterSize - 1);
        if (corner)
        {
            bool left = x < cornerSize;
            bool top = y < cornerSize;
            // Chaque fragment perd un coin différent ; le biseau suit aussi la coupe diagonale.
            int cut = Mathf.Max(3, cornerSize - (top ? (left ? 2 : 5) : (left ? 4 : 1)));
            int cutDistance = nearX + nearY - cut;
            if (cutDistance < 0) return Colors.Transparent;
            edge = Mathf.Min(edge, Mathf.FloorToInt(cutDistance * 0.7f));
        }
        if (edge >= NativeBorder) return surface;

        // Décrochements de longueurs différentes : la silhouette est celle d'un éclat de pierre.
        // Le bas et la droite décalent leur phase pour ne pas reproduire la même découpe en miroir.
        int profile = (run + (lit ? 0 : 23) + (horizontal ? 0 : 37)) & (CenterSize - 1);
        int inset = profile is >= 9 and <= 12 or >= 65 and <= 68 ? 1
            : profile is >= 39 and <= 44 ? 2 : profile is >= 103 and <= 108 ? 3 : 0;
        if (!corner)
        {
            if (edge < inset) return Colors.Transparent;
            edge = (edge - inset) * (NativeBorder - 1) / (NativeBorder - inset - 1);
        }
        Color ink = new("1a1a2e");
        // Une face, son biseau éclairé et le creux intérieur : pas de simple filet uniforme.
        Color face = lit ? rim : rim.Lerp(shadow, 0.65f);
        if (run is >= 19 and < 43 or >= 75 and < 94) face = face.Darkened(0.1f);
        else if (run >= 44) face = face.Darkened(0.04f);
        Color color = edge switch
        {
            0 => ink,
            1 => face.Darkened(0.46f),
            2 => lit ? face.Lightened(0.08f) : face,
            3 => face,
            4 => face.Darkened(0.3f),
            _ => body.Darkened(0.28f),
        };

        if (along is >= 0 and < CenterSize)
        {
            // Joints inclinés, profondeur différente et éclats retirés sur la lèvre extérieure.
            // La phase reprend à chaque région répétée, dans le même repère que le grain du centre.
            int crackA = (run - 17 - edge / 2) & (CenterSize - 1);
            int crackB = (run - 91 + edge / 3) & (CenterSize - 1);
            if ((run is 17 or 18 && edge == 0) || (run == 91 && edge < 2))
                return Colors.Transparent;
            if (crackA == 0 || crackB == 0) color = shadow.Darkened(0.35f);
            else if ((crackA == 1 || crackB == 1) && edge is >= 2 and <= 4)
                color = face.Darkened(0.38f);
            else if (edge == 3 && (run is >= 7 and <= 11 || run is >= 53 and <= 56))
                color = lit ? face.Lightened(0.09f) : face.Darkened(0.12f);
        }
        else if (corner)
        {
            // Le coin est une pierre taillée en biais, avec une jonction visible entre ses deux faces.
            if (nearX == nearY && edge is >= 2 and <= 4)
                color = lit ? rim.Lightened(0.15f) : shadow.Darkened(0.15f);
        }
        return color;
    }

    private static Color[] CreateGrain()
    {
        Color[] palette = { new("201e2a"), new("24212d"), new("282530"), new("2d2933") };
        Color[] pixels = new Color[TileSize * TileSize];
        for (int y = 0; y < TileSize; y++)
        for (int x = 0; x < TileSize; x++)
        {
            float broad = 0f;
            float fine = 0f;
            for (int dy = -4; dy <= 4; dy++)
            for (int dx = -4; dx <= 4; dx++)
            {
                float noise = Noise(x + dx, y + dy);
                broad += noise;
                if (Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 1) fine += noise;
            }
            broad /= 81f;
            int level = broad < 0.483f ? 0 : broad < 0.510f ? 1 : broad < 0.542f ? 2 : 3;
            if (fine / 9f > 0.79f && broad > 0.53f) level = 3;
            pixels[y * TileSize + x] = palette[level];
        }
        foreach (Vector2I spot in new[] { new Vector2I(12, 9), new Vector2I(39, 18), new Vector2I(54, 45),
            new Vector2I(18, 53), new Vector2I(31, 35) })
        {
            for (int x = 0; x < 3; x++)
            {
                pixels[spot.Y * TileSize + spot.X + x] = new Color("36323c");
                pixels[(spot.Y + 1) * TileSize + spot.X + x + 1] = new Color("25212f");
            }
        }
        return pixels;
    }

    private static float Noise(int x, int y)
    {
        uint hash = (uint)((x & 63) + (y & 63) * TileSize + 36);
        hash ^= hash >> 16;
        hash *= 0x7feb352d;
        hash ^= hash >> 15;
        hash *= 0x846ca68b;
        hash ^= hash >> 16;
        return (hash & 0x00ffffff) / 16777215f;
    }
}
