using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// HUD de run : trois plaques sombres à fort contraste lisibles sur tous les sols.
/// Haut-gauche : niveau, PV, XP. Haut-centre : phase, biome, temps, Effacement et alertes.
/// Haut-droite : score et Essence. Bas-centre : armes et passifs. Les PV sont doublés
/// par une jauge sous le héros (<see cref="PlayerHealthGauge"/>) pour rester visibles en combat.
///
/// Tous les éléments sont enfants d'un Control racine mis à l'échelle (référence 960×540) :
/// le texte reste net car Godot rastérise les polices à la taille finale.
/// </summary>
public partial class HUD : CanvasLayer
{
    [Export(PropertyHint.Range, "0,6.0,0.25")]
    public float HudScale
    {
        get => _hudScale;
        set
        {
            _hudScale = Mathf.Clamp(value, 0f, 6f);
            if (_hudRoot != null)
                ApplyScale();
        }
    }
    private float _hudScale;
    private Control _hudRoot;

    private static readonly Vector2 ReferenceResolution = new(960, 540);

    private const float PlateMargin = 8f;
    private const float VitalsWidth = 204f;
    private const float VitalsBarWidth = 156f;
    private const float RunPlateWidth = 320f;
    private const float ScorePlateWidth = 150f;
    private const float FpsUpdateInterval = 0.25f;
    private const float BiomeUpdateInterval = 0.5f;
    private const float ScoreTickInterval = 0.05f;
    private const float LowHpRatio = 0.3f;

    // --- Vitals ---
    private Label _levelLabel;
    private ColorRect _hpFill;
    private ColorRect _hpChip;
    private Label _hpValueLabel;
    private ColorRect _xpFill;
    private PanelContainer _vitalsPlate;

    // --- Run progress ---
    private Label _phaseLabel;
    private Label _biomeLabel;
    private Label _timeLabel;
    private ColorRect _erasureFill;
    private Label _erasureLabel;
    private Label _alertLabel;

    // --- Score ---
    private Label _scoreLabel;
    private Label _essenceLabel;
    private Label _fpsLabel;

    // --- Bottom-center bars ---
    private readonly NinePatchRect[] _weaponSlotFrames = new NinePatchRect[Player.MaxWeaponSlots];
    private readonly TextureRect[] _weaponSlotIcons = new TextureRect[Player.MaxWeaponSlots];
    private readonly Label[] _weaponSlotLevels = new Label[Player.MaxWeaponSlots];
    private readonly NinePatchRect[] _passiveSlotFrames = new NinePatchRect[Player.MaxPassiveSlots];
    private readonly TextureRect[] _passiveSlotIcons = new TextureRect[Player.MaxPassiveSlots];
    private readonly Label[] _passiveSlotLabels = new Label[Player.MaxPassiveSlots];
    private Texture2D _slotEmptyTex;
    private Texture2D _slotFilledTex;
    private Texture2D _passiveEmptyTex;
    private Texture2D _passiveFilledTex;

    // --- State ---
    private EventBus _eventBus;
    private GroupCache _groupCache;
    private GameManager _gameManager;
    private PlayerProgression _progression;
    private ErasureManager _erasureManager;
    private EssenceTracker _essenceTracker;
    private Player _player;
    private WorldSetup _worldSetup;
    private string _lastBiomeName;
    private string _phaseText = "";
    private float _fpsTimer;
    private float _biomeTimer;
    private float _scoreTimer;
    private float _runSeconds;
    private int _shownSeconds = -1;
    private int _targetScore;
    private float _shownScore;
    private float _hpRatio = 1f;
    private float _chipRatio = 1f;
    private float _lowHpPulse;
    private float _warningCountdown;

