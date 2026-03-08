using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Balançoire qui bouge seule, accrochée à un arbre ayant poussé à travers un balcon.
/// S'arrête quand le joueur approche, puis reprend. Rire d'enfant bref.
/// 25 XP + moment émotionnel fort. Rare (0-1 par map).
/// </summary>
public partial class GhostSwing : Node2D
{
	private bool _discovered;
	private EventBus _eventBus;
	private Tween _swingTween;
	private Node2D _swingPivot;
	private bool _playerNear;

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BuildVisual();
		CreateDetectArea();
		StartSwinging();
	}

	private void BuildVisual()
	{
		// Branche d'arbre (horizontale, en haut)
		Polygon2D branch = new()
		{
			Color = new Color(0.35f, 0.28f, 0.15f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-25, -32), new(25, -35),
				new(26, -31), new(-24, -28)
			}
		};
		AddChild(branch);

		// Feuillage au-dessus
		Polygon2D leaves = new()
		{
			Color = new Color(0.25f, 0.5f, 0.2f, 0.7f),
			Polygon = new Vector2[]
			{
				new(-20, -45), new(0, -50), new(20, -45),
				new(25, -35), new(-25, -35)
			}
		};
		AddChild(leaves);

		// Pivot de la balançoire (point d'attache sur la branche)
		_swingPivot = new Node2D { Position = new Vector2(5, -30) };
		AddChild(_swingPivot);

		// Cordes
		Polygon2D leftRope = new()
		{
			Color = new Color(0.5f, 0.4f, 0.25f, 0.8f),
			Polygon = new Vector2[]
			{
				new(-5, 0), new(-4, 0), new(-4, 28), new(-5, 28)
			}
		};
		_swingPivot.AddChild(leftRope);

		Polygon2D rightRope = new()
		{
			Color = new Color(0.5f, 0.4f, 0.25f, 0.8f),
			Polygon = new Vector2[]
			{
				new(4, 0), new(5, 0), new(5, 28), new(4, 28)
			}
		};
		_swingPivot.AddChild(rightRope);

		// Siège
		Polygon2D seat = new()
		{
			Color = new Color(0.4f, 0.3f, 0.2f, 0.9f),
			Polygon = new Vector2[]
			{
				new(-7, 26), new(7, 26), new(7, 29), new(-7, 29)
			}
		};
		_swingPivot.AddChild(seat);
	}

	private void StartSwinging()
	{
		_swingTween?.Kill();
		_swingTween = CreateTween().SetLoops();
		_swingTween.TweenProperty(_swingPivot, "rotation_degrees", 12f, 1.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_swingTween.TweenProperty(_swingPivot, "rotation_degrees", -12f, 1.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private void CreateDetectArea()
	{
		Area2D area = new() { Name = "DetectArea" };
		area.CollisionLayer = 0;
		area.CollisionMask = 1;
		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = 60f };
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

		_playerNear = true;

		// La balançoire s'arrête en remarquant le joueur
		_swingTween?.Kill();
		Tween stop = CreateTween();
		stop.TweenProperty(_swingPivot, "rotation_degrees", 0f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		if (!_discovered)
		{
			_discovered = true;
			_eventBus.EmitSignal(EventBus.SignalName.XpGained, 25f);

			// Attendre la fin de l'arrêt puis jouer le rire
			stop.TweenCallback(Callable.From(() =>
			{
				// Flash émotionnel subtil
				Modulate = new Color(1f, 0.95f, 0.85f, 1.1f);
				Tween flash = CreateTween();
				flash.TweenProperty(this, "modulate", Colors.White, 1.5f);

				// Reprendre le balancement après une pause
				GetTree().CreateTimer(2.5f).Timeout += () =>
				{
					if (_playerNear)
						return;
					StartSwinging();
				};
			}));
		}
	}

	private void OnPlayerExited(Node2D body)
	{
		if (body is not Player)
			return;

		_playerNear = false;

		// Reprendre le balancement quand le joueur s'éloigne
		GetTree().CreateTimer(1.5f).Timeout += () =>
		{
			if (!_playerNear)
				StartSwinging();
		};
	}
}
