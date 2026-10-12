using Godot;

namespace Vestiges.UI;

/// <summary>
/// Grammaire visuelle des écrans de choix (level-up, Mémorial, Faille) : cartes à cadre de rareté, boutons d'action,
/// libellés. La rareté se lit à la couleur, au symbole et à l'épaisseur du cadre.
/// </summary>
public static class ChoiceStyle
{
    public const float CardWidth = 760f;

    public static readonly Color GoldBright = UITheme.GoldBright;
    public static readonly Color GoldDim = UITheme.GoldDim;
    public static readonly Color TextLight = UITheme.TextLight;
    public static readonly Color TextColor = UITheme.TextColor;
    public static readonly Color TextDim = UITheme.TextDim;
    public static readonly Color GainColor = new(0.55f, 0.85f, 0.45f);
    public static readonly Color LossColor = new(0.85f, 0.38f, 0.42f);
    public static readonly Color NeutralBorder = new(0.55f, 0.52f, 0.46f);
    /// <summary>Cadre des perks : sans rareté, distinct des couleurs de rareté et du cadre neutre des nouveautés.</summary>
    public static readonly Color PerkBorder = new(0.80f, 0.74f, 0.58f);
    public static readonly Color CardBg = new(0.07f, 0.07f, 0.11f, 0.96f);
    public static readonly Color OverlayColor = new(0.035f, 0.035f, 0.055f, 1f);
    /// <summary>
    /// Voile d'un menu ouvert en run : transparent, il ne fait que bloquer les clics ; le fond flouté par-dessus montre
    /// le jeu (plan 37 R1). Le bilan garde le voile opaque.
    /// </summary>
    public static readonly Color InRunOverlayColor = new(0.035f, 0.035f, 0.055f, 0f);

    /// <summary>Tranche et fond teinté portent la rareté, avec un contraste conservé pour lire le gain.</summary>
    public static void StyleCard(PanelContainer card, Color border, int rank, bool focused, bool enabled = true)
    {
        card.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        card.AddThemeStyleboxOverride("panel", MenuSkin.Choice(border, focused, rank));
        card.Modulate = enabled ? Colors.White : new Color(0.6f, 0.6f, 0.65f, 1f);
    }

    public static void StyleButton(Button button, bool focused)
    {
        UITheme.ApplyButtonStyle(button,
            UITheme.LoadTex(UITheme.MenusPath + (focused ? "ui_button_hover.png" : "ui_button_normal.png")),
            UITheme.LoadTex(UITheme.MenusPath + "ui_button_hover.png"),
            UITheme.LoadTex(UITheme.MenusPath + "ui_button_pressed.png"),
            UITheme.LoadTex(UITheme.MenusPath + "ui_button_disabled.png"));
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
        {
            StyleBox style = button.GetThemeStylebox(state);
            style.ContentMarginLeft = 12;
            style.ContentMarginRight = 12;
        }
    }

    public static Label MakeLabel(string text, TextRole role, Color color, bool expand, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        Label label = UITheme.MakeLabel(text, role, color, TextWeight.Regular, align);
        if (expand || align == HorizontalAlignment.Right)
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return label;
    }

    /// <summary>La comparaison garde la valeur actuelle claire et distingue la valeur obtenue en vert.</summary>
    public static RichTextLabel MakeGainLabel(string text, Color color, TextRole role)
    {
        RichTextLabel label = new()
        {
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontOverride("normal_font", UITheme.BodyFont);
        label.AddThemeFontSizeOverride("normal_font_size", UITheme.FontSize(role));
        label.AddThemeColorOverride("default_color", color);
        int arrow = text.IndexOf('→');
        if (arrow < 0)
            label.AddText(text);
        else
        {
            label.PushColor(TextColor);
            label.AddText(text[..(arrow + 1)]);
            label.Pop();
            label.PushColor(GainColor);
            label.AddText(text[(arrow + 1)..]);
            label.Pop();
        }
        return label;
    }
}
