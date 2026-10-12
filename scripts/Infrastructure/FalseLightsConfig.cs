using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Rythme d'une fausse lumière (plan 39 B4) : clignote, grésille, respire ; fenêtre éclairée qui tremble à peine
/// (Steady) ou télévision restée allumée (Screen).
/// </summary>
public enum FalseLightPattern { Blink, Buzz, Breathe, Steady, Screen }

/// <summary>Une sorte de lampe : le décor qui la porte, son ampoule, sa couleur et son rythme.</summary>
public sealed class FalseLightKind
{
    public string Sprite { get; init; }
    public Vector2 Anchor { get; init; }
    public Color Color { get; init; }
    public FalseLightPattern Pattern { get; init; }
    public float PeriodS { get; init; }
    public float OnShare { get; init; }
    /// <summary>Creux d'une fenêtre qui tremble ou d'une image de télévision (rythmes Steady et Screen).</summary>
    public float Floor { get; init; }
    public float Halo { get; init; }
    public float Pool { get; init; }
    /// <summary>Part de ces décors qui gardent leur lumière ; par défaut, celle de la racine (lit_share).</summary>
    public float LitShare { get; init; }
    /// <summary>Fume (feu de camp éteint, plan 39 B4c).</summary>
    public bool Smoke { get; init; }
}

/// <summary>Fumée des feux de camp éteints (plan 39 B4c) : bouffées qui montent, dérivent au vent et se dissipent.</summary>
public sealed class FalseLightSmoke
{
    public float IntervalS { get; init; }
    public float LifeS { get; init; }
    public float RisePxPerSecond { get; init; }
    public float DriftPxPerSecond { get; init; }
    public float Grow { get; init; }
    public float Alpha { get; init; }
    public int MaxPuffs { get; init; }
    public Color Color { get; init; }
}

/// <summary>
/// Fausses lumières (plan 39 B4a), lues depuis <c>data/world/false_lights.json</c> et contrôlées en entier : fichier
/// invalide, aucune lampe ne s'allume, avec un message.
/// </summary>
public sealed class FalseLightsConfig
{
    private const string ConfigPath = "res://data/world/false_lights.json";
    /// <summary>Immeubles de la ville (UrbanBuildingPlacer) : leurs fenêtres vitrées peuvent s'allumer.</summary>
    public const string WindowSpritePrefix = "prop_bld_";
    private static readonly string[] Patterns = { "blink", "buzz", "breathe" };

    public float LitShare { get; private init; }
    public float DayStrength { get; private init; }
    public float DarkStrength { get; private init; }
    public float DarknessFadeS { get; private init; }
    public IReadOnlyDictionary<string, float> WeatherDarkness { get; private init; }
    public float MemorySteady { get; private init; }
    public float MemoryDead { get; private init; }
    /// <summary>Niveau d'une lampe éteinte par son rythme ou par l'oubli : le filament qui refroidit.</summary>
    public float OffLevel { get; private init; }
    public float BreatheFloor { get; private init; }
    public float BuzzDropChance { get; private init; }
    public float BuzzDropLevel { get; private init; }
    public float BuzzFloor { get; private init; }
    /// <summary>Part des pas coupés à l'approche de memory_dead, et durée d'un pas de coupure.</summary>
    public float FailCutMax { get; private init; }
    public float FailStepS { get; private init; }
    public IReadOnlyList<FalseLightKind> Kinds { get; private init; }

    /// <summary>Fenêtres éclairées (plan 39 B4b) : part des immeubles qui en gardent une, lueur chaude ou télévision.</summary>
    public string WindowsPath { get; private init; }
    public float BuildingShare { get; private init; }
    public float ScreenShare { get; private init; }
    public FalseLightKind WindowKind { get; private init; }
    public FalseLightKind ScreenKind { get; private init; }
    public FalseLightSmoke Smoke { get; private init; }

