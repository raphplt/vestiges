using Godot;
using Vestiges.Core;

namespace Vestiges.World.Lore;

/// <summary>
/// Messages graffiti sur murs urbains qui flickent et s'effacent.
/// "QUELQU'UN SOUVENEZ-VOUS DE NOUS", "EAU NORD 200M", etc.
/// Certains = indices gameplay. 10 XP. 3-5 par zone urbaine.
/// Lore : le graffiti résiste à l'effacement car il porte le poids émotionnel intense.
/// </summary>
public partial class SurvivingGraffiti : Node2D
{
	private static readonly string[] Messages =
	{
		"SOUVENEZ-VOUS",
		"ON ÉTAIT LÀ",
		"N'OUBLIEZ PAS",
		"EAU→NORD",
		"ABRI→SUD",
		"ICI VIVAIT",
		"LE CIEL AVAIT",
		"DES COULEURS",
		"RESTEZ ENSEMBLE",
		"NE PARTEZ PAS",
		"LUMIÈRE=VIE",
		"ÇA S'EFFACE"
	};

	private bool _discovered;
	private EventBus _eventBus;
	private Polygon2D _textBlock;
	private Tween _flickerTween;

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BuildVisual();
		CreateDetectArea();
	}

	private void BuildVisual()
	{
		// Mur de fond (fragment)
		Polygon2D wall = new()
		{
			Color = new Color(0.45f, 0.42f, 0.38f, 0.8f),
			Polygon = new Vector2[]
			{
				new(-20, -15), new(20, -15), new(22, -12),
				new(21, 15), new(-18, 14), new(-20, -15)
			}
		};
		AddChild(wall);

		// Texture du mur (lignes de brique)
		for (int row = 0; row < 3; row++)
		{
			Polygon2D brickLine = new()
			{
				Color = new Color(0.4f, 0.38f, 0.35f, 0.4f),
				Polygon = new Vector2[]
				{
					new(-18, -10 + row * 8), new(19, -10 + row * 8),
					new(19, -9 + row * 8), new(-18, -9 + row * 8)
				}
			};
			AddChild(brickLine);
		}

		// Texte graffiti (représenté par un bloc coloré stylisé)
		string message = Messages[GD.Randi() % Messages.Length];

		// Couleur de peinture aléatoire (spray)
		Color[] paintColors =
		{
			new(0.9f, 0.3f, 0.2f, 0.7f),  // rouge
			new(0.2f, 0.6f, 0.9f, 0.7f),  // bleu
			new(1f, 0.85f, 0.3f, 0.7f),   // jaune
			new(0.9f, 0.9f, 0.9f, 0.7f)   // blanc
		};
		Color paintColor = paintColors[GD.Randi() % paintColors.Length];

		// Simuler le texte avec des blocs de pixels (3-5 "lettres")
		int letterCount = Mathf.Min(message.Length, 8);
		float startX = -letterCount * 2f;
		for (int i = 0; i < letterCount; i++)
		{
			if (message[i] == ' ')
				continue;

			float lx = startX + i * 4f;
			float ly = (float)GD.RandRange(-4, 0);
			float lh = (float)GD.RandRange(4, 8);

			Polygon2D letter = new()
			{
				Color = paintColor,
				Polygon = new Vector2[]
				{
					new(lx, ly), new(lx + 3, ly),
					new(lx + 3, ly + lh), new(lx, ly + lh)
				}
			};
			AddChild(letter);
		}

		// Bloc principal de texte (zone de couleur pour l'ensemble)
		_textBlock = new Polygon2D
		{
			Color = new Color(paintColor.R, paintColor.G, paintColor.B, 0.15f),
			Polygon = new Vector2[]
			{
				new(-15, -6), new(15, -6),
				new(15, 6), new(-15, 6)
			}
		};
		AddChild(_textBlock);

		// Flickering continu : le graffiti lutte contre l'effacement
		_flickerTween = CreateTween().SetLoops();
		float flickerDuration = (float)GD.RandRange(2f, 5f);
		_flickerTween.TweenProperty(_textBlock, "modulate:a", 0.3f, flickerDuration)
			.SetTrans(Tween.TransitionType.Sine);
		_flickerTween.TweenProperty(_textBlock, "modulate:a", 1f, flickerDuration * 0.6f)
			.SetTrans(Tween.TransitionType.Sine);
		// Disparition brève (micro-effacement)
		_flickerTween.TweenProperty(_textBlock, "modulate:a", 0.05f, 0.15f);
		_flickerTween.TweenProperty(_textBlock, "modulate:a", 0.8f, 0.3f);
	}

	private void CreateDetectArea()
	{
		Area2D area = new() { Name = "DetectArea" };
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
		if (_discovered || body is not Player)
			return;

		_discovered = true;
		_eventBus.EmitSignal(EventBus.SignalName.XpGained, 10f);

		// Flash : le texte s'intensifie brièvement
		_flickerTween?.Kill();
		Tween reveal = CreateTween();
		reveal.TweenProperty(_textBlock, "modulate:a", 1.5f, 0.2f);
		reveal.TweenProperty(_textBlock, "modulate:a", 0.6f, 2f);

		GD.Print("[SurvivingGraffiti] Message lu — +10 XP");
	}
}
