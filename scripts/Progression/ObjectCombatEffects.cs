using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Progression;

/// <summary>Combinaisons d'armes et objets de dash. Délais persistants lors des échanges, dégâts passifs non récursifs.</summary>
public partial class ObjectCombatEffects : Node
{
    private readonly record struct HitMemory(Enemy Enemy, EnemyLife Life, string WeaponId, float Amount, float Time);
    private readonly record struct Wound(float Time, float Amount);
    private readonly Dictionary<EnemyLife, HitMemory> _hits = new();
    private readonly List<EnemyLife> _expired = new();
    private readonly Wound[] _wounds = new Wound[64];
    private int _woundHead, _woundCount;
    private Player _player;
    private ulong _ownerId;
    private EventBus _bus;
    private ErasureManager _erasure;
    private Sprite2D _portrait;
    private float _clock, _cleanupAt;
    private float _crossRatio, _crossWindow, _crossCooldown, _crossReady;
    private float _linkRatio, _linkDuration, _linkRadius, _linkCooldown, _linkMaximum, _linkReady, _linkExpires;
    private float _cancelCount;
    private int _cancelsLeft;
    private bool _dashActive;
    private float _recoveryRatio, _recoveryWindow, _recoveryCooldown, _recoveryReady;
    private float _decoyCopies, _decoySeconds, _decoyCooldown, _decoyRadius, _decoyReady, _decoyExpires;
    private Enemy _last, _previous, _linkedA, _linkedB;
    private EnemyLife _lastLife, _previousLife, _linkedLifeA, _linkedLifeB;
    public float RecoveryCooldown => Mathf.Max(0f, _recoveryReady - _clock);
    public float LinkRemaining => Mathf.Max(0f, _linkExpires - _clock);
    public bool DecoyActive => _decoyCopies > 0f && _decoyExpires > _clock && !_player.IsDead;
    public Vector2 DecoyPosition => _portrait?.GlobalPosition ?? Vector2.Zero;
    public int CancelsRemaining => _cancelsLeft;

    public static bool IsCombatStat(string stat) => stat is "cross_weapon_echo" or "damage_link"
        or "dash_cancel_projectiles" or "dash_recovery" or "dash_decoy";

