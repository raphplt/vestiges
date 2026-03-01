using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Events;

/// <summary>
/// Gère les événements aléatoires qui ponctuent chaque phase (jour/nuit).
/// Charge les définitions depuis data/events/events.json.
/// Un seul événement actif à la fois.
/// </summary>
public partial class RandomEventManager : Node
{
	private const float EventRollDelay = 10f;
	private const float EventCheckInterval = 5f;

	private EventBus _eventBus;
	private SpawnManager _spawnManager;
	private DayNightCycle _dayNightCycle;
	private Node2D _foyerNode;

	private readonly List<EventData> _dayEvents = new();
	private readonly List<EventData> _nightEvents = new();

	private EventData _activeEvent;
	private float _eventTimer;
	private float _rollDelay;
	private bool _eventRolled;

	// Effets actifs
	private float _originalVisibilityFactor = 1f;
	private CanvasModulate _canvasModulate;
	private Tween _fogTween;

	// Résurgence : buffs ennemis appliqués via scaling
	private bool _resurgenceActive;
	private float _resurgenceHpMult = 1f;
	private float _resurgenceDmgMult = 1f;
	private float _resurgenceXpMult = 1f;

	// Caravane : nœud marchand temporaire
	private Node2D _caravanNode;

	// Signal de fumée : nœud visuel + zone de détection
	private Node2D _smokeSignalNode;

	// L'Appel : leurre + ennemis traqués
	private Node2D _lureNode;
	private readonly List<Enemy> _lureEnemies = new();
	private bool _lureRewardGranted;

	public bool ResurgenceActive => _resurgenceActive;
	public float ResurgenceHpMultiplier => _resurgenceHpMult;
	public float ResurgenceDmgMultiplier => _resurgenceDmgMult;
	public float ResurgenceXpMultiplier => _resurgenceXpMult;

	public EventData ActiveEvent => _activeEvent;

	public override void _Ready()
	{
		LoadEvents();

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.DayPhaseChanged += OnDayPhaseChanged;

		_canvasModulate = GetNodeOrNull<CanvasModulate>("../CanvasModulate");

		Node2D foyer = GetNodeOrNull<Node2D>("../Foyer");
		if (foyer != null)
			_foyerNode = foyer;

		GD.Print($"[RandomEventManager] Loaded {_dayEvents.Count} day events, {_nightEvents.Count} night events");
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
			_eventBus.DayPhaseChanged -= OnDayPhaseChanged;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// Délai avant de roller un événement dans la nouvelle phase
		if (!_eventRolled)
		{
			_rollDelay -= dt;
			if (_rollDelay <= 0f)
			{
				_eventRolled = true;
				RollEvent();
			}
			return;
		}

		// Timer de l'événement actif (0 = dure toute la phase)
		if (_activeEvent != null && _activeEvent.Duration > 0)
		{
			_eventTimer -= dt;
			if (_eventTimer <= 0f)
				EndActiveEvent();
		}
	}

	private void OnDayPhaseChanged(string phase)
	{
		// Fin de l'événement précédent au changement de phase
		EndActiveEvent();

		// On ne roule d'événement qu'en Day ou Night
		if (phase == "Day" || phase == "Night")
		{
			_rollDelay = EventRollDelay;
			_eventRolled = false;
		}
	}

	private void RollEvent()
	{
		CacheDayNightCycle();
		bool isNight = _dayNightCycle != null && _dayNightCycle.CurrentPhase == DayPhase.Night;
		List<EventData> pool = isNight ? _nightEvents : _dayEvents;

		if (pool.Count == 0)
			return;

		// Weighted random selection
		float totalWeight = 0f;
		foreach (EventData ev in pool)
			totalWeight += ev.Weight;

		float roll = (float)GD.RandRange(0, totalWeight);
		float cumulative = 0f;

		foreach (EventData ev in pool)
		{
			cumulative += ev.Weight;
			if (roll <= cumulative)
			{
				ActivateEvent(ev);
				return;
			}
		}

		// Fallback : dernier événement
		ActivateEvent(pool[^1]);
	}

