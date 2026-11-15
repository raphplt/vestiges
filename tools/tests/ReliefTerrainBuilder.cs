using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Maillage du relief du bac à sable (plan 59) : le dessus de chaque cellule porte une vraie tuile losange du jeu (ses
/// coins tombent sur les pointes du losange, donc la vue orthographique la rend pixel pour pixel), et chaque marche
/// descend en falaise jusqu'au voisin, ou s'enfonce et noircit dans le vide. Une surface par texture. Avec la photographie
/// d'un vrai biome, tous les dessus prennent son sol, à l'endroit exact où le jeu le dessine, et la roche des falaises
/// prend sa teinte.
/// </summary>
public static class ReliefTerrainBuilder
{
    private const float VoidDepth = 4f;
    // Ombrage fixe des faces selon leur orientation : la vue 2D n'a pas de lumière, la 3D le garde comme occlusion.
    private const float FaceEast = 1f;
    private const float FaceSouth = 0.7f;
    private const float FaceWest = 0.85f;
    private const float FaceNorth = 0.6f;

    public static MeshInstance3D Build(ReliefArena arena, ReliefConfig config, bool shaded, ReliefBake bake)
    {
        Dictionary<string, SurfaceTool> tops = new();
        const string bakedKey = "baked";
        Vector2 bakedSize = bake == null ? Vector2.One : bake.Ground.GetSize();
        SurfaceTool cliffs = new();
        cliffs.Begin(Mesh.PrimitiveType.Triangles);
        for (int y = 0; y < arena.Depth; y++)
        {
            for (int x = 0; x < arena.Width; x++)
            {
                float h = arena.CellHeight(x, y);
                if (ReliefArena.IsVoid(h))
                    continue;
                string texture = bake == null ? TileFor(config, arena, h, x, y) : bakedKey;
                if (!tops.TryGetValue(texture, out SurfaceTool top))
                {
                    top = new SurfaceTool();
                    top.Begin(Mesh.PrimitiveType.Triangles);
                    tops[texture] = top;
                }
                if (bake == null)
                    AddTop(top, x, y, h);
                else
                    AddBakedTop(top, x, y, h, arena.Depth, bakedSize);
                AddCliff(cliffs, arena, x, y, h, 1, 0, FaceEast);
                AddCliff(cliffs, arena, x, y, h, 0, 1, FaceSouth);
                AddCliff(cliffs, arena, x, y, h, -1, 0, FaceWest);
                AddCliff(cliffs, arena, x, y, h, 0, -1, FaceNorth);
            }
        }

        ArrayMesh mesh = new();
        foreach ((string path, SurfaceTool top) in tops)
        {
            top.GenerateNormals();
            top.Commit(mesh);
            Texture2D texture = path == bakedKey ? BakedTexture(bake.Ground, shaded) : GD.Load<Texture2D>($"res://{path}");
            StandardMaterial3D material = Material(texture, shaded, false);
            // En 3D, le sol vu de loin et de biais se lisse par ses mipmaps au lieu de fourmiller.
            if (path == bakedKey && shaded)
                material.TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps;
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, material);
        }
        cliffs.GenerateNormals();
        cliffs.Commit(mesh);
        mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, Material(CliffTexture(bake?.Ground), shaded, true));
        return new MeshInstance3D { Name = "Terrain", Mesh = mesh };
    }

    private static string TileFor(ReliefConfig config, ReliefArena arena, float h, int x, int y)
    {
        float level = h / arena.LevelHeight;
        string[] set = Mathf.Abs(level - Mathf.Round(level)) > 0.01f ? config.StepTiles
            : level < 0.5f ? config.GroundTiles
            : level < 1.5f ? config.TerraceTiles
            : config.ButteTiles;
        // Variante tirée par cellule, stable d'une partie à l'autre.
        int hash = (x * 73856093) ^ (y * 19349663);
        return set[(hash & 0x7fffffff) % 7 == 0 ? set.Length - 1 : 0];
    }

    /// <summary>Dessus de cellule : les coins (x,y), (x+1,y), (x+1,y+1), (x,y+1) prennent les pointes haute, droite, basse et gauche du losange.</summary>
    private static void AddTop(SurfaceTool tool, int x, int y, float h)
    {
        Vector3 a = new(x, h, y);
        Vector3 b = new(x + 1, h, y);
        Vector3 c = new(x + 1, h, y + 1);
        Vector3 d = new(x, h, y + 1);
        Vector2 ua = new(0.5f, 0f), ub = new(1f, 0.5f), uc = new(0.5f, 1f), ud = new(0f, 0.5f);
        Vertex(tool, a, ua, Colors.White);
        Vertex(tool, b, ub, Colors.White);
        Vertex(tool, c, uc, Colors.White);
        Vertex(tool, a, ua, Colors.White);
        Vertex(tool, c, uc, Colors.White);
        Vertex(tool, d, ud, Colors.White);
    }

    /// <summary>Dessus pris dans le sol photographié : chaque coin retombe sur le pixel où le jeu dessine ce point du sol.</summary>
    private static void AddBakedTop(SurfaceTool tool, int x, int y, float h, int depth, Vector2 size)
    {
        Vector2 Uv(int u, int v) => new(((u - v) * 32f + depth * 32f) / size.X, (u + v) * 16f / size.Y);
        Vertex(tool, new Vector3(x, h, y), Uv(x, y), Colors.White);
        Vertex(tool, new Vector3(x + 1, h, y), Uv(x + 1, y), Colors.White);
        Vertex(tool, new Vector3(x + 1, h, y + 1), Uv(x + 1, y + 1), Colors.White);
        Vertex(tool, new Vector3(x, h, y), Uv(x, y), Colors.White);
        Vertex(tool, new Vector3(x + 1, h, y + 1), Uv(x + 1, y + 1), Colors.White);
        Vertex(tool, new Vector3(x, h, y + 1), Uv(x, y + 1), Colors.White);
    }

    private static ImageTexture BakedTexture(Image ground, bool mipmaps)
    {
        if (!mipmaps)
            return ImageTexture.CreateFromImage(ground);
        Image copy = (Image)ground.Duplicate();
        copy.GenerateMipmaps();
        return ImageTexture.CreateFromImage(copy);
    }

    /// <summary>Falaise sur le côté (dx, dy) de la cellule, du dessus jusqu'au voisin plus bas ; dans le vide, elle s'enfonce et noircit.</summary>
    private static void AddCliff(SurfaceTool tool, ReliefArena arena, int x, int y, float h, int dx, int dy, float shade)
    {
        float neighbour = arena.CellHeight(x + dx, y + dy);
        bool intoVoid = ReliefArena.IsVoid(neighbour);
        float bottom = intoVoid ? h - VoidDepth : neighbour;
        if (bottom >= h - 0.001f)
            return;
        // Arête commune, parcourue dans le sens qui tourne la face vers l'extérieur.
        Vector2 p0, p1;
        if (dx == 1) { p0 = new Vector2(x + 1, y + 1); p1 = new Vector2(x + 1, y); }
        else if (dx == -1) { p0 = new Vector2(x, y); p1 = new Vector2(x, y + 1); }
        else if (dy == 1) { p0 = new Vector2(x, y + 1); p1 = new Vector2(x + 1, y + 1); }
        else { p0 = new Vector2(x + 1, y); p1 = new Vector2(x, y); }

        // Texture de falaise répétée : une hauteur de texture par étage, une largeur par cellule.
        float vTop = 0f;
        float vBottom = (h - bottom) / arena.LevelHeight;
        Color top = new(shade, shade, shade);
        Color low = intoVoid ? Colors.Black : top;
        Vector3 a = new(p0.X, h, p0.Y);
        Vector3 b = new(p1.X, h, p1.Y);
        Vector3 c = new(p1.X, bottom, p1.Y);
        Vector3 d = new(p0.X, bottom, p0.Y);
        Vertex(tool, a, new Vector2(0f, vTop), top);
        Vertex(tool, b, new Vector2(1f, vTop), top);
        Vertex(tool, c, new Vector2(1f, vBottom), low);
        Vertex(tool, a, new Vector2(0f, vTop), top);
        Vertex(tool, c, new Vector2(1f, vBottom), low);
        Vertex(tool, d, new Vector2(0f, vBottom), low);
    }

    private static void Vertex(SurfaceTool tool, Vector3 at, Vector2 uv, Color color)
    {
        tool.SetColor(color);
        tool.SetUV(uv);
        tool.AddVertex(at);
    }

    private static StandardMaterial3D Material(Texture2D texture, bool shaded, bool repeat)
    {
        return new StandardMaterial3D
        {
            AlbedoTexture = texture,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            TextureRepeat = repeat,
            VertexColorUseAsAlbedo = true,
            ShadingMode = shaded ? BaseMaterial3D.ShadingModeEnum.PerPixel : BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Back,
            Roughness = 1f,
        };
    }

    /// <summary>
    /// Roche en strates, 32 px de large (une cellule de face) sur 28 px de haut (un étage) : liseré clair en haut, bandes
    /// sombres, grain. Avec un sol photographié, la roche prend sa teinte moyenne, assombrie et tirée vers la terre. Faite
    /// ici pour l'essai ; une vraie falaise dessinée viendrait si le relief est gardé.
    /// </summary>
    private static ImageTexture CliffTexture(Image ground)
    {
        const int width = 32;
        const int height = 28;
        Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        Color rim = new("8a7a62");
        Color rock = new("5b4a3c");
        if (ground != null)
        {
            Color mean = MeanColor(ground);
            rim = mean.Lerp(new Color("8a7a62"), 0.35f) * 0.95f;
            rock = mean.Lerp(new Color("4a3b30"), 0.55f) * 0.8f;
            rim.A = rock.A = 1f;
        }
        Color dark = rock.Darkened(0.32f);
        Color seam = rock.Darkened(0.5f);
        RandomNumberGenerator rng = new() { Seed = 59 };
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = y < 2 ? rim : y == 2 ? dark : (y + (x / 9 % 2)) % 9 == 0 ? seam : rock;
                if (y > 2 && rng.Randf() < 0.18f)
                    c = c.Lerp(dark, 0.6f);
                if (y > 2 && rng.Randf() < 0.05f)
                    c = c.Lerp(rim, 0.5f);
                image.SetPixel(x, y, c);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>Teinte moyenne des pixels non noirs du sol (le vide hors de la terre ne compte pas).</summary>
    private static Color MeanColor(Image ground)
    {
        Vector3 sum = Vector3.Zero;
        int count = 0;
        for (int y = 0; y < ground.GetHeight(); y += 7)
        {
            for (int x = 0; x < ground.GetWidth(); x += 7)
            {
                Color c = ground.GetPixel(x, y);
                if (c.R + c.G + c.B < 0.08f)
                    continue;
                sum += new Vector3(c.R, c.G, c.B);
                count++;
            }
        }
        if (count == 0)
            return new Color("5b4a3c");
        sum /= count;
        return new Color(sum.X, sum.Y, sum.Z);
    }
}
