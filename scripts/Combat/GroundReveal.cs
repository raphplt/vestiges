using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Sortie de terre d'un sprite dont l'origine est son point au sol (<c>Centered = false</c>, décalage = −pivot) : seule
/// la part sortie est dessinée, par la région du sprite, son bas au ras du sol. Ni shader ni nœud de découpe.
/// Partagé par la levée de la Barrière et les mains de l'Indicible.
/// </summary>
public static class GroundReveal
{
	/// <summary>
	/// <paramref name="sunk"/> 1 = sous terre, 0 = debout ; négatif = au-dessus de sa hauteur (un dépassement).
	/// <paramref name="size"/> et <paramref name="pivot"/> sont ceux de la texture du sprite.
	/// </summary>
	public static void Show(Sprite2D sprite, Vector2 size, Vector2 pivot, float sunk)
	{
		float hidden = Mathf.Clamp(sunk, 0f, 1f) * size.Y;
		float lift = Mathf.Min(0f, sunk) * size.Y;
		sprite.RegionEnabled = true;
		sprite.RegionRect = new Rect2(0f, 0f, size.X, size.Y - hidden);
		sprite.Offset = new Vector2(-pivot.X, -pivot.Y + hidden + lift);
		sprite.Visible = hidden < size.Y - 0.5f;
	}

	/// <summary>Sortie avec dépassement : 0 → 1 en passant un peu au-dessus de 1 (<paramref name="overshoot"/>).</summary>
	public static float EaseOutBack(float progress, float overshoot)
	{
		float p = Mathf.Clamp(progress, 0f, 1f) - 1f;
		float k = overshoot * 10f;
		return 1f + (k + 1f) * p * p * p + k * p * p;
	}
}
