using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Planche des modèles low poly (plan 60, lot 60.0a) : chaque .glb de assets/models est importé, montré de face, de
/// trois quarts, de profil et de dos à la même hauteur, avec la marche pose par pose, puis tous côte à côte à leur
/// vraie taille. Les images vont dans le dossier passé par <c>--capture</c>, et la scène se ferme.
/// Les couleurs viennent des sommets ; le matériau est posé ici, comme le feront les styles du bac à sable.
/// Avec <c>--library &lt;dossier&gt;</c> (plan 60 §17, lot C1) : la bibliothèque de l'export de masse, lue sans import
/// d'après son index.json, en planches d'alignement par catalogue, dix modèles par planche du plus petit au plus grand,
/// chacun nommé, le facteur à gauche pour l'échelle.
/// </summary>
public partial class ModelSheet : Node3D
{
    private const string ModelRoot = "res://assets/models";
    private static readonly float[] Yaws = { 0f, 45f, 90f, 180f };

    private readonly List<(string Path, Node3D Model, Dictionary<string, string[]> Animations)> _models = new();
    private SubViewport _view;
    private Camera3D _camera;
    private Node3D _stage;
    private string _captureDir;

    public override async void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        int at = System.Array.IndexOf(args, "--capture");
        _captureDir = at >= 0 && at + 1 < args.Length ? args[at + 1] : OS.GetUserDataDir() + "/model_sheet";
        DirAccess.MakeDirRecursiveAbsolute(_captureDir);

