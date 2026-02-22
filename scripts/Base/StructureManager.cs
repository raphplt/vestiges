using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Base;

/// <summary>
/// Gère toutes les structures placées sur la grille.
/// Registre centralisé pour les requêtes de position.
/// Applique les caps de structures par type.
/// </summary>
public partial class StructureManager : Node
{
    public const int MaxWalls = 20;
    public const int MaxTraps = 8;
    public const int MaxTurrets = 3;
    public const int MaxLights = 6;

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

    public int CountByType<T>() where T : Structure
    {
        int count = 0;
        foreach (Structure s in _structures.Values)
        {
            if (s is T && IsInstanceValid(s) && !s.IsDestroyed)
                count++;
        }
        return count;
    }

    public bool CanPlaceType(string type)
    {
        return type switch
        {
            "wall" => CountByType<Wall>() - CountByType<Torch>() < MaxWalls,
            "trap" => CountByType<Trap>() < MaxTraps,
            "turret" => CountByType<Turret>() < MaxTurrets,
            "light" => CountByType<Torch>() < MaxLights,
            _ => true
        };
    }

    public int GetMaxForType(string type)
    {
        return type switch
        {
            "wall" => MaxWalls,
            "trap" => MaxTraps,
            "turret" => MaxTurrets,
            "light" => MaxLights,
            _ => 99
        };
    }

    public int GetCountForType(string type)
    {
        return type switch
        {
            "wall" => CountByType<Wall>() - CountByType<Torch>(),
            "trap" => CountByType<Trap>(),
            "turret" => CountByType<Turret>(),
            "light" => CountByType<Torch>(),
            _ => 0
        };
    }

    private void OnStructureDestroyed(string _structureId, Vector2 position)
    {
        List<Vector2I> toRemove = new();
        foreach (KeyValuePair<Vector2I, Structure> kvp in _structures)
        {
            if (!IsInstanceValid(kvp.Value) || kvp.Value.IsDestroyed)
                toRemove.Add(kvp.Key);
        }

        foreach (Vector2I key in toRemove)
            _structures.Remove(key);
    }
}
