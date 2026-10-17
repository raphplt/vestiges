using System.Collections.Generic;
using Godot;

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

    public static LandsConfig Parse(Godot.Collections.Dictionary section)
    {
        Dictionary<string, float> scales = new();
        if (section.ContainsKey("count_scales"))
        {
            foreach (KeyValuePair<Variant, Variant> entry in section["count_scales"].AsGodotDictionary())
                scales[entry.Key.AsString()] = Mathf.Max(0f, (float)entry.Value.AsDouble());
        }
        bool enabled = section.GetValueOrDefault("enabled", false).AsBool();
#if TOOLS
        // Runs de dev : --lands allume les terres, --no-lands rend la grande carte en mosaïque (mesures, comparaisons).
        string[] args = OS.GetCmdlineUserArgs();
        enabled = (enabled || System.Array.IndexOf(args, "--lands") >= 0) && System.Array.IndexOf(args, "--no-lands") < 0;
#endif
        return new LandsConfig
        {
            Enabled = enabled,
            MapRadius = (int)section["map_radius"].AsDouble(),
            MapRadiusY = (int)section["map_radius_y"].AsDouble(),
            CountScales = scales,
        };
    }
}
