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

	// Pluie de Cendres : bonus récolte
	private float _ashRainHarvestMult = 1f;

	// Floraison Spontanée : fleurs temporaires
	private readonly List<Node2D> _bloomFlowers = new();

	// Faille Temporelle : zone dorée + buff + spawn retardé
	private Node2D _riftNode;
	private Tween _riftPulseTween;

	// Écho du Monde : bâtiments fantômes temporaires
	private readonly List<Node2D> _echoGhosts = new();

	// Vent des Oubliés : mult XP + push timer
	private float _windXpMult = 1f;
	private float _windPushTimer;
	private float _windPushInterval;
	private float _windPushForce;
	private bool _windActive;
	private Node2D _windParticlesNode;

	// Résonance du Foyer : rayon safe original
	private float _originalFoyerRadius;

	// Résurgence : buffs ennemis appliqués via scaling
	private bool _resurgenceActive;
	private float _resurgenceHpMult = 1f;
	private float _resurgenceDmgMult = 1f;
	private float _resurgenceXpMult = 1f;

	// Caravane : nœud marchand temporaire
	private Node2D _caravanNode;
	private Tween _caravanPulseTween;

	// Signal de fumée : nœud visuel + zone de détection
	private Node2D _smokeSignalNode;
	private readonly List<Tween> _smokePuffTweens = new();

	// L'Appel : leurre + ennemis traqués
	private Node2D _lureNode;
	private Tween _lurePulseTween;
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

		// Vent des Oubliés : push périodique
		if (_windActive)
		{
			_windPushTimer -= dt;
			if (_windPushTimer <= 0f)
			{
				_windPushTimer = _windPushInterval;
				ApplyWindPush();
			}
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
			case "ash_rain":
				ApplyAshRain(ev);
				break;
			case "clock_march":
				ApplyClockMarch(ev);
				break;
			case "spontaneous_bloom":
				ApplySpontaneousBloom(ev);
				break;
			case "temporal_rift":
				ApplyTemporalRift(ev);
				break;
			case "world_echo":
				ApplyWorldEcho(ev);
				break;
			case "forgotten_wind":
				ApplyForgottenWind(ev);
				break;
			case "foyer_resonance":
				ApplyFoyerResonance(ev);
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
			case "ash_rain":
				RevertAshRain();
				break;
			case "clock_march":
				RevertClockMarch();
				break;
			case "spontaneous_bloom":
				CleanupBloom();
				break;
			case "temporal_rift":
				CleanupRift();
				break;
			case "world_echo":
				CleanupEchoGhosts();
				break;
			case "forgotten_wind":
				RevertForgottenWind();
				break;
			case "foyer_resonance":
				RevertFoyerResonance();
				break;
		}
	}

	private void CleanupCaravan()
	{
		_caravanPulseTween?.Kill();
		if (_caravanNode != null && IsInstanceValid(_caravanNode))
			_caravanNode.QueueFree();
		_caravanNode = null;
	}

	private void CleanupSmokeSignal()
	{
		foreach (Tween t in _smokePuffTweens)
			t?.Kill();
		_smokePuffTweens.Clear();
		if (_smokeSignalNode != null && IsInstanceValid(_smokeSignalNode))
			_smokeSignalNode.QueueFree();
		_smokeSignalNode = null;
	}

	private void CleanupLure()
	{
		_lurePulseTween?.Kill();
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
		_caravanPulseTween = CreateTween().SetLoops();
		_caravanPulseTween.TweenProperty(body, "scale", new Vector2(1.15f, 1.15f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine);
		_caravanPulseTween.TweenProperty(body, "scale", Vector2.One, 1.0f)
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
			_caravanPulseTween?.Kill();
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
			_smokePuffTweens.Add(puffTween);
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
			foreach (Tween t in _smokePuffTweens)
				t?.Kill();
			_smokePuffTweens.Clear();
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
		_lurePulseTween = CreateTween().SetLoops();
		_lurePulseTween.TweenProperty(glow, "scale", new Vector2(1.4f, 1.4f), 0.8f)
			.SetTrans(Tween.TransitionType.Sine);
		_lurePulseTween.TweenProperty(glow, "scale", Vector2.One, 0.8f)
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
				_lurePulseTween?.Kill();
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

	// --- Pluie de Cendres ---

	private void ApplyAshRain(EventData ev)
	{
		// Visibilité réduite (plus légère que la tempête)
		float factor = ev.GetFloat("visibility_factor", 0.7f);
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

		// Bonus de récolte
		_ashRainHarvestMult = ev.GetFloat("harvest_multiplier", 1.5f);
		_eventBus.EmitSignal(EventBus.SignalName.ResourceBonusChanged, _ashRainHarvestMult);

		// Particules de cendres visuelles (attachées à la caméra du joueur)
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player != null)
		{
			GpuParticles2D ashParticles = new() { Name = "AshRainParticles" };
			ParticleProcessMaterial mat = new();
			mat.Direction = new Vector3(0, 1, 0);
			mat.Spread = 30f;
			mat.InitialVelocityMin = 30f;
			mat.InitialVelocityMax = 60f;
			mat.Gravity = new Vector3(0, 15f, 0);
			mat.ScaleMin = 0.5f;
			mat.ScaleMax = 2f;
			mat.Color = new Color(0.6f, 0.55f, 0.5f, 0.6f);
			ashParticles.ProcessMaterial = mat;
			ashParticles.Amount = 40;
			ashParticles.Lifetime = 3f;
			ashParticles.VisibilityRect = new Rect2(-600, -400, 1200, 800);
			ashParticles.ZIndex = 90;
			player.AddChild(ashParticles);
		}

		GD.Print($"[RandomEventManager] Pluie de Cendres — visibilité x{factor}, récolte x{_ashRainHarvestMult}");
	}

	private void RevertAshRain()
	{
		// Retour visibilité
		RevertVisibilityReduction();

		// Retour bonus récolte
		_ashRainHarvestMult = 1f;
		_eventBus.EmitSignal(EventBus.SignalName.ResourceBonusChanged, 1f);

		// Nettoyage des particules
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		Node ashParticles = player?.GetNodeOrNull("AshRainParticles");
		if (ashParticles != null && IsInstanceValid(ashParticles))
			ashParticles.QueueFree();
	}

	// --- Marche des Horloges ---

	private void ApplyClockMarch(EventData ev)
	{
		CacheDayNightCycle();
		if (_dayNightCycle != null)
			_dayNightCycle.SetPhasePaused(true);

		_eventBus.EmitSignal(EventBus.SignalName.DayTimerPaused, true);

		GD.Print("[RandomEventManager] Marche des Horloges — timer de jour gelé !");
	}

	private void RevertClockMarch()
	{
		CacheDayNightCycle();
		if (_dayNightCycle != null)
			_dayNightCycle.SetPhasePaused(false);

		_eventBus.EmitSignal(EventBus.SignalName.DayTimerPaused, false);
	}

	// --- Faille Temporelle ---

	private void ApplyTemporalRift(EventData ev)
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return;

		float distMin = ev.GetFloat("rift_distance_min", 200f);
		float distMax = ev.GetFloat("rift_distance_max", 300f);
		float speedBuff = ev.GetFloat("speed_buff", 1.3f);
		float attackBuff = ev.GetFloat("attack_buff", 1.25f);
		float buffDuration = ev.GetFloat("buff_duration", 15f);
		int enemyCount = (int)ev.GetFloat("enemy_count", 4f);
		float enemyDelay = ev.GetFloat("enemy_spawn_delay", 10f);

		// Position de la faille
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		float dist = (float)GD.RandRange(distMin, distMax);
		Vector2 riftPos = player.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

		// Nœud visuel : cercle doré pulsant
		Node2D rift = new() { Name = "TemporalRift", GlobalPosition = riftPos };

		// Cercle principal doré
		Polygon2D riftCircle = new()
		{
			Color = new Color(1f, 0.85f, 0.3f, 0.25f),
			Polygon = CreateCirclePolygon(40f, 16)
		};
		rift.AddChild(riftCircle);

		// Halo extérieur chaud
		Polygon2D halo = new()
		{
			Color = new Color(1f, 0.7f, 0.2f, 0.1f),
			Polygon = CreateCirclePolygon(65f, 16)
		};
		rift.AddChild(halo);

		// Spirale intérieure (3 lignes convergentes)
		for (int i = 0; i < 3; i++)
		{
			float spiralAngle = Mathf.Tau * i / 3f;
			Polygon2D spiral = new()
			{
				Color = new Color(1f, 0.95f, 0.6f, 0.5f),
				Polygon = new Vector2[]
				{
					new(Mathf.Cos(spiralAngle) * 30f, Mathf.Sin(spiralAngle) * 30f),
					new(Mathf.Cos(spiralAngle + 0.3f) * 15f, Mathf.Sin(spiralAngle + 0.3f) * 15f),
					new(0, 0)
				}
			};
			rift.AddChild(spiral);
		}

		// PointLight2D dorée
		PointLight2D light = new()
		{
			Color = new Color(1f, 0.85f, 0.4f),
			Energy = 0.8f,
			TextureScale = 0.6f,
			Texture = GD.Load<Texture2D>("res://icon.svg")
		};
		rift.AddChild(light);

		// Pulsation
		_riftPulseTween = CreateTween().SetLoops();
		_riftPulseTween.TweenProperty(riftCircle, "scale", new Vector2(1.2f, 1.2f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine);
		_riftPulseTween.TweenProperty(riftCircle, "scale", Vector2.One, 1.0f)
			.SetTrans(Tween.TransitionType.Sine);

		GetNode("..").CallDeferred("add_child", rift);

		// Zone d'interaction pour le buff
		Area2D riftArea = new() { Name = "RiftArea" };
		riftArea.CollisionLayer = 0;
		riftArea.CollisionMask = 1;
		CollisionShape2D riftShape = new();
		CircleShape2D riftCircleShape = new() { Radius = 45f };
		riftShape.Shape = riftCircleShape;
		riftArea.AddChild(riftShape);
		rift.AddChild(riftArea);

		bool buffGranted = false;
		riftArea.BodyEntered += (Node2D body) =>
		{
			if (buffGranted || body is not Player p)
				return;

			buffGranted = true;

			// Appliquer buff vitesse + attaque
			p.ApplySpeedMultiplier(speedBuff);
			_eventBus.EmitSignal(EventBus.SignalName.PlayerBuffApplied, "temporal_speed", buffDuration);

			GD.Print($"[RandomEventManager] Faille Temporelle — buff vitesse x{speedBuff}, attaque x{attackBuff} pendant {buffDuration}s");

			// Retirer le buff après durée
			GetTree().CreateTimer(buffDuration).Timeout += () =>
			{
				if (IsInstanceValid(p))
					p.ApplySpeedMultiplier(1f / speedBuff);
			};

			// Flash visuel sur la faille
			Tween flash = rift.CreateTween();
			flash.TweenProperty(rift, "modulate", new Color(1, 1, 0.5f, 1.5f), 0.2f);
			flash.TweenProperty(rift, "modulate", new Color(1, 1, 1, 0.5f), 1f);
		};

		// Spawn d'ennemis autour après un délai
		GetTree().CreateTimer(enemyDelay).Timeout += () =>
		{
			if (!IsInstanceValid(rift))
				return;

			SpawnTrapEnemies(riftPos, enemyCount);
			GD.Print($"[RandomEventManager] Faille Temporelle — {enemyCount} créatures émergent !");
		};

		_riftNode = rift;
		GD.Print($"[RandomEventManager] Faille Temporelle à {riftPos}");
	}

	private void CleanupRift()
	{
		_riftPulseTween?.Kill();
		if (_riftNode != null && IsInstanceValid(_riftNode))
		{
			Tween fade = _riftNode.CreateTween();
			fade.TweenProperty(_riftNode, "modulate", new Color(1, 1, 1, 0), 1f);
			fade.TweenCallback(Callable.From(() =>
			{
				if (IsInstanceValid(_riftNode))
					_riftNode.QueueFree();
			}));
		}
		_riftNode = null;
	}

	// --- Écho du Monde ---

	private void ApplyWorldEcho(EventData ev)
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return;

		int ghostCount = (int)ev.GetFloat("ghost_building_count", 5f);
		float ghostRadius = ev.GetFloat("ghost_building_radius", 250f);
		int revealRadius = (int)ev.GetFloat("reveal_radius", 8f);

		// Révéler le brouillard autour du joueur (gros burst)
		FogOfWar fog = GetNodeOrNull<FogOfWar>("../FogOfWar");
		if (fog != null)
		{
			Vector2I playerCell = GetPlayerCell();
			if (playerCell.X != int.MinValue)
				_eventBus.EmitSignal(EventBus.SignalName.FogRevealBurst, playerCell.X, playerCell.Y, revealRadius);
		}

		// Silhouettes fantômes de bâtiments du monde d'avant
		for (int i = 0; i < ghostCount; i++)
		{
			float angle = (float)GD.RandRange(0, Mathf.Tau);
			float dist = (float)GD.RandRange(80, ghostRadius);
			Vector2 ghostPos = player.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

			Node2D ghost = CreateGhostBuilding(ghostPos);
			GetNode("..").CallDeferred("add_child", ghost);
			_echoGhosts.Add(ghost);
		}

		// Effet visuel : flash doré bref sur tout l'écran
		Camera2D camera = player.GetNodeOrNull<Camera2D>("Camera");
		if (camera != null)
		{
			Tween screenFlash = CreateTween();
			screenFlash.TweenProperty(player, "modulate", new Color(1.2f, 1.1f, 0.8f), 0.3f);
			screenFlash.TweenProperty(player, "modulate", Colors.White, 1f);
		}

		GD.Print($"[RandomEventManager] Écho du Monde — {ghostCount} silhouettes, brouillard révélé !");
	}

	private Node2D CreateGhostBuilding(Vector2 position)
	{
		Node2D ghost = new() { Name = "GhostBuilding", GlobalPosition = position };
		ghost.Modulate = new Color(0.8f, 0.85f, 1f, 0f);

		// Type aléatoire de bâtiment fantôme
		int type = (int)(GD.Randi() % 4);
		Vector2[] outline;
		switch (type)
		{
			case 0: // Maison
				outline = new Vector2[]
				{
					new(-18, 10), new(-18, -12), new(0, -24),
					new(18, -12), new(18, 10)
				};
				break;
			case 1: // Tour
				outline = new Vector2[]
				{
					new(-8, 15), new(-8, -30), new(-4, -35),
					new(4, -35), new(8, -30), new(8, 15)
				};
				break;
			case 2: // Église
				outline = new Vector2[]
				{
					new(-15, 10), new(-15, -10), new(-5, -10),
					new(-5, -25), new(0, -32), new(5, -25),
					new(5, -10), new(15, -10), new(15, 10)
				};
				break;
			default: // Immeuble
				outline = new Vector2[]
				{
					new(-20, 10), new(-20, -18), new(-12, -18),
					new(-12, -25), new(12, -25), new(12, -18),
					new(20, -18), new(20, 10)
				};
				break;
		}

		Polygon2D building = new()
		{
			Color = new Color(0.7f, 0.75f, 0.9f, 0.3f),
			Polygon = outline
		};
		ghost.AddChild(building);

		// Contour lumineux
		Polygon2D glow = new()
		{
			Color = new Color(0.85f, 0.9f, 1f, 0.1f),
			Polygon = outline,
			Scale = new Vector2(1.15f, 1.15f)
		};
		ghost.AddChild(glow);

		// Fenêtres qui clignotent (points lumineux)
		int windowCount = 2 + (int)(GD.Randi() % 3);
		for (int w = 0; w < windowCount; w++)
		{
			Polygon2D window = new()
			{
				Color = new Color(1f, 0.9f, 0.6f, 0.4f),
				Polygon = new Vector2[]
				{
					new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)
				},
				Position = new Vector2(
					(float)GD.RandRange(-12, 12),
					(float)GD.RandRange(-20, 0))
			};
			ghost.AddChild(window);

			// Flickering aléatoire par fenêtre
			Tween flicker = ghost.CreateTween().SetLoops();
			float flickerTime = (float)GD.RandRange(0.8f, 2f);
			flicker.TweenProperty(window, "modulate:a", 0.1f, flickerTime)
				.SetTrans(Tween.TransitionType.Sine);
			flicker.TweenProperty(window, "modulate:a", 0.6f, flickerTime * 0.7f)
				.SetTrans(Tween.TransitionType.Sine);
		}

		// Fade-in spectral
		Tween fadeIn = ghost.CreateTween();
		fadeIn.TweenProperty(ghost, "modulate:a", 0.5f, 2f)
			.SetTrans(Tween.TransitionType.Sine);

		ghost.ZIndex = -2;
		return ghost;
	}

	private void CleanupEchoGhosts()
	{
		foreach (Node2D ghost in _echoGhosts)
		{
			if (ghost == null || !IsInstanceValid(ghost))
				continue;

			Tween fade = ghost.CreateTween();
			fade.TweenProperty(ghost, "modulate:a", 0f, 1.5f)
				.SetTrans(Tween.TransitionType.Sine);
			fade.TweenCallback(Callable.From(() =>
			{
				if (IsInstanceValid(ghost))
					ghost.QueueFree();
			}));
		}
		_echoGhosts.Clear();
	}

	private Vector2I GetPlayerCell()
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		TileMapLayer ground = GetNodeOrNull<TileMapLayer>("../WorldSetup/Ground");
		if (player == null || ground == null)
			return new Vector2I(int.MinValue, int.MinValue);
		return ground.LocalToMap(ground.ToLocal(player.GlobalPosition));
	}

	// --- Vent des Oubliés ---

	private void ApplyForgottenWind(EventData ev)
	{
		_windXpMult = ev.GetFloat("xp_multiplier", 2f);
		_windPushForce = ev.GetFloat("push_force", 80f);
		_windPushInterval = ev.GetFloat("push_interval", 3f);
		_windPushTimer = _windPushInterval;
		_windActive = true;

		// Appliquer le multiplicateur XP
		_eventBus.EmitSignal(EventBus.SignalName.XpMultiplierChanged, _windXpMult);

		// Créer des particules de vent spectral sur le joueur
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player != null)
		{
			GpuParticles2D windParticles = new() { Name = "WindParticles" };
			ParticleProcessMaterial mat = new();
			mat.Direction = new Vector3(1, -0.3f, 0);
			mat.Spread = 20f;
			mat.InitialVelocityMin = 80f;
			mat.InitialVelocityMax = 150f;
			mat.Gravity = Vector3.Zero;
			mat.ScaleMin = 0.8f;
			mat.ScaleMax = 2f;
			mat.Color = new Color(0.7f, 0.8f, 0.95f, 0.35f);
			windParticles.ProcessMaterial = mat;
			windParticles.Amount = 25;
			windParticles.Lifetime = 2.5f;
			windParticles.VisibilityRect = new Rect2(-600, -400, 1200, 800);
			windParticles.ZIndex = 85;
			player.AddChild(windParticles);
			_windParticlesNode = windParticles;
		}

		GD.Print($"[RandomEventManager] Vent des Oubliés — XP x{_windXpMult}, push toutes les {_windPushInterval}s");
	}

	private void ApplyWindPush()
	{
		// Pousser tous les ennemis visibles dans une direction cohérente
		Node enemyContainer = GetNodeOrNull("../EnemyContainer");
		if (enemyContainer == null)
			return;

		// Direction du vent (fixe pour la durée de l'event)
		Vector2 windDir = Vector2.Right.Rotated((float)GD.RandRange(-0.3f, 0.3f));

		foreach (Node child in enemyContainer.GetChildren())
		{
			if (child is not Enemy enemy || !enemy.IsActive)
				continue;

			// Impulsion visuelle sur l'ennemi
			enemy.GlobalPosition += windDir * _windPushForce * 0.1f;
		}

		// Pousser le joueur légèrement (moitié de la force)
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player != null)
			player.GlobalPosition += windDir * _windPushForce * 0.03f;
	}

	private void RevertForgottenWind()
	{
		_windActive = false;
		_windXpMult = 1f;
		_eventBus.EmitSignal(EventBus.SignalName.XpMultiplierChanged, 1f);

		// Nettoyage particules
		if (_windParticlesNode != null && IsInstanceValid(_windParticlesNode))
			_windParticlesNode.QueueFree();
		_windParticlesNode = null;
	}

	// --- Résonance du Foyer ---

	private void ApplyFoyerResonance(EventData ev)
	{
		float radiusMult = ev.GetFloat("radius_multiplier", 2f);
		float knockbackDmg = ev.GetFloat("knockback_damage", 30f);
		float knockbackRadius = ev.GetFloat("knockback_radius", 300f);

		// Doubler le rayon de sécurité du Foyer
		Foyer foyer = GetNodeOrNull<Foyer>("../Foyer");
		if (foyer != null)
		{
			_originalFoyerRadius = foyer.EffectiveSafeRadius;
			foyer.SetTemporarySafeRadius(_originalFoyerRadius * radiusMult);
			_eventBus.EmitSignal(EventBus.SignalName.FoyerRadiusChanged, _originalFoyerRadius * radiusMult);
		}

		// Knockback initial : repousser et endommager les ennemis proches
		Vector2 foyerPos = foyer?.GlobalPosition ?? Vector2.Zero;
		Node enemyContainer = GetNodeOrNull("../EnemyContainer");
		int knockbackCount = 0;

		if (enemyContainer != null)
		{
			foreach (Node child in enemyContainer.GetChildren())
			{
				if (child is not Enemy enemy || !enemy.IsActive)
					continue;

				float dist = enemy.GlobalPosition.DistanceTo(foyerPos);
				if (dist > knockbackRadius)
					continue;

				// Repousser l'ennemi
				Vector2 pushDir = (enemy.GlobalPosition - foyerPos).Normalized();
				enemy.GlobalPosition += pushDir * (knockbackRadius - dist);
				enemy.TakeDamage(knockbackDmg);
				knockbackCount++;
			}
		}

		// Onde visuelle : cercle doré qui s'étend depuis le Foyer
		if (foyer != null)
		{
			Polygon2D wave = new()
			{
				Color = new Color(1f, 0.85f, 0.4f, 0.4f),
				Polygon = CreateCirclePolygon(10f, 24),
				GlobalPosition = foyerPos,
				ZIndex = 50
			};
			GetNode("..").CallDeferred("add_child", wave);

			Tween waveTween = CreateTween();
			waveTween.SetParallel();
			waveTween.TweenProperty(wave, "scale", new Vector2(knockbackRadius / 10f, knockbackRadius / 10f), 1.5f)
				.SetTrans(Tween.TransitionType.Expo)
				.SetEase(Tween.EaseType.Out);
			waveTween.TweenProperty(wave, "modulate:a", 0f, 1.5f)
				.SetTrans(Tween.TransitionType.Sine);
			waveTween.Chain().TweenCallback(Callable.From(() =>
			{
				if (IsInstanceValid(wave))
					wave.QueueFree();
			}));
		}

		GD.Print($"[RandomEventManager] Résonance du Foyer — rayon x{radiusMult}, {knockbackCount} ennemis repoussés");
	}

	private void RevertFoyerResonance()
	{
		Foyer foyer = GetNodeOrNull<Foyer>("../Foyer");
		if (foyer != null)
		{
			foyer.SetTemporarySafeRadius(_originalFoyerRadius);
			_eventBus.EmitSignal(EventBus.SignalName.FoyerRadiusChanged, _originalFoyerRadius);
		}
	}

	// --- Floraison Spontanée ---

	private void ApplySpontaneousBloom(EventData ev)
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
			return;

		int countMin = (int)ev.GetFloat("flower_count_min", 5f);
		int countMax = (int)ev.GetFloat("flower_count_max", 8f);
		float radius = ev.GetFloat("flower_radius", 200f);
		int herbMin = (int)ev.GetFloat("herb_amount_min", 3f);
		int herbMax = (int)ev.GetFloat("herb_amount_max", 5f);
		float xpPerFlower = ev.GetFloat("xp_per_flower", 20f);

		int count = (int)GD.RandRange(countMin, countMax + 1);

		for (int i = 0; i < count; i++)
		{
			float angle = (float)GD.RandRange(0, Mathf.Tau);
			float dist = (float)GD.RandRange(50, radius);
			Vector2 flowerPos = player.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

			Node2D flower = CreateMemoryFlower(flowerPos, herbMin, herbMax, xpPerFlower);
			GetNode("..").CallDeferred("add_child", flower);
			_bloomFlowers.Add(flower);
		}

		GD.Print($"[RandomEventManager] Floraison Spontanée — {count} fleurs de mémoire !");
	}

	private Node2D CreateMemoryFlower(Vector2 position, int herbMin, int herbMax, float xp)
	{
		Node2D flower = new() { Name = "MemoryFlower", GlobalPosition = position };

		// Tige
		Polygon2D stem = new()
		{
			Color = new Color(0.3f, 0.6f, 0.2f, 0.9f),
			Polygon = new Vector2[] { new(-1, 0), new(1, 0), new(1, -14), new(-1, -14) }
		};
		flower.AddChild(stem);

		// Pétales (couleur aléatoire parmi des tons chauds)
		Color[] petalColors =
		{
			new(0.95f, 0.85f, 0.3f, 0.9f),  // jaune doré
			new(0.85f, 0.5f, 0.7f, 0.9f),   // rose
			new(0.6f, 0.4f, 0.85f, 0.9f),   // violet
			new(0.95f, 0.6f, 0.3f, 0.9f)    // orange
		};
		Color petalColor = petalColors[GD.Randi() % petalColors.Length];

		Polygon2D petals = new()
		{
			Color = petalColor,
			Polygon = CreateCirclePolygon(6f, 6),
			Position = new Vector2(0, -16)
		};
		flower.AddChild(petals);

		// Lueur dorée subtile
		Polygon2D glow = new()
		{
			Color = new Color(1f, 0.9f, 0.4f, 0.15f),
			Polygon = CreateCirclePolygon(14f, 8),
			Position = new Vector2(0, -16)
		};
		flower.AddChild(glow);

		// Pulsation douce
		Tween pulseTween = flower.CreateTween().SetLoops();
		pulseTween.TweenProperty(glow, "scale", new Vector2(1.3f, 1.3f), 1.5f)
			.SetTrans(Tween.TransitionType.Sine);
		pulseTween.TweenProperty(glow, "scale", Vector2.One, 1.5f)
			.SetTrans(Tween.TransitionType.Sine);

		// Zone de récolte
		Area2D harvestArea = new() { Name = "HarvestArea" };
		harvestArea.CollisionLayer = 0;
		harvestArea.CollisionMask = 1;
		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = 30f };
		shape.Shape = circle;
		harvestArea.AddChild(shape);
		flower.AddChild(harvestArea);

		bool harvested = false;
		harvestArea.BodyEntered += (Node2D body) =>
		{
			if (harvested || body is not Player)
				return;

			harvested = true;
			int herbs = (int)GD.RandRange(herbMin, herbMax + 1);
			_eventBus.EmitSignal(EventBus.SignalName.LootReceived, "resource", "herb", herbs);
			_eventBus.EmitSignal(EventBus.SignalName.XpGained, xp);

			// Feedback : flash et disparition
			Tween fadeTween = flower.CreateTween();
			fadeTween.TweenProperty(flower, "modulate", new Color(1, 1, 0.5f, 1.5f), 0.15f);
			fadeTween.TweenProperty(flower, "modulate", new Color(1, 1, 1, 0), 0.8f);
			fadeTween.TweenCallback(Callable.From(() =>
			{
				if (IsInstanceValid(flower))
					flower.QueueFree();
			}));
		};

		return flower;
	}

	private void CleanupBloom()
	{
		foreach (Node2D flower in _bloomFlowers)
		{
			if (flower != null && IsInstanceValid(flower))
				flower.QueueFree();
		}
		_bloomFlowers.Clear();
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
