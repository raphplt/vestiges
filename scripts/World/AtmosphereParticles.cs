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
		(Vector2I size, Vector3 direction, float spread, float speedMin, float speedMax, float lifetime, float gravity) = kind switch
		{
			// Cendres : flocons de 2 px qui tombent lentement et dérivent.
			AtmosphereParticleKind.Ash => (new Vector2I(2, 2), new Vector3(wind.X * 0.4f, 1f, 0f), 35f, 12f, 28f, 9f, 2f),
			// Débris au vent : traits pâles qui filent dans le sens du vent.
			AtmosphereParticleKind.Wind => (new Vector2I(7, 1), new Vector3(wind.X, wind.Y * 0.5f, 0f), 12f, 160f, 260f, 3.5f, 0f),
			// Pluie : traits de 6 px, rapides, inclinés par le vent.
			AtmosphereParticleKind.Rain => (new Vector2I(1, 6), new Vector3(wind.X * 0.25f, 1f, 0f), 4f, 420f, 520f, 1.1f, 0f),
			// Feuilles : tombent en biais, poussées par le vent.
			AtmosphereParticleKind.Leaves => (new Vector2I(3, 2), new Vector3(wind.X * 0.8f, 0.6f, 0f), 40f, 14f, 30f, 8f, 3f),
			// Papiers : rares, portés à plat par le vent.
			AtmosphereParticleKind.Paper => (new Vector2I(4, 3), new Vector3(wind.X, wind.Y * 0.5f - 0.1f, 0f), 25f, 40f, 80f, 7f, 0f),
			// Pollen : points qui flottent et montent un peu.
			AtmosphereParticleKind.Pollen => (new Vector2I(2, 1), new Vector3(wind.X * 0.5f, -0.2f, 0f), 70f, 4f, 12f, 9f, -1f),
			// Poussière : tombe des parois, lentement.
			_ => (new Vector2I(1, 1), new Vector3(wind.X * 0.2f, 1f, 0f), 25f, 6f, 14f, 10f, 1f),
		};
		Image image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
		image.Fill(Colors.White);
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
			Texture = ImageTexture.CreateFromImage(image),
			ProcessMaterial = material,
			LocalCoords = false,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Modulate = new Color(1f, 1f, 1f, 0f),
			// Devant les entités (0), derrière les plaques d'ennemis (20) et les chiffres de dégâts (30) qui restent lisibles.
			ZIndex = 15,
			ZAsRelative = false,
		};
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
