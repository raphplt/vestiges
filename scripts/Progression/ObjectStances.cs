using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Objets d'état d'un joueur (plan 21 §4, plan 23 R6) : ce qui vaut tant qu'une condition tient. Tabouret de camping
/// (immobile), Gilet réfléchissant (ennemis proches), Thermos (PV hauts), Médaille cabossée (PV bas), Porte-monnaie
/// usé (Essence gardée). Créé au premier objet de ce type ; il relit les conditions dix fois par seconde, pas à
/// chaque frame, et donne un multiplicateur de dégâts que le joueur applique à ses coups d'arme.
/// </summary>
public partial class ObjectStances : Node
{
    public const string StillAttackSpeedStat = "still_attack_speed";
    public const string CrowdDamageStat = "crowd_damage";
    public const string HighHpDamageStat = "high_hp_damage";
    public const string LowHpDamageStat = "low_hp_damage";
    public const string EssenceDamageCapStat = "essence_damage_cap";

    public const string StillArmorEffect = "still_armor";
    public const string CrowdCapEffect = "crowd_cap";
    public const string ThermosThresholdEffect = "thermos_threshold";
    public const string LowHpSpeedEffect = "low_hp_speed";
    public const string EssenceRefundEffect = "essence_refund";

    private const float CheckSeconds = 0.1f;
    private const float MovingSpeedSq = 25f;
    private const float SparkHeight = 14f;

    private Player _player;
    private EventBus _eventBus;
    private GroupCache _groupCache;
    private float _clock;

    private float _stillAttackSpeed;
    private float _stillSeconds;
    private float _stillArmor;
    private float _stillElapsed;
    private bool _still;
    private float _appliedStillSpeed;
    private float _appliedStillArmor;

    private float _crowdDamage;
    private float _crowdRadius;
    private int _crowdMax;
    private int _crowdCount;

    private float _highHpDamage;
    private float _highHpThreshold;
    private bool _highHp;

    private float _lowHpDamage;
    private float _lowHpThreshold;
    private float _lowHpSpeed;
    private bool _lowHp;
    private float _appliedLowHpSpeed;

    private float _essenceCap;
    private float _essencePerStep;
    private float _damagePerStep;
    private float _refundRatio;
    private int _essence;
    private int _pendingRefund;
    private float _essenceBonus;

    public void Initialize(Player player) => _player = player;

    /// <summary>Multiplicateur de dégâts des coups d'arme, selon les états qui tiennent en ce moment.</summary>
    public float DamageMultiplier => 1f + _crowdDamage * _crowdCount + (_highHp ? _highHpDamage : 0f)
        + (_lowHp ? _lowHpDamage : 0f) + _essenceBonus;

    public bool IsStill => _still;
    public int CrowdCount => _crowdCount;

    public static bool IsStanceStat(string stat) =>
        stat is StillAttackSpeedStat or CrowdDamageStat or HighHpDamageStat or LowHpDamageStat or EssenceDamageCapStat;

