using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Ecran roulette de loot de coffre — affiche un slot machine qui cycle
/// à travers des items aléatoires puis s'arrête sur le vrai loot.
/// Pause le jeu, joue le son chest_opening, effets dopamine à fond.
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

    // Rarity colors
    private static readonly Color CommonColor = new(0.9f, 0.85f, 0.6f);
    private static readonly Color RareColor = new(0.5f, 0.4f, 0.8f);
    private static readonly Color EpicColor = new(0.9f, 0.6f, 0.15f);
    private static readonly Color LoreColor = new(0.8f, 0.85f, 1f);

    // --- Roulette config ---
    private const float RouletteMinInterval = 0.04f;
    private const float RouletteMaxInterval = 0.35f;
    private const float RouletteDuration = 2.5f;
    private const float PostRevealDelay = 1.8f;

    // --- Rays config ---
    private const int RayCount = 16;
    private const float RaySpeed = 0.25f;

    // --- UI nodes ---
    private ColorRect _overlay;
    private LevelUpScreen.LightRaysControl _rays;
    private PanelContainer _panel;
    private VBoxContainer _slotsContainer;
    private Label _title;
    private Texture2D _panelTex;
    private Texture2D _separatorTex;

    // --- State ---
    private List<LootResolver.LootResult> _pendingLoots;
    private string _rarity;
    private Action _onComplete;
    private readonly List<SlotState> _slots = new();
    private bool _isRevealing;
    private int _slotsRevealed;

    // --- Fake items for roulette cycling ---
    private static readonly LootDisplayInfo[] FakeItems = new[]
    {
        new LootDisplayInfo("Bois x5", new Color(0.55f, 0.41f, 0.08f)),
        new LootDisplayInfo("Pierre x3", new Color(0.66f, 0.6f, 0.47f)),
        new LootDisplayInfo("Metal x2", new Color(0.44f, 0.53f, 0.63f)),
        new LootDisplayInfo("Fibre x4", new Color(0.35f, 0.55f, 0.3f)),
        new LootDisplayInfo("Essence x1", new Color(0.55f, 0.35f, 0.65f)),
        new LootDisplayInfo("+25 XP", new Color(0.4f, 0.8f, 1f)),
        new LootDisplayInfo("+40 XP", new Color(0.4f, 0.8f, 1f)),
        new LootDisplayInfo("Perk", new Color(0.5f, 1f, 0.5f)),
        new LootDisplayInfo("Bois x8", new Color(0.55f, 0.41f, 0.08f)),
        new LootDisplayInfo("Pierre x6", new Color(0.66f, 0.6f, 0.47f)),
        new LootDisplayInfo("Metal x4", new Color(0.44f, 0.53f, 0.63f)),
    };

    private struct LootDisplayInfo
    {
        public string Text;
        public Color Color;
        public LootDisplayInfo(string text, Color color) { Text = text; Color = color; }
    }

    private class SlotState
    {
        public Label Label;
        public PanelContainer Card;
        public ColorRect Flash;
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
        _panelTex = LoadTex(MenusPath + "ui_panel_frame.png");
        _separatorTex = LoadTex(MenusPath + "ui_separator_simple.png");
    }

    private static Texture2D LoadTex(string path)
    {
        if (ResourceLoader.Exists(path))
            return GD.Load<Texture2D>(path);
        return null;
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

        // Rays
        Color rayA = new(0.83f, 0.66f, 0.26f, 0.1f);
        Color rayB = new(0.9f, 0.78f, 0.39f, 0.05f);
        _rays = new LevelUpScreen.LightRaysControl(RayCount, RaySpeed, rayA, rayB);
        _rays.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _rays.MouseFilter = Control.MouseFilterEnum.Ignore;
        _rays.ProcessMode = ProcessModeEnum.Always;
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
        _title.AddThemeFontSizeOverride("font_size", 22);
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
    public void ShowLoot(List<LootResolver.LootResult> loots, string rarity, Action onComplete)
    {
        _pendingLoots = loots;
        _rarity = rarity;
        _onComplete = onComplete;
        _slotsRevealed = 0;
        _isRevealing = true;

        ClearSlots();

        // Set title based on rarity
        _title.Text = rarity switch
        {
            "epic" => "COFFRE EPIQUE",
            "rare" => "COFFRE RARE",
            "lore" => "COFFRE ANCESTRAL",
            _ => "COFFRE"
        };

        Color rarityColor = rarity switch
        {
            "epic" => EpicColor,
            "rare" => RareColor,
            "lore" => LoreColor,
            _ => GoldBright
        };
        _title.AddThemeColorOverride("font_color", rarityColor);

        // Create a slot for each loot
        for (int i = 0; i < loots.Count; i++)
        {
            SlotState slot = CreateSlot(loots[i], i);
            _slots.Add(slot);
        }

        ShowScreen();

        // Play chest opening sound
        AudioManager.PlayUI("sfx_chest_opening", 0f);
    }

    // ==============================
    // Slot creation
    // ==============================

    private SlotState CreateSlot(LootResolver.LootResult loot, int index)
    {
        PanelContainer card = new();
        card.CustomMinimumSize = new Vector2(370, 60);

        StyleBoxFlat cardStyle = new();
        cardStyle.BgColor = new Color(0.08f, 0.08f, 0.12f);
        cardStyle.SetBorderWidthAll(1);
        cardStyle.BorderColor = new Color(0.25f, 0.24f, 0.2f, 0.6f);
        cardStyle.SetCornerRadiusAll(3);
        card.AddThemeStyleboxOverride("panel", cardStyle);

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
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", TextDim);
        label.Text = "???";
        margin.AddChild(label);

        card.AddChild(flash);
        _slotsContainer.AddChild(card);

        LootDisplayInfo finalItem = LootToDisplayInfo(loot);

        return new SlotState
        {
            Label = label,
            Card = card,
            Flash = flash,
            FinalItem = finalItem,
            Timer = 0f,
            CurrentInterval = RouletteMinInterval,
            Elapsed = 0f,
            StopTime = RouletteDuration + index * 0.4f,
            Stopped = false,
            FakeIndex = GD.RandRange(0, FakeItems.Length - 1)
        };
    }

    private static LootDisplayInfo LootToDisplayInfo(LootResolver.LootResult loot)
    {
        switch (loot.Type)
        {
            case "resource":
                ResourceData res = ResourceDataLoader.Get(loot.ItemId);
                string resName = res?.Name ?? loot.ItemId;
                Color resColor = res?.Color ?? CommonColor;
                return new LootDisplayInfo($"{resName} x{loot.Amount}", resColor);
            case "xp":
                return new LootDisplayInfo($"+{loot.Amount} XP", new Color(0.4f, 0.8f, 1f));
            case "perk":
                return new LootDisplayInfo("Perk Aleatoire", new Color(0.5f, 1f, 0.5f));
            case "souvenir":
                return new LootDisplayInfo($"Souvenir: {loot.ItemId}", LoreColor);
            default:
                return new LootDisplayInfo(loot.ItemId, CommonColor);
        }
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
                slot.FakeIndex = (slot.FakeIndex + 1) % FakeItems.Length;
                LootDisplayInfo fakeItem = FakeItems[slot.FakeIndex];
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

    private void RevealSlot(SlotState slot)
    {
        slot.Stopped = true;
        _slotsRevealed++;

        // Set final item
        slot.Label.Text = slot.FinalItem.Text;
        slot.Label.AddThemeColorOverride("font_color", TextLight);
        slot.Label.AddThemeFontSizeOverride("font_size", 20);

        // Card glow border
        StyleBoxFlat revealStyle = new();
        revealStyle.BgColor = new Color(0.1f, 0.1f, 0.16f);
        revealStyle.SetBorderWidthAll(2);
        revealStyle.BorderColor = GoldColor;
        revealStyle.SetCornerRadiusAll(3);
        slot.Card.AddThemeStyleboxOverride("panel", revealStyle);

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

        // Sound — perk_choix for each reveal
        AudioManager.PlayUI("sfx_perk_choix", 0.05f);

        // Screen shake on last reveal
        if (_slotsRevealed >= _slots.Count)
            Combat.ScreenShake.Instance?.ShakeMedium();
    }

    private void SpawnRevealParticles(PanelContainer card)
    {
        Vector2 center = card.GlobalPosition + card.Size / 2f;
        int count = 8;

        // We need to add particles to a Control parent on the same layer
        for (int i = 0; i < count; i++)
        {
            ColorRect particle = new();
            float size = (float)GD.RandRange(3f, 7f);
            particle.CustomMinimumSize = new Vector2(size, size);
            particle.Size = new Vector2(size, size);

            float hue = (float)GD.RandRange(0.08f, 0.15f); // gold hue range
            particle.Color = Color.FromHsv(hue, 0.8f, 1f);

            // Position at card center
            particle.GlobalPosition = center + new Vector2(
                (float)GD.RandRange(-card.Size.X * 0.4f, card.Size.X * 0.4f),
                (float)GD.RandRange(-card.Size.Y * 0.3f, card.Size.Y * 0.3f));

            _overlay.AddChild(particle);

            float angle = (float)GD.RandRange(0, Mathf.Tau);
            float dist = (float)GD.RandRange(40f, 90f);
            Vector2 target = particle.GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            ColorRect p = particle;
            Tween tween = CreateTween();
            tween.SetProcessMode(Tween.TweenProcessMode.Physics);
            tween.SetParallel();
            tween.TweenProperty(p, "global_position", target, 0.5f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(p, "modulate:a", 0f, 0.5f)
                .SetDelay(0.2f);
            tween.TweenProperty(p, "size", Vector2.One * 1f, 0.5f);
            tween.Chain().TweenCallback(Callable.From(() =>
            {
                if (IsInstanceValid(p))
                    p.QueueFree();
            }));
        }
    }

    private void ScheduleClose()
    {
        SceneTreeTimer timer = GetTree().CreateTimer(PostRevealDelay, processAlways: true);
        timer.Timeout += () =>
        {
            HideScreen();
            _onComplete?.Invoke();
        };
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
        _rays.ResetAngle();
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
