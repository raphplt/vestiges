using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>Ce que fait le joueur et qui fait monter un Écusson (plan 35 §9.1) ; noms de passive_souvenirs.json.</summary>
public enum UsageTrigger
{
    WeaponHits,
    WideHits,
    FarKills,
    Crits,
    ContactSeconds,
    HpHealed,
    HitsTaken,
    ShieldAbsorbed,
    Meters,
    XpPickups,
    Levels,
    Chests,
    ProjectileHits,
    Lifesteal,
    Statuses,
    Pierces,
}

/// <summary>
/// Montée des Écussons par l'usage (plan 35 I7, H2) : chaque Écusson porté remplit un fil quand le joueur fait ce que sa
/// figure représente ; un fil plein donne un niveau, comme une carte commune. Le composant écoute l'EventBus ; une
/// horloge d'un quart de seconde ne tourne que pour les fils de contact et de distance. Le HUD suit les fils par
/// <see cref="EventBus.BadgeThreadChanged"/>, émis par seizièmes de fil et au niveau gagné, jamais à chaque coup.
/// </summary>
public partial class BadgeUsage : Node
{
    private const int ProgressSteps = 16;
    private const float ClockSeconds = 0.25f;
    // Couples (lancer, créature) déjà comptés, et créatures touchées par les derniers lancers : un cône, qui frappe sa
    // cible à chaque image sous un même lancer, ne compte qu'un coup par créature.
    private const int RecentHits = 64;
    private const int RecentLaunches = 8;

    private sealed class BadgeThread
    {
        public string Id;
        public UsageTrigger Trigger;
        public float Threshold;
        /// <summary>Seuil au niveau actuel de l'Écusson : il s'allonge à mesure que l'Écusson monte.</summary>
        public float Cost;
        public float Progress;
        public int Shown;
        public int Levels;
        public bool Maxed;
    }

    private static readonly Dictionary<string, UsageTrigger> TriggerNames = new()
    {
        ["weapon_hits"] = UsageTrigger.WeaponHits,
        ["wide_hits"] = UsageTrigger.WideHits,
        ["far_kills"] = UsageTrigger.FarKills,
        ["crits"] = UsageTrigger.Crits,
        ["contact_seconds"] = UsageTrigger.ContactSeconds,
        ["hp_healed"] = UsageTrigger.HpHealed,
        ["hits_taken"] = UsageTrigger.HitsTaken,
        ["shield_absorbed"] = UsageTrigger.ShieldAbsorbed,
        ["meters"] = UsageTrigger.Meters,
        ["xp_pickups"] = UsageTrigger.XpPickups,
        ["levels"] = UsageTrigger.Levels,
        ["chests"] = UsageTrigger.Chests,
        ["projectile_hits"] = UsageTrigger.ProjectileHits,
        ["lifesteal"] = UsageTrigger.Lifesteal,
        ["statuses"] = UsageTrigger.Statuses,
        ["pierces"] = UsageTrigger.Pierces,
    };

    private readonly List<BadgeThread> _threads = new();
    private Player _player;
    private ulong _playerId;
    private EventBus _eventBus;
    private float _pixelsPerMeter = 16f;
    private float _clock;
    private Vector2 _lastPosition;
    private float _lastShield;
    private readonly (ulong Launch, EnemyLife Target)[] _recentHits = new (ulong, EnemyLife)[RecentHits];
    private int _nextHit;
    private readonly ulong[] _launches = new ulong[RecentLaunches];
    private readonly int[] _launchTargets = new int[RecentLaunches];
    private int _nextLaunch;
    private bool _countsMeters;
    private bool _countsContact;
#if TOOLS
    private const int TriggerCount = (int)UsageTrigger.Pierces + 1;
    private readonly float[] _totals = new float[TriggerCount];
    private bool _measureAll;

    /// <summary>Mesure au bot : compte tous les déclencheurs de la run, portés ou non, pour régler les seuils.</summary>
    internal void MeasureAll()
    {
        DevelopmentMode.RequireTestAccess();
        _measureAll = _countsMeters = _countsContact = true;
        if (IsInsideTree())
            SetProcess(true);
    }

