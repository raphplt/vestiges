using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Écran roulette du butin d'un coffre : chaque case fait défiler des leurres puis s'arrête sur le butin
/// déjà résolu (Essence, XP, stat, niveaux d'objet ou Souvenir). Met le jeu en pause, joue le son d'ouverture.
/// Un appui révèle d'un coup les cases encore en défilement, un second ferme l'écran sans attendre.
/// Couleurs de rareté : palette unique (RarityPalette).
/// </summary>
public partial class ChestLootScreen : CanvasLayer
{
    private const string MenusPath = "res://assets/ui/menus/";

    // --- Colors ---
    private static readonly Color GoldColor = new(0.83f, 0.66f, 0.26f);
    private static readonly Color GoldBright = new(0.9f, 0.78f, 0.39f);
    private static readonly Color GoldDim = new(0.63f, 0.47f, 0.16f);
    private static readonly Color TextLight = new(0.92f, 0.9f, 0.85f);
    private static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);
    private static readonly Color OverlayColor = new(0.0f, 0.0f, 0.02f, 0.8f);

    // --- Roulette config ---
    private const float RouletteMinInterval = 0.04f;
    private const float RouletteMaxInterval = 0.35f;
    private const float RouletteDuration = 2.5f;
    private const float PostRevealDelay = 1.8f;
    private const string RevealSound = "sfx_chest_reveal";
    private const float RevealFadeSeconds = 0.5f;
    // Le clic d'ouverture frappe dans ses 0,3 premières secondes : la mélodie entre juste après, sur le défilement.
    private const float RevealMelodyDelay = 0.3f;

    // --- UI nodes ---
    private ColorRect _overlay;
    private PixelBackdrop _rays;
    private PanelContainer _panel;
    private VBoxContainer _slotsContainer;
    private Label _title;
    private Texture2D _panelTex;
    private Texture2D _separatorTex;
    private readonly Texture2D[] _revealFrames = new Texture2D[4];

    // --- State ---
    private List<ResolvedLoot> _pendingLoots;
    private string _rarity;
    private Action _onComplete;
    private readonly List<SlotState> _slots = new();
    private bool _isRevealing;
    private AudioStreamPlayer _revealAudio;
    private int _openingSerial;
    private int _closeSerial;
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
        _fakeItems = new[]
        {
            new LootDisplayInfo("Essence ×6", UITheme.CyanEssence, LootIconResolver.Get("essence")),
            new LootDisplayInfo("Essence ×12", UITheme.CyanEssence, LootIconResolver.Get("essence")),
            new LootDisplayInfo("+25 XP", new Color("8AB8C4"), LootIconResolver.Get("xp")),
            new LootDisplayInfo("+40 XP", new Color("8AB8C4"), LootIconResolver.Get("xp")),
            new LootDisplayInfo($"{StatCatalog.Name("max_hp")} +15", GoldBright, LootIconResolver.Get("stat", "max_hp")),
            new LootDisplayInfo($"{StatCatalog.Name("crit_multiplier")} +10 %", GoldBright, LootIconResolver.Get("stat", "crit_multiplier")),
            new LootDisplayInfo($"{StatCatalog.Name("armor")} +2", GoldBright, LootIconResolver.Get("stat", "armor")),
        };
        BuildUI();
        HideScreen();
    }

    private void LoadTextures()
    {
        _panelTex = LoadTex(MenusPath + "ui_panel_frame.png");
        _separatorTex = LoadTex(MenusPath + "ui_separator_simple.png");
        for (int i = 0; i < _revealFrames.Length; i++)
            _revealFrames[i] = GD.Load<Texture2D>($"res://assets/ui/hud/plan25/xp_burst_{i:00}.png");
    }

    private static Texture2D LoadTex(string path)
    {
        return UITheme.LoadTex(path);
    }

    // ==============================
    // UI construction
    // ==============================

    private void BuildUI()
    {
        _overlay = new ColorRect();
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay.Color = OverlayColor;
        _overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(_overlay);

        // Fond en gros pixels, rayons tramés et poussière d'oubli (plan 24 B1).
        _rays = new PixelBackdrop(PixelBackdrop.GoldTint);
        _rays.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_rays);

        // Panel
        _panel = new PanelContainer();
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.GrowHorizontal = Control.GrowDirection.Both;
        _panel.GrowVertical = Control.GrowDirection.Both;
        _panel.CustomMinimumSize = new Vector2(420, 100);

        if (_panelTex != null)
        {
            StyleBoxTexture panelStyle = CreateNinePatch(_panelTex, 6, 6, 6, 6);
            panelStyle.ContentMarginLeft = 24;
            panelStyle.ContentMarginRight = 24;
            panelStyle.ContentMarginTop = 18;
            panelStyle.ContentMarginBottom = 22;
            _panel.AddThemeStyleboxOverride("panel", panelStyle);
        }
        else
        {
            StyleBoxFlat fallback = new();
            fallback.BgColor = new Color(0.06f, 0.06f, 0.1f, 0.95f);
            fallback.SetBorderWidthAll(2);
            fallback.BorderColor = GoldDim;
            fallback.SetCornerRadiusAll(4);
            fallback.ContentMarginLeft = 24;
            fallback.ContentMarginRight = 24;
            fallback.ContentMarginTop = 18;
            fallback.ContentMarginBottom = 22;
            _panel.AddThemeStyleboxOverride("panel", fallback);
        }

        AddChild(_panel);

        VBoxContainer innerVBox = new();
        innerVBox.AddThemeConstantOverride("separation", 14);
        _panel.AddChild(innerVBox);

        // Title
        _title = new Label();
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        UITheme.SetTextRole(_title, TextRole.Heading);
        _title.AddThemeColorOverride("font_color", GoldBright);
        _title.Text = "COFFRE";
        innerVBox.AddChild(_title);

        // Separator
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

        // Slots container
        _slotsContainer = new VBoxContainer();
        _slotsContainer.AddThemeConstantOverride("separation", 10);
        innerVBox.AddChild(_slotsContainer);
    }

    // ==============================
    // Public API
    // ==============================

    /// <summary>
    /// Montre le chest roulette. Callback onComplete appelé quand l'animation est terminée.
    /// </summary>
    public void ShowLoot(List<ResolvedLoot> loots, string rarity, Action onComplete)
    {
        _pendingLoots = loots;
        _rarity = rarity;
        _onComplete = onComplete;
        _slotsRevealed = 0;
        _isRevealing = true;

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
        card.CustomMinimumSize = new Vector2(370, 60);

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
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        card.AddChild(margin);

        Label label = new();
        label.HorizontalAlignment = HorizontalAlignment.Left;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        UITheme.SetTextRole(label, TextRole.Lead);
        label.AddThemeColorOverride("font_color", TextDim);
        label.Text = "???";
        HBoxContainer contents = new() { Alignment = BoxContainer.AlignmentMode.Center };
        contents.AddThemeConstantOverride("separation", 8);
        margin.AddChild(contents);
        int fakeIndex = GD.RandRange(0, _fakeItems.Length - 1);
        LootDisplayInfo initial = _fakeItems[fakeIndex];
        TextureRect icon = (TextureRect)PlayerSheet.MakeIcon(null, 32);
        icon.Texture = initial.Icon;
        contents.AddChild(icon);
        label.Text = initial.Text;
        contents.AddChild(label);
        // L'éclat reste un badge secondaire : il ne remplace jamais le signe du gain.
        int rarityRank = RarityArt.Rank(_rarity);
        if (rarityRank >= 0)
            contents.AddChild(new RarityIcon(rarityRank, 12));

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
            ScheduleClose();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || !@event.IsPressed() || @event.IsEcho())
            return;
        // Les directions ne passent rien : le joueur se déplaçait encore en ouvrant le coffre.
        bool skip = @event is InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right }
            || @event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_cancel")
            || @event.IsActionPressed("interact");
        if (!skip)
            return;
        GetViewport().SetInputAsHandled();
        if (_isRevealing)
            RevealRemaining();
        else
            Close();
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
        ScheduleClose();
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

    private void ScheduleClose()
    {
        // Un minuteur dépassé par une fermeture anticipée ne ferme jamais l'écran d'un coffre suivant.
        int serial = ++_closeSerial;
        GetTree().CreateTimer(PostRevealDelay, processAlways: true).Timeout += () =>
        {
            if (serial == _closeSerial)
                Close();
        };
    }

    private void Close()
    {
        _closeSerial++;
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
        foreach (Node child in _slotsContainer.GetChildren())
            child.QueueFree();
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
}
