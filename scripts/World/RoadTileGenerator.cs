using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Chaussées de la ville, superposées au sol urbain (plan 37 M3). Les rues restent sur les axes de l'écran, mais leurs
/// bords se lisent au sol isométrique : vue du sud-est, la bordure au nord d'une rue montre sa face, celle à l'ouest
/// d'une rue verticale aussi ; au sud et à l'est, seul le dessus de la bordure dépasse. Aux croisements, les angles
/// sont abattus en diagonale 2:1, la pente du sol iso. Bordures cassées par tronçons, gravats et nids-de-poule
/// brisent la ligne droite.
/// Grille « stacked » : une colonne de cellules zigzague de ±16 px d'un rang à l'autre ; la bande verticale est
/// recentrée sur la colonne, et chaque tuile ne dessine que sa tranche de 16 px de haut, le pas entre deux rangs.
/// La géométrie est celle du réseau entier, évaluée autour de chaque cellule : un raccord peut déborder sur la
/// tranche d'une voisine. Une tuile ne dépend que des routes de son voisinage 5×5, d'où une clé par motif de voisinage
/// et une poignée de textures par carte.
/// </summary>
public static class RoadTileGenerator
{
    private const int WearVariants = 4;

    private const int TileW = 64;
    private const int TileH = 32;
    private const int HalfW = TileW / 2;
    private const int SlotHalf = 8;
    private const int SlotTop = TileH / 2 - SlotHalf;
    private const int ColumnShift = 16;
    private const int RowStep = 16;

    // Chaussée : 11 rangs pour une rue horizontale, 23 colonnes pour une verticale (2:1 au sol).
    private const int AsphaltHalfH = 5;
    private const int AsphaltHalfV = 11;
    // Angle abattu des croisements : dx + 2·dy ≤ portée, une diagonale du sol iso.
    private const int FilletReach = 14;
    // Hauteur de la face visible d'une bordure.
    private const int FaceDepth = 2;
    // Marge évaluée autour de la tranche : faces et dessus de bordure regardent jusqu'à trois pixels plus loin.
    private const int Margin = FaceDepth + 2;
    private const int EvalW = TileW + Margin * 2;
    private const int EvalH = SlotHalf * 2 + Margin * 2;

    // Tronçons de bordure : 8 px le long d'une rue horizontale, 4 rangs le long d'une verticale.
    private const int BlockW = 8;
    private const int BlockH = 4;
    private const int BrokenPercent = 24;
    private const int ChippedPercent = 22;
    private const int BiteMargin = Margin + 1;
    private const int BiteTop = 3;
    private const int BiteBottom = SlotHalf * 2 - 2;

    private const int Window = 2;
    private const int WindowSide = Window * 2 + 1;
    private const int ParityBit = 1 << (WindowSide * WindowSide);

    private enum Surface : byte
    {
        None,
        Asphalt,
        Gutter,
        Contour,
        FaceS,
        FaceSBase,
        FaceE,
        FaceEBase,
        Lip,
    }

    // Asphalte et bords de chaussée
    private static readonly Color AsphaltDark = new("3a3a3e");
    private static readonly Color AsphaltMid = new("454548");
    private static readonly Color AsphaltLight = new("505055");
    private static readonly Color GutterColor = new("323235");
    private static readonly Color ContourColor = new("27272a");
    private static readonly Color CrackDark = new("2d2d2f");
    private static readonly Color CrackLight = new("363538");
    private static readonly Color PatchDark = new("414144");
    private static readonly Color PatchLight = new("4b4a4d");
    private static readonly Color HoleDark = new("28282b");
    private static readonly Color HoleWall = new("38383b");
    private static readonly Color HoleRim = new("5a5a5e");
    private static readonly Color MarkingYellow = new(0.769f, 0.659f, 0.188f, 0.45f);

    // Bordure de pierre : dessus pris par la lumière, face sud claire, face est dans l'ombre (comme les immeubles).
    private static readonly Color CurbTop = new("a29f95");
    private static readonly Color CurbTopWorn = new("817e76");
    private static readonly Color FaceSouth = new("6a675f");
    private static readonly Color FaceSouthBase = new("3a3937");
    private static readonly Color FaceEast = new("55524c");
    private static readonly Color FaceEastBase = new("383735");
    private static readonly Color FaceJoint = new("33322f");

