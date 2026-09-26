using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Events;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Spawn;

public partial class SpawnManager : Node2D
{
	// Demi-cadre visible par défaut (1920×1080 logiques au zoom ×2) si la caméra n'est pas encore disponible.
	private static readonly Vector2 FallbackViewHalfExtents = new(480f, 270f);
	private readonly SpawnPositionPicker _positionPicker = new();
	private Camera2D _camera;

	private float _baseSpawnInterval;
	private float _minSpawnInterval;
	private float _spawnIntervalDecay;
	private float _hpScalingPerMinute;
	private float _dmgScalingPerMinute;
	private int _maxEnemies;
	private float _maxEnemiesGrowthPerMinute;
	private float _enemySpeedBaseMultiplier;
	private float _enemySpeedGrowthPerMinute;
	private float _enemySpeedMeleeBonus;
	private float _enemySpeedRangedBonus;
	private float _enemySpeedEliteBonus;
	private float _enemyAggressionBaseMultiplier;
	private float _enemyAggressionGrowthPerMinute;
	private float _enemyAggressionMeleeBonus;
	private float _enemyAggressionRangedBonus;
	private float _enemyAggressionEliteBonus;
	private float _dayEnemyDespawnDistance;
	private float _dayLocalEnemyRadius;
	private float _dayLocalEnemyTargetBase;
	// Ouverture de run : quelques secondes de répit puis montée progressive de la pression (spawn_flow.json).
	private float _openingGraceSec;
	private float _openingRampSec;
	private float _openingLocalTargetStart;
	private float _openingIntervalMultiplierStart = 1f;
	private float _dayLocalEnemyTargetGrowthPerMinute;
	private int _dayLocalSpawnBurstMax;
	private int _dayInvasionLocalBonus;
	private int _dayInvasionCount;
	private float _dayInvasionDurationSec;
	private float _dayInvasionSpawnRateMultiplier;
	private float _dayInvasionFirstStartRatio;
	private float _dayInvasionLastStartRatio;
	private float _sameTypeClusterChance;
	private int _sameTypeClusterMin;
	private int _sameTypeClusterMax;
	private float _sameTypeClusterSpacingMin;
	private float _sameTypeClusterSpacingMax;
	private float _crisisSpawnMultiplier = 1.65f;
	private int _crisisBurstBase = 8;
	private int _crisisBurstPerIntensity = 4;
	private float _lateGameSpawnMultiplier = 1.4f;
	private float _endgameSpawnMultiplier = 1.85f;
	private float _flatHpMultiplier = 1f;
	private float _flatDmgMultiplier = 1f;

	// Difficulty modifiers from Appel du Vide / Cursed Items
	private float _diffEnemyCountMult = 1f;
	private float _diffEnemyHpMult = 1f;
	private float _diffEnemyDmgMult = 1f;

	private float _elapsedTime;
	private float _spawnTimer;
	private float _cullTimer;
	private float _localDensityTimer;

	private const float LocalDensityCheckInterval = 0.25f;

	// Élites naturelles : une variante renforcée d'une créature locale, à intervalle réglé.
	private readonly List<Enemy> _naturalElites = new();
	private readonly List<string> _affixScratch = new();
	private readonly List<EnemyAffixData> _affixPick = new();
	private float _nextEliteAtSec;

	private GameManager.RunPhase _currentRunPhase = GameManager.RunPhase.Exploration;
	private string _clusterEnemyId;
	private int _clusterRemaining;
	private Vector2 _clusterAnchor;

	private EnemyPool _pool;
	private Player _player;
	private WorldSetup _worldSetup;
	private List<string> _enemyIds;
	private Node _enemyContainer;
	private EventBus _eventBus;
	private GroupCache _groupCache;
	private ErasureManager _erasureManager;
	private CrisisManager _crisisManager;

	// Fallback quand aucun biome n'est disponible
	private static readonly List<string> FallbackDayPool = new() { "shadow_crawler", "fading_spitter" };
	private static readonly List<string> FallbackNightPool = new() { "shadow_crawler", "shade", "shade", "fading_spitter", "void_brute", "wailing_sentinel" };

	public override void _Ready()
	{
		EnemyDataLoader.Load();
		BiomeDataLoader.Load();
		EnemyVariantDataLoader.Load();
		LoadScalingConfig();
		_nextEliteAtSec = EnemyVariantDataLoader.NaturalElites.StartSec;

		_enemyIds = EnemyDataLoader.GetAllIds();

		_pool = GetNode<EnemyPool>("../EnemyPool");
		_enemyContainer = GetNode("../EnemyContainer");

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.RunPhaseChanged += OnRunPhaseChanged;
		_eventBus.CrisisStarted += OnCrisisStarted;
		_eventBus.DifficultyModifierChanged += OnDifficultyModifierChanged;
		_groupCache = GetNode<GroupCache>("/root/GroupCache");
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
		{
			_eventBus.RunPhaseChanged -= OnRunPhaseChanged;
			_eventBus.CrisisStarted -= OnCrisisStarted;
			_eventBus.DifficultyModifierChanged -= OnDifficultyModifierChanged;
		}
	}

