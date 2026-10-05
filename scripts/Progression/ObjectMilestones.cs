using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Paliers d'objets atteints par un joueur (plan 21 §4) : leurs paramètres et leur état. Créé au premier palier
/// atteint ; ses réactions aux événements sortent aussitôt si leur palier n'est pas actif. Il ne tourne par frame que
/// s'il attend quelque chose (attaque répétée, zone qui refrappe, régénération renforcée).
/// </summary>
public partial class ObjectMilestones : Node
{
    public const string SpreadTargetsEffect = "spread_targets";
    public const string PierceDamageRampEffect = "pierce_damage_ramp";
    public const string StatusRenewEffect = "status_renew";
    public const string RepeatAttackEffect = "repeat_attack";
    public const string ZoneEchoEffect = "zone_echo";
    public const string RangeEndBurstEffect = "range_end_burst";
    public const string FullHpCritEffect = "full_hp_crit";
    public const string IgnoreSmallHitsEffect = "ignore_small_hits";
    public const string WoundRegenEffect = "wound_regen";
    public const string DashArmorEffect = "dash_armor";
    public const string ShieldBreakWaveEffect = "shield_break_wave";
    public const string DashDistanceEffect = "dash_distance";
    public const string OrbHealEffect = "orb_heal";
    public const string LowHpLifestealEffect = "low_hp_lifesteal";
    public const string LevelEssenceEffect = "level_essence";
    public const string LevelRerollEffect = "level_reroll";

    /// <summary>Papier carbone, palier 15 : les projectiles en plus visent chacun leur propre cible.</summary>
    public bool SpreadsExtraProjectiles => _spreadExtraProjectiles;
    /// <summary>Reflet brisé, palier 15 : part des dégâts de départ gagnée par ennemi traversé.</summary>
    public float PierceDamageRamp => _pierceDamageRamp;

    private const float SparkHeight = 14f;
    private const int MaxBurstFxPerFrame = 6;

    private struct PendingRepeat
    {
        public WeaponInstance Weapon;
        public float Remaining;
    }

    private Player _player;
    private ulong _playerId;
    private EventBus _eventBus;
    private GroupCache _groupCache;

    private float _renewChance;
    private bool _spreadExtraProjectiles;
    private float _pierceDamageRamp;
    private int _repeatEvery;
    private float _repeatDelay;
    private readonly Dictionary<WeaponInstance, int> _attackCounts = new();
    private readonly List<PendingRepeat> _pendingRepeats = new();
    private readonly List<WeaponInstance> _removedWeapons = new();
    private ZoneEchoes _zoneEchoes;
    private float _burstRatio;
    private float _burstRadius;
    private ulong _burstFrame;
    private int _burstFxThisFrame;
    private float _fullHpCritMultiplier = 1f;
    private float _ignoredHitRatio;
    private float _woundRegenMultiplier = 1f;
    private float _woundRegenSeconds;
    private float _woundRegenRemaining;
    private float _dashArmorMultiplier = 1f;
    private float _dashArmorSeconds = -1f;
    private float _waveRadius;
    private float _waveKnockback;
    private float _orbHeal;
    private float _lowHpLifestealThreshold;
    private float _lowHpLifestealMultiplier = 1f;
    private int _levelEssence;
    private int _rerollEveryLevels;

    /// <summary>Tirages des paliers à chance ; les bancs le graine pour rester déterministes.</summary>
    internal RandomNumberGenerator Rng { get; } = new();

    public bool HasZoneEcho => _zoneEchoes != null;
    public bool HasRangeEndBurst => _burstRatio > 0f;
    public float RegenMultiplier => _woundRegenRemaining > 0f ? _woundRegenMultiplier : 1f;

    /// <summary>Paille tordue au palier : sous le seuil de PV, le plafond du vol de vie est multiplié.</summary>
    public float LifestealCapMultiplier(float hpRatio) => hpRatio < _lowHpLifestealThreshold ? _lowHpLifestealMultiplier : 1f;

