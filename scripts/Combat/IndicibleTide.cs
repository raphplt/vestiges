using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Marée de l'Indicible (plan 07 B3b) : l'eau monte depuis un bord de l'écran, par bandes annoncées, ancrée dans le
/// monde. Le front avance vers le joueur puis le dépasse ; l'eau se retire et revient d'un autre bord. Peu profonde, elle
/// ralentit ; profonde, elle ralentit plus et blesse chaque seconde. Elle punit qui reste et qui recule sans regarder :
/// il faut garder du sec en se déplaçant de côté, sans fuir les mains qui en sortent.
/// </summary>
public partial class IndicibleTide : Node2D
{
	private const float Extent = 3200f;
	private const float RecedeSec = 0.6f;
	private const float SlowRefreshSec = 0.15f;
	private const float WarningPulseHz = 4f;

	private enum TideState { Idle, Rising, Receding }

	private static readonly Vector2[] Directions = { Vector2.Up, Vector2.Right, Vector2.Down, Vector2.Left };

	private IndicibleConfig _config;
	private Polygon2D _shallow;
	private Polygon2D _deep;
	private Polygon2D _warning;
	private readonly Vector2[] _quad = new Vector2[4];
	private TideState _state = TideState.Idle;
	private Vector2 _anchor;
	private Vector2 _direction;
	private int _directionIndex;
	private float _front;
	private float _timer;
	private float _deepTimer;

	/// <summary>Temps passé dans l'eau profonde et coups reçus de l'eau (mesure).</summary>
	public float DeepSeconds { get; private set; }
	public float ShallowSeconds { get; private set; }
	public int DeepHits { get; private set; }
	public bool IsActive => _state != TideState.Idle;

	public void Setup(IndicibleConfig config)
	{
		_config = config;
		// Posée au sol, sous les corps et au-dessus des routes, comme l'overlay d'Effacement.
		ZAsRelative = false;
		ZIndex = -5;
		_deep = MakeLayer(config.TideDeepColor, config.TideOpacity);
		_shallow = MakeLayer(config.TideShallowColor, config.TideOpacity * 0.85f);
		_warning = MakeLayer(config.TideShallowColor.Lightened(0.35f), config.TideOpacity * 0.5f);
		_directionIndex = RunRandom.Behavior.RandiRange(0, Directions.Length - 1);
	}

	/// <summary>Première montée : la marée commence au bord indiqué par le joueur.</summary>
	public void Begin(Vector2 playerPosition)
	{
		StartRise(playerPosition);
	}

	/// <summary>La marée cesse (phase suivante, mort du boss) : l'eau se retire.</summary>
	public void Stop()
	{
		_state = TideState.Idle;
		Visible = false;
	}

	/// <summary>Un tick : avance le front et ralentit le joueur dans l'eau ; vrai quand l'eau profonde le blesse.</summary>
	public bool Tick(Player player, float delta)
	{
		if (_state == TideState.Idle)
			return false;
		_timer -= delta;
		if (_state == TideState.Receding)
		{
			Modulate = Colors.White with { A = Mathf.Clamp(_timer / RecedeSec, 0f, 1f) };
			if (_timer <= -_config.TideTurnPauseSec)
			{
				_directionIndex = (_directionIndex + RunRandom.Behavior.RandiRange(1, Directions.Length - 1)) % Directions.Length;
				StartRise(player.GlobalPosition);
			}
			return false;
		}

		bool warning = _timer <= _config.TideWarningSec;
		_warning.Visible = warning;
		if (warning)
			_warning.Modulate = Colors.White with { A = 0.55f + 0.45f * Mathf.Sin(_timer * Mathf.Tau * WarningPulseHz) };
		if (_timer <= 0f)
			Advance();

		float depth = (player.GlobalPosition - _anchor).Dot(_direction) - _front;
		if (depth < 0f)
		{
			_deepTimer = 0f;
			return false;
		}
		bool deep = depth >= _config.TideShallowWidth;
		player.ApplySlow(deep ? _config.TideDeepSlow : _config.TideShallowSlow, SlowRefreshSec);
		if (!deep)
		{
			ShallowSeconds += delta;
			_deepTimer = 0f;
			return false;
		}
		DeepSeconds += delta;
		_deepTimer -= delta;
		if (_deepTimer > 0f)
			return false;
		_deepTimer = 1f;
		return true;
	}

	/// <summary>Le point est sous l'eau (peu profonde comprise).</summary>
	public bool IsUnderWater(Vector2 point) => _state == TideState.Rising && (point - _anchor).Dot(_direction) >= _front;

	/// <summary>Un point d'eau à <paramref name="min"/>–<paramref name="max"/> px de <paramref name="near"/>, si la marée en offre un.</summary>
	public bool TryWaterPoint(Vector2 near, float min, float max, out Vector2 point)
	{
		point = near;
		if (_state != TideState.Rising)
			return false;
		for (int attempt = 0; attempt < 6; attempt++)
		{
			float angle = _direction.Angle() + RunRandom.Behavior.RandfRange(-1.1f, 1.1f);
			Vector2 candidate = near + Iso.ToScreen(Vector2.FromAngle(angle) * RunRandom.Behavior.RandfRange(min, max));
			if (IsUnderWater(candidate))
			{
				point = candidate;
				return true;
			}
		}
		return false;
	}

	private void StartRise(Vector2 playerPosition)
	{
		_state = TideState.Rising;
		_direction = Directions[_directionIndex];
		_anchor = playerPosition;
		_front = _config.TideStartDistance;
		_timer = _config.TideBandSec;
		Modulate = Colors.White;
		Visible = true;
		_warning.Visible = false;
		Redraw();
	}

	private void Advance()
	{
		_front -= _config.TideBandStep;
		_warning.Visible = false;
		if (_front < _config.TideEndDistance)
		{
			_state = TideState.Receding;
			_timer = RecedeSec;
			return;
		}
		_timer = _config.TideBandSec;
		Redraw();
	}

	private void Redraw()
	{
		SetBand(_warning, _front - _config.TideBandStep, _front);
		SetBand(_shallow, _front, _front + _config.TideShallowWidth);
		SetBand(_deep, _front + _config.TideShallowWidth, _front + Extent);
	}

	/// <summary>Bande d'eau entre deux distances au point d'ancrage, le long du sens de la marée, très large de côté.</summary>
	private void SetBand(Polygon2D layer, float from, float to)
	{
		Vector2 side = _direction.Orthogonal() * Extent;
		Vector2 near = _anchor + _direction * from;
		Vector2 far = _anchor + _direction * to;
		_quad[0] = near - side;
		_quad[1] = near + side;
		_quad[2] = far + side;
		_quad[3] = far - side;
		layer.Polygon = _quad;
	}

	private Polygon2D MakeLayer(Color color, float opacity)
	{
		Polygon2D layer = new() { Color = color with { A = opacity } };
		AddChild(layer);
		return layer;
	}
}
