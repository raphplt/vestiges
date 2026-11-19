using System.Globalization;
using Godot;

using Vestiges.World3D;

namespace Vestiges.Tests;

/// <summary>
/// Bac à sable de la verticalité (plan 59), à jouer à la main : une arène faite main (gradin, butte, marches, mesa sans
/// escalier, arête, fosse de vide), la horde, un bond sur Espace, et deux rendus de la même simulation, qu'on bascule à
/// la volée avec Tab :
/// <list type="bullet">
/// <item>caméra libre (la vue de départ, plan 60, §234) : caméra en perspective derrière le joueur, tournée à la souris
/// de gauche à droite et de haut en bas, rapprochée à la molette, comme dans Megabonk ; elle se rapproche seule quand le
/// relief s'interpose ;</item>
/// <item>relief 2D : caméra orthographique isométrique, réglée pour rendre les tuiles 64×32 du jeu pixel pour pixel.
/// C'est l'image qu'aurait le jeu actuel avec des étages.</item>
/// </list>
/// La vue 3D de haut, à caméra fixe, a été retirée (DECISIONS §234).
/// Tout est en low poly (plan 60, lot 60.0b), la pile 3D de la run (<c>Vestiges.World3D</c>, plan 60 §29) : relief en
/// facettes, modèles et décors exportés du pipeline des sprites ; la vue « relief 2D » y est une isométrique
/// orthographique éclairée. Les sprites du plan 59 sont partis avec la run 2D (§254).
/// Arguments après -- : --biome &lt;terre&gt; (décors de data/props tirés de build/models, plan 60 §19), --lod-threshold px, --view chase|2d, --character &lt;id&gt;, --enemies N, --seed S, --world-scale w, --effects liste, --check-edges (contrôle des rebords, sans rendu) ; captures : --capture &lt;dossier&gt; --seconds S (un bot fait le
/// tour de l'arène, garde des images des deux vues, puis ferme).
/// </summary>
public partial class ReliefSandbox : Node
{
    // La horde d'essai et l'arme ; le reste vient des réglages de la run 3D (data/world3d, plan 60 §29).
    private const string ConfigPath = "res://data/sandbox/relief.json";
    // Hauteur de l'image de la vue relief 2D, pour la taille de sa caméra orthographique : les tuiles 64 × 32 du jeu.
    private const float OrthoPixels = 540f;
    // Un losange de 64 px couvre la diagonale d'une cellule : 64 / √2 px par unité en orthographique.
    private const float PixelsPerUnit = 45.254834f;
    private const float IsoPitchDegrees = 30f;
    private const float OrthoDistance = 40f;
    private const float RotateDegreesPerSecond = 110f;
    private const float StompRingSeconds = 0.32f;
    private static readonly Color VoidColor = new("0b0812");

    private ReliefConfig _config;
    private ReliefArena _arena;
    private ReliefSim _sim;
    private SubViewport _viewport;
    private Camera3D _camera;
    private DirectionalLight3D _sun;
    private WorldEnvironment _environment;
    private Label _hud;
    private Sprite3D[] _projectiles;
    private Sprite3D _stompRing;
    private float _stompRingTime = -1f;

    private ReliefLook _look;
    private ReliefAtmosphere _atmosphere;
    private PlayerBody3D _playerView;
    private RunCamera3D _chase;
    private CrowdBodies3D _crowdBodies;
    private StandardMaterial3D _flashMaterial;
    private StandardMaterial3D _hurtMaterial;

    private enum View { Chase3D, Relief2D }

    private View _view;
    // Recul réel de la caméra libre : sa distance, raccourcie quand le relief s'interpose.
    private Vector3 _orthoTarget;
    private int _crowdTarget;
    private ReliefBiomeProps _biomeProps;
    // Mesure du tour du bot en capture (plan 60 §19, C4c) : durées d'image et primitives dessinées.
    private readonly System.Collections.Generic.List<float> _frameMs = new();
    private double _primitiveSum;
    private ulong _lastFrameUsec;
    // Prise « décor devant le joueur » de la capture : le bot s'arrête, l'effacement se coupe pour la comparaison.
    private bool _botFrozen;
    private bool _seeThroughOff;
    private string _biome = "semis du réglage";
    private bool _ready;
    // Couches hautes des décors (feuillages) : leur pied et leur décalage vers la caméra, refait quand elle bouge.
    private float _hudTimer;