        _view = new SubViewport
        {
            Size = new Vector2I(1600, 900),
            Msaa3D = Viewport.Msaa.Msaa4X,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };
        AddChild(_view);
        _view.AddChild(new WorldEnvironment { Environment = MakeEnvironment() });
        _view.AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-50f, -35f, 0f),
            LightEnergy = 1.1f,
            ShadowEnabled = true,
        });
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal };
        _view.AddChild(_camera);
        _stage = new Node3D();
        _view.AddChild(_stage);

        int library = System.Array.IndexOf(args, "--library");
        if (library >= 0 && library + 1 < args.Length)
        {
            await ShootLibrary(args[library + 1]);
            GetTree().Quit();
            return;
        }

        foreach (string folder in new[] { "characters", "creatures", "props" })
            LoadFolder($"{ModelRoot}/{folder}");
        GD.Print($"[ModelSheet] {_models.Count} modèles chargés.");

        foreach ((string path, Node3D model, Dictionary<string, string[]> animations) in _models)
        {
            string id = path.GetFile().GetBaseName();
            await ShootViews(model, animations, id);
            if (animations.TryGetValue("walk", out string[] walk))
                await ShootStrip(model, walk, $"{id}_walk");
            foreach ((string name, string[] frames) in animations)
            {
                if (name is not ("walk" or "idle"))
                    await ShootStrip(model, frames, $"{id}_{name}");
            }
        }
        await ShootLineup();
        foreach ((_, Node3D model, _) in _models)
            model.Free();
        GD.Print($"[ModelSheet] Planches : {_captureDir}");
        GetTree().Quit();
    }

    private static Environment MakeEnvironment()
    {
        return new Environment
        {
            BackgroundMode = Environment.BGMode.Color,
            BackgroundColor = new Color("2a2730"),
            AmbientLightSource = Environment.AmbientSource.Color,
            AmbientLightColor = new Color("8a8aa8"),
            AmbientLightEnergy = 0.55f,
        };
    }

    private void LoadFolder(string folder)
    {
        using DirAccess dir = DirAccess.Open(folder);
        if (dir == null)
            return;
        foreach (string file in dir.GetFiles())
        {
            if (!file.EndsWith(".glb"))
                continue;
            string path = $"{folder}/{file}";
            Node3D model = GD.Load<PackedScene>(path).Instantiate<Node3D>();
            (Dictionary<string, string[]> animations, bool flatNormals) = ReadManifest(path.GetBaseName() + ".json");
            ApplyVertexColorMaterial(model, flatNormals);
            _models.Add((path, model, animations));
        }
    }

    private static (Dictionary<string, string[]> Animations, bool FlatNormals) ReadManifest(string path)
    {
        Dictionary<string, string[]> animations = new();
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        Godot.Collections.Dictionary data = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        foreach ((Variant key, Variant value) in data["animations"].AsGodotDictionary())
        {
            Godot.Collections.Array frames = value.AsGodotArray();
            string[] names = new string[frames.Count];
            for (int i = 0; i < frames.Count; i++)
                names[i] = frames[i].AsString();
            animations[key.AsString()] = names;
        }
        return (animations, ReliefLowPoly.FlatNormals(data));
    }

    /// <summary>
    /// Couleur par sommet, ombrage par faces (calculées dans le shader au format compact, sinon normales du maillage) ;
    /// les faces émissives brillent de leur couleur. Les matériaux du bac à sable, pour juger le même rendu.
    /// </summary>
    private static void ApplyVertexColorMaterial(Node node, bool flatNormals)
    {
        foreach (Node child in node.FindChildren("*", "MeshInstance3D", true, false))
        {
            MeshInstance3D mesh = (MeshInstance3D)child;
            if (mesh.Mesh == null)
                continue;
            ReliefLowPoly.AddSmoothNormals(mesh);
            for (int s = 0; s < mesh.Mesh.GetSurfaceCount(); s++)
            {
                bool emissive = mesh.Mesh.SurfaceGetMaterial(s)?.ResourceName.Contains("emissive") ?? false;
                mesh.SetSurfaceOverrideMaterial(s, ReliefLowPoly.SurfaceMaterial(emissive, flatNormals));
            }
        }
    }

    /// <summary>Godot coiffe le glTF d'une racine à lui : les poses sont les maillages, où qu'ils soient dans l'arbre.</summary>
    private static void ShowFrame(Node3D model, string frame)
    {
        foreach (Node child in model.FindChildren("*", "MeshInstance3D", true, false))
            ((Node3D)child).Visible = child.Name == frame;
    }

    /// <summary>Quatre vues à la même hauteur d'image : face, trois quarts, profil, dos.</summary>
    private async System.Threading.Tasks.Task ShootViews(Node3D model, Dictionary<string, string[]> animations, string id)
    {
        string frame = animations["idle"][0];
        float height = Height(model, frame);
        float spacing = Mathf.Max(height, Width(model, frame)) * 1.25f;
        List<Node3D> copies = new();
        for (int i = 0; i < Yaws.Length; i++)
        {
            Node3D copy = (Node3D)model.Duplicate();
            ShowFrame(copy, frame);
            copy.Position = new Vector3((i - 1.5f) * spacing, 0f, 0f);
            copy.RotationDegrees = new Vector3(0f, Yaws[i], 0f);
            _stage.AddChild(copy);
            copies.Add(copy);
        }
        Frame(spacing * Yaws.Length, height);
        await Save($"{id}_vues");
        foreach (Node3D copy in copies)
            copy.QueueFree();
    }

    /// <summary>Une animation pose par pose, de trois quarts.</summary>
    private async System.Threading.Tasks.Task ShootStrip(Node3D model, string[] frames, string name)
    {
        float height = Height(model, frames[0]);
        float spacing = Mathf.Max(height, Width(model, frames[0])) * 0.9f;
        List<Node3D> copies = new();
        for (int i = 0; i < frames.Length; i++)
        {
            Node3D copy = (Node3D)model.Duplicate();
            ShowFrame(copy, frames[i]);
            copy.Position = new Vector3((i - (frames.Length - 1) * 0.5f) * spacing, 0f, 0f);
            copy.RotationDegrees = new Vector3(0f, 60f, 0f);
            _stage.AddChild(copy);
            copies.Add(copy);
        }
        Frame(spacing * frames.Length, height);
        await Save(name);
        foreach (Node3D copy in copies)
            copy.QueueFree();
    }

    /// <summary>Tous les modèles côte à côte, à leur vraie taille, de trois quarts comme la caméra du jeu.</summary>
    private System.Threading.Tasks.Task ShootLineup()
    {
        List<(Node3D, string, string)> row = new();
        foreach ((_, Node3D model, Dictionary<string, string[]> animations) in _models)
            row.Add((model, animations["idle"][0], null));
        return ShootRow(row, "alignement");
    }

    private async System.Threading.Tasks.Task ShootRow(List<(Node3D Model, string Frame, string Label)> row, string name)
    {
        float x = 0f;
        float tallest = 0f;
        List<Node3D> copies = new();
        foreach ((Node3D model, string frame, string label) in row)
        {
            float width = Width(model, frame);
            Node3D copy = (Node3D)model.Duplicate();
            ShowFrame(copy, frame);
            copy.Position = new Vector3(x + width * 0.5f, 0f, 0f);
            copy.RotationDegrees = new Vector3(0f, 30f, 0f);
            if (label != null)
            {
                copy.AddChild(new Label3D
                {
                    Text = label,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    FixedSize = true,
                    PixelSize = 0.0009f,
                    FontSize = 22,
                    NoDepthTest = true,
                    Position = new Vector3(0f, -0.25f, 0f),
                });
            }
            _stage.AddChild(copy);
            copies.Add(copy);
            x += width + 0.6f;
            tallest = Mathf.Max(tallest, Height(model, frame));
        }
        foreach (Node3D copy in copies)
            copy.Position -= new Vector3(x * 0.5f, 0f, 0f);
        Frame(x, tallest, 35f);
        await Save(name);
        foreach (Node3D copy in copies)
            copy.QueueFree();
    }

    /// <summary>
    /// La bibliothèque de l'export de masse, catalogue par catalogue. Ses .glb sont lus à l'exécution (GltfDocument) :
    /// elle est hors de l'import de Godot, qui n'a pas à en garder des centaines en cache.
    /// </summary>
    private async System.Threading.Tasks.Task ShootLibrary(string root)
    {
        using FileAccess file = FileAccess.Open($"{root}/index.json", FileAccess.ModeFlags.Read);
        Godot.Collections.Array entries = Json.ParseString(file.GetAsText()).AsGodotDictionary()["models"].AsGodotArray();
        Dictionary<string, List<(string Path, float Height)>> folders = new();
        foreach (Variant value in entries)
        {
            Godot.Collections.Dictionary entry = value.AsGodotDictionary();
            // Un décor plat (flaque, rail, marelle) sort sans triangle : il deviendra un décalque, pas un maillage.
            if (entry.ContainsKey("error") || entry["max_triangles"].AsInt32() == 0)
                continue;
            string path = entry["path"].AsString();
            string folder = path.GetBaseDir();
            if (!folders.TryGetValue(folder, out List<(string, float)> list))
                folders[folder] = list = new List<(string, float)>();
            list.Add((path, (float)entry["height_m"].AsDouble()));
        }
        (Node3D Model, string Frame) reference = LoadRuntime($"{root}/characters/facteur");
        const int perPage = 10;
        int pages = 0;
        foreach ((string folder, List<(string Path, float Height)> list) in folders)
        {
            list.Sort((a, b) => a.Height.CompareTo(b.Height));
            for (int start = 0; start < list.Count; start += perPage)
            {
                List<(Node3D, string, string)> row = new();
                if (reference.Model != null)
                    row.Add((reference.Model, reference.Frame, "facteur"));
                List<Node3D> loaded = new();
                for (int i = start; i < Mathf.Min(start + perPage, list.Count); i++)
                {
                    (Node3D model, string frame) = LoadRuntime($"{root}/{list[i].Path.GetBaseName()}");
                    if (model == null)
                        continue;
                    loaded.Add(model);
                    row.Add((model, frame, list[i].Path.GetFile().GetBaseName().TrimPrefix("prop_")));
                }
                await ShootRow(row, $"{folder.Replace('/', '_')}_{start / perPage + 1:00}");
                foreach (Node3D model in loaded)
                    model.Free();
                pages++;
            }
        }
        reference.Model?.Free();
        GD.Print($"[ModelSheet] {pages} planches de la bibliothèque : {_captureDir}");
    }

    private static (Node3D Model, string Frame) LoadRuntime(string pathWithoutExtension)
    {
        if (!FileAccess.FileExists($"{pathWithoutExtension}.glb"))
            return (null, null);
        GltfDocument document = new();
        GltfState state = new();
        if (document.AppendFromFile($"{pathWithoutExtension}.glb", state) != Error.Ok)
            return (null, null);
        Node3D model = (Node3D)document.GenerateScene(state);
        (Dictionary<string, string[]> animations, bool flatNormals) = ReadManifest($"{pathWithoutExtension}.json");
        ApplyVertexColorMaterial(model, flatNormals);
        string first = animations.TryGetValue("idle", out string[] idle) ? idle[0] : animations.Values.GetEnumerator() is var e && e.MoveNext() ? e.Current[0] : null;
        return (model, first);
    }

    /// <summary>Cadre une bande de <paramref name="width"/> × <paramref name="height"/> mètres centrée au-dessus du sol.</summary>
    private void Frame(float width, float height, float pitch = 12f)
    {
        float aspect = _view.Size.X / (float)_view.Size.Y;
        _camera.Size = Mathf.Max(height * 1.3f, width / aspect * 1.05f);
        _camera.RotationDegrees = new Vector3(-pitch, 0f, 0f);
        Vector3 target = new(0f, height * 0.5f, 0f);
        _camera.Position = target + _camera.Basis.Z * 40f;
    }

    private async System.Threading.Tasks.Task Save(string name)
    {
        for (int i = 0; i < 3; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        string path = $"{_captureDir}/{name}.png";
        _view.GetTexture().GetImage().SavePng(path);
        GD.Print($"[ModelSheet] {path}");
    }

    private static Aabb Bounds(Node3D model, string frame)
    {
        MeshInstance3D mesh = model.FindChild(frame, true, false) as MeshInstance3D;
        return mesh?.GetAabb() ?? new Aabb(Vector3.Zero, Vector3.One);
    }

    private static float Height(Node3D model, string frame) => Bounds(model, frame).End.Y;

    private static float Width(Node3D model, string frame)
    {
        Aabb box = Bounds(model, frame);
        return Mathf.Max(box.Size.X, box.Size.Z);
    }
}
