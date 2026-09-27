using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Péril de la run (plan 17 lot 3A) : chaque point renforce les créatures et majore XP, score et rareté des tirages
/// (valeurs dans data/scaling/peril.json). Seul émetteur de <c>DifficultyModifierChanged</c>. Tient aussi les Oublis
/// (lot 3C), malus pris aux Failles jusqu'à ce qu'un Mémorial les lève.
/// </summary>
public partial class PerilManager : Node
{
    private EventBus _eventBus;
    private readonly List<ActiveOubli> _oublis = new();

    public int Peril { get; private set; }
    public IReadOnlyList<ActiveOubli> Oublis => _oublis;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    public void AddPeril(int amount)
    {
        int peril = Mathf.Clamp(Peril + amount, 0, PerilDataLoader.Max);
        if (peril == Peril)
            return;

        Peril = peril;
        _eventBus.EmitSignal(EventBus.SignalName.DifficultyModifierChanged,
            PerilDataLoader.EnemyCountMultiplier(peril), PerilDataLoader.EnemyHpMultiplier(peril),
            PerilDataLoader.EnemyDamageMultiplier(peril), PerilDataLoader.XpMultiplier(peril));
        _eventBus.EmitSignal(EventBus.SignalName.PerilChanged, peril);
        GD.Print($"[PerilManager] Péril {peril}");
    }

    public void AddOubli(StatEffectData data, Player player)
    {
        StatModifier modifier = StatModifier.Scaled(data.Stat, data.ModifierType, data.Amount, 1f);
        modifier.ApplyTo(player);
        _oublis.Add(new ActiveOubli(data, modifier));
        GD.Print($"[PerilManager] Oubli : {data.Id}");
    }

    public void LiftOubli(ActiveOubli oubli, Player player)
    {
        if (!_oublis.Remove(oubli))
            return;
        // Lever un Oubli rend la stat, jamais des PV : un Oubli de PV max pris à 1 PV (perte bornée) rendrait
        // sinon plus qu'il n'a pris.
        float hp = player.CurrentHp;
        oubli.Modifier.Inverse().ApplyTo(player);
        player.CapCurrentHp(hp);
        GD.Print($"[PerilManager] Oubli levé : {oubli.Data.Id}");
    }
}
