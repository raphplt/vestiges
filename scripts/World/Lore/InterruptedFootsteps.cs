using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Empreintes de pas qui s'arrêtent net — une personne effacée en plein pas.
/// La dernière empreinte brille doré. Marcher dessus = murmure + 5 XP.
/// </summary>
public partial class InterruptedFootsteps : Node2D
{
	private bool _discovered;
	private EventBus _eventBus;

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BuildVisual();
		CreateInteractArea();
	}

	private void BuildVisual()
	{
		// 4-6 empreintes en ligne, de plus en plus espacées
		int stepCount = (int)GD.RandRange(4, 7);
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));

		for (int i = 0; i < stepCount; i++)
		{
			float spacing = 12f + i * 3f;
			Vector2 pos = dir * spacing * i;
			bool isLast = i == stepCount - 1;

			Polygon2D footprint = new()
			{
				Position = pos,
				Polygon = new Vector2[]
				{
					new(-3, -4), new(3, -4), new(4, 0),
					new(3, 5), new(-3, 5), new(-4, 0)
				},
				Color = isLast
					? new Color(0.85f, 0.75f, 0.3f, 0.9f) // doré
					: new Color(0.4f, 0.35f, 0.3f, 0.5f - i * 0.05f) // brun de plus en plus pâle
			};
			AddChild(footprint);

			// La dernière empreinte pulse
			if (isLast)
			{
				Polygon2D glow = new()
				{
					Position = pos,
					Polygon = CreateCircle(10f, 8),
					Color = new Color(1f, 0.9f, 0.4f, 0.1f)
				};
				AddChild(glow);

				Tween pulse = CreateTween().SetLoops();
				pulse.TweenProperty(glow, "modulate:a", 0.3f, 1.5f)
					.SetTrans(Tween.TransitionType.Sine);
				pulse.TweenProperty(glow, "modulate:a", 0.08f, 1.5f)
					.SetTrans(Tween.TransitionType.Sine);
			}
		}
	}

	private void CreateInteractArea()
	{
		Area2D area = new() { Name = "DetectArea" };
		area.CollisionLayer = 0;
		area.CollisionMask = 1;
		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = 40f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);

		area.BodyEntered += OnPlayerEntered;
	}

	private void OnPlayerEntered(Node2D body)
	{
		if (_discovered || body is not Player)
			return;

		_discovered = true;
		_eventBus.EmitSignal(EventBus.SignalName.XpGained, 5f);

		// Feedback : flash subtil doré
		Modulate = new Color(1f, 0.95f, 0.7f, 1.2f);
		Tween tween = CreateTween();
		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0.4f), 2f);
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
