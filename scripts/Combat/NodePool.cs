using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Réserve d'objets de combat recyclés : un nœud créé une fois sert toute la run
/// au lieu d'être instancié puis libéré à chaque coup (plan 02 J0).
/// </summary>
public sealed class NodePool<T> where T : Node
{
    private readonly Func<T> _factory;
    private readonly Node _parent;
    private readonly Stack<T> _free = new();

    public int Created { get; private set; }

    public NodePool(Node parent, Func<T> factory)
    {
        _parent = parent;
        _factory = factory;
    }

    public T Take()
    {
        if (_free.Count > 0)
            return _free.Pop();
        T node = _factory();
        _parent.AddChild(node);
        Created++;
        return node;
    }

    public void Return(T node)
    {
        _free.Push(node);
    }
}
