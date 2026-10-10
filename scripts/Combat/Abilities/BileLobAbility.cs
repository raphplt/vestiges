using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Crachat en cloche (Cracheur, plan 33) : la créature s'arrête, une goutte part en arc vers la place du joueur, annoncée
/// au sol pendant tout son vol, puis laisse une flaque qui ralentit qui marche dedans. Rien ne traverse l'écran : on
/// contourne la flaque en se plaçant dans la foule, sans esquive. La flaque disparaît avec son auteur.
/// </summary>
public class BileLobAbility : IEnemyAbility
{
    private const float PuddleSlowRefreshSeconds = 0.2f;
    private const float ImpactFlashSeconds = 0.15f;
    // Au-dessus des corps qu'elle survole, sous le brouillard (10).
    private const int BlobZIndex = 1;

    // Plafond partagé : une grappe de Cracheurs ne doit pas couvrir l'écran de cercles.
    // Portée processus : équilibré par Land/Cancel ; à rattacher à l'instance de run si le coop en lance plusieurs.
    private static int _activeLobs;

    private readonly Enemy _owner;
    private readonly GroundTelegraph _target;
    private readonly GroundTelegraph _puddle;
    private readonly Sprite2D _blob;
    private ProjectileSprites.SpriteSet _blobSet;
    private int _blobDirection;
    private int _blobFrame = -1;

    private float _castRange;
    private float _windupSeconds;
    private float _flightSeconds;
    private float _arcHeight;
    private float _radius;
    private float _hitMargin;
    private float _cooldownSeconds;
    private float _damageMultiplier;
    private float _puddleSeconds;
    private float _puddleSlow;
    private int _maxSimultaneous;
    private FxFamily _family;
    private Color _flash;
    private string _castAudio;
    private string _impactAudio;

    private bool _aiming;
    private bool _flying;
    private float _timer;
    private float _cooldownTimer;
    private float _puddleTimer;
    private float _flashTimer;
    private Vector2 _origin;
    private Vector2 _impactCenter;

    public bool ReplacesBaseAttack => true;
    public bool IsActive => _aiming;

    public BileLobAbility(Enemy owner)
    {
        _owner = owner;
        _target = new GroundTelegraph { Name = "BileTarget" };
        owner.AddChild(_target);
        _puddle = new GroundTelegraph { Name = "BilePuddle" };
        owner.AddChild(_puddle);
        _blob = new Sprite2D
        {
            Name = "BileBlob", TopLevel = true, ZAsRelative = false, ZIndex = BlobZIndex,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest, Visible = false,
        };
        owner.AddChild(_blob);
    }

    public void Configure(EnemyAbilityData data)
    {
        _castRange = data.Number("cast_range");
        _windupSeconds = data.Number("windup_seconds");
        _flightSeconds = data.Number("flight_seconds");
        _arcHeight = data.Number("arc_height");
        _radius = data.Number("radius");
        _hitMargin = data.Number("player_hit_margin");
        _cooldownSeconds = data.Number("cooldown_seconds");
        _damageMultiplier = data.Number("damage_multiplier");
        _puddleSeconds = data.Number("puddle_seconds");
        _puddleSlow = data.Number("puddle_slow_multiplier");
        _maxSimultaneous = (int)data.Number("max_simultaneous");
        _family = PixelPalette.ParseFamily(data.Text("fx_family"), FxFamily.Rust);
        _flash = PixelPalette.Ramp(_family).Light;
        _castAudio = data.Text("cast_audio");
        _impactAudio = data.Text("impact_audio");

        Reset();
        _blobSet = ProjectileSprites.Get(_owner.ProjectileSpriteId) ?? ProjectileSprites.Get("spit");
        // Décalage initial : des Cracheurs apparus en grappe ne crachent pas en rythme.
        _cooldownTimer = data.TryGetNumber("first_delay", out float firstDelay)
            ? firstDelay
            : _cooldownSeconds * RunRandom.Behavior.RandfRange(0.4f, 1.0f);
    }

