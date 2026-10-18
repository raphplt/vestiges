using System;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages des relevés de run pour le bilan (data/ui/run_summary.json, plan 02 lot D M2), lus strictement une fois
/// (plan 26 Q7g-4a). Un fichier refusé arrête le chargement de la run (GameBootstrap) ; un champ absent garde sa valeur.
/// </summary>
public sealed class RunSummaryConfig
{
    public const string FilePath = "res://data/ui/run_summary.json";

    public float PixelsPerMeter { get; private init; } = 16f;
    public float DistanceStepSeconds { get; private init; } = 0.25f;

    private static RunSummaryConfig _loaded;
    private static string _loadError;
    private static bool _attempted;

    /// <summary>Les réglages du jeu ; refusés, une exception qui nomme le champ (le bootstrap les a déjà contrôlés).</summary>
    public static RunSummaryConfig Load()
    {
        if (!TryLoad(out RunSummaryConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    public static bool TryLoad(out RunSummaryConfig config, out string error)
    {
        if (!_attempted)
        {
            _attempted = true;
            _loadError = $"{FilePath} : lecture interrompue";
            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            if (file == null)
                _loadError = $"{FilePath} : fichier illisible";
            else if (TryParse(file.GetAsText(), out _loaded, out _loadError))
                _loadError = null;
            else
                _loadError = $"{FilePath} : {_loadError}";
        }
        config = _loaded;
        error = _loadError;
        return error == null;
    }

    public static bool TryParse(string text, out RunSummaryConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "bilan de run", "pixels_per_meter", "distance_step_seconds");
            RunSummaryConfig defaults = new();
            RunSummaryConfig parsed = new()
            {
                PixelsPerMeter = JsonConfigReader.Has(root, "pixels_per_meter") ? reader.Positive(root, "pixels_per_meter") : defaults.PixelsPerMeter,
                DistanceStepSeconds = JsonConfigReader.Has(root, "distance_step_seconds")
                    ? reader.Positive(root, "distance_step_seconds") : defaults.DistanceStepSeconds,
            };
            error = reader.Error;
            if (error != null)
                return false;
            config = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }
}
