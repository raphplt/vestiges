using Godot;
using Vestiges.World;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Source unique de l'Essence de run.
/// Recoit les drops explicites d'essence et les gains passifs lies aux kills.
/// </summary>
public partial class EssenceTracker : Node
{
    private EventBus _eventBus;
    private int _currentEssence;

    public int CurrentEssence => _currentEssence;

    public override void _Ready()
    {
        EnemyDataLoader.Load();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.LootReceived += OnLootReceived;
        EmitChanged();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EnemyKilled -= OnEnemyKilled;
            _eventBus.LootReceived -= OnLootReceived;
        }
    }

    public void AddEssence(int amount)
    {
        if (amount <= 0)
            return;

        _currentEssence += amount;
        EmitChanged();
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
            return true;
        if (_currentEssence < amount)
            return false;

        _currentEssence -= amount;
        EmitChanged();
        return true;
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        EnemyData data = EnemyDataLoader.Get(enemyId);
        int amount = data?.Tier switch
        {
            "boss" => 8,
            "elite" => 4,
            _ => 1
        };
        if (GD.Randf() < ErasureEffectAt(position).EssenceChance)
            amount++;
        AddEssence(amount);
    }

    private ErasureManager _erasureManager;

    /// <summary>Ce que l'oubli offre là où la créature est tombée (plan 16 O5).</summary>
    private ErasureEffects.Effect ErasureEffectAt(Vector2 position)
    {
        if (_erasureManager == null || !IsInstanceValid(_erasureManager))
            _erasureManager = GetTree().CurrentScene?.GetNodeOrNull<ErasureManager>("ErasureManager");
        return _erasureManager == null ? ErasureEffects.Effect.None : ErasureEffects.For(_erasureManager.GetZonePhaseAt(position));
    }

    private void OnLootReceived(string itemType, string itemId, int amount)
    {
        if (amount <= 0)
            return;

        if (itemType == "essence")
            AddEssence(amount);
    }

    private void EmitChanged()
    {
        _eventBus?.EmitSignal(EventBus.SignalName.EssenceChanged, _currentEssence);
    }
}
