using System;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Effet de forme recyclé (arc, poussée, anneau, zone, cône, rayon) dessiné par pixel_fx.gdshader
/// sur un quad aligné sur la grille des texels. Avance par poses, puis retourne au pool.
/// </summary>
public partial class PixelFx : Sprite2D
{
    private static readonly StringName ShapeParam = "shape";
    private static readonly StringName SizeParam = "size_px";
    private static readonly StringName AngleParam = "angle";
    private static readonly StringName RadiusParam = "radius";
    private static readonly StringName ThicknessParam = "thickness";
    private static readonly StringName ArcHalfParam = "arc_half";
    private static readonly StringName SquashParam = "squash";
    private static readonly StringName ProgressParam = "progress";
    private static readonly StringName FadeParam = "fade";
    private static readonly StringName FillParam = "fill_density";
    private static readonly StringName SeedParam = "seed";
    private static readonly StringName ProgressFillParam = "progress_fill";
    private static readonly StringName LightParam = "c_light";
    private static readonly StringName MidParam = "c_mid";
    private static readonly StringName DarkParam = "c_dark";
    private static readonly StringName OutlineParam = "c_outline";

    private static Shader _shader;
    private static Texture2D _pixel;

    private ShaderMaterial _material;
    private Action<PixelFx> _release;
    private Node2D _follow;
    private Vector2 _followOffset;
    private float _elapsed;
    private float _duration;
    private int _steps;
    private int _step;
    private float _fadeTail;
    private bool _loop;

    public static PixelFx Create(Action<PixelFx> release)
    {
        _shader ??= GD.Load<Shader>("res://assets/shaders/pixel_fx.gdshader");
        if (_pixel == null)
        {
            Image image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            image.Fill(Colors.White);
            _pixel = ImageTexture.CreateFromImage(image);
        }
        PixelFx fx = new()
        {
            Texture = _pixel,
            TextureFilter = TextureFilterEnum.Nearest,
            Visible = false,
            _release = release,
        };
        fx._material = new ShaderMaterial { Shader = _shader };
        fx.Material = fx._material;
        fx.SetProcess(false);
        return fx;
    }

    /// <summary>
    /// Lance l'effet. <paramref name="follow"/> (facultatif) garde l'effet attaché à une entité en mouvement,
    /// par exemple l'arc d'une lame autour du joueur.
    /// </summary>
    public void Play(Vector2 position, in PixelFxSpec spec, float opacity, Node2D follow = null)
    {
        FxRamp ramp = PixelPalette.Ramp(spec.Family);
        float reach = spec.Radius + spec.Thickness + 2f;
        Vector2 size = new(reach * 2f, reach * 2f / Mathf.Max(spec.Squash, 1f));
        // Taille paire : les bords du quad tombent sur la grille des texels.
        size = new Vector2(Mathf.Ceil(size.X / 2f) * 2f + 2f, Mathf.Ceil(size.Y / 2f) * 2f + 2f);

        Scale = size;
        ZIndex = spec.ZIndex;
        Modulate = new Color(1f, 1f, 1f, opacity);
        _follow = follow;
        _followOffset = follow != null ? position - follow.GlobalPosition : Vector2.Zero;
        GlobalPosition = position.Round();

        _material.SetShaderParameter(ShapeParam, (int)spec.Shape);
        _material.SetShaderParameter(SizeParam, size);
        _material.SetShaderParameter(AngleParam, spec.Angle);
        _material.SetShaderParameter(RadiusParam, spec.Radius);
        _material.SetShaderParameter(ThicknessParam, spec.Thickness);
        _material.SetShaderParameter(ArcHalfParam, spec.ArcHalf);
        _material.SetShaderParameter(SquashParam, spec.Squash);
        _material.SetShaderParameter(FillParam, spec.FillDensity);
        _material.SetShaderParameter(ProgressFillParam, spec.ProgressFill);
        _material.SetShaderParameter(SeedParam, (float)GD.Randf() * 100f);
        _material.SetShaderParameter(LightParam, ramp.Light);
        _material.SetShaderParameter(MidParam, ramp.Mid);
        _material.SetShaderParameter(DarkParam, ramp.Dark);
        _material.SetShaderParameter(OutlineParam, ramp.Outline);

        _elapsed = 0f;
        _duration = Mathf.Max(spec.Duration, 0.01f);
        _steps = Mathf.Max(spec.Steps, 2);
        _fadeTail = Mathf.Clamp(spec.FadeTail, 0f, 1f);
        _loop = spec.Loop;
        _step = -1;
        ApplyStep(0);
        Visible = true;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= _duration)
        {
            if (!_loop)
            {
                Stop();
                return;
            }
            _elapsed %= _duration;
        }
        if (_follow != null)
        {
            if (IsInstanceValid(_follow))
                GlobalPosition = (_follow.GlobalPosition + _followOffset).Round();
            else
                _follow = null;
        }
        ApplyStep(Mathf.Min(_steps - 1, (int)(_elapsed / _duration * _steps)));
    }

    /// <summary>
    /// Effet piloté de l'extérieur (annonce au sol) : affiché sans avancer seul,
    /// la progression et l'effacement viennent de SetProgress et SetFade.
    /// </summary>
    public void Hold(Vector2 position, in PixelFxSpec spec, float opacity)
    {
        Play(position, spec, opacity);
        SetProcess(false);
        SetProgress(0f);
        SetFade(0f);
    }

    public void SetProgress(float progress) => _material.SetShaderParameter(ProgressParam, progress);

    public void SetFade(float fade) => _material.SetShaderParameter(FadeParam, fade);

    public void SetFillDensity(float density) => _material.SetShaderParameter(FillParam, density);

    /// <summary>Réoriente un effet entretenu (cône qui suit la visée et s'ouvre).</summary>
    public void Aim(float angle, float arcHalf)
    {
        _material.SetShaderParameter(AngleParam, angle);
        _material.SetShaderParameter(ArcHalfParam, arcHalf);
    }

    public void Stop()
    {
        if (!Visible)
            return;
        Visible = false;
        _follow = null;
        SetProcess(false);
        _release(this);
    }

    private void ApplyStep(int step)
    {
        if (step == _step)
            return;
        _step = step;
        float progress = (float)step / (_steps - 1);
        float fade = _fadeTail > 0f ? Mathf.Clamp((progress - (1f - _fadeTail)) / _fadeTail, 0f, 1f) : 0f;
        _material.SetShaderParameter(ProgressParam, progress);
        _material.SetShaderParameter(FadeParam, fade);
    }
}
