using Godot;
using Vestiges.Base;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Combat;

public partial class Enemy : CharacterBody2D
{
	private const float MeleeRange = 25f;
	private const float MeleeAttackCooldown = 1.0f;
	private const float RangedAttackCooldown = 1.5f;
	private const float DeathTweenDuration = 0.3f;
	private const float DissolveDuration = 0.6f;
	private const float PlayerProximityRange = 80f;
	private const float StructureDetectRange = 40f;
	private const float GuardPatrolRadius = 150f;
	private const float ScreamerCryCooldown = 8f;
	private const float ScreamerCryRange = 250f;
	private const int ScreamerSpawnCount = 2;
	private const float BurrowerPhaseDuration = 2f;
	private const float BurrowerPhaseInterval = 5f;
	private const float ColosseChargeInterval = 7f;
	private const float ColosseChargeSpeed = 280f;
	private const float ColosseChargeDuration = 0.6f;
	private const float ColosseSlamRange = 50f;
	private const float ColosseSlamAoeRadius = 120f;
	private const float ColosseSlamCooldown = 4f;

	private float _maxHp;
	private float _currentHp;
	private float _baseSpeed;
	private float _speed;
	private float _damage;
	private float _attackRange;
	private float _xpReward;
	private string _enemyType;
	private string _enemyId;
	private string _behavior = "default";
	private bool _isDying;
	private float _attackTimer;
	private float _meleeAttackCooldown;
	private float _rangedAttackCooldown;
	private float _playerProximityRange;
	private float _spawnSpeedMultiplier = 1f;
	private float _spawnAggressionMultiplier = 1f;

	private bool _nightMode;
	private Vector2 _foyerPosition;

	// Mode garde : l'ennemi patrouille autour d'un POI
	private PointOfInterest _guardTarget;
	private Vector2 _guardPosition;

	// Hurleur : appel de renforts périodique
	private float _screamerTimer;

	// Rampant : phase souterraine (ignore collisions, semi-transparent)
	private float _burrowerPhaseTimer;
	private bool _isBurrowed;

	// Colosse : charge + ground slam
	private float _colosseChargeTimer;
	private float _colosseSlamTimer;
	private bool _isCharging;
	private float _chargeDurationLeft;
	private Vector2 _chargeDirection;
	private string _tier = "normal";

	// Void Brute (charger) : charge vers les murs/structures
	private float _chargerCooldown;
	private bool _chargerIsCharging;
	private float _chargerDurationLeft;
	private Vector2 _chargerDirection;

	// Pack (Charognard) : bonus groupé
	private float _packBonusDamage;
	private float _packBonusSpeed;
	private float _packRadius = 120f;

	// Audio
	private float _idleSoundTimer;
	private float _idleSoundInterval;
	private string _idleSoundKey;

	// Performance caches
	private Core.GroupCache _groupCache;
	private EventBus _eventBus;
	private string _attackAudioKey;
	private string _rangedAudioKey;
	private float _meleeRangeSq;
	private float _attackRangeSq;
	private float _packRadiusSq;

	// Off-screen culling
	private const float ActiveProcessingRange = 600f;
	private const float ActiveProcessingRangeSq = ActiveProcessingRange * ActiveProcessingRange;

	// Pack bonus throttle (évite O(n²) chaque frame)
	private float _packBonusTimer;
	private const float PackBonusInterval = 0.5f;

	// FindNearestStructure cache
	private Structure _cachedNearestStructure;
	private float _structureCacheTimer;
	private const float StructureCacheInterval = 1f;

	// Wave modifiers (nuit 7+ : enragé, régénérant, explosif)
	private string _waveModifier;
	private float _regenTimer;
	private Polygon2D _modifierAura;

	// Aberration : version corrompue (nuit 7+)
	private bool _isAberration;
	private Polygon2D _aberrationAura;

	// Ignite DOT
	private float _igniteDps;
	private float _igniteTimer;

	// Bleed DOT (arme on_hit_effect)
	private float _bleedDps;
	private float _bleedTimer;

	// Slow debuff
	private float _slowFactor = 1f;
	private float _slowTimer;

	// Disorientation (mouvement aléatoire)
	private float _disorientTimer;
	private Vector2 _disorientDirection;

	private Polygon2D _visual;
	private Color _originalColor;
	private Player _player;
	private static PackedScene _damageNumberScene;
	private static PackedScene _enemyProjectileScene;
	private static PackedScene _xpOrbScene;
	private static PackedScene _chestScene;

	// Sprite animé (remplace Polygon2D quand sprite_folder est défini)
	private AnimatedSprite2D _sprite;
	private bool _hasSprite;
	private string _lastDirection = "SE";
	private string _currentAnimName;
	private float _attackAnimTimer;

	// Shader VFX unifié (outline + hit flash + dissolve + aberration)
	private static Shader _entityShader;
	private ShaderMaterial _spriteMaterial;
	private Tween _hitFlashTween;

	public bool IsActive { get; private set; }
	public bool IsDying => _isDying;
	public float HpRatio => _maxHp > 0 ? _currentHp / _maxHp : 0f;

	public override void _Ready()
	{
		_visual = GetNode<Polygon2D>("Visual");
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_originalColor = _visual.Color;
		_damageNumberScene ??= GD.Load<PackedScene>("res://scenes/combat/DamageNumber.tscn");
		_enemyProjectileScene ??= GD.Load<PackedScene>("res://scenes/combat/EnemyProjectile.tscn");
		_xpOrbScene ??= GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn");
		_chestScene ??= GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
		_entityShader ??= GD.Load<Shader>("res://assets/shaders/entity.gdshader");
		_eventBus ??= GetNode<EventBus>("/root/EventBus");
	}

