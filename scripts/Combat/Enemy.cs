using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

public partial class Enemy : CharacterBody2D
{
	private const float MeleeRange = 25f;
	private const float MeleeAttackCooldown = 1.0f;
	private const float RangedAttackCooldown = 1.5f;
	private const float DeathTweenDuration = 0.3f;

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

	private Polygon2D _visual;
	private Color _originalColor;
	private Player _player;
	private static PackedScene _damageNumberScene;
	private static PackedScene _enemyProjectileScene;
	private static PackedScene _xpOrbScene;

	public bool IsActive { get; private set; }

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

	public void Reset()
	{
		IsActive = false;
		_isDying = false;
		_currentHp = 0;
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

		if (_enemyType == "melee")
			ProcessMelee(distToPlayer, (float)delta);
		else if (_enemyType == "ranged")
			ProcessRanged(distToPlayer, (float)delta);

		MoveAndSlide();
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

	private void ShootProjectile()
	{
		if (_player == null || !IsInstanceValid(_player))
			return;

		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		EnemyProjectile projectile = _enemyProjectileScene.Instantiate<EnemyProjectile>();
		projectile.GlobalPosition = GlobalPosition;
		projectile.Initialize(direction, _damage);
		GetTree().CurrentScene.AddChild(projectile);
	}

	public void TakeDamage(float damage)
	{
		if (_currentHp <= 0 || _isDying)
			return;

		_currentHp -= damage;
		HitFlash();
		SpawnDamageNumber(damage);

		if (_currentHp <= 0)
			Die();
	}

	private void HitFlash()
	{
		_visual.Color = Colors.White;
		Tween tween = CreateTween();
		tween.TweenProperty(_visual, "color", _originalColor, 0.15f)
			.SetDelay(0.05f);
	}

	private void SpawnDamageNumber(float damage)
	{
		DamageNumber dmgNum = _damageNumberScene.Instantiate<DamageNumber>();
		dmgNum.GlobalPosition = GlobalPosition + new Vector2(0, -20);
		dmgNum.SetDamage(damage);
		GetTree().CurrentScene.AddChild(dmgNum);
	}

	private void Die()
	{
		_isDying = true;
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
			GetTree().CurrentScene.AddChild(orb);
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
			_visual.Polygon = new Vector2[] { new(-s * 0.75f, -s * 0.5f), new(s * 0.75f, 0), new(-s * 0.75f, s * 0.5f) };
		else
			_visual.Polygon = new Vector2[] { new(-s, 0), new(0, -s * 0.625f), new(s, 0), new(0, s * 0.625f) };
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
