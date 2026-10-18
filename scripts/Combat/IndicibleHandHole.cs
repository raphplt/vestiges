using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Trou au pied d'une main de l'Indicible (plan 07 B5b) : terre retournée autour d'un fond noir, ou cercle de remous si
/// la main sort de l'eau. Décalque au sol en pixels, au bord rond et rugueux (plan 25 S12) ; sa présence varie par l'opacité du
/// nœud, sans redessin.
/// </summary>
public partial class IndicibleHandHole : Node2D
{
	private const int Radius = 16;
	private static readonly Color Dirt = new(0.27f, 0.21f, 0.16f);
	private static readonly Color DirtLight = new(0.4f, 0.32f, 0.24f);
	private static readonly Color Pit = new(0.05f, 0.04f, 0.05f);
	private static readonly Color Ripple = new(0.55f, 0.68f, 0.8f, 0.7f);
	private static readonly Color DeepWater = new(0.03f, 0.06f, 0.12f, 0.85f);

	private bool _water;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = -1;
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
		int variant = (int)(GetInstanceId() % PixelBlob.Variants);
		PixelBlob.Shape outer = PixelBlob.Get(Radius, variant, 1.4f);
		if (_water)
		{
			PixelBlob.Shape wake = PixelBlob.Get(Mathf.RoundToInt(Radius * 1.5f), variant, 1f);
			foreach (Vector2I pixel in wake.Rim)
				if (PixelBlob.Bayer(pixel) < 0.5f)
					Fill(pixel, Ripple with { A = 0.35f });
			for (int i = 0; i < outer.Inside.Length; i++)
				Fill(outer.Inside[i], outer.Depth[i] < 0.8f ? DeepWater : Ripple);
			foreach (Vector2I pixel in outer.Rim)
				Fill(pixel, Ripple);
			return;
		}
		// Terre retournée : mottes claires sur le bord éclairé (haut-gauche), fond noir au centre.
		for (int i = 0; i < outer.Inside.Length; i++)
		{
			Vector2I pixel = outer.Inside[i];
			bool lit = pixel.X + pixel.Y * 2 < 0;
			Color color = outer.Depth[i] < 0.62f ? Pit
				: lit && PixelBlob.Bayer(pixel) < 0.4f ? DirtLight : Dirt;
			Fill(pixel, color);
		}
		foreach (Vector2I pixel in outer.Rim)
			Fill(pixel, PixelBlob.Bayer(pixel) < 0.5f ? Dirt : DirtLight);
	}

	private void Fill(Vector2I pixel, Color color) => DrawRect(new Rect2(pixel, Vector2.One), color);
}
