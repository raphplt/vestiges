using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.UI;

namespace Vestiges.World3D;

/// <summary>
/// La run en 3D (plan 60, 60.1c et 60.2a), lancée par le Hub : la terre d'essai (<c>data/world3d/test_land.json</c>) et
/// les décors de sa terre, le personnage choisi au Hub sous la caméra libre, et la horde de cette terre qui grossit avec
/// le temps. On y marche, saute et use de sa capacité ; la horde touche au contact, meurt sous la retombée et dans le
/// vide ; les chutes blessent, et la mort ramène au Hub. Le combat arrive en 60.3.
/// L'image 3D se rend dans un <see cref="SubViewport"/> à la définition de la fenêtre, comme le bac à sable dont viennent
/// ses réglages de contours ; l'interface se dessine par-dessus.
/// </summary>
public partial class Run3D : Node
{
    private const string FallbackCharacter = "facteur";
    private const string LibraryRoot = "res://build/models";
    private const float StickDegreesPerSecond = 150f;
    // Pas de simulation le plus long : une image lente se découpe, pour ne pas traverser un mur ou une emprise d'un coup.
    private const float MaxStep = 1f / 60f;
    private static readonly Color VoidColor = new("0b0812");
    private static readonly StringName MoveLeft = "move_left", MoveRight = "move_right", MoveUp = "move_up", MoveDown = "move_down";
    private static readonly StringName CameraLeft = "camera_left", CameraRight = "camera_right", CameraUp = "camera_up", CameraDown = "camera_down";
    private static readonly StringName Jump = "jump", Mobility = "mobility";

    /// <summary>Taille de horde imposée (captures, banc dense) ; négative : celle de crowd.json, qui grossit avec le temps.</summary>
    public int CrowdTarget = -1;
    /// <summary>La mort rend ses PV au joueur au lieu de ramener au Hub (captures, banc dense).</summary>
    public bool Undying;
    /// <summary>Temps de run au départ, en secondes (captures : voir les sortes qui viennent plus tard).</summary>
    public float StartSeconds;

    private ReliefConfig _config;
    private ReliefArena _arena;
    private PlayerMotor3D _motor;
    private PlayerBody3D _body;
    private Crowd3D _crowd;
    private CrowdBodies3D _crowdBodies;
    private float _elapsed;
    private RunCamera3D _camera;
    private ReliefLook _look;
    private ReliefAtmosphere _atmosphere;
    private SubViewport _viewport;
    private Label _status;
    private bool _wasPaused = true;
    private bool _leaving;
    private float _statusTimer;
    // Le délégué de conversion, créé une fois : une méthode passée telle quelle en alloue un à chaque image.
    private readonly System.Func<Vector3, Vector3> _toWorld = ToWorld;

    public override void _Ready()
    {
        // Toujours actif, pour rendre la souris à la pause ; la simulation, elle, s'arrête avec l'arbre.
        ProcessMode = ProcessModeEnum.Always;
        GameManager gameManager = GetNode<GameManager>("/root/GameManager");
        _config = ReliefConfig.Load();
        string character = string.IsNullOrEmpty(gameManager.SelectedCharacterId) ? FallbackCharacter : gameManager.SelectedCharacterId;
        _config.SetCharacter(character);
        if (_config.LowPoly.WorldScale != 1f)
            _config.ScaleWorld(_config.LowPoly.WorldScale);
        _config.ScaleBodies(_config.LowPoly.BodyScale);
        _arena = new ReliefArena(_config.Arena, _config.LevelHeight);
        _motor = new PlayerMotor3D(_config, _arena);
        // Les fiches se convertissent avec le rythme déjà ramené à l'échelle du monde.
        _config.Creatures = CrowdSpecies.FromLand(_config.Land, _config.CellsPerPixel, LibraryRoot);
        _crowd = new Crowd3D(_config, _arena, _config.Creatures, CrowdSpecies.TrackingOf(_config.CellsPerPixel),
            Mathf.Max(_config.RunCrowdMax, CrowdTarget), (ulong)GD.Randi());
        _elapsed = StartSeconds;
        BuildWorld(character);
        BuildStatus();
        GD.Print($"[Run3D] Terre d'essai {_arena.Width}×{_arena.Depth}, terre {_config.Land}, {_look.PropCount} décors, {character}, "
            + $"{_config.Creatures.Length} sortes de créatures");
    }