    public void Initialize(Player player) { _player = player; _ownerId = player.GetInstanceId(); }
    public override void _Ready()
    {
        _bus = GetNode<EventBus>("/root/EventBus");
        _bus.EnemyDamageResolved += OnDamage;
        _bus.PlayerDamageResolved += OnPlayerDamage;
        _bus.PlayerDashStarted += OnDashStarted;
        _bus.PlayerDashMoved += OnDashMoved;
        _bus.PlayerDashFinished += OnDashFinished;
        _erasure = GetTree().CurrentScene?.GetNodeOrNull<ErasureManager>("ErasureManager");
        _portrait = new Sprite2D { Name = "RememberedPortrait", TopLevel = true, Visible = false,
            SelfModulate = new Color(.8f, .95f, 1f, .7f) };
        _player.AddChild(_portrait);
    }
    public override void _ExitTree()
    {
        if (_bus != null)
        {
            _bus.EnemyDamageResolved -= OnDamage;
            _bus.PlayerDamageResolved -= OnPlayerDamage;
            _bus.PlayerDashStarted -= OnDashStarted;
            _bus.PlayerDashMoved -= OnDashMoved;
            _bus.PlayerDashFinished -= OnDashFinished;
        }
        if (GodotObject.IsInstanceValid(_portrait)) _portrait.QueueFree();
    }
    public void Configure(PassiveSouvenirData data)
    {
        float P(string key) => data.Parameters.GetValueOrDefault(key);
        switch (data.Stat)
        {
            case "cross_weapon_echo": _crossWindow = P("window_seconds"); _crossCooldown = P("cooldown_seconds"); break;
            case "damage_link": _linkDuration = P("duration_seconds"); _linkRadius = P("radius");
                _linkCooldown = P("cooldown_seconds"); _linkMaximum = P("max_ratio"); break;
            case "dash_recovery": _recoveryWindow = P("history_seconds"); _recoveryCooldown = P("cooldown_seconds"); break;
            case "dash_decoy": _decoySeconds = P("seconds_per_copy"); _decoyCooldown = P("cooldown_seconds"); _decoyRadius = P("radius"); break;
        }
    }
    public void Add(string stat, float amount)
    {
        switch (stat)
        {
            case "cross_weapon_echo": _crossRatio = ObjectTriggers.Settle(Mathf.Max(0f, _crossRatio + amount)); if (_crossRatio == 0f) _hits.Clear(); break;
            case "damage_link": _linkRatio = ObjectTriggers.Settle(Mathf.Max(0f, _linkRatio + amount)); if (_linkRatio == 0f) { _linkExpires = 0f; _last = _previous = null; _lastLife = _previousLife = default; } break;
            case "dash_cancel_projectiles": _cancelCount = Mathf.Max(0f, _cancelCount + amount); _cancelsLeft = Mathf.Min(_cancelsLeft, Mathf.FloorToInt(_cancelCount)); break;
            case "dash_recovery": _recoveryRatio = ObjectTriggers.Settle(Mathf.Max(0f, _recoveryRatio + amount)); if (_recoveryRatio == 0f) _woundCount = 0; break;
            case "dash_decoy": _decoyCopies = Mathf.Max(0f, _decoyCopies + amount); if (_decoyCopies == 0f) HideDecoy(); break;
        }
    }
    public override void _Process(double delta) => AdvanceClock((float)delta);
    internal void AdvanceClock(float delta)
    {
        _clock += delta;
        if (_player.IsDead) { HideDecoy(); _linkExpires = 0f; _woundCount = 0; _dashActive = false; }
        if (_portrait.Visible && (!DecoyActive || (_erasure != null && GodotObject.IsInstanceValid(_erasure)
            && _erasure.GetZonePhaseAt(_portrait.GlobalPosition) == ErasureManager.ErasureZonePhase.Void))) HideDecoy();
        if (_clock < _cleanupAt) return;
        _cleanupAt = _clock + 1f;
        _expired.Clear();
        foreach (KeyValuePair<EnemyLife, HitMemory> pair in _hits)
            if (_clock - pair.Value.Time > _crossWindow || !Alive(pair.Value.Enemy, pair.Key)) _expired.Add(pair.Key);
        foreach (EnemyLife life in _expired) _hits.Remove(life);
    }
    private static bool Alive(Enemy enemy, EnemyLife life) => GodotObject.IsInstanceValid(enemy)
        && enemy.IsActive && !enemy.IsDying && enemy.Life == life;
    private void OnDamage(DamageResult hit)
    {
        if (_player.IsDead || !hit.Applied || hit.Source.OwnerId != _ownerId || hit.Source.Weapon == null
            || hit.Source.Kind is not (DamageKind.DirectWeapon or DamageKind.SecondaryWeapon)) return;
        Enemy enemy = GodotObject.InstanceFromId(hit.Target.InstanceId) as Enemy;
        if (!GodotObject.IsInstanceValid(enemy) || enemy.Life != hit.Target) return;
        if (_linkRatio > 0f && _linkExpires > _clock)
        {
            Enemy target = hit.Target == _linkedLifeA ? _linkedB : hit.Target == _linkedLifeB ? _linkedA : null;
            EnemyLife life = hit.Target == _linkedLifeA ? _linkedLifeB : _linkedLifeA;
            if (Alive(target, life)) PassiveHit(target, Mathf.Min(hit.NativeDamage, hit.HpLost) * Mathf.Min(_linkRatio, _linkMaximum), hit.Source);
        }
        if (hit.Fatal || !Alive(enemy, hit.Target)) return;
        if (_linkRatio > 0f && hit.Target != _lastLife)
        {
            _previous = _last; _previousLife = _lastLife;
            _last = enemy; _lastLife = hit.Target;
        }
        if (_crossRatio <= 0f) return;
        string weaponId = hit.Source.Weapon.Id;
        bool cross = _hits.TryGetValue(hit.Target, out HitMemory prior) && prior.WeaponId != weaponId
            && _clock - prior.Time <= _crossWindow && _clock >= _crossReady;
        if (cross)
        {
            _crossReady = _clock + _crossCooldown;
            _hits.Remove(hit.Target);
            PassiveHit(enemy, (prior.Amount + hit.NativeDamage) * _crossRatio, hit.Source);
        }
        else _hits[hit.Target] = new(enemy, hit.Target, weaponId, hit.NativeDamage, _clock);
    }
    private void PassiveHit(Enemy target, float damage, AttackContext source)
    {
        if (damage <= 0f) return;
        target.TakeDamage(damage, source: source.As(DamageKind.Passive));
    }
    private void OnPlayerDamage(PlayerDamageResult hit)
    {
        if (_recoveryRatio <= 0f || hit.PlayerId != _ownerId || !hit.CanRecover) return;
        int index = (_woundHead + _woundCount) % _wounds.Length;
        _wounds[index] = new(_clock, hit.HpLost);
        if (_woundCount == _wounds.Length) _woundHead = (_woundHead + 1) % _wounds.Length;
        else _woundCount++;
    }
    private void OnDashStarted(PlayerDashEvent dash)
    {
        if (dash.PlayerId != _ownerId || _player.IsDead) return;
        _dashActive = true;
        _cancelsLeft = Mathf.Max(0, Mathf.FloorToInt(_cancelCount));
    }
    private void OnDashMoved(PlayerDashEvent dash)
    {
        if (dash.PlayerId != _ownerId || !_dashActive || _cancelsLeft <= 0) return;
        _cancelsLeft -= EnemyProjectile.CancelAlongSweep(dash.From, dash.To, _player.BodyRadius, _cancelsLeft);
    }
    internal bool TryCancelIncoming(Vector2 from, Vector2 to, float radius)
    {
        if (!_dashActive || _cancelsLeft <= 0 || _player.IsDead) return false;
        Vector2 nearest = Geometry2D.GetClosestPointToSegment(_player.GlobalPosition, from, to);
        float reach = radius + _player.BodyRadius;
        if (nearest.DistanceSquaredTo(_player.GlobalPosition) > reach * reach) return false;
        _cancelsLeft--;
        return true;
    }
    private void OnDashFinished(PlayerDashEvent dash)
    {
        if (dash.PlayerId != _ownerId) return;
        _dashActive = false; _cancelsLeft = 0;
        if (_player.IsDead) return;
        if (_linkRatio > 0f && _clock >= _linkReady && Alive(_last, _lastLife) && Alive(_previous, _previousLife)
            && _last.GlobalPosition.DistanceSquaredTo(_previous.GlobalPosition) <= _linkRadius * _linkRadius
            && !(_last.Boss != null && _last.Boss == _previous.Boss))
        {
            _linkedA = _last; _linkedLifeA = _lastLife; _linkedB = _previous; _linkedLifeB = _previousLife;
            _linkExpires = _clock + _linkDuration; _linkReady = _clock + _linkCooldown;
        }
        if (_recoveryRatio > 0f && _clock >= _recoveryReady)
        {
            float loss = 0f;
            for (int i = 0; i < _woundCount; i++)
            {
                Wound wound = _wounds[(_woundHead + i) % _wounds.Length];
                if (_clock - wound.Time <= _recoveryWindow) loss += wound.Amount;
            }
            _woundCount = 0;
            if (loss > 0f)
            {
                _recoveryReady = _clock + _recoveryCooldown;
                _player.Heal(loss * _recoveryRatio, HealingKind.PerkRecovery);
            }
        }
        if (_decoyCopies > 0f && _clock >= _decoyReady)
        {
            _decoyReady = _clock + _decoyCooldown;
            _decoyExpires = _clock + _decoySeconds * _decoyCopies;
            _player.CopyPortraitAppearance(_portrait);
            _portrait.GlobalPosition = dash.From;
            _portrait.Visible = true;
        }
    }
    public bool TryGetDecoyTarget(Vector2 enemyPosition, out Vector2 position)
    {
        position = DecoyPosition;
        return DecoyActive && enemyPosition.DistanceSquaredTo(position) <= _decoyRadius * _decoyRadius;
    }
    private void HideDecoy()
    {
        _decoyExpires = 0f;
        if (_portrait != null) _portrait.Visible = false;
    }
}
