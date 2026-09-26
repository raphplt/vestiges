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
	private static Texture2D _dissolutionFrame1;
	private static Texture2D _dissolutionFrame2;
	private static Texture2D _dissolutionFrame3;
	private static Texture2D _dissolutionFrame4;
	private static Texture2D _bloodSplatterTex;

	private static Texture2D ExplosionFrame1 => _explosionFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f1.png");
	private static Texture2D ExplosionFrame2 => _explosionFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f2.png");
	private static Texture2D ExplosionFrame3 => _explosionFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f3.png");
	private static Texture2D ExplosionFrame4 => _explosionFrame4 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f4.png");
	private static Texture2D ExplosionFrame5 => _explosionFrame5 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_explosion_f5.png");
	private static Texture2D DashTrailFrame1 => _dashTrailFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dash_trail_f1.png");
	private static Texture2D DashTrailFrame2 => _dashTrailFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dash_trail_f2.png");
	private static Texture2D DashTrailFrame3 => _dashTrailFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dash_trail_f3.png");
	private static Texture2D DissolutionFrame1 => _dissolutionFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dissolution_f1.png");
	private static Texture2D DissolutionFrame2 => _dissolutionFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dissolution_f2.png");
	private static Texture2D DissolutionFrame3 => _dissolutionFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dissolution_f3.png");
	private static Texture2D DissolutionFrame4 => _dissolutionFrame4 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_dissolution_f4.png");
	private static Texture2D BloodSplatterTex => _bloodSplatterTex ??= GD.Load<Texture2D>("res://assets/vfx/vfx_blood_splatter.png");

	// =========================================================================
	// === Orbe XP : GPUParticles2D qui suit l'orbe ===
	// =========================================================================

	public static GpuParticles2D CreateXpOrbGlow()
	{
		if (CurrentParticleLevel == ParticleLevel.Off)
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
	// === XP collect burst (one-shot flash à la collecte) ===
	// =========================================================================

	public static Node2D CreateXpCollectBurst(Vector2 position)
	{
		if (CurrentParticleLevel == ParticleLevel.Off)
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

	// =========================================================================
	// === Level up burst (explosion radiale dorée) ===
	// =========================================================================

	public static Node2D CreateLevelUpBurst(Vector2 position)
	{
		if (CurrentParticleLevel == ParticleLevel.Off)
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
	// === Dissolution VFX — particules noires 4 frames ===
	// =========================================================================

	/// <summary>
	/// Crée un VFX de dissolution sprite (particules noires iridescentes).
	/// Complète le shader dissolve existant sur les ennemis.
	/// </summary>
	public static Node2D CreateDissolutionVfx(Vector2 position)
	{
		if (CurrentParticleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		SpriteFrames frames = new();
		frames.AddAnimation("dissolve");
		frames.SetAnimationSpeed("dissolve", 8);
		frames.SetAnimationLoopMode("dissolve", SpriteFrames.LoopMode.None);
		frames.AddFrame("dissolve", DissolutionFrame1);
		frames.AddFrame("dissolve", DissolutionFrame2);
		frames.AddFrame("dissolve", DissolutionFrame3);
		frames.AddFrame("dissolve", DissolutionFrame4);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Position = new Vector2(0, -8),
		};
		sprite.Play("dissolve");
		root.AddChild(sprite);

		sprite.TreeEntered += () =>
		{
			Tween tween = sprite.CreateTween();
			tween.TweenProperty(sprite, "position:y", -16f, 0.5f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		};

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

	// =========================================================================
	// === Iridescent Fluid Splatter (Sang des Aberrations) ===
	// =========================================================================

	private static ShaderMaterial _iridescentMaterial;

	/// <summary>
	/// Crée une flaque de sang iridescent (sprite basique + pixel shader iridescent_fluid.gdshader).
	/// S'étend et se "dissout" après quelques secondes.
	/// </summary>
	public static Node2D CreateIridescentBloodSplatter(Vector2 position, float scale = 1.0f)
	{
		if (CurrentParticleLevel == ParticleLevel.Off)
			return null;

		// Flaque au sol : sous les entités, que le tri en Y de la scène ne doit pas faire passer devant des pieds.
		Node2D root = new() { GlobalPosition = position, ZIndex = -1 };

		if (_iridescentMaterial == null && ResourceLoader.Exists("res://assets/shaders/iridescent_fluid.gdshader"))
		{
			Shader shader = GD.Load<Shader>("res://assets/shaders/iridescent_fluid.gdshader");
			_iridescentMaterial = new ShaderMaterial { Shader = shader };
		}

		// On utilise le pixel art asset généré au lieu de la texture cercle basique
		Sprite2D splatterSprite = new()
		{
			Texture = BloodSplatterTex,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(0.5f, 0.25f), // Commence petit
			Modulate = new Color(1f, 1f, 1f, 0.9f)
		};

		if (_iridescentMaterial != null)
		{
			splatterSprite.Material = _iridescentMaterial;
		}

		root.AddChild(splatterSprite);

		// Animation de flaque qui s'étend, puis disparaît lentement
		splatterSprite.TreeEntered += () =>
		{
			Tween tween = splatterSprite.CreateTween();
			tween.SetParallel();
			
			// Étirement (spawn) - The final scale will be around 1.0 based on passed `scale` parameter.
			// Flaque couchée au sol : deux fois plus large que haute.
			tween.TweenProperty(splatterSprite, "scale", new Vector2(scale, scale * 0.5f), 0.3f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
				
			// Disparition lente (retour au néant)
			tween.Chain().TweenProperty(splatterSprite, "modulate:a", 0f, 4.0f)
				  .SetDelay(1.0f);
			
			tween.Chain().TweenCallback(Callable.From(root.QueueFree));
		};

		return root;
	}
}