    /// <summary>Quantité de ce déclencheur depuis le début de la mesure.</summary>
    internal float TotalOf(UsageTrigger trigger) => _totals[(int)trigger];
#endif

    /// <summary>Niveaux d'Écusson gagnés par l'usage depuis le début de la run (mesure au bot, plan 35 I7).</summary>
    public int UsageLevels { get; private set; }

    /// <summary>Déclencheur connu du code : le contrat des objets ne doit nommer que ceux-là.</summary>
    public static bool IsKnownTrigger(string name) => TriggerNames.ContainsKey(name);

    public void Initialize(Player player)
    {
        _player = player;
        _playerId = player.GetInstanceId();
        _lastPosition = player.GlobalPosition;
        _lastShield = player.Shield;
        _pixelsPerMeter = RunSummaryConfig.Load().PixelsPerMeter;
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyDamageResolved += OnEnemyDamage;
        _eventBus.EnemyKillResolved += OnEnemyKill;
        _eventBus.PlayerHealingResolved += OnHealing;
        _eventBus.PlayerDamageResolved += OnPlayerDamage;
        _eventBus.EnemyStatusExpired += OnStatusExpired;
        _eventBus.PlayerShieldChanged += OnShieldChanged;
        _eventBus.XpGained += OnXpGained;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.ChestOpened += OnChestOpened;
        _eventBus.PassiveSouvenirUpgraded += OnBadgeUpgraded;
        _eventBus.LandChanged += OnLandChanged;
        SetProcess(_countsMeters || _countsContact);
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.EnemyDamageResolved -= OnEnemyDamage;
        _eventBus.EnemyKillResolved -= OnEnemyKill;
        _eventBus.PlayerHealingResolved -= OnHealing;
        _eventBus.PlayerDamageResolved -= OnPlayerDamage;
        _eventBus.EnemyStatusExpired -= OnStatusExpired;
        _eventBus.PlayerShieldChanged -= OnShieldChanged;
        _eventBus.XpGained -= OnXpGained;
        _eventBus.LevelUp -= OnLevelUp;
        _eventBus.ChestOpened -= OnChestOpened;
        _eventBus.PassiveSouvenirUpgraded -= OnBadgeUpgraded;
        _eventBus.LandChanged -= OnLandChanged;
    }

    /// <summary>Le joueur repart du départ d'une terre neuve (plan 51) : ce saut ne file pas les Écussons de la marche.</summary>
    private void OnLandChanged(int index)
    {
        if (_player != null)
            _lastPosition = _player.GlobalPosition;
    }

    /// <summary>Un Écusson vient d'être cousu : son fil commence vide. Sans déclencheur, il ne monte qu'au level-up.</summary>
    public void Track(PassiveSouvenirData badge)
    {
        if (!badge.IsBadge || badge.UsageThreshold <= 0f || !TriggerNames.TryGetValue(badge.UsageTrigger, out UsageTrigger trigger))
            return;
        foreach (BadgeThread existing in _threads)
            if (existing.Id == badge.Id)
                return;
        BadgeThread added = new() { Id = badge.Id, Trigger = trigger, Threshold = badge.UsageThreshold };
        added.Cost = CostAt(added, _player.GetPassiveLevel(badge.Id));
        _threads.Add(added);
        _countsMeters |= trigger == UsageTrigger.Meters;
        _countsContact |= trigger == UsageTrigger.ContactSeconds;
        _lastPosition = _player.GlobalPosition;
        if (IsInsideTree())
            SetProcess(_countsMeters || _countsContact);
    }

    /// <summary>Un Écusson oublié (plan 44) : son fil s'arrête et ne compte plus rien.</summary>
    public void Untrack(string badgeId)
    {
        _threads.RemoveAll(thread => thread.Id == badgeId);
#if TOOLS
        if (_measureAll)
            return;
#endif
        _countsMeters = _countsContact = false;
        foreach (BadgeThread thread in _threads)
        {
            _countsMeters |= thread.Trigger == UsageTrigger.Meters;
            _countsContact |= thread.Trigger == UsageTrigger.ContactSeconds;
        }
        if (IsInsideTree())
            SetProcess(_countsMeters || _countsContact);
    }

