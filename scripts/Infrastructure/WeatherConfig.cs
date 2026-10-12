using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Particules d'atmosphère autour de la caméra : météo (cendres, débris au vent, pluie) ou biome (feuilles, papiers,
/// pollen, poussière qui tombe, poussière qui scintille dans la lumière).
/// </summary>
public enum AtmosphereParticleKind { None, Ash, Wind, Rain, Leaves, Paper, Pollen, Dust, Glint }

/// <summary>
/// Une météo (plan 39 A5) : durée, biomes où elle peut tomber, et ce qu'elle fait aux couches d'atmosphère du biome
/// (étalonnage, nuages, ombres portées), à la vue (brouillard), aux particules et au ciel (éclairs).
/// </summary>
public sealed class WeatherData
{
    public string Id { get; init; }
    public float DurationMinS { get; init; }
    public float DurationMaxS { get; init; }
    public float FadeS { get; init; }
    /// <summary>Poids par biome ; un biome absent ou à 0 ne la reçoit jamais.</summary>
    public IReadOnlyDictionary<string, float> BiomeWeights { get; init; }
    /// <summary>Facteurs appliqués à l'étalonnage du biome.</summary>
    public float SaturationFactor { get; init; } = 1f;
    public float ContrastFactor { get; init; } = 1f;
    public float ExposureFactor { get; init; } = 1f;
    /// <summary>Couverture et noirceur des nuages : au moins ces valeurs ; vitesse de leur défilement multipliée.</summary>
    public float CloudCoverMin { get; init; }
    public float CloudOpacityMin { get; init; }
    public float CloudSpeedFactor { get; init; } = 1f;
    public float ShadowFactor { get; init; } = 1f;
    /// <summary>Balancement du vent commun multiplié (plan 39 B1) : herbes et canopées ploient plus fort.</summary>
    public float WindFactor { get; init; } = 1f;
    /// <summary>Brouillard qui réduit la vue : rayon net autour du joueur, rayon où il devient plein, opacité, couleur.</summary>
    public float ViewClearPx { get; init; }
    public float ViewFullPx { get; init; }
    public float ViewOpacity { get; init; }
    public Color ViewColor { get; init; }
    public AtmosphereParticleKind Particles { get; init; }
    public int ParticleAmount { get; init; }
    public Color ParticleColor { get; init; }
    /// <summary>Éclairs lointains : intervalle entre deux, et sursaut d'exposition (0 : aucun).</summary>
    public float FlashMinS { get; init; }
    public float FlashMaxS { get; init; }
    public float FlashExposure { get; init; }
}

/// <summary>
/// Météo de la run (plan 39 A5), lue depuis <c>data/world/weather.json</c> et contrôlée en entier : fichier invalide,
/// la run reste sans météo, avec un message.
/// </summary>
public sealed class WeatherConfig
{
    private const string ConfigPath = "res://data/world/weather.json";
    private const int MaxParticles = 400;

    public float FirstMinS { get; private init; }
    public float FirstMaxS { get; private init; }
    public float GapMinS { get; private init; }
    public float GapMaxS { get; private init; }
    public IReadOnlyList<WeatherData> Weathers { get; private init; }

    public WeatherData Find(string id)
    {
        foreach (WeatherData weather in Weathers)
        {
            if (weather.Id == id)
                return weather;
        }
        return null;
    }

    public static bool TryLoad(out WeatherConfig config, out string error)
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

    public static bool TryParse(string json, out WeatherConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonConfigReader reader = new(root);
            reader.AllowOnly(root, "racine", "schedule", "weathers");
            JsonElement schedule = reader.Section("schedule");
            reader.AllowOnly(schedule, "schedule", "first_sec", "gap_sec");
            (float firstMin, float firstMax) = reader.Range(schedule, "first_sec", 0f);
            (float gapMin, float gapMax) = reader.Range(schedule, "gap_sec", 10f);
            JsonElement weathers = reader.Section("weathers");
            List<WeatherData> list = new();
            foreach ((string id, JsonElement _) in reader.Entries(weathers, "weathers"))
                list.Add(Weather(reader, reader.Section(weathers, id), id));
            if (list.Count == 0)
                reader.Fail("weathers : au moins une météo attendue");
            error = reader.Error;
            if (error != null)
                return false;
            config = new WeatherConfig
            {
                FirstMinS = firstMin, FirstMaxS = firstMax, GapMinS = gapMin, GapMaxS = gapMax, Weathers = list.AsReadOnly()
            };
            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }

