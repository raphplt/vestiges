using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Base;

/// <summary>
/// Inventaire de ressources du joueur.
/// Stocke les quantités par type de ressource.
/// </summary>
public partial class Inventory : Node
{
    private readonly Dictionary<string, int> _resources = new();
    private EventBus _eventBus;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    public void Add(string resourceId, int amount)
    {
        if (!_resources.ContainsKey(resourceId))
            _resources[resourceId] = 0;

        _resources[resourceId] += amount;

        _eventBus.EmitSignal(EventBus.SignalName.ResourceCollected, resourceId, amount);
        _eventBus.EmitSignal(EventBus.SignalName.InventoryChanged, resourceId, _resources[resourceId]);
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
