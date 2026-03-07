using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Écran de choix au level up — affiche des Fragments de Mémoire (armes + passifs).
/// Pause le jeu, affiche 3 choix sous forme de cartes stylisées, reprend après sélection.
/// Supporte aussi les choix de perks via Mémorial (source monde).
/// </summary>
public partial class LevelUpScreen : CanvasLayer
{
    private const string MenusPath = "res://assets/ui/menus/";

    // --- Colors (shared palette from HubScreen) ---
    private static readonly Color GoldColor = new(0.83f, 0.66f, 0.26f);
    private static readonly Color GoldBright = new(0.9f, 0.78f, 0.39f);
    private static readonly Color GoldDim = new(0.63f, 0.47f, 0.16f);
    private static readonly Color TextColor = new(0.72f, 0.7f, 0.66f);
    private static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);
    private static readonly Color BgDark = new(0.04f, 0.05f, 0.09f);
    private static readonly Color WeaponNewColor = new(1f, 0.85f, 0.3f);
    private static readonly Color WeaponUpgradeColor = new(1f, 0.65f, 0.2f);
    private static readonly Color PassiveNewColor = new(0.5f, 0.85f, 1f);
    private static readonly Color PassiveUpgradeColor = new(0.3f, 0.7f, 0.95f);
    private static readonly Color OverlayColor = new(0.0f, 0.0f, 0.02f, 0.75f);

    // --- Cached textures ---
    private Texture2D _panelTex;
    private Texture2D _cardNormalTex;
    private Texture2D _cardSelectedTex;
    private Texture2D _separatorTex;

    // --- Rays config ---
    private const int RayCount = 14;
    private const float RaySpeed = 0.15f; // radians per second
    private static readonly Color RayColorA = new(0.83f, 0.66f, 0.26f, 0.08f);
    private static readonly Color RayColorB = new(0.9f, 0.78f, 0.39f, 0.04f);

    // --- Fragment rarity colors ---
    private static readonly Color RarityCommonBorder = new(0.25f, 0.24f, 0.2f, 0.6f);
    private static readonly Color RarityUncommonBorder = new(0.4f, 0.73f, 0.42f);
    private static readonly Color RarityRareBorder = new(1f, 0.7f, 0f);
    private static readonly Color RarityUncommonBg = new(0.05f, 0.1f, 0.05f, 0.95f);
    private static readonly Color RarityRareBg = new(0.12f, 0.09f, 0.02f, 0.95f);

    // --- Banish mode colors ---
    private static readonly Color BanishBorderColor = new(0.85f, 0.2f, 0.2f);
    private static readonly Color BanishBgColor = new(0.15f, 0.05f, 0.05f, 0.95f);
    private static readonly Color BanishLabelColor = new(1f, 0.3f, 0.3f);

    // --- UI nodes ---
    private ColorRect _overlay;
    private LightRaysControl _rays;
    private PanelContainer _panel;
    private VBoxContainer _cardsContainer;
    private HBoxContainer _actionButtons;
    private Button _rerollButton;
    private Button _banishButton;
    private Label _title;
    private Label _synergyNotification;
    private readonly List<PanelContainer> _cards = new();
    private readonly List<FragmentOption> _cardOptions = new();
    private readonly List<string> _cardRarities = new();
    private int _hoveredCardIndex = -1;
    private bool _banishMode;

    // --- Managers ---
    private PerkManager _perkManager;
    private FragmentManager _fragmentManager;
    private EventBus _eventBus;

    public override void _Ready()
    {
        LoadTextures();
        BuildUI();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.FragmentChoicesReady += OnFragmentChoicesReady;

        CreateSynergyNotification();
        HideScreen();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.FragmentChoicesReady -= OnFragmentChoicesReady;
    }

    public void SetPerkManager(PerkManager perkManager)
    {
        _perkManager = perkManager;
        _perkManager.PerkChoicesReady += OnPerkChoicesReady;
        _perkManager.SynergyActivated += OnSynergyActivated;
    }

    public void SetFragmentManager(FragmentManager fragmentManager)
    {
        _fragmentManager = fragmentManager;
        GD.Print("[LevelUpScreen] FragmentManager wired, listening on EventBus.FragmentChoicesReady");
    }

    // ==============================
    // Texture loading
    // ==============================

    private void LoadTextures()
    {
        _panelTex = LoadTex(MenusPath + "ui_panel_frame.png");
        _cardNormalTex = LoadTex(MenusPath + "ui_card_normal.png");
        _cardSelectedTex = LoadTex(MenusPath + "ui_card_selected.png");
        _separatorTex = LoadTex(MenusPath + "ui_separator_simple.png");
    }

    private static Texture2D LoadTex(string path)
    {
        if (ResourceLoader.Exists(path))
            return GD.Load<Texture2D>(path);
        GD.PushWarning($"[LevelUpScreen] Missing texture: {path}");
        return null;
    }

    // ==============================
    // UI construction
    // ==============================

    private void BuildUI()
    {
        // Dark overlay covering the whole screen
        _overlay = new ColorRect();
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay.Color = OverlayColor;
        _overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(_overlay);

        // Rotating light rays behind the panel
        _rays = new LightRaysControl(RayCount, RaySpeed, RayColorA, RayColorB);
        _rays.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _rays.MouseFilter = Control.MouseFilterEnum.Ignore;
        _rays.ProcessMode = ProcessModeEnum.Always;
        AddChild(_rays);

        // Main panel centered on screen
        _panel = new PanelContainer();
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.GrowHorizontal = Control.GrowDirection.Both;
        _panel.GrowVertical = Control.GrowDirection.Both;
        _panel.CustomMinimumSize = new Vector2(500, 100);

        if (_panelTex != null)
        {
            StyleBoxTexture panelStyle = CreateNinePatch(_panelTex, 6, 6, 6, 6);
            panelStyle.ContentMarginLeft = 20;
            panelStyle.ContentMarginRight = 20;
            panelStyle.ContentMarginTop = 16;
            panelStyle.ContentMarginBottom = 20;
            _panel.AddThemeStyleboxOverride("panel", panelStyle);
        }
        else
        {
            StyleBoxFlat fallback = new();
            fallback.BgColor = new Color(0.06f, 0.06f, 0.1f, 0.95f);
            fallback.SetBorderWidthAll(2);
            fallback.BorderColor = GoldDim;
            fallback.SetCornerRadiusAll(4);
            fallback.ContentMarginLeft = 20;
            fallback.ContentMarginRight = 20;
            fallback.ContentMarginTop = 16;
            fallback.ContentMarginBottom = 20;
            _panel.AddThemeStyleboxOverride("panel", fallback);
        }

        AddChild(_panel);

        // Inner VBox
        VBoxContainer innerVBox = new();
        innerVBox.AddThemeConstantOverride("separation", 12);
        _panel.AddChild(innerVBox);

        // Title
        _title = new Label();
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _title.AddThemeFontSizeOverride("font_size", 20);
        _title.AddThemeColorOverride("font_color", GoldBright);
        _title.Text = "FRAGMENT DE MÉMOIRE";
        innerVBox.AddChild(_title);

        // Separator under title
        if (_separatorTex != null)
        {
            TextureRect sep = new();
            sep.Texture = _separatorTex;
            sep.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            sep.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            sep.CustomMinimumSize = new Vector2(0, 6);
            sep.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            innerVBox.AddChild(sep);
        }

        // Cards container
        _cardsContainer = new VBoxContainer();
        _cardsContainer.AddThemeConstantOverride("separation", 8);
        innerVBox.AddChild(_cardsContainer);
    }

    // ==============================
    // Card creation
    // ==============================

    private PanelContainer CreateChoiceCard(
        string iconPath,
        string typeTag,
        Color tagColor,
        string name,
        string description,
        string badge,
        Color badgeColor,
        System.Action onPressed,
        string rarity = "common")
    {
        PanelContainer card = new();
        card.CustomMinimumSize = new Vector2(460, 80);

        // Card style (rarity-aware)
        _cardRarities.Add(rarity);
        ApplyCardStyle(card, false, rarity);

        // Make the card clickable
        card.MouseFilter = Control.MouseFilterEnum.Stop;
        card.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                onPressed?.Invoke();
                card.AcceptEvent();
            }
        };

        // Hover tracking
        int cardIndex = _cards.Count;
        card.MouseEntered += () => OnCardHovered(cardIndex);
        card.MouseExited += () => OnCardUnhovered(cardIndex);

        // Card inner layout: HBox with icon on left, text on right, badge on far right
        MarginContainer cardMargin = new();
        cardMargin.AddThemeConstantOverride("margin_left", 12);
        cardMargin.AddThemeConstantOverride("margin_right", 12);
        cardMargin.AddThemeConstantOverride("margin_top", 8);
        cardMargin.AddThemeConstantOverride("margin_bottom", 8);
        card.AddChild(cardMargin);

        HBoxContainer cardHBox = new();
        cardHBox.AddThemeConstantOverride("separation", 14);
        cardHBox.Alignment = BoxContainer.AlignmentMode.Begin;
        cardMargin.AddChild(cardHBox);

        // Icon (larger, 48x48)
        if (!string.IsNullOrEmpty(iconPath))
        {
            string resPath = iconPath.StartsWith("res://") ? iconPath : $"res://{iconPath}";
            if (ResourceLoader.Exists(resPath))
            {
                TextureRect icon = new();
                icon.CustomMinimumSize = new Vector2(48, 48);
                icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
                icon.Texture = GD.Load<Texture2D>(resPath);
                icon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
                cardHBox.AddChild(icon);
            }
        }

        // Text section (VBox with type tag, name, description)
        VBoxContainer textVBox = new();
        textVBox.AddThemeConstantOverride("separation", 2);
        textVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        textVBox.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        cardHBox.AddChild(textVBox);

        // Type tag (small, colored)
        if (!string.IsNullOrEmpty(typeTag))
        {
            Label tagLabel = new();
            tagLabel.Text = typeTag;
            tagLabel.AddThemeFontSizeOverride("font_size", 11);
            tagLabel.AddThemeColorOverride("font_color", tagColor);
            textVBox.AddChild(tagLabel);
        }

        // Name (prominent)
        Label nameLabel = new();
        nameLabel.Text = name;
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.9f, 0.85f));
        textVBox.AddChild(nameLabel);

        // Description / stats (smaller, dimmer)
        if (!string.IsNullOrEmpty(description))
        {
            Label descLabel = new();
            descLabel.Text = description;
            descLabel.AddThemeFontSizeOverride("font_size", 12);
            descLabel.AddThemeColorOverride("font_color", TextDim);
            textVBox.AddChild(descLabel);
        }

        // Badge on the right side (NEW / LVL X)
        if (!string.IsNullOrEmpty(badge))
        {
            Label badgeLabel = new();
            badgeLabel.Text = badge;
            badgeLabel.AddThemeFontSizeOverride("font_size", 14);
            badgeLabel.AddThemeColorOverride("font_color", badgeColor);
            badgeLabel.VerticalAlignment = VerticalAlignment.Center;
            badgeLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            cardHBox.AddChild(badgeLabel);
        }

        _cards.Add(card);
        return card;
    }

    private void ApplyCardStyle(PanelContainer card, bool selected, string rarity = "common")
    {
        Texture2D tex = selected ? _cardSelectedTex : _cardNormalTex;
        if (tex != null && rarity == "common")
        {
            StyleBoxTexture style = CreateNinePatch(tex, 4, 4, 4, 4);
            style.ContentMarginLeft = 0;
            style.ContentMarginRight = 0;
            style.ContentMarginTop = 0;
            style.ContentMarginBottom = 0;
            card.AddThemeStyleboxOverride("panel", style);
        }
        else
        {
            Color borderColor;
            Color bgColor;
            if (selected)
            {
                borderColor = rarity switch
                {
                    "rare" => RarityRareBorder,
                    "uncommon" => RarityUncommonBorder,
                    _ => GoldColor
                };
                bgColor = rarity switch
                {
                    "rare" => RarityRareBg,
                    "uncommon" => RarityUncommonBg,
                    _ => new Color(0.12f, 0.14f, 0.2f)
                };
            }
            else
            {
                borderColor = rarity switch
                {
                    "rare" => RarityRareBorder with { A = 0.7f },
                    "uncommon" => RarityUncommonBorder with { A = 0.6f },
                    _ => RarityCommonBorder
                };
                bgColor = rarity switch
                {
                    "rare" => new Color(0.1f, 0.08f, 0.02f, 0.9f),
                    "uncommon" => new Color(0.04f, 0.08f, 0.04f, 0.9f),
                    _ => new Color(0.08f, 0.08f, 0.12f)
                };
            }

            StyleBoxFlat flat = new();
            flat.BgColor = bgColor;
            flat.SetBorderWidthAll(rarity == "common" ? (selected ? 2 : 1) : 2);
            flat.BorderColor = borderColor;
            flat.SetCornerRadiusAll(3);
            card.AddThemeStyleboxOverride("panel", flat);
        }
    }

    private void OnCardHovered(int index)
    {
        _hoveredCardIndex = index;
        if (index >= 0 && index < _cards.Count)
        {
            string rarity = index < _cardRarities.Count ? _cardRarities[index] : "common";
            ApplyCardStyle(_cards[index], true, rarity);
        }
    }

    private void OnCardUnhovered(int index)
    {
        if (index == _hoveredCardIndex)
            _hoveredCardIndex = -1;
        if (index >= 0 && index < _cards.Count)
        {
            string rarity = index < _cardRarities.Count ? _cardRarities[index] : "common";
            ApplyCardStyle(_cards[index], false, rarity);
        }
    }

    // ==============================
    // Fragment Mode (level-up: armes + passifs)
    // ==============================

    private void OnFragmentChoicesReady(int count)
    {
        GD.Print($"[LevelUpScreen] OnFragmentChoicesReady received: count={count}");

        if (count <= 0 || _fragmentManager == null)
        {
            GD.PushWarning($"[LevelUpScreen] Aborting: count={count}, fragmentManager={(_fragmentManager != null ? "OK" : "NULL")}");
            return;
        }

        IReadOnlyList<FragmentOption> choices = _fragmentManager.PendingChoices;
        GD.Print($"[LevelUpScreen] PendingChoices.Count={choices.Count}");
        if (choices.Count == 0)
            return;

        ClearCards();
        _banishMode = false;
        _title.Text = "FRAGMENT DE MÉMOIRE";

        foreach (FragmentOption choice in choices)
        {
            _cardOptions.Add(choice);
            BuildFragmentCard(choice);
        }

        BuildActionButtons();

        // Vérifier si un choix rare est présent
        bool hasRare = false;
        foreach (FragmentOption c in choices)
        {
            if (c.Rarity is "rare" or "uncommon")
            {
                hasRare = true;
                break;
            }
        }

        ShowScreen();
        if (hasRare)
            Infrastructure.AudioManager.PlayUI("sfx_rare_fragment");
        GD.Print($"[LevelUpScreen] Fragment screen shown with {choices.Count} choices (hasRare={hasRare})");
    }

    private void BuildFragmentCard(FragmentOption choice)
    {
        Player player = GetTree().GetFirstNodeInGroup("player") as Player;
        string iconPath = GetFragmentSpritePath(choice.Id, choice.Type);
        string rarity = choice.Rarity ?? "common";

        string typeTag;
        Color tagColor;
        string name;
        string description;
        string badge;
        Color badgeColor;

        // Rarity label pour le tag
        string rarityPrefix = rarity switch
        {
            "rare" => "\u2605 ",     // ★
            "uncommon" => "\u25C6 ", // ◆
            _ => ""
        };

        switch (choice.Type)
        {
            case "weapon_new":
            {
                WeaponData weapon = WeaponDataLoader.Get(choice.Id);
                typeTag = rarityPrefix + "Arme";
                tagColor = rarity == "rare" ? RarityRareBorder : rarity == "uncommon" ? RarityUncommonBorder : WeaponNewColor;
                name = weapon?.Name ?? choice.Id;
                description = weapon != null ? FormatWeaponStats(weapon) : "";
                badge = "NOUVEAU";
                badgeColor = tagColor;
                break;
            }
            case "weapon_upgrade":
            {
                WeaponData weapon = WeaponDataLoader.Get(choice.Id);
                int level = player?.GetWeaponFragmentLevel(choice.Id) ?? 0;
                typeTag = rarityPrefix + "Arme";
                tagColor = rarity == "rare" ? RarityRareBorder : rarity == "uncommon" ? RarityUncommonBorder : WeaponUpgradeColor;
                name = weapon?.Name ?? choice.Id;
                description = weapon != null ? FormatWeaponStats(weapon) : "";
                badge = $"NIV {level} \u2192 {level + 1}";
                badgeColor = tagColor;
                break;
            }
            case "passive_new":
            {
                PassiveSouvenirData passive = PassiveSouvenirDataLoader.Get(choice.Id);
                typeTag = rarityPrefix + "Passif";
                tagColor = rarity == "rare" ? RarityRareBorder : rarity == "uncommon" ? RarityUncommonBorder : PassiveNewColor;
                name = passive?.Name ?? choice.Id;
                description = passive != null ? FormatPassiveStats(passive, 1) : "";
                badge = "NOUVEAU";
                badgeColor = tagColor;
                break;
            }
            case "passive_upgrade":
            {
                PassiveSouvenirData passive = PassiveSouvenirDataLoader.Get(choice.Id);
                int level = player?.GetPassiveLevel(choice.Id) ?? 0;
                typeTag = rarityPrefix + "Passif";
                tagColor = rarity == "rare" ? RarityRareBorder : rarity == "uncommon" ? RarityUncommonBorder : PassiveUpgradeColor;
                name = passive?.Name ?? choice.Id;
                description = passive != null ? FormatPassiveStats(passive, level + 1) : "";
                badge = $"NIV {level} \u2192 {level + 1}";
                badgeColor = tagColor;
                break;
            }
            default:
                typeTag = "";
                tagColor = TextColor;
                name = choice.Id;
                description = "";
                badge = "";
                badgeColor = TextColor;
                break;
        }

        string capturedId = choice.Id;
        string capturedType = choice.Type;

        PanelContainer card = CreateChoiceCard(
            iconPath, typeTag, tagColor, name, description, badge, badgeColor,
            () => OnFragmentSelected(capturedId, capturedType),
            rarity);

        _cardsContainer.AddChild(card);
    }

    private static string GetFragmentSpritePath(string id, string type)
    {
        if (type is "weapon_new" or "weapon_upgrade")
        {
            WeaponData weapon = WeaponDataLoader.Get(id);
            return weapon?.Sprite;
        }

        if (type is "passive_new" or "passive_upgrade")
        {
            PassiveSouvenirData passive = PassiveSouvenirDataLoader.Get(id);
            if (passive == null) return null;
            return passive.Stat switch
            {
                "damage" => "assets/ui/icons/ui_icon_perk_degats.png",
                "attack_speed" => "assets/ui/icons/ui_icon_perk_vitesse.png",
                "max_hp" => "assets/ui/icons/ui_icon_perk_hp.png",
                "speed" => "assets/ui/icons/ui_icon_perk_vitesse.png",
                "armor" => "assets/ui/icons/ui_icon_perk_armure.png",
                "aoe_radius" => "assets/ui/icons/ui_icon_perk_echo.png",
                "projectile_count" => "assets/ui/icons/ui_icon_perk_echo.png",
                "crit_chance" => "assets/ui/icons/ui_icon_perk_degats.png",
                "regen_rate" => "assets/ui/icons/ui_icon_perk_hp.png",
                "attack_range" => "assets/ui/icons/ui_icon_perk_vitesse.png",
                _ => "assets/ui/icons/ui_icon_perk_degats.png"
            };
        }

        return null;
    }

    private static string FormatWeaponStats(WeaponData w)
    {
        string patternLabel = w.AttackPattern switch
        {
            "arc" => "Arc",
            "linear" => "Ligne",
            "circular" => "Cercle",
            "orbital" => "Orbital",
            "burst" => "Rafale",
            "chain" => "Chaîne",
            "homing" => "Guidé",
            "ground" => "Sol",
            _ => w.AttackPattern
        };

        float dmg = w.Stats.TryGetValue("damage", out float d) ? d : 0;
        float spd = w.Stats.TryGetValue("attack_speed", out float s) ? s : 0;
        float range = w.Stats.TryGetValue("range", out float r) ? r : 0;

        return $"{patternLabel}  |  {dmg:0} dég  |  {spd:0.0}/s  |  {range:0}m";
    }

    private static string FormatPassiveStats(PassiveSouvenirData p, int level)
    {
        if (p.PerLevel == null || p.PerLevel.Length == 0)
            return p.Description;

        int idx = System.Math.Clamp(level - 1, 0, p.PerLevel.Length - 1);
        float value = p.PerLevel[idx];

        string statLabel = p.Stat switch
        {
            "damage" => "Dégâts",
            "attack_speed" => "Vit. attaque",
            "max_hp" => "PV max",
            "speed" => "Vitesse",
            "aoe_radius" => "Zone d'effet",
            "xp_magnet_radius" => "Rayon XP",
            "armor" => "Armure",
            "crit_chance" => "Chance crit",
            "regen_rate" => "Régén HP/s",
            "attack_range" => "Portée",
            "projectile_count" => "Projectiles",
            "cooldown_reduction" => "Réduction CD",
            "projectile_pierce" => "Perçage",
            _ => p.Stat
        };

        if (p.ModifierType == "multiplicative")
        {
            float percent = (value - 1f) * 100f;
            string sign = percent >= 0 ? "+" : "";
            return $"{statLabel} {sign}{percent:0}%";
        }

        string addSign = value >= 0 ? "+" : "";
        return $"{statLabel} {addSign}{value:0.#}";
    }

    private void BuildActionButtons()
    {
        // Remove old action buttons if any
        if (_actionButtons != null && IsInstanceValid(_actionButtons))
        {
            _actionButtons.QueueFree();
            _actionButtons = null;
        }

        _actionButtons = new HBoxContainer();
        _actionButtons.AddThemeConstantOverride("separation", 16);
        _actionButtons.Alignment = BoxContainer.AlignmentMode.Center;

        _rerollButton = CreateActionButton(
            $"Relancer ({_fragmentManager.RerollsRemaining})",
            _fragmentManager.RerollsRemaining > 0,
            OnRerollPressed);

        _banishButton = CreateActionButton(
            $"Bannir ({_fragmentManager.BanishesRemaining})",
            _fragmentManager.BanishesRemaining > 0,
            OnBanishPressed);

        _actionButtons.AddChild(_rerollButton);
        _actionButtons.AddChild(_banishButton);

        // Add to the inner VBox (parent of _cardsContainer)
        _cardsContainer.GetParent().AddChild(_actionButtons);
    }

    private Button CreateActionButton(string text, bool enabled, System.Action onPressed)
    {
        Button btn = new();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(140, 36);
        btn.Disabled = !enabled;

        StyleBoxFlat normalStyle = new();
        normalStyle.BgColor = enabled ? new Color(0.1f, 0.1f, 0.15f, 0.9f) : new Color(0.08f, 0.08f, 0.1f, 0.5f);
        normalStyle.SetBorderWidthAll(1);
        normalStyle.BorderColor = enabled ? GoldDim : new Color(0.3f, 0.3f, 0.3f, 0.3f);
        normalStyle.SetCornerRadiusAll(3);
        normalStyle.ContentMarginLeft = 12;
        normalStyle.ContentMarginRight = 12;
        normalStyle.ContentMarginTop = 6;
        normalStyle.ContentMarginBottom = 6;
        btn.AddThemeStyleboxOverride("normal", normalStyle);

        StyleBoxFlat hoverStyle = new();
        hoverStyle.BgColor = new Color(0.15f, 0.15f, 0.22f, 0.95f);
        hoverStyle.SetBorderWidthAll(1);
        hoverStyle.BorderColor = GoldColor;
        hoverStyle.SetCornerRadiusAll(3);
        hoverStyle.ContentMarginLeft = 12;
        hoverStyle.ContentMarginRight = 12;
        hoverStyle.ContentMarginTop = 6;
        hoverStyle.ContentMarginBottom = 6;
        btn.AddThemeStyleboxOverride("hover", hoverStyle);

        btn.AddThemeFontSizeOverride("font_size", 13);
        btn.AddThemeColorOverride("font_color", enabled ? TextColor : TextDim);

        btn.Pressed += () => onPressed?.Invoke();
        btn.ProcessMode = ProcessModeEnum.Always;

        return btn;
    }

    private void OnRerollPressed()
    {
        if (_banishMode)
        {
            _banishMode = false;
            UpdateBanishVisuals();
        }
        _fragmentManager?.Reroll();
    }

    private void OnBanishPressed()
    {
        if (_fragmentManager.BanishesRemaining <= 0)
            return;

        _banishMode = !_banishMode;
        UpdateBanishVisuals();
    }

    private void UpdateBanishVisuals()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            PanelContainer card = _cards[i];
            if (_banishMode)
            {
                StyleBoxFlat banishStyle = new();
                banishStyle.BgColor = BanishBgColor;
                banishStyle.SetBorderWidthAll(2);
                banishStyle.BorderColor = BanishBorderColor;
                banishStyle.SetCornerRadiusAll(3);
                card.AddThemeStyleboxOverride("panel", banishStyle);
            }
            else
            {
                string rarity = i < _cardRarities.Count ? _cardRarities[i] : "common";
                ApplyCardStyle(card, i == _hoveredCardIndex, rarity);
            }
        }

        if (_banishButton != null)
        {
            _banishButton.Text = _banishMode
                ? "Annuler"
                : $"Bannir ({_fragmentManager.BanishesRemaining})";
        }
    }

    private void OnFragmentSelected(string fragmentId, string fragmentType)
    {
        if (_banishMode)
        {
            _banishMode = false;
            _fragmentManager?.BanishFragment(fragmentId);
            return;
        }

        HideScreen();
        _fragmentManager?.SelectFragment(fragmentId, fragmentType);

        // Unpause seulement si aucun choix actif (la queue a été vidée)
        if (_fragmentManager == null || !_fragmentManager.IsChoiceActive)
            GetTree().Paused = false;
    }

    // ==============================
    // Perk Mode (mémorial, world perks)
    // ==============================

    private void OnPerkChoicesReady(string[] perkIds)
    {
        if (perkIds == null || perkIds.Length == 0)
            return;

        ClearCards();
        _title.Text = "MÉMORIAL";
        BuildPerkCards(perkIds);
        ShowScreen();
    }

    private void BuildPerkCards(string[] perkIds)
    {
        foreach (string perkId in perkIds)
        {
            PerkData data = PerkDataLoader.Get(perkId);
            if (data == null)
                continue;

            int currentStacks = _perkManager.GetStacks(perkId);
            Color rarityColor = GetRarityColor(data.Rarity);
            string rarityTag = GetRarityLabel(data.Rarity);

            string iconPath = !string.IsNullOrEmpty(data.Icon) ? data.Icon : null;
            string badge = $"{currentStacks}/{data.MaxStacks}";

            string capturedId = perkId;

            PanelContainer card = CreateChoiceCard(
                iconPath, rarityTag, rarityColor, data.Name, data.Description,
                badge, rarityColor,
                () => OnPerkSelected(capturedId),
                data.Rarity ?? "common");

            _cardsContainer.AddChild(card);
        }
    }

    private static Color GetRarityColor(string rarity)
    {
        return rarity switch
        {
            "common" => new Color(0.9f, 0.9f, 0.9f),
            "uncommon" => new Color(0.4f, 0.73f, 0.42f),
            "rare" => new Color(1f, 0.7f, 0f),
            _ => new Color(0.9f, 0.9f, 0.9f)
        };
    }

    private static string GetRarityLabel(string rarity)
    {
        return rarity switch
        {
            "common" => "Commun",
            "uncommon" => "Peu commun",
            "rare" => "Rare",
            _ => ""
        };
    }

    private void OnPerkSelected(string perkId)
    {
        _perkManager.SelectPerk(perkId);
        HideScreen();
        GetTree().Paused = false;
    }

    // ==============================
    // Synergy notification
    // ==============================

    private void CreateSynergyNotification()
    {
        _synergyNotification = new Label();
        _synergyNotification.HorizontalAlignment = HorizontalAlignment.Center;
        _synergyNotification.VerticalAlignment = VerticalAlignment.Center;
        _synergyNotification.AddThemeFontSizeOverride("font_size", 22);
        _synergyNotification.AddThemeColorOverride("font_color", GoldBright);
        _synergyNotification.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _synergyNotification.OffsetTop = 80;
        _synergyNotification.Visible = false;
        _synergyNotification.ProcessMode = ProcessModeEnum.Always;
        AddChild(_synergyNotification);
    }

    private void OnSynergyActivated(string synergyId, string notification)
    {
        _synergyNotification.Text = notification;
        _synergyNotification.Visible = true;
        _synergyNotification.Modulate = new Color(1f, 1f, 1f, 1f);

        Tween tween = CreateTween();
        tween.TweenInterval(2.0f);
        tween.TweenProperty(_synergyNotification, "modulate:a", 0f, 1.0f);
        tween.TweenCallback(Callable.From(() => _synergyNotification.Visible = false));
    }

    // ==============================
    // Common helpers
    // ==============================

    private void ClearCards()
    {
        foreach (Node child in _cardsContainer.GetChildren())
        {
            child.QueueFree();
        }
        _cards.Clear();
        _cardOptions.Clear();
        _cardRarities.Clear();
        _hoveredCardIndex = -1;
        _banishMode = false;

        if (_actionButtons != null && IsInstanceValid(_actionButtons))
        {
            _actionButtons.QueueFree();
            _actionButtons = null;
        }
    }

    private void ShowScreen()
    {
        _overlay.Visible = true;
        _rays.Visible = true;
        _rays.ResetAngle();
        _panel.Visible = true;
        Visible = true;
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.Always;

        // Play intro sound (uses UI pool that works during pause)
        Infrastructure.AudioManager.PlayUI("sfx_level_up");
        StartLoopAfterIntro();
    }

    private void StartLoopAfterIntro()
    {
        // Delay the loop start to let the intro play
        SceneTreeTimer timer = GetTree().CreateTimer(1.0, processAlways: true);
        timer.Timeout += () =>
        {
            if (Visible)
                Infrastructure.AudioManager.PlayLoop("sfx_level_up_loop", -4f);
        };
    }

    private void HideScreen()
    {
        _overlay.Visible = false;
        _rays.Visible = false;
        _panel.Visible = false;
        Visible = false;
        Infrastructure.AudioManager.StopLoop();
        Infrastructure.AudioManager.PlayUI("sfx_level_up_after");
    }

    private static StyleBoxTexture CreateNinePatch(Texture2D texture, int left, int top, int right, int bottom)
    {
        StyleBoxTexture sbt = new()
        {
            Texture = texture,
            RegionRect = new Rect2(0, 0, texture.GetWidth(), texture.GetHeight()),
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile
        };

        sbt.TextureMarginLeft = left;
        sbt.TextureMarginTop = top;
        sbt.TextureMarginRight = right;
        sbt.TextureMarginBottom = bottom;

        sbt.ContentMarginLeft = left + 2;
        sbt.ContentMarginTop = top + 2;
        sbt.ContentMarginRight = right + 2;
        sbt.ContentMarginBottom = bottom + 2;

        return sbt;
    }

    // ==============================
    // Light rays background effect
    // ==============================

    internal partial class LightRaysControl : Control
    {
        private readonly int _rayCount;
        private readonly float _speed;
        private readonly Color _colorA;
        private readonly Color _colorB;
        private float _angle;

        public LightRaysControl(int rayCount, float speed, Color colorA, Color colorB)
        {
            _rayCount = rayCount;
            _speed = speed;
            _colorA = colorA;
            _colorB = colorB;
        }

        public void ResetAngle()
        {
            _angle = 0f;
        }

        public override void _Process(double delta)
        {
            _angle += _speed * (float)delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 center = Size / 2f;
            float radius = center.Length() * 1.5f;
            float sliceAngle = Mathf.Tau / _rayCount;

            for (int i = 0; i < _rayCount; i++)
            {
                float startAngle = _angle + i * sliceAngle;
                float endAngle = startAngle + sliceAngle * 0.5f;

                Color rayColor = i % 2 == 0 ? _colorA : _colorB;

                Vector2 p1 = center + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
                Vector2 p2 = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius;

                // Subdivide the arc for smoother triangles
                float midAngle = (startAngle + endAngle) * 0.5f;
                Vector2 pMid = center + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * radius;

                DrawColoredPolygon(new Vector2[] { center, p1, pMid }, rayColor);
                DrawColoredPolygon(new Vector2[] { center, pMid, p2 }, rayColor);
            }
        }
    }
}
