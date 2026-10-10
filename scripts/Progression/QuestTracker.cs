using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Progression;

/// <summary>
/// Suit en run les conditions des quêtes de déblocage pas encore accomplies (plan 06 §9, lot Q2). Rien ne s'affiche
/// pendant la run : une quête remplie s'enregistre aussitôt (<see cref="MetaSaveManager.ClaimQuest"/>) et le signal
/// <c>QuestCompleted</c> part pour la notification et le bilan. L'avancée des autres est retenue à la mort et à la
/// sortie de la run. Un seul nœud : chaque élimination parcourt une liste fixe de conditions, sans allocation.
/// </summary>
public partial class QuestTracker : Node
{
    /// <summary>Vitesse sous laquelle le joueur est immobile (px/s), la même que pour le Tabouret de camping.</summary>
    private const float StillSpeed = 5f;
    private const float PollInterval = 0.5f;
    private const int BurstCapacity = 128;

    private enum Fact
    {
        SovereignKills, SovereignCritKills, BarrierDefeated, IndicibleDefeated, CrisesAfterIndicible, MemorialsRevived,
        CrisesSurvived, DistanceMeters, WaymarksFound, ErasedZoneKills, MeleeKills, RangedKills, WeaponKills,
        WeaponsAtLevel, EventsSucceeded, ZonesDiscovered, ChestsOpened, Level, SlowedKills, BurningKills, CritKills,
        EliteCritKills, OublisThroughCrisis, DamageTaken, HpHealed, CloseKills, StillKills, KillBurst, LowHpKills,
        HealthyTime, CleanCrises, EssenceHeld, Peril, Ascensions,
    }

    private static readonly Dictionary<string, Fact> Facts = new()
    {
        [QuestStat.SovereignKills] = Fact.SovereignKills, [QuestStat.SovereignCritKills] = Fact.SovereignCritKills,
        [QuestStat.BarrierDefeated] = Fact.BarrierDefeated, [QuestStat.IndicibleDefeated] = Fact.IndicibleDefeated,
        [QuestStat.CrisesAfterIndicible] = Fact.CrisesAfterIndicible, [QuestStat.MemorialsRevived] = Fact.MemorialsRevived,
        [QuestStat.CrisesSurvived] = Fact.CrisesSurvived, [QuestStat.DistanceMeters] = Fact.DistanceMeters,
        [QuestStat.WaymarksFound] = Fact.WaymarksFound, [QuestStat.ErasedZoneKills] = Fact.ErasedZoneKills,
        [QuestStat.MeleeKills] = Fact.MeleeKills, [QuestStat.RangedKills] = Fact.RangedKills,
        [QuestStat.WeaponKills] = Fact.WeaponKills, [QuestStat.WeaponsAtLevel] = Fact.WeaponsAtLevel,
        [QuestStat.EventsSucceeded] = Fact.EventsSucceeded, [QuestStat.ZonesDiscovered] = Fact.ZonesDiscovered,
        [QuestStat.ChestsOpened] = Fact.ChestsOpened, [QuestStat.Level] = Fact.Level,
        [QuestStat.SlowedKills] = Fact.SlowedKills, [QuestStat.BurningKills] = Fact.BurningKills,
        [QuestStat.CritKills] = Fact.CritKills, [QuestStat.EliteCritKills] = Fact.EliteCritKills,
        [QuestStat.OublisThroughCrisis] = Fact.OublisThroughCrisis, [QuestStat.DamageTaken] = Fact.DamageTaken,
        [QuestStat.HpHealed] = Fact.HpHealed, [QuestStat.CloseKills] = Fact.CloseKills,
        [QuestStat.StillKills] = Fact.StillKills, [QuestStat.KillBurst] = Fact.KillBurst,
        [QuestStat.LowHpKills] = Fact.LowHpKills, [QuestStat.HealthyTime] = Fact.HealthyTime,
        [QuestStat.CleanCrises] = Fact.CleanCrises, [QuestStat.EssenceHeld] = Fact.EssenceHeld,
        [QuestStat.Peril] = Fact.Peril, [QuestStat.Ascensions] = Fact.Ascensions,
    };

