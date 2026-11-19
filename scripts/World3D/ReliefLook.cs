using System.Collections.Generic;
using Godot;

namespace Vestiges.World3D;

/// <summary>
/// Les styles du bac à sable de la verticalité (plan 60, lot 60.0d), basculés en jeu, chacun indépendamment : le sol
/// texturé (falaises taillées ou droites), le détail des corps et des décors (low ou mid poly), les contours noirs,
/// l'ombrage (normal ou toon). La pixelisation de l'image a été écartée (§234), les sols en facettes, lisse et en tuiles
/// du jeu aussi (§238). Tout est construit au lancement ; basculer ne fait que montrer, cacher ou échanger des maillages
/// et régler les matériaux.
/// </summary>
public sealed class ReliefLook
{
    public enum Ground { Carved, Straight }

    private static readonly string[] GroundNames = { "texturé, falaises taillées", "texturé, falaises droites" };
    // Épaisseur des contours (px) pour une image de 1080 lignes : elle suit la définition, pour garder la même largeur à l'écran.
    private const float OutlineThickness1080 = 2f;
    private const string OutlineShader = "res://assets/shaders/world3d/relief_outline.gdshader";

    private readonly MeshInstance3D[] _grounds = new MeshInstance3D[2];
    private readonly ShaderMaterial _outlineMaterial;
    private readonly MeshInstance3D _outline;
    private readonly List<BaseMaterial3D> _shaded = new();
    private readonly List<(MeshInstance3D Body, ReliefLowPoly.Model[] Models)> _props = new();
    private readonly SubViewport _viewport;
    private readonly MultiMeshInstance3D _grass;

    public Ground CurrentGround;
    public bool Mid;
    public bool Outlines;
    public bool Toon;

    /// <summary>Le mid poly n'est pas versionné : sans lui, la bascule des corps reste en low poly.</summary>
    public bool MidAvailable { get; }

    /// <summary>Le joueur, en low [0] et mid [1] poly.</summary>
    public ReliefLowPoly.Model[] Player { get; } = new ReliefLowPoly.Model[2];

    /// <summary>Les créatures par type, en low [0] et mid [1] poly.</summary>
    public ReliefLowPoly.Model[][] Creatures { get; } = new ReliefLowPoly.Model[2][];

