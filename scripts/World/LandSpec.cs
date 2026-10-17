using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Une terre à construire (plan 51) : son rang dans la run, sa graine, sa taille, son biome et la part de lieux gardée.
/// Terres éteintes, la terre 0 reprend exactement la grande carte en mosaïque de la run.
/// </summary>
public sealed class LandSpec
{
    public int Index { get; private init; }

    /// <summary>Terre du prototype (terres allumées) ; faux pour la grande carte en mosaïque d'aujourd'hui.</summary>
    public bool IsLand { get; private init; }

    /// <summary>Graine de la carte : celle de la run pour la terre 0, dérivée du rang ensuite.</summary>
    public ulong Seed { get; private init; }
    public int MapRadius { get; private init; }
    public int MapRadiusY { get; private init; }
    public IReadOnlyList<string> BiomeIds { get; private init; }
    public int BiomeCount { get; private init; }
    private IReadOnlyDictionary<string, float> CountScales { get; init; }

    public static LandSpec For(int index, WorldGenConfig config, ulong runSeed)
    {
        LandsConfig lands = config.Lands;
        ulong seed = index == 0 ? runSeed : RunRandom.SeedFor($"land/{index}");
        if (!lands.Enabled)
        {
            return new LandSpec
            {
                Index = index, Seed = seed, MapRadius = config.MapRadius, MapRadiusY = config.MapRadiusY,
                BiomeIds = config.AvailableBiomes, BiomeCount = config.BiomeCount,
                CountScales = new Dictionary<string, float>(),
            };
        }
        return new LandSpec
        {
            Index = index, IsLand = true, Seed = seed, MapRadius = lands.MapRadius, MapRadiusY = lands.MapRadiusY,
            BiomeIds = new[] { BiomeOf(index, config.AvailableBiomes) }, BiomeCount = 1,
            CountScales = lands.CountScales,
        };
    }

    /// <summary>
    /// Biomes tirés sans remise sur la run (un ordre mélangé par la graine de run), puis l'ordre recommence : deux
    /// terres voisines ne se ressemblent jamais tant qu'il reste des biomes à voir.
    /// </summary>
    private static string BiomeOf(int index, IReadOnlyList<string> available)
    {
        List<string> order = new(available);
        RandomNumberGenerator rng = RunRandom.Create("lands/order");
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = rng.RandiRange(0, i);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order[index % order.Count];
    }

    /// <summary>Part gardée d'une sorte de lieu (1 sans réglage, donc terres éteintes).</summary>
    public float CountScale(string kind) => CountScales.TryGetValue(kind, out float scale) ? scale : 1f;

    /// <summary>Applique la taille et les biomes de la terre aux réglages du monde en construction.</summary>
    public void ApplyTo(WorldGenConfig config)
    {
        config.MapRadius = MapRadius;
        config.MapRadiusY = MapRadiusY;
        config.AvailableBiomes = new List<string>(BiomeIds);
        config.BiomeCount = BiomeCount;
    }
}
