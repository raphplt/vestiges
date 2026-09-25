using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Bond annoncé : l'ennemi s'accroupit en montrant sa trajectoire, puis bondit plus vite que le joueur
/// dans la direction verrouillée au début de l'annonce. Un pas de côté pendant l'annonce l'évite ;
/// la récupération qui suit laisse une fenêtre pour riposter.
/// </summary>
public class PounceAbility : IEnemyAbility
{
    private enum Phase { Ready, Windup, Leap, Recovery }

    private readonly Enemy _owner;
    private readonly GroundTelegraph _marker;

    private float _triggerRange;
    private float _minRange;
    private float _windupSeconds;
    private float _distance;
    private float _leapSeconds;
    private float _recoverySeconds;
    private float _cooldownSeconds;
    private float _hitRange;
    private float _damageMultiplier;
    private float _markerWidth;
    private FxFamily _family;
    private string _windupAudio;
    private string _leapAudio;

    private Phase _phase;
    private float _timer;
    private float _cooldownTimer;
    private Vector2 _direction;
    private bool _hasHit;

    public bool ReplacesBaseAttack => false;

    public PounceAbility(Enemy owner)
    {
        _owner = owner;
        _marker = new GroundTelegraph { Name = "PounceMarker" };
        owner.AddChild(_marker);
    }

    public void Configure(EnemyAbilityData data)
    {
        _triggerRange = data.GetNumber("trigger_range", 150f);
        _minRange = data.GetNumber("min_range", 50f);
        _windupSeconds = Mathf.Max(0.05f, data.GetNumber("windup_seconds", 0.4f));
        _distance = data.GetNumber("distance", 120f);
        _leapSeconds = Mathf.Max(0.05f, data.GetNumber("leap_seconds", 0.18f));
        _recoverySeconds = data.GetNumber("recovery_seconds", 0.35f);
        _cooldownSeconds = data.GetNumber("cooldown_seconds", 3.5f);
        _hitRange = data.GetNumber("hit_range", 30f);
        _damageMultiplier = data.GetNumber("damage_multiplier", 1.2f);
        _markerWidth = data.GetNumber("marker_width", 10f);
        _family = PixelPalette.ParseFamily(data.GetText("fx_family", "hostile"), FxFamily.Hostile);
        _windupAudio = data.GetText("windup_audio", "");
        _leapAudio = data.GetText("leap_audio", "");

        _phase = Phase.Ready;
        _marker.HideMarker();
        _cooldownTimer = _cooldownSeconds * (float)GD.RandRange(0.3, 1.0);
    }

    public bool Process(Enemy owner, Player player, float distToPlayer, float delta)
    {
        switch (_phase)
        {
            case Phase.Windup:
                _timer -= delta;
                _marker.SetProgress(1f - _timer / _windupSeconds);
                owner.Velocity = Vector2.Zero;
                if (_timer <= 0f)
                    StartLeap(owner);
                return true;

            case Phase.Leap:
                _timer -= delta;
                owner.Velocity = _direction * (_distance / _leapSeconds) * owner.SlowFactor;
                if (!_hasHit && owner.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) <= _hitRange * _hitRange)
                {
                    _hasHit = true;
                    owner.MeleeHitPlayer(player, owner.Damage * _damageMultiplier);
                }
                if (_timer <= 0f)
                {
                    _phase = Phase.Recovery;
                    _timer = _recoverySeconds;
                }
                return true;

            case Phase.Recovery:
                _timer -= delta;
                owner.Velocity = Vector2.Zero;
                if (_timer <= 0f)
                {
                    _phase = Phase.Ready;
                    _cooldownTimer = _cooldownSeconds;
                }
                return true;
        }

        _cooldownTimer -= delta;
        if (_cooldownTimer > 0f || distToPlayer > _triggerRange || distToPlayer < _minRange || owner.IsDisoriented)
            return false;

        StartWindup(owner, player);
        return true;
    }

    public void Cancel()
    {
        if (_phase == Phase.Windup)
            _owner.SetWindupPose(false);

        _phase = Phase.Ready;
        _timer = 0f;
        _marker.HideMarker();
    }

    private void StartWindup(Enemy owner, Player player)
    {
        _direction = (player.GlobalPosition - owner.GlobalPosition).Normalized();
        _phase = Phase.Windup;
        _timer = _windupSeconds;
        owner.Velocity = Vector2.Zero;
        owner.SetWindupPose(true);
        _marker.ShowLine(owner.GlobalPosition, owner.GlobalPosition + _direction * _distance, _markerWidth, _family);
        if (_windupAudio.Length > 0)
            AudioManager.Play(_windupAudio, 0.1f, -6f);
    }

    private void StartLeap(Enemy owner)
    {
        _phase = Phase.Leap;
        _timer = _leapSeconds;
        _hasHit = false;
        owner.SetWindupPose(false);
        owner.PlayAttackAnim();
        _marker.HideMarker();
        if (_leapAudio.Length > 0)
            AudioManager.Play(_leapAudio, 0.1f, -4f);
    }
}
