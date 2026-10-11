using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Atmosphère commune aux biomes (plan 39), lue depuis <c>data/world/atmosphere.json</c> et contrôlée en entier : un
/// fichier invalide est refusé avec le nom du champ, et la run reste sans étalonnage plutôt que de jouer avec des
/// valeurs inventées.
/// </summary>
public sealed class AtmosphereConfig
{
    private const string ConfigPath = "res://data/world/atmosphere.json";

    /// <summary>Fondu de l'étalonnage au changement de biome.</summary>
    public float GradeFadeSeconds { get; private init; }
    /// <summary>Part de l'étalonnage reçue par le joueur et les créatures.</summary>
    public float EntityGradeShare { get; private init; }

    public static bool TryLoad(out AtmosphereConfig config, out string error)
    {
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            config = null;
            error = $"{ConfigPath} absent";
            return false;
        }
        if (!TryParse(file.GetAsText(), out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        error = null;
        return true;
    }

    public static bool TryParse(string json, out AtmosphereConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonConfigReader reader = new(root);
            reader.AllowOnly(root, "racine", "grade");
            JsonElement grade = reader.Section("grade");
            reader.AllowOnly(grade, "grade", "fade_seconds", "entity_share");
            AtmosphereConfig parsed = new()
            {
                GradeFadeSeconds = reader.Positive(grade, "fade_seconds"),
                EntityGradeShare = reader.Chance(grade, "entity_share"),
            };
            if (parsed.GradeFadeSeconds > 10f)
                reader.Fail("grade.fade_seconds : au plus 10 s attendu");
            error = reader.Error;
            config = error == null ? parsed : null;
            return error == null;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }
}
