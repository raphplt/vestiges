using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Les styles du bac à sable de la verticalité (plan 60, lot 60.0d), basculés en jeu, chacun indépendamment : le sol
/// (facettes, lisse, tuiles pixel art), le détail des corps et des décors (low ou mid poly), les contours noirs, la
/// pixelisation de l'image, l'ombrage (normal ou toon). Tout est construit au lancement ; basculer ne fait que montrer,
/// cacher ou échanger des maillages et régler les matériaux.
/// </summary>
public sealed class ReliefLook
{
    public enum Ground { Facets, Smooth, PixelArt }

    private static readonly string[] GroundNames = { "facettes", "lisse", "pixel art" };
    private const string OutlineShader = "res://tools/tests/relief_outline.gdshader";

    private readonly MeshInstance3D[] _grounds = new MeshInstance3D[3];
    private readonly MeshInstance3D _outline;
    private readonly List<BaseMaterial3D> _shaded = new();
    private readonly List<(MeshInstance3D Body, ReliefLowPoly.Model[] Models)> _props = new();
    private readonly SubViewport _viewport;
    private readonly TextureRect _picture;
    private readonly Vector2I _fullSize;
    private readonly int _pixelScale;

    public Ground CurrentGround;
    public bool Mid;
    public bool Outlines;
    public bool Pixels;
    public bool Toon;

    /// <summary>Le mid poly n'est pas versionné : sans lui, la bascule des corps reste en low poly.</summary>
    public bool MidAvailable { get; }

    /// <summary>Le joueur, en low [0] et mid [1] poly.</summary>
    public ReliefLowPoly.Model[] Player { get; } = new ReliefLowPoly.Model[2];

    /// <summary>Les créatures par type, en low [0] et mid [1] poly.</summary>
    public ReliefLowPoly.Model[][] Creatures { get; } = new ReliefLowPoly.Model[2][];

