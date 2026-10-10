using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Pluie de la nuit de l'Indicible (plan 07 B5b), dessinée à l'écran sur sa propre couche, au-dessus du monde et sous
/// l'interface : la nuit ne l'assombrit pas, ses couleurs sont donc déjà celles de la nuit. Chaque goutte tombe jusqu'à
/// une hauteur tirée au hasard, où elle éclate en une petite couronne. Un seul tracé pour les gouttes, un pour les
/// éclats : tableaux alloués une fois, gouttes inactives réduites à un point hors de l'écran.
/// </summary>
public partial class IndicibleRain : Node2D
{
	private const float SplashSec = 0.14f;
	private const float SplashSize = 3f;
	// Les gouttes ne vont pas toutes à la même vitesse : la pluie a de la profondeur.
	private const float SpeedSpread = 0.35f;

	private IndicibleConfig _config;
	private Vector2[] _drops;
	private float[] _speeds;
	private float[] _ground;
	private Vector2[] _dropLines;
	private Vector2[] _splashes;
	private float[] _splashTimers;
	private Vector2[] _splashLines;
	private RandomNumberGenerator _rng;
	private Color _splashColor;
	private int _nextSplash;
	private float _coverage;
	private float _targetCoverage;
	private float _fadeSpeed;
	private Vector2 _fall = Vector2.Down;

	/// <summary>Part de la pluie en cours (0 = arrêtée), vers laquelle elle glisse en <paramref name="fadeSec"/>.</summary>
	public void SetCoverage(float coverage, float fadeSec)
	{
		_targetCoverage = Mathf.Clamp(coverage, 0f, 1f);
		_fadeSpeed = 1f / Mathf.Max(0.05f, fadeSec);
		SetProcess(!IsDry);
	}

	public bool IsDry => _coverage <= 0f && _targetCoverage <= 0f;

	/// <summary>Le vent (px/s, au sol) incline la pluie.</summary>
	public void SetWind(Vector2 wind)
	{
		_fall = new Vector2(wind.X * _config.WindSlant, 1f).Normalized();
	}

	public void Setup(IndicibleConfig config)
	{
		_config = config;
		// Flux à part : la pluie ne décale aucun tirage du jeu.
		_rng = RunRandom.Create("indicible_rain");
		int count = config.RainDrops;
		_drops = new Vector2[count];
		_speeds = new float[count];
		_ground = new float[count];
		_dropLines = new Vector2[count * 2];
		int splashes = Mathf.Max(8, count / 5);
		_splashes = new Vector2[splashes];
		_splashTimers = new float[splashes];
		_splashLines = new Vector2[splashes * 4];
		_splashColor = config.RainColor with { A = Mathf.Min(1f, config.RainColor.A * 1.5f) };
		Vector2 size = GetViewportRect().Size;
		for (int i = 0; i < count; i++)
			Respawn(i, size, _rng.RandfRange(0f, size.Y));
		// Rien à faire tant qu'il ne pleut pas.
		SetProcess(false);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_coverage = Mathf.MoveToward(_coverage, _targetCoverage, _fadeSpeed * dt);
		if (IsDry)
		{
			QueueRedraw();
			SetProcess(false);
			return;
		}
		Vector2 size = GetViewportRect().Size;
		int active = Mathf.RoundToInt(_drops.Length * _coverage);
		Vector2 trail = -_fall * _config.RainLength;
		for (int i = 0; i < _drops.Length; i++)
		{
			_drops[i] += _fall * _config.RainSpeed * _speeds[i] * dt;
			if (_drops[i].Y >= _ground[i])
			{
				if (i < active)
					Splash(_drops[i]);
				Respawn(i, size, -_rng.RandfRange(0f, size.Y * 0.2f));
			}
			// Une goutte au-delà de la part en cours n'est pas dessinée : un point hors de l'écran.
			Vector2 head = i < active ? _drops[i] : new Vector2(-10f, -10f);
			_dropLines[i * 2] = head;
			_dropLines[i * 2 + 1] = i < active ? head + trail * _speeds[i] : head;
		}
		for (int i = 0; i < _splashes.Length; i++)
		{
			_splashTimers[i] -= dt;
			float grow = _splashTimers[i] > 0f ? 1f - _splashTimers[i] / SplashSec : -1f;
			Vector2 at = grow >= 0f ? _splashes[i] : new Vector2(-10f, -10f);
			float spread = grow >= 0f ? SplashSize * (0.5f + grow) : 0f;
			_splashLines[i * 4] = at;
			_splashLines[i * 4 + 1] = at + new Vector2(-spread, -spread * 0.6f);
			_splashLines[i * 4 + 2] = at;
			_splashLines[i * 4 + 3] = at + new Vector2(spread, -spread * 0.6f);
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_coverage <= 0f && _targetCoverage <= 0f)
			return;
		DrawMultiline(_dropLines, _config.RainColor);
		DrawMultiline(_splashLines, _splashColor);
	}

	private void Respawn(int index, Vector2 size, float y)
	{
		// Le vent pousse la pluie de côté : elle part d'une bande plus large que l'écran, du côté d'où il souffle.
		float margin = size.Y * Mathf.Abs(_fall.X / Mathf.Max(0.2f, _fall.Y));
		float x = _rng.RandfRange(-margin * Mathf.Max(0f, Mathf.Sign(_fall.X)), size.X + margin * Mathf.Max(0f, -Mathf.Sign(_fall.X)));
		_drops[index] = new Vector2(x, y);
		_speeds[index] = 1f + _rng.RandfRange(-SpeedSpread, SpeedSpread);
		_ground[index] = _rng.RandfRange(size.Y * 0.1f, size.Y);
	}

	private void Splash(Vector2 at)
	{
		_splashes[_nextSplash] = at;
		_splashTimers[_nextSplash] = SplashSec;
		_nextSplash = (_nextSplash + 1) % _splashes.Length;
	}
}