    public static bool TryLoad(out FalseLightsConfig config, out string error)
    {
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            config = null;
            error = $"{ConfigPath} absent";
            return false;
        }
        HashSet<string> weathers = new();
        if (WeatherConfig.TryLoad(out WeatherConfig weather, out _))
        {
            foreach (WeatherData data in weather.Weathers)
                weathers.Add(data.Id);
        }
        if (!TryParse(file.GetAsText(), PropSpriteExists, path => FileAccess.FileExists(path), weathers, out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        error = null;
        return true;
    }

    public static bool TryParse(string json, Func<string, bool> spriteExists, Func<string, bool> windowsExist,
        IReadOnlyCollection<string> weathers,
        out FalseLightsConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonConfigReader reader = new(root);
            reader.AllowOnly(root, "racine", "lit_share", "day_strength", "dark_strength", "darkness_fade_s",
                "weather_darkness", "memory_steady", "memory_dead", "rhythm", "kinds", "windows", "smoke");
            float litShare = reader.Chance(root, "lit_share");
            float dayStrength = reader.Chance(root, "day_strength");
            float darkStrength = reader.Ratio(root, "dark_strength");
            float fade = reader.Positive(root, "darkness_fade_s");
            float memorySteady = reader.Chance(root, "memory_steady");
            float memoryDead = reader.Chance(root, "memory_dead");
            JsonElement rhythm = reader.Section("rhythm");
            reader.AllowOnly(rhythm, "rhythm", "off_level", "breathe_floor", "buzz_drop_chance", "buzz_drop_level",
                "buzz_floor", "fail_cut_max", "fail_step_s");
            float offLevel = reader.Chance(rhythm, "off_level");
            float breatheFloor = reader.Chance(rhythm, "breathe_floor");
            float buzzDropChance = reader.Chance(rhythm, "buzz_drop_chance");
            float buzzDropLevel = reader.Chance(rhythm, "buzz_drop_level");
            float buzzFloor = reader.Chance(rhythm, "buzz_floor");
            float failCutMax = reader.Chance(rhythm, "fail_cut_max");
            float failStep = reader.Positive(rhythm, "fail_step_s");

            Dictionary<string, float> darkness = new();
            foreach ((string id, JsonElement value) in reader.Entries(reader.Section("weather_darkness"), "weather_darkness"))
            {
                if (!JsonConfigReader.Contains(weathers, id))
                    reader.Fail($"weather_darkness : météo « {id} » inconnue");
                else if (value.ValueKind != JsonValueKind.Number || value.GetDouble() < 0.0 || value.GetDouble() > 1.0)
                    reader.Fail($"weather_darkness.{id} : obscurité dans [0 ; 1] attendue");
                else
                    darkness[id] = (float)value.GetDouble();
            }

            List<FalseLightKind> kinds = new();
            HashSet<string> sprites = new();
            foreach (JsonElement entry in reader.List(root, "kinds", 1))
            {
                reader.AllowOnly(entry, "kinds", "sprite", "anchor", "color", "pattern", "period_s", "on_share", "halo", "pool",
                    "lit_share", "smoke");
                string sprite = reader.Text(entry, "sprite");
                if (sprite != null && !spriteExists(sprite))
                    reader.Fail($"kinds : décor « {sprite} » absent des manifestes");
                if (sprite != null && !sprites.Add(sprite))
                    reader.Fail($"kinds : décor « {sprite} » en double");
                Vector2 anchor = Anchor(reader, entry);
                Color color = reader.Rgb(entry, "color");
                string pattern = reader.OneOf(entry, "pattern", Patterns);
                float period = reader.Positive(entry, "period_s");
                FalseLightPattern parsed = pattern switch
                {
                    "buzz" => FalseLightPattern.Buzz,
                    "breathe" => FalseLightPattern.Breathe,
                    _ => FalseLightPattern.Blink,
                };
                // Seul un clignotement a une part allumée ; ailleurs la clé serait trompeuse.
                float onShare = 1f;
                if (parsed == FalseLightPattern.Blink)
                    onShare = reader.Ratio(entry, "on_share");
                else if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("on_share", out _))
                    reader.Fail($"kinds : on_share réservé au rythme blink ({sprite})");
                float halo = reader.Chance(entry, "halo");
                float pool = reader.Chance(entry, "pool");
                float kindShare = entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("lit_share", out _)
                    ? reader.Chance(entry, "lit_share")
                    : litShare;
                bool smokes = reader.Flag(entry, "smoke");
                kinds.Add(new FalseLightKind
                {
                    Sprite = sprite, Anchor = anchor, Color = color, Pattern = parsed, PeriodS = period,
                    OnShare = onShare, Halo = halo, Pool = pool, LitShare = kindShare, Smoke = smokes,
                });
            }

            JsonElement windows = reader.Section("windows");
            reader.AllowOnly(windows, "windows", "data", "building_share", "color", "flicker", "halo", "screen_share",
                "screen_color", "screen_period_s", "screen_floor");
            string windowsPath = reader.Text(windows, "data");
            if (windowsPath != null && !windowsExist("res://" + windowsPath))
                reader.Fail($"windows.data : « {windowsPath} » introuvable");
            float buildingShare = reader.Chance(windows, "building_share");
            Color windowColor = reader.Rgb(windows, "color");
            float flicker = reader.Chance(windows, "flicker");
            float windowHalo = reader.Chance(windows, "halo");
            float screenShare = reader.Chance(windows, "screen_share");
            Color screenColor = reader.Rgb(windows, "screen_color");
            float screenPeriod = reader.Positive(windows, "screen_period_s");
            float screenFloor = reader.Chance(windows, "screen_floor");

            JsonElement smoke = reader.Section("smoke");
            reader.AllowOnly(smoke, "smoke", "interval_s", "life_s", "rise_px_s", "drift_px_s", "grow", "alpha", "max_puffs", "color");
            FalseLightSmoke smokeConfig = new()
            {
                IntervalS = reader.Positive(smoke, "interval_s"),
                LifeS = reader.Positive(smoke, "life_s"),
                RisePxPerSecond = reader.Positive(smoke, "rise_px_s"),
                DriftPxPerSecond = reader.NonNegative(smoke, "drift_px_s"),
                Grow = reader.NonNegative(smoke, "grow"),
                Alpha = reader.Ratio(smoke, "alpha"),
                MaxPuffs = reader.Integer(smoke, "max_puffs", 1, 256),
                Color = reader.Rgb(smoke, "color"),
            };

            // Au-dessus de memory_steady la lampe tient, sous memory_dead elle meurt : l'ordre inverse n'a pas de sens.
            if (memoryDead >= memorySteady)
                reader.Fail("memory_dead : doit rester sous memory_steady");
            // Une lampe éteinte qui brillerait plus qu'allumée inverserait chaque rythme.
            if (offLevel >= buzzDropLevel || buzzDropLevel >= buzzFloor || offLevel >= breatheFloor)
                reader.Fail("rhythm : off_level < buzz_drop_level < buzz_floor et off_level < breathe_floor attendus");
            if (dayStrength > darkStrength)
                reader.Fail("day_strength : doit rester sous dark_strength (les lampes ressortent dans le sombre)");
            error = reader.Error;
            if (error != null)
                return false;
            config = new FalseLightsConfig
            {
                LitShare = litShare, DayStrength = dayStrength, DarkStrength = darkStrength, DarknessFadeS = fade,
                WeatherDarkness = darkness, MemorySteady = memorySteady, MemoryDead = memoryDead, Kinds = kinds,
                WindowsPath = windowsPath, BuildingShare = buildingShare, ScreenShare = screenShare, Smoke = smokeConfig,
                // Une fenêtre tremble à peine (une variation par demi-seconde) ; un écran change à chaque plan.
                WindowKind = new FalseLightKind
                {
                    Sprite = WindowSpritePrefix, Color = windowColor, Pattern = FalseLightPattern.Steady, PeriodS = 0.5f,
                    OnShare = 1f, Floor = 1f - flicker, Halo = windowHalo,
                },
                ScreenKind = new FalseLightKind
                {
                    Sprite = WindowSpritePrefix, Color = screenColor, Pattern = FalseLightPattern.Screen,
                    PeriodS = screenPeriod, OnShare = 1f, Floor = screenFloor, Halo = windowHalo,
                },
                OffLevel = offLevel, BreatheFloor = breatheFloor, BuzzDropChance = buzzDropChance, BuzzDropLevel = buzzDropLevel,
                BuzzFloor = buzzFloor, FailCutMax = failCutMax, FailStepS = failStep,
            };
            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }

    /// <summary>Le décor existe dans l'un des dossiers de décors (assets/props/&lt;dossier&gt;/&lt;nom&gt;.png).</summary>
    public static bool PropSpriteExists(string stem)
    {
        foreach (string folder in DirAccess.GetDirectoriesAt("res://assets/props"))
        {
            if (ResourceLoader.Exists($"res://assets/props/{folder}/{stem}.png"))
                return true;
        }
        return false;
    }

    private static Vector2 Anchor(JsonConfigReader reader, JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("anchor", out JsonElement anchor)
            || anchor.ValueKind != JsonValueKind.Array || anchor.GetArrayLength() != 2
            || anchor[0].ValueKind != JsonValueKind.Number || anchor[1].ValueKind != JsonValueKind.Number)
        {
            reader.Fail("anchor : [x, y] en px du sprite attendu");
            return Vector2.Zero;
        }
        Vector2 value = new((float)anchor[0].GetDouble(), (float)anchor[1].GetDouble());
        // Pixel du sprite, compté depuis son coin haut-gauche.
        if (value.X < 0f || value.Y < 0f)
            reader.Fail("anchor : px du sprite, positifs, attendus");
        return value;
    }
}
