using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Rendu low poly du bac à sable de la verticalité (plan 60, lots 60.0b et 60.0d) : le relief de l'arène en couleurs,
/// en facettes ou lisse, les modèles exportés par <c>tools/export_lowpoly.py</c> et leurs poses, les décors semés. La
/// simulation ne change pas : le sol qu'elle connaît reste plat par cellule, les facettes ne s'en écartent que de
/// quelques centimètres.
/// </summary>
public static class ReliefLowPoly
{
    private const float VoidDepth = 4f;
    private const int CliffColumns = 3;
    private const float CliffRowHeight = 0.35f;

    /// <summary>Un modèle low poly : ses poses par animation, déjà habillées du matériau à couleurs de sommet.</summary>
    public sealed class Model
    {
        public readonly Dictionary<string, Mesh[]> Animations = new();
        public float Fps = 8f;
        public Aabb Bounds;

        public Mesh[] Animation(string name) =>
            Animations.TryGetValue(name, out Mesh[] frames) ? frames : Animations["idle"];

        public Mesh Frame(string name, float time)
        {
            Mesh[] frames = Animation(name);
            return frames[(int)(time * Fps) % frames.Length];
        }

        /// <summary>Pose choisie par avancement (0 au début, 1 à la fin), pour une animation qui ne boucle pas.</summary>
        public Mesh FrameAt(string name, float progress)
        {
            Mesh[] frames = Animation(name);
            return frames[Mathf.Clamp((int)(progress * frames.Length), 0, frames.Length - 1)];
        }
    }

    private static StandardMaterial3D _plain;
    private static StandardMaterial3D _glow;

