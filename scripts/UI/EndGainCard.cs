using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Carte de gain du bilan (plan 02 lot D, M4). Elle attend face cachée, se retourne à son tour, puis fait monter son
/// compteur s'il y en a un (Vestiges gagnés). Le gain est déjà acquis quand la carte s'anime : l'animation ne fait que
/// le montrer, et <see cref="Advance"/> avec un temps au-delà de <see cref="Duration"/> donne l'état final.
/// </summary>
public partial class EndGainCard : PanelContainer
{
    private const float FlipSec = 0.3f;
    private const float CountSec = 0.8f;
    private const float TickInterval = 0.06f;
    private static readonly Color SlotColor = new(0.08f, 0.07f, 0.12f, 0.9f);
    private static readonly Color BackColor = new(0.05f, 0.045f, 0.08f, 0.95f);

    private StyleBoxFlat _face;
    private StyleBoxFlat _back;
    private Control _content;
    private Label _label;
    private string _flipSound = "sfx_perk_choix";
    private string _counterFormat;
    private int _counterTarget;
    private int _shownValue = -1;
    private bool _faceUp;
    private float _lastTick = float.MinValue;

    /// <summary>Durée de l'animation de cette carte, retournement et compteur compris.</summary>
    public float Duration => FlipSec + (_counterFormat != null ? CountSec : 0f);

    public void Setup(string text, Color accent, Font font, string icon = null, string flipSound = null)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        if (flipSound != null)
            _flipSound = flipSound;
        _face = new StyleBoxFlat
        {
            BgColor = SlotColor,
            BorderColor = accent,
            BorderWidthTop = 3,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        };
        _back = (StyleBoxFlat)_face.Duplicate();
        _back.BgColor = BackColor;
        _back.BorderColor = new Color(accent, 0.45f);
        _back.BorderWidthLeft = 2;
        _back.BorderWidthRight = 2;
        _back.BorderWidthBottom = 2;

        _label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        _label.AddThemeFontOverride("font", font);
        UITheme.SetTextRole(_label, TextRole.Lead);
        _label.AddThemeColorOverride("font_color", accent);
        _label.AddThemeColorOverride("font_outline_color", UITheme.OutlineColor);
        _label.AddThemeConstantOverride("outline_size", 4);

        if (string.IsNullOrEmpty(icon) || !ResourceLoader.Exists(icon))
        {
            _content = _label;
        }
        else
        {
            HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 12);
            row.AddChild(new TextureRect
            {
                Texture = GD.Load<Texture2D>(icon),
                CustomMinimumSize = new Vector2(36f, 36f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            row.AddChild(_label);
            _content = row;
        }
        AddChild(_content);
        ShowFace(false);
    }

    /// <summary>
    /// Compteur qui monte de zéro à <paramref name="target"/> une fois la carte retournée. La largeur est celle du texte
    /// final : la carte ne change pas de taille pendant le décompte.
    /// </summary>
    public void SetCounter(string format, int target)
    {
        _counterFormat = format;
        _counterTarget = target;
        _label.Text = string.Format(format, target.ToString("N0"));
        _label.CustomMinimumSize = new Vector2(_label.GetMinimumSize().X, 0f);
    }

    /// <summary>
    /// Place la carte à <paramref name="t"/> secondes de son tour (négatif : pas encore venu). Muette, elle ne joue
    /// aucun son : c'est le cas quand le joueur passe la révélation.
    /// </summary>
    public void Advance(float t, bool quiet)
    {
        PivotOffset = Size / 2f;
        float flip = Mathf.Clamp(t / FlipSec, 0f, 1f);
        // Le retournement écrase la carte à plat puis la redéplie, face visible.
        Scale = new Vector2(Mathf.Max(0.02f, Mathf.Abs(1f - 2f * flip)), 1f);
        bool faceUp = flip >= 0.5f;
        if (faceUp != _faceUp)
        {
            ShowFace(faceUp);
            if (faceUp && !quiet)
                AudioManager.PlayUI(_flipSound, 0.04f);
        }

        if (_counterFormat == null || !faceUp)
            return;
        float count = Mathf.Clamp((t - FlipSec) / CountSec, 0f, 1f);
        int value = Mathf.RoundToInt(_counterTarget * (1f - Mathf.Pow(1f - count, 3f)));
        if (value == _shownValue)
            return;
        _shownValue = value;
        _label.Text = string.Format(_counterFormat, value.ToString("N0"));
        if (!quiet && t - _lastTick >= TickInterval)
        {
            _lastTick = t;
            AudioManager.PlayUI("xp_gain", 0.06f, -8f);
        }
    }

    private void ShowFace(bool faceUp)
    {
        _faceUp = faceUp;
        AddThemeStyleboxOverride("panel", faceUp ? _face : _back);
        _content.Modulate = new Color(1f, 1f, 1f, faceUp ? 1f : 0f);
    }
}
