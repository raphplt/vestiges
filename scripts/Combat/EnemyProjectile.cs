using System;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Projectile ennemi recyclé par CombatPools : lancé par Launch, rendu au pool à l'impact ou en fin de course.
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
	private Polygon2D _visual;
	private Polygon2D _trail;
	private Tween _tween;
	private bool _isDespawning;
	private Action<EnemyProjectile> _release;
	private EventBus _eventBus;

	public void SetRelease(Action<EnemyProjectile> release)
	{
		_release = release;
	}

	public override void _Ready()
	{
		_visual = GetNodeOrNull<Polygon2D>("Visual");
		_trail = GetNodeOrNull<Polygon2D>("Trail");
		_eventBus = GetNode<EventBus>("/root/EventBus");
		BodyEntered += OnBodyEntered;
	}

	public void Launch(Vector2 position, Vector2 direction, float damage, string sourceEnemyId, float slowFactor = 1f, float slowDuration = 0f)
	{
		GlobalPosition = position;
		_direction = direction.Normalized();
		Rotation = _direction.Angle();
		_damage = damage;
		_sourceEnemyId = sourceEnemyId;
		_slowFactor = slowFactor;
		_slowDuration = slowDuration;
		_age = 0f;
		_isDespawning = false;
		Visible = true;
		ProcessMode = ProcessModeEnum.Inherit;
		SetDeferred(Area2D.PropertyName.Monitoring, true);

		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetParallel();
		if (_visual != null)
		{
			_visual.Scale = new Vector2(0.3f, 0.3f);
			_visual.Modulate = Colors.White;
			_tween.TweenProperty(_visual, "scale", Vector2.One, 0.1f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}
		if (_trail != null)
		{
			_trail.Scale = new Vector2(0f, 0.3f);
			_trail.Modulate = Colors.White;
			_tween.TweenProperty(_trail, "scale", Vector2.One, 0.15f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDespawning)
			return;

		_age += (float)delta;
		if (_age >= MaxLifetime)
		{
			Release();
			return;
		}
		Position += _direction * Speed * (float)delta;
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
		if (_visual == null)
		{
			CallDeferred(MethodName.Release);
			return;
		}

		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetParallel();
		_tween.TweenProperty(_visual, "scale", new Vector2(1.5f, 1.5f), 0.06f);
		_tween.TweenProperty(_visual, "modulate:a", 0f, 0.06f);
		if (_trail != null)
		{
			_tween.TweenProperty(_trail, "scale", new Vector2(0.2f, 0.2f), 0.06f);
			_tween.TweenProperty(_trail, "modulate:a", 0f, 0.06f);
		}
		_tween.Chain().TweenCallback(Callable.From(Release));
	}

	private void Release()
	{
		_tween?.Kill();
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