    public void Initialize(Player player)
    {
        _player = player;
        _playerId = player.GetInstanceId();
        Rng.Seed = RunRandom.SeedFor("object_milestones");
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
        _eventBus.EnemyStatusExpired += OnStatusExpired;
        _eventBus.PlayerDamageResolved += OnPlayerDamage;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.WeaponInventoryChanged += OnWeaponInventoryChanged;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.EnemyStatusExpired -= OnStatusExpired;
        _eventBus.PlayerDamageResolved -= OnPlayerDamage;
        _eventBus.LevelUp -= OnLevelUp;
        _eventBus.WeaponInventoryChanged -= OnWeaponInventoryChanged;
    }

    /// <summary>Branche l'effet d'un palier que l'objet vient d'atteindre (le joueur l'a ajouté à l'arbre avant).</summary>
    public void Activate(ObjectMilestoneData milestone)
    {
        switch (milestone.Effect)
        {
            case SpreadTargetsEffect:
                _spreadExtraProjectiles = true;
                break;
            case PierceDamageRampEffect:
                _pierceDamageRamp = milestone.Parameter("per_enemy");
                break;
            case StatusRenewEffect:
                _renewChance = milestone.Parameter("chance");
                break;
            case RepeatAttackEffect:
                _repeatEvery = Mathf.Max(1, Mathf.RoundToInt(milestone.Parameter("every")));
                _repeatDelay = milestone.Parameter("delay_seconds");
                break;
            case ZoneEchoEffect:
                _zoneEchoes = new ZoneEchoes(_player, _groupCache, milestone.Parameter("ratio"), milestone.Parameter("delay_seconds"));
                break;
            case RangeEndBurstEffect:
                _burstRatio = milestone.Parameter("damage_ratio");
                _burstRadius = milestone.Parameter("radius");
                break;
            case FullHpCritEffect:
                _fullHpCritMultiplier = milestone.Parameter("multiplier");
                break;
            case IgnoreSmallHitsEffect:
                _ignoredHitRatio = milestone.Parameter("max_hp_ratio");
                break;
            case WoundRegenEffect:
                _woundRegenMultiplier = milestone.Parameter("multiplier");
                _woundRegenSeconds = milestone.Parameter("seconds");
                break;
            case DashArmorEffect:
                _dashArmorMultiplier = milestone.Parameter("multiplier");
                _dashArmorSeconds = milestone.Parameter("seconds_after");
                break;
            case ShieldBreakWaveEffect:
                _waveRadius = milestone.Parameter("radius");
                _waveKnockback = milestone.Parameter("knockback");
                break;
            case DashDistanceEffect:
                _player.Mobility.DistanceMultiplier += milestone.Parameter("bonus");
                break;
            case OrbHealEffect:
                _orbHeal = milestone.Parameter("heal");
                break;
            case LowHpLifestealEffect:
                _lowHpLifestealThreshold = milestone.Parameter("threshold");
                _lowHpLifestealMultiplier = milestone.Parameter("multiplier");
                break;
            case LevelEssenceEffect:
                _levelEssence = Mathf.RoundToInt(milestone.Parameter("essence"));
                break;
            case LevelRerollEffect:
                _rerollEveryLevels = Mathf.Max(1, Mathf.RoundToInt(milestone.Parameter("every_levels")));
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (_player.IsDead)
        {
            // Rien ne frappe plus après la mort : ni attaque répétée, ni zone qui refrappe.
            _pendingRepeats.Clear();
            _zoneEchoes?.Clear();
            _woundRegenRemaining = 0f;
            SetProcess(false);
            return;
        }
        float dt = (float)delta;
        for (int i = _pendingRepeats.Count - 1; i >= 0; i--)
        {
            PendingRepeat repeat = _pendingRepeats[i];
            repeat.Remaining -= dt;
            if (repeat.Remaining > 0f)
            {
                _pendingRepeats[i] = repeat;
                continue;
            }
            _pendingRepeats[i] = _pendingRepeats[^1];
            _pendingRepeats.RemoveAt(_pendingRepeats.Count - 1);
            _player.RepeatAttack(repeat.Weapon);
        }
        _zoneEchoes?.Advance(dt);
        _woundRegenRemaining = Mathf.Max(0f, _woundRegenRemaining - dt);
        SetProcess(NeedsClock);
    }

    private bool NeedsClock => _pendingRepeats.Count > 0 || (_zoneEchoes?.HasPending ?? false) || _woundRegenRemaining > 0f;

    /// <summary>Ressort de sommier : compte les attaques d'une arme ; la n-ième repart peu après.</summary>
    public void CountAttack(WeaponInstance weapon)
    {
        if (_repeatEvery <= 0 || weapon == null)
            return;
        _attackCounts.TryGetValue(weapon, out int count);
        count++;
        if (count >= _repeatEvery)
        {
            count = 0;
            _pendingRepeats.Add(new PendingRepeat { Weapon = weapon, Remaining = _repeatDelay });
            SetProcess(true);
        }
        _attackCounts[weapon] = count;
    }

    /// <summary>Rondelle de cuivre : l'arc ou le cercle de mêlée qui vient de frapper refrappera.</summary>
    public void QueueArcEcho(Vector2 direction, float range, float arcAngle, float damage, WeaponInstance weapon, AttackContext source)
    {
        _zoneEchoes.QueueArc(direction, range, arcAngle, damage, weapon, source);
        SetProcess(true);
    }

    /// <summary>Rondelle de cuivre : la zone posée par une arme refrappera à sa place.</summary>
    public void QueueCircleEcho(Vector2 center, float radius, float damage, WeaponInstance weapon, AttackContext source)
    {
        _zoneEchoes.QueueCircle(center, radius, damage, weapon, source);
        SetProcess(true);
    }

    /// <summary>
    /// Mètre pliant : un projectile d'arme arrivé en bout de course éclate. Le rayon suit la Taille ; l'éclat ne
    /// déclenche aucun effet à l'impact.
    /// </summary>
    public void BurstAtRangeEnd(Vector2 position, float projectileDamage, AttackContext source)
    {
        float radius = _burstRadius * _player.AoeMultiplier;
        float radiusSq = radius * radius;
        float damage = projectileDamage * _burstRatio;
        AttackContext burst = source.As(DamageKind.Passive);
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is Enemy { IsActive: true, IsDying: false } enemy
                && Iso.GroundDistanceSquared(enemy.GlobalPosition, position) <= radiusSq)
                enemy.TakeDamage(damage, source: burst);
        }
        // Une salve de projectiles peut éclater d'un coup : quelques éclats dessinés par frame suffisent à le lire.
        ulong frame = Engine.GetProcessFrames();
        if (frame != _burstFrame)
        {
            _burstFrame = frame;
            _burstFxThisFrame = 0;
        }
        if (++_burstFxThisFrame <= MaxBurstFxPerFrame)
            _player.AttackFx.PlayBurst(position, PlayerAttackFx.FamilyOf(source.Weapon?.Base), radius);
    }

    /// <summary>Lunettes de lecture : un critique sur une cible encore intacte compte davantage.</summary>
    public float CritAgainst(Enemy enemy, float damage, bool isCrit) =>
        isCrit && enemy.HpRatio >= 0.999f ? damage * _fullHpCritMultiplier : damage;

    /// <summary>Bouton de manteau : un coup trop petit face aux PV max ne passe pas.</summary>
    public bool Ignores(float damage, float maxHp) => _ignoredHitRatio > 0f && damage < maxHp * _ignoredHitRatio;

    /// <summary>Genouillère : l'armure pendant le dash et juste après.</summary>
    public float ArmorMultiplier(PlayerMobility mobility) =>
        mobility.IsDashing || mobility.SecondsSinceDash <= _dashArmorSeconds ? _dashArmorMultiplier : 1f;

    /// <summary>Écusson de pompier : le bouclier qui casse repousse les ennemis proches.</summary>
    public void OnShieldBroken()
    {
        if (_waveRadius <= 0f)
            return;
        Vector2 origin = _player.GlobalPosition;
        float radiusSq = _waveRadius * _waveRadius;
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy
                || Iso.GroundDistanceSquared(enemy.GlobalPosition, origin) > radiusSq)
                continue;
            Vector2 away = enemy.GlobalPosition - origin;
            enemy.ApplyKnockback(away == Vector2.Zero ? Vector2.Right : away, _waveKnockback);
        }
        _player.AttackFx.PlayObjectRing(origin, FxFamily.Brass, _waveRadius);
        _player.AttackFx.PlayObjectRing(origin, FxFamily.Brass, _waveRadius * 0.6f);
    }

    /// <summary>Aimant de frigo : une orbe ramassée soigne un peu.</summary>
    public void OnOrbCollected()
    {
        if (_orbHeal > 0f)
            _player.Heal(_orbHeal);
    }

    /// <summary>Jeton de fête foraine : un niveau de joueur multiple du pas donne une relance.</summary>
    public bool GrantsRerollAt(int playerLevel) => _rerollEveryLevels > 0 && playerLevel % _rerollEveryLevels == 0;

    private void OnPlayerDamage(PlayerDamageResult result)
    {
        if (_woundRegenSeconds <= 0f || result.PlayerId != _playerId || !result.Applied || result.HpLost <= 0f)
            return;
        _woundRegenRemaining = _woundRegenSeconds;
        SetProcess(true);
    }

    /// <summary>Photo de classe : chaque niveau gagné rapporte de l'Essence, qui vole du joueur vers le compteur.</summary>
    private void OnLevelUp(int newLevel)
    {
        if (_levelEssence <= 0 || _player.IsDead)
            return;
        _eventBus.EmitSignal(EventBus.SignalName.LootReceived, "essence", LevelEssenceEffect, _levelEssence);
        _eventBus.EmitSignal(EventBus.SignalName.EssenceGained, _levelEssence, _player.GlobalPosition);
    }

    /// <summary>
    /// Une arme retirée perd son compteur ; une arme qui monte de niveau garde le sien (le signal part aussi alors).
    /// Une attaque répétée en attente d'une arme retirée est écartée par le joueur au moment de partir.
    /// </summary>
    private void OnWeaponInventoryChanged()
    {
        if (_attackCounts.Count == 0)
            return;
        _removedWeapons.Clear();
        foreach (WeaponInstance weapon in _attackCounts.Keys)
            if (!IsHeld(weapon))
                _removedWeapons.Add(weapon);
        foreach (WeaponInstance weapon in _removedWeapons)
            _attackCounts.Remove(weapon);
    }

    private bool IsHeld(WeaponInstance weapon)
    {
        foreach (WeaponInstance held in _player.WeaponSlots)
            if (held == weapon)
                return true;
        return false;
    }

    /// <summary>Un contrôle natif reste natif ; tout autre reste non transmissible.</summary>
    private static ControlOrigin RenewedOrigin(in StatusExpiry expiry) =>
        expiry.Origin == ControlOrigin.NativeWeapon ? ControlOrigin.NativeWeapon : ControlOrigin.Unknown;

    /// <summary>Pince à linge : un statut du joueur qui expire peut repartir pour la même durée.</summary>
    private void OnStatusExpired(StatusExpiry expiry)
    {
        if (_renewChance <= 0f || expiry.Source.OwnerId != _playerId || !IsInstanceValid(expiry.Target) || expiry.Target.IsDying)
            return;
        if (Rng.Randf() >= _renewChance)
            return;
        Enemy target = expiry.Target;
        FxFamily family;
        switch (expiry.Kind)
        {
            case StatusKind.Burn:
                target.ApplyIgnite(expiry.Strength, expiry.Duration, expiry.Source);
                family = FxFamily.Fire;
                break;
            case StatusKind.Bleed:
                target.ApplyBleed(expiry.Strength, expiry.Duration, expiry.Source);
                family = FxFamily.Blood;
                break;
            case StatusKind.Slow:
                target.ApplySlow(expiry.Strength, expiry.Duration, expiry.Source, RenewedOrigin(expiry));
                family = FxFamily.Hybrid;
                break;
            case StatusKind.Fragility:
                target.ApplyFragile(expiry.Strength, expiry.Duration, expiry.Source);
                family = FxFamily.Void;
                break;
            default:
                target.ApplyDisorient(expiry.Duration, expiry.Source, RenewedOrigin(expiry));
                family = FxFamily.Brass;
                break;
        }
        CombatPools.Instance?.EmitSparks(target.GlobalPosition + new Vector2(0f, -SparkHeight), new SparkBurst
        {
            Family = family,
            Owner = FxOwner.Player,
            Count = 4,
            Direction = Vector2.Up,
            Spread = 1.4f,
            SpeedMin = 25f,
            SpeedMax = 60f,
            LifeMin = 0.18f,
            LifeMax = 0.3f,
            Size = 1,
        });
    }
}
