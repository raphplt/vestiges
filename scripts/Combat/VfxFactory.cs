using Godot;

namespace Vestiges.Combat;

public enum ParticleLevel
{
	Full,
	Reduced,
	Off
}

/// <summary>
/// Fabrique centralisée de VFX (particules, flash, slash, sprites animés).
/// Produit des nodes prêtes à ajouter à la scène, auto-nettoyées.
/// Combine particules procédurales et sprites pixel art pour un rendu riche.
/// </summary>
public static class VfxFactory
{
	// --- Réglage global particules (persisté par CombatFxSettings) ---

	public static ParticleLevel CurrentParticleLevel
	{
		get => CombatFxSettings.ParticleLevel;
		set => CombatFxSettings.ParticleLevel = value;
	}

	/// <summary>Applique le multiplicateur Reduced aux quantités de particules.</summary>
	private static int ScaleAmount(int amount)
	{
		return CurrentParticleLevel == ParticleLevel.Reduced
			? Mathf.Max(amount / 2, 1)
			: amount;
	}

	// --- Textures procédurales (créées une seule fois, cachées en static) ---
	private static Texture2D _circleTexture;

	/// <summary>Petit disque doux 8×8 pour orbes, flammes, particules génériques.</summary>
	public static Texture2D CircleTexture => _circleTexture ??= CreateCircleTexture(8);


	// --- Sprites VFX pixel art (chargés une seule fois) ---
	private static Texture2D _dashTrailFrame1;
	private static Texture2D _dashTrailFrame2;
	private static Texture2D _dashTrailFrame3;
	private static Texture2D[] _dashTrailTextures;

	/// <summary>Partage les textures existantes avec les traînées préallouées du joueur.</summary>
	public static Texture2D[] GetDashTrailTextures() => _dashTrailTextures ??=
		new[] { DashTrailFrame1, DashTrailFrame2, DashTrailFrame3 };

	private static Texture2D DashTrailFrame1 => _dashTrailFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dash_trail_f1.png");
	private static Texture2D DashTrailFrame2 => _dashTrailFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dash_trail_f2.png");
	private static Texture2D DashTrailFrame3 => _dashTrailFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dash_trail_f3.png");

	// =========================================================================
	// === Orbe XP : GPUParticles2D qui suit l'orbe ===
	// =========================================================================

	private static ParticleProcessMaterial _xpOrbGlowMaterial;

	/// <summary>Lueur d'une orbe d'XP, créée une fois par orbe recyclée ; allumée ou non à chaque lancement.</summary>
	public static GpuParticles2D CreateXpOrbGlow()
	{
		_xpOrbGlowMaterial ??= new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 2f,
			Direction = new Vector3(0, -1, 0),
			Spread = 30f,
			InitialVelocityMin = 5f,
			InitialVelocityMax = 12f,
			Gravity = new Vector3(0, -10, 0),
			ScaleMin = 0.25f,
			ScaleMax = 0.5f,
			Color = new Color(0.55f, 0.82f, 1f, 0.8f),
		};

		return new GpuParticles2D
		{
			Amount = 3,
			Lifetime = 0.5f,
			SpeedScale = 1f,
			Explosiveness = 0f,
			Texture = CircleTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			ProcessMaterial = _xpOrbGlowMaterial,
		};
	}

	// =========================================================================
	// === Textures procédurales ===
	// =========================================================================

	private static ImageTexture CreateCircleTexture(int size)
	{
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		float center = size / 2f;
		float radius = center - 0.5f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dist = new Vector2(x - center + 0.5f, y - center + 0.5f).Length();
				if (dist <= radius)
				{
					float alpha = 1f - Mathf.Clamp((dist - radius + 1.5f) / 1.5f, 0f, 1f);
					img.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp(alpha + 0.3f, 0f, 1f)));
				}
			}
		}

		return ImageTexture.CreateFromImage(img);
	}

}