	private void ActivateEvent(EventData ev)
	{
		_activeEvent = ev;
		_eventTimer = ev.Duration;

		_eventBus.EmitSignal(EventBus.SignalName.RandomEventTriggered, ev.Id, ev.Name);
		GD.Print($"[RandomEventManager] Event triggered: {ev.Name} ({ev.Id}), duration: {ev.Duration}s");

		ApplyEventEffects(ev);
	}

	private void EndActiveEvent()
	{
		if (_activeEvent == null)
			return;

		string endedId = _activeEvent.Id;
		RevertEventEffects(_activeEvent);
		_activeEvent = null;

		_eventBus.EmitSignal(EventBus.SignalName.RandomEventEnded, endedId);
		GD.Print($"[RandomEventManager] Event ended: {endedId}");
	}

	private void ApplyEventEffects(EventData ev)
	{
		switch (ev.EffectType)
		{
			case "merchant_spawn":
				ApplyCaravan(ev);
				break;
			case "visibility_reduction":
				ApplyVisibilityReduction(ev);
				break;
			case "tremor":
				ApplyTremor(ev);
				break;
			case "rescue_poi":
				ApplyRescuePoi(ev);
				break;
			case "horde_spawn":
				ApplyHordeSpawn(ev);
				break;
			case "enemy_buff":
				ApplyResurgence(ev);
				break;
			case "lure_spawn":
				ApplyLureSpawn(ev);
				break;
		}
	}

	private void RevertEventEffects(EventData ev)
	{
		switch (ev.EffectType)
		{
			case "merchant_spawn":
				CleanupCaravan();
				break;
			case "visibility_reduction":
				RevertVisibilityReduction();
				break;
			case "rescue_poi":
				CleanupSmokeSignal();
				break;
			case "enemy_buff":
				RevertResurgence();
				break;
			case "lure_spawn":
				CleanupLure();
				break;
		}
	}

	private void CleanupCaravan()
	{
		if (_caravanNode != null && IsInstanceValid(_caravanNode))
			_caravanNode.QueueFree();
		_caravanNode = null;
	}

	private void CleanupSmokeSignal()
	{
		if (_smokeSignalNode != null && IsInstanceValid(_smokeSignalNode))
			_smokeSignalNode.QueueFree();
		_smokeSignalNode = null;
	}

	private void CleanupLure()
	{
		if (_lureNode != null && IsInstanceValid(_lureNode))
			_lureNode.QueueFree();
		_lureNode = null;
		_lureEnemies.Clear();
	}

	// --- Caravane de passage ---

	private void ApplyCaravan(EventData ev)
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return;

		// Position du marchand : 150-200 unités du joueur
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		float dist = (float)GD.RandRange(150, 200);
		Vector2 merchantPos = player.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

		// Créer le nœud marchand
		Node2D merchant = new() { Name = "CaravanMerchant", GlobalPosition = merchantPos };

		// Visuel : losange doré (icône marchand)
		Polygon2D body = new()
		{
			Color = new Color(0.85f, 0.7f, 0.2f, 0.9f),
			Polygon = new Vector2[]
			{
				new(0, -18), new(12, 0), new(0, 18), new(-12, 0)
			}
		};
		merchant.AddChild(body);

		// Petit chapeau (triangle au-dessus)
		Polygon2D hat = new()
		{
			Color = new Color(0.6f, 0.45f, 0.1f),
			Polygon = new Vector2[]
			{
				new(-10, -18), new(10, -18), new(0, -30)
			}
		};
		merchant.AddChild(hat);

		// Zone d'interaction (Area2D + CollisionShape2D)
		Area2D interactArea = new() { Name = "InteractArea" };
		interactArea.CollisionLayer = 0;
		interactArea.CollisionMask = 1; // Layer 1 = player
		CollisionShape2D interactShape = new();
		CircleShape2D circleShape = new() { Radius = 50f };
		interactShape.Shape = circleShape;
		interactArea.AddChild(interactShape);
		merchant.AddChild(interactArea);

