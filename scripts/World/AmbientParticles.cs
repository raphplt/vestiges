using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Particules ambiantes qui suivent le joueur et changent avec le cycle jour/nuit.
/// Jour : poussière dorée flottante (mémoire qui persiste).
/// Nuit : brume violette (effacement qui avance).
/// </summary>
public partial class AmbientParticles : Node2D
{
	private GpuParticles2D _dayParticles;
	private GpuParticles2D _nightParticles;
	private EventBus _eventBus;
	private Node2D _followTarget;
	private bool _disabled;

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.DayPhaseChanged += OnDayPhaseChanged;

		_disabled = VfxFactory.CurrentParticleLevel == ParticleLevel.Off;
		if (_disabled)
			return;

		bool reduced = VfxFactory.CurrentParticleLevel == ParticleLevel.Reduced;
		_dayParticles = CreateDayParticles(reduced);
		_nightParticles = CreateNightParticles(reduced);
		AddChild(_dayParticles);
		AddChild(_nightParticles);

		// Jour par défaut
		_dayParticles.Emitting = true;
		_nightParticles.Emitting = false;
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
			_eventBus.DayPhaseChanged -= OnDayPhaseChanged;
	}

	public override void _Process(double delta)
	{
		if (_followTarget == null || !IsInstanceValid(_followTarget))
		{
			_followTarget = GetTree().GetFirstNodeInGroup("player") as Node2D;
			if (_followTarget == null)
				return;
		}

		GlobalPosition = _followTarget.GlobalPosition;
	}

	private void OnDayPhaseChanged(string phase)
	{
		if (_disabled)
			return;

		switch (phase)
		{
			case "Day":
				TransitionTo(day: true, duration: 3f);
				break;
			case "Dusk":
				// Mélange : jour diminue, nuit monte
				FadeParticles(_dayParticles, 0.3f, 2f);
				_nightParticles.Emitting = true;
				FadeParticles(_nightParticles, 0.5f, 2f);
				break;
			case "Night":
				TransitionTo(day: false, duration: 2f);
				break;
			case "Dawn":
				TransitionTo(day: true, duration: 4f);
				break;
		}
	}

	private void TransitionTo(bool day, float duration)
	{
		_dayParticles.Emitting = day;
		_nightParticles.Emitting = !day;

		FadeParticles(_dayParticles, day ? 1f : 0f, duration);
		FadeParticles(_nightParticles, day ? 0f : 1f, duration);
	}

	private void FadeParticles(GpuParticles2D particles, float targetAlpha, float duration)
	{
		Tween tween = CreateTween();
		tween.TweenProperty(particles, "modulate:a", targetAlpha, duration)
			.SetTrans(Tween.TransitionType.Sine);
	}

	private static GpuParticles2D CreateDayParticles(bool reduced)
	{
		var particles = new GpuParticles2D
		{
			Amount = reduced ? 6 : 12,
			Lifetime = 3f,
			SpeedScale = 0.5f,
			Explosiveness = 0f,
			Texture = VfxFactory.CircleTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(200, 120, 0),
			Direction = new Vector3(0.3f, -0.5f, 0),
			Spread = 60f,
			InitialVelocityMin = 3f,
			InitialVelocityMax = 8f,
			Gravity = new Vector3(0, -2, 0),
			ScaleMin = 0.2f,
			ScaleMax = 0.5f,
			Color = new Color(0.83f, 0.66f, 0.26f, 0.4f), // Or Foyer atténué
		};
		particles.ProcessMaterial = mat;

		return particles;
	}

	private static GpuParticles2D CreateNightParticles(bool reduced)
	{
		var particles = new GpuParticles2D
		{
			Amount = reduced ? 8 : 16,
			Lifetime = 4f,
			SpeedScale = 0.3f,
			Explosiveness = 0f,
			Modulate = new Color(1, 1, 1, 0),
			Texture = VfxFactory.CircleTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var gradient = new GradientTexture1D();
		var g = new Gradient();
		g.SetColor(0, new Color(0.29f, 0.19f, 0.4f, 0f));    // Violet brume fade in
		g.AddPoint(0.3f, new Color(0.29f, 0.19f, 0.4f, 0.35f));
		g.SetColor(g.GetPointCount() - 1, new Color(0.29f, 0.19f, 0.4f, 0f)); // Fade out
		gradient.Gradient = g;

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(220, 140, 0),
			Direction = new Vector3(-0.2f, 0.1f, 0),
			Spread = 90f,
			InitialVelocityMin = 2f,
			InitialVelocityMax = 6f,
			Gravity = Vector3.Zero,
			ScaleMin = 0.8f,
			ScaleMax = 1.8f,
			ColorRamp = gradient,
		};
		particles.ProcessMaterial = mat;

		return particles;
	}
}
