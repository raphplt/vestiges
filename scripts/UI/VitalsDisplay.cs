using Godot;

namespace Vestiges.UI;

/// <summary>
/// Plaque de vie : métal patiné, sceau de niveau et jauge à relief pixel.
/// Les métriques de Saira réservent une vraie ligne aux chiffres, hors de la jauge.
/// </summary>
public partial class VitalsDisplay : Control
{
    private const float PlateWidth = 190f;
    private const float BadgeWidth = 30f;
    private const float BarLeft = 43f;
    private const float BarWidth = 140f;
    private const float LowHpRatio = 0.3f;
    private static readonly Color Ink = new("1a1a2e");
    private static readonly Color Blue = new("16213e");
    private static readonly Color Metal = new("6b6161");
    private static readonly Color Paper = new("e8e0d4");
    private static readonly Color Gold = new("d4a843");
    private static readonly Color Cyan = new("5ec4c4");
    private static readonly Color Green = new("7bc558");
    private static readonly Color Orange = new("e07b39");
    private static readonly Color Red = new("c4432b");
    private static readonly Color Shield = new(0.72f, 0.86f, 1f);

    private Label _caption;
    private Label _level;
    private Label _health;
    private VitalsPerkOverlay _perks;
    private Rect2 _bar;
    private float _hpRatio = 1f;
    private float _chipRatio = 1f;
    private float _shieldRatio;
    private float _pulse;
    private float _headerLineEnd;
    private Tween _levelTween;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _caption = CreateLabel("LevelCaption", Tr("UI_HUD_LEVEL"), 6, Paper.Darkened(0.25f), UITheme.BodyFont);
        _level = CreateLabel("Level", "1", 13, Cyan, UITheme.BoldFont);
        _health = CreateLabel("HealthValue", "100 / 100", 11, Paper, UITheme.StrongFont);
        _health.HorizontalAlignment = HorizontalAlignment.Right;
        _perks = new VitalsPerkOverlay { Name = "SurvivalPerks" };
        AddChild(_perks);
        LayoutLabels();
        SetProcess(false);
    }

    private Label CreateLabel(string name, string text, int fontSize, Color color, Font font)
    {
        Label label = new()
        {
            Name = name, Text = text, MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        AddChild(label);
        return label;
    }

    private void LayoutLabels()
    {
        float captionHeight = Mathf.Ceil(_caption.GetMinimumSize().Y);
        float levelHeight = Mathf.Ceil(_level.GetMinimumSize().Y);
        float healthHeight = Mathf.Ceil(_health.GetMinimumSize().Y);
        float height = Mathf.Max(38f, Mathf.Max(captionHeight + levelHeight + 6f, healthHeight + 20f));
        CustomMinimumSize = Size = new Vector2(PlateWidth, height);
        float badgeTextTop = Mathf.Floor((height - captionHeight - levelHeight) / 2f);
        _caption.Position = new Vector2(4f, badgeTextTop);
        _caption.Size = new Vector2(BadgeWidth, captionHeight);
        _level.Position = new Vector2(4f, badgeTextTop + captionHeight);
        _level.Size = new Vector2(BadgeWidth, levelHeight);
        _health.Position = new Vector2(BarLeft + 12f, 3f);
        _health.Size = new Vector2(BarWidth - 12f, healthHeight);
        _headerLineEnd = _health.GetRect().End.X - _health.GetMinimumSize().X - 8f;
        _bar = new Rect2(BarLeft, height - 15f, BarWidth, 9f);
        // L'overlay garde son contrat : un pixel de bord autour de la piste.
        _perks.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        _perks.Position = _bar.Position - Vector2.One;
        _perks.Size = _bar.Size + Vector2.One * 2f;
        QueueRedraw();
    }

    public void SetHealth(float currentHp, float maxHp)
    {
        float maximum = Mathf.Max(1f, maxHp);
        float current = Mathf.Clamp(currentHp, 0f, maximum);
        float previous = _hpRatio;
        _hpRatio = current / maximum;
        if (_hpRatio > previous)
            _chipRatio = _hpRatio;
        _health.Text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(maximum)}";
        _headerLineEnd = _health.GetRect().End.X - _health.GetMinimumSize().X - 8f;
        _health.AddThemeColorOverride("font_color", _hpRatio < LowHpRatio ? Paper.Lerp(Red, 0.35f) : Paper);
        SetProcess(_chipRatio > _hpRatio || IsLowHealth());
        QueueRedraw();
    }

    public void SetShield(float shield, float maxShield)
    {
        _shieldRatio = maxShield > 0f ? Mathf.Clamp(shield / maxShield, 0f, 1f) : 0f;
        QueueRedraw();
    }

    public void SetLevel(int level)
    {
        _level.Text = $"{level}";
        // Un éclat de couleur reste dans le sceau, même sur plusieurs niveaux consécutifs.
        _levelTween?.Kill();
        _level.Modulate = new Color(1.8f, 1.8f, 1.8f);
        _levelTween = CreateTween();
        _levelTween.TweenProperty(_level, "modulate", Colors.White, 0.35f);
    }

    private bool IsLowHealth() => _hpRatio > 0f && _hpRatio < LowHpRatio;

    public override void _Process(double delta)
    {
        _chipRatio = Mathf.Max(_hpRatio, _chipRatio - (float)delta * 0.6f);
        if (IsLowHealth())
            _pulse += (float)delta * 5f;
        else
            _pulse = 0f;
        QueueRedraw();
        SetProcess(_chipRatio > _hpRatio || IsLowHealth());
    }

    public override void _Draw()
    {
        float height = Size.Y;
        Color border = Metal.Lerp(Gold, 0.25f);
        if (IsLowHealth())
            border = border.Lerp(Red, 0.45f + 0.3f * Mathf.Sin(_pulse));

        // Coins coupés, ombre portée courte et double filet de métal usé.
        DrawRect(new Rect2(2f, 3f, PlateWidth - 2f, height - 1f), Ink.Darkened(0.65f) with { A = 0.55f });
        DrawRect(new Rect2(2f, 0f, PlateWidth - 4f, height), border);
        DrawRect(new Rect2(0f, 2f, PlateWidth, height - 4f), border);
        DrawRect(new Rect2(2f, 2f, PlateWidth - 4f, height - 4f), Ink.Darkened(0.4f) with { A = 0.97f });
        DrawRect(new Rect2(3f, 2f, PlateWidth - 6f, 1f), Metal with { A = 0.5f });
        DrawRect(new Rect2(3f, height - 3f, PlateWidth - 6f, 1f), Ink);
        DrawRect(new Rect2(PlateWidth - 11f, 0f, 7f, 1f), Gold.Darkened(0.2f));
        DrawRect(new Rect2(0f, height - 11f, 1f, 7f), Gold.Darkened(0.2f));
        DrawRect(new Rect2(4f, 4f, BadgeWidth, height - 8f), Blue);
        DrawRect(new Rect2(5f, 4f, BadgeWidth - 2f, 1f), Cyan.Darkened(0.6f));
        DrawRect(new Rect2(5f, height - 5f, BadgeWidth - 2f, 1f), Cyan.Darkened(0.35f));
        DrawRect(new Rect2(38f, 6f, 1f, height - 12f), Metal with { A = 0.5f });
        DrawRect(new Rect2(37f, 4f, 3f, 2f), Gold.Darkened(0.25f));
        DrawRect(new Rect2(37f, height - 6f, 3f, 2f), Gold.Darkened(0.45f));

        // Petit signe de soin crème, aligné sur le centre réel de la ligne de PV.
        float crossY = Mathf.Floor(_health.Position.Y + _health.Size.Y / 2f) - 3f;
        DrawRect(new Rect2(BarLeft + 2f, crossY, 3f, 7f), Paper.Darkened(0.2f));
        DrawRect(new Rect2(BarLeft, crossY + 2f, 7f, 3f), Paper.Darkened(0.2f));
        float lineStart = BarLeft + 14f;
        if (_headerLineEnd > lineStart)
        {
            DrawRect(new Rect2(lineStart, crossY + 3f, _headerLineEnd - lineStart, 1f), Metal.Darkened(0.35f));
            DrawRect(new Rect2(lineStart, crossY + 3f, 9f, 1f), Gold.Darkened(0.4f));
        }

        DrawRect(_bar.Grow(1f), Metal.Darkened(0.45f));
        DrawRect(_bar, Ink.Darkened(0.6f));
        float chipWidth = Mathf.Floor(_bar.Size.X * _chipRatio);
        if (chipWidth > 0f)
            DrawRect(new Rect2(_bar.Position, new Vector2(chipWidth, _bar.Size.Y)), Paper.Darkened(0.2f));
        float fillWidth = Mathf.Floor(_bar.Size.X * _hpRatio);
        if (fillWidth > 0f)
        {
            Color fill = _hpRatio < LowHpRatio ? Red : _hpRatio < 0.55f ? Orange : Green;
            DrawRect(new Rect2(_bar.Position, new Vector2(fillWidth, _bar.Size.Y)), fill.Darkened(0.16f));
            DrawRect(new Rect2(_bar.Position, new Vector2(fillWidth, 1f)), fill.Lerp(Paper, 0.4f));
            DrawRect(new Rect2(_bar.Position + new Vector2(0f, _bar.Size.Y - 3f), new Vector2(fillWidth, 3f)), fill.Darkened(0.4f));
            DrawRect(new Rect2(_bar.Position + new Vector2(fillWidth - 1f, 1f), new Vector2(1f, _bar.Size.Y - 1f)), fill.Lerp(Paper, 0.25f));
        }
        // Repères gravés sous le rail : ils ne coupent pas la longueur des PV.
        for (int tick = 0; tick <= 4; tick++)
            DrawRect(new Rect2(_bar.Position.X + Mathf.Floor((_bar.Size.X - 1f) * tick / 4f), _bar.End.Y + 2f, 1f, 1f), Metal);
        if (_shieldRatio > 0f)
            DrawRect(new Rect2(_bar.Position, new Vector2(Mathf.Floor(_bar.Size.X * _shieldRatio), 2f)), Shield);
    }
}
