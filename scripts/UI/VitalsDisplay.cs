using Godot;

namespace Vestiges.UI;

/// <summary>
/// Vie et niveau posés sur le jeu, sans plaque (plan 37 R2, DECISIONS §117) : niveau en grand chiffre détouré, jauge
/// fine cerclée de métal, PV au-dessus. Les textes Pixel Operator restent sur leur grille native, hors de la jauge.
/// </summary>
public partial class VitalsDisplay : Control
{
    private const float PlateWidth = 190f;
    private const float BadgeWidth = 34f;
    private const float BarLeft = 40f;
    private const float BarWidth = 146f;
    private const float BarHeight = 7f;
    private const float LowHpRatio = 0.3f;
    private const int Outline = 4;
    private static readonly Color Ink = new("1a1a2e");
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
    private Tween _levelTween;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _caption = CreateLabel("LevelCaption", Tr("UI_HUD_LEVEL"), 7, Paper.Darkened(0.15f), TextWeight.Strong);
        _level = CreateLabel("Level", "1", 22, Cyan, TextWeight.Bold);
        _health = CreateLabel("HealthValue", "100 / 100", 11, Paper, TextWeight.Strong);
        _health.HorizontalAlignment = HorizontalAlignment.Right;
        _perks = new VitalsPerkOverlay { Name = "SurvivalPerks" };
        AddChild(_perks);
        LayoutLabels();
        SetProcess(false);
    }

    private Label CreateLabel(string name, string text, int fontSize, Color color, TextWeight weight)
    {
        Label label = new()
        {
            Name = name, Text = text, MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        };
        UITheme.SetCompactText(label, fontSize, weight);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", Outline);
        AddChild(label);
        return label;
    }

    private void LayoutLabels()
    {
        float captionHeight = Mathf.Ceil(_caption.GetMinimumSize().Y);
        float levelHeight = Mathf.Ceil(_level.GetMinimumSize().Y);
        float healthHeight = Mathf.Ceil(_health.GetMinimumSize().Y);
        // La légende conserve sa propre ligne au-dessus du grand chiffre.
        float levelTop = captionHeight - 2f;
        float height = Mathf.Max(levelTop + levelHeight, healthHeight + BarHeight + 4f);
        CustomMinimumSize = Size = new Vector2(PlateWidth, height);
        _caption.Position = new Vector2(0f, 0f);
        _caption.Size = new Vector2(BadgeWidth, captionHeight);
        _level.Position = new Vector2(0f, levelTop);
        _level.Size = new Vector2(BadgeWidth, levelHeight);
        _bar = new Rect2(BarLeft, height - BarHeight - 3f, BarWidth, BarHeight);
        _health.Position = new Vector2(BarLeft, _bar.Position.Y - healthHeight + 1f);
        _health.Size = new Vector2(BarWidth, healthHeight);
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
        Color rim = Metal.Lerp(Gold, 0.3f);
        if (IsLowHealth())
            rim = rim.Lerp(Red, 0.45f + 0.3f * Mathf.Sin(_pulse));

        // Jauge seule sur le jeu : contour d'encre, liseré de métal, piste sombre.
        DrawRect(_bar.Grow(2f), Ink with { A = 0.85f });
        DrawRect(_bar.Grow(1f), rim);
        DrawRect(new Rect2(_bar.Position - Vector2.One, new Vector2(_bar.Size.X + 2f, 1f)), rim.Lightened(0.25f));
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
            DrawRect(new Rect2(_bar.Position + new Vector2(0f, _bar.Size.Y - 2f), new Vector2(fillWidth, 2f)), fill.Darkened(0.4f));
            DrawRect(new Rect2(_bar.Position + new Vector2(fillWidth - 1f, 1f), new Vector2(1f, _bar.Size.Y - 1f)), fill.Lerp(Paper, 0.25f));
        }
        // Quarts gravés dans la piste, au-dessus du remplissage : ils se lisent sans allonger la jauge.
        for (int tick = 1; tick < 4; tick++)
            DrawRect(new Rect2(_bar.Position.X + Mathf.Floor(_bar.Size.X * tick / 4f), _bar.Position.Y + 2f, 1f, _bar.Size.Y - 4f),
                Ink with { A = 0.45f });
        if (_shieldRatio > 0f)
            DrawRect(new Rect2(_bar.Position, new Vector2(Mathf.Floor(_bar.Size.X * _shieldRatio), 2f)), Shield);
    }
}
