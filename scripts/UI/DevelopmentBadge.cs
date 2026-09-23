using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Repère permanent du profil de test, y compris dans les menus de pause.</summary>
public static class DevelopmentBadge
{
    public static void AttachTo(Node owner)
    {
        if (!DevelopmentMode.IsEnabled)
            return;

        CanvasLayer layer = new() { Name = "DevelopmentBadge", Layer = 2000 };
        owner.AddChild(layer);
        PanelContainer panel = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.GrowVertical = Control.GrowDirection.Begin;
        panel.OffsetRight = -12f;
        panel.OffsetBottom = -12f;
        StyleBoxFlat style = new()
        {
            BgColor = new Color("1a1a2e"),
            ContentMarginLeft = 12f, ContentMarginRight = 12f,
            ContentMarginTop = 6f, ContentMarginBottom = 6f
        };
        panel.AddThemeStyleboxOverride("panel", style);
        Label label = new()
        {
            Text = owner.Tr("UI_DEV_MODE"),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", new Color("d4a843"));
        panel.AddChild(label);
    }
}
