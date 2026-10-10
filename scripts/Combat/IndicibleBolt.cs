using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Éclair qui frappe (plan 07 B5b) : un trait brisé tombé du ciel sur son point, avec une branche, un cœur blanc dans un
/// halo bleu. Il claque deux fois, puis s'efface. Tracé recalculé à chaque frappe dans des tableaux alloués une fois ;
/// son clignotement passe par l'opacité du nœud, sans redessin.
/// </summary>
public partial class IndicibleBolt : Node2D
{
	private const int Segments = 16;
	private const int BranchSegments = 5;
	private const float LifeSec = 0.26f;
	private const float Jag = 34f;
	private static readonly Color Core = new(0.96f, 0.98f, 1f);
	private static readonly Color Halo = new(0.55f, 0.7f, 1f, 0.45f);

	private readonly Vector2[] _points = new Vector2[Segments + 1];
	private readonly Vector2[] _branch = new Vector2[BranchSegments + 1];
	private RandomNumberGenerator _rng;
	private float _timer;

	public override void _Ready()
	{
		ZAsRelative = false;
		// Au-dessus des créatures et de leurs plaques, sous les chiffres de dégâts.
		ZIndex = 25;
		Visible = false;
		_rng = RunRandom.Create("indicible_bolt");
		SetProcess(false);
	}

	/// <summary>L'éclair tombe de <paramref name="height"/> px au-dessus de <paramref name="ground"/>.</summary>
	public void Strike(Vector2 ground, float height)
	{
		Vector2 top = ground + new Vector2(_rng.RandfRange(-height * 0.25f, height * 0.25f), -height);
		for (int i = 0; i <= Segments; i++)
		{
			float t = (float)i / Segments;
			// L'écart se resserre en bas : l'éclair touche exactement son point.
			float jag = i == 0 || i == Segments ? 0f : _rng.RandfRange(-Jag, Jag) * (1f - t * 0.7f);
			_points[i] = top.Lerp(ground, t) + new Vector2(jag, 0f);
		}
		int fork = _rng.RandiRange(2, Segments / 2);
		Vector2 direction = new Vector2(_rng.Randf() < 0.5f ? -1f : 1f, 1.4f).Normalized();
		_branch[0] = _points[fork];
		for (int i = 1; i <= BranchSegments; i++)
			_branch[i] = _branch[i - 1] + direction * height / Segments * 0.7f + new Vector2(_rng.RandfRange(-Jag, Jag) * 0.5f, 0f);
		_timer = LifeSec;
		Visible = true;
		Modulate = Colors.White;
		QueueRedraw();
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		_timer -= (float)delta;
		if (_timer <= 0f)
		{
			Visible = false;
			SetProcess(false);
			return;
		}
		float age = LifeSec - _timer;
		// Deux claquements : allumé, éteint un instant, rallumé, puis il s'efface.
		float alpha = age < 0.05f ? 1f : age < 0.08f ? 0.15f : Mathf.Clamp(_timer / (LifeSec - 0.08f), 0f, 1f);
		Modulate = Colors.White with { A = alpha };
	}

	public override void _Draw()
	{
		// Halo d'impact au sol, puis le trait : halo large, cœur blanc.
		DrawCircle(_points[Segments], 14f, Halo);
		DrawCircle(_points[Segments], 6f, Core with { A = 0.8f });
		DrawPolyline(_points, Halo, 10f);
		DrawPolyline(_branch, Halo, 5f);
		DrawPolyline(_points, Core, 3f);
		DrawPolyline(_branch, Core, 2f);
	}
}
