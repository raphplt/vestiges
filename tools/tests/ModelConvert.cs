using System.Collections.Generic;
using System.Diagnostics;
using Godot;

using Vestiges.World3D;

namespace Vestiges.Tests;

/// <summary>
/// Convertit les modèles low poly exportés (.glb, <c>tools/export_lowpoly.py</c>) en scènes prêtes pour le moteur
/// (plan 60 §19, lot C4a) : un <c>.scn</c> à côté de chaque .glb, poses en enfants, normales lissées (pour le décalage
/// des ombres, voir <see cref="ReliefLowPoly.AddSmoothNormals"/>) et niveaux de détail générés par Godot. Seuls les
/// modèles dont le .glb est plus récent que le .scn repassent, sauf <c>--force</c>. Le rapport (<c>lods.json</c>, à la
/// racine) donne, modèle par modèle, les triangles de chaque niveau de son plus gros maillage, son poids et sa durée.
/// Avec <c>--probe &lt;modèle&gt;</c>, fenêtré : les primitives dessinées par image quand le modèle s'éloigne, niveaux
/// de détail laissés à Godot puis forcés au plus fin, pour savoir si le rendu les applique.
/// Lancement : <c>tools/convert_models.sh [dossier] [--force]</c>, <c>tools/convert_models.sh --probe &lt;modèle&gt;</c>.
/// </summary>
public partial class ModelConvert : Node
{
    // L'angle de l'importeur de Godot : au-delà, deux faces ne se fondent pas en simplifiant.
    private const float NormalMergeAngle = 60f;
    private static readonly float[] ProbeDistances = { 4f, 12f, 30f, 60f, 120f };
    // Seuils d'erreur tolérée, en pixels (Viewport.MeshLodThreshold) : 1 par défaut dans Godot.
    private static readonly float[] ProbeThresholds = { 1f, 4f, 8f };

