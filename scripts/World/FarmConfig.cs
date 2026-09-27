using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Campagne des Champs Sauvages (data/world/farms.json, plan 08 P4b) : plan des fermes, bords de parcelles,
/// vergers et scènes-récits.
/// </summary>
public sealed class FarmConfig
{
    /// <summary>Plan d'une ferme, posé par <see cref="SiteComposer"/>.</summary>
    public SitePlan Site = new();
    /// <summary>Décors des bords de parcelles, le long des allées des champs (plan 08 P4b-3).</summary>
    public string[] ParcelEdges = System.Array.Empty<string>();
    /// <summary>Arbres de verger : paires tronc / canopée.</summary>
    public readonly List<(string Base, string Canopy)> OrchardTrees = new();
    /// <summary>Scènes-récits (pique-nique abandonné, linge étendu…), une par région de champs au plus (plan 08 P4b-4).</summary>
    public string[] Scenes = System.Array.Empty<string>();
    /// <summary>Scènes assez grandes pour arrêter le joueur (tracteur embourbé) ; les autres se traversent.</summary>
    public string[] BlockingScenes = System.Array.Empty<string>();
    public float SceneChancePerRegion = 0.5f;

    public static FarmConfig Load()
    {
        FarmConfig config = new();
        Godot.Collections.Dictionary d = SitePlan.ReadJson("res://data/world/farms.json");
        if (d == null)
            return config;

        config.Site = SitePlan.Read(d, WildFieldsComposer.BiomeId, WildFieldsComposer.Folder, "fermes");
        if (d.ContainsKey("scenes"))
            config.Scenes = d["scenes"].AsStringArray();
        if (d.ContainsKey("blocking_scenes"))
            config.BlockingScenes = d["blocking_scenes"].AsStringArray();
        config.SceneChancePerRegion = (float)d.GetValueOrDefault("scene_chance_per_region", config.SceneChancePerRegion).AsDouble();
        if (d.ContainsKey("parcel_edges"))
            config.ParcelEdges = d["parcel_edges"].AsStringArray();
        if (d.ContainsKey("orchard_trees"))
        {
            foreach (Variant pair in d["orchard_trees"].AsGodotArray())
            {
                string[] stems = pair.AsStringArray();
                if (stems.Length >= 2)
                    config.OrchardTrees.Add((stems[0], stems[1]));
            }
        }
        return config;
    }
}
