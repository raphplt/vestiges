using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Écran du level-up (plan 17 lot 1B, forme Megabonk du plan 23 R2) : trois cartes au centre, l'inventaire à gauche
/// et les stats du joueur à droite. Une carte montre sa rareté en petit, son nom, son niveau à droite et une ligne de
/// gain en valeur (deux au plus) ; un palier atteint se signale par un badge. Souris, clavier et manette : haut/bas
/// entre les cartes et les actions, validation pour choisir.
/// </summary>
public partial class LevelUpScreen : CanvasLayer
{
    private const string MenusPath = "res://assets/ui/menus/";
    private const float CardWidth = ChoiceStyle.CardWidth;

    private static readonly Color GoldBright = ChoiceStyle.GoldBright;
    private static readonly Color TextLight = ChoiceStyle.TextLight;
    private static readonly Color TextColor = ChoiceStyle.TextColor;
    private static readonly Color TextDim = ChoiceStyle.TextDim;
    private static readonly Color NeutralBorder = ChoiceStyle.NeutralBorder;
    private static readonly Color OverlayColor = ChoiceStyle.InRunOverlayColor;
    private const float PanelEntranceScale = 0.82f;
    private const float InventoryWidth = 340f;
    private const float StatsWidth = 380f;
    private const float SideMinHeight = 0f;
    private Tween _entranceTween;
    private static readonly Color BanishColor = new(0.85f, 0.25f, 0.2f);

    private Texture2D _separatorTex;

    private ColorRect _overlay;
    private PixelBackdrop _rays;
    private HBoxContainer _layout;
    private PanelContainer _panel;
    private VBoxContainer _inventoryContainer;
    private VBoxContainer _statsContainer;
    // Colonnes latérales : sans contrôle focalisable, elles défilent au stick droit ou à Page haut/bas, comme la pause.
    private readonly List<ScrollContainer> _sideScrolls = new();
    private const float SideScrollSpeed = 900f;
    private VBoxContainer _cardsContainer;
    private ScrollContainer _choiceScroll;
    private ScrollContainer _activeSideScroll;
    private HBoxContainer _actionButtons;
    private Label _title;
    private Label _hint;
    private readonly List<PanelContainer> _cards = new();
    private readonly List<FragmentOption> _cardOptions = new();
    private readonly List<Color> _cardColors = new();
    private readonly List<Button> _buttons = new();
    private Button _banishButton;
    private int _focusIndex;
    private bool _banishMode;

    private FragmentManager _fragmentManager;
    private EventBus _eventBus;

    public override void _Ready()
    {
        _separatorTex = LoadTex(MenusPath + "ui_separator_simple.png");
        BuildUI();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.FragmentChoicesReady += OnFragmentChoicesReady;
        _eventBus.LevelUp += OnLevelUp;

        HideScreen();
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.FragmentChoicesReady -= OnFragmentChoicesReady;
        _eventBus.LevelUp -= OnLevelUp;
    }

    /// <summary>Une cascade ajoute ses niveaux après l'ouverture : le compteur se met à jour une fois la file remplie.</summary>
    private void OnLevelUp(int level)
    {
        if (Visible)
            Callable.From(RefreshTitle).CallDeferred();
    }

    private void RefreshTitle()
    {
        int queued = _fragmentManager?.QueuedLevels ?? 0;
        string title = Tr("LEVELUP_TITLE");
        if (_fragmentManager is { IsSpecializationChoice: true } && GetTree().GetFirstNodeInGroup("player") is Player player)
            title = string.Format(Tr("LEVELUP_TITLE_PERK"), player.Specializations.Count + 1, _fragmentManager.SpecializationCapacity);
        _title.Text = queued > 0 ? string.Format(Tr("LEVELUP_TITLE_QUEUED"), title, queued) : title;
    }

    public void SetFragmentManager(FragmentManager fragmentManager) => _fragmentManager = fragmentManager;

    private static Texture2D LoadTex(string path) => UITheme.LoadTex(path);

    // ==============================
    // Construction
    // ==============================

    private void BuildUI()
    {
        _overlay = new ColorRect { Color = OverlayColor, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_overlay);

        // Fond en gros pixels, rayons tramés et poussière d'oubli (plan 24 B1).
        _rays = new PixelBackdrop(PixelBackdrop.GoldTint);
        _rays.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_rays);

