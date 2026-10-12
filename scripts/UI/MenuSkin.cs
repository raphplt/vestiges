using System.Collections.Generic;
using Godot;

namespace Vestiges.UI;

/// <summary>Matière commune des menus : pierre ébréchée, creux calmes et arêtes chaudes.</summary>
public static class MenuSkin
{
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static Texture2D _frame;
    private static Texture2D _interiorBackground;
    public static Texture2D InteriorBackground => _interiorBackground ??=
        GD.Load<Texture2D>("res://assets/ui/menus/stone/loading.png");

    public static Texture2D TextureFor(string name)
    {
        if (name is not ("ui_panel_frame.png" or "ui_button_normal.png" or "ui_button_hover.png"
            or "ui_button_pressed.png" or "ui_button_disabled.png" or "ui_card_normal.png"
            or "ui_card_selected.png" or "ui_card_locked.png" or "ui_tab_normal.png"
            or "ui_tab_hover.png" or "ui_tab_active.png"))
            return null;
        if (name == "ui_panel_frame.png")
        {
            if (_frame != null) return _frame;
            _frame = Stone(new Color("24212d"), new Color("6b6161"), new Color("3a3535"), false, 18);
            return _frame;
        }
        if (Textures.TryGetValue(name, out Texture2D cached))
            return cached;
        bool active = name.Contains("hover") || name.Contains("selected") || name.Contains("active");
        bool pressed = name.Contains("pressed");
        bool disabled = name.Contains("disabled") || name.Contains("locked");
        Texture2D texture = Stone(disabled ? new Color("201e2a") : new Color("24212d"), active ? new Color("a68b6b") : new Color("6b6161"),
            new Color("3a3535"), pressed, name.StartsWith("ui_card", System.StringComparison.Ordinal) ? 12 : 6);
        Textures[name] = texture;
        return texture;
    }

    private static Texture2D Stone(Color body, Color rim, Color shadow, bool pressed, int cornerSize = 6) =>
        MenuStoneTextures.Create(body, rim, shadow, pressed, cornerSize);

    private static Texture2D Cached(string key, System.Func<Texture2D> create)
    {
        if (!Textures.TryGetValue(key, out Texture2D texture))
            Textures[key] = texture = create();
        return texture;
    }

