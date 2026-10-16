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
#if TOOLS
    private bool _useInvulnerabilityTrial;
    public bool UseInvulnerabilityTrial
    {
        get => _useInvulnerabilityTrial;
        set { DevelopmentMode.RequireTestAccess(); _useInvulnerabilityTrial = value; }
    }
#endif
    public MobilityState State { get; private set; }
    public float CooldownRemaining { get; private set; }

    /// <summary>Vitesse de recharge du dash (1 = réglage de base) : le temps de recharge est divisé par ce facteur.</summary>
    public float RechargeMultiplier { get; set; } = 1f;

    /// <summary>Temps de recharge effectif d'un dash, recharge accélérée comprise : base des jauges.</summary>
    public float CooldownSeconds => Config.CooldownSeconds / Mathf.Max(0.01f, RechargeMultiplier);

    /// <summary>Longueur du dash (1 = réglage de base) : même durée, vitesse accrue (Lacet rouge, palier 25).</summary>
    public float DistanceMultiplier { get; set; } = 1f;

    /// <summary>
    /// Plan de métro (plan 35 I6) : au-dessus de zéro, le dash devient un saut instantané, parcouru en une seule image,
    /// de cette fraction de la longueur d'un dash. L'invulnérabilité du dash, s'il en a, est gardée et comptée depuis le
    /// saut.
    /// </summary>
    public float BlinkDistance { get; set; }

    /// <summary>Le dash commencé à cette image est un saut instantané.</summary>
    public bool BlinkedThisStep { get; private set; }

    /// <summary>
    /// Dashs commencés depuis la création, sauts compris : un lecteur qui tourne en _Process voit ainsi un saut, qui
    /// ne dure jamais jusqu'à son image.
    /// </summary>
    public int DashesStarted { get; private set; }

    /// <summary>Temps écoulé depuis la fin du dernier dash ; nul pendant un dash, infini avant le premier.</summary>
    public float SecondsSinceDash { get; private set; } = float.MaxValue;
    public float BufferRemaining { get; private set; }
    public bool StartedThisStep { get; private set; }
    public bool IsDashStep { get; private set; }
    public bool IsDashing => State == MobilityState.Mobility;
    /// <summary>Arc visuel ; interruption et saut instantané remettent immédiatement la hauteur à zéro.</summary>
    public float VisualHeight => IsDashing && !BlinkedThisStep
        ? Config.VisualHeightPixels * Mathf.Max(0f, Mathf.Sin(Mathf.Pi * Mathf.Clamp(_dashElapsed / Config.DurationSeconds, 0f, 1f)))
        : 0f;
    public bool IsInvulnerable => IsDashing && _dashElapsed < Mathf.Min(Config.DurationSeconds, InvulnerabilitySeconds)
        || _blinkInvulnerableRemaining > 0f;

    private float InvulnerabilitySeconds
    {
        get
        {
#if TOOLS
            if (UseInvulnerabilityTrial)
                return Config.TrialInvulnerabilitySeconds;
#endif
            return Config.InvulnerabilitySeconds;
        }
    }

    private Vector2 _lastDirection = Vector2.Right;
    private Vector2 _dashDirection;
    private Vector2 _locomotionVelocity;
    private float _dashRemaining;
    private float _dashElapsed;
    private float _hurtRemaining;
    private float _blinkInvulnerableRemaining;
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

    public Vector2 Step(float delta, Vector2 input, float speed, float terrainFactor, bool allowed,
        float? mobilityTerrainFactor = null)
    {
        StartedThisStep = false;
        IsDashStep = false;
        BlinkedThisStep = false;
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
        _blinkInvulnerableRemaining = Mathf.Max(0f, _blinkInvulnerableRemaining - delta);
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
            CooldownRemaining = CooldownSeconds;
            ClearBuffer();
            StartedThisStep = true;
            DashesStarted++;
            if (BlinkDistance > 0f)
            {
                // Toute la longueur du dash en une image : le joueur la résout encore contre les murs et le Néant.
                BlinkedThisStep = true;
                _blinkInvulnerableRemaining = Mathf.Min(Config.DurationSeconds, InvulnerabilitySeconds);
                SecondsSinceDash = 0f;
                IsDashStep = true;
                float length = speed * (mobilityTerrainFactor ?? terrainFactor) * Config.SpeedMultiplier * DistanceMultiplier * Config.DurationSeconds;
                // Le saut est fini dès cette image : aucun lecteur ne le voit durer.
                State = MobilityState.Locomotion;
                _dashRemaining = 0f;
                _dashElapsed = Config.DurationSeconds;
                return _dashDirection * (length * BlinkDistance / delta);
            }
        }

        if (_requestPending)
        {
            BufferRemaining = Mathf.Max(0f, BufferRemaining - delta);
            if (BufferRemaining == 0f)
                ClearBuffer();
        }

        SecondsSinceDash = IsDashing ? 0f : SecondsSinceDash + delta;
        if (IsDashing)
        {
            IsDashStep = true;
            float activeDelta = Mathf.Min(delta, _dashRemaining);
            _dashRemaining = Mathf.Max(0f, _dashRemaining - activeDelta);
            _dashElapsed += activeDelta;
            if (Config.TurnRateDegrees > 0f && input != Vector2.Zero)
            {
                // Glissade : la direction suit l'entrée, à vitesse angulaire bornée (elle tourne large).
                float turn = Mathf.DegToRad(Config.TurnRateDegrees) * activeDelta;
                _dashDirection = _dashDirection.Rotated(Mathf.Clamp(_dashDirection.AngleTo(input), -turn, turn));
            }
            // La dernière fraction de tick conserve la distance prévue, même hors 60 Hz.
            return _dashDirection * (speed * (mobilityTerrainFactor ?? terrainFactor) * Config.SpeedMultiplier * DistanceMultiplier * activeDelta / delta);
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

    /// <summary>Rend une part de la recharge pleine du dash (esquive parfaite, plan 52 E2), sans descendre sous zéro.</summary>
    public void RefundCooldown(float fraction)
    {
        CooldownRemaining = Mathf.Max(0f, CooldownRemaining - CooldownSeconds * fraction);
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
        _blinkInvulnerableRemaining = 0f;
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
