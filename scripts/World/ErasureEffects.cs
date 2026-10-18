using System.Collections.Generic;

namespace Vestiges.World;

/// <summary>
/// Ce que l'oubli coûte et offre selon la phase d'une zone (plan 16 O4–O5). Coût pour le joueur qui s'y tient :
/// vitesse, dégâts et voile d'écran, adoucis par rapport à la Stratégie V2 §8. Gain pour qui y combat : chance
/// d'Essence en plus à chaque créature abattue (le bonus de score est parti avec le score, DECISIONS §155). Réglages : zone_effects dans data/scaling/erasure.json.
/// </summary>
public static class ErasureEffects
{
    public readonly record struct Effect(float Speed, float Damage, float Veil, float EssenceChance)
    {
        public static readonly Effect None = new(1f, 1f, 0f, 0f);
    }

    private static readonly Dictionary<ErasureManager.ErasureZonePhase, Effect> ByPhase = new();
    private static bool _loaded;

    public static Effect For(ErasureManager.ErasureZonePhase phase)
    {
        Load();
        return ByPhase.TryGetValue(phase, out Effect effect) ? effect : Effect.None;
    }

    /// <summary>Effets contrôlés avec tout le fichier par <see cref="ErasureConfig"/> (plan 26 Q7d-2).</summary>
    private static void Load()
    {
        if (_loaded)
            return;
        foreach ((ErasureManager.ErasureZonePhase phase, Effect effect) in ErasureConfig.Load().ZoneEffects)
            ByPhase[phase] = effect;
        _loaded = true;
    }
}
