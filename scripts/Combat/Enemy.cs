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

	private ulong _lifeGeneration;
	private AttackContext _igniteSource;
	private AttackContext _bleedSource;
	private AttackContext _slowSource;
	private AttackContext _disorientSource;
	private ControlOrigin _slowOrigin;
	private ControlOrigin _disorientOrigin;
	private ControlState _nativeSlow;
	private ControlState _nativeDisorientation;
	private float _propagatedDisorientationRemaining;
	public EnemyLife Life => new(GetInstanceId(), _lifeGeneration);
	public bool IsPriorityTarget => _mods.IsVariant && _mods.Variant.Id is "elite" or "champion";
	/// <summary>Contribution native transmissible, sinon provenance du contrôle actif non transmissible.</summary>
	public ControlState SlowControl => _nativeSlow.Remaining > 0f ? _nativeSlow
		: new(_slowFactor, Mathf.Max(0f, _slowTimer), _slowSource, ControlOrigin.Propagated == _slowOrigin ? _slowOrigin : ControlOrigin.Unknown);
	public ControlState DisorientationControl => _nativeDisorientation.Remaining > 0f ? _nativeDisorientation
		: new(1f, Mathf.Max(0f, _disorientTimer), _disorientSource, ControlOrigin.Propagated == _disorientOrigin ? _disorientOrigin : ControlOrigin.Unknown);

	private float _maxHp;
	private float _baseHp;
	private float _currentHp;
	private float _baseSpeed;
	private float _speed;
	private float _damage;
	private float _attackRange;
	private float _xpReward;
	private EnemyCombatType _enemyType;
	private string _enemyId;
	private string _attackAudio;
	private EnemyBehavior _behavior = EnemyBehavior.Default;
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

	// Rampant enfoui (BurrowAbility) : invulnérable, ne bloque personne, estompé.
	private bool _isBurrowed;

	private EnemyTier _tier = EnemyTier.Normal;

	// Meute : bonus groupé entre créatures de la même famille (pack_family de la fiche)
	private string _packFamily;
	private float _packBonusSpeed;
	private float _packRadius;
	private float _webSlowMultiplier;
	private float _webSlowSeconds;

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
	private PixelGroundRing _modifierAura;
	private readonly List<Color> _affixColors = new();
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

	private static readonly Vector2 DamageNumberOffset = new(0f, -20f);
	private const float BurnEmberInterval = 0.3f;
	private const float BurnEmberHeight = 12f;
	private float _burnEmberTimer;

	// Gel (Glaçon, palier 25) : immobile, à part du ralentissement pour ne pas en prendre la durée.
	private float _freezeTimer;

	// Fragilité (plan 21 §7) : dégâts subis augmentés tant qu'elle dure.
	private float _fragileBonus;
	private float _fragileTimer;
	private float _fragileDuration;
	private AttackContext _fragileSource;

	// Durée posée par la dernière application de chaque statut : ce qu'un renouvellement reprend (Pince à linge).
	private float _igniteDuration;
	private float _bleedDuration;
	private float _slowDuration;
	private float _disorientDuration;
	private Vector2 _disorientDirection;

	private Polygon2D _visual;
	private Color _originalColor;
	private Player _player;
	private static PackedScene _chestScene;

	// Sprite animé (remplace Polygon2D quand sprite_folder est défini)
	private AnimatedSprite2D _sprite;
	private bool _hasSprite;
	// Image de la mort par un coup (pas d'une évanescence) : l'arme qui achève la créature réclame l'élimination
	// dans la foulée de son coup, une seule fois (bilan, plan 02 M2). Brûlure ou explosion : personne ne la réclame.
	private bool _killed;
	private ulong _killedFrame;
	private bool _killCredited;
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
	private readonly EnemyStatusVisual _statusVisual = new();
	private EnemyStatusMarks _statusMarks;
	private readonly DamageOverTimeNumbers _dotNumbers = new();
	private readonly EnemyExplosionWarning _explosionWarning = new();
	private ContinuousImpactCadence _continuousImpact;
	private DamageNumber _damageNumber;
	// Sens du dernier coup reçu (du joueur vers la créature) : oriente la mort (plan 02 J2).
	private Vector2 _lastHitDirection;
	private int _damageNumberSerial;

	// Capacités composées décrites par le bloc "abilities" du JSON, réutilisées d'un spawn à l'autre
	private readonly List<IEnemyAbility> _abilities = new();
	private readonly Dictionary<EnemyAbilityKind, IEnemyAbility> _abilityCache = new();
	private bool _abilityReplacesAttack;

	/// <summary>Tempo des animations de toutes les créatures : le présage d'une Résurgence les agite (plan 03 lot C).</summary>
	public static float AnimationTempo = 1f;

	public bool IsActive { get; private set; }
	public bool IsDying => _isDying;
	public float HpRatio => _maxHp > 0 ? _currentHp / _maxHp : 0f;
	public float MaxHp => _maxHp;
	public EnemyModifiers Modifiers => _mods;
	public string EnemyId => _enemyId;
	/// <summary>Sprite animé de la créature, ou null si elle n'est dessinée que par son polygone de repli.</summary>
	public AnimatedSprite2D Sprite => _hasSprite ? _sprite : null;
	public string DisplayName => _displayName;
	public bool IsFeminine => _isFeminine;
	internal float Damage => _damage;
	internal float Speed => _speed;
	/// <summary>Montée en puissance reçue à l'apparition : les renforts qu'elle appelle la reprennent.</summary>
	internal float HpScale { get; private set; } = 1f;
	internal float DamageScale { get; private set; } = 1f;
	internal float SlowFactor => _slowFactor;
	internal float AttackRange => _attackRange;
	internal float RangedCooldown => _rangedAttackCooldown;
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
	internal string ProjectileSpriteId => _projectileSprite ?? "spit";

	public void Initialize(EnemyData data, float hpScale, float dmgScale)
	{
		_lifeGeneration++;
		_hitFeedback.RestScale = Vector2.One;
		_hitFeedback.Stop();
		_statusVisual.Attach(null);
		_dotNumbers.Clear();
		_explosionWarning.Hide();
		_continuousImpact = default;
		_lastHitDirection = Vector2.Zero;

		_enemyId = data.Id;
		_enemyType = data.CombatType;
		HpScale = hpScale;
		DamageScale = dmgScale;
		_attackAudio = data.AttackAudio;
		_projectileSprite = data.Visual.ProjectileSprite;
		_projectileFamily = PixelPalette.ParseFamily(data.Visual.ProjectileFamily, FxFamily.Hostile);
		_behavior = data.Behavior;
		_tier = data.Tier;
		_packFamily = data.PackFamily;
		_baseHp = data.Stats.Hp;
		_maxHp = data.Stats.Hp * hpScale;
		_currentHp = _maxHp;
		_baseSpeed = data.Stats.Speed;
		_speed = _baseSpeed;
		_damage = data.Stats.Damage * dmgScale;
		_attackRange = data.Stats.AttackRange;
		_xpReward = data.Stats.XpReward;
		_tracking.Configure(data.GetStat("perception"), data.GetStat("leash"));
		_isDying = false;
		_killed = false;
		_killCredited = false;
		_attackTimer = 0f;
		_meleeAttackCooldown = MeleeAttackCooldown;
		_rangedAttackCooldown = RangedAttackCooldown;
		_playerProximityRange = PlayerProximityRange;
		_spawnSpeedMultiplier = 1f;
		_spawnAggressionMultiplier = 1f;
		_igniteSource = _bleedSource = _slowSource = _disorientSource = _fragileSource = default;
		_slowOrigin = _disorientOrigin = ControlOrigin.Unknown;
		_fragileBonus = _fragileTimer = _freezeTimer = 0f;
		_nativeSlow = _nativeDisorientation = default;
		_propagatedDisorientationRemaining = 0f;
		_igniteDps = 0f;
		_igniteTimer = 0f;
		_bleedDps = 0f;
		_bleedTimer = 0f;
		_slowFactor = 1f;
		_slowTimer = 0f;
		_disorientTimer = 0f;
		_knockVelocity = Vector2.Zero;
		_isBurrowed = false;
		_packBonusSpeed = data.GetStat("pack_bonus_speed");
		_packRadius = data.GetStat("pack_radius");
		_webSlowMultiplier = data.GetStat("web_slow_multiplier");
		_webSlowSeconds = data.GetStat("web_slow_seconds");
		_displayName = data.Name;
		_isFeminine = data.IsFeminine;
		_packBonusTimer = RunRandom.Behavior.RandfRange(0f, PackBonusInterval);
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

	/// <summary>Vrai une seule fois, pour le coup qui vient de tuer la créature : il lui vaut l'élimination.</summary>
	public bool ClaimKillCredit()
	{
		if (!_killed || _killCredited || _killedFrame != Engine.GetProcessFrames())
			return false;
		_killCredited = true;
		return true;
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
			ShowAffixAura();
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
		ShowAffixAura();
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
		_continuousImpact = default;
		_damageNumber = null;
		_modifierAura?.HideAura();
		CancelAbilities();

		IsActive = false;
		_isDying = false;
		_killed = false;
		_killCredited = false;
		_guardTarget = null;
		_tracking.Reset();
		_isBurrowed = false;
		_tier = EnemyTier.Normal;
		_behavior = EnemyBehavior.Default;
		_packFamily = null;
		_currentHp = 0;
		_igniteSource = _bleedSource = _slowSource = _disorientSource = _fragileSource = default;
		_slowOrigin = _disorientOrigin = ControlOrigin.Unknown;
		_fragileBonus = _fragileTimer = _freezeTimer = 0f;
		_nativeSlow = _nativeDisorientation = default;
		_propagatedDisorientationRemaining = 0f;
		_igniteDps = 0f;
		_igniteTimer = 0f;
		_bleedDps = 0f;
		_bleedTimer = 0f;
		_slowFactor = 1f;
		_slowTimer = 0f;
		_disorientTimer = 0f;
		_mods.Reset();
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
		_modifierAura?.HideAura();
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
			_sprite.SpeedScale = 1f;
			_sprite.Scale = Vector2.One;
			_statusVisual.Attach(null);
			_statusMarks?.Clear();
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
		bool fullProcessing = distToPlayerSq <= ActiveProcessingRangeSq;
		// La distance allège les décisions et les collisions, jamais la durée des effets déjà appliqués.
		ProcessIgnite(dt);
		if (_isDying) return;
		ProcessBleed(dt);
		if (_isDying) return;
		_dotNumbers.Tick(dt, GlobalPosition + DamageNumberOffset);
		ProcessSlowDecay(dt);
		ProcessDisorient(dt, fullProcessing);
		ProcessFragility(dt);
		StatusMarks marks = CurrentStatusMarks;
		_statusVisual.Update(marks, MoveFactor, _tier is EnemyTier.Miniboss or EnemyTier.Boss);
		// Hors de la portée complète (au-delà de l'écran), les marques ne se redessinent pas.
		if (fullProcessing && _hasSprite)
			_statusMarks?.Tick(dt, marks, _igniteDps / Mathf.Max(_maxHp, 1f));
		else if (_statusMarks is { Visible: true })
			_statusMarks.Clear();
		if (fullProcessing)
			_explosionWarning.Tick(this, _mods.DeathExplosionRadius, HpRatio);
		else
			_explosionWarning.Hide();
		float regen = _mods.TickRegen(dt, _maxHp);
		if (regen > 0f)
			_currentHp = Mathf.Min(_currentHp + regen, _maxHp);
		// Gardiens, hardes et créatures d'événement ont leur propre logique de déplacement.
		bool lostTrack = _guardTarget == null && !_mods.IsEventBound && !_mods.IsTraveling
			&& _tracking.Tick(distToPlayerSq, dt);

		// Off-screen culling : ennemis loin du joueur → traitement minimal
		if (!fullProcessing)
		{
			// Le déplacement simplifié n'applique pas le recul, mais il ne doit pas le restituer au retour.
			DecayKnockback(dt);
			// Une annonce en cours ne doit pas rester figée à l'écran hors du traitement complet.
			CancelAbilities();

			// Un gardien ne quitte pas son poste pour un joueur hors de vue.
			if (_guardTarget != null)
				return;
			if (lostTrack)
			{
				GlobalPosition += _tracking.WanderDirection * _speed * _tracking.WanderSpeedFactor * MoveFactor * dt;
				return;
			}

			// Mouvement simplifié sans MoveAndSlide complet : traversée de harde ou approche du joueur.
			if (_mods.IsTraveling)
			{
				_mods.TickTravel(dt);
				GlobalPosition += _mods.TravelDirection * _speed * _mods.TravelSpeedMultiplier * MoveFactor * dt;
				return;
			}
			Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition += direction * _speed * MoveFactor * dt;
			return;
		}

		float distToPlayer = Mathf.Sqrt(distToPlayerSq);

		// Figée (Berceuse, Arrêt sur image, Glaçon) : ni pas, ni attaque, ni capacité, et l'annonce en cours tombe ; le
		// recul la pousse encore. Mini-boss et boss, que le recul ne pousse pas, gardent leurs coups : sinon une arme
		// qui fige en continu les tiendrait sans fin.
		if (_freezeTimer > 0f && _tier is not (EnemyTier.Miniboss or EnemyTier.Boss))
		{
			CancelAbilities();
			Velocity = Vector2.Zero;
			UpdateSpriteAnimation(dt);
			MoveWithKnockback(dt);
			return;
		}

		if (lostTrack)
		{
			Velocity = _tracking.WanderDirection * _speed * _tracking.WanderSpeedFactor * MoveFactor;
			UpdateSpriteAnimation(dt);
			MoveWithKnockback(dt);
			return;
		}

		ProcessBehaviorAbilities(dt);

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
		else if (_behavior == EnemyBehavior.Sentinel)
		{
			ProcessSentinel(distToPlayer, dt);
		}
		else if (_enemyType == EnemyCombatType.Melee)
		{
			ProcessMelee(distToPlayer, dt);
		}
		else if (_enemyType == EnemyCombatType.Ranged)
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
			DecayKnockback(delta);
		}
		MoveAndSlide();
	}

	private void DecayKnockback(float delta)
	{
		if (_knockVelocity == Vector2.Zero)
			return;
		_knockVelocity *= Mathf.Exp(-KnockbackDecay * delta);
		if (_knockVelocity.LengthSquared() < 1f)
			_knockVelocity = Vector2.Zero;
	}

	/// <summary>Bonus de meute à proximité : les actions annoncées passent par les capacités composées.</summary>
	private void ProcessBehaviorAbilities(float delta)
	{
		if (_behavior == EnemyBehavior.Pack)
			ProcessPackBonus(delta);
	}

	/// <summary>Son d'une action de la créature, muet si elle est loin : l'AudioManager n'est pas spatialisé.</summary>
	internal void PlayNearbyAudio(string key)
	{
		if (_player != null && IsInstanceValid(_player) && GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) < RangedAttackAudioRadius * RangedAttackAudioRadius)
			Infrastructure.AudioManager.Play(key, 0.06f, -7f);
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

	/// <summary>Meute : bonus de vitesse quand d'autres créatures de la même famille sont proches.</summary>
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
			if (node is Enemy { IsActive: true, IsDying: false } other && other != this && IsInstanceValid(other)
				&& other._packFamily == _packFamily
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
		Velocity = _mods.TravelDirection * _speed * _mods.TravelSpeedMultiplier * MoveFactor;
		_attackTimer -= delta;
		if (distToPlayer < MeleeRange && _attackTimer <= 0f)
		{
			MeleeHitPlayer(_player, _damage);
			_attackTimer = _meleeAttackCooldown;
		}
	}

	/// <summary>Anneau aux couleurs de tous les affixes portés, repris à chaque nouvel affixe.</summary>
	private void ShowAffixAura()
	{
		if (_modifierAura == null)
		{
			_modifierAura = new PixelGroundRing { Name = "AffixAura" };
			AddChild(_modifierAura);
		}
		_affixColors.Clear();
		foreach (EnemyAffixData affix in _mods.Affixes)
			_affixColors.Add(affix.Color);
		// Sous les pieds, comme l'ombre de contact, et non au centre du corps.
		_modifierAura.Position = _shadow.Position;
		_modifierAura.Show(_affixColors, AuraRadius());
	}

	/// <summary>Demi-largeur au sol du visuel : l'anneau dépasse un peu des pieds, quelle que soit l'échelle.</summary>
	private float AuraRadius()
	{
		if (!_hasSprite)
			return 16f;
		Texture2D frame = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, 0);
		return (frame?.GetWidth() ?? 32) * 0.4f * _sprite.Scale.X;
	}

	/// <summary>Garde : attaque le joueur s'il est dans le rayon de patrouille, sinon retourne au poste.</summary>
	private void ProcessGuardBehavior(float distToPlayer, float delta)
	{
		_attackTimer -= delta;
		float distToPostSq = GlobalPosition.DistanceSquaredTo(_guardPosition);

		if (distToPlayer < GuardPatrolRadius)
		{
			// Joueur dans la zone : comportement d'attaque normal
			if (_enemyType == EnemyCombatType.Melee)
			{
				Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
				Velocity = direction * _speed;

				if (!_abilityReplacesAttack && distToPlayer < MeleeRange && _attackTimer <= 0f)
				{
					MeleeHitPlayer(_player, _damage);
					_attackTimer = _meleeAttackCooldown;
				}
			}
			else if (_enemyType == EnemyCombatType.Ranged)
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
			Velocity = _disorientDirection * _speed * MoveFactor * 0.4f;
			return;
		}
		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * _speed * MoveFactor;

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
		ShootProjectile((_player.GlobalPosition - GlobalPosition).Normalized());
	}

	/// <summary>Tir dans une direction donnée : celle qu'un tir annoncé a verrouillée au début de sa visée.</summary>
	internal void ShootProjectile(Vector2 direction)
	{
		PlayRangedAttackVfx(direction);
		PlayNearbyAudio(_attackAudio ?? "sfx_enemy_ranged_shot");
		// Tisseuse : les projectiles ralentissent le joueur
		bool slows = _behavior == EnemyBehavior.Weaver;
		CombatPools.Instance?.TakeEnemyProjectile()
			.Launch(GlobalPosition, direction, _damage, _enemyId, _projectileSprite, _projectileFamily,
				slows ? _webSlowMultiplier : 1f, slows ? _webSlowSeconds : 0f);
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

	/// <summary>Dégâts et signaux à chaque tick ; seule la mise en scène est cadencée.</summary>
	public bool TakeContinuousDamage(float damage, float delta, AttackContext source = default)
	{
		bool showImpact = _continuousImpact.Advance(delta) || damage * _mods.DamageTakenMultiplier >= _currentHp;
		TakeDamage(damage, false, showImpact, source);
		return showImpact;
	}

	public DamageResult TakeDamage(float damage, bool isCrit = false, bool showImpact = true,
		AttackContext source = default, float carriedDamage = 0f)
	{
		if (_currentHp <= 0 || _isDying || _isBurrowed)
			return default;

		// Débordement : le premier impact direct d'un lancement ultérieur emporte la réserve de son arme.
		if (carriedDamage <= 0f)
			carriedDamage = OverflowLedger.Take(source);
		float taken = _fragileTimer > 0f ? _mods.DamageTakenMultiplier * (1f + _fragileBonus) : _mods.DamageTakenMultiplier;
		DamageResult result = DamageResult.Resolve(Life, source, _currentHp, damage, carriedDamage, taken);
		damage = result.NativeDamage + result.CarriedDamage;
		_mods.NotifyDamaged();
		_currentHp -= damage;
		// La surcharge Span évite un tableau params par tick, tout en gardant le signal Godot synchrone.
		_eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, (System.ReadOnlySpan<Variant>)[this, damage]);
		SpawnDamageNumber(damage, isCrit, result.CarriedDamage > 0f);
		if (showImpact)
		{
			TriggerHitFeedback();
			SpawnHitFlashSprite();
			Infrastructure.AudioManager.Play(isCrit ? "sfx_hit_critique" : "sfx_hit_ennemi", 0.07f);
		}

		// Screen shake + hitstop selon l'intensité
		if (isCrit)
		{
			ScreenShake.Instance?.ShakeHeavy();
			ScreenShake.Instance?.Hitstop(0.045f);
		}
		else if (showImpact && damage > 20f)
		{
			ScreenShake.Instance?.ShakeLight();
		}

		_eventBus.PublishEnemyDamage(result);
		if (result.Fatal)
			Die(result);
		return result;
	}

	/// <summary>
	/// Brûlure (plan 21 §7, planche 05 R1) : l'intensité la plus forte et la réserve de dégâts la plus grande. Une
	/// Brûlure n'apporte jamais plus qu'elle-même : le feu de la Lampe et l'Allumette ne se prêtent ni intensité ni
	/// durée ; une même Brûlure renouvelée repart pour sa durée. La source suit la Brûlure la plus forte.
	/// </summary>
	public void ApplyIgnite(float dps, float duration, AttackContext source = default)
	{
		if (_igniteTimer <= 0f || dps >= _igniteDps)
			_igniteSource = OverTime(source);
		if (_igniteTimer > 0f)
		{
			float strongest = Mathf.Max(_igniteDps, dps);
			float reserve = Mathf.Max(_igniteDps * _igniteTimer, dps * duration);
			_igniteDps = strongest;
			_igniteTimer = _igniteDuration = strongest > 0f ? reserve / strongest : Mathf.Max(_igniteTimer, duration);
		}
		else
		{
			_igniteDps = dps;
			_igniteTimer = _igniteDuration = duration;
		}
		_visual.Color = new Color(1f, 0.5f, 0.1f);
	}

	/// <summary>Saignement (effet à l'impact des armes) : même règle de cumul que la Brûlure.</summary>
	public void ApplyBleed(float dps, float duration, AttackContext source = default)
	{
		_bleedSource = OverTime(source);
		_bleedDps = _bleedTimer > 0f ? Mathf.Max(_bleedDps, dps) : dps;
		_bleedTimer = _bleedDuration = Mathf.Max(_bleedTimer, duration);
		_visual.Color = new Color(0.8f, 0.15f, 0.15f);
	}

	/// <summary>Un effet posé par un objet reste celui d'un objet ; posé par une arme, il devient son dégât sur la durée.</summary>
	private static AttackContext OverTime(AttackContext source) =>
		source.Kind == DamageKind.Passive ? source : source.As(DamageKind.DamageOverTime);

	/// <summary>Ralentit l'ennemi pendant une durée. Facteur 0.5 = 50% de vitesse.</summary>
	public void ApplySlow(float factor, float duration, AttackContext source = default, ControlOrigin origin = ControlOrigin.NativeWeapon)
	{
		_slowSource = source;
		_slowOrigin = origin == ControlOrigin.Propagated ? origin
			: source.IsDirectWeapon ? origin : ControlOrigin.Unknown;
		if (_slowOrigin == ControlOrigin.NativeWeapon)
		{
			// Le report éventuel ne prête jamais son intensité ou sa durée au contrôle natif.
			bool sameOwner = _nativeSlow.Remaining > 0f && _nativeSlow.Source.OwnerId == source.OwnerId;
			_nativeSlow = new(sameOwner ? Mathf.Min(_nativeSlow.Strength, factor) : factor,
				sameOwner ? Mathf.Max(_nativeSlow.Remaining, duration) : duration, source, origin);
		}
		_slowFactor = Mathf.Min(_slowFactor, factor);
		_slowTimer = _slowDuration = Mathf.Max(_slowTimer, duration);
		_visual.Color = _visual.Color.Lerp(new Color(0.4f, 0.6f, 1f), 0.4f);
	}

	/// <summary>Désorientation : l'ennemi erre aléatoirement pendant la durée.</summary>
	public void ApplyDisorient(float duration, AttackContext source = default, ControlOrigin origin = ControlOrigin.NativeWeapon)
	{
		bool wasDisoriented = _disorientTimer > 0f;
		_disorientSource = source;
		_disorientOrigin = origin == ControlOrigin.Propagated ? origin
			: source.IsDirectWeapon ? origin : ControlOrigin.Unknown;
		if (_disorientOrigin == ControlOrigin.NativeWeapon)
			_nativeDisorientation = new(1f, duration, source, origin);
		else if (origin != ControlOrigin.Propagated)
			_nativeDisorientation = _nativeDisorientation with { Remaining = Mathf.Min(_nativeDisorientation.Remaining, duration) };
		// La transmission ne raccourcit pas un contrôle présent ; les applications natives gardent leur comportement.
		if (origin == ControlOrigin.Propagated)
		{
			_propagatedDisorientationRemaining = Mathf.Max(_propagatedDisorientationRemaining, duration);
			_disorientTimer = Mathf.Max(_disorientTimer, duration);
		}
		else
			_disorientTimer = Mathf.Max(duration, _propagatedDisorientationRemaining);
		_disorientDuration = _disorientTimer;
		// Une créature déjà désorientée garde son cap : un cône qui la désoriente à chaque image la ferait trembler sur
		// place au lieu d'errer.
		if (!wasDisoriented)
		{
			float angle = RunRandom.Behavior.RandfRange(0f, Mathf.Tau);
			_disorientDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		}
		_visual.Color = new Color(1f, 1f, 0.4f);
	}

	/// <summary>
	/// Recul d'arme : <paramref name="distance"/> est la distance parcourue en pixels (stat `knockback` des armes).
	/// Les variantes agrandies (élites, Souverains) reculent d'autant moins qu'elles sont grandes ; boss et
	/// miniboss n'en subissent pas.
	/// </summary>
	public void ApplyKnockback(Vector2 direction, float distance)
	{
		if (_isDying || _tier is EnemyTier.Miniboss or EnemyTier.Boss || direction == Vector2.Zero)
			return;
		_knockVelocity += direction.Normalized() * (distance * KnockbackDecay / Mathf.Max(1f, Scale.X));
		_knockVelocity = _knockVelocity.LimitLength(MaxKnockbackDistance * KnockbackDecay);
		CombatPools.Instance?.EmitKnockbackDust(GlobalPosition, direction.Normalized());
	}

	private void ProcessIgnite(float delta)
	{
		if (_igniteTimer <= 0f)
			return;

		_igniteTimer -= delta;
		EmitBurnEmbers(delta);
		float igniteDamage = _igniteDps * delta * DamageOverTimeFactor(_igniteSource);
		DamageResult result = DamageResult.Resolve(Life, _igniteSource, _currentHp, igniteDamage, 0f);
		_currentHp -= igniteDamage;
		_dotNumbers.Add(StatusKind.Burn, igniteDamage);
		_eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, this, igniteDamage);
		_eventBus.PublishEnemyDamage(result);

		if (_igniteTimer <= 0f)
		{
			float dps = _igniteDps;
			_igniteDps = 0f;
			_visual.Color = _originalColor;
			if (!result.Fatal)
				PublishStatusExpiry(StatusKind.Burn, dps, _igniteDuration, _igniteSource);
		}

		if (result.Fatal && !_isDying)
			Die(result);
	}

	private void ProcessBleed(float delta)
	{
		if (_bleedTimer <= 0f)
			return;

		_bleedTimer -= delta;
		float bleedDamage = _bleedDps * delta * DamageOverTimeFactor(_bleedSource);
		DamageResult result = DamageResult.Resolve(Life, _bleedSource, _currentHp, bleedDamage, 0f);
		_currentHp -= bleedDamage;
		_dotNumbers.Add(StatusKind.Bleed, bleedDamage);
		_eventBus.EmitSignal(EventBus.SignalName.EntityDamaged, this, bleedDamage);
		_eventBus.PublishEnemyDamage(result);

		if (_bleedTimer <= 0f)
		{
			float dps = _bleedDps;
			_bleedDps = 0f;
			_visual.Color = _originalColor;
			if (!result.Fatal)
				PublishStatusExpiry(StatusKind.Bleed, dps, _bleedDuration, _bleedSource);
		}

		if (result.Fatal && !_isDying)
			Die(result);
	}

	private void ProcessSlowDecay(float delta)
	{
		_freezeTimer = Mathf.Max(0f, _freezeTimer - delta);
		_nativeSlow = _nativeSlow with { Remaining = Mathf.Max(0f, _nativeSlow.Remaining - delta) };
		if (_slowTimer <= 0f)
			return;

		_slowTimer -= delta;
		if (_slowTimer <= 0f)
		{
			float factor = _slowFactor;
			_slowFactor = 1f;
			_slowTimer = 0f;
			if (_igniteTimer <= 0f && _bleedTimer <= 0f && _disorientTimer <= 0f)
				_visual.Color = _originalColor;
			PublishStatusExpiry(StatusKind.Slow, factor, _slowDuration, _slowSource, _slowOrigin);
		}
	}

	private void ProcessDisorient(float delta, bool updateDirection)
	{
		_nativeDisorientation = _nativeDisorientation with { Remaining = Mathf.Max(0f, _nativeDisorientation.Remaining - delta) };
		_propagatedDisorientationRemaining = Mathf.Max(0f, _propagatedDisorientationRemaining - delta);
		if (_disorientTimer <= 0f)
			return;

		_disorientTimer = Mathf.Max(0f, _disorientTimer - delta);
		// Hors de l'IA complète, seule l'expiration compte : aucun tirage aléatoire ni choix de direction.
		if (updateDirection && RunRandom.Behavior.Randf() < delta * 2f)
		{
			float angle = RunRandom.Behavior.RandfRange(0f, Mathf.Tau);
			_disorientDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		}
		if (_disorientTimer <= 0f)
		{
			if (_igniteTimer <= 0f && _bleedTimer <= 0f && _slowTimer <= 0f)
				_visual.Color = _originalColor;
			PublishStatusExpiry(StatusKind.Disorientation, 1f, _disorientDuration, _disorientSource, _disorientOrigin);
		}
	}

	/// <summary>
	/// Un tic suit la règle d'un coup : rien sur une créature terrée, réduction des affixes, et Fragile (planche 05,
	/// R2) pour les Brûlures et saignements posés par le joueur.
	/// </summary>
	private float DamageOverTimeFactor(AttackContext source)
	{
		if (_isBurrowed)
			return 0f;
		float fragile = _fragileTimer > 0f && source.IsPlayerOwned ? 1f + _fragileBonus : 1f;
		return _mods.DamageTakenMultiplier * fragile;
	}

	public bool IsBurning => _igniteTimer > 0f;
	public bool IsSlowed => _slowTimer > 0f || _freezeTimer > 0f;
	public bool IsFrozen => _freezeTimer > 0f;
	/// <summary>Entravé (planche 05, R3) : ralenti, figé ou désorienté.</summary>
	public bool IsHindered => IsSlowed || _disorientTimer > 0f;
	private float MoveFactor => _freezeTimer > 0f ? 0f : _slowFactor;

	private StatusMarks CurrentStatusMarks =>
		(_freezeTimer > 0f ? StatusMarks.Frozen : 0) | (_slowTimer > 0f ? StatusMarks.Slowed : 0)
		| (_igniteTimer > 0f ? StatusMarks.Burning : 0) | (_bleedTimer > 0f ? StatusMarks.Bleeding : 0)
		| (_disorientTimer > 0f ? StatusMarks.Disoriented : 0) | (_fragileTimer > 0f ? StatusMarks.Fragile : 0);

	/// <summary>Fige la créature <paramref name="seconds"/> secondes, sans toucher au ralentissement en cours.</summary>
	public void Freeze(float seconds) => _freezeTimer = Mathf.Max(_freezeTimer, seconds);

	/// <summary>
	/// Fragilité : les dégâts subis augmentent de <paramref name="bonus"/> (0,1 = +10 %). Même cumul que les autres
	/// statuts : la durée repart, l'intensité la plus forte reste.
	/// </summary>
	public void ApplyFragile(float bonus, float duration, AttackContext source = default)
	{
		_fragileBonus = _fragileTimer > 0f ? Mathf.Max(_fragileBonus, bonus) : bonus;
		_fragileTimer = _fragileDuration = Mathf.Max(_fragileTimer, duration);
		_fragileSource = source;
	}

	/// <summary>
	/// Prolonge un ralentissement en cours (Épingle à nourrice), sans dépasser <paramref name="maxRemaining"/> secondes
	/// restantes ; sans effet sur une créature non ralentie.
	/// </summary>
	public void ExtendSlow(float seconds, float maxRemaining)
	{
		if (_slowTimer > 0f)
			_slowTimer = Mathf.Max(_slowTimer, Mathf.Min(_slowTimer + seconds, maxRemaining));
		// Une créature seulement figée est ralentie à l'arrêt : c'est sa pause qui se prolonge.
		else if (_freezeTimer > 0f)
			_freezeTimer = Mathf.Max(_freezeTimer, Mathf.Min(_freezeTimer + seconds, maxRemaining));
	}

	private void ProcessFragility(float delta)
	{
		if (_fragileTimer <= 0f)
			return;
		_fragileTimer -= delta;
		if (_fragileTimer > 0f)
			return;
		float bonus = _fragileBonus;
		_fragileBonus = 0f;
		_fragileTimer = 0f;
		PublishStatusExpiry(StatusKind.Fragility, bonus, _fragileDuration, _fragileSource);
	}

	/// <summary>
	/// Fioriture de la brûlure : deux étincelles toutes les 0,3 s depuis le milieu du corps, par le pool d'étincelles,
	/// soumises au budget et au réglage « Effets d'attaque ». La marque qui reste toujours est EnemyStatusMarks.
	/// </summary>
	private void EmitBurnEmbers(float delta)
	{
		_burnEmberTimer -= delta;
		if (_burnEmberTimer > 0f || CombatPools.Instance == null)
			return;
		_burnEmberTimer = BurnEmberInterval;
		Vector2 body = _hasSprite && _statusMarks != null ? _statusMarks.BodyCenter : new Vector2(0f, -BurnEmberHeight);
		CombatPools.Instance.EmitSparks(GlobalPosition + body, new SparkBurst
		{
			Family = FxFamily.Fire,
			Owner = FxOwner.Player,
			Count = 2,
			Direction = Vector2.Up,
			Spread = 0.8f,
			SpeedMin = 15f,
			SpeedMax = 35f,
			LifeMin = 0.25f,
			LifeMax = 0.4f,
			Size = 1,
		});
	}

	/// <summary>Statut d'un joueur arrivé à son terme sur une créature vivante : publié pour ce qui le prolonge.</summary>
	private void PublishStatusExpiry(StatusKind kind, float strength, float duration, AttackContext source,
		ControlOrigin origin = ControlOrigin.Unknown)
	{
		if (source.IsPlayerOwned && !_isDying && duration > 0f)
			_eventBus.PublishStatusExpiry(new StatusExpiry(this, kind, strength, duration, source, origin));
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

	private void SpawnDamageNumber(float damage, bool isCrit = false, bool isCarried = false)
	{
		if (CombatPools.Instance == null)
			return;
		// Les coups rapprochés s'additionnent dans le même chiffre ; un critique ou un coup renforcé a toujours le sien.
		bool pops = isCrit || isCarried;
		if (!pops && IsInstanceValid(_damageNumber) && _damageNumber.TryMerge(_damageNumberSerial, damage))
			return;
		DamageNumber number = CombatPools.Instance.ShowDamageNumber(GlobalPosition + DamageNumberOffset, damage, isCrit, isCarried);
		if (pops || number == null)
			return;
		_damageNumber = number;
		_damageNumberSerial = number.Serial;
	}

	private void Die(DamageResult damage)
	{
		if (_isDying)
			return;
		_isDying = true;
		// Le tic fatal et le reliquat de brûlure ou de saignement s'affichent avec la mort.
		_dotNumbers.Flush(GlobalPosition + DamageNumberOffset);
		_explosionWarning.Hide();
		// Capturer les contrôles avant leur nettoyage et avant les explosions de mort en cascade.
		_eventBus.PublishEnemyKill(new EnemyKillResult(Life, _enemyId, GlobalPosition, damage, SlowControl, DisorientationControl,
			new ControlState(_igniteDps, Mathf.Max(0f, _igniteTimer), _igniteSource, ControlOrigin.Unknown),
			IsPriorityTarget || _tier is EnemyTier.Elite or EnemyTier.Miniboss or EnemyTier.Boss));
		_killed = true;
		_killedFrame = Engine.GetProcessFrames();
		// Le corps se dissout : son contact avec le sol disparaît avec lui.
		_shadow.Visible = false;
		CancelAbilities();
		_modifierAura?.HideAura();
		_igniteDps = 0f;
		_igniteTimer = 0f;
		Velocity = Vector2.Zero;

		// Screen shake à la mort (plus fort pour les mini-boss/aberrations)
		if (_tier == EnemyTier.Miniboss)
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
		{
			PlaySpriteAnim(SpriteAnimations[(int)_facing.Current, (int)SpriteAction.Death]);
			// Une créature tuée figée ou ralentie meurt à cadence normale ; givre et chaleur se dissolvent avec elle.
			_statusVisual.ReleaseTempo();
			_sprite.SpeedScale = AnimationTempo;
			_statusMarks?.Clear();
		}

		// Explosive : AoE de dégâts à la mort
		if (_mods.DeathExplosionRadius > 0f)
		{
			float explosionRadius = _mods.DeathExplosionRadius;
			float explosionDamage = _damage * _mods.DeathExplosionDamageMult;

			// Dégâts au joueur
			float explosionRadiusSq = explosionRadius * explosionRadius;
			if (_player != null && IsInstanceValid(_player))
			{
				// Mesurée au sol, comme la zone dessinée : plus large que haute à l'écran.
				float distToPlayerSq = Iso.GroundDistanceSquared(GlobalPosition, _player.GlobalPosition);
				if (distToPlayerSq < explosionRadiusSq)
				{
					float distToPlayer = Mathf.Sqrt(distToPlayerSq);
					// La cause de mort est la créature qui explose, pas le dernier coup reçu avant (plan 27 V4).
					_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, explosionDamage);
					_player.TakeDamage(explosionDamage * (1f - distToPlayer / explosionRadius), GlobalPosition);
				}
			}

			// Dégâts aux ennemis proches
			Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
			foreach (Node node in enemies)
			{
				if (node is Enemy { IsActive: true, IsDying: false } e && e != this && IsInstanceValid(e))
				{
					if (Iso.GroundDistanceSquared(GlobalPosition, e.GlobalPosition) < explosionRadiusSq)
						e.TakeDamage(explosionDamage * 0.5f, source: new AttackContext(0, null, 0, DamageKind.EnemyExplosion));
				}
			}

			CombatPools.Instance?.ShowExplosion(GlobalPosition, explosionRadius);
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

		// Mini-boss : drop un coffre épique garanti
		if (_tier == EnemyTier.Miniboss)
			SpawnRewardChest("chest_epic");
		if (_mods.IsVariant)
			GrantVariantRewards();

		// Retour au néant : éclats sombres, nuage de dissolution et flaque irisée, recyclés (plan 02 J0).
		bool miniboss = _tier == EnemyTier.Miniboss;
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

		if (!string.IsNullOrEmpty(variant.RewardChest) && RunRandom.Loot.Randf() < variant.RewardChestChance)
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

	/// <summary>
	/// Bas visible du sprite (dernière ligne opaque de la pose de repos), relatif au nœud : l'ombre s'y pose,
	/// sinon une créature dont les pieds ne sont pas à l'origine a l'air de flotter. Mesure partagée avec les marques.
	/// </summary>
	private float SpriteFeetY(string enemyId) =>
		EnemyStatusMarks.MeasureBody(enemyId, _sprite) is Rect2 body ? body.End.Y - 2f : 0f;

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
				_shadow.Position = new Vector2(0f, SpriteFeetY(data.Id));
				_facing.Reset(EnemySpriteLoader.HasEightDirections(frames));
				_currentAnimName = null;
				_attackAnimTimer = 0f;

				// Shader unifié : outline + hit flash + dissolve
				_spriteMaterial = new ShaderMaterial { Shader = _entityShader };
				_spriteMaterial.SetShaderParameter("outline_enabled", true);
				_spriteMaterial.SetShaderParameter("outline_color", GetOutlineColor(data));
				_sprite.Material = _spriteMaterial;
				_statusVisual.Attach(_spriteMaterial);
				if (_statusMarks == null)
				{
					_statusMarks = new EnemyStatusMarks { Name = "StatusMarks" };
					AddChild(_statusMarks);
				}
				_statusMarks.Configure(data.Id, _sprite);

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

		float tempo = _isDying ? 1f : AnimationTempo * _statusVisual.AnimationTempo;
		// Figée : l'animation s'arrête sur la pose en cours, sans repasser à l'attente.
		if (tempo > 0f)
			PlaySpriteAnim(SpriteAnimations[(int)_facing.Current, (int)action]);
		if (_sprite.SpeedScale != tempo)
			_sprite.SpeedScale = tempo;
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

		foreach (KeyValuePair<EnemyAbilityKind, EnemyAbilityData> entry in data.Abilities)
		{
			if (!_abilityCache.TryGetValue(entry.Key, out IEnemyAbility ability))
			{
				ability = EnemyAbilityFactory.Create(entry.Key, this);
				_abilityCache[entry.Key] = ability;
			}

			ability.Configure(entry.Value);
			_abilities.Add(ability);
			_abilityReplacesAttack |= ability.ReplacesBaseAttack;
		}
	}

	/// <summary>Une annonce à la fois : le Hurleur ne vise pas pendant son cri, pour que chaque signal reste lisible.</summary>
	internal bool IsAnotherAbilityActive(IEnemyAbility self)
	{
		foreach (IEnemyAbility ability in _abilities)
		{
			if (ability != self && ability.IsActive)
				return true;
		}
		return false;
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
		player.TakeDamage(damage, GlobalPosition);
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

	/// <summary>Enfouissement du Rampant : ni dégâts reçus, ne bloque personne, corps estompé.</summary>
	internal void SetBurrowed(bool burrowed)
	{
		_isBurrowed = burrowed;
		CollisionLayer = burrowed ? 0u : 2u;
		Modulate = burrowed ? new Color(1f, 1f, 1f, 0.35f) : Colors.White;
	}

	/// <summary>Annonce une apparition hors du SpawnManager (renforts), pour le suivi de run et l'audio.</summary>
	internal void EmitSpawned(string enemyId, float hpScale, float dmgScale)
	{
		_eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, enemyId, hpScale, dmgScale);
	}

	/// <summary>Teinte d'annonce brève sur le sprite (charge, cri).</summary>
	internal void FlashWarning(Color paletteColor, float duration)
	{
		if (_hasSprite)
			EnemyAttackFx.FlashWarning(_sprite, paletteColor, duration);
	}

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