    // Captures.
    private string _captureDir;
    private float _captureSeconds;
    private int _captureStep;
    private float _captureTimer;
    private bool _airCaptured;
    private int _waypoint;
    private Vector2 _botLastPos;
    private float _botStuckTimer;
    private static readonly Vector2[] BotTour =
    {
        // Toutes les falaises sont trop hautes pour le saut (§240) : chaque plateau par sa rampe, et retour par elle.
        new(20.5f, 13.5f), new(24f, 10.9f), new(15f, 10.9f), new(7f, 12.4f), new(7f, 6.5f), new(7f, 12.4f),
        new(15f, 10.9f), new(24f, 10.9f),
        new(28.9f, 20.5f), new(28.9f, 12.5f), new(33.9f, 12.4f), new(33.9f, 7f), new(33.9f, 12.4f), new(28.9f, 12.5f),
        new(28.9f, 20.5f),
        new(22f, 25.9f), new(34f, 26f), new(22f, 25.9f),
        new(24.9f, 30.4f), new(24.9f, 38.5f), new(24.9f, 30.4f),
        new(21.8f, 31.9f), new(8f, 32f), new(21.8f, 31.9f), new(20.5f, 18.5f),
    };

    public override void _Ready()
    {
        try
        {
            Setup();
        }
        catch (System.Exception exception)
        {
            GD.PushError($"[ReliefSandbox] {exception}");
            GetTree().Quit(1);
        }
    }

