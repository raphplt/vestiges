using Godot;

namespace Vestiges.UI;

/// <summary>Essai M8 : typographie pixel du Campement, isolée pour juger l'écran avant une reprise globale.</summary>
public static class CampTypography
{
    private static Font _regular;
    private static Font _strong;

    public static Font Regular => _regular ??= Load("PixelOperator.ttf");
    private static Font Strong => _strong ??= Load("PixelOperator-Bold.ttf");

    private static Font Load(string name)
    {
        // Copie locale : les réglages de cet essai ne modifient pas les ressources partagées ailleurs.
        FontFile font = (FontFile)GD.Load<FontFile>($"res://assets/fonts/pixel-operator/{name}").Duplicate();
        font.Antialiasing = TextServer.FontAntialiasing.None;
        return font;
    }

    public static void Apply(Control control, TextRole role, bool strong = false)
    {
        int size = role switch
        {
            TextRole.Caption or TextRole.Small => 24,
            TextRole.Title => 48,
            TextRole.Banner or TextRole.Display => 64,
            _ => 32,
        };
        control.AddThemeFontOverride("font", strong ? Strong : Regular);
        control.AddThemeFontSizeOverride("font_size", size);
        control.AddThemeConstantOverride("outline_size", 2);
        control.AddThemeColorOverride("font_outline_color", UITheme.OutlineColor);
    }

    public static Label Label(string text, TextRole role, Color color, TextWeight weight = TextWeight.Regular,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
        Label label = UITheme.MakeLabel(text, role, color, weight, align);
        Apply(label, role, weight != TextWeight.Regular);
        return label;
    }
}
