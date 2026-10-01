using System;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Réveil d'un lieu dans le monde, avant son écran de choix (plan 24 B2) : la run se fige, l'image se pixelise puis
/// se désature autour du lieu, la caméra glisse vers lui ; enfin l'oubli recule jusqu'aux bords. Le lieu anime sa
/// part (éclats, colonne) par le rappel de progression. Sous le HUD, au-dessus du monde et du voile d'Effacement.
/// Tout se compte en secondes réelles. Un appui passe la séquence ; l'écran s'ouvre alors sans son entrée.
/// La caméra n'est rendue au joueur qu'une fois la run relancée, à la fermeture de l'écran.
/// Hors séquence, la couche est invisible : aucun coût.
/// </summary>
public partial class LandmarkReveal : CanvasLayer
{
    private const float FreezeEnd = 0.08f;
    private const float PixelPeak = 0.22f;
    private const float PixelEnd = 0.42f;
    private const int PixelMaxBlock = 3;
    private const float FadeStart = 0.06f;
    private const float FadeEnd = 0.3f;
    private const float PanEnd = 0.4f;
    private const float RecedeStart = 0.55f;
    private const float ClearRadius = 72f;
    private const float FrontWidth = 48f;
    private const float MuffleShare = 0.15f;
    private const float ReturnSec = 0.35f;

    private static readonly StringName ViewOriginParam = "view_origin";
    private static readonly StringName ViewSizeParam = "view_size";
    private static readonly StringName OriginParam = "origin";
    private static readonly StringName BlockParam = "block";
    private static readonly StringName ClearRadiusParam = "clear_radius";
    private static readonly StringName FrontWidthParam = "front_width";
    private static readonly StringName FadeParam = "fade_amount";
    private static readonly StringName FrontParam = "front_amount";
    private static readonly StringName FrontColorParam = "front_color";

    private Camera2D _camera;
    private ShaderMaterial _material;
    private Vector2 _focus;
    private float _duration;
    private Action<float> _onProgress;
    private Action<bool> _onFinished;
    private Vector2 _panFrom;
    private Vector2 _panTo;
    private ulong _startUsec;
    private bool _playing;
    private bool _returning;
    private ulong _returnStartUsec;
    private Vector2 _returnFrom;

    public bool IsPlaying => _playing;

    /// <summary>
    /// La caméra du joueur. La secousse n'est pas suspendue pendant le retour : elle porte aussi le hitstop, qui doit
    /// rester bref ; une secousse pendant ces 0,35 s l'emporte simplement sur le retour.
    /// </summary>
    public void Setup(Camera2D camera) => _camera = camera;

