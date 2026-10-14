using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

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
        button.AddThemeFontOverride("font", UITheme.TitleFont);
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
            Color edge = focused ? accent.Lightened(0.22f) : rank >= 0 ? accent : accent.Darkened(0.18f);
            // Une teinte grise (le commun) éclaircit la dalle sans la colorer : on la mêle moins, pour qu'elle reste un
            // voile sur la pierre sombre comme les autres raretés au lieu d'une plaque claire.
            float tint = (focused ? 0.26f : 0.18f) * Mathf.Lerp(0.45f, 1f, Mathf.Clamp(accent.S * 2.5f, 0f, 1f));
            Color body = rank >= 0 ? new Color("24212d").Lerp(accent, tint) : new Color("24212d");
            texture = Stone(body, edge, rank >= 0 ? accent.Darkened(0.65f) : new Color("3a3535"), false, 12);
            Textures[key] = texture;
        }
        StyleBoxTexture style = UITheme.CreateNinePatch(texture, 12, 8, 8, 8);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = style.ContentMarginBottom = 10;
        return style;
    }

    /// <summary>La surface entière annonce la rareté ; une case verrouillée conserve une teinte assourdie.</summary>
    public static StyleBoxTexture ItemCard(string rarityId, bool focused = false, bool locked = false)
    {
        Color accent = RarityPalette.Main(rarityId);
        string key = $"item-{rarityId}-{focused}-{locked}";
        Texture2D texture = Cached(key, () => Stone(
            new Color("24212d").Lerp(accent, locked ? 0.14f : focused ? 0.54f : 0.44f),
            locked ? accent.Darkened(0.48f) : focused ? accent.Lightened(0.2f) : accent,
            accent.Darkened(0.68f), false, 12));
        StyleBoxTexture style = UITheme.CreateNinePatch(texture, 12, 12, 12, 12);
        style.ContentMarginLeft = style.ContentMarginRight = 12;
        style.ContentMarginTop = style.ContentMarginBottom = 12;
        return style;
    }

    public static void StyleItemButton(Button button, string rarityId, bool locked = false)
    {
        StyleBoxTexture normal = ItemCard(rarityId, locked: locked);
        StyleBoxTexture lit = ItemCard(rarityId, focused: true, locked: locked);
        button.AddThemeStyleboxOverride("normal", normal);
        foreach (string state in new[] { "hover", "pressed", "hover_pressed" })
            button.AddThemeStyleboxOverride(state, lit);
        // Le focus reste distinct d'un légendaire : contour clair, sans remplacer sa teinte.
        StyleBoxFlat focus = new() { DrawCenter = false, BorderColor = UITheme.TextLight, CornerDetail = 1, AntiAliasing = false };
        focus.SetBorderWidthAll(2);
        focus.SetCornerRadiusAll(14);
        // À l'intérieur de la case : le ScrollContainer ne coupe pas le focus de la première rangée.
        focus.SetExpandMarginAll(-2);
        button.AddThemeStyleboxOverride("focus", focus);
        button.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }

    /// <summary>Petit format du HUD : fond plein et bord d'un pixel, sans rogner l'icône avec une grosse tranche.</summary>
    public static StyleBoxFlat ItemIcon(string rarityId)
    {
        Color accent = RarityPalette.Main(rarityId);
        StyleBoxFlat style = new() { BgColor = new Color("24212d").Lerp(accent, 0.44f), BorderColor = accent };
        style.SetBorderWidthAll(1);
        style.ContentMarginLeft = style.ContentMarginRight = 2;
        style.ContentMarginTop = style.ContentMarginBottom = 2;
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

    /// <summary>
    /// Lien de texte : au survol et au focus, le texte passe en or et se souligne d'un trait de pixels, sans fond ni bandeau.
    /// Le bouton prend la largeur de son texte pour que le trait reste sous les mots.
    /// </summary>
    public static void StyleLink(Button button, bool active = false)
    {
        StyleBoxEmpty empty = new() { ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 8, ContentMarginBottom = 8 };
        foreach (string state in new[] { "normal", "pressed", "hover_pressed", "disabled" })
            button.AddThemeStyleboxOverride(state, empty);
        StyleBoxFlat underline = new() { BgColor = Colors.Transparent, BorderWidthBottom = 2, BorderColor = UITheme.GoldBright,
            AntiAliasing = false, ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 8, ContentMarginBottom = 8 };
        button.AddThemeStyleboxOverride("hover", underline);
        button.AddThemeStyleboxOverride("focus", underline);
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
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

    // Pixels de menu doublés, comme les dalles de pierre (MenuStoneTextures).
    private const int PixelScale = 2;
    private static readonly Color Ink = new("1a1a2e");
    private static readonly Color[] Bronze = { new("f6dc92"), new("e2c06e"), new("b08a3e"), new("8a6629"), new("5e4219") };
    private const int SliderNativeHeight = 9;

    /// <summary>Hauteur affichée de la rainure et de son incrustation : marges hautes et basses de leur StyleBox.</summary>
    public const int SliderHeight = SliderNativeHeight * PixelScale;

    private static Texture2D Pixels(int width, int height, System.Func<int, int, Color> paint)
    {
        using Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            image.SetPixel(x, y, paint(x, y));
        image.Resize(width * PixelScale, height * PixelScale, Image.Interpolation.Nearest);
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Rainure creusée dans la pierre : la lèvre du haut tombe dans l'ombre, le fond est sombre, la paroi du bas prend
    /// la lumière venue d'en haut à gauche.
    /// </summary>
    public static Texture2D SliderTrack() => Cached("slider-track", () => Pixels(16, SliderNativeHeight, (x, y) =>
    {
        bool end = x == 0 || x == 15;
        if (end && (y == 0 || y == SliderNativeHeight - 1)) return Colors.Transparent;
        if (end || y == 0) return Ink;
        return y switch
        {
            1 => new Color("0c0a11"),
            2 => new Color("131119"),
            7 => new Color("4a4240"),
            8 => new Color("736868"),
            _ => (x * 7 + y * 3) % 5 == 0 ? new Color("211e29") : new Color("1a1821"),
        };
    }));

    /// <summary>Incrustation de bronze au fond de la rainure, jusqu'à la poignée ; même hauteur que la rainure.</summary>
    public static Texture2D SliderFill() => Cached("slider-fill", () => Pixels(16, SliderNativeHeight, (x, y) =>
    {
        if (x == 0 || x == 15 || y == 0 || y == SliderNativeHeight - 1) return Colors.Transparent;
        return y switch
        {
            1 => new Color("3b2a12"),
            2 => Bronze[0],
            3 => Bronze[1],
            4 or 5 => Bronze[2],
            6 => Bronze[3],
            _ => Bronze[4],
        };
    }));

    /// <summary>
    /// Poignée : taquet de bronze à pans coupés, modelé en quatre tons depuis le haut à gauche et gravé d'un trait
    /// vertical ; au survol et au focus, le métal s'éclaircit d'un ton.
    /// </summary>
    public static Texture2D SliderThumb(bool active) => Cached(active ? "thumb-active" : "thumb", () =>
    {
        const int width = 13, height = 19, cut = 3;
        bool Inside(int x, int y) => x >= 0 && y >= 0 && x < width && y < height
            && x + y >= cut && (width - 1 - x) + y >= cut && x + (height - 1 - y) >= cut && (width - 1 - x) + (height - 1 - y) >= cut;
        int shift = active ? 0 : 1;
        return Pixels(width, height, (x, y) =>
        {
            if (!Inside(x, y)) return Colors.Transparent;
            if (!Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1)) return Ink;
            // Arêtes : le pan du haut et celui de gauche accrochent la lumière, ceux du bas et de droite tombent dans l'ombre.
            if (!Inside(x, y - 2) || !Inside(x - 2, y)) return Bronze[shift];
            if (!Inside(x, y + 2) || !Inside(x + 2, y)) return Bronze[4];
            if (x == width / 2 && y >= 5 && y <= height - 6) return Bronze[4];
            if (x == width / 2 + 1 && y >= 5 && y <= height - 6) return Bronze[shift];
            float light = (x / (float)width + y / (float)height) * 0.5f;
            return Bronze[Mathf.Min(4, shift + (light < 0.4f ? 1 : light < 0.6f ? 2 : 3))];
        });
    });

    /// <summary>Croix de fermeture : deux traits épais au contour sombre ; la couleur d'icône du bouton la teinte.</summary>
    public static Texture2D CloseCross() => Cached("close-cross", () =>
    {
        const int size = 11;
        bool Stroke(int x, int y) => x >= 1 && y >= 1 && x < size - 1 && y < size - 1
            && (Mathf.Abs(x - y) <= 1 || Mathf.Abs(x + y - (size - 1)) <= 1);
        return Pixels(size, size, (x, y) =>
        {
            if (Stroke(x, y)) return new Color("f2ead8");
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (Stroke(x + dx, y + dy)) return Ink;
            return Colors.Transparent;
        });
    });

    public static void Clear(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}
