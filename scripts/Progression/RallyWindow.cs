using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Reprise (plan 05 §3.5) : un coup non fatal ouvre une fenêtre ; une part de la perte, sous plafond, redevient
/// récupérable par éliminations. Un nouveau coup ajoute au budget sans repousser l'échéance ; les soins reçus
/// d'ailleurs le réduisent d'autant, pour ne jamais récupérer deux fois la même blessure.
/// </summary>
public sealed class RallyWindow
{
    private readonly Player _player;
    private readonly float _duration;
    private readonly float _recoverableFraction;
    private readonly float _capFraction;
    private readonly float _perKillFraction;
    private float _budget;
    private float _remaining;

    public RallyWindow(Player player, PerkSpecializationData perk)
    {
        _player = player;
        _duration = SpecializationRuntime.Parameter(perk, "duration_seconds");
        _recoverableFraction = SpecializationRuntime.Parameter(perk, "recoverable_damage_fraction");
        _capFraction = SpecializationRuntime.Parameter(perk, "max_health_fraction");
        _perKillFraction = SpecializationRuntime.Parameter(perk, "heal_per_kill_health_fraction");
    }

    public bool IsOpen => _remaining > 0f;
    public float Remaining => _remaining;
    public float Duration => _duration;

    /// <summary>Ce qui peut encore revenir : le budget, borné par les PV manquants.</summary>
    public float Recoverable => IsOpen ? Mathf.Min(_budget, Missing) : 0f;

    private float Missing => Mathf.Max(0f, _player.EffectiveMaxHp - _player.CurrentHp);

    public void Wound(float loss)
    {
        if (loss <= 0f)
            return;
        _budget = Mathf.Min(_budget + loss * _recoverableFraction, _player.EffectiveMaxHp * _capFraction);
        if (!IsOpen)
            _remaining = _duration;
    }

    public bool ExternalHeal(float restored)
    {
        if (!IsOpen || restored <= 0f)
            return false;
        _budget = Mathf.Max(0f, _budget - restored);
        return true;
    }

    /// <summary>Une élimination attribuée rend jusqu'à sa part des PV max, sur le budget et les PV manquants.</summary>
    public bool Kill()
    {
        float amount = Mathf.Min(_player.EffectiveMaxHp * _perKillFraction, Recoverable);
        if (amount <= 0f)
            return false;
        _budget -= _player.Heal(amount, HealingKind.PerkRecovery).HpRestored;
        return true;
    }

    /// <summary>Vrai à l'expiration : le reste du budget est perdu.</summary>
    public bool Advance(float delta)
    {
        if (!IsOpen)
            return false;
        _remaining -= delta;
        if (_remaining > 0f)
            return false;
        _remaining = 0f;
        _budget = 0f;
        return true;
    }
}
