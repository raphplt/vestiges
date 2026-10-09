using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Bonus lâchés (plan 24 C4, DECISIONS §40) : sème les ramassables à la mort des élites et des Souverains, rarement à
/// celle des autres créatures, et à la fin d'une Résurgence survécue ; applique leur effet au contact. Les effets à
/// durée (aimant, bouclier, frénésie) sont retirés à leur fin. Au plus quelques bonus au sol à la fois ; recyclés par un
/// pool. Données : data/world/field_bonuses.json. Enfant de Main, comme les autres directeurs du monde.
/// </summary>
public partial class FieldBonusDirector : Node
{
    private readonly record struct TimedModifier(string Stat, float Value, string ModifierType, float Remaining);

    private FieldBonusConfig _config;
    private EventBus _eventBus;
    private GroupCache _groups;
    private NodePool<FieldBonus> _pool;
    private readonly List<FieldBonus> _active = new();
    private readonly List<TimedModifier> _timed = new();
    private Player _boosted;
    private float _totalWeight;

    public IReadOnlyList<FieldBonus> Active => _active;

    public override void _Ready()
    {
        _config = FieldBonusDataLoader.Load();
        if (!_config.Enabled)
            return;
        foreach (FieldBonusData bonus in _config.Bonuses)
            _totalWeight += bonus.Weight;
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.VariantEnemyKilled += OnVariantKilled;
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.CrisisEnded += OnCrisisEnded;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.VariantEnemyKilled -= OnVariantKilled;
        _eventBus.EnemyKilled -= OnEnemyKilled;
        _eventBus.CrisisEnded -= OnCrisisEnded;
    }

    private void OnVariantKilled(string displayName, string variantId, Vector2 position)
    {
        if (_config.VariantChance.TryGetValue(variantId, out float chance) && RunRandom.Loot.Randf() < chance)
            Spawn(PickWeighted(), position);
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        if (_config.KillPool.Count == 0 || RunRandom.Loot.Randf() >= _config.KillChance)
            return;
        Spawn(_config.Get(_config.KillPool[(int)(RunRandom.Loot.Randi() % _config.KillPool.Count)]), position);
    }

    /// <summary>Fin d'une Résurgence survécue : des bonus près du joueur, dont un soin.</summary>
    private void OnCrisisEnded(int crisisNumber)
    {
        if (_groups.GetPlayer() is not Player player)
            return;
        for (int i = 0; i < _config.CrisisEnd; i++)
        {
            FieldBonusData data = i == 0 ? _config.Get(_config.CrisisFirst) : PickWeighted();
            Vector2 offset = Vector2.FromAngle(RunRandom.Loot.Randf() * Mathf.Tau) * RunRandom.Loot.RandfRange(70f, 130f);
            Spawn(data, player.GlobalPosition + Iso.ToScreen(offset));
        }
    }

    /// <summary>Pose un bonus, s'il en reste la place ; différé, une mort survenant pendant la physique.</summary>
    public void Spawn(FieldBonusData data, Vector2 position)
    {
        if (data == null || _active.Count >= _config.MaxOnGround)
            return;
        _pool ??= new NodePool<FieldBonus>(GetParent(), () =>
        {
            FieldBonus bonus = new() { Name = "FieldBonus" };
            bonus.SetRelease(OnReleased, OnDespawnStarted);
            return bonus;
        });
        // Réservé tout de suite : deux morts dans la même frame ne dépassent pas la limite.
        FieldBonus node = _pool.Take();
        _active.Add(node);
        Callable.From(() => node.Launch(data, position, _config.LifetimeSeconds, _config.BlinkSeconds)).CallDeferred();
    }

    private void OnReleased(FieldBonus bonus)
    {
        _active.Remove(bonus);
        _pool.Return(bonus);
    }

    // Le ramassable ne compte plus parmi les quatre au sol pendant sa courte disparition visuelle.
    private void OnDespawnStarted(FieldBonus bonus) => _active.Remove(bonus);

