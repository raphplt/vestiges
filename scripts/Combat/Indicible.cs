using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// L'Indicible — boss rare nuit 10+. Trop grand pour l'écran.
/// Reste aux bords, projette des tentacules et des yeux mouvants.
/// Ne rentre pas dans le rayon du Foyer : il l'engloutit.
/// </summary>
public partial class Indicible : Node2D
{
	private const float TentacleInterval = 2.5f;
	private const float TentacleWarningDuration = 0.8f;
	private const float TentacleDamage = 15f;
	private const float TentacleWidth = 30f;
	private const float EyeShiftInterval = 3f;
	private const float EdgeOffset = 40f;
	private const int MaxTentacles = 3;
	private const int EyeCount = 5;
	private const int ScoreReward = 5000;

	private float _maxHp;
	private float _currentHp;
	private float _hpScale;
	private float _dmgScale;
	private float _tentacleTimer;
	private float _eyeShiftTimer;
	private bool _isDying;
	private bool _isActive;
	private int _phase; // 0 = idle, 1 = active (HP > 50%), 2 = enraged (HP <= 50%)

	private Player _player;
	private Camera2D _camera;
	private EventBus _eventBus;
	private Vector2 _foyerPosition;

	private readonly List<Node2D> _edgeSegments = new();
	private readonly List<Polygon2D> _eyes = new();

	private static PackedScene _damageNumberScene;

	public override void _Ready()
	{
		_damageNumberScene ??= GD.Load<PackedScene>("res://scenes/combat/DamageNumber.tscn");
		_eventBus = GetNode<EventBus>("/root/EventBus");
		AddToGroup("enemies");
		AddToGroup("indicible");
	}

