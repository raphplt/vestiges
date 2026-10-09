using Godot;

namespace Vestiges.Core;

/// <summary>
/// Ombre au sol d'une entité nombreuse, dessinée par <see cref="GroundShadowLayer"/> plutôt que par un nœud à elle.
/// L'entité tient à jour sa largeur, son décalage, son échelle et son opacité ; la couche lit sa position à chaque image.
/// </summary>
public sealed class ShadowCaster
{
	public ShadowCaster(Node2D owner)
	{
		Owner = owner;
	}

	public Node2D Owner { get; }

	/// <summary>Largeur de la texture d'ombre (<see cref="GroundShadow.SnapWidth"/>).</summary>
	public int Width { get; set; } = GroundShadow.SnapWidth(24f);

	/// <summary>Centre de l'ombre dans le repère de l'entité, avant son échelle.</summary>
	public Vector2 Offset { get; set; }

	public float Scale { get; set; } = 1f;

	public float Alpha { get; set; } = 1f;

	/// <summary>Rang dans la couche, −1 hors de la couche ; tenu par la couche.</summary>
	internal int Slot { get; set; } = -1;
}
