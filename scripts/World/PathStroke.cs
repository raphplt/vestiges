using Godot;

namespace Vestiges.World;

/// <summary>Un tronçon de chemin : points en pixels locaux du sol, largeur au sol et style (rgb : ton, a : ornières) par point.</summary>
public sealed class PathStroke
{
    public Vector2[] Points;
    public float[] Widths;
    public Color[] Styles;
}
