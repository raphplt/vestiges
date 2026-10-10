using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// La nuit de l'Indicible (plan 07 B5b), sans effet sur le combat : l'obscurité qui tombe sur le monde, la pluie, les
/// éclairs lointains qui ouvrent le ciel un instant, et l'éclair qui frappe. L'obscurité et ses éclats sont tenus ici
/// image par image (une couleur posée par image) plutôt que par des tweens concurrents.
/// Le <c>CanvasModulate</c> est cherché chez le parent du boss (la scène de run), pas dans la scène courante : en
/// mesure, la scène de run est l'enfant d'une scène d'observation.
/// </summary>
public partial class IndicibleStorm : Node
{
	// La pluie arrive un peu après la nuit, et s'en va avec le jour.
	private const float RainFadeSec = 3f;
	private const float CrackleInterval = 0.12f;
	// Un éclair d'arrivée ouvre la nuit : le boss entre par la lumière.
	private const float ArrivalFlashDelay = 0.25f;

	private IndicibleConfig _config;
	private CanvasModulate _night;
	private Color _dayColor;
	private CanvasLayer _rainLayer;
	private IndicibleRain _rain;
	private IndicibleBolt _bolt;
	private RandomNumberGenerator _rng;
	private float _darkness;
	private float _darknessTarget;
	private float _flash;
	private float _flashPeak;
	private float _skyTimer;
	private float _arrivalTimer = -1f;
	private float _crackleTimer;
	private bool _ending;

	public void Setup(IndicibleConfig config, Node scene)
	{
		_config = config;
		// Flux à part : la météo ne décale aucun tirage du jeu.
		_rng = RunRandom.Create("indicible_storm");
		_night = scene.GetNodeOrNull<CanvasModulate>("CanvasModulate");
		_dayColor = _night?.Color ?? Colors.White;
		_rainLayer = new CanvasLayer { Name = "RainLayer", Layer = 1 };
		AddChild(_rainLayer);
		_rain = new IndicibleRain { Name = "Rain" };
		_rainLayer.AddChild(_rain);
		_rain.Setup(config);
		_bolt = new IndicibleBolt { Name = "Bolt" };
		// L'éclair est dans le monde, trié avec la scène, pas sur la couche de la pluie.
		scene.AddChild(_bolt);
		_skyTimer = _rng.RandfRange(config.SkyFlashMinSec, config.SkyFlashMaxSec);
	}

	/// <summary>La nuit tombe, un éclair l'ouvre, la pluie arrive.</summary>
	public void Begin()
	{
		_darknessTarget = 1f;
		_arrivalTimer = ArrivalFlashDelay;
		_rain.SetCoverage(_config.RainByPhase[0], RainFadeSec);
	}

	/// <summary>Part de pluie de la phase <paramref name="phase"/>, vent qui l'incline.</summary>
	public void SetPhase(int phase) => _rain.SetCoverage(_config.RainByPhase[Mathf.Clamp(phase, 0, _config.RainByPhase.Count - 1)], RainFadeSec);

	public void SetWind(Vector2 wind) => _rain.SetWind(wind);

	/// <summary>Le jour revient (boss vaincu) : la nuit se lève, la pluie cesse.</summary>
	public void End()
	{
		_ending = true;
		_darknessTarget = 0f;
		_rain.SetCoverage(0f, _config.DarkFadeSec);
	}

	/// <summary>L'éclair frappe <paramref name="ground"/> : trait, ciel ouvert, étincelles.</summary>
	public void Strike(Vector2 ground)
	{
		_bolt.Strike(ground, _config.BoltHeight);
		Flash(_config.StrikeFlashStrength);
		CombatPools.Instance?.EmitSparks(ground, new SparkBurst
		{
			Family = FxFamily.Pale, Owner = FxOwner.World, Count = 16, Direction = Vector2.Up, Spread = 2.6f,
			SpeedMin = 40f, SpeedMax = 150f, LifeMin = 0.2f, LifeMax = 0.45f, Ballistic = true, Size = 1, Decorative = true,
		});
	}

	/// <summary>Pendant l'annonce d'un éclair, l'air grésille dans le cercle.</summary>
	public void Charge(Vector2 center, float radius, float delta)
	{
		_crackleTimer -= delta;
		if (_crackleTimer > 0f)
			return;
		_crackleTimer = CrackleInterval;
		Vector2 point = center + Iso.ToScreen(Vector2.FromAngle(_rng.Randf() * Mathf.Tau) * radius * _rng.Randf());
		CombatPools.Instance?.EmitSparks(point, new SparkBurst
		{
			Family = FxFamily.Pale, Owner = FxOwner.World, Count = 2, Direction = Vector2.Up, Spread = 0.8f,
			SpeedMin = 30f, SpeedMax = 70f, LifeMin = 0.1f, LifeMax = 0.2f, Ballistic = false, Size = 1, Decorative = true,
		});
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_darkness = Mathf.MoveToward(_darkness, _darknessTarget, dt / _config.DarkFadeSec);
		if (_arrivalTimer > 0f)
		{
			_arrivalTimer -= dt;
			if (_arrivalTimer <= 0f)
				Flash(1f);
		}
		if (!_ending)
		{
			_skyTimer -= dt;
			if (_skyTimer <= 0f)
			{
				_skyTimer = _rng.RandfRange(_config.SkyFlashMinSec, _config.SkyFlashMaxSec);
				Flash(_config.SkyFlashStrength);
			}
		}
		_flash = Mathf.MoveToward(_flash, 0f, dt * _flashPeak / _config.FlashFadeSec);
		if (_night != null && IsInstanceValid(_night))
		{
			Color dark = _dayColor * _config.DarkColor;
			_night.Color = _dayColor.Lerp(dark, _darkness).Lerp(_dayColor, _flash);
		}
		if (_ending && _darkness <= 0f && _flash <= 0f && _rain.IsDry)
			SetProcess(false);
	}

	/// <summary>Retiré sans être vaincu (fin de run, mesure) : le jour revient d'un coup.</summary>
	public override void _ExitTree()
	{
		if (_night != null && IsInstanceValid(_night))
			_night.Color = _dayColor;
		if (_bolt != null && IsInstanceValid(_bolt))
			_bolt.QueueFree();
	}

	private void Flash(float strength)
	{
		// Un éclat plus fort recouvre le plus faible ; un faible n'éteint pas un fort en cours.
		if (strength < _flash)
			return;
		_flash = strength;
		_flashPeak = strength;
	}
}