    public bool Process(Enemy owner, Player player, float distToPlayer, float delta)
    {
        UpdatePuddle(player, delta);

        if (_flying)
        {
            _timer -= delta;
            _target.SetProgress(1f - _timer / _flightSeconds);
            UpdateBlob();
            if (_timer <= 0f)
                Land(owner, player);
        }

        if (_aiming)
        {
            _timer -= delta;
            owner.Velocity = Vector2.Zero;
            if (_timer <= 0f)
                Launch();
            return true;
        }

        _cooldownTimer -= delta;
        if (_flying || _cooldownTimer > 0f || distToPlayer > _castRange || owner.IsDisoriented
            || owner.IsAnotherAbilityActive(this) || _activeLobs >= _maxSimultaneous)
            return false;

        // La cible est la place du joueur au départ du geste : qui avance n'est jamais touché, la flaque reste derrière.
        _aiming = true;
        _timer = _windupSeconds;
        _origin = owner.GlobalPosition;
        _impactCenter = player.GlobalPosition;
        _activeLobs++;
        owner.Velocity = Vector2.Zero;
        owner.PlayAttackAnim();
        owner.FlashWarning(_flash, _windupSeconds);
        return true;
    }

    public void Cancel()
    {
        // Appelé à chaque tick hors écran ou en errance : rien à défaire, rien à toucher.
        if (_aiming || _flying || _puddleTimer > 0f || _flashTimer > 0f)
            Reset();
    }

    private void Reset()
    {
        if (_aiming || _flying)
            _activeLobs--;
        _aiming = false;
        _flying = false;
        _timer = 0f;
        _puddleTimer = 0f;
        _flashTimer = 0f;
        _target.HideMarker();
        _puddle.HideMarker();
        _blob.Visible = false;
    }

    private void Launch()
    {
        _aiming = false;
        _flying = true;
        _timer = _flightSeconds;
        _origin = _owner.GlobalPosition;
        _target.ShowCircle(_impactCenter, _radius, _family);
        _blobDirection = _blobSet?.DirectionIndex(_impactCenter - _origin) ?? 0;
        _blobFrame = -1;
        _blob.Modulate = Colors.White with { A = CombatFxSettings.EnemyOpacity };
        UpdateBlob();
        _blob.Visible = _blobSet != null;
        if (_castAudio.Length > 0)
            _owner.PlayNearbyAudio(_castAudio);
    }

    private void Land(Enemy owner, Player player)
    {
        _flying = false;
        _activeLobs--;
        _cooldownTimer = _cooldownSeconds;
        _blob.Visible = false;
        _target.SetProgress(1f);
        _target.SetFlash(1f);
        _flashTimer = ImpactFlashSeconds;
        CombatPools.Instance?.ShowProjectileImpact(_impactCenter, _blobSet?.Impact);
        if (_impactAudio.Length > 0)
            owner.PlayNearbyAudio(_impactAudio);

        if (_puddleSeconds > 0f)
        {
            _puddle.ShowCircle(_impactCenter, _radius, _family);
            _puddle.SetProgress(1f);
            _puddleTimer = _puddleSeconds;
        }

        float reach = _radius + _hitMargin;
        if (GodotObject.IsInstanceValid(player) && Iso.GroundDistanceSquared(player.GlobalPosition, _impactCenter) <= reach * reach)
            owner.HitPlayer(player, owner.Damage * _damageMultiplier);
    }

    /// <summary>Goutte en cloche : position au sol interpolée, hauteur en parabole, planche orientée selon le trajet.</summary>
    private void UpdateBlob()
    {
        if (_blobSet == null)
            return;
        float t = 1f - Mathf.Clamp(_timer / _flightSeconds, 0f, 1f);
        Vector2 ground = _origin.Lerp(_impactCenter, t);
        float height = Iso.FlightHeight + 4f * _arcHeight * t * (1f - t);
        _blob.GlobalPosition = ground + new Vector2(0f, -height);
        int frame = _blobSet.FrameAt(t * _flightSeconds);
        if (frame == _blobFrame)
            return;
        _blobFrame = frame;
        _blob.Texture = _blobSet.Get(_blobDirection, frame);
    }

    private void UpdatePuddle(Player player, float delta)
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= delta;
            if (_flashTimer <= 0f)
                _target.HideMarker();
            else
                _target.SetFlash(_flashTimer / ImpactFlashSeconds);
        }

        if (_puddleTimer <= 0f)
            return;
        _puddleTimer -= delta;
        if (_puddleTimer <= 0f)
        {
            _puddle.HideMarker();
            return;
        }
        if (GodotObject.IsInstanceValid(player) && Iso.GroundDistanceSquared(player.GlobalPosition, _impactCenter) <= _radius * _radius)
            player.ApplySlow(_puddleSlow, PuddleSlowRefreshSeconds);
    }
}
