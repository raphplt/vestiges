using System;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Profil de test réservé à la compilation locale pour l'éditeur ; les acquis normaux restent séparés.</summary>
public static class DevelopmentMode
{
#if TOOLS
    // Godot.NET.Sdk définit TOOLS pour Debug, mais jamais pour ExportDebug/ExportRelease.
    private const string SettingsPath = "user://development.cfg";
    public static bool IsAvailable => OS.IsDebugBuild();
    private static bool _enabled = LoadPreference();
    public static bool IsEnabled => IsAvailable && _enabled;

    private static bool LoadPreference()
    {
        if (!IsAvailable)
            return false;
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--dev") >= 0)
            return true;
        using ConfigFile settings = new();
        return settings.Load(SettingsPath) == Error.Ok && settings.GetValue("profile", "all_unlocked", false).AsBool();
    }
#else
    // Aucun argument ni fichier local ne peut activer le mode dans l'export de production.
    public static bool IsAvailable => false;
    public static bool IsEnabled => false;
#endif

    /// <summary>Appelé depuis le Hub, avant de le recharger. Aucun changement de profil en pleine run.</summary>
    internal static bool SetEnabled(bool enabled)
    {
#if TOOLS
        if (!IsAvailable)
            return false;
        using ConfigFile settings = new();
        settings.SetValue("profile", "all_unlocked", enabled);
        Error result = settings.Save(SettingsPath);
        if (result != Error.Ok)
        {
            GD.PushError($"Impossible de mémoriser le mode dev : {result}.");
            return false;
        }
        if (_enabled == enabled)
            return true;

        Analytics.AnalyticsManager.Instance?.FlushProfile();
        _enabled = enabled;
        MetaSaveManager.ReloadProfile();
        RunHistoryManager.ForceReload();
        Analytics.AnalyticsManager.Instance?.ReloadProfile();
        if (enabled)
            Steam.SteamManager.Instance?.DisableForDevelopmentSession();
        return true;
#else
        return false;
#endif
    }

    public static string GetSavePath(string relativePath)
    {
        string path = (IsEnabled ? "user://dev/" : "user://") + relativePath;
        Error result = DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());
        if (result != Error.Ok)
            throw new InvalidOperationException($"Impossible de préparer le profil : {path.GetBaseDir()} ({result}).");
        return path;
    }
}
