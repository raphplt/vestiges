using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Écran du level-up (plan 17, lot 1B) : trois Fragments de mémoire. Une amélioration montre sa rareté (couleur
/// et symbole) et ce qui change, « avant → après » sur les valeurs effectives ; une nouveauté montre ce qu'elle fait,
/// en une phrase. Souris, clavier et manette : haut/bas entre les cartes et les actions, validation pour choisir.
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
    private static readonly Color OverlayColor = ChoiceStyle.OverlayColor;
    private const float PanelEntranceScale = 0.82f;
    private Tween _entranceTween;
    private static readonly Color BanishColor = new(0.85f, 0.25f, 0.2f);

    private const int RayCount = 14;
    private const float RaySpeed = 0.15f;
    private static readonly Color RayColorA = new(0.83f, 0.66f, 0.26f, 0.08f);
    private static readonly Color RayColorB = new(0.9f, 0.78f, 0.39f, 0.04f);

    private Texture2D _panelTex;
    private Texture2D _separatorTex;

    private ColorRect _overlay;
    private LightRaysControl _rays;
    private PanelContainer _panel;
    private VBoxContainer _cardsContainer;
    private HBoxContainer _actionButtons;
    private Label _title;
    private Label _hint;
    private Label _synergyNotification;
    private readonly List<PanelContainer> _cards = new();
    private readonly List<FragmentOption> _cardOptions = new();
    private readonly List<Color> _cardColors = new();
    private readonly List<Button> _buttons = new();
    private Button _banishButton;
    private int _focusIndex;
    private bool _banishMode;

    private PerkManager _perkManager;
    private FragmentManager _fragmentManager;
    private EventBus _eventBus;

    public override void _Ready()
    {
        _panelTex = LoadTex(MenusPath + "ui_panel_frame.png");
        _separatorTex = LoadTex(MenusPath + "ui_separator_simple.png");
        BuildUI();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.FragmentChoicesReady += OnFragmentChoicesReady;
        _eventBus.LevelUp += OnLevelUp;

        CreateSynergyNotification();
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

    public void SetPerkManager(PerkManager perkManager)
    {
        _perkManager = perkManager;
        _perkManager.SynergyActivated += OnSynergyActivated;
    }

    public void SetFragmentManager(FragmentManager fragmentManager) => _fragmentManager = fragmentManager;

    private static Texture2D LoadTex(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    // ==============================
    // Construction
    // ==============================

    private void BuildUI()
    {
        _overlay = new ColorRect { Color = OverlayColor, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_overlay);

        _rays = new LightRaysControl(RayCount, RaySpeed, RayColorA, RayColorB)
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ProcessMode = ProcessModeEnum.Always,
        };
        _rays.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_rays);

        _panel = new PanelContainer { CustomMinimumSize = new Vector2(CardWidth + 40f, 100) };
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.GrowHorizontal = Control.GrowDirection.Both;
        _panel.GrowVertical = Control.GrowDirection.Both;
        if (_panelTex != null)
        {
            StyleBoxTexture panelStyle = UITheme.CreateNinePatch(_panelTex, 6, 6, 6, 6);
            panelStyle.ContentMarginLeft = 20;
            panelStyle.ContentMarginRight = 20;
            panelStyle.ContentMarginTop = 16;
            panelStyle.ContentMarginBottom = 18;
            _panel.AddThemeStyleboxOverride("panel", panelStyle);
        }
        AddChild(_panel);

        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(inner);

        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center, Text = Tr("LEVELUP_TITLE") };
        UITheme.SetTextRole(_title, TextRole.Subhead);
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
        inner.AddChild(_cardsContainer);

        _hint = new Label { HorizontalAlignment = HorizontalAlignment.Center, Visible = false, Text = Tr("LEVELUP_BANISH_HINT") };
        UITheme.SetTextRole(_hint, TextRole.Caption);
        _hint.AddThemeColorOverride("font_color", BanishColor);
        inner.AddChild(_hint);

        _actionButtons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _actionButtons.AddThemeConstantOverride("separation", 14);
        inner.AddChild(_actionButtons);
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
        bool isWeapon = choice.Type is "weapon_new" or "weapon_upgrade";
        bool isPerk = choice.Type == PerkSpecializationOffers.OptionType;
        bool isNew = isPerk || choice.Type is "weapon_new" or "passive_new";
        Color frame = choice.Rarity != null ? RarityPalette.Main(choice.Rarity.Id) : isPerk ? ChoiceStyle.PerkBorder : NeutralBorder;

        PanelContainer card = new() { CustomMinimumSize = new Vector2(CardWidth, 76), MouseFilter = Control.MouseFilterEnum.Stop };
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
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 14);
        margin.AddChild(row);

        Texture2D icon = isPerk ? null : LoadIcon(choice, isWeapon);
        if (icon != null)
        {
            row.AddChild(new TextureRect
            {
                Texture = icon,
                // Icônes 32×32 à échelle entière (×2).
                CustomMinimumSize = new Vector2(64, 64),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            });
        }

        VBoxContainer text = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 2);
        row.AddChild(text);

        // Bandeau : rareté (symbole et nom), « nouvelle arme / nouveau passif » ou famille du perk, puis le niveau à droite.
        HBoxContainer header = new();
        text.AddChild(header);
        string tag = choice.Rarity != null
            ? $"{ChoiceStyle.RarityGlyph(choice.Rarity.Rank)} {RarityPalette.DisplayName(choice.Rarity.Id).ToUpper()}".Trim()
            : isPerk ? PerkTag(choice.Id)
            : Tr(isWeapon ? "LEVELUP_NEW_WEAPON" : "LEVELUP_NEW_PASSIVE");
        if (choice.IsCarried)
            tag = $"{tag}  ·  {Tr("LEVELUP_CARRIED")}";
        header.AddChild(MakeLabel(tag, TextRole.Caption, frame, true));
        if (!isNew)
        {
            int level = isWeapon ? player?.GetWeaponFragmentLevel(choice.Id) ?? 0 : player?.GetPassiveLevel(choice.Id) ?? 0;
            int next = isWeapon ? level + 1 : Mathf.Min(level + choice.PassiveLevels, PassiveSouvenirDataLoader.Get(choice.Id)?.MaxLevel ?? level + 1);
            header.AddChild(MakeLabel(string.Format(Tr("LEVELUP_LEVEL"), level, next), TextRole.Caption, TextColor, false, HorizontalAlignment.Right));
        }

        text.AddChild(MakeLabel(choice.DisplayName, TextRole.Body, TextLight, false));
        foreach ((string line, Color color) in UpgradeText.Describe(choice, player))
            text.AddChild(MakeLabel(line, TextRole.Small, color, false));

        _cards.Add(card);
        _cardColors.Add(frame);
        _cardsContainer.AddChild(card);
        StyleCard(index, false);
    }

    private string PerkTag(string id)
    {
        string family = PerkSpecializationDataLoader.Get(id)?.Family ?? "";
        return $"{Tr("LEVELUP_NEW_PERK")}  ·  {Tr($"PERK_FAMILY_{family.ToUpperInvariant()}")}";
    }

    private static Texture2D LoadIcon(FragmentOption choice, bool isWeapon)
    {
        string path = isWeapon
            ? WeaponDataLoader.Get(choice.Id)?.Sprite
            : PerkIconResolver.GetPassiveStatIconPath(PassiveSouvenirDataLoader.Get(choice.Id)?.Stat);
        if (string.IsNullOrEmpty(path))
            return null;
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }

    private static Label MakeLabel(string text, TextRole role, Color color, bool expand, HorizontalAlignment align = HorizontalAlignment.Left) =>
        ChoiceStyle.MakeLabel(text, role, color, expand, align);

    private void StyleCard(int index, bool focused) =>
        ChoiceStyle.StyleCard(_cards[index], _banishMode ? BanishColor : _cardColors[index], _cardOptions[index].Rarity?.Rank ?? 0, focused);

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
            CustomMinimumSize = new Vector2(150, 34),
            FocusMode = Control.FocusModeEnum.None,
            ProcessMode = ProcessModeEnum.Always,
        };
        UITheme.SetTextRole(button, TextRole.Small);
        button.AddThemeColorOverride("font_color", enabled ? TextColor : TextDim);
        StyleButton(button, false);
        UITheme.WireButtonAudio(button);
        button.Pressed += () => onPressed();
        int index = _cards.Count + _buttons.Count;
        button.MouseEntered += () => SetFocus(index);
        return button;
    }

    private static void StyleButton(Button button, bool focused) => ChoiceStyle.StyleButton(button, focused);

    public override void _Input(InputEvent @event)
    {
        if (!Visible || _cards.Count == 0)
            return;

        int total = _cards.Count + _buttons.Count;
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
    // Synergies
    // ==============================

    private void CreateSynergyNotification()
    {
        _synergyNotification = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
            ProcessMode = ProcessModeEnum.Always,
            OffsetTop = 80,
        };
        UITheme.SetTextRole(_synergyNotification, TextRole.Heading);
        _synergyNotification.AddThemeColorOverride("font_color", GoldBright);
        _synergyNotification.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        AddChild(_synergyNotification);
    }

    private void OnSynergyActivated(string synergyId, string notification)
    {
        _synergyNotification.Text = notification;
        _synergyNotification.Visible = true;
        _synergyNotification.Modulate = Colors.White;

        Tween tween = CreateTween();
        tween.TweenInterval(2.0f);
        tween.TweenProperty(_synergyNotification, "modulate:a", 0f, 1.0f);
        tween.TweenCallback(Callable.From(() => _synergyNotification.Visible = false));
    }

    // ==============================
    // Affichage
    // ==============================

    private void ClearCards()
    {
        foreach (Node child in _cardsContainer.GetChildren())
            child.QueueFree();
        foreach (Node child in _actionButtons.GetChildren())
            child.QueueFree();
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
        _rays.ResetAngle();
        _panel.Visible = true;
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

    private void SetPanelScale(float scale)
    {
        // Le panneau vient d'être rempli et sa mise en page est différée : sa taille minimale, calculée sur demande,
        // donne déjà le bon centre dès la première frame.
        _panel.PivotOffset = _panel.GetCombinedMinimumSize() / 2f;
        _panel.Scale = Vector2.One * scale;
    }

    private void HideScreen()
    {
        _entranceTween?.Kill();
        _panel.Scale = Vector2.One;
        _overlay.Color = OverlayColor;
        _overlay.Visible = false;
        _rays.Visible = false;
        _panel.Visible = false;
        Visible = false;
        AudioManager.StopLoop();
        AudioManager.PlayUI("sfx_level_up_after");
    }

    // ==============================
    // Rayons de lumière (aussi utilisés par l'écran de butin)
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

        public void ResetAngle() => _angle = 0f;

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
                float midAngle = (startAngle + endAngle) * 0.5f;
                Color rayColor = i % 2 == 0 ? _colorA : _colorB;

                Vector2 p1 = center + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
                Vector2 p2 = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius;
                Vector2 pMid = center + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * radius;
                DrawColoredPolygon(new[] { center, p1, pMid }, rayColor);
                DrawColoredPolygon(new[] { center, pMid, p2 }, rayColor);
            }
        }
    }
}
