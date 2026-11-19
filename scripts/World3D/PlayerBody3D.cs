using Godot;

namespace Vestiges.World3D;

/// <summary>
/// Le corps du joueur à l'écran (plan 60, 60.1b) : la pose de l'animation en cours du modèle low ou mid poly, tourné vers
/// sa course, blanc qui clignote pendant l'invulnérabilité, rouge à la blessure, et son ombre au sol.
/// </summary>
public sealed class PlayerBody3D
{
    // Pas de marche par cellule parcourue : les pieds ne glissent pas au sol.
    private const float WalkCyclesPerCell = 0.43f;
    private const float TurnRate = 14f;
    private const float ShadowSize = 1.1f;

    private readonly MeshInstance3D _body;
    private readonly Sprite3D _shadow;
    private readonly ReliefArena _arena;
    private readonly ReliefConfig _config;
    private readonly StandardMaterial3D _flash;
    private readonly StandardMaterial3D _hurt;
    private float _yaw;
    private float _walkPhase;
    private float _clock;

    public PlayerBody3D(Node3D world, ReliefConfig config, ReliefArena arena, StandardMaterial3D flash, StandardMaterial3D hurt)
    {
        _config = config;
        _arena = arena;
        _flash = flash;
        _hurt = hurt;
        _shadow = ReliefShadow.Create(world, "PlayerShadow");
        _body = new MeshInstance3D { Name = "Player", Scale = Vector3.One * config.LowPoly.BodyScale / config.LowPoly.MetresPerCell };
        world.AddChild(_body);
    }

    public MeshInstance3D Node => _body;

    public void Update(float dt, PlayerMotor3D motor, ReliefLowPoly.Model model)
    {
        _clock += dt;
        float turn = 1f - Mathf.Exp(-TurnRate * dt);
        bool moving = motor.Velocity.LengthSquared() > 0.05f;
        _walkPhase += dt * motor.Velocity.Length() * WalkCyclesPerCell;
        Mesh pose = motor.AirborneFromJump
            ? model.FrameAt("dash", 1f - Mathf.Clamp(motor.Position.Z - motor.GroundUnder, 0f, 1f))
            : motor.SkillTime > 0f ? model.FrameAt("dash", 0.5f)
            : moving ? model.Frame("walk", _walkPhase) : model.Frame("idle", _clock);
        if (_body.Mesh != pose)
            _body.Mesh = pose;
        _yaw = Mathf.LerpAngle(_yaw, Mathf.Atan2(motor.Facing.X, motor.Facing.Y), turn);
        _body.Position = motor.WorldPosition;
        _body.Rotation = new Vector3(0f, _yaw, 0f);
        bool blink = motor.Invulnerable > 0f && !motor.AirborneFromJump && (int)(_clock * 16f) % 2 == 0;
        _body.MaterialOverride = motor.HurtFlash > 0f ? _hurt : blink ? _flash : null;
        ReliefShadow.Place(_shadow, _arena, motor.Position, ShadowSize * _config.LowPoly.BodyScale, _config.PlayerRadius);
    }
}
