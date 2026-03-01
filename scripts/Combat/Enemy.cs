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
	private float _speed;
	private float _damage;
	private float _attackRange;
	private float _xpReward;
	private string _enemyType;
	private string _enemyId;
	private string _behavior = "default";
	private bool _isDying;
	private float _attackTimer;

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

	// Aberration : version corrompue (nuit 7+)
	private bool _isAberration;
	private Polygon2D _aberrationAura;

	// Ignite DOT
	private float _igniteDps;
	private float _igniteTimer;

	private Polygon2D _visual;
	private Color _originalColor;
	private Player _player;
	private static PackedScene _damageNumberScene;
	private static PackedScene _enemyProjectileScene;
	private static PackedScene _xpOrbScene;
	private static PackedScene _chestScene;

	public bool IsActive { get; private set; }
	public bool IsDying => _isDying;
	public float HpRatio => _maxHp > 0 ? _currentHp / _maxHp : 0f;

	public override void _Ready()
	{
		_visual = GetNode<Polygon2D>("Visual");
		_originalColor = _visual.Color;
		_damageNumberScene ??= GD.Load<PackedScene>("res://scenes/combat/DamageNumber.tscn");
		_enemyProjectileScene ??= GD.Load<PackedScene>("res://scenes/combat/EnemyProjectile.tscn");
		_xpOrbScene ??= GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn");
		_chestScene ??= GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
	}

	public void Initialize(EnemyData data, float hpScale, float dmgScale)
	{
		_enemyId = data.Id;
		_enemyType = data.Type;
		_behavior = data.Behavior ?? "default";
		_tier = data.Tier ?? "normal";
		_maxHp = data.Stats.Hp * hpScale;
		_currentHp = _maxHp;
		_speed = data.Stats.Speed;
		_damage = data.Stats.Damage * dmgScale;
		_attackRange = data.Stats.AttackRange;
		_xpReward = data.Stats.XpReward;
		_isDying = false;
		_attackTimer = 0f;
		_nightMode = false;
		_igniteDps = 0f;
		_igniteTimer = 0f;
		_screamerTimer = ScreamerCryCooldown * 0.5f;
		_burrowerPhaseTimer = BurrowerPhaseInterval;
		_isBurrowed = false;
		_colosseChargeTimer = ColosseChargeInterval * 0.5f;
		_colosseSlamTimer = ColosseSlamCooldown;
		_isCharging = false;
		_chargeDurationLeft = 0f;
		IsActive = true;

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

		// Teinte sombre violacée
		Color aberrationTint = new(0.4f, 0.15f, 0.5f);
		_visual.Color = _visual.Color.Lerp(aberrationTint, 0.5f);
		_originalColor = _visual.Color;

		// Aura de particules sombres (ellipse pulsante)
		_aberrationAura = new Polygon2D();
		int segments = 12;
		Vector2[] points = new Vector2[segments];
		float auraSize = 20f;
		for (int i = 0; i < segments; i++)
		{
			float angle = Mathf.Tau * i / segments;
			points[i] = new Vector2(Mathf.Cos(angle) * auraSize, Mathf.Sin(angle) * auraSize * 0.5f);
		}
		_aberrationAura.Polygon = points;
		_aberrationAura.Color = new Color(0.15f, 0.05f, 0.2f, 0.3f);
		_aberrationAura.ZIndex = -1;
		AddChild(_aberrationAura);

		Tween auraTween = CreateTween();
		auraTween.SetLoops();
		auraTween.TweenProperty(_aberrationAura, "scale", Vector2.One * 1.3f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine);
		auraTween.TweenProperty(_aberrationAura, "scale", Vector2.One, 0.8f)
			.SetTrans(Tween.TransitionType.Sine);
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
		_screamerTimer = 0f;
		_burrowerPhaseTimer = 0f;
		_isAberration = false;
		if (_aberrationAura != null)
		{
			_aberrationAura.QueueFree();
			_aberrationAura = null;
		}
		CollisionLayer = 2;
		CollisionMask = 4;
		Velocity = Vector2.Zero;
		Visible = false;
		Scale = Vector2.One;
		SetPhysicsProcess(false);
		SetProcess(false);

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

		float distToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
		float dt = (float)delta;

		ProcessIgnite(dt);
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
		else if (_nightMode && distToPlayer > PlayerProximityRange)
		{
			ProcessNightMovement(distToPlayer, dt);
		}
		else if (_enemyType == "melee")
		{
			ProcessMelee(distToPlayer, dt);
		}
		else if (_enemyType == "ranged")
		{
			ProcessRanged(distToPlayer, dt);
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
			case "colosse":
				ProcessColosseAbilities(distToPlayer, delta);
				break;
		}
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
				GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage * 1.5f);
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
		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage * 2f);
		_player.TakeDamage(_damage * 2f);

		// Knockback joueur
		Vector2 knockbackDir = (_player.GlobalPosition - GlobalPosition).Normalized();
		_player.Velocity += knockbackDir * 300f;

		// Dégâts AoE aux structures proches
		Godot.Collections.Array<Node> structures = GetTree().GetNodesInGroup("structures");
		foreach (Node node in structures)
		{
			if (node is Base.Structure structure && !structure.IsDestroyed)
			{
				float dist = GlobalPosition.DistanceTo(structure.GlobalPosition);
				if (dist < ColosseSlamAoeRadius)
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

	/// <summary>Garde : attaque le joueur s'il est dans le rayon de patrouille, sinon retourne au poste.</summary>
	private void ProcessGuardBehavior(float distToPlayer, float delta)
	{
		_attackTimer -= delta;
		float distToPost = GlobalPosition.DistanceTo(_guardPosition);

		if (distToPlayer < GuardPatrolRadius)
		{
			// Joueur dans la zone : comportement d'attaque normal
			if (_enemyType == "melee")
			{
				Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
				Velocity = direction * _speed;

				if (distToPlayer < MeleeRange && _attackTimer <= 0f)
				{
					GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage);
					_player.TakeDamage(_damage);
					_attackTimer = MeleeAttackCooldown;
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
					_attackTimer = RangedAttackCooldown;
				}
			}
		}
		else if (distToPost > 10f)
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
			float distToWall = GlobalPosition.DistanceTo(blockingWall.GlobalPosition);
			Vector2 dirToWall = (blockingWall.GlobalPosition - GlobalPosition).Normalized();
			Velocity = dirToWall * _speed;

			if (distToWall < MeleeRange && _attackTimer <= 0f)
			{
				blockingWall.TakeDamage(_damage);
				_attackTimer = MeleeAttackCooldown;
			}
			return;
		}

		Vector2 directionToFoyer = (_foyerPosition - GlobalPosition).Normalized();
		Velocity = directionToFoyer * _speed;

		if (_enemyType == "melee")
		{
			float distToFoyer = GlobalPosition.DistanceTo(_foyerPosition);
			if (distToFoyer < MeleeRange && distToPlayer < PlayerProximityRange * 2f && _attackTimer <= 0f)
			{
				GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage);
				_player.TakeDamage(_damage);
				_attackTimer = MeleeAttackCooldown;
			}
		}
		else if (_enemyType == "ranged" && _attackTimer <= 0f)
		{
			float distToFoyer = GlobalPosition.DistanceTo(_foyerPosition);
			if (distToFoyer <= _attackRange)
			{
				ShootProjectile();
				_attackTimer = RangedAttackCooldown;
			}
		}
	}

	private void ProcessMelee(float distToPlayer, float delta)
	{
		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * _speed;

		_attackTimer -= delta;
		if (distToPlayer < MeleeRange && _attackTimer <= 0f)
		{
			GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.PlayerHitBy, _enemyId, _damage);
			_player.TakeDamage(_damage);
			_attackTimer = MeleeAttackCooldown;
		}
	}

	private void ProcessRanged(float distToPlayer, float delta)
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

		_attackTimer -= delta;
		if (distToPlayer <= _attackRange && _attackTimer <= 0f)
		{
			ShootProjectile();
			_attackTimer = RangedAttackCooldown;
		}
	}

	private Structure FindNearestStructure()
	{
		Godot.Collections.Array<Node> structures = GetTree().GetNodesInGroup("structures");
		Structure nearest = null;
		float nearestDist = StructureDetectRange;

		Vector2 dirToFoyer = (_foyerPosition - GlobalPosition).Normalized();

		foreach (Node node in structures)
		{
			if (node is Structure structure && !structure.IsDestroyed)
			{
				float dist = GlobalPosition.DistanceTo(structure.GlobalPosition);
				if (dist >= nearestDist)
					continue;

				Vector2 dirToStructure = (structure.GlobalPosition - GlobalPosition).Normalized();
				float dot = dirToFoyer.Dot(dirToStructure);
				if (dot > 0.3f)
				{
					nearest = structure;
					nearestDist = dist;
				}
			}
		}

		return nearest;
	}

	private void ShootProjectile()
	{
		if (_player == null || !IsInstanceValid(_player))
			return;

		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		PlayRangedAttackVfx(direction);
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
		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.EntityDamaged, this, damage);
		HitFlash();
		SpawnDamageNumber(damage, isCrit);

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

		// Visual feedback: tint orange while ignited
		_visual.Color = new Color(1f, 0.5f, 0.1f);
	}

	private void ProcessIgnite(float delta)
	{
		if (_igniteTimer <= 0f)
			return;

		_igniteTimer -= delta;
		float igniteDamage = _igniteDps * delta;
		_currentHp -= igniteDamage;
		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.EntityDamaged, this, igniteDamage);

		if (_igniteTimer <= 0f)
		{
			_igniteDps = 0f;
			_visual.Color = _originalColor;
		}

		if (_currentHp <= 0 && !_isDying)
			Die();
	}

	private void HitFlash()
	{
		_visual.Color = Colors.White;
		Tween tween = CreateTween();
		tween.TweenProperty(_visual, "color", _originalColor, 0.15f)
			.SetDelay(0.05f);
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

		if (IsInGroup("enemies"))
			RemoveFromGroup("enemies");

		// Notifier le POI gardé si c'est un garde
		if (_guardTarget != null && IsInstanceValid(_guardTarget))
			_guardTarget.OnGuardKilled();

		EventBus eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.EmitSignal(EventBus.SignalName.EnemyKilled, _enemyId, GlobalPosition);

		SpawnXpOrbs();
		TryDropWeapon();

		// Mini-boss : drop un coffre épique garanti
		if (_tier == "miniboss")
			SpawnMinibossChest();

		SpawnDisintegrationParticles();

		Tween tween = CreateTween();
		tween.SetParallel();
		tween.TweenProperty(this, "scale", Vector2.Zero, DeathTweenDuration);
		tween.TweenProperty(this, "modulate:a", 0f, DeathTweenDuration);
		tween.Chain().TweenCallback(Callable.From(OnDeathComplete));
	}

	/// <summary>Désintégration en particules sombres iridescentes — retour au néant.</summary>
	private void SpawnDisintegrationParticles()
	{
		int count = _tier == "miniboss" ? 16 : (_isAberration ? 10 : 6);
		float baseSize = _visual != null ? Mathf.Max(_visual.Scale.X * 3f, 3f) : 3f;
		Vector2 origin = GlobalPosition;
		Color baseColor = _originalColor.Lerp(new Color(0.08f, 0.05f, 0.12f), 0.6f);

		for (int i = 0; i < count; i++)
		{
			Polygon2D particle = new();
			float ps = (float)GD.RandRange(baseSize * 0.4f, baseSize);
			particle.Polygon = new Vector2[]
			{
				new(-ps, 0), new(0, -ps * 0.6f), new(ps, 0), new(0, ps * 0.6f)
			};

			// Teinte iridescente : variation violet/bleu/noir
			float hueShift = (float)GD.RandRange(-0.08f, 0.08f);
			particle.Color = new Color(
				Mathf.Clamp(baseColor.R + hueShift, 0f, 0.3f),
				Mathf.Clamp(baseColor.G + hueShift * 0.5f, 0f, 0.2f),
				Mathf.Clamp(baseColor.B + hueShift + 0.1f, 0f, 0.4f),
				0.85f
			);

			float angle = (float)GD.RandRange(0, Mathf.Tau);
			float dist = (float)GD.RandRange(2f, 6f);
			particle.GlobalPosition = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
			particle.Rotation = (float)GD.RandRange(0, Mathf.Tau);

			GetTree().CurrentScene.AddChild(particle);

			// Trajectoire : s'éloigne du centre puis fade
			float speed = (float)GD.RandRange(30f, 80f);
			float lifetime = (float)GD.RandRange(0.3f, 0.6f);
			Vector2 targetPos = particle.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * lifetime;

			Tween pTween = particle.CreateTween();
			pTween.SetParallel();
			pTween.TweenProperty(particle, "global_position", targetPos, lifetime)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			pTween.TweenProperty(particle, "modulate:a", 0f, lifetime)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			pTween.TweenProperty(particle, "scale", Vector2.Zero, lifetime)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			pTween.Chain().TweenCallback(Callable.From(() => particle.QueueFree()));
		}
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
	}

	private void CachePlayer()
	{
		if (_player != null && IsInstanceValid(_player))
			return;

		Node playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player p)
			_player = p;
	}
}