    private static WeatherData Weather(JsonConfigReader reader, JsonElement section, string id)
    {
        string where = $"weathers.{id}";
        reader.AllowOnly(section, where, "duration_sec", "fade_sec", "biomes", "grade", "clouds", "shadows", "wind", "view", "particles", "flashes");
        (float durationMin, float durationMax) = reader.Range(section, "duration_sec", 5f);
        float fade = reader.Positive(section, "fade_sec");
        JsonElement biomeSection = reader.Section(section, "biomes");
        Dictionary<string, float> biomes = new();
        foreach ((string biome, JsonElement _) in reader.Entries(biomeSection, $"{where}.biomes"))
            biomes[biome] = reader.NonNegative(biomeSection, biome);

        float saturation = 1f, contrast = 1f, exposure = 1f;
        if (Has(section, "grade"))
        {
            JsonElement grade = reader.Section(section, "grade");
            reader.AllowOnly(grade, $"{where}.grade", "saturation", "contrast", "exposure");
            saturation = reader.Positive(grade, "saturation");
            contrast = reader.Positive(grade, "contrast");
            exposure = reader.Positive(grade, "exposure");
            if (exposure is < 0.5f or > 1.3f || contrast is < 0.6f or > 1.4f || saturation > 1.6f)
                reader.Fail($"{where}.grade : exposition 0,5–1,3, contraste 0,6–1,4, saturation ≤ 1,6 attendus");
        }
        float cover = 0f, cloudOpacity = 0f, cloudSpeed = 1f;
        if (Has(section, "clouds"))
        {
            JsonElement clouds = reader.Section(section, "clouds");
            reader.AllowOnly(clouds, $"{where}.clouds", "cover", "opacity", "speed");
            cover = reader.Chance(clouds, "cover");
            cloudOpacity = reader.Chance(clouds, "opacity");
            cloudSpeed = reader.Positive(clouds, "speed");
            if (cloudOpacity > 0.6f || cloudSpeed > 10f)
                reader.Fail($"{where}.clouds : opacité ≤ 0,6 et vitesse ≤ 10 attendues");
        }
        float shadows = Has(section, "shadows") ? reader.Chance(section, "shadows") : 1f;
        float windFactor = Has(section, "wind") ? reader.Positive(section, "wind") : 1f;
        if (windFactor > 4f)
            reader.Fail($"{where}.wind : au plus 4 attendu");
        float clear = 0f, full = 0f, viewOpacity = 0f;
        Color viewColor = Colors.White;
        if (Has(section, "view"))
        {
            JsonElement view = reader.Section(section, "view");
            reader.AllowOnly(view, $"{where}.view", "clear_px", "full_px", "opacity", "color");
            clear = reader.Positive(view, "clear_px");
            full = reader.Positive(view, "full_px");
            viewOpacity = reader.Chance(view, "opacity");
            viewColor = reader.Rgb(view, "color");
            // En deçà, le joueur ne voit plus ce qui l'attaque : le brouillard gêne, il n'aveugle pas.
            if (clear < 120f || full <= clear)
                reader.Fail($"{where}.view : clear_px ≥ 120 et full_px > clear_px attendus");
        }
        AtmosphereParticleKind kind = AtmosphereParticleKind.None;
        int amount = 0;
        Color particleColor = Colors.White;
        if (Has(section, "particles"))
        {
            JsonElement particles = reader.Section(section, "particles");
            reader.AllowOnly(particles, $"{where}.particles", "kind", "amount", "color", "opacity");
            string name = reader.OneOf(particles, "kind", new[] { "ash", "wind", "rain" });
            kind = name switch { "ash" => AtmosphereParticleKind.Ash, "wind" => AtmosphereParticleKind.Wind, _ => AtmosphereParticleKind.Rain };
            amount = reader.Count(particles, "amount", MaxParticles);
            particleColor = reader.Rgb(particles, "color") with { A = reader.Ratio(particles, "opacity") };
        }
        float flashMin = 0f, flashMax = 0f, flashExposure = 0f;
        if (Has(section, "flashes"))
        {
            JsonElement flashes = reader.Section(section, "flashes");
            reader.AllowOnly(flashes, $"{where}.flashes", "interval_sec", "exposure");
            (flashMin, flashMax) = reader.Range(flashes, "interval_sec", 1f);
            flashExposure = reader.Positive(flashes, "exposure");
            if (flashExposure > 1f)
                reader.Fail($"{where}.flashes.exposure : au plus 1 attendu (sursaut ajouté à l'exposition)");
        }
        return new WeatherData
        {
            Id = id, DurationMinS = durationMin, DurationMaxS = durationMax, FadeS = fade, BiomeWeights = biomes,
            SaturationFactor = saturation, ContrastFactor = contrast, ExposureFactor = exposure,
            CloudCoverMin = cover, CloudOpacityMin = cloudOpacity, CloudSpeedFactor = cloudSpeed, ShadowFactor = shadows, WindFactor = windFactor,
            ViewClearPx = clear, ViewFullPx = full, ViewOpacity = viewOpacity, ViewColor = viewColor,
            Particles = kind, ParticleAmount = amount, ParticleColor = particleColor,
            FlashMinS = flashMin, FlashMaxS = flashMax, FlashExposure = flashExposure,
        };
    }

    private static bool Has(JsonElement owner, string key) =>
        owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(key, out _);
}
