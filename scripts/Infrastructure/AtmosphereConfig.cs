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
    /// <summary>Dosage commun des étalonnages de biome (1 : réglages des biomes tels quels).</summary>
    public float GradeStrength { get; private init; }

    /// <summary>Ombres de nuages (A2) : sens de défilement à l'écran (unitaire), vitesse et taille d'un nuage au sol.</summary>
    public Vector2 CloudDirection { get; private init; }
    public float CloudSpeedPxPerSecond { get; private init; }
    public float CloudScalePx { get; private init; }

    /// <summary>
    /// Ombres portées des grands décors (A3) : décalage à l'écran d'un pixel de hauteur (soleil bas, direction au sol
    /// projetée en 2:1), hauteur visible minimale d'un décor qui projette, couleur de l'ombre.
    /// </summary>
    public Vector2 CastShadowPerHeight { get; private init; }
    public float CastShadowMinHeightPx { get; private init; }
    public Color CastShadowColor { get; private init; }

    /// <summary>
    /// Direction crade (plan 40) : dosage commun des biomes, puis la recette, salissure de la palette (saturation
    /// gardée, jaunissement du papier, noir relevé, part des ombres tirée vers le vert-gris) et grain au pixel du monde.
    /// </summary>
    public float GrimeStrength { get; private init; }
    public float GrimeSaturation { get; private init; }
    public Color GrimePaper { get; private init; }
    public Color GrimeInk { get; private init; }
    public float GrimeShadowTone { get; private init; }
    public float GrimeGrain { get; private init; }
    public float GrimeFibers { get; private init; }

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
            reader.AllowOnly(root, "racine", "grade", "clouds", "cast_shadows", "grime");
            JsonElement grade = reader.Section("grade");
            reader.AllowOnly(grade, "grade", "fade_seconds", "entity_share", "strength");
            JsonElement clouds = reader.Section("clouds");
            reader.AllowOnly(clouds, "clouds", "direction_deg", "speed_px_s", "scale_px");
            JsonElement shadows = reader.Section("cast_shadows");
            reader.AllowOnly(shadows, "cast_shadows", "direction_deg", "length", "min_height_px", "color");
            JsonElement grime = reader.Section("grime");
            reader.AllowOnly(grime, "grime", "strength", "saturation", "paper", "ink", "shadow_tone", "grain", "fibers");
            float shadowAngle = Mathf.DegToRad(reader.Number(shadows, "direction_deg"));
            float shadowLength = reader.Positive(shadows, "length");
            AtmosphereConfig parsed = new()
            {
                GradeFadeSeconds = reader.Positive(grade, "fade_seconds"),
                EntityGradeShare = reader.Chance(grade, "entity_share"),
                GradeStrength = reader.Chance(grade, "strength"),
                CloudDirection = Vector2.Right.Rotated(Mathf.DegToRad(reader.Number(clouds, "direction_deg"))),
                CloudSpeedPxPerSecond = reader.NonNegative(clouds, "speed_px_s"),
                CloudScalePx = reader.Positive(clouds, "scale_px"),
                CastShadowPerHeight = new Vector2(Mathf.Cos(shadowAngle), Mathf.Sin(shadowAngle) * 0.5f) * shadowLength,
                CastShadowMinHeightPx = reader.Positive(shadows, "min_height_px"),
                CastShadowColor = reader.Rgb(shadows, "color"),
                GrimeStrength = reader.Chance(grime, "strength"),
                GrimeSaturation = reader.Chance(grime, "saturation"),
                GrimePaper = reader.Rgb(grime, "paper"),
                GrimeInk = reader.Rgb(grime, "ink"),
                GrimeShadowTone = reader.Chance(grime, "shadow_tone"),
                GrimeGrain = reader.NonNegative(grime, "grain"),
                GrimeFibers = reader.NonNegative(grime, "fibers"),
            };
            // Au-delà, la salissure devient un filtre : sol boueux, noirs délavés, bruit qui mange le dessin.
            if (Mathf.Min(parsed.GrimePaper.R, Mathf.Min(parsed.GrimePaper.G, parsed.GrimePaper.B)) < 0.6f)
                reader.Fail("grime.paper : chaque composante de 0,6 à 1 attendue");
            if (Mathf.Max(parsed.GrimeInk.R, Mathf.Max(parsed.GrimeInk.G, parsed.GrimeInk.B)) > 0.2f)
                reader.Fail("grime.ink : au plus 0,2 par composante attendu");
            if (parsed.GrimeGrain > 0.2f || parsed.GrimeFibers > 0.2f)
                reader.Fail("grime.grain et grime.fibers : au plus 0,2 attendu");
            if (shadowLength > 2f)
                reader.Fail("cast_shadows.length : au plus 2 attendu");
            if (parsed.GradeFadeSeconds > 10f)
                reader.Fail("grade.fade_seconds : au plus 10 s attendu");
            if (parsed.CloudSpeedPxPerSecond > 200f)
                reader.Fail("clouds.speed_px_s : au plus 200 attendu");
            if (parsed.CloudScalePx is > 0f and (< 64f or > 4096f))
                reader.Fail("clouds.scale_px : de 64 à 4096 attendu");
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
