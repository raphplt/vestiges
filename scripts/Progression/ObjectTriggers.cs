using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Objets de déclencheur d'un joueur (plan 21 §4) : ce qui se passe à l'impact d'une arme, contre une cible déjà
/// touchée par un statut, à l'élimination, après une marche, au niveau gagné. Créé au premier objet de ce type ; il ne
/// tourne par frame que pour la Semelle usée ou une explosion en attente. Règles communes (§7) :
/// <list type="bullet">
/// <item>seul ce qu'une arme a fait déclenche (coup direct, écho, forme, effet sur la durée qu'elle a posé), jamais
/// ce qu'un objet a produit (DECISIONS §70) ;</item>
/// <item>une chance est multipliée par le coefficient de déclenchement de l'arme ;</item>
/// <item>une chance plafonne à 100 %, l'excédent renforce l'effet.</item>
/// </list>
/// </summary>
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

    public const string BurnSpreadEffect = "burn_spread";
    public const string DoubleSlowFreezeEffect = "double_slow_freeze";
    public const string BurnSlowsEffect = "burn_slows";
    public const string SlowKillExtendsEffect = "slow_kill_extends";
    public const string DoubleExplosionEffect = "double_explosion";
    public const string EliteKillHealEffect = "elite_kill_heal";
    public const string DoubleStrideEffect = "double_stride";
    public const string CascadeInvulnerabilityEffect = "cascade_invulnerability";
    public const string LongFragileEffect = "long_fragile";
    public const string DoubleCritEchoEffect = "double_crit_echo";
    public const string StickyFragileEffect = "sticky_fragile";
    private const float TrailTickSeconds = 0.25f;

    private const float SparkHeight = 12f;
    private const float MovingSpeedSq = 25f;

    private struct TrailPatch
    {
        public Vector2 Position;
        public float Remaining;
    }

    private struct PendingExplosion
    {
        public Vector2 Position;
        public float Damage;
        public float Remaining;
        public AttackContext Source;
    }

    private Player _player;
    private ulong _playerId;
    private EventBus _eventBus;
    private GroupCache _groupCache;

    private float _burnChance;
    private float _burnDamageRatio;
    private float _burnSeconds;
    private float _chillChance;
    private float _chillFactor = 1f;
    private float _chillSeconds;
    private float _burningTargetDamage;
    private float _slowedTargetDamage;

    private float _burnSpreadRadius;
    private float _freezeSeconds;
    private float _burnSlowFactor = 1f;
    private float _slowKillRadius;
    private float _slowKillSeconds;
    private float _slowKillMaxSeconds;

    private float _killExplosion;
    private float _explosionRadius;
    private float _secondExplosionDelay = -1f;
    private readonly System.Collections.Generic.List<PendingExplosion> _pendingExplosions = new();
    private float _killHeal;
    private float _eliteHealRatio;
    private float _strideDamage;
    private float _strideSeconds;
    private int _strideMaxCharges = 1;
    private float _strideElapsed;
    private int _strideCharges;
    private float _levelHeal;
    private int _cascadeLevels;
    private float _cascadeSeconds;
    private ulong _levelFrame;
    private int _levelsThisFrame;

    // Loupe de philatéliste, Stylo à quatre couleurs : ce qu'un coup critique déclenche.
    private float _critFragile;
    private float _critFragileSeconds;
    private float _critEcho;
    private float _critEchoRadius;
    private int _critEchoTargets = 1;
    private readonly System.Collections.Generic.List<Enemy> _echoTargets = new();
    // Chewing-gum : traînée collante du dash, taches au sol qui ralentissent.
    private float _dashTrail;
    private float _trailBaseSeconds;
    private float _trailSlowFactor = 1f;
    private float _trailRadius;
    private float _trailSpacing;
    private float _trailFragile;
    private float _trailTick;
    private Vector2 _lastPatch;
    private bool _trailDashing;
    // Source des statuts posés par la traînée : le joueur, pour que la Pince à linge et la Propagation les reconnaissent.
    private AttackContext _trailSource;
    private readonly System.Collections.Generic.List<TrailPatch> _trailPatches = new();

    /// <summary>Tirages des déclencheurs ; les bancs le graine pour rester déterministes.</summary>
    internal RandomNumberGenerator Rng { get; } = new();

    public float BurnChance => _burnChance;
    public float ChillChance => _chillChance;
    public int StrideCharges => _strideCharges;

    public static bool IsTriggerStat(string stat) =>
        stat is BurnChanceStat or ChillChanceStat or BurningTargetDamageStat or SlowedTargetDamageStat
            or KillExplosionStat or KillHealStat or StrideDamageStat or LevelHealStat
            or CritFragileStat or CritEchoStat or DashTrailStat;

    public static bool IsTriggerMilestone(string effect) =>
        effect is BurnSpreadEffect or DoubleSlowFreezeEffect or BurnSlowsEffect or SlowKillExtendsEffect
            or DoubleExplosionEffect or EliteKillHealEffect or DoubleStrideEffect or CascadeInvulnerabilityEffect
            or LongFragileEffect or DoubleCritEchoEffect or StickyFragileEffect;

    public void Initialize(Player player)
    {
        _player = player;
        _playerId = player.GetInstanceId();
        Rng.Seed = RunRandom.SeedFor("object_triggers");
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
        _eventBus.EnemyKillResolved += OnEnemyKill;
        _eventBus.LevelUp += OnLevelUp;
        SetProcess(NeedsClock);
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.EnemyKillResolved -= OnEnemyKill;
        _eventBus.LevelUp -= OnLevelUp;
    }

    private bool NeedsClock => _strideDamage > 0f || _pendingExplosions.Count > 0 || _dashTrail > 0f;

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_player.IsDead)
        {
            _pendingExplosions.Clear();
            SetProcess(false);
            return;
        }
        AdvanceStride(dt);
        AdvanceTrail(dt);
        for (int i = _pendingExplosions.Count - 1; i >= 0; i--)
        {
            PendingExplosion explosion = _pendingExplosions[i];
            explosion.Remaining -= dt;
            if (explosion.Remaining > 0f)
            {
                _pendingExplosions[i] = explosion;
                continue;
            }
            _pendingExplosions[i] = _pendingExplosions[^1];
            _pendingExplosions.RemoveAt(_pendingExplosions.Count - 1);
            Explode(explosion.Position, explosion.Damage, explosion.Source);
        }
        SetProcess(NeedsClock);
    }

    /// <summary>Réglages d'un objet de déclencheur, lus à son arrivée.</summary>
    public void Configure(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
        {
            switch (effect.Stat)
            {
                case BurnChanceStat:
                    _burnDamageRatio = Parameter(data, "burn_damage_ratio");
                    _burnSeconds = Parameter(data, "burn_seconds");
                    break;
                case ChillChanceStat:
                    _chillFactor = Parameter(data, "slow_factor");
                    _chillSeconds = Parameter(data, "slow_seconds");
                    break;
                case KillExplosionStat:
                    _explosionRadius = Parameter(data, "radius");
                    break;
                case StrideDamageStat:
                    _strideSeconds = Parameter(data, "move_seconds");
                    SetProcess(true);
                    break;
                case CritFragileStat:
                    _critFragileSeconds = Parameter(data, "fragile_seconds");
                    break;
                case CritEchoStat:
                    _critEchoRadius = Parameter(data, "radius");
                    break;
                case DashTrailStat:
                    _trailBaseSeconds = Parameter(data, "base_seconds");
                    _trailSlowFactor = Parameter(data, "slow_factor");
                    _trailRadius = Parameter(data, "radius");
                    // Un écart nul ferait poser des taches sans fin.
                    _trailSpacing = Mathf.Max(4f, Parameter(data, "spacing"));
                    SetProcess(true);
                    break;
            }
        }
    }

    /// <summary>Écart de valeur d'un effet d'objet de déclencheur (formule additive du niveau).</summary>
    public void Add(string stat, float change)
    {
        switch (stat)
        {
            case BurnChanceStat: _burnChance += change; break;
            case ChillChanceStat: _chillChance += change; break;
            case BurningTargetDamageStat: _burningTargetDamage += change; break;
            case SlowedTargetDamageStat: _slowedTargetDamage += change; break;
            case KillExplosionStat: _killExplosion += change; break;
            case KillHealStat: _killHeal += change; break;
            case StrideDamageStat: _strideDamage += change; break;
            case LevelHealStat: _levelHeal += change; break;
            case CritFragileStat: _critFragile += change; break;
            case CritEchoStat: _critEcho += change; break;
            case DashTrailStat: _dashTrail += change; break;
        }
    }

    /// <summary>Branche le palier d'un objet de déclencheur.</summary>
    public void Activate(ObjectMilestoneData milestone)
    {
        switch (milestone.Effect)
        {
            case BurnSpreadEffect: _burnSpreadRadius = milestone.Parameter("radius"); break;
            case DoubleSlowFreezeEffect: _freezeSeconds = milestone.Parameter("freeze_seconds"); break;
            case BurnSlowsEffect: _burnSlowFactor = milestone.Parameter("slow_factor"); break;
            case SlowKillExtendsEffect:
                _slowKillRadius = milestone.Parameter("radius");
                _slowKillSeconds = milestone.Parameter("seconds");
                _slowKillMaxSeconds = milestone.Parameter("max_remaining_seconds");
                break;
            case DoubleExplosionEffect: _secondExplosionDelay = milestone.Parameter("delay_seconds"); break;
            case EliteKillHealEffect: _eliteHealRatio = milestone.Parameter("max_hp_ratio"); break;
            case DoubleStrideEffect: _strideMaxCharges = Mathf.Max(1, Mathf.RoundToInt(milestone.Parameter("charges"))); break;
            case LongFragileEffect: _critFragileSeconds = milestone.Parameter("seconds"); break;
            case DoubleCritEchoEffect: _critEchoTargets = Mathf.Max(1, Mathf.RoundToInt(milestone.Parameter("targets"))); break;
            case StickyFragileEffect: _trailFragile = milestone.Parameter("bonus"); break;
            case CascadeInvulnerabilityEffect:
                _cascadeLevels = Mathf.Max(1, Mathf.RoundToInt(milestone.Parameter("levels")));
                _cascadeSeconds = milestone.Parameter("seconds");
                break;
        }
    }

    /// <summary>Thermomètre, Épingle à nourrice : dégâts d'un coup d'arme contre une cible brûlée ou entravée.</summary>
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

    /// <summary>
    /// Impact d'une arme, direct ou secondaire : Allumette humide et Glaçon tirent leur chance, pondérée par l'arme et
    /// par le nombre de frappes qui ont touché ensemble. <paramref name="hitDamage"/> est le coup de base de l'arme : un
    /// cône continu, qui touche par fractions de frame, brûle comme les autres armes.
    /// </summary>
    public void OnWeaponImpact(Enemy enemy, float hitDamage, WeaponInstance weapon, int hits, AttackContext context,
        bool isCrit = false, float dealt = 0f)
    {
        if (isCrit)
            OnCrit(enemy, dealt, context);
        if (enemy.IsDying)
            return;
        float coefficient = weapon?.Base.TriggerCoefficient ?? 1f;
        if (_burnChance > 0f && Roll(_burnChance * coefficient, hits, out float burnStrength))
            Burn(enemy, hitDamage * _burnDamageRatio * burnStrength, context);
        if (_chillChance > 0f && Roll(_chillChance * coefficient, hits, out float chillStrength))
            Chill(enemy, chillStrength, context);
    }

    /// <summary>
    /// Coup critique direct : la Loupe rend la cible Fragile ; le Stylo fait repartir une part des dégâts vers la
    /// ou les cibles les plus proches. Ce qui repart ne déclenche rien (plan 21 §7).
    /// </summary>
    private void OnCrit(Enemy enemy, float dealt, AttackContext context)
    {
        if (_critFragile > 0f && !enemy.IsDying)
        {
            enemy.ApplyFragile(_critFragile, _critFragileSeconds * _player.StatusDurationMultiplier, context);
            Spark(enemy, FxFamily.Glass);
            _player.ObjectProcs?.Show(CritFragileStat);
        }
        if (_critEcho <= 0f || dealt <= 0f)
            return;
        NearestOthers(enemy, _critEchoRadius, _critEchoTargets, _echoTargets);
        AttackContext echo = context.As(DamageKind.Passive);
        foreach (Enemy target in _echoTargets)
        {
            target.TakeDamage(dealt * _critEcho, source: echo);
            _player.AttackFx.PlayBeam(enemy.GlobalPosition, target.GlobalPosition, FxFamily.Crit);
            _player.AttackFx.PlayHit(FxFamily.Crit, target.GlobalPosition, false, enemy.GlobalPosition);
        }
        if (_echoTargets.Count > 0)
            _player.ObjectProcs?.Show(CritEchoStat);
    }

    private void LayPatch(Vector2 position)
    {
        float seconds = (_trailBaseSeconds + _dashTrail) * _player.StatusDurationMultiplier;
        _trailPatches.Add(new TrailPatch { Position = position, Remaining = seconds });
        _lastPatch = position;
        _player.AttackFx.PlayTrail(position, _trailRadius, seconds);
        _player.ObjectProcs?.Show(DashTrailStat);
    }

    /// <summary>Les <paramref name="count"/> ennemis actifs les plus proches de <paramref name="from"/>, à moins de <paramref name="radius"/>.</summary>
    private void NearestOthers(Enemy from, float radius, int count, System.Collections.Generic.List<Enemy> result)
    {
        result.Clear();
        float radiusSq = radius * radius;
        foreach (Node node in _groupCache.GetEnemies())
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

    /// <summary>
    /// Chewing-gum : pendant le dash, une tache collante tous les quelques pixels ; chaque tache ralentit (et rend
    /// Fragile au palier) les ennemis qui s'y trouvent, quatre fois par seconde, le temps qu'elle dure.
    /// </summary>
    private void AdvanceTrail(float delta)
    {
        if (_dashTrail <= 0f)
            return;
        bool dashing = _player.Mobility?.IsDashing == true;
        Vector2 position = _player.GlobalPosition;
        if (dashing && !_trailDashing)
        {
            _trailSource = _player.BeginAttack(null, 0f, DamageKind.Passive);
            LayPatch(position);
        }
        // Le long du trajet depuis la dernière tache, quelle que soit la cadence d'image : à faible fréquence, une
        // seule frame peut couvrir tout le dash.
        if (dashing || _trailDashing)
        {
            while (_lastPatch.DistanceSquaredTo(position) >= _trailSpacing * _trailSpacing)
                LayPatch(_lastPatch + (position - _lastPatch).Normalized() * _trailSpacing);
        }
        _trailDashing = dashing;
        if (_trailPatches.Count == 0)
            return;
        _trailTick -= delta;
        bool tick = _trailTick <= 0f;
        if (tick)
            _trailTick = TrailTickSeconds;
        float radiusSq = _trailRadius * _trailRadius;
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
            if (!tick)
                continue;
            foreach (Node node in _groupCache.GetEnemies())
            {
                if (node is not Enemy { IsActive: true, IsDying: false } enemy
                    || Iso.GroundDistanceSquared(enemy.GlobalPosition, patch.Position) > radiusSq)
                    continue;
                enemy.ApplySlow(_trailSlowFactor, TrailTickSeconds * 2f, _trailSource, ControlOrigin.Unknown);
                if (_trailFragile > 0f)
                    enemy.ApplyFragile(_trailFragile, TrailTickSeconds * 2f, _trailSource);
            }
        }
    }

    /// <summary>
    /// Tirage d'une chance sur <paramref name="hits"/> frappes. Au-delà de 100 %, l'effet part toujours et
    /// <paramref name="strength"/> porte l'excédent.
    /// </summary>
    private bool Roll(float chance, int hits, out float strength)
    {
        float combined = chance >= 1f ? 1f : 1f - Mathf.Pow(1f - chance, Mathf.Max(1, hits));
        strength = Mathf.Max(1f, chance);
        return Rng.Randf() < combined;
    }

    /// <summary>Allumette humide : la Brûlure est celle d'un objet ; ce qu'elle fait ne déclenche aucun objet.</summary>
    private void Burn(Enemy enemy, float dps, AttackContext context)
    {
        float seconds = _burnSeconds * _player.StatusDurationMultiplier;
        AttackContext burn = context.As(DamageKind.Passive);
        enemy.ApplyIgnite(dps, seconds, burn);
        SlowBurning(enemy, seconds, burn);
        Spark(enemy, FxFamily.Fire);
        _player.ObjectProcs?.Show(BurnChanceStat);
    }

    /// <summary>Thermomètre, palier 25 : les Brûlures du joueur ralentissent aussi, le temps qu'elles durent.</summary>
    private void SlowBurning(Enemy enemy, float seconds, AttackContext context)
    {
        if (_burnSlowFactor < 1f)
            enemy.ApplySlow(_burnSlowFactor, seconds, context, ControlOrigin.Unknown);
    }

    /// <summary>
    /// Glaçon : ralentit ; au palier, une cible déjà ralentie est figée un instant. Ce ralentissement compte comme
    /// celui de l'arme qui a frappé : la Propagation le transmet, la Pince à linge le renouvelle.
    /// </summary>
    private void Chill(Enemy enemy, float strength, AttackContext context)
    {
        if (_freezeSeconds > 0f && enemy.IsSlowed)
            enemy.Freeze(_freezeSeconds * _player.StatusDurationMultiplier);
        else
            enemy.ApplySlow(Mathf.Pow(_chillFactor, strength), _chillSeconds * _player.StatusDurationMultiplier, context);
        Spark(enemy, FxFamily.Pale);
        _player.ObjectProcs?.Show(ChillChanceStat);
    }

    /// <summary>
    /// Semelle usée : <paramref name="seconds"/> de marche sans arrêt chargent les prochaines attaques. Appelé par
    /// l'attaque elle-même : rend le multiplicateur de dégâts et consomme une charge.
    /// </summary>
    public float ConsumeStride()
    {
        if (_strideCharges <= 0)
            return 1f;
        _strideCharges--;
        _player.ObjectProcs?.Show(StrideDamageStat);
        // La marche suivante repart de zéro : une charge se regagne, elle ne se reprend pas en continu.
        _strideElapsed = 0f;
        return 1f + _strideDamage;
    }

    private void AdvanceStride(float delta)
    {
        if (_strideDamage <= 0f)
            return;
        if (_player.Velocity.LengthSquared() < MovingSpeedSq || _strideCharges >= _strideMaxCharges)
        {
            _strideElapsed = 0f;
            return;
        }
        _strideElapsed += delta;
        if (_strideElapsed < _strideSeconds)
            return;
        _strideElapsed = 0f;
        _strideCharges = _strideMaxCharges;
        CombatPools.Instance?.EmitSparks(_player.GlobalPosition, new SparkBurst
        {
            Family = FxFamily.Brass,
            Owner = FxOwner.Player,
            Count = 6,
            Direction = Vector2.Up,
            Spread = 2.5f,
            SpeedMin = 20f,
            SpeedMax = 45f,
            LifeMin = 0.2f,
            LifeMax = 0.35f,
            Size = 1,
        });
    }

    /// <summary>Boîte de pansements : chaque niveau soigne ; au palier, une cascade rend invulnérable un instant.</summary>
    private void OnLevelUp(int newLevel)
    {
        if (_levelHeal <= 0f || _player.IsDead)
            return;
        _player.Heal(_player.EffectiveMaxHp * _levelHeal);
        _player.ObjectProcs?.Show(LevelHealStat);
        ulong frame = Engine.GetProcessFrames();
        _levelsThisFrame = frame == _levelFrame ? _levelsThisFrame + 1 : 1;
        _levelFrame = frame;
        if (_cascadeLevels > 0 && _levelsThisFrame == _cascadeLevels)
            _player.GrantInvulnerability(_cascadeSeconds);
    }

    /// <summary>Pétard mouillé : la victime explose ; l'explosion ne déclenche aucun objet (plan 21 §7).</summary>
    private void Explode(Vector2 position, float damage, AttackContext source)
    {
        float radius = _explosionRadius * _player.AoeMultiplier;
        float radiusSq = radius * radius;
        foreach (Node node in _groupCache.GetEnemies())
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

    /// <summary>
    /// Élimination par ce qu'une arme a fait : Pétard mouillé, Dé à coudre. Paliers de l'Allumette et de l'Épingle :
    /// ce que la victime transmet à ses voisins, quel que soit le coup qui l'a tuée.
    /// </summary>
    private void OnEnemyKill(EnemyKillResult kill)
    {
        if (kill.Damage.Source.OwnerId != _playerId)
            return;
        if (kill.Damage.Source.IsWeaponWork)
            RewardWeaponKill(kill);
        bool spreadBurn = _burnSpreadRadius > 0f && kill.Burn.Remaining > 0f && kill.Burn.Source.OwnerId == _playerId;
        bool extendSlows = _slowKillRadius > 0f && kill.Slow.Remaining > 0f;
        if (!spreadBurn && !extendSlows)
            return;

        Enemy nearest = null;
        bool extended = false;
        float nearestSq = _burnSpreadRadius * _burnSpreadRadius;
        float slowRadiusSq = _slowKillRadius * _slowKillRadius;
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy || enemy.Life == kill.Target)
                continue;
            float distanceSq = Iso.GroundDistanceSquared(enemy.GlobalPosition, kill.Position);
            if (extendSlows && distanceSq <= slowRadiusSq && enemy.IsSlowed)
            {
                enemy.ExtendSlow(_slowKillSeconds, _slowKillMaxSeconds);
                extended = true;
            }
            if (spreadBurn && distanceSq <= nearestSq)
            {
                nearestSq = distanceSq;
                nearest = enemy;
            }
        }
        // Paliers de l'Épingle et de l'Allumette : leur icône s'élève quand ils agissent (plan 27 V2d).
        if (extended)
            _player.ObjectProcs?.Show(SlowedTargetDamageStat);
        if (nearest == null)
            return;
        nearest.ApplyIgnite(kill.Burn.Strength, kill.Burn.Remaining, kill.Burn.Source);
        SlowBurning(nearest, kill.Burn.Remaining, kill.Burn.Source);
        Spark(nearest, FxFamily.Fire);
        // La brûlure passe de la victime à sa voisine : un trait de feu le montre.
        _player.AttackFx.PlayBeam(kill.Position, nearest.GlobalPosition, FxFamily.Fire);
        _player.ObjectProcs?.Show(BurnChanceStat);
    }

    private void RewardWeaponKill(in EnemyKillResult kill)
    {
        if (_killExplosion > 0f)
        {
            float damage = FatalBlow(kill.Damage) * _killExplosion;
            AttackContext source = kill.Damage.Source.As(DamageKind.Passive);
            Explode(kill.Position, damage, source);
            if (_secondExplosionDelay >= 0f)
            {
                _pendingExplosions.Add(new PendingExplosion { Position = kill.Position, Damage = damage, Remaining = _secondExplosionDelay, Source = source });
                SetProcess(true);
            }
        }
        if (_killHeal > 0f)
        {
            _player.Heal(_killHeal + (kill.Elite ? _player.EffectiveMaxHp * _eliteHealRatio : 0f));
            _player.ObjectProcs?.Show(KillHealStat);
        }
    }

    /// <summary>
    /// Coup fatal lu par le Pétard. Une mort par saignement ou dans le feu vient du coup d'arme qui a posé l'effet :
    /// le dernier tic, de quelques dixièmes de PV, n'en est qu'une fraction.
    /// </summary>
    private static float FatalBlow(in DamageResult damage) =>
        damage.Source.Kind == DamageKind.DamageOverTime
            ? Mathf.Max(damage.NativeDamage, damage.Source.ReferenceDamage)
            : damage.NativeDamage;

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