    public override void _Ready()
    {
        Layer = 6;
        ProcessMode = ProcessModeEnum.Always;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/landmark_reveal.gdshader") };
        _material.SetShaderParameter(FrontWidthParam, FrontWidth);
        ColorRect overlay = new() { Material = _material, MouseFilter = Control.MouseFilterEnum.Ignore };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);
        Visible = false;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_playing)
            AudioManager.SetWorldMuffle(0f);
    }

    /// <summary>
    /// Joue le réveil du lieu en <paramref name="focus"/>. <paramref name="onProgress"/> reçoit l'avancement de 0 à 1 à
    /// chaque image, et 1 à la fin même si la séquence est passée ; <paramref name="onFinished"/> reçoit vrai si le
    /// joueur l'a passée. La run reste figée à la fin : l'écran de choix qui suit la garde en pause.
    /// </summary>
    public void Play(Vector2 focus, Color frontColor, float duration, Action<float> onProgress, Action<bool> onFinished)
    {
        if (_playing)
            Finish(true);
        _focus = focus;
        _duration = Mathf.Max(duration, 0.1f);
        _onProgress = onProgress;
        _onFinished = onFinished;
        _material.SetShaderParameter(FrontColorParam, frontColor);
        _returning = false;
        if (_camera != null)
        {
            _camera.ProcessMode = ProcessModeEnum.Always;
            _panFrom = _camera.Offset;
            _panTo = _camera.Offset + (focus - _camera.GetScreenCenterPosition());
        }
        _startUsec = Time.GetTicksUsec();
        _playing = true;
        Visible = true;
        SetProcess(true);
        GetTree().Paused = true;
        Advance(0f);
    }

    public override void _Process(double delta)
    {
        if (_playing)
        {
            float u = (Time.GetTicksUsec() - _startUsec) / 1_000_000f / _duration;
            if (u >= 1f)
                Finish(false);
            else
                Advance(u);
            return;
        }
        if (_returning)
            UpdateReturn();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playing || !@event.IsPressed() || @event.IsEcho())
            return;
        // Les directions ne passent pas la séquence : le joueur se déplaçait en ramassant le dernier éclat.
        bool skip = @event is InputEventMouseButton or InputEventJoypadButton
            || @event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_cancel");
        if (!skip)
            return;
        GetViewport().SetInputAsHandled();
        Finish(true);
    }

    private void Advance(float u)
    {
        float pixel = u < PixelPeak
            ? Mathf.Clamp((u - FreezeEnd) / (PixelPeak - FreezeEnd), 0f, 1f)
            : 1f - Mathf.Clamp((u - PixelPeak) / (PixelEnd - PixelPeak), 0f, 1f);
        // Par paliers entiers de pixels du monde : la pixelisation reste du pixel art.
        _material.SetShaderParameter(BlockParam, (float)(1 + Mathf.RoundToInt(pixel * (PixelMaxBlock - 1))));
        _material.SetShaderParameter(FadeParam, Mathf.SmoothStep(FadeStart, FadeEnd, u));
        _material.SetShaderParameter(FrontParam, u >= RecedeStart ? 1f : 0f);

        Viewport viewport = GetViewport();
        if (_camera != null)
            _camera.Offset = _panFrom.Lerp(_panTo, Mathf.SmoothStep(0f, PanEnd, u));
        Transform2D toWorld = viewport.CanvasTransform.AffineInverse();
        Vector2 viewOrigin = toWorld * Vector2.Zero;
        Vector2 viewSize = toWorld * viewport.GetVisibleRect().Size - viewOrigin;
        _material.SetShaderParameter(ViewOriginParam, viewOrigin);
        _material.SetShaderParameter(ViewSizeParam, viewSize);
        _material.SetShaderParameter(OriginParam, _focus);

        // L'oubli recule : la zone gardée s'ouvre en accélérant jusqu'à dépasser les coins de l'écran.
        float recede = Mathf.Clamp((u - RecedeStart) / (1f - RecedeStart), 0f, 1f);
        float reach = viewSize.Length() * 0.5f + _focus.DistanceTo(viewOrigin + viewSize * 0.5f) + FrontWidth;
        _material.SetShaderParameter(ClearRadiusParam, Mathf.Lerp(ClearRadius, reach, recede * recede));

        AudioManager.SetWorldMuffle(Mathf.Clamp(u / MuffleShare, 0f, 1f) * (1f - recede));
        _onProgress?.Invoke(u);
    }

    private void Finish(bool skipped)
    {
        _playing = false;
        _onProgress?.Invoke(1f);
        if (_camera != null)
            _camera.Offset = _panTo;
        Visible = false;
        AudioManager.SetWorldMuffle(0f);
        _returning = _camera != null;
        _returnStartUsec = 0;
        SetProcess(_returning);
        Action<bool> callback = _onFinished;
        _onProgress = null;
        _onFinished = null;
        callback?.Invoke(skipped);
    }

    /// <summary>Une fois la run relancée, la caméra revient en douceur sur le joueur.</summary>
    private void UpdateReturn()
    {
        if (GetTree().Paused)
            return;
        if (_returnStartUsec == 0)
        {
            _returnStartUsec = Time.GetTicksUsec();
            _returnFrom = _camera.Offset;
        }
        float t = Mathf.Clamp((Time.GetTicksUsec() - _returnStartUsec) / 1_000_000f / ReturnSec, 0f, 1f);
        _camera.Offset = _returnFrom.Lerp(Vector2.Zero, Mathf.SmoothStep(0f, 1f, t));
        if (t < 1f)
            return;
        _returning = false;
        _camera.ProcessMode = ProcessModeEnum.Inherit;
        SetProcess(false);
    }
}
