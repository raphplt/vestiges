using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Cadre de porte debout seul, sans murs. Ouvrir = souffle d'air chaud +
/// buff "Chaleur" (-10% dégâts reçus la nuit, 60s). Souvenir "L'Avant".
/// Lore : la porte se souvient d'être un seuil. L'ouvrir libère le dernier
/// écho de chaleur d'un foyer qui n'existe plus.
/// </summary>
public partial class DoorWithoutWall : Node2D
{
	private bool _opened;
	private EventBus _eventBus;
	private Node2D _doorPanel;
	private float _doorAngle;

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BuildVisual();
		CreateInteractArea();
	}

	private void BuildVisual()
	{
		// Montant gauche
		Polygon2D leftFrame = new()
		{
			Color = new Color(0.45f, 0.35f, 0.25f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-14, 12), new(-10, 12), new(-10, -28), new(-14, -28)
			}
		};
		AddChild(leftFrame);

		// Montant droit
		Polygon2D rightFrame = new()
		{
			Color = new Color(0.45f, 0.35f, 0.25f, 0.9f),
			Polygon = new Vector2[]
			{
				new(10, 12), new(14, 12), new(14, -28), new(10, -28)
			}
		};
		AddChild(rightFrame);

		// Linteau (traverse supérieure)
		Polygon2D lintel = new()
		{
			Color = new Color(0.42f, 0.32f, 0.22f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-16, -28), new(16, -28), new(16, -32), new(-16, -32)
			}
		};
		AddChild(lintel);

		// Seuil usé
		Polygon2D threshold = new()
		{
			Color = new Color(0.5f, 0.45f, 0.38f, 0.6f),
			Polygon = new Vector2[]
			{
				new(-16, 12), new(16, 12), new(16, 15), new(-16, 15)
			}
		};
		AddChild(threshold);

		// Panneau de porte (pivote quand ouvert)
		_doorPanel = new Node2D { Position = new Vector2(-10, 0) };
		Polygon2D door = new()
		{
			Color = new Color(0.5f, 0.38f, 0.25f, 0.85f),
			Polygon = new Vector2[]
			{
				new(0, 10), new(20, 10), new(20, -26), new(0, -26)
			}
		};
		_doorPanel.AddChild(door);

		// Poignée
		Polygon2D handle = new()
		{
			Color = new Color(0.65f, 0.55f, 0.3f, 0.8f),
			Polygon = new Vector2[]
			{
				new(16, -6), new(18, -6), new(18, -4), new(16, -4)
			}
		};
		_doorPanel.AddChild(handle);

		AddChild(_doorPanel);

		// Lueur chaude subtile venant du seuil
		Polygon2D warmGlow = new()
		{
			Color = new Color(1f, 0.8f, 0.4f, 0.06f),
			Polygon = CreateCircle(25f, 10),
			Position = new Vector2(0, 5)
		};
		AddChild(warmGlow);

		// Pulsation de chaleur fantôme
		Tween warmPulse = CreateTween().SetLoops();
		warmPulse.TweenProperty(warmGlow, "modulate:a", 0.15f, 3f)
			.SetTrans(Tween.TransitionType.Sine);
		warmPulse.TweenProperty(warmGlow, "modulate:a", 0.05f, 3f)
			.SetTrans(Tween.TransitionType.Sine);
	}

	private void CreateInteractArea()
	{
		Area2D area = new() { Name = "DoorArea" };
		area.CollisionLayer = 0;
		area.CollisionMask = 1;
		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = 35f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);

		area.BodyEntered += OnPlayerEntered;
	}

	private void OnPlayerEntered(Node2D body)
	{
		if (_opened || body is not Player)
			return;

		_opened = true;

		// Animer l'ouverture de la porte (rotation du panneau)
		Tween openTween = CreateTween();
		openTween.TweenProperty(_doorPanel, "rotation_degrees", -70f, 0.8f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);

		// Flash de chaleur : l'air chaud s'échappe
		openTween.TweenCallback(Callable.From(() =>
		{
			// Lueur dorée intense momentanée
			Polygon2D heatBurst = new()
			{
				Color = new Color(1f, 0.85f, 0.5f, 0.6f),
				Polygon = CreateCircle(8f, 12),
				GlobalPosition = GlobalPosition,
				ZIndex = 60
			};
			GetParent().AddChild(heatBurst);

			Tween burst = heatBurst.CreateTween();
			burst.SetParallel();
			burst.TweenProperty(heatBurst, "scale", new Vector2(8f, 8f), 1f)
				.SetTrans(Tween.TransitionType.Expo)
				.SetEase(Tween.EaseType.Out);
			burst.TweenProperty(heatBurst, "modulate:a", 0f, 1f);
			burst.Chain().TweenCallback(Callable.From(() =>
			{
				if (IsInstanceValid(heatBurst))
					heatBurst.QueueFree();
			}));
		}));

		// Récompenses
		_eventBus.EmitSignal(EventBus.SignalName.XpGained, 20f);
		_eventBus.EmitSignal(EventBus.SignalName.PlayerBuffApplied, "warmth", 60f);
		_eventBus.EmitSignal(EventBus.SignalName.SouvenirDiscovered, "souvenir_avant", "L'Avant", "l_avant");

		GD.Print("[DoorWithoutWall] Porte ouverte — souffle de chaleur, buff Chaleur 60s, Souvenir 'L'Avant'");
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
}
