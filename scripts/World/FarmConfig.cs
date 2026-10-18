using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

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
    public string[] ParcelEdges = Array.Empty<string>();
    /// <summary>Arbres de verger : paires tronc / canopée.</summary>
    public readonly List<(string Base, string Canopy)> OrchardTrees = new();
    /// <summary>Scènes-récits (pique-nique abandonné, linge étendu…), une par région de champs au plus (plan 08 P4b-4).</summary>
    public string[] Scenes = Array.Empty<string>();
    /// <summary>Scènes assez grandes pour arrêter le joueur (tracteur embourbé) ; les autres se traversent.</summary>
    public string[] BlockingScenes = Array.Empty<string>();
    public float SceneChancePerRegion = 0.5f;

    public const string FilePath = "res://data/world/farms.json";

    /// <summary>Réglages valides, ou une exception qui nomme le champ fautif (génération, bancs).</summary>
    public static FarmConfig Load()
    {
        if (!TryLoad(out FarmConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    /// <summary>Fermes lues strictement (plan 26 Q7c-4c) : décors des champs présents, scènes bloquantes parmi les scènes.</summary>
    public static bool TryLoad(out FarmConfig config, out string error)
    {
        config = null;
        if (!SitePlan.ReadText(FilePath, out string text, out error))
            return false;
        if (!TryParse(text, stem => ResourceLoader.Exists($"{WildFieldsComposer.Folder}{stem}.png"), out config, out error))
        {
            error = $"{FilePath} : {error}";
            return false;
        }
        return true;
    }

    public static bool TryParse(string text, Func<string, bool> spriteExists, out FarmConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            List<string> keys = new(SitePlan.CommonKeys)
                { "parcel_edges", "orchard_trees", "scene_chance_per_region", "scenes", "blocking_scenes" };
            reader.AllowOnly(root, "fermes", keys.ToArray());
            FarmConfig parsed = new()
            {
                Site = SitePlan.Read(reader, root, WildFieldsComposer.BiomeId, WildFieldsComposer.Folder, "fermes", spriteExists),
            };
            if (JsonConfigReader.Has(root, "scenes"))
                parsed.Scenes = SitePlan.Sprites(reader, root, "scenes", 0, spriteExists).ToArray();
            if (JsonConfigReader.Has(root, "blocking_scenes"))
            {
                parsed.BlockingScenes = SitePlan.Sprites(reader, root, "blocking_scenes", 0, spriteExists).ToArray();
                foreach (string scene in parsed.BlockingScenes)
                {
                    if (Array.IndexOf(parsed.Scenes, scene) < 0)
                        reader.Fail($"blocking_scenes : « {scene} » absente de scenes");
                }
            }
            if (JsonConfigReader.Has(root, "scene_chance_per_region"))
                parsed.SceneChancePerRegion = reader.Chance(root, "scene_chance_per_region");
            if (JsonConfigReader.Has(root, "parcel_edges"))
                parsed.ParcelEdges = SitePlan.Sprites(reader, root, "parcel_edges", 0, spriteExists).ToArray();
            if (JsonConfigReader.Has(root, "orchard_trees"))
            {
                foreach (JsonElement pair in reader.List(root, "orchard_trees", 0))
                {
                    if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2)
                    {
                        reader.Fail("orchard_trees : paires [tronc, canopée] attendues");
                        continue;
                    }
                    string trunk = pair[0].ValueKind == JsonValueKind.String ? pair[0].GetString() : null;
                    string canopy = pair[1].ValueKind == JsonValueKind.String ? pair[1].GetString() : null;
                    if (string.IsNullOrEmpty(trunk) || !spriteExists(trunk) || string.IsNullOrEmpty(canopy) || !spriteExists(canopy))
                        reader.Fail($"orchard_trees : décors « {trunk} » et « {canopy} » introuvables");
                    else
                        parsed.OrchardTrees.Add((trunk, canopy));
                }
            }
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
