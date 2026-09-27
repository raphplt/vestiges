using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Dessine les chemins de terre (plan 10 T3) : un ruban maillé par tronçon, à largeur constante au sol
/// (donc deux fois moins épais à l'écran quand il file vers la profondeur), avec le shader path.gdshader.
/// Construit une fois au chargement ; le moteur écarte les rubans hors de l'écran.
/// </summary>
public static class PathMeshes
{
    private const string ShaderPath = "res://assets/shaders/path.gdshader";

    public static Node2D Build(PathNetwork network)
    {
        // Couche des routes, sous les décalques et les entités.
        Node2D root = new() { Name = "Paths", ZIndex = -9 };
        ShaderMaterial material = new() { Shader = GD.Load<Shader>(ShaderPath) };
        foreach (PathStroke stroke in network.Strokes)
        {
            MeshInstance2D mesh = new() { Mesh = BuildRibbon(stroke), Material = material };
            root.AddChild(mesh);
        }
        return root;
    }

    private static ArrayMesh BuildRibbon(PathStroke stroke)
    {
        int count = stroke.Points.Length;
        Vector2[] vertices = new Vector2[count * 2];
        Vector2[] uvs = new Vector2[count * 2];
        Color[] colors = new Color[count * 2];
        float arc = 0f;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                arc += Iso.ToGround(stroke.Points[i] - stroke.Points[i - 1]).Length();
            Vector2 side = Iso.ToScreen(PathNetworkGenerator.GroundNormal(stroke.Points, i) * stroke.Widths[i] * 0.5f);
            vertices[i * 2] = stroke.Points[i] + side;
            vertices[i * 2 + 1] = stroke.Points[i] - side;
            uvs[i * 2] = new Vector2(arc, 0f);
            uvs[i * 2 + 1] = new Vector2(arc, 1f);
            colors[i * 2] = stroke.Styles[i];
            colors[i * 2 + 1] = stroke.Styles[i];
        }

        int[] indices = new int[(count - 1) * 6];
        for (int i = 0; i < count - 1; i++)
        {
            int a = i * 2;
            int o = i * 6;
            indices[o] = a;
            indices[o + 1] = a + 1;
            indices[o + 2] = a + 2;
            indices[o + 3] = a + 1;
            indices[o + 4] = a + 3;
            indices[o + 5] = a + 2;
        }

        Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
