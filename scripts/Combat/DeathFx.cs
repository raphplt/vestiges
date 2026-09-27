using System;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Traces d'une mort, recyclées par CombatPools (plan 02 J0, J2) : nuage de dissolution qui monte (quatre poses) en dérivant avec le coup,
/// flaque irisée au sol qui s'étend puis s'efface lentement. Animé dans _Process, sans tween ni minuterie.
/// </summary>
public partial class DeathFx : Node2D
{
    private const float CloudSec = 0.6f;
    private const float CloudRiseSec = 0.5f;
    private const float PoolGrowSec = 0.3f;
    private const float PoolHoldSec = 1.0f;
    private const float PoolFadeSec = 4.0f;
    private const float PoolAlpha = 0.9f;

    private static SpriteFrames _cloudFrames;
    private static Texture2D _poolTexture;
    private static ShaderMaterial _poolMaterial;

    private AnimatedSprite2D _cloud;
    private Sprite2D _pool;
    private Action<DeathFx> _release;
    private float _elapsed;
    private float _poolScale;
    private float _duration;
    private Vector2 _drift;

    public static DeathFx Create(Action<DeathFx> release)
    {
        if (_cloudFrames == null)
        {
            _cloudFrames = new SpriteFrames();
            _cloudFrames.AddAnimation("dissolve");
            _cloudFrames.SetAnimationSpeed("dissolve", 8);
            _cloudFrames.SetAnimationLoopMode("dissolve", SpriteFrames.LoopMode.None);
            for (int frame = 1; frame <= 4; frame++)
                _cloudFrames.AddFrame("dissolve", GD.Load<Texture2D>($"res://assets/vfx/vfx_dissolution_f{frame}.png"));
            _poolTexture = GD.Load<Texture2D>("res://assets/vfx/vfx_blood_splatter.png");
            if (ResourceLoader.Exists("res://assets/shaders/iridescent_fluid.gdshader"))
                _poolMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/iridescent_fluid.gdshader") };
        }

        DeathFx fx = new() { Name = "DeathFx", Visible = false, _release = release };
        // Flaque au sol : sous les entités, que le tri en Y ne doit pas faire passer devant des pieds.
        fx._pool = new Sprite2D { Texture = _poolTexture, TextureFilter = TextureFilterEnum.Nearest, Material = _poolMaterial, ZIndex = -1 };
        fx.AddChild(fx._pool);
        fx._cloud = new AnimatedSprite2D { SpriteFrames = _cloudFrames, TextureFilter = TextureFilterEnum.Nearest };
        fx.AddChild(fx._cloud);
        fx.SetProcess(false);
        return fx;
    }

    /// <summary>
    /// Joue les traces d'une mort ; <paramref name="poolScale"/> ≤ 0 : nuage seul, sans flaque. Le nuage dérive
    /// dans le sens du dernier coup (<paramref name="direction"/>, normalisée ou nulle).
    /// </summary>
    public void Play(Vector2 position, float poolScale, Vector2 direction = default)
    {
        GlobalPosition = position;
        _drift = direction * 10f;
        _elapsed = 0f;
        _poolScale = poolScale;
        _duration = poolScale > 0f ? PoolGrowSec + PoolHoldSec + PoolFadeSec : CloudSec;
        _cloud.Visible = true;
        _cloud.Position = new Vector2(0f, -8f);
        _cloud.Play("dissolve");
        _pool.Visible = poolScale > 0f;
        UpdatePool();
        Visible = true;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_cloud.Visible)
        {
            float rise = Mathf.Clamp(_elapsed / CloudRiseSec, 0f, 1f);
            float eased = 1f - (1f - rise) * (1f - rise);
            _cloud.Position = new Vector2(0f, Mathf.Lerp(-8f, -16f, eased)) + _drift * eased;
            if (_elapsed >= CloudSec)
                _cloud.Visible = false;
        }
        if (_pool.Visible)
            UpdatePool();
        if (_elapsed >= _duration)
        {
            Visible = false;
            _cloud.Stop();
            SetProcess(false);
            _release(this);
        }
    }

    private void UpdatePool()
    {
        // S'étend (couchée au sol, deux fois plus large que haute), attend, puis retourne au néant.
        float grow = Mathf.Clamp(_elapsed / PoolGrowSec, 0f, 1f);
        grow = 1f - (1f - grow) * (1f - grow);
        _pool.Scale = new Vector2(Mathf.Lerp(0.5f, _poolScale, grow), Mathf.Lerp(0.25f, _poolScale * 0.5f, grow));
        float fade = Mathf.Clamp((_elapsed - PoolGrowSec - PoolHoldSec) / PoolFadeSec, 0f, 1f);
        _pool.Modulate = new Color(1f, 1f, 1f, PoolAlpha * (1f - fade));
    }
}
