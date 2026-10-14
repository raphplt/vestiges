using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.UI;

/// <summary>
/// Matières du level-up au fil (plan 04 §11) : papier déchiré, liseré, taches, scotch, pince, clou, étiquette, cuir
/// cousu et cadre de tampon. Dessinées au pixel et doublées comme les dalles des menus ; le papier et l'étiquette sont
/// presque blancs pour que la rareté ou le kraft les teintent par <c>ModulateColor</c>.
/// </summary>
public static class PaperArt
{
    private const int PixelScale = 2;

    public static readonly Color Ink = new("2a2222");
    public static readonly Color InkDim = new("5e534e");
    public static readonly Color InkGreen = new("265c28");
    public static readonly Color InkRed = new("a82c28");
    public static readonly Color InkBlue = new("283a6e");
    public static readonly Color InkGold = new("8a5a1c");
    public static readonly Color Paper = new("d6ccb6");
    public static readonly Color PaleTag = new("ccc2aa");
    public static readonly Color Kraft = new("b08c5c");
    public static readonly Color Thread = new("968670");
    public static readonly Color ThreadTwist = new("6e604e");
    public static readonly Color ThreadShadow = new("2c241e");

    /// <summary>Marges du papier, en pixels affichés : bord gauche, haut, coin corné à droite, bas déchiré.</summary>
    public const int PaperLeft = 8, PaperTop = 24, PaperRight = 24, PaperBottom = 20;
    /// <summary>Le liseré déborde du papier de ce nombre de pixels affichés de chaque côté.</summary>
    public const int GlowSpread = 6;
    public const int TagMargin = 12;
    public const int BeltMargin = 16;
    public const int StampMargin = 8;

    private const int PaperWidth = 160, PaperHeight = 64, TearDepth = 8, Fold = 10, RuleStep = 6;
    public const int PaperVariants = 3;
    /// <summary>Tirage photo, bord compris, en pixels affichés.</summary>
    public const int PhotoWidth = 304, PhotoHeight = 176;
    private const int PhotoBorder = 4;
    private static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    private static readonly Color Near = new("f2ede4");
    private static readonly Dictionary<string, Texture2D> Cache = new();

    /// <summary>
    /// Papier : 0 uni jauni, 1 cahier ligné, 2 papier quadrillé (copie d'écolier). Les lignes tombent sur la grille de la
    /// zone répétée du NinePatch, sans couture d'une répétition à l'autre.
    /// </summary>
    public static Texture2D PaperTexture(int variant) => Cached($"paper-{variant}", () => Paint(PaperWidth, PaperHeight, (x, y) =>
    {
        if (!InPaper(x, y))
            return Colors.Transparent;
        if (!InPaper(x - 1, y) || !InPaper(x + 1, y) || !InPaper(x, y - 1) || !InPaper(x, y + 1))
            return new Color("6e6458");
        if (InFold(x, y))
            return (x + y) % 3 == 0 ? new Color("a89c88") : new Color("b6ab98");
        // L'ombre du rabat sur la feuille, juste sous la pliure.
        if (InFold(x - 1, y - 1) || InFold(x + 1, y + 1))
            return new Color("cfc6b4");
        float grain = Noise(x, y, 3);
        Color paper = Near.Darkened(grain * 0.06f);
        // Fibres : de courts traits plus sombres, couchés dans le sens de la feuille.
        if (Noise(x / 3, y, 29) > 0.97f)
            paper = paper.Darkened(0.07f);
        bool ruled = variant >= 1 && y % RuleStep == 0;
        bool column = variant == 2 && (x - PaperLeft / PixelScale) % RuleStep == 0;
        if (ruled || column)
            paper = paper.Lerp(new Color("8ea0bc"), ruled && column ? 0.42f : 0.3f);
        return Noise(x, y, 11) > 0.988f ? paper.Darkened(0.14f) : paper;
    }));

