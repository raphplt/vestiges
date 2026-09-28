using System;

namespace Vestiges.Combat;

/// <summary>
/// Budget purement visuel : première touche immédiate puis dix impulsions par seconde d'exposition.
/// Les interruptions gèlent le reste ; le recyclage remet la structure à zéro. Aucun RNG de gameplay.
/// </summary>
public struct ContinuousImpactCadence
{
    private const double Interval = 0.1;
    private double _remaining;

    public bool Advance(float delta)
    {
        bool emit = _remaining <= 1e-6;
        if (emit)
            _remaining += Interval;
        _remaining = Math.Max(0, _remaining - delta);
        return emit;
    }
}
