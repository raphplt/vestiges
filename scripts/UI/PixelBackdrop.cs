using Godot;

namespace Vestiges.UI;

/// <summary>
/// Fond commun en 480 × 270 : rayons stables et scintillement continu des poussières natives.
/// Une teinte par écran ; <see cref="FadeIn"/> le fait monter à l'ouverture.
/// </summary>
public partial class PixelBackdrop : TextureRect
{
    public static readonly Color GoldTint = new(0.95f, 0.78f, 0.35f);
    public static readonly Color MemorialTint = new(0.55f, 0.88f, 0.86f);
    public static readonly Color RiftTint = new(0.72f, 0.45f, 0.92f);
    public static readonly Color NeutralTint = new(0.55f, 0.52f, 0.46f);

    private static Shader _twinkleShader;
    private Tween _fade;

    public PixelBackdrop() : this(GoldTint)
    {
    }

    public PixelBackdrop(Color tint)
    {
        SetTint(tint);
        _twinkleShader ??= GD.Load<Shader>("res://assets/shaders/ui_dust_twinkle.gdshader");
        Material = new ShaderMaterial { Shader = _twinkleShader };
        TextureFilter = TextureFilterEnum.Nearest;
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.Scale;
        Modulate = new Color(1f, 1f, 1f, 0.75f);
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void SetTint(Color tint)
    {
        Texture = ScreenArt.Background(tint == GoldTint ? 0 : tint == MemorialTint ? 1 : tint == RiftTint ? 2 : 3);
    }

    /// <summary>Le fond monte de rien à plein en <paramref name="seconds"/>.</summary>
    public void FadeIn(float seconds)
    {
        _fade?.Kill();
        Modulate = new Color(1f, 1f, 1f, 0f);
        _fade = CreateTween();
        _fade.TweenProperty(this, "modulate:a", 0.75f, seconds);
    }
}
