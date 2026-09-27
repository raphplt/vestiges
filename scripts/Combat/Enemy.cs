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

	private string _tier = "normal";

	// Void Brute (charger) : charge vers les murs/structures
	private float _chargerCooldown;
	// Réglages de comportement lus dans la fiche (plan 07 lot B) : cri du Hurleur, phases du Rampant, charge de la Brute.
	private float _cryCooldown;
	private float _cryRange;
	private int _cryReinforcements;
	private float _burrowDuration;
	private float _surfaceDuration;
	private float _chargeSpeed;
	private float _chargeCooldown;
	private float _chargeDuration;
	private float _chargeRetry;
	private float _chargeRangeSq;
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
	// Décroissance du recul (par seconde) : l'essentiel du recul se joue en 0,2 s.
	private const float KnockbackDecay = 12f;
	// Plusieurs coups rapprochés se cumulent sans dépasser ce recul total (px) : la créature recule, elle ne s'envole pas.
	private const float MaxKnockbackDistance = 80f;
	private Vector2 _knockVelocity;

	// V2: structures retirees — FindNearestStructure retourne toujours null

	// Variante (élite, Souverain, Aberration), affixes, harde et micro-événements
	private readonly EnemyModifiers _mods = new();
	private readonly EnemyTracking _tracking = new();
	private Sprite2D _shadow;
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
	private readonly HitFeedback _hitFeedback = new();
	private DamageNumber _damageNumber;
	// Sens du dernier coup reçu (du joueur vers la créature) : oriente la mort (plan 02 J2).
	private Vector2 _lastHitDirection;
	private int _damageNumberSerial;

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
		_chestScene ??= GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
		_entityShader ??= GD.Load<Shader>("res://assets/shaders/entity.gdshader");
		_eventBus ??= GetNode<EventBus>("/root/EventBus");
		_shadow = GroundShadow.Create(24f);
		AddChild(_shadow);
	}

	private string _projectileSprite = "spit";
	private FxFamily _projectileFamily = FxFamily.Hostile;

	public void Initialize(EnemyData data, float hpScale, float dmgScale)
	{
		_hitFeedback.RestScale = Vector2.One;
		_hitFeedback.Stop();
		_lastHitDirection = Vector2.Zero;

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
		_tracking.Configure(data.ExtraStats.GetValueOrDefault("perception"), data.ExtraStats.GetValueOrDefault("leash"));
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
		_knockVelocity = Vector2.Zero;
		_cryCooldown = data.GetStat("cry_cooldown", 8f);
		_cryRange = data.GetStat("cry_range", 250f);
		_cryReinforcements = (int)data.GetStat("cry_reinforcements", 2f);
		_burrowDuration = data.GetStat("burrow_duration", 2f);
		_surfaceDuration = data.GetStat("surface_duration", 5f);
		_chargeSpeed = data.GetStat("charge_speed", 200f);
		_chargeCooldown = data.GetStat("charge_cooldown", 8f);
		_chargeDuration = data.GetStat("charge_duration", 0.8f);
		_chargeRetry = data.GetStat("charge_retry", 0.5f);
		float chargeRange = data.GetStat("charge_range", 200f);
		_chargeRangeSq = chargeRange * chargeRange;
		_screamerTimer = _cryCooldown * 0.5f;
		_burrowerPhaseTimer = _surfaceDuration;
		_isBurrowed = false;
		_chargerCooldown = data.GetStat("charge_first_delay", 4f);
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
		_shadow.Visible = false;
		CancelAbilities();
		Velocity = Vector2.Zero;
		if (IsInGroup("enemies"))
			RemoveFromGroup("enemies");
		CombatPools.Instance?.ShowDeath(GlobalPosition, 0, 0f, 0f);
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
		_hitFeedback.RestScale = Vector2.One;
		_hitFeedback.Stop();
		_damageNumber = null;
		_modifierAuraTween?.Kill();
		_modifierAuraTween = null;
		CancelAbilities();

		IsActive = false;
		_isDying = false;
		_guardTarget = null;
		_tracking.Reset();
		_isBurrowed = false;
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
		_knockVelocity = Vector2.Zero;
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
		// Avant le retour anticipé : un coup fatal finit son flash pendant l'animation de mort.
		if (_hitFeedback.IsActive)
			_hitFeedback.Tick((float)delta);
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
			MoveWithKnockback(dt);
			return;
		}

		ProcessBehaviorAbilities(distToPlayer, dt);

		// La charge garde sa vitesse jusqu'au bout : la poursuite ordinaire l'écrasait dans la même frame.
		if (_chargerIsCharging)
		{
			UpdateSpriteAnimation(dt);
			MoveWithKnockback(dt);
			return;
		}

		if (_mods.IsTraveling)
		{
			ProcessTravel(distToPlayer, dt);
			UpdateSpriteAnimation(dt);
			MoveWithKnockback(dt);
			return;
		}

		if (ProcessAbilities(distToPlayer, dt))
		{
			UpdateSpriteAnimation(dt);
			MoveWithKnockback(dt);
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
		MoveWithKnockback(dt);
	}

	/// <summary>
	/// Déplacement du tick, recul compris. Le recul s'ajoute à la vitesse choisie par le comportement au lieu d'être
	/// écrasé par elle ; il décroît de façon exponentielle et parcourt au total la distance demandée.
	/// </summary>
	private void MoveWithKnockback(float delta)
	{
		if (_knockVelocity != Vector2.Zero)
		{
			Velocity += _knockVelocity;
			_knockVelocity *= Mathf.Exp(-KnockbackDecay * delta);
			if (_knockVelocity.LengthSquared() < 1f)
				_knockVelocity = Vector2.Zero;
		}
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
		if (distToPlayer > _cryRange)
			return;

		_screamerTimer -= delta;
		if (_screamerTimer > 0f)
			return;

		_screamerTimer = _cryCooldown;
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

		for (int i = 0; i < _cryReinforcements; i++)
		{
			float angle = Mathf.Tau * i / _cryReinforcements + (float)GD.RandRange(0, Mathf.Pi);
			Vector2 offset = new(Mathf.Cos(angle) * 40f, Mathf.Sin(angle) * 40f);
			Enemy reinforcement = pool.Get();
			reinforcement.GlobalPosition = GlobalPosition + offset;
			enemyContainer.AddChild(reinforcement);
			reinforcement.Initialize(shadeData, 1f, 1f);
		}
	}

	private void PlayActionAudio(string key)
	{
		if (_player != null && IsInstanceValid(_player) && GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) < RangedAttackAudioRadius * RangedAttackAudioRadius)
			Infrastructure.AudioManager.Play(key, 0.06f, -7f);
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
			_burrowerPhaseTimer = _surfaceDuration;
			PlayActionAudio("sfx_burrow_transition");
			CollisionLayer = 2;
			Modulate = new Color(1f, 1f, 1f, 1f);
		}
		else
		{
			// S'enfouit : semi-transparent, ignore les collisions structures
			_isBurrowed = true;
			_burrowerPhaseTimer = _burrowDuration;
			CollisionLayer = 0;
			Modulate = new Color(1f, 1f, 1f, 0.35f);
		}
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
			Velocity = _chargerDirection * _chargeSpeed;

			if (_chargerDurationLeft <= 0f)
			{
				_chargerIsCharging = false;
				_chargerCooldown = _chargeCooldown;
			}

			return;
		}

		_chargerCooldown -= delta;
		if (_chargerCooldown <= 0f)
		{
			// Charge vers le joueur, seulement à portée (`charge_range`) : de plus loin, elle retente peu après.
			if (_player != null && IsInstanceValid(_player)
				&& GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) <= _chargeRangeSq)
			{
				_chargerIsCharging = true;
				PlayActionAudio("sfx_enemy_charge");
				_chargerDurationLeft = _chargeDuration;
				_chargerDirection = (_player.GlobalPosition - GlobalPosition).Normalized();
				if (_hasSprite)
					EnemyAttackFx.FlashWarning(_sprite, PixelPalette.FlowerViolet, 0.3f);
				_visual.Color = new Color(0.8f, 0.2f, 0.8f);
				Tween chargeTween = CreateTween();
				chargeTween.TweenProperty(_visual, "color", _originalColor, 0.3f).SetDelay(0.1f);
			}
			else
			{
				_chargerCooldown = _chargeRetry;
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
		PlayActionAudio("sfx_enemy_ranged_shot");
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

		CombatPools.Instance?.ShowMuzzleFlash(GlobalPosition + direction * 14f + new Vector2(0f, -Iso.FlightHeight), _projectileFamily);
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
		TriggerHitFeedback();
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

	/// <summary>
	/// Recul d'arme : <paramref name="distance"/> est la distance parcourue en pixels (stat `knockback` des armes).
	/// Les variantes agrandies (élites, Souverains) reculent d'autant moins qu'elles sont grandes ; boss et
	/// miniboss n'en subissent pas.
	/// </summary>
	public void ApplyKnockback(Vector2 direction, float distance)
	{
		if (_isDying || _tier is "miniboss" or "boss" || direction == Vector2.Zero)
			return;
		_knockVelocity += direction.Normalized() * (distance * KnockbackDecay / Mathf.Max(1f, Scale.X));
		_knockVelocity = _knockVelocity.LimitLength(MaxKnockbackDistance * KnockbackDecay);
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

	private void TriggerHitFeedback()
	{
		// Recul dans le sens du coup, depuis le joueur : sur le visuel seul, le corps physique ne bouge pas.
		Vector2 direction = _player != null && IsInstanceValid(_player)
			? (GlobalPosition - _player.GlobalPosition).Normalized()
			: Vector2.Zero;
		_lastHitDirection = direction;
		_hitFeedback.Trigger(_hasSprite ? _sprite : null, _spriteMaterial, _visual, _originalColor, direction);
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
		if (CombatPools.Instance == null)
			return;
		// Les coups rapprochés s'additionnent dans le même chiffre ; un critique a toujours le sien.
		if (!isCrit && IsInstanceValid(_damageNumber) && _damageNumber.TryMerge(_damageNumberSerial, damage))
			return;
		DamageNumber number = CombatPools.Instance.ShowDamageNumber(GlobalPosition + new Vector2(0, -20), damage, isCrit);
		if (isCrit || number == null)
			return;
		_damageNumber = number;
		_damageNumberSerial = number.Serial;
	}

	private void Die()
	{
		_isDying = true;
		// Le corps se dissout : son contact avec le sol disparaît avec lui.
		_shadow.Visible = false;
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

		// Retour au néant : éclats sombres, nuage de dissolution et flaque irisée, recyclés (plan 02 J0).
		bool miniboss = _tier == "miniboss";
		CombatPools.Instance?.ShowDeath(GlobalPosition, miniboss ? 20 : (_mods.IsVariant ? 14 : 8), Mathf.Tau,
			miniboss ? 2.5f : (_mods.IsVariant ? 1.5f : 1.0f), miniboss || _mods.IsVariant, _lastHitDirection);

		if (_hasSprite && _spriteMaterial != null)
		{
			// Dissolution via le shader unifié (pas de swap de shader), emportée dans le sens du dernier coup.
			_spriteMaterial.SetShaderParameter("outline_enabled", false);
			_spriteMaterial.SetShaderParameter("dissolve_direction", _lastHitDirection);

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
			Vector2 offset = new Vector2(
				(float)GD.RandRange(-15, 15),
				(float)GD.RandRange(-15, 15)
			);
			// Le butin jaillit du corps, poussé dans le sens du coup.
			CombatPools.Instance?.SpawnXpOrb(GlobalPosition + offset * 1.6f + _lastHitDirection * 10f, xpPerOrb, GlobalPosition);
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

	private static readonly Dictionary<string, float> FeetYById = new();

	/// <summary>
	/// Bas visible du sprite (dernière ligne opaque de la pose de repos), relatif au nœud : l'ombre s'y pose,
	/// sinon une créature dont les pieds ne sont pas à l'origine a l'air de flotter. Calculé une fois par créature.
	/// </summary>
	private float SpriteFeetY(string enemyId, SpriteFrames frames)
	{
		if (FeetYById.TryGetValue(enemyId, out float cached))
			return cached;
		float feet = 0f;
		string animation = frames.HasAnimation("SE_idle") ? "SE_idle" : _sprite.Animation;
		Texture2D texture = frames.GetFrameCount(animation) > 0 ? frames.GetFrameTexture(animation, 0) : null;
		using Image image = texture?.GetImage();
		if (image != null)
		{
			int bottom = image.GetUsedRect().End.Y;
			// Centré par défaut : le bas de l'image est à +hauteur/2 du centre, décalé de l'offset du sprite.
			feet = _sprite.Offset.Y + bottom - image.GetHeight() * 0.5f - 2f;
		}
		FeetYById[enemyId] = feet;
		return feet;
	}

	private void ConfigureVisual(EnemyData data)
	{
		// Ombre proportionnelle à la créature : la variante (élite, Souverain) l'agrandit avec le reste du corps.
		_shadow.Texture = GroundShadow.TextureFor(GroundShadow.SnapWidth(Mathf.Clamp(data.Visual.Size * 2f, 12f, 72f)));
		_shadow.Position = Vector2.Zero;
		_shadow.Visible = true;
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
				_shadow.Position = new Vector2(0f, SpriteFeetY(data.Id, frames));
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

	/// <summary>Poussée purement visuelle (onde de montée de niveau) : le corps et l'IA ne bougent pas.</summary>
	public void Shove(Vector2 direction, float distance)
	{
		_hitFeedback.Shove(_hasSprite ? _sprite : null, _visual, direction, distance);
	}

	internal void PlayAttackAnim() => TriggerAttackAnim();

	/// <summary>Posture accroupie d'annonce, sur le visuel seul pour ne pas toucher l'échelle d'Aberration.</summary>
	internal void SetWindupPose(bool active)
	{
		Vector2 pose = active ? new Vector2(1.18f, 0.78f) : Vector2.One;
		_hitFeedback.RestScale = pose;
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
