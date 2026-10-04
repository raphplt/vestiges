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
    private readonly Enemy _owner;
    private readonly Sprite2D _appearance;
    private ProjectileSprites.SpriteSet _appearanceSet;
    private int _appearanceDirection;
    private int _appearanceFrame = -1;

    private float _windupSeconds;
    private float _cooldownMultiplier;
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
    public bool IsActive => _aiming;

    public AimedShotAbility(Enemy owner)
    {
        _owner = owner;
        _appearance = new Sprite2D
        {
            Name = "ProjectileAppearance", TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = new Vector2(0f, -Iso.FlightHeight), Visible = false,
        };
        owner.AddChild(_appearance);
        _lane = new GroundTelegraph { Name = "AimMarker" };
        owner.AddChild(_lane);
        _ring = new GroundTelegraph { Name = "RangeMarker" };
        owner.AddChild(_ring);
    }

    public void Configure(EnemyAbilityData data)
    {
        _windupSeconds = data.Number("windup_seconds");
        _cooldownMultiplier = data.Number("cooldown_multiplier");
        _laneLength = data.Number("lane_length");
        _laneWidth = data.Number("lane_width");
        _showRange = data.Number("show_range") > 0f;
        _ringMargin = data.Number("range_reveal_margin");
        _family = PixelPalette.ParseFamily(data.Text("fx_family"), FxFamily.Hostile);
        _flash = PixelPalette.Ramp(_family).Light;

        Cancel();
        _appearanceSet = ProjectileSprites.Get(_owner.ProjectileSpriteId)?.Appearance;
        _ringShown = false;
        _ring.HideMarker();
        // Décalage initial : des tireurs apparus ensemble ne tirent pas en rythme.
        _cooldownTimer = data.TryGetNumber("first_delay", out float firstDelay) ? firstDelay : (float)GD.RandRange(0.3, 1.0);
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
            UpdateAppearance();
            if (_timer <= 0f)
            {
                _aiming = false;
                _lane.HideMarker();
                _appearance.Visible = false;
                owner.ShootProjectile(_direction);
                _cooldownTimer = owner.RangedCooldown * _cooldownMultiplier;
            }
            return true;
        }

        _cooldownTimer -= delta;
        if (_cooldownTimer > 0f || reach > owner.AttackRange || owner.IsDisoriented || owner.IsAnotherAbilityActive(this))
            return false;

        _aiming = true;
        _timer = _windupSeconds;
        _direction = (player.GlobalPosition - owner.GlobalPosition).Normalized();
        _appearanceDirection = _appearanceSet?.DirectionIndex(_direction) ?? 0;
        _appearanceFrame = -1;
        _appearance.Modulate = Colors.White with { A = CombatFxSettings.EnemyOpacity };
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
        _appearance.Visible = false;
    }

    private void UpdateAppearance()
    {
        if (_appearanceSet == null || _appearanceSet.Fps <= 0f)
            return;
        // L'annonce occupe la fin de la visée existante, sans décaler le départ ni les collisions.
        float elapsed = _appearanceSet.Frames / _appearanceSet.Fps - _timer;
        if (elapsed < 0f)
            return;
        int frame = Mathf.Min((int)(elapsed * _appearanceSet.Fps), _appearanceSet.Frames - 1);
        if (frame == _appearanceFrame)
            return;
        _appearanceFrame = frame;
        _appearance.Texture = _appearanceSet.Get(_appearanceDirection, frame);
        _appearance.Visible = true;
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