    private void Setup()
    {
        string[] args = OS.GetCmdlineUserArgs();
        _config = ReliefConfig.Load(ConfigPath);
        string character = Argument(args, "--character", null);
        if (character != null)
            _config.SetCharacter(character);
        _arena = new ReliefArena(_config.Arena, _config.LevelHeight);
        _crowdTarget = int.Parse(Argument(args, "--enemies", _config.CrowdCount.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
        ulong seed = ulong.Parse(Argument(args, "--seed", "59"), CultureInfo.InvariantCulture);
        float worldScale = float.Parse(Argument(args, "--world-scale",
            _config.LowPoly.WorldScale.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
        if (worldScale != 1f)
            _config.ScaleWorld(worldScale);
        _config.ScaleBodies(_config.LowPoly.BodyScale);
        if (System.Array.IndexOf(args, "--check-edges") >= 0)
        {
            GetTree().Quit(ReliefEdgeCheck.Run(_config, _arena) == 0 ? 0 : 1);
            return;
        }
        _sim = new ReliefSim(_config, _arena, Mathf.Max(_crowdTarget, 1) + 120, seed);
        _view = Argument(args, "--view", "chase") == "2d" ? View.Relief2D : View.Chase3D;
        _captureDir = Argument(args, "--capture", null);
        // La capture mesure les images par seconde : sans synchronisation verticale, sinon l'écran les plafonne.
        if (_captureDir != null)
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        _captureSeconds = float.Parse(Argument(args, "--seconds", "14"), CultureInfo.InvariantCulture);

        RegisterActions();
        string biome = Argument(args, "--biome", null);
        if (biome != null)
            ReliefTexturedGround.UseLand(biome);
        LoadMaterials();
        if (biome != null)
        {
            _biomeProps = new ReliefBiomeProps(biome, ProjectSettings.GlobalizePath("res://build/models"));
            _biome = biome;
        }
        // --effects ssao,fog,lights (ou none) : les effets de Forward+ allumés au lancement, à la place du réglage.
        string effects = Argument(args, "--effects", null);
        if (effects != null)
        {
            _config.Atmosphere.StartSsao = effects.Contains("ssao");
            _config.Atmosphere.StartFog = effects.Contains("fog");
            _config.Atmosphere.StartLights = effects.Contains("lights");
        }
        _config.LowPoly.LodThreshold = float.Parse(Argument(args, "--lod-threshold",
            _config.LowPoly.LodThreshold.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
        BuildScene();
        ApplyView();
        _chase.Snap(_sim.Player.WorldPosition);
        _orthoTarget = OrthoAnchor();
        _ready = true;
        GD.Print($"[ReliefSandbox] Arène {_arena.Width}×{_arena.Depth}, sol {_biome}, {_look.PropCount} décors, foule {_crowdTarget}, vue {_view}");
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return fallback;
    }

    /// <summary>Touches propres au bac à sable, déclarées ici pour ne pas encombrer la carte d'entrées du jeu.</summary>
    private static void RegisterActions()
    {
        AddKeyAction("relief_toggle_view", Key.Tab);
        AddKeyAction("relief_rotate_left", Key.Q);
        AddKeyAction("relief_rotate_right", Key.E);
        AddKeyAction("relief_crowd_more", Key.Equal);
        AddKeyAction("relief_crowd_less", Key.Minus);
        AddKeyAction("relief_respawn", Key.R);
        AddKeyAction("relief_style_ground", Key.F1);
        AddKeyAction("relief_style_models", Key.F2);
        AddKeyAction("relief_style_outlines", Key.F3);
        AddKeyAction("relief_style_toon", Key.F4);
        AddKeyAction("relief_effect_ssao", Key.F5);
        AddKeyAction("relief_effect_fog", Key.F6);
        AddKeyAction("relief_effect_lights", Key.F7);
        AddKeyAction("relief_skill", Key.Shift);
        AddMouseAction("relief_skill", MouseButton.Right);
    }

    private static void AddKeyAction(string action, Key key)
    {
        if (InputMap.HasAction(action))
            return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    private static void AddMouseAction(string action, MouseButton button) =>
        InputMap.ActionAddEvent(action, new InputEventMouseButton { ButtonIndex = button });

    private void LoadMaterials()
    {
        _flashMaterial = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1f, 0.97f, 0.9f) };
        _hurtMaterial = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.95f, 0.25f, 0.2f) };
    }

    private void BuildScene()
    {
        _viewport = new SubViewport
        {
            Name = "World",
            // Le low poly n'a pas de pixels à respecter : il se rend à la définition réelle de la fenêtre (§238), pour
            // qu'un écran 4K ne montre pas une image 1080p agrandie, aux contours en escalier.
            Size = WindowPixels(),
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            Msaa3D = Viewport.Msaa.Msaa4X,
            MeshLodThreshold = _config.LowPoly.LodThreshold,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        AddChild(_viewport);
        CanvasLayer screen = new() { Name = "Screen", Layer = -1 };
        AddChild(screen);
        TextureRect picture = new()
        {
            Name = "Picture",
            Texture = _viewport.GetTexture(),
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
        };
        picture.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        screen.AddChild(picture);

        Node3D world = new() { Name = "Arena" };
        _viewport.AddChild(world);
        _sun = new DirectionalLight3D
        {
            Name = "Sun",
            RotationDegrees = new Vector3(-52f, -35f, 0f),
            LightEnergy = 1.15f,
            LightColor = new Color("fff1dc"),
            ShadowEnabled = true,
            // La caméra orthographique se tient à 40 unités : ses ombres doivent porter au-delà.
            DirectionalShadowMaxDistance = 70f,
        };
        world.AddChild(_sun);
        _environment = new WorldEnvironment { Name = "Environment", Environment = new Godot.Environment() };
        world.AddChild(_environment);
        // Proche plan reculé : la profondeur garde sa précision au loin, où les contours la lisent.
        _camera = new Camera3D { Name = "Camera", Current = true, Near = 0.4f, Far = 200f };
        world.AddChild(_camera);
        _look = new ReliefLook(_config, _arena, world, _camera, _viewport, BotTour, _biomeProps);
        _atmosphere = new ReliefAtmosphere(_config.Atmosphere, world, _sim.ProjectilePos.Length, _look.Glows);
        GetWindow().SizeChanged += FitRender;

        _chase = new RunCamera3D(_camera, _config, _arena);
        _playerView = new PlayerBody3D(world, _config, _arena, _flashMaterial, _hurtMaterial);
        _crowdBodies = new CrowdBodies3D(world, _config, _arena, _sim.Crowd.Capacity, _flashMaterial);
        Texture2D bolt = BoltTexture();
        _projectiles = new Sprite3D[_sim.ProjectilePos.Length];
        for (int p = 0; p < _projectiles.Length; p++)
        {
            _projectiles[p] = NewSprite(world, $"Bolt{p}");
            _projectiles[p].Texture = bolt;
            _projectiles[p].Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            _projectiles[p].Visible = false;
        }
        _stompRing = new Sprite3D
        {
            Name = "StompRing",
            Texture = RingTexture(),
            PixelSize = 1f / 32f,
            Shaded = false,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Disabled,
            RotationDegrees = new Vector3(-90f, 0f, 0f),
            Visible = false,
        };
        world.AddChild(_stompRing);

        CanvasLayer hudLayer = new() { Name = "Hud", Layer = 5 };
        AddChild(hudLayer);
        _hud = new Label { Name = "Stats", Position = new Vector2(16f, 12f) };
        _hud.AddThemeFontSizeOverride("font_size", 22);
        _hud.AddThemeColorOverride("font_outline_color", Colors.Black);
        _hud.AddThemeConstantOverride("outline_size", 6);
        hudLayer.AddChild(_hud);
    }

    private static Sprite3D NewSprite(Node3D parent, string name)
    {
        Sprite3D sprite = new()
        {
            Name = name,
            PixelSize = 1f / PixelsPerUnit,
            Shaded = false,
            DoubleSided = true,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
        };
        parent.AddChild(sprite);
        return sprite;
    }

    /// <summary>Bascule entre le relief 2D (orthographique, sans lumière) et la 3D (perspective, lumière, brume).</summary>
    private bool Is3D => _view == View.Chase3D;

    private void SetView(View view)
    {
        _view = view;
        ApplyView();
    }

    private void ApplyView()
    {
        // Les reliefs de ReliefLook servent aux deux vues, toujours éclairés.
        _sun.Visible = true;
        if (_captureDir == null)
            Input.MouseMode = Is3D ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        Godot.Environment env = _environment.Environment;
        env.BackgroundMode = Godot.Environment.BGMode.Color;
        env.BackgroundColor = VoidColor;
        env.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        env.AmbientLightColor = new Color("8c86a8");
        env.AmbientLightEnergy = 0.55f;
        env.FogEnabled = Is3D;
        env.FogLightColor = VoidColor.Lerp(new Color("3a2f55"), 0.5f);
        env.FogDensity = 0.012f;
        _camera.Projection = Is3D ? Camera3D.ProjectionType.Perspective : Camera3D.ProjectionType.Orthogonal;
        _camera.Size = OrthoPixels / PixelsPerUnit;
        _camera.Fov = _config.ChaseFov;
        if (!Is3D)
            _chase.YawDegrees = 45f;
        _look.Apply();
        _atmosphere.Apply(env, Is3D);
    }

    public override void _ExitTree()
    {
        if (_look != null)
            GetWindow().SizeChanged -= FitRender;
    }

    public override void _Process(double delta)
    {
        if (!_ready)
            return;
        float dt = Mathf.Min((float)delta, 1f / 20f);
        HandleInput(dt);
        Vector2 wish = _captureDir != null ? _botFrozen ? Vector2.Zero : BotWish(dt) : _chase.ScreenToGround(Input.GetVector("move_left", "move_right", "move_up", "move_down"));
        if (_captureDir == null && Input.IsActionJustPressed("mobility"))
            _sim.Player.QueueJump();
        if (_captureDir == null && Input.IsActionJustPressed("relief_skill"))
            _sim.Player.QueueSkill();
        _sim.Step(dt, wish, _crowdTarget);
        UpdateCamera(dt);
        ReliefLowPoly.SetSeeThrough(_camera.GlobalPosition, ToWorld(_sim.Player.Position) + Vector3.Up * 0.5f,
            _seeThroughOff ? 0f : _config.LowPoly.SeeThroughRadius);
        UpdateBodies(dt);
        UpdateProjectiles();
        _atmosphere.Update(_sim.ProjectilePos, _sim.ProjectileActive, ToWorld);
        UpdateFeedback(dt);
        UpdateHud(dt);
        if (_captureDir != null)
            StepCapture(dt);
    }

    private void HandleInput(float dt)
    {
        if (Input.IsActionJustPressed("relief_toggle_view"))
            SetView(Is3D ? View.Relief2D : View.Chase3D);
        if (Is3D)
        {
            float turn = Input.GetAxis("relief_rotate_left", "relief_rotate_right");
            _chase.YawDegrees += turn * RotateDegreesPerSecond * dt;
        }
        if (Input.IsActionJustPressed("relief_crowd_more"))
            _crowdTarget = Mathf.Min(_sim.Crowd.Capacity, _crowdTarget + 10);
        if (Input.IsActionJustPressed("relief_crowd_less"))
            _crowdTarget = Mathf.Max(0, _crowdTarget - 10);
        if (Input.IsActionJustPressed("relief_respawn"))
            _sim.Player.Teleport(_config.Spawn);
        HandleStyleInput();
    }

    /// <summary>F1 à F7 : chaque effet du style se bascule seul, pour comparer les combinaisons.</summary>
    private void HandleStyleInput()
    {
        bool changed = true;
        if (Input.IsActionJustPressed("relief_style_ground"))
            _look.CycleGround();
        else if (Input.IsActionJustPressed("relief_style_models"))
            _look.Mid = !_look.Mid;
        else if (Input.IsActionJustPressed("relief_style_outlines"))
            _look.Outlines = !_look.Outlines;
        else if (Input.IsActionJustPressed("relief_style_toon"))
            _look.Toon = !_look.Toon;
        else if (Input.IsActionJustPressed("relief_effect_ssao"))
            _atmosphere.Ssao = !_atmosphere.Ssao;
        else if (Input.IsActionJustPressed("relief_effect_fog"))
            _atmosphere.Fog = !_atmosphere.Fog;
        else if (Input.IsActionJustPressed("relief_effect_lights"))
            _atmosphere.Lights = !_atmosphere.Lights;
        else
            changed = false;
        if (changed)
        {
            _look.Apply();
            _atmosphere.Apply(_environment.Environment, Is3D);
            _hudTimer = 0f;
        }
    }

    /// <summary>
    /// Caméra libre : la souris la tourne autour du joueur ; monter la souris abaisse la caméra et relève le regard vers
    /// l'horizon, comme dans les jeux à la troisième personne.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (Is3D && @event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _chase.Turn(motion.Relative);
        }
        if (@event.IsActionPressed("ui_cancel"))
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private static Vector3 ToWorld(Vector3 sim) => new(sim.X, sim.Z, sim.Y);

    private Vector3 OrthoAnchor() => _sim.Player.WorldPosition + new Vector3(0f, 0.4f, 0f);

    private void UpdateCamera(float dt)
    {
        if (Is3D)
        {
            _chase.Follow(dt, _sim.Player.WorldPosition);
            return;
        }
        _orthoTarget = _orthoTarget.Lerp(OrthoAnchor(), 1f - Mathf.Exp(-9f * dt));
        float yaw = Mathf.DegToRad(_chase.YawDegrees);
        float pitch = Mathf.DegToRad(IsoPitchDegrees);
        Vector3 back = new(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch));
        Vector3 target = _orthoTarget;
        _camera.Position = target + back * OrthoDistance;
        _camera.LookAt(target, Vector3.Up);
        // Orthographique : la caméra se cale sur la grille des pixels, sans quoi les tuiles scintillent.
        Basis basis = _camera.GlobalBasis;
        float right = target.Dot(basis.X);
        float up = target.Dot(basis.Y);
        float snappedRight = Mathf.Round(right * PixelsPerUnit) / PixelsPerUnit;
        float snappedUp = Mathf.Round(up * PixelsPerUnit) / PixelsPerUnit;
        _camera.Position += basis.X * (snappedRight - right) + basis.Y * (snappedUp - up);
    }

    private void UpdateProjectiles()
    {
        for (int p = 0; p < _projectiles.Length; p++)
        {
            bool active = _sim.ProjectileActive[p];
            _projectiles[p].Visible = active;
            if (active)
                _projectiles[p].Position = ToWorld(_sim.ProjectilePos[p]);
        }
    }

    /// <summary>
    /// Les corps low poly : la pose de l'animation en cours, tournés vers leur marche ; blanc au coup, rouge à la blessure.
    /// </summary>
    private void UpdateBodies(float dt)
    {
        _playerView.Update(dt, _sim.Player, _look.PlayerModel);

        _crowdBodies.Update(dt, _sim.Crowd, _look.CreatureModels);
    }

    private void UpdateFeedback(float dt)
    {
        if (_sim.Player.StompedThisStep)
        {
            _stompRingTime = 0f;
            _stompRing.Position = ToWorld(_sim.Player.LastStompAt) + new Vector3(0f, 0.02f, 0f);
        }
        if (_stompRingTime < 0f)
        {
            _stompRing.Visible = false;
            return;
        }
        _stompRingTime += dt;
        float t = _stompRingTime / StompRingSeconds;
        if (t >= 1f)
        {
            _stompRingTime = -1f;
            _stompRing.Visible = false;
            return;
        }
        float diameter = Mathf.Lerp(0.6f, _config.StompRadius * 2f, Mathf.Sqrt(t));
        _stompRing.Visible = true;
        _stompRing.Scale = Vector3.One * diameter / 2f;
        _stompRing.Modulate = new Color(1f, 0.92f, 0.7f, 1f - t);
    }

    private void UpdateHud(float dt)
    {
        _hudTimer -= dt;
        if (_hudTimer > 0f)
            return;
        _hudTimer = 0.25f;
        int alive = 0;
        int climbing = 0;
        for (int i = 0; i < _sim.Crowd.Capacity; i++)
        {
            if (_sim.Crowd.State[i] != Crowd3D.Life.Alive)
                continue;
            alive++;
            if (_sim.Crowd.Climbing[i])
                climbing++;
        }
        string view = Is3D
            ? $"CAMÉRA LIBRE  (Tab : relief 2D · souris : tourner et incliner ({_chase.PitchDegrees:0}°) · Échap libère la souris)"
            : "RELIEF 2D  (Tab : caméra libre)";
        _hud.Text = $"Vue : {view}" + (_biomeProps == null ? "" : $" · terre : {_biome}") + "\n"
            + _look.Describe() + "\n" + _atmosphere.Describe() + "\n"
            + $"{_config.Character} · Espace : saut · Maj ou clic droit : {_config.Mobility.Name} ({(_sim.Player.SkillCooldown > 0f ? $"{_sim.Player.SkillCooldown:0.0} s" : "prête")})"
            + $" · R : retour au départ · +/- : foule ({_crowdTarget})\n"
            + $"PV {Mathf.CeilToInt(_sim.Player.Hp)} · étage {_sim.Player.Level:0.0} · morts {_sim.Deaths}\n"
            + $"Éliminations {_sim.Crowd.Kills} (retombée {_sim.Crowd.KillsByStomp}, chute {_sim.Crowd.KillsByFall}, vide {_sim.Crowd.KillsByVoid})\n"
            + $"Sauts {_sim.Player.Jumps} · {_config.Mobility.Name} {_sim.Player.Skills} · retombées {_sim.Player.Stomps} · chutes dans le vide {_sim.Player.VoidFalls}\n"
            + $"Horde {alive}, dont {climbing} qui grimpent · {Engine.GetFramesPerSecond():0} i/s"
            + $" · {_look.PropCount} décors · {Primitives() / 1000:0} k triangles · niveaux de détail : "
            + (_viewport.MeshLodThreshold > 0f ? $"{_viewport.MeshLodThreshold:0.#} px" : "non");
    }

    /// <summary>Le bot des captures fait le tour de l'arène et bondit quand il bute ou qu'une marche est trop haute.</summary>
    private Vector2 BotWish(float dt)
    {
        Vector2 at = new(_sim.Player.Position.X, _sim.Player.Position.Y);
        Vector2 goal = BotTour[_waypoint];
        if (at.DistanceTo(goal) < 0.7f)
        {
            _waypoint = (_waypoint + 1) % BotTour.Length;
            goal = BotTour[_waypoint];
        }
        _botStuckTimer += dt;
        if (_botStuckTimer > 0.35f)
        {
            if (at.DistanceTo(_botLastPos) < 0.25f && _sim.Player.Grounded)
                _sim.Player.QueueJump();
            _botLastPos = at;
            _botStuckTimer = 0f;
        }
        Vector2 dir = (goal - at).Normalized();
        if (Is3D)
        {
            // Caméra derrière le bot : le « bas » de l'écran est l'opposé de sa marche.
            float behind = Mathf.RadToDeg(Mathf.Atan2(-dir.X, -dir.Y));
            _chase.YawDegrees = Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(_chase.YawDegrees), Mathf.DegToRad(behind), 1f - Mathf.Exp(-4f * dt)));
        }
        Vector2 ahead = at + dir * 0.9f;
        if (_sim.Player.Grounded && _arena.GroundAt(ahead.X, ahead.Y) > _sim.Player.Position.Z + _config.Step)
            _sim.Player.QueueJump();
        return dir;
    }

