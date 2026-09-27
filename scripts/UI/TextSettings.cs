using Godot;

namespace Vestiges.UI;

/// <summary>
/// Taille du texte choisie par le joueur (Paramètres › Graphismes), persistée dans user://display_settings.cfg.
/// Elle agrandit les écrans d'interface en base 1080p ; le HUD, qui a sa propre échelle, et les textes posés
/// dans le monde n'en dépendent pas.
/// </summary>
public static class TextSettings
{
    private const string SettingsPath = "user://display_settings.cfg";
    private const string Section = "interface";

    /// <summary>Paliers proposés : normal, grand, très grand.</summary>
    public static readonly float[] Steps = { 1f, 1.15f, 1.3f };

    private static bool _loaded;
    private static int _step;

    public static float Scale
    {
        get { EnsureLoaded(); return Steps[_step]; }
    }

    public static int Step
    {
        get { EnsureLoaded(); return _step; }
        set { EnsureLoaded(); _step = Mathf.Clamp(value, 0, Steps.Length - 1); }
    }

    public static void Save()
    {
        EnsureLoaded();
        ConfigFile cfg = new();
        cfg.Load(SettingsPath);
        cfg.SetValue(Section, "text_scale_step", _step);
        cfg.Save(SettingsPath);
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        ConfigFile cfg = new();
        if (cfg.Load(SettingsPath) == Error.Ok)
            _step = Mathf.Clamp(cfg.GetValue(Section, "text_scale_step", 0).AsInt32(), 0, Steps.Length - 1);
    }
}
