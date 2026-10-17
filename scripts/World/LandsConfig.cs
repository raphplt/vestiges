using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Réglages des terres successives (plan 51, section <c>lands</c> de data/world/world_gen.json) : taille d'une terre et
/// part des lieux et des coffres gardée par rapport à la grande carte. Allumées par défaut (plan 51 §15) ; éteintes, la
/// run garde la grande carte en mosaïque.
/// </summary>
public sealed class LandsConfig
{
    public static readonly LandsConfig Disabled = new();

    public bool Enabled { get; init; }
    public int MapRadius { get; init; }
    public int MapRadiusY { get; init; }

    /// <summary>Part gardée par sorte de lieu (« chests », « memorial », « rift », « workshop », « peril_stele », « well »).</summary>
    public IReadOnlyDictionary<string, float> CountScales { get; init; } = new Dictionary<string, float>();

    /// <summary>Couronne des Mémoriaux sur une terre, en part du rayon : loin du départ, vers le front.</summary>
    public Vector2 MemorialBand { get; init; } = new(0f, 1f);

    /// <summary>
    /// Section <c>lands</c> lue strictement : taille d'une terre obligatoire, parts gardées positives ou nulles pour les sortes
    /// de <paramref name="siteKinds"/>, couronne des Mémoriaux croissante dans [0 ; 1].
    /// </summary>
    public static LandsConfig Read(JsonConfigReader reader, JsonElement section, IReadOnlyCollection<string> siteKinds)
    {
        reader.AllowOnly(section, "lands", "enabled", "map_radius", "map_radius_y", "count_scales", "memorial_band");
        bool enabled = reader.Flag(section, "enabled");
        int radius = reader.Integer(section, "map_radius", 8, 4000);
        int radiusY = reader.Integer(section, "map_radius_y", 8, 4000);
        Dictionary<string, float> scales = new();
        if (JsonConfigReader.Has(section, "count_scales"))
        {
            JsonElement table = reader.Section(section, "count_scales");
            foreach ((string kind, JsonElement _) in reader.Entries(table, "lands.count_scales"))
            {
                if (!JsonConfigReader.Contains(siteKinds, kind))
                    reader.Fail($"lands.count_scales : sorte de lieu « {kind} » inconnue ({string.Join(", ", siteKinds)})");
                scales[kind] = reader.NonNegative(table, kind);
            }
        }
        Vector2 band = new(0f, 1f);
        if (JsonConfigReader.Has(section, "memorial_band"))
        {
            (float min, float max) = reader.Range(section, "memorial_band", 0f, 1f);
            band = new Vector2(min, max);
        }
#if TOOLS
        // Runs de dev : --lands allume les terres, --no-lands rend la grande carte en mosaïque (mesures, comparaisons).
        string[] args = OS.GetCmdlineUserArgs();
        enabled = (enabled || System.Array.IndexOf(args, "--lands") >= 0) && System.Array.IndexOf(args, "--no-lands") < 0;
#endif
        return new LandsConfig
        {
            Enabled = enabled,
            MapRadius = radius,
            MapRadiusY = radiusY,
            CountScales = scales,
            MemorialBand = band,
        };
    }
}
