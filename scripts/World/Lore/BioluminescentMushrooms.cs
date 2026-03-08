using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Clusters de champignons luisants bleu-vert sous les arbres.
/// La nuit, mini PointLight2D. Marcher dedans = spores lumineuses 10s.
/// Lore : les champignons décomposent le vide et le reconvertissent en lumière.
/// </summary>
public partial class BioluminescentMushrooms : Node2D
{
	private EventBus _eventBus;
	private bool _sporesApplied;
	private readonly Polygon2D[] _caps = new Polygon2D[5];
	private PointLight2D _nightLight;

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.DayPhaseChanged += OnDayPhaseChanged;
		BuildVisual();
		CreateDetectArea();
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
			_eventBus.DayPhaseChanged -= OnDayPhaseChanged;
	}

	private void BuildVisual()
	{
		int count = (int)GD.RandRange(3, 6);
		Color[] glowColors =
		{
			new(0.2f, 0.85f, 0.7f, 0.7f),
			new(0.15f, 0.7f, 0.85f, 0.7f),
			new(0.3f, 0.9f, 0.6f, 0.7f)
		};

		for (int i = 0; i < count; i++)
		{
			float x = (float)GD.RandRange(-12, 12);
			float y = (float)GD.RandRange(-8, 8);
			float capSize = (float)GD.RandRange(3f, 6f);
			Color capColor = glowColors[GD.Randi() % glowColors.Length];

			// Tige
			Polygon2D stem = new()
			{
				Color = new Color(0.5f, 0.55f, 0.4f, 0.7f),
				Polygon = new Vector2[]
				{
					new(x - 1, y), new(x + 1, y),
					new(x + 1, y - capSize * 1.5f), new(x - 1, y - capSize * 1.5f)
				}
			};
			AddChild(stem);

			// Chapeau (demi-cercle)
			Vector2[] capPoly = CreateHalfCircle(capSize, 8);
			Vector2[] offsetCap = new Vector2[capPoly.Length];
			for (int j = 0; j < capPoly.Length; j++)
				offsetCap[j] = capPoly[j] + new Vector2(x, y - capSize * 1.5f);

			Polygon2D cap = new()
			{
				Color = capColor,
				Polygon = offsetCap
			};
			AddChild(cap);

			if (i < _caps.Length)
				_caps[i] = cap;
		}

		// Lueur subtile ambiante (toujours visible mais plus forte la nuit)
		Polygon2D ambientGlow = new()
		{
			Color = new Color(0.2f, 0.8f, 0.6f, 0.08f),
			Polygon = CreateCircle(20f, 10)
		};
		AddChild(ambientGlow);

		// Pulsation de la lueur
		Tween pulse = CreateTween().SetLoops();
		pulse.TweenProperty(ambientGlow, "modulate:a", 0.25f, 2.5f)
			.SetTrans(Tween.TransitionType.Sine);
		pulse.TweenProperty(ambientGlow, "modulate:a", 0.08f, 2.5f)
			.SetTrans(Tween.TransitionType.Sine);

		// PointLight2D pour la nuit (désactivé de jour)
		_nightLight = new PointLight2D
		{
			Color = new Color(0.2f, 0.85f, 0.65f),
			Energy = 0f,
			TextureScale = 0.3f,
			Texture = GD.Load<Texture2D>("res://icon.svg")
		};
		AddChild(_nightLight);
	}

	private void OnDayPhaseChanged(string phase)
	{
		float targetEnergy = phase == "Night" ? 0.6f : 0f;
		Tween tween = CreateTween();
		tween.TweenProperty(_nightLight, "energy", targetEnergy, 2f)
			.SetTrans(Tween.TransitionType.Sine);
	}

	private void CreateDetectArea()
	{
		Area2D area = new() { Name = "SporeArea" };
		area.CollisionLayer = 0;
		area.CollisionMask = 1;
		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = 25f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);

		area.BodyEntered += OnPlayerEntered;
	}

	private void OnPlayerEntered(Node2D body)
	{
		if (_sporesApplied || body is not Player player)
			return;

		_sporesApplied = true;

		// Attacher des particules de spores au joueur pendant 10s
		GpuParticles2D spores = new() { Name = "SporeGlow" };
		ParticleProcessMaterial mat = new();
		mat.Direction = new Vector3(0, -1, 0);
		mat.Spread = 180f;
		mat.InitialVelocityMin = 5f;
		mat.InitialVelocityMax = 15f;
		mat.Gravity = new Vector3(0, -5f, 0);
		mat.ScaleMin = 0.5f;
		mat.ScaleMax = 1.5f;
		mat.Color = new Color(0.3f, 0.9f, 0.7f, 0.5f);
		spores.ProcessMaterial = mat;
		spores.Amount = 10;
		spores.Lifetime = 1.5f;
		spores.VisibilityRect = new Rect2(-50, -50, 100, 100);
		spores.ZIndex = 80;
		player.AddChild(spores);

		// PointLight2D temporaire sur le joueur (éclairage nocturne)
		PointLight2D playerGlow = new()
		{
			Name = "SporeLight",
			Color = new Color(0.2f, 0.85f, 0.65f),
			Energy = 0.4f,
			TextureScale = 0.4f,
			Texture = GD.Load<Texture2D>("res://icon.svg")
		};
		player.AddChild(playerGlow);

		GD.Print("[BioluminescentMushrooms] Spores lumineuses appliquées au joueur !");

		// Retirer après 10s
		GetTree().CreateTimer(10f).Timeout += () =>
		{
			if (IsInstanceValid(spores))
				spores.QueueFree();
			if (IsInstanceValid(playerGlow))
				playerGlow.QueueFree();
		};

		// Cooldown : réactivable après 30s
		GetTree().CreateTimer(30f).Timeout += () => _sporesApplied = false;
	}

	private static Vector2[] CreateCircle(float radius, int segments)
	{
		Vector2[] pts = new Vector2[segments];
		for (int i = 0; i < segments; i++)
		{
			float a = Mathf.Tau * i / segments;
			pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
		}
		return pts;
	}

	private static Vector2[] CreateHalfCircle(float radius, int segments)
	{
		Vector2[] pts = new Vector2[segments + 2];
		pts[0] = new Vector2(-radius, 0);
		for (int i = 0; i <= segments; i++)
		{
			float a = Mathf.Pi * i / segments;
			pts[i + 1] = new Vector2(-Mathf.Cos(a) * radius, -Mathf.Sin(a) * radius);
		}
		return pts;
	}
}