    private static long Primitives() =>
        (long)RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalPrimitivesInFrame);

    /// <summary>Le tour du bot, hors des deux premières secondes (chargement des shaders) : images par seconde et triangles.</summary>
    private void ReportMeasure()
    {
        if (_frameMs.Count == 0)
            return;
        _frameMs.Sort();
        float total = 0f;
        foreach (float ms in _frameMs)
            total += ms;
        float p99 = _frameMs[Mathf.Min(_frameMs.Count - 1, (int)(_frameMs.Count * 0.99f))];
        GD.Print($"[ReliefSandbox] mesure : terre {_biome}, {_look.PropCount} décors, "
            + $"seuil {_viewport.MeshLodThreshold:0.#} px, {_frameMs.Count} images, {1000f * _frameMs.Count / total:0.0} i/s, "
            + $"p99 {p99:0.0} ms, {_primitiveSum / _frameMs.Count / 1000:0} k triangles par image");
    }

    private void StepCapture(float dt)
    {
        _captureTimer += dt;
        ulong now = Time.GetTicksUsec();
        if (_captureStep == 0 && _captureTimer > 2f && _lastFrameUsec > 0)
        {
            _frameMs.Add((now - _lastFrameUsec) / 1000f);
            _primitiveSum += Primitives();
        }
        _lastFrameUsec = now;
        float airHeight = _sim.Player.Position.Z - _sim.Player.GroundUnder;
        if (!_airCaptured && _captureTimer > 3f && _sim.Player.AirborneFromJump && airHeight > 0.45f)
        {
            _airCaptured = true;
            Save("air");
        }
        switch (_captureStep)
        {
            case 0 when _captureTimer >= _captureSeconds:
                ReportMeasure();
                SetView(View.Chase3D);
                Advance();
                break;
            case 1 when _captureTimer >= 1.5f:
                Save("chase");
                SetPitch(_config.ChaseMaxPitch);
                Advance();
                break;
            case 2 when _captureTimer >= 0.6f:
                Save("chase_plongee");
                SetPitch(_config.ChaseMinPitch);
                Advance();
                break;
            case 3 when _captureTimer >= 0.6f:
                Save("chase_horizon");
                SetPitch(_config.ChasePitch);
                SetView(View.Relief2D);
                Advance();
                break;
            case 4 when _captureTimer >= 0.6f:
                Save("2d");
                SetView(View.Chase3D);
                // Au pied de la rampe du plateau sud-ouest, quatre étages, que le bot monte.
                _sim.Player.Teleport(new Vector2(21.8f, 31.9f));
                _waypoint = System.Array.IndexOf(BotTour, new Vector2(8f, 32f));
                Advance();
                break;
            case 5 when _captureTimer >= 2.5f:
                Save("chase_edge");
                SetView(View.Relief2D);
                Advance();
                break;
            case 6 when _captureTimer >= 0.6f:
                Save("2d_edge");
                _sim.Player.Teleport(new Vector2(5.5f, 4.5f));
                _waypoint = System.Array.IndexOf(BotTour, new Vector2(20.5f, 13.5f));
                Advance();
                break;
            case 7 when _captureTimer >= 0.5f:
                Save("2d_high");
                SetView(View.Chase3D);
                Advance();
                break;
            case 8 when _captureTimer >= 0.5f:
                Save("chase_high");
                // Un grand décor entre la caméra et le joueur : effacé en trame, puis plein pour comparer.
                Vector2? tall = _look.TallPropNear(_config.Spawn, 3f);
                if (tall != null)
                {
                    SetView(View.Chase3D);
                    _botFrozen = true;
                    float yaw = Mathf.DegToRad(_chase.YawDegrees);
                    _sim.Player.Teleport(tall.Value - new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)) * 1.8f);
                }
                Advance();
                break;
            case 9 when _captureTimer >= 1.2f:
                Save("occlusion");
                _seeThroughOff = true;
                Advance();
                break;
            case 10 when _captureTimer >= 0.3f:
                Save("occlusion_sans");
                _seeThroughOff = false;
                _botFrozen = false;
                _sim.Player.Teleport(_config.Spawn);
                _waypoint = 0;
                ShowGalleryStyle(0);
                Advance();
                break;
            // Galerie des styles (60.0d) : la même scène sous chaque combinaison.
            case 11 when _captureTimer >= 0.8f:
                Save(Gallery[_galleryIndex].Name);
                _galleryIndex++;
                if (_galleryIndex >= Gallery.Length)
                {
                    if (!_atmosphere.Supported)
                    {
                        Finish();
                        break;
                    }
                    ShowGalleryStyle(0);
                    ShowAtmosphere(0);
                    // La même scène sous chaque effet : le bot s'arrête, la horde et les tirs continuent.
                    _botFrozen = true;
                    Advance();
                    break;
                }
                ShowGalleryStyle(_galleryIndex);
                _captureTimer = 0f;
                break;
            // Les effets de Forward+ (60.0f), un à un puis ensemble, sur la même scène.
            case 12 when _captureTimer >= 0.8f:
                Save(AtmosphereGallery[_atmosphereIndex].Name);
                _atmosphereIndex++;
                if (_atmosphereIndex >= AtmosphereGallery.Length)
                {
                    Finish();
                    break;
                }
                ShowAtmosphere(_atmosphereIndex);
                _captureTimer = 0f;
                break;
        }
    }

    private void SetPitch(float degrees) => _chase.SetPitch(degrees);

    private static readonly (string Name, View View, ReliefLook.Ground Ground, bool Mid, bool Outlines, bool Toon)[] Gallery =
    {
        ("style_taille_mid_contours", View.Chase3D, ReliefLook.Ground.Carved, true, true, false),
        ("style_taille_mid_contours_toon", View.Chase3D, ReliefLook.Ground.Carved, true, true, true),
        ("style_iso_taille_mid_contours", View.Relief2D, ReliefLook.Ground.Carved, true, true, false),
        ("style_droit_mid_contours", View.Chase3D, ReliefLook.Ground.Straight, true, true, false),
        ("style_taille_low_contours", View.Chase3D, ReliefLook.Ground.Carved, false, true, false),
        ("style_taille_low_contours_toon", View.Chase3D, ReliefLook.Ground.Carved, false, true, true),
        ("style_taille_mid_sans_contours", View.Chase3D, ReliefLook.Ground.Carved, true, false, false),
    };

    private int _galleryIndex;

    private static readonly (string Name, bool Ssao, bool Fog, bool Lights)[] AtmosphereGallery =
    {
        ("forward_sans_effets", false, false, false),
        ("forward_ombres_contact", true, false, false),
        ("forward_brume", false, true, false),
        ("forward_lumieres", false, false, true),
        ("forward_tout", true, true, true),
    };

    private int _atmosphereIndex;

    private void ShowAtmosphere(int index)
    {
        (_, _atmosphere.Ssao, _atmosphere.Fog, _atmosphere.Lights) = AtmosphereGallery[index];
        _atmosphere.Apply(_environment.Environment, Is3D);
    }

    private void ShowGalleryStyle(int index)
    {
        (_, View view, ReliefLook.Ground ground, bool mid, bool outlines, bool toon) = Gallery[index];
        _look.CurrentGround = ground;
        _look.Mid = mid;
        _look.Outlines = outlines;
        _look.Toon = toon;
        SetView(view);
    }

    private void Finish()
    {
        GD.Print($"[ReliefSandbox] RESULT seconds={_sim.Elapsed:0.0} kills={_sim.Crowd.Kills} stomp={_sim.Crowd.KillsByStomp} fall={_sim.Crowd.KillsByFall} void={_sim.Crowd.KillsByVoid} bonds={_sim.Player.Jumps} stomps={_sim.Player.Stomps} deaths={_sim.Deaths} void_falls={_sim.Player.VoidFalls}");
        GetTree().Quit();
    }

    private Vector2I WindowPixels()
    {
        Vector2I size = GetWindow().Size;
        return new Vector2I(Mathf.Max(size.X, 1), Mathf.Max(size.Y, 1));
    }

    /// <summary>L'image 3D suit la fenêtre, et les contours sa définition.</summary>
    private void FitRender()
    {
        Vector2I size = WindowPixels();
        _viewport.Size = size;
        _look.FitOutlines(size.Y);
    }

    private void Advance()
    {
        _captureStep++;
        _captureTimer = 0f;
    }

    private void Save(string name)
    {
        DirAccess.MakeDirRecursiveAbsolute(_captureDir);
        string path = $"{_captureDir}/{name}.png";
        GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"[ReliefSandbox] Capture : {path} (étage {_sim.Player.Level:0.0}, éliminations {_sim.Crowd.Kills})");
    }

    private static ImageTexture BoltTexture()
    {
        Image image = Image.CreateEmpty(5, 5, false, Image.Format.Rgba8);
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                int d = Mathf.Abs(x - 2) + Mathf.Abs(y - 2);
                image.SetPixel(x, y, d == 0 ? Colors.White : d == 1 ? new Color("ffe08a") : d == 2 ? new Color("e07b39") : Colors.Transparent);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }

    private static ImageTexture RingTexture()
    {
        const int size = 64;
        Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).Length();
                image.SetPixel(x, y, d > size / 2f - 4f && d < size / 2f - 1f ? Colors.White : Colors.Transparent);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }
}
