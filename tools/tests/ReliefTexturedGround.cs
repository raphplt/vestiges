using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Le relief texturé du bac à sable (plan 60, §235), à la façon de Minecraft et de Megabonk : la géométrie du relief lisse,
/// couverte de textures pixel art (tools/generate_relief_textures.py) lues au plus proche. Les coordonnées de texture
/// sont celles du monde, une texture pour 4 × 4 cellules : le motif court d'une cellule à l'autre et d'une falaise à
/// l'autre sans couture, les strates des flancs restent à la même hauteur partout. Une frange d'herbe déborde du haut des
/// falaises herbues ; la teinte des sommets ondule en larges plaques ; les falaises du vide plongent au noir.
/// </summary>
public static class ReliefTexturedGround
{
    private const string TextureDir = "res://assets/models/textures";
    private const float CellsPerTexture = 4f;
    // Hauteur de la frange (cellules) : ses 16 px à la densité des dessus, 32 px par cellule.
    private const float LipHeight = 0.5f;
    // La frange se tient un rien devant la falaise, sans s'y mêler à la profondeur.
    private const float LipOffset = 0.004f;

    private enum Layer { Ground, Terrace, Butte, Steps, EarthSide, RockSide, LipGround, LipTerrace }

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

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud,
            Color ca, Color cb, Color cc, Color cd)
        {
            Vector3 normal = (c - a).Cross(b - a).Normalized();
            Add(a, ua, ca, normal);
            Add(b, ub, cb, normal);
            Add(c, uc, cc, normal);
            Add(a, ua, ca, normal);
            Add(c, uc, cc, normal);
            Add(d, ud, cd, normal);
        }

        private void Add(Vector3 p, Vector2 uv, Color color, Vector3 normal)
        {
            Vertices.Add(p);
            Uvs.Add(uv);
            Colors.Add(color);
            Normals.Add(normal);
        }
    }

    /// <summary>Les matériaux du relief, que le style toon règle comme les autres.</summary>
    public static IReadOnlyList<BaseMaterial3D> Materials => _materials;

    private static readonly List<BaseMaterial3D> _materials = new();

    public static bool Available()
    {
        foreach (string name in TextureNames)
        {
            if (!FileAccess.FileExists($"{TextureDir}/relief_{name}.png"))
                return false;
        }
        return true;
    }

    public static MeshInstance3D Build(ReliefArena arena)
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
                Layer top = TopLayer(arena, h);
                AddTop(surfaces[(int)top], x, y, h);
                for (int side = 0; side < 4; side++)
                {
                    int dx = side == 0 ? 1 : side == 1 ? -1 : 0;
                    int dy = side == 2 ? 1 : side == 3 ? -1 : 0;
                    AddCliff(surfaces, arena, top, x, y, h, dx, dy);
                }
            }
        }

        ArrayMesh mesh = new();
        _materials.Clear();
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
            StandardMaterial3D material = NewMaterial(TextureNames[s], s >= (int)Layer.LipGround);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, material);
            _materials.Add(material);
            triangles += surface.Vertices.Count / 3;
        }
        GD.Print($"[ReliefTexturedGround] Relief texturé : {triangles} triangles, {mesh.GetSurfaceCount()} matières");
        return new MeshInstance3D { Name = "TexturedTerrain", Mesh = mesh };
    }

    /// <summary>
    /// Texture lue du PNG, sans passer par l'import : ses mipmaps se calculent ici, pour qu'au loin le motif se fonde
    /// au lieu de scintiller.
    /// </summary>
    private static StandardMaterial3D NewMaterial(string name, bool lip)
    {
        Image image = Image.LoadFromFile(ProjectSettings.GlobalizePath($"{TextureDir}/relief_{name}.png"));
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
            : level < 1.5f ? Layer.Terrace
            : Layer.Butte;
    }

    /// <summary>Teinte d'un sommet : de larges plaques un peu plus claires ou plus sombres, comme les biomes de Minecraft.</summary>
    private static Color Tint(float x, float y, int salt)
    {
        float k = 0.86f + 0.24f * ReliefLowPoly.Noise(x, y, 2.6f, salt) + 0.06f * ReliefLowPoly.Noise(x, y, 0.9f, salt + 1);
        return new Color(k, k, k);
    }

    private static Vector2 TopUv(float x, float y) => new(x / CellsPerTexture, y / CellsPerTexture);

    private static void AddTop(Surface surface, int x, int y, float h)
    {
        Vector3 a = new(x, h, y), b = new(x + 1, h, y), c = new(x + 1, h, y + 1), d = new(x, h, y + 1);
        surface.Quad(a, b, c, d, TopUv(x, y), TopUv(x + 1, y), TopUv(x + 1, y + 1), TopUv(x, y + 1),
            Tint(x, y, 31), Tint(x + 1, y, 31), Tint(x + 1, y + 1, 31), Tint(x, y + 1, 31));
    }

    /// <summary>
    /// Flanc vers le voisin (dx, dy) s'il est plus bas : terre en strates, roche sous la butte. Les rangées ne servent
    /// qu'à faire plonger au noir les falaises du vide, sommet par sommet.
    /// </summary>
    private static void AddCliff(Surface[] surfaces, ReliefArena arena, Layer top, int x, int y, float h, int dx, int dy)
    {
        float neighbour = arena.CellHeight(x + dx, y + dy);
        bool intoVoid = ReliefArena.IsVoid(neighbour);
        float bottom = intoVoid ? h - ReliefLowPoly.VoidDepth : neighbour;
        if (bottom >= h - 0.001f)
            return;
        (Vector2 p0, Vector2 p1) = ReliefLowPoly.CliffEdge(x, y, dx, dy);
        // Le long d'une falaise, u suit la coordonnée du monde qui varie : le motif court d'une cellule à la suivante.
        float u0 = (dx != 0 ? p0.Y : p0.X) / CellsPerTexture;
        float u1 = (dx != 0 ? p1.Y : p1.X) / CellsPerTexture;
        Surface side = surfaces[(int)(top == Layer.Butte ? Layer.RockSide : Layer.EarthSide)];
        int rows = intoVoid ? Mathf.CeilToInt((h - bottom) / ReliefLowPoly.CliffRowHeight) : 1;
        for (int j = 0; j < rows; j++)
        {
            float yTop = Mathf.Lerp(h, bottom, j / (float)rows);
            float yBottom = Mathf.Lerp(h, bottom, (j + 1f) / rows);
            Color cTop = SideTint(p0, p1, h, yTop, intoVoid, 0f), cTop1 = SideTint(p0, p1, h, yTop, intoVoid, 1f);
            Color cBottom = SideTint(p0, p1, h, yBottom, intoVoid, 0f), cBottom1 = SideTint(p0, p1, h, yBottom, intoVoid, 1f);
            side.Quad(new Vector3(p0.X, yTop, p0.Y), new Vector3(p1.X, yTop, p1.Y), new Vector3(p1.X, yBottom, p1.Y), new Vector3(p0.X, yBottom, p0.Y),
                new Vector2(u0, -yTop / CellsPerTexture), new Vector2(u1, -yTop / CellsPerTexture),
                new Vector2(u1, -yBottom / CellsPerTexture), new Vector2(u0, -yBottom / CellsPerTexture),
                cTop, cTop1, cBottom1, cBottom);
        }

        if (top is not (Layer.Ground or Layer.Terrace))
            return;
        // Frange d'herbe : elle pend du rebord, jamais plus bas que le pied de la falaise.
        float lip = Mathf.Min(LipHeight, h - bottom);
        Vector3 outward = new Vector3(dx, 0f, dy) * LipOffset;
        Surface fringe = surfaces[(int)(top == Layer.Ground ? Layer.LipGround : Layer.LipTerrace)];
        float v = lip / LipHeight;
        fringe.Quad(new Vector3(p0.X, h, p0.Y) + outward, new Vector3(p1.X, h, p1.Y) + outward,
            new Vector3(p1.X, h - lip, p1.Y) + outward, new Vector3(p0.X, h - lip, p0.Y) + outward,
            new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, v), new Vector2(u0, v),
            Tint(p0.X, p0.Y, 31), Tint(p1.X, p1.Y, 31), Tint(p1.X, p1.Y, 31), Tint(p0.X, p0.Y, 31));
    }

    private static Color SideTint(Vector2 p0, Vector2 p1, float h, float at, bool intoVoid, float end)
    {
        Vector2 p = p0.Lerp(p1, end);
        float k = 0.9f + 0.16f * ReliefLowPoly.Noise(p.X + at, p.Y - at, 2.2f, 41);
        if (intoVoid)
            k *= 1f - Mathf.Clamp((h - at) / ReliefLowPoly.VoidDepth * 1.4f, 0f, 1f);
        return new Color(k, k, k);
    }
}