    /// <summary>Silhouette du papier épaissie de trois pixels, pour le liseré du fragment survolé.</summary>
    public static Texture2D GlowTexture() => Cached("paper-glow", () =>
    {
        const int spread = GlowSpread / PixelScale;
        return Paint(PaperWidth + spread * 2, PaperHeight + spread * 2, (x, y) =>
        {
            for (int dy = -spread; dy <= spread; dy++)
            for (int dx = -spread; dx <= spread; dx++)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= spread && InPaper(x - spread + dx, y - spread + dy))
                    return Colors.White;
            return Colors.Transparent;
        });
    });

    /// <summary>Tache d'humidité ronde et tramée : trois variantes, posées au hasard sur les fragments.</summary>
    public static Texture2D Stain(int variant) => Cached($"stain-{variant}", () =>
    {
        int size = 10 + variant * 4;
        float radius = size / 2f;
        return Paint(size, size, (x, y) =>
        {
            float d = new Vector2(x + 0.5f - radius, y + 0.5f - radius).Length() / radius;
            if (d > 1f || Noise(x, y, 40 + variant) < d * 0.6f)
                return Colors.Transparent;
            return new Color(0.47f, 0.35f, 0.2f, d > 0.8f ? 0.2f : 0.11f);
        });
    });

    /// <summary>Rond de tasse : un anneau brun, interrompu là où la tasse a glissé.</summary>
    public static Texture2D CoffeeRing() => Cached("coffee-ring", () => Paint(26, 26, (x, y) =>
    {
        Vector2 p = new(x + 0.5f - 13f, y + 0.5f - 13f);
        float d = p.Length();
        float angle = Mathf.Atan2(p.Y, p.X);
        if (d < 10.2f || d > 12.4f || angle > 0.6f && angle < 1.5f)
            return Colors.Transparent;
        return new Color(0.42f, 0.28f, 0.14f, d > 11.6f ? 0.26f : 0.14f);
    }));

    /// <summary>Pliure de la feuille : une arête claire sur une ombre, répétée en largeur.</summary>
    public static Texture2D Crease() => Cached("crease", () => Paint(8, 3, (x, y) => y switch
    {
        0 => new Color(1f, 1f, 1f, Noise(x, 0, 31) > 0.3f ? 0.32f : 0.14f),
        1 => new Color(0.24f, 0.18f, 0.12f, Noise(x, 1, 31) > 0.3f ? 0.16f : 0.08f),
        _ => new Color(0.24f, 0.18f, 0.12f, 0.05f),
    }));

    /// <summary>Scotch jauni, froissé en travers, bords déchirés.</summary>
    public static Texture2D Tape() => Cached("tape", () => Paint(20, 8, (x, y) =>
    {
        bool ragged = (x <= 1 || x >= 18) && Noise(x, y, 5) < (x == 0 || x == 19 ? 0.7f : 0.3f);
        if (ragged)
            return Colors.Transparent;
        bool wrinkle = (x + y * 2) % 7 == 0;
        return wrinkle ? new Color(1f, 0.98f, 0.86f, 0.62f) : new Color(0.9f, 0.85f, 0.6f, 0.46f);
    }));

    /// <summary>
    /// Tirage photo : bord blanc jauni aux coins, scène de ruines au crépuscule virée dans les teintes de la rareté
    /// (sombre, principale, claire), halo derrière l'objet, ombre au sol, grain, rayures, poussière et fuite de lumière.
    /// Tramée en cinq tons par une matrice de Bayer, comme un tirage pixelisé. Quatre cadrages par teinte.
    /// </summary>
    public static Texture2D Photo(string key, Color dark, Color main, Color light, int variant) => Cached($"photo-{key}-{variant}", () =>
    {
        const int width = PhotoWidth / PixelScale, height = PhotoHeight / PixelScale;
        const int innerWidth = width - PhotoBorder * 2, innerHeight = height - PhotoBorder * 2;
        Color[] tones = { dark.Darkened(0.72f), dark.Darkened(0.45f), dark.Darkened(0.1f), main.Darkened(0.25f), light };
        int seed = variant * 101 + 13;
        float horizon = innerHeight * 0.7f;
        int[] roof = Skyline(innerWidth, seed);
        int scratchA = (int)(Noise(0, 7, seed) * innerWidth), scratchB = (int)(Noise(0, 8, seed) * innerWidth);
        Vector2 leakCorner = variant % 2 == 0 ? new Vector2(innerWidth, 0f) : new Vector2(0f, innerHeight);
        return Paint(width, height, (x, y) =>
        {
            if (x < PhotoBorder || y < PhotoBorder || x >= width - PhotoBorder || y >= height - PhotoBorder)
                return PrintBorder(x, y, width, height);
            int px = x - PhotoBorder, py = y - PhotoBorder;
            float cx = innerWidth / 2f, cy = innerHeight * 0.42f;
            // Ciel de crépuscule qui s'allume vers l'horizon, pour que les immeubles s'y découpent ; halo derrière l'objet.
            float rise = py / horizon;
            float light01 = 0.12f + 0.62f * rise * rise;
            float halo = new Vector2((px - cx) / (innerWidth * 0.42f), (py - cy) / (innerHeight * 0.62f)).Length();
            if (halo < 1f)
                light01 += 0.5f * (1f - halo);
            if (py < horizon && roof[px] > 0 && py >= horizon - roof[px])
            {
                // Immeubles en ombre chinoise, quelques fenêtres encore allumées.
                light01 = 0.08f;
                if (px % 3 == 1 && py % 4 == 2 && py < horizon - 2 && Noise(px, py, seed + 5) > 0.72f)
                    light01 = 0.6f;
            }
            else if (py >= horizon)
            {
                // Sol : une bande claire au pied des façades, puis le pavé qui s'assombrit, l'ombre de l'objet au milieu.
                float depth = (py - horizon) / (innerHeight - horizon);
                light01 = 0.42f - 0.3f * depth + 0.1f * Noise(px / 4, py, seed);
                if ((px + py * 3) % 9 == 0)
                    light01 -= 0.12f;
                float shadow = new Vector2((px - cx) / (innerWidth * 0.2f), (py - horizon - 6f) / 3.5f).Length();
                if (shadow < 1f)
                    light01 *= 0.3f;
            }
            // Vignettage, grain, rayures verticales interrompues.
            float vx = (px - innerWidth / 2f) / (innerWidth / 2f), vy = (py - innerHeight / 2f) / (innerHeight / 2f);
            light01 *= 1f - 1.3f * Mathf.Max(0f, Mathf.Sqrt(vx * vx * 0.8f + vy * vy) - 0.62f);
            light01 += (Noise(px, py, seed + 9) - 0.5f) * 0.12f;
            if ((px == scratchA || px == scratchB) && Noise(px, py / 5, seed + 3) > 0.35f)
                light01 = 0.82f;
            float threshold = (Bayer[(py & 3) * 4 + (px & 3)] + 0.5f) / 16f - 0.5f;
            int level = Mathf.Clamp(Mathf.FloorToInt(light01 * 4f + threshold + 0.5f), 0, tones.Length - 1);
            Color c = tones[level];
            if (Noise(px, py, seed + 21) > 0.997f)
                c = light.Lightened(0.35f);
            float leak = Mathf.Max(0f, 1f - new Vector2(px, py).DistanceTo(leakCorner) / (innerWidth * 0.34f));
            return c.Lerp(new Color("e8a060"), leak * leak * 0.5f);
        });
    });

    /// <summary>Hauteurs des toits : immeubles de largeur et de hauteur tirées, quelques trous de rues.</summary>
    private static int[] Skyline(int width, int seed)
    {
        int[] roof = new int[width];
        int x = 0;
        while (x < width)
        {
            int span = 5 + (int)(Noise(x, 1, seed) * 14);
            int height = Noise(x, 3, seed) < 0.18f ? 0 : 4 + (int)(Noise(x, 2, seed) * 16);
            for (int i = x; i < Mathf.Min(width, x + span); i++)
                roof[i] = height;
            // Toits cassés : un pan qui descend d'un pixel sur deux.
            if (height > 0 && Noise(x, 4, seed) > 0.6f)
                for (int i = x; i < Mathf.Min(width, x + span); i += 2)
                    roof[i] = Mathf.Max(1, height - (i - x) / 2);
            x += span;
        }
        return roof;
    }

    private static Color PrintBorder(int x, int y, int width, int height)
    {
        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            return new Color("a89c84");
        if (x == PhotoBorder - 1 || y == PhotoBorder - 1 || x == width - PhotoBorder || y == height - PhotoBorder)
            return new Color("3a3028");
        float corner = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y))
            + 0.25f * Mathf.Min(Mathf.Min(x, width - 1 - x) + Mathf.Min(y, height - 1 - y), 20);
        Color print = new Color("ece6d6").Darkened(Noise(x, y, 61) * 0.07f);
        return corner < 4f ? print.Lerp(new Color("c8b488"), 0.6f) : print;
    }

    /// <summary>Pince à linge en bois veiné, ressort de fer enroulé au milieu.</summary>
    public static Texture2D Peg() => Cached("peg", () => Paint(12, 30, (x, y) =>
    {
        if (y >= 12 && y <= 16 && x >= 1 && x <= 10)
            return x == 1 || x == 10 ? new Color("2a2a30") : (y + x) % 2 == 0 ? new Color("9c9ca6") : new Color("4a4a54");
        if (x <= 1 || x >= 10 || y == 0 || y == 29)
            return x == 0 || x == 11 || y == 0 || y == 29 ? new Color("2e2016") : new Color("3e2c1c");
        if (x == 5 || x == 6)
            return y > 16 ? new Color("2e2016") : new Color("54402a");
        Color wood = x < 5 ? new Color("a27c56") : new Color("7e5e3e");
        if (x == 2 || x == 7)
            wood = wood.Lightened(0.12f);
        return Noise(x, y / 3, 47) > 0.72f ? wood.Darkened(0.18f) : wood;
    }));

    /// <summary>Clou à tête ronde, rouille dans le creux.</summary>
    public static Texture2D Nail() => Cached("nail", () => Paint(10, 10, (x, y) =>
    {
        float d = new Vector2(x - 4.5f, y - 4.5f).Length();
        if (d > 4.9f)
            return Colors.Transparent;
        if (d > 4f)
            return new Color("1e1e24");
        if (x + y < 6)
            return new Color("a8a8b2");
        return Noise(x, y, 53) > 0.6f ? new Color("7a4a2a") : new Color("4a4a54");
    }));

    /// <summary>Œillet d'étiquette : anneau de fer autour d'un trou.</summary>
    public static Texture2D Eyelet() => Cached("eyelet", () => Paint(7, 7, (x, y) =>
    {
        float d = new Vector2(x - 3, y - 3).Length();
        return d <= 1.2f ? new Color("1a1614") : d <= 3.2f ? new Color("6c6460") : Colors.Transparent;
    }));

    /// <summary>Étiquette à coins coupés en haut ; presque blanche, teintée en kraft ou en papier pâle.</summary>
    public static Texture2D TagTexture() => Cached("tag", () => Paint(24, 16, (x, y) =>
    {
        bool Inside(int px, int py) => px >= 0 && py >= 0 && px < 24 && py < 16 && px + py >= 3 && (23 - px) + py >= 3;
        if (!Inside(x, y))
            return Colors.Transparent;
        if (!Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1))
            return new Color("5a4632");
        return Near.Darkened(Noise(x, y, 7) * 0.14f);
    }));

    /// <summary>Cuir sombre grainé et rayé, bord arrondi éclairé en haut, piqûres claires en haut et en bas.</summary>
    public static Texture2D Leather() => Cached("leather", () => Paint(48, 24, (x, y) =>
    {
        if (x == 0 || x == 47 || y == 0 || y == 23)
            return new Color("1a120e");
        if (y == 1)
            return new Color("5e4434");
        if (y == 22)
            return new Color("24180f");
        if ((y == 4 || y == 19) && x % 6 < 3 && x > 2 && x < 45)
            return new Color("a88a5c");
        if ((y == 5 || y == 20) && x % 6 < 3 && x > 2 && x < 45)
            return new Color("2a1c14");
        Color leather = new Color("402c22").Darkened(Noise(x, y, 13) * 0.2f);
        // Éraflures : de courts traits clairs en biais.
        if (Noise(x - y, 0, 19) > 0.93f && Noise(x, y, 23) > 0.5f)
            leather = leather.Lightened(0.14f);
        return leather;
    }));

    /// <summary>Rivet de cuivre entre deux groupes de poches.</summary>
    public static Texture2D Rivet() => Cached("rivet", () => Paint(8, 8, (x, y) =>
    {
        float d = new Vector2(x - 3.5f, y - 3.5f).Length();
        if (d > 3.9f)
            return Colors.Transparent;
        if (d > 3f)
            return new Color("2a1c12");
        return x + y < 5 ? new Color("e0a870") : x + y < 8 ? new Color("a8683a") : new Color("6e4024");
    }));

    /// <summary>Poche cousue : creux sombre, ombre du rabat en haut, couture tiretée tout autour.</summary>
    public static Texture2D Pocket() => Cached("pocket", () => Paint(16, 16, (x, y) =>
    {
        if (x == 0 || y == 0 || x == 15 || y == 15)
            return new Color("16100c");
        bool seam = (x == 1 || x == 14 || y == 1 || y == 14) && (x + y) % 3 != 0;
        if (seam)
            return new Color("8a6e4a");
        if (y <= 3)
            return new Color("120c08");
        return Colors.White.Darkened(0.2f + Noise(x, y, 37) * 0.1f);
    }));

    /// <summary>Cadre de tampon encré, rongé par endroits ; blanc, teint par la couleur de l'encre.</summary>
    public static Texture2D StampFrame() => Cached("stamp", () => Paint(16, 12, (x, y) =>
    {
        bool border = x <= 1 || x >= 14 || y <= 1 || y >= 10;
        if (!border)
            return Colors.Transparent;
        return Noise(x, y, 21) < 0.22f ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.86f);
    }));

    /// <summary>Trait tireté tracé à la main, répété en largeur.</summary>
    public static Texture2D Dash() => Cached("dash", () => Paint(6, 1, (x, _) =>
        x < 3 ? new Color(Ink, 0.45f) : Colors.Transparent));

    public static StyleBoxTexture PaperStyle(Color tint, int variant) =>
        NinePatch(PaperTexture(variant), PaperLeft, PaperTop, PaperRight, PaperBottom, tint);

    public static StyleBoxTexture TagStyle(Color tint) => NinePatch(TagTexture(), TagMargin, TagMargin, TagMargin, TagMargin, tint);

    public const int PocketMargin = 8;

    /// <summary>Poche teinte par la rareté de son contenu ; le creux reste sombre, la couture claire.</summary>
    public static StyleBoxTexture PocketStyle(Color tint) => NinePatch(Pocket(), PocketMargin, PocketMargin, PocketMargin, PocketMargin, tint);

    public static StyleBoxTexture BeltStyle() => NinePatch(Leather(), BeltMargin, BeltMargin, BeltMargin, BeltMargin, Colors.White);

    public static StyleBoxTexture StampStyle(Color ink) => NinePatch(StampFrame(), StampMargin, StampMargin, StampMargin, StampMargin, ink);

    private static StyleBoxTexture NinePatch(Texture2D texture, int left, int top, int right, int bottom, Color tint)
    {
        StyleBoxTexture style = new()
        {
            Texture = texture,
            ModulateColor = tint,
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile,
        };
        style.TextureMarginLeft = left;
        style.TextureMarginTop = top;
        style.TextureMarginRight = right;
        style.TextureMarginBottom = bottom;
        return style;
    }

    // Déchirure du bas : marche aléatoire figée, profonde de huit pixels au plus.
    private static int[] _tear;

    private static int Tear(int x)
    {
        if (_tear == null)
        {
            _tear = new int[PaperWidth];
            int depth = 3;
            for (int i = 0; i < PaperWidth; i++)
            {
                float step = Noise(i, 0, 17);
                depth = Mathf.Clamp(depth + (step < 0.3f ? -1 : step > 0.7f ? 1 : 0), 0, TearDepth - 1);
                _tear[i] = depth;
            }
        }
        return _tear[x];
    }

    private static bool InPaper(int x, int y)
    {
        if (x < 0 || y < 0 || x >= PaperWidth || y >= PaperHeight)
            return false;
        if (y > PaperHeight - 1 - Tear(x))
            return false;
        // Coin corné en haut à droite : le triangle replié laisse un vide au-delà de la pliure.
        return (PaperWidth - 1 - x) + y >= Fold;
    }

    private static bool InFold(int x, int y) => x >= PaperWidth - 1 - Fold && y <= Fold && x - (PaperWidth - 1 - Fold) <= y;

    /// <summary>Bruit entier déterministe entre 0 et 1 : la matière reste la même d'une partie à l'autre.</summary>
    private static float Noise(int x, int y, int seed)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
        h = (h ^ (h >> 13)) * 1274126177;
        h ^= h >> 16;
        return (h & 0xFFFF) / 65535f;
    }

    private static Texture2D Cached(string key, Func<Texture2D> build)
    {
        if (!Cache.TryGetValue(key, out Texture2D texture))
        {
            texture = build();
            Cache[key] = texture;
        }
        return texture;
    }

    private static Texture2D Paint(int width, int height, Func<int, int, Color> paint)
    {
        int scaledWidth = width * PixelScale;
        byte[] data = new byte[scaledWidth * height * PixelScale * 4];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Color c = paint(x, y);
            byte r = (byte)(c.R8), g = (byte)(c.G8), b = (byte)(c.B8), a = (byte)(c.A8);
            for (int sy = 0; sy < PixelScale; sy++)
            for (int sx = 0; sx < PixelScale; sx++)
            {
                int i = (((y * PixelScale + sy) * scaledWidth) + x * PixelScale + sx) * 4;
                data[i] = r;
                data[i + 1] = g;
                data[i + 2] = b;
                data[i + 3] = a;
            }
        }
        using Image image = Image.CreateFromData(scaledWidth, height * PixelScale, false, Image.Format.Rgba8, data);
        return ImageTexture.CreateFromImage(image);
    }
}