    /// <summary>Action qui engage la partie (Partir) : dalle de bronze à arête dorée, plus haute que tout autre bouton.</summary>
    public static void StyleCallToAction(Button button)
    {
        Color shadow = new("2a1d0c");
        Texture2D normal = Cached("cta", () => Stone(new Color("6e5022"), new Color("e2c06e"), shadow, false));
        Texture2D hover = Cached("cta-hover", () => Stone(new Color("8a6629"), new Color("f6dc92"), shadow, false));
        Texture2D pressed = Cached("cta-pressed", () => Stone(new Color("8a6629"), new Color("f6dc92"), shadow, true));
        UITheme.ApplyButtonStyle(button, normal, hover, pressed, TextureFor("ui_button_disabled.png"));
        // Le focus entoure la dalle sans la remplacer : au repos, Partir garde souvent le focus.
        StyleBoxFlat focus = new() { DrawCenter = false, BorderColor = new Color("f6dc92") };
        focus.SetBorderWidthAll(2);
        focus.SetExpandMarginAll(4);
        button.AddThemeStyleboxOverride("focus", focus);
        button.AddThemeFontOverride("font", UITheme.StrongFont);
        UITheme.SetTextRole(button, TextRole.Title);
        foreach (string state in new[] { "font_color", "font_hover_color", "font_focus_color", "font_pressed_color", "font_hover_pressed_color" })
            button.AddThemeColorOverride(state, UITheme.TextLight);
        button.AddThemeColorOverride("font_disabled_color", UITheme.TextDim);
        button.AddThemeColorOverride("font_outline_color", shadow);
        button.AddThemeConstantOverride("outline_size", 6);
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            StyleBox style = button.GetThemeStylebox(state);
            style.ContentMarginLeft = style.ContentMarginRight = 24;
            style.ContentMarginTop = style.ContentMarginBottom = 14;
        }
    }

    /// <summary>Dalle d'une chose acquise (quête accomplie) : pierre chaude à arête dorée, lisible d'un coup d'œil dans une liste.</summary>
    public static StyleBoxTexture AcquiredCard() =>
        UITheme.CreateNinePatch(Cached("acquired", () => Stone(new Color("3d3527"), new Color("a88a4c"), new Color("1a150c"), false, 12)), 8, 8, 8, 8);

    /// <summary>
    /// Carte d'une liste à choisir (personnages) : dalle de pierre au repos, dalle à tranche dorée quand elle est examinée.
    /// <paramref name="examined"/> garde la carte allumée hors survol.
    /// </summary>
    public static void StyleRosterCard(Button card, bool examined)
    {
        StyleBoxTexture rest = UITheme.CreateNinePatch(TextureFor("ui_card_normal.png"), 8, 8, 8, 8);
        StyleBoxTexture lit = Choice(UITheme.GoldBright, true);
        card.AddThemeStyleboxOverride("normal", examined ? lit : rest);
        foreach (string state in new[] { "hover", "focus", "pressed", "hover_pressed" })
            card.AddThemeStyleboxOverride(state, lit);
    }

    /// <summary>Dalle de choix : tranche de rareté, surface opaque et repère de sélection gravé.</summary>
    public static StyleBoxTexture Choice(Color accent, bool focused = false, int rank = -1)
    {
        string key = $"choice-{accent.ToHtml()}-{focused}-{rank}";
        if (!Textures.TryGetValue(key, out Texture2D texture))
        {
            Color edge = focused ? accent.Lightened(0.22f) : accent.Darkened(0.18f);
            texture = Stone(new Color("24212d"), edge, new Color("3a3535"), false, 12);
            Textures[key] = texture;
        }
        StyleBoxTexture style = UITheme.CreateNinePatch(texture, 12, 8, 8, 8);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = style.ContentMarginBottom = 10;
        return style;
    }

    public static StyleBoxTexture Panel(int margin = 28)
    {
        StyleBoxTexture style = UITheme.CreateNinePatch(TextureFor("ui_panel_frame.png"), 8, 8, 8, 8);
        style.ContentMarginLeft = style.ContentMarginRight = Mathf.Max(36, margin);
        style.ContentMarginTop = style.ContentMarginBottom = Mathf.Max(36, margin);
        return style;
    }

    /// <summary>Une fenêtre de rencontre grandit avec ses cartes, sans dépasser la hauteur de lecture.</summary>
    public static void CenterPanel(PanelContainer panel, float width, float height)
    {
        panel.CustomMinimumSize = new Vector2(width, height);
        panel.OffsetLeft = -width / 2f;
        panel.OffsetRight = width / 2f;
        panel.OffsetTop = -height / 2f;
        panel.OffsetBottom = height / 2f;
    }

    public static void StyleButton(Button button, bool primary = false)
    {
        UITheme.ApplyButtonStyle(button, TextureFor(primary ? "ui_button_hover.png" : "ui_button_normal.png"),
            TextureFor("ui_button_hover.png"), TextureFor("ui_button_pressed.png"), TextureFor("ui_button_disabled.png"));
        button.AddThemeFontOverride("font", UITheme.StrongFont);
        UITheme.SetTextRole(button, TextRole.Heading);
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            StyleBox style = button.GetThemeStylebox(state);
            style.ContentMarginLeft = style.ContentMarginRight = 24;
            style.ContentMarginTop = style.ContentMarginBottom = 10;
        }
    }

    public static Button Button(string text, bool primary = false)
    {
        Button button = new() { Text = text, CustomMinimumSize = new Vector2(0, 54), MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        StyleButton(button, primary);
        return button;
    }

    public static void StyleLink(Button button, bool active = false)
    {
        StyleBoxEmpty empty = new() { ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 8, ContentMarginBottom = 8 };
        foreach (string state in new[] { "normal", "pressed", "hover_pressed", "disabled" })
            button.AddThemeStyleboxOverride(state, empty);
        StyleBoxFlat lit = new() { BgColor = new Color(0.83f, 0.66f, 0.26f, 0.07f), BorderWidthLeft = 3, BorderColor = UITheme.GoldBright,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 8, ContentMarginBottom = 8 };
        button.AddThemeStyleboxOverride("hover", lit);
        button.AddThemeStyleboxOverride("focus", lit);
        button.AddThemeFontOverride("font", UITheme.StrongFont);
        button.AddThemeColorOverride("font_color", active ? UITheme.GoldBright : UITheme.TextColor);
        button.AddThemeColorOverride("font_focus_color", UITheme.GoldBright);
        button.AddThemeColorOverride("font_hover_color", UITheme.GoldBright);
        UITheme.SetTextRole(button, TextRole.Heading);
        UITheme.WireButtonAudio(button);
    }

    public static void AddBackdrop(Control parent, bool subdued = false, float brightness = 1f)
    {
        ColorRect ground = new() { Color = new Color(0.045f, 0.045f, 0.075f, 1f), MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(ground);
        ground.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        TextureRect art = new()
        {
            Texture = InteriorBackground, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Modulate = new Color(brightness, brightness, brightness, subdued ? 0.3f : 1f)
        };
        parent.AddChild(art);
        art.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    /// <summary>Fond d'un écran ouvert en run (pause, journal) : le jeu flou et assombri derrière (plan 37 R1).</summary>
    public static PixelBackdrop AddGameBackdrop(Control parent, Color tint)
    {
        PixelBackdrop backdrop = new(tint);
        parent.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return backdrop;
    }

    public static Texture2D SliderThumb(bool active)
    {
        string key = active ? "thumb-active" : "thumb";
        if (Textures.TryGetValue(key, out Texture2D texture)) return texture;
        using Image image = Image.CreateEmpty(24, 32, false, Image.Format.Rgba8);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 24; x++)
        {
            if (Mathf.Abs(x - 12) + Mathf.Abs(y - 16) * 0.55f > 12) continue;
            Color color = active ? UITheme.GoldBright : UITheme.GoldColor;
            if (x > 12) color = color.Darkened(0.35f);
            if (y > 25) color = color.Darkened(0.35f);
            image.SetPixel(x, y, color);
        }
        texture = ImageTexture.CreateFromImage(image);
        Textures[key] = texture;
        return texture;
    }

    public static void Clear(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}