	private void OnDifficultyModifierChanged(float enemyCountMult, float enemyHpMult, float enemyDmgMult, float xpMult)
	{
		_diffEnemyCountMult = enemyCountMult;
		_diffEnemyHpMult = enemyHpMult;
		_diffEnemyDmgMult = enemyDmgMult;
		GD.Print($"[SpawnManager] Difficulty modifiers updated — count: x{enemyCountMult:F1}, HP: x{enemyHpMult:F1}, DMG: x{enemyDmgMult:F1}");
	}

	public override void _Process(double delta)
	{
		CachePlayer();
		CacheErasureManager();
		CacheCrisisManager();
		if (_player == null || !IsInstanceValid(_player))
			return;

		float dt = (float)delta;
		_elapsedTime += dt;
		_cullTimer += dt;

		if (_cullTimer >= 1f)
		{
			_cullTimer = 0f;
			CullFarDayEnemies();
		}

		// Répit d'ouverture : rien n'apparaît, et le minuteur n'accumule pas de rafale à rattraper.
		if (_elapsedTime < _openingGraceSec)
			return;

		float elapsedMinutes = _elapsedTime / 60f;
		_spawnTimer += dt;
		int safety = 0;
		while (safety < 8)
		{
			float interval = GetCurrentInterval(elapsedMinutes);
			if (_spawnTimer < interval)
				break;

			_spawnTimer -= interval;
			TrySpawnEnemy(elapsedMinutes);
			safety++;
		}

		_localDensityTimer += dt;
		if (_localDensityTimer >= LocalDensityCheckInterval)
		{
			_localDensityTimer = 0f;
			EnsureDayLocalDensity(elapsedMinutes);
		}

		if (_elapsedTime >= _nextEliteAtSec)
			TrySpawnNaturalElite(elapsedMinutes);
	}

	// =========================================================
	// Scaling
	// =========================================================

	private void ComputeScaling(float elapsedMinutes, out float hpScale, out float dmgScale)
	{
		hpScale = Mathf.Pow(_hpScalingPerMinute, elapsedMinutes) * _flatHpMultiplier;
		dmgScale = Mathf.Pow(_dmgScalingPerMinute, elapsedMinutes) * _flatDmgMultiplier;

		if (_currentRunPhase == GameManager.RunPhase.Crisis)
		{
			hpScale *= 1.18f;
			dmgScale *= 1.12f;
		}
		else if (_currentRunPhase == GameManager.RunPhase.LateGame)
		{
			hpScale *= 1.35f;
			dmgScale *= 1.22f;
		}
		else if (_currentRunPhase == GameManager.RunPhase.Endgame)
		{
			hpScale *= 1.55f;
			dmgScale *= 1.32f;
		}

		// Difficulty modifiers (Appel du Vide + Cursed Items)
		hpScale *= _diffEnemyHpMult;
		dmgScale *= _diffEnemyDmgMult;
	}

	private void ApplyRunPhaseModifiers(Enemy enemy, EnemyData data)
	{
		if (_currentRunPhase == GameManager.RunPhase.Exploration || data.Tier != "normal")
			return;

		PhaseModifierConfig config = EnemyVariantDataLoader.PhaseModifiers;
		if (_currentRunPhase == GameManager.RunPhase.Crisis)
		{
			float chance = config.CrisisAberrationChance
				+ config.CrisisAberrationChancePerIntensity * Mathf.Max(0, (_crisisManager?.CurrentIntensity ?? 1) - 1);
			if (GD.Randf() < chance)
				enemy.ApplyVariant(EnemyVariantDataLoader.GetVariant("aberration"), System.Array.Empty<EnemyAffixData>());
		}

		float affixChance = _currentRunPhase switch
		{
			GameManager.RunPhase.Endgame => config.AffixChanceEndgame,
			GameManager.RunPhase.LateGame => config.AffixChanceLateGame,
			GameManager.RunPhase.Crisis => config.AffixChanceCrisis,
			_ => 0f
		};
		if (config.AffixPool.Count > 0 && GD.Randf() < affixChance)
		{
			EnemyAffixData affix = EnemyVariantDataLoader.GetAffix(config.AffixPool[(int)(GD.Randi() % config.AffixPool.Count)]);
			if (affix != null)
				enemy.ApplyAffix(affix);
		}
	}

	// =========================================================
	// Variantes et API des micro-événements
	// =========================================================

