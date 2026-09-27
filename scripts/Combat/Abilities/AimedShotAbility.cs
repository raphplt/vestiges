using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Tir annoncé (Cracheur, Sentinelle, Tisseuse) : remplace le tir instantané de base. Le tireur s'arrête, vise, et un
/// couloir court montre la direction verrouillée avant que le projectile parte ; un pas de côté pendant l'annonce
/// suffit. Une Sentinelle peut aussi montrer au sol le cercle de sa portée quand le joueur s'en approche : dedans on
/// est visé, dehors on ne l'est pas.
/// </summary>
public class AimedShotAbility : IEnemyAbility
{
    private readonly GroundTelegraph _lane;
    private readonly GroundTelegraph _ring;

    private float _windupSeconds;
    private float _laneLength;
    private float _laneWidth;
    private bool _showRange;
    private float _ringMargin;
    private FxFamily _family;
    private Color _flash;

    private bool _aiming;
    private bool _ringShown;
    private float _timer;
    private float _cooldownTimer;
    private Vector2 _direction;

    public bool ReplacesBaseAttack => true;

    public AimedShotAbility(Enemy owner)
    {
        _lane = new GroundTelegraph { Name = "AimMarker" };
        owner.AddChild(_lane);
        _ring = new GroundTelegraph { Name = "RangeMarker" };
        owner.AddChild(_ring);
    }

    public void Configure(EnemyAbilityData data)
    {
        _windupSeconds = Mathf.Max(0.05f, data.GetNumber("windup_seconds", 0.35f));
        _laneLength = data.GetNumber("lane_length", 90f);
        _laneWidth = data.GetNumber("lane_width", 6f);
        _showRange = data.GetNumber("show_range", 0f) > 0f;
        _ringMargin = data.GetNumber("range_reveal_margin", 90f);
        _family = PixelPalette.ParseFamily(data.GetText("fx_family", "hostile"), FxFamily.Hostile);
        _flash = PixelPalette.Ramp(_family).Light;

        Cancel();
        _ringShown = false;
        _ring.HideMarker();
        // Décalage initial : des tireurs apparus ensemble ne tirent pas en rythme.
        _cooldownTimer = data.GetNumber("first_delay", (float)GD.RandRange(0.3, 1.0));
    }

    public bool Process(Enemy owner, Player player, float distToPlayer, float delta)
    {
        // La portée se mesure au sol pour que le cercle montré soit exactement la zone visée.
        float reach = _showRange
            ? Mathf.Sqrt(Iso.GroundDistanceSquared(owner.GlobalPosition, player.GlobalPosition))
            : distToPlayer;
        UpdateRing(owner, reach);

        if (_aiming)
        {
            _timer -= delta;
            owner.Velocity = Vector2.Zero;
            _lane.SetProgress(1f - _timer / _windupSeconds);
            if (_timer <= 0f)
            {
                _aiming = false;
                _lane.HideMarker();
                owner.ShootProjectile(_direction);
                _cooldownTimer = owner.RangedCooldown;
            }
            return true;
        }

        _cooldownTimer -= delta;
        if (_cooldownTimer > 0f || reach > owner.AttackRange || owner.IsDisoriented)
            return false;

        _aiming = true;
        _timer = _windupSeconds;
        _direction = (player.GlobalPosition - owner.GlobalPosition).Normalized();
        owner.Velocity = Vector2.Zero;
        owner.PlayAttackAnim();
        owner.FlashWarning(_flash, _windupSeconds);
        if (_laneLength > 0f)
            _lane.ShowLine(owner.GlobalPosition, owner.GlobalPosition + _direction * _laneLength, _laneWidth, _family);
        return true;
    }

    public void Cancel()
    {
        _aiming = false;
        _timer = 0f;
        _lane.HideMarker();
    }

    private void UpdateRing(Enemy owner, float reach)
    {
        if (!_showRange)
            return;
        bool show = reach <= owner.AttackRange + _ringMargin;
        if (show == _ringShown)
            return;
        _ringShown = show;
        if (show)
            _ring.ShowRing(owner.GlobalPosition, owner.AttackRange, _family);
        else
            _ring.HideMarker();
    }
}
