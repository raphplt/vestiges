using Godot;

namespace Vestiges.UI;

/// <summary>
/// Affichage des quêtes de run (plan 24 A2), choisi dans Paramètres › Graphismes et persisté dans
/// user://display_settings.cfg : sceaux repliés (défaut), panneau détaillé, ou rien. Maintenir la touche « show_quests »
/// montre le détail dans tous les cas.
/// </summary>
public static class QuestDisplaySettings
{
    public enum Mode { Folded, Detailed, Hidden }

    private const string SettingsPath = "user://display_settings.cfg";
    private const string Section = "interface";

    private static bool _loaded;
    private static Mode _mode = Mode.Folded;

    public static Mode Current
    {
        get { EnsureLoaded(); return _mode; }
        set { EnsureLoaded(); _mode = value; }
    }

    public static Mode Next(Mode mode) => mode switch
    {
        Mode.Folded => Mode.Detailed,
        Mode.Detailed => Mode.Hidden,
        _ => Mode.Folded,
    };

    public static string Label(Mode mode) => TranslationServer.Translate(mode switch
    {
        Mode.Detailed => "SETTINGS_QUESTS_DETAILED",
        Mode.Hidden => "SETTINGS_QUESTS_HIDDEN",
        _ => "SETTINGS_QUESTS_FOLDED",
    });

    public static void Save()
    {
        EnsureLoaded();
        ConfigFile cfg = new();
        cfg.Load(SettingsPath);
        cfg.SetValue(Section, "quest_display", (int)_mode);
        cfg.Save(SettingsPath);
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        ConfigFile cfg = new();
        if (cfg.Load(SettingsPath) == Error.Ok)
            _mode = (Mode)Mathf.Clamp(cfg.GetValue(Section, "quest_display", 0).AsInt32(), 0, 2);
    }
}