	/// <summary>Transforme une créature en variante avec des affixes tirés sans doublon.</summary>
	public void MakeVariant(Enemy enemy, string variantId)
	{
		EnemyVariantData variant = EnemyVariantDataLoader.GetVariant(variantId);
		if (variant == null)
			return;

		_affixScratch.Clear();
		_affixScratch.AddRange(EnemyVariantDataLoader.NaturalElites.AffixPool);
		_affixPick.Clear();
		for (int i = 0; i < variant.AffixCount && _affixScratch.Count > 0; i++)
		{
			int index = (int)(GD.Randi() % _affixScratch.Count);
			EnemyAffixData affix = EnemyVariantDataLoader.GetAffix(_affixScratch[index]);
			_affixScratch.RemoveAt(index);
			if (affix != null)
				_affixPick.Add(affix);
		}
		enemy.ApplyVariant(variant, _affixPick);
	}

	/// <summary>Créature du biome local, de rang normal, en préférant la liste fournie si elle y figure.</summary>
	public string PickLocalEnemyId(Vector2 worldPos, IReadOnlyList<string> preferred = null)
	{
		CacheWorldSetup();
		List<string> pool = _worldSetup?.GetBiomeAt(worldPos)?.DayEnemyPool;
		if (pool == null || pool.Count == 0)
			pool = FallbackDayPool;

		if (preferred != null)
		{
			foreach (string id in preferred)
			{
				if (pool.Contains(id))
					return id;
			}
		}

		for (int attempt = 0; attempt < 8; attempt++)
		{
			string id = pool[(int)(GD.Randi() % pool.Count)];
			if (EnemyDataLoader.Get(id)?.Tier == "normal")
				return id;
		}
		return pool[0];
	}

	/// <summary>Apparition hors du flux naturel (événements, débogage), sans plafond de population.</summary>
	public Enemy SpawnEventEnemy(string enemyId, Vector2 spawnPos, string variantId = null)
	{
		EnemyData data = EnemyDataLoader.Get(enemyId);
		if (data == null)
			return null;
		Enemy enemy = SpawnAt(data, spawnPos, _elapsedTime / 60f);
		if (!string.IsNullOrEmpty(variantId))
			MakeVariant(enemy, variantId);
		return enemy;
	}

	public bool IsSpawnablePosition(Vector2 position) => !IsWaterAt(position);

	public Vector2 ViewHalfExtents => _player != null ? GetViewHalfExtents() : FallbackViewHalfExtents;

	public float ElapsedSeconds => _elapsedTime;

	private void TrySpawnNaturalElite(float elapsedMinutes)
	{
		NaturalEliteConfig config = EnemyVariantDataLoader.NaturalElites;
		_nextEliteAtSec = _elapsedTime + (float)GD.RandRange(config.IntervalMinSec, config.IntervalMaxSec);

		for (int i = _naturalElites.Count - 1; i >= 0; i--)
		{
			Enemy tracked = _naturalElites[i];
			if (!IsInstanceValid(tracked) || !tracked.IsActive || tracked.IsDying || tracked.Modifiers.Variant?.Id != "elite")
				_naturalElites.RemoveAt(i);
		}
		if (_naturalElites.Count >= config.MaxAlive || _currentRunPhase == GameManager.RunPhase.Death)
			return;

		Vector2 spawnPos = GetSpawnPosition();
		string enemyId = PickLocalEnemyId(spawnPos);
		EnemyData data = EnemyDataLoader.Get(enemyId);
		if (data == null)
			return;
		Enemy enemy = SpawnAt(data, spawnPos, elapsedMinutes);
		MakeVariant(enemy, "elite");
		_naturalElites.Add(enemy);
		GD.Print($"[SpawnManager] Élite : {enemy.DisplayName} à {_elapsedTime:F0} s");
	}

	// =========================================================
	// Spawn de jour (inchange sauf scaling)
	// =========================================================

	private void EnsureDayLocalDensity(float elapsedMinutes)
	{
		float zoneMemoryMult = _erasureManager?.GetSpawnDensityMultiplier(_player.GlobalPosition) ?? 1f;
		float phaseMult = _currentRunPhase switch
		{
			GameManager.RunPhase.Crisis => _crisisSpawnMultiplier,
			GameManager.RunPhase.LateGame => _lateGameSpawnMultiplier,
			GameManager.RunPhase.Endgame => _endgameSpawnMultiplier,
			_ => 1f
		};
		float fullTarget = _dayLocalEnemyTargetBase + _dayLocalEnemyTargetGrowthPerMinute * elapsedMinutes;
		float openingTarget = Mathf.Lerp(Mathf.Min(_openingLocalTargetStart, fullTarget), fullTarget, OpeningProgress);
		int target = Mathf.RoundToInt(openingTarget * _diffEnemyCountMult * zoneMemoryMult);
		target = Mathf.RoundToInt(target * phaseMult);

		int nearCount = CountActiveEnemiesNear(_player.GlobalPosition, _dayLocalEnemyRadius);
		int missing = target - nearCount;
		if (missing <= 0)
			return;

		int budget = Mathf.Min(_dayLocalSpawnBurstMax, missing);
		for (int i = 0; i < budget; i++)
			TrySpawnEnemy(elapsedMinutes);
	}

