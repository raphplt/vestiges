using System.Collections.Generic;
using Godot;
// V2: Vestiges.Base retire
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Combat;

public partial class Enemy : CharacterBody2D
{
	private const float MeleeRange = 38f;
	private const float MeleeAttackCooldown = 0.75f;
	// Un tir lointain reste muet : l'AudioManager n'est pas spatialisé, seul ce qui menace le joueur s'entend.
	private const float RangedAttackAudioRadius = 650f;
	private const float RangedAttackCooldown = 1.1f;
	private const float DeathTweenDuration = 0.3f;
	private const float DissolveDuration = 0.6f;
	private const float PlayerProximityRange = 80f;
	// V2: StructureDetectRange retire
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
	private float _baseHp;
	private float _currentHp;
	private float _baseSpeed;
	private float _speed;
	private float _damage;
	private float _attackRange;
	private float _xpReward;
	private string _enemyType;
	private string _enemyId;
	private string _attackAudio;
	private string _behavior = "default";
	private bool _isDying;
	private float _attackTimer;
	private float _meleeAttackCooldown;
	private float _rangedAttackCooldown;
	private float _playerProximityRange;
	private float _spawnSpeedMultiplier = 1f;
	private float _spawnAggressionMultiplier = 1f;

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

	// Performance caches
	private Core.GroupCache _groupCache;
	private EventBus _eventBus;
	private float _meleeRangeSq;
	private float _attackRangeSq;
	private float _packRadiusSq;

	// Off-screen culling
	private const float ActiveProcessingRange = 600f;
	private const float ActiveProcessingRangeSq = ActiveProcessingRange * ActiveProcessingRange;

	// Pack bonus throttle (évite O(n²) chaque frame)
	private float _packBonusTimer;
	private const float PackBonusInterval = 0.5f;

	// V2: structures retirees — FindNearestStructure retourne toujours null

	// Variante (élite, Souverain, Aberration), affixes, harde et micro-événements
	private readonly EnemyModifiers _mods = new();
	private readonly EnemyTracking _tracking = new();
	private Polygon2D _modifierAura;
	private Tween _modifierAuraTween;
	private EnemyNameplate _nameplate;
	private string _displayName;
	private bool _isFeminine;

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
	private static PackedScene _xpOrbScene;
	private static PackedScene _chestScene;

	// Sprite animé (remplace Polygon2D quand sprite_folder est défini)
	private AnimatedSprite2D _sprite;
	private bool _hasSprite;
	private readonly CharacterFacing _facing = new();
	private StringName _currentAnimName;
	private enum SpriteAction { Idle, Walk, Attack, Death }
	// Noms d'animation précalculés [direction, action] : aucune chaîne allouée par frame.
	private static readonly StringName[,] SpriteAnimations = BuildSpriteAnimations();
	// Pieds légèrement sous le centre de collision, comme les anciens sprites centrés.
	private const float SpriteFeetBelowOrigin = 6f;
	private float _attackAnimTimer;

	// Shader VFX unifié (outline + hit flash + dissolve + aberration)
	private static Shader _entityShader;
	private ShaderMaterial _spriteMaterial;
	private Tween _hitFlashTween;

	// Capacités composées décrites par le bloc "abilities" du JSON, réutilisées d'un spawn à l'autre
	private readonly List<IEnemyAbility> _abilities = new();
	private readonly Dictionary<string, IEnemyAbility> _abilityCache = new();
	private bool _abilityReplacesAttack;

	public bool IsActive { get; private set; }
	public bool IsDying => _isDying;
	public float HpRatio => _maxHp > 0 ? _currentHp / _maxHp : 0f;
	public EnemyModifiers Modifiers => _mods;
	public string EnemyId => _enemyId;
	public string DisplayName => _displayName;
	public bool IsFeminine => _isFeminine;
	internal float Damage => _damage;
	internal float SlowFactor => _slowFactor;
	internal bool IsDisoriented => _disorientTimer > 0f;

	public override void _Ready()
	{
		_visual = GetNode<Polygon2D>("Visual");
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_originalColor = _visual.Color;
		_xpOrbScene ??= GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn");
		_chestScene ??= GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
		_entityShader ??= GD.Load<Shader>("res://assets/shaders/entity.gdshader");
		_eventBus ??= GetNode<EventBus>("/root/EventBus");
	}