    public override void _Process(double delta)
    {
        if (_config is not { Enabled: true })
            return;
        UpdateTimed((float)delta);
        if (_active.Count == 0 || _groups.GetPlayer() is not Player player || player.IsDead)
            return;
        float reachSq = _config.PickupPx * _config.PickupPx;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            FieldBonus bonus = _active[i];
            if (!bonus.Active || bonus.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) > reachSq)
                continue;
            Apply(bonus.Data, player);
            bonus.Release();
        }
    }

    private void Apply(FieldBonusData data, Player player)
    {
        switch (data.Effect)
        {
            case "heal":
                player.Heal(player.EffectiveMaxHp * data.Param("ratio"));
                break;
            case "magnet":
                AddTimed(player, "xp_magnet_radius", data.Param("multiplier"), "multiplicative", data.Param("duration_s"));
                break;
            case "shield":
                AddTimed(player, "shield", player.EffectiveMaxHp * data.Param("ratio"), "additive", data.Param("duration_s"));
                break;
            case "frenzy":
                AddTimed(player, "attack_speed", 1f + data.Param("amount"), "multiplicative", data.Param("duration_s"));
                break;
            case "blast":
                Blast(player, data);
                break;
        }
        player.ShowPopup(TranslationServer.Translate(data.NameKey), data.Color.Lightened(0.3f));
        AudioManager.Play("sfx_souvenir_trouve", 0f, -6f);
        CombatPools.Instance?.EmitSparks(player.GlobalPosition + new Vector2(0f, -14f), new SparkBurst
        {
            Family = FxFamily.Pale,
            Owner = FxOwner.World,
            Count = 10,
            Direction = Vector2.Up,
            Spread = Mathf.Tau,
            SpeedMin = 40f,
            SpeedMax = 90f,
            LifeMin = 0.25f,
            LifeMax = 0.45f,
            Size = 1,
            Decorative = true,
        });
    }

    /// <summary>Pétard : onde autour du joueur ; les variantes n'en perdent que le quart, pour que le pétard ne remplace pas le combat.</summary>
    private void Blast(Player player, FieldBonusData data)
    {
        float radius = data.Param("radius_px");
        float ratio = data.Param("hp_ratio");
        float knockback = data.Param("knockback_px");
        AttackContext context = new(player.GetInstanceId(), null, 0, DamageKind.Passive);
        using CrowdQuery crowd = CrowdIndex.Near(player.GlobalPosition, radius);
        foreach (Node node in crowd.Targets)
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                continue;
            Vector2 toEnemy = enemy.GlobalPosition - player.GlobalPosition;
            if (Iso.ToGround(toEnemy).Length() > radius)
                continue;
            enemy.TakeDamage(enemy.MaxHp * ratio * (enemy.IsPriorityTarget ? 0.25f : 1f), source: context);
            enemy.ApplyKnockback(toEnemy.Normalized(), knockback);
        }
        PixelFxSpec ring = PixelFxSpec.Of(PixelFxShape.Ring, FxFamily.Fire, radius, 4f, 0.4f);
        ring.Squash = 2f;
        ring.Steps = 8;
        ring.FadeTail = 0.4f;
        ring.ZIndex = -1;
        CombatPools.Instance?.PlayFx(player.GlobalPosition, ring, FxOwner.World);
    }

    private void AddTimed(Player player, string stat, float value, string modifierType, float seconds)
    {
        player.ApplyPerkModifier(stat, value, modifierType);
        _boosted = player;
        _timed.Add(new TimedModifier(stat, value, modifierType, seconds));
    }

    /// <summary>Retire les effets arrivés à leur fin : l'inverse du multiplicateur, ou la valeur ôtée.</summary>
    private void UpdateTimed(float dt)
    {
        for (int i = _timed.Count - 1; i >= 0; i--)
        {
            TimedModifier timed = _timed[i];
            float remaining = timed.Remaining - dt;
            if (remaining > 0f)
            {
                _timed[i] = timed with { Remaining = remaining };
                continue;
            }
            _timed.RemoveAt(i);
            if (_boosted == null || !IsInstanceValid(_boosted))
                continue;
            if (timed.ModifierType == "multiplicative")
                _boosted.ApplyPerkModifier(timed.Stat, 1f / timed.Value, timed.ModifierType);
            else
                _boosted.ApplyPerkModifier(timed.Stat, -timed.Value, timed.ModifierType);
        }
    }

    private FieldBonusData PickWeighted()
    {
        float roll = RunRandom.Loot.Randf() * _totalWeight;
        foreach (FieldBonusData bonus in _config.Bonuses)
        {
            roll -= bonus.Weight;
            if (roll <= 0f)
                return bonus;
        }
        return _config.Bonuses.Count > 0 ? _config.Bonuses[^1] : null;
    }
}
