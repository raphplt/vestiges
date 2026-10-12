using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Particules d'atmosphère autour de la caméra (plan 39 A5 et B1) : météo (cendres, débris au vent, pluie) et biome
/// (feuilles, papiers, pollen, poussière). Émises dans le monde, elles restent en place quand le joueur avance. Le réglage
/// des particules s'applique : réduites de moitié, ou aucune.
/// </summary>
public static class AtmosphereParticles
{
	// Vue en px monde autour du joueur (caméra de run) : boîte d'émission un peu plus large.
	private static readonly Vector2 EmissionHalfExtents = new(560f, 340f);

	/// <summary>Émetteur prêt à ajouter (alpha 0, à monter par <see cref="Swap"/>) ; null si les particules sont coupées.</summary>
	public static GpuParticles2D Create(AtmosphereParticleKind kind, int amount, Color color, Vector2 wind, string name)
	{
		if (kind == AtmosphereParticleKind.None || VfxFactory.CurrentParticleLevel == ParticleLevel.Off)
			return null;
		if (VfxFactory.CurrentParticleLevel == ParticleLevel.Reduced)
			amount = Mathf.Max(1, amount / 2);
		(Vector3 direction, float spread, float speedMin, float speedMax, float lifetime, float gravity, float animCycles) = kind switch
		{
			// Cendres : flocons qui tombent lentement et dérivent.
			AtmosphereParticleKind.Ash => (new Vector3(wind.X * 0.4f, 1f, 0f), 35f, 12f, 28f, 9f, 2f, 6f),
			// Débris au vent : traits pâles qui filent dans le sens du vent.
			AtmosphereParticleKind.Wind => (new Vector3(wind.X, wind.Y * 0.5f, 0f), 12f, 160f, 260f, 3.5f, 0f, 0f),
			// Pluie : traits de 6 px, rapides, inclinés par le vent.
			AtmosphereParticleKind.Rain => (new Vector3(wind.X * 0.25f, 1f, 0f), 4f, 420f, 520f, 1.1f, 0f, 0f),
			// Feuilles : tombent en biais, poussées par le vent, en virevoltant.
			AtmosphereParticleKind.Leaves => (new Vector3(wind.X * 0.8f, 0.6f, 0f), 40f, 14f, 30f, 8f, 3f, 14f),
			// Papiers : rares, portés à plat par le vent, ils se retournent.
			AtmosphereParticleKind.Paper => (new Vector3(wind.X, wind.Y * 0.5f - 0.1f, 0f), 25f, 40f, 80f, 7f, 0f, 9f),
			// Pollen : points qui flottent, montent un peu et luisent.
			AtmosphereParticleKind.Pollen => (new Vector3(wind.X * 0.5f, -0.2f, 0f), 70f, 4f, 12f, 9f, -1f, 4f),
			// Poussière dans la lumière : flotte presque immobile et scintille par moments (Bible §7.3).
			AtmosphereParticleKind.Glint => (new Vector3(wind.X * 0.3f, -0.1f, 0f), 180f, 2f, 7f, 10f, 0f, 3f),
			// Poussière : tombe des parois, lentement.
			_ => (new Vector3(wind.X * 0.2f, 1f, 0f), 25f, 6f, 14f, 10f, 1f, 0f),
		};
		string[][] frames = Frames(kind);
		ParticleProcessMaterial material = new()
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(EmissionHalfExtents.X, EmissionHalfExtents.Y, 0f),
			Direction = direction,
			Spread = spread,
			InitialVelocityMin = speedMin,
			InitialVelocityMax = speedMax,
			Gravity = new Vector3(0f, gravity, 0f),
			Color = color,
			// Les traits suivent leur trajectoire (pluie inclinée).
			ParticleFlagAlignY = kind == AtmosphereParticleKind.Rain,
			// Poses parcourues pendant la vie de la particule, chacune partant d'une pose au hasard.
			AnimSpeedMin = animCycles * 0.7f,
			AnimSpeedMax = animCycles * 1.3f,
			AnimOffsetMax = 1f,
		};
		Curve alpha = new();
		alpha.AddPoint(new Vector2(0f, 0f));
		alpha.AddPoint(new Vector2(0.1f, 1f));
		alpha.AddPoint(new Vector2(0.85f, 1f));
		alpha.AddPoint(new Vector2(1f, 0f));
		material.AlphaCurve = new CurveTexture { Curve = alpha };
		return new GpuParticles2D
		{
			Name = name,
			Amount = amount,
			Lifetime = lifetime,
			Preprocess = lifetime,
			Randomness = 1f,
			Texture = Sheet(frames),
			ProcessMaterial = material,
			Material = new CanvasItemMaterial
			{
				ParticlesAnimation = true, ParticlesAnimHFrames = frames.Length, ParticlesAnimVFrames = 1, ParticlesAnimLoop = true,
			},
			LocalCoords = false,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Modulate = new Color(1f, 1f, 1f, 0f),
			// Devant les entités (0), derrière les plaques d'ennemis (20) et les chiffres de dégâts (30) qui restent lisibles.
			ZIndex = 15,
			ZAsRelative = false,
		};
	}

	/// <summary>
	/// Poses dessinées au pixel (plan 41 R5) : '#' plein, '+' ombré, '.' vide ; la couleur des données les teinte. Une
	/// particule ne tourne pas librement (la grille des pixels casserait) : elle change de pose.
	/// </summary>
	private static string[][] Frames(AtmosphereParticleKind kind) => kind switch
	{
		AtmosphereParticleKind.Leaves => new[]
		{
			new[] { "....", ".##.", "#++#", ".##." }, new[] { "...#", "..#+", ".#+.", "#..." },
			new[] { "....", "....", "####", "...." }, new[] { "#...", "+#..", ".+#.", "...#" },
		},
		AtmosphereParticleKind.Paper => new[]
		{
			new[] { "#####", "#+++#", "#####", "....." }, new[] { ".####", "#+++#", "####.", "....." },
			new[] { ".....", "#####", ".....", "....." }, new[] { "####.", "#+++#", ".####", "....." },
		},
		AtmosphereParticleKind.Ash => new[] { new[] { ".#.", "#+#", ".#." }, new[] { "...", ".#.", "..." } },
		AtmosphereParticleKind.Pollen => new[] { new[] { "...", ".#.", "..." }, new[] { ".+.", "+#+", ".+." } },
		AtmosphereParticleKind.Glint => new[]
		{
			new[] { "...", ".+.", "..." }, new[] { "...", ".+.", "..." }, new[] { "...", ".#.", "..." },
			new[] { "...", ".#.", "..." }, new[] { "...", ".+.", "..." }, new[] { ".+.", "+#+", ".+." },
		},
		AtmosphereParticleKind.Rain => new[] { new[] { "#", "#", "#", "#", "#", "#" } },
		AtmosphereParticleKind.Wind => new[] { new[] { "#######" } },
		_ => new[] { new[] { "#" } },
	};

	/// <summary>Planche horizontale des poses, toutes de la taille de la première.</summary>
	private static ImageTexture Sheet(string[][] frames)
	{
		int width = frames[0][0].Length;
		int height = frames[0].Length;
		Image image = Image.CreateEmpty(width * frames.Length, height, false, Image.Format.Rgba8);
		Color shade = new(0.72f, 0.72f, 0.72f);
		for (int f = 0; f < frames.Length; f++)
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					char pixel = frames[f][y][x];
					if (pixel != '.')
						image.SetPixel(f * width + x, y, pixel == '+' ? shade : Colors.White);
				}
		return ImageTexture.CreateFromImage(image);
	}

	/// <summary>
	/// Remplace <paramref name="current"/> par <paramref name="next"/> en <paramref name="fadeSec"/> : chaque émetteur a son
	/// propre tween, qui meurt avec lui, si bien que deux changements rapprochés ne laissent jamais d'émetteur orphelin.
	/// </summary>
	public static GpuParticles2D Swap(Node parent, GpuParticles2D current, GpuParticles2D next, float fadeSec)
	{
		if (current != null && GodotObject.IsInstanceValid(current))
		{
			Tween fadeOut = current.CreateTween();
			fadeOut.TweenProperty(current, "modulate:a", 0f, fadeSec);
			fadeOut.TweenCallback(Callable.From(current.QueueFree));
		}
		if (next != null)
		{
			parent.AddChild(next);
			next.CreateTween().TweenProperty(next, "modulate:a", 1f, fadeSec);
		}
		return next;
	}
}
