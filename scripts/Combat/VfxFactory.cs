using Godot;

namespace Vestiges.Combat;

public enum ParticleLevel
{
	Full,
	Reduced,
	Off
}

/// <summary>
/// Fabrique centralisée de VFX (particules, flash, slash).
/// Produit des nodes prêtes à ajouter à la scène, auto-nettoyées.
/// </summary>
public static class VfxFactory
{
	// --- Réglage global particules ---
	private static ParticleLevel _particleLevel = ParticleLevel.Full;
	private const string SettingsPath = "user://display_settings.cfg";

	public static ParticleLevel CurrentParticleLevel
	{
		get => _particleLevel;
		set => _particleLevel = value;
	}

	public static void LoadSettings()
	{
		ConfigFile cfg = new();
		if (cfg.Load(SettingsPath) != Error.Ok)
			return;
		int level = (int)cfg.GetValue("display", "particle_level", 0).AsInt32();
		_particleLevel = (ParticleLevel)Mathf.Clamp(level, 0, 2);
	}

	public static void SaveSettings()
	{
		ConfigFile cfg = new();
		cfg.SetValue("display", "particle_level", (int)_particleLevel);
		cfg.Save(SettingsPath);
	}

	/// <summary>Applique le multiplicateur Reduced aux quantités de particules.</summary>
	private static int ScaleAmount(int amount)
	{
		return _particleLevel == ParticleLevel.Reduced
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

	// === Orbe XP : GPUParticles2D qui suit l'orbe ===

	public static GpuParticles2D CreateXpOrbGlow()
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var particles = new GpuParticles2D
		{
			Amount = ScaleAmount(3),
			Lifetime = 0.5f,
			SpeedScale = 1f,
			Explosiveness = 0f,
			Texture = CircleTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var mat = new ParticleProcessMaterial
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
		particles.ProcessMaterial = mat;

		return particles;
	}

	// === Impact de projectile ===

	public static Node2D CreateProjectileImpact(Vector2 position, Color color)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		var particles = new GpuParticles2D
		{
			Amount = ScaleAmount(6),
			Lifetime = 0.25f,
			Explosiveness = 1f,
			OneShot = true,
			Texture = SparkTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 2f,
			Direction = new Vector3(0, 0, 0),
			Spread = 180f,
			InitialVelocityMin = 30f,
			InitialVelocityMax = 60f,
			Gravity = new Vector3(0, 40, 0),
			ScaleMin = 0.5f,
			ScaleMax = 1.2f,
			Color = new Color(color, 0.9f),
		};
		particles.ProcessMaterial = mat;

		root.AddChild(particles);
		particles.Emitting = true;

		// Auto-nettoyage via timer interne
		var timer = new Timer { WaitTime = 0.5f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	// === Flamme (torche, feu de camp, foyer) ===

	public static GpuParticles2D CreateFlameParticles(float intensity = 1f)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		int amount = ScaleAmount(Mathf.RoundToInt(8 * intensity));
		var particles = new GpuParticles2D
		{
			Amount = Mathf.Max(amount, 2),
			Lifetime = 0.4f,
			SpeedScale = 1.2f,
			Explosiveness = 0f,
			Texture = CircleTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var gradient = new GradientTexture1D();
		var g = new Gradient();
		g.SetColor(0, new Color(1f, 0.85f, 0.3f, 0.9f));   // Or Foyer
		g.AddPoint(0.4f, new Color(0.88f, 0.48f, 0.22f, 0.7f)); // Orange flamme
		g.SetColor(g.GetPointCount() - 1, new Color(0.6f, 0.15f, 0.05f, 0f));
		gradient.Gradient = g;

		var scaleOverLife = new CurveTexture();
		var curve = new Curve();
		curve.AddPoint(new Vector2(0f, 0.6f));
		curve.AddPoint(new Vector2(0.3f, 1f));
		curve.AddPoint(new Vector2(1f, 0.1f));
		scaleOverLife.Curve = curve;

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 2f * intensity,
			Direction = new Vector3(0, -1, 0),
			Spread = 15f,
			InitialVelocityMin = 10f * intensity,
			InitialVelocityMax = 25f * intensity,
			Gravity = new Vector3(0, -30, 0),
			ScaleMin = 0.5f * intensity,
			ScaleMax = 1.0f * intensity,
			ColorRamp = gradient,
			ScaleCurve = scaleOverLife,
		};
		particles.ProcessMaterial = mat;

		return particles;
	}

	// === Flash de lumière ponctuelle (hit, collecte) ===

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

	// === Slash VFX amélioré ===

	public static Node2D CreateSlashVfx(Vector2 position, Vector2 direction, float range, float arcAngle, Color color)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D
		{
			GlobalPosition = position + direction * 8f,
			Rotation = direction.Angle(),
		};

		// Particules de slash en arc
		var particles = new GpuParticles2D
		{
			Amount = ScaleAmount(Mathf.RoundToInt(Mathf.Clamp(arcAngle / 30f, 4, 12))),
			Lifetime = 0.15f,
			Explosiveness = 1f,
			OneShot = true,
			Texture = SparkTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		float radius = Mathf.Clamp(range * 0.5f, 15f, 70f);
		float spreadDeg = arcAngle >= 359f ? 180f : Mathf.Clamp(arcAngle * 0.5f, 20f, 90f);

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = radius * 0.3f,
			Direction = new Vector3(1, 0, 0),
			Spread = spreadDeg,
			InitialVelocityMin = radius * 1.5f,
			InitialVelocityMax = radius * 2.5f,
			Gravity = Vector3.Zero,
			ScaleMin = 0.6f,
			ScaleMax = 1.5f,
			Color = new Color(color, 0.85f),
			DampingMin = 80f,
			DampingMax = 120f,
		};
		particles.ProcessMaterial = mat;
		particles.Emitting = true;

		root.AddChild(particles);

		// Auto-nettoyage
		var timer = new Timer { WaitTime = 0.4f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	// === Trail de projectile ===

	public static GpuParticles2D CreateProjectileTrail(Color color, bool isCrit = false)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		int amount = ScaleAmount(isCrit ? 8 : 5);
		var particles = new GpuParticles2D
		{
			Amount = amount,
			Lifetime = 0.2f,
			SpeedScale = 1f,
			Explosiveness = 0f,
			Texture = CircleTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var gradient = new GradientTexture1D();
		var g = new Gradient();
		g.SetColor(0, new Color(color, 0.7f));
		g.SetColor(g.GetPointCount() - 1, new Color(color, 0f));
		gradient.Gradient = g;

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 1f,
			Direction = new Vector3(-1, 0, 0),
			Spread = 15f,
			InitialVelocityMin = 2f,
			InitialVelocityMax = 6f,
			Gravity = Vector3.Zero,
			ScaleMin = isCrit ? 0.4f : 0.3f,
			ScaleMax = isCrit ? 0.8f : 0.5f,
			ColorRamp = gradient,
			DampingMin = 20f,
			DampingMax = 40f,
		};
		particles.ProcessMaterial = mat;

		return particles;
	}

	// === XP collect burst (one-shot flash à la collecte) ===

	public static Node2D CreateXpCollectBurst(Vector2 position)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		var particles = new GpuParticles2D
		{
			Amount = ScaleAmount(4),
			Lifetime = 0.3f,
			Explosiveness = 1f,
			OneShot = true,
			Texture = SparkTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 2f,
			Direction = new Vector3(0, -1, 0),
			Spread = 180f,
			InitialVelocityMin = 25f,
			InitialVelocityMax = 50f,
			Gravity = new Vector3(0, 20, 0),
			ScaleMin = 0.2f,
			ScaleMax = 0.5f,
			Color = new Color(0.55f, 0.82f, 1f, 0.9f),
		};
		particles.ProcessMaterial = mat;
		particles.Emitting = true;

		root.AddChild(particles);

		var timer = new Timer { WaitTime = 0.5f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	// === Level up burst (explosion radiale dorée) ===

	public static Node2D CreateLevelUpBurst(Vector2 position)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		var particles = new GpuParticles2D
		{
			Amount = ScaleAmount(20),
			Lifetime = 0.6f,
			Explosiveness = 0.95f,
			OneShot = true,
			Texture = SparkTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var gradient = new GradientTexture1D();
		var g = new Gradient();
		g.SetColor(0, new Color(1f, 0.92f, 0.4f, 1f));
		g.AddPoint(0.4f, new Color(0.83f, 0.66f, 0.26f, 0.8f));
		g.SetColor(g.GetPointCount() - 1, new Color(1f, 1f, 1f, 0f));
		gradient.Gradient = g;

		var scaleOverLife = new CurveTexture();
		var curve = new Curve();
		curve.AddPoint(new Vector2(0f, 1.5f));
		curve.AddPoint(new Vector2(0.3f, 1f));
		curve.AddPoint(new Vector2(1f, 0.2f));
		scaleOverLife.Curve = curve;

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 4f,
			Direction = new Vector3(0, 0, 0),
			Spread = 180f,
			InitialVelocityMin = 60f,
			InitialVelocityMax = 120f,
			Gravity = new Vector3(0, 30, 0),
			ScaleMin = 0.6f,
			ScaleMax = 1.5f,
			ColorRamp = gradient,
			ScaleCurve = scaleOverLife,
			DampingMin = 40f,
			DampingMax = 80f,
		};
		particles.ProcessMaterial = mat;
		particles.Emitting = true;

		root.AddChild(particles);

		// Flash de lumière dorée
		PointLight2D light = CreateFlashLight(position, new Color(1f, 0.92f, 0.5f), 1.2f, 0.4f);
		root.AddChild(light);
		light.Position = Vector2.Zero;

		var timer = new Timer { WaitTime = 1f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	// === Textures procédurales ===

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