    public static bool IsStanceMilestone(string effect) =>
        effect is StillArmorEffect or CrowdCapEffect or ThermosThresholdEffect or LowHpSpeedEffect or EssenceRefundEffect;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
        _eventBus.EssenceChanged += OnEssenceChanged;
        if (GetNodeOrNull<EssenceTracker>("/root/Main/EssenceTracker") is { } tracker)
            _essence = tracker.CurrentEssence;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.EssenceChanged -= OnEssenceChanged;
    }

    /// <summary>Réglages d'un objet d'état, lus à son arrivée.</summary>
    public void Configure(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
        {
            switch (effect.Stat)
            {
                case StillAttackSpeedStat: _stillSeconds = Parameter(data, "still_seconds"); break;
                case CrowdDamageStat:
                    _crowdRadius = Parameter(data, "radius");
                    _crowdMax = Mathf.RoundToInt(Parameter(data, "max_enemies"));
                    break;
                case HighHpDamageStat: _highHpThreshold = Parameter(data, "threshold"); break;
                case LowHpDamageStat: _lowHpThreshold = Parameter(data, "threshold"); break;
                case EssenceDamageCapStat:
                    _essencePerStep = Parameter(data, "essence_per_step");
                    _damagePerStep = Parameter(data, "damage_per_step");
                    break;
            }
        }
    }

    /// <summary>Écart de valeur d'un effet d'objet d'état (somme des gains de ses cartes).</summary>
    public void Add(string stat, float change)
    {
        switch (stat)
        {
            case StillAttackSpeedStat: _stillAttackSpeed += change; ApplyStill(); break;
            case CrowdDamageStat: _crowdDamage += change; break;
            case HighHpDamageStat: _highHpDamage += change; break;
            case LowHpDamageStat: _lowHpDamage += change; break;
            case EssenceDamageCapStat: _essenceCap += change; RefreshEssenceBonus(); break;
        }
    }

    /// <summary>Branche le palier d'un objet d'état.</summary>
    public void Activate(ObjectMilestoneData milestone)
    {
        switch (milestone.Effect)
        {
            case StillArmorEffect: _stillArmor = milestone.Parameter("armor"); ApplyStill(); break;
            case CrowdCapEffect: _crowdMax = Mathf.RoundToInt(milestone.Parameter("max_enemies")); break;
            case ThermosThresholdEffect: _highHpThreshold = milestone.Parameter("threshold"); break;
            case LowHpSpeedEffect: _lowHpSpeed = milestone.Parameter("speed"); ApplyLowHpSpeed(); break;
            case EssenceRefundEffect: _refundRatio = milestone.Parameter("ratio"); break;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_stillAttackSpeed > 0f)
            AdvanceStill(dt);
        _clock -= dt;
        if (_clock > 0f)
            return;
        _clock = CheckSeconds;
        if (_crowdDamage > 0f)
            CountCrowd();
        float ratio = _player.EffectiveMaxHp > 0f ? _player.CurrentHp / _player.EffectiveMaxHp : 0f;
        if (_highHpDamage > 0f)
            SetHighHp(ratio >= _highHpThreshold);
        if (_lowHpDamage > 0f)
            SetLowHp(ratio > 0f && ratio < _lowHpThreshold);
    }

    /// <summary>Tabouret de camping : immobile depuis le délai, la cadence monte (et l'armure au palier).</summary>
    private void AdvanceStill(float delta)
    {
        bool moving = _player.Velocity.LengthSquared() >= MovingSpeedSq;
        _stillElapsed = moving ? 0f : _stillElapsed + delta;
        bool still = !_player.IsDead && _stillElapsed >= _stillSeconds;
        if (still == _still)
            return;
        _still = still;
        ApplyStill();
        if (still)
        {
            Sparks(FxFamily.Brass, 6);
            _player.ObjectProcs?.Show(StillAttackSpeedStat);
        }
    }

    /// <summary>Accorde la cadence et l'armure de l'état immobile, ou les retire : seul l'écart est appliqué.</summary>
    private void ApplyStill()
    {
        float speed = _still ? _stillAttackSpeed : 0f;
        if (!Mathf.IsEqualApprox(speed, _appliedStillSpeed))
        {
            _player.ApplyPerkModifier("attack_speed", (1f + speed) / (1f + _appliedStillSpeed), "multiplicative");
            _appliedStillSpeed = speed;
        }
        float armor = _still ? _stillArmor : 0f;
        if (!Mathf.IsEqualApprox(armor, _appliedStillArmor))
        {
            _player.ApplyPerkModifier("armor", armor - _appliedStillArmor, "additive");
            _appliedStillArmor = armor;
        }
    }

    /// <summary>Gilet réfléchissant : ennemis à portée, plafonnés.</summary>
    private void CountCrowd()
    {
        int count = 0;
        float radiusSq = _crowdRadius * _crowdRadius;
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is Enemy { IsActive: true, IsDying: false } enemy
                && Iso.GroundDistanceSquared(enemy.GlobalPosition, _player.GlobalPosition) <= radiusSq
                && ++count >= _crowdMax)
                break;
        }
        // Le gilet brille quand il atteint son plafond : on voit que la foule le sert.
        if (count >= _crowdMax && _crowdCount < _crowdMax)
        {
            Sparks(FxFamily.Brass, 8);
            _player.ObjectProcs?.Show(CrowdDamageStat);
        }
        _crowdCount = count;
    }

    /// <summary>Thermos : les dégâts montent tant que les PV restent au-dessus du seuil.</summary>
    private void SetHighHp(bool high)
    {
        if (high == _highHp)
            return;
        _highHp = high;
        if (high)
        {
            Sparks(FxFamily.Pale, 6);
            _player.ObjectProcs?.Show(HighHpDamageStat);
        }
    }

    /// <summary>Médaille cabossée : sous le seuil, dégâts (et vitesse au palier) en plus.</summary>
    private void SetLowHp(bool low)
    {
        if (low == _lowHp)
            return;
        _lowHp = low;
        ApplyLowHpSpeed();
        if (low)
        {
            Sparks(FxFamily.Blood, 8);
            _player.ObjectProcs?.Show(LowHpDamageStat);
        }
    }

    private void ApplyLowHpSpeed()
    {
        float speed = _lowHp ? _lowHpSpeed : 0f;
        if (Mathf.IsEqualApprox(speed, _appliedLowHpSpeed))
            return;
        _player.ApplyPerkModifier("speed", (1f + speed) / (1f + _appliedLowHpSpeed), "multiplicative");
        _appliedLowHpSpeed = speed;
    }

    /// <summary>Porte-monnaie usé : l'Essence gardée renforce les coups ; au palier, une dépense est en partie rendue.</summary>
    private void OnEssenceChanged(int amount)
    {
        int spent = _essence - amount;
        _essence = amount;
        if (spent > 0 && _refundRatio > 0f)
        {
            int refund = Mathf.FloorToInt(spent * _refundRatio);
            // Rendue comme un butin, après la dépense : elle vole du joueur vers le compteur. Différée, pour que les
            // autres abonnés reçoivent la dépense avant le remboursement.
            if (refund > 0)
            {
                if (_pendingRefund == 0)
                    Callable.From(FlushRefund).CallDeferred();
                _pendingRefund += refund;
            }
        }
        RefreshEssenceBonus();
    }

    private void FlushRefund()
    {
        int amount = _pendingRefund;
        _pendingRefund = 0;
        if (amount <= 0 || _player == null || _player.IsDead)
            return;
        _eventBus.EmitSignal(EventBus.SignalName.LootReceived, "essence", EssenceRefundEffect, amount);
        _eventBus.EmitSignal(EventBus.SignalName.EssenceGained, amount, _player.GlobalPosition);
    }

    private void RefreshEssenceBonus()
    {
        if (_essenceCap <= 0f || _essencePerStep <= 0f)
            return;
        float bonus = Mathf.Min(_essenceCap, Mathf.Floor(_essence / _essencePerStep) * _damagePerStep);
        // Une étincelle d'Essence chaque fois que le bonus franchit une tranche de 5 %.
        if (Mathf.FloorToInt(bonus * 20f) > Mathf.FloorToInt(_essenceBonus * 20f))
        {
            Sparks(FxFamily.Essence, 5);
            _player.ObjectProcs?.Show(EssenceDamageCapStat);
        }
        _essenceBonus = bonus;
    }

    private void Sparks(FxFamily family, int count)
    {
        if (_player == null)
            return;
        CombatPools.Instance?.EmitSparks(_player.GlobalPosition + new Vector2(0f, -SparkHeight), new SparkBurst
        {
            Family = family,
            Owner = FxOwner.Player,
            Count = count,
            Direction = Vector2.Up,
            Spread = 2.5f,
            SpeedMin = 20f,
            SpeedMax = 50f,
            LifeMin = 0.25f,
            LifeMax = 0.4f,
            Size = 1,
        });
    }

    private static float Parameter(PassiveSouvenirData data, string name)
    {
        if (data.Parameters.TryGetValue(name, out float value))
            return value;
        GD.PushError($"[ObjectStances] Paramètre {name} absent de l'objet {data.Id}.");
        return 0f;
    }
}
