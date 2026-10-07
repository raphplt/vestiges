using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Péril de la run (plan 17 lot 3A, plan 28) : sans plafond, chaque point renforce les créatures et majore le score
/// (valeurs dans data/scaling/peril.json). Seul émetteur de <c>DifficultyModifierChanged</c>. Tient aussi les Oublis
/// (lots 3C et 3D), malus de carte pris aux Failles : <c>OubliEffectChanged</c> publie le total de chaque effet, que le
/// système concerné applique (apparition, Effacement, Résurgences, brouillard, coffres, Mémoriaux).
/// </summary>
public partial class PerilManager : Node
{
    private EventBus _eventBus;
    private readonly List<ActiveOubli> _oublis = new();

    /// <summary>Péril de la run en cours : un objet apparu après un Oubli lit l'effet déjà en vigueur.</summary>
    public static PerilManager Current { get; private set; }

    public int Peril { get; private set; }
    public IReadOnlyList<ActiveOubli> Oublis => _oublis;

    public override void _EnterTree()
    {
        Current = this;
    }

    public override void _ExitTree()
    {
        if (Current == this)
            Current = null;
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    public void AddPeril(int amount)
    {
        int peril = Mathf.Max(Peril + amount, 0);
        if (peril == Peril)
            return;

        Peril = peril;
        _eventBus.EmitSignal(EventBus.SignalName.DifficultyModifierChanged,
            PerilDataLoader.EnemyCountMultiplier(peril), PerilDataLoader.EnemyHpMultiplier(peril),
            PerilDataLoader.EnemyDamageMultiplier(peril));
        _eventBus.EmitSignal(EventBus.SignalName.PerilChanged, peril);
        GD.Print($"[PerilManager] Péril {peril}");
    }

    /// <summary>Prend un Oubli : son effet s'ajoute à ceux du même nom, et chaque système concerné l'applique.</summary>
    public void AddOubli(OubliData data)
    {
        _oublis.Add(new ActiveOubli(data));
        EmitEffect(data.Effect);
        GD.Print($"[PerilManager] Oubli : {data.Id}");
    }

    /// <summary>Lève un Oubli au Mémorial ; un Oubli définitif ne se lève pas.</summary>
    public bool LiftOubli(ActiveOubli oubli)
    {
        if (oubli.Data.Permanent || !_oublis.Remove(oubli))
            return false;
        EmitEffect(oubli.Data.Effect);
        GD.Print($"[PerilManager] Oubli levé : {oubli.Data.Id}");
        return true;
    }

    /// <summary>Somme des Oublis portés pour un effet (0 sans Oubli).</summary>
    public float EffectTotal(string effect)
    {
        float total = 0f;
        foreach (ActiveOubli oubli in _oublis)
            if (oubli.Data.Effect == effect)
                total += oubli.Data.Amount;
        return total;
    }

    private void EmitEffect(string effect)
    {
        _eventBus?.EmitSignal(EventBus.SignalName.OubliEffectChanged, effect, EffectTotal(effect));
    }
}
