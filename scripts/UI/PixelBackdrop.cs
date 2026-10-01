using Godot;

namespace Vestiges.UI;

/// <summary>
/// Fond animé commun des écrans de choix (plan 24 B1) : level-up, coffre, Mémorial, Faille. Un rectangle plein écran
/// et son shader (assets/shaders/choice_backdrop.gdshader), dessinés en gros pixels sur la grille de l'écran. Une
/// teinte par écran ; <see cref="FadeIn"/> le fait monter à l'ouverture. Ne capte pas la souris.
/// </summary>
public partial class PixelBackdrop : ColorRect
{
    public static readonly Color GoldTint = new(0.95f, 0.78f, 0.35f);
    public static readonly Color MemorialTint = new(0.55f, 0.88f, 0.86f);
    public static readonly Color RiftTint = new(0.72f, 0.45f, 0.92f);

    private static Shader _shader;
    private ShaderMaterial _material;
    private Tween _fade;

    public PixelBackdrop() : this(GoldTint)
    {
    }

    public PixelBackdrop(Color tint)
    {
        _shader ??= GD.Load<Shader>("res://assets/shaders/choice_backdrop.gdshader");
        _material = new ShaderMaterial { Shader = _shader };
        _material.SetShaderParameter("tint", tint);
        Material = _material;
        Color = Colors.White;
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void SetTint(Color tint) => _material.SetShaderParameter("tint", tint);

    /// <summary>Le fond monte de rien à plein en <paramref name="seconds"/>.</summary>
    public void FadeIn(float seconds)
    {
        _fade?.Kill();
        _material.SetShaderParameter("intensity", 0f);
        _fade = CreateTween();
        _fade.TweenMethod(Callable.From<float>(value => _material.SetShaderParameter("intensity", value)), 0f, 1f, seconds);
    }
}
