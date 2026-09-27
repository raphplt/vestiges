using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Signal précurseur d'une Résurgence (V2 §8, plan 03 lot C) : pendant l'avertissement, les bords de l'écran se
/// désaturent en trame, de plus en plus à mesure que la crise approche, et les créatures s'agitent. Pendant la crise
/// l'écran reste terni ; il reprend ses couleurs à l'accalmie. Sous le voile de l'oubli et le HUD ; aucun coût hors
/// avertissement et crise (le calque est masqué).
/// </summary>
public partial class CrisisOmen : CanvasLayer
{
    private const float WarningPeak = 0.8f;
    private const float CrisisIntensity = 0.9f;
    private const float CrisisRampSeconds = 1.2f;
    private const float CalmFadeSeconds = 2.5f;
    // Tempo d'animation des créatures pendant l'avertissement : elles piétinent, nerveuses.
    private const float AgitatedAnimationSpeed = 1.7f;
    private static readonly StringName IntensityParam = "intensity";

    private ShaderMaterial _material;
    private ColorRect _rect;
    private EventBus _eventBus;
    private Tween _tween;
    private float _intensity;

    public override void _Ready()
    {
        Layer = 4;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/crisis_omen.gdshader") };
        _rect = new ColorRect { Material = _material, MouseFilter = Control.MouseFilterEnum.Ignore };
        _rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_rect);
        SetIntensity(0f);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.CrisisWarning += OnCrisisWarning;
        _eventBus.CrisisStarted += OnCrisisStarted;
        _eventBus.CrisisEnded += OnCrisisEnded;
    }

    public override void _ExitTree()
    {
        Enemy.AnimationTempo = 1f;
        if (_eventBus == null)
            return;
        _eventBus.CrisisWarning -= OnCrisisWarning;
        _eventBus.CrisisStarted -= OnCrisisStarted;
        _eventBus.CrisisEnded -= OnCrisisEnded;
    }

    private void OnCrisisWarning(int crisisNumber, float countdown)
    {
        Enemy.AnimationTempo = AgitatedAnimationSpeed;
        FadeTo(WarningPeak, Mathf.Max(0.5f, countdown));
    }

    private void OnCrisisStarted(int crisisNumber, int intensity)
    {
        Enemy.AnimationTempo = 1f;
        FadeTo(CrisisIntensity, CrisisRampSeconds);
    }

    private void OnCrisisEnded(int crisisNumber) => FadeTo(0f, CalmFadeSeconds);

    private void FadeTo(float target, float seconds)
    {
        _tween?.Kill();
        _tween = CreateTween();
        // Montée rapide puis lente : le présage se remarque dès les premières secondes.
        _tween.TweenMethod(Callable.From<float>(SetIntensity), _intensity, target, seconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    private void SetIntensity(float value)
    {
        _intensity = value;
        _material.SetShaderParameter(IntensityParam, value);
        // Masqué à zéro : la copie de l'écran ne se paie que pendant un présage ou une crise.
        _rect.Visible = value > 0.01f;
    }
}