        // Inventaire à gauche, cartes au centre, stats à droite : on choisit en voyant ce qu'on a (plan 23 R2).
        _layout = new HBoxContainer { CustomMinimumSize = new Vector2(0, 850) };
        _layout.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _layout.GrowHorizontal = Control.GrowDirection.Both;
        _layout.GrowVertical = Control.GrowDirection.Both;
        _layout.AddThemeConstantOverride("separation", 18);
        AddChild(_layout);

        _inventoryContainer = BuildSidePanel(Tr("LEVELUP_INVENTORY"), InventoryWidth);
        _panel = new PanelContainer { CustomMinimumSize = new Vector2(CardWidth + 40f, 100) };
        StylePanel(_panel);
        _layout.AddChild(_panel);

        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(inner);

        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center, Text = Tr("LEVELUP_TITLE") };
        UITheme.SetTextRole(_title, TextRole.Title);
        _title.AddThemeColorOverride("font_color", GoldBright);
        inner.AddChild(_title);

        if (_separatorTex != null)
        {
            inner.AddChild(new TextureRect
            {
                Texture = _separatorTex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(0, 6),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            });
        }

        _cardsContainer = new VBoxContainer();
        _cardsContainer.AddThemeConstantOverride("separation", 8);
        _choiceScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        _cardsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _choiceScroll.AddChild(_cardsContainer);
        inner.AddChild(_choiceScroll);

        _hint = new Label { HorizontalAlignment = HorizontalAlignment.Center, Visible = false, Text = Tr("LEVELUP_BANISH_HINT") };
        UITheme.SetTextRole(_hint, TextRole.Caption);
        _hint.AddThemeColorOverride("font_color", BanishColor);
        inner.AddChild(_hint);

        _actionButtons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _actionButtons.AddThemeConstantOverride("separation", 14);
        inner.AddChild(_actionButtons);

        _statsContainer = BuildSidePanel(Tr("LEVELUP_STATS"), StatsWidth);
    }

    private void StylePanel(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
    }

    /// <summary>Colonne latérale : titre, puis un contenu qui défile s'il dépasse la hauteur des cartes.</summary>
    private VBoxContainer BuildSidePanel(string title, float width)
    {
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(width, 0) };
        StylePanel(panel);
        _layout.AddChild(panel);
        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 8);
        panel.AddChild(inner);
        Label label = new() { Text = title };
        UITheme.SetTextRole(label, TextRole.Section);
        label.AddThemeColorOverride("font_color", GoldBright);
        inner.AddChild(label);
        ScrollContainer scroll = new()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, SideMinHeight),
        };
        inner.AddChild(scroll);
        _sideScrolls.Add(scroll);
        _activeSideScroll ??= scroll;
        scroll.MouseEntered += () => _activeSideScroll = scroll;
        scroll.FocusMode = Control.FocusModeEnum.All;
        scroll.FocusEntered += () => _activeSideScroll = scroll;
        // La barre de défilement se pose sur le bord droit : la colonne des valeurs garde sa marge.
        MarginContainer gutter = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        gutter.AddThemeConstantOverride("margin_right", 14);
        scroll.AddChild(gutter);
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 4);
        gutter.AddChild(content);
        return content;
    }

    /// <summary>Inventaire et stats du moment : reconstruits à chaque offre, un choix précédent a pu les changer.</summary>
    private void RefreshSidePanels(Player player)
    {
        MenuSkin.Clear(_inventoryContainer);
        MenuSkin.Clear(_statsContainer);
        if (player == null)
            return;
        PlayerSheet.AddCompactInventory(_inventoryContainer, player, _fragmentManager?.SpecializationCapacity ?? 0);
        PlayerSheet.AddStatLines(_statsContainer, player, GetNodeOrNull<EssenceTracker>("/root/Main/EssenceTracker"),
            GetNodeOrNull<PerilManager>("/root/Main/PerilManager"), withOublis: false, TextRole.Small);
    }

    // ==============================
    // Cartes
    // ==============================

    private void OnFragmentChoicesReady(int count)
    {
        if (count <= 0 || _fragmentManager == null || _fragmentManager.PendingChoices.Count == 0)
            return;

        ClearCards();
        Player player = GetTree().GetFirstNodeInGroup("player") as Player;
        RefreshSidePanels(player);
        int bestRank = -1;
        foreach (FragmentOption choice in _fragmentManager.PendingChoices)
        {
            _cardOptions.Add(choice);
            BuildCard(choice, player);
            bestRank = Mathf.Max(bestRank, choice.Rarity?.Rank ?? -1);
        }
        BuildActionButtons();
        SetFocus(0);
        RefreshTitle();

        // Choix suivant d'une même réserve : l'écran reste ouvert, seules les cartes changent (plan 20 §6.7).
        if (!Visible)
            ShowScreen();
        if (bestRank >= UpgradeRoller.Get("rare").Rank)
            AudioManager.PlayUI("sfx_rare_fragment");
    }

    private void BuildCard(FragmentOption choice, Player player)
    {
        bool isAscension = choice.Type == FragmentOption.AscensionType;
        bool isWeapon = isAscension || choice.Type is "weapon_new" or "weapon_upgrade" or FragmentOption.SurplusType;
        bool isPerk = choice.Type == PerkSpecializationOffers.OptionType;
        bool isNew = isPerk || isAscension || choice.Type is "weapon_new" or "passive_new";
        Color frame = choice.Rarity != null ? RarityPalette.Main(choice.Rarity.Id)
            : isAscension ? ChoiceStyle.GoldBright : isPerk ? ChoiceStyle.PerkBorder : NeutralBorder;

        PanelContainer card = new() { CustomMinimumSize = new Vector2(CardWidth, 172), MouseFilter = Control.MouseFilterEnum.Stop };
        int index = _cards.Count;
        card.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                Activate(index);
                card.AcceptEvent();
            }
        };
        card.MouseEntered += () => SetFocus(index);

        MarginContainer margin = new();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", side is "left" or "right" ? 12 : 8);
        card.AddChild(margin);
        VBoxContainer body = new();
        body.AddThemeConstantOverride("separation", 8);
        margin.AddChild(body);

        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 18);
        body.AddChild(row);
        Texture2D icon = LoadIcon(choice, isWeapon);
        PanelContainer iconFrame = new() { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        iconFrame.AddThemeStyleboxOverride("panel", MenuSkin.Choice(frame, false, choice.Rarity?.Rank ?? -1));
        iconFrame.AddChild(new TextureRect
        {
            Texture = icon,
            CustomMinimumSize = new Vector2(96, 96),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        row.AddChild(iconFrame);
        VBoxContainer text = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        text.AddThemeConstantOverride("separation", 6);
        row.AddChild(text);

        // La rareté occupe son bandeau ; le niveau appartient à la ligne du nom.
        HBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 8);
        text.AddChild(header);
        string tag = choice.Rarity != null
            ? RarityPalette.DisplayName(choice.Rarity.Id).ToUpper()
            : isPerk ? PerkTag(choice.Id)
            : isAscension ? Tr("LEVELUP_ASCENSION")
            : Tr(isWeapon ? "LEVELUP_KIND_WEAPON"
                : PassiveSouvenirDataLoader.Get(choice.Id)?.IsBadge == true ? "LEVELUP_KIND_BADGE" : "LEVELUP_KIND_OBJECT");
        if (choice.IsCarried)
            tag = $"{tag}  ·  {Tr("LEVELUP_CARRIED")}";
        Label tagLabel = MakeLabel(tag, TextRole.Small, frame, true);
        RarityIcon rarityIcon = choice.Rarity != null ? new RarityIcon(choice.Rarity.Rank) : null;
        if (rarityIcon != null)
            header.AddChild(rarityIcon);
        header.AddChild(tagLabel);
        if (choice.RarityRaised && !choice.IsCarried)
            ShowRarityRaise(header, tagLabel, rarityIcon, choice, tag, frame, _cards.Count);

        HBoxContainer title = new();
        title.AddThemeConstantOverride("separation", 10);
        text.AddChild(title);
        Color titleColor = choice.Rarity != null ? RarityPalette.Colors(choice.Rarity.Id).Light : TextLight;
        Label name = MakeLabel(choice.DisplayName, TextRole.Section, titleColor, true);
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        title.AddChild(name);
        if (UpgradeText.ReachesMilestone(choice, player))
            title.AddChild(MilestoneBadge());
        Label level = MakeLabel(LevelText(choice, player, isWeapon, isAscension, isPerk, isNew), TextRole.Body,
            GoldBright, false);
        level.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        title.AddChild(level);
        int lineIndex = 0;
        foreach ((string line, Color color) in UpgradeText.Describe(choice, player))
        {
            text.AddChild(ChoiceStyle.MakeGainLabel(line, color, lineIndex++ == 0 ? TextRole.Subhead : TextRole.Small));
        }

        _cards.Add(card);
        _cardColors.Add(frame);
        _cardsContainer.AddChild(card);
        StyleCard(index, false);
    }

    /// <summary>« Niv 3 → 4 » pour une amélioration, « NOUVEAU » pour une arme ou un objet, « DÉFINITIVE » pour une voie.</summary>
    private string LevelText(FragmentOption choice, Player player, bool isWeapon, bool isAscension, bool isPerk, bool isNew)
    {
        if (isAscension)
            return Tr("LEVELUP_ASCENSION_FINAL");
        if (isPerk)
            return "";
        if (isNew)
            return Tr("LEVELUP_NEW");
        if (choice.Type == FragmentOption.SurplusType)
            return Tr("LEVELUP_SURPLUS");
        int level = isWeapon ? player?.GetWeaponFragmentLevel(choice.Id) ?? 0 : player?.GetPassiveLevel(choice.Id) ?? 0;
        return string.Format(Tr("LEVELUP_LEVEL"), level, level + 1);
    }

    /// <summary>Badge doré « Palier ! » : la carte fait atteindre à l'objet un effet qui change la manière de jouer.</summary>
    private Control MilestoneBadge()
    {
        PanelContainer badge = new() { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        StyleBoxFlat style = new() { BgColor = GoldBright with { A = 0.18f }, BorderColor = GoldBright };
        style.SetBorderWidthAll(1);

        style.ContentMarginLeft = 6;
        style.ContentMarginRight = 6;
        badge.AddThemeStyleboxOverride("panel", style);
        badge.AddChild(MakeLabel(Tr("LEVELUP_MILESTONE_BADGE"), TextRole.Caption, GoldBright, false));
        return badge;
    }

    private string PerkTag(string id)
    {
        string family = PerkSpecializationDataLoader.Get(id)?.Family ?? "";
        return $"{Tr("LEVELUP_NEW_PERK")}  ·  {Tr($"PERK_FAMILY_{family.ToUpperInvariant()}")}";
    }

    private static Texture2D LoadIcon(FragmentOption choice, bool isWeapon)
    {
        string path = choice.Type == PerkSpecializationOffers.OptionType
            ? PerkSpecializationDataLoader.Get(choice.Id)?.Icon
            : isWeapon
            ? WeaponDataLoader.Get(choice.Id)?.Sprite
            : PassiveSouvenirDataLoader.Get(choice.Id)?.Icon;
        if (string.IsNullOrEmpty(path))
            return null;
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }

    private static Label MakeLabel(string text, TextRole role, Color color, bool expand, HorizontalAlignment align = HorizontalAlignment.Left) =>
        ChoiceStyle.MakeLabel(text, role, color, expand, align);

    private void StyleCard(int index, bool focused) =>
        ChoiceStyle.StyleCard(_cards[index], _banishMode ? BanishColor : _cardColors[index], _cardOptions[index].Rarity?.Rank ?? -1, focused);

    // ==============================
    // Actions et navigation
    // ==============================

    private void BuildActionButtons()
    {
        _buttons.Add(CreateActionButton(string.Format(Tr("LEVELUP_REROLL"), _fragmentManager.RerollsRemaining),
            _fragmentManager.RerollsRemaining > 0, () =>
            {
                _fragmentManager.Reroll();
                CloseIfDone();
            }));
        _banishButton = CreateActionButton(BanishLabel(), true, ToggleBanish);
        _buttons.Add(_banishButton);
        _buttons.Add(CreateActionButton(Tr("LEVELUP_SKIP"), true, Skip));
        foreach (Button button in _buttons)
            _actionButtons.AddChild(button);
    }

    private Button CreateActionButton(string text, bool enabled, System.Action onPressed)
    {
        Button button = new()
        {
            Text = text,
            Disabled = !enabled,
            CustomMinimumSize = new Vector2(190, 54),
            FocusMode = Control.FocusModeEnum.None,
            ProcessMode = ProcessModeEnum.Always,
        };
        UITheme.SetTextRole(button, TextRole.Subhead);
        button.AddThemeColorOverride("font_color", enabled ? TextColor : TextDim);
        StyleButton(button, false);
        UITheme.WireButtonAudio(button);
        button.Pressed += () => onPressed();
        int index = _cards.Count + _buttons.Count;
        button.MouseEntered += () => SetFocus(index);
        return button;
    }

    private static void StyleButton(Button button, bool focused) => ChoiceStyle.StyleButton(button, focused);

    public override void _Process(double delta)
    {
        if (!Visible)
            return;
        float axis = Input.GetAxis("scroll_up", "scroll_down");
        if (axis == 0f)
            return;
        int step = Mathf.RoundToInt(axis * SideScrollSpeed * (float)delta);
        if (_activeSideScroll != null)
            _activeSideScroll.ScrollVertical += step;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || _cards.Count == 0)
            return;

        int total = _cards.Count + _buttons.Count;
        if (@event.IsActionPressed("ui_cancel") && _banishMode)
        {
            ToggleBanish();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed("ui_down") || @event.IsActionPressed("ui_right") && _focusIndex >= _cards.Count)
            SetFocus((_focusIndex + 1) % total);
        else if (@event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_left") && _focusIndex >= _cards.Count)
            SetFocus((_focusIndex - 1 + total) % total);
        else if (@event.IsActionPressed("ui_accept"))
            Activate(_focusIndex);
        else
            return;
        GetViewport().SetInputAsHandled();
    }

    private void SetFocus(int index)
    {
        _focusIndex = index;
        if (index < _cards.Count)
            _choiceScroll.EnsureControlVisible(_cards[index]);
        for (int i = 0; i < _cards.Count; i++)
            StyleCard(i, i == index);
        for (int i = 0; i < _buttons.Count; i++)
            StyleButton(_buttons[i], _cards.Count + i == index);
    }

    private void Activate(int index)
    {
        if (index < _cards.Count)
        {
            OnCardChosen(_cardOptions[index]);
            return;
        }
        Button button = _buttons[index - _cards.Count];
        if (!button.Disabled)
            button.EmitSignal(BaseButton.SignalName.Pressed);
    }

    private void ToggleBanish()
    {
        _banishMode = !_banishMode;
        _hint.Visible = _banishMode;
        _banishButton.Text = _banishMode
            ? Tr("LEVELUP_BANISH_CANCEL")
            : BanishLabel();
        SetFocus(_banishMode ? 0 : _focusIndex);
    }

    /// <summary>Gratuits restants, puis le Péril que coûtera le prochain bannissement, en fractions (⅓, ⅔, 1⅓…).</summary>
    private string BanishLabel() => _fragmentManager.BanishesRemaining > 0
        ? string.Format(Tr("LEVELUP_BANISH"), _fragmentManager.BanishesRemaining)
        : string.Format(Tr("LEVELUP_BANISH_PERIL"), Fraction(_fragmentManager.NextBanishPerilFractions, PerilDataLoader.BanishPerilDivisor));

    private static string Fraction(int numerator, int denominator)
    {
        int whole = numerator / denominator;
        int rest = numerator % denominator;
        string part = rest == 0 ? "" : denominator == 3 ? (rest == 1 ? "⅓" : "⅔") : $"{rest}/{denominator}";
        return whole == 0 ? part : $"{whole}{part}";
    }

    private void OnCardChosen(FragmentOption option)
    {
        if (_banishMode)
        {
            AudioManager.PlayUI("sfx_perk_refuse", 0f);
            // Une voie d'ascension ne s'oublie pas : on la choisit, ou on passe.
            if (option.Type == FragmentOption.AscensionType)
                return;
            _fragmentManager?.BanishFragment(option.Id);
            CloseIfDone();
            return;
        }

        _fragmentManager?.SelectFragment(option);
        CloseIfDone();
    }

    private void Skip()
    {
        _fragmentManager?.SkipChoice();
        CloseIfDone();
    }

    /// <summary>L'écran ne se ferme, et le jeu ne reprend, que lorsque la file des niveaux est vide.</summary>
    private void CloseIfDone()
    {
        if (_fragmentManager != null && _fragmentManager.IsChoiceActive)
            return;
        HideScreen();
        GetTree().Paused = false;
    }

    // ==============================
    // Affichage
    // ==============================

    private void ClearCards()
    {
        MenuSkin.Clear(_cardsContainer);
        MenuSkin.Clear(_actionButtons);
        _choiceScroll.ScrollVertical = 0;
        _cards.Clear();
        _cardOptions.Clear();
        _cardColors.Clear();
        _buttons.Clear();
        _banishMode = false;
        _hint.Visible = false;
    }

    private void ShowScreen()
    {
        _overlay.Visible = true;
        _rays.Visible = true;
        _rays.SetTint(_cardOptions.Count > 0 && _cardOptions[0].Type == PerkSpecializationOffers.OptionType
            ? PixelBackdrop.NeutralTint : PixelBackdrop.GoldTint);
        _rays.FadeIn(0.3f);
        _layout.Visible = true;
        Visible = true;
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.Always;

        AudioManager.PlayUI("sfx_level_up");
        SceneTreeTimer timer = GetTree().CreateTimer(1.0, processAlways: true);
        timer.Timeout += () =>
        {
            if (Visible)
                AudioManager.PlayLoop("sfx_level_up_loop", -4f);
        };
        PlayEntrance();
    }

    /// <summary>
    /// Entrée avec du punch (plan 02 J4) : le voile tombe en 0,12 s, le panneau jaillit de 82 % avec un léger
    /// dépassement. Les cartes sont cliquables dès la première frame : le délai jusqu'au choix ne change pas.
    /// </summary>
    private void PlayEntrance()
    {
        _entranceTween?.Kill();
        _overlay.Color = new Color(OverlayColor, 0f);
        SetPanelScale(PanelEntranceScale);
        _entranceTween = CreateTween();
        _entranceTween.SetPauseMode(Tween.TweenPauseMode.Process);
        _entranceTween.SetParallel();
        _entranceTween.TweenProperty(_overlay, "color", OverlayColor, 0.12f);
        _entranceTween.TweenMethod(Callable.From<float>(SetPanelScale), PanelEntranceScale, 1f, 0.22f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// La Chance (ou l'oubli, le Péril) a monté la rareté : la carte montre d'abord la rareté tirée, puis elle saute au
    /// rang gagné avec un trèfle et un son (plan 24 D2). Les cartes d'une offre sautent l'une après l'autre.
    /// </summary>
    private void ShowRarityRaise(HBoxContainer header, Label tagLabel, RarityIcon rarityIcon, FragmentOption choice, string finalTag, Color finalColor, int cardIndex)
    {
        UpgradeRarity rolled = choice.RolledRarity;
        tagLabel.Text = RarityPalette.DisplayName(rolled.Id).ToUpper();
        tagLabel.AddThemeColorOverride("font_color", RarityPalette.Main(rolled.Id));
        float delay = 0.35f + 0.15f * cardIndex;
        rarityIcon.RevealFrom(rolled.Rank, choice.Rarity.Rank, delay);
        TextureRect clover = new()
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/rarities/rarity_clover.png"),
            CustomMinimumSize = new Vector2(16, 16), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        clover.Modulate = Colors.Transparent;
        header.AddChild(clover);
        header.MoveChild(clover, tagLabel.GetIndex() + 1);

        Tween tween = tagLabel.CreateTween();
        tween.TweenInterval(delay + (choice.Rarity.Rank - rolled.Rank) * 6f / RarityArt.Fps);
        tween.TweenCallback(Callable.From(() =>
        {
            tagLabel.Text = finalTag;
            tagLabel.AddThemeColorOverride("font_color", finalColor);
            tagLabel.PivotOffset = tagLabel.Size / 2f;
            tagLabel.Scale = new Vector2(1.5f, 1.5f);
            clover.Modulate = Colors.White;
            AudioManager.PlayUI("sfx_rare_fragment", 0.08f, -6f);
        }));
        tween.TweenProperty(tagLabel, "scale", Vector2.One, 0.25f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    private void SetPanelScale(float scale)
    {
        // Les colonnes viennent d'être remplies et leur mise en page est différée : la taille minimale, calculée sur
        // demande, donne déjà le bon centre dès la première frame.
        _layout.PivotOffset = _layout.GetCombinedMinimumSize() / 2f;
        _layout.Scale = Vector2.One * scale;
    }

    private void HideScreen()
    {
        _entranceTween?.Kill();
        _layout.Scale = Vector2.One;
        _overlay.Color = OverlayColor;
        _overlay.Visible = false;
        _rays.Visible = false;
        _layout.Visible = false;
        Visible = false;
        AudioManager.StopLoop();
        AudioManager.PlayUI("sfx_level_up_after");
    }
}