    /// <summary>
    /// Couleur par sommet ; l'ombrage suit les normales du maillage (de face en low poly, lissées en mid poly). Les
    /// couleurs du .glb sont écrites en sRGB (plan 60 §8) : le matériau les prend telles quelles. Partagé par le relief
    /// et tous les modèles : l'ombrage toon se bascule ici, une fois.
    /// </summary>
    public static StandardMaterial3D Plain => _plain ??= new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 1f };

    private static StandardMaterial3D Glow => _glow ??= new StandardMaterial3D
    {
        VertexColorUseAsAlbedo = true,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
    };

    /// <summary>Le modèle existe-t-il (le mid poly n'est pas versionné et se génère à la demande) ?</summary>
    public static bool Exists(string path) => FileAccess.FileExists($"res://assets/models/{path}.json");

    /// <summary>
    /// Charge <c>res://assets/models/&lt;path&gt;.glb</c> et son manifeste ; les poses deviennent des maillages à poser.
    /// Le .glb est lu à l'exécution, sans passer par l'import de Godot : le mid poly, non versionné, n'a pas de .import.
    /// </summary>
    public static Model LoadModel(string path)
    {
        string root = $"res://assets/models/{path}";
        Model model = new();
        using FileAccess file = FileAccess.Open($"{root}.json", FileAccess.ModeFlags.Read);
        Godot.Collections.Dictionary manifest = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        model.Fps = (float)manifest["fps"].AsDouble();

        GltfDocument document = new();
        GltfState state = new();
        Error error = document.AppendFromFile(ProjectSettings.GlobalizePath($"{root}.glb"), state);
        if (error != Error.Ok)
            throw new System.IO.IOException($"{root}.glb : {error}");
        Node scene = document.GenerateScene(state);
        Dictionary<string, Mesh> byName = new();
        foreach (Node child in scene.FindChildren("*", "MeshInstance3D", true, false))
        {
            Mesh mesh = ((MeshInstance3D)child).Mesh;
            for (int s = 0; s < mesh.GetSurfaceCount(); s++)
            {
                bool emissive = mesh.SurfaceGetMaterial(s)?.ResourceName.Contains("emissive") ?? false;
                mesh.SurfaceSetMaterial(s, emissive ? Glow : Plain);
            }
            byName[child.Name] = mesh;
        }
        scene.Free();

        foreach ((Variant key, Variant value) in manifest["animations"].AsGodotDictionary())
        {
            Godot.Collections.Array names = value.AsGodotArray();
            Mesh[] frames = new Mesh[names.Count];
            for (int i = 0; i < names.Count; i++)
                frames[i] = byName[names[i].AsString()];
            model.Animations[key.AsString()] = frames;
        }
        model.Bounds = model.Animation("idle")[0].GetAabb();
        return model;
    }

    /// <summary>
    /// Le relief : dessus des cellules, puis falaises vers chaque voisin plus bas ou vers le vide. En facettes, une
    /// couleur par face et des coins bossués ; lisse, des cellules découpées en 3 × 3, à plat, dont la couleur ondule
    /// doucement de sommet en sommet, et des falaises droites.
    /// </summary>
    public static MeshInstance3D BuildTerrain(ReliefArena arena, ReliefConfig.LowPolyStyle style, bool facets)
    {
        List<Vector3> vertices = new();
        List<Color> colors = new();
        for (int y = 0; y < arena.Depth; y++)
        {
            for (int x = 0; x < arena.Width; x++)
            {
                float h = arena.CellHeight(x, y);
                if (ReliefArena.IsVoid(h))
                    continue;
                if (facets)
                    AddTop(vertices, colors, arena, style, x, y, h);
                else
                    AddSmoothTop(vertices, colors, arena, style, x, y, h);
                AddCliff(vertices, colors, arena, style, x, y, h, 1, 0, facets);
                AddCliff(vertices, colors, arena, style, x, y, h, 0, 1, facets);
                AddCliff(vertices, colors, arena, style, x, y, h, -1, 0, facets);
                AddCliff(vertices, colors, arena, style, x, y, h, 0, -1, facets);
            }
        }

        // Normale de face, répétée sur ses trois sommets : pas de lissage.
        Vector3[] normals = new Vector3[vertices.Count];
        for (int i = 0; i < vertices.Count; i += 3)
        {
            Vector3 n = (vertices[i + 2] - vertices[i]).Cross(vertices[i + 1] - vertices[i]).Normalized();
            normals[i] = normals[i + 1] = normals[i + 2] = n;
        }
        Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
        ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, Plain);
        GD.Print($"[ReliefLowPoly] Relief {(facets ? "en facettes" : "lisse")} : {vertices.Count / 3} triangles");
        return new MeshInstance3D { Name = facets ? "FacetTerrain" : "SmoothTerrain", Mesh = mesh };
    }

    private static Color TopColor(ReliefArena arena, ReliefConfig.LowPolyStyle style, float h)
    {
        float level = h / arena.LevelHeight;
        return Mathf.Abs(level - Mathf.Round(level)) > 0.01f ? style.Steps
            : level < 0.5f ? style.Ground
            : level < 1.5f ? style.Terrace
            : style.Butte;
    }

    /// <summary>Hachage stable d'un point de la grille, dans [0, 1[ : la même graine donne le même relief.</summary>
    private static float Hash(int x, int y, int salt)
    {
        uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(salt * 83492791);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return (h & 0xffffff) / (float)0x1000000;
    }

    private static Color Vary(Color color, float amount, float t)
    {
        float k = 1f + amount * (t * 2f - 1f);
        return new Color(color.R * k, color.G * k, color.B * k);
    }

    /// <summary>
    /// Hauteur d'un coin de cellule : bossué seulement si les quatre cellules qui le partagent sont au même étage, sinon
    /// il tient l'arête d'une paroi et ne bouge pas.
    /// </summary>
    private static float CornerHeight(ReliefArena arena, ReliefConfig.LowPolyStyle style, int vx, int vy, float h)
    {
        if (arena.CellHeight(vx - 1, vy - 1) != h || arena.CellHeight(vx, vy - 1) != h
            || arena.CellHeight(vx - 1, vy) != h || arena.CellHeight(vx, vy) != h)
            return h;
        return h + style.HeightJitter * (Hash(vx, vy, 1) * 2f - 1f);
    }

    private static void AddTop(List<Vector3> vertices, List<Color> colors, ReliefArena arena, ReliefConfig.LowPolyStyle style, int x, int y, float h)
    {
        Vector3 a = new(x, CornerHeight(arena, style, x, y, h), y);
        Vector3 b = new(x + 1, CornerHeight(arena, style, x + 1, y, h), y);
        Vector3 c = new(x + 1, CornerHeight(arena, style, x + 1, y + 1, h), y + 1);
        Vector3 d = new(x, CornerHeight(arena, style, x, y + 1, h), y + 1);
        Color baseColor = TopColor(arena, style, h);
        // La diagonale change d'une cellule à l'autre : la grille ne se lit pas en rangées.
        bool flip = Hash(x, y, 2) < 0.5f;
        Triangle(vertices, colors, a, b, flip ? d : c, Vary(baseColor, style.ShadeJitter, Hash(x, y, 3)));
        if (flip)
            Triangle(vertices, colors, b, c, d, Vary(baseColor, style.ShadeJitter, Hash(x, y, 4)));
        else
            Triangle(vertices, colors, a, c, d, Vary(baseColor, style.ShadeJitter, Hash(x, y, 4)));
    }

    private const int SmoothSubdivisions = 3;

    /// <summary>Bruit de valeur lissé, dans [0, 1], à l'échelle de <paramref name="scale"/> cellules.</summary>
    private static float Noise(float x, float y, float scale, int salt)
    {
        float fx = x / scale, fy = y / scale;
        int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
        float tx = fx - ix, ty = fy - iy;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float top = Mathf.Lerp(Hash(ix, iy, salt), Hash(ix + 1, iy, salt), tx);
        float bottom = Mathf.Lerp(Hash(ix, iy + 1, salt), Hash(ix + 1, iy + 1, salt), tx);
        return Mathf.Lerp(top, bottom, ty);
    }

    /// <summary>Couleur du sol lisse en un point : clair et sombre en larges plaques, avec des taches de terre sèche.</summary>
    private static Color SmoothGround(Color baseColor, ReliefConfig.LowPolyStyle style, float x, float y)
    {
        float light = Noise(x, y, 1.6f, 11) * 0.65f + Noise(x, y, 0.55f, 12) * 0.35f;
        Color color = baseColor * (1f - style.ShadeJitter * 1.6f + light * style.ShadeJitter * 3.2f);
        float dry = Mathf.SmoothStep(0.62f, 0.82f, Noise(x, y, 3.3f, 13));
        return color.Lerp(style.Steps, dry * 0.4f) with { A = 1f };
    }

    private static void AddSmoothTop(List<Vector3> vertices, List<Color> colors, ReliefArena arena, ReliefConfig.LowPolyStyle style, int x, int y, float h)
    {
        Color baseColor = TopColor(arena, style, h);
        const float cell = 1f / SmoothSubdivisions;
        for (int j = 0; j < SmoothSubdivisions; j++)
        {
            for (int i = 0; i < SmoothSubdivisions; i++)
            {
                float x0 = x + i * cell, y0 = y + j * cell, x1 = x0 + cell, y1 = y0 + cell;
                Vector3 a = new(x0, h, y0), b = new(x1, h, y0), c = new(x1, h, y1), d = new(x0, h, y1);
                Color ca = SmoothGround(baseColor, style, x0, y0), cb = SmoothGround(baseColor, style, x1, y0);
                Color cc = SmoothGround(baseColor, style, x1, y1), cd = SmoothGround(baseColor, style, x0, y1);
                Triangle(vertices, colors, a, b, c, ca, cb, cc);
                Triangle(vertices, colors, a, c, d, ca, cc, cd);
            }
        }
    }

    /// <summary>
    /// Falaise sur le côté (dx, dy) : une grille de facettes du dessus jusqu'au voisin plus bas, dont les sommets
    /// intérieurs ressortent un peu ; ses quatre arêtes restent droites, pour se raccorder aux falaises voisines.
    /// </summary>
    private static void AddCliff(List<Vector3> vertices, List<Color> colors, ReliefArena arena, ReliefConfig.LowPolyStyle style,
        int x, int y, float h, int dx, int dy, bool facets)
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
        Vector3 outward = new(dx, 0f, dy);
        int rows = Mathf.Max(1, Mathf.CeilToInt((h - bottom) / CliffRowHeight));
        Color lip = TopColor(arena, style, h) * 0.82f;

        Vector3 Point(int i, int j)
        {
            Vector2 along = p0.Lerp(p1, i / (float)CliffColumns);
            float height = Mathf.Lerp(h, bottom, j / (float)rows);
            Vector3 p = new(along.X, height, along.Y);
            if (facets && i > 0 && i < CliffColumns && j > 0 && j < rows)
            {
                int gx = Mathf.RoundToInt(along.X * CliffColumns);
                int gy = Mathf.RoundToInt(along.Y * CliffColumns);
                p += outward * style.CliffJitter * Hash(gx, gy, Mathf.RoundToInt(height * 20f));
            }
            return p;
        }

        for (int j = 0; j < rows; j++)
        {
            float faceHeight = Mathf.Lerp(h, bottom, (j + 0.5f) / rows);
            // Strates : la roche alterne tous les demi-étages ; la première rangée garde la couleur du dessus.
            int stratum = Mathf.FloorToInt(faceHeight / (arena.LevelHeight * 0.5f));
            Color rock = j == 0 && !intoVoid ? lip : (stratum & 1) == 0 ? style.Cliff : style.CliffDark;
            if (intoVoid)
                rock = rock.Lerp(Colors.Black, Mathf.Clamp((h - faceHeight) / VoidDepth * 1.4f, 0f, 1f));
            for (int i = 0; i < CliffColumns; i++)
            {
                Vector3 a = Point(i, j), b = Point(i + 1, j), c = Point(i + 1, j + 1), d = Point(i, j + 1);
                int gx = Mathf.RoundToInt((a.X + c.X) * CliffColumns);
                int gy = Mathf.RoundToInt((a.Z + c.Z) * CliffColumns);
                float shade = facets ? style.ShadeJitter : style.ShadeJitter * 0.4f;
                Triangle(vertices, colors, a, b, c, Vary(rock, shade, Hash(gx, gy, j * 2 + 5)));
                Triangle(vertices, colors, a, c, d, Vary(rock, shade, Hash(gx, gy, j * 2 + 6)));
            }
        }
    }

    private static void Triangle(List<Vector3> vertices, List<Color> colors, Vector3 a, Vector3 b, Vector3 c, Color color) =>
        Triangle(vertices, colors, a, b, c, color, color, color);

    private static void Triangle(List<Vector3> vertices, List<Color> colors, Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
    {
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        colors.Add(ca);
        colors.Add(cb);
        colors.Add(cc);
    }

    /// <summary>Un décor posé : son modèle et son chemin sous assets/models, son pied (cellules), son orientation.</summary>
    public readonly record struct Placement(Model Model, string Path, Vector2 At, float YawDegrees);

    /// <summary>
    /// Décors semés par graine sur les cellules plates, à l'écart du chemin <paramref name="keepClear"/> et du départ, plus
    /// les repères posés à la main. Chaque emprise devient un obstacle de la simulation.
    /// </summary>
    public static List<Placement> PlaceProps(ReliefArena arena, ReliefConfig.LowPolyStyle style, ReliefSim sim,
        IReadOnlyList<Vector2> keepClear, Vector2 spawn, ulong seed)
    {
        List<Placement> placed = new();
        float cellsPerMetre = 1f / style.MetresPerCell;
        foreach (ReliefConfig.LowPolyLandmark landmark in style.Landmarks)
        {
            Model model = LoadModel(landmark.Model);
            placed.Add(new Placement(model, landmark.Model, landmark.At, landmark.YawDegrees));
            AddFootprint(sim, model.Bounds, landmark.At, landmark.YawDegrees, cellsPerMetre);
        }

        RandomNumberGenerator rng = new() { Seed = seed };
        foreach (ReliefConfig.LowPolyScatter scatter in style.Scatter)
        {
            Model model = LoadModel(scatter.Model);
            for (int y = 0; y < arena.Depth; y++)
            {
                for (int x = 0; x < arena.Width; x++)
                {
                    if (rng.Randf() >= scatter.Density)
                        continue;
                    Vector2 at = new(x + rng.RandfRange(0.25f, 0.75f), y + rng.RandfRange(0.25f, 0.75f));
                    if (!IsFlatInterior(arena, x, y) || at.DistanceTo(spawn) < 2.5f || DistanceToPath(at, keepClear) < 1.4f
                        || Crowded(placed, at, cellsPerMetre))
                        continue;
                    placed.Add(new Placement(model, scatter.Model, at, rng.RandfRange(0f, 360f)));
                    sim.AddObstacle(at, scatter.Radius);
                }
            }
        }
        sim.FinishObstacles();
        return placed;
    }

    /// <summary>Emprise rectangulaire d'un repère, en chapelet de cercles le long de son grand côté (yaw multiple de 90°).</summary>
    private static void AddFootprint(ReliefSim sim, Aabb bounds, Vector2 at, float yawDegrees, float cellsPerMetre)
    {
        bool turned = Mathf.Abs(Mathf.Sin(Mathf.DegToRad(yawDegrees))) > 0.5f;
        float sizeX = (turned ? bounds.Size.Z : bounds.Size.X) * cellsPerMetre;
        float sizeY = (turned ? bounds.Size.X : bounds.Size.Z) * cellsPerMetre;
        bool alongX = sizeX >= sizeY;
        float radius = Mathf.Min(sizeX, sizeY) * 0.5f;
        float length = Mathf.Max(sizeX, sizeY);
        int count = Mathf.Max(1, Mathf.CeilToInt(length / radius));
        for (int i = 0; i < count; i++)
        {
            float offset = count == 1 ? 0f : Mathf.Lerp(-length * 0.5f + radius, length * 0.5f - radius, i / (float)(count - 1));
            sim.AddObstacle(at + (alongX ? new Vector2(offset, 0f) : new Vector2(0f, offset)), radius);
        }
    }

    private static bool IsFlatInterior(ReliefArena arena, int x, int y)
    {
        float h = arena.CellHeight(x, y);
        if (ReliefArena.IsVoid(h) || Mathf.Abs(h / arena.LevelHeight - Mathf.Round(h / arena.LevelHeight)) > 0.01f)
            return false;
        return arena.CellHeight(x + 1, y) == h && arena.CellHeight(x - 1, y) == h
            && arena.CellHeight(x, y + 1) == h && arena.CellHeight(x, y - 1) == h;
    }

    private static float DistanceToPath(Vector2 at, IReadOnlyList<Vector2> path)
    {
        float best = float.MaxValue;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 a = path[i];
            Vector2 b = path[(i + 1) % path.Count];
            Vector2 ab = b - a;
            float t = ab.LengthSquared() > 0f ? Mathf.Clamp((at - a).Dot(ab) / ab.LengthSquared(), 0f, 1f) : 0f;
            best = Mathf.Min(best, at.DistanceTo(a + ab * t));
        }
        return best;
    }

    /// <summary>Un décor ne se pose pas dans l'emprise d'un autre (la moitié de leurs largeurs cumulées).</summary>
    private static bool Crowded(List<Placement> placed, Vector2 at, float cellsPerMetre)
    {
        foreach (Placement other in placed)
        {
            float reach = Mathf.Max(other.Model.Bounds.Size.X, other.Model.Bounds.Size.Z) * cellsPerMetre * 0.5f;
            if (at.DistanceTo(other.At) < reach + 0.6f)
                return true;
        }
        return false;
    }
}
