using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Écran du level-up au fil (plan 04 §11, DECISIONS §182) : les fragments pendent à un fil tendu entre deux clous,
/// l'inventaire est une ceinture en bas de l'écran, une étiquette sous le fragment survolé dit ce qui change.
/// Relancer, Bannir et Passer sont des étiquettes pendues aux clous ; toutes les stats restent à la touche du journal.
/// Souris, clavier et manette : gauche/droite entre fragments et étiquettes, bas vers les actions, haut vers les
/// fragments, validation pour choisir.
/// </summary>
public partial class LevelUpScreen : CanvasLayer
{
    private const float NailInset = 150f;
    private const float ThreadTop = 186f;
    private const float ThreadSag = 34f;
    private const float CardSpacing = PaperFragment.Width + 90f;
    // Largeur laissée de chaque côté aux étiquettes des clous : au-delà de trois fragments, ils se chevauchent.
    private const float ActionZone = 270f;
    private const float TightGap = 44f;
    private const float StageDrop = -720f;
    private const float MaxTilt = 2.6f;
    private static readonly float[] Jitter = { 0.7f, -0.9f, 0.4f, -0.5f, 1f };
    private static readonly Color OverlayColor = new(0.03f, 0.025f, 0.05f, 0.45f);
    private static readonly Color BanishColor = new(0.85f, 0.25f, 0.2f);
    private const float StatsScrollSpeed = 900f;
    private const float ScreenMargin = 24f;
    private const float TagGap = 26f;
    private const string RerollAction = "levelup_reroll";
    private const string BanishAction = "levelup_banish";
    private const string SkipAction = "levelup_skip";
    private const string KeyLabelName = "Key";

    private ColorRect _overlay;
    private PixelBackdrop _veil;
    private Control _stage;
    private ClothesLine _line;
    private Control _cardLayer;
    private Control _pegLayer;
    private Control _actionLayer;
    private Control _fallLayer;
    private PanelContainer _changeTag;
    private Label _changeLabel;
    private TextureRect _changeEyelet;
    private InventoryBelt _belt;
    private Label _title;
    private Label _hint;
    private Button _statsButton;
    private ColorRect _statsShade;
    private PanelContainer _statsPanel;
    private ScrollContainer _statsScroll;
    private VBoxContainer _statsContainer;
    private Tween _entranceTween;

    private readonly List<PaperFragment> _cards = new();
    private readonly List<FragmentOption> _cardOptions = new();
    private readonly List<Button> _buttons = new();
    private readonly List<Vector2> _strings = new();
    private Button _banishButton;
    private int _focusIndex;
    private int _lastCardFocus;
    private bool _banishMode;
    private int _banishedIndex = -1;
    private int _offerSeed;

    private FragmentManager _fragmentManager;
    private EventBus _eventBus;

    public override void _Ready()
    {
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

    // ==============================
    // Construction
    // ==============================

    private void BuildUI()
    {
        _overlay = new ColorRect { Color = OverlayColor, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_overlay);

        // Le jeu se devine derrière, assombri, avec la poussière d'oubli (plan 24 B1, plan 37 R1).
        _veil = new PixelBackdrop(PixelBackdrop.NeutralTint);
        _veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_veil);

        _stage = FullRect(this);
        _line = new ClothesLine();
        _stage.AddChild(_line);
        _line.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        // Les fragments qui tombent passent derrière ceux qu'on vient d'accrocher.
        _fallLayer = FullRect(_stage);
        _cardLayer = FullRect(_stage);
        // Le fragment survolé passe devant ses voisins (ZIndex 1) : pinces et étiquettes restent devant lui.
        _pegLayer = FullRect(_stage);
        _pegLayer.ZIndex = 2;
        _actionLayer = FullRect(_stage);
        _actionLayer.ZIndex = 2;
        // Au clou de gauche, toutes les stats : le pendant des actions, au clou de droite.
        _statsButton = MakeTag("journal", "", PaperArt.Ink, () => SetStatsVisible(!_statsPanel.Visible));

        _changeTag = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false, ZIndex = 2 };
        StyleBoxTexture tagStyle = PaperArt.TagStyle(PaperArt.Kraft);
        tagStyle.ContentMarginLeft = tagStyle.ContentMarginRight = 24;
        tagStyle.ContentMarginTop = 22;
        tagStyle.ContentMarginBottom = 10;
        _changeTag.AddThemeStyleboxOverride("panel", tagStyle);
        _changeLabel = UITheme.MakeLabel("", TextRole.Small, PaperArt.Ink, TextWeight.Regular, HorizontalAlignment.Center, outline: 0);
        _changeTag.AddChild(_changeLabel);
        _stage.AddChild(_changeTag);
        _changeEyelet = Eyelet(_stage);
        _changeEyelet.ZIndex = 2;

