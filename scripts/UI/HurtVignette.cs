using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Vignette de blessure (plan 27 V3a) : un coup de combat qui retire des PV teinte les bords de l'écran d'un damier
/// rouge, plus épais du côté d'où vient le coup, qui s'efface en quelques dixièmes de seconde. Une tranche du Néant le
/// fait pulser en pâle, sur tous les bords (V3d). Calque plein écran sous le HUD, à l'écoute de l'EventBus ; inactif
/// hors effacement.
/// </summary>
public partial class HurtVignette : ColorRect
{
    private static readonly StringName IntensityParam = "intensity";
    private static readonly StringName SideParam = "side";
    private static readonly StringName TintParam = "tint";

    private EventBus _eventBus;
    private ShaderMaterial _material;
    private PlayerFeedbackConfig _config;
    private float _remaining;
    private float _shownIntensity;
    private bool _voidShown;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Color = Colors.White;
        _config = PlayerFeedbackConfig.Get();
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/hurt_vignette.gdshader") };
        if (_config != null)
        {
            _material.SetShaderParameter("tint", _config.VignetteColor with { A = _config.VignetteMaxOpacity });
            _material.SetShaderParameter("thickness", _config.VignetteThickness);
            _material.SetShaderParameter("side_bias", _config.VignetteSideBias);
        }
        Material = _material;
        Visible = false;
        SetProcess(false);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PlayerDamageResolved += OnPlayerDamage;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.PlayerDamageResolved -= OnPlayerDamage;
    }

    private void OnPlayerDamage(PlayerDamageResult result)
    {
        if (_config == null || result.HpLost <= 0f)
            return;
        bool voidTick = result.Kind == PlayerDamageKind.Erasure;
        // Une blessure en cours garde sa vignette rouge : la tranche du Néant ne l'écrase pas.
        if (voidTick && _remaining > 0f && !_voidShown)
            return;
        _voidShown = voidTick;
        _remaining = _config.VignetteSeconds;
        _material.SetShaderParameter(TintParam, voidTick ? _config.VoidVignetteColor : _config.VignetteColor with { A = _config.VignetteMaxOpacity });
        _material.SetShaderParameter(SideParam, voidTick ? Vector2.Zero : result.FromDirection);
        _material.SetShaderParameter(IntensityParam, 1f);
        _shownIntensity = 1f;
        Visible = true;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0f)
        {
            Visible = false;
            SetProcess(false);
            return;
        }
        // Effacement en trois paliers, sans interpolation continue (charte : poses clés).
        float left = _remaining / _config.VignetteSeconds;
        float intensity = left > 0.66f ? 1f : left > 0.33f ? 0.66f : 0.33f;
        if (intensity == _shownIntensity)
            return;
        _shownIntensity = intensity;
        _material.SetShaderParameter(IntensityParam, intensity);
    }
}
