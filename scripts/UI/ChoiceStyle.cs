using Godot;

namespace Vestiges.UI;

/// <summary>
/// Grammaire visuelle des écrans de choix (level-up, Mémorial, Faille) : cartes à cadre de rareté, boutons d'action,
/// libellés. La rareté se lit à la couleur, au symbole et à l'épaisseur du cadre.
/// </summary>
public static class ChoiceStyle
{
    public const float CardWidth = 540f;

    public static readonly Color GoldBright = new(0.9f, 0.78f, 0.39f);
    public static readonly Color GoldDim = new(0.63f, 0.47f, 0.16f);
    public static readonly Color TextLight = new(0.92f, 0.9f, 0.85f);
    public static readonly Color TextColor = new(0.72f, 0.7f, 0.66f);
    public static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);
    public static readonly Color GainColor = new(0.55f, 0.85f, 0.45f);
    public static readonly Color LossColor = new(0.85f, 0.38f, 0.42f);
    public static readonly Color NeutralBorder = new(0.55f, 0.52f, 0.46f);
    public static readonly Color CardBg = new(0.07f, 0.07f, 0.11f, 0.96f);
    public static readonly Color OverlayColor = new(0.0f, 0.0f, 0.02f, 0.75f);

    /// <summary>La rareté se lit aussi à la forme : rien, ◆, ◆◆, ★, ★★ du Commun au Légendaire.</summary>
    public static string RarityGlyph(int rank) => rank switch
    {
        1 => "◆",
        2 => "◆◆",
        3 => "★",
        4 => "★★",
        _ => "",
    };

    /// <summary>Cadre d'une carte ; les grandes raretés ont un cadre plus épais, visible aussi sans la couleur.</summary>
    public static void StyleCard(PanelContainer card, Color border, int rank, bool focused, bool enabled = true)
    {
        StyleBoxFlat style = new()
        {
            BgColor = focused ? CardBg.Lightened(0.08f) : CardBg,
            BorderColor = focused ? border.Lightened(0.25f) : border with { A = enabled ? 0.85f : 0.35f },
        };
        style.SetBorderWidthAll((focused ? 3 : 2) + (rank >= 3 ? 1 : 0));
        style.SetCornerRadiusAll(3);
        card.AddThemeStyleboxOverride("panel", style);
        card.Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.55f);
    }

    public static void StyleButton(Button button, bool focused)
    {
        StyleBoxFlat style = new()
        {
            BgColor = focused ? new Color(0.16f, 0.15f, 0.22f, 0.95f) : new Color(0.1f, 0.1f, 0.15f, 0.9f),
            BorderColor = focused ? GoldBright : GoldDim with { A = button.Disabled ? 0.3f : 1f },
        };
        style.SetBorderWidthAll(focused ? 2 : 1);
        style.SetCornerRadiusAll(3);
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
            button.AddThemeStyleboxOverride(state, style);
    }

    public static Label MakeLabel(string text, int size, Color color, bool expand, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        Label label = new() { Text = text, HorizontalAlignment = align };
        if (expand || align == HorizontalAlignment.Right)
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
