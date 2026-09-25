using System;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Éclat bref recyclé (flash d'impact, flash de tir) : grossit, s'efface, retourne au pool.
/// </summary>
public partial class FlashSprite : Node2D
{
    private Node2D _visual;
    private Vector2 _startScale;
    private Vector2 _endScale;
    private float _growDuration;
    private float _fadeDuration;
    private Tween _tween;
    private Action<FlashSprite> _release;

    public static FlashSprite CreateHitFlash(Action<FlashSprite> release)
    {
        Sprite2D sprite = new() { Texture = VfxFactory.HitFlashTex, TextureFilter = TextureFilterEnum.Nearest };
        return Create(sprite, new Vector2(0.5f, 0.5f), new Vector2(1.5f, 1.5f), 0.08f, 0.12f, release);
    }

    public static FlashSprite CreateMuzzleFlash(Action<FlashSprite> release)
    {
        Polygon2D flash = new()
        {
            Color = new Color(0.7f, 1f, 0.35f, 0.8f),
            Polygon = new Vector2[] { new(-2.5f, 0f), new(6f, -3f), new(11f, 0f), new(6f, 3f) },
        };
        return Create(flash, Vector2.One, new Vector2(1.4f, 1.15f), 0.08f, 0.08f, release);
    }

    private static FlashSprite Create(Node2D visual, Vector2 startScale, Vector2 endScale, float grow, float fade,
                                      Action<FlashSprite> release)
    {
        FlashSprite flash = new()
        {
            _visual = visual,
            _startScale = startScale,
            _endScale = endScale,
            _growDuration = grow,
            _fadeDuration = fade,
            _release = release,
            Visible = false,
        };
        flash.AddChild(visual);
        return flash;
    }

    public void Play(Vector2 position, float angle)
    {
        GlobalPosition = position;
        Rotation = angle;
        Visible = true;
        _visual.Scale = _startScale;
        _visual.Modulate = Colors.White;
        _tween?.Kill();
        _tween = CreateTween();
        _tween.SetParallel();
        _tween.TweenProperty(_visual, "scale", _endScale, _growDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(_visual, "modulate:a", 0f, _fadeDuration);
        _tween.Chain().TweenCallback(Callable.From(Finish));
    }

    private void Finish()
    {
        Visible = false;
        _release(this);
    }
}