	public void Initialize(EnemyData data, float hpScale, float dmgScale)
	{
		_enemyId = data.Id;
		_enemyType = data.Type;
		_behavior = data.Behavior ?? "default";
		_tier = data.Tier ?? "normal";
		_maxHp = data.Stats.Hp * hpScale;
		_currentHp = _maxHp;
		_baseSpeed = data.Stats.Speed;
		_speed = _baseSpeed;
		_damage = data.Stats.Damage * dmgScale;
		_attackRange = data.Stats.AttackRange;
		_xpReward = data.Stats.XpReward;
		_isDying = false;
		_attackTimer = 0f;
		_meleeAttackCooldown = MeleeAttackCooldown;
		_rangedAttackCooldown = RangedAttackCooldown;
		_playerProximityRange = PlayerProximityRange;
		_spawnSpeedMultiplier = 1f;
		_spawnAggressionMultiplier = 1f;
		_nightMode = false;
		_igniteDps = 0f;
		_igniteTimer = 0f;
		_bleedDps = 0f;
		_bleedTimer = 0f;
		_slowFactor = 1f;
		_slowTimer = 0f;
		_disorientTimer = 0f;
		_screamerTimer = ScreamerCryCooldown * 0.5f;
		_burrowerPhaseTimer = BurrowerPhaseInterval;
		_isBurrowed = false;
		_colosseChargeTimer = ColosseChargeInterval * 0.5f;
		_colosseSlamTimer = ColosseSlamCooldown;
		_isCharging = false;
		_chargeDurationLeft = 0f;
		_chargerCooldown = 4f;
		_chargerIsCharging = false;
		_chargerDurationLeft = 0f;
		_packBonusDamage = data.ExtraStats.TryGetValue("pack_bonus_damage", out float pbd) ? pbd : 0.15f;
		_packBonusSpeed = data.ExtraStats.TryGetValue("pack_bonus_speed", out float pbs) ? pbs : 0.10f;
		_packRadius = data.ExtraStats.TryGetValue("pack_radius", out float pr) ? pr : 120f;
		_waveModifier = null;
		_regenTimer = 0f;
		_packBonusTimer = (float)GD.RandRange(0.0, PackBonusInterval);
		_cachedNearestStructure = null;
		_structureCacheTimer = 0f;
		IsActive = true;

		// Son idle : intervalle aléatoire selon le type d'ennemi
		_idleSoundInterval = _behavior switch
		{
			"screamer" => 8f,   // Le hurleur crie via ProcessScreamerCry
			"burrower" => 10f,
			_ => (float)GD.RandRange(5.0, 10.0)
		};
		_idleSoundTimer = (float)GD.RandRange(0.0, _idleSoundInterval);
		_idleSoundKey = _enemyId == "indicible" ? "sfx_indicible_presence" : $"sfx_{_enemyId}_idle";
		_attackAudioKey = $"sfx_{_enemyId}_attaque";
		_rangedAudioKey = $"sfx_{_enemyId}_tir";
		_meleeRangeSq = MeleeRange * MeleeRange;
		_attackRangeSq = _attackRange * _attackRange;
		_packRadiusSq = _packRadius * _packRadius;

		_groupCache ??= GetNode<Core.GroupCache>("/root/GroupCache");

		ConfigureVisual(data);

		Visible = true;
		SetPhysicsProcess(true);
		SetProcess(true);
		Modulate = Colors.White;
		Scale = Vector2.One;

		if (!IsInGroup("enemies"))
			AddToGroup("enemies");
	}

	public void SetNightTarget(bool nightMode, Vector2 foyerPosition)
	{
		_nightMode = nightMode;
		_foyerPosition = foyerPosition;
	}

	public void ApplySpawnTuning(float speedMultiplier, float aggressionMultiplier)
	{
		_spawnSpeedMultiplier = Mathf.Max(0.5f, speedMultiplier);
		_spawnAggressionMultiplier = Mathf.Clamp(aggressionMultiplier, 0.7f, 3f);
		_speed = _baseSpeed * _spawnSpeedMultiplier;
		_meleeAttackCooldown = MeleeAttackCooldown / _spawnAggressionMultiplier;
		_rangedAttackCooldown = RangedAttackCooldown / _spawnAggressionMultiplier;
		_playerProximityRange = PlayerProximityRange * (1f + (_spawnAggressionMultiplier - 1f) * 0.9f);
		_damage *= 1f + (_spawnAggressionMultiplier - 1f) * 0.2f;
	}

	/// <summary>Assigne cet ennemi comme garde d'un POI. Il patrouillera autour.</summary>
	public void SetGuardTarget(PointOfInterest poi)
	{
		_guardTarget = poi;
		_guardPosition = GlobalPosition;
	}

	/// <summary>Transforme cet ennemi en Aberration : stats boostées, taille accrue, aura sombre.</summary>
	public void Aberrate()
	{
		_isAberration = true;

		// Boost de stats : +60% HP, +40% dégâts, +20% vitesse, +100% XP
		_maxHp *= 1.6f;
		_currentHp = _maxHp;
		_damage *= 1.4f;
		_speed *= 1.2f;
		_xpReward *= 2f;

		// Taille accrue
		Scale = Vector2.One * 1.4f;

		// Teinte sombre violacée via le shader unifié
		Color aberrationTint = new(0.4f, 0.15f, 0.5f);
		_visual.Color = _visual.Color.Lerp(aberrationTint, 0.5f);
		_originalColor = _visual.Color;

		if (_hasSprite && _spriteMaterial != null)
		{
			_spriteMaterial.SetShaderParameter("aberration_amount", 1.0f);
			_spriteMaterial.SetShaderParameter("outline_color", new Color(0.25f, 0.08f, 0.35f, 1f));
		}

		// Aura GPU particules pulsantes (remplace l'ancien Polygon2D)
		_aberrationAura = null;
		if (VfxFactory.CurrentParticleLevel == ParticleLevel.Off)
			return;

		int auraAmount = VfxFactory.CurrentParticleLevel == ParticleLevel.Reduced ? 5 : 10;
		var aura = new GpuParticles2D
		{
			Amount = auraAmount,
			Lifetime = 1.2f,
			SpeedScale = 0.6f,
			Explosiveness = 0f,
			ZIndex = -1,
			Texture = VfxFactory.CircleTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		var gradient = new GradientTexture1D();
		var g = new Gradient();
		g.SetColor(0, new Color(0.15f, 0.05f, 0.2f, 0f));
		g.AddPoint(0.3f, new Color(0.25f, 0.08f, 0.35f, 0.35f));
		g.SetColor(g.GetPointCount() - 1, new Color(0.15f, 0.05f, 0.2f, 0f));
		gradient.Gradient = g;

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(14, 8, 0),
			Direction = new Vector3(0, -0.3f, 0),
			Spread = 180f,
			InitialVelocityMin = 3f,
			InitialVelocityMax = 8f,
			Gravity = new Vector3(0, -5, 0),
			ScaleMin = 0.6f,
			ScaleMax = 1.4f,
			ColorRamp = gradient,
		};
		aura.ProcessMaterial = mat;
		aura.Name = "AberrationAura";
		AddChild(aura);
	}

	public void Reset()
	{
		IsActive = false;
		_isDying = false;
		_nightMode = false;
		_guardTarget = null;
		_isBurrowed = false;
		_isCharging = false;
		_chargeDurationLeft = 0f;
		_tier = "normal";
		_behavior = "default";
		_currentHp = 0;
		_igniteDps = 0f;
		_igniteTimer = 0f;
		_bleedDps = 0f;
		_bleedTimer = 0f;
		_slowFactor = 1f;
		_slowTimer = 0f;
		_disorientTimer = 0f;
		_screamerTimer = 0f;
		_burrowerPhaseTimer = 0f;
		_isAberration = false;
		_chargerCooldown = 0f;
		_chargerIsCharging = false;
		_chargerDurationLeft = 0f;
		_packBonusDamage = 0f;
		_packBonusSpeed = 0f;
		_waveModifier = null;
		_regenTimer = 0f;
		_packBonusTimer = 0f;
		_cachedNearestStructure = null;
		_structureCacheTimer = 0f;
		// Nettoyage aura aberration (GPU particles ou Polygon2D legacy)
		Node auraNode = GetNodeOrNull("AberrationAura");
		if (auraNode != null)
			auraNode.QueueFree();
		if (_aberrationAura != null)
		{
			_aberrationAura.QueueFree();
			_aberrationAura = null;
		}
		if (_modifierAura != null)
		{
			_modifierAura.QueueFree();
			_modifierAura = null;
		}
		CollisionLayer = 2;
		CollisionMask = 4;
		Velocity = Vector2.Zero;
		Visible = false;
		Scale = Vector2.One;
		SetPhysicsProcess(false);
		SetProcess(false);

		// Reset sprite et shaders
		if (_hasSprite)
		{
			_sprite.Visible = false;
			_sprite.Stop();
			_sprite.SelfModulate = Colors.White;
			_sprite.Material = null;
			_spriteMaterial = null;
			_visual.Visible = true;
			_hasSprite = false;
			_lastDirection = "SE";
			_currentAnimName = null;
			_attackAnimTimer = 0f;
		}

		if (IsInGroup("enemies"))
			RemoveFromGroup("enemies");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDying || !IsActive)
			return;

