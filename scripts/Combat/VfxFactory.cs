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
	private static Texture2D _sparkTexture;

	/// <summary>Petit disque doux 8×8 pour orbes, flammes, particules génériques.</summary>
	public static Texture2D CircleTexture => _circleTexture ??= CreateCircleTexture(8);

	/// <summary>Losange 6×6 pour étincelles, impacts.</summary>
	public static Texture2D SparkTexture => _sparkTexture ??= CreateSparkTexture(6);

	// --- Sprites VFX pixel art (chargés une seule fois) ---
	private static Texture2D _explosionFrame1;
	private static Texture2D _explosionFrame2;
	private static Texture2D _explosionFrame3;
	private static Texture2D _explosionFrame4;
	private static Texture2D _explosionFrame5;
	private static Texture2D _dashTrailFrame1;
	private static Texture2D _dashTrailFrame2;
	private static Texture2D _dashTrailFrame3;
	private static Texture2D[] _dashTrailTextures;

	/// <summary>Partage les textures existantes avec les traînées préallouées du joueur.</summary>
	public static Texture2D[] GetDashTrailTextures() => _dashTrailTextures ??=
		new[] { DashTrailFrame1, DashTrailFrame2, DashTrailFrame3 };

	private static Texture2D ExplosionFrame1 => _explosionFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f1.png");
	private static Texture2D ExplosionFrame2 => _explosionFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f2.png");
	private static Texture2D ExplosionFrame3 => _explosionFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f3.png");
	private static Texture2D ExplosionFrame4 => _explosionFrame4 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f4.png");
	private static Texture2D ExplosionFrame5 => _explosionFrame5 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f5.png");
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
	// === Flash de lumière ponctuelle (hit, collecte) ===
	// =========================================================================

	public static PointLight2D CreateFlashLight(Vector2 position, Color color, float energy = 0.8f, float duration = 0.15f)
	{
		var light = new PointLight2D
		{
			GlobalPosition = position,
			Color = color,
			Energy = energy,
			TextureScale = 0.3f,
			Texture = GD.Load<Texture2D>("res://icon.svg"),
		};

		light.TreeEntered += () =>
		{
			Tween tween = light.CreateTween();
			tween.TweenProperty(light, "energy", 0f, duration);
			tween.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(light))
					light.QueueFree();
			}));
		};

		return light;
	}

	// =========================================================================
	// === Explosion VFX — sprite animé 5 frames (bombes, mines) ===
	// =========================================================================

	/// <summary>
	/// Crée un VFX d'explosion avec 5 frames pixel art + particules.
	/// Pour bombes, mines, et autres effets de zone.
	/// </summary>
	public static Node2D CreateExplosionVfx(Vector2 position)
	{
		var root = new Node2D { GlobalPosition = position };

		SpriteFrames frames = new();
		frames.AddAnimation("explode");
		frames.SetAnimationSpeed("explode", 15);
		frames.SetAnimationLoopMode("explode", SpriteFrames.LoopMode.None);
		frames.AddFrame("explode", ExplosionFrame1);
		frames.AddFrame("explode", ExplosionFrame2);
		frames.AddFrame("explode", ExplosionFrame3);
		frames.AddFrame("explode", ExplosionFrame4);
		frames.AddFrame("explode", ExplosionFrame5);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		sprite.Play("explode");
		root.AddChild(sprite);

		if (CurrentParticleLevel != ParticleLevel.Off)
		{
			var particles = new GpuParticles2D
			{
				Amount = ScaleAmount(8),
				Lifetime = 0.3f,
				Explosiveness = 0.9f,
				OneShot = true,
				Texture = SparkTexture,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			};

			var mat = new ParticleProcessMaterial
			{
				EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
				EmissionSphereRadius = 6f,
				Direction = new Vector3(0, 0, 0),
				Spread = 180f,
				InitialVelocityMin = 50f,
				InitialVelocityMax = 100f,
				Gravity = new Vector3(0, 40, 0),
				ScaleMin = 0.6f,
				ScaleMax = 1.8f,
				Color = new Color(0.88f, 0.48f, 0.22f, 0.6f),
			};
			particles.ProcessMaterial = mat;
			particles.Emitting = true;
			root.AddChild(particles);
		}

		PointLight2D light = CreateFlashLight(position, new Color(1f, 0.7f, 0.3f), 1.5f, 0.3f);
		root.AddChild(light);
		light.Position = Vector2.Zero;

		var timer = new Timer { WaitTime = 0.6f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
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

	private static ImageTexture CreateSparkTexture(int size)
	{
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		float center = size / 2f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dx = Mathf.Abs(x - center + 0.5f);
				float dy = Mathf.Abs(y - center + 0.5f);
				float diamond = (dx + dy) / center;
				if (diamond <= 1f)
				{
					float alpha = 1f - diamond;
					img.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
				}
			}
		}

		return ImageTexture.CreateFromImage(img);
	}
}
