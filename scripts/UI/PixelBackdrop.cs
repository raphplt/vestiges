using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Fond commun aux menus, poussières et bords rongés sur une grille de 480 × 270. Par-dessus le jeu (menus ouverts en
/// run, plan 37 R1), il montre le jeu flou et assombri derrière le panneau ; sinon, le fond illustré intérieur.
/// Une teinte par écran ; <see cref="FadeIn"/> le fait monter à l'ouverture.
/// </summary>
public partial class PixelBackdrop : TextureRect
{
    public static readonly Color GoldTint = new(0.95f, 0.78f, 0.35f);
    public static readonly Color MemorialTint = new(0.55f, 0.88f, 0.86f);
    public static readonly Color RiftTint = new(0.72f, 0.45f, 0.92f);
    public static readonly Color WorkshopTint = new(0.92f, 0.56f, 0.3f);
    public static readonly Color NeutralTint = new(0.55f, 0.52f, 0.46f);

    private static Shader _stoneShader;
    private static Shader _gameShader;
    private readonly float _opacity;
    private Tween _fade;

    public PixelBackdrop() : this(GoldTint)
    {
    }

    /// <param name="overGame">Vrai pour un menu ouvert en run : le jeu se devine derrière (DECISIONS §117).</param>
    public PixelBackdrop(Color tint, bool overGame = true)
    {
        _stoneShader ??= GD.Load<Shader>("res://assets/shaders/menu_erasure.gdshader");
        _gameShader ??= GD.Load<Shader>("res://assets/shaders/menu_game_veil.gdshader");
        Material = new ShaderMaterial { Shader = overGame ? _gameShader : _stoneShader };
        Texture = MenuSkin.InteriorBackground;
        _opacity = overGame ? 1f : 0.75f;
        SetTint(tint);
        TextureFilter = TextureFilterEnum.Nearest;
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.KeepAspectCovered;
        Modulate = new Color(1f, 1f, 1f, _opacity);
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void SetTint(Color tint)
    {
        ShaderMaterial material = (ShaderMaterial)Material;
        material.SetShaderParameter("dust_color", tint);
    }

    /// <summary>Passer une entrée termine aussi son fondu, sans changer la rotation.</summary>
    public void FinishFade()
    {
        _fade?.Kill();
        Modulate = new Color(1f, 1f, 1f, _opacity);
    }

    /// <summary>Le fond monte de rien à plein en <paramref name="seconds"/>.</summary>
    public void FadeIn(float seconds)
    {
        _fade?.Kill();
        Modulate = new Color(1f, 1f, 1f, 0f);
        _fade = CreateTween();
        _fade.TweenProperty(this, "modulate:a", _opacity, seconds);
    }
}
