using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Halo rouge sang arrondi, dissous en pixels, sous la blessure et le HUD ; calé sur le cœur.</summary>
public partial class LowHealthVignette : ColorRect
{
    private static readonly StringName IntensityParam = "intensity";
    private static readonly StringName ElapsedParam = "elapsed";
    private ShaderMaterial _material;
    private AudioManager _audio;
    private float _elapsed;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Visible = false;
        _audio = AudioManager.Instance;
        PlayerFeedbackConfig config = PlayerFeedbackConfig.Get();
        if (config == null || _audio == null)
        {
            SetProcess(false);
            return;
        }
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/low_health_vignette.gdshader") };
        _material.SetShaderParameter("tint", new Color("c4432b") with { A = config.LowHealthOpacity });
        _material.SetShaderParameter("thickness", config.LowHealthThickness);
        Material = _material;
    }

    public override void _Process(double delta)
    {
        float intensity = _audio.LowHealthIntensity;
        Visible = intensity > 0f;
        if (Visible)
        {
            _elapsed += (float)delta;
            _material.SetShaderParameter(ElapsedParam, _elapsed);
            _material.SetShaderParameter(IntensityParam, intensity * (0.78f + 0.22f * _audio.HeartbeatPulse));
        }
    }
}
