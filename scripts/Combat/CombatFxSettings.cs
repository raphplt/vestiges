using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Réglages d'affichage des effets de combat (Paramètres › Graphismes), persistés dans
/// user://display_settings.cfg. Chargés au premier accès, donc valables dès le Hub.
/// </summary>
public static class CombatFxSettings
{
    private const string SettingsPath = "user://display_settings.cfg";
    private const string Section = "display";

    /// <summary>Les attaques ennemies sont des informations de danger : jamais masquées ni presque invisibles.</summary>
    public const float MinEnemyOpacity = 0.4f;
    public const float MinPlayerOpacity = 0.1f;

    private static bool _loaded;
    private static ParticleLevel _particleLevel = ParticleLevel.Full;
    private static bool _playerAttackFx = true;
    private static bool _playerProjectiles = true;
    private static float _playerOpacity = 1f;
    private static float _enemyOpacity = 1f;
    private static float _screenShake = 1f;

    public static ParticleLevel ParticleLevel
    {
        get { EnsureLoaded(); return _particleLevel; }
        set { EnsureLoaded(); _particleLevel = value; }
    }

    /// <summary>Animations des attaques du joueur (arcs, ondes, étincelles de coup).</summary>
    public static bool PlayerAttackFx
    {
        get { EnsureLoaded(); return _playerAttackFx; }
        set { EnsureLoaded(); _playerAttackFx = value; }
    }

    /// <summary>Visuel des projectiles du joueur. Masqués, ils touchent toujours.</summary>
    public static bool PlayerProjectiles
    {
        get { EnsureLoaded(); return _playerProjectiles; }
        set { EnsureLoaded(); _playerProjectiles = value; }
    }

    public static float PlayerOpacity
    {
        get { EnsureLoaded(); return _playerOpacity; }
        set { EnsureLoaded(); _playerOpacity = Mathf.Clamp(value, MinPlayerOpacity, 1f); }
    }

    public static float EnemyOpacity
    {
        get { EnsureLoaded(); return _enemyOpacity; }
        set { EnsureLoaded(); _enemyOpacity = Mathf.Clamp(value, MinEnemyOpacity, 1f); }
    }

    /// <summary>Multiplicateur des secousses d'écran (0 = aucune).</summary>
    public static float ScreenShake
    {
        get { EnsureLoaded(); return _screenShake; }
        set { EnsureLoaded(); _screenShake = Mathf.Clamp(value, 0f, 1f); }
    }

    public static void Load()
    {
        _loaded = true;
        ConfigFile cfg = new();
        if (cfg.Load(SettingsPath) != Error.Ok)
            return;
        _particleLevel = (ParticleLevel)Mathf.Clamp(cfg.GetValue(Section, "particle_level", 0).AsInt32(), 0, 2);
        _playerAttackFx = cfg.GetValue(Section, "player_attack_fx", true).AsBool();
        _playerProjectiles = cfg.GetValue(Section, "player_projectiles", true).AsBool();
        _playerOpacity = Mathf.Clamp(cfg.GetValue(Section, "player_fx_opacity", 1f).AsSingle(), MinPlayerOpacity, 1f);
        _enemyOpacity = Mathf.Clamp(cfg.GetValue(Section, "enemy_fx_opacity", 1f).AsSingle(), MinEnemyOpacity, 1f);
        _screenShake = Mathf.Clamp(cfg.GetValue(Section, "screen_shake", 1f).AsSingle(), 0f, 1f);
    }

    public static void Save()
    {
        EnsureLoaded();
        ConfigFile cfg = new();
        cfg.Load(SettingsPath);
        cfg.SetValue(Section, "particle_level", (int)_particleLevel);
        cfg.SetValue(Section, "player_attack_fx", _playerAttackFx);
        cfg.SetValue(Section, "player_projectiles", _playerProjectiles);
        cfg.SetValue(Section, "player_fx_opacity", _playerOpacity);
        cfg.SetValue(Section, "enemy_fx_opacity", _enemyOpacity);
        cfg.SetValue(Section, "screen_shake", _screenShake);
        cfg.Save(SettingsPath);
    }

    private static void EnsureLoaded()
    {
        if (!_loaded)
            Load();
    }
}
