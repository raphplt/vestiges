using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Péril de la run (plan 17 lot 3A) : chaque point renforce les créatures et majore XP, score et rareté des tirages
/// (valeurs dans data/scaling/peril.json). Seul émetteur de <c>DifficultyModifierChanged</c>.
/// </summary>
public partial class PerilManager : Node
{
    private EventBus _eventBus;

    public int Peril { get; private set; }

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
}
