using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Infrastructure;

/// <summary>
/// Soumet les shaders de run au rendu dans un petit viewport pendant le chargement.
/// Sa texture n'est jamais présentée au joueur ; les échantillons restent pourtant dans le champ du GPU.
/// Les références fortes conservent les Shader après destruction des échantillons et entre deux runs.
/// </summary>
public partial class ShaderWarmup : SubViewport
{
    private enum SampleKind { Sprite, Rectangle, Mesh }
    private static readonly (string Path, SampleKind Kind)[] Samples =
    {
        ("res://assets/shaders/entity.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/sway.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/prop_forget.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/prop_trample.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/ground.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/path.gdshader", SampleKind.Mesh),
        ("res://assets/shaders/echo.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/light_column.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/pixel_fx.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/player_projectile.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/iridescent_fluid.gdshader", SampleKind.Sprite),
        ("res://assets/shaders/swamp_atmosphere.gdshader", SampleKind.Rectangle),
        ("res://assets/shaders/erasure_veil.gdshader", SampleKind.Rectangle),
        ("res://assets/shaders/crisis_omen.gdshader", SampleKind.Rectangle),
        ("res://assets/shaders/death_erasure.gdshader", SampleKind.Rectangle),
        ("res://assets/shaders/landmark_reveal.gdshader", SampleKind.Rectangle),
        ("res://assets/shaders/colorblind.gdshader", SampleKind.Rectangle),
        ("res://assets/shaders/hurt_vignette.gdshader", SampleKind.Rectangle),
        ("res://assets/shaders/indicible_tide.gdshader", SampleKind.Rectangle),
        ("res://assets/shaders/ui_dust_twinkle.gdshader", SampleKind.Sprite),
    };
    private static readonly Dictionary<string, Shader> Shaders = new();
    private static Texture2D _texture;

    public override void _Ready()
    {
        Name = "ShaderWarmup";
        Size = new Vector2I(256, ((Samples.Length + 3) / 4) * 56 + 32);
        Disable3D = true;
        World2D = new World2D();
        RenderTargetUpdateMode = UpdateMode.Always;
        ProcessMode = ProcessModeEnum.Always;
        Node2D content = new() { Name = "Samples" };
        AddChild(content);
        if (_texture == null)
        {
            using Image image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
            image.Fill(Colors.White);
            _texture = ImageTexture.CreateFromImage(image);
        }
        for (int index = 0; index < Samples.Length; index++)
        {
            (string path, SampleKind kind) = Samples[index];
            if (!Shaders.TryGetValue(path, out Shader shader))
            {
                shader = GD.Load<Shader>(path);
                Shaders.Add(path, shader);
            }
            ShaderMaterial material = new() { Shader = shader };
            Vector2 position = new(32 + (index % 4) * 60, 24 + (index / 4) * 56);
            CanvasItem sample;
            switch (kind)
            {
                case SampleKind.Rectangle:
                    sample = new ColorRect { Position = position, Size = new Vector2(32, 32) };
                    break;
                case SampleKind.Mesh:
                    sample = new MeshInstance2D { Position = position, Mesh = CreateRibbonSample() };
                    break;
                default:
                    sample = new Sprite2D { Position = position, Texture = _texture };
                    break;
            }
            sample.Name = System.IO.Path.GetFileNameWithoutExtension(path);
            sample.Material = material;
            content.AddChild(sample);
        }
        // Matériau généré par Godot : même fabrique que la lueur des vraies orbes, sans créer d'orbe ramassable.
        GpuParticles2D glow = VfxFactory.CreateXpOrbGlow();
        glow.Name = "XpGlow";
        glow.Position = new Vector2(128, Size.Y - 12);
        glow.Preprocess = 0.5;
        content.AddChild(glow);
        // Particules de la run, avec les matériaux partagés qu'elles garderont : leur shader est compilé ici, pas à la
        // première Résurgence (plan 29). Opaques le temps du préchauffage : une brume transparente n'est pas dessinée.
        GpuParticles2D[] runParticles =
        {
            World.AmbientParticles.CreateDayParticles(false), World.AmbientParticles.CreateNightParticles(false),
            Combat.AberrationAura.Create(10),
        };
        for (int index = 0; index < runParticles.Length; index++)
        {
            GpuParticles2D sample = runParticles[index];
            sample.Name = $"RunParticles{index}";
            sample.Modulate = Colors.White;
            sample.ZIndex = 0;
            sample.Position = new Vector2(32 + index * 60, Size.Y - 12);
            sample.Preprocess = 0.5;
            content.AddChild(sample);
        }
    }

    public static async Task RenderAsync(Node owner)
    {
        // FramePostDraw n'arrive pas avec le serveur factice ; aucun rendu à préchauffer dans les bancs headless.
        if (DisplayServer.GetName() == "headless")
            return;
        ShaderWarmup viewport = new();
        owner.AddChild(viewport);
        try
        {
            for (int frame = 0; frame < 3; frame++)
            {
                await owner.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (!IsInstanceValid(viewport) || !viewport.IsInsideTree())
                    return;
            }
            int draws = RenderingServer.ViewportGetRenderInfo(viewport.GetViewportRid(),
                RenderingServer.ViewportRenderInfoType.Canvas, RenderingServer.ViewportRenderInfo.DrawCallsInFrame);
            GD.Print($"[ShaderWarmup] shaders={Samples.Length} draw_calls={draws} frames=3");
        }
        finally
        {
            if (IsInstanceValid(viewport))
            {
                viewport.RenderTargetUpdateMode = UpdateMode.Disabled;
                viewport.QueueFree();
            }
        }
    }

    /// <summary>Même format que les chemins : sommets 2D, UV, couleurs et indices de triangles.</summary>
    private static ArrayMesh CreateRibbonSample()
    {
        Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = new Vector2[] { new(-16, -8), new(-16, 8), new(16, -8), new(16, 8) };
        arrays[(int)Mesh.ArrayType.TexUV] = new Vector2[] { new(0, 0), new(0, 1), new(32, 0), new(32, 1) };
        arrays[(int)Mesh.ArrayType.Color] = new Color[] { Colors.White, Colors.White, Colors.White, Colors.White };
        arrays[(int)Mesh.ArrayType.Index] = new int[] { 0, 1, 2, 1, 3, 2 };
        ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
