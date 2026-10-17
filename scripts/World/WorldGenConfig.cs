using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Réglages de génération du monde (data/world/world_gen.json), lus strictement (plan 26 Q7c-4b) : clés connues, types,
/// bornes, biomes et décors existants. Un fichier refusé ne génère rien : le chargement s'arrête sur un diagnostic.
/// Chaque lecture rend une copie neuve, que la terre en construction peut ajuster (<see cref="LandSpec.ApplyTo"/>).
/// </summary>
public class WorldGenConfig
{
    public const string FilePath = "res://data/world/world_gen.json";
    private static readonly string[] Terrains = { "grass", "concrete", "water", "forest" };

    /// <summary>Demi-largeur de la carte, en colonnes de cellules (map_radius).</summary>
    public int MapRadius;
    /// <summary>Demi-hauteur, en rangées (map_radius_y, plan 22 C6) ; égale à la largeur si absente.</summary>
    public int MapRadiusY;
    public int SpawnClearance;
    public int CaIterations;
    public List<WorldGenerator.ZoneConfig> Zones = new();
    public int BiomeCount = 3;
    public int EdgeFadeWidth = 5;
    public int PoiMinDistanceBetween = 8;
    /// <summary>POI et éléments de lore V1, désactivés jusqu'à leur refonte (plan 18 §5, plan 17 lot 0B).</summary>
    public bool PoisEnabled = true;
    public bool LoreElementsEnabled = true;
    public int FogRevealRadius = 6;
    public int FogInitialClearRadius = 8;
    public List<string> AvailableBiomes = new();
    public WorldGenerator.BiomeLayoutConfig BiomeLayout = WorldGenerator.BiomeLayoutConfig.Default;
    public PropRules PropRules = PropRules.Default;
    public GroundBlendConfig GroundBlend = GroundBlendConfig.Default;
    public PathNetworkConfig Paths = PathNetworkConfig.Default;
    public JunctionPropPlacer.Config JunctionProps;
    public LandsConfig Lands = LandsConfig.Disabled;

