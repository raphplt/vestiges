using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Une liaison de harpon par propriétaire. Les salves vivent aussi longtemps que leurs projectiles : le registre
/// préalloué ne conserve ni historique de coups ni référence de cible recyclée. La ligne est créée une seule fois.
/// </summary>
public partial class HarpoonTether : Node2D
{
    private const int LaunchCapacity = 256;
    private readonly Dictionary<ulong, int> _launches = new(LaunchCapacity);
    private readonly Launch[] _slots = new Launch[LaunchCapacity];
    private readonly Stack<int> _free = new(LaunchCapacity);
    private Player _owner;
    private Line2D _line;
    private TargetLock _target;
    private Vector2 _origin;
    private float _remainingTime;
    private float _remainingDistance;
    private float _speed;
    private float _stopDistance;
    private bool _echo;
    private Vector2 _requestedPlayerTravel;
    public bool IsActive { get; private set; }
    public bool PullsPlayer { get; private set; }
    public int TrackedLaunches => _launches.Count;

    private struct Launch
    {
        public ulong Id;
        public int References;
        public bool Claimed;
        public bool Echo;
        public WeaponSpecialEffect Settings;
    }

    public void Initialize(Player owner)
    {
        _owner = owner;
        for (int i = LaunchCapacity - 1; i >= 0; i--) _free.Push(i);
        _line = new Line2D { Name = "SafetyRope", Width = 1f, DefaultColor = new Color("b8a47a"), Visible = false };
        _line.AddPoint(Vector2.Zero);
        _line.AddPoint(Vector2.Zero);
        AddChild(_line);
    }

    /// <summary>Chaque projectile garde sa référence jusqu'au retour au pool ; -1 refuse une salve hors capacité.</summary>
    public int Register(AttackContext context)
    {
        if (context.Weapon?.SpecialEffect?.Kind != SpecialEffectKind.HarpoonTraction
            || context.OwnerId != _owner.GetInstanceId()) return -1;
        if (_launches.TryGetValue(context.LaunchId, out int index))
        {
            _slots[index].References++;
            return index;
        }
        // Saturer la capacité supprime seulement le rappel ; dégâts et perforation continuent normalement.
        if (_free.Count == 0) return -1;
        index = _free.Pop();
        _slots[index] = new Launch { Id = context.LaunchId, References = 1, Echo = context.IsEcho,
            Settings = context.Weapon.SpecialEffect };
        _launches.Add(context.LaunchId, index);
        return index;
    }

    public void Release(int index)
    {
        if (index < 0 || _slots[index].References <= 0) return;
        if (--_slots[index].References != 0) return;
        _launches.Remove(_slots[index].Id);
        _slots[index] = default;
        _free.Push(index);
    }

    /// <summary>Le premier survivant consomme l'accroche, même ancré ou pendant une mobilité prioritaire.</summary>
    public bool TryCatch(int index, Enemy enemy, Vector2 origin)
    {
        if (index < 0 || _slots[index].References <= 0 || _slots[index].Claimed
            || !enemy.IsActive || enemy.IsDying) return false;
        _slots[index].Claimed = true;
        if (IsActive || !_owner.CanUseHarpoon || enemy.IsHarpoonAnchored || enemy.HarpoonOwner != 0) return false;
        bool heavy = enemy.IsHarpoonHeavy;
        if (heavy && _slots[index].Echo) return false;
        WeaponSpecialEffect settings = _slots[index].Settings;
        _target = TargetLock.On(enemy);
        _origin = origin;
        _echo = _slots[index].Echo;
        _speed = settings.Get(SpecialEffectParam.PullSpeed);
        _remainingDistance = settings.Get(SpecialEffectParam.PullDistance);
        _remainingTime = settings.Get(SpecialEffectParam.PullDuration);
        _stopDistance = settings.Get(SpecialEffectParam.StopDistance) + enemy.BodyRadius + _owner.BodyRadius;
        PullsPlayer = heavy;
        IsActive = true;
        if (!heavy) enemy.HarpoonOwner = _owner.GetInstanceId();
        UpdateLine(enemy);
        return true;
    }

    /// <summary>Le léger avance ici ; pour un lourd, renvoie le trajet à donner au corps physique du joueur.</summary>
    public Vector2 Advance(float delta, bool allowed)
    {
        if (!IsActive) return Vector2.Zero;
        if (!allowed || !_owner.CanUseHarpoon || !_target.TryGet(out Node2D node))
        {
            Cancel();
            return Vector2.Zero;
        }
        Enemy enemy = (Enemy)node;
        float stepTime = Mathf.Min(Mathf.Max(0f, delta), _remainingTime);
        Vector2 from = PullsPlayer ? _owner.GlobalPosition : enemy.GlobalPosition;
        Vector2 to = PullsPlayer ? enemy.GlobalPosition : _echo ? _origin : _owner.GlobalPosition;
        Vector2 offset = to - from;
        float length = offset.Length();
        float speed = _speed * (PullsPlayer ? _owner.HarpoonTravelFactor : 1f);
        float distance = Mathf.Min(_remainingDistance, Mathf.Min(speed * stepTime, length - _stopDistance));
        if (distance <= 0.001f || stepTime <= 0f)
        {
            Cancel();
            return Vector2.Zero;
        }
        _remainingTime -= stepTime;
        Vector2 requested = offset / length * distance;
        Vector2 permitted = _owner.ClampHarpoonTravel(from, requested);
        if (PullsPlayer)
        {
            _requestedPlayerTravel = requested;
            return permitted;
        }
        Vector2 traveled = enemy.MoveByHarpoon(permitted);
        FinishTravel(requested, traveled);
        if (IsActive) UpdateLine(enemy);
        return Vector2.Zero;
    }

    /// <summary>Après MoveAndSlide, un mur, la limite de trajet ou un compteur épuisé relâchent le filin.</summary>
    public void FinishPlayerTravel(Vector2 traveled)
    {
        if (!IsActive || !PullsPlayer) return;
        FinishTravel(_requestedPlayerTravel, traveled);
        if (IsActive && _target.TryGet(out Node2D node)) UpdateLine((Enemy)node);
    }

    private void FinishTravel(Vector2 requested, Vector2 traveled)
    {
        _remainingDistance -= traveled.Length();
        // Une différence de positions lointaines perd quelques décimales : elle ne signifie pas un obstacle.
        if (_remainingTime <= 0f || _remainingDistance <= 0.001f || traveled.DistanceSquaredTo(requested) > 0.0025f) Cancel();
    }

    private void UpdateLine(Enemy enemy)
    {
        _line.Visible = CombatFxSettings.PlayerProjectiles;
        Vector2 from = _echo ? _origin : _owner.GlobalPosition;
        _line.SetPointPosition(0, ToLocal(from) + new Vector2(0f, -Iso.FlightHeight));
        _line.SetPointPosition(1, ToLocal(enemy.GlobalPosition) + new Vector2(0f, -Iso.FlightHeight));
    }

    public void Cancel()
    {
        if (_target.TryGet(out Node2D node) && node is Enemy enemy && enemy.HarpoonOwner == _owner.GetInstanceId())
            enemy.HarpoonOwner = 0;
        _target = default;
        IsActive = false;
        PullsPlayer = false;
        if (_line != null) _line.Visible = false;
    }

    public override void _ExitTree() => Cancel();
}
