using System.Text.Json;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>Réglages du réseau de chemins de terre (<c>paths</c> dans world_gen.json, plan 10 T3).</summary>
public struct PathNetworkConfig
{
    public bool Enabled;
    /// <summary>Deux régions sont voisines sous cette distance, en espacements de régions.</summary>
    public float NeighbourFactor;
    /// <summary>Part des liaisons voisines ajoutées à l'arbre couvrant, pour former des boucles.</summary>
    public float ExtraEdgeChance;
    /// <summary>Longueur maximale d'une liaison ajoutée, en espacements de régions.</summary>
    public float ExtraEdgeFactor;
    /// <summary>Nombre de régions reliées au point de départ.</summary>
    public int SpawnLinks;
    /// <summary>Variation du coût de marche par un bruit lent : les chemins serpentent.</summary>
    public float NoiseAmplitude;
    public float NoiseFrequency;
    public float RoadCost;
    public float ExistingPathCost;
    public float FieldLaneCost;
    public float ForestCost;
    public float SidewalkCost;
    public float PlazaCost;
    /// <summary>Ondulation latérale du tracé lissé, en pixels au sol.</summary>
    public float WobblePx;
    /// <summary>Aucun chemin à moins de ce nombre de cellules du bord dissous.</summary>
    public int EdgeMargin;
    /// <summary>Plafond de cellules explorées par liaison ; au-delà, la liaison est abandonnée.</summary>
    public int MaxExpansions;
    public PathStyle DefaultStyle;

    public static PathNetworkConfig Default => new()
    {
        Enabled = true,
        NeighbourFactor = 1.9f,
        ExtraEdgeChance = 0.3f,
        ExtraEdgeFactor = 1.5f,
        SpawnLinks = 2,
        NoiseAmplitude = 0.45f,
        NoiseFrequency = 0.05f,
        RoadCost = 0.35f,
        ExistingPathCost = 0.4f,
        FieldLaneCost = 0.6f,
        ForestCost = 1.35f,
        SidewalkCost = 2f,
        PlazaCost = 1.5f,
        WobblePx = 7f,
        EdgeMargin = 8,
        MaxExpansions = 80000,
        DefaultStyle = PathStyle.Default,
    };

    /// <summary>Section <c>paths</c> lue strictement ; un champ absent garde sa valeur de <see cref="Default"/>.</summary>
    public static PathNetworkConfig Read(JsonConfigReader reader, JsonElement section)
    {
        reader.AllowOnly(section, "paths", "enabled", "neighbour_factor", "extra_edge_chance", "extra_edge_factor",
            "spawn_links", "noise_amplitude", "noise_frequency", "costs", "wobble_px", "edge_margin", "max_expansions",
            "default_style");
        PathNetworkConfig config = Default;
        if (JsonConfigReader.Has(section, "enabled"))
            config.Enabled = reader.Flag(section, "enabled");
        if (JsonConfigReader.Has(section, "neighbour_factor"))
            config.NeighbourFactor = reader.Positive(section, "neighbour_factor");
        if (JsonConfigReader.Has(section, "extra_edge_chance"))
            config.ExtraEdgeChance = reader.Chance(section, "extra_edge_chance");
        if (JsonConfigReader.Has(section, "extra_edge_factor"))
            config.ExtraEdgeFactor = reader.Positive(section, "extra_edge_factor");
        if (JsonConfigReader.Has(section, "spawn_links"))
            config.SpawnLinks = reader.Integer(section, "spawn_links", 0, 16);
        if (JsonConfigReader.Has(section, "noise_amplitude"))
            config.NoiseAmplitude = reader.NonNegative(section, "noise_amplitude");
        if (JsonConfigReader.Has(section, "noise_frequency"))
            config.NoiseFrequency = reader.Positive(section, "noise_frequency");
        if (JsonConfigReader.Has(section, "costs"))
        {
            JsonElement costs = reader.Section(section, "costs");
            reader.AllowOnly(costs, "paths.costs", "road", "existing_path", "field_lane", "forest", "sidewalk", "plaza");
            if (JsonConfigReader.Has(costs, "road"))
                config.RoadCost = reader.Positive(costs, "road");
            if (JsonConfigReader.Has(costs, "existing_path"))
                config.ExistingPathCost = reader.Positive(costs, "existing_path");
            if (JsonConfigReader.Has(costs, "field_lane"))
                config.FieldLaneCost = reader.Positive(costs, "field_lane");
            if (JsonConfigReader.Has(costs, "forest"))
                config.ForestCost = reader.Positive(costs, "forest");
            if (JsonConfigReader.Has(costs, "sidewalk"))
                config.SidewalkCost = reader.Positive(costs, "sidewalk");
            if (JsonConfigReader.Has(costs, "plaza"))
                config.PlazaCost = reader.Positive(costs, "plaza");
        }
        if (JsonConfigReader.Has(section, "wobble_px"))
            config.WobblePx = reader.NonNegative(section, "wobble_px");
        if (JsonConfigReader.Has(section, "edge_margin"))
            config.EdgeMargin = reader.Integer(section, "edge_margin", 0, 4000);
        if (JsonConfigReader.Has(section, "max_expansions"))
            config.MaxExpansions = reader.Integer(section, "max_expansions", 1, 10_000_000);
        if (JsonConfigReader.Has(section, "default_style"))
            config.DefaultStyle = PathStyle.Read(reader, reader.Section(section, "default_style"), "paths.default_style");
        return config;
    }
}