	public void Initialize(float hpScale, float dmgScale, Vector2 foyerPosition)
	{
		_hpScale = hpScale;
		_dmgScale = dmgScale;
		_foyerPosition = foyerPosition;

		EnemyData data = EnemyDataLoader.Get("indicible");
		_maxHp = (data?.Stats.Hp ?? 2000f) * hpScale;
		_currentHp = _maxHp;
		_isActive = true;
		_isDying = false;
		_phase = 1;
		_tentacleTimer = TentacleInterval * 0.3f;
		_eyeShiftTimer = 1f;

		GlobalPosition = foyerPosition;

		BuildEdgePresence();
		SpawnEyes();

		_eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, "indicible", hpScale, dmgScale);
		GD.Print($"[Indicible] L'Indicible émerge... (HP: {_maxHp:F0})");
	}

	public override void _Process(double delta)
	{
		if (!_isActive || _isDying)
			return;

		CachePlayer();
		if (_player == null || !IsInstanceValid(_player))
			return;

		float dt = (float)delta;

		// Phase enragée à 50% HP
		if (_phase == 1 && _currentHp <= _maxHp * 0.5f)
		{
			_phase = 2;
			EnterEnragedPhase();
		}

		_tentacleTimer -= dt;
		if (_tentacleTimer <= 0f)
		{
			int tentacleCount = _phase == 2 ? MaxTentacles : 2;
			for (int i = 0; i < tentacleCount; i++)
				SpawnTentacleAttack();
			_tentacleTimer = _phase == 2 ? TentacleInterval * 0.6f : TentacleInterval;
		}

		_eyeShiftTimer -= dt;
		if (_eyeShiftTimer <= 0f)
		{
			ShiftEyes();
			_eyeShiftTimer = EyeShiftInterval;
		}

		PulseEdgePresence(dt);
	}

	public void TakeDamage(float damage)
	{
		if (_isDying || !_isActive)
			return;

		_currentHp -= damage;
		SpawnDamageNumber(damage);
		FlashEdges();

		if (_currentHp <= 0)
			Die();
	}

	/// <summary>Construit la présence visuelle aux 4 bords de l'écran + hitboxes.</summary>
	private void BuildEdgePresence()
	{
		Color darkColor = new(0.1f, 0.04f, 0.18f, 0.7f);
		float segmentLength = 200f;

		// 4 segments de bord (haut, bas, gauche, droite)
		for (int edge = 0; edge < 4; edge++)
		{
			Node2D segment = new();
			Polygon2D body = new();

			float w = edge < 2 ? segmentLength : EdgeOffset;
			float h = edge < 2 ? EdgeOffset : segmentLength;

			body.Polygon = new Vector2[]
			{
				new(-w, -h), new(w, -h), new(w, h), new(-w, h)
			};
			body.Color = darkColor;

			// Excroissances organiques sur le bord intérieur
			for (int j = 0; j < 3; j++)
			{
				Polygon2D tendril = new();
				float tx = (float)GD.RandRange(-w * 0.6f, w * 0.6f);
				float ty = (float)GD.RandRange(-h * 0.6f, h * 0.6f);
				float ts = (float)GD.RandRange(8f, 20f);
				tendril.Polygon = new Vector2[]
				{
					new(0, -ts), new(ts * 0.4f, 0), new(0, ts), new(-ts * 0.4f, 0)
				};
				tendril.Color = new Color(0.15f, 0.06f, 0.25f, 0.5f);
				tendril.Position = new Vector2(tx, ty);
				body.AddChild(tendril);
			}

			// Hitbox : Area2D qui détecte les projectiles joueur
			Area2D hitbox = new();
			hitbox.CollisionLayer = 0;
			hitbox.CollisionMask = 4; // Même mask que les ennemis pour détecter les projectiles
			CollisionShape2D shape = new();
			RectangleShape2D rect = new();
			rect.Size = new Vector2(w * 2f, h * 2f);
			shape.Shape = rect;
			hitbox.AddChild(shape);
			hitbox.AreaEntered += OnHitboxAreaEntered;
			segment.AddChild(hitbox);

			segment.AddChild(body);
			AddChild(segment);
			_edgeSegments.Add(segment);
		}

		PositionEdgeSegments();
	}

	private void OnHitboxAreaEntered(Area2D area)
	{
		if (area is Projectile projectile && !_isDying)
		{
			TakeDamage(20f); // Dégâts fixes des projectiles sur L'Indicible
			projectile.QueueFree();
		}
	}

	private void PositionEdgeSegments()
	{
		if (_edgeSegments.Count < 4)
			return;

		// Position relative au foyer (sera ajusté par la caméra)
		float spread = 350f;
		_edgeSegments[0].Position = new Vector2(0, -spread); // haut
		_edgeSegments[1].Position = new Vector2(0, spread);  // bas
		_edgeSegments[2].Position = new Vector2(-spread, 0); // gauche
		_edgeSegments[3].Position = new Vector2(spread, 0);  // droite
	}

	/// <summary>Spawn des yeux sur les bords qui bougent périodiquement.</summary>
	private void SpawnEyes()
	{
		for (int i = 0; i < EyeCount; i++)
		{
			Polygon2D eye = new();
			float eyeSize = (float)GD.RandRange(4f, 8f);

			// Forme d'œil : ovale horizontal
			int segments = 8;
			Vector2[] points = new Vector2[segments];
			for (int s = 0; s < segments; s++)
			{
				float angle = Mathf.Tau * s / segments;
				points[s] = new Vector2(Mathf.Cos(angle) * eyeSize, Mathf.Sin(angle) * eyeSize * 0.5f);
			}
			eye.Polygon = points;
			eye.Color = new Color(0.4f, 0.9f, 0.2f, 0.8f); // Vert acide (yeux des créatures)

			// Pupille
			Polygon2D pupil = new();
			float pupilSize = eyeSize * 0.35f;
			Vector2[] pupilPoints = new Vector2[6];
			for (int s = 0; s < 6; s++)
			{
				float angle = Mathf.Tau * s / 6;
				pupilPoints[s] = new Vector2(Mathf.Cos(angle) * pupilSize, Mathf.Sin(angle) * pupilSize);
			}
			pupil.Polygon = pupilPoints;
			pupil.Color = new Color(0.05f, 0.02f, 0.08f, 0.95f);
			eye.AddChild(pupil);

			// Position aléatoire sur un bord
			PlaceEyeOnEdge(eye);
			AddChild(eye);
			_eyes.Add(eye);
		}
	}

	private void PlaceEyeOnEdge(Polygon2D eye)
	{
		float spread = 330f;
		int edge = (int)(GD.Randi() % 4);
		float offset = (float)GD.RandRange(-150f, 150f);

		eye.Position = edge switch
		{
			0 => new Vector2(offset, -spread),
			1 => new Vector2(offset, spread),
			2 => new Vector2(-spread, offset),
			_ => new Vector2(spread, offset)
		};
	}

	private void ShiftEyes()
	{
		foreach (Polygon2D eye in _eyes)
		{
			if (!IsInstanceValid(eye))
				continue;

			Vector2 newPos = eye.Position;
			PlaceEyeOnEdge(eye);
			Vector2 target = eye.Position;
			eye.Position = newPos;

			Tween tween = eye.CreateTween();
			tween.TweenProperty(eye, "position", target, 1.2f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		}
	}

	/// <summary>Attaque tentaculaire : indicateur au sol → dégâts après warning.</summary>
	private void SpawnTentacleAttack()
	{
		if (_player == null || !IsInstanceValid(_player))
			return;

		// Cible : position du joueur + léger offset aléatoire
		Vector2 targetPos = _player.GlobalPosition + new Vector2(
			(float)GD.RandRange(-60f, 60f),
			(float)GD.RandRange(-60f, 60f)
		);

		// Direction depuis un bord aléatoire
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		float tentacleLength = 120f;
		Vector2 startPos = targetPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * tentacleLength * 0.5f;
		Vector2 endPos = targetPos - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * tentacleLength * 0.5f;

		// Phase 1 : Indicateur de warning (zone rouge semi-transparente)
		Polygon2D warning = new();
		float hw = TentacleWidth * 0.5f;
		Vector2 dir = (endPos - startPos).Normalized();
		Vector2 perp = new(-dir.Y, dir.X);

		warning.Polygon = new Vector2[]
		{
			startPos + perp * hw,
			endPos + perp * hw,
			endPos - perp * hw,
			startPos - perp * hw
		};
		warning.Color = new Color(0.8f, 0.1f, 0.1f, 0.2f);
		GetTree().CurrentScene.AddChild(warning);

		// Flash du warning
		Tween warnTween = warning.CreateTween();
		warnTween.TweenProperty(warning, "modulate:a", 0.6f, TentacleWarningDuration * 0.5f)
			.SetTrans(Tween.TransitionType.Sine);
		warnTween.TweenProperty(warning, "modulate:a", 0.2f, TentacleWarningDuration * 0.5f);

		// Phase 2 : Après le warning, la tentacule frappe
		float damage = TentacleDamage * _dmgScale;
		Vector2[] tentacleShape = warning.Polygon;
		GetTree().CreateTimer(TentacleWarningDuration).Timeout += () =>
		{
			if (IsInstanceValid(warning))
				warning.QueueFree();

			if (_isDying || !_isActive)
				return;

			// Tentacule visuelle
			Polygon2D tentacle = new();
			tentacle.Polygon = tentacleShape;
			tentacle.Color = new Color(0.12f, 0.04f, 0.2f, 0.9f);
			GetTree().CurrentScene.AddChild(tentacle);

			// Dégâts au joueur s'il est dans la zone
			if (IsInstanceValid(_player))
			{
				float distToLine = DistancePointToSegment(_player.GlobalPosition, startPos, endPos);
				if (distToLine < TentacleWidth)
				{
					_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, "indicible", damage);
					_player.TakeDamage(damage);
				}
			}

			// Dégâts aux structures dans la zone
			Godot.Collections.Array<Node> structures = GetTree().GetNodesInGroup("structures");
			foreach (Node node in structures)
			{
				if (node is Vestiges.Base.Structure structure && !structure.IsDestroyed)
				{
					float dist = DistancePointToSegment(structure.GlobalPosition, startPos, endPos);
					if (dist < TentacleWidth)
						structure.TakeDamage(damage * 0.5f);
				}
			}

			// Fade out de la tentacule
			Tween fadeTween = tentacle.CreateTween();
			fadeTween.TweenProperty(tentacle, "modulate:a", 0f, 0.4f);
			fadeTween.TweenCallback(Callable.From(() => tentacle.QueueFree()));
		};
	}

	private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float t = Mathf.Clamp((point - a).Dot(ab) / ab.LengthSquared(), 0f, 1f);
		Vector2 closest = a + ab * t;
		return point.DistanceTo(closest);
	}

	private void PulseEdgePresence(float delta)
	{
		// Les segments pulsent lentement
		float pulse = Mathf.Sin((float)Time.GetTicksMsec() * 0.001f) * 0.1f + 0.9f;
		foreach (Node2D segment in _edgeSegments)
		{
			if (IsInstanceValid(segment))
				segment.Scale = Vector2.One * pulse;
		}
	}

	private void EnterEnragedPhase()
	{
		GD.Print("[Indicible] Phase enragée !");

		// Tous les yeux deviennent rouges
		foreach (Polygon2D eye in _eyes)
		{
			if (IsInstanceValid(eye))
			{
				Tween tween = eye.CreateTween();
				tween.TweenProperty(eye, "color", new Color(0.9f, 0.15f, 0.1f, 0.9f), 0.5f);
			}
		}

		// Flash rouge sur les bords
		foreach (Node2D segment in _edgeSegments)
		{
			if (!IsInstanceValid(segment))
				continue;
			Polygon2D body = segment.GetChildOrNull<Polygon2D>(0);
			if (body != null)
			{
				Tween tween = body.CreateTween();
				tween.TweenProperty(body, "color", new Color(0.25f, 0.04f, 0.08f, 0.8f), 0.3f);
				tween.TweenProperty(body, "color", new Color(0.12f, 0.04f, 0.2f, 0.75f), 0.5f);
			}
		}
	}

	private void Die()
	{
		_isDying = true;
		_isActive = false;

		if (IsInGroup("enemies"))
			RemoveFromGroup("enemies");

		_eventBus.EmitSignal(EventBus.SignalName.EnemyKilled, "indicible", GlobalPosition);

		GD.Print($"[Indicible] L'Indicible est vaincu ! Score: +{ScoreReward}");

		// Désintégration des bords et des yeux
		foreach (Polygon2D eye in _eyes)
		{
			if (!IsInstanceValid(eye))
				continue;
			Tween tween = eye.CreateTween();
			tween.TweenProperty(eye, "modulate:a", 0f, 0.6f);
			tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(eye)) eye.QueueFree(); }));
		}

		foreach (Node2D segment in _edgeSegments)
		{
			if (!IsInstanceValid(segment))
				continue;
			Tween tween = segment.CreateTween();
			tween.TweenProperty(segment, "modulate:a", 0f, 1f)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(segment)) segment.QueueFree(); }));
		}

		// Suppression après la fade
		GetTree().CreateTimer(1.2f).Timeout += QueueFree;
	}

	private void FlashEdges()
	{
		foreach (Node2D segment in _edgeSegments)
		{
			if (!IsInstanceValid(segment))
				continue;
			Polygon2D body = segment.GetChildOrNull<Polygon2D>(0);
			if (body == null)
				continue;

			Color originalColor = body.Color;
			body.Color = Colors.White;
			Tween tween = body.CreateTween();
			tween.TweenProperty(body, "color", originalColor, 0.12f).SetDelay(0.04f);
		}
	}

	private void SpawnDamageNumber(float damage)
	{
		if (_damageNumberScene == null)
			return;

		// Afficher les dégâts sur un bord aléatoire
		DamageNumber dmgNum = _damageNumberScene.Instantiate<DamageNumber>();
		int edge = (int)(GD.Randi() % _edgeSegments.Count);
		Vector2 pos = _edgeSegments.Count > edge && IsInstanceValid(_edgeSegments[edge])
			? _edgeSegments[edge].GlobalPosition
			: GlobalPosition;
		dmgNum.GlobalPosition = pos + new Vector2(0, -20);
		dmgNum.SetDamage(damage, false);
		GetTree().CurrentScene.AddChild(dmgNum);
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
