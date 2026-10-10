using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Trou au pied d'une main de l'Indicible (plan 07 B5b) : terre retournée autour d'un fond noir, ou cercle de remous si
/// la main sort de l'eau. Décalque au sol ; sa présence varie par l'opacité du nœud, sans redessin.
/// </summary>
public partial class IndicibleHandHole : Node2D
{
	private const float Radius = 13f;
	private static readonly Color Dirt = new(0.27f, 0.21f, 0.16f);
	private static readonly Color Pit = new(0.05f, 0.04f, 0.05f);
	private static readonly Color Ripple = new(0.55f, 0.68f, 0.8f, 0.7f);
	private static readonly Color DeepWater = new(0.03f, 0.06f, 0.12f, 0.85f);

	private bool _water;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = -1;
		// Disque au sol : deux fois plus large que haut à l'écran.
		Scale = new Vector2(1f, 0.5f);
		Visible = false;
	}

	public void Place(Vector2 position, bool water)
	{
		Position = position;
		Visible = true;
		Modulate = Colors.White with { A = 0f };
		if (water != _water)
		{
			_water = water;
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (_water)
		{
			DrawCircle(Vector2.Zero, Radius * 0.8f, DeepWater);
			DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 20, Ripple, 1f);
			DrawArc(Vector2.Zero, Radius * 1.5f, 0f, Mathf.Tau, 24, Ripple with { A = 0.35f }, 1f);
			return;
		}
		DrawCircle(Vector2.Zero, Radius * 1.25f, Dirt);
		DrawCircle(Vector2.Zero, Radius * 0.8f, Pit);
	}
}
