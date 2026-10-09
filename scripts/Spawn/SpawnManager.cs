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
	// Croissance des PV par segments (minute de début, facteur par minute), triés par minute de début.
	private readonly List<float> _hpSegmentFromMinute = new();
	private readonly List<float> _hpSegmentPerMinute = new();
	private float _dmgScalingPerMinute;
	private float _rangedDamageGrowthShare = 1f;
	private float _xpGrowthPerMinute;
	private float _xpOblivionBonus;
	private float _xpCrisisMultiplier = 1f;
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

	// Péril (PerilManager)
	// Péril (plan 28) : celui que le joueur a pris, plus celui que le temps ajoute (P4), aux mêmes effets par point.
	private int _playerPeril;
	private float _timePerilFromMinute;
	private float _timePerilPerMinute;
	// PV et dégâts des créatures apparues pendant une Résurgence, en fin de partie et en endgame.
	private float _crisisHpMultiplier = 1.18f;
	private float _crisisDamageMultiplier = 1.12f;
	private float _lateGameHpMultiplier = 1.35f;
	private float _lateGameDamageMultiplier = 1.22f;
	private float _endgameHpMultiplier = 1.55f;
	private float _endgameDamageMultiplier = 1.32f;

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
	// Oublis (plan 17 lot 3D) : élites en plus, affixes aussi hors Résurgence.
	private int _extraElites;
	private float _affixChanceBonus;

	private GameManager.RunPhase _currentRunPhase = GameManager.RunPhase.Exploration;
	private string _clusterEnemyId;
	private int _clusterRemaining;
	private Vector2 _clusterAnchor;

	private EnemyPool _pool;
	private Player _player;
	private WorldSetup _worldSetup;
	private Node _enemyContainer;
	private EventBus _eventBus;
	private ErasureManager _erasureManager;
	private CrisisManager _crisisManager;

	// Groupes de secours (spawn_flow.json) quand la position n'a pas de biome ou que son groupe est vide.
	private readonly List<string> _fallbackExplorationPool = new();
	private readonly List<string> _fallbackResurgencePool = new();

	public override void _Ready()
	{
		EnemyDataLoader.Load();
		BiomeDataLoader.Load();
		EnemyVariantDataLoader.Load();
		LoadScalingConfig();
		_nextEliteAtSec = EnemyVariantDataLoader.NaturalElites.StartSec;

		_pool = GetNode<EnemyPool>("../EnemyPool");
		_enemyContainer = GetNode("../EnemyContainer");

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.RunPhaseChanged += OnRunPhaseChanged;
		_eventBus.CrisisStarted += OnCrisisStarted;
		_eventBus.PerilChanged += OnPerilChanged;
		_eventBus.OubliEffectChanged += OnOubliEffectChanged;
		EnemyTracking.SetDetectionScale(1f);
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
		{
			_eventBus.RunPhaseChanged -= OnRunPhaseChanged;
			_eventBus.CrisisStarted -= OnCrisisStarted;
			_eventBus.PerilChanged -= OnPerilChanged;
			_eventBus.OubliEffectChanged -= OnOubliEffectChanged;
		}
	}

	private void OnOubliEffectChanged(string effect, float total)
	{
		switch (effect)
		{
			case "extra_elite":
				int previous = _extraElites;
				_extraElites = Mathf.RoundToInt(total);
				// Une élite de plus : elle arrive tout de suite, pas au prochain tirage d'intervalle.
				if (_extraElites > previous)
					_nextEliteAtSec = _elapsedTime;
				break;
			case "affix_chance":
				_affixChanceBonus = total;
				break;
			case "enemy_detection":
				EnemyTracking.SetDetectionScale(1f + total);
				break;
		}
	}

	private void OnPerilChanged(int peril)
	{
		_playerPeril = peril;
		GD.Print($"[SpawnManager] Péril {peril} : nombre x{PerilDataLoader.EnemyCountMultiplier(peril):F2}, PV x{PerilDataLoader.EnemyHpMultiplier(peril):F2}, dégâts x{PerilDataLoader.EnemyDamageMultiplier(peril):F2}");
	}

	/// <summary>
	/// Points de Péril en vigueur : ceux du joueur, plus ceux que le temps ajoute après <c>time_peril_from_minute</c>
	/// (plan 28 P4 : la run se durcit d'elle-même par le nombre de créatures plutôt que par leurs seuls PV).
	/// </summary>
	private float PerilPoints(float elapsedMinutes) =>
		_playerPeril + Mathf.Max(0f, elapsedMinutes - _timePerilFromMinute) * _timePerilPerMinute;

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

	private void ComputeScaling(EnemyData data, float elapsedMinutes, out float hpScale, out float dmgScale)
	{
		// Une pente par segment, quelle que soit la phase : la puissance du joueur décolle par à-coups (plan 21 H3).
		hpScale = _flatHpMultiplier;
		for (int i = 0; i < _hpSegmentFromMinute.Count; i++)
		{
			float end = i + 1 < _hpSegmentFromMinute.Count ? _hpSegmentFromMinute[i + 1] : float.MaxValue;
			float minutes = Mathf.Min(elapsedMinutes, end) - _hpSegmentFromMinute[i];
			if (minutes <= 0f)
				break;
			hpScale *= Mathf.Pow(_hpSegmentPerMinute[i], minutes);
		}
		// Les tirs et les zones touchent de loin, en nombre, et leurs créatures meurent peu : leurs dégâts ne suivent
		// qu'une part de la croissance commune (plan 20 §7.1).
		float damageGrowthMinutes = data.CombatType == EnemyCombatType.Ranged ? elapsedMinutes * _rangedDamageGrowthShare : elapsedMinutes;
		dmgScale = Mathf.Pow(_dmgScalingPerMinute, damageGrowthMinutes) * _flatDmgMultiplier;

		if (_currentRunPhase == GameManager.RunPhase.Crisis)
		{
			hpScale *= _crisisHpMultiplier;
			dmgScale *= _crisisDamageMultiplier;
		}
		else if (_currentRunPhase == GameManager.RunPhase.LateGame)
		{
			hpScale *= _lateGameHpMultiplier;
			dmgScale *= _lateGameDamageMultiplier;
		}
		else if (_currentRunPhase == GameManager.RunPhase.Endgame)
		{
			hpScale *= _endgameHpMultiplier;
			dmgScale *= _endgameDamageMultiplier;
		}

		float peril = PerilPoints(elapsedMinutes);
		hpScale *= PerilDataLoader.EnemyHpMultiplier(peril);
		dmgScale *= PerilDataLoader.EnemyDamageMultiplier(peril);
	}

	private void ApplyRunPhaseModifiers(Enemy enemy, EnemyData data)
	{
		if (data.Tier != EnemyTier.Normal || (_currentRunPhase == GameManager.RunPhase.Exploration && _affixChanceBonus <= 0f))
			return;

		PhaseModifierConfig config = EnemyVariantDataLoader.PhaseModifiers;
		if (_currentRunPhase == GameManager.RunPhase.Crisis)
		{
			float chance = config.CrisisAberrationChance
				+ config.CrisisAberrationChancePerIntensity * Mathf.Max(0, (_crisisManager?.CurrentIntensity ?? 1) - 1);
			if (RunRandom.Spawn.Randf() < chance)
				enemy.ApplyVariant(EnemyVariantDataLoader.GetVariant("aberration"), System.Array.Empty<EnemyAffixData>());
		}

		float affixChance = _currentRunPhase switch
		{
			GameManager.RunPhase.Endgame => config.AffixChanceEndgame,
			GameManager.RunPhase.LateGame => config.AffixChanceLateGame,
			GameManager.RunPhase.Crisis => config.AffixChanceCrisis,
			_ => 0f
		} + _affixChanceBonus;
		if (config.AffixPool.Count > 0 && RunRandom.Spawn.Randf() < affixChance)
		{
			EnemyAffixData affix = EnemyVariantDataLoader.GetAffix(config.AffixPool[(int)(RunRandom.Spawn.Randi() % config.AffixPool.Count)]);
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
			int index = (int)(RunRandom.Spawn.Randi() % _affixScratch.Count);
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
		IReadOnlyList<string> pool = _worldSetup?.GetBiomeAt(worldPos)?.ExplorationEnemyPool;
		if (pool == null || pool.Count == 0)
			pool = _fallbackExplorationPool;
		if (pool.Count == 0)
			return null;

		if (preferred != null)
		{
			foreach (string id in preferred)
			{
				if (JsonConfigReader.Contains(pool, id))
					return id;
			}
		}

		for (int attempt = 0; attempt < 8; attempt++)
		{
			string id = PickWeighted(pool, _elapsedTime / 60f);
			if (EnemyDataLoader.Get(id)?.Tier == EnemyTier.Normal)
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
		_nextEliteAtSec = _elapsedTime + RunRandom.Spawn.RandfRange((float)config.IntervalMinSec, (float)config.IntervalMaxSec);

		for (int i = _naturalElites.Count - 1; i >= 0; i--)
		{
			Enemy tracked = _naturalElites[i];
			if (!IsInstanceValid(tracked) || !tracked.IsActive || tracked.IsDying || tracked.Modifiers.Variant?.Id != "elite")
				_naturalElites.RemoveAt(i);
		}
		if (_naturalElites.Count >= config.MaxAlive + _extraElites || _currentRunPhase == GameManager.RunPhase.Death)
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
		int target = Mathf.RoundToInt(openingTarget * PerilDataLoader.EnemyCountMultiplier(PerilPoints(elapsedMinutes)) * zoneMemoryMult);
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
		using CrowdQuery crowd = CrowdIndex.Near(center, radius);
		foreach (Node node in crowd.Targets)
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

		if (CrowdIndex.Count == 0)
			return;

		using CrowdQuery crowd = CrowdIndex.All();
		List<Enemy> toDespawn = new();
		float maxDist = _dayEnemyDespawnDistance;

		foreach (Node node in crowd.Targets)
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
		ComputeScaling(data, elapsedMinutes, out float hpScale, out float dmgScale);

		Enemy enemy = _pool.Get();
		enemy.GlobalPosition = spawnPos;
		_enemyContainer.AddChild(enemy);
		enemy.Initialize(data, hpScale, dmgScale);
		float speedMultiplier = ComputeEnemySpeedMultiplier(data, elapsedMinutes, spawnPos);
		float aggressionMultiplier = ComputeEnemyAggressionMultiplier(data, elapsedMinutes);
		enemy.ApplySpawnTuning(speedMultiplier, aggressionMultiplier);
		enemy.MultiplyXpReward(ComputeXpMultiplier(elapsedMinutes, spawnPos));
		_eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, data.Id, hpScale, dmgScale);
		return enemy;
	}

	/// <summary>
	/// XP qui suit le risque (plan 20 §6.9, R1-A) : plus la run avance, plus le lieu est oublié, et pendant une
	/// Résurgence, plus chaque créature rapporte. Fixée à l'apparition : rien ne se calcule à la mort.
	/// </summary>
	private float ComputeXpMultiplier(float elapsedMinutes, Vector2 spawnPos)
	{
		float multiplier = 1f + _xpGrowthPerMinute * elapsedMinutes;
		if (_erasureManager != null)
			multiplier *= 1f + _xpOblivionBonus * (1f - _erasureManager.GetMemoryAt(spawnPos));
		if (_currentRunPhase == GameManager.RunPhase.Crisis)
			multiplier *= _xpCrisisMultiplier;
		return multiplier;
	}

	private float ComputeEnemySpeedMultiplier(EnemyData data, float elapsedMinutes, Vector2 spawnPos = default)
	{
		float multiplier = _enemySpeedBaseMultiplier + _enemySpeedGrowthPerMinute * elapsedMinutes;

		if (data.CombatType == EnemyCombatType.Melee)
			multiplier *= _enemySpeedMeleeBonus;
		else if (data.CombatType == EnemyCombatType.Ranged)
			multiplier *= _enemySpeedRangedBonus;

		if (data.Tier != EnemyTier.Normal)
			multiplier *= _enemySpeedEliteBonus;

		// Enemies in faded zones are faster
		if (spawnPos != default && _erasureManager != null)
			multiplier += _erasureManager.GetEnemySpeedBonus(spawnPos);

		return Mathf.Max(0.5f, multiplier);
	}

#if TOOLS
	public void ForceSpawnEnemy(string enemyId, Vector2 spawnPos)
	{
		DevelopmentMode.RequireTestAccess();
		if (SpawnEventEnemy(enemyId, spawnPos) == null)
			return;
		GD.Print($"[SpawnManager] Debug spawned: {enemyId} at {spawnPos}");
	}
#endif

	private float ComputeEnemyAggressionMultiplier(EnemyData data, float elapsedMinutes)
	{
		float multiplier = _enemyAggressionBaseMultiplier + _enemyAggressionGrowthPerMinute * elapsedMinutes;

		if (data.CombatType == EnemyCombatType.Melee)
			multiplier *= _enemyAggressionMeleeBonus;
		else if (data.CombatType == EnemyCombatType.Ranged)
			multiplier *= _enemyAggressionRangedBonus;

		if (data.Tier != EnemyTier.Normal)
			multiplier *= _enemyAggressionEliteBonus;

		return Mathf.Clamp(multiplier, 0.7f, 3f);
	}

	private int _activeEnemiesCeilingOverride;

	/// <summary>
	/// Plafond de créatures actives : celui de la run, relevé par le Péril, jamais au-delà du plafond de coût fixé au
	/// banc (data/scaling/peril.json, plan 29).
	/// </summary>
	private int GetCurrentMaxEnemies(float elapsedMinutes)
	{
		float scaled = (_maxEnemies + _maxEnemiesGrowthPerMinute * elapsedMinutes)
			* PerilDataLoader.EnemyCountMultiplier(PerilPoints(elapsedMinutes));
		return Mathf.Clamp(Mathf.RoundToInt(scaled), 1, _activeEnemiesCeilingOverride > 0 ? _activeEnemiesCeilingOverride : PerilDataLoader.ActiveEnemiesCeiling);
	}

	/// <summary>
	/// Selectionne un ennemi en fonction du biome a la position donnee.
	/// Exploration : pool ambiant du biome. Crise/Late game : pool hostile.
	/// </summary>
	private string PickEnemyForPosition(Vector2 worldPos)
	{
		bool isResurgence = _currentRunPhase is GameManager.RunPhase.Crisis or GameManager.RunPhase.LateGame or GameManager.RunPhase.Endgame;

		CacheWorldSetup();
		BiomeData biome = _worldSetup?.GetBiomeAt(worldPos);

		IReadOnlyList<string> pool;
		if (biome != null)
		{
			pool = isResurgence ? biome.ResurgenceEnemyPool : biome.ExplorationEnemyPool;
			if (pool == null || pool.Count == 0)
				pool = isResurgence ? _fallbackResurgencePool : _fallbackExplorationPool;
		}
		else
		{
			pool = isResurgence ? _fallbackResurgencePool : _fallbackExplorationPool;
		}
		if (pool.Count == 0)
			return null;

		if (_clusterRemaining > 0 && !string.IsNullOrEmpty(_clusterEnemyId))
		{
			_clusterRemaining--;
			return _clusterEnemyId;
		}

		string picked = PickWeighted(pool, _elapsedTime / 60f);

		if (RunRandom.Spawn.Randf() < _sameTypeClusterChance)
		{
			int clusterSize = RunRandom.Spawn.RandiRange(_sameTypeClusterMin, _sameTypeClusterMax + 1);
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

	/// <summary>
	/// Tirage dans un groupe de biome, pondéré par les fiches (stats <c>spawn_weight</c>, 1 par défaut, et
	/// <c>spawn_from_minute</c>, 0 par défaut). Un identifiant répété dans le groupe compte autant de fois.
	/// </summary>
	private static string PickWeighted(IReadOnlyList<string> pool, float elapsedMinutes)
	{
		float total = 0f;
		foreach (string id in pool)
			total += SpawnWeight(id, elapsedMinutes);
		if (total <= 0f)
		{
			GD.PushWarning($"[SpawnManager] Aucune créature éligible à {elapsedMinutes:F1} min dans un groupe de {pool.Count} : tirage uniforme");
			return pool[(int)(RunRandom.Spawn.Randi() % pool.Count)];
		}

		float roll = RunRandom.Spawn.Randf() * total;
		string picked = null;
		foreach (string id in pool)
		{
			float weight = SpawnWeight(id, elapsedMinutes);
			if (weight <= 0f)
				continue;
			picked = id;
			roll -= weight;
			if (roll < 0f)
				break;
		}
		return picked;
	}

	private static float SpawnWeight(string enemyId, float elapsedMinutes)
	{
		EnemyData data = EnemyDataLoader.Get(enemyId);
		if (data == null || elapsedMinutes < data.GetStat("spawn_from_minute"))
			return 0f;
		return data.GetStat("spawn_weight");
	}

	private Vector2 ApplyClusterSpawnOffset(Vector2 spawnPos, string enemyId)
	{
		if (string.IsNullOrEmpty(_clusterEnemyId) || enemyId != _clusterEnemyId)
			return spawnPos;

		float angle = RunRandom.Spawn.RandfRange(0f, Mathf.Tau);
		float radius = RunRandom.Spawn.RandfRange(_sameTypeClusterSpacingMin, _sameTypeClusterSpacingMax);
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
		// Le joueur n'est résolu qu'au tick : une crise annoncée avant lui ferait échouer toute la vague.
		CachePlayer();
		if (_player == null || !IsInstanceValid(_player))
			return;
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
		foreach (string error in ReadFallbackPool(dict, "fallback_exploration_pool", _fallbackExplorationPool))
			GD.PushError($"[SpawnManager] {error}");
		foreach (string error in ReadFallbackPool(dict, "fallback_resurgence_pool", _fallbackResurgencePool))
			GD.PushError($"[SpawnManager] {error}");
		_baseSpawnInterval = (float)dict["base_spawn_interval"].AsDouble();
		_minSpawnInterval = (float)dict["min_spawn_interval"].AsDouble();
		_spawnIntervalDecay = (float)dict["spawn_interval_decay_per_minute"].AsDouble();
		_hpSegmentFromMinute.Clear();
		_hpSegmentPerMinute.Clear();
		foreach (Variant segment in dict["hp_scaling_segments"].AsGodotArray())
		{
			Godot.Collections.Dictionary entry = segment.AsGodotDictionary();
			_hpSegmentFromMinute.Add((float)entry["from_minute"].AsDouble());
			_hpSegmentPerMinute.Add((float)entry["per_minute"].AsDouble());
		}
		if (_hpSegmentFromMinute.Count == 0)
		{
			GD.PushWarning("[SpawnManager] hp_scaling_segments vide : PV constants après flat_hp_multiplier");
			_hpSegmentFromMinute.Add(0f);
			_hpSegmentPerMinute.Add(1f);
		}
		SortHpSegments();
		_flatHpMultiplier = dict.ContainsKey("flat_hp_multiplier") ? (float)dict["flat_hp_multiplier"].AsDouble() : 1f;
		_dmgScalingPerMinute = (float)dict["damage_scaling_per_minute"].AsDouble();
		_flatDmgMultiplier = dict.ContainsKey("flat_dmg_multiplier") ? (float)dict["flat_dmg_multiplier"].AsDouble() : 1f;
		_rangedDamageGrowthShare = dict.ContainsKey("ranged_damage_growth_share") ? (float)dict["ranged_damage_growth_share"].AsDouble() : 1f;
		_xpGrowthPerMinute = dict.ContainsKey("xp_growth_per_minute") ? (float)dict["xp_growth_per_minute"].AsDouble() : 0f;
		_xpOblivionBonus = dict.ContainsKey("xp_oblivion_bonus") ? (float)dict["xp_oblivion_bonus"].AsDouble() : 0f;
		_xpCrisisMultiplier = dict.ContainsKey("xp_crisis_multiplier") ? (float)dict["xp_crisis_multiplier"].AsDouble() : 1f;
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
		_crisisHpMultiplier = dict.ContainsKey("crisis_hp_multiplier") ? (float)dict["crisis_hp_multiplier"].AsDouble() : _crisisHpMultiplier;
		_crisisDamageMultiplier = dict.ContainsKey("crisis_damage_multiplier") ? (float)dict["crisis_damage_multiplier"].AsDouble() : _crisisDamageMultiplier;
		_lateGameHpMultiplier = dict.ContainsKey("late_game_hp_multiplier") ? (float)dict["late_game_hp_multiplier"].AsDouble() : _lateGameHpMultiplier;
		_lateGameDamageMultiplier = dict.ContainsKey("late_game_damage_multiplier") ? (float)dict["late_game_damage_multiplier"].AsDouble() : _lateGameDamageMultiplier;
		_endgameHpMultiplier = dict.ContainsKey("endgame_hp_multiplier") ? (float)dict["endgame_hp_multiplier"].AsDouble() : _endgameHpMultiplier;
		_endgameDamageMultiplier = dict.ContainsKey("endgame_damage_multiplier") ? (float)dict["endgame_damage_multiplier"].AsDouble() : _endgameDamageMultiplier;
		_timePerilFromMinute = dict.ContainsKey("time_peril_from_minute") ? (float)dict["time_peril_from_minute"].AsDouble() : 0f;
		_timePerilPerMinute = dict.ContainsKey("time_peril_per_minute") ? (float)dict["time_peril_per_minute"].AsDouble() : 0f;
		_positionPicker.MarginMin = dict.ContainsKey("spawn_screen_margin_min") ? (float)dict["spawn_screen_margin_min"].AsDouble() : _positionPicker.MarginMin;
		_positionPicker.MarginMax = dict.ContainsKey("spawn_screen_margin_max") ? (float)dict["spawn_screen_margin_max"].AsDouble() : _positionPicker.MarginMax;
		_positionPicker.ForwardBias = dict.ContainsKey("spawn_forward_bias") ? (float)dict["spawn_forward_bias"].AsDouble() : _positionPicker.ForwardBias;
		_positionPicker.ForwardArcDegrees = dict.ContainsKey("spawn_forward_arc_degrees") ? (float)dict["spawn_forward_arc_degrees"].AsDouble() : _positionPicker.ForwardArcDegrees;

		GD.Print($"[SpawnManager] Config loaded — interval: {_baseSpawnInterval}s, max: {_maxEnemies}, crisis x{_crisisSpawnMultiplier:F2}");
	}

	/// <summary>Groupe de secours : chaque créature doit exister ; renvoie un message par absence (clé ou créature).</summary>
	internal static List<string> ReadFallbackPool(Godot.Collections.Dictionary dict, string key, List<string> pool)
	{
		pool.Clear();
		if (!dict.ContainsKey(key))
			return new List<string> { $"spawn_flow.json : {key} absent, aucun secours hors biome" };
		foreach (Variant entry in dict[key].AsGodotArray())
			pool.Add(entry.AsString());
		return EnemyPools.KeepKnown(pool, $"spawn_flow.json, {key}");
	}

	private void SetDefaults()
	{
		_baseSpawnInterval = 2.0f;
		_minSpawnInterval = 0.3f;
		_spawnIntervalDecay = 0.05f;
		_hpSegmentFromMinute.Clear();
		_hpSegmentPerMinute.Clear();
		_hpSegmentFromMinute.Add(0f);
		_hpSegmentPerMinute.Add(1.06f);
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
				case "xp_growth_per_minute": _xpGrowthPerMinute = kv.Value; break;
				case "xp_oblivion_bonus": _xpOblivionBonus = kv.Value; break;
				case "xp_crisis_multiplier": _xpCrisisMultiplier = kv.Value; break;
				case "damage_scaling_per_minute": _dmgScalingPerMinute = kv.Value; break;
				case "ranged_damage_growth_share": _rangedDamageGrowthShare = kv.Value; break;
				case "max_enemies_on_screen": _maxEnemies = (int)kv.Value; break;
				// Mesure au-delà du plafond de coût (plan 29) : sans elle, le plafond de peril.json borne l'essai.
				case "active_enemies_ceiling": _activeEnemiesCeilingOverride = (int)kv.Value; break;
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
				case "crisis_spawn_multiplier": _crisisSpawnMultiplier = kv.Value; break;
				case "crisis_burst_base": _crisisBurstBase = (int)kv.Value; break;
				case "crisis_burst_per_intensity": _crisisBurstPerIntensity = (int)kv.Value; break;
				case "crisis_hp_multiplier": _crisisHpMultiplier = kv.Value; break;
				case "crisis_damage_multiplier": _crisisDamageMultiplier = kv.Value; break;
				case "time_peril_from_minute": _timePerilFromMinute = kv.Value; break;
				case "time_peril_per_minute": _timePerilPerMinute = kv.Value; break;
				default:
					if (!TryOverrideHpSegment(kv.Key, kv.Value))
						GD.PushWarning($"[SpawnManager] Unknown scaling override '{kv.Key}'");
					break;
			}
		}
		SortHpSegments();
		GD.Print($"[SpawnManager] Applied {overrides.Count} scaling override(s)");
	}

	/// <summary>hp_segment_N_per_minute ou hp_segment_N_from_minute : segment existant de hp_scaling_segments.</summary>
	private bool TryOverrideHpSegment(string key, float value)
	{
		const string prefix = "hp_segment_";
		if (!key.StartsWith(prefix, System.StringComparison.Ordinal))
			return false;
		string[] parts = key[prefix.Length..].Split('_', 2);
		if (parts.Length != 2 || !int.TryParse(parts[0], out int index) || index < 0 || index >= _hpSegmentFromMinute.Count)
			return false;
		switch (parts[1])
		{
			case "per_minute": _hpSegmentPerMinute[index] = value; return true;
			case "from_minute": _hpSegmentFromMinute[index] = value; return true;
			default: return false;
		}
	}

	/// <summary>ComputeScaling parcourt les segments dans l'ordre : un JSON ou une surcharge en désordre est retrié.</summary>
	private void SortHpSegments()
	{
		for (int i = 1; i < _hpSegmentFromMinute.Count; i++)
		{
			for (int j = i; j > 0 && _hpSegmentFromMinute[j] < _hpSegmentFromMinute[j - 1]; j--)
			{
				(_hpSegmentFromMinute[j], _hpSegmentFromMinute[j - 1]) = (_hpSegmentFromMinute[j - 1], _hpSegmentFromMinute[j]);
				(_hpSegmentPerMinute[j], _hpSegmentPerMinute[j - 1]) = (_hpSegmentPerMinute[j - 1], _hpSegmentPerMinute[j]);
			}
		}
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
