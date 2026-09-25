using System;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Étincelles et éclats de combat en pixels pleins : un seul nœud par scène de run, un tableau fixe
/// de particules et un seul tracé par frame. Aucun nœud créé par coup (plan 08, effets d'attaque).
/// Positions arrondies au texel, couleur qui descend la rampe de palette au lieu de s'estomper.
/// </summary>
public partial class PixelSparks : Node2D
{
    private const int Capacity = 1024;
    private const float Gravity = 520f;
    private const float Drag = 5f;

    private readonly Vector2[] _position = new Vector2[Capacity];
    private readonly Vector2[] _velocity = new Vector2[Capacity];
    private readonly float[] _height = new float[Capacity];
    private readonly float[] _heightVelocity = new float[Capacity];
    private readonly float[] _life = new float[Capacity];
    private readonly float[] _maxLife = new float[Capacity];
    private readonly byte[] _family = new byte[Capacity];
    private readonly byte[] _size = new byte[Capacity];
    private readonly bool[] _ballistic = new bool[Capacity];
    private readonly bool[] _hostile = new bool[Capacity];
    private readonly Random _random = new();
    private int _next;
    private int _active;

    public int ActiveCount => _active;

    public override void _Ready()
    {
        ZIndex = 5;
        SetProcess(false);
    }

    public void Emit(Vector2 origin, in SparkBurst burst)
    {
        ParticleLevel level = CombatFxSettings.ParticleLevel;
        if (level == ParticleLevel.Off || (burst.Owner == FxOwner.Player && !CombatFxSettings.PlayerAttackFx))
            return;
        int count = level == ParticleLevel.Reduced ? Mathf.Max(1, burst.Count / 2) : burst.Count;
        float baseAngle = burst.Direction == Vector2.Zero ? 0f : burst.Direction.Angle();
        float spread = burst.Direction == Vector2.Zero ? Mathf.Tau : burst.Spread;

        for (int i = 0; i < count; i++)
        {
            int index = _next;
            _next = (_next + 1) % Capacity;
            if (_life[index] <= 0f)
                _active++;

            float angle = baseAngle + (Random01() - 0.5f) * spread;
            float speed = Mathf.Lerp(burst.SpeedMin, burst.SpeedMax, Random01());
            Vector2 velocity = Vector2.FromAngle(angle) * speed;
            _position[index] = origin;
            _ballistic[index] = burst.Ballistic;
            if (burst.Ballistic)
            {
                // Au sol, la profondeur est compressée de moitié ; la hauteur part vers le haut.
                _velocity[index] = new Vector2(velocity.X, velocity.Y * 0.5f) * 0.6f;
                _height[index] = 2f;
                _heightVelocity[index] = speed * (0.5f + 0.5f * Random01());
            }
            else
            {
                _velocity[index] = velocity;
                _height[index] = 0f;
                _heightVelocity[index] = 0f;
            }
            float life = Mathf.Lerp(burst.LifeMin, burst.LifeMax, Random01());
            _life[index] = life;
            _maxLife[index] = life;
            _family[index] = (byte)burst.Family;
            _size[index] = (byte)Mathf.Clamp(burst.Size, 1, 3);
            _hostile[index] = burst.Owner == FxOwner.Enemy;
        }
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        float damping = Mathf.Exp(-Drag * dt);
        int alive = 0;
        for (int i = 0; i < Capacity; i++)
        {
            if (_life[i] <= 0f)
                continue;
            _life[i] -= dt;
            if (_life[i] <= 0f)
                continue;
            alive++;
            _position[i] += _velocity[i] * dt;
            if (_ballistic[i])
            {
                _heightVelocity[i] -= Gravity * dt;
                _height[i] += _heightVelocity[i] * dt;
                if (_height[i] < 0f)
                {
                    _height[i] = 0f;
                    _heightVelocity[i] = -_heightVelocity[i] * 0.35f;
                    _velocity[i] *= 0.5f;
                }
            }
            else
            {
                _velocity[i] *= damping;
            }
        }
        _active = alive;
        QueueRedraw();
        if (alive == 0)
            SetProcess(false);
    }

    public override void _Draw()
    {
        if (_active == 0)
            return;
        float playerOpacity = CombatFxSettings.PlayerOpacity;
        float enemyOpacity = CombatFxSettings.EnemyOpacity;
        for (int i = 0; i < Capacity; i++)
        {
            if (_life[i] <= 0f)
                continue;
            FxRamp ramp = PixelPalette.Ramp((FxFamily)_family[i]);
            float age = 1f - _life[i] / _maxLife[i];
            Color color = age < 0.3f ? ramp.Light : age < 0.65f ? ramp.Mid : ramp.Dark;
            color.A = _hostile[i] ? enemyOpacity : playerOpacity;
            Vector2 corner = new(Mathf.Floor(_position[i].X), Mathf.Floor(_position[i].Y - _height[i]));
            float size = _size[i];
            // Les éclats rétrécissent d'un texel en fin de vie plutôt que de devenir transparents.
            if (size > 1f && age > 0.7f)
                size -= 1f;
            DrawRect(new Rect2(corner, new Vector2(size, size)), color);
        }
    }

    private float Random01() => (float)_random.NextDouble();
}
