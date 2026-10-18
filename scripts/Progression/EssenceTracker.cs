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
    private float _multiplier = 1f;

    public int CurrentEssence => _currentEssence;

    public override void _Ready()
    {
        EnemyDataLoader.Load();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.LootReceived += OnLootReceived;
        _eventBus.EssenceMultiplierChanged += OnMultiplierChanged;
        _eventBus.StreakChanged += OnStreakChanged;
        EmitChanged();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EnemyKilled -= OnEnemyKilled;
            _eventBus.LootReceived -= OnLootReceived;
            _eventBus.EssenceMultiplierChanged -= OnMultiplierChanged;
            _eventBus.StreakChanged -= OnStreakChanged;
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
        // Selon la créature (data/enemies, DECISIONS §184) : éliminations et Essence ne sont plus deux compteurs égaux.
        int amount = EnemyDataLoader.Get(enemyId)?.Stats.Essence ?? 1;
        if (RunRandom.Loot.Randf() < ErasureEffectAt(position).EssenceChance)
            amount++;
        // Accalmie après une crise : l'Essence des morts rapporte davantage (arrondi au supérieur).
        amount = Mathf.CeilToInt(amount * _multiplier);
        AddEssence(amount);
        _eventBus.EmitSignal(EventBus.SignalName.EssenceGained, amount, position);
    }

    /// <summary>
    /// Une Série encaissée (plan 52 §15) : son Essence est publiée sans lieu dans le monde, car la gerbe de grains part
    /// du bloc de la Série au HUD.
    /// </summary>
    private void OnStreakChanged(StreakChange change)
    {
        if (change.Kind != StreakChangeKind.CashOut || change.Essence <= 0)
            return;
        AddEssence(change.Essence);
        _eventBus.EmitSignal(EventBus.SignalName.EssenceGained, change.Essence, Vector2.Zero);
    }

    private void OnMultiplierChanged(float multiplier, float seconds) => _multiplier = Mathf.Max(1f, multiplier);

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