    public ReliefLook(ReliefConfig config, ReliefArena arena, ReliefSim sim, Node3D world, Camera3D camera,
        SubViewport viewport, TextureRect picture, IReadOnlyList<Vector2> keepClear)
    {
        ReliefConfig.LowPolyStyle style = config.LowPoly;
        _viewport = viewport;
        _picture = picture;
        _fullSize = viewport.Size;
        _pixelScale = Mathf.Max(1, style.PixelScale);

        string character = $"characters/{config.Character}";
        MidAvailable = ReliefLowPoly.Exists($"mid/{character}");
        foreach (ReliefConfig.Creature creature in config.Creatures)
            MidAvailable &= ReliefLowPoly.Exists($"mid/creatures/{creature.Id}");
        Player[0] = ReliefLowPoly.LoadModel(character);
        Player[1] = MidAvailable ? ReliefLowPoly.LoadModel($"mid/{character}") : Player[0];
        for (int detail = 0; detail < 2; detail++)
        {
            Creatures[detail] = new ReliefLowPoly.Model[config.Creatures.Length];
            for (int t = 0; t < config.Creatures.Length; t++)
            {
                string path = $"creatures/{config.Creatures[t].Id}";
                Creatures[detail][t] = detail == 1 && MidAvailable ? ReliefLowPoly.LoadModel($"mid/{path}") : ReliefLowPoly.LoadModel(path);
            }
        }
        if (!MidAvailable)
            GD.PushWarning("[ReliefLook] Mid poly absent : python3 tools/export_lowpoly.py --detail mid");

        _grounds[(int)Ground.Facets] = ReliefLowPoly.BuildTerrain(arena, style, true);
        _grounds[(int)Ground.Smooth] = ReliefLowPoly.BuildTerrain(arena, style, false);
        _grounds[(int)Ground.PixelArt] = ReliefTerrainBuilder.Build(arena, config, true, null);
        foreach (MeshInstance3D ground in _grounds)
            world.AddChild(ground);
        _shaded.Add(ReliefLowPoly.Plain);
        Mesh tiles = _grounds[(int)Ground.PixelArt].Mesh;
        for (int s = 0; s < tiles.GetSurfaceCount(); s++)
        {
            if (tiles.SurfaceGetMaterial(s) is BaseMaterial3D material)
                _shaded.Add(material);
        }

        PlaceProps(arena, style, sim, world, keepClear, config.Spawn);

        // Quadrilatère plein écran, devant la caméra : son shader place lui-même ses sommets aux coins de l'image.
        ShaderMaterial outline = new() { Shader = GD.Load<Shader>(OutlineShader), RenderPriority = (int)Material.RenderPriorityMax };
        outline.SetShaderParameter("line_color", style.OutlineColor);
        outline.SetShaderParameter("sensitivity", style.OutlineSensitivity);
        outline.SetShaderParameter("ortho_sensitivity", style.OutlineOrthoSensitivity);
        _outline = new MeshInstance3D
        {
            Name = "Outline",
            Mesh = new QuadMesh { Size = new Vector2(2f, 2f) },
            MaterialOverride = outline,
            Position = new Vector3(0f, 0f, -1f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        camera.AddChild(_outline);

        CurrentGround = style.StartGround switch { "facets" => Ground.Facets, "pixel_art" => Ground.PixelArt, _ => Ground.Smooth };
        Mid = style.StartMid && MidAvailable;
        Outlines = style.StartOutlines;
        Pixels = style.StartPixels;
        Toon = style.StartToon;
    }

    private void PlaceProps(ReliefArena arena, ReliefConfig.LowPolyStyle style, ReliefSim sim, Node3D world, IReadOnlyList<Vector2> keepClear,
        Vector2 spawn)
    {
        Node3D container = new() { Name = "Props" };
        world.AddChild(container);
        Vector3 scale = Vector3.One / style.MetresPerCell;
        Dictionary<string, ReliefLowPoly.Model> mids = new();
        foreach (ReliefLowPoly.Placement placement in ReliefLowPoly.PlaceProps(arena, style, sim, keepClear, spawn, 59))
        {
            if (!mids.TryGetValue(placement.Path, out ReliefLowPoly.Model mid))
            {
                mid = ReliefLowPoly.Exists($"mid/{placement.Path}") ? ReliefLowPoly.LoadModel($"mid/{placement.Path}") : placement.Model;
                mids[placement.Path] = mid;
            }
            MeshInstance3D body = new()
            {
                Mesh = placement.Model.Animation("idle")[0],
                Position = new Vector3(placement.At.X, arena.GroundAt(placement.At.X, placement.At.Y), placement.At.Y),
                RotationDegrees = new Vector3(0f, placement.YawDegrees, 0f),
                Scale = scale,
            };
            container.AddChild(body);
            _props.Add((body, new[] { placement.Model, mid }));
        }
        GD.Print($"[ReliefLook] {_props.Count} décors");
    }

    private int Detail => Mid ? 1 : 0;

    public ReliefLowPoly.Model PlayerModel => Player[Detail];

    public ReliefLowPoly.Model CreatureModel(int type) => Creatures[Detail][type];

    public void CycleGround() => CurrentGround = (Ground)(((int)CurrentGround + 1) % _grounds.Length);

    /// <summary>Met la scène au style courant.</summary>
    public void Apply()
    {
        Mid &= MidAvailable;
        for (int g = 0; g < _grounds.Length; g++)
            _grounds[g].Visible = g == (int)CurrentGround;
        foreach ((MeshInstance3D body, ReliefLowPoly.Model[] models) in _props)
            body.Mesh = models[Detail].Animation("idle")[0];
        _outline.Visible = Outlines;
        // Un trait de deux pixels en pleine résolution, d'un seul une fois pixelisé (il y grossit déjà quatre fois).
        ((ShaderMaterial)_outline.MaterialOverride).SetShaderParameter("thickness", Pixels ? 1f : 2f);
        foreach (BaseMaterial3D material in _shaded)
        {
            material.DiffuseMode = Toon ? BaseMaterial3D.DiffuseModeEnum.Toon : BaseMaterial3D.DiffuseModeEnum.Burley;
            material.SpecularMode = Toon ? BaseMaterial3D.SpecularModeEnum.Disabled : BaseMaterial3D.SpecularModeEnum.SchlickGgx;
        }
        _viewport.Size = Pixels ? _fullSize / _pixelScale : _fullSize;
        // Avec le MSAA, GL Compatibility ne donne aux contours qu'une profondeur où les petits corps manquent (mesuré) :
        // les contours passent au FXAA.
        _viewport.Msaa3D = Pixels || Outlines ? Viewport.Msaa.Disabled : Viewport.Msaa.Msaa4X;
        _viewport.ScreenSpaceAA = !Pixels && Outlines ? Viewport.ScreenSpaceAAEnum.Fxaa : Viewport.ScreenSpaceAAEnum.Disabled;
        _picture.TextureFilter = Pixels ? CanvasItem.TextureFilterEnum.Nearest : CanvasItem.TextureFilterEnum.Linear;
    }

    public string Describe() =>
        $"F1 sol : {GroundNames[(int)CurrentGround]} · F2 corps : {(Mid ? "mid poly" : MidAvailable ? "low poly" : "low poly (mid absent)")} · "
        + $"F3 contours : {(Outlines ? "oui" : "non")} · F4 pixels : {(Pixels ? "oui" : "non")} · F5 ombrage : {(Toon ? "toon" : "normal")}";
}
