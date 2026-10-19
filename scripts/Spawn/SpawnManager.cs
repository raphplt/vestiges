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
	// Croissance des PV et des dégâts par segments (minute de début, facteur par minute), triés par minute de début.
	private readonly List<float> _hpSegmentFromMinute = new();
	private readonly List<float> _hpSegmentPerMinute = new();
	private readonly List<float> _dmgSegmentFromMinute = new();
	private readonly List<float> _dmgSegmentPerMinute = new();
	private float _rangedDamageGrowthShare = 1f;
	private float _xpGrowthPerMinute;
	private float _earlyXpMultiplier = 1f;
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
	private float _earlyDensityMultiplier = 1f;
	private float _earlyDensityUntilMinute;
	private float _earlyDensityRampMinutes;
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
	// Plafond de run relevé pendant une Résurgence (plan 30 T1) : sans lui, la crise bute sur le plafond atteint dès 5 min.
	private float _crisisCapMultiplier = 1f;
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

	private RunClock _clock;
	// Outil de dev : décale l'horloge des tables et courbes de spawn seules, sans toucher à celle de la run. Assigné
	// seulement sous TOOLS : l'initialiseur évite l'avertissement « jamais assigné » du build d'export.
	private float _developmentTimeOffset = 0f;
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
	// Niveau du joueur, suivi par l'EventBus : les variantes calées sur lui (Souverains) le lisent à leur apparition.
	private int _playerLevel = 1;
	private WorldSetup _worldSetup;
	private Node _enemyContainer;
	private EventBus _eventBus;
	private ErasureManager _erasureManager;
	private CrisisManager _crisisManager;

	// Groupes de secours (spawn_flow.json) quand la position n'a pas de biome ou que son groupe est vide.
	private readonly List<string> _fallbackExplorationPool = new();
	private readonly List<string> _fallbackResurgencePool = new();

	/// <summary>Niveau de difficulté de la run : nombre, PV et dégâts des créatures, et des boss qui le lisent ici.</summary>
	public DifficultyLevel Difficulty { get; private set; } = DifficultyDataLoader.Get(null);

	public override void _Ready()
	{
		EnemyDataLoader.Load();
		BiomeDataLoader.Load();
		EnemyVariantDataLoader.Load();
		LoadScalingConfig();
		Difficulty = DifficultyDataLoader.Get(GetNode<GameManager>("/root/GameManager").DifficultyId);
		_nextEliteAtSec = EnemyVariantDataLoader.NaturalElites.StartSec;

		_clock = GetNode<RunClock>("../RunClock");
		_pool = GetNode<EnemyPool>("../EnemyPool");
		_enemyContainer = GetNode("../EnemyContainer");

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.RunPhaseChanged += OnRunPhaseChanged;
		_eventBus.CrisisStarted += OnCrisisStarted;
		_eventBus.PerilChanged += OnPerilChanged;
		_eventBus.OubliEffectChanged += OnOubliEffectChanged;
		_eventBus.LevelUp += OnLevelUp;
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
			_eventBus.LevelUp -= OnLevelUp;
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
					_nextEliteAtSec = ElapsedSeconds;
				break;
			case "affix_chance":
				_affixChanceBonus = total;
				break;
			case "enemy_detection":
				EnemyTracking.SetDetectionScale(1f + total);
				break;
		}
	}

	private void OnLevelUp(int newLevel) => _playerLevel = Mathf.Max(_playerLevel, newLevel);

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
		_cullTimer += dt;

		if (_cullTimer >= 1f)
		{
			_cullTimer = 0f;
			CullFarDayEnemies();
		}

		// Répit d'ouverture : rien n'apparaît, et le minuteur n'accumule pas de rafale à rattraper.
		if (ElapsedSeconds < _openingGraceSec)
			return;

		float elapsedMinutes = ElapsedSeconds / 60f;
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

		if (ElapsedSeconds >= _nextEliteAtSec)
			TrySpawnNaturalElite(elapsedMinutes);
	}

	// =========================================================
	// Scaling
	// =========================================================

	private void ComputeScaling(EnemyData data, float elapsedMinutes, out float hpScale, out float dmgScale)
	{
		// Une pente par segment, quelle que soit la phase : la puissance du joueur décolle par à-coups (plan 21 H3).
		hpScale = _flatHpMultiplier * SegmentedGrowth(_hpSegmentFromMinute, _hpSegmentPerMinute, elapsedMinutes);
		// Les tirs et les zones touchent de loin, en nombre, et leurs créatures meurent peu : leurs dégâts ne suivent
		// qu'une part de la croissance commune (plan 20 §7.1).
		float damageGrowth = SegmentedGrowth(_dmgSegmentFromMinute, _dmgSegmentPerMinute, elapsedMinutes);
		if (data.CombatType == EnemyCombatType.Ranged)
			damageGrowth = Mathf.Pow(damageGrowth, _rangedDamageGrowthShare);
		dmgScale = damageGrowth * _flatDmgMultiplier;

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
		hpScale *= PerilDataLoader.EnemyHpMultiplier(peril) * Difficulty.EnemyHp;
		dmgScale *= PerilDataLoader.EnemyDamageMultiplier(peril) * Difficulty.EnemyDamage;
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
	public void MakeVariant(Enemy enemy, string variantId, int extraAffixes = 0)
	{
		EnemyVariantData variant = EnemyVariantDataLoader.GetVariant(variantId);
		if (variant == null)
			return;

		_affixScratch.Clear();
		_affixScratch.AddRange(variant.AffixPool.Count > 0 ? variant.AffixPool : EnemyVariantDataLoader.NaturalElites.AffixPool);
		_affixPick.Clear();
		for (int i = 0; i < variant.AffixCount + extraAffixes && _affixScratch.Count > 0; i++)
		{
			int index = (int)(RunRandom.Spawn.Randi() % _affixScratch.Count);
			EnemyAffixData affix = EnemyVariantDataLoader.GetAffix(_affixScratch[index]);
			_affixScratch.RemoveAt(index);
			if (affix != null)
				_affixPick.Add(affix);
		}
		VariantLevelScaling scaling = variant.LevelScaling;
		if (scaling == null)
		{
			enemy.ApplyVariant(variant, _affixPick);
			return;
		}
		float peril = PerilPoints(ElapsedSeconds / 60f);
		enemy.ApplyVariant(variant, _affixPick, scaling.Hp(_playerLevel) * PerilDataLoader.EnemyHpMultiplier(peril) * Difficulty.EnemyHp,
			scaling.DamageScale(_playerLevel) * PerilDataLoader.EnemyDamageMultiplier(peril) * Difficulty.EnemyDamage);
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
			string id = PickWeighted(pool, ElapsedSeconds / 60f);
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
		Enemy enemy = SpawnAt(data, spawnPos, ElapsedSeconds / 60f);
		if (!string.IsNullOrEmpty(variantId))
			MakeVariant(enemy, variantId);
		return enemy;
	}

	public bool IsSpawnablePosition(Vector2 position) => !IsWaterAt(position);

	public Vector2 ViewHalfExtents => _player != null ? GetViewHalfExtents() : FallbackViewHalfExtents;

	/// <summary>Temps de la run qui règle les tables et courbes de spawn (horloge de la run, plus le décalage de dev).</summary>
	public float ElapsedSeconds => _clock.Seconds + _developmentTimeOffset;

	/// <summary>Part de la foule locale visée pendant un combat de boss (la Barrière la réduit), 1 hors combat.</summary>
	public float EncounterDensityMultiplier { get; set; } = 1f;

	private void TrySpawnNaturalElite(float elapsedMinutes)
	{
		NaturalEliteConfig config = EnemyVariantDataLoader.NaturalElites;
		// Fin de run (plan 47) : élites plus fréquentes, plus nombreuses et plus fortes, progressivement.
		float ramp = config.LateRamp(elapsedMinutes);
		_nextEliteAtSec = ElapsedSeconds + RunRandom.Spawn.RandfRange(Mathf.Lerp(config.IntervalMinSec, config.LateIntervalMinSec, ramp),
			Mathf.Lerp(config.IntervalMaxSec, config.LateIntervalMaxSec, ramp)) / Difficulty.EliteFrequency;
		int maxAlive = Mathf.RoundToInt(Mathf.Lerp(config.MaxAlive, config.LateMaxAlive, ramp));

		for (int i = _naturalElites.Count - 1; i >= 0; i--)
		{
			Enemy tracked = _naturalElites[i];
			if (!IsInstanceValid(tracked) || !tracked.IsActive || tracked.IsDying || tracked.Modifiers.Variant?.Id != "elite")
				_naturalElites.RemoveAt(i);
		}
		if (_naturalElites.Count >= maxAlive + _extraElites || _currentRunPhase == GameManager.RunPhase.Death)
			return;

		Vector2 spawnPos = GetSpawnPosition();
		string enemyId = PickLocalEnemyId(spawnPos);
		EnemyData data = EnemyDataLoader.Get(enemyId);
		if (data == null)
			return;
		Enemy enemy = SpawnAt(data, spawnPos, elapsedMinutes);
		MakeVariant(enemy, "elite", ramp >= config.LateExtraAffixAt ? 1 : 0);
		if (ramp > 0f)
			enemy.Empower(Mathf.Lerp(1f, config.LateHpMult, ramp), Mathf.Lerp(1f, config.LateDamageMult, ramp));
		_naturalElites.Add(enemy);
		GD.Print($"[SpawnManager] Élite : {enemy.DisplayName} à {ElapsedSeconds:F0} s");
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
		// L'ouverture peut partir au-dessus de la courbe normale : l'écran se remplit dès le départ (LE-JEU, étape 3).
		float openingTarget = Mathf.Lerp(_openingLocalTargetStart, fullTarget, OpeningProgress);
		int target = Mathf.RoundToInt(openingTarget * EarlyDensity(elapsedMinutes) * PerilDataLoader.EnemyCountMultiplier(PerilPoints(elapsedMinutes)) * zoneMemoryMult);
		target = Mathf.RoundToInt(target * phaseMult * EncounterDensityMultiplier * Difficulty.EnemyCount);

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
		return Mathf.Max(baseInterval * phaseFactor * openingFactor / EarlyDensity(elapsedMinutes), _minSpawnInterval);
	}

	/// <summary>Densité du début de run (DECISIONS §171) : réduite jusqu'à une minute donnée, puis rendue progressivement.</summary>
	private float EarlyDensity(float elapsedMinutes) => Mathf.Lerp(1f, _earlyDensityMultiplier, EarlyWeight(elapsedMinutes));

	/// <summary>1 pendant le début de run, puis descend à 0 sur la rampe de retour au réglage normal.</summary>
	private float EarlyWeight(float elapsedMinutes)
	{
		if (elapsedMinutes <= _earlyDensityUntilMinute)
			return 1f;
		if (_earlyDensityRampMinutes <= 0f)
			return 0f;
		return 1f - Mathf.Clamp((elapsedMinutes - _earlyDensityUntilMinute) / _earlyDensityRampMinutes, 0f, 1f);
	}

	/// <summary>0 à la fin du répit, 1 une fois la montée d'ouverture terminée.</summary>
	private float OpeningProgress => _openingRampSec <= 0f
		? 1f
		: Mathf.Clamp((ElapsedSeconds - _openingGraceSec) / _openingRampSec, 0f, 1f);

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
		float multiplier = (1f + _xpGrowthPerMinute * elapsedMinutes) * Mathf.Lerp(1f, _earlyXpMultiplier, EarlyWeight(elapsedMinutes));
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
	/// <summary>Change uniquement l'horloge des tables et courbes de spawn ; aucun événement passé n'est rejoué.</summary>
	internal void SetDifficultyTimeForDevelopment(float seconds)
	{
		DevelopmentMode.RequireTestAccess();
		if (!float.IsFinite(seconds) || seconds < 0f || seconds > 86400f)
			throw new System.ArgumentOutOfRangeException(nameof(seconds));
		_developmentTimeOffset = seconds - _clock.Seconds;
		_spawnTimer = 0f;
		_localDensityTimer = 0f;
		_nextEliteAtSec = seconds + EnemyVariantDataLoader.NaturalElites.IntervalMinSec;
	}

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
	/// Plafond de créatures actives : celui de la run, relevé par le Péril et pendant une Résurgence, jamais au-delà du
	/// plafond de coût fixé au banc (data/scaling/peril.json, plan 29). À la fin de la crise il redescend sans retirer
	/// personne : la foule en trop s'use d'elle-même (DECISIONS §84).
	/// </summary>
	private int GetCurrentMaxEnemies(float elapsedMinutes)
	{
		float crisisCap = _currentRunPhase == GameManager.RunPhase.Crisis ? _crisisCapMultiplier : 1f;
		float scaled = (_maxEnemies + _maxEnemiesGrowthPerMinute * elapsedMinutes)
			* PerilDataLoader.EnemyCountMultiplier(PerilPoints(elapsedMinutes)) * crisisCap * Difficulty.EnemyCount;
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

		string picked = PickWeighted(pool, ElapsedSeconds / 60f);

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

		Vector2 position = _player.GlobalPosition;
		for (int attempt = 0; attempt < 15; attempt++)
		{
			position = _positionPicker.Pick(_player.GlobalPosition, halfExtents, _player.Velocity, marginScale);
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
		float elapsedMinutes = ElapsedSeconds / 60f;
		int burstCount = _crisisBurstBase + _crisisBurstPerIntensity * Mathf.Max(0, intensity - 1);
		for (int i = 0; i < burstCount; i++)
			TrySpawnEnemy(elapsedMinutes);
	}

	// =========================================================
	// Chargement config
	// =========================================================

	/// <summary>Réglages contrôlés au chargement de la run (SpawnFlowConfig, plan 26 Q7e-1) ; un champ absent garde son secours.</summary>
	private void LoadScalingConfig()
	{
		SpawnFlowConfig config = SpawnFlowConfig.Load();
		_fallbackExplorationPool.Clear();
		_fallbackExplorationPool.AddRange(config.FallbackExplorationPool);
		_fallbackResurgencePool.Clear();
		_fallbackResurgencePool.AddRange(config.FallbackResurgencePool);
		CopySegments(config.HpSegments, _hpSegmentFromMinute, _hpSegmentPerMinute);
		CopySegments(config.DamageSegments, _dmgSegmentFromMinute, _dmgSegmentPerMinute);
		_baseSpawnInterval = config.Number("base_spawn_interval");
		_minSpawnInterval = config.Number("min_spawn_interval");
		_spawnIntervalDecay = config.Number("spawn_interval_decay_per_minute");
		_maxEnemies = config.Integer("max_enemies_on_screen");
		_flatHpMultiplier = config.Number("flat_hp_multiplier");
		_flatDmgMultiplier = config.Number("flat_dmg_multiplier");
		_rangedDamageGrowthShare = config.Number("ranged_damage_growth_share");
		_xpGrowthPerMinute = config.Number("xp_growth_per_minute");
		_xpOblivionBonus = config.Number("xp_oblivion_bonus");
		_xpCrisisMultiplier = config.Number("xp_crisis_multiplier");
		_maxEnemiesGrowthPerMinute = config.Number("max_enemies_growth_per_minute");
		_enemySpeedBaseMultiplier = config.Number("enemy_speed_base_multiplier");
		_enemySpeedGrowthPerMinute = config.Number("enemy_speed_growth_per_minute");
		_enemySpeedMeleeBonus = config.Number("enemy_speed_melee_bonus");
		_enemySpeedRangedBonus = config.Number("enemy_speed_ranged_bonus");
		_enemySpeedEliteBonus = config.Number("enemy_speed_elite_bonus");
		_enemyAggressionBaseMultiplier = config.Number("enemy_aggression_base_multiplier");
		_enemyAggressionGrowthPerMinute = config.Number("enemy_aggression_growth_per_minute");
		_enemyAggressionMeleeBonus = config.Number("enemy_aggression_melee_bonus");
		_enemyAggressionRangedBonus = config.Number("enemy_aggression_ranged_bonus");
		_enemyAggressionEliteBonus = config.Number("enemy_aggression_elite_bonus");
		_dayEnemyDespawnDistance = config.Number("day_enemy_despawn_distance");
		_dayLocalEnemyRadius = config.Number("local_enemy_radius");
		_dayLocalEnemyTargetBase = config.Number("local_enemy_target_base");
		_openingGraceSec = config.Number("opening_grace_seconds");
		_openingRampSec = config.Number("opening_ramp_seconds");
		_openingLocalTargetStart = config.Number("opening_local_target_start");
		_openingIntervalMultiplierStart = config.Number("opening_interval_multiplier_start");
		_earlyDensityMultiplier = config.Number("early_density_multiplier");
		_earlyDensityUntilMinute = config.Number("early_density_until_minute");
		_earlyDensityRampMinutes = config.Number("early_density_ramp_minutes");
		_earlyXpMultiplier = config.Number("early_xp_multiplier");
		_dayLocalEnemyTargetGrowthPerMinute = config.Number("local_enemy_target_growth_per_minute");
		_dayLocalSpawnBurstMax = config.Integer("local_spawn_burst_max");
		_sameTypeClusterChance = config.Number("same_type_cluster_chance");
		_sameTypeClusterMin = config.Integer("same_type_cluster_min");
		_sameTypeClusterMax = config.Integer("same_type_cluster_max");
		_sameTypeClusterSpacingMin = config.Number("same_type_cluster_spacing_min");
		_sameTypeClusterSpacingMax = config.Number("same_type_cluster_spacing_max");
		_crisisSpawnMultiplier = config.Number("crisis_spawn_multiplier");
		_crisisCapMultiplier = config.Number("crisis_cap_multiplier");
		_crisisBurstBase = config.Integer("crisis_burst_base");
		_crisisBurstPerIntensity = config.Integer("crisis_burst_per_intensity");
		_lateGameSpawnMultiplier = config.Number("late_game_spawn_multiplier");
		_endgameSpawnMultiplier = config.Number("endgame_spawn_multiplier");
		_crisisHpMultiplier = config.Number("crisis_hp_multiplier");
		_crisisDamageMultiplier = config.Number("crisis_damage_multiplier");
		_lateGameHpMultiplier = config.Number("late_game_hp_multiplier");
		_lateGameDamageMultiplier = config.Number("late_game_damage_multiplier");
		_endgameHpMultiplier = config.Number("endgame_hp_multiplier");
		_endgameDamageMultiplier = config.Number("endgame_damage_multiplier");
		_timePerilFromMinute = config.Number("time_peril_from_minute");
		_timePerilPerMinute = config.Number("time_peril_per_minute");
		_positionPicker.MarginMin = config.Number("spawn_screen_margin_min");
		_positionPicker.MarginMax = config.Number("spawn_screen_margin_max");
		_positionPicker.ForwardBias = config.Number("spawn_forward_bias");
		_positionPicker.ForwardArcDegrees = config.Number("spawn_forward_arc_degrees");
		_positionPicker.LeadSeconds = config.Number("spawn_speed_lead_seconds");
		_positionPicker.MinDistance = config.Number("spawn_min_distance_px");

		GD.Print($"[SpawnManager] Config loaded — interval: {_baseSpawnInterval}s, max: {_maxEnemies}, crisis x{_crisisSpawnMultiplier:F2}");
	}

	private static void CopySegments(IReadOnlyList<(float FromMinute, float PerMinute)> segments, List<float> fromMinute, List<float> perMinute)
	{
		fromMinute.Clear();
		perMinute.Clear();
		foreach ((float from, float rate) in segments)
		{
			fromMinute.Add(from);
			perMinute.Add(rate);
		}
		SortSegments(fromMinute, perMinute);
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
				case "crisis_cap_multiplier": _crisisCapMultiplier = kv.Value; break;
				case "crisis_burst_base": _crisisBurstBase = (int)kv.Value; break;
				case "crisis_burst_per_intensity": _crisisBurstPerIntensity = (int)kv.Value; break;
				case "crisis_hp_multiplier": _crisisHpMultiplier = kv.Value; break;
				case "crisis_damage_multiplier": _crisisDamageMultiplier = kv.Value; break;
				case "time_peril_from_minute": _timePerilFromMinute = kv.Value; break;
				case "time_peril_per_minute": _timePerilPerMinute = kv.Value; break;
				default:
					if (!TryOverrideSegment(kv.Key, kv.Value))
						GD.PushWarning($"[SpawnManager] Unknown scaling override '{kv.Key}'");
					break;
			}
		}
		SortSegments(_hpSegmentFromMinute, _hpSegmentPerMinute);
		SortSegments(_dmgSegmentFromMinute, _dmgSegmentPerMinute);
		GD.Print($"[SpawnManager] Applied {overrides.Count} scaling override(s)");
	}

	/// <summary>
	/// hp_segment_N_per_minute, hp_segment_N_from_minute (segment existant de hp_scaling_segments) ou leurs pendants
	/// damage_segment_N_… pour damage_scaling_segments.
	/// </summary>
	private bool TryOverrideSegment(string key, float value) =>
		TryOverrideSegment(key, value, "hp_segment_", _hpSegmentFromMinute, _hpSegmentPerMinute)
		|| TryOverrideSegment(key, value, "damage_segment_", _dmgSegmentFromMinute, _dmgSegmentPerMinute);

	private static bool TryOverrideSegment(string key, float value, string prefix, List<float> fromMinute, List<float> perMinute)
	{
		if (!key.StartsWith(prefix, System.StringComparison.Ordinal))
			return false;
		string[] parts = key[prefix.Length..].Split('_', 2);
		if (parts.Length != 2 || !int.TryParse(parts[0], out int index) || index < 0 || index >= fromMinute.Count)
			return false;
		switch (parts[1])
		{
			case "per_minute": perMinute[index] = value; return true;
			case "from_minute": fromMinute[index] = value; return true;
			default: return false;
		}
	}

	/// <summary>Produit des pentes de chaque segment sur les minutes passées dedans.</summary>
	private static float SegmentedGrowth(List<float> fromMinute, List<float> perMinute, float elapsedMinutes)
	{
		float growth = 1f;
		for (int i = 0; i < fromMinute.Count; i++)
		{
			float end = i + 1 < fromMinute.Count ? fromMinute[i + 1] : float.MaxValue;
			float minutes = Mathf.Min(elapsedMinutes, end) - fromMinute[i];
			if (minutes <= 0f)
				break;
			growth *= Mathf.Pow(perMinute[i], minutes);
		}
		return growth;
	}

	/// <summary>ComputeScaling parcourt les segments dans l'ordre : un JSON ou une surcharge en désordre est retrié.</summary>
	private static void SortSegments(List<float> fromMinute, List<float> perMinute)
	{
		for (int i = 1; i < fromMinute.Count; i++)
		{
			for (int j = i; j > 0 && fromMinute[j] < fromMinute[j - 1]; j--)
			{
				(fromMinute[j], fromMinute[j - 1]) = (fromMinute[j - 1], fromMinute[j]);
				(perMinute[j], perMinute[j - 1]) = (perMinute[j - 1], perMinute[j]);
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