    /// <summary>Réglages valides, ou une exception qui nomme le champ fautif (outils et bancs).</summary>
    public static WorldGenConfig Load()
    {
        if (!TryLoad(out WorldGenConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    /// <summary>Copie neuve des réglages, ou un diagnostic fichier/champ sans réglages.</summary>
    public static bool TryLoad(out WorldGenConfig config, out string error)
    {
        config = null;
        using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            error = $"{FilePath} : fichier illisible";
            return false;
        }
        BiomeDataLoader.Load();
        if (!TryParse(file.GetAsText(), id => BiomeDataLoader.Get(id) != null,
                path => ResourceLoader.Exists($"res://assets/props/{path}.png"), out config, out error))
        {
            error = $"{FilePath} : {error}";
            return false;
        }
        GD.Print($"[WorldGenConfig] Loaded — radius={config.MapRadius}×{config.MapRadiusY}, zones={config.Zones.Count}, biomes={config.BiomeCount}");
        return true;
    }

    /// <summary>
    /// Lit le texte du fichier. <paramref name="biomeExists"/> reconnaît un biome du catalogue ; <paramref name="propExists"/>
    /// un décor par son chemin relatif à assets/props, sans extension.
    /// </summary>
    public static bool TryParse(string text, Func<string, bool> biomeExists, Func<string, bool> propExists,
        out WorldGenConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonElement root = document.RootElement;
            JsonConfigReader reader = new(root);
            reader.AllowOnly(root, "world_gen", "map_radius", "map_radius_y", "spawn_clearance", "lands", "props",
                "ground_blend", "paths", "junction_props", "ca_iterations", "edge_fade_width", "biome_count", "biome_layout",
                "available_biomes", "zones", "poi_min_distance_between", "pois_enabled", "lore_elements_enabled",
                "fog_reveal_radius", "fog_initial_clear_radius");
            WorldGenConfig parsed = new();
            parsed.MapRadius = reader.Integer(root, "map_radius", 8, 4000);
            parsed.MapRadiusY = JsonConfigReader.Has(root, "map_radius_y") ? reader.Integer(root, "map_radius_y", 8, 4000) : parsed.MapRadius;
            parsed.SpawnClearance = JsonConfigReader.Has(root, "spawn_clearance") ? reader.Integer(root, "spawn_clearance", 0, 64) : 4;
            parsed.CaIterations = reader.Integer(root, "ca_iterations", 0, 16);
            if (JsonConfigReader.Has(root, "edge_fade_width"))
                parsed.EdgeFadeWidth = reader.Integer(root, "edge_fade_width", 0, 64);
            if (JsonConfigReader.Has(root, "poi_min_distance_between"))
                parsed.PoiMinDistanceBetween = reader.Integer(root, "poi_min_distance_between", 0, 4000);
            if (JsonConfigReader.Has(root, "pois_enabled"))
                parsed.PoisEnabled = reader.Flag(root, "pois_enabled");
            if (JsonConfigReader.Has(root, "lore_elements_enabled"))
                parsed.LoreElementsEnabled = reader.Flag(root, "lore_elements_enabled");
            if (JsonConfigReader.Has(root, "fog_reveal_radius"))
                parsed.FogRevealRadius = reader.Integer(root, "fog_reveal_radius", 0, 4000);
            if (JsonConfigReader.Has(root, "fog_initial_clear_radius"))
                parsed.FogInitialClearRadius = reader.Integer(root, "fog_initial_clear_radius", 0, 4000);

            // Liste absente ou vide : tous les biomes du catalogue (WorldSetup.LoadAvailableBiomes).
            if (JsonConfigReader.Has(root, "available_biomes"))
            {
                foreach (JsonElement item in reader.List(root, "available_biomes", 0))
                {
                    string id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                    if (string.IsNullOrEmpty(id) || !biomeExists(id))
                        reader.Fail($"available_biomes : biome « {id} » inconnu");
                    else if (parsed.AvailableBiomes.Contains(id))
                        reader.Fail($"available_biomes : biome « {id} » en double");
                    else
                        parsed.AvailableBiomes.Add(id);
                }
            }
            if (JsonConfigReader.Has(root, "biome_count"))
            {
                parsed.BiomeCount = reader.Integer(root, "biome_count", 1, 64);
                if (reader.Error == null && parsed.AvailableBiomes.Count > 0 && parsed.BiomeCount > parsed.AvailableBiomes.Count)
                    reader.Fail($"biome_count : {parsed.BiomeCount} (au plus les {parsed.AvailableBiomes.Count} biomes de available_biomes)");
            }

            if (JsonConfigReader.Has(root, "biome_layout"))
                parsed.BiomeLayout = ReadLayout(reader, reader.Section("biome_layout"));
            if (JsonConfigReader.Has(root, "ground_blend"))
                parsed.GroundBlend = ReadBlend(reader, reader.Section("ground_blend"));
            if (JsonConfigReader.Has(root, "paths"))
                parsed.Paths = PathNetworkConfig.Read(reader, reader.Section("paths"));
            if (JsonConfigReader.Has(root, "lands"))
                parsed.Lands = LandsConfig.Read(reader, reader.Section("lands"), SitePlacer.ScaledKinds);
            if (JsonConfigReader.Has(root, "junction_props"))
                parsed.JunctionProps = JunctionPropPlacer.Config.Read(reader, reader.Section("junction_props"), biomeExists, propExists);
            if (JsonConfigReader.Has(root, "props"))
                parsed.PropRules = ReadProps(reader, reader.Section("props"));
            parsed.Zones = ReadZones(reader);

            if (reader.Error != null)
            {
                error = reader.Error;
                return false;
            }
            config = parsed;
            error = null;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static WorldGenerator.BiomeLayoutConfig ReadLayout(JsonConfigReader reader, JsonElement section)
    {
        reader.AllowOnly(section, "biome_layout", "region_spacing", "warp_strength", "spawn_offset_factor");
        WorldGenerator.BiomeLayoutConfig layout = WorldGenerator.BiomeLayoutConfig.Default;
        if (JsonConfigReader.Has(section, "region_spacing"))
            layout.RegionSpacing = reader.Positive(section, "region_spacing");
        if (JsonConfigReader.Has(section, "warp_strength"))
            layout.WarpStrength = reader.NonNegative(section, "warp_strength");
        if (JsonConfigReader.Has(section, "spawn_offset_factor"))
            layout.SpawnOffsetFactor = reader.Chance(section, "spawn_offset_factor");
        return layout;
    }

    private static GroundBlendConfig ReadBlend(JsonConfigReader reader, JsonElement section)
    {
        reader.AllowOnly(section, "ground_blend", "enabled", "band_px", "edge_noise", "grain", "noise_scale");
        GroundBlendConfig blend = GroundBlendConfig.Default;
        if (JsonConfigReader.Has(section, "enabled"))
            blend.Enabled = reader.Flag(section, "enabled");
        if (JsonConfigReader.Has(section, "band_px"))
            blend.BandPx = reader.Positive(section, "band_px");
        if (JsonConfigReader.Has(section, "edge_noise"))
            blend.EdgeNoise = reader.NonNegative(section, "edge_noise");
        if (JsonConfigReader.Has(section, "noise_scale"))
            blend.NoiseScale = reader.Positive(section, "noise_scale");
        if (JsonConfigReader.Has(section, "grain"))
            blend.Grain = reader.Chance(section, "grain");
        return blend;
    }

    private static PropRules ReadProps(JsonConfigReader reader, JsonElement section)
    {
        reader.AllowOnly(section, "props", "min_blocking_height", "min_blocking_pixels", "footprint_scale",
            "ground_decal_max_height", "occluder_min_height", "trample_max_height", "trample_prefixes");
        PropRules rules = PropRules.Default;
        if (JsonConfigReader.Has(section, "min_blocking_height"))
            rules.MinBlockingHeight = reader.NonNegative(section, "min_blocking_height");
        if (JsonConfigReader.Has(section, "min_blocking_pixels"))
            rules.MinBlockingPixels = reader.Integer(section, "min_blocking_pixels", 0, 1_000_000);
        if (JsonConfigReader.Has(section, "footprint_scale"))
            rules.FootprintScale = reader.Positive(section, "footprint_scale");
        if (JsonConfigReader.Has(section, "ground_decal_max_height"))
            rules.GroundDecalMaxHeight = reader.NonNegative(section, "ground_decal_max_height");
        if (JsonConfigReader.Has(section, "occluder_min_height"))
            rules.OccluderMinHeight = reader.NonNegative(section, "occluder_min_height");
        if (JsonConfigReader.Has(section, "trample_max_height"))
            rules.TrampleMaxHeight = reader.NonNegative(section, "trample_max_height");
        if (JsonConfigReader.Has(section, "trample_prefixes"))
        {
            List<string> prefixes = new();
            foreach (JsonElement item in reader.List(section, "trample_prefixes", 0))
            {
                if (item.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(item.GetString()))
                    reader.Fail("props.trample_prefixes : textes non vides attendus");
                else
                    prefixes.Add(item.GetString());
            }
            rules.TramplePrefixes = prefixes.ToArray();
        }
        return rules;
    }

    /// <summary>Zones de terrain du centre vers le bord : rayons strictement croissants, quatre poids de total positif.</summary>
    private static List<WorldGenerator.ZoneConfig> ReadZones(JsonConfigReader reader)
    {
        List<WorldGenerator.ZoneConfig> zones = new();
        float previous = 0f;
        foreach (JsonElement item in reader.List(reader.Root, "zones", 1))
        {
            reader.AllowOnly(item, "zones", "name", "max_radius", "weights");
            if (JsonConfigReader.Has(item, "name"))
                reader.Text(item, "name");
            float maxRadius = reader.Positive(item, "max_radius");
            if (reader.Error == null && maxRadius <= previous)
                reader.Fail(FormattableString.Invariant($"zones.max_radius : {maxRadius} (rayons strictement croissants attendus)"));
            previous = maxRadius;
            JsonElement weights = reader.Section(item, "weights");
            reader.AllowOnly(weights, "zones.weights", Terrains);
            float[] values = new float[Terrains.Length];
            float total = 0f;
            for (int i = 0; i < Terrains.Length; i++)
            {
                values[i] = reader.NonNegative(weights, Terrains[i]);
                total += values[i];
            }
            if (reader.Error == null && total <= 0f)
                reader.Fail("zones.weights : total strictement positif attendu");
            zones.Add(new WorldGenerator.ZoneConfig
            {
                MaxRadius = maxRadius,
                GrassWeight = values[0],
                ConcreteWeight = values[1],
                WaterWeight = values[2],
                ForestWeight = values[3],
            });
        }
        return zones;
    }
}