		// Pulsation douce pour attirer l'attention
		Tween pulseTween = CreateTween().SetLoops();
		pulseTween.TweenProperty(body, "scale", new Vector2(1.15f, 1.15f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine);
		pulseTween.TweenProperty(body, "scale", Vector2.One, 1.0f)
			.SetTrans(Tween.TransitionType.Sine);

		GetNode("..").CallDeferred("add_child", merchant);

		// Quand le joueur entre dans la zone, donner une ressource aléatoire
		bool rewardGiven = false;
		interactArea.BodyEntered += (Node2D bodyNode) =>
		{
			if (rewardGiven)
				return;
			if (bodyNode is not Player)
				return;

			rewardGiven = true;

			// Ressources utiles possibles
			string[] resources = { "wood", "stone", "iron", "herb", "crystal" };
			string chosenResource = resources[GD.Randi() % resources.Length];
			int amount = (int)GD.RandRange(5, 15);

			_eventBus.EmitSignal(EventBus.SignalName.LootReceived, "resource", chosenResource, amount);
			GD.Print($"[RandomEventManager] Caravane — joueur reçoit {amount}x {chosenResource}");

			// Feedback visuel : le marchand disparaît progressivement
			Tween fadeTween = merchant.CreateTween();
			fadeTween.TweenProperty(merchant, "modulate", new Color(1, 1, 1, 0), 1.5f);
			fadeTween.TweenCallback(Callable.From(() =>
			{
				if (IsInstanceValid(merchant))
					merchant.QueueFree();
			}));
		};

		_caravanNode = merchant;

		GD.Print($"[RandomEventManager] Caravane de passage à {merchantPos} — marchand disponible pendant {ev.Duration}s");
	}

	// --- Visibilité réduite (Tempête / Brume épaisse) ---

	private void ApplyVisibilityReduction(EventData ev)
	{
		float factor = ev.GetFloat("visibility_factor", 0.5f);
		_originalVisibilityFactor = factor;

		if (_canvasModulate != null)
		{
			Color current = _canvasModulate.Color;
			Color fogColor = new(current.R * factor, current.G * factor, current.B * factor);

			_fogTween?.Kill();
			_fogTween = CreateTween();
			_fogTween.TweenProperty(_canvasModulate, "color", fogColor, 2f)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.InOut);
		}

