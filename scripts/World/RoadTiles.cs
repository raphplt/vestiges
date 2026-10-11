using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>Tuiles de chaussée d'une carte : le groupe de chaque cellule concernée et les images de chaque groupe.</summary>
public sealed class RoadTiles
{
    /// <summary>Images d'un groupe (ses variantes d'usure), par nom de groupe.</summary>
    public Dictionary<string, Image[]> Groups { get; } = new();

    /// <summary>Nom du groupe à poser sur chaque cellule, chaussée ou trottoir qu'un raccord déborde.</summary>
    public Dictionary<Vector2I, string> Cells { get; } = new();
}
