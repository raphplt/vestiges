using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Touffes d'herbe en relief sur le sol texturé (plan 60, §236) : ce que fait Megabonk, des textures très simples et du
/// détail posé dessus en volume. Une seule touffe de quelques brins, répétée par un <see cref="MultiMesh"/> (un appel de
/// dessin pour toutes), semée par graine en bouquets épars sur les cellules d'herbe (§237), à l'écart des rebords ; sa
/// teinte suit celle du sol.
/// </summary>
public static class ReliefGrass
{
    /// <summary>
    /// Le matériau des touffes, que le style toon règle comme les autres. Passé après les opaques, sans écrire la
    /// profondeur : les contours, qui la lisent, ne voient pas l'herbe (un brin fin n'y serait qu'un trait noir), et le
    /// relief qui la cache la cache toujours.
    /// </summary>
    public static StandardMaterial3D Material { get; } = new()
    {
        VertexColorUseAsAlbedo = true,
        Roughness = 1f,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
    };

    public static MultiMeshInstance3D Build(ReliefArena arena, ReliefConfig.LowPolyStyle style, int seed)
    {
        RandomNumberGenerator rng = new() { Seed = (ulong)seed };
        List<Transform3D> transforms = new();
        List<Color> colors = new();
        int perCell = Mathf.RoundToInt(style.GrassPerCell);
        for (int y = 0; y < arena.Depth; y++)
        {
            for (int x = 0; x < arena.Width; x++)
            {
                float h = arena.CellHeight(x, y);
                if (!IsGrass(arena, h))
                    continue;
                Color tone = h / arena.LevelHeight < 0.5f ? style.Ground : style.Terrace;
                for (int k = 0; k < perCell; k++)
                {
                    Vector2 at = new(x + rng.Randf(), y + rng.Randf());
                    // L'herbe pousse en bouquets épars, comme chez Megabonk, pas en tapis : de larges plaques nues entre eux.
                    if (rng.Randf() > Mathf.SmoothStep(0.4f, 0.7f, ReliefLowPoly.Noise(at.X, at.Y, 3.5f, 53)))
                        continue;
                    // Pas de touffe qui flotte au-dessus d'une falaise ni qui pousse dans la cellule voisine plus haute.
                    if (arena.HighestUnder(at.X, at.Y, 0.12f) != h || LowerAround(arena, at, 0.1f, h))
                        continue;
                    float size = rng.RandfRange(style.GrassHeight * 0.6f, style.GrassHeight * 1.3f);
                    Basis basis = new Basis(Vector3.Up, rng.Randf() * Mathf.Tau).Scaled(new Vector3(size, size, size));
                    transforms.Add(new Transform3D(basis, new Vector3(at.X, h, at.Y)));
                    // Même plaques que la teinte du sol texturé, un peu plus claires : l'herbe se détache du sol.
                    float k2 = 0.95f + 0.25f * ReliefLowPoly.Noise(at.X, at.Y, 2.6f, 31) + rng.RandfRange(-0.06f, 0.06f);
                    colors.Add(new Color(tone.R * k2, tone.G * k2, tone.B * k2));
                }
            }
        }

        MultiMesh multimesh = new()
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = TuftMesh(),
            InstanceCount = transforms.Count,
        };
        for (int i = 0; i < transforms.Count; i++)
        {
            multimesh.SetInstanceTransform(i, transforms[i]);
            multimesh.SetInstanceColor(i, colors[i]);
        }
        GD.Print($"[ReliefGrass] {transforms.Count} touffes");
        return new MultiMeshInstance3D
        {
            Name = "Grass",
            Multimesh = multimesh,
            MaterialOverride = Material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static bool IsGrass(ReliefArena arena, float h)
    {
        if (ReliefArena.IsVoid(h))
            return false;
        float level = h / arena.LevelHeight;
        return Mathf.Abs(level - Mathf.Round(level)) < 0.01f && level < 1.5f;
    }

    private static bool LowerAround(ReliefArena arena, Vector2 at, float margin, float h) =>
        arena.GroundAt(at.X + margin, at.Y) < h || arena.GroundAt(at.X - margin, at.Y) < h
        || arena.GroundAt(at.X, at.Y + margin) < h || arena.GroundAt(at.X, at.Y - margin) < h;

    /// <summary>
    /// Une touffe d'une unité de haut : neuf brins larges couchés vers l'extérieur, à peine plus sombres au pied (une touffe
    /// basse et étalée, comme celles de Megabonk, plutôt qu'un épi dressé). Les
    /// normales pointent vers le ciel, pour que l'herbe prenne la lumière du sol et ne noircisse pas de dos.
    /// </summary>
    private static ArrayMesh TuftMesh()
    {
        const int blades = 9;
        List<Vector3> vertices = new();
        List<Color> colors = new();
        List<Vector3> normals = new();
        Color root = new(0.68f, 0.7f, 0.64f);
        Color tip = new(1f, 1f, 0.92f);
        for (int b = 0; b < blades; b++)
        {
            float angle = b * Mathf.Tau / blades + (b % 2) * 0.4f;
            Vector3 outward = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 side = new(-outward.Z, 0f, outward.X);
            float spread = b == 0 ? 0f : 0.2f + 0.1f * (b % 3);
            float height = 0.55f + 0.45f * ((b * 5) % 7) / 6f;
            Vector3 foot = outward * spread * 0.4f;
            Vector3 point = outward * (spread + 0.45f) + Vector3.Up * height;
            const float width = 0.13f;
            vertices.Add(foot - side * width);
            vertices.Add(point);
            vertices.Add(foot + side * width);
            colors.Add(root);
            colors.Add(tip);
            colors.Add(root);
            normals.Add(Vector3.Up);
            normals.Add(Vector3.Up);
            normals.Add(Vector3.Up);
        }
        Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
        ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