		// Tempête de jour : réduire le timer de jour
		float dayReduction = ev.GetFloat("day_time_reduction", 0f);
		if (dayReduction > 0f)
		{
			GD.Print($"[RandomEventManager] Tempête — jour raccourci de {dayReduction}s (non implémenté dans DayNightCycle)");
		}
	}

	private void RevertVisibilityReduction()
	{
		if (_canvasModulate == null)
			return;

		// Retour progressif à la couleur de base de la phase actuelle
		_fogTween?.Kill();
		_fogTween = CreateTween();

		CacheDayNightCycle();
		Color targetColor = _dayNightCycle?.CurrentPhase switch
		{
			DayPhase.Day => new Color(1.0f, 0.95f, 0.85f),
			DayPhase.Dusk => new Color(0.55f, 0.45f, 0.65f),
			DayPhase.Night => new Color(0.08f, 0.06f, 0.12f),
			_ => new Color(1.0f, 0.95f, 0.85f)
		};

		_fogTween.TweenProperty(_canvasModulate, "color", targetColor, 3f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	// --- Tremblement ---

	private void ApplyTremor(EventData ev)
	{
		float chestChance = ev.GetFloat("chest_chance", 0.5f);
		float structureDamage = ev.GetFloat("structure_damage", 50f);

		// Effet visuel : tremblement de l'écran
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		Camera2D camera = player?.GetNodeOrNull<Camera2D>("Camera");
		if (camera != null)
		{
			Tween shakeTween = CreateTween();
			for (int i = 0; i < 8; i++)
			{
				Vector2 offset = new((float)GD.RandRange(-4, 4), (float)GD.RandRange(-4, 4));
				shakeTween.TweenProperty(camera, "offset", offset, 0.06f);
			}
			shakeTween.TweenProperty(camera, "offset", Vector2.Zero, 0.1f);
		}

		// Chance de faire apparaître un coffre ou endommager une structure
		if (GD.Randf() < chestChance)
		{
			// Spawn un coffre rare près du joueur
			if (player != null)
			{
				float angle = (float)GD.RandRange(0, Mathf.Tau);
				Vector2 chestPos = player.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 120f;
				_eventBus.EmitSignal(EventBus.SignalName.ChestOpened, "tremor_chest", "rare", chestPos);
				GD.Print("[RandomEventManager] Tremblement — coffre révélé !");
			}
		}
		else
		{
			// Dégâts aux structures
			_eventBus.EmitSignal(EventBus.SignalName.StructureDestroyed, "tremor", _foyerNode?.GlobalPosition ?? Vector2.Zero);
			GD.Print($"[RandomEventManager] Tremblement — structures endommagées ({structureDamage} dégâts)");
		}
	}

	// --- Signal de fumée ---

	private void ApplyRescuePoi(EventData ev)
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return;

		// Position du signal à ~300 unités du joueur
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		Vector2 poiPos = player.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 300f;

		bool isTrap = GD.Randf() < ev.GetFloat("trap_chance", 0.25f);
		string poiType = isTrap ? "trap" : "rescue";

		// Signaler le POI sur la minimap via EventBus
		_eventBus.EmitSignal(EventBus.SignalName.PoiDiscovered, "smoke_signal", poiType, poiPos);

		// Créer la colonne de fumée visuelle
		Node2D smokeNode = new() { Name = "SmokeSignal", GlobalPosition = poiPos };

		// Base : petit feu (triangle orange)
		Polygon2D fireBase = new()
		{
			Color = new Color(0.9f, 0.5f, 0.1f, 0.8f),
			Polygon = new Vector2[]
			{
				new(-6, 0), new(6, 0), new(0, -12)
			}
		};
		smokeNode.AddChild(fireBase);

		// Colonne de fumée : ellipses empilées de plus en plus transparentes
		for (int i = 0; i < 4; i++)
		{
			float yOff = -18f - i * 14f;
			float alpha = 0.5f - i * 0.1f;
			float size = 8f + i * 3f;

			Polygon2D smokePuff = new()
			{
				Color = new Color(0.6f, 0.6f, 0.6f, alpha),
				Polygon = CreateCirclePolygon(size, 6),
				Position = new Vector2(0, yOff)
			};
			smokeNode.AddChild(smokePuff);

			// Légère ondulation de chaque bouffée
			Tween puffTween = CreateTween().SetLoops();
			float drift = 3f + i * 1.5f;
			puffTween.TweenProperty(smokePuff, "position",
				new Vector2(drift, yOff - 4f), 1.2f + i * 0.3f)
				.SetTrans(Tween.TransitionType.Sine);
			puffTween.TweenProperty(smokePuff, "position",
				new Vector2(-drift, yOff + 2f), 1.2f + i * 0.3f)
				.SetTrans(Tween.TransitionType.Sine);
		}

		// Zone de détection du joueur
		Area2D detectArea = new() { Name = "DetectArea" };
		detectArea.CollisionLayer = 0;
		detectArea.CollisionMask = 1; // Layer 1 = player
		CollisionShape2D detectShape = new();
		CircleShape2D detectCircle = new() { Radius = 60f };
		detectShape.Shape = detectCircle;
		detectArea.AddChild(detectShape);
		smokeNode.AddChild(detectArea);

		GetNode("..").CallDeferred("add_child", smokeNode);

		// Capturer les données pour la lambda
		float rewardXp = ev.GetFloat("reward_xp", 100f);
		int rewardResources = (int)ev.GetFloat("reward_resources", 10f);
		bool activated = false;

		detectArea.BodyEntered += (Node2D bodyNode) =>
		{
			if (activated)
				return;
			if (bodyNode is not Player)
				return;

			activated = true;

			if (isTrap)
			{
				// Piège : spawn 4-5 ennemis agressifs
				GD.Print("[RandomEventManager] Signal de fumée — PIÈGE !");
				SpawnTrapEnemies(poiPos, 4 + (int)(GD.Randi() % 2));
			}
			else
			{
				// Secours : récompense XP + ressources
				_eventBus.EmitSignal(EventBus.SignalName.XpGained, rewardXp);
				_eventBus.EmitSignal(EventBus.SignalName.LootReceived, "resource", "herb", rewardResources);
				GD.Print($"[RandomEventManager] Signal de fumée — survivant secouru ! +{rewardXp} XP, +{rewardResources} herbes");
			}

			// Dissoudre le nœud après interaction
			Tween fadeTween = smokeNode.CreateTween();
			fadeTween.TweenProperty(smokeNode, "modulate", new Color(1, 1, 1, 0), 1.0f);
			fadeTween.TweenCallback(Callable.From(() =>
			{
				if (IsInstanceValid(smokeNode))
					smokeNode.QueueFree();
			}));
		};

		_smokeSignalNode = smokeNode;

		GD.Print($"[RandomEventManager] Signal de fumée à {poiPos} ({poiType})");
	}

	/// <summary>Spawn des ennemis de piège autour d'une position.</summary>
	private void SpawnTrapEnemies(Vector2 center, int count)
	{
		EnemyPool pool = GetNodeOrNull<EnemyPool>("../EnemyPool");
		Node enemyContainer = GetNodeOrNull("../EnemyContainer");
		if (pool == null || enemyContainer == null)
			return;

		// Utiliser shade (coriace) pour le piège
		EnemyData data = EnemyDataLoader.Get("shade");
		if (data == null)
		{
			data = EnemyDataLoader.Get("shadow_crawler");
			if (data == null) return;
		}

		for (int i = 0; i < count; i++)
		{
			float spawnAngle = Mathf.Tau * i / count;
			Vector2 spawnPos = center + new Vector2(Mathf.Cos(spawnAngle), Mathf.Sin(spawnAngle)) * 40f;

			Enemy enemy = pool.Get();
			enemy.GlobalPosition = spawnPos;
			enemyContainer.AddChild(enemy);
			enemy.Initialize(data, 1.2f, 1.2f);

			_eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, data.Id, 1.2f, 1.2f);
		}

		GD.Print($"[RandomEventManager] Piège — {count} {data.Id} spawned !");
	}

	// --- Migration (horde de jour) ---

	private void ApplyHordeSpawn(EventData ev)
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return;

		int count = (int)ev.GetFloat("enemy_count", 8f);
		string enemyId = ev.GetString("enemy_id", "charognard");

		EnemyData data = EnemyDataLoader.Get(enemyId);
		if (data == null)
		{
			// Fallback
			data = EnemyDataLoader.Get("shadow_crawler");
			if (data == null) return;
		}

		CacheSpawnManager();
		EnemyPool pool = GetNodeOrNull<EnemyPool>("../EnemyPool");
		Node enemyContainer = GetNodeOrNull("../EnemyContainer");
		if (pool == null || enemyContainer == null)
			return;

		// Spawn en formation de ligne perpendiculaire à la direction vers le joueur
		float baseAngle = (float)GD.RandRange(0, Mathf.Tau);
		Vector2 baseDir = new(Mathf.Cos(baseAngle), Mathf.Sin(baseAngle));
		Vector2 perpDir = new(-baseDir.Y, baseDir.X);
		Vector2 center = player.GlobalPosition + baseDir * 500f;

		for (int i = 0; i < count; i++)
		{
			float offset = (i - count / 2f) * 30f;
			Vector2 spawnPos = center + perpDir * offset;

			Enemy enemy = pool.Get();
			enemy.GlobalPosition = spawnPos;
			enemyContainer.AddChild(enemy);
			enemy.Initialize(data, 1f, 1f);

			_eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, enemyId, 1f, 1f);
		}

		GD.Print($"[RandomEventManager] Migration — {count} {enemyId} spawned !");
	}

	// --- Résurgence (buff ennemis nuit) ---

	private void ApplyResurgence(EventData ev)
	{
		_resurgenceActive = true;
		_resurgenceHpMult = ev.GetFloat("hp_multiplier", 1.5f);
		_resurgenceDmgMult = ev.GetFloat("damage_multiplier", 1.3f);
		_resurgenceXpMult = ev.GetFloat("xp_multiplier", 2f);

		GD.Print($"[RandomEventManager] Résurgence — HP x{_resurgenceHpMult}, DMG x{_resurgenceDmgMult}, XP x{_resurgenceXpMult}");
	}

	private void RevertResurgence()
	{
		_resurgenceActive = false;
		_resurgenceHpMult = 1f;
		_resurgenceDmgMult = 1f;
		_resurgenceXpMult = 1f;
	}

	// --- L'Appel (lure de nuit) ---

	private void ApplyLureSpawn(EventData ev)
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return;

		float lureDistance = ev.GetFloat("lure_distance", 400f);
		float rewardXp = ev.GetFloat("reward_xp", 200f);
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		Vector2 lurePos = (_foyerNode?.GlobalPosition ?? player.GlobalPosition) +
			new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * lureDistance;

		// Créer un indicateur visuel attirant (lumière pulsante)
		Node2D lure = new() { Name = "LureSignal", GlobalPosition = lurePos };

		Polygon2D glow = new()
		{
			Color = new Color(0.4f, 0.9f, 0.5f, 0.6f),
			Polygon = CreateCirclePolygon(20f, 8)
		};
		lure.AddChild(glow);

		// Halo extérieur pour renforcer l'attraction visuelle
		Polygon2D halo = new()
		{
			Color = new Color(0.3f, 0.8f, 0.4f, 0.2f),
			Polygon = CreateCirclePolygon(35f, 12)
		};
		lure.AddChild(halo);

		GetNode("..").CallDeferred("add_child", lure);

		// Pulsation lumineuse
		Tween pulseTween = CreateTween().SetLoops();
		pulseTween.TweenProperty(glow, "scale", new Vector2(1.4f, 1.4f), 0.8f)
			.SetTrans(Tween.TransitionType.Sine);
		pulseTween.TweenProperty(glow, "scale", Vector2.One, 0.8f)
			.SetTrans(Tween.TransitionType.Sine);

		// Préparer le tracking des ennemis du leurre
		_lureEnemies.Clear();
		_lureRewardGranted = false;
		_lureNode = lure;

		// Spawn des ennemis coriaces après un délai de 3 secondes
		GetTree().CreateTimer(3f).Timeout += () =>
		{
			if (!IsInstanceValid(lure))
				return;

			SpawnLureEnemies(lurePos, rewardXp);
		};

		// Auto-destruction après la phase
		GetTree().CreateTimer(90f).Timeout += () =>
		{
			if (IsInstanceValid(lure))
				lure.QueueFree();
			_lureEnemies.Clear();
		};

		GD.Print($"[RandomEventManager] L'Appel — leurre à {lurePos}");
	}

	/// <summary>Spawn 2-3 ennemis coriaces autour du leurre et traque leur mort.</summary>
	private void SpawnLureEnemies(Vector2 lurePos, float rewardXp)
	{
		EnemyPool pool = GetNodeOrNull<EnemyPool>("../EnemyPool");
		Node enemyContainer = GetNodeOrNull("../EnemyContainer");
		if (pool == null || enemyContainer == null)
			return;

		// Alterner entre void_brute et shade pour la variété
		string[] lureEnemyIds = { "void_brute", "shade" };
		int count = 2 + (int)(GD.Randi() % 2); // 2 ou 3

		for (int i = 0; i < count; i++)
		{
			string enemyId = lureEnemyIds[i % lureEnemyIds.Length];
			EnemyData data = EnemyDataLoader.Get(enemyId);
			if (data == null)
			{
				data = EnemyDataLoader.Get("shadow_crawler");
				if (data == null) continue;
			}

			float spawnAngle = Mathf.Tau * i / count;
			Vector2 spawnPos = lurePos + new Vector2(Mathf.Cos(spawnAngle), Mathf.Sin(spawnAngle)) * 60f;

			// Scaling renforcé : ce sont des ennemis dangereux
			float hpScale = 1.5f;
			float dmgScale = 1.3f;

			Enemy enemy = pool.Get();
			enemy.GlobalPosition = spawnPos;
			enemyContainer.AddChild(enemy);
			enemy.Initialize(data, hpScale, dmgScale);

			_lureEnemies.Add(enemy);
			_eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, data.Id, hpScale, dmgScale);
		}

		// Écouter les kills pour vérifier si tous les ennemis du leurre sont morts
		_eventBus.EnemyKilled += OnLureEnemyKilled;

		// Capturer rewardXp pour la vérification
		void OnLureEnemyKilled(string enemyId, Vector2 position)
		{
			if (_lureRewardGranted)
				return;

			// Retirer les ennemis morts de la liste
			_lureEnemies.RemoveAll(e => !IsInstanceValid(e) || !e.IsActive);

			if (_lureEnemies.Count == 0)
			{
				// Tous les ennemis du leurre sont morts : récompense bonus
				_lureRewardGranted = true;
				_eventBus.EmitSignal(EventBus.SignalName.XpGained, rewardXp);
				GD.Print($"[RandomEventManager] L'Appel — défi relevé ! +{rewardXp} XP bonus");

				// Dissoudre le leurre
				if (IsInstanceValid(_lureNode))
				{
					Tween fadeTween = _lureNode.CreateTween();
					fadeTween.TweenProperty(_lureNode, "modulate", new Color(1, 1, 1, 0), 1.0f);
					fadeTween.TweenCallback(Callable.From(() =>
					{
						if (IsInstanceValid(_lureNode))
							_lureNode.QueueFree();
					}));
				}

				// Se désabonner
				_eventBus.EnemyKilled -= OnLureEnemyKilled;
			}
		}

		GD.Print($"[RandomEventManager] L'Appel — {count} ennemis coriaces spawned près du leurre !");
	}

	// --- Chargement des données ---

	private void LoadEvents()
	{
		FileAccess file = FileAccess.Open("res://data/events/events.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[RandomEventManager] Cannot open events.json");
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[RandomEventManager] Parse error: {json.GetErrorMessage()}");
			return;
		}

		Godot.Collections.Array arr = json.Data.AsGodotArray();
		foreach (Variant item in arr)
		{
			Godot.Collections.Dictionary dict = item.AsGodotDictionary();
			EventData ev = EventData.FromDict(dict);
			if (ev == null)
				continue;

			if (ev.Phase == "day")
				_dayEvents.Add(ev);
			else if (ev.Phase == "night")
				_nightEvents.Add(ev);
		}
	}

	private void CacheDayNightCycle()
	{
		if (_dayNightCycle != null && IsInstanceValid(_dayNightCycle))
			return;
		_dayNightCycle = GetNodeOrNull<DayNightCycle>("../DayNightCycle");
	}

	private void CacheSpawnManager()
	{
		if (_spawnManager != null && IsInstanceValid(_spawnManager))
			return;
		_spawnManager = GetNodeOrNull<SpawnManager>("../SpawnManager");
	}

	private static Vector2[] CreateCirclePolygon(float radius, int segments)
	{
		Vector2[] points = new Vector2[segments];
		for (int i = 0; i < segments; i++)
		{
			float angle = Mathf.Tau * i / segments;
			points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}
		return points;
	}
}

