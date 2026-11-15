using System.Globalization;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Bac à sable de la verticalité (plan 59), à jouer à la main : une arène faite main (gradin, butte, marches, mesa sans
/// escalier, arête, fosse de vide), la horde, un bond sur Espace, et deux rendus de la même simulation, qu'on bascule à
/// la volée avec Tab :
/// <list type="bullet">
/// <item>relief 2D : caméra orthographique isométrique, sans lumière, réglée pour rendre les tuiles 64×32 du jeu pixel
/// pour pixel. C'est l'image qu'aurait le jeu actuel avec des étages ;</item>
/// <item>3D vue de haut : caméra en perspective qu'on tourne (Q/E) et qu'on rapproche (molette), lumière, ombres portées,
/// brume ;</item>
/// <item>3D en poursuite : caméra basse derrière le joueur, tournée à la souris, comme dans Megabonk.</item>
/// </list>
/// Par défaut, tout est en low poly (plan 60, lot 60.0b, <see cref="ReliefLowPoly"/>) : relief en facettes, modèles et
/// décors exportés du pipeline des sprites ; la vue « relief 2D » y devient une isométrique orthographique éclairée.
/// Avec <c>--sprites</c>, le rendu du plan 59 : sprites, et sol et décors d'un vrai biome (<see cref="ReliefBake"/>),
/// la run générée sur la terre de <c>--land-order</c>, photographiée, puis libérée ; <c>--tiles</c> garde alors les
/// tuiles simples, sans run.
/// Arguments après -- : --view 2d|3d|chase, --enemies N, --seed S, --sprites, --land-order &lt;biome&gt;, --tiles, --check-edges (contrôle des rebords, sans rendu) ; captures : --capture &lt;dossier&gt; --seconds S (un bot fait le
/// tour de l'arène, garde des images des deux vues, puis ferme).
/// </summary>
public partial class ReliefSandbox : Node
{
    private const string ConfigPath = "res://data/sandbox/relief.json";
    private static readonly Vector2I RenderSize = new(960, 540);
    // Le low poly n'a pas de pixels à respecter : il se rend à la taille de la fenêtre, lissé.
    private static readonly Vector2I LowPolyRenderSize = new(1920, 1080);
    private const float PlayerWalkRate = 2f;
    private const float TurnRate = 14f;
    // Un losange de 64 px couvre la diagonale d'une cellule : 64 / √2 px par unité en orthographique.
    private const float PixelsPerUnit = 45.254834f;
    private const float IsoPitchDegrees = 30f;
    private const float OrthoDistance = 40f;
    private const float SpriteNudge = 0.18f;
    private const float AnimFps = 9f;
    private const float RotateDegreesPerSecond = 110f;
    private const float StompRingSeconds = 0.32f;
    private static readonly string[] Directions = { "E", "SE", "S", "SW", "W", "NW", "N", "NE" };
    private static readonly Color VoidColor = new("0b0812");

    private ReliefConfig _config;
    private ReliefArena _arena;
    private ReliefSim _sim;
    private SubViewport _viewport;
    private Camera3D _camera;
    private DirectionalLight3D _sun;
    private WorldEnvironment _environment;
    private MeshInstance3D _flatTerrain;
    private MeshInstance3D _litTerrain;
    private Label _hud;
    private Sprite3D _player;
    private Sprite3D _playerShadow;
    private Sprite3D[] _enemies;
    private Sprite3D[] _enemyShadows;
    private Sprite3D[] _projectiles;
    private Sprite3D _stompRing;
    private float _stompRingTime = -1f;

    private Texture2D[,][] _playerWalk;
    private Texture2D[,][] _playerBond;
    private Texture2D[][,][] _creatureWalk;
    private Texture2D[][,][] _creatureDeath;
    private float _playerFoot;
    private float[] _creatureFoot;

    // Rendu low poly (plan 60, lot 60.0b).
    private bool _lowPoly;
    private ReliefLowPoly.Model _playerModel;
    private ReliefLowPoly.Model[] _creatureModels;
    private MeshInstance3D _playerBody;
    private MeshInstance3D[] _creatureBodies;
    private float _playerYaw;
    private float[] _creatureYaw;
    private StandardMaterial3D _flashMaterial;
    private StandardMaterial3D _hurtMaterial;

    private enum View { Relief2D, Iso3D, Chase3D }

    private View _view;
    private float _yawDegrees = 45f;
    private float _distance;
    private Vector3 _cameraTarget;
    private int _crowdTarget;
    private ReliefBake _bake;
    private string _biome = "tuiles simples";
    private Color _tint = Colors.White;
    private bool _ready;
    private readonly System.Collections.Generic.List<(Sprite3D Sprite, Vector2 Scale)> _props = new();
    // Couches hautes des décors (feuillages) : leur pied et leur décalage vers la caméra, refait quand elle bouge.
    private readonly System.Collections.Generic.List<(Sprite3D Sprite, Vector3 Foot, int Layer)> _layered = new();
    private const float LayerNudge = 0.03f;
    private float _hudTimer;
    private float _animClock;

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
        new(20.5f, 13.5f), new(24.8f, 13.5f), new(27.6f, 11.6f), new(31.2f, 11.6f), new(34f, 12f), new(34f, 17.5f),
        new(36f, 22f), new(34.5f, 30.5f), new(34.5f, 35.5f), new(26f, 30.5f), new(21f, 30.5f), new(17f, 30f),
        new(11f, 29.5f), new(9f, 30f), new(15f, 25f), new(20.5f, 18.5f),
        // Le haut palier par son escalier, puis la tour par le sien : trop hauts pour le bond.
        new(22.5f, 8f), new(14.5f, 8f), new(14.5f, 5f), new(5.5f, 4.5f),
    };

    public override async void _Ready()
    {
        try
        {
            await Setup();
        }
        catch (System.Exception exception)
        {
            GD.PushError($"[ReliefSandbox] {exception}");
            GetTree().Quit(1);
        }
    }

    private async System.Threading.Tasks.Task Setup()
    {
        string[] args = OS.GetCmdlineUserArgs();
        _config = ReliefConfig.Load(ConfigPath);
        _arena = new ReliefArena(_config.Arena, _config.LevelHeight);
        _crowdTarget = int.Parse(Argument(args, "--enemies", _config.CrowdCount.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
        ulong seed = ulong.Parse(Argument(args, "--seed", "59"), CultureInfo.InvariantCulture);
        if (System.Array.IndexOf(args, "--check-edges") >= 0)
        {
            GetTree().Quit(ReliefEdgeCheck.Run(_config, _arena) == 0 ? 0 : 1);
            return;
        }
        _sim = new ReliefSim(_config, _arena, Mathf.Max(_crowdTarget, 1) + 120, seed);
        _view = Argument(args, "--view", "2d") switch { "3d" => View.Iso3D, "chase" => View.Chase3D, _ => View.Relief2D };
        _captureDir = Argument(args, "--capture", null);
        _captureSeconds = float.Parse(Argument(args, "--seconds", "14"), CultureInfo.InvariantCulture);
        _distance = _config.CameraDistance;

        RegisterActions();
        _lowPoly = System.Array.IndexOf(args, "--sprites") < 0;
        if (_lowPoly)
        {
            LoadModels();
            _biome = "low poly";
        }
        else
        {
            LoadSprites();
        }
        if (!_lowPoly && System.Array.IndexOf(args, "--tiles") < 0)
        {
            _biome = Argument(args, "--land-order", "forest_reclaimed");
            _bake = await ReliefBake.Capture(this, _arena.Width, _arena.Depth, seed, _config.Character);
            _tint = _bake.CanvasTint;
        }
        BuildScene();
        ApplyView();
        _cameraTarget = PlayerAnchor();
        _ready = true;
        GD.Print($"[ReliefSandbox] Arène {_arena.Width}×{_arena.Depth}, sol {_biome}, {_props.Count} décors, foule {_crowdTarget}, vue {_view}");
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
        AddWheelAction("relief_zoom_in", MouseButton.WheelUp);
        AddWheelAction("relief_zoom_out", MouseButton.WheelDown);
    }

    private static void AddKeyAction(string action, Key key)
    {
        if (InputMap.HasAction(action))
            return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    private static void AddWheelAction(string action, MouseButton button)
    {
        if (InputMap.HasAction(action))
            return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventMouseButton { ButtonIndex = button });
    }

    private void LoadModels()
    {
        _playerModel = ReliefLowPoly.LoadModel($"characters/{_config.Character}");
        _creatureModels = new ReliefLowPoly.Model[_config.Creatures.Length];
        for (int t = 0; t < _creatureModels.Length; t++)
            _creatureModels[t] = ReliefLowPoly.LoadModel($"creatures/{_config.Creatures[t].Id}");
        _flashMaterial = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1f, 0.97f, 0.9f) };
        _hurtMaterial = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.95f, 0.25f, 0.2f) };
    }

    private void LoadSprites()
    {
        string id = _config.Character;
        _playerWalk = LoadFrames($"res://assets/characters/{id}/char_{id}", "walk");
        _playerBond = LoadFrames($"res://assets/characters/{id}/char_{id}", "dash");
        _playerFoot = FootOf(_playerWalk[1, 0][0]);
        int count = _config.Creatures.Length;
        _creatureWalk = new Texture2D[count][,][];
        _creatureDeath = new Texture2D[count][,][];
        _creatureFoot = new float[count];
        for (int t = 0; t < count; t++)
        {
            string creature = _config.Creatures[t].Id;
            _creatureWalk[t] = LoadFrames($"res://assets/enemies/{creature}/enemy_{creature}", "walk");
            _creatureDeath[t] = LoadFrames($"res://assets/enemies/{creature}/enemy_{creature}", "death");
            _creatureFoot[t] = FootOf(_creatureWalk[t][1, 0][0]);
        }
    }

    /// <summary>Images [direction, 0][image] d'une animation à huit directions et quatre images.</summary>
    private static Texture2D[,][] LoadFrames(string prefix, string anim)
    {
        Texture2D[,][] frames = new Texture2D[Directions.Length, 1][];
        for (int d = 0; d < Directions.Length; d++)
        {
            frames[d, 0] = new Texture2D[4];
            for (int f = 0; f < 4; f++)
                frames[d, 0][f] = GD.Load<Texture2D>($"{prefix}_{Directions[d]}_{anim}_0{f + 1}.png");
        }
        return frames;
    }

    /// <summary>Décalage vertical (px) qui pose la dernière rangée opaque de l'image sur le point du sol.</summary>
    private static float FootOf(Texture2D texture)
    {
        Image image = texture.GetImage();
        if (image.IsCompressed())
            image.Decompress();
        int bottom = image.GetHeight() - 1;
        for (int y = image.GetHeight() - 1; y >= 0; y--)
        {
            bool opaque = false;
            for (int x = 0; x < image.GetWidth() && !opaque; x++)
                opaque = image.GetPixel(x, y).A > 0.5f;
            if (opaque)
            {
                bottom = y;
                break;
            }
        }
        return bottom + 1f - image.GetHeight() / 2f;
    }

    private void BuildScene()
    {
        _viewport = new SubViewport
        {
            Name = "World",
            Size = _lowPoly ? LowPolyRenderSize : RenderSize,
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            Msaa3D = _lowPoly ? Viewport.Msaa.Msaa4X : Viewport.Msaa.Disabled,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        AddChild(_viewport);
        CanvasLayer screen = new() { Name = "Screen", Layer = -1 };
        AddChild(screen);
        TextureRect picture = new()
        {
            Name = "Picture",
            Texture = _viewport.GetTexture(),
            TextureFilter = _lowPoly ? CanvasItem.TextureFilterEnum.Linear : CanvasItem.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
        };
        picture.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        screen.AddChild(picture);

        Node3D world = new() { Name = "Arena" };
        _viewport.AddChild(world);
        if (_lowPoly)
        {
            _litTerrain = ReliefLowPoly.BuildTerrain(_arena, _config.LowPoly);
            world.AddChild(_litTerrain);
            PlaceLowPolyProps(world);
        }
        else
        {
            _flatTerrain = ReliefTerrainBuilder.Build(_arena, _config, false, _bake);
            _litTerrain = ReliefTerrainBuilder.Build(_arena, _config, true, _bake);
            world.AddChild(_flatTerrain);
            world.AddChild(_litTerrain);
            if (_bake != null)
                PlaceUprights(world);
        }

        _sun = new DirectionalLight3D
        {
            Name = "Sun",
            RotationDegrees = new Vector3(-52f, -35f, 0f),
            LightEnergy = 1.15f,
            LightColor = new Color("fff1dc"),
            ShadowEnabled = true,
            // La caméra orthographique se tient à 40 unités : ses ombres doivent porter au-delà.
            DirectionalShadowMaxDistance = _lowPoly ? 70f : 40f,
        };
        world.AddChild(_sun);
        _environment = new WorldEnvironment { Name = "Environment", Environment = new Godot.Environment() };
        world.AddChild(_environment);
        _camera = new Camera3D { Name = "Camera", Current = true, Near = 0.1f, Far = 200f };
        world.AddChild(_camera);

        Texture2D shadow = ShadowTexture();
        _playerShadow = NewShadow(world, shadow, "PlayerShadow");
        _enemyShadows = new Sprite3D[_sim.Capacity];
        for (int i = 0; i < _sim.Capacity; i++)
            _enemyShadows[i] = NewShadow(world, shadow, $"CreatureShadow{i}");
        if (_lowPoly)
        {
            Vector3 scale = Vector3.One / _config.LowPoly.MetresPerCell;
            _playerBody = new MeshInstance3D { Name = "Player", Scale = scale };
            world.AddChild(_playerBody);
            _creatureBodies = new MeshInstance3D[_sim.Capacity];
            _creatureYaw = new float[_sim.Capacity];
            for (int i = 0; i < _sim.Capacity; i++)
            {
                _creatureBodies[i] = new MeshInstance3D { Name = $"Creature{i}", Scale = scale, Visible = false };
                world.AddChild(_creatureBodies[i]);
            }
        }
        else
        {
            _player = NewSprite(world, "Player");
            _enemies = new Sprite3D[_sim.Capacity];
            for (int i = 0; i < _sim.Capacity; i++)
                _enemies[i] = NewSprite(world, $"Creature{i}");
        }
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

    /// <summary>
    /// Les vrais décors du biome, debout à leur point au sol, sur le relief. Ceux qui tombent sur une marche, un bord de
    /// falaise ou le vide sont écartés ; ceux qui bloquent en jeu deviennent des obstacles de la simulation.
    /// </summary>
    private void PlaceUprights(Node3D world)
    {
        Node3D container = new() { Name = "Props" };
        world.AddChild(container);
        foreach (ReliefBake.Upright upright in _bake.Uprights)
        {
            Vector2 at = PixelToGround(upright.Base);
            if (!IsFlatInterior(at))
                continue;
            float ground = _arena.GroundAt(at.X, at.Y);
            Vector2 scale = new(Mathf.Max(0.01f, Mathf.Abs(upright.Scale.X)), Mathf.Max(0.01f, Mathf.Abs(upright.Scale.Y)));
            Sprite3D sprite = new()
            {
                Texture = upright.Texture,
                PixelSize = 1f / PixelsPerUnit,
                Shaded = false,
                DoubleSided = true,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
                FlipH = upright.FlipH,
                RegionEnabled = upright.RegionEnabled,
                RegionRect = upright.Region,
                Offset = new Vector2(upright.CenterFromBase.X / scale.X, -upright.CenterFromBase.Y / scale.Y),
                Modulate = upright.Modulate * _tint,
                Position = new Vector3(at.X, ground, at.Y),
                // Les ombres des décors sont déjà dans le sol photographié.
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            container.AddChild(sprite);
            _props.Add((sprite, scale));
            if (upright.Layer > 0)
                _layered.Add((sprite, sprite.Position, upright.Layer));
            if (upright.GroundRadius > 0f)
                _sim.AddObstacle(at, upright.GroundRadius / PixelsPerUnit);
        }
        _sim.FinishObstacles();
    }

    /// <summary>Décors low poly semés à l'écart du tour du bot, et la maison ; leurs emprises bloquent la simulation.</summary>
    private void PlaceLowPolyProps(Node3D world)
    {
        Node3D container = new() { Name = "Props" };
        world.AddChild(container);
        Vector3 scale = Vector3.One / _config.LowPoly.MetresPerCell;
        foreach (ReliefLowPoly.Placement placement in ReliefLowPoly.PlaceProps(_arena, _config.LowPoly, _sim, BotTour, _config.Spawn, 59))
        {
            container.AddChild(new MeshInstance3D
            {
                Mesh = placement.Model.Animation("idle")[0],
                Position = new Vector3(placement.At.X, _arena.GroundAt(placement.At.X, placement.At.Y), placement.At.Y),
                RotationDegrees = new Vector3(0f, placement.YawDegrees, 0f),
                Scale = scale,
            });
        }
        GD.Print($"[ReliefSandbox] {container.GetChildCount()} décors low poly");
    }

    /// <summary>Point du monde du jeu (px) vers le sol de l'arène : l'inverse de la projection 64 × 32.</summary>
    private Vector2 PixelToGround(Vector2 pixel)
    {
        float a = (pixel.X - _bake.Origin.X) / 32f;
        float b = (pixel.Y - _bake.Origin.Y) / 16f;
        return new Vector2((a + b) / 2f, (b - a) / 2f);
    }

    /// <summary>Cellule d'un étage plein, entourée de cellules à la même hauteur : ni marche, ni bord, ni vide.</summary>
    private bool IsFlatInterior(Vector2 at)
    {
        int x = Mathf.FloorToInt(at.X);
        int y = Mathf.FloorToInt(at.Y);
        float h = _arena.CellHeight(x, y);
        if (ReliefArena.IsVoid(h) || Mathf.Abs(h / _arena.LevelHeight - Mathf.Round(h / _arena.LevelHeight)) > 0.01f)
            return false;
        return _arena.CellHeight(x + 1, y) == h && _arena.CellHeight(x - 1, y) == h
            && _arena.CellHeight(x, y + 1) == h && _arena.CellHeight(x, y - 1) == h;
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

    private static Sprite3D NewShadow(Node3D parent, Texture2D texture, string name)
    {
        Sprite3D shadow = new()
        {
            Name = name,
            Texture = texture,
            PixelSize = 1f / 26f,
            Shaded = false,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Disabled,
            RotationDegrees = new Vector3(-90f, 0f, 0f),
            Modulate = new Color(1f, 1f, 1f, 0.55f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(shadow);
        return shadow;
    }

    /// <summary>Bascule entre le relief 2D (orthographique, sans lumière) et la 3D (perspective, lumière, brume).</summary>
    private bool Is3D => _view != View.Relief2D;

    private float PitchDegrees => _view switch
    {
        View.Iso3D => _config.CameraPitch,
        View.Chase3D => _config.ChasePitch,
        _ => IsoPitchDegrees,
    };

    private void SetView(View view)
    {
        _view = view;
        ApplyView();
    }

    private void ApplyView()
    {
        // En low poly, un seul relief éclairé sert aux trois vues.
        if (_flatTerrain != null)
            _flatTerrain.Visible = !Is3D;
        _litTerrain.Visible = Is3D || _lowPoly;
        _sun.Visible = Is3D || _lowPoly;
        if (_captureDir == null)
            Input.MouseMode = _view == View.Chase3D ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
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
        _camera.Size = RenderSize.Y / PixelsPerUnit;
        _camera.Fov = _view == View.Chase3D ? _config.ChaseFov : _config.CameraFov;
        if (!Is3D)
            _yawDegrees = 45f;
        if (_lowPoly)
            return;
        // Les sprites restent debout ; on compense l'écrasement vertical de la caméra pour garder des pixels carrés.
        float pitch = Mathf.DegToRad(PitchDegrees);
        Vector3 stretch = new(1f, 1f / Mathf.Cos(pitch), 1f);
        _player.Scale = stretch;
        foreach (Sprite3D enemy in _enemies)
            enemy.Scale = stretch;
        foreach ((Sprite3D sprite, Vector2 scale) in _props)
            sprite.Scale = new Vector3(scale.X, scale.Y * stretch.Y, 1f);
    }

    public override void _Process(double delta)
    {
        if (!_ready)
            return;
        float dt = Mathf.Min((float)delta, 1f / 20f);
        _animClock += dt;
        HandleInput(dt);
        Vector2 wish = _captureDir != null ? BotWish(dt) : ScreenToGround(Input.GetVector("move_left", "move_right", "move_up", "move_down"));
        if (_captureDir == null && Input.IsActionJustPressed("mobility"))
            _sim.QueueBond();
        _sim.Step(dt, wish, _crowdTarget);
        UpdateCamera(dt);
        if (_lowPoly)
            UpdateBodies(dt);
        else
            UpdateSprites();
        UpdateProjectiles();
        UpdateFeedback(dt);
        UpdateHud(dt);
        if (_captureDir != null)
            StepCapture(dt);
    }

    private void HandleInput(float dt)
    {
        if (Input.IsActionJustPressed("relief_toggle_view"))
            SetView((View)(((int)_view + 1) % 3));
        if (Is3D)
        {
            float turn = Input.GetAxis("relief_rotate_left", "relief_rotate_right");
            _yawDegrees += turn * RotateDegreesPerSecond * dt;
            if (Input.IsActionJustPressed("relief_zoom_in"))
                _distance = Mathf.Max(_config.CameraMinDistance, _distance * 0.88f);
            if (Input.IsActionJustPressed("relief_zoom_out"))
                _distance = Mathf.Min(_config.CameraMaxDistance, _distance / 0.88f);
        }
        if (Input.IsActionJustPressed("relief_crowd_more"))
            _crowdTarget = Mathf.Min(_sim.Capacity, _crowdTarget + 10);
        if (Input.IsActionJustPressed("relief_crowd_less"))
            _crowdTarget = Mathf.Max(0, _crowdTarget - 10);
        if (Input.IsActionJustPressed("relief_respawn"))
            _sim.Teleport(_config.Spawn);
    }

    /// <summary>En poursuite, la souris tourne la caméra autour du joueur.</summary>
    public override void _Input(InputEvent @event)
    {
        if (_view == View.Chase3D && @event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
            _yawDegrees -= motion.Relative.X * _config.ChaseMouseDegrees;
        if (@event.IsActionPressed("ui_cancel"))
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    /// <summary>Entrée à l'écran vers le sol : la droite et le bas de l'écran projetés sur le plan, selon la caméra.</summary>
    private Vector2 ScreenToGround(Vector2 input)
    {
        float yaw = Mathf.DegToRad(_yawDegrees);
        // La caméra regarde depuis (sin yaw, cos yaw) vers le centre : le bas de l'écran pointe vers elle.
        Vector2 down = new(Mathf.Sin(yaw), Mathf.Cos(yaw));
        Vector2 right = new(down.Y, -down.X);
        Vector2 wish = right * input.X + down * input.Y;
        return wish.LengthSquared() > 1f ? wish.Normalized() : wish;
    }

    private Vector3 PlayerAnchor() => ToWorld(_sim.PlayerPos) + new Vector3(0f, _view == View.Chase3D ? _config.ChaseHeight : 0.4f, 0f);

    private static Vector3 ToWorld(Vector3 sim) => new(sim.X, sim.Z, sim.Y);

    private void UpdateCamera(float dt)
    {
        _cameraTarget = _cameraTarget.Lerp(PlayerAnchor(), 1f - Mathf.Exp(-9f * dt));
        float yaw = Mathf.DegToRad(_yawDegrees);
        float pitch = Mathf.DegToRad(PitchDegrees);
        Vector3 back = new(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch));
        Vector3 target = _cameraTarget;
        float distance = _view switch
        {
            View.Iso3D => _distance,
            View.Chase3D => _config.ChaseDistance,
            _ => OrthoDistance,
        };
        _camera.Position = target + back * distance;
        _camera.LookAt(target, Vector3.Up);
        if (Is3D)
            return;
        // Orthographique : la caméra se cale sur la grille des pixels, sans quoi les tuiles scintillent.
        Basis basis = _camera.GlobalBasis;
        float right = target.Dot(basis.X);
        float up = target.Dot(basis.Y);
        float snappedRight = Mathf.Round(right * PixelsPerUnit) / PixelsPerUnit;
        float snappedUp = Mathf.Round(up * PixelsPerUnit) / PixelsPerUnit;
        _camera.Position += basis.X * (snappedRight - right) + basis.Y * (snappedUp - up);
    }

    /// <summary>Direction d'écran (0 = E, 2 = S, 6 = N) d'une vitesse au sol, vue par la caméra actuelle.</summary>
    private int DirectionOf(Vector2 ground)
    {
        Basis basis = _camera.GlobalBasis;
        Vector3 v = new(ground.X, 0f, ground.Y);
        // L'axe Z de la caméra pointe vers elle : sa part au sol est le « bas » de l'écran.
        Vector3 down = basis.Z with { Y = 0f };
        float sx = v.Dot(basis.X);
        float sy = v.Dot(down.Normalized());
        int index = Mathf.RoundToInt(Mathf.Atan2(sy, sx) / (Mathf.Pi / 4f));
        return ((index % 8) + 8) % 8;
    }

    /// <summary>
    /// Petit pas vers la caméra, le long du rayon de vue : en orthographique, l'image ne bouge pas d'un pixel, mais le sprite
    /// ne s'enfonce plus dans le mur contre lequel il se tient.
    /// </summary>
    private Vector3 Nudge() => (_camera.GlobalPosition - _cameraTarget).Normalized() * SpriteNudge;

    private void UpdateSprites()
    {
        Vector3 nudge = Nudge();
        Vector3 toCamera = nudge / SpriteNudge;
        foreach ((Sprite3D sprite, Vector3 foot, int layer) in _layered)
            sprite.Position = foot + toCamera * (LayerNudge * layer);
        int frame = (int)(_animClock * AnimFps) % 4;

        bool moving = _sim.PlayerVelocity.LengthSquared() > 0.05f;
        int dir = DirectionOf(_sim.PlayerFacing);
        Texture2D[,][] anim = _sim.PlayerAirborneFromBond ? _playerBond : _playerWalk;
        int playerFrame = _sim.PlayerAirborneFromBond ? Mathf.Min(3, (int)((1f - Mathf.Clamp(_sim.PlayerPos.Z - PlayerGround(), 0f, 1f)) * 3f)) : moving ? frame : 0;
        _player.Texture = anim[dir, 0][playerFrame];
        _player.Offset = new Vector2(0f, _playerFoot);
        _player.Position = ToWorld(_sim.PlayerPos) + nudge;
        bool blink = _sim.PlayerInvulnerable > 0f && !_sim.PlayerAirborneFromBond && (int)(_animClock * 16f) % 2 == 0;
        _player.Modulate = _sim.PlayerHurtFlash > 0f ? new Color(1.6f, 0.5f, 0.45f) : blink ? new Color(1f, 1f, 1f, 1f) * 1.4f : _tint;
        PlaceShadow(_playerShadow, _sim.PlayerPos, 1.1f, _config.PlayerRadius);

        for (int i = 0; i < _sim.Capacity; i++)
        {
            Sprite3D sprite = _enemies[i];
            Sprite3D shadow = _enemyShadows[i];
            ReliefSim.EnemyState state = _sim.State[i];
            if (state == ReliefSim.EnemyState.Inactive || _sim.EnemyPos[i].Z < -2.5f)
            {
                sprite.Visible = false;
                shadow.Visible = false;
                continue;
            }
            int type = _sim.EnemyType[i];
            int d = DirectionOf(_sim.EnemyFacing[i]);
            Texture2D texture;
            if (state == ReliefSim.EnemyState.Dying)
            {
                float t = 1f - _sim.EnemyTimer[i] / ReliefSim.DyingDuration;
                texture = _creatureDeath[type][d, 0][Mathf.Clamp((int)(t * 4f), 0, 3)];
            }
            else
            {
                texture = _creatureWalk[type][d, 0][(frame + i) % 4];
            }
            sprite.Visible = true;
            sprite.Texture = texture;
            sprite.Offset = new Vector2(0f, _creatureFoot[type]);
            sprite.Position = ToWorld(_sim.EnemyPos[i]) + nudge;
            sprite.Modulate = _sim.EnemyFlash[i] > 0f ? new Color(2.2f, 2.2f, 2.2f) : _sim.EnemyClimbing[i] ? new Color(0.85f, 0.8f, 0.95f) * _tint : _tint;
            PlaceShadow(shadow, _sim.EnemyPos[i], 0.9f);
        }
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
        float turn = 1f - Mathf.Exp(-TurnRate * dt);
        bool moving = _sim.PlayerVelocity.LengthSquared() > 0.05f;
        Mesh pose = _sim.PlayerAirborneFromBond
            ? _playerModel.FrameAt("dash", 1f - Mathf.Clamp(_sim.PlayerPos.Z - PlayerGround(), 0f, 1f))
            : moving ? _playerModel.Frame("walk", _animClock * PlayerWalkRate) : _playerModel.Frame("idle", _animClock);
        SetPose(_playerBody, pose);
        _playerYaw = Mathf.LerpAngle(_playerYaw, Mathf.Atan2(_sim.PlayerFacing.X, _sim.PlayerFacing.Y), turn);
        _playerBody.Position = ToWorld(_sim.PlayerPos);
        _playerBody.Rotation = new Vector3(0f, _playerYaw, 0f);
        bool blink = _sim.PlayerInvulnerable > 0f && !_sim.PlayerAirborneFromBond && (int)(_animClock * 16f) % 2 == 0;
        _playerBody.MaterialOverride = _sim.PlayerHurtFlash > 0f ? _hurtMaterial : blink ? _flashMaterial : null;
        PlaceShadow(_playerShadow, _sim.PlayerPos, 1.1f, _config.PlayerRadius);

        for (int i = 0; i < _sim.Capacity; i++)
        {
            MeshInstance3D body = _creatureBodies[i];
            Sprite3D shadow = _enemyShadows[i];
            ReliefSim.EnemyState state = _sim.State[i];
            if (state == ReliefSim.EnemyState.Inactive || _sim.EnemyPos[i].Z < -2.5f)
            {
                body.Visible = false;
                shadow.Visible = false;
                continue;
            }
            ReliefLowPoly.Model model = _creatureModels[_sim.EnemyType[i]];
            SetPose(body, state == ReliefSim.EnemyState.Dying
                ? model.FrameAt("death", 1f - _sim.EnemyTimer[i] / ReliefSim.DyingDuration)
                : model.Frame("walk", _animClock + i * 0.13f));
            Vector2 facing = _sim.EnemyFacing[i];
            if (state != ReliefSim.EnemyState.Dying && facing.LengthSquared() > 0.0001f)
                _creatureYaw[i] = Mathf.LerpAngle(_creatureYaw[i], Mathf.Atan2(facing.X, facing.Y), turn);
            body.Visible = true;
            body.Position = ToWorld(_sim.EnemyPos[i]);
            body.Rotation = new Vector3(0f, _creatureYaw[i], 0f);
            body.MaterialOverride = _sim.EnemyFlash[i] > 0f ? _flashMaterial : null;
            PlaceShadow(shadow, _sim.EnemyPos[i], 0.9f);
        }
    }

    private static void SetPose(MeshInstance3D body, Mesh pose)
    {
        if (body.Mesh != pose)
            body.Mesh = pose;
    }

    /// <summary>Ombre posée sur le sol réel sous l'entité : en l'air, elle reste en bas, et dit la hauteur du saut.</summary>
    private void PlaceShadow(Sprite3D shadow, Vector3 at, float size, float footRadius = 0f)
    {
        // Le joueur tient sur le plus haut sol sous son corps (ReliefSim) : son ombre aussi, au bord d'un rebord.
        float ground = footRadius > 0f ? _arena.HighestUnder(at.X, at.Y, footRadius) : _arena.GroundAt(at.X, at.Y);
        if (ReliefArena.IsVoid(ground))
        {
            shadow.Visible = false;
            return;
        }
        float height = Mathf.Max(0f, at.Z - ground);
        shadow.Visible = true;
        shadow.Position = new Vector3(at.X, ground + 0.012f, at.Y);
        float scale = size * Mathf.Clamp(1f - height * 0.35f, 0.45f, 1f);
        shadow.Scale = new Vector3(scale, scale, scale);
    }

    private void UpdateFeedback(float dt)
    {
        if (_sim.StompsThisTick > 0)
        {
            _stompRingTime = 0f;
            _stompRing.Position = ToWorld(_sim.LastStompAt) + new Vector3(0f, 0.02f, 0f);
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
        for (int i = 0; i < _sim.Capacity; i++)
        {
            if (_sim.State[i] != ReliefSim.EnemyState.Alive)
                continue;
            alive++;
            if (_sim.EnemyClimbing[i])
                climbing++;
        }
        string view = _view switch
        {
            View.Iso3D => "3D DE HAUT  (Tab : poursuite · Q/E tourner · molette zoom)",
            View.Chase3D => "3D EN POURSUITE  (Tab : relief 2D · souris pour tourner · Échap libère la souris)",
            _ => "RELIEF 2D  (Tab : 3D de haut)",
        };
        _hud.Text = $"Vue : {view} · sol : {_biome}\n"
            + $"Espace : bond · R : retour au départ · +/- : foule ({_crowdTarget})\n"
            + $"PV {Mathf.CeilToInt(_sim.PlayerHp)} · étage {_sim.PlayerLevel:0.0} · morts {_sim.Deaths}\n"
            + $"Éliminations {_sim.Kills} (retombée {_sim.KillsByStomp}, chute {_sim.KillsByFall}, vide {_sim.KillsByVoid})\n"
            + $"Bonds {_sim.Bonds} · retombées {_sim.Stomps} · chutes dans le vide {_sim.VoidFalls}\n"
            + $"Horde {alive}, dont {climbing} qui grimpent · {Engine.GetFramesPerSecond():0} i/s";
    }

    /// <summary>Le bot des captures fait le tour de l'arène et bondit quand il bute ou qu'une marche est trop haute.</summary>
    private Vector2 BotWish(float dt)
    {
        Vector2 at = new(_sim.PlayerPos.X, _sim.PlayerPos.Y);
        Vector2 goal = BotTour[_waypoint];
        if (at.DistanceTo(goal) < 0.7f)
        {
            _waypoint = (_waypoint + 1) % BotTour.Length;
            goal = BotTour[_waypoint];
        }
        _botStuckTimer += dt;
        if (_botStuckTimer > 0.35f)
        {
            if (at.DistanceTo(_botLastPos) < 0.25f && _sim.PlayerGrounded)
                _sim.QueueBond();
            _botLastPos = at;
            _botStuckTimer = 0f;
        }
        Vector2 dir = (goal - at).Normalized();
        if (_view == View.Chase3D)
        {
            // Caméra derrière le bot : le « bas » de l'écran est l'opposé de sa marche.
            float behind = Mathf.RadToDeg(Mathf.Atan2(-dir.X, -dir.Y));
            _yawDegrees = Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(_yawDegrees), Mathf.DegToRad(behind), 1f - Mathf.Exp(-4f * dt)));
        }
        Vector2 ahead = at + dir * 0.9f;
        if (_sim.PlayerGrounded && _arena.GroundAt(ahead.X, ahead.Y) > _sim.PlayerPos.Z + _config.Step)
            _sim.QueueBond();
        return dir;
    }

    private void StepCapture(float dt)
    {
        _captureTimer += dt;
        float airHeight = _sim.PlayerPos.Z - PlayerGround();
        if (!_airCaptured && _captureTimer > 3f && _sim.PlayerAirborneFromBond && airHeight > 0.45f)
        {
            _airCaptured = true;
            Save("2d_air");
        }
        switch (_captureStep)
        {
            case 0 when _captureTimer >= _captureSeconds:
                Save("2d");
                SetView(View.Iso3D);
                Advance();
                break;
            case 1 when _captureTimer >= 0.6f:
                Save("3d");
                _yawDegrees += 90f;
                Advance();
                break;
            case 2 when _captureTimer >= 0.8f:
                Save("3d_rot");
                _yawDegrees = 45f;
                SetView(View.Chase3D);
                Advance();
                break;
            case 3 when _captureTimer >= 2f:
                Save("chase");
                SetView(View.Relief2D);
                _sim.Teleport(new Vector2(39f, 11f));
                Advance();
                break;
            case 4 when _captureTimer >= 2.5f:
                Save("2d_edge");
                SetView(View.Iso3D);
                Advance();
                break;
            case 5 when _captureTimer >= 0.6f:
                Save("3d_edge");
                SetView(View.Relief2D);
                _sim.Teleport(new Vector2(5.5f, 4.5f));
                _waypoint = System.Array.IndexOf(BotTour, new Vector2(20.5f, 13.5f));
                Advance();
                break;
            case 6 when _captureTimer >= 0.5f:
                Save("2d_high");
                SetView(View.Chase3D);
                Advance();
                break;
            case 7 when _captureTimer >= 0.5f:
                Save("chase_high");
                GD.Print($"[ReliefSandbox] RESULT seconds={_sim.Elapsed:0.0} kills={_sim.Kills} stomp={_sim.KillsByStomp} fall={_sim.KillsByFall} void={_sim.KillsByVoid} bonds={_sim.Bonds} stomps={_sim.Stomps} deaths={_sim.Deaths} void_falls={_sim.VoidFalls}");
                GetTree().Quit();
                break;
        }
    }

    private float PlayerGround() => _arena.HighestUnder(_sim.PlayerPos.X, _sim.PlayerPos.Y, _config.PlayerRadius);

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
        GD.Print($"[ReliefSandbox] Capture : {path} (étage {_sim.PlayerLevel:0.0}, éliminations {_sim.Kills})");
    }

    private static ImageTexture ShadowTexture()
    {
        const int size = 16;
        Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).Length() / (size / 2f);
                image.SetPixel(x, y, d < 1f ? new Color(0.02f, 0.01f, 0.05f, d < 0.7f ? 1f : 0.6f) : Colors.Transparent);
            }
        }
        return ImageTexture.CreateFromImage(image);
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