    /// <summary>Niveaux que cet Écusson a gagnés par l'usage (mesure au bot).</summary>
    public int UsageLevelsOf(string badgeId)
    {
        foreach (BadgeThread thread in _threads)
            if (thread.Id == badgeId)
                return thread.Levels;
        return 0;
    }

    /// <summary>Remplissage du fil de cet Écusson, de 0 à 1 ; 0 s'il n'en a pas.</summary>
    public float ProgressOf(string badgeId)
    {
        foreach (BadgeThread thread in _threads)
            if (thread.Id == badgeId)
                return thread.Progress / thread.Cost;
        return 0f;
    }

    public override void _Process(double delta)
    {
        if (_player.IsDead)
            return;
        _clock += (float)delta;
        if (_clock < ClockSeconds)
            return;
        float elapsed = _clock;
        _clock = 0f;
        Vector2 position = _player.GlobalPosition;
        if (_countsMeters)
            Add(UsageTrigger.Meters, position.DistanceTo(_lastPosition) / _pixelsPerMeter);
        _lastPosition = position;
        if (_countsContact && IsInContact(position))
            Add(UsageTrigger.ContactSeconds, elapsed);
    }

    private bool IsInContact(Vector2 position)
    {
        float radius = InventoryConfig.UsageContactRadius;
        using CrowdQuery crowd = CrowdIndex.Near(position, radius);
        foreach (Node node in crowd.Targets)
        {
            if (node is Enemy { IsActive: true, IsDying: false, IsBurrowed: false } enemy
                && Iso.GroundDistanceSquared(enemy.GlobalPosition, position) <= radius * radius)
                return true;
        }
        return false;
    }

    private void OnEnemyDamage(DamageResult result)
    {
        if (!Counting || !result.Applied || result.Source.OwnerId != _playerId
            || result.Source.Kind != DamageKind.DirectWeapon || !FirstHit(result.Source.LaunchId, result.Target))
            return;
        Add(UsageTrigger.WeaponHits, 1f);
        if (result.Critical)
            Add(UsageTrigger.Crits, 1f);
        bool ranged = result.Source.Weapon?.Base.Category == WeaponCategory.Ranged;
        if (ranged)
            Add(UsageTrigger.ProjectileHits, 1f);
        // Une même attaque (un lancer) qui touche plusieurs créatures : coup large au seuil ; pour une arme à distance,
        // chaque créature au-delà de la première (traversée, rebond ou salve) compte pour l'archer.
        int targets = CountLaunchTarget(result.Source.LaunchId);
        if (targets == InventoryConfig.UsageWideHitTargets)
            Add(UsageTrigger.WideHits, 1f);
        if (ranged && targets > 1)
            Add(UsageTrigger.Pierces, 1f);
    }

    /// <summary>Premier coup de ce lancer sur cette créature ; vrai une seule fois par couple, parmi les plus récents.</summary>
    private bool FirstHit(ulong launch, EnemyLife target)
    {
        for (int i = 0; i < RecentHits; i++)
            if (_recentHits[i].Launch == launch && _recentHits[i].Target == target)
                return false;
        _recentHits[_nextHit] = (launch, target);
        _nextHit = (_nextHit + 1) % RecentHits;
        return true;
    }

    /// <summary>Créatures distinctes touchées par ce lancer, celle-ci comprise ; les huit derniers lancers sont suivis.</summary>
    private int CountLaunchTarget(ulong launch)
    {
        for (int i = 0; i < RecentLaunches; i++)
        {
            if (_launches[i] == launch)
                return ++_launchTargets[i];
        }
        _launches[_nextLaunch] = launch;
        _launchTargets[_nextLaunch] = 1;
        _nextLaunch = (_nextLaunch + 1) % RecentLaunches;
        return 1;
    }

    private void OnEnemyKill(EnemyKillResult kill)
    {
        // Ce qu'une arme a fait, effets sur la durée compris ; pas ce qu'un objet a produit.
        if (!Counting || kill.Damage.Source.OwnerId != _playerId || !kill.Damage.Source.IsWeaponWork)
            return;
        float far = InventoryConfig.UsageFarKillDistance;
        if (Iso.GroundDistanceSquared(kill.Position, _player.GlobalPosition) > far * far)
            Add(UsageTrigger.FarKills, 1f);
    }

