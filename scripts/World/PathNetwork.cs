using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>Chemins de terre de la run (plan 10 T3) : tracés à dessiner et cellules qu'ils recouvrent.</summary>
public sealed class PathNetwork
{
    public readonly List<PathStroke> Strokes = new();

    /// <summary>Cellules recouvertes par un chemin : aucun décor n'y est posé.</summary>
    public readonly HashSet<Vector2I> Cells = new();

    /// <summary>Passerelles sur les chenaux franchis par l'axe d'un chemin (plan 58 T2a).</summary>
    public readonly List<ChannelBridge> Bridges = new();
}

/// <summary>Sens d'une passerelle à l'écran : le long de x, de y, ou d'une diagonale qui descend ou monte vers la droite.</summary>
public enum BridgeHeading
{
    AlongX,
    AlongY,
    Falling,
    Rising,
}

/// <summary>Une passerelle : sa place sur l'axe du chemin, et le sens de la traversée.</summary>
public readonly record struct ChannelBridge(Vector2 Position, BridgeHeading Heading);