	private int CountActiveEnemiesNear(Vector2 center, float radius)
	{
		if (radius <= 0f)
			return 0;

		float radiusSq = radius * radius;
		int count = 0;
		Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
		foreach (Node node in enemies)
		{
			if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
				continue;

			if (enemy.GlobalPosition.DistanceSquaredTo(center) <= radiusSq)
				count++;
		}

		return count;
	}

	private float GetCurrentInterval(float elapsedMinutes)
	{
		float baseInterval = Mathf.Max(
			_baseSpawnInterval - (_spawnIntervalDecay * elapsedMinutes),
			_minSpawnInterval
		);
		float phaseFactor = _currentRunPhase switch
		{
			GameManager.RunPhase.Crisis => 1f / _crisisSpawnMultiplier,
			GameManager.RunPhase.LateGame => 1f / _lateGameSpawnMultiplier,
			GameManager.RunPhase.Endgame => 1f / _endgameSpawnMultiplier,
			_ => 1f
		};

		float openingFactor = Mathf.Lerp(_openingIntervalMultiplierStart, 1f, OpeningProgress);
		return Mathf.Max(baseInterval * phaseFactor * openingFactor, _minSpawnInterval);
	}

	/// <summary>0 à la fin du répit, 1 une fois la montée d'ouverture terminée.</summary>
	private float OpeningProgress => _openingRampSec <= 0f
		? 1f
		: Mathf.Clamp((_elapsedTime - _openingGraceSec) / _openingRampSec, 0f, 1f);

	private bool IsInDayInvasionWindow()
	{
		return false;
	}

	private void CullFarDayEnemies()
	{
		if (_dayEnemyDespawnDistance <= 0f)
			return;

		if (_player == null || !IsInstanceValid(_player))
			return;

		Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
		if (enemies.Count == 0)
			return;

		List<Enemy> toDespawn = new();
		float maxDist = _dayEnemyDespawnDistance;

		foreach (Node node in enemies)
		{
			if (node is not Enemy enemy)
				continue;
			if (!enemy.IsActive || enemy.IsDying || enemy.Modifiers.IsEventBound)
				continue;

			if (enemy.GlobalPosition.DistanceTo(_player.GlobalPosition) > maxDist)
				toDespawn.Add(enemy);
		}

		foreach (Enemy enemy in toDespawn)
			_pool.Return(enemy);
	}

	private void TrySpawnEnemy(float elapsedMinutes)
	{
		int currentMaxEnemies = GetCurrentMaxEnemies(elapsedMinutes);
		if (_pool.ActiveCount >= currentMaxEnemies)
			return;

		// Position d'abord, puis on interroge le biome a cet endroit
		Vector2 spawnPos = GetSpawnPosition();
		string enemyId = PickEnemyForPosition(spawnPos);
		spawnPos = ApplyClusterSpawnOffset(spawnPos, enemyId);
		EnemyData data = EnemyDataLoader.Get(enemyId);
		if (data == null)
			return;

		Enemy enemy = SpawnAt(data, spawnPos, elapsedMinutes);
		ApplyRunPhaseModifiers(enemy, data);
	}

