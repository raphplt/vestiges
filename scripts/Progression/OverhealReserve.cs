using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Prévoyance (plan 05 §3.4) : réserve de PV remplie à l'acquisition, puis par les seuls excédents de soin et de
/// régénération ; elle rend au plus la perte d'un coup non fatal. Sa capacité suit les PV max sans se remplir.
/// </summary>
public sealed class OverhealReserve
{
    private readonly Player _player;
    private readonly float _capacityFraction;
    private float _stock;

    public OverhealReserve(Player player, PerkSpecializationData perk)
    {
        _player = player;
        _capacityFraction = SpecializationRuntime.Parameter(perk, "max_health_fraction");
        _stock = Capacity * SpecializationRuntime.Parameter(perk, "initial_fill_fraction");
    }

    public float Capacity => _player.EffectiveMaxHp * _capacityFraction;
    public float Stock => _stock;

    /// <summary>Des PV max en baisse tronquent la réserve pour de bon ; une hausse ne la remplit pas.</summary>
    public bool Truncate()
    {
        float capacity = Capacity;
        if (_stock <= capacity)
            return false;
        _stock = capacity;
        return true;
    }

    public bool Store(float excess)
    {
        Truncate();
        float before = _stock;
        _stock = Mathf.Min(_stock + Mathf.Max(0f, excess), Capacity);
        return _stock > before;
    }

    /// <summary>PV rendus, jamais plus que la perte ni que le stock ; la restitution ne recharge rien.</summary>
    public float Restore(float lost)
    {
        Truncate();
        float amount = Mathf.Min(lost, _stock);
        if (amount <= 0f)
            return 0f;
        float restored = _player.Heal(amount, HealingKind.PerkRecovery).HpRestored;
        _stock -= restored;
        return restored;
    }
}