    public ReliefLook(ReliefConfig config, ReliefArena arena, Node3D world, Camera3D camera,
        SubViewport viewport, IReadOnlyList<Vector2> keepClear, ReliefBiomeProps biome, string playerModel = null)
    {
        ReliefConfig.LowPolyStyle style = config.LowPoly;
        _viewport = viewport;

        string character = $"characters/{playerModel ?? config.Character}";
        MidAvailable = ReliefLowPoly.Exists($"mid/{character}");
        Player[0] = ReliefLowPoly.LoadModel(character);
        Player[1] = MidAvailable ? ReliefLowPoly.LoadModel($"mid/{character}") : Player[0];
        // Une créature sans mid poly (non versionné, exporté à la demande) garde son low poly en mid.
        Creatures[0] = new ReliefLowPoly.Model[config.Creatures.Length];
        Creatures[1] = new ReliefLowPoly.Model[config.Creatures.Length];
        for (int t = 0; t < config.Creatures.Length; t++)
        {
            Crowd3D.Species creature = config.Creatures[t];
            // Le modèle porte le nom du dossier de sprites, pas toujours l'id de la fiche (fading_spitter : cracheur).
            string path = $"creatures/{(creature.ModelRoot != null ? creature.ModelRoot.GetFile() : creature.Id)}";
            Creatures[0][t] = creature.ModelRoot != null ? ReliefLowPoly.LoadModelAt(creature.ModelRoot) : ReliefLowPoly.LoadModel(path);
            Creatures[1][t] = ReliefLowPoly.Exists($"mid/{path}") ? ReliefLowPoly.LoadModel($"mid/{path}") : Creatures[0][t];
        }
        if (!MidAvailable)
            GD.PushWarning("[ReliefLook] Mid poly absent : python3 tools/export_lowpoly.py --detail mid");

        _grounds[(int)Ground.Carved] = ReliefTexturedGround.Build(arena, style.CarvedRelief);
        _grounds[(int)Ground.Straight] = ReliefTexturedGround.Build(arena, 0f);
        foreach (MeshInstance3D ground in _grounds)
            world.AddChild(ground);
        _shaded.AddRange(ReliefTexturedGround.Materials);
        _grass = ReliefGrass.Build(arena, style, 61);
        world.AddChild(_grass);
        _shaded.Add(ReliefGrass.Material);
        _shaded.Add(ReliefLowPoly.Plain);

        PlaceProps(arena, style, world, keepClear, config.Spawn, biome);

        // Quadrilatère plein écran, devant la caméra : son shader place lui-même ses sommets aux coins de l'image.
        _outlineMaterial = new ShaderMaterial { Shader = GD.Load<Shader>(OutlineShader), RenderPriority = (int)Material.RenderPriorityMax };
        _outlineMaterial.SetShaderParameter("line_color", style.OutlineColor);
        _outlineMaterial.SetShaderParameter("sensitivity", style.OutlineSensitivity);
        _outlineMaterial.SetShaderParameter("ortho_sensitivity", style.OutlineOrthoSensitivity);
        FitOutlines(viewport.Size.Y);
        _outline = new MeshInstance3D
        {
            Name = "Outline",
            Mesh = new QuadMesh { Size = new Vector2(2f, 2f) },
            MaterialOverride = _outlineMaterial,
            Position = new Vector3(0f, 0f, -1f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        camera.AddChild(_outline);

        CurrentGround = style.StartGround == "straight" ? Ground.Straight : Ground.Carved;
        Mid = style.StartMid && MidAvailable;
        Outlines = style.StartOutlines;
        Toon = style.StartToon;
    }

    private void PlaceProps(ReliefArena arena, ReliefConfig.LowPolyStyle style, Node3D world, IReadOnlyList<Vector2> keepClear,
        Vector2 spawn, ReliefBiomeProps biome)
    {
        Node3D container = new() { Name = "Props" };
        world.AddChild(container);
        Vector3 scale = Vector3.One / style.MetresPerCell;
        Dictionary<string, ReliefLowPoly.Model> mids = new();
        foreach (ReliefLowPoly.Placement placement in ReliefLowPoly.PlaceProps(arena, style, keepClear, spawn, 59, biome))
        {
            if (!mids.TryGetValue(placement.Path, out ReliefLowPoly.Model mid))
            {
                mid = ReliefLowPoly.Exists($"mid/{placement.Path}") ? ReliefLowPoly.LoadModel($"mid/{placement.Path}", true) : placement.Model;
                mids[placement.Path] = mid;
            }
            MeshInstance3D body = new()
            {
                Mesh = placement.Model.Animation("idle")[0],
                Position = new Vector3(placement.At.X, arena.GroundAt(placement.At.X, placement.At.Y), placement.At.Y),
                RotationDegrees = new Vector3(0f, placement.YawDegrees, 0f),
                Scale = scale,
                CastShadow = ReliefLowPoly.IsDecal(placement.Model)
                    ? GeometryInstance3D.ShadowCastingSetting.Off : GeometryInstance3D.ShadowCastingSetting.On,
            };
            container.AddChild(body);
            _props.Add((body, new[] { placement.Model, mid }));
            foreach ((Vector3 centre, Color color) in placement.Model.Glows)
                Glows.Add((body.Transform * centre, color));
        }
        GD.Print($"[ReliefLook] {_props.Count} décors");
    }

    public int PropCount => _props.Count;

    /// <summary>Les lueurs des décors posés (lanternes, champignons, cristaux), en coordonnées du monde.</summary>
    public List<(Vector3 At, Color Color)> Glows { get; } = new();

    /// <summary>Le pied du décor d'au moins <paramref name="minHeight"/> mètres le plus proche de <paramref name="at"/>.</summary>
    public Vector2? TallPropNear(Vector2 at, float minHeight)
    {
        Vector2? best = null;
        foreach ((MeshInstance3D body, ReliefLowPoly.Model[] models) in _props)
        {
            Vector2 foot = new(body.Position.X, body.Position.Z);
            if (models[0].Bounds.Size.Y >= minHeight && (best == null || foot.DistanceTo(at) < best.Value.DistanceTo(at)))
                best = foot;
        }
        return best;
    }

    private int Detail => Mid ? 1 : 0;

    public ReliefLowPoly.Model PlayerModel => Player[Detail];

    /// <summary>Les modèles des créatures, par sorte, au niveau de détail courant.</summary>
    public ReliefLowPoly.Model[] CreatureModels => Creatures[Detail];

    /// <summary>Règle l'épaisseur des contours sur la hauteur de l'image rendue.</summary>
    public void FitOutlines(int renderHeight) =>
        _outlineMaterial.SetShaderParameter("thickness", OutlineThickness1080 * renderHeight / 1080f);

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
        ReliefLowPoly.SetToon(Toon);
        foreach (BaseMaterial3D material in _shaded)
        {
            material.DiffuseMode = Toon ? BaseMaterial3D.DiffuseModeEnum.Toon : BaseMaterial3D.DiffuseModeEnum.Burley;
            material.SpecularMode = Toon ? BaseMaterial3D.SpecularModeEnum.Disabled : BaseMaterial3D.SpecularModeEnum.SchlickGgx;
        }
        // Avec le MSAA, GL Compatibility ne donne aux contours qu'une profondeur où les petits corps manquent (mesuré), et
        // il n'a pas de FXAA : avec les contours, l'image se rend à la définition de l'écran et le trait s'adoucit dans
        // son propre shader (§238).
        _viewport.Msaa3D = Outlines ? Viewport.Msaa.Disabled : Viewport.Msaa.Msaa4X;
    }

    public string Describe() =>
        $"F1 sol : {GroundNames[(int)CurrentGround]} · F2 corps : {(Mid ? "mid poly" : MidAvailable ? "low poly" : "low poly (mid absent)")} · "
        + $"F3 contours : {(Outlines ? "oui" : "non")} · F4 ombrage : {(Toon ? "toon" : "normal")}";
}