	private string _projectileSprite = "spit";
	private FxFamily _projectileFamily = FxFamily.Hostile;

	public void Initialize(EnemyData data, float hpScale, float dmgScale)
	{
		//TODO : nécessaire ?
		_hitFlashTween?.Kill();
		_hitFlashTween = null;

		_enemyId = data.Id;
		_enemyType = data.Type;
		_attackAudio = data.AttackAudio;
		_projectileSprite = data.Visual.ProjectileSprite;
		_projectileFamily = PixelPalette.ParseFamily(data.Visual.ProjectileFamily, FxFamily.Hostile);
		_behavior = data.Behavior ?? "default";
		_tier = data.Tier ?? "normal";
		_baseHp = data.Stats.Hp;
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
		_displayName = data.Name;
		_isFeminine = data.IsFeminine;
		_packBonusTimer = (float)GD.RandRange(0.0, PackBonusInterval);
		IsActive = true;

		_meleeRangeSq = MeleeRange * MeleeRange;
		_attackRangeSq = _attackRange * _attackRange;
		_packRadiusSq = _packRadius * _packRadius;

		_groupCache ??= GetNode<Core.GroupCache>("/root/GroupCache");

		ConfigureVisual(data);
		ConfigureAbilities(data);

		Visible = true;
		SetPhysicsProcess(true);
		SetProcess(true);
		Modulate = Colors.White;
		Scale = Vector2.One;
		if (_visual != null) _visual.Scale = Vector2.One;
		if (_sprite != null) _sprite.Scale = Vector2.One;

		if (!IsInGroup("enemies"))
			AddToGroup("enemies");
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

	/// <summary>
	/// Transforme la créature en variante renforcée (élite, Souverain, Aberration) avec ses affixes.
	/// Appelé après Initialize et ApplySpawnTuning : les multiplicateurs s'appliquent aux valeurs déjà mises à l'échelle.
	/// </summary>
	public void ApplyVariant(EnemyVariantData variant, IReadOnlyList<EnemyAffixData> affixes)
	{
		_mods.SetVariant(variant);
		float hpMult = variant.HpMultiplierFor(_baseHp);
		float damageMult = variant.DamageMult;
		float speedMult = variant.SpeedMult;
		foreach (EnemyAffixData affix in affixes)
		{
			_mods.AddAffix(affix);
			hpMult *= affix.HpMult;
			damageMult *= affix.DamageMult;
			speedMult *= affix.SpeedMult;
		}
		ScaleStats(hpMult, damageMult, speedMult);
		_xpReward *= variant.XpMult;
		Scale = Vector2.One * variant.Scale;
		_displayName = _mods.BuildDisplayName(_displayName, _isFeminine);

		if (_hasSprite && _spriteMaterial != null)
		{
			_spriteMaterial.SetShaderParameter("outline_color", variant.OutlineColor);
			if (variant.AberrationShader)
				_spriteMaterial.SetShaderParameter("aberration_amount", 1.0f);
		}
		if (variant.AberrationShader)
		{
			_visual.Color = _visual.Color.Lerp(variant.OutlineColor, 0.5f);
			_originalColor = _visual.Color;
			SpawnAberrationAura();
		}
		else if (affixes.Count > 0)
		{
			SpawnModifierAura(affixes[0].Color with { A = 0.22f });
		}

		if (variant.Nameplate)
		{
			_nameplate = new EnemyNameplate { Name = "Nameplate" };
			AddChild(_nameplate);
			_nameplate.Setup(this, _displayName, _mods.BuildAffixLine(_isFeminine), variant.OutlineColor, variant.Scale, VisualTop());
		}
	}

	/// <summary>Haut du visuel en coordonnées locales, avant mise à l'échelle.</summary>
	private float VisualTop()
	{
		if (!_hasSprite)
			return -_visual.Polygon[0].Length() - 4f;
		Texture2D frame = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, 0);
		return _sprite.Offset.Y - (frame?.GetHeight() ?? 32) * 0.5f;
	}

