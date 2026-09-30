using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Paliers d'objets atteints par un joueur (plan 21 §4) : leurs paramètres et leur état. Créé au premier palier
/// atteint, il n'écoute que les événements des paliers actifs.
/// </summary>
public partial class ObjectMilestones : Node
{
    /// <summary>Palier porté par la formule d'un effet (cran <c>step</c>) : rien à brancher, seulement à annoncer.</summary>
    public const string StatStepEffect = "stat_step";
    public const string StatusRenewEffect = "status_renew";

    private const float SparkHeight = 14f;

    private Player _player;
    private ulong _playerId;
    private EventBus _eventBus;
    private float _renewChance;

    /// <summary>Tirages des paliers à chance ; les bancs le graine pour rester déterministes.</summary>
    internal RandomNumberGenerator Rng { get; } = new();

    public void Initialize(Player player)
    {
        _player = player;
        _playerId = player.GetInstanceId();
        Rng.Randomize();
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        if (_renewChance > 0f)
            _eventBus.EnemyStatusExpired += OnStatusExpired;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null && _renewChance > 0f)
            _eventBus.EnemyStatusExpired -= OnStatusExpired;
    }

    /// <summary>Branche l'effet d'un palier que l'objet vient d'atteindre.</summary>
    public void Activate(ObjectMilestoneData milestone)
    {
        switch (milestone.Effect)
        {
            case StatusRenewEffect:
                bool subscribe = _renewChance <= 0f && _eventBus != null;
                _renewChance = milestone.Parameter("chance");
                if (subscribe)
                    _eventBus.EnemyStatusExpired += OnStatusExpired;
                break;
        }
    }

    /// <summary>Un contrôle natif reste natif ; tout autre reste non transmissible.</summary>
    private static ControlOrigin RenewedOrigin(in StatusExpiry expiry) =>
        expiry.Origin == ControlOrigin.NativeWeapon ? ControlOrigin.NativeWeapon : ControlOrigin.Unknown;

    /// <summary>Pince à linge : un statut du joueur qui expire peut repartir pour la même durée.</summary>
    private void OnStatusExpired(StatusExpiry expiry)
    {
        if (expiry.Source.OwnerId != _playerId || !IsInstanceValid(expiry.Target) || expiry.Target.IsDying)
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