    /// <summary>Une condition suivie : sa valeur de la run, et pour un cumul la part déjà retenue.</summary>
    private sealed class Watch
    {
        public Tracked Owner;
        public QuestCondition Condition;
        public Fact Fact;
        public float Value;
        public float Stored;
        public float Flushed;
        /// <summary>« Bien au chaud » : la run est passée sous le seuil, la valeur ne bouge plus.</summary>
        public bool Broken;
    }

    private sealed class Tracked
    {
        public QuestDefinition Quest;
        public Watch[] Watches;
        public bool Done;
    }

    private readonly List<Tracked> _quests = new();
    private readonly List<Watch> _killWatches = new();
    private readonly Dictionary<Fact, List<Watch>> _byFact = new();
    private readonly float[] _killTimes = new float[BurstCapacity];
    private int _killCursor;
    private int _killCount;

    private EventBus _eventBus;
    private GroupCache _groups;
    private Player _player;
    private RunTracker _runTracker;
    private ErasureManager _erasure;
    private float _runTime;
    private float _pollTimer;
    private bool _ended;
    private bool _crisisActive;
    private float _crisisHpLost;
    private int _crisisMinOublis;
    private bool _indicibleDown;
    private bool _crisisAfterIndicible;
    /// <summary>Une condition lit la phase d'Effacement au point de mort : sans elle, rien n'est cherché par élimination.</summary>
    private bool _needsErasure;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _runTracker = GetParent()?.GetNodeOrNull<RunTracker>("RunTracker");
        _erasure = GetParent()?.GetNodeOrNull<ErasureManager>("ErasureManager");
        BuildWatches();

