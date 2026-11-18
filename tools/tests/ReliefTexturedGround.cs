using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Le relief texturé du bac à sable (plan 60, §235), seul sol gardé (§238) : la géométrie de l'arène couverte de
/// textures pixel art (tools/generate_relief_textures.py) lues au plus proche. Les coordonnées de texture sont celles du
/// monde, une texture pour 4 × 4 cellules : le motif court d'une cellule à l'autre et d'une falaise à l'autre sans
/// couture, les strates des flancs restent à la même hauteur partout. Une frange d'herbe déborde du haut des falaises
/// herbues ; la teinte des sommets ondule en larges plaques ; les falaises du vide plongent au noir.
/// Deux variantes : droite, des falaises planes ; taillée (§238), des falaises en facettes de roche, dont le bruit court
/// d'une cellule à la suivante tant que la paroi continue, sans que la texture s'étire. Les rampes (§239) ont un dessus
/// en pente, couvert de terre battue, et des talus droits.
/// </summary>
public static class ReliefTexturedGround
{
    private const string TextureDir = "res://assets/models/textures";
    private const float CellsPerTexture = 4f;
    // Hauteur de la frange (cellules) : ses 16 px à la densité des dessus, 32 px par cellule.
    private const float LipHeight = 0.5f;
    // La frange se tient un rien devant la falaise, sans s'y mêler à la profondeur.
    private const float LipOffset = 0.004f;
    // Maillage des falaises taillées : trois facettes par cellule de large, une rangée par quart de cellule.
    private const int CarvedColumns = 3;
    private const float CarvedRowHeight = 0.25f;

    // Les plateaux d'un à trois étages sont herbus ; au-delà, la roche nue (la tour), §239.
    internal const float TerraceTopLevel = 3.5f;

    private enum Layer { Ground, Terrace, Butte, Steps, EarthSide, RockSide, LipGround, LipTerrace }

    // Les dessus du sol et des plateaux herbus, et leurs franges, changent avec la terre semée (UseLand, plan 60 §22).
    private static readonly string[] TextureNames =
    {
        "sol", "terrasse", "butte", "marches", "flanc_terre", "flanc_roche", "frange_sol", "frange_terrasse",
    };

    private sealed class Surface
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<Vector3> Normals = new();
        public readonly List<Vector2> Uvs = new();
        public readonly List<Color> Colors = new();

