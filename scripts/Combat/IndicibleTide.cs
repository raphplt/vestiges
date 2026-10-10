using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Marée de l'Indicible (plan 07 B3b) : l'eau monte depuis un bord de l'écran, par bandes annoncées, ancrée dans le
/// monde. Le front avance vers le joueur puis le dépasse ; l'eau se retire et revient d'un autre bord. Peu profonde, elle
/// ralentit ; profonde, elle ralentit plus et blesse chaque seconde. Elle punit qui reste et qui recule sans regarder :
/// il faut garder du sec en se déplaçant de côté, sans fuir les mains qui en sortent.
/// Rendu (B5c) : une seule surface d'eau dessinée par un shader (bord qui lèche la côte, écume, vaguelettes, remous
/// autour du joueur, bande annoncée qui luit). Le front visible rattrape en une montée le front du jeu, qui avance par
/// bandes ; à la retraite, l'eau recule en s'effaçant.
/// </summary>
public partial class IndicibleTide : Node2D
{
	private const float Extent = 3200f;
	private const float RecedeSec = 0.6f;
	private const float SlowRefreshSec = 0.15f;
	private const float WarningPulseHz = 4f;
	// Montée d'une bande à l'écran, et entrée de l'eau depuis le bord de l'écran.
	private const float SurgeSec = 0.3f;
	private const float EntryDistance = 260f;
	private const float RetreatDistance = 140f;
	// Le bord ondule de part et d'autre du front : la surface en déborde un peu.
	private const float EdgeMargin = 40f;
	private static readonly StringName AnchorParam = "anchor";
	private static readonly StringName DirectionParam = "direction";
	private static readonly StringName FrontParam = "front";
	private static readonly StringName WarningParam = "warning";
	private static readonly StringName FadeParam = "fade";
	private static readonly StringName WaderParam = "wader";
	private static readonly StringName WaderWetParam = "wader_wet";

	private enum TideState { Idle, Rising, Receding }

	private static readonly Vector2[] Directions = { Vector2.Up, Vector2.Right, Vector2.Down, Vector2.Left };

	private IndicibleConfig _config;
	private Polygon2D _water;
	private ShaderMaterial _material;
	private readonly Vector2[] _quad = new Vector2[4];
	private float _visualFront;
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
		_material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/indicible_tide.gdshader") };
		_material.SetShaderParameter("shallow_width", config.TideShallowWidth);
		_material.SetShaderParameter("band_step", config.TideBandStep);
		_material.SetShaderParameter("shallow_color", config.TideShallowColor);
		_material.SetShaderParameter("deep_color", config.TideDeepColor);
		_material.SetShaderParameter("foam_color", config.TideFoamColor);
		_material.SetShaderParameter("opacity", config.TideOpacity);
		_water = new Polygon2D { Name = "Water", Material = _material };
		AddChild(_water);
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
		_visualFront = Mathf.MoveToward(_visualFront, _front, _config.TideBandStep / SurgeSec * delta
			+ (_state == TideState.Rising ? Mathf.Abs(_visualFront - _front) * delta * 2f : 0f));
		_material.SetShaderParameter(FrontParam, _visualFront);
		_material.SetShaderParameter(WaderParam, player.GlobalPosition);
		if (_state == TideState.Receding)
		{
			_material.SetShaderParameter(FadeParam, Mathf.Clamp(_timer / RecedeSec, 0f, 1f));
			_material.SetShaderParameter(WarningParam, 0f);
			_material.SetShaderParameter(WaderWetParam, 0f);
			if (_timer <= -_config.TideTurnPauseSec)
			{
				_directionIndex = (_directionIndex + RunRandom.Behavior.RandiRange(1, Directions.Length - 1)) % Directions.Length;
				StartRise(player.GlobalPosition);
			}
			return false;
		}

		bool warning = _timer <= _config.TideWarningSec;
		_material.SetShaderParameter(WarningParam, warning ? 0.65f + 0.35f * Mathf.Sin(_timer * Mathf.Tau * WarningPulseHz) : 0f);
		if (_timer <= 0f)
			Advance();

		float depth = (player.GlobalPosition - _anchor).Dot(_direction) - _front;
		_material.SetShaderParameter(WaderWetParam, depth >= 0f ? 1f : 0f);
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
		// L'eau entre depuis le bord de l'écran, puis rattrape son front.
		_visualFront = _front + EntryDistance;
		_timer = _config.TideBandSec;
		Visible = true;
		_material.SetShaderParameter(AnchorParam, _anchor);
		_material.SetShaderParameter(DirectionParam, _direction);
		_material.SetShaderParameter(FrontParam, _visualFront);
		_material.SetShaderParameter(FadeParam, 1f);
		_material.SetShaderParameter(WarningParam, 0f);
		Redraw();
	}

	private void Advance()
	{
		_front -= _config.TideBandStep;
		if (_front < _config.TideEndDistance)
		{
			_state = TideState.Receding;
			_timer = RecedeSec;
			// Le front visible recule pendant que l'eau s'efface : la mer se retire.
			_front += RetreatDistance;
			return;
		}
		_timer = _config.TideBandSec;
		Redraw();
	}

	/// <summary>
	/// Surface d'eau, de la bande annoncée jusqu'au large, très large de côté : le shader y dessine l'eau au-delà du
	/// front, la bande qui luit en deçà, rien ailleurs.
	/// </summary>
	private void Redraw()
	{
		Vector2 side = _direction.Orthogonal() * Extent;
		Vector2 near = _anchor + _direction * (_front - _config.TideBandStep - EdgeMargin);
		Vector2 far = _anchor + _direction * (_front + Extent);
		_quad[0] = near - side;
		_quad[1] = near + side;
		_quad[2] = far + side;
		_quad[3] = far - side;
		_water.Polygon = _quad;
	}
}
