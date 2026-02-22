using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Base;

/// <summary>
/// Gère toutes les structures placées sur la grille.
/// Registre centralisé pour les requêtes de position.
/// </summary>
public partial class StructureManager : Node
{
    private readonly Dictionary<Vector2I, Structure> _structures = new();
    private EventBus _eventBus;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.StructureDestroyed += OnStructureDestroyed;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.StructureDestroyed -= OnStructureDestroyed;
    }

    public void Register(Vector2I gridPos, Structure structure)
    {
        _structures[gridPos] = structure;
    }

    public void Unregister(Vector2I gridPos)
    {
        _structures.Remove(gridPos);
    }

    public bool IsOccupied(Vector2I gridPos)
    {
        return _structures.ContainsKey(gridPos);
    }

    public Structure GetStructureAt(Vector2I gridPos)
    {
        return _structures.TryGetValue(gridPos, out Structure s) ? s : null;
    }

    public int Count => _structures.Count;

    private void OnStructureDestroyed(string _structureId, Vector2 position)
    {
        Vector2I toRemove = new(int.MinValue, int.MinValue);
        foreach (KeyValuePair<Vector2I, Structure> kvp in _structures)
        {
            if (!IsInstanceValid(kvp.Value) || kvp.Value.IsDestroyed)
            {
                toRemove = kvp.Key;
                break;
            }
        }

        if (toRemove.X != int.MinValue)
            _structures.Remove(toRemove);
    }
}