	private Enemy SpawnAt(EnemyData data, Vector2 spawnPos, float elapsedMinutes)
	{
		ComputeScaling(elapsedMinutes, out float hpScale, out float dmgScale);

		Enemy enemy = _pool.Get();
		enemy.GlobalPosition = spawnPos;
		_enemyContainer.AddChild(enemy);
		enemy.Initialize(data, hpScale, dmgScale);
		float speedMultiplier = ComputeEnemySpeedMultiplier(data, elapsedMinutes, spawnPos);
		float aggressionMultiplier = ComputeEnemyAggressionMultiplier(data, elapsedMinutes);
		enemy.ApplySpawnTuning(speedMultiplier, aggressionMultiplier);
		_eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, data.Id, hpScale, dmgScale);
		return enemy;
	}

	private float ComputeEnemySpeedMultiplier(EnemyData data, float elapsedMinutes, Vector2 spawnPos = default)
	{
		float multiplier = _enemySpeedBaseMultiplier + _enemySpeedGrowthPerMinute * elapsedMinutes;

		if (data.Type == "melee")
			multiplier *= _enemySpeedMeleeBonus;
		else if (data.Type == "ranged")
			multiplier *= _enemySpeedRangedBonus;

		if (data.Tier != "normal")
			multiplier *= _enemySpeedEliteBonus;

		// Enemies in faded zones are faster
		if (spawnPos != default && _erasureManager != null)
			multiplier += _erasureManager.GetEnemySpeedBonus(spawnPos);

		return Mathf.Max(0.5f, multiplier);
	}

	public void ForceSpawnEnemy(string enemyId, Vector2 spawnPos)
	{
		if (SpawnEventEnemy(enemyId, spawnPos) == null)
			return;
		GD.Print($"[SpawnManager] Debug spawned: {enemyId} at {spawnPos}");
	}

	private float ComputeEnemyAggressionMultiplier(EnemyData data, float elapsedMinutes)
	{
		float multiplier = _enemyAggressionBaseMultiplier + _enemyAggressionGrowthPerMinute * elapsedMinutes;

		if (data.Type == "melee")
			multiplier *= _enemyAggressionMeleeBonus;
		else if (data.Type == "ranged")
			multiplier *= _enemyAggressionRangedBonus;

		if (data.Tier != "normal")
			multiplier *= _enemyAggressionEliteBonus;

		return Mathf.Clamp(multiplier, 0.7f, 3f);
	}

	private int GetCurrentMaxEnemies(float elapsedMinutes)
	{
		float scaled = _maxEnemies + _maxEnemiesGrowthPerMinute * elapsedMinutes;
		return Mathf.Max(1, Mathf.RoundToInt(scaled));
	}

	/// <summary>
	/// Selectionne un ennemi en fonction du biome a la position donnee.
	/// Exploration : pool ambiant du biome. Crise/Late game : pool hostile.
	/// </summary>
	private string PickEnemyForPosition(Vector2 worldPos)
	{
		bool isNight = _currentRunPhase is GameManager.RunPhase.Crisis or GameManager.RunPhase.LateGame or GameManager.RunPhase.Endgame;

		CacheWorldSetup();
		BiomeData biome = _worldSetup?.GetBiomeAt(worldPos);

		List<string> pool;
		if (biome != null)
		{
			pool = isNight ? biome.NightEnemyPool : biome.DayEnemyPool;
			if (pool == null || pool.Count == 0)
				pool = isNight ? FallbackNightPool : FallbackDayPool;
		}
		else
		{
			pool = isNight ? FallbackNightPool : FallbackDayPool;
		}

		if (_clusterRemaining > 0 && !string.IsNullOrEmpty(_clusterEnemyId))
		{
			_clusterRemaining--;
			return _clusterEnemyId;
		}

		int index = (int)(GD.Randi() % pool.Count);
		string picked = pool[index];

		if (GD.Randf() < _sameTypeClusterChance)
		{
			int clusterSize = (int)GD.RandRange(_sameTypeClusterMin, _sameTypeClusterMax + 1);
			_clusterEnemyId = picked;
			_clusterRemaining = Mathf.Max(0, clusterSize - 1);
			_clusterAnchor = worldPos;
		}
		else
		{
			_clusterEnemyId = null;
			_clusterRemaining = 0;
		}

		return picked;
	}

	private Vector2 ApplyClusterSpawnOffset(Vector2 spawnPos, string enemyId)
	{
		if (string.IsNullOrEmpty(_clusterEnemyId) || enemyId != _clusterEnemyId)
			return spawnPos;

		float angle = (float)GD.RandRange(0, Mathf.Tau);
		float radius = (float)GD.RandRange(_sameTypeClusterSpacingMin, _sameTypeClusterSpacingMax);
		Vector2 candidate = _clusterAnchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

		if (!IsValidDaySpawnPosition(candidate))
			return spawnPos;

		_clusterAnchor = candidate;
		return candidate;
	}

	private bool IsValidDaySpawnPosition(Vector2 position)
	{
		return !IsWaterAt(position);
	}

	private Vector2 GetSpawnPosition()
	{
		return GetDaySpawnPosition();
	}

	/// <summary>Spawn juste hors du cadre visible, biaisé vers l'avant du joueur, jamais sur l'eau.</summary>
	private Vector2 GetDaySpawnPosition()
	{
		float marginScale = _currentRunPhase switch
		{
			GameManager.RunPhase.Crisis => 0.6f,
			GameManager.RunPhase.LateGame => 0.85f,
			GameManager.RunPhase.Endgame => 0.5f,
			_ => 1f
		};
		Vector2 halfExtents = GetViewHalfExtents();
		Vector2 moveDirection = _player.Velocity.Normalized();

		Vector2 position = _player.GlobalPosition;
		for (int attempt = 0; attempt < 15; attempt++)
		{
			position = _positionPicker.Pick(_player.GlobalPosition, halfExtents, moveDirection, marginScale);
			if (!IsWaterAt(position))
				return position;
		}
		return position;
	}

	private Vector2 GetViewHalfExtents()
	{
		if (_camera == null || !IsInstanceValid(_camera))
			_camera = _player.GetViewport()?.GetCamera2D();
		if (_camera == null)
			return FallbackViewHalfExtents;
		return GetViewport().GetVisibleRect().Size / (2f * _camera.Zoom);
	}

	private bool IsWaterAt(Vector2 worldPos)
	{
		if (_worldSetup == null)
			CacheWorldSetup();
		return _worldSetup != null && _worldSetup.IsWaterAt(worldPos);
	}

	private void OnRunPhaseChanged(string oldPhase, string newPhase)
	{
		_currentRunPhase = newPhase switch
		{
			"Crisis" => GameManager.RunPhase.Crisis,
			"LateGame" => GameManager.RunPhase.LateGame,
			"Endgame" => GameManager.RunPhase.Endgame,
			"Death" => GameManager.RunPhase.Death,
			_ => GameManager.RunPhase.Exploration
		};
	}

	private void OnCrisisStarted(int crisisNumber, int intensity)
	{
		float elapsedMinutes = _elapsedTime / 60f;
		int burstCount = _crisisBurstBase + _crisisBurstPerIntensity * Mathf.Max(0, intensity - 1);
		for (int i = 0; i < burstCount; i++)
			TrySpawnEnemy(elapsedMinutes);
	}

	// =========================================================
	// Chargement config
	// =========================================================

	private void LoadScalingConfig()
	{
		FileAccess file = FileAccess.Open("res://data/scaling/spawn_flow.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[SpawnManager] Cannot open spawn_flow.json, using defaults");
			SetDefaults();
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[SpawnManager] Parse error: {json.GetErrorMessage()}");
			SetDefaults();
			return;
		}

		Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
		_baseSpawnInterval = (float)dict["base_spawn_interval"].AsDouble();
		_minSpawnInterval = (float)dict["min_spawn_interval"].AsDouble();
		_spawnIntervalDecay = (float)dict["spawn_interval_decay_per_minute"].AsDouble();
		_hpScalingPerMinute = (float)dict["hp_scaling_per_minute"].AsDouble();
		_dmgScalingPerMinute = (float)dict["damage_scaling_per_minute"].AsDouble();
		_maxEnemies = (int)dict["max_enemies_on_screen"].AsDouble();
		_maxEnemiesGrowthPerMinute = dict.ContainsKey("max_enemies_growth_per_minute") ? (float)dict["max_enemies_growth_per_minute"].AsDouble() : 0f;
		_enemySpeedBaseMultiplier = dict.ContainsKey("enemy_speed_base_multiplier") ? (float)dict["enemy_speed_base_multiplier"].AsDouble() : 1f;
		_enemySpeedGrowthPerMinute = dict.ContainsKey("enemy_speed_growth_per_minute") ? (float)dict["enemy_speed_growth_per_minute"].AsDouble() : 0f;
		_enemySpeedMeleeBonus = dict.ContainsKey("enemy_speed_melee_bonus") ? (float)dict["enemy_speed_melee_bonus"].AsDouble() : 1f;
		_enemySpeedRangedBonus = dict.ContainsKey("enemy_speed_ranged_bonus") ? (float)dict["enemy_speed_ranged_bonus"].AsDouble() : 1f;
		_enemySpeedEliteBonus = dict.ContainsKey("enemy_speed_elite_bonus") ? (float)dict["enemy_speed_elite_bonus"].AsDouble() : 1f;
		_enemyAggressionBaseMultiplier = dict.ContainsKey("enemy_aggression_base_multiplier") ? (float)dict["enemy_aggression_base_multiplier"].AsDouble() : 1f;
		_enemyAggressionGrowthPerMinute = dict.ContainsKey("enemy_aggression_growth_per_minute") ? (float)dict["enemy_aggression_growth_per_minute"].AsDouble() : 0f;
		_enemyAggressionMeleeBonus = dict.ContainsKey("enemy_aggression_melee_bonus") ? (float)dict["enemy_aggression_melee_bonus"].AsDouble() : 1f;
		_enemyAggressionRangedBonus = dict.ContainsKey("enemy_aggression_ranged_bonus") ? (float)dict["enemy_aggression_ranged_bonus"].AsDouble() : 1f;
		_enemyAggressionEliteBonus = dict.ContainsKey("enemy_aggression_elite_bonus") ? (float)dict["enemy_aggression_elite_bonus"].AsDouble() : 1f;
		_dayEnemyDespawnDistance = dict.ContainsKey("day_enemy_despawn_distance") ? (float)dict["day_enemy_despawn_distance"].AsDouble() : 1400f;
		_dayLocalEnemyRadius = dict.ContainsKey("local_enemy_radius") ? (float)dict["local_enemy_radius"].AsDouble() : 900f;
		_dayLocalEnemyTargetBase = dict.ContainsKey("local_enemy_target_base") ? (float)dict["local_enemy_target_base"].AsDouble() : 24f;
		_openingGraceSec = dict.ContainsKey("opening_grace_seconds") ? (float)dict["opening_grace_seconds"].AsDouble() : 0f;
		_openingRampSec = dict.ContainsKey("opening_ramp_seconds") ? (float)dict["opening_ramp_seconds"].AsDouble() : 0f;
		_openingLocalTargetStart = dict.ContainsKey("opening_local_target_start") ? (float)dict["opening_local_target_start"].AsDouble() : _dayLocalEnemyTargetBase;
		_openingIntervalMultiplierStart = dict.ContainsKey("opening_interval_multiplier_start") ? (float)dict["opening_interval_multiplier_start"].AsDouble() : 1f;
		_dayLocalEnemyTargetGrowthPerMinute = dict.ContainsKey("local_enemy_target_growth_per_minute") ? (float)dict["local_enemy_target_growth_per_minute"].AsDouble() : 6f;
		_dayLocalSpawnBurstMax = dict.ContainsKey("local_spawn_burst_max") ? (int)dict["local_spawn_burst_max"].AsDouble() : 4;
		_sameTypeClusterChance = dict.ContainsKey("same_type_cluster_chance") ? (float)dict["same_type_cluster_chance"].AsDouble() : 0.4f;
		_sameTypeClusterMin = dict.ContainsKey("same_type_cluster_min") ? (int)dict["same_type_cluster_min"].AsDouble() : 2;
		_sameTypeClusterMax = dict.ContainsKey("same_type_cluster_max") ? (int)dict["same_type_cluster_max"].AsDouble() : 4;
		_sameTypeClusterSpacingMin = dict.ContainsKey("same_type_cluster_spacing_min") ? (float)dict["same_type_cluster_spacing_min"].AsDouble() : 18f;
		_sameTypeClusterSpacingMax = dict.ContainsKey("same_type_cluster_spacing_max") ? (float)dict["same_type_cluster_spacing_max"].AsDouble() : 46f;
		_crisisSpawnMultiplier = dict.ContainsKey("crisis_spawn_multiplier") ? (float)dict["crisis_spawn_multiplier"].AsDouble() : _crisisSpawnMultiplier;
		_crisisBurstBase = dict.ContainsKey("crisis_burst_base") ? (int)dict["crisis_burst_base"].AsDouble() : _crisisBurstBase;
		_crisisBurstPerIntensity = dict.ContainsKey("crisis_burst_per_intensity") ? (int)dict["crisis_burst_per_intensity"].AsDouble() : _crisisBurstPerIntensity;
		_lateGameSpawnMultiplier = dict.ContainsKey("late_game_spawn_multiplier") ? (float)dict["late_game_spawn_multiplier"].AsDouble() : _lateGameSpawnMultiplier;
		_endgameSpawnMultiplier = dict.ContainsKey("endgame_spawn_multiplier") ? (float)dict["endgame_spawn_multiplier"].AsDouble() : _endgameSpawnMultiplier;
		_positionPicker.MarginMin = dict.ContainsKey("spawn_screen_margin_min") ? (float)dict["spawn_screen_margin_min"].AsDouble() : _positionPicker.MarginMin;
		_positionPicker.MarginMax = dict.ContainsKey("spawn_screen_margin_max") ? (float)dict["spawn_screen_margin_max"].AsDouble() : _positionPicker.MarginMax;
		_positionPicker.ForwardBias = dict.ContainsKey("spawn_forward_bias") ? (float)dict["spawn_forward_bias"].AsDouble() : _positionPicker.ForwardBias;
		_positionPicker.ForwardArcDegrees = dict.ContainsKey("spawn_forward_arc_degrees") ? (float)dict["spawn_forward_arc_degrees"].AsDouble() : _positionPicker.ForwardArcDegrees;

		GD.Print($"[SpawnManager] Config loaded — interval: {_baseSpawnInterval}s, max: {_maxEnemies}, crisis x{_crisisSpawnMultiplier:F2}");
	}

	private void SetDefaults()
	{
		_baseSpawnInterval = 2.0f;
		_minSpawnInterval = 0.3f;
		_spawnIntervalDecay = 0.05f;
		_hpScalingPerMinute = 1.06f;
		_dmgScalingPerMinute = 1.04f;
		_maxEnemies = 120;
		_maxEnemiesGrowthPerMinute = 4f;
		_enemySpeedBaseMultiplier = 1.10f;
		_enemySpeedGrowthPerMinute = 0.025f;
		_enemySpeedMeleeBonus = 1f;
		_enemySpeedRangedBonus = 1f;
		_enemySpeedEliteBonus = 1f;
		_enemyAggressionBaseMultiplier = 1.05f;
		_enemyAggressionGrowthPerMinute = 0.04f;
		_enemyAggressionMeleeBonus = 1f;
		_enemyAggressionRangedBonus = 1f;
		_enemyAggressionEliteBonus = 1f;
		_dayEnemyDespawnDistance = 1400f;
		_dayLocalEnemyRadius = 920f;
		_dayLocalEnemyTargetBase = 6f;
		_dayLocalEnemyTargetGrowthPerMinute = 2.5f;
		_dayLocalSpawnBurstMax = 3;
		_sameTypeClusterChance = 0.6f;
		_sameTypeClusterMin = 2;
		_sameTypeClusterMax = 5;
		_sameTypeClusterSpacingMin = 16f;
		_sameTypeClusterSpacingMax = 42f;
		_crisisSpawnMultiplier = 1.65f;
		_crisisBurstBase = 8;
		_crisisBurstPerIntensity = 4;
		_lateGameSpawnMultiplier = 1.4f;
		_endgameSpawnMultiplier = 1.85f;
	}

	/// <summary>
	/// Override scaling values at runtime for simulation.
	/// Only overrides keys present in the dictionary. Does NOT modify the JSON file on disk.
	/// </summary>
	public void ApplyScalingOverrides(Dictionary<string, float> overrides)
	{
		foreach (KeyValuePair<string, float> kv in overrides)
		{
			switch (kv.Key)
			{
				case "base_spawn_interval": _baseSpawnInterval = kv.Value; break;
				case "min_spawn_interval": _minSpawnInterval = kv.Value; break;
				case "spawn_interval_decay_per_minute": _spawnIntervalDecay = kv.Value; break;
				case "hp_scaling_per_minute": _hpScalingPerMinute = kv.Value; break;
				case "damage_scaling_per_minute": _dmgScalingPerMinute = kv.Value; break;
				case "max_enemies_on_screen": _maxEnemies = (int)kv.Value; break;
				case "max_enemies_growth_per_minute": _maxEnemiesGrowthPerMinute = kv.Value; break;
				case "enemy_speed_base_multiplier": _enemySpeedBaseMultiplier = kv.Value; break;
				case "enemy_speed_growth_per_minute": _enemySpeedGrowthPerMinute = kv.Value; break;
				case "enemy_speed_melee_bonus": _enemySpeedMeleeBonus = kv.Value; break;
				case "enemy_speed_ranged_bonus": _enemySpeedRangedBonus = kv.Value; break;
				case "enemy_speed_elite_bonus": _enemySpeedEliteBonus = kv.Value; break;
				case "enemy_aggression_base_multiplier": _enemyAggressionBaseMultiplier = kv.Value; break;
				case "enemy_aggression_growth_per_minute": _enemyAggressionGrowthPerMinute = kv.Value; break;
				case "enemy_aggression_melee_bonus": _enemyAggressionMeleeBonus = kv.Value; break;
				case "enemy_aggression_ranged_bonus": _enemyAggressionRangedBonus = kv.Value; break;
				case "enemy_aggression_elite_bonus": _enemyAggressionEliteBonus = kv.Value; break;
				case "day_enemy_despawn_distance": _dayEnemyDespawnDistance = kv.Value; break;
				case "day_local_enemy_radius": _dayLocalEnemyRadius = kv.Value; break;
				case "day_local_enemy_target_base": _dayLocalEnemyTargetBase = kv.Value; break;
				case "day_local_enemy_target_growth_per_minute": _dayLocalEnemyTargetGrowthPerMinute = kv.Value; break;
				case "day_local_spawn_burst_max": _dayLocalSpawnBurstMax = (int)kv.Value; break;
				case "same_type_cluster_chance": _sameTypeClusterChance = kv.Value; break;
				case "same_type_cluster_min": _sameTypeClusterMin = (int)kv.Value; break;
				case "same_type_cluster_max": _sameTypeClusterMax = (int)kv.Value; break;
				case "same_type_cluster_spacing_min": _sameTypeClusterSpacingMin = kv.Value; break;
				case "same_type_cluster_spacing_max": _sameTypeClusterSpacingMax = kv.Value; break;
				case "flat_hp_multiplier": _flatHpMultiplier = kv.Value; break;
				case "flat_dmg_multiplier": _flatDmgMultiplier = kv.Value; break;
			}
		}
		GD.Print($"[SpawnManager] Applied {overrides.Count} scaling override(s)");
	}

	private void CachePlayer()
	{
		if (_player != null && IsInstanceValid(_player))
			return;

		Node playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player p)
			_player = p;
	}

	private void CacheErasureManager()
	{
		if (_erasureManager != null && IsInstanceValid(_erasureManager))
			return;

		_erasureManager = GetNodeOrNull<ErasureManager>("../ErasureManager");
	}

	private void CacheCrisisManager()
	{
		if (_crisisManager != null && IsInstanceValid(_crisisManager))
			return;

		_crisisManager = GetNodeOrNull<CrisisManager>("../CrisisManager");
	}

	private void CacheWorldSetup()
	{
		if (_worldSetup != null && IsInstanceValid(_worldSetup))
			return;

		_worldSetup = GetNodeOrNull<WorldSetup>("/root/Main");
	}

}
