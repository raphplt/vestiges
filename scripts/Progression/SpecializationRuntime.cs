using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// État et réactions des perks d'un joueur (plan 05, B2). Un seul composant ordonne les effets d'un même résultat :
/// sur une blessure, Prévoyance rend d'abord ses PV, Reprise compte ensuite la perte restante. Il n'écoute que les
/// résultats de son joueur et ne tourne par frame que pendant une fenêtre ouverte.
/// </summary>
public partial class SpecializationRuntime : Node
{
    public const string OverhealReserveEffect = "overheal_reserve";
    public const string RallyEffect = "rally";
    public const string OverflowEffect = "overflow";

    private Player _player;
    private ulong _playerId;
    private EventBus _eventBus;
    private OverhealReserve _reserve;
    private RallyWindow _rally;
    private OverflowCharge _overflow;
    private readonly System.Collections.Generic.List<WeaponInstance> _expired = new();

    public OverhealReserve Reserve => _reserve;
    public RallyWindow Rally => _rally;
    public OverflowCharge Overflow => _overflow;

    public void Initialize(Player player)
    {
        _player = player;
        _playerId = player.GetInstanceId();
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PlayerDamageResolved += OnPlayerDamage;
        _eventBus.PlayerHealingResolved += OnPlayerHealing;
        _eventBus.EnemyKillResolved += OnEnemyKill;
        _eventBus.PlayerDamaged += OnVitalsChanged;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.PlayerDamageResolved -= OnPlayerDamage;
        _eventBus.PlayerHealingResolved -= OnPlayerHealing;
        _eventBus.EnemyKillResolved -= OnEnemyKill;
        _eventBus.PlayerDamaged -= OnVitalsChanged;
        if (_overflow == null)
            return;
        _eventBus.EnemyDamageResolved -= OnEnemyDamage;
        _eventBus.WeaponInventoryChanged -= OnWeaponInventoryChanged;
        _overflow.Clear();
    }

    /// <summary>Branche l'effet d'un perk acquis ; un effet non encore implémenté n'a pas d'état.</summary>
    public void Add(PerkSpecializationData perk)
    {
        switch (perk.Effect)
        {
            case OverhealReserveEffect:
                _reserve = new OverhealReserve(_player, perk);
                PublishReserve();
                break;
            case RallyEffect:
                _rally = new RallyWindow(_player, perk);
                break;
            case OverflowEffect:
                // Seul perk à lire chaque impact : l'abonnement n'existe que s'il est acquis.
                _overflow = new OverflowCharge(_playerId, perk);
                _eventBus.EnemyDamageResolved += OnEnemyDamage;
                _eventBus.WeaponInventoryChanged += OnWeaponInventoryChanged;
                break;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_rally != null && _rally.IsOpen)
        {
            _rally.Advance(dt);
            PublishRally();
        }
        if (_overflow != null && _overflow.HasReserves)
        {
            _overflow.Advance(dt, _expired);
            PublishExpiredOverflow();
        }
        SetProcess(NeedsClock);
    }

    /// <summary>Le temps de jeu ne compte que pour une fenêtre ou une réserve limitée dans le temps.</summary>
    private bool NeedsClock => (_rally?.IsOpen ?? false) || (_overflow?.HasReserves ?? false);

    private void OnEnemyDamage(DamageResult result)
    {
        if (!_overflow.Resolve(result))
            return;
        WeaponInstance weapon = result.Source.Weapon;
        PublishOverflow(weapon, _overflow.Amount(weapon));
        SetProcess(NeedsClock);
    }

    private void OnWeaponInventoryChanged()
    {
        _overflow.DropMissing(_player.WeaponSlots, _expired);
        PublishExpiredOverflow();
    }

    private void PublishExpiredOverflow()
    {
        foreach (WeaponInstance weapon in _expired)
            PublishOverflow(weapon, 0f);
        _expired.Clear();
    }

    private void OnPlayerDamage(PlayerDamageResult result)
    {
        if (result.PlayerId != _playerId || !result.CanRecover)
            return;
        float restored = 0f;
        if (_reserve != null)
        {
            restored = _reserve.Restore(result.HpLost);
            PublishReserve();
        }
        if (_rally != null)
        {
            _rally.Wound(result.HpLost - restored);
            SetProcess(NeedsClock);
            PublishRally();
        }
    }

    private void OnPlayerHealing(HealingResult result)
    {
        // Les restitutions de perks ne rechargent rien et ne se déduisent pas deux fois.
        if (result.PlayerId != _playerId || !result.Applied || result.Kind == HealingKind.PerkRecovery)
            return;
        if (_reserve != null && result.CanStoreExcess && _reserve.Store(result.Excess))
            PublishReserve();
        if (_rally != null && _rally.ExternalHeal(result.HpRestored))
            PublishRally();
    }

    private void OnEnemyKill(EnemyKillResult result)
    {
        if (_rally == null || result.Damage.Source.OwnerId != _playerId || !_rally.IsOpen)
            return;
        if (_rally.Kill())
            PublishRally();
    }

    private void OnVitalsChanged(float currentHp, float maxHp)
    {
        if (_reserve != null && _reserve.Truncate())
            PublishReserve();
    }

    private void PublishReserve() =>
        _eventBus?.PublishSpecializationGauge(new SpecializationGauge(_playerId, OverhealReserveEffect, "",
            _reserve.Stock, _reserve.Capacity));

    private void PublishOverflow(WeaponInstance weapon, float amount) =>
        _eventBus?.PublishSpecializationGauge(new SpecializationGauge(_playerId, OverflowEffect, weapon.Id, amount, amount));

    private void PublishRally() =>
        _eventBus?.PublishSpecializationGauge(new SpecializationGauge(_playerId, RallyEffect, "",
            _rally.Recoverable, _player.EffectiveMaxHp, _rally.Remaining, _rally.Duration));

    /// <summary>Coefficient d'un perk ; son absence est une erreur de données, pas une valeur par défaut silencieuse.</summary>
    public static float Parameter(PerkSpecializationData perk, string name)
    {
        if (perk.Parameters.TryGetValue(name, out float value))
            return value;
        GD.PushError($"[SpecializationRuntime] Paramètre {name} absent du perk {perk.Id}.");
        return 0f;
    }
}