    public override async void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        string root = ProjectSettings.GlobalizePath(ArgValue(args, "--root") ?? "res://build/models");
        string probe = ArgValue(args, "--probe");
        int exitCode = probe != null ? await Probe($"{root}/{probe}") : Convert(root, System.Array.IndexOf(args, "--force") >= 0);
        GetTree().Quit(exitCode);
    }

    private static string ArgValue(string[] args, string name)
    {
        int at = System.Array.IndexOf(args, name);
        return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
    }

    private static int Convert(string root, bool force)
    {
        List<string> roots = new();
        FindModels(root, roots);
        roots.Sort(System.StringComparer.Ordinal);
        Stopwatch total = Stopwatch.StartNew();
        Godot.Collections.Array report = new();
        Godot.Collections.Dictionary previous = PreviousReport(root);
        int converted = 0, failures = 0;
        long bytes = 0;
        foreach (string model in roots)
        {
            string glb = $"{model}.glb", scene = $"{model}.scn";
            bool stale = force || !FileAccess.FileExists(scene)
                || FileAccess.GetModifiedTime(scene) < FileAccess.GetModifiedTime(glb);
            Stopwatch watch = Stopwatch.StartNew();
            Godot.Collections.Dictionary entry = new() { ["path"] = model.TrimPrefix(root + "/") + ".scn" };
            if (stale)
            {
                Error error = ConvertOne(glb, scene, entry);
                if (error != Error.Ok)
                {
                    entry["error"] = error.ToString();
                    failures++;
                    GD.PrintErr($"[ModelConvert] ÉCHEC {glb} : {error}");
                }
                else
                    converted++;
                entry["seconds"] = Mathf.Snapped(watch.Elapsed.TotalSeconds, 0.01);
            }
            else if (previous.TryGetValue(entry["path"], out Variant before))
                entry = before.AsGodotDictionary();
            if (FileAccess.FileExists(scene))
            {
                using FileAccess file = FileAccess.Open(scene, FileAccess.ModeFlags.Read);
                entry["bytes"] = (long)file.GetLength();
                bytes += (long)file.GetLength();
            }
            report.Add(entry);
        }
        Godot.Collections.Dictionary summary = new()
        {
            ["models"] = roots.Count,
            ["converted"] = converted,
            ["failures"] = failures,
            ["megabytes"] = Mathf.Snapped(bytes / 1e6, 0.1),
            ["seconds"] = Mathf.Snapped(total.Elapsed.TotalSeconds, 0.1),
        };
        using (FileAccess file = FileAccess.Open($"{root}/lods.json", FileAccess.ModeFlags.Write))
            file.StoreString(Json.Stringify(new Godot.Collections.Dictionary { ["summary"] = summary, ["models"] = report }, "\t"));
        GD.Print($"[ModelConvert] {roots.Count} modèles, {converted} convertis, {failures} échecs, "
            + $"{summary["megabytes"]} Mo de .scn, {summary["seconds"]} s : {root}/lods.json");
        return failures > 0 ? 1 : 0;
    }

    /// <summary>Les entrées du rapport précédent, par chemin : un modèle à jour garde la sienne.</summary>
    private static Godot.Collections.Dictionary PreviousReport(string root)
    {
        Godot.Collections.Dictionary byPath = new();
        if (!FileAccess.FileExists($"{root}/lods.json"))
            return byPath;
        using FileAccess file = FileAccess.Open($"{root}/lods.json", FileAccess.ModeFlags.Read);
        foreach (Variant value in Json.ParseString(file.GetAsText()).AsGodotDictionary()["models"].AsGodotArray())
            byPath[value.AsGodotDictionary()["path"]] = value;
        return byPath;
    }

    /// <summary>Les modèles (chemins sans extension) de l'arbre : un .glb accompagné de son manifeste .json.</summary>
    private static void FindModels(string folder, List<string> found)
    {
        using DirAccess dir = DirAccess.Open(folder);
        if (dir == null)
            return;
        foreach (string sub in dir.GetDirectories())
            FindModels($"{folder}/{sub}", found);
        foreach (string file in dir.GetFiles())
        {
            string model = $"{folder}/{file.GetBaseName()}";
            if (file.EndsWith(".glb") && FileAccess.FileExists($"{model}.json"))
                found.Add(model);
        }
    }

    private static Error ConvertOne(string glb, string scene, Godot.Collections.Dictionary entry)
    {
        GltfDocument document = new();
        GltfState state = new();
        Error error = document.AppendFromFile(glb, state);
        if (error != Error.Ok)
            return error;
        Node gltf = document.GenerateScene(state);
        Node3D model = new() { Name = gltf.Name };
        // Le plus gros maillage du modèle (sa pose la plus lourde) : celui que l'index de l'export appelle max_triangles.
        Godot.Collections.Array largest = new();
        foreach (Node child in gltf.FindChildren("*", "MeshInstance3D", true, false))
        {
            if (((MeshInstance3D)child).Mesh is not ArrayMesh source)
                continue;
            ImporterMesh importer = new();
            for (int s = 0; s < source.GetSurfaceCount(); s++)
            {
                Godot.Collections.Array arrays = source.SurfaceGetArrays(s);
                if ((source.SurfaceGetFormat(s) & Mesh.ArrayFormat.FormatNormal) == 0)
                    arrays = ReliefLowPoly.WithSmoothNormals(arrays);
                importer.AddSurface(Mesh.PrimitiveType.Triangles, arrays, null, null, source.SurfaceGetMaterial(s),
                    source.SurfaceGetName(s));
            }
            importer.GenerateLods(NormalMergeAngle, 0f, new Godot.Collections.Array());
            Godot.Collections.Array triangles = LodTriangles(importer);
            if (largest.Count == 0 || triangles[0].AsInt32() > largest[0].AsInt32())
                largest = triangles;
            ArrayMesh mesh = importer.GetMesh();
            mesh.ResourceName = child.Name;
            MeshInstance3D instance = new() { Name = child.Name, Mesh = mesh };
            model.AddChild(instance);
            instance.Owner = model;
        }
        gltf.Free();
        entry["lod_triangles"] = largest;
        PackedScene packed = new();
        error = packed.Pack(model);
        model.Free();
        return error != Error.Ok ? error : ResourceSaver.Save(packed, scene, ResourceSaver.SaverFlags.Compress);
    }

    /// <summary>Triangles d'un maillage au plus fin puis à chaque niveau de détail (une surface sans ce niveau garde son dernier).</summary>
    private static Godot.Collections.Array LodTriangles(ImporterMesh mesh)
    {
        int levels = 0, full = 0;
        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
        {
            levels = Mathf.Max(levels, mesh.GetSurfaceLodCount(s));
            full += mesh.GetSurfaceArrays(s)[(int)Mesh.ArrayType.Index].AsInt32Array().Length / 3;
        }
        Godot.Collections.Array triangles = new() { full };
        for (int level = 0; level < levels; level++)
        {
            int count = 0;
            for (int s = 0; s < mesh.GetSurfaceCount(); s++)
            {
                int available = mesh.GetSurfaceLodCount(s);
                count += available == 0
                    ? mesh.GetSurfaceArrays(s)[(int)Mesh.ArrayType.Index].AsInt32Array().Length / 3
                    : mesh.GetSurfaceLodIndices(s, Mathf.Min(level, available - 1)).Length / 3;
            }
            triangles.Add(count);
        }
        return triangles;
    }

    /// <summary>
    /// Le modèle à des distances croissantes devant la caméra : primitives dessinées avec les niveaux de détail de
    /// Godot, à plusieurs seuils d'erreur tolérée, puis avec <see cref="GeometryInstance3D.LodBias"/> poussé pour garder
    /// le plus fin. Égales partout : le rendu ne les applique pas.
    /// </summary>
    private async System.Threading.Tasks.Task<int> Probe(string model)
    {
        if (!FileAccess.FileExists($"{model}.glb"))
        {
            GD.PrintErr($"[ModelConvert] modèle introuvable : {model}.glb");
            return 1;
        }
        Camera3D camera = new() { Fov = 50f, Far = 500f };
        AddChild(camera);
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-50f, 30f, 0f) });
        Node3D scene = ReliefLowPoly.LoadScene(model);
        AddChild(scene);
        foreach (Node child in scene.FindChildren("*", "MeshInstance3D", true, false))
            ((Node3D)child).Visible = child.GetIndex() == 0;
        GD.Print($"[ModelConvert] sonde {model} ({RenderingServer.GetCurrentRenderingDriverName()}, "
            + $"{ProjectSettings.GetSetting("rendering/renderer/rendering_method")})");
        foreach (float distance in ProbeDistances)
        {
            scene.Position = new Vector3(0f, -1f, -distance);
            string line = $"[ModelConvert] {distance,5:0} m :";
            foreach (float threshold in ProbeThresholds)
            {
                GetViewport().MeshLodThreshold = threshold;
                line += $" {await Primitives(scene, 1f),7} (seuil {threshold:0} px)";
            }
            GetViewport().MeshLodThreshold = 1f;
            GD.Print($"{line}, {await Primitives(scene, 1e6f),7} au plus fin");
        }
        return 0;
    }

    private async System.Threading.Tasks.Task<long> Primitives(Node3D scene, float lodBias)
    {
        foreach (Node child in scene.FindChildren("*", "MeshInstance3D", true, false))
            ((GeometryInstance3D)child).LodBias = lodBias;
        for (int i = 0; i < 4; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        return (long)RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalPrimitivesInFrame);
    }
}
