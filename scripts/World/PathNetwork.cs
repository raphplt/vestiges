using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>Chemins de terre de la run (plan 10 T3) : tracés à dessiner et cellules qu'ils recouvrent.</summary>
public sealed class PathNetwork
{
    public readonly List<PathStroke> Strokes = new();

    /// <summary>Cellules recouvertes par un chemin : aucun décor n'y est posé.</summary>
    public readonly HashSet<Vector2I> Cells = new();
}
