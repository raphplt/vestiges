using System;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.Tests;

/// <summary>
/// Galerie des zones au sol de pixel_fx.gdshader (plan 25 S10 et suite) : chaque matière, le couloir et la limite de
/// portée posés sur trois sols, à graine fixe, capturés à plusieurs poses de l'animation. Sert d'avant/après sans run.
/// --output DIR, --shots N (poses capturées, une toutes les 8 images à 60 i/s, soit une pose du shader).
/// --bench N : coût relatif du shader. Les douze zones répétées N fois sur l'écran, temps moyen par image mesuré sur
/// 240 images après 60 de chauffe. Avec un rendu logiciel (Xvfb), le coût des fragments domine : à comparer
/// seulement avant/après sur la même machine.
/// </summary>
public partial class GroundZoneGallery : Node2D
{
    private const float Zoom = 2f;
    private static readonly Vector2 Cell = new(240f, 180f);
    private static readonly Color[] Grounds =
    {
        new(0.29f, 0.35f, 0.23f), new(0.43f, 0.42f, 0.38f), new(0.18f, 0.16f, 0.19f),
    };

    private readonly record struct Sample(string Label, PixelFxShape Shape, ZoneMatter Matter, FxFamily Family,
        float Radius, float Progress, float Fill, float Fade = 0f, float Angle = 0f, float Thickness = 1f);

    private static readonly Sample[] Samples =
    {
        new("annonce 25 %", PixelFxShape.Zone, ZoneMatter.Mark, FxFamily.Void, 44f, 0.25f, 0.25f),
        new("annonce 75 %", PixelFxShape.Zone, ZoneMatter.Mark, FxFamily.Void, 44f, 0.75f, 0.25f),
        new("impact", PixelFxShape.Zone, ZoneMatter.Mark, FxFamily.Hostile, 44f, 1f, 1f, 0.3f),
        new("portée", PixelFxShape.Zone, ZoneMatter.Mark, FxFamily.Hostile, 52f, 0f, 0f),
        new("bile", PixelFxShape.Zone, ZoneMatter.Liquid, FxFamily.Rust, 40f, 0.5f, 0.9f),
        new("gomme", PixelFxShape.Zone, ZoneMatter.Liquid, FxFamily.Gum, 30f, 0.5f, 0.4f),
        new("feu", PixelFxShape.Zone, ZoneMatter.Fire, FxFamily.Fire, 40f, 0.5f, 0.35f),
        new("cadran", PixelFxShape.Zone, ZoneMatter.Dial, FxFamily.Hybrid, 48f, 0.4f, 0.35f),
        new("éclat 20 %", PixelFxShape.Zone, ZoneMatter.Burst, FxFamily.Fire, 40f, 0.2f, 0.35f),
        new("éclat 70 %", PixelFxShape.Zone, ZoneMatter.Burst, FxFamily.Essence, 40f, 0.7f, 0.35f, 0.2f),
        new("couloir", PixelFxShape.Lane, ZoneMatter.Mark, FxFamily.Void, 150f, 0.5f, 0.25f, 0f, 0.35f, 22f),
        new("gardien 50 %", PixelFxShape.Zone, ZoneMatter.Mark, FxFamily.Void, 64f, 0.5f, 0.25f),
    };

    public override async void _Ready()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            string output = Argument(args, "--output", "/tmp/vestiges-zones");
            int shots = int.Parse(Argument(args, "--shots", "4"), CultureInfo.InvariantCulture);
            int bench = int.Parse(Argument(args, "--bench", "0"), CultureInfo.InvariantCulture);
            DirAccess.MakeDirRecursiveAbsolute(output);
            Shader shader = GD.Load<Shader>("res://assets/shaders/pixel_fx.gdshader");
            Image white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            white.Fill(Colors.White);
            ImageTexture pixel = ImageTexture.CreateFromImage(white);

