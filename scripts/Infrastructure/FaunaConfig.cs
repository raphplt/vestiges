using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Une espèce de la faune qui fuit (plan 39 B2) : sprites, comportement de fuite, biomes où elle vit.</summary>
public sealed class FaunaSpecies
{
    public string Id { get; init; }
    public string RestAction { get; init; }
    public string FleeAction { get; init; }
    public float Fps { get; init; }
    public int GroupMin { get; init; }
    public int GroupMax { get; init; }
    public float FleeRadiusPx { get; init; }
    public float FleeSpeedPxPerSecond { get; init; }
    /// <summary>Montée en vol (px/s) ; 0 : reste au sol.</summary>
    public float ClimbPxPerSecond { get; init; }
    /// <summary>Distance parcourue en fuite avant de s'effacer.</summary>
    public float VanishAfterPx { get; init; }
    /// <summary>Errance au repos (papillons) ; 0 : immobile.</summary>
    public float WanderPx { get; init; }
    public IReadOnlyDictionary<string, float> BiomeWeights { get; init; }
}

/// <summary>
/// Faune qui fuit (plan 39 B2), lue depuis <c>data/world/fauna.json</c> et contrôlée en entier : fichier invalide, la run
/// reste sans faune, avec un message.
/// </summary>
public sealed class FaunaConfig
{
    private const string ConfigPath = "res://data/world/fauna.json";
    /// <summary>Manifeste des sprites écrit par tools/generate_fauna.py : une espèce n'existe que s'il la décrit.</summary>
    public const string ManifestPath = "res://assets/fauna/fauna_manifest.json";

    public int MaxAlive { get; private init; }
    public float SpawnIntervalS { get; private init; }
    /// <summary>Distance au bord de la vue où naît un groupe : jamais sous les yeux du joueur.</summary>
    public float SpawnMarginMinPx { get; private init; }
    public float SpawnMarginMaxPx { get; private init; }
    public float GroupSpreadPx { get; private init; }
    public float FadeS { get; private init; }
    /// <summary>Délai de réaction tiré par bête : un groupe ne décolle pas comme un seul oiseau.</summary>
    public float ReactionMaxS { get; private init; }
    /// <summary>Part de la fuite tirée vers la mémoire la plus forte, et écart tiré par bête (radians).</summary>
    public float ErasurePull { get; private init; }
    public float ScatterRad { get; private init; }
    /// <summary>Intervalle entre deux lectures de la mémoire de la zone, par bête.</summary>
    public float MemoryCheckS { get; private init; }
    public float DespawnDistancePx { get; private init; }
    public float MinMemory { get; private init; }
    public float NoiseRadiusPx { get; private init; }
    public IReadOnlyList<FaunaSpecies> AllSpecies { get; private init; }

