using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Rendu low poly du bac à sable de la verticalité (plan 60, lots 60.0b et 60.0d) : les modèles exportés par
/// <c>tools/export_lowpoly.py</c> et leurs poses, les décors semés, et le bruit qui varie le relief texturé
/// (<see cref="ReliefTexturedGround"/>). Les sols en facettes et lisse sont retirés (§238).
/// </summary>
public static class ReliefLowPoly
{
    internal const float VoidDepth = 4f;
    internal const float CliffRowHeight = 0.35f;

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
    /// couleurs du .glb sont écrites en sRGB (plan 60 §8) : le matériau les prend telles quelles. Partagé par tous les
    /// modèles : l'ombrage toon se bascule ici, une fois.
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

    /// <summary>Hachage stable d'un point de la grille, dans [0, 1[ : la même graine donne le même relief.</summary>
    internal static float Hash(int x, int y, int salt)
    {
        uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(salt * 83492791);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return (h & 0xffffff) / (float)0x1000000;
    }

    /// <summary>Bruit de valeur lissé, dans [0, 1], à l'échelle de <paramref name="scale"/> cellules.</summary>
    internal static float Noise(float x, float y, float scale, int salt)
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

    /// <summary>Arête commune d'une cellule et de son voisin (dx, dy), parcourue dans le sens qui tourne la falaise vers l'extérieur.</summary>
    internal static (Vector2 From, Vector2 To) CliffEdge(int x, int y, int dx, int dy)
    {
        if (dx == 1)
            return (new Vector2(x + 1, y + 1), new Vector2(x + 1, y));
        if (dx == -1)
            return (new Vector2(x, y), new Vector2(x, y + 1));
        if (dy == 1)
            return (new Vector2(x, y + 1), new Vector2(x + 1, y + 1));
        return (new Vector2(x + 1, y), new Vector2(x, y));
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
