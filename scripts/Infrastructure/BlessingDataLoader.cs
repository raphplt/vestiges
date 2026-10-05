using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Bénédictions des Mémoriaux (data/progression/blessings.json), contrôlées en entier (plan 26 Q7c).</summary>
public static class BlessingDataLoader
{
    private const string ConfigPath = "res://data/progression/blessings.json";
    private static readonly List<StatEffectData> _blessings = new();
    private static bool _loaded;
    private static string _loadError;

    public static IReadOnlyList<StatEffectData> All
    {
        get
        {
            Load();
            return _blessings;
        }
    }

    /// <summary>Liste lue et contrôlée ; faux, avec la raison, si elle a été refusée (le chargement de la run s'arrête).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    /// <summary>Contrôle un texte de bénédictions et ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json)
    {
        if (!ObjectDataValidator.TryParseContract(FileAccess.GetFileAsString(ObjectDataValidator.ContractPath),
                out ObjectDataValidator.Contract contract, out string contractError))
            return contractError;
        return StatEffectReader.Read(json, "blessings", contract, key => TranslationServer.Translate(key) != key, _blessings);
    }

    private static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        string error = FileAccess.FileExists(ConfigPath) ? Apply(FileAccess.GetFileAsString(ConfigPath)) : "absent";
        if (error != null)
        {
            _loadError = $"{ConfigPath} : {error}";
            GD.PushError($"[BlessingDataLoader] {_loadError}");
        }
    }
}
