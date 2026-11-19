using Godot;

namespace Vestiges.World3D;

/// <summary>
/// La caméra libre de la run 3D (plan 60, §234 et §28) : en perspective derrière le joueur, à distance fixe, tournée à la
/// souris de gauche à droite et de haut en bas entre deux inclinaisons. Monter la souris abaisse la caméra et relève le
/// regard vers l'horizon. Elle se rapproche seule quand le relief s'interpose. Les entrées de déplacement se lisent
/// dans son repère : le haut de l'écran est devant elle.
/// </summary>
public sealed class RunCamera3D
{
    private const float FollowRate = 9f;
    private const float ApproachRate = 25f;
    private const float RecedeRate = 4f;

    private readonly Camera3D _camera;
    private readonly ReliefConfig _config;
    private readonly ReliefArena _arena;
    private float _reach;
    private Vector3 _target;

    public float YawDegrees = 45f;
    public float PitchDegrees;

    public RunCamera3D(Camera3D camera, ReliefConfig config, ReliefArena arena)
    {
        _camera = camera;
        _config = config;
        _arena = arena;
        PitchDegrees = config.ChasePitch;
        _reach = config.ChaseDistance;
        camera.Projection = Camera3D.ProjectionType.Perspective;
        camera.Fov = config.ChaseFov;
    }

    public Camera3D Camera => _camera;

    /// <summary>Le point visé, au-dessus des pieds du joueur (coordonnées Godot), lissé.</summary>
    public Vector3 Target => _target;

    /// <summary>Un déplacement de souris : tourner et incliner.</summary>
    public void Turn(Vector2 relative)
    {
        YawDegrees -= relative.X * _config.ChaseMouseDegrees;
        PitchDegrees = Mathf.Clamp(PitchDegrees + relative.Y * _config.ChaseMouseDegrees, _config.ChaseMinPitch, _config.ChaseMaxPitch);
    }

    /// <summary>Tourner et incliner d'un stick, en degrés par seconde à fond.</summary>
    public void TurnBy(Vector2 stick, float degreesPerSecond, float dt)
    {
        YawDegrees -= stick.X * degreesPerSecond * dt;
        PitchDegrees = Mathf.Clamp(PitchDegrees + stick.Y * degreesPerSecond * dt, _config.ChaseMinPitch, _config.ChaseMaxPitch);
    }

    public void SetPitch(float degrees) => PitchDegrees = Mathf.Clamp(degrees, _config.ChaseMinPitch, _config.ChaseMaxPitch);

    /// <summary>Place aussitôt la caméra derrière ses pieds, sans lissage (départ, téléportation).</summary>
    public void Snap(Vector3 feet)
    {
        _target = Anchor(feet);
        _reach = _config.ChaseDistance;
    }

    public void Follow(float dt, Vector3 feet)
    {
        _target = _target.Lerp(Anchor(feet), 1f - Mathf.Exp(-FollowRate * dt));
        Vector3 back = Back();
        // Rapprochée vite quand le relief s'interpose, éloignée en douceur quand il s'efface.
        float clear = ClearDistance(_target, back, _config.ChaseDistance);
        float rate = clear < _reach ? ApproachRate : RecedeRate;
        _reach = Mathf.Lerp(_reach, clear, 1f - Mathf.Exp(-rate * dt));
        _camera.Position = _target + back * _reach;
        _camera.LookAt(_target, Vector3.Up);
    }

    /// <summary>Entrée à l'écran vers le sol du relief : la droite et le bas de l'écran projetés sur le plan, selon la caméra.</summary>
    public Vector2 ScreenToGround(Vector2 input)
    {
        float yaw = Mathf.DegToRad(YawDegrees);
        // La caméra regarde depuis (sin yaw, cos yaw) vers le centre : le bas de l'écran pointe vers elle.
        Vector2 down = new(Mathf.Sin(yaw), Mathf.Cos(yaw));
        Vector2 right = new(down.Y, -down.X);
        Vector2 wish = right * input.X + down * input.Y;
        return wish.LengthSquared() > 1f ? wish.Normalized() : wish;
    }

    private Vector3 Anchor(Vector3 feet) => feet + new Vector3(0f, _config.ChaseHeight, 0f);

    private Vector3 Back()
    {
        float yaw = Mathf.DegToRad(YawDegrees);
        float pitch = Mathf.DegToRad(PitchDegrees);
        return new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch));
    }

    /// <summary>
    /// Recul sans traverser le relief : le premier point du rayon, du joueur vers la caméra, qui passe sous le sol (plus une
    /// marge) arrête la caméra juste avant lui.
    /// </summary>
    private float ClearDistance(Vector3 target, Vector3 back, float distance)
    {
        const float sampleStep = 0.2f;
        const float minimum = 0.8f;
        int samples = Mathf.CeilToInt(distance / sampleStep);
        for (int s = 1; s <= samples; s++)
        {
            float d = distance * s / samples;
            Vector3 at = target + back * d;
            float ground = _arena.GroundAt(at.X, at.Z);
            if (!ReliefArena.IsVoid(ground) && ground + _config.ChaseClearance > at.Y)
                return Mathf.Max(minimum, distance * (s - 1) / samples);
        }
        return distance;
    }
}
