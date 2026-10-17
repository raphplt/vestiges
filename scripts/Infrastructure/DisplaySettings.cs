using System;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Plafond de résolution interne du rendu (plan 53 V1) : sur un écran Retina / 4K, calculer les shaders 2D au-delà de
/// 1080p multiplie par 3 à 4 le nombre de fragments sans gain sur du pixel art en base 960×540 monde.
/// </summary>
public enum RenderScaleMode
{
    Max1080p = 0,
    Native = 1,
    Max720p = 2,
}

/// <summary>
/// Réglages vidéo (Paramètres › Graphismes, plan 53 V1) : mode plein écran, résolution fenêtrée, échelle de rendu
/// interne (Retina / HiDPI), synchronisation verticale (V-Sync) et plafond d'images par seconde (FPS max).
/// Persistés dans <c>user://display_settings.cfg</c> (section <c>video</c>) et appliqués dès le démarrage.
/// </summary>
public static class DisplaySettings
{
    private const string SettingsPath = "user://display_settings.cfg";
    private const string Section = "video";

    public static readonly Vector2I[] Resolutions =
    {
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
    };

    public static readonly int[] FpsCaps = { 0, 60, 120, 144, 240 };

    private static bool _loaded;
    private static bool _fullscreen = true;
    private static int _resolutionIndex = 2; // 1920×1080 par défaut
    private static RenderScaleMode _renderScale = RenderScaleMode.Max1080p;
    private static bool _vsync = true;
    private static int _maxFpsIndex; // 0 = Illimité
    private static SceneTree _tree;

    public static bool Fullscreen
    {
        get { EnsureLoaded(); return _fullscreen; }
        set { EnsureLoaded(); _fullscreen = value; ApplyWindowMode(); }
    }

    public static int ResolutionIndex
    {
        get { EnsureLoaded(); return _resolutionIndex; }
        set
        {
            EnsureLoaded();
            _resolutionIndex = Mathf.Clamp(value, 0, Resolutions.Length - 1);
            ApplyResolution();
        }
    }

    public static Vector2I CurrentResolution => Resolutions[ResolutionIndex];

    public static RenderScaleMode RenderScale
    {
        get { EnsureLoaded(); return _renderScale; }
        set
        {
            EnsureLoaded();
            _renderScale = value;
            ApplyRenderScale();
        }
    }

    public static bool VSync
    {
        get { EnsureLoaded(); return _vsync; }
        set
        {
            EnsureLoaded();
            _vsync = value;
            ApplyVSync();
        }
    }

    public static int MaxFpsIndex
    {
        get { EnsureLoaded(); return _maxFpsIndex; }
        set
        {
            EnsureLoaded();
            _maxFpsIndex = Mathf.Clamp(value, 0, FpsCaps.Length - 1);
            ApplyMaxFps();
        }
    }

    public static int CurrentMaxFps => FpsCaps[MaxFpsIndex];

    /// <summary>
    /// Installe et applique les réglages vidéo au lancement du jeu, sauf dans les bancs automatisés ou en headless
    /// qui pilotent eux-mêmes leur fenêtre et leur cadence.
    /// </summary>
    public static void Install(SceneTree tree)
    {
        EnsureLoaded();
        if (DisplayServer.GetName() == "headless" || IsAutomatedRun())
            return;
        if (_tree == null && tree != null)
        {
            _tree = tree;
            _tree.Root.SizeChanged += ApplyRenderScale;
        }
        ApplyAll();
    }

    public static void Release()
    {
        if (_tree != null)
        {
            _tree.Root.SizeChanged -= ApplyRenderScale;
            _tree = null;
        }
    }

    public static void ApplyAll()
    {
        EnsureLoaded();
        if (DisplayServer.GetName() == "headless" || IsAutomatedRun())
            return;
        ApplyWindowMode();
        if (!_fullscreen)
            ApplyResolution();
        ApplyVSync();
        ApplyMaxFps();
        ApplyRenderScale();
    }

    public static void CycleResolution()
    {
        ResolutionIndex = (ResolutionIndex + 1) % Resolutions.Length;
    }

    public static void CycleRenderScale()
    {
        RenderScale = RenderScale == RenderScaleMode.Max1080p
            ? RenderScaleMode.Native
            : RenderScaleMode.Max1080p;
    }

    public static void CycleMaxFps()
    {
        MaxFpsIndex = (MaxFpsIndex + 1) % FpsCaps.Length;
    }

    public static string ResolutionLabel(Vector2I res) => $"{res.X} × {res.Y}";

    public static string RenderScaleLabel(RenderScaleMode mode) => mode switch
    {
        RenderScaleMode.Native => "Native (Retina)",
        _ => "1080p (Opti.)",
    };

    public static string MaxFpsLabel(int fps) => fps <= 0 ? "Illimité" : $"{fps} FPS";

