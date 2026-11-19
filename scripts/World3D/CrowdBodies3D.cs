using Godot;

namespace Vestiges.World3D;

/// <summary>
/// Les corps de la horde à l'écran (plan 60, 60.2a) : un <see cref="MeshInstance3D"/> sans script et une ombre par place
/// de la horde, posés une fois par image. La pose suit l'état : marche (au pas de sa course), attaque après un coup,
/// mort ; le corps se tourne vers sa course et blanchit au coup.
/// </summary>
public sealed class CrowdBodies3D
{
    private const float TurnRate = 14f;
    // Pas de marche par cellule parcourue, comme le joueur : les pieds ne glissent pas.
    private const float WalkCyclesPerCell = 0.43f;
    private const float ShadowSize = 0.9f;
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
    }

    public void Update(float dt, Crowd3D crowd, ReliefLowPoly.Model[] models)
    {
        float turn = 1f - Mathf.Exp(-TurnRate * dt);
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
            Mesh pose = state == Crowd3D.Life.Dying
                ? model.FrameAt("death", 1f - crowd.Timer[i] / Crowd3D.DyingSeconds)
                : crowd.Strike[i] > 0f ? model.FrameAt("attack", 1f - crowd.Strike[i] / Crowd3D.StrikeSeconds)
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
