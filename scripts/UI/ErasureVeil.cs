using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Signal visuel de ce que l'oubli coûte (plan 16 O4) : trame blanche aux bords de l'écran selon la phase de la zone
/// du joueur. Sous le HUD ; transition douce à chaque changement de phase.
/// </summary>
public partial class ErasureVeil : CanvasLayer
{
    private const float TransitionSeconds = 0.8f;
    private static readonly StringName IntensityParam = "intensity";

    private ShaderMaterial _material;
    private EventBus _eventBus;
    private Tween _tween;
    private float _intensity;

    public override void _Ready()
    {
        Layer = 5;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/erasure_veil.gdshader") };
        ColorRect veil = new() { Material = _material, MouseFilter = Control.MouseFilterEnum.Ignore };
        veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(veil);
        SetIntensity(0f);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PlayerErasurePhaseChanged += OnPhaseChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.PlayerErasurePhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(int phase)
    {
        float target = ErasureEffects.For((ErasureManager.ErasureZonePhase)phase).Veil;
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenMethod(Callable.From<float>(SetIntensity), _intensity, target, TransitionSeconds);
    }

    private void SetIntensity(float value)
    {
        _intensity = value;
        _material.SetShaderParameter(IntensityParam, value);
    }
}