    public static void Load()
    {
        _loaded = true;
        ConfigFile cfg = new();
        if (cfg.Load(SettingsPath) != Error.Ok)
            return;
        _fullscreen = cfg.GetValue(Section, "fullscreen", true).AsBool();
        _resolutionIndex = Mathf.Clamp(cfg.GetValue(Section, "resolution_index", 2).AsInt32(), 0, Resolutions.Length - 1);
        _renderScale = (RenderScaleMode)Mathf.Clamp(
            cfg.GetValue(Section, "render_scale", (int)RenderScaleMode.Max1080p).AsInt32(), 0, 1);
        _vsync = cfg.GetValue(Section, "vsync", true).AsBool();
        _maxFpsIndex = Mathf.Clamp(cfg.GetValue(Section, "max_fps_index", 0).AsInt32(), 0, FpsCaps.Length - 1);
    }

    public static void Save()
    {
        EnsureLoaded();
        ConfigFile cfg = new();
        cfg.Load(SettingsPath);
        cfg.SetValue(Section, "fullscreen", _fullscreen);
        cfg.SetValue(Section, "resolution_index", _resolutionIndex);
        cfg.SetValue(Section, "render_scale", (int)_renderScale);
        cfg.SetValue(Section, "vsync", _vsync);
        cfg.SetValue(Section, "max_fps_index", _maxFpsIndex);
        cfg.Save(SettingsPath);
    }

    private static void EnsureLoaded()
    {
        if (!_loaded)
            Load();
    }

    private static void ApplyWindowMode()
    {
        if (DisplayServer.GetName() == "headless" || IsAutomatedRun())
            return;
        DisplayServer.WindowMode current = DisplayServer.WindowGetMode();
        bool isFullscreen = current is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
        if (_fullscreen && !isFullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
        else if (!_fullscreen && isFullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            ApplyResolution();
        }
        ApplyRenderScale();
    }

    private static void ApplyResolution()
    {
        if (DisplayServer.GetName() == "headless" || IsAutomatedRun() || _fullscreen)
            return;
        Vector2I target = CurrentResolution;
        int screen = DisplayServer.WindowGetCurrentScreen();
        Rect2I usable = DisplayServer.ScreenGetUsableRect(screen);
        Vector2I clamped = new(
            Mathf.Min(target.X, Mathf.Max(640, usable.Size.X)),
            Mathf.Min(target.Y, Mathf.Max(360, usable.Size.Y)));
        DisplayServer.WindowSetSize(clamped);
        Vector2I pos = usable.Position + (usable.Size - clamped) / 2;
        DisplayServer.WindowSetPosition(pos);
        ApplyRenderScale();
    }

    private static void ApplyVSync()
    {
        if (DisplayServer.GetName() == "headless" || IsAutomatedRun())
            return;
        DisplayServer.WindowSetVsyncMode(_vsync
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);
    }

    private static void ApplyMaxFps()
    {
        if (DisplayServer.GetName() == "headless" || IsAutomatedRun())
            return;
        Engine.MaxFps = CurrentMaxFps;
    }

    private static void ApplyRenderScale()
    {
        if (DisplayServer.GetName() == "headless" || IsAutomatedRun())
            return;
        Window root = _tree?.Root ?? (Engine.GetMainLoop() as SceneTree)?.Root;
        if (root == null)
            return;

        // En 2D Godot, Scaling3DScale n'affecte que la 3D : pour plafonner réellement le framebuffer 2D à 1080p sur un
        // écran Retina / 4K (> 1080 px physiques de haut) tout en gardant le canevas UI 1920×1080 intact, on passe en
        // ContentScaleMode.Viewport (Expand) dès que la fenêtre dépasse 1080p.
        Vector2I winSize = DisplayServer.WindowGetSize();
        bool capTo1080p = _renderScale != RenderScaleMode.Native && winSize.Y > 1100;
        Window.ContentScaleModeEnum targetMode = capTo1080p
            ? Window.ContentScaleModeEnum.Viewport
            : Window.ContentScaleModeEnum.CanvasItems;
        if (root.ContentScaleMode != targetMode)
            root.ContentScaleMode = targetMode;
        if (root.ContentScaleAspect != Window.ContentScaleAspectEnum.Expand)
            root.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        if (root.ContentScaleSize != new Vector2I(1920, 1080))
            root.ContentScaleSize = new Vector2I(1920, 1080);
    }

    private static bool IsAutomatedRun()
    {
        foreach (string arg in OS.GetCmdlineArgs())
        {
            if (arg is "--disable-vsync" or "--fixed-fps" or "--headless" or "--resolution")
                return true;
        }
        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--capture", StringComparison.Ordinal)
                || arg is "--output" or "--measure-seconds" or "--frames")
                return true;
        }
        return false;
    }
}
