using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Écran roulette du butin d'un coffre : chaque case fait défiler des leurres puis s'arrête sur le butin
/// déjà résolu (objets du sac). Met le jeu en pause, joue le son d'ouverture.
/// Un appui révèle d'un coup les cases encore en défilement, un second ferme l'écran sans attendre.
/// Couleurs de rareté : palette unique (RarityPalette).
/// </summary>
public partial class ChestLootScreen : CanvasLayer
{

    // --- Colors ---
    private static readonly Color GoldBright = new(0.9f, 0.78f, 0.39f);
    private static readonly Color TextLight = new(0.92f, 0.9f, 0.85f);
    private static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);
    private static readonly Color OverlayColor = ChoiceStyle.InRunOverlayColor;

    // --- Roulette config ---
    private const float RouletteMinInterval = 0.04f;
    private const float RouletteMaxInterval = 0.35f;
    private const float RouletteDuration = 2.5f;
    private const string RevealSound = "sfx_chest_reveal";
    private const float RevealFadeSeconds = 0.5f;
    // Le clic d'ouverture frappe dans ses 0,3 premières secondes : la mélodie entre juste après, sur le défilement.
    private const float RevealMelodyDelay = 0.3f;
    // Largeur resserrée sur le contenu (retour du 11 octobre) : une ligne d'objet tient en 600 px, au-delà le nom
    // flottait dans un rectangle vide.
    private const float PanelWidth = 720f;
    private const float CardWidth = 610f;
    private const float PanelBaseHeight = 300f;
    private const float SlotHeight = 104f;
    private const int IconSize = 52;

    // --- UI nodes ---
    private ColorRect _overlay;
    private PixelBackdrop _rays;
    private PanelContainer _panel;
    private VBoxContainer _slotsContainer;
    private Label _title;
    private TextureRect _chestArt;
    private Button _continueButton;
    private ScrollContainer _scroll;
    private readonly Texture2D[] _revealFrames = new Texture2D[4];

    // --- State ---
    private string _rarity;
    private Action _onComplete;
    private readonly List<SlotState> _slots = new();
    private bool _isRevealing;
    private AudioStreamPlayer _revealAudio;
    private int _openingSerial;
    private int _slotsRevealed;

    // Leurres de la roulette : ce que le coffre aurait pu donner.
    private LootDisplayInfo[] _fakeItems;

    private struct LootDisplayInfo
    {
        public string Text;
        public Color Color;
        public Texture2D Icon;
        public LootDisplayInfo(string text, Color color, Texture2D icon) { Text = text; Color = color; Icon = icon; }
    }

    private class SlotState
    {
        public Label Label;
        public PanelContainer Card;
        public ColorRect Flash;
        public TextureRect Icon;
        public LootDisplayInfo FinalItem;
        public float Timer;
        public float CurrentInterval;
        public float Elapsed;
        public float StopTime;
        public bool Stopped;
        public int FakeIndex;
    }

    public override void _Ready()
    {
        LoadTextures();
        BuildUI();
        HideScreen();
    }

    private void LoadTextures()
    {
        for (int i = 0; i < _revealFrames.Length; i++)
            _revealFrames[i] = GD.Load<Texture2D>($"res://assets/ui/hud/plan25/xp_burst_{i:00}.png");
    }

    private void BuildUI()
    {
        _overlay = new ColorRect { Color = OverlayColor, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_overlay);
        _rays = new PixelBackdrop(PixelBackdrop.GoldTint);
        _rays.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_rays);

        _panel = new PanelContainer { CustomMinimumSize = new Vector2(PanelWidth, 870) };
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.GrowHorizontal = _panel.GrowVertical = Control.GrowDirection.Both;
        _panel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        AddChild(_panel);
        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 18);
        _panel.AddChild(inner);
        HBoxContainer heading = new();
        heading.AddThemeConstantOverride("separation", 28);
        inner.AddChild(heading);
        _chestArt = new TextureRect { CustomMinimumSize = new Vector2(144, 144),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest, MouseFilter = Control.MouseFilterEnum.Ignore };
        heading.AddChild(_chestArt);
        VBoxContainer titles = new() { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        heading.AddChild(titles);
        _title = UITheme.MakeLabel("", TextRole.Title, GoldBright, TextWeight.Strong);
        titles.AddChild(_title);
        titles.AddChild(UITheme.MakeLabel("Butin de l’expédition", TextRole.Subhead, UITheme.TextColor));

        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        inner.AddChild(_scroll);
        _slotsContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _slotsContainer.AddThemeConstantOverride("separation", 12);
        _scroll.AddChild(_slotsContainer);
        _continueButton = MenuSkin.Button("Révéler le butin", true);
        _continueButton.FocusMode = Control.FocusModeEnum.None;
        _continueButton.ActionMode = BaseButton.ActionModeEnum.Press;
        _continueButton.Pressed += Advance;
        inner.AddChild(_continueButton);
    }

    // ==============================
    // Public API
    // ==============================

    /// <summary>
    /// Montre le chest roulette. Callback onComplete appelé quand l'animation est terminée.
    /// </summary>
    public void ShowLoot(List<ResolvedLoot> loots, string rarity, Action onComplete)
    {
        // Seulement des objets accessibles : la roulette ne promet plus d'autres catégories de butin.
        List<LootDisplayInfo> candidates = new();
        foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
            if (!item.IsBadge && MetaSaveManager.IsObjectUnlocked(item.Id))
                candidates.Add(new LootDisplayInfo(item.Name, new Color("6ACA5A"), LootIconResolver.Get("object", item.Id)));
        if (candidates.Count == 0)
            foreach (ResolvedLoot loot in loots)
                candidates.Add(new LootDisplayInfo(loot.Label, loot.Color, LootIconResolver.Get(loot.Type, loot.ItemId)));
        _fakeItems = candidates.ToArray();
        _openingSerial++;
        AudioManager.FadeOutUI(_revealAudio, RevealSound, RevealFadeSeconds);
        _revealAudio = null;
        _rarity = rarity;
        _onComplete = onComplete;
        _slotsRevealed = 0;
        _isRevealing = loots.Count > 0;
        _continueButton.Text = _isRevealing ? "Révéler le butin" : "Reprendre la route";
        string artRarity = rarity is "rare" or "epic" or "lore" ? rarity : "common";
        _chestArt.Texture = GD.Load<Texture2D>($"res://assets/chests/chest_{artRarity}_open.png");
        _scroll.ScrollVertical = 0;

        ClearSlots();

        _title.Text = Tr(rarity switch
        {
            "epic" => "CHEST_EPIC",
            "rare" => "CHEST_RARE",
            "lore" => "CHEST_LORE",
            _ => "CHEST_COMMON"
        });
        _title.AddThemeColorOverride("font_color", RarityPalette.Main(rarity));

        // Create a slot for each loot
        for (int i = 0; i < loots.Count; i++)
        {
            SlotState slot = CreateSlot(loots[i], i);
            _slots.Add(slot);
        }

        MenuSkin.CenterPanel(_panel, PanelWidth, Mathf.Min(870f, PanelBaseHeight + loots.Count * SlotHeight));
        ShowScreen();

        // Le clic d'ouverture d'abord, puis la mélodie qui accompagne le défilement (retour du 28 septembre).
        AudioManager.PlayUI("sfx_chest_opening", 0f);
        int opening = ++_openingSerial;
        GetTree().CreateTimer(RevealMelodyDelay, processAlways: true).Timeout += () =>
        {
            if (opening == _openingSerial && _isRevealing)
                _revealAudio = AudioManager.PlayUI(RevealSound, 0f);
        };
    }

    // ==============================
    // Slot creation
    // ==============================

    private SlotState CreateSlot(ResolvedLoot loot, int index)
    {
        PanelContainer card = new();
        card.CustomMinimumSize = new Vector2(CardWidth, 84);

        ChoiceStyle.StyleCard(card, ChoiceStyle.NeutralBorder, -1, false);

        // Flash overlay (hidden initially)
        ColorRect flash = new();
        flash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        flash.Color = new Color(1f, 1f, 1f, 0f);
        flash.MouseFilter = Control.MouseFilterEnum.Ignore;

        // Inner layout
        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        card.AddChild(margin);

        Label label = new();
        label.HorizontalAlignment = HorizontalAlignment.Left;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        UITheme.SetTextRole(label, TextRole.Lead);
        label.AddThemeColorOverride("font_color", TextDim);
        label.Text = "???";
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(CardWidth - 170f, 0);
        HBoxContainer contents = new() { Alignment = BoxContainer.AlignmentMode.Center };
        contents.AddThemeConstantOverride("separation", 8);
        margin.AddChild(contents);
        int fakeIndex = GD.RandRange(0, _fakeItems.Length - 1);
        LootDisplayInfo initial = _fakeItems[fakeIndex];
        TextureRect icon = (TextureRect)PlayerSheet.MakeIcon(null, IconSize);
        icon.Texture = initial.Icon;
        contents.AddChild(icon);
        label.Text = initial.Text;
        contents.AddChild(label);
        // L'éclat reste un badge secondaire : il ne remplace jamais le signe du gain.
        int rarityRank = RarityArt.Rank(_rarity);
        if (rarityRank >= 0)
            contents.AddChild(new RarityIcon(rarityRank, 24));

        card.AddChild(flash);
        _slotsContainer.AddChild(card);

        LootDisplayInfo finalItem = new(loot.Label, loot.Color, LootIconResolver.Get(loot.Type, loot.ItemId));

        return new SlotState
        {
            Label = label,
            Card = card,
            Flash = flash,
            Icon = icon,
            FinalItem = finalItem,
            Timer = 0f,
            CurrentInterval = RouletteMinInterval,
            Elapsed = 0f,
            StopTime = RouletteDuration + index * 0.4f,
            Stopped = false,
            FakeIndex = fakeIndex
        };
    }

    // ==============================
    // Process — animate roulette
    // ==============================

    public override void _Process(double delta)
    {
        if (!_isRevealing || _slots.Count == 0)
            return;

        float dt = (float)delta;
        bool allStopped = true;

        foreach (SlotState slot in _slots)
        {
            if (slot.Stopped)
                continue;

            allStopped = false;
            slot.Elapsed += dt;
            slot.Timer += dt;

            // Easing: interval increases as we approach stop time
            float progress = Mathf.Clamp(slot.Elapsed / slot.StopTime, 0f, 1f);
            // Ease-in-out cubic for the slowdown
            float easedProgress = progress * progress * (3f - 2f * progress);
            slot.CurrentInterval = Mathf.Lerp(RouletteMinInterval, RouletteMaxInterval, easedProgress);

            if (slot.Timer >= slot.CurrentInterval)
            {
                slot.Timer = 0f;
                // Cycle to next fake item
                slot.FakeIndex = (slot.FakeIndex + 1) % _fakeItems.Length;
                LootDisplayInfo fakeItem = _fakeItems[slot.FakeIndex];
                slot.Icon.Texture = fakeItem.Icon;
                slot.Label.Text = fakeItem.Text;
                slot.Label.AddThemeColorOverride("font_color", new Color(fakeItem.Color, 0.6f));
            }

            // Time to stop
            if (slot.Elapsed >= slot.StopTime)
            {
                RevealSlot(slot);
            }
        }

        if (allStopped && _isRevealing)
        {
            _isRevealing = false;
            FinishReveal();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || !@event.IsPressed() || @event.IsEcho())
            return;
        if (@event.IsActionPressed("ui_down") || @event.IsActionPressed("ui_up"))
        {
            _scroll.ScrollVertical += @event.IsActionPressed("ui_down") ? 110 : -110;
            GetViewport().SetInputAsHandled();
            return;
        }
        // Cliquer le bouton passe par son signal ; cliquer le décor ne referme pas la lecture.
        if (!@event.IsActionPressed("ui_accept") && !@event.IsActionPressed("ui_cancel")
            && !@event.IsActionPressed("interact"))
            return;
        GetViewport().SetInputAsHandled();
        Advance();
    }

    private void Advance()
    {
        if (!Visible) return;
        if (_isRevealing) RevealRemaining();
        else Close();
    }

    private void RevealRemaining()
    {
        _isRevealing = false;
        bool playSound = true;
        foreach (SlotState slot in _slots)
        {
            if (slot.Stopped)
                continue;
            // Un seul son pour toutes les cases révélées ensemble : empilés, ils saturent.
            RevealSlot(slot, playSound);
            playSound = false;
        }
        FinishReveal();
    }

    private void RevealSlot(SlotState slot, bool playSound = true)
    {
        slot.Stopped = true;
        slot.Icon.Texture = slot.FinalItem.Icon;
        _slotsRevealed++;

        // Set final item
        slot.Label.Text = slot.FinalItem.Text;
        slot.Label.AddThemeColorOverride("font_color", TextLight);
        UITheme.SetTextRole(slot.Label, TextRole.Subhead);

        // Card glow border
        ChoiceStyle.StyleCard(slot.Card, RarityPalette.Main(_rarity), RarityArt.Rank(_rarity), false);

        // White flash
        slot.Flash.Color = new Color(1f, 1f, 1f, 0.5f);
        Tween flashTween = CreateTween();
        flashTween.SetProcessMode(Tween.TweenProcessMode.Physics);
        flashTween.TweenProperty(slot.Flash, "color:a", 0f, 0.3f)
            .SetTrans(Tween.TransitionType.Expo);

        // Scale bounce on card
        slot.Card.PivotOffset = slot.Card.Size / 2f;
        Tween scaleTween = CreateTween();
        scaleTween.SetProcessMode(Tween.TweenProcessMode.Physics);
        scaleTween.TweenProperty(slot.Card, "scale", Vector2.One * 1.12f, 0.08f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        scaleTween.TweenProperty(slot.Card, "scale", Vector2.One * 0.95f, 0.1f);
        scaleTween.TweenProperty(slot.Card, "scale", Vector2.One, 0.08f);

        // Color pulse on the label
        Tween colorTween = CreateTween();
        colorTween.SetProcessMode(Tween.TweenProcessMode.Physics);
        Color finalColor = slot.FinalItem.Color;
        slot.Label.AddThemeColorOverride("font_color", Colors.White);
        colorTween.TweenProperty(slot.Label, "theme_override_colors/font_color", finalColor, 0.4f);

        // Spawn particles around the card
        SpawnRevealParticles(slot.Card);

        if (playSound)
            AudioManager.PlayUI("sfx_perk_choix", 0.05f);

        // Screen shake on last reveal
        if (_slotsRevealed >= _slots.Count)
            Combat.ScreenShake.Instance?.ShakeMedium();
    }

    private void SpawnRevealParticles(PanelContainer card)
    {
        // Même éclat natif que l'XP : quatre poses, agrandies exactement ×4.
        TextureRect burst = new()
        {
            Texture = _revealFrames[0],
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(64, 64),
        };
        _overlay.AddChild(burst);
        burst.GlobalPosition = (card.GlobalPosition + card.Size / 2f - burst.Size / 2f).Round();
        Tween tween = burst.CreateTween();
        tween.TweenMethod(Callable.From<float>(pose => burst.Texture = _revealFrames[Mathf.Min((int)pose, 3)]), 0f, 4f, 0.5f);
        tween.TweenCallback(Callable.From(burst.QueueFree));
    }

    private void FinishReveal()
    {
        _continueButton.Text = "Reprendre la route";
        // La musique s'achève, mais la lecture du butin n'est plus minutée.
        AudioManager.FadeOutUI(_revealAudio, RevealSound, RevealFadeSeconds);
        _revealAudio = null;
    }

    private void Close()
    {
        if (!Visible) return;
        _openingSerial++;
        _isRevealing = false;
        // La mélodie s'éteint avec l'écran au lieu de déborder sur la reprise du jeu.
        AudioManager.FadeOutUI(_revealAudio, RevealSound, RevealFadeSeconds);
        _revealAudio = null;
        HideScreen();
        // Le butin ne s'applique qu'une fois, même si un appui et le minuteur arrivent ensemble.
        Action onComplete = _onComplete;
        _onComplete = null;
        onComplete?.Invoke();
    }

    // ==============================
    // Common helpers
    // ==============================

    private void ClearSlots()
    {
        MenuSkin.Clear(_slotsContainer);
        _slots.Clear();
    }

    private void ShowScreen()
    {
        _overlay.Visible = true;
        _rays.Visible = true;
        _rays.FadeIn(0.35f);
        _panel.Visible = true;
        Visible = true;
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.Always;
    }

    private void HideScreen()
    {
        _overlay.Visible = false;
        _rays.Visible = false;
        _panel.Visible = false;
        Visible = false;
        GetTree().Paused = false;
    }

}