        _eventBus.EnemyKillResolved += OnEnemyKill;
        _eventBus.PlayerDamageResolved += OnPlayerDamage;
        _eventBus.PlayerHealingResolved += OnPlayerHealing;
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.CrisisStarted += OnCrisisStarted;
        _eventBus.CrisisEnded += OnCrisisEnded;
        _eventBus.OubliEffectChanged += OnOubliChanged;
        _eventBus.MemorialAwakened += OnMemorialAwakened;
        _eventBus.WaymarkFound += OnWaymarkFound;
        _eventBus.RunEventEnded += OnRunEventEnded;
        _eventBus.ZoneDiscovered += OnZoneDiscovered;
        _eventBus.ChestOpened += OnChestOpened;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.EssenceChanged += OnEssenceChanged;
        _eventBus.PerilChanged += OnPerilChanged;
        _eventBus.WeaponUpgraded += OnWeaponUpgraded;
        _eventBus.WeaponInventoryChanged += OnWeaponInventoryChanged;
        _eventBus.GameStateChanged += OnGameStateChanged;
        GD.Print($"[QuestTracker] {_quests.Count} quêtes suivies");
    }

    public override void _ExitTree()
    {
        Flush();
        if (_eventBus == null)
            return;
        _eventBus.EnemyKillResolved -= OnEnemyKill;
        _eventBus.PlayerDamageResolved -= OnPlayerDamage;
        _eventBus.PlayerHealingResolved -= OnPlayerHealing;
        _eventBus.EnemyKilled -= OnEnemyKilled;
        _eventBus.CrisisStarted -= OnCrisisStarted;
        _eventBus.CrisisEnded -= OnCrisisEnded;
        _eventBus.OubliEffectChanged -= OnOubliChanged;
        _eventBus.MemorialAwakened -= OnMemorialAwakened;
        _eventBus.WaymarkFound -= OnWaymarkFound;
        _eventBus.RunEventEnded -= OnRunEventEnded;
        _eventBus.ZoneDiscovered -= OnZoneDiscovered;
        _eventBus.ChestOpened -= OnChestOpened;
        _eventBus.LevelUp -= OnLevelUp;
        _eventBus.EssenceChanged -= OnEssenceChanged;
        _eventBus.PerilChanged -= OnPerilChanged;
        _eventBus.WeaponUpgraded -= OnWeaponUpgraded;
        _eventBus.WeaponInventoryChanged -= OnWeaponInventoryChanged;
        _eventBus.GameStateChanged -= OnGameStateChanged;
    }

    /// <summary>Horloge de jeu de la run, pauses exclues : délais (« avant la 12ᵉ minute ») et fenêtres d'éliminations.</summary>
    public override void _Process(double delta)
    {
        if (_ended)
            return;
        _runTime += (float)delta;
        _pollTimer += (float)delta;
        if (_pollTimer < PollInterval)
            return;
        _pollTimer = 0f;
        if (_runTracker != null)
            Raise(Fact.DistanceMeters, _runTracker.Journey.TravelledMeters);
        UpdateHealthyTime();
    }

    private void BuildWatches()
    {
        foreach (QuestDefinition quest in QuestDataLoader.GetAll())
        {
            if (MetaSaveManager.HasCompletedQuest(quest.Id))
                continue;
            IReadOnlyList<float> stored = MetaSaveManager.GetQuestProgress(quest.Id);
            Tracked tracked = new() { Quest = quest, Watches = new Watch[quest.Conditions.Count] };
            for (int i = 0; i < quest.Conditions.Count; i++)
            {
                QuestCondition condition = quest.Conditions[i];
                Watch watch = new()
                {
                    Owner = tracked,
                    Condition = condition,
                    Fact = Facts[condition.Stat],
                    Stored = quest.Scope == QuestScope.Cumulative && i < stored.Count ? stored[i] : 0f,
                };
                tracked.Watches[i] = watch;
                if (!_byFact.TryGetValue(watch.Fact, out List<Watch> list))
                    _byFact[watch.Fact] = list = new List<Watch>();
                list.Add(watch);
                if (IsKillFact(watch.Fact))
                    _killWatches.Add(watch);
                _needsErasure |= watch.Fact == Fact.ErasedZoneKills;
            }
            _quests.Add(tracked);
        }
    }

    private static bool IsKillFact(Fact fact) => fact is Fact.SovereignKills or Fact.SovereignCritKills
        or Fact.ErasedZoneKills or Fact.MeleeKills or Fact.RangedKills or Fact.WeaponKills or Fact.SlowedKills
        or Fact.BurningKills or Fact.CritKills or Fact.EliteCritKills or Fact.CloseKills or Fact.StillKills
        or Fact.KillBurst or Fact.LowHpKills;

    // ==============================
    // Faits observés
    // ==============================

    private void OnEnemyKill(EnemyKillResult kill)
    {
        // Une partie de boss n'est pas une élimination : le boss entier compte une fois, par EnemyKilled.
        if (_ended || _killWatches.Count == 0 || kill.EnemyId == EnemyGrammar.BossPartId)
            return;
        Player player = CachePlayer();
        float hpRatio = player != null ? player.CurrentHp / Mathf.Max(1f, player.EffectiveMaxHp) : 1f;
        bool still = player != null && player.Velocity.LengthSquared() < StillSpeed * StillSpeed;
        float distanceSq = player != null ? Iso.GroundDistanceSquared(kill.Position, player.GlobalPosition) : float.MaxValue;
        bool critical = kill.Damage.Critical;
        WeaponInstance weapon = kill.Damage.Source.IsWeaponWork ? kill.Damage.Source.Weapon : null;
        bool erased = _needsErasure && _erasure != null
            && _erasure.GetZonePhaseAt(kill.Position) >= ErasureManager.ErasureZonePhase.Frayed;
        _killTimes[_killCursor] = _runTime;
        _killCursor = (_killCursor + 1) % BurstCapacity;
        _killCount = Mathf.Min(_killCount + 1, BurstCapacity);

        for (int i = 0; i < _killWatches.Count; i++)
        {
            Watch watch = _killWatches[i];
            if (watch.Owner.Done)
                continue;
            QuestCondition condition = watch.Condition;
            switch (watch.Fact)
            {
                case Fact.KillBurst:
                    Update(watch, KillsWithin(condition.Param), false);
                    continue;
                case Fact.SovereignKills when kill.Sovereign:
                case Fact.SovereignCritKills when kill.Sovereign && critical:
                case Fact.ErasedZoneKills when erased:
                case Fact.MeleeKills when weapon?.Base.Category == WeaponCategory.Melee:
                case Fact.RangedKills when weapon?.Base.Category == WeaponCategory.Ranged:
                case Fact.WeaponKills when weapon?.Id == condition.Weapon:
                case Fact.SlowedKills when kill.Slow.Remaining > 0f:
                case Fact.BurningKills when kill.Burn.Remaining > 0f && kill.Burn.Strength > 0f:
                case Fact.CritKills when critical:
                case Fact.EliteCritKills when kill.Elite && critical:
                case Fact.CloseKills when distanceSq <= condition.Param * condition.Param:
                case Fact.StillKills when still:
                case Fact.LowHpKills when hpRatio < condition.Param:
                    Update(watch, 1f, true);
                    continue;
            }
        }
    }

    /// <summary>Éliminations tombées dans les <paramref name="window"/> dernières secondes, la dernière comprise.</summary>
    private int KillsWithin(float window)
    {
        int count = 0;
        float since = _runTime - window;
        for (int i = 1; i <= _killCount; i++)
        {
            if (_killTimes[(_killCursor - i + BurstCapacity) % BurstCapacity] < since)
                break;
            count++;
        }
        return count;
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        if (enemyId == EnemyGrammar.BossPartId)
            Raise(Fact.BarrierDefeated, 1f);
        else if (enemyId == EnemyGrammar.FinalBossId)
        {
            _indicibleDown = true;
            Raise(Fact.IndicibleDefeated, 1f);
        }
    }

    private void OnPlayerDamage(PlayerDamageResult result)
    {
        if (!result.Applied || result.HpLost <= 0f)
            return;
        Add(Fact.DamageTaken, result.HpLost);
        if (_crisisActive)
            _crisisHpLost += result.HpLost;
        UpdateHealthyTime();
    }

    private void OnPlayerHealing(HealingResult result)
    {
        if (result.Applied && result.HpRestored > 0f)
            Add(Fact.HpHealed, result.HpRestored);
    }

    private void OnCrisisStarted(int crisisNumber, int intensity)
    {
        _crisisActive = true;
        // « Après la chute de l'Indicible » : la Résurgence doit avoir commencé une fois l'Indicible tombé.
        _crisisAfterIndicible = _indicibleDown;
        _crisisHpLost = 0f;
        _crisisMinOublis = OublisHeld();
    }

    private void OnOubliChanged(string effect, float total)
    {
        if (_crisisActive)
            _crisisMinOublis = Mathf.Min(_crisisMinOublis, OublisHeld());
    }

    private void OnCrisisEnded(int crisisNumber)
    {
        Player player = CachePlayer();
        bool traversed = _crisisActive && player is { IsDead: false };
        _crisisActive = false;
        if (!traversed)
            return;
        Add(Fact.CrisesSurvived, 1f);
        if (_crisisAfterIndicible)
            Add(Fact.CrisesAfterIndicible, 1f);
        Raise(Fact.OublisThroughCrisis, _crisisMinOublis);
        if (!_byFact.TryGetValue(Fact.CleanCrises, out List<Watch> clean))
            return;
        foreach (Watch watch in clean)
        {
            if (_crisisHpLost < watch.Condition.Param * player.EffectiveMaxHp)
                Update(watch, 1f, true);
        }
    }

    private void OnMemorialAwakened(Vector2 position) => Add(Fact.MemorialsRevived, 1f);
    private void OnWaymarkFound(string placeType) => Add(Fact.WaymarksFound, 1f);
    private void OnZoneDiscovered(int cellX, int cellY, int cellCount) => Add(Fact.ZonesDiscovered, cellCount);
    private void OnChestOpened(string chestId, string rarity, Vector2 position) => Add(Fact.ChestsOpened, 1f);
    private void OnLevelUp(int newLevel) => Raise(Fact.Level, newLevel);
    private void OnEssenceChanged(int amount) => Raise(Fact.EssenceHeld, amount);
    private void OnPerilChanged(int peril) => Raise(Fact.Peril, peril);

    private void OnRunEventEnded(string eventId, bool success, string summary)
    {
        if (success)
            Add(Fact.EventsSucceeded, 1f);
    }

    private void OnWeaponUpgraded(string weaponId, int slotIndex, string stat, int newLevel)
    {
        if (stat == "ascension")
            Add(Fact.Ascensions, 1f);
        UpdateWeaponLevels();
    }

    private void OnWeaponInventoryChanged() => UpdateWeaponLevels();

    private void OnGameStateChanged(string oldState, string newState)
    {
        if (newState != nameof(GameManager.GameState.Death) || _ended)
            return;
        Flush();
        _ended = true;
    }

    private void UpdateWeaponLevels()
    {
        if (!_byFact.TryGetValue(Fact.WeaponsAtLevel, out List<Watch> watches) || CachePlayer() is not Player player)
            return;
        foreach (Watch watch in watches)
        {
            int count = 0;
            foreach (WeaponInstance weapon in player.WeaponSlots)
                if (weapon.Level >= watch.Condition.Param)
                    count++;
            Update(watch, count, false);
        }
    }

    /// <summary>« Bien au chaud » : la valeur suit le temps de run tant que les PV ne sont jamais passés sous le seuil.</summary>
    private void UpdateHealthyTime()
    {
        if (!_byFact.TryGetValue(Fact.HealthyTime, out List<Watch> watches) || CachePlayer() is not Player player)
            return;
        float ratio = player.CurrentHp / Mathf.Max(1f, player.EffectiveMaxHp);
        foreach (Watch watch in watches)
        {
            if (watch.Broken)
                continue;
            if (ratio < watch.Condition.Param)
                watch.Broken = true;
            else
                Update(watch, _runTime, false);
        }
    }

    private int OublisHeld() => PerilManager.Current?.Oublis.Count ?? 0;

    // ==============================
    // Avancée et accomplissement
    // ==============================

    private void Add(Fact fact, float amount)
    {
        if (_byFact.TryGetValue(fact, out List<Watch> watches))
            foreach (Watch watch in watches)
                Update(watch, amount, true);
    }

    private void Raise(Fact fact, float value)
    {
        if (_byFact.TryGetValue(fact, out List<Watch> watches))
            foreach (Watch watch in watches)
                Update(watch, value, false);
    }

    /// <summary>Ajoute <paramref name="amount"/>, ou retient le maximum ; un délai passé gèle la condition.</summary>
    private void Update(Watch watch, float amount, bool additive)
    {
        if (_ended || watch.Owner.Done || (watch.Condition.BeforeSec > 0f && _runTime >= watch.Condition.BeforeSec))
            return;
        float value = additive ? watch.Value + amount : Mathf.Max(watch.Value, amount);
        if (value <= watch.Value)
            return;
        watch.Value = value;
        if (watch.Stored + value >= watch.Condition.Target)
            TryComplete(watch.Owner);
    }

    private void TryComplete(Tracked tracked)
    {
        foreach (Watch watch in tracked.Watches)
        {
            if (watch.Stored + watch.Value < watch.Condition.Target)
                return;
        }
        tracked.Done = true;
        if (!MetaSaveManager.ClaimQuest(tracked.Quest, out SaveFile.WriteResult saved))
            return;
        if (!saved.Succeeded)
            GD.PushError($"[QuestTracker] Quête {tracked.Quest.Id} non enregistrée : {saved.Error}");
        GD.Print($"[QuestTracker] Quête accomplie à {_runTime:F0} s : {tracked.Quest.Id}");
        _eventBus.EmitSignal(EventBus.SignalName.QuestCompleted, tracked.Quest.Id);
    }

    /// <summary>
    /// Retient l'avancée des quêtes non accomplies en une écriture : meilleure valeur de la run, ou part du cumul pas
    /// encore retenue. Sans effet si rien n'a bougé ; appelé à la mort et à la sortie de la run.
    /// </summary>
    private void Flush()
    {
        bool dirty = false;
        MetaSaveManager.BeginBatch();
        try
        {
            foreach (Tracked tracked in _quests)
            {
                if (tracked.Done || !HasNewProgress(tracked))
                    continue;
                float[] values = new float[tracked.Watches.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    Watch watch = tracked.Watches[i];
                    values[i] = tracked.Quest.Scope == QuestScope.Cumulative ? watch.Value - watch.Flushed : watch.Value;
                    watch.Flushed = watch.Value;
                }
                MetaSaveManager.RecordQuestProgress(tracked.Quest, values);
                dirty = true;
            }
            if (dirty)
                MetaSaveManager.Save();
        }
        finally
        {
            SaveFile.WriteResult saved = MetaSaveManager.EndBatch();
            if (!saved.Succeeded)
                GD.PushError($"[QuestTracker] Avancée des quêtes non enregistrée : {saved.Error}");
        }
    }

    private static bool HasNewProgress(Tracked tracked)
    {
        foreach (Watch watch in tracked.Watches)
            if (watch.Value > watch.Flushed)
                return true;
        return false;
    }

    private Player CachePlayer()
    {
        if (_player == null || !IsInstanceValid(_player))
            _player = _groups.GetPlayer() as Player;
        return _player;
    }
}
