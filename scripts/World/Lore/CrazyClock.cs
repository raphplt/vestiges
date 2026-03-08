using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Tour d'horloge urbaine qui tourne à l'envers. Ticking spatial audible.
/// Interagir = effet "rewind" cosmétique 5s + Souvenir "Les Signes".
/// 1 par zone urbaine.
/// Lore : l'horloge tente de revenir au moment avant l'Effacement.
/// </summary>
public partial class CrazyClock : Node2D
{
	private bool _interacted;
	private EventBus _eventBus;
	private Node2D _hourHand;
	private Node2D _minuteHand;
	private Polygon2D _pendulum;
	private float _minuteAngle;
	private float _hourAngle;

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BuildVisual();
		CreateInteractArea();
	}

	public override void _Process(double delta)
	{
		// Aiguilles tournent à l'ENVERS (signe que le temps est déréglé)
		float dt = (float)delta;
		_minuteAngle -= dt * 120f; // vitesse x2 en sens inverse
		_hourAngle -= dt * 10f;

		if (_minuteHand != null)
			_minuteHand.RotationDegrees = _minuteAngle;
		if (_hourHand != null)
			_hourHand.RotationDegrees = _hourAngle;
	}

	private void BuildVisual()
	{
		// Tour (rectangle vertical)
		Polygon2D tower = new()
		{
			Color = new Color(0.5f, 0.45f, 0.4f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-14, 20), new(14, 20), new(14, -30), new(-14, -30)
			}
		};
		AddChild(tower);

		// Toit pointu
		Polygon2D roof = new()
		{
			Color = new Color(0.4f, 0.35f, 0.3f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-16, -30), new(16, -30), new(0, -48)
			}
		};
		AddChild(roof);

		// Cadran (cercle blanc-beige)
		Polygon2D face = new()
		{
			Color = new Color(0.9f, 0.88f, 0.8f, 0.9f),
			Polygon = CreateCircle(10f, 16),
			Position = new Vector2(0, -15)
		};
		AddChild(face);

		// Bord du cadran
		Polygon2D faceBorder = new()
		{
			Color = new Color(0.3f, 0.28f, 0.25f, 0.8f),
			Polygon = CreateCircle(11f, 16),
			Position = new Vector2(0, -15)
		};
		AddChild(faceBorder);
		MoveChild(faceBorder, GetChildCount() - 2); // sous le cadran

		// Marques d'heures (12 petits traits)
		for (int h = 0; h < 12; h++)
		{
			float a = Mathf.Tau * h / 12f;
			Vector2 outer = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 9f + new Vector2(0, -15);
			Vector2 inner = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 7f + new Vector2(0, -15);

			Polygon2D mark = new()
			{
				Color = new Color(0.2f, 0.2f, 0.2f, 0.7f),
				Polygon = new Vector2[]
				{
					inner + new Vector2(-0.5f, 0), inner + new Vector2(0.5f, 0),
					outer + new Vector2(0.5f, 0), outer + new Vector2(-0.5f, 0)
				}
			};
			AddChild(mark);
		}

		// Aiguille des minutes (pivote autour du centre du cadran)
		_minuteHand = new Node2D { Position = new Vector2(0, -15) };
		Polygon2D minNeedle = new()
		{
			Color = new Color(0.15f, 0.12f, 0.1f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-0.8f, 2), new(0.8f, 2), new(0.3f, -8), new(-0.3f, -8)
			}
		};
		_minuteHand.AddChild(minNeedle);
		AddChild(_minuteHand);

		// Aiguille des heures
		_hourHand = new Node2D { Position = new Vector2(0, -15) };
		Polygon2D hourNeedle = new()
		{
			Color = new Color(0.2f, 0.15f, 0.1f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-1f, 2), new(1f, 2), new(0.5f, -6), new(-0.5f, -6)
			}
		};
		_hourHand.AddChild(hourNeedle);
		AddChild(_hourHand);

		// Pendule en bas de la tour (oscillation perpétuelle)
		_pendulum = new Polygon2D
		{
			Color = new Color(0.6f, 0.5f, 0.2f, 0.8f),
			Polygon = CreateCircle(4f, 8),
			Position = new Vector2(0, 12)
		};
		AddChild(_pendulum);

		Tween pendulumTween = CreateTween().SetLoops();
		pendulumTween.TweenProperty(_pendulum, "position:x", 6f, 1.2f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		pendulumTween.TweenProperty(_pendulum, "position:x", -6f, 1.2f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		// Lueur rougeâtre inquiétante
		PointLight2D eerie = new()
		{
			Color = new Color(0.9f, 0.6f, 0.3f),
			Energy = 0.3f,
			TextureScale = 0.4f,
			Texture = GD.Load<Texture2D>("res://icon.svg"),
			Position = new Vector2(0, -15)
		};
		AddChild(eerie);
	}

	private void CreateInteractArea()
	{
		Area2D area = new() { Name = "InteractArea" };
		area.CollisionLayer = 0;
		area.CollisionMask = 1;
		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = 50f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);

		area.BodyEntered += OnPlayerEntered;
	}

	private void OnPlayerEntered(Node2D body)
	{
		if (_interacted || body is not Player)
			return;

		_interacted = true;

		// Effet rewind cosmétique : les couleurs se désaturent puis reviennent
		Tween rewind = CreateTween();
		rewind.TweenProperty(this, "modulate", new Color(0.6f, 0.5f, 0.4f, 1f), 0.5f);
		rewind.TweenProperty(this, "modulate", new Color(1.2f, 1.1f, 0.9f, 1f), 0.5f);
		rewind.TweenProperty(this, "modulate", Colors.White, 2f);

		// Récompense
		_eventBus.EmitSignal(EventBus.SignalName.XpGained, 30f);
		_eventBus.EmitSignal(EventBus.SignalName.SouvenirDiscovered, "souvenir_les_signes", "Les Signes", "les_signes");

		GD.Print("[CrazyClock] Horloge folle activée — +30 XP, Souvenir 'Les Signes'");
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