    public static bool TryLoad(out FaunaConfig config, out string error)
    {
        HashSet<string> known = KnownSpecies();
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            config = null;
            error = $"{ConfigPath} absent";
            return false;
        }
        if (!TryParse(file.GetAsText(), known.Contains, out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        error = null;
        return true;
    }

    /// <summary>Espèces décrites par le manifeste des sprites.</summary>
    public static HashSet<string> KnownSpecies()
    {
        HashSet<string> known = new();
        using FileAccess file = FileAccess.Open(ManifestPath, FileAccess.ModeFlags.Read);
        if (file != null && Json.ParseString(file.GetAsText()).Obj is Godot.Collections.Dictionary manifest)
            foreach (Variant key in manifest.Keys)
                known.Add(key.AsString());
        return known;
    }

    public static bool TryParse(string json, System.Func<string, bool> speciesKnown, out FaunaConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonConfigReader reader = new(root);
            reader.AllowOnly(root, "racine", "max_alive", "spawn_interval_s", "spawn_margin_px", "despawn_distance_px",
                "min_memory", "noise_radius_px", "group_spread_px", "fade_s", "reaction_max_s", "erasure_pull", "scatter_rad",
                "memory_check_s", "species");
            // Au-delà, la faune devient une foule à mettre à jour chaque image et elle couvre le terrain.
            int maxAlive = reader.Count(root, "max_alive", 80);
            float interval = reader.Positive(root, "spawn_interval_s");
            (float marginMin, float marginMax) = reader.Range(root, "spawn_margin_px", 0f);
            float despawn = reader.Positive(root, "despawn_distance_px");
            float minMemory = reader.Chance(root, "min_memory");
            float noise = reader.NonNegative(root, "noise_radius_px");
            float spread = reader.NonNegative(root, "group_spread_px");
            float fade = reader.Positive(root, "fade_s");
            float reaction = reader.NonNegative(root, "reaction_max_s");
            float pull = reader.NonNegative(root, "erasure_pull");
            float scatter = reader.NonNegative(root, "scatter_rad");
            float memoryCheck = reader.Positive(root, "memory_check_s");
            // Un groupe déborde de son point de naissance : la marge doit l'éloigner du bord de la vue.
            if (marginMin < spread + 16f)
                reader.Fail("spawn_margin_px : au moins group_spread_px + 16 attendu (les bêtes naîtraient au bord de l'écran)");
            // Demi-diagonale de la vue (960 × 540 px monde) plus la marge : au-delà, rien ne naît sans disparaître aussitôt.
            if (despawn <= 551f + marginMax)
                reader.Fail("despawn_distance_px : plus loin que la vue et la marge d'apparition attendu");
            if (pull > 2f || scatter > 1.6f)
                reader.Fail("erasure_pull ≤ 2 et scatter_rad ≤ 1,6 attendus");
            JsonElement section = reader.Section("species");
            List<FaunaSpecies> species = new();
            foreach ((string id, JsonElement _) in reader.Entries(section, "species"))
            {
                if (!speciesKnown(id))
                    reader.Fail($"species.{id} : espèce sans sprites (assets/fauna/fauna_manifest.json)");
                species.Add(ParseSpecies(reader, reader.Section(section, id), id));
            }
            if (species.Count == 0)
                reader.Fail("species : au moins une espèce attendue");
            error = reader.Error;
            if (error != null)
                return false;
            config = new FaunaConfig
            {
                MaxAlive = maxAlive, SpawnIntervalS = interval, SpawnMarginMinPx = marginMin, SpawnMarginMaxPx = marginMax,
                DespawnDistancePx = despawn, MinMemory = minMemory, NoiseRadiusPx = noise, GroupSpreadPx = spread, FadeS = fade,
                ReactionMaxS = reaction, ErasurePull = pull, ScatterRad = scatter, MemoryCheckS = memoryCheck,
                AllSpecies = species.AsReadOnly(),
            };
            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }

    private static FaunaSpecies ParseSpecies(JsonConfigReader reader, JsonElement section, string id)
    {
        string where = $"species.{id}";
        reader.AllowOnly(section, where, "rest", "flee", "fps", "group", "flee_radius_px", "flee_speed_px_s", "climb_px_s",
            "vanish_after_px", "wander_px", "biomes");
        (float groupMin, float groupMax) = reader.Range(section, "group", 1f, 8f);
        float fps = reader.Positive(section, "fps");
        if (fps > 24f)
            reader.Fail($"{where}.fps : au plus 24 attendu");
        JsonElement biomes = reader.Section(section, "biomes");
        Dictionary<string, float> weights = new();
        foreach ((string biome, JsonElement _) in reader.Entries(biomes, $"{where}.biomes"))
            weights[biome] = reader.NonNegative(biomes, biome);
        return new FaunaSpecies
        {
            Id = id, RestAction = reader.Text(section, "rest"), FleeAction = reader.Text(section, "flee"), Fps = fps,
            GroupMin = Mathf.RoundToInt(groupMin), GroupMax = Mathf.RoundToInt(groupMax),
            FleeRadiusPx = reader.Positive(section, "flee_radius_px"), FleeSpeedPxPerSecond = reader.Positive(section, "flee_speed_px_s"),
            ClimbPxPerSecond = reader.NonNegative(section, "climb_px_s"), VanishAfterPx = reader.Positive(section, "vanish_after_px"),
            WanderPx = reader.NonNegative(section, "wander_px"), BiomeWeights = weights,
        };
    }
}
