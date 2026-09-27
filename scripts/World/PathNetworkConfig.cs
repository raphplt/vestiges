using System.Collections.Generic;
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

    public static PathNetworkConfig Parse(Godot.Collections.Dictionary dict)
    {
        PathNetworkConfig d = Default;
        Godot.Collections.Dictionary costs = dict.ContainsKey("costs") ? dict["costs"].AsGodotDictionary() : new Godot.Collections.Dictionary();
        return new PathNetworkConfig
        {
            Enabled = dict.GetValueOrDefault("enabled", d.Enabled).AsBool(),
            NeighbourFactor = (float)dict.GetValueOrDefault("neighbour_factor", d.NeighbourFactor).AsDouble(),
            ExtraEdgeChance = (float)dict.GetValueOrDefault("extra_edge_chance", d.ExtraEdgeChance).AsDouble(),
            ExtraEdgeFactor = (float)dict.GetValueOrDefault("extra_edge_factor", d.ExtraEdgeFactor).AsDouble(),
            SpawnLinks = (int)dict.GetValueOrDefault("spawn_links", d.SpawnLinks).AsDouble(),
            NoiseAmplitude = (float)dict.GetValueOrDefault("noise_amplitude", d.NoiseAmplitude).AsDouble(),
            NoiseFrequency = (float)dict.GetValueOrDefault("noise_frequency", d.NoiseFrequency).AsDouble(),
            RoadCost = (float)costs.GetValueOrDefault("road", d.RoadCost).AsDouble(),
            ExistingPathCost = (float)costs.GetValueOrDefault("existing_path", d.ExistingPathCost).AsDouble(),
            FieldLaneCost = (float)costs.GetValueOrDefault("field_lane", d.FieldLaneCost).AsDouble(),
            ForestCost = (float)costs.GetValueOrDefault("forest", d.ForestCost).AsDouble(),
            SidewalkCost = (float)costs.GetValueOrDefault("sidewalk", d.SidewalkCost).AsDouble(),
            PlazaCost = (float)costs.GetValueOrDefault("plaza", d.PlazaCost).AsDouble(),
            WobblePx = (float)dict.GetValueOrDefault("wobble_px", d.WobblePx).AsDouble(),
            EdgeMargin = (int)dict.GetValueOrDefault("edge_margin", d.EdgeMargin).AsDouble(),
            MaxExpansions = (int)dict.GetValueOrDefault("max_expansions", d.MaxExpansions).AsDouble(),
            DefaultStyle = dict.ContainsKey("default_style")
                ? PathStyle.Parse(dict["default_style"].AsGodotDictionary(), d.DefaultStyle)
                : d.DefaultStyle,
        };
    }
}
