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

	// --- Sprites VFX pixel art (chargés une seule fois) ---
	private static Texture2D _slashFrame1;
	private static Texture2D _slashFrame2;
	private static Texture2D _slashFrame3;
	private static Texture2D _masseFrame1;
	private static Texture2D _masseFrame2;
	private static Texture2D _masseFrame3;
	private static Texture2D _hitFlashTex;
	private static Texture2D _flammeFrame1;
	private static Texture2D _flammeFrame2;
	private static Texture2D _flammeFrame3;
	private static Texture2D _etincelleCraftTex;
	private static Texture2D _thrustFrame1;
	private static Texture2D _thrustFrame2;
	private static Texture2D _thrustFrame3;
	private static Texture2D _explosionFrame1;
	private static Texture2D _explosionFrame2;
	private static Texture2D _explosionFrame3;
	private static Texture2D _explosionFrame4;
	private static Texture2D _explosionFrame5;
	private static Texture2D _dashTrailFrame1;
	private static Texture2D _dashTrailFrame2;
	private static Texture2D _dashTrailFrame3;
	private static Texture2D _dissolutionFrame1;
	private static Texture2D _dissolutionFrame2;
	private static Texture2D _dissolutionFrame3;
	private static Texture2D _dissolutionFrame4;
	private static Texture2D _impactFrame1;
	private static Texture2D _impactFrame2;
	private static Texture2D _impactFrame3;
	private static Texture2D _auraEssenceFrame1;
	private static Texture2D _auraEssenceFrame2;
	private static Texture2D _auraEssenceFrame3;
	private static Texture2D _orbXpFrame2;
	private static Texture2D _orbEssenceFrame1;
	private static Texture2D _orbEssenceFrame2;
	private static Texture2D _orbEssenceFrame3;
	private static Texture2D _fouetFrame1;
	private static Texture2D _fouetFrame2;
	private static Texture2D _fouetFrame3;
	private static Texture2D _fouetFrame4;

	private static Texture2D SlashFrame1 => _slashFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_slash_f1.png");
	private static Texture2D SlashFrame2 => _slashFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_slash_f2.png");
	private static Texture2D SlashFrame3 => _slashFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_slash_f3.png");
	private static Texture2D MasseFrame1 => _masseFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_masse_impact_f1.png");
	private static Texture2D MasseFrame2 => _masseFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_masse_impact_f2.png");
	private static Texture2D MasseFrame3 => _masseFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_masse_impact_f3.png");
	private static Texture2D HitFlashTex => _hitFlashTex ??= GD.Load<Texture2D>("res://assets/vfx/vfx_hit_flash.png");
	private static Texture2D FlammeFrame1 => _flammeFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_flamme_torche_f1.png");
	private static Texture2D FlammeFrame2 => _flammeFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_flamme_torche_f2.png");
	private static Texture2D FlammeFrame3 => _flammeFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_flamme_torche_f3.png");
	private static Texture2D EtincelleCraftTex => _etincelleCraftTex ??= GD.Load<Texture2D>("res://assets/vfx/vfx_etincelle_craft.png");
	private static Texture2D ThrustFrame1 => _thrustFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_thrust_f1.png");
	private static Texture2D ThrustFrame2 => _thrustFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_thrust_f2.png");
	private static Texture2D ThrustFrame3 => _thrustFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_thrust_f3.png");
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
	private static Texture2D ImpactFrame1 => _impactFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_impact_f1.png");
	private static Texture2D ImpactFrame2 => _impactFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_impact_f2.png");
	private static Texture2D ImpactFrame3 => _impactFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_impact_f3.png");
	private static Texture2D AuraEssenceFrame1 => _auraEssenceFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_aura_essence_f1.png");
	private static Texture2D AuraEssenceFrame2 => _auraEssenceFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_aura_essence_f2.png");
	private static Texture2D AuraEssenceFrame3 => _auraEssenceFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_aura_essence_f3.png");
	private static Texture2D OrbXpFrame2 => _orbXpFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_orb_xp_f2.png");
	private static Texture2D OrbEssenceFrame1 => _orbEssenceFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_orb_essence_f1.png");
	private static Texture2D OrbEssenceFrame2 => _orbEssenceFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_orb_essence_f2.png");
	private static Texture2D OrbEssenceFrame3 => _orbEssenceFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_orb_essence_f3.png");
	private static Texture2D FouetFrame1 => _fouetFrame1 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_fouet_f1.png");
	private static Texture2D FouetFrame2 => _fouetFrame2 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_fouet_f2.png");
	private static Texture2D FouetFrame3 => _fouetFrame3 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_fouet_f3.png");
	private static Texture2D FouetFrame4 => _fouetFrame4 ??= GD.Load<Texture2D>("res://assets/vfx/vfx_fouet_f4.png");

	// =========================================================================
	// === Orbe XP : GPUParticles2D qui suit l'orbe ===
	// =========================================================================

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

	// =========================================================================
	// === Impact de projectile (particules complémentaires au sprite) ===
	// =========================================================================

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

	// =========================================================================
	// === Flamme (torche, feu de camp, foyer) — particules procédurales ===
	// =========================================================================

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

	// =========================================================================
	// === Flamme sprite animée (3 frames pixel art) — pour torches ===
	// =========================================================================

	/// <summary>
	/// Crée un AnimatedSprite2D avec les 3 frames de flamme pixel art.
	/// À utiliser en complément (ou remplacement) des particules procédurales.
	/// </summary>
	public static AnimatedSprite2D CreateFlameSprite()
	{
		SpriteFrames frames = new();
		frames.AddAnimation("burn");
		frames.SetAnimationSpeed("burn", 6);
		frames.SetAnimationLoop("burn", true);
		frames.AddFrame("burn", FlammeFrame1);
		frames.AddFrame("burn", FlammeFrame2);
		frames.AddFrame("burn", FlammeFrame3);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		sprite.Play("burn");

		return sprite;
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
	// === Hit Flash — sprite pixel art qui apparaît sur l'ennemi touché ===
	// =========================================================================

	/// <summary>
	/// Crée un sprite de hit flash (étoile/burst pixel art) qui scale up et fade.
	/// À ajouter à la scène principale à la position de l'impact.
	/// </summary>
	public static Node2D CreateHitFlashSprite(Vector2 position)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		Sprite2D sprite = new()
		{
			GlobalPosition = position,
			Texture = HitFlashTex,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(0.5f, 0.5f),
		};

		sprite.TreeEntered += () =>
		{
			Tween tween = sprite.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(sprite, "scale", new Vector2(1.5f, 1.5f), 0.08f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(sprite, "modulate:a", 0f, 0.12f);
			tween.Chain().TweenCallback(Callable.From(sprite.QueueFree));
		};

		return sprite;
	}

	// =========================================================================
	// === Slash VFX — sprite animé 3 frames + particules ===
	// =========================================================================

	/// <summary>
	/// Crée un VFX de slash combinant le sprite animé pixel art et des particules.
	/// Le sprite est orienté selon la direction de l'attaque.
	/// </summary>
	public static Node2D CreateSlashVfx(Vector2 position, Vector2 direction, float range, float arcAngle, Color color)
	{
		var root = new Node2D
		{
			GlobalPosition = position + direction * 8f,
			Rotation = direction.Angle(),
		};

		// --- Sprite animé de slash (3 frames) ---
		SpriteFrames slashFrames = new();
		slashFrames.AddAnimation("slash");
		slashFrames.SetAnimationSpeed("slash", 24);
		slashFrames.SetAnimationLoop("slash", false);
		slashFrames.AddFrame("slash", SlashFrame1);
		slashFrames.AddFrame("slash", SlashFrame2);
		slashFrames.AddFrame("slash", SlashFrame3);

		AnimatedSprite2D slashSprite = new()
		{
			SpriteFrames = slashFrames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SelfModulate = new Color(color, 0.9f),
		};
		slashSprite.Play("slash");
		root.AddChild(slashSprite);

		// --- Particules complémentaires en arc (si pas Off) ---
		if (_particleLevel != ParticleLevel.Off)
		{
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
		}

		// Auto-nettoyage
		var timer = new Timer { WaitTime = 0.4f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	// =========================================================================
	// === Masse Impact VFX — sprite animé 3 frames (onde de choc) ===
	// =========================================================================

	/// <summary>
	/// Crée un VFX d'impact de masse/marteau avec l'animation sprite 3 frames.
	/// L'onde de choc s'expand et fade out.
	/// </summary>
	public static Node2D CreateMasseImpactVfx(Vector2 position, Color color)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		SpriteFrames impactFrames = new();
		impactFrames.AddAnimation("impact");
		impactFrames.SetAnimationSpeed("impact", 18);
		impactFrames.SetAnimationLoop("impact", false);
		impactFrames.AddFrame("impact", MasseFrame1);
		impactFrames.AddFrame("impact", MasseFrame2);
		impactFrames.AddFrame("impact", MasseFrame3);

		AnimatedSprite2D impactSprite = new()
		{
			SpriteFrames = impactFrames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SelfModulate = new Color(color, 0.85f),
		};
		impactSprite.Play("impact");
		root.AddChild(impactSprite);

		// Tween de scale up + fade
		impactSprite.TreeEntered += () =>
		{
			Tween tween = impactSprite.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(impactSprite, "scale", new Vector2(2f, 2f), 0.2f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(impactSprite, "modulate:a", 0f, 0.25f);
		};

		// Particules complémentaires (petits débris)
		var particles = new GpuParticles2D
		{
			Amount = ScaleAmount(8),
			Lifetime = 0.3f,
			Explosiveness = 1f,
			OneShot = true,
			Texture = SparkTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 4f,
			Direction = new Vector3(0, 0, 0),
			Spread = 180f,
			InitialVelocityMin = 40f,
			InitialVelocityMax = 80f,
			Gravity = new Vector3(0, 60, 0),
			ScaleMin = 0.4f,
			ScaleMax = 1.0f,
			Color = new Color(color, 0.7f),
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
	// === Étincelle de Craft — sprite + particules ===
	// =========================================================================

	/// <summary>
	/// Crée un effet d'étincelles pour la station de craft.
	/// Combine le sprite pixel art avec des particules dorées.
	/// </summary>
	public static Node2D CreateCraftSparkVfx(Vector2 position)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		// Sprite d'étincelle pixel art
		Sprite2D sparkSprite = new()
		{
			Texture = EtincelleCraftTex,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(0.8f, 0.8f),
		};
		root.AddChild(sparkSprite);

		// Animation : pop + fade
		sparkSprite.TreeEntered += () =>
		{
			Tween tween = sparkSprite.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(sparkSprite, "scale", new Vector2(1.2f, 1.2f), 0.06f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(sparkSprite, "modulate:a", 0f, 0.3f);
		};

		// Petites particules dorées complémentaires
		var particles = new GpuParticles2D
		{
			Amount = ScaleAmount(4),
			Lifetime = 0.35f,
			Explosiveness = 0.8f,
			OneShot = true,
			Texture = SparkTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 3f,
			Direction = new Vector3(0, -1, 0),
			Spread = 60f,
			InitialVelocityMin = 15f,
			InitialVelocityMax = 35f,
			Gravity = new Vector3(0, 30, 0),
			ScaleMin = 0.3f,
			ScaleMax = 0.7f,
			Color = new Color(0.83f, 0.66f, 0.26f, 0.9f), // Or Foyer
		};
		particles.ProcessMaterial = mat;
		particles.Emitting = true;
		root.AddChild(particles);

		var timer = new Timer { WaitTime = 0.6f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	// =========================================================================
	// === Trail de projectile ===
	// =========================================================================

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

	// =========================================================================
	// === XP collect burst (one-shot flash à la collecte) ===
	// =========================================================================

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

	// =========================================================================
	// === Level up burst (explosion radiale dorée) ===
	// =========================================================================

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

	// =========================================================================
	// === Thrust Lance VFX — sprite animé 3 frames ===
	// =========================================================================

	/// <summary>
	/// Crée un VFX de thrust de lance (ligne directionnelle qui s'étend).
	/// Orienté selon la direction de l'attaque.
	/// </summary>
	public static Node2D CreateThrustVfx(Vector2 position, Vector2 direction, Color color)
	{
		var root = new Node2D
		{
			GlobalPosition = position + direction * 10f,
			Rotation = direction.Angle(),
		};

		SpriteFrames thrustFrames = new();
		thrustFrames.AddAnimation("thrust");
		thrustFrames.SetAnimationSpeed("thrust", 20);
		thrustFrames.SetAnimationLoop("thrust", false);
		thrustFrames.AddFrame("thrust", ThrustFrame1);
		thrustFrames.AddFrame("thrust", ThrustFrame2);
		thrustFrames.AddFrame("thrust", ThrustFrame3);

		AnimatedSprite2D thrustSprite = new()
		{
			SpriteFrames = thrustFrames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SelfModulate = new Color(color, 0.9f),
		};
		thrustSprite.Play("thrust");
		root.AddChild(thrustSprite);

		if (_particleLevel != ParticleLevel.Off)
		{
			var particles = new GpuParticles2D
			{
				Amount = ScaleAmount(4),
				Lifetime = 0.12f,
				Explosiveness = 1f,
				OneShot = true,
				Texture = SparkTexture,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			};

			var mat = new ParticleProcessMaterial
			{
				EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
				EmissionSphereRadius = 2f,
				Direction = new Vector3(1, 0, 0),
				Spread = 15f,
				InitialVelocityMin = 40f,
				InitialVelocityMax = 70f,
				Gravity = Vector3.Zero,
				ScaleMin = 0.3f,
				ScaleMax = 0.8f,
				Color = new Color(color, 0.8f),
				DampingMin = 60f,
				DampingMax = 100f,
			};
			particles.ProcessMaterial = mat;
			particles.Emitting = true;
			root.AddChild(particles);
		}

		var timer = new Timer { WaitTime = 0.3f, OneShot = true, Autostart = true };
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
		frames.SetAnimationLoop("explode", false);
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

		if (_particleLevel != ParticleLevel.Off)
		{
			var particles = new GpuParticles2D
			{
				Amount = ScaleAmount(12),
				Lifetime = 0.4f,
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
				ScaleMin = 0.5f,
				ScaleMax = 1.5f,
				Color = new Color(0.88f, 0.48f, 0.22f, 0.9f),
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
	// === Dash Trail VFX — afterimage 3 frames ===
	// =========================================================================

	/// <summary>
	/// Crée une afterimage de dash (silhouette cyan translucide qui fade).
	/// Placer à la position du joueur à chaque frame du dash.
	/// </summary>
	public static Node2D CreateDashTrailVfx(Vector2 position)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		SpriteFrames frames = new();
		frames.AddAnimation("fade");
		frames.SetAnimationSpeed("fade", 10);
		frames.SetAnimationLoop("fade", false);
		frames.AddFrame("fade", DashTrailFrame1);
		frames.AddFrame("fade", DashTrailFrame2);
		frames.AddFrame("fade", DashTrailFrame3);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		sprite.Play("fade");
		root.AddChild(sprite);

		var timer = new Timer { WaitTime = 0.35f, OneShot = true, Autostart = true };
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
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		SpriteFrames frames = new();
		frames.AddAnimation("dissolve");
		frames.SetAnimationSpeed("dissolve", 8);
		frames.SetAnimationLoop("dissolve", false);
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
	// === Impact animé 3 frames (version enrichie) ===
	// =========================================================================

	/// <summary>
	/// Crée un impact de projectile animé en 3 frames au lieu d'un sprite statique.
	/// </summary>
	public static Node2D CreateAnimatedImpactVfx(Vector2 position, Color color)
	{
		var root = new Node2D { GlobalPosition = position };

		SpriteFrames frames = new();
		frames.AddAnimation("impact");
		frames.SetAnimationSpeed("impact", 20);
		frames.SetAnimationLoop("impact", false);
		frames.AddFrame("impact", ImpactFrame1);
		frames.AddFrame("impact", ImpactFrame2);
		frames.AddFrame("impact", ImpactFrame3);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SelfModulate = new Color(color, 0.9f),
		};
		sprite.Play("impact");
		root.AddChild(sprite);

		var timer = new Timer { WaitTime = 0.25f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	// =========================================================================
	// === Aura d'Essence — overlay animé 3 frames ===
	// =========================================================================

	/// <summary>
	/// Crée une aura d'Essence animée autour du joueur.
	/// À ajouter comme enfant du nœud joueur.
	/// </summary>
	public static AnimatedSprite2D CreateAuraEssenceSprite()
	{
		SpriteFrames frames = new();
		frames.AddAnimation("pulse");
		frames.SetAnimationSpeed("pulse", 6);
		frames.SetAnimationLoop("pulse", true);
		frames.AddFrame("pulse", AuraEssenceFrame1);
		frames.AddFrame("pulse", AuraEssenceFrame2);
		frames.AddFrame("pulse", AuraEssenceFrame3);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		sprite.Play("pulse");

		return sprite;
	}

	// =========================================================================
	// === Orbe Essence VFX — projectile homing animé 3 frames loop ===
	// =========================================================================

	/// <summary>
	/// Crée un VFX d'orbe d'Essence animé (3 frames en boucle).
	/// Pour les projectiles homing du bâton d'Essence et armes similaires.
	/// </summary>
	public static AnimatedSprite2D CreateOrbEssenceSprite()
	{
		SpriteFrames frames = new();
		frames.AddAnimation("pulse");
		frames.SetAnimationSpeed("pulse", 8);
		frames.SetAnimationLoop("pulse", true);
		frames.AddFrame("pulse", OrbEssenceFrame1);
		frames.AddFrame("pulse", OrbEssenceFrame2);
		frames.AddFrame("pulse", OrbEssenceFrame3);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};
		sprite.Play("pulse");

		return sprite;
	}

	// =========================================================================
	// === Fouet VFX — frappe circulaire 4 frames ===
	// =========================================================================

	/// <summary>
	/// Crée un VFX de frappe circulaire de fouet (4 frames).
	/// Animation rapide de la lanière qui claque en cercle.
	/// </summary>
	public static Node2D CreateFouetVfx(Vector2 position, Color color)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

		SpriteFrames frames = new();
		frames.AddAnimation("whip");
		frames.SetAnimationSpeed("whip", 20);
		frames.SetAnimationLoop("whip", false);
		frames.AddFrame("whip", FouetFrame1);
		frames.AddFrame("whip", FouetFrame2);
		frames.AddFrame("whip", FouetFrame3);
		frames.AddFrame("whip", FouetFrame4);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SelfModulate = new Color(color, 0.9f),
		};
		sprite.Play("whip");
		root.AddChild(sprite);

		// Particules complémentaires (étincelles du claquement)
		if (_particleLevel != ParticleLevel.Off)
		{
			var particles = new GpuParticles2D
			{
				Amount = ScaleAmount(6),
				Lifetime = 0.2f,
				Explosiveness = 0.9f,
				OneShot = true,
				Texture = SparkTexture,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			};

			var mat = new ParticleProcessMaterial
			{
				EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
				EmissionSphereRadius = 10f,
				Direction = new Vector3(0, 0, 0),
				Spread = 180f,
				InitialVelocityMin = 30f,
				InitialVelocityMax = 60f,
				Gravity = new Vector3(0, 20, 0),
				ScaleMin = 0.3f,
				ScaleMax = 0.8f,
				Color = new Color(color, 0.7f),
				DampingMin = 40f,
				DampingMax = 80f,
			};
			particles.ProcessMaterial = mat;
			particles.Emitting = true;
			root.AddChild(particles);
		}

		var timer = new Timer { WaitTime = 0.4f, OneShot = true, Autostart = true };
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
