using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Panneau routier demi-avalé par un chêne. Texte à moitié visible.
/// Certains pointent vers le POI non-découvert le plus proche (aide directionnelle).
/// 15 XP à la première approche.
/// </summary>
public partial class SwallowedSign : Node2D
{
	private static readonly string[] SignTexts =
	{
		"DIRECTION →",
		"ATTENTION ZO...",
		"BOULANGERIE P...",
		"SORTIE 12 — M...",
		"ZONE RÉSIDEN...",
		"INTERDIT AU...",
		"CENTRE VIL...",
		"PARKNG SOUS...",
		"LIMIT 50 KM...",
		"PROCH. STATION..."
	};

	private bool _discovered;
	private EventBus _eventBus;

	/// <summary>Direction vers le POI le plus proche, injectée au spawn.</summary>
	public float PoiDirection { get; set; }

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BuildVisual();
		CreateDetectArea();
	}

	private void BuildVisual()
	{
		// Tronc d'arbre qui enveloppe le panneau
		Polygon2D trunk = new()
		{
			Color = new Color(0.35f, 0.28f, 0.15f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-8, -20), new(-3, -25), new(3, -25), new(8, -20),
				new(10, 15), new(6, 20), new(-6, 20), new(-10, 15)
			}
		};
		AddChild(trunk);

		// Panneau (rectangle partiellement absorbé)
		Polygon2D sign = new()
		{
			Color = new Color(0.3f, 0.45f, 0.6f, 0.7f),
			Polygon = new Vector2[]
			{
				new(3, -22), new(22, -22),
				new(22, -10), new(3, -10)
			}
		};
		AddChild(sign);

		// Bordure du panneau
		Polygon2D border = new()
		{
			Color = new Color(0.7f, 0.7f, 0.7f, 0.5f),
			Polygon = new Vector2[]
			{
				new(2, -23), new(23, -23), new(23, -22),
				new(2, -22)
			}
		};
		AddChild(border);

		// Feuillage
		Polygon2D leaves = new()
		{
			Color = new Color(0.25f, 0.5f, 0.2f, 0.7f),
			Polygon = new Vector2[]
			{
				new(-15, -30), new(0, -38), new(15, -30),
				new(12, -22), new(-12, -22)
			}
		};
		AddChild(leaves);
	}

	private void CreateDetectArea()
	{
		Area2D area = new() { Name = "DetectArea" };
		area.CollisionLayer = 0;
		area.CollisionMask = 1;
		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = 45f };
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
		_eventBus.EmitSignal(EventBus.SignalName.XpGained, 15f);

		// Feedback subtil
		Modulate = new Color(1f, 0.95f, 0.85f, 1.1f);
		Tween tween = CreateTween();
		tween.TweenProperty(this, "modulate", Colors.White, 1f);
	}
}