	/// <summary>Affixe seul, sans variante (pression des Résurgences et du late game).</summary>
	public void ApplyAffix(EnemyAffixData affix)
	{
		_mods.AddAffix(affix);
		ScaleStats(affix.HpMult, affix.DamageMult, affix.SpeedMult);
		SpawnModifierAura(affix.Color with { A = 0.2f });
	}

	/// <summary>Harde : la créature traverse la zone en ligne droite, en frappant ce qu'elle percute.</summary>
	public void StartTravel(Vector2 direction, float speedMultiplier, float duration)
	{
		_mods.StartTravel(direction, speedMultiplier, duration);
	}

	/// <summary>Multiplie l'XP donnée à la mort (créatures d'événement).</summary>
	public void MultiplyXpReward(float multiplier) => _xpReward *= multiplier;

	/// <summary>Dissolution sans récompense : la créature d'un événement expiré retourne au Néant.</summary>
	public void Vanish()
	{
		if (_isDying || !IsActive)
			return;
		_isDying = true;
		CancelAbilities();
		Velocity = Vector2.Zero;
		if (IsInGroup("enemies"))
			RemoveFromGroup("enemies");
		Node2D dissolutionVfx = VfxFactory.CreateDissolutionVfx(GlobalPosition);
		if (dissolutionVfx != null)
			GetTree().CurrentScene.AddChild(dissolutionVfx);
		Tween tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 0f, DissolveDuration);
		tween.TweenCallback(Callable.From(OnDeathComplete));
	}

	private void ScaleStats(float hpMult, float damageMult, float speedMult)
	{
		_maxHp *= hpMult;
		_currentHp = _maxHp;
		_damage *= damageMult;
		_baseSpeed *= speedMult;
		_speed *= speedMult;
	}

	private void SpawnAberrationAura()
	{
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
		// Tuer tous les tweens actifs pour eviter qu'ils reprennent apres re-pooling
		// (les tweens Godot sont pauses quand le node quitte l'arbre et reprennent quand il y revient)
		_hitFlashTween?.Kill();
		_hitFlashTween = null;
		_modifierAuraTween?.Kill();
		_modifierAuraTween = null;
		CancelAbilities();

		IsActive = false;
		_isDying = false;
		_guardTarget = null;
		_tracking.Reset();
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
		_mods.Reset();
		_chargerCooldown = 0f;
		_chargerIsCharging = false;
		_chargerDurationLeft = 0f;
		_packBonusDamage = 0f;
		_packBonusSpeed = 0f;
		_packBonusTimer = 0f;
		Node auraNode = GetNodeOrNull("AberrationAura");
		if (auraNode != null)
			auraNode.QueueFree();
		if (_nameplate != null)
		{
			_nameplate.QueueFree();
			_nameplate = null;
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
			_sprite.Scale = Vector2.One;
			_sprite.Material = null;
			_spriteMaterial = null;
			_visual.Visible = true;
			_hasSprite = false;
			_facing.Reset(false);
			_currentAnimName = null;
			_attackAnimTimer = 0f;
		}

		// Reset visual scale (peut etre distordu par un squash-stretch interrompu)
		if (_visual != null)
			_visual.Scale = Vector2.One;

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
		// Gardiens, hardes et créatures d'événement ont leur propre logique de déplacement.
		bool lostTrack = _guardTarget == null && !_mods.IsEventBound && !_mods.IsTraveling
			&& _tracking.Tick(distToPlayerSq, dt);

		// Off-screen culling : ennemis loin du joueur → traitement minimal
		if (distToPlayerSq > ActiveProcessingRangeSq)
		{
			ProcessIgnite(dt);
			ProcessBleed(dt);
			// Une annonce en cours ne doit pas rester figée à l'écran hors du traitement complet.
			CancelAbilities();

			// Un gardien ne quitte pas son poste pour un joueur hors de vue.
			if (_guardTarget != null)
				return;
			if (lostTrack)
			{
				GlobalPosition += _tracking.WanderDirection * _speed * _tracking.WanderSpeedFactor * _slowFactor * dt;
				return;
			}

			// Mouvement simplifié sans MoveAndSlide complet : traversée de harde ou approche du joueur.
			if (_mods.IsTraveling)
			{
				_mods.TickTravel(dt);
				GlobalPosition += _mods.TravelDirection * _speed * _mods.TravelSpeedMultiplier * _slowFactor * dt;
				return;
			}
			Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition += direction * _speed * _slowFactor * dt;
			return;
		}

		float distToPlayer = Mathf.Sqrt(distToPlayerSq);

		ProcessIgnite(dt);
		ProcessBleed(dt);
		ProcessSlowDecay(dt);
		ProcessDisorient(dt);

		if (lostTrack)
		{
			Velocity = _tracking.WanderDirection * _speed * _tracking.WanderSpeedFactor * _slowFactor;
			UpdateSpriteAnimation(dt);
			MoveAndSlide();
			return;
		}

		ProcessBehaviorAbilities(distToPlayer, dt);

		// Colosse en charge : skip le mouvement normal
		if (_isCharging)
		{
			MoveAndSlide();
			return;
		}

		if (_mods.IsTraveling)
		{
			ProcessTravel(distToPlayer, dt);
			UpdateSpriteAnimation(dt);
			MoveAndSlide();
			return;
		}

		if (ProcessAbilities(distToPlayer, dt))
		{
			UpdateSpriteAnimation(dt);
			MoveAndSlide();
			return;
		}

		if (_guardTarget != null)
		{
			ProcessGuardBehavior(distToPlayer, dt);
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

		float regen = _mods.TickRegen(delta, _maxHp);
		if (regen > 0f)
			_currentHp = Mathf.Min(_currentHp + regen, _maxHp);
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
		if (_hasSprite)
			EnemyAttackFx.FlashWarning(_sprite, PixelPalette.CreatureAcid, 0.4f);
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
			// VFX : flash rouge + tremblement
			if (_hasSprite)
				EnemyAttackFx.FlashWarning(_sprite, PixelPalette.PlayerBlood, 0.3f);
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

		PlayColosseSlamVfx();
	}

	private void PlayColosseSlamVfx()
	{
		EnemyAttackFx.PlaySlam(GlobalPosition, ColosseSlamRange);

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
		if (!_abilityReplacesAttack && distToPlayer <= _attackRange && _attackTimer <= 0f)
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

			return;
		}

		_chargerCooldown -= delta;
		if (_chargerCooldown <= 0f)
		{
			// V2: charge vers le joueur au lieu de structures
			if (_player != null && IsInstanceValid(_player))
			{
				_chargerIsCharging = true;
				_chargerDurationLeft = 0.8f;
				_chargerDirection = (_player.GlobalPosition - GlobalPosition).Normalized();
				if (_hasSprite)
					EnemyAttackFx.FlashWarning(_sprite, PixelPalette.FlowerViolet, 0.3f);
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

	private void ProcessTravel(float distToPlayer, float delta)
	{
		_mods.TickTravel(delta);
		Velocity = _mods.TravelDirection * _speed * _mods.TravelSpeedMultiplier * _slowFactor;
		_attackTimer -= delta;
		if (distToPlayer < MeleeRange && _attackTimer <= 0f)
		{
			MeleeHitPlayer(_player, _damage);
			_attackTimer = _meleeAttackCooldown;
		}
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

		_modifierAuraTween = _modifierAura.CreateTween().SetLoops();
		_modifierAuraTween.TweenProperty(_modifierAura, "scale", Vector2.One * 1.2f, 0.6f).SetTrans(Tween.TransitionType.Sine);
		_modifierAuraTween.TweenProperty(_modifierAura, "scale", Vector2.One, 0.6f).SetTrans(Tween.TransitionType.Sine);
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

				if (!_abilityReplacesAttack && distToPlayer < MeleeRange && _attackTimer <= 0f)
				{
					MeleeHitPlayer(_player, _damage);
					_attackTimer = _meleeAttackCooldown;
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

				if (!_abilityReplacesAttack && distToPlayer <= _attackRange && _attackTimer <= 0f)
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
		if (!_abilityReplacesAttack && distToPlayer < MeleeRange && _attackTimer <= 0f)
		{
			MeleeHitPlayer(_player, _damage);
			_attackTimer = _meleeAttackCooldown;
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
		if (!_abilityReplacesAttack && distToPlayer <= _attackRange && _attackTimer <= 0f)
		{
			ShootProjectile();
			_attackTimer = _rangedAttackCooldown;
			TriggerAttackAnim();
		}
	}

	// V2: FindNearestStructure retire — plus de structures

	private void ShootProjectile()
	{
		if (_player == null || !IsInstanceValid(_player))
			return;

		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		PlayRangedAttackVfx(direction);
		if (_attackAudio != null && GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) < RangedAttackAudioRadius * RangedAttackAudioRadius)
			Infrastructure.AudioManager.Play(_attackAudio, 0.08f, -9f);
		// Tisseuse : les projectiles ralentissent le joueur
		bool slows = _behavior == "weaver";
		CombatPools.Instance?.TakeEnemyProjectile()
			.Launch(GlobalPosition, direction, _damage, _enemyId, _projectileSprite, _projectileFamily,
				slows ? 0.4f : 1f, slows ? 2f : 0f);
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

		CombatPools.Instance?.ShowMuzzleFlash(GlobalPosition + direction * 14f + new Vector2(0f, -EnemyProjectile.FlightHeight), _projectileFamily);
	}

	// --- Damage & Death ---

	public void TakeDamage(float damage, bool isCrit = false)
	{
		if (_currentHp <= 0 || _isDying || _isBurrowed)
			return;

		damage *= _mods.DamageTakenMultiplier;
		_mods.NotifyDamaged();
		_currentHp -= damage;
		_eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, this, damage);
		HitFlash();
		SpawnHitFlashSprite();
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
		// Toujours utiliser Vector2.One comme base pour eviter les distorsions cumulatives
		Node2D target = _hasSprite ? (Node2D)_sprite : _visual;
		target.Scale = new Vector2(1.25f, 0.75f);
		tween.Parallel().TweenProperty(target, "scale", Vector2.One, 0.15f)
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

	/// <summary>
	/// Affiche le sprite pixel art de hit flash (vfx_hit_flash) à la position de l'ennemi.
	/// Complète le shader flash avec un feedback visuel sprite.
	/// </summary>
	private void SpawnHitFlashSprite()
	{
		CombatPools.Instance?.ShowHitFlash(GlobalPosition + new Vector2(0, -8));
	}

	private void SpawnDamageNumber(float damage, bool isCrit = false)
	{
		CombatPools.Instance?.ShowDamageNumber(GlobalPosition + new Vector2(0, -20), damage, isCrit);
	}

	private void Die()
	{
		_isDying = true;
		CancelAbilities();
		_modifierAuraTween?.Kill();
		_igniteDps = 0f;
		_igniteTimer = 0f;
		Velocity = Vector2.Zero;

		// Screen shake à la mort (plus fort pour les mini-boss/aberrations)
		if (_tier == "miniboss")
		{
			ScreenShake.Instance?.ShakeHeavy();
			ScreenShake.Instance?.Hitstop(0.07f);
		}
		else if (_mods.IsVariant)
		{
			if (_mods.Variant.DeathShake == "heavy")
			{
				ScreenShake.Instance?.ShakeHeavy();
				ScreenShake.Instance?.Hitstop(0.06f);
			}
			else
			{
				ScreenShake.Instance?.ShakeMedium();
			}
		}

		// Lancer l'animation de mort sur le sprite
		if (_hasSprite)
			PlaySpriteAnim(SpriteAnimations[(int)_facing.Current, (int)SpriteAction.Death]);

		// Explosive : AoE de dégâts à la mort
		if (_mods.DeathExplosionRadius > 0f)
		{
			float explosionRadius = _mods.DeathExplosionRadius;
			float explosionDamage = _damage * _mods.DeathExplosionDamageMult;

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

			// VFX explosion (sprite animé 5 frames + particules)
			Node2D explosionVfx = VfxFactory.CreateExplosionVfx(GlobalPosition);
			if (explosionVfx != null)
				GetTree().CurrentScene.AddChild(explosionVfx);
		}

		if (IsInGroup("enemies"))
			RemoveFromGroup("enemies");

		// Notifier le POI gardé si c'est un garde
		if (_guardTarget != null && IsInstanceValid(_guardTarget))
			_guardTarget.OnGuardKilled();

		_eventBus.EmitSignal(EventBus.SignalName.EnemyKilled, _enemyId, GlobalPosition);
		if (_mods.EventToken != 0)
			_eventBus.EmitSignal(EventBus.SignalName.EventEnemyKilled, _mods.EventToken, GlobalPosition);

		SpawnXpOrbs();
		TryDropWeapon();

		// Mini-boss : drop un coffre épique garanti
		if (_tier == "miniboss")
			SpawnRewardChest("chest_epic");
		if (_mods.IsVariant)
			GrantVariantRewards();

		SpawnDisintegrationParticles();

		// VFX dissolution sprite animé (particules noires iridescentes montantes)
		Node2D dissolutionVfx = VfxFactory.CreateDissolutionVfx(GlobalPosition);
		if (dissolutionVfx != null)
			GetTree().CurrentScene.AddChild(dissolutionVfx);

		// VFX flaque de sang iridescent sous l'ennemi mort
		Node2D poolVfx = VfxFactory.CreateIridescentBloodSplatter(GlobalPosition, _tier == "miniboss" ? 2.5f : (_mods.IsVariant ? 1.5f : 1.0f));
		if (poolVfx != null)
			GetTree().CurrentScene.AddChild(poolVfx);

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

		int count = _tier == "miniboss" ? 20 : (_mods.IsVariant ? 14 : 8);
		if (VfxFactory.CurrentParticleLevel == ParticleLevel.Reduced)
			count = Mathf.Max(count / 2, 1);
		float emissionRadius = _tier == "miniboss" ? 20f : (_mods.IsVariant ? 12f : 6f);

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

	/// <summary>Essence, coffre et annonce propres à la variante abattue.</summary>
	private void GrantVariantRewards()
	{
		EnemyVariantData variant = _mods.Variant;
		if (variant.BonusEssence > 0)
			_eventBus.EmitSignal(EventBus.SignalName.LootReceived, "essence", _enemyId, variant.BonusEssence);

		if (!string.IsNullOrEmpty(variant.RewardChest) && GD.Randf() < variant.RewardChestChance)
			SpawnRewardChest(variant.RewardChest);

		if (variant.Nameplate)
			_eventBus.EmitSignal(EventBus.SignalName.VariantEnemyKilled, _displayName, variant.Id, GlobalPosition);
	}

	private void SpawnRewardChest(string chestId)
	{
		if (_chestScene == null)
			return;

		ChestDataLoader.Load();
		ChestData chestData = ChestDataLoader.Get(chestId);
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
			_ when _mods.IsVariant => _mods.Variant.WeaponDropChance,
			_ => 0.01f
		};

		if (GD.Randf() >= dropChance)
			return;

		// Sélectionner une arme aléatoire (tier proportionnel au tier de l'ennemi)
		int maxWeaponTier = _tier == "miniboss" ? 4 : (_mods.IsVariant ? 3 : 2);
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
				
				// Sprites du pipeline procédural : pieds ancrés par le JSON. Anciens sprites : centrés, remontés.
				if (data.Visual.SpriteFeetOffset > 0f)
					_sprite.Offset = new Vector2(0f, SpriteFeetBelowOrigin - data.Visual.SpriteFeetOffset);
				else
					_sprite.Offset = new Vector2(0f, -frames.GetFrameTexture("SE_idle", 0).GetHeight() * 0.35f);
				_visual.Visible = false;
				_hasSprite = true;
				_facing.Reset(EnemySpriteLoader.HasEightDirections(frames));
				_currentAnimName = null;
				_attackAnimTimer = 0f;

				// Shader unifié : outline + hit flash + dissolve
				_spriteMaterial = new ShaderMaterial { Shader = _entityShader };
				_spriteMaterial.SetShaderParameter("outline_enabled", true);
				_spriteMaterial.SetShaderParameter("outline_color", GetOutlineColor(data));
				_sprite.Material = _spriteMaterial;

				PlaySpriteAnim(SpriteAnimations[(int)_facing.Current, (int)SpriteAction.Idle]);
			}
		}
	}

	// --- Sprite Animation ---

	private void UpdateSpriteAnimation(float delta)
	{
		if (!_hasSprite)
			return;

		_attackAnimTimer = Mathf.Max(_attackAnimTimer - delta, 0f);

		// Direction depuis la vélocité ; à l'arrêt pendant une attaque (incantation, bond annoncé), face au joueur.
		bool moving = Velocity.LengthSquared() > 1f;
		if (moving)
			_facing.Update(Velocity);
		else if (_attackAnimTimer > 0f && _player != null && IsInstanceValid(_player))
		{
			Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
			if (toPlayer.LengthSquared() > 1f)
				_facing.Update(toPlayer);
		}

		SpriteAction action;
		if (_isDying)
			action = SpriteAction.Death;
		else if (_attackAnimTimer > 0f)
			action = SpriteAction.Attack;
		else if (moving)
			action = SpriteAction.Walk;
		else
			action = SpriteAction.Idle;

		PlaySpriteAnim(SpriteAnimations[(int)_facing.Current, (int)action]);
	}

	private static StringName[,] BuildSpriteAnimations()
	{
		string[] directions = CharacterFacing.DirectionNames;
		string[] actions = EnemySpriteLoader.Actions;
		StringName[,] animations = new StringName[directions.Length, actions.Length];
		for (int direction = 0; direction < directions.Length; direction++)
			for (int action = 0; action < actions.Length; action++)
				animations[direction, action] = $"{directions[direction]}_{actions[action]}";
		return animations;
	}

	private void PlaySpriteAnim(StringName animName)
	{
		if (animName == _currentAnimName)
			return;

		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(animName))
		{
			_sprite.Play(animName);
			_currentAnimName = animName;
		}
	}

	private void ConfigureAbilities(EnemyData data)
	{
		_abilities.Clear();
		_abilityReplacesAttack = false;

		foreach (KeyValuePair<string, EnemyAbilityData> entry in data.Abilities)
		{
			if (!_abilityCache.TryGetValue(entry.Key, out IEnemyAbility ability))
			{
				ability = EnemyAbilityFactory.Create(entry.Key, this);
				if (ability == null)
				{
					GD.PushWarning($"[Enemy] Capacité inconnue '{entry.Key}' pour {data.Id}");
					continue;
				}
				_abilityCache[entry.Key] = ability;
			}

			ability.Configure(entry.Value);
			_abilities.Add(ability);
			_abilityReplacesAttack |= ability.ReplacesBaseAttack;
		}
	}

	/// <summary>Retourne true si une capacité pilote le mouvement de ce tick.</summary>
	private bool ProcessAbilities(float distToPlayer, float delta)
	{
		bool controlsMovement = false;
		foreach (IEnemyAbility ability in _abilities)
			controlsMovement |= ability.Process(this, _player, distToPlayer, delta);
		return controlsMovement;
	}

	private void CancelAbilities()
	{
		foreach (IEnemyAbility ability in _abilities)
			ability.Cancel();
	}

	public override void _ExitTree()
	{
		CancelAbilities();
	}

	internal void HitPlayer(Player player, float damage)
	{
		_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, damage);
		player.TakeDamage(damage);
		TriggerAttackAnim();
	}

	/// <summary>Coup au contact : dégâts et griffe visible vers le joueur.</summary>
	internal void MeleeHitPlayer(Player player, float damage)
	{
		HitPlayer(player, damage);
		if (_attackAudio != null)
			Infrastructure.AudioManager.Play(_attackAudio, 0.08f, -7f);
		EnemyAttackFx.PlayMeleeHit(GlobalPosition, player.GlobalPosition);
	}

	internal void PlayAttackAnim() => TriggerAttackAnim();

	/// <summary>Posture accroupie d'annonce, sur le visuel seul pour ne pas toucher l'échelle d'Aberration.</summary>
	internal void SetWindupPose(bool active)
	{
		Vector2 pose = active ? new Vector2(1.18f, 0.78f) : Vector2.One;
		_visual.Scale = pose;
		if (_sprite != null)
			_sprite.Scale = pose;
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