            const int columns = 4;
            for (int index = 0; index < Samples.Length; index++)
            {
                Vector2 origin = new(index % columns * Cell.X, index / columns * Cell.Y);
                AddChild(new ColorRect
                {
                    Position = origin,
                    Size = Cell,
                    Color = Grounds[index / columns % Grounds.Length],
                    ZIndex = -10,
                });
                AddChild(new Label { Text = Samples[index].Label, Position = origin + new Vector2(4f, 2f), ZIndex = 5 });
                AddChild(Zone(Samples[index], index, origin + Cell * 0.5f + new Vector2(0f, 8f), shader, pixel));
                // Copies décalées dans la case : la charge de fragments grandit sans changer le cadrage.
                for (int copy = 1; copy < bench; copy++)
                {
                    Vector2 jitter = new((copy * 37 % 61 - 30) * 1.5f, (copy * 23 % 41 - 20) * 1.2f);
                    AddChild(Zone(Samples[index], index + copy * Samples.Length, origin + Cell * 0.5f + jitter, shader, pixel));
                }
            }
            AddChild(new Camera2D
            {
                Zoom = new Vector2(Zoom, Zoom),
                Position = new Vector2(columns * Cell.X, 3f * Cell.Y) * 0.5f,
            });

            if (bench > 0)
            {
                for (int frame = 0; frame < 60; frame++)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                ulong start = Time.GetTicksUsec();
                const int measured = 240;
                for (int frame = 0; frame < measured; frame++)
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                double average = (Time.GetTicksUsec() - start) / 1000.0 / measured;
                GD.Print($"[GroundZoneGallery] BENCH zones={Samples.Length * bench} frame_ms={average.ToString("F2", CultureInfo.InvariantCulture)}");
            }

            for (int shot = 0; shot < shots; shot++)
            {
                for (int frame = 0; frame < 8; frame++)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using Image image = GetViewport().GetTexture().GetImage();
                string path = $"{output}/zones-{shot:00}.png";
                image.SavePng(path);
            }
            GD.Print($"[GroundZoneGallery] RESULT shots={shots} output={output}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[GroundZoneGallery] {exception}");
            GetTree().Quit(1);
        }
    }

    /// <summary>Même quad que PixelFx.Play (taille paire calée sur les texels), à graine fixe.</summary>
    private static Sprite2D Zone(Sample sample, int index, Vector2 position, Shader shader, Texture2D pixel)
    {
        float squash = Iso.GroundSquash;
        float reach = sample.Shape == PixelFxShape.Zone ? sample.Radius * 1.4f + 8f : sample.Radius + sample.Thickness + 6f;
        Vector2 size = new(reach * 2f, reach * 2f / squash);
        size = new Vector2(Mathf.Ceil(size.X / 2f) * 2f + 2f, Mathf.Ceil(size.Y / 2f) * 2f + 2f);
        // Le couloir part de son origine : décalé à gauche pour tenir dans sa case.
        if (sample.Shape == PixelFxShape.Lane)
            position -= new Vector2(sample.Radius * 0.45f, 0f);
        FxRamp ramp = PixelPalette.Ramp(sample.Family);
        ShaderMaterial material = new() { Shader = shader };
        material.SetShaderParameter("shape", (int)sample.Shape);
        material.SetShaderParameter("size_px", size);
        material.SetShaderParameter("angle", sample.Angle);
        material.SetShaderParameter("radius", sample.Radius);
        material.SetShaderParameter("thickness", sample.Thickness);
        material.SetShaderParameter("squash", squash);
        material.SetShaderParameter("progress", sample.Progress);
        material.SetShaderParameter("fade", sample.Fade);
        material.SetShaderParameter("fill_density", sample.Fill);
        material.SetShaderParameter("progress_fill", true);
        material.SetShaderParameter("seed", 13.7f * (index + 1));
        material.SetShaderParameter("matter", (int)sample.Matter);
        material.SetShaderParameter("c_light", ramp.Light);
        material.SetShaderParameter("c_mid", ramp.Mid);
        material.SetShaderParameter("c_dark", ramp.Dark);
        material.SetShaderParameter("c_outline", ramp.Outline);
        return new Sprite2D
        {
            Texture = pixel,
            TextureFilter = TextureFilterEnum.Nearest,
            Scale = size,
            Position = position.Round(),
            Material = material,
        };
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
