using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Tiles d'eau spéciales montrant des reflets fantômes de bâtiments.
/// La proximité clarifie le reflet. 3s immobile = découverte de lore.
/// 1-2 par zone marais.
/// Lore : l'eau est une archive de mémoire. Les reflets sont de vrais
/// souvenirs stockés dans l'eau.
/// </summary>
public partial class WaterMirror : Node2D
{
	private bool _discovered;
	private EventBus _eventBus;
	private bool _playerInside;
	private float _stareTimer;
	private const float StareThreshold = 3f;
	private Polygon2D _reflection;
	private Polygon2D _ripple;

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BuildVisual();
		CreateDetectArea();
	}

	public override void _Process(double delta)
	{
		if (!_playerInside || _discovered)
			return;

		// Vérifier si le joueur est immobile
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return;

		if (player.Velocity.LengthSquared() < 10f)
		{
			_stareTimer += (float)delta;

			// Le reflet se clarifie progressivement
			float clarity = Mathf.Clamp(_stareTimer / StareThreshold, 0f, 1f);
			if (_reflection != null)
				_reflection.Modulate = new Color(1, 1, 1, 0.1f + clarity * 0.5f);

			if (_stareTimer >= StareThreshold)
				DiscoverLore();
		}
		else
		{
			// Le joueur bouge : réinitialiser
			_stareTimer = Mathf.Max(0f, _stareTimer - (float)delta * 2f);
			if (_reflection != null)
			{
				float clarity = Mathf.Clamp(_stareTimer / StareThreshold, 0f, 1f);
				_reflection.Modulate = new Color(1, 1, 1, 0.1f + clarity * 0.5f);
			}
		}
	}

	private void BuildVisual()
	{
		// Surface d'eau réfléchissante (ovale bleu-vert)
		Polygon2D water = new()
		{
			Color = new Color(0.15f, 0.3f, 0.5f, 0.6f),
			Polygon = CreateOval(28f, 18f, 12)
		};
		AddChild(water);

		// Bord de mousse/vase
		Polygon2D shore = new()
		{
			Color = new Color(0.3f, 0.35f, 0.2f, 0.5f),
			Polygon = CreateOval(32f, 22f, 12)
		};
		AddChild(shore);
		MoveChild(shore, 0); // derrière l'eau

		// Reflet fantôme (bâtiment inversé, très pâle)
		_reflection = new Polygon2D
		{
			Color = new Color(0.7f, 0.75f, 0.9f, 0.1f),
			Polygon = new Vector2[]
			{
				new(-12, -2), new(12, -2), new(12, 10),
				new(6, 10), new(6, 15), new(-6, 15),
				new(-6, 10), new(-12, 10)
			}
		};
		_reflection.Modulate = new Color(1, 1, 1, 0.1f);
		AddChild(_reflection);

		// Fenêtres dans le reflet
		Polygon2D refWindow1 = new()
		{
			Color = new Color(0.9f, 0.85f, 0.6f, 0.15f),
			Polygon = new Vector2[]
			{
				new(-6, 1), new(-3, 1), new(-3, 4), new(-6, 4)
			}
		};
		_reflection.AddChild(refWindow1);

		Polygon2D refWindow2 = new()
		{
			Color = new Color(0.9f, 0.85f, 0.6f, 0.15f),
			Polygon = new Vector2[]
			{
				new(3, 1), new(6, 1), new(6, 4), new(3, 4)
			}
		};
		_reflection.AddChild(refWindow2);

		// Ondulations concentriques sur l'eau
		_ripple = new Polygon2D
		{
			Color = new Color(0.5f, 0.6f, 0.8f, 0.15f),
			Polygon = CreateOval(15f, 10f, 10)
		};
		AddChild(_ripple);

		Tween rippleTween = CreateTween().SetLoops();
		rippleTween.TweenProperty(_ripple, "scale", new Vector2(1.8f, 1.8f), 3f)
			.SetTrans(Tween.TransitionType.Sine);
		rippleTween.TweenProperty(_ripple, "modulate:a", 0f, 3f)
			.SetTrans(Tween.TransitionType.Sine);
		rippleTween.TweenCallback(Callable.From(() =>
		{
			_ripple.Scale = Vector2.One;
			_ripple.Modulate = new Color(1, 1, 1, 0.15f);
		}));

		// Lueur bleutée subtile
		PointLight2D waterGlow = new()
		{
			Color = new Color(0.3f, 0.5f, 0.8f),
			Energy = 0.2f,
			TextureScale = 0.3f,
			Texture = GD.Load<Texture2D>("res://icon.svg")
		};
		AddChild(waterGlow);
	}

	private void CreateDetectArea()
	{
		Area2D area = new() { Name = "MirrorArea" };
		area.CollisionLayer = 0;
		area.CollisionMask = 1;
		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = 40f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);

		area.BodyEntered += OnPlayerEntered;
		area.BodyExited += OnPlayerExited;
	}

	private void OnPlayerEntered(Node2D body)
	{
		if (body is not Player)
			return;
		_playerInside = true;
		_stareTimer = 0f;
	}

	private void OnPlayerExited(Node2D body)
	{
		if (body is not Player)
			return;
		_playerInside = false;
		_stareTimer = 0f;

		// Le reflet s'estompe quand le joueur s'éloigne
		if (_reflection != null && !_discovered)
		{
			Tween fade = CreateTween();
			fade.TweenProperty(_reflection, "modulate:a", 0.1f, 1f);
		}
	}

	private void DiscoverLore()
	{
		_discovered = true;

		// Le reflet se clarifie totalement
		Tween reveal = CreateTween();
		reveal.TweenProperty(_reflection, "modulate:a", 0.7f, 0.5f);
		reveal.TweenProperty(_reflection, "modulate:a", 0.3f, 3f);

		// Flash
		Modulate = new Color(0.8f, 0.85f, 1.2f, 1f);
		Tween flash = CreateTween();
		flash.TweenProperty(this, "modulate", Colors.White, 2f);

		// Récompense
		_eventBus.EmitSignal(EventBus.SignalName.XpGained, 20f);
		_eventBus.EmitSignal(EventBus.SignalName.SouvenirDiscovered, "souvenir_eau", "Mémoire des Eaux", "l_effacement");

		GD.Print("[WaterMirror] Reflet découvert — +20 XP, lore débloqué");
	}

	private static Vector2[] CreateOval(float rx, float ry, int segments)
	{
		Vector2[] pts = new Vector2[segments];
		for (int i = 0; i < segments; i++)
		{
			float a = Mathf.Tau * i / segments;
			pts[i] = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
		}
		return pts;
	}
}
