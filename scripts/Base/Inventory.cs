using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Base;

/// <summary>
/// Inventaire de ressources du joueur.
/// Capacité limitée : chaque unité de ressource prend un slot.
/// </summary>
public partial class Inventory : Node
{
    public const int MaxCapacity = 50;

    private readonly Dictionary<string, int> _resources = new();
    private EventBus _eventBus;

    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (int amount in _resources.Values)
                total += amount;
            return total;
        }
    }

    public int RemainingSpace => MaxCapacity - TotalCount;
    public bool IsFull => TotalCount >= MaxCapacity;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    /// <summary>Ajoute des ressources. Retourne la quantité réellement ajoutée (capée par la capacité).</summary>
    public int Add(string resourceId, int amount)
    {
        int actualAmount = Mathf.Min(amount, RemainingSpace);
        if (actualAmount <= 0)
            return 0;

        if (!_resources.ContainsKey(resourceId))
            _resources[resourceId] = 0;

        _resources[resourceId] += actualAmount;

        _eventBus.EmitSignal(EventBus.SignalName.ResourceCollected, resourceId, actualAmount);
        _eventBus.EmitSignal(EventBus.SignalName.InventoryChanged, resourceId, _resources[resourceId]);

        return actualAmount;
    }

    public bool Remove(string resourceId, int amount)
    {
        if (!Has(resourceId, amount))
            return false;

        _resources[resourceId] -= amount;

        _eventBus.EmitSignal(EventBus.SignalName.InventoryChanged, resourceId, _resources[resourceId]);
        return true;
    }

    public bool Has(string resourceId, int amount)
    {
        return _resources.ContainsKey(resourceId) && _resources[resourceId] >= amount;
    }

    public int GetAmount(string resourceId)
    {
        return _resources.TryGetValue(resourceId, out int amount) ? amount : 0;
    }

    public Dictionary<string, int> GetAll()
    {
        return new Dictionary<string, int>(_resources);
    }
}