    public override void _ExitTree()
    {
        if (_viewport != null)
            GetWindow().SizeChanged -= FitRender;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void BuildWorld(string character)
    {
        ReliefTexturedGround.UseLand(_config.Land);
        ReliefBiomeProps props = null;
        if (FileAccess.FileExists($"{LibraryRoot}/index.json"))
            props = new ReliefBiomeProps(_config.Land, ProjectSettings.GlobalizePath(LibraryRoot));
        else
            GD.PushWarning("[Run3D] Bibliothèque des décors absente : python3 tools/export_lowpoly.py --all --jobs 2 ; semis du réglage à la place");
        string model = character;
        if (!ReliefLowPoly.Exists($"characters/{character}"))
        {
            GD.PushWarning($"[Run3D] Modèle 3D de {character} absent : python3 tools/export_lowpoly.py --character {character} ; le facteur à la place");
            model = FallbackCharacter;
        }

        _viewport = new SubViewport
        {
            Name = "World",
            Size = WindowPixels(),
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            Msaa3D = Viewport.Msaa.Msaa4X,
            MeshLodThreshold = _config.LowPoly.LodThreshold,
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
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        picture.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        screen.AddChild(picture);

        Node3D world = new() { Name = "Land" };
        _viewport.AddChild(world);
        world.AddChild(new DirectionalLight3D
        {
            Name = "Sun",
            RotationDegrees = new Vector3(-52f, -35f, 0f),
            LightEnergy = 1.15f,
            LightColor = new Color("fff1dc"),
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 70f,
        });
        WorldEnvironment environment = new() { Name = "Environment", Environment = BuildEnvironment() };
        world.AddChild(environment);
        // Proche plan reculé : la profondeur garde sa précision au loin, où les contours la lisent.
        Camera3D camera = new() { Name = "Camera", Current = true, Near = 0.4f, Far = 200f };
        world.AddChild(camera);

        _look = new ReliefLook(_config, _arena, world, camera, _viewport, new List<Vector2>(), props, model);
        // Les tirs des créatures n'éclairent pas : une lumière par tir surexpose la scène dès qu'ils sont nombreux.
        _atmosphere = new ReliefAtmosphere(_config.Atmosphere, world, 0, _look.Glows);
        _look.Apply();
        _atmosphere.Apply(environment.Environment, true);
        GetWindow().SizeChanged += FitRender;

        StandardMaterial3D flash = new() { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1f, 0.97f, 0.9f) };
        StandardMaterial3D hurt = new() { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.95f, 0.25f, 0.2f) };
        _body = new PlayerBody3D(world, _config, _arena, flash, hurt);
        _crowdBodies = new CrowdBodies3D(world, _config, _arena, _crowd.Capacity, flash);
        _camera = new RunCamera3D(camera, _config, _arena);
        _camera.Snap(_motor.WorldPosition);
    }

    private static Godot.Environment BuildEnvironment() => new()
    {
        BackgroundMode = Godot.Environment.BGMode.Color,
        BackgroundColor = VoidColor,
        AmbientLightSource = Godot.Environment.AmbientSource.Color,
        AmbientLightColor = new Color("8c86a8"),
        AmbientLightEnergy = 0.55f,
        FogEnabled = true,
        FogLightColor = VoidColor.Lerp(new Color("3a2f55"), 0.5f),
        FogDensity = 0.012f,
    };

    /// <summary>Les PV et la capacité, en attendant le HUD de la run 3D (60.3).</summary>
    private void BuildStatus()
    {
        CanvasLayer layer = new() { Name = "Status", Layer = 5 };
        AddChild(layer);
        _status = new Label { Name = "Vitals", Position = new Vector2(24f, 20f), MouseFilter = Control.MouseFilterEnum.Ignore };
        UITheme.ApplyTypography(_status, TextRole.Body);
        _status.AddThemeColorOverride("font_color", UITheme.GoldBright);
        _status.AddThemeColorOverride("font_outline_color", Colors.Black);
        _status.AddThemeConstantOverride("outline_size", 6);
        layer.AddChild(_status);
    }

