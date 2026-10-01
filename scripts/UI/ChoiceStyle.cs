using Godot;

namespace Vestiges.UI;

/// <summary>
/// Grammaire visuelle des écrans de choix (level-up, Mémorial, Faille) : cartes à cadre de rareté, boutons d'action,
/// libellés. La rareté se lit à la couleur, au symbole et à l'épaisseur du cadre.
/// </summary>
public static class ChoiceStyle
{
    public const float CardWidth = 540f;

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
    public static readonly Color OverlayColor = new(0.0f, 0.0f, 0.02f, 0.75f);

    /// <summary>Cadre nine-patch natif ; la frise légendaire anime ses quatre poses sans recréer le style.</summary>
    public static void StyleCard(PanelContainer card, Color border, int rank, bool focused, bool enabled = true)
    {
        Texture2D[] frames = rank >= 0 ? RarityArt.Cards(rank)
            : new[] { UITheme.LoadTex(UITheme.MenusPath + "ui_card_normal.png") };
        StyleBoxTexture style = UITheme.CreateNinePatch(frames[0], 3, 3, 3, 3);
        style.ModulateColor = focused ? new Color(1.25f, 1.25f, 1.25f) : Colors.White;
        card.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        card.AddThemeStyleboxOverride("panel", style);
        card.Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.55f);
        RarityFrame animation = card.GetNodeOrNull<RarityFrame>("RarityFrame");
        if (animation == null)
        {
            animation = new RarityFrame { Name = "RarityFrame" };
            card.AddChild(animation);
        }
        animation.Configure(card, style, frames);
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
}
