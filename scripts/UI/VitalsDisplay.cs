using Godot;

namespace Vestiges.UI;

/// <summary>
/// Vie et niveau posés sur le jeu (plan 37 R2, DECISIONS §117) : niveau dans un petit sceau cerclé de métal, jauge fine
/// du même métal centrée sur sa hauteur, PV au-dessus. Textes en Alkhemikal sur sa grille de 16 px, hors de la jauge.
/// </summary>
public partial class VitalsDisplay : Control
{
    private const float PlateWidth = 192f;
    private const float BadgeWidth = 34f;
    private const float BadgeInset = 2f;
    private const float BarLeft = 42f;
    private const float BarWidth = 146f;
    private const float BarHeight = 7f;
    private const float LowHpRatio = 0.3f;
    private const float CaptionBandRatio = 0.85f;
    private const float LevelLeadRatio = 0.2f;
    private const float LevelBodyRatio = 0.85f;
    private const float HealthOverlap = 3f;
    private const int Outline = 4;
    private static readonly Color Ink = new("1a1a2e");
    private static readonly Color Metal = new("6b6161");
    private static readonly Color Paper = new("e8e0d4");
    private static readonly Color Gold = new("d4a843");
    private static readonly Color Green = new("7bc558");
    private static readonly Color Orange = new("e07b39");
    private static readonly Color Red = new("c4432b");
    private static readonly Color Shield = new(0.72f, 0.86f, 1f);

    private Label _caption;
    private Label _level;
    private Label _health;
    private VitalsPerkOverlay _perks;
    private Rect2 _bar;
    private Rect2 _badge;
    private float _badgeBandHeight;
    private float _hpRatio = 1f;
    private float _chipRatio = 1f;
    private float _shieldRatio;
    private float _pulse;
    private Tween _levelTween;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _caption = CreateLabel("LevelCaption", Tr("UI_HUD_LEVEL"), 16, Gold.Lerp(Paper, 0.35f));
        _level = CreateLabel("Level", "1", 24, Paper);
        _health = CreateLabel("HealthValue", "100 / 100", 16, Paper);
        _health.HorizontalAlignment = HorizontalAlignment.Right;
        _perks = new VitalsPerkOverlay { Name = "SurvivalPerks" };
        AddChild(_perks);
        LayoutLabels();
        SetProcess(false);
    }

    private Label CreateLabel(string name, string text, int fontSize, Color color)
    {
        Label label = new()
        {
            Name = name, Text = text, MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        };
        UITheme.SetCompactText(label, fontSize);
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
        // Les boîtes de ligne d'Alkhemikal débordent des glyphes : la légende et le chiffre se chevauchent dans le sceau.
        _badgeBandHeight = Mathf.Floor(captionHeight * CaptionBandRatio);
        float levelTop = _badgeBandHeight - Mathf.Floor(levelHeight * LevelLeadRatio);
        float badgeHeight = levelTop + Mathf.Floor(levelHeight * LevelBodyRatio);
        _badge = new Rect2(BadgeInset, BadgeInset, BadgeWidth, badgeHeight);
        float height = badgeHeight + BadgeInset * 2f;
        CustomMinimumSize = Size = new Vector2(PlateWidth, height);
        _caption.Position = _badge.Position + new Vector2(0f, (_badgeBandHeight - captionHeight) * 0.5f);
        _caption.Size = new Vector2(BadgeWidth, captionHeight);
        _level.Position = _badge.Position + new Vector2(0f, levelTop);
        _level.Size = new Vector2(BadgeWidth, levelHeight);
        // PV et jauge forment un bloc centré sur la hauteur du sceau.
        float groupHeight = healthHeight - HealthOverlap + BarHeight;
        float groupTop = Mathf.Round(_badge.GetCenter().Y - groupHeight * 0.5f);
        _bar = new Rect2(BarLeft, groupTop + healthHeight - HealthOverlap, BarWidth, BarHeight);
        _health.Position = new Vector2(BarLeft, groupTop);
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

        DrawBadge(rim);

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

    /// <summary>Sceau du niveau : coins coupés, liseré de métal, bandeau de légende, rivets d'or.</summary>
    private void DrawBadge(Color rim)
    {
        Rect2 b = _badge;
        Color ink = Ink with { A = 0.9f };
        DrawRect(new Rect2(b.Position.X - 2f, b.Position.Y - 1f, b.Size.X + 4f, b.Size.Y + 2f), ink);
        DrawRect(new Rect2(b.Position.X - 1f, b.Position.Y - 2f, b.Size.X + 2f, b.Size.Y + 4f), ink);
        DrawRect(new Rect2(b.Position.X - 1f, b.Position.Y, b.Size.X + 2f, b.Size.Y), rim);
        DrawRect(new Rect2(b.Position.X, b.Position.Y - 1f, b.Size.X, b.Size.Y + 2f), rim);
        DrawRect(new Rect2(b.Position.X, b.Position.Y - 1f, b.Size.X, 1f), rim.Lightened(0.25f));
        DrawRect(new Rect2(b.Position.X, b.End.Y, b.Size.X, 1f), rim.Darkened(0.3f));
        DrawRect(b, Ink.Darkened(0.45f) with { A = 0.92f });
        Rect2 band = new(b.Position, new Vector2(b.Size.X, _badgeBandHeight));
        DrawRect(band, Metal.Darkened(0.45f));
        DrawRect(new Rect2(b.Position.X, band.End.Y, b.Size.X, 1f), rim.Darkened(0.15f));
        DrawRect(new Rect2(b.Position.X, band.End.Y + 1f, b.Size.X, 1f), Ink with { A = 0.6f });
        Color rivet = Gold.Darkened(0.1f);
        DrawRect(new Rect2(b.Position.X + 1f, b.End.Y - 2f, 1f, 1f), rivet);
        DrawRect(new Rect2(b.End.X - 2f, b.End.Y - 2f, 1f, 1f), rivet);
    }
}
