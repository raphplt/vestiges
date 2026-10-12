using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>Bonus conditionnels du sac, additifs entre copies et sans paliers. Les conditions coûteuses se relisent dix fois par seconde.</summary>
public partial class ObjectStances : Node
{
    public const string StillDamageStat = "still_damage";
    public const string CrowdDamageStat = "crowd_damage";
    public const string HighHpDamageStat = "high_hp_damage";
    public const string LowHpDamageStat = "low_hp_damage";
    public const string EssenceDamageCapStat = "essence_damage_cap";
    private const float CheckSeconds = .1f;
    private const float MovingSpeedSq = 25f;
    private Player _player;
    private EventBus _eventBus;
    private float _clock, _stillRate, _stillElapsed, _stillCap, _stillBonus;
    private float _crowdDamage, _crowdRadius;
    private int _crowdMax, _crowdCount;
    private float _highHpDamage, _highHpThreshold, _lowHpDamage, _lowHpThreshold;
    private bool _highHp, _lowHp;
    private float _essenceCap, _essencePerStep, _damagePerStep, _essenceBonus;
    private int _essence;
    public void Initialize(Player player) => _player = player;
    public float DamageMultiplier => 1f + _stillBonus + _crowdDamage * _crowdCount
        + (_highHp ? _highHpDamage : 0f) + (_lowHp ? _lowHpDamage : 0f) + _essenceBonus;
    public bool IsStill => _stillBonus > 0f;
    public float StillDamageBonus => _stillBonus;
    public int CrowdCount => _crowdCount;
    public static bool IsStanceStat(string stat) => stat is StillDamageStat or CrowdDamageStat or HighHpDamageStat or LowHpDamageStat or EssenceDamageCapStat;
    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EssenceChanged += OnEssenceChanged;
        _eventBus.PlayerDashStarted += OnDashStarted;
        if (GetNodeOrNull<EssenceTracker>("/root/Main/EssenceTracker") is { } tracker)
            _essence = tracker.CurrentEssence;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null) return;
        _eventBus.EssenceChanged -= OnEssenceChanged;
        _eventBus.PlayerDashStarted -= OnDashStarted;
    }

    public void Configure(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
            switch (effect.Stat)
            {
                case StillDamageStat:
                    _stillCap = data.Parameters["damage_cap"];
                    break;
                case CrowdDamageStat:
                    _crowdRadius = data.Parameters["radius"];
                    _crowdMax = Mathf.RoundToInt(data.Parameters["max_enemies"]);
                    break;
                case HighHpDamageStat:
                    _highHpThreshold = data.Parameters["threshold"];
                    break;
                case LowHpDamageStat:
                    _lowHpThreshold = data.Parameters["threshold"];
                    break;
                case EssenceDamageCapStat:
                    _essencePerStep = data.Parameters["essence_per_step"];
                    _damagePerStep = data.Parameters["damage_per_step"];
                    break;
            }

    }
    public void Add(string stat, float change)
    {
        switch (stat)
        {
            case StillDamageStat:
                _stillRate = ObjectTriggers.Settle(_stillRate + change);
                if (_stillRate <= 0f) _stillElapsed = 0f;
                _stillBonus = Mathf.Min(_stillCap, _stillElapsed * _stillRate);
                break;
            case CrowdDamageStat:
                _crowdDamage = ObjectTriggers.Settle(_crowdDamage + change);
                break;
            case HighHpDamageStat:
                _highHpDamage = ObjectTriggers.Settle(_highHpDamage + change);
                break;
            case LowHpDamageStat:
                _lowHpDamage = ObjectTriggers.Settle(_lowHpDamage + change);
                break;
            case EssenceDamageCapStat:
                _essenceCap = ObjectTriggers.Settle(_essenceCap + change);
                RefreshEssenceBonus();
                break;
        }

        RefreshHpConditions();
    }

    private void OnDashStarted(PlayerDashEvent dash)
    {
        if (dash.PlayerId == _player.GetInstanceId())
        {
            _stillElapsed = 0f;
            _stillBonus = 0f;
        }

    }
    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_stillRate > 0f)
        {
            bool moving = _player.IsDead || _player.Mobility.IsDashing || _player.Mobility.IsDashStep || _player.Velocity.LengthSquared() >= MovingSpeedSq;
            _stillElapsed = moving ? 0f : _stillElapsed + dt;
            float bonus = Mathf.Min(_stillCap, _stillElapsed * _stillRate);
            if (bonus >= _stillCap && _stillBonus < _stillCap) _player.ObjectProcs?.Show(StillDamageStat);
            _stillBonus = bonus;
        }

        _clock -= dt;
        if (_clock > 0f) return;
        _clock = CheckSeconds;
        if (_crowdDamage > 0f) CountCrowd();
        RefreshHpConditions();
    }

    private void RefreshHpConditions()
    {
        float ratio = _player.EffectiveMaxHp > 0f ? _player.CurrentHp / _player.EffectiveMaxHp : 0f;
        _highHp = ratio >= _highHpThreshold;
        _lowHp = ratio > 0f && ratio < _lowHpThreshold;
    }

    private void CountCrowd()
    {
        int count = 0;
        using CrowdQuery crowd = CrowdIndex.Near(_player.GlobalPosition, _crowdRadius);
        foreach (Node node in crowd.Targets)
            if (node is Enemy { IsActive: true, IsDying: false } enemy
                && Iso.GroundDistanceSquared(enemy.GlobalPosition, _player.GlobalPosition) <= _crowdRadius * _crowdRadius && ++count >= _crowdMax) break;
        if (count >= _crowdMax && _crowdCount < _crowdMax) _player.ObjectProcs?.Show(CrowdDamageStat);
        _crowdCount = count;
    }

    private void OnEssenceChanged(int amount)
    {
        _essence = amount;
        RefreshEssenceBonus();
    }

    private void RefreshEssenceBonus() => _essenceBonus = _essenceCap > 0f && _essencePerStep > 0f
        ? Mathf.Min(_essenceCap, Mathf.Floor(_essence / _essencePerStep) * _damagePerStep) : 0f;
}
