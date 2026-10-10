using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// À la mort de l'Indicible (plan 07 B5d), trente et une lueurs montent lentement de l'eau et s'éteignent : les noyés
/// de la Montée, enfin regardés. Un nœud par mort de boss, qui se libère seul ; un tracé par image.
/// </summary>
public partial class IndicibleRelease : Node2D
{
	private const int Count = 31;
	private const float LifeSec = 3.6f;
	private const float SpreadX = 90f;
	private const float SpreadY = 40f;
	private static readonly Color Glow = new(1f, 0.86f, 0.55f);

	private readonly Vector2[] _start = new Vector2[Count];
	private readonly float[] _speed = new float[Count];
	private readonly float[] _delay = new float[Count];
	private readonly float[] _phase = new float[Count];
	private float _age;

	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = 25;
		RandomNumberGenerator rng = RunRandom.Create("indicible_release");
		for (int i = 0; i < Count; i++)
		{
			_start[i] = new Vector2(rng.RandfRange(-SpreadX, SpreadX), rng.RandfRange(-SpreadY, SpreadY));
			_speed[i] = rng.RandfRange(18f, 36f);
			_delay[i] = rng.RandfRange(0f, 1.2f);
			_phase[i] = rng.RandfRange(0f, Mathf.Tau);
		}
	}

	public override void _Process(double delta)
	{
		_age += (float)delta;
		if (_age >= LifeSec + 1.2f)
		{
			QueueFree();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		for (int i = 0; i < Count; i++)
		{
			float t = _age - _delay[i];
			if (t <= 0f || t >= LifeSec)
				continue;
			// Elles montent en ondulant, s'allument puis s'éteignent.
			Vector2 at = _start[i] + new Vector2(Mathf.Sin(t * 1.7f + _phase[i]) * 4f, -_speed[i] * t);
			float alpha = Mathf.Min(1f, t * 2f) * Mathf.Clamp((LifeSec - t) / 1.2f, 0f, 1f);
			Vector2 pixel = at.Floor();
			DrawRect(new Rect2(pixel - Vector2.One, new Vector2(4f, 4f)), Glow with { A = alpha * 0.5f });
			DrawRect(new Rect2(pixel, new Vector2(2f, 2f)), Glow with { A = alpha });
		}
	}
}
