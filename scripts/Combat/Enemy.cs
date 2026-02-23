using Godot;
using Vestiges.Base;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

public partial class Enemy : CharacterBody2D
{
	private const float MeleeRange = 25f;
	private const float MeleeAttackCooldown = 1.0f;
	private const float RangedAttackCooldown = 1.5f;
	private const float DeathTweenDuration = 0.3f;
	private const float PlayerProximityRange = 80f;
	private const float StructureDetectRange = 40f;

	private float _maxHp;
	private float _currentHp;
	private float _speed;
	private float _damage;
	private float _attackRange;
	private float _xpReward;
	private string _enemyType;
	private string _enemyId;
	private bool _isDying;
	private float _attackTimer;

	private bool _nightMode;
	private Vector2 _foyerPosition;

	// Ignite DOT
	private float _igniteDps;
	private float _igniteTimer;

	private Polygon2D _visual;
	private Color _originalColor;
	private Player _player;
	private static PackedScene _damageNumberScene;
	private static PackedScene _enemyProjectileScene;
	private static PackedScene _xpOrbScene;

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
	}

	public void Initialize(EnemyData data, float hpScale, float dmgScale)
	{
		_enemyId = data.Id;
		_enemyType = data.Type;
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

	public void Reset()
	{
		IsActive = false;
		_isDying = false;
		_nightMode = false;
		_currentHp = 0;
		_igniteDps = 0f;
		_igniteTimer = 0f;
		Velocity = Vector2.Zero;
		Visible = false;
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

		if (_nightMode && distToPlayer > PlayerProximityRange)
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
		projectile.Initialize(direction, _damage);
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
		if (_currentHp <= 0 || _isDying)
			return;

		_currentHp -= damage;
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

		EventBus eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.EmitSignal(EventBus.SignalName.EnemyKilled, _enemyId, GlobalPosition);

		SpawnXpOrbs();

		Tween tween = CreateTween();
		tween.SetParallel();
		tween.TweenProperty(this, "scale", Vector2.Zero, DeathTweenDuration);
		tween.TweenProperty(this, "modulate:a", 0f, DeathTweenDuration);
		tween.Chain().TweenCallback(Callable.From(OnDeathComplete));
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