    private void OnHealing(HealingResult result)
    {
        if (result.PlayerId != _playerId || result.HpRestored <= 0f)
            return;
        Add(UsageTrigger.HpHealed, result.HpRestored);
        if (result.Kind == HealingKind.Lifesteal)
            Add(UsageTrigger.Lifesteal, result.HpRestored);
    }

    private void OnPlayerDamage(PlayerDamageResult result)
    {
        if (result.PlayerId == _playerId && result.Applied && result.Kind == PlayerDamageKind.Combat
            && (result.HpLost > 0f || result.ShieldAbsorbed))
            Add(UsageTrigger.HitsTaken, 1f);
    }

    private void OnShieldChanged(float shield, float maxShield)
    {
        // Seule la baisse sous le maximum compte : ni la recharge, ni un bouclier rabattu à un maximum qui baisse ne sont
        // une absorption. Le fil peut faire monter l'Écusson à bouclier, qui rappelle ce signal : l'état est mis à jour
        // avant.
        float absorbed = shield < _lastShield && shield < maxShield ? _lastShield - shield : 0f;
        _lastShield = shield;
        if (absorbed > 0f)
            Add(UsageTrigger.ShieldAbsorbed, absorbed);
    }

    private void OnStatusExpired(StatusExpiry expiry)
    {
        // Les statuts posés par les armes, pas ceux des objets.
        if (expiry.Source.OwnerId == _playerId && expiry.Source.Kind != DamageKind.Passive)
            Add(UsageTrigger.Statuses, 1f);
    }

    private void OnXpGained(float amount) => Add(UsageTrigger.XpPickups, 1f);

    private void OnLevelUp(int level) => Add(UsageTrigger.Levels, 1f);

    private void OnChestOpened(string chestId, string rarity, Vector2 position) => Add(UsageTrigger.Chests, 1f);

    /// <summary>
    /// Le fil d'un Écusson s'allonge avec son niveau (level_scale d'inventory.json) : un Écusson qui renforce ce qui
    /// remplit son fil (portée, critiques, cadence) ne s'emballe pas, et l'usage garde sa part au fil de la run.
    /// </summary>
    private static float CostAt(BadgeThread thread, int level) =>
        thread.Threshold * (1f + InventoryConfig.UsageLevelScale * Mathf.Max(0, level - 1));

    /// <summary>Un Écusson a monté par une carte : son fil s'allonge d'autant.</summary>
    private void OnBadgeUpgraded(string badgeId, int level)
    {
        foreach (BadgeThread thread in _threads)
            if (thread.Id == badgeId)
                thread.Cost = CostAt(thread, level);
    }

    private bool Counting
    {
        get
        {
#if TOOLS
            if (_measureAll)
                return true;
#endif
            return _threads.Count > 0;
        }
    }

    private void Add(UsageTrigger trigger, float amount)
    {
#if TOOLS
        _totals[(int)trigger] += amount;
#endif
        for (int i = 0; i < _threads.Count; i++)
        {
            BadgeThread thread = _threads[i];
            if (thread.Trigger != trigger || thread.Maxed)
                continue;
            thread.Progress += amount;
            bool leveled = false;
            while (thread.Progress >= thread.Cost)
            {
                thread.Progress -= thread.Cost;
                // Un Écusson au niveau maximal garde son fil plein : rien de plus à gagner.
                if (!_player.AddOrUpgradePassive(thread.Id))
                {
                    thread.Progress = thread.Cost;
                    thread.Maxed = true;
                    break;
                }
                leveled = true;
                thread.Levels++;
                UsageLevels++;
                thread.Cost = CostAt(thread, _player.GetPassiveLevel(thread.Id));
            }
            int shown = Mathf.FloorToInt(thread.Progress / thread.Cost * ProgressSteps);
            if (shown == thread.Shown && !leveled)
                continue;
            thread.Shown = shown;
            _eventBus?.EmitSignal(EventBus.SignalName.BadgeThreadChanged, thread.Id, thread.Progress / thread.Cost, leveled);
        }
    }
}
