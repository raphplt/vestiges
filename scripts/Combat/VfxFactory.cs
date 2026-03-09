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
	private static Texture2D _bloodSplatterTex;

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
	private static Texture2D BloodSplatterTex => _bloodSplatterTex ??= GD.Load<Texture2D>("res://assets/vfx/vfx_blood_splatter.png");

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
			Amount = ScaleAmount(4),
			Lifetime = 0.2f,
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
			ScaleMin = 0.6f,
			ScaleMax = 1.4f,
			Color = new Color(color, 0.6f),
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

		// --- Sprite animé de slash (3 frames) — 16fps pour lisibilité pixel art ---
		SpriteFrames slashFrames = new();
		slashFrames.AddAnimation("slash");
		slashFrames.SetAnimationSpeed("slash", 16);
		slashFrames.SetAnimationLoop("slash", false);
		slashFrames.AddFrame("slash", SlashFrame1);
		slashFrames.AddFrame("slash", SlashFrame2);
		slashFrames.AddFrame("slash", SlashFrame3);

		AnimatedSprite2D slashSprite = new()
		{
			SpriteFrames = slashFrames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(1.5f, 1.5f),
			Modulate = new Color(1f, 1f, 1f, 0.9f),
		};
		slashSprite.Play("slash");
		root.AddChild(slashSprite);

		// --- Particules complémentaires en arc (réduites, discrètes) ---
		if (_particleLevel != ParticleLevel.Off)
		{
			int baseAmount = Mathf.RoundToInt(Mathf.Clamp(arcAngle / 30f, 4, 12));
			var particles = new GpuParticles2D
			{
				Amount = ScaleAmount(Mathf.RoundToInt(baseAmount * 0.6f)),
				Lifetime = 0.12f,
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
				ScaleMin = 0.8f,
				ScaleMax = 1.8f,
				Color = new Color(color, 0.6f),
				DampingMin = 80f,
				DampingMax = 120f,
			};
			particles.ProcessMaterial = mat;
			particles.Emitting = true;

			root.AddChild(particles);
		}

		// Auto-nettoyage (0.5s pour laisser le sprite être lisible)
		var timer = new Timer { WaitTime = 0.5f, OneShot = true, Autostart = true };
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

		// --- Sprite animé masse (3 frames) — 12fps pour lisibilité pixel art ---
		SpriteFrames impactFrames = new();
		impactFrames.AddAnimation("impact");
		impactFrames.SetAnimationSpeed("impact", 12);
		impactFrames.SetAnimationLoop("impact", false);
		impactFrames.AddFrame("impact", MasseFrame1);
		impactFrames.AddFrame("impact", MasseFrame2);
		impactFrames.AddFrame("impact", MasseFrame3);

		AnimatedSprite2D impactSprite = new()
		{
			SpriteFrames = impactFrames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(1.5f, 1.5f),
			Modulate = new Color(1f, 1f, 1f, 0.9f),
		};
		impactSprite.Play("impact");
		root.AddChild(impactSprite);

		// Tween de scale up + fade (plus lent)
		impactSprite.TreeEntered += () =>
		{
			Tween tween = impactSprite.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(impactSprite, "scale", new Vector2(2.5f, 2.5f), 0.3f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(impactSprite, "modulate:a", 0f, 0.35f);
		};

		// Particules complémentaires (réduites — débris discrets)
		var particles = new GpuParticles2D
		{
			Amount = ScaleAmount(5),
			Lifetime = 0.25f,
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
			ScaleMin = 0.5f,
			ScaleMax = 1.2f,
			Color = new Color(color, 0.5f),
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

		// --- Sprite animé thrust (3 frames) — 14fps pour lisibilité pixel art ---
		SpriteFrames thrustFrames = new();
		thrustFrames.AddAnimation("thrust");
		thrustFrames.SetAnimationSpeed("thrust", 14);
		thrustFrames.SetAnimationLoop("thrust", false);
		thrustFrames.AddFrame("thrust", ThrustFrame1);
		thrustFrames.AddFrame("thrust", ThrustFrame2);
		thrustFrames.AddFrame("thrust", ThrustFrame3);

		AnimatedSprite2D thrustSprite = new()
		{
			SpriteFrames = thrustFrames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(1.5f, 1.5f),
			Modulate = new Color(1f, 1f, 1f, 0.9f),
		};
		thrustSprite.Play("thrust");
		root.AddChild(thrustSprite);

		if (_particleLevel != ParticleLevel.Off)
		{
			var particles = new GpuParticles2D
			{
				Amount = ScaleAmount(3),
				Lifetime = 0.1f,
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
				ScaleMin = 0.4f,
				ScaleMax = 1.0f,
				Color = new Color(color, 0.5f),
				DampingMin = 60f,
				DampingMax = 100f,
			};
			particles.ProcessMaterial = mat;
			particles.Emitting = true;
			root.AddChild(particles);
		}

		var timer = new Timer { WaitTime = 0.5f, OneShot = true, Autostart = true };
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
			Scale = new Vector2(1.5f, 1.5f),
			Modulate = new Color(1f, 1f, 1f, 0.9f),
		};
		sprite.Play("impact");
		root.AddChild(sprite);

		var timer = new Timer { WaitTime = 0.3f, OneShot = true, Autostart = true };
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

		// --- Sprite animé fouet (4 frames) — 14fps pour lisibilité pixel art ---
		SpriteFrames frames = new();
		frames.AddAnimation("whip");
		frames.SetAnimationSpeed("whip", 14);
		frames.SetAnimationLoop("whip", false);
		frames.AddFrame("whip", FouetFrame1);
		frames.AddFrame("whip", FouetFrame2);
		frames.AddFrame("whip", FouetFrame3);
		frames.AddFrame("whip", FouetFrame4);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(1.5f, 1.5f),
			Modulate = new Color(1f, 1f, 1f, 0.9f),
		};
		sprite.Play("whip");
		root.AddChild(sprite);

		// Particules complémentaires (réduites — étincelles discrètes)
		if (_particleLevel != ParticleLevel.Off)
		{
			var particles = new GpuParticles2D
			{
				Amount = ScaleAmount(4),
				Lifetime = 0.15f,
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
				ScaleMin = 0.4f,
				ScaleMax = 1.0f,
				Color = new Color(color, 0.5f),
				DampingMin = 40f,
				DampingMax = 80f,
			};
			particles.ProcessMaterial = mat;
			particles.Emitting = true;
			root.AddChild(particles);
		}

		var timer = new Timer { WaitTime = 0.5f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	// =========================================================================
	// === Tier 3+ Weapon VFX — sprites pixel art procéduraux ===
	// =========================================================================

	// --- Cache static pour les sprites tier 3+ ---
	private static ImageTexture[] _bellWaveFrames;
	private static ImageTexture[] _chainLightningFrames;
	private static ImageTexture[] _timeDistortionFrames;
	private static ImageTexture _voidSlashTex;
	private static ImageTexture[] _echoFrames;

	/// <summary>
	/// Onde sonore concentrique pour La Cloche de l'Institutrice (teachers_bell).
	/// 3 frames d'anneaux concentriques qui s'expandent, 24x24 px.
	/// </summary>
	public static Node2D CreateBellWaveVfx(Vector2 position, Color color)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		_bellWaveFrames ??= GenerateBellWaveFrames();

		var root = new Node2D { GlobalPosition = position };

		SpriteFrames frames = new();
		frames.AddAnimation("wave");
		frames.SetAnimationSpeed("wave", 10);
		frames.SetAnimationLoop("wave", false);
		for (int i = 0; i < _bellWaveFrames.Length; i++)
			frames.AddFrame("wave", _bellWaveFrames[i]);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(2f, 2f),
		};
		sprite.Play("wave");
		root.AddChild(sprite);

		sprite.TreeEntered += () =>
		{
			Tween tween = sprite.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(sprite, "scale", new Vector2(3.5f, 3.5f), 0.4f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(sprite, "modulate:a", 0f, 0.5f);
		};

		var timer = new Timer { WaitTime = 0.6f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	private static ImageTexture[] GenerateBellWaveFrames()
	{
		const int size = 24;
		const int center = size / 2;
		var result = new ImageTexture[3];
		int[][] radii = { new[] { 3, 4 }, new[] { 5, 6, 8 }, new[] { 7, 9, 10, 11 } };
		Color ring = new(0.85f, 0.75f, 0.45f, 1f); // Or pâle
		Color ringFade = new(0.85f, 0.75f, 0.45f, 0.5f);

		for (int f = 0; f < 3; f++)
		{
			Image img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
			foreach (int r in radii[f])
			{
				Color c = r == radii[f][^1] ? ringFade : ring;
				DrawPixelCircle(img, center, center, r, c);
			}
			result[f] = ImageTexture.CreateFromImage(img);
		}
		return result;
	}

	/// <summary>
	/// Éclair de chaîne entre ennemis pour La Chaîne des Noms (chain_of_names).
	/// 2 frames de zigzag pixel art électrique, 24x12 px.
	/// </summary>
	public static Node2D CreateChainLightningVfx(Vector2 from, Vector2 to, Color color)
	{
		_chainLightningFrames ??= GenerateChainLightningFrames();

		var root = new Node2D { GlobalPosition = from };
		Vector2 delta = to - from;
		root.Rotation = delta.Angle();

		SpriteFrames frames = new();
		frames.AddAnimation("chain");
		frames.SetAnimationSpeed("chain", 12);
		frames.SetAnimationLoop("chain", false);
		for (int i = 0; i < _chainLightningFrames.Length; i++)
			frames.AddFrame("chain", _chainLightningFrames[i]);

		float stretchX = delta.Length() / 24f;
		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(Mathf.Max(stretchX, 1f), 2f),
			Modulate = new Color(1f, 1f, 1f, 0.9f),
		};
		sprite.Play("chain");
		root.AddChild(sprite);

		var timer = new Timer { WaitTime = 0.3f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	private static ImageTexture[] GenerateChainLightningFrames()
	{
		const int w = 24, h = 12;
		var result = new ImageTexture[2];
		Color bright = new(0.7f, 0.85f, 1f, 1f);
		Color core = new(1f, 1f, 1f, 1f);

		for (int f = 0; f < 2; f++)
		{
			Image img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
			int y = h / 2;
			int offset = f * 2;
			for (int x = 0; x < w; x++)
			{
				// Zigzag avec décalage par frame
				int dy = ((x + offset) % 4 < 2) ? -1 : 1;
				if (x % 3 == 0) dy *= 2;
				int py = Mathf.Clamp(y + dy, 1, h - 2);
				SetPixelSafe(img, x, py, core, w, h);
				SetPixelSafe(img, x, py - 1, bright, w, h);
				SetPixelSafe(img, x, py + 1, bright, w, h);
			}
			result[f] = ImageTexture.CreateFromImage(img);
		}
		return result;
	}

	/// <summary>
	/// Distorsion temporelle pour L'Aiguille de l'Horloge (clock_hand).
	/// 3 frames d'anneaux bleu/violet qui se contractent, 32x32 px.
	/// </summary>
	public static Node2D CreateTimeDistortionVfx(Vector2 position)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		_timeDistortionFrames ??= GenerateTimeDistortionFrames();

		var root = new Node2D { GlobalPosition = position };

		SpriteFrames frames = new();
		frames.AddAnimation("distort");
		frames.SetAnimationSpeed("distort", 8);
		frames.SetAnimationLoop("distort", false);
		for (int i = 0; i < _timeDistortionFrames.Length; i++)
			frames.AddFrame("distort", _timeDistortionFrames[i]);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(2.5f, 2.5f),
		};
		sprite.Play("distort");
		root.AddChild(sprite);

		sprite.TreeEntered += () =>
		{
			Tween tween = sprite.CreateTween();
			tween.TweenProperty(sprite, "modulate:a", 0f, 0.6f)
				.SetDelay(0.2f);
		};

		var timer = new Timer { WaitTime = 0.8f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	private static ImageTexture[] GenerateTimeDistortionFrames()
	{
		const int size = 32;
		const int center = size / 2;
		var result = new ImageTexture[3];
		Color outer = new(0.4f, 0.3f, 0.7f, 0.7f);    // Violet
		Color mid = new(0.3f, 0.5f, 0.9f, 0.8f);       // Bleu
		Color inner = new(0.6f, 0.7f, 1f, 0.9f);        // Bleu clair

		// Frame 0: grand anneau extérieur, Frame 1: moyen, Frame 2: anneau intérieur serré
		int[][] rings = { new[] { 14, 12 }, new[] { 11, 9, 7 }, new[] { 8, 6, 4 } };
		Color[][] colors = {
			new[] { outer, mid },
			new[] { outer, mid, inner },
			new[] { mid, inner, inner },
		};

		for (int f = 0; f < 3; f++)
		{
			Image img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
			for (int r = 0; r < rings[f].Length; r++)
				DrawPixelCircle(img, center, center, rings[f][r], colors[f][r]);
			// Aiguille d'horloge au centre
			for (int i = 0; i < 5; i++)
				SetPixelSafe(img, center, center - i, inner, size, size);
			result[f] = ImageTexture.CreateFromImage(img);
		}
		return result;
	}

	/// <summary>
	/// Slash du Vide pour Tranchant du Vide (void_edge).
	/// Trainée noire iridescente single-frame, 20x8 px.
	/// </summary>
	public static Node2D CreateVoidSlashVfx(Vector2 position, Vector2 direction, Color color)
	{
		_voidSlashTex ??= GenerateVoidSlashTexture();

		var root = new Node2D
		{
			GlobalPosition = position + direction * 8f,
			Rotation = direction.Angle(),
		};

		Sprite2D sprite = new()
		{
			Texture = _voidSlashTex,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(2f, 2f),
		};
		root.AddChild(sprite);

		sprite.TreeEntered += () =>
		{
			Tween tween = sprite.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(sprite, "scale", new Vector2(3f, 2.5f), 0.15f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(sprite, "modulate:a", 0f, 0.4f);
		};

		var timer = new Timer { WaitTime = 0.5f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	private static ImageTexture GenerateVoidSlashTexture()
	{
		const int w = 20, h = 8;
		Image img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		Color voidCore = new(0.1f, 0.05f, 0.15f, 1f);
		Color voidEdge = new(0.3f, 0.1f, 0.5f, 0.8f);
		Color voidGlow = new(0.5f, 0.2f, 0.8f, 0.4f);

		int mid = h / 2;
		for (int x = 1; x < w - 1; x++)
		{
			// Forme de lame qui s'affine aux extrémités
			float t = (float)x / w;
			int thickness = (int)(3f * Mathf.Sin(t * Mathf.Pi));
			for (int dy = -thickness; dy <= thickness; dy++)
			{
				int py = mid + dy;
				if (py < 0 || py >= h) continue;
				Color c = Mathf.Abs(dy) == 0 ? voidCore : (Mathf.Abs(dy) <= 1 ? voidEdge : voidGlow);
				img.SetPixel(x, py, c);
			}
		}
		return ImageTexture.CreateFromImage(img);
	}

	/// <summary>
	/// Écho retardé pour Gantelets d'Écho (echo_gauntlets).
	/// 2 frames d'onde de choc secondaire, 16x16 px.
	/// </summary>
	public static Node2D CreateEchoVfx(Vector2 position, Vector2 direction, Color color)
	{
		if (_particleLevel == ParticleLevel.Off)
			return null;

		_echoFrames ??= GenerateEchoFrames();

		var root = new Node2D
		{
			GlobalPosition = position + direction * 12f,
			Rotation = direction.Angle(),
		};

		SpriteFrames frames = new();
		frames.AddAnimation("echo");
		frames.SetAnimationSpeed("echo", 8);
		frames.SetAnimationLoop("echo", false);
		for (int i = 0; i < _echoFrames.Length; i++)
			frames.AddFrame("echo", _echoFrames[i]);

		AnimatedSprite2D sprite = new()
		{
			SpriteFrames = frames,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(1.5f, 1.5f),
			Modulate = new Color(1f, 1f, 1f, 0.6f),
		};
		sprite.Play("echo");
		root.AddChild(sprite);

		sprite.TreeEntered += () =>
		{
			Tween tween = sprite.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(sprite, "scale", new Vector2(2.5f, 2.5f), 0.25f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(sprite, "modulate:a", 0f, 0.35f);
		};

		var timer = new Timer { WaitTime = 0.45f, OneShot = true, Autostart = true };
		timer.Timeout += root.QueueFree;
		root.AddChild(timer);

		return root;
	}

	private static ImageTexture[] GenerateEchoFrames()
	{
		const int size = 16;
		const int center = size / 2;
		var result = new ImageTexture[2];
		Color wave1 = new(0.6f, 0.75f, 0.9f, 0.8f);
		Color wave2 = new(0.4f, 0.55f, 0.8f, 0.5f);

		for (int f = 0; f < 2; f++)
		{
			Image img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
			int r1 = 4 + f * 2;
			int r2 = 6 + f * 2;
			DrawPixelCircle(img, center, center, r1, wave1);
			DrawPixelCircle(img, center, center, r2, wave2);
			result[f] = ImageTexture.CreateFromImage(img);
		}
		return result;
	}

	// --- Helpers pour la génération pixel art ---

	private static void DrawPixelCircle(Image img, int cx, int cy, int radius, Color color)
	{
		int w = img.GetWidth();
		int h = img.GetHeight();
		// Bresenham circle (outline only)
		int x = radius, y = 0;
		int d = 1 - radius;
		while (x >= y)
		{
			SetPixelSafe(img, cx + x, cy + y, color, w, h);
			SetPixelSafe(img, cx - x, cy + y, color, w, h);
			SetPixelSafe(img, cx + x, cy - y, color, w, h);
			SetPixelSafe(img, cx - x, cy - y, color, w, h);
			SetPixelSafe(img, cx + y, cy + x, color, w, h);
			SetPixelSafe(img, cx - y, cy + x, color, w, h);
			SetPixelSafe(img, cx + y, cy - x, color, w, h);
			SetPixelSafe(img, cx - y, cy - x, color, w, h);
			y++;
			if (d <= 0)
				d += 2 * y + 1;
			else
			{
				x--;
				d += 2 * (y - x) + 1;
			}
		}
	}

	private static void SetPixelSafe(Image img, int x, int y, Color color, int w, int h)
	{
		if (x >= 0 && x < w && y >= 0 && y < h)
			img.SetPixel(x, y, color);
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
		if (_particleLevel == ParticleLevel.Off)
			return null;

		var root = new Node2D { GlobalPosition = position };

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
			Scale = new Vector2(0.5f, 0.5f), // Commence petit
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
			tween.TweenProperty(splatterSprite, "scale", new Vector2(scale, scale * 0.8f), 0.3f)
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
