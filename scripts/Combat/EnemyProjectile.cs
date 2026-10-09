using System;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Projectile ennemi recyclé par CombatPools : lancé par Launch, rendu au pool à l'impact ou en fin de course.
/// Sprite et couleurs propres à chaque créature (bloc visual.projectile du JSON), à hauteur de buste,
/// jamais masqué : c'est un danger.
/// </summary>
public partial class EnemyProjectile : Node2D, ITicked
{
	// Godot n'appelle plus les projectiles un par un : une seule boucle C# les avance tous (plan 29).
	private static readonly TickRoster<EnemyProjectile> Roster = new("EnemyProjectiles");
	public int TickSlot { get; set; } = -1;

	[Export] public float Speed = 185f;
	[Export] public float MaxLifetime = 4f;
	/// <summary>Rayon du tir : il touche le joueur dont le corps est à cette distance, sans zone physique (plan 29).</summary>
	[Export] public float HitRadius = 5f;

	private Vector2 _direction;
	private float _damage;
	private string _sourceEnemyId = "enemy_projectile";
	private float _slowDuration;
	private float _slowFactor = 1f;
	private float _age;
	private Sprite2D _visual;
	// Ombre dessinée en lot par GroundShadowLayer : des centaines de tirs en vol en foule dense (plan 29 C2).
	private readonly ShadowCaster _shadow;
	private ProjectileSprites.SpriteSet _spriteSet;
	private int _spriteFrame = -1;
	private int _spriteDirection;
	private ulong _trailFrame;
	private FxFamily _family = FxFamily.Hostile;
	private bool _isDespawning;
	private Action<EnemyProjectile> _release;
	private EventBus _eventBus;
	private GroupCache _groups;

	public EnemyProjectile()
	{
		_shadow = new ShadowCaster(this);
	}

	public void SetRelease(Action<EnemyProjectile> release)
	{
		_release = release;
	}

	public override void _Ready()
	{
		_visual = GetNode<Sprite2D>("Visual");
		_visual.Position = new Vector2(0f, -Iso.FlightHeight);
		// Au sol sous le projectile : c'est l'écart entre l'ombre et le visuel qui dit qu'il vole.
		_visual.TextureFilter = TextureFilterEnum.Nearest;
		_eventBus = GetNode<EventBus>("/root/EventBus");
		_groups = GetNode<GroupCache>("/root/GroupCache");
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
		_shadow.Width = GroundShadow.SnapWidth(_spriteSet?.ShadowWidth ?? 8f);
		GroundShadowLayer.Add(_shadow);
		_trailFrame = Engine.GetPhysicsFrames();
		_spriteFrame = -1;
		// Planche prérendue en vue 30° : la colonne suit la direction du tir, jamais une rotation 2D.
		_spriteDirection = _spriteSet?.DirectionIndex(_direction) ?? 0;
		_visual.Modulate = new Color(1f, 1f, 1f, CombatFxSettings.EnemyOpacity);
		UpdateSprite();
		Visible = true;
		Roster.Add(this);
	}

	private void UpdateSprite()
	{
		if (_spriteSet == null)
			return;
		int frame = _spriteSet.FrameAt(_age);
		if (frame == _spriteFrame)
			return;
		_spriteFrame = frame;
		_visual.Texture = _spriteSet.Get(_spriteDirection, frame);
	}

	/// <summary>Gouttes ou éclats qui retombent derrière le projectile, un toutes les trois frames.</summary>
	private void EmitTrail()
	{
		ulong frame = Engine.GetPhysicsFrames();
		if (frame - _trailFrame < 3 || CombatPools.Instance == null)
			return;
		_trailFrame = frame;
		CombatPools.Instance.EmitSparks(GlobalPosition + new Vector2(0f, -Iso.FlightHeight), new SparkBurst
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

	public override void _ExitTree()
	{
		Roster.Remove(this);
		GroundShadowLayer.Remove(_shadow);
	}

	/// <summary>Un tick du projectile, appelé par la boucle des projectiles (ou directement par un test).</summary>
	public void PhysicsTick(double delta)
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
		if (_groups.GetPlayer() is Player player)
		{
			float reach = HitRadius + player.BodyRadius;
			if (GlobalPosition.DistanceSquaredTo(player.GlobalPosition) <= reach * reach)
			{
				HitPlayer(player);
				return;
			}
		}
		UpdateSprite();
		EmitTrail();
	}

	private void HitPlayer(Player player)
	{
		if (_isDespawning)
			return;
		_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _sourceEnemyId, _damage);
		PlayerDamageResult hit = player.TakeDamage(_damage, GlobalPosition - _direction * 8f);
		// La toile ne colle qu'à un coup qui porte : ni bouclier, ni invulnérabilité, ni dash (plan 27 V3b).
		if (_slowDuration > 0f && hit.Applied && !hit.ShieldAbsorbed)
			player.ApplySlow(_slowFactor, _slowDuration);
		StartDespawn();
	}

	private void StartDespawn()
	{
		_isDespawning = true;
		if (_spriteSet?.Impact != null)
			CombatPools.Instance?.ShowProjectileImpact(GlobalPosition, _spriteSet.Impact);
		else
			CombatPools.Instance?.ShowEnemyImpact(GlobalPosition + new Vector2(0f, -Iso.FlightHeight), _direction, _family);
		CallDeferred(MethodName.Release);
	}

	private void Release()
	{
		Roster.Remove(this);
		GroundShadowLayer.Remove(_shadow);
		_isDespawning = true;
		Visible = false;
		if (_release != null)
			_release(this);
		else
			QueueFree();
	}
}