    // Gravats d'une bordure effondrée, herbe des joints (#4A7A3A de la charte), rouille.
    private static readonly Color[] Rubble = { new("2f2d2c"), new("4a4744"), new("5f5a54"), new("77716a") };
    private static readonly Color GrassDark = new("2e5222");
    private static readonly Color GrassLight = new("4a7a3a");
    private static readonly Color DebrisColor = new("6b3a24");

    // Une même clé donne toujours les mêmes tuiles : elles servent d'une run à l'autre. Deux voisinages qui dessinent
    // la même tranche partagent leur groupe. Images remplies par la génération (hors du thread principal), textures
    // par le thread principal.
    private static readonly Dictionary<int, string> GroupOfKey = new();
    private static readonly Dictionary<string, Image[]> Images = new();
    private static readonly Dictionary<string, ImageTexture[]> Textures = new();

    /// <summary>
    /// Tuiles de chaussée de toute la ville : une clé par motif de voisinage, ses variantes d'usure. Calcul pur, sans
    /// texture : tourne avec la génération du monde, hors du thread principal.
    /// </summary>
    public static RoadTiles Build(UrbanLayout layout)
    {
        lock (Images)
            return BuildLocked(layout);
    }

    private static RoadTiles BuildLocked(UrbanLayout layout)
    {
        RoadTiles tiles = new();
        HashSet<Vector2I> candidates = new();
        foreach (Vector2I road in layout.RoadCells)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                    candidates.Add(new Vector2I(road.X + dx, road.Y + dy));
            }
        }

        foreach (Vector2I cell in candidates)
        {
            if (!InGrid(layout, cell.X, cell.Y))
                continue;
            int key = KeyOf(layout, cell);
            if (!GroupOfKey.TryGetValue(key, out string group))
            {
                group = Render(key);
                GroupOfKey[key] = group;
            }
            if (group == null)
                continue;
            tiles.Cells[cell] = group;
            tiles.Groups.TryAdd(group, Images[group]);
        }
        return tiles;
    }

    /// <summary>Textures d'un groupe, créées à sa première pose : sur le thread principal.</summary>
    public static ImageTexture[] TexturesOf(string group, Image[] images)
    {
        lock (Images)
            return TexturesOfLocked(group, images);
    }

    private static ImageTexture[] TexturesOfLocked(string group, Image[] images)
    {
        if (Textures.TryGetValue(group, out ImageTexture[] textures))
            return textures;
        textures = new ImageTexture[images.Length];
        for (int i = 0; i < images.Length; i++)
            textures[i] = ImageTexture.CreateFromImage(images[i]);
        Textures[group] = textures;
        return textures;
    }

    /// <summary>Motif des routes dans le voisinage 5×5 de la cellule, et parité de son rang.</summary>
    private static int KeyOf(UrbanLayout layout, Vector2I cell)
    {
        int key = (cell.Y & 1) != 0 ? ParityBit : 0;
        for (int j = -Window; j <= Window; j++)
        {
            for (int i = -Window; i <= Window; i++)
            {
                if (InGrid(layout, cell.X + i, cell.Y + j)
                    && layout.CellGrid[cell.X + i + layout.MapRadius, cell.Y + j + layout.MapRadiusY] == UrbanCellType.Road)
                    key |= 1 << WindowIndex(i, j);
            }
        }
        return key;
    }

    private static bool InGrid(UrbanLayout layout, int x, int y)
    {
        int gx = x + layout.MapRadius;
        int gy = y + layout.MapRadiusY;
        return gx >= 0 && gy >= 0 && gx < layout.CellGrid.GetLength(0) && gy < layout.CellGrid.GetLength(1);
    }

    private static int WindowIndex(int i, int j) => (i + Window) + (j + Window) * WindowSide;

    private static bool IsRoad(int key, int i, int j) =>
        i >= -Window && i <= Window && j >= -Window && j <= Window && (key & (1 << WindowIndex(i, j))) != 0;

    // =====================================================================
    //  GÉOMÉTRIE — chaussée du réseau, autour de la tranche de la cellule
    // =====================================================================

    /// <summary>
    /// Chaussée vue de la cellule centrale : repère centré sur elle, x de −32 à 31, sa tranche de −8 à 7. Une cellule
    /// voisine du rang j est décalée de 32 px si sa parité diffère, et sa bande verticale recentrée de ±16 px.
    /// </summary>
    private static bool[] Asphalt(int key)
    {
        int parity = (key & ParityBit) != 0 ? 1 : 0;
        bool[] asphalt = new bool[EvalW * EvalH];
        for (int j = -1; j <= 1; j++)
        {
            int rowParity = (parity + j) & 1;
            int centerY = j * RowStep;
            for (int i = -1; i <= 1; i++)
            {
                if (!IsRoad(key, i, j))
                    continue;
                int centerX = i * TileW + (rowParity - parity) * HalfW;
                int column = centerX + (rowParity == 0 ? ColumnShift : -ColumnShift);
                bool left = IsRoad(key, i - 1, j);
                bool right = IsRoad(key, i + 1, j);
                bool up = IsRoad(key, i, j - 1);
                bool down = IsRoad(key, i, j + 1);
                // Emprise de la cellule : sa tranche, l'angle abattu au-dessus, la colonne et ses angles de part et d'autre.
                int fromX = Mathf.Max(0, centerX - HalfW - ColumnShift + Margin + HalfW);
                int toX = Mathf.Min(EvalW, centerX + HalfW + ColumnShift + Margin + HalfW);
                int fromY = Mathf.Max(0, centerY - SlotHalf - FilletReach / 2 + Margin + SlotHalf);
                int toY = Mathf.Min(EvalH, centerY + SlotHalf + FilletReach / 2 + Margin + SlotHalf);
                for (int y = fromY; y < toY; y++)
                {
                    int py = y - Margin - SlotHalf;
                    for (int x = fromX; x < toX; x++)
                    {
                        int px = x - Margin - HalfW;
                        if (InCell(px - column, py - centerY, px - centerX, left, right, up, down))
                            asphalt[x + y * EvalW] = true;
                    }
                }
            }
        }
        return asphalt;
    }

    /// <summary>
    /// Le point (dx, dy), relatif à la colonne et au centre de la cellule, est-il sur sa chaussée ? Bande horizontale,
    /// bande verticale bornée à la tranche, et angles abattus là où deux bras se rejoignent.
    /// </summary>
    private static bool InCell(int dx, int dy, int fromCenter, bool left, bool right, bool up, bool down)
    {
        if ((left || right) && Mathf.Abs(dy) <= AsphaltHalfH)
        {
            bool fromLeft = left ? fromCenter >= -HalfW : dx >= -AsphaltHalfV;
            bool toRight = right ? fromCenter < HalfW : dx <= AsphaltHalfV;
            if (fromLeft && toRight)
                return true;
        }

        if ((up || down) && Mathf.Abs(dx) <= AsphaltHalfV)
        {
            int top = up ? -SlotHalf : -AsphaltHalfH;
            int bottom = down ? SlotHalf - 1 : AsphaltHalfH;
            if (dy >= top && dy <= bottom)
                return true;
        }

        if (!(left || right) || !(up || down))
            return false;
        int outLeft = -AsphaltHalfV - dx;
        int outRight = dx - AsphaltHalfV;
        int outUp = -AsphaltHalfH - dy;
        int outDown = dy - AsphaltHalfH;
        return (up && left && Fillet(outLeft, outUp))
            || (up && right && Fillet(outRight, outUp))
            || (down && left && Fillet(outLeft, outDown))
            || (down && right && Fillet(outRight, outDown));
    }

    private static bool Fillet(int across, int along) => across > 0 && along > 0 && across + 2 * along <= FilletReach;

    /// <summary>
    /// Surface de chaque pixel de la tranche. Vue du sud-est : une bordure dont la chaussée est au sud ou à l'est
    /// montre sa face ; à l'opposé, la chaussée finit sur un trait sombre, sous le dessus de la bordure.
    /// </summary>
    private static Surface[] Surfaces(bool[] asphalt)
    {
        // Faces sur toute la zone évaluée, d'où le dessus de bordure se déduit par voisinage.
        bool[] face = new bool[EvalW * EvalH];
        for (int y = 0; y < EvalH - FaceDepth; y++)
        {
            for (int x = 0; x < EvalW - FaceDepth; x++)
            {
                int i = x + y * EvalW;
                face[i] = !asphalt[i] && (asphalt[i + EvalW] || asphalt[i + EvalW * 2] || asphalt[i + 1] || asphalt[i + 2]);
            }
        }

        Surface[] surfaces = new Surface[TileW * SlotHalf * 2];
        for (int y = 0; y < SlotHalf * 2; y++)
        {
            for (int x = 0; x < TileW; x++)
            {
                int i = (x + Margin) + (y + Margin) * EvalW;
                Surface surface;
                if (asphalt[i])
                {
                    surface = !asphalt[i + EvalW] || !asphalt[i + 1] ? Surface.Contour
                        : !asphalt[i - EvalW] || !asphalt[i - 1] ? Surface.Gutter
                        : Surface.Asphalt;
                }
                else if (asphalt[i + EvalW])
                    surface = Surface.FaceSBase;
                else if (asphalt[i + EvalW * 2])
                    surface = Surface.FaceS;
                else if (asphalt[i + 1])
                    surface = Surface.FaceEBase;
                else if (asphalt[i + 2])
                    surface = Surface.FaceE;
                else
                {
                    surface = Surface.None;
                    for (int ny = -1; ny <= 1 && surface == Surface.None; ny++)
                    {
                        for (int nx = -1; nx <= 1; nx++)
                        {
                            int n = i + nx + ny * EvalW;
                            if (asphalt[n] || face[n])
                            {
                                surface = Surface.Lip;
                                break;
                            }
                        }
                    }
                }
                surfaces[x + y * TileW] = surface;
            }
        }
        return surfaces;
    }

    // =====================================================================
    //  PEINTURE — une tuile par variante d'usure
    // =====================================================================

    /// <summary>
    /// Morsures du bord de chaussée : demi-ellipses où le trottoir s'est effondré dans la rue. Elles restent hors des
    /// pixels que lisent les tuiles voisines (les trois premières colonnes et la dernière, les rangs 0 à 2 et 15 de la tranche ; on garde cinq colonnes) :
    /// les tuiles se raccordent sans savoir qu'une morsure existe.
    /// </summary>
    private static void Bite(bool[] asphalt, uint seed)
    {
        bool At(int x, int y) => asphalt[(x + Margin) + (y + Margin) * EvalW];

        int bites = (int)(seed % 3);
        for (int attempt = 0; attempt < 24 && bites > 0; attempt++)
        {
            seed = CellHash.Of((int)seed, attempt, 0xB17E);
            int x = BiteMargin + (int)(seed % (TileW - BiteMargin * 2));
            int y = (int)(seed / 64 % (SlotHalf * 2));
            if (!At(x, y))
                continue;
            bool northEdge = !At(x, y - 1);
            bool southEdge = !At(x, y + 1);
            bool westEdge = !At(x - 1, y) && y >= BlockH && y < SlotHalf * 2 - BlockH;
            bool eastEdge = !At(x + 1, y) && y >= BlockH && y < SlotHalf * 2 - BlockH;
            if (!(northEdge || southEdge || westEdge || eastEdge))
                continue;

            bool horizontal = northEdge || southEdge;
            int length = horizontal ? 3 + (int)(seed / 1024 % 4) : 2 + (int)(seed / 1024 % 2);
            int depth = horizontal ? 1 + (int)(seed / 8192 % 2) : 2 + (int)(seed / 8192 % 3);
            int towardX = westEdge ? 1 : eastEdge ? -1 : 0;
            int towardY = northEdge ? 1 : southEdge ? -1 : 0;
            for (int along = -length; along <= length; along++)
            {
                float t = along / (length + 0.5f);
                int reach = Mathf.RoundToInt(depth * Mathf.Sqrt(1f - t * t));
                for (int step = 0; step < reach; step++)
                {
                    int bx = horizontal ? x + along : x + towardX * step;
                    int by = horizontal ? y + towardY * step : y + along;
                    if (bx >= BiteMargin && bx < TileW - BiteMargin && by >= BiteTop && by <= BiteBottom
                        && (horizontal || (by >= BlockH && by < SlotHalf * 2 - BlockH)))
                        asphalt[(bx + Margin) + (by + Margin) * EvalW] = false;
                }
            }
            bites--;
        }
    }

    /// <summary>Groupe de tuiles de la clé, peint à sa première rencontre ; nul si la tranche est vide.</summary>
    private static string Render(int key)
    {
        bool[] asphalt = Asphalt(key);
        Surface[] surfaces = Surfaces(asphalt);
        bool left = IsRoad(key, -1, 0) && IsRoad(key, 0, 0);
        bool right = IsRoad(key, 1, 0) && IsRoad(key, 0, 0);
        bool up = IsRoad(key, 0, -1) && IsRoad(key, 0, 0);
        bool down = IsRoad(key, 0, 1) && IsRoad(key, 0, 0);
        bool horizontalMarking = left && right && !up && !down;
        bool verticalMarking = up && down && !left && !right;
        int column = HalfW + ((key & ParityBit) != 0 ? -ColumnShift : ColumnShift);

        // Empreinte de la tranche : surfaces et marquage, ce qui seul décide du dessin.
        ulong signature = 14695981039346656037UL;
        bool any = false;
        foreach (Surface surface in surfaces)
        {
            any |= surface != Surface.None;
            signature = (signature ^ (byte)surface) * 1099511628211UL;
        }
        if (!any)
            return null;
        signature = (signature ^ (horizontalMarking ? 1UL : 0UL)) * 1099511628211UL;
        signature = (signature ^ (verticalMarking ? (ulong)column : 0UL)) * 1099511628211UL;
        string group = $"road_{signature:x16}";
        if (Images.ContainsKey(group))
            return group;

        Image[] images = new Image[WearVariants];
        for (int variant = 0; variant < WearVariants; variant++)
        {
            uint seed = CellHash.Of((int)(signature >> 32), (int)signature ^ variant, 0x80AD);
            bool[] bitten = (bool[])asphalt.Clone();
            Bite(bitten, seed);
            Tile tile = new(Surfaces(bitten), seed);
            tile.PaintBase();
            tile.Wear();
            tile.Cracks();
            if (horizontalMarking)
                tile.HorizontalMarking();
            if (verticalMarking)
                tile.VerticalMarking(column);
            // Un nid-de-poule par motif au plus, sur la dernière variante : répété d'une tuile à l'autre, il ferait motif.
            if (variant == WearVariants - 1)
                tile.Pothole();
            tile.Grass();
            images[variant] = tile.ToImage();
        }
        Images[group] = images;
        return group;
    }

    /// <summary>Peinture d'une tranche : couleurs par surface, puis usure tirée d'une graine propre à la variante.</summary>
    private sealed class Tile
    {
        private readonly Surface[] _surfaces;
        private readonly Color?[] _pixels = new Color?[TileW * SlotHalf * 2];
        private uint _rng;

        public Tile(Surface[] surfaces, uint seed)
        {
            _surfaces = surfaces;
            _rng = seed == 0 ? 1u : seed;
        }

        private Surface SurfaceAt(int x, int y) =>
            x < 0 || x >= TileW || y < 0 || y >= SlotHalf * 2 ? Surface.None : _surfaces[x + y * TileW];

        private static bool IsPaved(Surface surface) =>
            surface is Surface.Asphalt or Surface.Gutter or Surface.Contour;

        private uint Next()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return _rng;
        }

        private int Roll(int range) => (int)(Next() % (uint)range);

        public void PaintBase()
        {
            uint baseSeed = _rng;
            for (int y = 0; y < SlotHalf * 2; y++)
            {
                for (int x = 0; x < TileW; x++)
                {
                    Surface surface = _surfaces[x + y * TileW];
                    uint grain = CellHash.Of(x, y, baseSeed);
                    _pixels[x + y * TileW] = surface switch
                    {
                        Surface.Asphalt => AsphaltGrain(grain),
                        Surface.Gutter => GutterColor,
                        Surface.Contour => ContourColor,
                        Surface.FaceS => x % BlockW == 0 ? FaceJoint : FaceSouth,
                        Surface.FaceSBase => x % BlockW == 0 ? FaceJoint : FaceSouthBase,
                        Surface.FaceE => y % BlockH == 0 ? FaceJoint : FaceEast,
                        Surface.FaceEBase => FaceEastBase,
                        Surface.Lip => grain % 9 == 0 ? CurbTopWorn : CurbTop,
                        _ => null,
                    };
                }
            }
        }

        private static Color AsphaltGrain(uint grain)
        {
            int noise = (int)(grain % 100);
            Color color = noise < 45 ? AsphaltMid : noise < 75 ? AsphaltDark : noise < 92 ? AsphaltLight : AsphaltMid;
            float shift = ((int)(grain / 100 % 5) - 2) / 255f;
            return new Color(color.R + shift, color.G + shift, color.B + shift);
        }

        /// <summary>
        /// Bordures cassées par tronçons : la face tombe en gravats, le dessus manque, la chaussée s'effrite à son pied.
        /// D'autres tronçons sont seulement écornés. Les tronçons suivent la grille 8×4 de la tuile : jamais à cheval
        /// sur deux tuiles, donc sans joint.
        /// </summary>
        public void Wear()
        {
            uint wearSeed = Next();
            for (int y = 0; y < SlotHalf * 2; y++)
            {
                for (int x = 0; x < TileW; x++)
                {
                    Surface surface = _surfaces[x + y * TileW];
                    if (surface == Surface.None)
                        continue;
                    int roll = (int)(CellHash.Of(x / BlockW, y / BlockH, wearSeed) % 100);
                    uint pixel = CellHash.Of(x, y, wearSeed ^ 0x5EED);
                    if (roll < BrokenPercent)
                        Break(x, y, surface, pixel);
                    else if (roll < BrokenPercent + ChippedPercent && surface == Surface.Lip && pixel % 3 == 0)
                        _pixels[x + y * TileW] = CurbTopWorn;
                }
            }
        }

        private void Break(int x, int y, Surface surface, uint pixel)
        {
            int index = x + y * TileW;
            switch (surface)
            {
                case Surface.FaceS or Surface.FaceSBase or Surface.FaceE or Surface.FaceEBase:
                    _pixels[index] = Rubble[pixel % 3];
                    break;
                case Surface.Lip:
                    _pixels[index] = pixel % 2 == 0 ? null : Rubble[1 + pixel % 3];
                    break;
                default:
                    if (NearCurb(x, y) && pixel % 5 < 3)
                        _pixels[index] = pixel % 4 == 0 ? null : Rubble[1 + pixel % 3];
                    break;
            }
        }

        private bool NearCurb(int x, int y)
        {
            for (int ny = -2; ny <= 2; ny++)
            {
                for (int nx = -2; nx <= 2; nx++)
                {
                    if (!IsPaved(SurfaceAt(x + nx, y + ny)) && SurfaceAt(x + nx, y + ny) != Surface.None)
                        return true;
                }
            }
            return false;
        }

        /// <summary>Fissures en marche aléatoire sur l'asphalte, plus nombreuses sur les grandes surfaces.</summary>
        public void Cracks()
        {
            int paved = 0;
            foreach (Surface surface in _surfaces)
            {
                if (surface == Surface.Asphalt)
                    paved++;
            }
            int count = paved / 180;
            for (int i = 0; i < count; i++)
            {
                int cx = Roll(TileW);
                int cy = Roll(SlotHalf * 2);
                bool horizontal = Roll(3) != 0;
                int length = 4 + Roll(8);
                for (int step = 0; step < length; step++)
                {
                    if (SurfaceAt(cx, cy) == Surface.Asphalt)
                        _pixels[cx + cy * TileW] = step % 3 == 0 ? CrackDark : CrackLight;
                    int turn = Roll(6);
                    if (horizontal)
                    {
                        cx++;
                        if (turn == 0) cy++;
                        else if (turn == 1) cy--;
                    }
                    else
                    {
                        cy++;
                        if (turn == 0) cx++;
                        else if (turn == 1) cx--;
                    }
                }
            }

            if (paved > 120 && Roll(3) == 0)
                Patch();
        }

        private void Patch()
        {
            int px = Roll(TileW - 8);
            int py = Roll(SlotHalf * 2 - 3);
            int width = 3 + Roll(5);
            int height = 2 + Roll(2);
            for (int y = py; y < py + height; y++)
            {
                for (int x = px; x < px + width; x++)
                {
                    if (SurfaceAt(x, y) == Surface.Asphalt)
                        _pixels[x + y * TileW] = Roll(3) == 0 ? PatchLight : PatchDark;
                }
            }
        }

        /// <summary>
        /// Nid-de-poule en ellipse 2:1, couché sur le sol iso : on voit sa paroi nord, un liseré clair borde son
        /// rebord sud.
        /// </summary>
        public void Pothole()
        {
            if (Roll(2) != 0)
                return;
            int radiusX = 4 + Roll(2);
            int radiusY = radiusX / 2;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                int cx = radiusX + 1 + Roll(TileW - radiusX * 2 - 2);
                int cy = radiusY + 1 + Roll(Mathf.Max(1, SlotHalf * 2 - radiusY * 2 - 2));
                if (!HoleFits(cx, cy, radiusX, radiusY))
                    continue;
                for (int y = cy - radiusY - 1; y <= cy + radiusY + 1; y++)
                {
                    for (int x = cx - radiusX; x <= cx + radiusX; x++)
                    {
                        if (InHole(x - cx, y - cy, radiusX, radiusY))
                            _pixels[x + y * TileW] = InHole(x - cx, y - cy - 1, radiusX, radiusY) ? HoleDark : HoleWall;
                        else if (InHole(x - cx, y - cy - 1, radiusX, radiusY))
                            _pixels[x + y * TileW] = HoleRim;
                    }
                }
                return;
            }
        }

        private static bool InHole(int dx, int dy, int radiusX, int radiusY)
        {
            float nx = dx / (radiusX + 0.5f);
            float ny = dy / (radiusY + 0.5f);
            return nx * nx + ny * ny <= 1f;
        }

        private bool HoleFits(int cx, int cy, int radiusX, int radiusY)
        {
            for (int y = cy - radiusY - 1; y <= cy + radiusY + 1; y++)
            {
                for (int x = cx - radiusX - 1; x <= cx + radiusX + 1; x++)
                {
                    if (SurfaceAt(x, y) != Surface.Asphalt)
                        return false;
                }
            }
            return true;
        }

        // Pointillés fanés, calés sur la grille de la tuile pour se suivre d'une tuile à l'autre.
        public void HorizontalMarking()
        {
            for (int x = 0; x < TileW; x++)
            {
                if (x % 8 < 4 && Roll(7) != 0)
                    Mark(x, SlotHalf);
            }
        }

        // Une rue verticale est vue raccourcie de moitié : tirets de deux rangs.
        public void VerticalMarking(int column)
        {
            for (int y = 0; y < SlotHalf * 2; y++)
            {
                if (y % 4 < 2 && Roll(7) != 0)
                    Mark(column, y);
            }
        }

        private void Mark(int x, int y)
        {
            if (SurfaceAt(x, y) != Surface.Asphalt || _pixels[x + y * TileW] is not Color existing)
                return;
            _pixels[x + y * TileW] = existing.Lerp(MarkingYellow with { A = 1f }, MarkingYellow.A);
        }

        /// <summary>Herbe et rouille dans les joints de bordure et sur les gravats.</summary>
        public void Grass()
        {
            int tufts = 2 + Roll(4);
            for (int attempt = 0; attempt < 40 && tufts > 0; attempt++)
            {
                int x = Roll(TileW);
                int y = Roll(SlotHalf * 2);
                Surface surface = SurfaceAt(x, y);
                bool joint = surface == Surface.Lip || (surface != Surface.None && _pixels[x + y * TileW] is Color c && c == FaceJoint);
                bool rubble = _pixels[x + y * TileW] is Color r && System.Array.IndexOf(Rubble, r) >= 0;
                if (!joint && !rubble)
                    continue;
                _pixels[x + y * TileW] = Roll(6) == 0 ? DebrisColor : Roll(2) == 0 ? GrassDark : GrassLight;
                if (y > 0 && Roll(2) == 0 && SurfaceAt(x, y - 1) == Surface.None)
                    _pixels[x + (y - 1) * TileW] = GrassLight;
                tufts--;
            }
        }

        public Image ToImage()
        {
            byte[] data = new byte[TileW * TileH * 4];
            for (int y = 0; y < SlotHalf * 2; y++)
            {
                for (int x = 0; x < TileW; x++)
                {
                    if (_pixels[x + y * TileW] is not Color color)
                        continue;
                    int offset = (x + (y + SlotTop) * TileW) * 4;
                    data[offset] = (byte)color.R8;
                    data[offset + 1] = (byte)color.G8;
                    data[offset + 2] = (byte)color.B8;
                    data[offset + 3] = 255;
                }
            }
            return Image.CreateFromData(TileW, TileH, false, Image.Format.Rgba8, data);
        }
    }
}
