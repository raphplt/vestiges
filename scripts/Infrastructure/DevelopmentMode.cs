using System;
using Godot;

namespace Vestiges.Infrastructure;

public enum RunProvenance { Normal, Development, Test }

/// <summary>Profil de test réservé à la compilation locale pour l'éditeur ; les acquis normaux restent séparés.</summary>
public static class DevelopmentMode
{
#if TOOLS
    // Godot.NET.Sdk définit TOOLS pour Debug, mais jamais pour ExportDebug/ExportRelease.
    private const string SettingsPath = "user://development.cfg";
    public static bool IsAvailable => OS.IsDebugBuild();
    public static bool IsTestSession { get; } = IsTestLaunch();
    private static bool _enabled = LoadPreference();
    private static bool _usedDevelopment = _enabled;
    public static bool IsEnabled => IsAvailable && _enabled;
    public static bool CanSubmitResults => !IsTestSession && !_usedDevelopment;

    // Résolu avant les autoloads : un banc ne contacte jamais Steam et écrit dans son propre profil.
    private static bool IsTestLaunch()
    {
        if (!IsAvailable)
            return false;
        string toolsPath = ProjectSettings.GlobalizePath("res://tools/");
        foreach (string argument in OS.GetCmdlineArgs())
        {
            if (IsToolsPath(argument, toolsPath))
                return true;
        }
        return IsToolsPath(ProjectSettings.GetSetting("application/run/main_scene", "").AsString(), toolsPath);
    }

    private static bool IsToolsPath(string path, string absoluteToolsPath)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.StartsWith("res://tools/", StringComparison.Ordinal)
            || normalized.StartsWith("tools/", StringComparison.Ordinal)
            || normalized.StartsWith(absoluteToolsPath, StringComparison.Ordinal);
    }

    internal static void RequireTestAccess()
    {
        if (!IsEnabled && !IsTestSession)
            throw new InvalidOperationException("Les outils de run exigent le profil dev ou une scène de banc dans tools/.");
    }

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
    public static bool IsTestSession => false;
    public static bool CanSubmitResults => true;
#endif

    public static RunProvenance CurrentProvenance => IsEnabled ? RunProvenance.Development
        : IsTestSession ? RunProvenance.Test : RunProvenance.Normal;

    /// <summary>Appelé depuis le Hub, avant de le recharger. Aucun changement de profil en pleine run.</summary>
    internal static bool SetEnabled(bool enabled)
    {
#if TOOLS
        if (!IsAvailable)
            return false;
        if (GameManagerStateIsRun())
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
        _usedDevelopment |= enabled;
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

#if TOOLS
    private static bool GameManagerStateIsRun()
    {
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        return tree?.Root.GetNodeOrNull<Core.GameManager>("GameManager")?.CurrentState == Core.GameManager.GameState.Run;
    }
#endif

    public static string GetSavePath(string relativePath)
    {
        string path = (IsEnabled ? "user://dev/" : IsTestSession ? "user://tests/" : "user://") + relativePath;
        Error result = DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());
        if (result != Error.Ok)
            throw new InvalidOperationException($"Impossible de préparer le profil : {path.GetBaseDir()} ({result}).");
        return path;
    }
}
