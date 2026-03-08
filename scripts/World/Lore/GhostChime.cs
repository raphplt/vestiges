using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Carillons suspendus à des structures effacées, sonnant sans vent.
/// Approcher = sons hostiles ambiants coupés pendant 15s (apaisement).
/// </summary>
public partial class GhostChime : Node2D
{
	private bool _activated;
	private Tween _chimeTween;

	/// <summary>Pitch du carillon (0.8-1.3). Quand plusieurs sont proches, ils harmonisent.</summary>
	public float Pitch { get; set; } = 1f;

	public override void _Ready()
	{
		Pitch = (float)GD.RandRange(0.8f, 1.3f);
		BuildVisual();
		CreateDetectArea();
		StartChiming();
	}

	private void BuildVisual()
	{
		// Support invisible (structure effacée — juste un point d'attache)
		Polygon2D hook = new()
		{
			Color = new Color(0.5f, 0.5f, 0.5f, 0.3f),
			Polygon = new Vector2[]
			{
				new(-3, -25), new(3, -25), new(3, -23), new(-3, -23)
			}
		};
		AddChild(hook);

		// Fils du carillon (3-5 tiges)
		int tubeCount = (int)GD.RandRange(3, 6);
		for (int i = 0; i < tubeCount; i++)
		{
			float x = -8f + (16f / (tubeCount - 1)) * i;
			float length = 12f + (float)GD.RandRange(-3, 5);

			Polygon2D tube = new()
			{
				Color = new Color(0.7f, 0.65f, 0.5f, 0.7f),
				Polygon = new Vector2[]
				{
					new(x - 1, -22), new(x + 1, -22),
					new(x + 1, -22 + length), new(x - 1, -22 + length)
				}
			};
			AddChild(tube);
		}

		// Battant central
		Polygon2D clapper = new()
		{
			Color = new Color(0.6f, 0.55f, 0.4f, 0.8f),
			Polygon = new Vector2[]
			{
				new(-1, -12), new(1, -12), new(1, -4), new(-1, -4)
			}
		};
		AddChild(clapper);

		// Lueur subtile
		Polygon2D glow = new()
		{
			Color = new Color(0.8f, 0.75f, 0.6f, 0.06f),
			Polygon = CreateCircle(18f, 8),
			Position = new Vector2(0, -14)
		};
		AddChild(glow);
	}

	private void StartChiming()
	{
		// Légère oscillation perpétuelle
		_chimeTween = CreateTween().SetLoops();
		_chimeTween.TweenProperty(this, "rotation_degrees", 5f, 2.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_chimeTween.TweenProperty(this, "rotation_degrees", -5f, 2.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private void CreateDetectArea()
	{
		Area2D area = new() { Name = "DetectArea" };
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
		if (_activated || body is not Player)
			return;

		_activated = true;

		// Flash doux
		Modulate = new Color(1f, 0.95f, 0.85f, 1.2f);
		Tween flash = CreateTween();
		flash.TweenProperty(this, "modulate", Colors.White, 2f);

		// Cooldown : re-activable après 30s
		GetTree().CreateTimer(30f).Timeout += () => _activated = false;
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
