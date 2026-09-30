using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Objets de déclencheur d'un joueur (plan 21 §4) : ce qui se passe à l'impact d'une arme, contre une cible déjà
/// touchée par un statut, à l'élimination. Créé au premier objet de ce type. Règles communes (§7) :
/// <list type="bullet">
/// <item>seul un coup direct d'arme déclenche, jamais un effet déjà déclenché ;</item>
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

    public const string BurnSpreadEffect = "burn_spread";
    public const string DoubleSlowFreezeEffect = "double_slow_freeze";
    public const string BurnSlowsEffect = "burn_slows";
    public const string SlowKillExtendsEffect = "slow_kill_extends";

    private const float SparkHeight = 12f;

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

    /// <summary>Tirages des déclencheurs ; les bancs le graine pour rester déterministes.</summary>
    internal RandomNumberGenerator Rng { get; } = new();

    public float BurnChance => _burnChance;
    public float ChillChance => _chillChance;

    public static bool IsTriggerStat(string stat) =>
        stat is BurnChanceStat or ChillChanceStat or BurningTargetDamageStat or SlowedTargetDamageStat;

    public static bool IsTriggerMilestone(string effect) =>
        effect is BurnSpreadEffect or DoubleSlowFreezeEffect or BurnSlowsEffect or SlowKillExtendsEffect;

    public void Initialize(Player player)
    {
        _player = player;
        _playerId = player.GetInstanceId();
        Rng.Randomize();
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
        _eventBus.EnemyKillResolved += OnEnemyKill;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.EnemyKillResolved -= OnEnemyKill;
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
        }
    }

    /// <summary>Thermomètre, Épingle à nourrice : dégâts d'un coup d'arme contre une cible brûlée ou ralentie.</summary>
    public float AgainstTarget(Enemy enemy, float damage)
    {
        float bonus = 0f;
        if (_burningTargetDamage > 0f && enemy.IsBurning)
            bonus += _burningTargetDamage;
        if (_slowedTargetDamage > 0f && enemy.IsSlowed)
            bonus += _slowedTargetDamage;
        return damage * (1f + bonus);
    }

    /// <summary>
    /// Impact direct d'une arme : Allumette humide et Glaçon tirent leur chance, pondérée par l'arme et par le
    /// nombre de frappes qui ont touché ensemble. <paramref name="hitDamage"/> est le coup de base de l'arme : un
    /// cône continu, qui touche par fractions de frame, brûle comme les autres armes.
    /// </summary>
    public void OnWeaponImpact(Enemy enemy, float hitDamage, WeaponInstance weapon, int hits, AttackContext context)
    {
        if (enemy.IsDying)
            return;
        float coefficient = weapon?.Base.TriggerCoefficient ?? 1f;
        if (_burnChance > 0f && Roll(_burnChance * coefficient, hits, out float burnStrength))
            Burn(enemy, hitDamage * _burnDamageRatio * burnStrength, context);
        if (_chillChance > 0f && Roll(_chillChance * coefficient, hits, out float chillStrength))
            Chill(enemy, chillStrength, context);
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

    private void Burn(Enemy enemy, float dps, AttackContext context)
    {
        float seconds = _burnSeconds * _player.StatusDurationMultiplier;
        enemy.ApplyIgnite(dps, seconds, context);
        SlowBurning(enemy, seconds, context);
        Spark(enemy, FxFamily.Fire);
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
    }

    /// <summary>Allumette humide et Épingle à nourrice, paliers 25 : ce qu'une élimination transmet aux voisins.</summary>
    private void OnEnemyKill(EnemyKillResult kill)
    {
        if (kill.Damage.Source.OwnerId != _playerId)
            return;
        bool spreadBurn = _burnSpreadRadius > 0f && kill.Burn.Remaining > 0f && kill.Burn.Source.OwnerId == _playerId;
        bool extendSlows = _slowKillRadius > 0f && kill.Slow.Remaining > 0f;
        if (!spreadBurn && !extendSlows)
            return;

        Enemy nearest = null;
        float nearestSq = _burnSpreadRadius * _burnSpreadRadius;
        float slowRadiusSq = _slowKillRadius * _slowKillRadius;
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy || enemy.Life == kill.Target)
                continue;
            float distanceSq = Iso.GroundDistanceSquared(enemy.GlobalPosition, kill.Position);
            if (extendSlows && distanceSq <= slowRadiusSq && enemy.IsSlowed)
                enemy.ExtendSlow(_slowKillSeconds, _slowKillMaxSeconds);
            if (spreadBurn && distanceSq <= nearestSq)
            {
                nearestSq = distanceSq;
                nearest = enemy;
            }
        }
        if (nearest == null)
            return;
        nearest.ApplyIgnite(kill.Burn.Strength, kill.Burn.Remaining, kill.Burn.Source);
        SlowBurning(nearest, kill.Burn.Remaining, kill.Burn.Source);
        Spark(nearest, FxFamily.Fire);
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
