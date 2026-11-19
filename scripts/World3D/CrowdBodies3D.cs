using Godot;

namespace Vestiges.World3D;

/// <summary>
/// Les corps de la horde à l'écran (plan 60, 60.2a) : un <see cref="MeshInstance3D"/> sans script et une ombre par place
/// de la horde, posés une fois par image. La pose suit l'état : marche (au pas de sa course), repos à l'arrêt, attaque
/// après un coup ou un tir, mort ; le corps se tourne vers sa course et blanchit au coup. Les tirs des créatures à distance
/// sont de petites boules lumineuses.
/// </summary>
public sealed class CrowdBodies3D
{
    private const float TurnRate = 14f;
    // Pas de marche par cellule parcourue, comme le joueur : les pieds ne glissent pas.
    private const float WalkCyclesPerCell = 0.43f;
    private const float ShadowSize = 0.9f;
    // Sous cette vitesse (cellules par seconde), la créature est à l'arrêt et se met au repos.
    private const float StillCellsPerSecond = 0.05f;
    // Sous cette hauteur, la créature est tombée dans le vide : on ne la montre plus.
    private const float HiddenBelow = -2.5f;

    private readonly ReliefArena _arena;
    private readonly ReliefConfig _config;
    private readonly StandardMaterial3D _flash;
    private readonly MeshInstance3D[] _bodies;
    private readonly Sprite3D[] _shadows;
    private readonly float[] _yaw;
    private readonly float[] _walkPhase;
    private readonly Vector3[] _last;
    private readonly MeshInstance3D[] _shots;
    private float _clock;

    public CrowdBodies3D(Node3D world, ReliefConfig config, ReliefArena arena, int capacity, StandardMaterial3D flash)
    {
        _arena = arena;
        _config = config;
        _flash = flash;
        _bodies = new MeshInstance3D[capacity];
        _shadows = new Sprite3D[capacity];
        _yaw = new float[capacity];
        _walkPhase = new float[capacity];
        _last = new Vector3[capacity];
        Node3D group = new() { Name = "Crowd" };
        world.AddChild(group);
        Vector3 scale = Vector3.One * config.LowPoly.BodyScale / config.LowPoly.MetresPerCell;
        for (int i = 0; i < capacity; i++)
        {
            _shadows[i] = ReliefShadow.Create(group, $"Shadow{i}");
            _bodies[i] = new MeshInstance3D { Name = $"Creature{i}", Scale = scale, Visible = false };
            group.AddChild(_bodies[i]);
            // Décalage de phase propre à chaque place : la horde ne marche pas au pas.
            _walkPhase[i] = i * 0.37f;
        }
        SphereMesh ball = new()
        {
            Radius = config.ShotRadius,
            Height = config.ShotRadius * 2f,
            RadialSegments = 8,
            Rings = 4,
            Material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = config.ShotColor,
                EmissionEnabled = true,
                Emission = config.ShotColor,
            },
        };
        _shots = new MeshInstance3D[Crowd3D.MaxShots];
        for (int p = 0; p < _shots.Length; p++)
        {
            _shots[p] = new MeshInstance3D { Name = $"Shot{p}", Mesh = ball, Visible = false, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
            group.AddChild(_shots[p]);
        }
    }

    public void Update(float dt, Crowd3D crowd, ReliefLowPoly.Model[] models)
    {
        _clock += dt;
        float turn = 1f - Mathf.Exp(-TurnRate * dt);
        for (int p = 0; p < _shots.Length; p++)
        {
            bool on = crowd.ShotActive[p];
            if (_shots[p].Visible != on)
                _shots[p].Visible = on;
            if (on)
                _shots[p].Position = new Vector3(crowd.ShotPos[p].X, crowd.ShotPos[p].Z, crowd.ShotPos[p].Y);
        }
        for (int i = 0; i < _bodies.Length; i++)
        {
            MeshInstance3D body = _bodies[i];
            Sprite3D shadow = _shadows[i];
            Crowd3D.Life state = crowd.State[i];
            Vector3 at = crowd.Position[i];
            if (state == Crowd3D.Life.Inactive || at.Z < HiddenBelow)
            {
                body.Visible = false;
                shadow.Visible = false;
                continue;
            }
            ReliefLowPoly.Model model = models[crowd.Kind[i]];
            if (!body.Visible)
                _last[i] = at;
            Vector2 moved = new(at.X - _last[i].X, at.Y - _last[i].Y);
            _last[i] = at;
            // Une créature qui grimpe sur place agite encore ses pattes.
            _walkPhase[i] += crowd.Climbing[i] ? dt : moved.Length() * WalkCyclesPerCell;
            bool still = !crowd.Climbing[i] && moved.Length() < dt * StillCellsPerSecond;
            Mesh pose = state == Crowd3D.Life.Dying
                ? model.FrameAt("death", 1f - crowd.Timer[i] / Crowd3D.DyingSeconds)
                : crowd.Strike[i] > 0f ? model.FrameAt("attack", 1f - crowd.Strike[i] / Crowd3D.StrikeSeconds)
                : still ? model.Frame("idle", _clock + i * 0.37f)
                : model.Frame("walk", _walkPhase[i]);
            if (body.Mesh != pose)
                body.Mesh = pose;
            Vector2 facing = crowd.Facing[i];
            if (state != Crowd3D.Life.Dying && facing.LengthSquared() > 0.0001f)
                _yaw[i] = Mathf.LerpAngle(_yaw[i], Mathf.Atan2(facing.X, facing.Y), turn);
            body.Visible = true;
            body.Position = new Vector3(at.X, at.Z, at.Y);
            body.Rotation = new Vector3(0f, _yaw[i], 0f);
            Material material = crowd.Flash[i] > 0f ? _flash : null;
            if (body.MaterialOverride != material)
                body.MaterialOverride = material;
            ReliefShadow.Place(shadow, _arena, at, ShadowSize * _config.LowPoly.BodyScale);
        }
    }
}