		CachePlayer();
		if (_player == null || !IsInstanceValid(_player))
			return;

		float distToPlayerSq = GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
		float dt = (float)delta;

		// Off-screen culling : ennemis loin du joueur → traitement minimal
		if (distToPlayerSq > ActiveProcessingRangeSq)
		{
			ProcessIgnite(dt);
			ProcessBleed(dt);

			// Mouvement simplifié vers la cible sans MoveAndSlide complet
			if (_nightMode)
			{
				Vector2 directionToFoyer = (_foyerPosition - GlobalPosition).Normalized();
				GlobalPosition += directionToFoyer * _speed * dt;
			}
			else
			{
				Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
				GlobalPosition += direction * _speed * _slowFactor * dt;
			}
			return;
		}

		float distToPlayer = Mathf.Sqrt(distToPlayerSq);

		ProcessIgnite(dt);
		ProcessBleed(dt);
		ProcessSlowDecay(dt);
		ProcessDisorient(dt);
		ProcessIdleSound(dt);
		ProcessBehaviorAbilities(distToPlayer, dt);

		// Colosse en charge : skip le mouvement normal
		if (_isCharging)
		{
			MoveAndSlide();
			return;
		}

		if (_guardTarget != null)
		{
			ProcessGuardBehavior(distToPlayer, dt);
		}
		else if (_nightMode && distToPlayer > _playerProximityRange)
		{
			ProcessNightMovement(distToPlayer, dt);
		}
		else if (_behavior == "sentinel")
		{
			ProcessSentinel(distToPlayer, dt);
		}
		else if (_enemyType == "melee")
		{
			ProcessMelee(distToPlayer, dt);
		}
		else if (_enemyType == "ranged")
		{
			ProcessRanged(distToPlayer, dt);
		}