        /// <summary>
        /// Un quadrilatère en deux triangles, chacun de sa normale : une facette de roche qui n'est pas plane prend la
        /// lumière en deux tons. <paramref name="flip"/> change de diagonale, pour que la grille ne se lise pas.
        /// </summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud,
            Color ca, Color cb, Color cc, Color cd, bool flip = false)
        {
            if (flip)
            {
                Triangle(a, b, d, ua, ub, ud, ca, cb, cd);
                Triangle(b, c, d, ub, uc, ud, cb, cc, cd);
                return;
            }
            Triangle(a, b, c, ua, ub, uc, ca, cb, cc);
            Triangle(a, c, d, ua, uc, ud, ca, cc, cd);
        }

        private void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Color ca, Color cb, Color cc)
        {
            Vector3 normal = (c - a).Cross(b - a).Normalized();
            Add(a, ua, ca, normal);
            Add(b, ub, cb, normal);
            Add(c, uc, cc, normal);
        }

        private void Add(Vector3 p, Vector2 uv, Color color, Vector3 normal)
        {
            Vertices.Add(p);
            Uvs.Add(uv);
            Colors.Add(color);
            Normals.Add(normal);
        }
    }

    private static List<BaseMaterial3D> _materials;

    /// <summary>
    /// Habille le relief des sols d'une terre (plan 60 §22), avant le premier accès aux matériaux : le sol bas prend la
    /// matière du terrain le plus pesant de <c>data/biomes/&lt;terre&gt;.json</c>, les plateaux herbus la suivante, sans
    /// l'eau ni les tuiles qui ne sont pas une matière (fondations, routes). Textures de
    /// <c>tools/generate_relief_textures.py</c>, dans <c>assets/models/textures/ground</c>.
    /// </summary>
    public static void UseLand(string biome)
    {
        using FileAccess file = FileAccess.Open($"res://data/biomes/{biome}.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning($"[ReliefTexturedGround] Terre sans fiche : data/biomes/{biome}.json ; sol d'essai gardé");
            return;
        }
        Godot.Collections.Dictionary land = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        Godot.Collections.Dictionary sources = land["tile_sources"].AsGodotDictionary();
        List<(string Material, float Weight)> tops = new();
        foreach ((Variant terrain, Variant weight) in land["terrain_weights"].AsGodotDictionary())
        {
            if (terrain.AsString() == "water" || !sources.TryGetValue(terrain, out Variant tiles))
                continue;
            string material = MaterialOf(tiles.AsGodotArray()[0].AsString());
            if (material != null && FileAccess.FileExists($"{TextureDir}/ground/{material}.png") && !tops.Exists(t => t.Material == material))
                tops.Add((material, (float)weight.AsDouble()));
        }
        if (tops.Count == 0)
            return;
        tops.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        string ground = tops[0].Material;
        string terrace = tops.Count > 1 ? tops[1].Material : ground;
        TextureNames[(int)Layer.Ground] = $"ground/{ground}";
        TextureNames[(int)Layer.Terrace] = $"ground/{terrace}";
        TextureNames[(int)Layer.LipGround] = $"ground/frange_{ground}";
        TextureNames[(int)Layer.LipTerrace] = $"ground/frange_{terrace}";
        _materials = null;
        GD.Print($"[ReliefTexturedGround] {biome} : sol {ground}, plateaux {terrace}");
    }

    /// <summary>« foret/tile_foret_sol_w00 » → « foret_sol » ; null pour une tuile qui n'est pas une matière de Wang.</summary>
    private static string MaterialOf(string tile)
    {
        string name = tile.GetFile();
        int wang = name.LastIndexOf("_w", System.StringComparison.Ordinal);
        if (!name.StartsWith("tile_", System.StringComparison.Ordinal) || wang < 0 || !int.TryParse(name[(wang + 2)..], out _))
            return null;
        return name[5..wang];
    }

    /// <summary>Les matériaux du relief, partagés par les deux variantes, que le style toon règle comme les autres.</summary>
    public static IReadOnlyList<BaseMaterial3D> Materials => _materials ??= LoadMaterials();

    /// <param name="carvedRelief">Saillie des facettes des falaises taillées (cellules) ; 0 pour des falaises droites.</param>
    public static MeshInstance3D Build(ReliefArena arena, float carvedRelief)
    {
        Surface[] surfaces = new Surface[TextureNames.Length];
        for (int s = 0; s < surfaces.Length; s++)
            surfaces[s] = new Surface();
        for (int y = 0; y < arena.Depth; y++)
        {
            for (int x = 0; x < arena.Width; x++)
            {
                float h = arena.CellHeight(x, y);
                if (ReliefArena.IsVoid(h))
                    continue;
                Layer top = arena.IsRamp(x, y) ? Layer.Steps : TopLayer(arena, h);
                AddTop(surfaces[(int)top], arena, x, y);
                for (int side = 0; side < 4; side++)
                {
                    int dx = side == 0 ? 1 : side == 1 ? -1 : 0;
                    int dy = side == 2 ? 1 : side == 3 ? -1 : 0;
                    AddCliff(surfaces, arena, carvedRelief, top, x, y, h, dx, dy);
                }
            }
        }

        ArrayMesh mesh = new();
        int triangles = 0;
        for (int s = 0; s < surfaces.Length; s++)
        {
            Surface surface = surfaces[s];
            if (surface.Vertices.Count == 0)
                continue;
            Godot.Collections.Array arrays = new();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = surface.Vertices.ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = surface.Normals.ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = surface.Uvs.ToArray();
            arrays[(int)Mesh.ArrayType.Color] = surface.Colors.ToArray();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, Materials[s]);
            triangles += surface.Vertices.Count / 3;
        }
        string variant = carvedRelief > 0f ? "taillé" : "droit";
        GD.Print($"[ReliefTexturedGround] Relief texturé {variant} : {triangles} triangles, {mesh.GetSurfaceCount()} matières");
        return new MeshInstance3D { Name = carvedRelief > 0f ? "CarvedTerrain" : "TexturedTerrain", Mesh = mesh };
    }

    private static List<BaseMaterial3D> LoadMaterials()
    {
        List<BaseMaterial3D> materials = new();
        for (int s = 0; s < TextureNames.Length; s++)
            materials.Add(NewMaterial(TextureNames[s], s >= (int)Layer.LipGround));
        return materials;
    }

    /// <summary>
    /// Texture lue du PNG, sans passer par l'import : ses mipmaps se calculent ici, pour qu'au loin le motif se fonde
    /// au lieu de scintiller.
    /// </summary>
    private static StandardMaterial3D NewMaterial(string name, bool lip)
    {
        string file = name.Contains('/') ? name : $"relief_{name}";
        Image image = Image.LoadFromFile(ProjectSettings.GlobalizePath($"{TextureDir}/{file}.png"));
        image.GenerateMipmaps();
        StandardMaterial3D material = new()
        {
            AlbedoTexture = ImageTexture.CreateFromImage(image),
            VertexColorUseAsAlbedo = true,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmapsAnisotropic,
            Roughness = 1f,
        };
        if (lip)
        {
            material.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
            material.AlphaScissorThreshold = 0.5f;
        }
        return material;
    }

    private static Layer TopLayer(ReliefArena arena, float h)
    {
        float level = h / arena.LevelHeight;
        return Mathf.Abs(level - Mathf.Round(level)) > 0.01f ? Layer.Steps
            : level < 0.5f ? Layer.Ground
            : level < TerraceTopLevel ? Layer.Terrace
            : Layer.Butte;
    }

    /// <summary>Teinte d'un sommet : de larges plaques un peu plus claires ou plus sombres, comme les biomes de Minecraft.</summary>
    private static Color Tint(float x, float y, int salt)
    {
        float k = 0.86f + 0.24f * ReliefLowPoly.Noise(x, y, 2.6f, salt) + 0.06f * ReliefLowPoly.Noise(x, y, 0.9f, salt + 1);
        return new Color(k, k, k);
    }

    private static Vector2 TopUv(float x, float y) => new(x / CellsPerTexture, y / CellsPerTexture);

    /// <summary>Dessus de la cellule, en pente sur une rampe.</summary>
    private static void AddTop(Surface surface, ReliefArena arena, int x, int y)
    {
        Vector3 a = new(x, arena.CellSurface(x, y, x, y), y), b = new(x + 1, arena.CellSurface(x, y, x + 1, y), y);
        Vector3 c = new(x + 1, arena.CellSurface(x, y, x + 1, y + 1), y + 1), d = new(x, arena.CellSurface(x, y, x, y + 1), y + 1);
        surface.Quad(a, b, c, d, TopUv(x, y), TopUv(x + 1, y), TopUv(x + 1, y + 1), TopUv(x, y + 1),
            Tint(x, y, 31), Tint(x + 1, y, 31), Tint(x + 1, y + 1, 31), Tint(x, y + 1, 31));
    }

    /// <summary>
    /// Flanc vers le voisin (dx, dy) s'il est plus bas : terre en strates, roche sous la butte. Sa grille de rangées a une
    /// limite au bas de la frange d'herbe, pour que la frange en épouse exactement les facettes ; en version droite, les
    /// rangées ne servent qu'à faire plonger au noir les falaises du vide, sommet par sommet.
    /// </summary>
    private static void AddCliff(Surface[] surfaces, ReliefArena arena, float relief, Layer top, int x, int y, float h, int dx, int dy)
    {
        float neighbour = arena.CellHeight(x + dx, y + dy);
        bool intoVoid = ReliefArena.IsVoid(neighbour);
        (Vector2 p0, Vector2 p1) = ReliefLowPoly.CliffEdge(x, y, dx, dy);
        if (arena.IsRamp(x, y) || arena.IsRamp(x + dx, y + dy))
        {
            AddSlopedCliff(surfaces[(int)Layer.EarthSide], arena, x, y, dx, dy, intoVoid, p0, p1);
            return;
        }
        float bottom = intoVoid ? h - ReliefLowPoly.VoidDepth : neighbour;
        if (bottom >= h - 0.001f)
            return;
        bool carved = relief > 0f;
        bool grassy = top is Layer.Ground or Layer.Terrace;
        float lip = grassy ? Mathf.Min(LipHeight, h - bottom) : 0f;
        float rowHeight = carved ? CarvedRowHeight : intoVoid ? ReliefLowPoly.CliffRowHeight : float.PositiveInfinity;
        List<float> levels = RowLevels(h, bottom, lip, rowHeight, out int lipRows);
        int columns = carved ? CarvedColumns : 1;
        Vector3 outward = new(dx, 0f, dy);
        // Une extrémité ne bouge que si la paroi continue, plane, chez la cellule voisine : les coins restent droits.
        Vector2I along = new(Mathf.RoundToInt(p1.X - p0.X), Mathf.RoundToInt(p1.Y - p0.Y));
        bool startFree = carved && WallContinues(arena, x - along.X, y - along.Y, h, neighbour, dx, dy);
        bool endFree = carved && WallContinues(arena, x + along.X, y + along.Y, h, neighbour, dx, dy);
        int salt = 80 + (dx + 1) * 3 + dy + 1;
        float wall = dx != 0 ? p0.X : p0.Y;

        Vector3 Point(int i, int j)
        {
            Vector2 at = p0.Lerp(p1, i / (float)columns);
            Vector3 p = new(at.X, levels[j], at.Y);
            bool fixedPoint = !carved || j == 0 || j == levels.Count - 1 || (i == 0 && !startFree) || (i == columns && !endFree);
            if (fixedPoint)
                return p;
            float u = dx != 0 ? at.Y : at.X;
            // La saillie se lit le long de la paroi et en hauteur ; la paroi d'en face (autre graine) ne la répète pas.
            float bulge = 0.25f + 0.75f * ReliefLowPoly.Noise(u + wall * 7.31f, levels[j] * 1.7f, 0.45f, salt);
            return p + outward * relief * bulge;
        }

        Vector2 Uv(int i, int j)
        {
            Vector2 at = p0.Lerp(p1, i / (float)columns);
            return new Vector2((dx != 0 ? at.Y : at.X) / CellsPerTexture, -levels[j] / CellsPerTexture);
        }

        Color Shade(int i, int j) => SideTint(p0.Lerp(p1, i / (float)columns), h, levels[j], intoVoid);

        Surface side = surfaces[(int)(top == Layer.Butte ? Layer.RockSide : Layer.EarthSide)];
        for (int j = 0; j < levels.Count - 1; j++)
        {
            for (int i = 0; i < columns; i++)
            {
                bool flip = carved && ReliefLowPoly.Hash(Mathf.RoundToInt((p0.X + p1.X) * 4f) + i, Mathf.RoundToInt((p0.Y + p1.Y) * 4f) + j, salt) < 0.5f;
                side.Quad(Point(i, j), Point(i + 1, j), Point(i + 1, j + 1), Point(i, j + 1),
                    Uv(i, j), Uv(i + 1, j), Uv(i + 1, j + 1), Uv(i, j + 1),
                    Shade(i, j), Shade(i + 1, j), Shade(i + 1, j + 1), Shade(i, j + 1), flip);
                if (j >= lipRows)
                    continue;
                // Frange d'herbe : les mêmes facettes, un rien devant, jamais plus bas que le pied de la falaise.
                Surface fringe = surfaces[(int)(top == Layer.Ground ? Layer.LipGround : Layer.LipTerrace)];
                Vector3 off = outward * LipOffset;
                Vector2 Lip(int fi, int fj) => new(Uv(fi, fj).X, (h - levels[fj]) / LipHeight);
                Color tint0 = Tint(Point(i, j).X, Point(i, j).Z, 31), tint1 = Tint(Point(i + 1, j).X, Point(i + 1, j).Z, 31);
                fringe.Quad(Point(i, j) + off, Point(i + 1, j) + off, Point(i + 1, j + 1) + off, Point(i, j + 1) + off,
                    Lip(i, j), Lip(i + 1, j), Lip(i + 1, j + 1), Lip(i, j + 1), tint0, tint1, tint1, tint0, flip);
            }
        }
    }

    /// <summary>
    /// Flanc contre une rampe (§239) : son haut ou son pied suit la pente, d'un bout de l'arête à l'autre. Il reste droit,
    /// sans frange : ce sont les talus des cases de montée et les parois que longe une rampe.
    /// </summary>
    private static void AddSlopedCliff(Surface side, ReliefArena arena, int x, int y, int dx, int dy, bool intoVoid, Vector2 p0, Vector2 p1)
    {
        float top0 = arena.CellSurface(x, y, p0.X, p0.Y), top1 = arena.CellSurface(x, y, p1.X, p1.Y);
        float bottom0 = intoVoid ? top0 - ReliefLowPoly.VoidDepth : Mathf.Min(top0, arena.CellSurface(x + dx, y + dy, p0.X, p0.Y));
        float bottom1 = intoVoid ? top1 - ReliefLowPoly.VoidDepth : Mathf.Min(top1, arena.CellSurface(x + dx, y + dy, p1.X, p1.Y));
        float tallest = Mathf.Max(top0 - bottom0, top1 - bottom1);
        if (tallest <= 0.001f)
            return;
        int rows = intoVoid ? Mathf.CeilToInt(tallest / ReliefLowPoly.CliffRowHeight) : 1;
        float u0 = (dx != 0 ? p0.Y : p0.X) / CellsPerTexture, u1 = (dx != 0 ? p1.Y : p1.X) / CellsPerTexture;
        for (int j = 0; j < rows; j++)
        {
            float a0 = Mathf.Lerp(top0, bottom0, j / (float)rows), a1 = Mathf.Lerp(top1, bottom1, j / (float)rows);
            float b0 = Mathf.Lerp(top0, bottom0, (j + 1f) / rows), b1 = Mathf.Lerp(top1, bottom1, (j + 1f) / rows);
            side.Quad(new Vector3(p0.X, a0, p0.Y), new Vector3(p1.X, a1, p1.Y), new Vector3(p1.X, b1, p1.Y), new Vector3(p0.X, b0, p0.Y),
                new Vector2(u0, -a0 / CellsPerTexture), new Vector2(u1, -a1 / CellsPerTexture),
                new Vector2(u1, -b1 / CellsPerTexture), new Vector2(u0, -b0 / CellsPerTexture),
                SideTint(p0, top0, a0, intoVoid), SideTint(p1, top1, a1, intoVoid), SideTint(p1, top1, b1, intoVoid), SideTint(p0, top0, b0, intoVoid));
        }
    }

    /// <summary>
    /// Hauteurs des rangées d'une falaise, du haut au pied : d'abord celles de la frange, puis le reste, au plus
    /// <paramref name="rowHeight"/> chacune.
    /// </summary>
    private static List<float> RowLevels(float h, float bottom, float lip, float rowHeight, out int lipRows)
    {
        List<float> levels = new() { h };
        lipRows = lip > 0f ? Mathf.Max(1, Mathf.RoundToInt(lip / rowHeight)) : 0;
        for (int j = 1; j <= lipRows; j++)
            levels.Add(h - lip * j / lipRows);
        float start = h - lip, rest = start - bottom;
        if (rest > 0.001f)
        {
            int rows = float.IsPositiveInfinity(rowHeight) ? 1 : Mathf.Max(1, Mathf.CeilToInt(rest / rowHeight));
            for (int j = 1; j <= rows; j++)
                levels.Add(start - rest * j / rows);
        }
        return levels;
    }

    /// <summary>La cellule (cx, cy) porte-t-elle la même paroi, au même sommet et au même pied ?</summary>
    private static bool WallContinues(ReliefArena arena, int cx, int cy, float h, float neighbour, int dx, int dy)
    {
        if (arena.CellHeight(cx, cy) != h)
            return false;
        float beyond = arena.CellHeight(cx + dx, cy + dy);
        return ReliefArena.IsVoid(neighbour) ? ReliefArena.IsVoid(beyond) : beyond == neighbour;
    }

    private static Color SideTint(Vector2 p, float h, float at, bool intoVoid)
    {
        float k = 0.9f + 0.16f * ReliefLowPoly.Noise(p.X + at, p.Y - at, 2.2f, 41);
        if (intoVoid)
            k *= 1f - Mathf.Clamp((h - at) / ReliefLowPoly.VoidDepth * 1.4f, 0f, 1f);
        return new Color(k, k, k);
    }
}
