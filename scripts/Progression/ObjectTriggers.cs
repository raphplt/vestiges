using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>Effets d’impact du sac. Chaque copie renforce un seul axe ; les budgets et délais survivent aux échanges.</summary>
public partial class ObjectTriggers : Node
{
    public const string BurnChanceStat = "burn_chance";
    public const string ChillChanceStat = "chill_chance";
    public const string BurningTargetDamageStat = "burning_target_damage";
    public const string SlowedTargetDamageStat = "slowed_target_damage";
    public const string KillExplosionStat = "kill_explosion";
    public const string KillHealStat = "kill_heal";
    public const string StrideDamageStat = "stride_damage";
    public const string LevelHealStat = "level_heal";
    public const string CritFragileStat = "crit_fragile";
    public const string CritEchoStat = "crit_echo";
    public const string DashTrailStat = "dash_trail";
    public const string PoisonStat = "poison";
    public const string EliteSlowStat = "elite_slow";
    private const float TrailTickSeconds = .25f;
    private const float SparkHeight = 12f;
    private struct TrailPatch
    {
        public Vector2 Position;
        public float Remaining;
    }

    private sealed class AttemptHistory
    {
        public Enemy Target;
        public readonly HashSet<ulong> Launches = new();
    }

    private readonly record struct TimedAmount(float Time, float Amount);
    private sealed class Budget
    {
        private readonly Queue<TimedAmount> _spent = new();
        private float _total;
        public float Take(float now, float window, float capacity, float wanted)
        {
            while (_spent.TryPeek(out TimedAmount first) && first.Time <= now - window + .00001f)
                _total -= _spent.Dequeue().Amount;
            float taken = Mathf.Min(wanted, Mathf.Max(0f, capacity - _total));
            if (taken > 0f)
            {
                _spent.Enqueue(new(now, taken));
                _total += taken;
            }

            return taken;
        }

    }
    private readonly Budget _burnBudget = new(), _chillBudget = new(), _poisonBudget = new(), _eliteBudget = new(), _healBudget = new();
    private readonly Dictionary<EnemyLife, AttemptHistory> _attempts = new();
    private readonly Stack<AttemptHistory> _attemptPool = new();
    private readonly Dictionary<EnemyLife, float> _chillReady = new(), _eliteReady = new();
    private readonly List<EnemyLife> _expiredLives = new();
    private readonly List<Enemy> _echoTargets = new();
    private readonly List<TrailPatch> _trailPatches = new();
    private Player _player;
    private ulong _playerId;
    private EventBus _eventBus;
    private float _clock, _cleanupAt;
    private float _burnCopies, _chillCopies, _poisonCopies, _eliteCopies;
    private float _burnChance, _burnDamageRatio, _burnSeconds, _burnRate;
    private float _chillChance, _chillFactor = 1f, _chillSeconds, _chillRate, _chillRest, _eliteChillRest;
    private float _poisonChance, _poisonRatio, _poisonSeconds, _poisonRate, _poisonDoses;
    private float _eliteChance, _eliteFactor, _eliteSeconds, _eliteMaxSeconds, _eliteWindow, _eliteRest;
    private float _burningTargetDamage, _slowedTargetDamage, _killExplosion, _explosionRadius, _killHeal;
    private float _strideDamage, _strideSeconds, _strideExpires;
    private int _strideCharges;
    private float _healPerKill;
    private float _levelHeal, _critFragile, _critFragileSeconds, _critEcho, _critEchoRadius;
    private float _dashTrail, _trailBaseSeconds, _trailMaxSeconds, _trailSlowFactor, _trailRadius, _trailSpacing, _trailTick;
    private Vector2 _lastPatch;
    private bool _trailDashActive;
    private AttackContext _trailSource;
    internal RandomNumberGenerator Rng { get; } = new();
    public float BurnChance => _burnCopies > 0f ? _burnChance : 0f;
    public float ChillChance => _chillCopies > 0f ? _chillChance : 0f;
    public int StrideCharges => _strideCharges;
    public static bool IsTriggerStat(string stat) => stat is BurnChanceStat or ChillChanceStat or BurningTargetDamageStat
        or SlowedTargetDamageStat or KillExplosionStat or KillHealStat or StrideDamageStat or LevelHealStat
        or CritFragileStat or CritEchoStat or DashTrailStat or PoisonStat or EliteSlowStat;
    public void Initialize(Player player)
    {
        _player = player;
        _playerId = player.GetInstanceId();
        Rng.Seed = RunRandom.SeedFor("object_triggers");
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKillResolved += OnEnemyKill;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.PlayerDashStarted += OnDashStarted;
        _eventBus.PlayerDashMoved += OnDashMoved;
        _eventBus.PlayerDashFinished += OnDashFinished;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null) return;
        _eventBus.EnemyKillResolved -= OnEnemyKill;
        _eventBus.LevelUp -= OnLevelUp;
        _eventBus.PlayerDashStarted -= OnDashStarted;
        _eventBus.PlayerDashMoved -= OnDashMoved;
        _eventBus.PlayerDashFinished -= OnDashFinished;
    }

    public override void _Process(double delta) => AdvanceClock((float)delta);
    /// <summary>Horloge de jeu (la pause la suspend), jamais l’horloge réelle. Ne repart pas à zéro au rééquipement.</summary>
    internal void AdvanceClock(float delta)
    {
        _clock += delta;
        if (_strideCharges > 0 && _clock >= _strideExpires) _strideCharges = 0;
        if (_player.IsDead)
        {
            _strideCharges = 0;
            _trailPatches.Clear();
            return;
        }

        AdvanceTrail(delta);
        if (_clock < _cleanupAt) return;
        _cleanupAt = _clock + 1f;
        _expiredLives.Clear();
        foreach (KeyValuePair<EnemyLife, AttemptHistory> pair in _attempts)
            if (!GodotObject.IsInstanceValid(pair.Value.Target) || !pair.Value.Target.IsActive
                || pair.Value.Target.IsDying || pair.Value.Target.Life != pair.Key)
                _expiredLives.Add(pair.Key);
        foreach (EnemyLife life in _expiredLives) ForgetAttempts(life);
        CleanReady(_chillReady);
        CleanReady(_eliteReady);
    }

    private void CleanReady(Dictionary<EnemyLife, float> ready)
    {
        _expiredLives.Clear();
        foreach (KeyValuePair<EnemyLife, float> pair in ready)
            if (pair.Value <= _clock) _expiredLives.Add(pair.Key);
        foreach (EnemyLife key in _expiredLives) ready.Remove(key);
    }

    public void Configure(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
        {
            switch (effect.Stat)
            {
                case BurnChanceStat:
                    _burnChance = Parameter(data, "chance");
                    _burnDamageRatio = Parameter(data, "burn_damage_ratio");
                    _burnSeconds = Parameter(data, "burn_seconds");
                    _burnRate = Parameter(data, "applications_per_second");
                    break;
                case ChillChanceStat:
                    _chillChance = Parameter(data, "chance");
                    _chillFactor = Parameter(data, "slow_factor");
                    _chillSeconds = Parameter(data, "slow_seconds");
                    _chillRate = Parameter(data, "applications_per_second");
                    _chillRest = Parameter(data, "reprieve_seconds");
                    _eliteChillRest = Parameter(data, "elite_reprieve_seconds");
                    break;
                case PoisonStat:
                    _poisonChance = Parameter(data, "chance");
                    _poisonRatio = Parameter(data, "damage_ratio");
                    _poisonSeconds = Parameter(data, "seconds");
                    _poisonRate = Parameter(data, "applications_per_second");
                    _poisonDoses = Parameter(data, "doses_per_copy");
                    break;
                case EliteSlowStat:
                    _eliteChance = Parameter(data, "chance");
                    _eliteFactor = Parameter(data, "slow_factor");
                    _eliteSeconds = Parameter(data, "seconds");
                    _eliteMaxSeconds = Parameter(data, "max_seconds");
                    _eliteWindow = Parameter(data, "window_seconds");
                    _eliteRest = Parameter(data, "target_cooldown_seconds");
                    break;
                case KillHealStat:
                    _healPerKill = Parameter(data, "heal_per_kill");
                    break;
                case KillExplosionStat:
                    _explosionRadius = Parameter(data, "radius");
                    break;
                case StrideDamageStat:
                    _strideSeconds = Parameter(data, "charge_seconds");
                    break;
                case CritFragileStat:
                    _critFragileSeconds = Parameter(data, "fragile_seconds");
                    break;
                case CritEchoStat:
                    _critEchoRadius = Parameter(data, "radius");
                    break;
                case DashTrailStat:
                    _trailBaseSeconds = Parameter(data, "base_seconds");
                    _trailMaxSeconds = Parameter(data, "max_seconds");
                    _trailSlowFactor = Parameter(data, "slow_factor");
                    _trailRadius = Parameter(data, "radius");
                    _trailSpacing = Mathf.Max(4f, Parameter(data, "spacing"));
                    break;
            }

        }

    }
    public void Add(string stat, float change)
    {
        switch (stat)
        {
            case BurnChanceStat:
                _burnCopies = Settle(_burnCopies + change);
                break;
            case ChillChanceStat:
                _chillCopies = Settle(_chillCopies + change);
                break;
            case PoisonStat:
                _poisonCopies = Settle(_poisonCopies + change);
                break;
            case EliteSlowStat:
                _eliteCopies = Settle(_eliteCopies + change);
                break;
            case BurningTargetDamageStat:
                _burningTargetDamage = Settle(_burningTargetDamage + change);
                break;
            case SlowedTargetDamageStat:
                _slowedTargetDamage = Settle(_slowedTargetDamage + change);
                break;
            case KillExplosionStat:
                _killExplosion = Settle(_killExplosion + change);
                break;
            case KillHealStat:
                _killHeal = Settle(_killHeal + change);
                break;
            case StrideDamageStat:
                _strideDamage = Settle(_strideDamage + change);
                if (_strideDamage <= 0f) _strideCharges = 0;
                break;
            case LevelHealStat:
                _levelHeal = Settle(_levelHeal + change);
                break;
            case CritFragileStat:
                _critFragile = Settle(_critFragile + change);
                break;
            case CritEchoStat:
                _critEcho = Settle(_critEcho + change);
                break;
            case DashTrailStat:
                _dashTrail = Settle(_dashTrail + change);
                if (_dashTrail <= 0f)
                {
                    _trailPatches.Clear();
                    _trailDashActive = false;
                }

                break;
        }

    }
    internal static float Settle(float value) => Mathf.Abs(value) < .00001f ? 0f : value;
    public float AgainstTarget(Enemy enemy, float damage)
    {
        float bonus = 0f;
        if (_burningTargetDamage > 0f && enemy.IsBurning)
        {
            bonus += _burningTargetDamage;
            _player.ObjectProcs?.Show(BurningTargetDamageStat);
        }

        if (_slowedTargetDamage > 0f && enemy.IsHindered)
        {
            bonus += _slowedTargetDamage;
            _player.ObjectProcs?.Show(SlowedTargetDamageStat);
        }

        return damage * (1f + bonus);
    }

    public void OnWeaponImpact(Enemy enemy, float hitDamage, WeaponInstance weapon, int hits, AttackContext context,
        bool isCrit = false, float dealt = 0f)
    {
        if (!context.IsDirectWeapon || context.OwnerId != _playerId || !GodotObject.IsInstanceValid(enemy)) return;
        if (isCrit) OnCrit(enemy, dealt, context);
        if (enemy.IsDying || (_burnCopies <= 0f && _chillCopies <= 0f && _poisonCopies <= 0f && _eliteCopies <= 0f)) return;
        if (!_attempts.TryGetValue(enemy.Life, out AttemptHistory history))
        {
            history = _attemptPool.TryPop(out AttemptHistory reused) ? reused : new AttemptHistory();
            history.Target = enemy;
            _attempts.Add(enemy.Life, history);
        }

        if (!history.Launches.Add(context.LaunchId)) return;
        float coefficient = Mathf.Clamp(weapon?.Base.TriggerCoefficient ?? 1f, 0f, 1f);
        if (_burnCopies > 0f && Roll(_burnChance * coefficient) && _burnBudget.Take(_clock, 1f, _burnRate * _burnCopies, 1f) >= 1f)
        {
            enemy.ApplyIgnite(hitDamage * _burnDamageRatio, _burnSeconds * _player.StatusDurationMultiplier, context.As(DamageKind.Passive));
            Spark(enemy, FxFamily.Fire);
            _player.ObjectProcs?.Show(BurnChanceStat);
        }

        bool elite = enemy.IsPriorityTarget || enemy.Tier is EnemyTier.Elite or EnemyTier.Miniboss or EnemyTier.Boss;
        if (_chillCopies > 0f && (!_chillReady.TryGetValue(enemy.Life, out float chillReady) || chillReady <= _clock) && Roll(_chillChance * coefficient)
            && _chillBudget.Take(_clock, 1f, _chillRate * _chillCopies, 1f) >= 1f)
        {
            float seconds = _chillSeconds * _player.StatusDurationMultiplier;
            enemy.ApplySlow(_chillFactor, seconds, context.As(DamageKind.Passive), ControlOrigin.Unknown);
            _chillReady[enemy.Life] = _clock + seconds + (elite ? _eliteChillRest : _chillRest);
            Spark(enemy, FxFamily.Pale);
            _player.ObjectProcs?.Show(ChillChanceStat);
        }

        int doses = Mathf.Max(0, Mathf.FloorToInt(_poisonDoses * _poisonCopies));
        if (_poisonCopies > 0f && enemy.PoisonDoseCount < doses && Roll(_poisonChance * coefficient)
            && _poisonBudget.Take(_clock, 1f, _poisonRate * _poisonCopies, 1f) >= 1f)
        {
            enemy.ApplyPoison(hitDamage * _poisonRatio, _poisonSeconds * _player.StatusDurationMultiplier, context.As(DamageKind.Passive), doses);
            _player.ObjectProcs?.Show(PoisonStat);
        }

        if (_eliteCopies > 0f && elite && (!_eliteReady.TryGetValue(enemy.Life, out float eliteReady) || eliteReady <= _clock) && Roll(_eliteChance * coefficient)
            && _eliteBudget.Take(_clock, _eliteWindow, _eliteCopies, 1f) >= 1f)
        {
            enemy.ApplySlow(_eliteFactor, Mathf.Min(_eliteMaxSeconds, _eliteSeconds * _player.StatusDurationMultiplier), context.As(DamageKind.Passive), ControlOrigin.Unknown);
            _eliteReady[enemy.Life] = _clock + _eliteRest;
            Spark(enemy, FxFamily.Glass);
            _player.ObjectProcs?.Show(EliteSlowStat);
        }

    }
    private bool Roll(float chance) => Rng.Randf() < chance;
    private void OnCrit(Enemy enemy, float dealt, AttackContext context)
    {
        if (_critFragile > 0f && !enemy.IsDying)
        {
            enemy.ApplyFragile(_critFragile, _critFragileSeconds * _player.StatusDurationMultiplier, context.As(DamageKind.Passive));
            Spark(enemy, FxFamily.Glass);
            _player.ObjectProcs?.Show(CritFragileStat);
        }

        if (_critEcho <= 0f || dealt <= 0f) return;
        NearestOthers(enemy, _critEchoRadius, 1, _echoTargets);
        foreach (Enemy target in _echoTargets)
        {
            target.TakeDamage(dealt * _critEcho, source: context.As(DamageKind.Passive));
            _player.AttackFx.PlayBeam(enemy.GlobalPosition, target.GlobalPosition, FxFamily.Crit);
        }

        if (_echoTargets.Count > 0) _player.ObjectProcs?.Show(CritEchoStat);
    }

    private void OnDashStarted(PlayerDashEvent dash)
    {
        if (dash.PlayerId != _playerId) return;
        _trailDashActive = _dashTrail > 0f;
        if (!_trailDashActive) return;
        _trailSource = _player.BeginAttack(null, 0f, DamageKind.Passive);
        LayPatch(dash.From);
    }

    private void OnDashMoved(PlayerDashEvent dash)
    {
        if (dash.PlayerId != _playerId || !_trailDashActive || _dashTrail <= 0f) return;
        while (_lastPatch.DistanceSquaredTo(dash.To) >= _trailSpacing * _trailSpacing)
            LayPatch(_lastPatch + (dash.To - _lastPatch).Normalized() * _trailSpacing);
    }

    private void OnDashFinished(PlayerDashEvent dash)
    {
        if (dash.PlayerId != _playerId) return;
        _trailDashActive = false;
        if (_strideDamage <= 0f || _player.IsDead) return;
        _strideCharges = 1;
        _strideExpires = _clock + _strideSeconds;
    }

    /// <summary>Trajet instantané public pour les bancs ; en jeu le même chemin vient des événements communs du dash.</summary>
    public void OnBlink(Vector2 from, Vector2 to)
    {
        PlayerDashEvent dash = new(_playerId, from, to, true);
        OnDashStarted(dash);
        OnDashMoved(dash);
    }

    private void LayPatch(Vector2 position)
    {
        float seconds = Mathf.Min(_trailMaxSeconds, _trailBaseSeconds * _dashTrail * _player.StatusDurationMultiplier);
        _trailPatches.Add(new TrailPatch
        {
            Position = position,
            Remaining = seconds
        });
        _lastPatch = position;
        _player.AttackFx.PlayTrail(position, _trailRadius, seconds);
        _player.ObjectProcs?.Show(DashTrailStat);
    }

    private void AdvanceTrail(float delta)
    {
        _trailTick -= delta;
        bool tick = _trailTick <= 0f;
        if (tick) _trailTick = TrailTickSeconds;
        for (int i = _trailPatches.Count - 1; i >= 0; i--)
        {
            TrailPatch patch = _trailPatches[i];
            patch.Remaining -= delta;
            if (patch.Remaining <= 0f)
            {
                _trailPatches[i] = _trailPatches[^1];
                _trailPatches.RemoveAt(_trailPatches.Count - 1);
                continue;
            }

            _trailPatches[i] = patch;
            if (!tick) continue;
            using CrowdQuery crowd = CrowdIndex.Near(patch.Position, _trailRadius);
            foreach (Node node in crowd.Targets)
                if (node is Enemy { IsActive: true, IsDying: false } enemy && Iso.GroundDistanceSquared(enemy.GlobalPosition, patch.Position) <= _trailRadius * _trailRadius)
                    enemy.ApplySlow(_trailSlowFactor, Mathf.Min(patch.Remaining, TrailTickSeconds * 2f), _trailSource, ControlOrigin.Unknown);
        }

    }
    public float ConsumeStride()
    {
        if (_strideCharges <= 0) return 1f;
        _strideCharges = 0;
        _player.ObjectProcs?.Show(StrideDamageStat);
        return 1f + _strideDamage;
    }

    private void OnLevelUp(int level)
    {
        if (_levelHeal <= 0f || _player.IsDead) return;
        _player.Heal(_player.EffectiveMaxHp * _levelHeal);
        _player.ObjectProcs?.Show(LevelHealStat);
    }

    private void ForgetAttempts(EnemyLife life)
    {
        if (!_attempts.Remove(life, out AttemptHistory history)) return;
        history.Target = null;
        history.Launches.Clear();
        _attemptPool.Push(history);
    }

    private void OnEnemyKill(EnemyKillResult kill)
    {
        ForgetAttempts(kill.Target);
        if (kill.Damage.Source.OwnerId != _playerId || !kill.Damage.Source.IsWeaponWork || _player.IsDead) return;
        if (_killExplosion > 0f) Explode(kill.Position, FatalBlow(kill.Damage) * _killExplosion, kill.Damage.Source.As(DamageKind.Passive));
        if (_killHeal > 0f && _player.CurrentHp < _player.EffectiveMaxHp)
        {
            float heal = _healBudget.Take(_clock, 1f, _killHeal, Mathf.Min(_healPerKill, _player.EffectiveMaxHp - _player.CurrentHp));
            if (heal > 0f)
            {
                _player.Heal(heal);
                _player.ObjectProcs?.Show(KillHealStat);
            }

        }

    }
    private static float FatalBlow(in DamageResult damage) => damage.Source.Kind == DamageKind.DamageOverTime
        ? Mathf.Max(damage.NativeDamage, damage.Source.ReferenceDamage) : damage.NativeDamage;
    private void NearestOthers(Enemy from, float radius, int count, System.Collections.Generic.List<Enemy> result)
    {
        result.Clear();
        float radiusSq = radius * radius;
        using CrowdQuery crowd = CrowdIndex.Near(from.GlobalPosition, radius);
        foreach (Node node in crowd.Targets)
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy || enemy == from)
                continue;
            float distanceSq = Iso.GroundDistanceSquared(enemy.GlobalPosition, from.GlobalPosition);
            if (distanceSq > radiusSq)
                continue;
            int index = result.Count;
            while (index > 0 && Iso.GroundDistanceSquared(result[index - 1].GlobalPosition, from.GlobalPosition) > distanceSq)
                index--;
            if (index >= count)
                continue;
            result.Insert(index, enemy);
            if (result.Count > count)
                result.RemoveAt(result.Count - 1);
        }

    }
    private void Explode(Vector2 position, float damage, AttackContext source)
    {
        float radius = _explosionRadius * _player.AoeMultiplier;
        float radiusSq = radius * radius;
        using CrowdQuery crowd = CrowdIndex.Near(position, radius);
        foreach (Node node in crowd.Targets)
        {
            if (node is Enemy { IsActive: true, IsDying: false } enemy
                && Iso.GroundDistanceSquared(enemy.GlobalPosition, position) <= radiusSq)
            {
                enemy.TakeDamage(damage, source: source);
                _player.AttackFx.PlayHit(FxFamily.Fire, enemy.GlobalPosition, false, position);
            }

        }

        _player.AttackFx.PlayBurst(position, FxFamily.Fire, radius);
        _player.ObjectProcs?.Show(KillExplosionStat);
    }

    private static void Spark(Enemy enemy, FxFamily family)
    {
        CombatPools.Instance?.EmitSparks(enemy.GlobalPosition + new Vector2(0f, -SparkHeight), new SparkBurst
        {
            Family = family,
            Owner = FxOwner.Player,
            Count = 3,
            Direction = Vector2.Up,
            Spread = 1.2f,
            SpeedMin = 20f,
            SpeedMax = 50f,
            LifeMin = 0.15f,
            LifeMax = 0.25f,
            Size = 1,
        });
    }

    private static float Parameter(PassiveSouvenirData data, string name)
    {
        if (data.Parameters.TryGetValue(name, out float value))
            return value;
        GD.PushError($"[ObjectTriggers] Paramètre {name} absent de l'objet {data.Id}.");
        return 0f;
    }
}