    public override void _Process(double delta)
    {
        // La pause (menu, Échap) rend la souris ; la reprise la recapture pour la caméra.
        bool paused = GetTree().Paused;
        if (paused != _wasPaused)
        {
            Input.MouseMode = paused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
            _wasPaused = paused;
        }
        if (paused || _leaving)
            return;
        float dt = Mathf.Min((float)delta, 1f / 20f);
        _camera.TurnBy(Input.GetVector(CameraLeft, CameraRight, CameraUp, CameraDown), StickDegreesPerSecond, dt);
        if (Input.IsActionJustPressed(Jump))
            _motor.QueueJump();
        if (Input.IsActionJustPressed(Mobility))
            _motor.QueueSkill();
        Vector2 wish = _camera.ScreenToGround(Input.GetVector(MoveLeft, MoveRight, MoveUp, MoveDown));
        _elapsed += dt;
        float minutes = _elapsed / 60f;
        int target = Mathf.Min(_crowd.Capacity, CrowdTarget >= 0 ? CrowdTarget
            : _config.RunCrowdStart + Mathf.FloorToInt(_config.RunCrowdPerMinute * minutes));
        _crowd.Fill(_motor, target, minutes);
        int steps = Mathf.CeilToInt(dt / MaxStep);
        for (int i = 0; i < steps; i++)
        {
            _motor.Step(dt / steps, wish);
            if (_motor.StompedThisStep)
                _crowd.Stomp(_motor.LastStompAt, _motor.Facing);
            // Revenu du vide au dernier sol sûr, le joueur y trouve un peu de place.
            if (_motor.VoidFallThisStep)
                ClearAroundPlayer();
            _crowd.Step(dt / steps, _motor);
        }
        _camera.Follow(dt, _motor.WorldPosition);
        ReliefLowPoly.SetSeeThrough(_camera.Camera.GlobalPosition, _motor.WorldPosition + Vector3.Up * 0.5f, _config.LowPoly.SeeThroughRadius);
        _body.Update(dt, _motor, _look.PlayerModel);
        _crowdBodies.Update(dt, _crowd, _look.CreatureModels);
        _atmosphere.Update(_crowd.ShotPos, _crowd.ShotActive, _toWorld);
        UpdateStatus(dt);
        if (_motor.Dead && Undying)
        {
            _motor.Revive();
            ClearAroundPlayer();
        }
        else if (_motor.Dead)
            Die();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured && !GetTree().Paused)
            _camera.Turn(motion.Relative);
    }

    private void UpdateStatus(float dt)
    {
        _statusTimer -= dt;
        if (_statusTimer > 0f)
            return;
        _statusTimer = 0.1f;
        string skill = _motor.SkillCooldown > 0f ? $"{_motor.SkillCooldown:0.0} s" : Tr("RUN3D_READY");
        _status.Text = $"{Tr("RUN3D_HP")} {Mathf.CeilToInt(_motor.Hp)} / {Mathf.CeilToInt(_config.PlayerHp)}\n{_config.Mobility.Name} : {skill}\n"
            + $"{Tr("RUN3D_KILLS")} {_crowd.Kills}";
    }

    /// <summary>La mort ramène au Hub, en attendant la fin de run de la 3D (60.3).</summary>
    private void Die()
    {
        _leaving = true;
        GD.Print($"[Run3D] Mort après {_elapsed:0} s : {_crowd.Kills} éliminations (retombée {_crowd.KillsByStomp}, chute {_crowd.KillsByFall}, "
            + $"vide {_crowd.KillsByVoid}), {_motor.VoidFalls} chutes dans le vide, {_motor.Jumps} sauts");
        GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Hub);
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }

    private void ClearAroundPlayer() =>
        _crowd.Repel(new Vector2(_motor.Position.X, _motor.Position.Y), _config.RespawnClearRadius, _config.RespawnClearPush);

    /// <summary>La horde, pour les captures et le banc.</summary>
    public Crowd3D Crowd => _crowd;

    private static Vector3 ToWorld(Vector3 relief) => new(relief.X, relief.Z, relief.Y);

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
}