        _belt = new InventoryBelt();
        AddChild(_belt);
        _belt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _belt.GrowHorizontal = Control.GrowDirection.Both;
        _belt.GrowVertical = Control.GrowDirection.Begin;
        _belt.OffsetBottom = -22f;
        _belt.Resized += () =>
        {
            if (Visible && _cards.Count > 0)
                UpdateChangeTag();
        };

        _title = UITheme.MakeLabel(Tr("LEVELUP_TITLE"), TextRole.Title, UITheme.GoldBright, TextWeight.Regular, HorizontalAlignment.Center);
        AddChild(_title);
        _title.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _title.GrowHorizontal = Control.GrowDirection.Both;
        _title.OffsetTop = 28f;

        _hint = UITheme.MakeLabel(Tr("LEVELUP_BANISH_HINT"), TextRole.Small, BanishColor, TextWeight.Regular, HorizontalAlignment.Center);
        _hint.Visible = false;
        AddChild(_hint);
        _hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _hint.GrowHorizontal = Control.GrowDirection.Both;
        _hint.OffsetTop = 96f;


        BuildStatsPanel();
    }

    /// <summary>Toutes les stats, en surimpression à la touche du journal : la ceinture ne dit que ce que l'on porte.</summary>
    private void BuildStatsPanel()
    {
        // Au-dessus du fragment survolé, qui passe devant ses voisins ; le voile retient les clics sur les fragments.
        _statsShade = new ColorRect { Color = new Color(0f, 0f, 0f, 0.45f), Visible = false, ZIndex = 5,
            MouseFilter = Control.MouseFilterEnum.Stop };
        _statsShade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _statsShade.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true })
                SetStatsVisible(false);
        };
        AddChild(_statsShade);
        _statsPanel = new PanelContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Stop, ZIndex = 5 };
        _statsPanel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        AddChild(_statsPanel);
        _statsPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        MenuSkin.CenterPanel(_statsPanel, 620f, 820f);
        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 8);
        _statsPanel.AddChild(inner);
        inner.AddChild(UITheme.MakeLabel(Tr("LEVELUP_STATS"), TextRole.Section, UITheme.GoldBright));
        _statsScroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        inner.AddChild(_statsScroll);
        MarginContainer gutter = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        gutter.AddThemeConstantOverride("margin_right", 14);
        _statsScroll.AddChild(gutter);
        _statsContainer = new VBoxContainer();
        _statsContainer.AddThemeConstantOverride("separation", 4);
        gutter.AddChild(_statsContainer);
    }

    private static Control FullRect(Node parent)
    {
        Control layer = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(layer);
        layer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return layer;
    }

    private static TextureRect Eyelet(Control parent)
    {
        TextureRect eyelet = new()
        {
            Texture = PaperArt.Eyelet(),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        parent.AddChild(eyelet);
        return eyelet;
    }

    // ==============================
    // Offre
    // ==============================

    private void OnFragmentChoicesReady(int count)
    {
        if (count <= 0 || _fragmentManager == null || _fragmentManager.PendingChoices.Count == 0)
            return;

        bool replacing = Visible && _cards.Count > 0;
        DropCards(replacing);
        ClearCards();
        Player player = GetTree().GetFirstNodeInGroup("player") as Player;
        _belt.Build(player, _fragmentManager.SpecializationCapacity);
        RefreshStats(player);
        _offerSeed++;
        foreach (FragmentOption choice in _fragmentManager.PendingChoices)
            BuildCard(choice, player);
        LayoutStage();
        BuildActionButtons();
        _lastCardFocus = 0;
        SetFocus(0);
        RefreshTitle();

        // Choix suivant d'une même réserve : l'écran reste ouvert, de nouveaux fragments sont accrochés (plan 20 §6.7).
        if (!Visible)
            ShowScreen();
        else
            SwingCards(0f);
        int bestRank = -1;
        foreach (FragmentOption choice in _cardOptions)
            bestRank = Mathf.Max(bestRank, choice.Rarity?.Rank ?? -1);
        if (bestRank >= UpgradeRoller.Get("rare").Rank)
            AudioManager.PlayUI("sfx_rare_fragment");
    }

    private void BuildCard(FragmentOption choice, Player player)
    {
        int index = _cards.Count;
        PaperFragment card = new();
        // Les fragments des bords penchent vers l'extérieur, pour que deux voisins s'écartent par le bas ; serrés
        // au-delà de trois, ils penchent moitié moins.
        int count = _fragmentManager.PendingChoices.Count;
        float side = count <= 1 ? 0f : (index - (count - 1) / 2f) / ((count - 1) / 2f);
        float tilt = count > 3 ? 0.5f : 1f;
        float angle = (-side * MaxTilt + Jitter[(index + _offerSeed) % Jitter.Length]) * tilt;
        card.Setup(choice, player, angle, _offerSeed * 7 + index);
        card.Pressed += () => Activate(index);
        card.Hovered += () => SetFocus(index);
        // Le texte replié change la hauteur du papier après coup : l'étiquette suit le bas du fragment survolé.
        card.Resized += () =>
        {
            if (_focusIndex == index)
                UpdateChangeTag();
        };
        _cardLayer.AddChild(card);
        _cards.Add(card);
        _cardOptions.Add(choice);
        if (choice.RarityRaised && !choice.IsCarried)
            card.PlayRarityRaise(0.35f + 0.15f * index);
    }

    /// <summary>Clous aux bords, fragments répartis au milieu du fil, une pince par fragment.</summary>
    private void LayoutStage()
    {
        Vector2 view = _stage.GetViewportRect().Size;
        _line.Stretch(NailInset, view.X - NailInset, ThreadTop, ThreadSag);
        MenuSkin.Clear(_pegLayer);
        int count = _cards.Count;
        // Au-delà de trois (Trèfle plastifié), les fragments rapetissent pour tenir entre les étiquettes des clous.
        float available = view.X - ActionZone * 2f;
        float scale = Mathf.Min(1f, (available - (count - 1) * TightGap) / (count * PaperFragment.Width));
        float spacing = Mathf.Min(CardSpacing, PaperFragment.Width * scale + TightGap);
        Texture2D pegTexture = PaperArt.Peg();
        for (int i = 0; i < count; i++)
        {
            float x = view.X / 2f + (i - (count - 1) / 2f) * spacing;
            float y = _line.ThreadY(x);
            _cards[i].Scale = Vector2.One * scale;
            _cards[i].HangAt(new Vector2(x, y + 6f));
            TextureRect peg = new()
            {
                Texture = pegTexture,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Position = new Vector2(x - pegTexture.GetWidth() / 2f, y - 26f),
            };
            _pegLayer.AddChild(peg);
        }
    }

    // ==============================
    // Actions et navigation
    // ==============================

    private void BuildActionButtons()
    {
        _buttons.Add(CreateActionButton(RerollAction, string.Format(Tr("LEVELUP_REROLL"), _fragmentManager.RerollsRemaining),
            _fragmentManager.RerollsRemaining > 0, PaperArt.Ink, () =>
            {
                _fragmentManager.Reroll();
                CloseIfDone();
            }));
        _banishButton = CreateActionButton(BanishAction, BanishLabel(), true, PaperArt.InkRed, ToggleBanish);
        _buttons.Add(_banishButton);
        _buttons.Add(CreateActionButton(SkipAction, Tr("LEVELUP_SKIP"), true, PaperArt.Ink, Skip));
        Callable.From(LayoutActions).CallDeferred();
    }

    /// <summary>
    /// Les trois actions pendent l'une sous l'autre au clou de droite, dans l'ordre de la navigation ; toutes les stats
    /// au clou de gauche. Une étiquette trop large pour le bord de l'écran s'en écarte, sa ficelle part en biais.
    /// </summary>
    private void LayoutActions()
    {
        if (_buttons.Count < 3)
            return;
        float viewWidth = _stage.GetViewportRect().Size.X;
        float top = _line.Top + 64f;
        _strings.Clear();
        float columnWidth = 0f;
        foreach (Button button in _buttons)
            columnWidth = Mathf.Max(columnWidth, button.GetCombinedMinimumSize().X);
        float columnX = Mathf.Min(_line.Right, viewWidth - columnWidth / 2f - ScreenMargin);
        Vector2 from = new(_line.Right, _line.Top);
        float y = top;
        for (int i = 0; i < _buttons.Count; i++)
        {
            Button button = _buttons[i];
            HangButton(button, columnX, y, i % 2 == 0 ? 1.5f : -1.5f);
            _strings.Add(from);
            _strings.Add(new Vector2(columnX, y + 10f));
            from = new Vector2(columnX, y + button.Size.Y - 6f);
            y += button.Size.Y + TagGap;
        }
        float statsX = Mathf.Max(_line.Left, _statsButton.GetCombinedMinimumSize().X / 2f + ScreenMargin);
        HangButton(_statsButton, statsX, top, 2f);
        _strings.Add(new Vector2(_line.Left, _line.Top));
        _strings.Add(new Vector2(statsX, top + 10f));
        UpdateChangeTag();
    }

    private static void HangButton(Button button, float x, float top, float degrees)
    {
        button.Size = button.GetCombinedMinimumSize();
        button.Position = new Vector2(x - button.Size.X / 2f, top);
        button.PivotOffset = new Vector2(button.Size.X / 2f, 0f);
        button.Rotation = Mathf.DegToRad(degrees);
        PlaceTagParts(button);
    }

    private Button CreateActionButton(string action, string text, bool enabled, Color ink, System.Action onPressed)
    {
        Button button = MakeTag(action, text, ink, onPressed);
        button.Disabled = !enabled;
        button.AddThemeColorOverride("font_color", enabled ? ink : PaperArt.InkDim);
        StyleButton(button, false);
        int index = _cards.Count + _buttons.Count;
        button.MouseEntered += () => SetFocus(index);
        return button;
    }

    /// <summary>
    /// Étiquette d'action pendue : œillet en haut, touche du raccourci en petit au-dessus du libellé. La touche suit le
    /// clavier ou la manette branchée.
    /// </summary>
    private Button MakeTag(string action, string text, Color ink, System.Action onPressed)
    {
        Button button = new()
        {
            Text = text,
            FocusMode = Control.FocusModeEnum.None,
            ProcessMode = ProcessModeEnum.Always,
        };
        UITheme.SetTextRole(button, TextRole.Body);
        button.AddThemeColorOverride("font_color", ink);
        button.AddThemeColorOverride("font_hover_color", ink);
        button.AddThemeColorOverride("font_pressed_color", ink);
        button.AddThemeColorOverride("font_disabled_color", new Color(PaperArt.InkDim, 0.6f));
        button.AddThemeConstantOverride("outline_size", 0);
        StyleButton(button, false);
        UITheme.WireButtonAudio(button);
        button.Pressed += () => onPressed();
        _actionLayer.AddChild(button);
        Eyelet(button).Position = new Vector2(0f, 6f);
        // Touche tracée à l'encre sous l'œillet, comme une touche de clavier dessinée sur l'étiquette.
        StyleBoxFlat cap = new() { DrawCenter = false, BorderColor = new Color(PaperArt.Ink, 0.75f) };
        cap.SetBorderWidthAll(2);
        cap.BorderWidthBottom = 4;
        cap.ContentMarginLeft = cap.ContentMarginRight = 8;
        cap.ContentMarginBottom = 2;
        PanelContainer keyCap = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        keyCap.AddThemeStyleboxOverride("panel", cap);
        Label key = UITheme.MakeLabel(KeyName(action), TextRole.Small, PaperArt.Ink, TextWeight.Regular, HorizontalAlignment.Center, outline: 0);
        key.Name = KeyLabelName;
        keyCap.AddChild(key);
        button.AddChild(keyCap);
        button.Resized += () => PlaceTagParts(button);
        keyCap.Resized += () => PlaceTagParts(button);
        return button;
    }

    /// <summary>Œillet et touche au milieu du haut de l'étiquette : ses deux premiers enfants.</summary>
    private static void PlaceTagParts(Button button)
    {
        if (button.GetChildCount() < 2 || button.GetChild(0) is not TextureRect eyelet || button.GetChild(1) is not Control keyCap)
            return;
        eyelet.Position = new Vector2((button.Size.X - eyelet.Size.X) / 2f, 6f);
        keyCap.Size = keyCap.GetCombinedMinimumSize();
        keyCap.Position = new Vector2((button.Size.X - keyCap.Size.X) / 2f, 24f);
    }

    private static string KeyName(string action) => InputRemapManager.Instance is { GamepadConnected: true }
        ? InputRemapManager.GetJoyButtonName(action)
        : InputRemapManager.GetKeyName(action);


    /// <summary>Étiquette pâle au repos ; survolée, elle prend le kraft et le liseré, comme le fragment survolé.</summary>
    private static void StyleButton(Button button, bool focused)
    {
        Color tint = button.Disabled ? PaperArt.PaleTag.Darkened(0.25f) : focused ? PaperArt.Kraft.Lightened(0.15f) : PaperArt.PaleTag;
        StyleBoxTexture style = TagButtonStyle(tint);
        foreach (string state in new[] { "normal", "pressed", "disabled", "focus" })
            button.AddThemeStyleboxOverride(state, style);
        button.AddThemeStyleboxOverride("hover", button.Disabled ? style : TagButtonStyle(PaperArt.Kraft.Lightened(0.15f)));
    }

    /// <summary>Le défilement au stick ou à Page haut/bas ne tourne que panneau des stats ouvert.</summary>
    private void SetStatsVisible(bool visible)
    {
        _statsPanel.Visible = visible;
        _statsShade.Visible = visible;
        SetProcess(visible);
    }

    private static StyleBoxTexture TagButtonStyle(Color tint)
    {
        StyleBoxTexture style = PaperArt.TagStyle(tint);
        style.ContentMarginLeft = style.ContentMarginRight = 26;
        style.ContentMarginTop = 70;
        style.ContentMarginBottom = 10;
        return style;
    }

    public override void _Process(double delta)
    {
        float axis = Input.GetAxis("scroll_up", "scroll_down");
        if (axis != 0f)
            _statsScroll.ScrollVertical += Mathf.RoundToInt(axis * StatsScrollSpeed * (float)delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || _cards.Count == 0)
            return;

        if (@event.IsActionPressed("journal") || _statsPanel.Visible && @event.IsActionPressed("ui_cancel"))
        {
            SetStatsVisible(!_statsPanel.Visible);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_statsPanel.Visible)
            return;
        if (@event.IsActionPressed("ui_cancel") && _banishMode)
        {
            ToggleBanish();
            GetViewport().SetInputAsHandled();
            return;
        }
        int total = _cards.Count + _buttons.Count;
        bool onCard = _focusIndex < _cards.Count;
        if (@event.IsActionPressed(RerollAction))
            Activate(_cards.Count);
        else if (@event.IsActionPressed(BanishAction))
            Activate(_cards.Count + 1);
        else if (@event.IsActionPressed(SkipAction))
            Activate(_cards.Count + 2);
        else if (@event.IsActionPressed("ui_right"))
            SetFocus((_focusIndex + 1) % total);
        else if (@event.IsActionPressed("ui_left"))
            SetFocus((_focusIndex - 1 + total) % total);
        else if (@event.IsActionPressed("ui_down"))
            SetFocus(onCard ? _cards.Count : Mathf.Min(_focusIndex + 1, total - 1));
        else if (@event.IsActionPressed("ui_up"))
            SetFocus(onCard ? _focusIndex : _focusIndex == _cards.Count ? _lastCardFocus : _focusIndex - 1);
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
            _lastCardFocus = index;
        for (int i = 0; i < _cards.Count; i++)
            _cards[i].SetFocused(i == index, _banishMode);
        for (int i = 0; i < _buttons.Count; i++)
            StyleButton(_buttons[i], _cards.Count + i == index);
        FragmentOption focused = index < _cards.Count ? _cardOptions[index] : null;
        _belt.Highlight(focused, focused != null ? _cards[index].Icon : null);
        UpdateChangeTag();
    }

    /// <summary>L'étiquette de ce qui change pend sous le fragment survolé, sans descendre sur la ceinture.</summary>
    private void UpdateChangeTag()
    {
        string change = _focusIndex < _cards.Count ? ChangeText(_cardOptions[_focusIndex]) : "";
        _changeTag.Visible = change.Length > 0;
        _changeEyelet.Visible = _changeTag.Visible;
        List<Vector2> strings = new(_strings);
        if (_changeTag.Visible)
        {
            _changeLabel.Text = change;
            _changeTag.Size = _changeTag.GetCombinedMinimumSize();
            PaperFragment card = _cards[_focusIndex];
            Vector2 bottom = card.BottomCenter;
            // Repère de la scène au repos : pendant l'entrée, la scène descend mais pas la ceinture.
            float beltTop = _belt.Position.Y;
            float top = Mathf.Min(bottom.Y + 24f, beltTop - _changeTag.Size.Y - 10f);
            _changeTag.Position = new Vector2(bottom.X - _changeTag.Size.X / 2f, top);
            _changeTag.PivotOffset = new Vector2(_changeTag.Size.X / 2f, 0f);
            _changeTag.Rotation = Mathf.DegToRad(1.5f);
            _changeEyelet.Position = new Vector2(bottom.X - _changeEyelet.Size.X / 2f, top + 6f);
            if (top > bottom.Y)
            {
                strings.Add(new Vector2(bottom.X, bottom.Y - 4f));
                strings.Add(new Vector2(bottom.X, top + 10f));
            }
        }
        _line.SetStrings(strings);
    }

    /// <summary>
    /// Ce qui change, dit sur l'étiquette : l'avant → après d'une amélioration, la valeur actuelle de la stat qu'un
    /// nouvel objet touche, l'emplacement que prend une arme ou une Réminiscence.
    /// </summary>
    private string ChangeText(FragmentOption option)
    {
        Player player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player == null)
            return "";
        UpgradeParts parts = UpgradeText.Parts(option, player);
        if (parts.Before.Length > 0)
            return $"{parts.Label}   {parts.Before} → {parts.After}";
        switch (option.Type)
        {
            case "weapon_new":
                return string.Format(Tr("LEVELUP_CHANGE_WEAPON_SLOT"), player.WeaponSlots.Count + 1, player.WeaponSlotLimit);
            case PerkSpecializationOffers.OptionType:
                return string.Format(Tr("LEVELUP_CHANGE_REMINISCENCE"), player.Specializations.Count + 1, _fragmentManager.SpecializationCapacity);
            case "passive_new":
            {
                PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(option.Id);
                if (data == null || data.Effects.Count == 0)
                    return "";
                string stat = data.Effects[0].Stat;
                foreach (PlayerSheet.StatLine line in PlayerSheet.StatLines(player))
                    if (line.Id == stat)
                        return string.Format(Tr("LEVELUP_CHANGE_NOW"), line.Label, line.Value);
                return "";
            }
            default:
                return "";
        }
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
        _banishButton.Text = _banishMode ? Tr("LEVELUP_BANISH_CANCEL") : BanishLabel();
        SetFocus(_banishMode ? 0 : _focusIndex);
        Callable.From(LayoutActions).CallDeferred();
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
            _banishedIndex = _cardOptions.IndexOf(option);
            _fragmentManager?.BanishFragment(option.Id);
            _banishedIndex = -1;
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

    /// <summary>
    /// Une nouvelle offre arrive écran ouvert (relance, bannissement, niveau suivant) : les anciens fragments quittent
    /// leur pince et tombent derrière les nouveaux ; le fragment banni, oublié, s'éteint en tombant.
    /// </summary>
    private void DropCards(bool animate)
    {
        if (!animate)
            return;
        for (int i = 0; i < _cards.Count; i++)
            _cards[i].Fall(_fallLayer, i % 2 == 0 ? 1f : -1f, i == _banishedIndex);
    }

    private void ClearCards()
    {
        foreach (PaperFragment card in _cards)
            if (card.GetParent() == _cardLayer)
                card.QueueFree();
        foreach (Button button in _buttons)
            button.QueueFree();
        _cards.Clear();
        _cardOptions.Clear();
        _buttons.Clear();
        _banishMode = false;
        _hint.Visible = false;
    }

    private void RefreshStats(Player player)
    {
        MenuSkin.Clear(_statsContainer);
        if (player == null)
            return;
        PlayerSheet.AddStatLines(_statsContainer, player, GetNodeOrNull<EssenceTracker>("/root/Main/EssenceTracker"),
            GetNodeOrNull<PerilManager>("/root/Main/PerilManager"), withOublis: false, TextRole.Small);
        _statsButton.Text = Tr("LEVELUP_STATS_TAG");
        _statsButton.FindChild(KeyLabelName, true, false).Set("text", KeyName("journal"));
    }

    private void ShowScreen()
    {
        _overlay.Visible = true;
        _veil.Visible = true;
        _veil.SetTint(_cardOptions.Count > 0 && _cardOptions[0].Type == PerkSpecializationOffers.OptionType
            ? PixelBackdrop.NeutralTint : PixelBackdrop.GoldTint);
        _veil.FadeIn(0.3f);
        SetStatsVisible(false);
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
    /// Le fil descend d'en haut avec ses fragments et se pose avec un léger rebond, puis les fragments se balancent.
    /// Ils sont cliquables dès la première image : le délai jusqu'au choix ne change pas (plan 02 J4).
    /// </summary>
    private void PlayEntrance()
    {
        _entranceTween?.Kill();
        _overlay.Color = new Color(OverlayColor, 0f);
        _stage.Position = new Vector2(0f, StageDrop);
        _belt.Modulate = new Color(1f, 1f, 1f, 0f);
        _entranceTween = CreateTween();
        _entranceTween.SetPauseMode(Tween.TweenPauseMode.Process);
        _entranceTween.SetParallel();
        _entranceTween.TweenProperty(_overlay, "color", OverlayColor, 0.12f);
        _entranceTween.TweenProperty(_stage, "position:y", 0f, 0.32f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _entranceTween.TweenProperty(_belt, "modulate:a", 1f, 0.2f);
        SwingCards(0.2f);
    }

    private void SwingCards(float delay)
    {
        for (int i = 0; i < _cards.Count; i++)
            _cards[i].Swing(delay + 0.05f * i, i % 2 == 0 ? 7f : -6f);
    }

    private void HideScreen()
    {
        _entranceTween?.Kill();
        _stage.Position = Vector2.Zero;
        _belt.Modulate = Colors.White;
        _overlay.Color = OverlayColor;
        _overlay.Visible = false;
        _veil.Visible = false;
        SetStatsVisible(false);
        MenuSkin.Clear(_fallLayer);
        Visible = false;
        AudioManager.StopLoop();
        AudioManager.PlayUI("sfx_level_up_after");
    }
}
