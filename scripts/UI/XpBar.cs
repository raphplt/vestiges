using Godot;

namespace Vestiges.UI;

/// <summary>Rail d'XP pleine largeur : métal patiné, or animé et éclats calculés au pixel natif (plan 25 S6).</summary>
public partial class XpBar : Control
{
    public const float BarHeight = 9f;
    private const float CapWidth = 4f;
    private const float FillSpeed = 3.5f;
    private const float ShimmerPeriod = 2.6f;
    private const string Folder = "res://assets/ui/hud/plan25/";
    private readonly Texture2D[] _fills = new Texture2D[4];
    private readonly Texture2D[] _tips = new Texture2D[4];
    private readonly Texture2D[] _bursts = new Texture2D[4];
    private StyleBoxTexture _rail;
    private float _target;
    private float _shown;
    private float _shimmer;
    private float _time;
    private float _pulse;
    private float _flash;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        TextureRepeat = TextureRepeatEnum.Enabled;
        AnchorLeft = 0f;
        AnchorRight = 1f;
        AnchorTop = 1f;
        AnchorBottom = 1f;
        OffsetLeft = OffsetRight = OffsetBottom = 0f;
        OffsetTop = -BarHeight;
        _rail = UITheme.CreateNinePatch(GD.Load<Texture2D>(Folder + "xp_frame.png"), 4, 2, 4, 1);
        for (int frame = 0; frame < 4; frame++)
        {
            _fills[frame] = GD.Load<Texture2D>($"{Folder}xp_fill_{frame:00}.png");
            _tips[frame] = GD.Load<Texture2D>($"{Folder}xp_tip_{frame:00}.png");
            _bursts[frame] = GD.Load<Texture2D>($"{Folder}xp_burst_{frame:00}.png");
        }
    }

    public void SetRatio(float ratio, bool gained)
    {
        _target = Mathf.Clamp(ratio, 0f, 1f);
        if (gained)
            _pulse = 1f;
    }

    public void Flash()
    {
        _flash = 1f;
        _shown = _target = 0f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _shown = _shown < _target ? Mathf.Min(_target, _shown + Mathf.Max(0.002f, (_target - _shown) * FillSpeed * dt)) : _target;
        _time += dt;
        _shimmer = (_shimmer + dt / ShimmerPeriod) % 1f;
        _pulse = Mathf.Max(0f, _pulse - dt * 4f);
        _flash = Mathf.Max(0f, _flash - dt * 2.5f);
        QueueRedraw();
    }

    public override void _Draw()
    {
        float width = Mathf.Floor(Size.X);
        if (_rail == null || width <= CapWidth * 2f)
            return;
        DrawStyleBox(_rail, new Rect2(0f, 0f, width, BarHeight));
        float inner = width - CapWidth * 2f;
        float fill = Mathf.Floor(inner * _shown);
        int frame = (int)(_time * 8f) % 4;
        if (fill >= 1f)
        {
            Color pulse = new(1f + _pulse * 0.25f, 1f + _pulse * 0.25f, 1f + _pulse * 0.25f);
            DrawTextureRect(_fills[frame], new Rect2(CapWidth, 2f, fill, 6f), true, pulse);
            float shimmerX = Mathf.Floor(_shimmer * (fill + 28f) - 14f);
            for (int row = 0; row < 6; row++)
            {
                float start = Mathf.Max(0f, shimmerX - row);
                float end = Mathf.Min(fill, shimmerX + 7f - row);
                if (end > start)
                    DrawRect(new Rect2(CapWidth + start, 2f + row, end - start, 1f), new Color(1f, 0.94f, 0.72f, 0.25f));
            }
            DrawTexture(_tips[frame], new Vector2(Mathf.Clamp(CapWidth + fill - 4f, CapWidth, width - CapWidth - 8f), 0f));
        }
        if (_flash > 0f)
        {
            int burst = Mathf.Min(3, (int)((1f - _flash) * 4f));
            DrawRect(new Rect2(CapWidth, 2f, inner, 6f), new Color(1f, 0.94f, 0.72f, _flash * 0.5f));
            for (int index = 1; index < 8; index++)
                DrawTexture(_bursts[burst], new Vector2(Mathf.Floor(width * index / 8f) - 8f, -7f));
        }
    }
}
