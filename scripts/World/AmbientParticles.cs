using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Particules ambiantes qui suivent le joueur et changent avec les phases de run V2.
/// Exploration : poussière dorée flottante (mémoire qui persiste).
/// Crise / late game : brume violette (effacement qui avance).
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
		_eventBus.RunPhaseChanged += OnRunPhaseChanged;

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
			_eventBus.RunPhaseChanged -= OnRunPhaseChanged;
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

	private void OnRunPhaseChanged(string oldPhase, string newPhase)
	{
		if (_disabled)
			return;

		switch (newPhase)
		{
			case "Exploration":
				TransitionTo(day: true, duration: 3f);
				break;
			case "Crisis":
			case "LateGame":
			case "Death":
				TransitionTo(day: false, duration: 2f);
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

	// Matériaux partagés et gardés : leurs shaders sont compilés au chargement (ShaderWarmup) et le restent. La brume,
	// transparente jusqu'à la première Résurgence, n'était sinon compilée qu'à ce moment-là (≈ 50 ms, plan 29).
	private static ParticleProcessMaterial _dayMaterial;
	private static ParticleProcessMaterial _nightMaterial;

	/// <summary>Poussière dorée de l'exploration.</summary>
	internal static GpuParticles2D CreateDayParticles(bool reduced)
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

		_dayMaterial ??= new ParticleProcessMaterial
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
			Color = new Color(0.83f, 0.66f, 0.26f, 0.4f),
		};
		particles.ProcessMaterial = _dayMaterial;

		return particles;
	}

	/// <summary>Brume violette des Résurgences et du late game, transparente au départ.</summary>
	internal static GpuParticles2D CreateNightParticles(bool reduced)
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

		if (_nightMaterial == null)
		{
			Gradient gradient = new();
			gradient.SetColor(0, new Color(0.29f, 0.19f, 0.4f, 0f));
			gradient.AddPoint(0.3f, new Color(0.29f, 0.19f, 0.4f, 0.35f));
			gradient.SetColor(gradient.GetPointCount() - 1, new Color(0.29f, 0.19f, 0.4f, 0f));
			_nightMaterial = new ParticleProcessMaterial
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
				ColorRamp = new GradientTexture1D { Gradient = gradient },
			};
		}
		particles.ProcessMaterial = _nightMaterial;

		return particles;
	}
}