/// <summary>
/// Données statiques d'un événement aléatoire, chargé depuis JSON.
/// </summary>
public class EventData
{
	public string Id;
	public string Name;
	public string Phase;
	public float Weight;
	public float Duration;
	public string Description;
	public string EffectType;
	private Godot.Collections.Dictionary _effects;

	public float GetFloat(string key, float defaultValue)
	{
		if (_effects != null && _effects.ContainsKey(key))
			return (float)_effects[key].AsDouble();
		return defaultValue;
	}

	public string GetString(string key, string defaultValue)
	{
		if (_effects != null && _effects.ContainsKey(key))
			return _effects[key].AsString();
		return defaultValue;
	}

	public static EventData FromDict(Godot.Collections.Dictionary dict)
	{
		if (!dict.ContainsKey("id"))
			return null;

		EventData ev = new()
		{
			Id = dict["id"].AsString(),
			Name = dict.ContainsKey("name") ? dict["name"].AsString() : dict["id"].AsString(),
			Phase = dict.ContainsKey("phase") ? dict["phase"].AsString() : "day",
			Weight = dict.ContainsKey("weight") ? (float)dict["weight"].AsDouble() : 1f,
			Duration = dict.ContainsKey("duration") ? (float)dict["duration"].AsDouble() : 0f,
			Description = dict.ContainsKey("description") ? dict["description"].AsString() : ""
		};

		if (dict.ContainsKey("effects"))
		{
			Godot.Collections.Dictionary effects = dict["effects"].AsGodotDictionary();
			ev.EffectType = effects.ContainsKey("type") ? effects["type"].AsString() : "";
			ev._effects = effects;
		}

		return ev;
	}
}
