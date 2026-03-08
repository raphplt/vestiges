using Godot;

namespace Vestiges.World.Lore;

/// <summary>
/// Étagères vides — les objets ont cessé d'exister, pas été pillés.
/// Un objet fantôme flicker in/out sur l'étagère.
/// </summary>
public partial class EmptyShelves : Node2D
{
	public override void _Ready()
	{
		BuildVisual();
	}

	private void BuildVisual()
	{
		// Étagère (rectangle avec planches)
		float width = (float)GD.RandRange(20, 30);
		float height = (float)GD.RandRange(28, 38);
		int shelves = (int)GD.RandRange(3, 5);

		// Cadre
		Polygon2D frame = new()
		{
			Color = new Color(0.45f, 0.35f, 0.25f, 0.8f),
			Polygon = new Vector2[]
			{
				new(-width / 2, -height / 2),
				new(width / 2, -height / 2),
				new(width / 2, height / 2),
				new(-width / 2, height / 2)
			}
		};
		AddChild(frame);

		// Planches
		for (int i = 1; i < shelves; i++)
		{
			float y = -height / 2 + (height / shelves) * i;
			Polygon2D plank = new()
			{
				Color = new Color(0.35f, 0.25f, 0.18f, 0.9f),
				Polygon = new Vector2[]
				{
					new(-width / 2 + 1, y - 1),
					new(width / 2 - 1, y - 1),
					new(width / 2 - 1, y + 1),
					new(-width / 2 + 1, y + 1)
				}
			};
			AddChild(plank);
		}

		// Objet fantôme qui flicker (petit losange sur une planche aléatoire)
		int ghostShelf = (int)GD.RandRange(1, shelves);
		float ghostY = -height / 2 + (height / shelves) * ghostShelf - 6f;
		float ghostX = (float)GD.RandRange(-width / 3, width / 3);

		// Forme aléatoire : livre, vase ou boîte
		Vector2[] ghostShape = (GD.Randi() % 3) switch
		{
			0 => new Vector2[] // livre
			{
				new(-4, -6), new(4, -6), new(4, 0), new(-4, 0)
			},
			1 => new Vector2[] // vase
			{
				new(-2, -7), new(2, -7), new(3, -2), new(2, 0), new(-2, 0), new(-3, -2)
			},
			_ => new Vector2[] // boîte
			{
				new(-5, -4), new(5, -4), new(5, 0), new(-5, 0)
			}
		};

		Polygon2D ghost = new()
		{
			Position = new Vector2(ghostX, ghostY),
			Polygon = ghostShape,
			Color = new Color(0.7f, 0.65f, 0.55f, 0f)
		};
		AddChild(ghost);

		// Flickering : apparaît et disparaît en boucle
		Tween flicker = CreateTween().SetLoops();
		flicker.TweenProperty(ghost, "color:a", 0.5f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine);
		flicker.TweenProperty(ghost, "color:a", 0.5f, 1.5f); // reste visible un instant
		flicker.TweenProperty(ghost, "color:a", 0f, 0.6f)
			.SetTrans(Tween.TransitionType.Sine);
		flicker.TweenProperty(ghost, "color:a", 0f, 2f); // reste invisible plus longtemps
	}
}
