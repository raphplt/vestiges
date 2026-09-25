using System;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Projectile ennemi recyclé par CombatPools : lancé par Launch, rendu au pool à l'impact ou en fin de course.
/// Sprite et couleurs propres à chaque créature (bloc visual.projectile du JSON), à hauteur de buste,
/// jamais masqué : c'est un danger.
/// </summary>
public partial class EnemyProjectile : Area2D
{
	[Export] public float Speed = 185f;
	[Export] public float MaxLifetime = 4f;

	private Vector2 _direction;
	private float _damage;
	private string _sourceEnemyId = "enemy_projectile";
	private float _slowDuration;
	private float _slowFactor = 1f;
	private float _age;
	private const float FlightHeight = 10f;

	private Sprite2D _visual;
	private ProjectileSprites.SpriteSet _spriteSet;
	private int _spriteFrame = -1;
	private ulong _trailFrame;
	private FxFamily _family = FxFamily.Hostile;
	private bool _isDespawning;
	private Action<EnemyProjectile> _release;
	private EventBus _eventBus;

	public void SetRelease(Action<EnemyProjectile> release)
	{
		_release = release;
	}

	public override void _Ready()
	{
		_visual = GetNode<Sprite2D>("Visual");
		_visual.Position = new Vector2(0f, -FlightHeight);
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BodyEntered += OnBodyEntered;
	}

	public void Launch(Vector2 position, Vector2 direction, float damage, string sourceEnemyId, string spriteId, FxFamily family,
		float slowFactor = 1f, float slowDuration = 0f)
	{
		GlobalPosition = position;
		_direction = direction.Normalized();
		_damage = damage;
		_sourceEnemyId = sourceEnemyId;
		_slowFactor = slowFactor;
		_slowDuration = slowDuration;
		_age = 0f;
		_isDespawning = false;
		_family = family;
		_spriteSet = ProjectileSprites.Get(spriteId) ?? ProjectileSprites.Get("spit");
		_spriteFrame = -1;
		_visual.Modulate = new Color(1f, 1f, 1f, CombatFxSettings.EnemyOpacity);
		UpdateSprite();
		Visible = true;
		ProcessMode = ProcessModeEnum.Inherit;
		SetDeferred(Area2D.PropertyName.Monitoring, true);
	}

	private void UpdateSprite()
	{
		if (_spriteSet == null)
			return;
		int frame = (int)(_age * _spriteSet.Fps) % _spriteSet.Frames;
		if (frame == _spriteFrame)
			return;
		_spriteFrame = frame;
		_visual.Texture = _spriteSet.Get(0, frame);
	}

	/// <summary>Gouttes ou éclats qui retombent derrière le projectile, un toutes les trois frames.</summary>
	private void EmitTrail()
	{
		ulong frame = Engine.GetPhysicsFrames();
		if (frame - _trailFrame < 3 || CombatPools.Instance == null)
			return;
		_trailFrame = frame;
		CombatPools.Instance.EmitSparks(GlobalPosition + new Vector2(0f, -FlightHeight), new SparkBurst
		{
			Family = _family,
			Owner = FxOwner.Enemy,
			Count = 1,
			Direction = -_direction,
			Spread = 0.8f,
			SpeedMin = 8f,
			SpeedMax = 24f,
			LifeMin = 0.15f,
			LifeMax = 0.25f,
			Size = 1,
		});
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDespawning)
			return;

		_age += (float)delta;
		if (_age >= MaxLifetime)
		{
			// Différé comme à l'impact : le retour au pool et la désactivation doivent passer ensemble.
			_isDespawning = true;
			CallDeferred(MethodName.Release);
			return;
		}
		Position += _direction * Speed * (float)delta;
		UpdateSprite();
		EmitTrail();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_isDespawning || body is not Player player)
			return;

		_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _sourceEnemyId, _damage);
		player.TakeDamage(_damage);
		if (_slowDuration > 0f)
			player.ApplySlow(_slowFactor, _slowDuration);
		StartDespawn();
	}

	private void StartDespawn()
	{
		_isDespawning = true;
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		CombatPools.Instance?.ShowEnemyImpact(GlobalPosition + new Vector2(0f, -FlightHeight), _direction, _family);
		CallDeferred(MethodName.Release);
	}

	private void Release()
	{
		_isDespawning = true;
		Visible = false;
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		// Hors traitement : retiré de la physique (DisableMode Remove) jusqu'au prochain Launch.
		SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
		if (_release != null)
			_release(this);
		else
			QueueFree();
	}
}