		UpdateSpriteAnimation(dt);
		MoveAndSlide();
	}

	/// <summary>Traite les capacités spéciales selon le behavior de l'ennemi.</summary>
	private void ProcessBehaviorAbilities(float distToPlayer, float delta)
	{
		switch (_behavior)
		{
			case "screamer":
				ProcessScreamerCry(distToPlayer, delta);
				break;
			case "burrower":
				ProcessBurrowerPhase(delta);
				break;
			case "colosse":
				ProcessColosseAbilities(distToPlayer, delta);
				break;
			case "charger":
				ProcessChargerAbilities(distToPlayer, delta);
				break;
			case "pack":
				ProcessPackBonus(delta);
				break;
		}

		// Wave modifiers (indépendant du behavior)
		if (_waveModifier == "regenerant")
			ProcessRegenModifier(delta);
	}

	/// <summary>Hurleur : crie périodiquement pour appeler des renforts (shade).</summary>
	private void ProcessScreamerCry(float distToPlayer, float delta)
	{
		if (distToPlayer > ScreamerCryRange)
			return;

		_screamerTimer -= delta;
		if (_screamerTimer > 0f)
			return;

		_screamerTimer = ScreamerCryCooldown;
		Infrastructure.AudioManager.Play("sfx_hurleur_cri", 0.05f);

		// Flash vert + pulse visuel pour indiquer le cri
		_visual.Color = new Color(0.2f, 1f, 0.3f);
		Tween flashTween = CreateTween();
		flashTween.TweenProperty(_visual, "color", _originalColor, 0.4f).SetDelay(0.15f);
		flashTween.Parallel().TweenProperty(_visual, "scale", new Vector2(1.3f, 1.3f), 0.1f);
		flashTween.Parallel().TweenProperty(_visual, "scale", Vector2.One, 0.25f).SetDelay(0.1f);

		// Spawn des shade en renfort autour du hurleur
		Spawn.EnemyPool pool = GetNodeOrNull<Spawn.EnemyPool>("/root/Main/EnemyPool");
		Node enemyContainer = GetNodeOrNull("/root/Main/EnemyContainer");
		if (pool == null || enemyContainer == null)
			return;

		EnemyData shadeData = EnemyDataLoader.Get("shade");
		if (shadeData == null)
			return;

		for (int i = 0; i < ScreamerSpawnCount; i++)
		{
			float angle = Mathf.Tau * i / ScreamerSpawnCount + (float)GD.RandRange(0, Mathf.Pi);
			Vector2 offset = new(Mathf.Cos(angle) * 40f, Mathf.Sin(angle) * 40f);
			Enemy reinforcement = pool.Get();
			reinforcement.GlobalPosition = GlobalPosition + offset;
			enemyContainer.AddChild(reinforcement);
			reinforcement.Initialize(shadeData, 1f, 1f);
			if (_nightMode)
				reinforcement.SetNightTarget(true, _foyerPosition);
		}
	}

	/// <summary>Rampant : alterne entre phase souterraine (invulnérable, ignore murs) et surface.</summary>
	private void ProcessBurrowerPhase(float delta)
	{
		_burrowerPhaseTimer -= delta;
		if (_burrowerPhaseTimer > 0f)
			return;

		if (_isBurrowed)
		{
			// Émerge : redevient vulnérable et visible
			_isBurrowed = false;
			_burrowerPhaseTimer = BurrowerPhaseInterval;
			CollisionLayer = 2;
			Modulate = new Color(1f, 1f, 1f, 1f);
			Infrastructure.AudioManager.Play("sfx_rampant_surgissement", 0.05f);
		}
		else
		{
			// S'enfouit : semi-transparent, ignore les collisions structures
			_isBurrowed = true;
			_burrowerPhaseTimer = BurrowerPhaseDuration;
			CollisionLayer = 0;
			Modulate = new Color(1f, 1f, 1f, 0.35f);
		}
	}

	/// <summary>Colosse : charge périodique + ground slam AoE au contact.</summary>
	private void ProcessColosseAbilities(float distToPlayer, float delta)
	{
		// Phase de charge active : le colosse fonce dans une direction
		if (_isCharging)
		{
			_chargeDurationLeft -= delta;
			Velocity = _chargeDirection * ColosseChargeSpeed;

			if (_chargeDurationLeft <= 0f)
			{
				_isCharging = false;
				_colosseChargeTimer = ColosseChargeInterval;
			}

			// Impact joueur pendant la charge
			if (distToPlayer < MeleeRange * 2f)
			{
				_isCharging = false;
				_colosseChargeTimer = ColosseChargeInterval;
				_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage * 1.5f);
				_player.TakeDamage(_damage * 1.5f);
				PlayColosseSlamVfx();
			}
			return;
		}

		// Timer de charge : lance une charge vers la position du joueur
		_colosseChargeTimer -= delta;
		if (_colosseChargeTimer <= 0f && distToPlayer < 350f && distToPlayer > ColosseSlamRange)
		{
			_isCharging = true;
			_chargeDurationLeft = ColosseChargeDuration;
			_chargeDirection = (_player.GlobalPosition - GlobalPosition).Normalized();
			Infrastructure.AudioManager.Play("sfx_brute_charge", 0.05f);

			// VFX : flash rouge + tremblement
			_visual.Color = new Color(1f, 0.2f, 0.2f);
			Tween chargeTween = CreateTween();
			chargeTween.TweenProperty(_visual, "color", _originalColor, 0.3f).SetDelay(0.1f);
		}

		// Ground slam au contact : AoE qui repousse le joueur
		_colosseSlamTimer -= delta;
		if (_colosseSlamTimer <= 0f && distToPlayer < ColosseSlamRange)
		{
			_colosseSlamTimer = ColosseSlamCooldown;
			PerformGroundSlam();
		}
	}

	private void PerformGroundSlam()
	{
		_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage * 2f);
		_player.TakeDamage(_damage * 2f);

		// Knockback joueur
		Vector2 knockbackDir = (_player.GlobalPosition - GlobalPosition).Normalized();
		_player.Velocity += knockbackDir * 300f;

		// Dégâts AoE aux structures proches
		float slamAoeRadiusSq = ColosseSlamAoeRadius * ColosseSlamAoeRadius;
		Godot.Collections.Array<Node> structures = _groupCache.GetStructures();
		foreach (Node node in structures)
		{
			if (node is Base.Structure structure && !structure.IsDestroyed)
			{
				float distSq = GlobalPosition.DistanceSquaredTo(structure.GlobalPosition);
				if (distSq < slamAoeRadiusSq)
					structure.TakeDamage(_damage * 1.5f);
			}
		}

		PlayColosseSlamVfx();
	}

	private void PlayColosseSlamVfx()
	{
		// Onde de choc visuelle
		Polygon2D shockwave = new();
		int segments = 16;
		Vector2[] points = new Vector2[segments];
		for (int i = 0; i < segments; i++)
		{
			float angle = Mathf.Tau * i / segments;
			points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10f;
		}
		shockwave.Polygon = points;
		shockwave.Color = new Color(_originalColor.R, _originalColor.G, _originalColor.B, 0.5f);
		shockwave.GlobalPosition = GlobalPosition;

		GetTree().CurrentScene.AddChild(shockwave);

		Tween tween = shockwave.CreateTween();
		tween.SetParallel();
		tween.TweenProperty(shockwave, "scale", new Vector2(ColosseSlamAoeRadius / 10f, ColosseSlamAoeRadius / 10f), 0.3f)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(shockwave, "modulate:a", 0f, 0.3f);
		tween.Chain().TweenCallback(Callable.From(() => shockwave.QueueFree()));

		// Shake visuel du colosse
		_visual.Scale = new Vector2(1.3f, 0.7f);
		Tween squash = CreateTween();
		squash.TweenProperty(_visual, "scale", Vector2.One, 0.25f)
			.SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
	}

	/// <summary>Sentinelle : immobile, tire à distance. Pilier organique ancré.</summary>
	private void ProcessSentinel(float distToPlayer, float delta)
	{
		Velocity = Vector2.Zero;
		_attackTimer -= delta;
		if (distToPlayer <= _attackRange && _attackTimer <= 0f)
		{
			ShootProjectile();
			_attackTimer = _rangedAttackCooldown;
			TriggerAttackAnim();
		}
	}

	/// <summary>Brute du Vide : charge les structures/murs quand elles sont sur son chemin.</summary>
	private void ProcessChargerAbilities(float distToPlayer, float delta)
	{
		if (_chargerIsCharging)
		{
			_chargerDurationLeft -= delta;
			Velocity = _chargerDirection * 200f;

			if (_chargerDurationLeft <= 0f)
			{
				_chargerIsCharging = false;
				_chargerCooldown = 8f;
			}

			// Impact structure pendant la charge
			float chargeImpactRangeSq = (MeleeRange * 2f) * (MeleeRange * 2f);
			Godot.Collections.Array<Node> structures = _groupCache.GetStructures();
			foreach (Node node in structures)
			{
				if (node is Structure structure && !structure.IsDestroyed)
				{
					float distSq = GlobalPosition.DistanceSquaredTo(structure.GlobalPosition);
					if (distSq < chargeImpactRangeSq)
					{
						structure.TakeDamage(_damage * 2f);
						_chargerIsCharging = false;
						_chargerCooldown = 8f;
						// Flash impact
						_visual.Color = new Color(0.5f, 0.2f, 0.5f);
						Tween flash = CreateTween();
						flash.TweenProperty(_visual, "color", _originalColor, 0.2f);
						break;
					}
				}
			}
			return;
		}

		_chargerCooldown -= delta;
		if (_chargerCooldown <= 0f)
		{
			// Cherche une structure à charger
			Structure target = FindNearestStructure();
			if (target != null)
			{
				_chargerIsCharging = true;
				_chargerDurationLeft = 0.8f;
				_chargerDirection = (target.GlobalPosition - GlobalPosition).Normalized();
				_visual.Color = new Color(0.8f, 0.2f, 0.8f);
				Tween chargeTween = CreateTween();
				chargeTween.TweenProperty(_visual, "color", _originalColor, 0.3f).SetDelay(0.1f);
			}
			else
			{
				_chargerCooldown = 3f;
			}
		}
	}

	/// <summary>Charognard (meute) : bonus de dégâts et vitesse quand d'autres charognards sont proches.</summary>
	private void ProcessPackBonus(float delta)
	{
		_packBonusTimer -= delta;
		if (_packBonusTimer > 0f)
			return;
		_packBonusTimer = PackBonusInterval;

		int packCount = 0;
		Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
		foreach (Node node in enemies)
		{
			if (node is Enemy other && other != this && IsInstanceValid(other) && !other.IsDying
				&& other._enemyId == "charognard"
				&& GlobalPosition.DistanceSquaredTo(other.GlobalPosition) < _packRadiusSq)
			{
				packCount++;
				if (packCount >= 5)
					break;
			}
		}

		if (packCount > 0)
		{
			float speedBonus = 1f + (_packBonusSpeed * packCount);
			_speed = _baseSpeed * _spawnSpeedMultiplier * speedBonus;
		}
		else
		{
			_speed = _baseSpeed * _spawnSpeedMultiplier;
		}
	}

	/// <summary>Applique un modificateur de vague (enragé, régénérant, explosif).</summary>
	public void ApplyWaveModifier(string modifier)
	{
		_waveModifier = modifier;

		switch (modifier)
		{
			case "enraged":
				_damage *= 1.4f;
				_speed *= 1.3f;
				_visual.Color = _visual.Color.Lerp(new Color(1f, 0.2f, 0.1f), 0.5f);
				_originalColor = _visual.Color;
				SpawnModifierAura(new Color(1f, 0.3f, 0.1f, 0.2f));
				break;
			case "regenerant":
				SpawnModifierAura(new Color(0.2f, 1f, 0.3f, 0.2f));
				break;
			case "explosive":
				SpawnModifierAura(new Color(1f, 0.7f, 0.1f, 0.2f));
				break;
		}
	}

	private void ProcessRegenModifier(float delta)
	{
		_regenTimer += delta;
		if (_regenTimer >= 1f)
		{
			_regenTimer = 0f;
			float regenAmount = _maxHp * 0.03f;
			_currentHp = Mathf.Min(_currentHp + regenAmount, _maxHp);
		}
	}

	private void ProcessIdleSound(float delta)
	{
		_idleSoundTimer -= delta;
		if (_idleSoundTimer > 0f)
			return;

		_idleSoundTimer = _idleSoundInterval;

		Infrastructure.AudioManager.Play(_idleSoundKey, 0.06f, -10f);
	}

	private void SpawnModifierAura(Color color)
	{
		if (_modifierAura != null)
			return;

		_modifierAura = new Polygon2D();
		int segments = 8;
		Vector2[] points = new Vector2[segments];
		float auraSize = 16f;
		for (int i = 0; i < segments; i++)
		{
			float angle = Mathf.Tau * i / segments;
			points[i] = new Vector2(Mathf.Cos(angle) * auraSize, Mathf.Sin(angle) * auraSize * 0.5f);
		}
		_modifierAura.Polygon = points;
		_modifierAura.Color = color;
		_modifierAura.ZIndex = -1;
		AddChild(_modifierAura);

		Tween pulse = _modifierAura.CreateTween().SetLoops();
		pulse.TweenProperty(_modifierAura, "scale", Vector2.One * 1.2f, 0.6f).SetTrans(Tween.TransitionType.Sine);
		pulse.TweenProperty(_modifierAura, "scale", Vector2.One, 0.6f).SetTrans(Tween.TransitionType.Sine);
	}

	/// <summary>Garde : attaque le joueur s'il est dans le rayon de patrouille, sinon retourne au poste.</summary>
	private void ProcessGuardBehavior(float distToPlayer, float delta)
	{
		_attackTimer -= delta;
		float distToPostSq = GlobalPosition.DistanceSquaredTo(_guardPosition);

		if (distToPlayer < GuardPatrolRadius)
		{
			// Joueur dans la zone : comportement d'attaque normal
			if (_enemyType == "melee")
			{
				Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
				Velocity = direction * _speed;

				if (distToPlayer < MeleeRange && _attackTimer <= 0f)
				{
					_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage);
					_player.TakeDamage(_damage);
					_attackTimer = _meleeAttackCooldown;
					TriggerAttackAnim();
				}
			}
			else if (_enemyType == "ranged")
			{
				if (distToPlayer > _attackRange)
				{
					Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
					Velocity = direction * _speed;
				}
				else
				{
					Velocity = Vector2.Zero;
				}

				if (distToPlayer <= _attackRange && _attackTimer <= 0f)
				{
					ShootProjectile();
					_attackTimer = _rangedAttackCooldown;
					TriggerAttackAnim();
				}
			}
		}
		else if (distToPostSq > 100f)
		{
			// Joueur hors zone : retour au poste de garde
			Vector2 returnDir = (_guardPosition - GlobalPosition).Normalized();
			Velocity = returnDir * _speed * 0.6f;
		}
		else
		{
			Velocity = Vector2.Zero;
		}
	}

	private void ProcessNightMovement(float distToPlayer, float delta)
	{
		_attackTimer -= delta;

		Structure blockingWall = FindNearestStructure();
		if (blockingWall != null && _enemyType == "melee")
		{
			float distToWallSq = GlobalPosition.DistanceSquaredTo(blockingWall.GlobalPosition);
			Vector2 dirToWall = (blockingWall.GlobalPosition - GlobalPosition).Normalized();
			Velocity = dirToWall * _speed;

			if (distToWallSq < _meleeRangeSq && _attackTimer <= 0f)
			{
				blockingWall.TakeDamage(_damage);
				_attackTimer = _meleeAttackCooldown;
				TriggerAttackAnim();
			}
			return;
		}

		Vector2 directionToFoyer = (_foyerPosition - GlobalPosition).Normalized();
		Velocity = directionToFoyer * _speed;

		if (_enemyType == "melee")
		{
			float distToFoyerSq = GlobalPosition.DistanceSquaredTo(_foyerPosition);
			if (distToFoyerSq < _meleeRangeSq && distToPlayer < _playerProximityRange * 2f && _attackTimer <= 0f)
			{
				_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage);
				_player.TakeDamage(_damage);
				_attackTimer = _meleeAttackCooldown;
				TriggerAttackAnim();
			}
		}
		else if (_enemyType == "ranged" && _attackTimer <= 0f)
		{
			float distToFoyerSq = GlobalPosition.DistanceSquaredTo(_foyerPosition);
			if (distToFoyerSq <= _attackRangeSq)
			{
				ShootProjectile();
				_attackTimer = _rangedAttackCooldown;
				TriggerAttackAnim();
			}
		}
	}

	private void ProcessMelee(float distToPlayer, float delta)
	{
		if (_disorientTimer > 0f)
		{
			Velocity = _disorientDirection * _speed * _slowFactor * 0.4f;
			return;
		}
		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * _speed * _slowFactor;

		_attackTimer -= delta;
		if (distToPlayer < MeleeRange && _attackTimer <= 0f)
		{
			_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage);
			_player.TakeDamage(_damage);
			_attackTimer = _meleeAttackCooldown;
			TriggerAttackAnim();
			Infrastructure.AudioManager.Play(_attackAudioKey, 0.08f, -6f);
		}
	}

	private void ProcessRanged(float distToPlayer, float delta)
	{
		if (_disorientTimer > 0f)
		{
			Velocity = _disorientDirection * _speed * _slowFactor * 0.4f;
			_attackTimer = Mathf.Max(_attackTimer, 0.5f);
			return;
		}
		if (distToPlayer > _attackRange)
		{
			Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
			Velocity = direction * _speed * _slowFactor;
		}
		else
		{
			Velocity = Vector2.Zero;
		}

		_attackTimer -= delta;
		if (distToPlayer <= _attackRange && _attackTimer <= 0f)
		{
			ShootProjectile();
			_attackTimer = _rangedAttackCooldown;
			TriggerAttackAnim();
		}
	}

	private static readonly float StructureDetectRangeSq = StructureDetectRange * StructureDetectRange;

	private Structure FindNearestStructure()
	{
		_structureCacheTimer -= 0.016f; // Approximation d'un frame à 60 FPS
		if (_structureCacheTimer > 0f && _cachedNearestStructure != null)
		{
			if (IsInstanceValid(_cachedNearestStructure) && !_cachedNearestStructure.IsDestroyed)
				return _cachedNearestStructure;
		}
		_structureCacheTimer = StructureCacheInterval;

		Godot.Collections.Array<Node> structures = _groupCache.GetStructures();
		Structure nearest = null;
		float nearestDistSq = StructureDetectRangeSq;

		Vector2 dirToFoyer = (_foyerPosition - GlobalPosition).Normalized();

		foreach (Node node in structures)
		{
			if (node is Structure structure && !structure.IsDestroyed)
			{
				float distSq = GlobalPosition.DistanceSquaredTo(structure.GlobalPosition);
				if (distSq >= nearestDistSq)
					continue;

				Vector2 dirToStructure = (structure.GlobalPosition - GlobalPosition).Normalized();
				float dot = dirToFoyer.Dot(dirToStructure);
				if (dot > 0.3f)
				{
					nearest = structure;
					nearestDistSq = distSq;
				}
			}
		}

		_cachedNearestStructure = nearest;
		return nearest;
	}

	private void ShootProjectile()
	{
		if (_player == null || !IsInstanceValid(_player))
			return;

		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		PlayRangedAttackVfx(direction);
		// Son de tir spécifique selon le type d'ennemi
		Infrastructure.AudioManager.Play(_rangedAudioKey, 0.06f);
		EnemyProjectile projectile = _enemyProjectileScene.Instantiate<EnemyProjectile>();
		projectile.GlobalPosition = GlobalPosition;
		projectile.Initialize(direction, _damage, _enemyId);

		// Tisseuse : les projectiles ralentissent le joueur
		if (_behavior == "weaver")
			projectile.SetSlow(0.4f, 2f);

		GetTree().CurrentScene.AddChild(projectile);
	}

	private void PlayRangedAttackVfx(Vector2 direction)
	{
		if (_visual == null)
			return;

		_visual.Scale = new Vector2(1.08f, 0.92f);
		Tween recoil = CreateTween();
		recoil.TweenProperty(_visual, "scale", Vector2.One, 0.1f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		Node2D flashRoot = new();
		flashRoot.GlobalPosition = GlobalPosition + direction * 14f;
		flashRoot.Rotation = direction.Angle();

		Polygon2D flash = new();
		flash.Color = new Color(0.7f, 1f, 0.35f, 0.8f);
		flash.Polygon = new Vector2[]
		{
			new(-2.5f, 0f),
			new(6f, -3f),
			new(11f, 0f),
			new(6f, 3f)
		};
		flashRoot.AddChild(flash);
		GetTree().CurrentScene.AddChild(flashRoot);

		Tween flashTween = flashRoot.CreateTween();
		flashTween.SetParallel();
		flashTween.TweenProperty(flash, "scale", new Vector2(1.4f, 1.15f), 0.08f);
		flashTween.TweenProperty(flash, "modulate:a", 0f, 0.08f);
		flashTween.Chain().TweenCallback(Callable.From(() => flashRoot.QueueFree()));
	}

	// --- Damage & Death ---

	public void TakeDamage(float damage, bool isCrit = false)
	{
		if (_currentHp <= 0 || _isDying || _isBurrowed)
			return;

		_currentHp -= damage;
		_eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, this, damage);
		HitFlash();
		SpawnDamageNumber(damage, isCrit);
		Infrastructure.AudioManager.Play(isCrit ? "sfx_hit_critique" : "sfx_hit_ennemi", 0.07f);

		// Screen shake + hitstop selon l'intensité
		if (isCrit)
		{
			ScreenShake.Instance?.ShakeHeavy();
			ScreenShake.Instance?.Hitstop(0.045f);
		}
		else if (damage > 20f)
		{
			ScreenShake.Instance?.ShakeLight();
		}

		if (_currentHp <= 0)
			Die();
	}

	/// <summary>Instant kill from execution perk.</summary>
	public void Execute()
	{
		if (_currentHp <= 0 || _isDying)
			return;

		SpawnDamageNumber(_currentHp, false);
		_currentHp = 0;
		Die();
	}

	/// <summary>Apply ignite DOT (damage over time). Refreshes if already ignited.</summary>
	public void ApplyIgnite(float dps, float duration)
	{
		_igniteDps = dps;
		_igniteTimer = duration;
		_visual.Color = new Color(1f, 0.5f, 0.1f);
	}

	/// <summary>Apply bleed DOT (weapon on_hit_effect type "dot"). Refreshes if already bleeding.</summary>
	public void ApplyBleed(float dps, float duration)
	{
		_bleedDps = dps;
		_bleedTimer = duration;
		_visual.Color = new Color(0.8f, 0.15f, 0.15f);
	}

	/// <summary>Ralentit l'ennemi pendant une durée. Facteur 0.5 = 50% de vitesse.</summary>
	public void ApplySlow(float factor, float duration)
	{
		_slowFactor = Mathf.Min(_slowFactor, factor);
		_slowTimer = Mathf.Max(_slowTimer, duration);
		_visual.Color = _visual.Color.Lerp(new Color(0.4f, 0.6f, 1f), 0.4f);
	}

	/// <summary>Désorientation : l'ennemi erre aléatoirement pendant la durée.</summary>
	public void ApplyDisorient(float duration)
	{
		_disorientTimer = duration;
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		_disorientDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		_visual.Color = new Color(1f, 1f, 0.4f);
	}

	/// <summary>Applique un knockback (vélocité instantanée) depuis une direction.</summary>
	public void ApplyKnockback(Vector2 direction, float force)
	{
		if (_isDying || _isCharging || _tier == "miniboss")
			return;
		Velocity += direction.Normalized() * force;
	}

	private void ProcessIgnite(float delta)
	{
		if (_igniteTimer <= 0f)
			return;

		_igniteTimer -= delta;
		float igniteDamage = _igniteDps * delta;
		_currentHp -= igniteDamage;
		_eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, this, igniteDamage);

		if (_igniteTimer <= 0f)
		{
			_igniteDps = 0f;
			_visual.Color = _originalColor;
		}

		if (_currentHp <= 0 && !_isDying)
			Die();
	}

	private void ProcessBleed(float delta)
	{
		if (_bleedTimer <= 0f)
			return;

		_bleedTimer -= delta;
		float bleedDamage = _bleedDps * delta;
		_currentHp -= bleedDamage;
		_eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, this, bleedDamage);

		if (_bleedTimer <= 0f)
		{
			_bleedDps = 0f;
			_visual.Color = _originalColor;
		}

		if (_currentHp <= 0 && !_isDying)
			Die();
	}

	private void ProcessSlowDecay(float delta)
	{
		if (_slowTimer <= 0f)
			return;

		_slowTimer -= delta;
		if (_slowTimer <= 0f)
		{
			_slowFactor = 1f;
			_slowTimer = 0f;
			if (_igniteTimer <= 0f && _bleedTimer <= 0f && _disorientTimer <= 0f)
				_visual.Color = _originalColor;
		}
	}

	private void ProcessDisorient(float delta)
	{
		if (_disorientTimer <= 0f)
			return;

		_disorientTimer -= delta;
		// Changement de direction aléatoire régulier
		if (GD.Randf() < delta * 2f)
		{
			float angle = (float)GD.RandRange(0, Mathf.Tau);
			_disorientDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		}
		if (_disorientTimer <= 0f)
		{
			if (_igniteTimer <= 0f && _bleedTimer <= 0f && _slowTimer <= 0f)
				_visual.Color = _originalColor;
		}
	}

	private void HitFlash()
	{
		_hitFlashTween?.Kill();
		Tween tween = CreateTween();
		_hitFlashTween = tween;

		if (_hasSprite && _spriteMaterial != null)
		{
			// Flash shader sur le sprite
			_spriteMaterial.SetShaderParameter("flash_amount", 1.0f);
			ShaderMaterial mat = _spriteMaterial;
			tween.TweenMethod(
				Callable.From((float v) =>
				{
					if (mat != null)
						mat.SetShaderParameter("flash_amount", v);
				}),
				1.0f, 0.0f, 0.15f
			).SetDelay(0.06f);

			// SelfModulate flash redondant 
			// TODO : à refactor
			_sprite.SelfModulate = new Color(3f, 3f, 3f, 1f);
			tween.Parallel().TweenProperty(_sprite, "self_modulate", Colors.White, 0.15f)
				.SetDelay(0.06f);
		}
		else
		{
			// Flash blanc sur le Polygon2D
			_visual.Color = Colors.White;
			tween.TweenProperty(_visual, "color", _originalColor, 0.15f)
				.SetDelay(0.06f);
		}

		// Squash-stretch : compression rapide puis rebond élastique
		Node2D target = _hasSprite ? (Node2D)_sprite : _visual;
		Vector2 originalScale = target.Scale;
		target.Scale = new Vector2(originalScale.X * 1.25f, originalScale.Y * 0.75f);
		tween.Parallel().TweenProperty(target, "scale", originalScale, 0.15f)
			.SetTrans(Tween.TransitionType.Elastic)
			.SetEase(Tween.EaseType.Out);

		// Micro-recul dans la direction opposée au joueur
		if (_player != null && IsInstanceValid(_player))
		{
			Vector2 knockDir = (GlobalPosition - _player.GlobalPosition).Normalized();
			Vector2 basePos = Position;
			Position += knockDir * 3f;
			tween.Parallel().TweenProperty(this, "position", basePos, 0.1f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		}
	}

	private void SpawnDamageNumber(float damage, bool isCrit = false)
	{
		DamageNumber dmgNum = _damageNumberScene.Instantiate<DamageNumber>();
		dmgNum.GlobalPosition = GlobalPosition + new Vector2(0, -20);
		dmgNum.SetDamage(damage, isCrit);
		GetTree().CurrentScene.AddChild(dmgNum);
	}

	private void Die()
	{
		_isDying = true;
		_igniteDps = 0f;
		_igniteTimer = 0f;
		Velocity = Vector2.Zero;

		// Screen shake à la mort (plus fort pour les mini-boss/aberrations)
		if (_tier == "miniboss")
		{
			ScreenShake.Instance?.ShakeHeavy();
			ScreenShake.Instance?.Hitstop(0.07f);
		}
		else if (_isAberration)
		{
			ScreenShake.Instance?.ShakeMedium();
		}

		// Lancer l'animation de mort sur le sprite
		if (_hasSprite)
			PlaySpriteAnim($"{_lastDirection}_death");

		// Explosive : AoE de dégâts à la mort
		if (_waveModifier == "explosive")
		{
			float explosionRadius = 60f;
			float explosionDamage = _damage * 1.5f;

			// Dégâts au joueur
			float explosionRadiusSq = explosionRadius * explosionRadius;
			if (_player != null && IsInstanceValid(_player))
			{
				float distToPlayerSq = GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
				if (distToPlayerSq < explosionRadiusSq)
				{
					float distToPlayer = Mathf.Sqrt(distToPlayerSq);
					_player.TakeDamage(explosionDamage * (1f - distToPlayer / explosionRadius));
				}
			}

			// Dégâts aux ennemis proches
			Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
			foreach (Node node in enemies)
			{
				if (node is Enemy e && e != this && IsInstanceValid(e) && !e.IsDying)
				{
					if (GlobalPosition.DistanceSquaredTo(e.GlobalPosition) < explosionRadiusSq)
						e.TakeDamage(explosionDamage * 0.5f);
				}
			}

			// VFX explosion
			Polygon2D explosion = new();
			int segs = 12;
			Vector2[] pts = new Vector2[segs];
			for (int i = 0; i < segs; i++)
			{
				float angle = Mathf.Tau * i / segs;
				pts[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5f;
			}
			explosion.Polygon = pts;
			explosion.Color = new Color(1f, 0.5f, 0.1f, 0.7f);
			explosion.GlobalPosition = GlobalPosition;
			GetTree().CurrentScene.AddChild(explosion);

			Tween expTween = explosion.CreateTween();
			expTween.SetParallel();
			expTween.TweenProperty(explosion, "scale", Vector2.One * (explosionRadius / 5f), 0.2f);
			expTween.TweenProperty(explosion, "modulate:a", 0f, 0.2f);
			expTween.Chain().TweenCallback(Callable.From(() => explosion.QueueFree()));
		}

		if (IsInGroup("enemies"))
			RemoveFromGroup("enemies");

		// Notifier le POI gardé si c'est un garde
		if (_guardTarget != null && IsInstanceValid(_guardTarget))
			_guardTarget.OnGuardKilled();

		_eventBus.EmitSignal(EventBus.SignalName.EnemyKilled, _enemyId, GlobalPosition);

		SpawnXpOrbs();
		TryDropWeapon();

		// Mini-boss : drop un coffre épique garanti
		if (_tier == "miniboss")
			SpawnMinibossChest();

		SpawnDisintegrationParticles();

		if (_hasSprite && _spriteMaterial != null)
		{
			// Dissolution via le shader unifié (pas de swap de shader)
			_spriteMaterial.SetShaderParameter("outline_enabled", false);

			Tween tween = CreateTween();
			tween.TweenMethod(
				Callable.From((float v) => _spriteMaterial.SetShaderParameter("dissolve_amount", v)),
				0.0f, 1.0f, DissolveDuration
			);
			tween.TweenCallback(Callable.From(OnDeathComplete));
		}
		else
		{
			// Fallback Polygon2D : ancien comportement scale + fade
			Tween tween = CreateTween();
			tween.SetParallel();
			tween.TweenProperty(this, "scale", Vector2.Zero, DeathTweenDuration);
			tween.TweenProperty(this, "modulate:a", 0f, DeathTweenDuration);
			tween.Chain().TweenCallback(Callable.From(OnDeathComplete));
		}
	}

	/// <summary>Désintégration en particules sombres iridescentes — retour au néant.</summary>
	private void SpawnDisintegrationParticles()
	{
		if (VfxFactory.CurrentParticleLevel == ParticleLevel.Off)
			return;

		int count = _tier == "miniboss" ? 20 : (_isAberration ? 14 : 8);
		if (VfxFactory.CurrentParticleLevel == ParticleLevel.Reduced)
			count = Mathf.Max(count / 2, 1);
		float emissionRadius = _tier == "miniboss" ? 20f : (_isAberration ? 12f : 6f);

		var particles = new GpuParticles2D
		{
			Amount = count,
			Lifetime = 0.6f,
			Explosiveness = 0.9f,
			OneShot = true,
			GlobalPosition = GlobalPosition,
			Texture = VfxFactory.SparkTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		};

		// Gradient : noir iridescent → violet → transparent
		var gradient = new GradientTexture1D();
		var g = new Gradient();
		g.SetColor(0, new Color(0.176f, 0.106f, 0.239f, 0.9f)); // #2D1B3D
		g.AddPoint(0.5f, new Color(0.353f, 0.227f, 0.478f, 0.6f)); // #5A3A7A
		g.SetColor(g.GetPointCount() - 1, new Color(0.08f, 0.05f, 0.12f, 0f));
		gradient.Gradient = g;

		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = emissionRadius,
			Direction = new Vector3(0, -0.5f, 0),
			Spread = 180f,
			InitialVelocityMin = 20f,
			InitialVelocityMax = 60f,
			Gravity = new Vector3(0, -15, 0),
			ScaleMin = 0.5f,
			ScaleMax = 1.5f,
			ColorRamp = gradient,
			DampingMin = 30f,
			DampingMax = 60f,
		};
		particles.ProcessMaterial = mat;
		particles.Emitting = true;

		GetTree().CurrentScene.AddChild(particles);

		// Auto-nettoyage
		var timer = new Timer { WaitTime = 1f, OneShot = true, Autostart = true };
		timer.Timeout += particles.QueueFree;
		particles.AddChild(timer);
	}

	private void SpawnMinibossChest()
	{
		if (_chestScene == null)
			return;

		ChestDataLoader.Load();
		ChestData chestData = ChestDataLoader.Get("chest_epic");
		if (chestData == null)
			return;

		Vector2 spawnPos = GlobalPosition;
		Callable.From(() =>
		{
			Chest chest = _chestScene.Instantiate<Chest>();
			chest.GlobalPosition = spawnPos;
			GetTree().CurrentScene.AddChild(chest);
			chest.Initialize(chestData);
		}).CallDeferred();
	}

	/// <summary>Chance de lâcher une arme au sol à la mort.</summary>
	private void TryDropWeapon()
	{
		// Drop chance basée sur le tier : normal 1%, aberration 4%, miniboss 15%
		float dropChance = _tier switch
		{
			"miniboss" => 0.15f,
			_ when _isAberration => 0.04f,
			_ => 0.01f
		};

		if (GD.Randf() >= dropChance)
			return;

		// Sélectionner une arme aléatoire (tier proportionnel au tier de l'ennemi)
		int maxWeaponTier = _tier == "miniboss" ? 4 : (_isAberration ? 3 : 2);
		System.Collections.Generic.List<WeaponData> candidates = new();
		foreach (WeaponData weapon in WeaponDataLoader.GetAll())
		{
			if (!string.IsNullOrEmpty(weapon.DefaultFor))
				continue;
			if (weapon.Tier > maxWeaponTier || weapon.Tier >= 5)
				continue;
			candidates.Add(weapon);
		}

		if (candidates.Count == 0)
			return;

		WeaponData drop = candidates[(int)(GD.Randf() * candidates.Count)];
		Vector2 spawnPos = GlobalPosition;

		Callable.From(() =>
		{
			WeaponPickup pickup = new();
			pickup.Initialize(drop, spawnPos);
			GetTree().CurrentScene.AddChild(pickup);
		}).CallDeferred();

		GD.Print($"[Enemy] Weapon dropped: {drop.Name} (tier {drop.Tier})");
	}

	private void SpawnXpOrbs()
	{
		int orbCount = _xpReward >= 20 ? 3 : _xpReward >= 10 ? 2 : 1;
		float xpPerOrb = _xpReward / orbCount;

		for (int i = 0; i < orbCount; i++)
		{
			XpOrb orb = _xpOrbScene.Instantiate<XpOrb>();
			Vector2 offset = new Vector2(
				(float)GD.RandRange(-15, 15),
				(float)GD.RandRange(-15, 15)
			);
			orb.GlobalPosition = GlobalPosition + offset;
			orb.Initialize(xpPerOrb);
			GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, orb);
		}
	}

	private void OnDeathComplete()
	{
		Spawn.EnemyPool pool = GetNodeOrNull<Spawn.EnemyPool>("/root/Main/EnemyPool");
		if (pool != null)
			pool.Return(this);
		else
			QueueFree();
	}

	private void ConfigureVisual(EnemyData data)
	{
		_visual.Color = data.Visual.Color;
		_originalColor = data.Visual.Color;

		float s = data.Visual.Size;
		if (data.Visual.Shape == "triangle")
			_visual.Polygon = new Vector2[] { new(-s * 0.75f, -s * 0.375f), new(s * 0.75f, 0), new(-s * 0.75f, s * 0.375f) };
		else if (data.Visual.Shape == "square")
			_visual.Polygon = new Vector2[] { new(-s, -s * 0.5f), new(s, -s * 0.5f), new(s, s * 0.5f), new(-s, s * 0.5f) };
		else
			_visual.Polygon = new Vector2[] { new(-s, 0), new(0, -s * 0.5f), new(s, 0), new(0, s * 0.5f) };

		// Sprite animé : remplace le Polygon2D si sprite_folder est défini
		if (!string.IsNullOrEmpty(data.Visual.SpriteFolder))
		{
			SpriteFrames frames = EnemySpriteLoader.LoadOrGet(data.Id, data.Visual.SpriteFolder);
			if (frames == null)
				GD.PushWarning($"[Enemy] Sprite load failed for '{data.Id}' (folder: {data.Visual.SpriteFolder})");

			if (frames != null)
			{
				_sprite.SpriteFrames = frames;
				_sprite.Visible = true;
				_sprite.SelfModulate = Colors.White;
				// Offset vers le haut : les pieds du sprite doivent toucher le sol isométrique
				_sprite.Offset = new Vector2(0, -frames.GetFrameTexture("SE_idle", 0).GetHeight() * 0.35f);
				_visual.Visible = false;
				_hasSprite = true;
				_lastDirection = "SE";
				_currentAnimName = null;
				_attackAnimTimer = 0f;

				// Shader unifié : outline + hit flash + dissolve
				_spriteMaterial = new ShaderMaterial { Shader = _entityShader };
				_spriteMaterial.SetShaderParameter("outline_enabled", true);
				_spriteMaterial.SetShaderParameter("outline_color", GetOutlineColor(data));
				_sprite.Material = _spriteMaterial;

				PlaySpriteAnim("SE_idle");
			}
		}
	}

	// --- Sprite Animation ---

	private void UpdateSpriteAnimation(float delta)
	{
		if (!_hasSprite)
			return;

		_attackAnimTimer = Mathf.Max(_attackAnimTimer - delta, 0f);

		// Direction depuis la vélocité (conserve la dernière si immobile)
		if (Velocity.LengthSquared() > 1f)
		{
			float angle = Velocity.Angle();
			if (angle >= -Mathf.Pi * 0.5f && angle < 0f)
				_lastDirection = "NE";
			else if (angle >= 0f && angle < Mathf.Pi * 0.5f)
				_lastDirection = "SE";
			else if (angle >= Mathf.Pi * 0.5f && angle <= Mathf.Pi)
				_lastDirection = "SW";
			else
				_lastDirection = "NW";
		}

		// Action : death > attack > walk > idle
		string action;
		if (_isDying)
			action = "death";
		else if (_attackAnimTimer > 0f)
			action = "attack";
		else if (Velocity.LengthSquared() > 1f)
			action = "walk";
		else
			action = "idle";

		PlaySpriteAnim($"{_lastDirection}_{action}");
	}

	private void PlaySpriteAnim(string animName)
	{
		if (animName == _currentAnimName)
			return;

		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(animName))
		{
			_sprite.Play(animName);
			_currentAnimName = animName;
		}
	}

	private void TriggerAttackAnim()
	{
		if (_hasSprite)
			_attackAnimTimer = 0.4f;
	}

	/// <summary>Couleur de contour sel-out basée sur la couleur de l'ennemi (version sombre).</summary>
	private static Color GetOutlineColor(EnemyData data)
	{
		Color baseColor = data.Visual.Color;
		// Sel-out : version assombrie de la couleur de base, jamais noir pur
		return new Color(
			baseColor.R * 0.3f + 0.05f,
			baseColor.G * 0.3f + 0.05f,
			baseColor.B * 0.3f + 0.05f,
			0.9f
		);
	}

	private static Player _cachedPlayer;
	private static ulong _cachedPlayerFrame;

	private void CachePlayer()
	{
		if (_player != null && IsInstanceValid(_player))
			return;

		ulong frame = Engine.GetProcessFrames();
		if (frame == _cachedPlayerFrame && _cachedPlayer != null && IsInstanceValid(_cachedPlayer))
		{
			_player = _cachedPlayer;
			return;
		}

		Node playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player p)
		{
			_player = p;
			_cachedPlayer = p;
			_cachedPlayerFrame = frame;
		}
	}
}
