using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

public enum MovementResponse { Direct, Brief }
public enum MobilityState { Locomotion, Mobility, Hurt, Death }

/// <summary>État de locomotion composé ; Player conserve l'entrée et la résolution des collisions.</summary>
public sealed class PlayerMobility
{
    public MobilityConfig Config { get; }
    public MovementResponse Response { get; set; }
    public bool UseInvulnerabilityTrial { get; set; }
    public MobilityState State { get; private set; }
    public float CooldownRemaining { get; private set; }
    public float BufferRemaining { get; private set; }
    public bool StartedThisStep { get; private set; }
    public bool IsDashStep { get; private set; }
    public bool IsDashing => State == MobilityState.Mobility;
    public bool IsInvulnerable => IsDashing && _dashElapsed < Mathf.Min(Config.DurationSeconds,
        UseInvulnerabilityTrial ? Config.TrialInvulnerabilitySeconds : Config.InvulnerabilitySeconds);

    private Vector2 _lastDirection = Vector2.Right;
    private Vector2 _dashDirection;
    private Vector2 _locomotionVelocity;
    private float _dashRemaining;
    private float _dashElapsed;
    private float _hurtRemaining;
    private bool _requestPending;

    public PlayerMobility(MobilityConfig config)
    {
        Config = config;
        Response = config.BriefResponse ? MovementResponse.Brief : MovementResponse.Direct;
    }

    public void Request()
    {
        if (State is MobilityState.Death or MobilityState.Hurt)
            return;
        _requestPending = true;
        BufferRemaining = Config.InputBufferSeconds;
    }

    public Vector2 Step(float delta, Vector2 input, float speed, float terrainFactor, bool allowed)
    {
        StartedThisStep = false;
        IsDashStep = false;
        if (!allowed || State == MobilityState.Death)
        {
            Suspend();
            return Vector2.Zero;
        }
        if (delta <= 0f)
            return Vector2.Zero;

        input = input.LimitLength();
        if (input != Vector2.Zero)
            _lastDirection = input.Normalized();
        bool requestSurvivesCooldown = _requestPending &&
            (CooldownRemaining <= 0f || CooldownRemaining <= BufferRemaining + 0.00001f);
        CooldownRemaining = Mathf.Max(0f, CooldownRemaining - delta);
        if (CooldownRemaining <= 0.00001f)
            CooldownRemaining = 0f;
        _hurtRemaining = Mathf.Max(0f, _hurtRemaining - delta);
        if (State == MobilityState.Hurt && _hurtRemaining == 0f)
            State = MobilityState.Locomotion;
        if (IsDashing && _dashRemaining <= 0.00001f)
            StopDash();

        if (requestSurvivesCooldown && State == MobilityState.Locomotion && CooldownRemaining <= 0.00001f)
        {
            State = MobilityState.Mobility;
            _dashDirection = _lastDirection;
            _dashRemaining = Config.DurationSeconds;
            _dashElapsed = 0f;
            CooldownRemaining = Config.CooldownSeconds;
            ClearBuffer();
            StartedThisStep = true;
        }

        if (_requestPending)
        {
            BufferRemaining = Mathf.Max(0f, BufferRemaining - delta);
            if (BufferRemaining == 0f)
                ClearBuffer();
        }

        if (IsDashing)
        {
            IsDashStep = true;
            float activeDelta = Mathf.Min(delta, _dashRemaining);
            _dashRemaining = Mathf.Max(0f, _dashRemaining - activeDelta);
            _dashElapsed += activeDelta;
            // La dernière fraction de tick conserve la distance prévue, même hors 60 Hz.
            return _dashDirection * (speed * terrainFactor * Config.SpeedMultiplier * activeDelta / delta);
        }

        if (speed <= 0f || terrainFactor <= 0f)
        {
            _locomotionVelocity = Vector2.Zero;
            return Vector2.Zero;
        }
        Vector2 target = input * (speed * terrainFactor);
        if (Response == MovementResponse.Direct)
            _locomotionVelocity = target;
        else
        {
            // Un demi-tour ne conserve jamais une poussée dans le sens abandonné.
            if (target.Dot(_locomotionVelocity) < 0f)
                _locomotionVelocity = Vector2.Zero;
            float responseTime = target.LengthSquared() < _locomotionVelocity.LengthSquared()
                ? Config.BrakingSeconds : Config.AccelerationSeconds;
            _locomotionVelocity = _locomotionVelocity.MoveToward(target, speed * terrainFactor * delta / responseTime);
        }
        return _locomotionVelocity;
    }

    public void FinishMovement(Vector2 actualVelocity)
    {
        // Ne pas accumuler une vitesse virtuelle contre le mur dans la variante progressive.
        if (!IsDashStep)
            _locomotionVelocity = actualVelocity;
    }

    public void StopDash()
    {
        if (IsDashing)
            State = MobilityState.Locomotion;
        _dashRemaining = 0f;
        _locomotionVelocity = Vector2.Zero;
    }

    public void Hurt(float duration)
    {
        if (State == MobilityState.Death)
            return;
        StopDash();
        ClearBuffer();
        _hurtRemaining = duration;
        State = MobilityState.Hurt;
    }

    public void Suspend()
    {
        StopDash();
        ClearBuffer();
        StartedThisStep = false;
        IsDashStep = false;
    }

    public void Die()
    {
        Suspend();
        State = MobilityState.Death;
    }

    private void ClearBuffer()
    {
        _requestPending = false;
        BufferRemaining = 0f;
    }
}