    // Palette de la charte graphique
    private static readonly Color PalBlackDeep = new(0x1A / 255f, 0x1A / 255f, 0x2E / 255f);
    private static readonly Color PalBlackBlue = new(0x16 / 255f, 0x21 / 255f, 0x3E / 255f);
    private static readonly Color PalGrayWarm = new(0x6B / 255f, 0x61 / 255f, 0x61 / 255f);
    private static readonly Color PalGrayLight = new(0x9E / 255f, 0x94 / 255f, 0x94 / 255f);
    private static readonly Color PalWhiteOff = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    private static readonly Color PalGoldFoyer = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);
    private static readonly Color PalOrangeFlame = new(0xE0 / 255f, 0x7B / 255f, 0x39 / 255f);
    private static readonly Color PalRedBlood = new(0xC4 / 255f, 0x43 / 255f, 0x2B / 255f);
    private static readonly Color PalCyanEssence = new(0x5E / 255f, 0xC4 / 255f, 0xC4 / 255f);
    private static readonly Color PalVioletMist = new(0x4A / 255f, 0x30 / 255f, 0x66 / 255f);
    private static readonly Color HealthyColor = new(0.42f, 0.74f, 0.36f);
    private static readonly Color PlateColor = new(0.04f, 0.045f, 0.08f, 0.82f);
    private static readonly Color PlateBorder = new(0.83f, 0.66f, 0.26f, 0.35f);
    private static readonly Color BarTrack = new(0.02f, 0.02f, 0.04f, 0.9f);

    public override void _Ready()
    {
        DevelopmentBadge.AttachTo(this);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groupCache = GetNodeOrNull<GroupCache>("/root/GroupCache");
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.XpGained += OnXpChanged;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.ScoreChanged += OnScoreChanged;
        _eventBus.RunPhaseChanged += OnRunPhaseChanged;
        _eventBus.ErasureUpdated += OnErasureUpdated;
        _eventBus.CrisisWarning += OnCrisisWarning;
        _eventBus.CrisisStarted += OnCrisisStarted;
        _eventBus.CrisisEnded += OnCrisisEnded;
        _eventBus.EssenceChanged += OnEssenceChanged;
        _eventBus.WeaponInventoryChanged += OnWeaponInventoryChanged;
        _eventBus.WeaponUpgraded += OnWeaponUpgraded;
        _eventBus.PassiveSouvenirSlotsChanged += OnPassiveSlotsChanged;

        _slotEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_empty.png");
        _slotFilledTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_filled.png");
        _passiveEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive.png");
        _passiveFilledTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive_filled.png");

        _hudRoot = new Control { Name = "HudRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_hudRoot);
        ApplyScale();
        GetViewport().SizeChanged += OnViewportResized;

        BuildVitals();
        BuildRunProgress();
        BuildScoreArea();
        BuildWeaponBar();
        BuildPassiveBar();

        RunEventHud eventHud = new() { Name = "RunEventHud" };
        _hudRoot.AddChild(eventHud);
    }

    public void SetHudScale(float scale) => HudScale = scale;

    private void ApplyScale()
    {
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        if (viewport.X < 1 || viewport.Y < 1)
            viewport = new Vector2(1920, 1080);

        float effectiveScale = _hudScale;
        if (effectiveScale < 0.5f)
            effectiveScale = Mathf.Max(1f, Mathf.Min(viewport.X / ReferenceResolution.X, viewport.Y / ReferenceResolution.Y));

        _hudRoot.Position = Vector2.Zero;
        _hudRoot.Size = viewport / effectiveScale;
        _hudRoot.Scale = new Vector2(effectiveScale, effectiveScale);
    }

    private void OnViewportResized()
    {
        if (_hudRoot != null)
            ApplyScale();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        _fpsTimer += dt;
        if (_fpsTimer >= FpsUpdateInterval)
        {
            _fpsTimer = 0f;
            _fpsLabel.Text = $"{Engine.GetFramesPerSecond()} FPS";
        }

        if (_gameManager == null || _gameManager.CurrentState == GameManager.GameState.Run)
            _runSeconds += dt;
        int seconds = (int)_runSeconds;
        if (seconds != _shownSeconds)
        {
            _shownSeconds = seconds;
            _timeLabel.Text = $"{seconds / 60:00}:{seconds % 60:00}";
            if (_warningCountdown > 0f)
                UpdateWarningText();
        }
        if (_warningCountdown > 0f)
            _warningCountdown -= dt;

        if (_erasureManager != null)
            SetBarRatio(_erasureFill, _erasureManager.GlobalErasurePercent);

        UpdateHpChip(dt);
        UpdateScoreCounter(dt);

        _biomeTimer += dt;
        if (_biomeTimer >= BiomeUpdateInterval)
        {
            _biomeTimer = 0f;
            UpdateBiomeLabel();
        }
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
            _eventBus.XpGained -= OnXpChanged;
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.ScoreChanged -= OnScoreChanged;
            _eventBus.RunPhaseChanged -= OnRunPhaseChanged;
            _eventBus.ErasureUpdated -= OnErasureUpdated;
            _eventBus.CrisisWarning -= OnCrisisWarning;
            _eventBus.CrisisStarted -= OnCrisisStarted;
            _eventBus.CrisisEnded -= OnCrisisEnded;
            _eventBus.EssenceChanged -= OnEssenceChanged;
            _eventBus.WeaponInventoryChanged -= OnWeaponInventoryChanged;
            _eventBus.WeaponUpgraded -= OnWeaponUpgraded;
            _eventBus.PassiveSouvenirSlotsChanged -= OnPassiveSlotsChanged;
        }

        if (GetViewport() != null)
            GetViewport().SizeChanged -= OnViewportResized;
    }

    public void SetProgression(PlayerProgression progression) => _progression = progression;
    public void SetErasureManager(ErasureManager manager) => _erasureManager = manager;

    public void SetEssenceTracker(EssenceTracker tracker)
    {
        _essenceTracker = tracker;
        OnEssenceChanged(_essenceTracker?.CurrentEssence ?? 0);
    }

    /// <summary>Relie le HUD au héros : PV initiaux et jauge sous ses pieds.</summary>
    public void SetPlayer(Player player)
    {
        _player = player;
        if (player == null)
            return;
        player.AddChild(new PlayerHealthGauge { Name = "HealthGauge" });
        UpdateHpDisplay(player.CurrentHp, player.EffectiveMaxHp);
    }

    // ==================== CONSTRUCTION ====================

    private static Label MakeLabel(string text, int size, Color color, int outline = 3)
    {
        Label label = new() { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        if (outline > 0)
        {
            label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
            label.AddThemeConstantOverride("outline_size", outline);
        }
        return label;
    }

    private static PanelContainer MakePlate(float left, float top, float width, float height)
    {
        PanelContainer plate = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        StyleBoxFlat style = new()
        {
            BgColor = PlateColor,
            BorderColor = PlateBorder,
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0
        };
        plate.AddThemeStyleboxOverride("panel", style);
        plate.Position = new Vector2(left, top);
        plate.Size = new Vector2(width, height);
        return plate;
    }

    /// <summary>Barre à fond sombre ; le remplissage est redimensionné par ancre droite.</summary>
    private static ColorRect MakeBar(Control parent, Rect2 rect, Color fillColor, out ColorRect track)
    {
        track = new ColorRect { Color = BarTrack, Position = rect.Position, Size = rect.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(track);
        ColorRect fill = new() { Color = fillColor, MouseFilter = Control.MouseFilterEnum.Ignore };
        fill.AnchorBottom = 1f;
        fill.AnchorRight = 1f;
        fill.OffsetLeft = 1f;
        fill.OffsetTop = 1f;
        fill.OffsetRight = -1f;
        fill.OffsetBottom = -1f;
        track.AddChild(fill);
        return fill;
    }

    private static void SetBarRatio(ColorRect fill, float ratio)
    {
        fill.AnchorRight = Mathf.Clamp(ratio, 0f, 1f);
        fill.OffsetRight = ratio >= 1f ? -1f : 0f;
    }

    private void BuildVitals()
    {
        _vitalsPlate = MakePlate(PlateMargin, PlateMargin, VitalsWidth, 40f);
        _hudRoot.AddChild(_vitalsPlate);
        Control content = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        _vitalsPlate.AddChild(content);

        // Pastille de niveau : le chiffre prime, la légende reste discrète.
        ColorRect badge = new() { Color = PalBlackBlue, Position = new Vector2(4, 4), Size = new Vector2(32, 32) };
        content.AddChild(badge);
        ColorRect badgeEdge = new() { Color = PalCyanEssence with { A = 0.7f }, Position = new Vector2(4, 35), Size = new Vector2(32, 1) };
        content.AddChild(badgeEdge);
        Label caption = MakeLabel(Tr("UI_HUD_LEVEL"), 7, PalGrayLight, 0);
        caption.Position = new Vector2(4, 3);
        caption.Size = new Vector2(32, 9);
        caption.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(caption);
        _levelLabel = MakeLabel("1", 17, PalCyanEssence);
        _levelLabel.Position = new Vector2(4, 9);
        _levelLabel.Size = new Vector2(32, 26);
        _levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _levelLabel.VerticalAlignment = VerticalAlignment.Center;
        content.AddChild(_levelLabel);

        Rect2 hpRect = new(42, 5, VitalsBarWidth, 17);
        _hpFill = MakeBar(content, hpRect, HealthyColor, out ColorRect hpTrack);
        // Trace claire des PV perdus, rattrapée en douceur : le coup reçu se lit d'un coup d'œil.
        _hpChip = new ColorRect { Color = PalWhiteOff with { A = 0.75f } };
        _hpChip.AnchorBottom = 1f;
        _hpChip.OffsetTop = 1f;
        _hpChip.OffsetBottom = -1f;
        hpTrack.AddChild(_hpChip);
        hpTrack.MoveChild(_hpChip, 0);
        _hpValueLabel = MakeLabel("100 / 100", 11, PalWhiteOff);
        _hpValueLabel.Position = Vector2.Zero;
        _hpValueLabel.Size = hpRect.Size;
        _hpValueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _hpValueLabel.VerticalAlignment = VerticalAlignment.Center;
        hpTrack.AddChild(_hpValueLabel);

        _xpFill = MakeBar(content, new Rect2(42, 26, VitalsBarWidth, 8), PalCyanEssence, out _);
        SetBarRatio(_xpFill, 0f);

        _fpsLabel = MakeLabel("", 8, PalGrayWarm, 2);
        _fpsLabel.AnchorTop = 1f;
        _fpsLabel.AnchorBottom = 1f;
        _fpsLabel.OffsetLeft = 6;
        _fpsLabel.OffsetTop = -14;
        _fpsLabel.OffsetRight = 60;
        _fpsLabel.OffsetBottom = -3;
        _hudRoot.AddChild(_fpsLabel);
    }

    private void BuildRunProgress()
    {
        Control anchor = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        anchor.AnchorLeft = 0.5f;
        anchor.AnchorRight = 0.5f;
        _hudRoot.AddChild(anchor);

        PanelContainer plate = MakePlate(-RunPlateWidth / 2f, PlateMargin, RunPlateWidth, 30f);
        anchor.AddChild(plate);
        Control content = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        plate.AddChild(content);

        _phaseText = Tr("UI_HUD_PHASE_EXPLORATION");
        _phaseLabel = MakeLabel(_phaseText, 11, PalWhiteOff);
        _phaseLabel.Position = new Vector2(8, 1);
        _phaseLabel.Size = new Vector2(110, 16);
        content.AddChild(_phaseLabel);

        _biomeLabel = MakeLabel("", 9, PalGrayLight, 2);
        _biomeLabel.Position = new Vector2(100, 3);
        _biomeLabel.Size = new Vector2(120, 14);
        _biomeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_biomeLabel);

        _timeLabel = MakeLabel("00:00", 11, PalWhiteOff);
        _timeLabel.Position = new Vector2(RunPlateWidth - 68, 1);
        _timeLabel.Size = new Vector2(60, 16);
        _timeLabel.HorizontalAlignment = HorizontalAlignment.Right;
        content.AddChild(_timeLabel);

        _erasureFill = MakeBar(content, new Rect2(8, 19, RunPlateWidth - 52, 6), PalCyanEssence, out _);
        SetBarRatio(_erasureFill, 0f);
        _erasureLabel = MakeLabel("0 %", 8, PalGrayLight, 2);
        _erasureLabel.Position = new Vector2(RunPlateWidth - 42, 14);
        _erasureLabel.Size = new Vector2(34, 14);
        _erasureLabel.HorizontalAlignment = HorizontalAlignment.Right;
        content.AddChild(_erasureLabel);

        _alertLabel = MakeLabel("", 13, PalOrangeFlame, 4);
        _alertLabel.Position = new Vector2(-RunPlateWidth / 2f, PlateMargin + 32f);
        _alertLabel.Size = new Vector2(RunPlateWidth, 20);
        _alertLabel.HorizontalAlignment = HorizontalAlignment.Center;
        anchor.AddChild(_alertLabel);
    }

    private void BuildScoreArea()
    {
        Control anchor = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        anchor.AnchorLeft = 1f;
        anchor.AnchorRight = 1f;
        _hudRoot.AddChild(anchor);

        PanelContainer plate = MakePlate(-ScorePlateWidth - PlateMargin, PlateMargin, ScorePlateWidth, 40f);
        anchor.AddChild(plate);
        Control content = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        plate.AddChild(content);

        Label caption = MakeLabel(Tr("UI_HUD_SCORE"), 8, PalGrayLight, 0);
        caption.Position = new Vector2(8, 3);
        caption.Size = new Vector2(50, 10);
        content.AddChild(caption);

        _scoreLabel = MakeLabel("0", 18, PalGoldFoyer, 4);
        _scoreLabel.Position = new Vector2(8, 0);
        _scoreLabel.Size = new Vector2(ScorePlateWidth - 16, 24);
        _scoreLabel.HorizontalAlignment = HorizontalAlignment.Right;
        content.AddChild(_scoreLabel);

        _essenceLabel = MakeLabel("", 10, PalCyanEssence);
        _essenceLabel.Position = new Vector2(8, 22);
        _essenceLabel.Size = new Vector2(ScorePlateWidth - 16, 15);
        _essenceLabel.HorizontalAlignment = HorizontalAlignment.Right;
        content.AddChild(_essenceLabel);
    }

    private void BuildWeaponBar()
    {
        const float slotSize = 28f;
        HBoxContainer bar = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AnchorLeft = 0.5f;
        bar.AnchorRight = 0.5f;
        bar.AnchorTop = 1f;
        bar.AnchorBottom = 1f;
        float totalWidth = Player.MaxWeaponSlots * (slotSize + 3);
        bar.OffsetLeft = -totalWidth / 2;
        bar.OffsetRight = totalWidth / 2;
        bar.OffsetTop = -54;
        bar.OffsetBottom = -26;
        bar.AddThemeConstantOverride("separation", 3);
        bar.Alignment = BoxContainer.AlignmentMode.Center;
        _hudRoot.AddChild(bar);

        for (int i = 0; i < Player.MaxWeaponSlots; i++)
        {
            Control slotRoot = new() { CustomMinimumSize = new Vector2(slotSize, slotSize) };
            NinePatchRect frame = MakeSlotFrame(_slotEmptyTex, 4);
            slotRoot.AddChild(frame);

            const float iconSize = 22f;
            TextureRect icon = new()
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Position = new Vector2((slotSize - iconSize) / 2, (slotSize - iconSize) / 2 - 1),
                Size = new Vector2(iconSize, iconSize),
                Visible = false
            };
            slotRoot.AddChild(icon);

            Label level = MakeLabel("", 9, PalGoldFoyer, 3);
            level.Position = new Vector2(slotSize - 14, slotSize - 13);
            level.Size = new Vector2(13, 12);
            level.HorizontalAlignment = HorizontalAlignment.Right;
            slotRoot.AddChild(level);

            _weaponSlotFrames[i] = frame;
            _weaponSlotIcons[i] = icon;
            _weaponSlotLevels[i] = level;
            bar.AddChild(slotRoot);
        }
    }

    private void BuildPassiveBar()
    {
        const float passiveSize = 18f;
        HBoxContainer bar = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AnchorLeft = 0.5f;
        bar.AnchorRight = 0.5f;
        bar.AnchorTop = 1f;
        bar.AnchorBottom = 1f;
        float totalWidth = Player.MaxPassiveSlots * (passiveSize + 3);
        bar.OffsetLeft = -totalWidth / 2;
        bar.OffsetRight = totalWidth / 2;
        bar.OffsetTop = -23;
        bar.OffsetBottom = -5;
        bar.AddThemeConstantOverride("separation", 3);
        bar.Alignment = BoxContainer.AlignmentMode.Center;
        _hudRoot.AddChild(bar);

        for (int i = 0; i < Player.MaxPassiveSlots; i++)
        {
            Control slotRoot = new() { CustomMinimumSize = new Vector2(passiveSize, passiveSize) };
            NinePatchRect frame = MakeSlotFrame(_passiveEmptyTex, 3);
            slotRoot.AddChild(frame);

            TextureRect icon = new()
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Visible = false
            };
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            icon.OffsetLeft = 2;
            icon.OffsetTop = 2;
            icon.OffsetRight = -2;
            icon.OffsetBottom = -2;
            slotRoot.AddChild(icon);

            Label level = MakeLabel("", 8, PalWhiteOff, 3);
            level.Position = new Vector2(passiveSize - 10, passiveSize - 11);
            level.Size = new Vector2(10, 10);
            level.HorizontalAlignment = HorizontalAlignment.Right;
            slotRoot.AddChild(level);

            _passiveSlotFrames[i] = frame;
            _passiveSlotIcons[i] = icon;
            _passiveSlotLabels[i] = level;
            bar.AddChild(slotRoot);
        }
    }

    private static NinePatchRect MakeSlotFrame(Texture2D texture, int margin)
    {
        NinePatchRect frame = new()
        {
            Texture = texture,
            PatchMarginLeft = margin,
            PatchMarginRight = margin,
            PatchMarginTop = margin,
            PatchMarginBottom = margin,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return frame;
    }

    // ==================== MISES À JOUR ====================

    private void OnPlayerDamaged(float currentHp, float maxHp) => UpdateHpDisplay(currentHp, maxHp);

    private void UpdateHpDisplay(float currentHp, float maxHp)
    {
        float clampedMax = Mathf.Max(1f, maxHp);
        float clampedHp = Mathf.Clamp(currentHp, 0f, clampedMax);
        float previous = _hpRatio;
        _hpRatio = clampedHp / clampedMax;
        if (_hpRatio > previous)
            _chipRatio = _hpRatio;

        SetBarRatio(_hpFill, _hpRatio);
        _hpFill.Color = _hpRatio < LowHpRatio ? PalRedBlood : (_hpRatio < 0.55f ? PalOrangeFlame : HealthyColor);
        _hpValueLabel.Text = $"{Mathf.RoundToInt(clampedHp)} / {Mathf.RoundToInt(clampedMax)}";
    }

    private void UpdateHpChip(float dt)
    {
        if (_chipRatio > _hpRatio)
            _chipRatio = Mathf.Max(_hpRatio, _chipRatio - dt * 0.6f);
        _hpChip.AnchorRight = _chipRatio;

        // Bord de la plaque qui bat quand la vie est basse.
        if (_hpRatio < LowHpRatio && _hpRatio > 0f)
        {
            _lowHpPulse += dt * 5f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_lowHpPulse);
            _vitalsPlate.SelfModulate = Colors.White.Lerp(new Color(1.6f, 0.7f, 0.6f), pulse);
        }
        else if (_lowHpPulse != 0f)
        {
            _lowHpPulse = 0f;
            _vitalsPlate.SelfModulate = Colors.White;
        }
    }

    private void OnXpChanged(float _amount)
    {
        if (_progression == null)
            return;
        float ratio = _progression.XpToNextLevel > 0 ? _progression.CurrentXp / _progression.XpToNextLevel : 0f;
        SetBarRatio(_xpFill, ratio);
    }

    private void OnLevelUp(int newLevel)
    {
        _levelLabel.Text = $"{newLevel}";
        OnXpChanged(0);
        _levelLabel.PivotOffset = _levelLabel.Size / 2f;
        Tween tween = CreateTween();
        tween.TweenProperty(_levelLabel, "scale", new Vector2(1.5f, 1.5f), 0.08f);
        tween.TweenProperty(_levelLabel, "scale", Vector2.One, 0.25f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    private void OnScoreChanged(int newScore) => _targetScore = newScore;

    /// <summary>Le score défile vers sa cible : chaque gain se voit, sans reconstruire la chaîne à chaque frame.</summary>
    private void UpdateScoreCounter(float dt)
    {
        if ((int)_shownScore == _targetScore)
            return;
        _scoreTimer += dt;
        if (_scoreTimer < ScoreTickInterval)
            return;
        float step = Mathf.Max(1f, Mathf.Abs(_targetScore - _shownScore) * 0.25f);
        _shownScore = _shownScore < _targetScore
            ? Mathf.Min(_targetScore, _shownScore + step)
            : Mathf.Max(_targetScore, _shownScore - step);
        _scoreTimer = 0f;
        _scoreLabel.Text = ((int)_shownScore).ToString("N0");
    }

    private void OnRunPhaseChanged(string oldPhase, string newPhase)
    {
        _phaseText = newPhase switch
        {
            "Crisis" => Tr("UI_HUD_PHASE_CRISIS"),
            "LateGame" => Tr("UI_HUD_PHASE_LATE"),
            "Endgame" => Tr("UI_HUD_PHASE_ENDGAME"),
            "Death" => Tr("UI_HUD_PHASE_DEATH"),
            _ => Tr("UI_HUD_PHASE_EXPLORATION")
        };
        _phaseLabel.Text = _phaseText;

        Color phaseColor = newPhase switch
        {
            "Crisis" => PalOrangeFlame,
            "LateGame" => PalRedBlood,
            "Endgame" => PalGoldFoyer,
            _ => PalWhiteOff
        };
        _phaseLabel.AddThemeColorOverride("font_color", phaseColor);

        Color barColor = newPhase switch
        {
            "Crisis" => PalOrangeFlame,
            "LateGame" => PalRedBlood,
            "Endgame" => PalGoldFoyer,
            _ => PalCyanEssence
        };
        CreateTween().TweenProperty(_erasureFill, "color", barColor, 1f);
    }

    private void OnErasureUpdated(float globalErasurePercent)
    {
        SetBarRatio(_erasureFill, globalErasurePercent);
        _erasureLabel.Text = $"{Mathf.RoundToInt(globalErasurePercent * 100f)} %";
    }

    private void OnCrisisWarning(int crisisNumber, float countdown)
    {
        _warningCountdown = countdown;
        _alertLabel.AddThemeColorOverride("font_color", PalOrangeFlame);
        UpdateWarningText();
    }

    private void UpdateWarningText()
    {
        _alertLabel.Text = string.Format(Tr("UI_HUD_CRISIS_IN"), Mathf.CeilToInt(Mathf.Max(0f, _warningCountdown)));
    }

    private void OnCrisisStarted(int crisisNumber, int intensity)
    {
        _warningCountdown = 0f;
        _alertLabel.Text = intensity > 1
            ? string.Format(Tr("UI_HUD_CRISIS_INTENSITY"), crisisNumber, intensity)
            : string.Format(Tr("UI_HUD_CRISIS"), crisisNumber);
        _alertLabel.AddThemeColorOverride("font_color", PalRedBlood.Lightened(0.2f));
    }

    private void OnCrisisEnded(int crisisNumber)
    {
        _alertLabel.Text = "";
    }

    private void OnEssenceChanged(int amount)
    {
        if (_essenceLabel != null)
            _essenceLabel.Text = string.Format(Tr("UI_HUD_ESSENCE"), amount);
    }

    private void OnWeaponInventoryChanged()
    {
        Player player = ResolvePlayer();
        if (player == null)
            return;

        System.Collections.Generic.IReadOnlyList<WeaponInstance> weapons = player.WeaponSlots;
        for (int i = 0; i < Player.MaxWeaponSlots; i++)
        {
            if (i < weapons.Count)
            {
                WeaponInstance weapon = weapons[i];
                _weaponSlotFrames[i].Texture = _slotFilledTex;
                _weaponSlotFrames[i].Modulate = weapon.RarityColor;
                LoadWeaponIcon(i, weapon.Sprite);
                int fragLevel = player.GetWeaponFragmentLevel(weapon.Id);
                _weaponSlotLevels[i].Text = fragLevel > 1 ? $"{fragLevel}" : "";
            }
            else
            {
                _weaponSlotFrames[i].Texture = _slotEmptyTex;
                _weaponSlotFrames[i].Modulate = Colors.White;
                _weaponSlotIcons[i].Visible = false;
                _weaponSlotLevels[i].Text = "";
            }
        }
    }

    private void LoadWeaponIcon(int slotIndex, string spritePath)
    {
        TextureRect icon = _weaponSlotIcons[slotIndex];
        string resPath = string.IsNullOrEmpty(spritePath) ? null : (spritePath.StartsWith("res://") ? spritePath : $"res://{spritePath}");
        Texture2D texture = resPath != null && ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
        icon.Texture = texture;
        icon.Visible = texture != null;
    }

    private void OnWeaponUpgraded(string _weaponId, int _slotIndex, string _stat, int _newLevel) => OnWeaponInventoryChanged();

    private void OnPassiveSlotsChanged()
    {
        Player player = ResolvePlayer();
        if (player == null)
            return;

        System.Collections.Generic.IReadOnlyList<ActivePassiveSouvenir> passives = player.PassiveSlots;
        for (int i = 0; i < Player.MaxPassiveSlots; i++)
        {
            if (i < passives.Count)
            {
                ActivePassiveSouvenir passive = passives[i];
                _passiveSlotFrames[i].Texture = _passiveFilledTex;
                _passiveSlotFrames[i].Modulate = Colors.White;
                _passiveSlotLabels[i].Text = passive.Level > 1 ? $"{passive.Level}" : "";

                string iconPath = PerkIconResolver.GetPassiveStatIconPath(passive.Data.Stat);
                Texture2D iconTex = GD.Load<Texture2D>($"res://{iconPath}");
                _passiveSlotIcons[i].Texture = iconTex;
                _passiveSlotIcons[i].Modulate = passive.Data.IconColor;
                _passiveSlotIcons[i].Visible = iconTex != null;
                if (iconTex == null)
                    _passiveSlotFrames[i].Modulate = passive.Data.IconColor;
            }
            else
            {
                _passiveSlotFrames[i].Texture = _passiveEmptyTex;
                _passiveSlotFrames[i].Modulate = Colors.White;
                _passiveSlotIcons[i].Visible = false;
                _passiveSlotLabels[i].Text = "";
            }
        }
    }

    private Player ResolvePlayer()
    {
        if (_player == null || !IsInstanceValid(_player))
            _player = (_groupCache?.GetPlayer() ?? GetTree().GetFirstNodeInGroup("player")) as Player;
        return _player;
    }

    private void UpdateBiomeLabel()
    {
        if (_worldSetup == null || !IsInstanceValid(_worldSetup))
            _worldSetup = GetNodeOrNull<WorldSetup>("/root/Main");
        Player player = ResolvePlayer();
        if (_worldSetup == null || player == null)
            return;

        string biomeName = _worldSetup.GetBiomeAt(player.GlobalPosition)?.Name ?? "";
        if (biomeName != _lastBiomeName)
        {
            _lastBiomeName = biomeName;
            _biomeLabel.Text = biomeName;
        }
    }
}
