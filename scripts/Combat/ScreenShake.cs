using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Système de screen shake centralisé, attaché à la Camera2D du joueur.
/// Supporte plusieurs intensités simultanées (additive), décroissance exponentielle,
/// et trauma-based shake (plus le trauma est fort, plus c'est violent).
/// </summary>
public partial class ScreenShake : Node
{
	private Camera2D _camera;
	private float _trauma;
	private float _maxOffset = 6f;
	private float _decay = 3f;
	private float _frequency = 30f;
	private float _time;

	// Hitstop
	private float _hitstopTimer;
	private float _savedTimeScale;
	private bool _hitstopActive;

	public static ScreenShake Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
		if (_hitstopActive)
			RestoreTimeScale();
	}

	public void SetCamera(Camera2D camera) => _camera = camera;

	/// <summary>Ajoute du trauma (0-1). Le shake est proportionnel au carré du trauma.</summary>
	public void AddTrauma(float amount)
	{
		_trauma = Mathf.Min(_trauma + amount, 1f);
	}

	/// <summary>Shake léger (petit hit).</summary>
	public void ShakeLight() => AddTrauma(0.15f);

	/// <summary>Shake moyen (hit normal, kill).</summary>
	public void ShakeMedium() => AddTrauma(0.3f);

	/// <summary>Shake fort (crit, explosion, mort de mini-boss).</summary>
	public void ShakeHeavy() => AddTrauma(0.5f);

	/// <summary>Hitstop : gèle le jeu pendant quelques frames pour accentuer un impact.</summary>
	public void Hitstop(float duration = 0.04f)
	{
		if (_hitstopActive)
			return;

		_hitstopActive = true;
		_savedTimeScale = (float)Engine.TimeScale;
		Engine.TimeScale = 0.05;
		_hitstopTimer = duration;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// Hitstop
		if (_hitstopActive)
		{
			_hitstopTimer -= dt;
			if (_hitstopTimer <= 0f)
				RestoreTimeScale();
		}

		if (_camera == null || _trauma <= 0f)
			return;

		_time += dt * _frequency;
		_trauma = Mathf.Max(_trauma - _decay * dt, 0f);

		// Shake intensity = trauma² (quadratique = plus naturel)
		float shake = _trauma * _trauma;
		float offsetX = _maxOffset * shake * Noise(_time, 0f);
		float offsetY = _maxOffset * shake * Noise(0f, _time);

		_camera.Offset = new Vector2(offsetX, offsetY);

		if (_trauma <= 0.001f)
		{
			_trauma = 0f;
			_camera.Offset = Vector2.Zero;
		}
	}

	private void RestoreTimeScale()
	{
		Engine.TimeScale = _savedTimeScale > 0.1 ? _savedTimeScale : 1.0;
		_hitstopActive = false;
	}

	/// <summary>Bruit pseudo-aléatoire déterministe basé sur sin (pas besoin de FastNoiseLite).</summary>
	private static float Noise(float x, float y)
	{
		return Mathf.Sin(x * 1.3f + y * 2.7f) * Mathf.Cos(x * 0.7f + y * 1.1f);
	}
}
