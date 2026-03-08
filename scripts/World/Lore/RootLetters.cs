using Godot;

namespace Vestiges.World.Lore;

/// <summary>
/// Racines d'arbres qui ont soulevé l'asphalte en formes de lettres.
/// Fragments de noms de rues et enseignes absorbés par les racines.
/// Lueur dorée la nuit (PointLight2D faible).
/// </summary>
public partial class RootLetters : Node2D
{
	private static readonly string[] WordFragments =
	{
		"RUE D", "BOULA", "ÉCOL", "BIBLIO",
		"PARC", "AVENU", "PLAC", "MARCH",
		"JARD", "MAIS", "PONT", "CHAP",
		"PHARM", "GARE", "CINEM", "LIBRA",
		"CAFÉ", "HÔPI", "MUSÉU", "THÉÂT"
	};

	public override void _Ready()
	{
		BuildVisual();
	}

	private void BuildVisual()
	{
		string word = WordFragments[GD.Randi() % WordFragments.Length];

		// Racines de fond (forme organique brune)
		float rootWidth = word.Length * 9f;
		Polygon2D rootBase = new()
		{
			Color = new Color(0.3f, 0.22f, 0.12f, 0.8f),
			Polygon = new Vector2[]
			{
				new(-rootWidth / 2 - 5, -4),
				new(-rootWidth / 2, -8),
				new(rootWidth / 2, -6),
				new(rootWidth / 2 + 5, -2),
				new(rootWidth / 2 + 3, 4),
				new(-rootWidth / 2 - 3, 5)
			}
		};
		AddChild(rootBase);

		// Fragments d'asphalte soulevé
		for (int i = 0; i < 3; i++)
		{
			float x = (float)GD.RandRange(-rootWidth / 2, rootWidth / 2);
			Polygon2D asphalt = new()
			{
				Position = new Vector2(x, (float)GD.RandRange(-3, 3)),
				Color = new Color(0.25f, 0.25f, 0.28f, 0.5f),
				Polygon = new Vector2[]
				{
					new(-4, -2), new(3, -3), new(5, 1), new(-3, 2)
				},
				Rotation = (float)GD.RandRange(-0.3f, 0.3f)
			};
			AddChild(asphalt);
		}

		// Lumière dorée visible la nuit
		PointLight2D light = new()
		{
			Color = new Color(1f, 0.9f, 0.4f),
			Energy = 0.15f,
			TextureScale = 0.6f,
			Texture = CreateGradientTexture(),
			Position = new Vector2(0, -2)
		};
		AddChild(light);
	}

	private static GradientTexture2D CreateGradientTexture()
	{
		GradientTexture2D tex = new();
		tex.Width = 128;
		tex.Height = 128;
		tex.Fill = GradientTexture2D.FillEnum.Radial;
		tex.FillFrom = new Vector2(0.5f, 0.5f);
		tex.FillTo = new Vector2(0.5f, 0f);
		Gradient gradient = new();
		gradient.SetColor(0, Colors.White);
		gradient.AddPoint(1f, new Color(1, 1, 1, 0));
		tex.Gradient = gradient;
		return tex;
	}
}
