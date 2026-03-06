using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// HUD pixel art : barres dessinées manuellement, icônes 16×16, slots d'armes visuels,
/// style semi-diégétique (parchemin usé, métal oxydé) comme décrit dans le GDD §9.
/// Layout: top=jour/nuit + HP/XP + score, bottom-center=weapon slots, bottom-left=inventaire.
///
/// Scaling : tous les éléments sont enfants d'un Control racine dont le Scale est ajusté.
/// HudScale = 0 (défaut) → auto-scale basé sur viewport/640×360. Ex: 1920×1080 → scale 3.0.
/// Modifier HudScale dans l'inspecteur ou via SetHudScale(float) au runtime.
/// </summary>
public partial class HUD : CanvasLayer
{
    // --- Scale system ---
    /// <summary>
    /// Facteur de zoom du HUD entier. 0 = auto (recommandé : cible 640×360 logique).
    /// Valeur manuelle : 1.0 = natif (minuscule), 3.0 = typique Full HD, 4+ = très grand.
    /// Modifiable depuis l'inspecteur Godot ou via SetHudScale() au runtime.
    /// </summary>
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
    private float _hudScale = 0f; // 0 = auto
    private Control _hudRoot;

    /// <summary>Résolution logique de référence. Le scale auto est calculé pour que le HUD
    /// soit rendu comme s'il était sur un écran de cette taille.</summary>
    private static readonly Vector2 ReferenceResolution = new(960, 540);

    // --- Top-left vitals ---
    private TextureRect _heartIcon;
    private Control _hpBarContainer;
    private ColorRect _hpBarFill;
    private ColorRect _hpBarBg;
    private Label _hpValueLabel;
    private TextureRect _levelIcon;
    private Label _levelLabel;
    private Control _xpBarContainer;
    private ColorRect _xpBarFill;
    private ColorRect _xpBarBg;

    // --- Top-center day/night ---
    private Control _dayNightContainer;
    private ColorRect _dayNightBg;
    private ColorRect _dayNightFill;
    private TextureRect _phaseIcon;
    private Label _phaseLabel;
    private Label _nightLabel;

    // --- Top-right score ---
    private TextureRect _scoreIcon;
    private Label _scoreLabel;
    private Label _fpsLabel;

    // --- Bottom-center: weapon quick bar ---
    private HBoxContainer _weaponBar;
    private readonly NinePatchRect[] _weaponSlotFrames = new NinePatchRect[Player.MaxWeaponSlots];
    private readonly TextureRect[] _weaponSlotIcons = new TextureRect[Player.MaxWeaponSlots];
    private readonly Label[] _weaponSlotLabels = new Label[Player.MaxWeaponSlots];
    private readonly Label[] _weaponSlotLevels = new Label[Player.MaxWeaponSlots];

    // --- Bottom-center: passive souvenir bar ---
    private HBoxContainer _passiveBar;
    private readonly NinePatchRect[] _passiveSlotFrames = new NinePatchRect[Player.MaxPassiveSlots];
    private readonly Label[] _passiveSlotLabels = new Label[Player.MaxPassiveSlots];

    // --- Bottom-left: inventory ---
    private TextureRect _woodIcon;
    private Label _woodLabel;
    private TextureRect _stoneIcon;
    private Label _stoneLabel;
    private TextureRect _metalIcon;
    private Label _metalLabel;
    private Control _capacityBarContainer;
    private ColorRect _capacityBarFill;
    private ColorRect _capacityBarBg;
    private Label _capacityLabel;

    // --- Dawn summary ---
    private PanelContainer _dawnSummary;
    private Label _dawnSummaryLabel;

    // --- Minimap ---
    private Minimap _minimap;

    // --- Interact hint ---
    private Label _interactHint;

    // --- Compass ---
    private PanelContainer _compassPanel;
    private Node2D _compassArrowRoot;
    private Polygon2D _compassArrowHead;
    private Polygon2D _compassArrowTail;
    private Label _compassDistanceLabel;

    // --- Biome ---
    private Label _biomeLabel;
    private WorldSetup _worldSetupRef;
    private string _lastBiomeName;
    private float _biomeUpdateTimer;

    // --- State ---
    private EventBus _eventBus;
    private GroupCache _groupCache;
    private PlayerProgression _progression;
    private DayNightCycle _dayNightCycle;
    private Player _compassPlayer;
    private Node2D _mapCenterAnchor;
    private float _fpsUpdateTimer;
    private float _interactHintUpdateTimer;
    private float _currentHpRatio = 1f;

    private const float FpsUpdateInterval = 0.25f;
    private const float InteractHintUpdateInterval = 0.25f;
    private const float BiomeUpdateInterval = 0.5f;

    // Palette from charte graphique
    private static readonly Color PalBlackDeep = new(0x1A / 255f, 0x1A / 255f, 0x2E / 255f);
    private static readonly Color PalBlackBlue = new(0x16 / 255f, 0x21 / 255f, 0x3E / 255f);
    private static readonly Color PalGrayDark = new(0x3A / 255f, 0x35 / 255f, 0x35 / 255f);
    private static readonly Color PalGrayWarm = new(0x6B / 255f, 0x61 / 255f, 0x61 / 255f);
    private static readonly Color PalGrayLight = new(0x9E / 255f, 0x94 / 255f, 0x94 / 255f);
    private static readonly Color PalWhiteOff = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    private static readonly Color PalGoldFoyer = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);
    private static readonly Color PalOrangeFlame = new(0xE0 / 255f, 0x7B / 255f, 0x39 / 255f);
    private static readonly Color PalRedBlood = new(0xC4 / 255f, 0x43 / 255f, 0x2B / 255f);
    private static readonly Color PalCyanEssence = new(0x5E / 255f, 0xC4 / 255f, 0xC4 / 255f);
    private static readonly Color PalVioletMist = new(0x4A / 255f, 0x30 / 255f, 0x66 / 255f);

    // Day/night bar colors
    private static readonly Color DayBarColor = PalGoldFoyer;
    private static readonly Color DuskBarColor = PalVioletMist;
    private static readonly Color NightBarColor = PalRedBlood;
    private static readonly Color DawnBarColor = PalWhiteOff;

    // Tier colors
    private static readonly Color TierT1 = PalGrayLight;
    private static readonly Color TierT2 = new(0.4f, 0.7f, 1f);
    private static readonly Color TierT3 = PalOrangeFlame;
    private static readonly Color TierT4 = new(0.6f, 0.4f, 1f);
    private static readonly Color TierT5 = PalGoldFoyer;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groupCache = GetNodeOrNull<GroupCache>("/root/GroupCache");
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.XpGained += OnXpChanged;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.ScoreChanged += OnScoreChanged;
        _eventBus.DayPhaseChanged += OnDayPhaseChanged;
        _eventBus.NightSummary += OnNightSummary;
        _eventBus.InventoryChanged += OnInventoryChanged;
        _eventBus.WeaponInventoryChanged += OnWeaponInventoryChanged;
        _eventBus.WeaponUpgraded += OnWeaponUpgraded;
        _eventBus.PassiveSouvenirSlotsChanged += OnPassiveSlotsChanged;

        // Root container : tous les éléments du HUD sont enfants de ce Control.
        // On le dimensionne à viewport/scale et on applique Scale dessus,
        // ce qui fait que les anchors (0-1) correspondent aux bords réels de l'écran.
        _hudRoot = new Control();
        _hudRoot.Name = "HudRoot";
        _hudRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_hudRoot);
        ApplyScale();
        GetViewport().SizeChanged += OnViewportResized;

        BuildTopBar();
        BuildVitals();
        BuildScoreArea();
        BuildWeaponBar();
        BuildPassiveBar();
        BuildInventoryPanel();
        CreateDawnSummaryPanel();
        CreateMinimap();
        CreateInteractHint();
        CreateCompassWidget();
        ResolveCompassTargets();
    }

    /// <summary>Change la taille du HUD au runtime. Valeur recommandée : 1.0–2.5.</summary>
    public void SetHudScale(float scale)
    {
        HudScale = scale;
    }

    private void ApplyScale()
    {
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        if (viewport.X < 1 || viewport.Y < 1)
            viewport = new Vector2(1920, 1080);

        // Auto-scale : on cible ReferenceResolution logique (640×360).
        // Ex. viewport 1920×1080 → scale = 3.0, viewport 1280×720 → scale = 2.0
        float effectiveScale = _hudScale;
        if (effectiveScale < 0.5f) // 0 = auto
        {
            float scaleX = viewport.X / ReferenceResolution.X;
            float scaleY = viewport.Y / ReferenceResolution.Y;
            effectiveScale = Mathf.Min(scaleX, scaleY);
            effectiveScale = Mathf.Max(effectiveScale, 1f);
        }

        _hudRoot.Position = Vector2.Zero;
        _hudRoot.Size = viewport / effectiveScale;
        _hudRoot.Scale = new Vector2(effectiveScale, effectiveScale);
        _hudRoot.PivotOffset = Vector2.Zero;
    }

    private void OnViewportResized()
    {
        if (_hudRoot != null)
            ApplyScale();
    }

    public override void _Process(double delta)
    {
        _fpsUpdateTimer += (float)delta;
        if (_fpsUpdateTimer >= FpsUpdateInterval)
        {
            _fpsUpdateTimer = 0f;
            _fpsLabel.Text = $"{Engine.GetFramesPerSecond()}";
        }

        if (_dayNightCycle != null)
            UpdateDayNightBar(_dayNightCycle.PhaseProgress);

        if (Engine.GetProcessFrames() % 2 == 0)
            UpdateCompass();

        _interactHintUpdateTimer += (float)delta;
        if (_interactHintUpdateTimer >= InteractHintUpdateInterval)
        {
            _interactHintUpdateTimer = 0f;
            UpdateInteractHint();
        }

        _biomeUpdateTimer += (float)delta;
        if (_biomeUpdateTimer >= BiomeUpdateInterval)
        {
            _biomeUpdateTimer = 0f;
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
            _eventBus.DayPhaseChanged -= OnDayPhaseChanged;
            _eventBus.NightSummary -= OnNightSummary;
            _eventBus.InventoryChanged -= OnInventoryChanged;
            _eventBus.WeaponInventoryChanged -= OnWeaponInventoryChanged;
            _eventBus.WeaponUpgraded -= OnWeaponUpgraded;
            _eventBus.PassiveSouvenirSlotsChanged -= OnPassiveSlotsChanged;
        }

        if (GetViewport() != null)
            GetViewport().SizeChanged -= OnViewportResized;
    }

    public void SetProgression(PlayerProgression progression) => _progression = progression;
    public void SetDayNightCycle(DayNightCycle cycle) => _dayNightCycle = cycle;
    public void SetCompassTargets(Player player, Node2D mapCenterAnchor)
    {
        _compassPlayer = player;
        _mapCenterAnchor = mapCenterAnchor;
    }

    // ==================== BUILD UI ====================

    private static Label MakeLabel(string text, int size, Color color, bool shadow = true)
    {
        Label label = new();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        if (shadow)
        {
            label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.85f));
            label.AddThemeConstantOverride("shadow_offset_x", 1);
            label.AddThemeConstantOverride("shadow_offset_y", 1);
        }
        return label;
    }

    private static TextureRect MakeIcon(string path, int size = 16)
    {
        TextureRect icon = new();
        icon.CustomMinimumSize = new Vector2(size, size);
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        if (ResourceLoader.Exists(path))
            icon.Texture = GD.Load<Texture2D>(path);
        return icon;
    }

    private static ColorRect MakeColorBar(Color color, Vector2 size)
    {
        ColorRect rect = new();
        rect.Color = color;
        rect.CustomMinimumSize = size;
        rect.Size = size;
        return rect;
    }

    // --- Top bar: day/night progress ---
    private void BuildTopBar()
    {
        _dayNightContainer = new Control();
        _dayNightContainer.AnchorLeft = 0.15f;
        _dayNightContainer.AnchorRight = 0.85f;
        _dayNightContainer.AnchorTop = 0f;
        _dayNightContainer.OffsetTop = 4;
        _dayNightContainer.OffsetBottom = 14;
        _hudRoot.AddChild(_dayNightContainer);

        _dayNightBg = MakeColorBar(PalBlackDeep with { A = 0.7f }, new Vector2(0, 10));
        _dayNightBg.AnchorRight = 1f;
        _dayNightBg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _dayNightContainer.AddChild(_dayNightBg);

        _dayNightFill = MakeColorBar(DayBarColor, new Vector2(0, 10));
        _dayNightFill.AnchorTop = 0f;
        _dayNightFill.AnchorBottom = 1f;
        _dayNightFill.AnchorLeft = 0f;
        _dayNightFill.AnchorRight = 0f;
        _dayNightFill.OffsetLeft = 1;
        _dayNightFill.OffsetTop = 1;
        _dayNightFill.OffsetBottom = -1;
        _dayNightContainer.AddChild(_dayNightFill);

        // Phase icon (left of bar)
        _phaseIcon = MakeIcon("res://assets/ui/hud/hud_icon_sun.png", 14);
        _phaseIcon.AnchorLeft = 0.15f;
        _phaseIcon.AnchorTop = 0f;
        _phaseIcon.OffsetLeft = -20;
        _phaseIcon.OffsetTop = 2;
        _phaseIcon.OffsetRight = _phaseIcon.OffsetLeft + 14;
        _phaseIcon.OffsetBottom = _phaseIcon.OffsetTop + 14;
        _hudRoot.AddChild(_phaseIcon);

        // Phase label centered below bar
        _phaseLabel = MakeLabel("Jour", 10, PalGoldFoyer);
        _phaseLabel.AnchorLeft = 0.5f;
        _phaseLabel.AnchorRight = 0.5f;
        _phaseLabel.OffsetLeft = -30;
        _phaseLabel.OffsetRight = 30;
        _phaseLabel.OffsetTop = 16;
        _phaseLabel.OffsetBottom = 28;
        _phaseLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _hudRoot.AddChild(_phaseLabel);

        // Night label (right of bar)
        _nightLabel = MakeLabel("", 10, PalGrayLight);
        _nightLabel.AnchorLeft = 0.85f;
        _nightLabel.OffsetLeft = 4;
        _nightLabel.OffsetRight = 50;
        _nightLabel.OffsetTop = 2;
        _nightLabel.OffsetBottom = 14;
        _hudRoot.AddChild(_nightLabel);

        // Biome label (below phase label)
        _biomeLabel = MakeLabel("", 8, PalGrayWarm);
        _biomeLabel.AnchorLeft = 0.5f;
        _biomeLabel.AnchorRight = 0.5f;
        _biomeLabel.OffsetLeft = -50;
        _biomeLabel.OffsetRight = 50;
        _biomeLabel.OffsetTop = 27;
        _biomeLabel.OffsetBottom = 37;
        _biomeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _hudRoot.AddChild(_biomeLabel);
    }

    private void UpdateDayNightBar(float progress)
    {
        float barWidth = _dayNightContainer.Size.X - 2;
        _dayNightFill.OffsetRight = _dayNightFill.OffsetLeft + barWidth * Mathf.Clamp(progress, 0f, 1f);
    }

    // --- Top-left vitals: heart + HP bar + level star + XP ---
    private void BuildVitals()
    {
        float baseY = 22;

        // Heart icon
        _heartIcon = MakeIcon("res://assets/ui/hud/hud_icon_heart.png", 14);
        _heartIcon.OffsetLeft = 6;
        _heartIcon.OffsetTop = baseY;
        _heartIcon.OffsetRight = 20;
        _heartIcon.OffsetBottom = baseY + 14;
        _hudRoot.AddChild(_heartIcon);

        // HP bar (custom drawn)
        float hpBarX = 22;
        float hpBarW = 100;
        float hpBarH = 10;
        _hpBarContainer = new Control();
        _hpBarContainer.OffsetLeft = hpBarX;
        _hpBarContainer.OffsetTop = baseY + 2;
        _hpBarContainer.OffsetRight = hpBarX + hpBarW;
        _hpBarContainer.OffsetBottom = baseY + 2 + hpBarH;
        _hudRoot.AddChild(_hpBarContainer);

        _hpBarBg = MakeColorBar(PalBlackDeep with { A = 0.8f }, new Vector2(hpBarW, hpBarH));
        _hpBarContainer.AddChild(_hpBarBg);

        _hpBarFill = MakeColorBar(PalRedBlood, new Vector2(hpBarW - 2, hpBarH - 2));
        _hpBarFill.Position = new Vector2(1, 1);
        _hpBarContainer.AddChild(_hpBarFill);

        // HP border (1px outline)
        ColorRect hpBorderTop = MakeColorBar(PalGrayWarm with { A = 0.6f }, new Vector2(hpBarW, 1));
        _hpBarContainer.AddChild(hpBorderTop);
        ColorRect hpBorderBot = MakeColorBar(PalGrayWarm with { A = 0.6f }, new Vector2(hpBarW, 1));
        hpBorderBot.Position = new Vector2(0, hpBarH - 1);
        _hpBarContainer.AddChild(hpBorderBot);
        ColorRect hpBorderLeft = MakeColorBar(PalGrayWarm with { A = 0.6f }, new Vector2(1, hpBarH));
        _hpBarContainer.AddChild(hpBorderLeft);
        ColorRect hpBorderRight = MakeColorBar(PalGrayWarm with { A = 0.6f }, new Vector2(1, hpBarH));
        hpBorderRight.Position = new Vector2(hpBarW - 1, 0);
        _hpBarContainer.AddChild(hpBorderRight);

        _hpValueLabel = MakeLabel("100/100", 8, PalWhiteOff);
        _hpValueLabel.Position = new Vector2(2, -1);
        _hpValueLabel.Size = new Vector2(hpBarW - 4, hpBarH);
        _hpValueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _hpValueLabel.VerticalAlignment = VerticalAlignment.Center;
        _hpBarContainer.AddChild(_hpValueLabel);

        // Level row
        float levelY = baseY + 16;
        _levelIcon = MakeIcon("res://assets/ui/hud/hud_icon_level.png", 14);
        _levelIcon.OffsetLeft = 6;
        _levelIcon.OffsetTop = levelY;
        _levelIcon.OffsetRight = 20;
        _levelIcon.OffsetBottom = levelY + 14;
        _hudRoot.AddChild(_levelIcon);

        _levelLabel = MakeLabel("1", 10, PalCyanEssence);
        _levelLabel.OffsetLeft = 22;
        _levelLabel.OffsetTop = levelY;
        _levelLabel.OffsetRight = 40;
        _levelLabel.OffsetBottom = levelY + 14;
        _hudRoot.AddChild(_levelLabel);

        // XP bar
        float xpBarW = 80;
        float xpBarH = 6;
        _xpBarContainer = new Control();
        _xpBarContainer.OffsetLeft = 42;
        _xpBarContainer.OffsetTop = levelY + 4;
        _xpBarContainer.OffsetRight = 42 + xpBarW;
        _xpBarContainer.OffsetBottom = levelY + 4 + xpBarH;
        _hudRoot.AddChild(_xpBarContainer);

        _xpBarBg = MakeColorBar(PalBlackDeep with { A = 0.8f }, new Vector2(xpBarW, xpBarH));
        _xpBarContainer.AddChild(_xpBarBg);

        _xpBarFill = MakeColorBar(PalCyanEssence, new Vector2(0, xpBarH - 2));
        _xpBarFill.Position = new Vector2(1, 1);
        _xpBarContainer.AddChild(_xpBarFill);

        // XP border
        ColorRect xpBorder = MakeColorBar(PalGrayDark with { A = 0.6f }, new Vector2(xpBarW, 1));
        _xpBarContainer.AddChild(xpBorder);
        ColorRect xpBorderBot = MakeColorBar(PalGrayDark with { A = 0.6f }, new Vector2(xpBarW, 1));
        xpBorderBot.Position = new Vector2(0, xpBarH - 1);
        _xpBarContainer.AddChild(xpBorderBot);

        // FPS (debug, small, bottom-left)
        _fpsLabel = MakeLabel("0", 8, PalGrayWarm with { A = 0.5f });
        _fpsLabel.OffsetLeft = 6;
        _fpsLabel.OffsetTop = levelY + 18;
        _fpsLabel.OffsetRight = 40;
        _fpsLabel.OffsetBottom = levelY + 30;
        _hudRoot.AddChild(_fpsLabel);
    }

    // --- Top-right: score ---
    private void BuildScoreArea()
    {
        _scoreIcon = MakeIcon("res://assets/ui/hud/hud_icon_score.png", 14);
        _scoreIcon.AnchorLeft = 1f;
        _scoreIcon.AnchorRight = 1f;
        _scoreIcon.OffsetLeft = -64;
        _scoreIcon.OffsetTop = 22;
        _scoreIcon.OffsetRight = -50;
        _scoreIcon.OffsetBottom = 36;
        _hudRoot.AddChild(_scoreIcon);

        _scoreLabel = MakeLabel("0", 12, PalGoldFoyer);
        _scoreLabel.AnchorLeft = 1f;
        _scoreLabel.AnchorRight = 1f;
        _scoreLabel.OffsetLeft = -48;
        _scoreLabel.OffsetTop = 21;
        _scoreLabel.OffsetRight = -6;
        _scoreLabel.OffsetBottom = 37;
        _scoreLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _hudRoot.AddChild(_scoreLabel);
    }

    // --- Bottom-center: weapon quick bar ---
    private void BuildWeaponBar()
    {
        _weaponBar = new HBoxContainer();
        _weaponBar.AnchorLeft = 0.5f;
        _weaponBar.AnchorRight = 0.5f;
        _weaponBar.AnchorTop = 1f;
        _weaponBar.AnchorBottom = 1f;

        float slotSize = 26;
        float totalWidth = Player.MaxWeaponSlots * (slotSize + 3);
        _weaponBar.OffsetLeft = -totalWidth / 2;
        _weaponBar.OffsetRight = totalWidth / 2;
        _weaponBar.OffsetTop = -58;
        _weaponBar.OffsetBottom = -32;
        _weaponBar.AddThemeConstantOverride("separation", 3);
        _weaponBar.Alignment = BoxContainer.AlignmentMode.Center;
        _hudRoot.AddChild(_weaponBar);

        Texture2D slotEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_empty.png");
        Texture2D slotFilledTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_filled.png");

        for (int i = 0; i < Player.MaxWeaponSlots; i++)
        {
            Control slotRoot = new();
            slotRoot.CustomMinimumSize = new Vector2(slotSize, slotSize);

            // Slot frame (NinePatch)
            NinePatchRect frame = new();
            frame.Texture = slotEmptyTex;
            frame.PatchMarginLeft = 4;
            frame.PatchMarginRight = 4;
            frame.PatchMarginTop = 4;
            frame.PatchMarginBottom = 4;
            frame.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            slotRoot.AddChild(frame);

            // Weapon icon (centered in slot)
            TextureRect weaponIcon = new();
            float iconSize = 20;
            weaponIcon.CustomMinimumSize = new Vector2(iconSize, iconSize);
            weaponIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            weaponIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            weaponIcon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            weaponIcon.Position = new Vector2((slotSize - iconSize) / 2, (slotSize - iconSize) / 2 - 1);
            weaponIcon.Size = new Vector2(iconSize, iconSize);
            weaponIcon.Visible = false;
            slotRoot.AddChild(weaponIcon);

            // Level indicator (small, bottom-right corner)
            Label lvlLabel = MakeLabel("", 7, PalGoldFoyer);
            lvlLabel.Position = new Vector2(slotSize - 12, slotSize - 11);
            lvlLabel.Size = new Vector2(12, 11);
            lvlLabel.HorizontalAlignment = HorizontalAlignment.Right;
            slotRoot.AddChild(lvlLabel);

            _weaponSlotFrames[i] = frame;
            _weaponSlotIcons[i] = weaponIcon;
            _weaponSlotLevels[i] = lvlLabel;
            _weaponBar.AddChild(slotRoot);
        }
    }

    // --- Bottom-center: passive souvenir bar (below weapons) ---
    private void BuildPassiveBar()
    {
        _passiveBar = new HBoxContainer();
        _passiveBar.AnchorLeft = 0.5f;
        _passiveBar.AnchorRight = 0.5f;
        _passiveBar.AnchorTop = 1f;
        _passiveBar.AnchorBottom = 1f;

        float passiveSize = 18;
        float totalWidth = Player.MaxPassiveSlots * (passiveSize + 3);
        _passiveBar.OffsetLeft = -totalWidth / 2;
        _passiveBar.OffsetRight = totalWidth / 2;
        _passiveBar.OffsetTop = -30;
        _passiveBar.OffsetBottom = -16;
        _passiveBar.AddThemeConstantOverride("separation", 3);
        _passiveBar.Alignment = BoxContainer.AlignmentMode.Center;
        _hudRoot.AddChild(_passiveBar);

        Texture2D passiveEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive.png");

        for (int i = 0; i < Player.MaxPassiveSlots; i++)
        {
            Control slotRoot = new();
            slotRoot.CustomMinimumSize = new Vector2(passiveSize, passiveSize);

            NinePatchRect frame = new();
            frame.Texture = passiveEmptyTex;
            frame.PatchMarginLeft = 3;
            frame.PatchMarginRight = 3;
            frame.PatchMarginTop = 3;
            frame.PatchMarginBottom = 3;
            frame.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            slotRoot.AddChild(frame);

            Label nameLabel = MakeLabel("", 6, PalGrayWarm);
            nameLabel.Position = new Vector2(0, passiveSize - 1);
            nameLabel.Size = new Vector2(passiveSize, 10);
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            nameLabel.ClipText = true;
            slotRoot.AddChild(nameLabel);

            _passiveSlotFrames[i] = frame;
            _passiveSlotLabels[i] = nameLabel;
            _passiveBar.AddChild(slotRoot);
        }
    }

    // --- Bottom-left: inventory ---
    private void BuildInventoryPanel()
    {
        float panelW = 100;
        float panelH = 66;
        Control panel = new();
        panel.AnchorTop = 1f;
        panel.AnchorBottom = 1f;
        panel.OffsetLeft = 8;
        panel.OffsetRight = 8 + panelW;
        panel.OffsetTop = -panelH - 6;
        panel.OffsetBottom = -6;
        _hudRoot.AddChild(panel);

        // Semi-transparent background
        ColorRect bg = MakeColorBar(PalBlackDeep with { A = 0.55f }, new Vector2(panelW, panelH));
        panel.AddChild(bg);

        // Border
        ColorRect borderTop = MakeColorBar(PalGrayDark with { A = 0.5f }, new Vector2(panelW, 1));
        panel.AddChild(borderTop);
        ColorRect borderLeft = MakeColorBar(PalGrayDark with { A = 0.5f }, new Vector2(1, panelH));
        panel.AddChild(borderLeft);
        ColorRect borderRight = MakeColorBar(PalGrayDark with { A = 0.5f }, new Vector2(1, panelH));
        borderRight.Position = new Vector2(panelW - 1, 0);
        panel.AddChild(borderRight);
        ColorRect borderBot = MakeColorBar(PalGrayDark with { A = 0.5f }, new Vector2(panelW, 1));
        borderBot.Position = new Vector2(0, panelH - 1);
        panel.AddChild(borderBot);

        float rowH = 18;
        float iconSize = 14;

        // Wood row
        _woodIcon = MakeIcon("res://assets/sprites/items/item_bois.png", (int)iconSize);
        _woodIcon.Position = new Vector2(5, 5);
        _woodIcon.Size = new Vector2(iconSize, iconSize);
        panel.AddChild(_woodIcon);

        _woodLabel = MakeLabel("0", 10, new Color(0.75f, 0.58f, 0.2f));
        _woodLabel.Position = new Vector2(22, 5);
        _woodLabel.Size = new Vector2(34, 14);
        panel.AddChild(_woodLabel);

        // Stone row
        _stoneIcon = MakeIcon("res://assets/sprites/items/item_pierre.png", (int)iconSize);
        _stoneIcon.Position = new Vector2(5, 5 + rowH);
        _stoneIcon.Size = new Vector2(iconSize, iconSize);
        panel.AddChild(_stoneIcon);

        _stoneLabel = MakeLabel("0", 10, new Color(0.72f, 0.66f, 0.52f));
        _stoneLabel.Position = new Vector2(22, 5 + rowH);
        _stoneLabel.Size = new Vector2(34, 14);
        panel.AddChild(_stoneLabel);

        // Metal row
        _metalIcon = MakeIcon("res://assets/sprites/items/item_metal.png", (int)iconSize);
        _metalIcon.Position = new Vector2(5, 5 + rowH * 2);
        _metalIcon.Size = new Vector2(iconSize, iconSize);
        panel.AddChild(_metalIcon);

        _metalLabel = MakeLabel("0", 10, new Color(0.56f, 0.67f, 0.75f));
        _metalLabel.Position = new Vector2(22, 5 + rowH * 2);
        _metalLabel.Size = new Vector2(34, 14);
        panel.AddChild(_metalLabel);

        // Capacity bar (right side of inventory)
        float capX = 60;
        float capW = 32;
        float capH = 48;

        _capacityBarContainer = new Control();
        _capacityBarContainer.Position = new Vector2(capX, 6);
        _capacityBarContainer.Size = new Vector2(capW, capH);
        panel.AddChild(_capacityBarContainer);

        // Vertical capacity bar (fills from bottom)
        _capacityBarBg = MakeColorBar(PalBlackDeep with { A = 0.6f }, new Vector2(capW, capH));
        _capacityBarContainer.AddChild(_capacityBarBg);

        _capacityBarFill = MakeColorBar(PalGrayWarm with { A = 0.5f }, new Vector2(capW - 2, 0));
        _capacityBarFill.Position = new Vector2(1, capH - 1);
        _capacityBarContainer.AddChild(_capacityBarFill);

        // Capacity border
        ColorRect capBorder = MakeColorBar(PalGrayDark with { A = 0.4f }, new Vector2(capW, 1));
        _capacityBarContainer.AddChild(capBorder);
        ColorRect capBorderB = MakeColorBar(PalGrayDark with { A = 0.4f }, new Vector2(capW, 1));
        capBorderB.Position = new Vector2(0, capH - 1);
        _capacityBarContainer.AddChild(capBorderB);

        _capacityLabel = MakeLabel("0", 9, PalGrayLight);
        _capacityLabel.Position = new Vector2(capX, capH + 10);
        _capacityLabel.Size = new Vector2(capW, 14);
        _capacityLabel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.AddChild(_capacityLabel);
    }

    // ==================== CALLBACKS ====================

    private void OnPlayerDamaged(float currentHp, float maxHp)
    {
        UpdateHpDisplay(currentHp, maxHp);
    }

    private void UpdateHpDisplay(float currentHp, float maxHp)
    {
        float clampedMaxHp = Mathf.Max(1f, maxHp);
        float clampedHp = Mathf.Clamp(currentHp, 0f, clampedMaxHp);
        _currentHpRatio = clampedHp / clampedMaxHp;

        // Update fill width
        float barInnerW = _hpBarContainer.Size.X - 2;
        _hpBarFill.Size = new Vector2(barInnerW * _currentHpRatio, _hpBarFill.Size.Y);

        // Update fill color based on HP
        Color hpColor;
        if (_currentHpRatio < 0.25f)
            hpColor = PalRedBlood;
        else if (_currentHpRatio < 0.5f)
            hpColor = PalOrangeFlame;
        else
            hpColor = new Color(0.3f, 0.75f, 0.3f); // healthy green
        _hpBarFill.Color = hpColor;

        _hpValueLabel.Text = $"{Mathf.RoundToInt(clampedHp)}/{Mathf.RoundToInt(clampedMaxHp)}";
    }

    private void OnXpChanged(float _amount)
    {
        if (_progression == null) return;
        float ratio = _progression.XpToNextLevel > 0 ? _progression.CurrentXp / _progression.XpToNextLevel : 0f;
        float barInnerW = _xpBarContainer.Size.X - 2;
        _xpBarFill.Size = new Vector2(barInnerW * Mathf.Clamp(ratio, 0f, 1f), _xpBarFill.Size.Y);
    }

    private void OnLevelUp(int newLevel)
    {
        _levelLabel.Text = $"{newLevel}";
        OnXpChanged(0);
    }

    private void OnScoreChanged(int newScore)
    {
        _scoreLabel.Text = newScore.ToString();
    }

    private void OnDayPhaseChanged(string phase)
    {
        string phaseText = phase switch
        {
            "Day" => "Jour",
            "Dusk" => "Crépuscule",
            "Night" => "Nuit",
            "Dawn" => "Aube",
            _ => phase
        };
        _phaseLabel.Text = phaseText;

        // Update phase icon
        string iconPath = phase is "Night" or "Dusk"
            ? "res://assets/ui/hud/hud_icon_void.png"
            : "res://assets/ui/hud/hud_icon_sun.png";
        if (ResourceLoader.Exists(iconPath))
            _phaseIcon.Texture = GD.Load<Texture2D>(iconPath);

        // Phase label color
        Color phaseColor = phase switch
        {
            "Day" => PalGoldFoyer,
            "Dusk" => PalVioletMist,
            "Night" => PalRedBlood,
            "Dawn" => PalWhiteOff,
            _ => PalGoldFoyer
        };
        _phaseLabel.AddThemeColorOverride("font_color", phaseColor);

        if (_dayNightCycle != null && _dayNightCycle.CurrentNight > 0)
            _nightLabel.Text = $"N{_dayNightCycle.CurrentNight}";

        if (phase == "Day")
            HideDawnSummary();

        // Animate bar color
        Color barColor = phase switch
        {
            "Day" => DayBarColor,
            "Dusk" => DuskBarColor,
            "Night" => NightBarColor,
            "Dawn" => DawnBarColor,
            _ => DayBarColor
        };
        Tween tween = CreateTween();
        tween.TweenProperty(_dayNightFill, "color", barColor, 1f);
    }

    private void OnInventoryChanged(string resourceId, int newAmount)
    {
        switch (resourceId)
        {
            case "wood":
                _woodLabel.Text = newAmount.ToString();
                break;
            case "stone":
                _stoneLabel.Text = newAmount.ToString();
                break;
            case "metal":
                _metalLabel.Text = newAmount.ToString();
                break;
        }
        UpdateCapacityDisplay();
    }

    private void UpdateCapacityDisplay()
    {
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode == null) return;

        Base.Inventory inventory = playerNode.GetNodeOrNull<Base.Inventory>("Inventory");
        if (inventory == null) return;

        int total = inventory.TotalCount;
        int max = Base.Inventory.MaxCapacity;
        float ratio = max > 0 ? (float)total / max : 0f;

        // Vertical fill from bottom
        float barH = _capacityBarContainer.Size.Y - 2;
        float fillH = barH * Mathf.Clamp(ratio, 0f, 1f);
        _capacityBarFill.Position = new Vector2(1, _capacityBarContainer.Size.Y - 1 - fillH);
        _capacityBarFill.Size = new Vector2(_capacityBarFill.Size.X, fillH);

        Color barColor = ratio >= 1f
            ? PalRedBlood with { A = 0.7f }
            : ratio >= 0.8f
                ? PalOrangeFlame with { A = 0.6f }
                : PalGrayWarm with { A = 0.5f };
        _capacityBarFill.Color = barColor;

        _capacityLabel.Text = $"{total}/{max}";
    }

    // --- Weapon slots ---
    private void OnWeaponInventoryChanged()
    {
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is not Player player) return;

        System.Collections.Generic.IReadOnlyList<WeaponInstance> weapons = player.WeaponSlots;
        Texture2D slotEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_empty.png");
        Texture2D slotFilledTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_filled.png");

        for (int i = 0; i < Player.MaxWeaponSlots; i++)
        {
            if (i < weapons.Count)
            {
                WeaponInstance weapon = weapons[i];
                _weaponSlotFrames[i].Texture = slotFilledTex;

                // Tint frame border by tier
                Color tierColor = GetTierColor(weapon.Tier);
                _weaponSlotFrames[i].Modulate = tierColor;

                // Load weapon sprite
                LoadWeaponIcon(i, weapon.Sprite);

                // Level
                int fragLevel = player.GetWeaponFragmentLevel(weapon.Id);
                _weaponSlotLevels[i].Text = fragLevel > 1 ? $"{fragLevel}" : "";
            }
            else
            {
                _weaponSlotFrames[i].Texture = slotEmptyTex;
                _weaponSlotFrames[i].Modulate = Colors.White;
                _weaponSlotIcons[i].Visible = false;
                _weaponSlotLevels[i].Text = "";
            }
        }
    }

    private void LoadWeaponIcon(int slotIndex, string spritePath)
    {
        TextureRect icon = _weaponSlotIcons[slotIndex];
        if (string.IsNullOrEmpty(spritePath))
        {
            icon.Visible = false;
            return;
        }

        string resPath = spritePath.StartsWith("res://") ? spritePath : $"res://{spritePath}";
        if (!ResourceLoader.Exists(resPath))
        {
            icon.Visible = false;
            return;
        }

        Texture2D texture = GD.Load<Texture2D>(resPath);
        if (texture != null)
        {
            icon.Texture = texture;
            icon.Visible = true;
        }
        else
        {
            icon.Visible = false;
        }
    }

    private void OnWeaponUpgraded(string _weaponId, int _slotIndex, string _stat, int _newLevel)
    {
        OnWeaponInventoryChanged();
    }

    // --- Passive slots ---
    private void OnPassiveSlotsChanged()
    {
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is not Player player) return;

        System.Collections.Generic.IReadOnlyList<ActivePassiveSouvenir> passives = player.PassiveSlots;
        Texture2D passiveEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive.png");
        Texture2D passiveFilledTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive_filled.png");

        for (int i = 0; i < Player.MaxPassiveSlots; i++)
        {
            if (i < passives.Count)
            {
                ActivePassiveSouvenir passive = passives[i];
                _passiveSlotFrames[i].Texture = passiveFilledTex;
                _passiveSlotFrames[i].Modulate = passive.Data.IconColor;
                _passiveSlotLabels[i].Text = passive.Level > 1 ? $"{passive.Level}" : "";
            }
            else
            {
                _passiveSlotFrames[i].Texture = passiveEmptyTex;
                _passiveSlotFrames[i].Modulate = Colors.White;
                _passiveSlotLabels[i].Text = "";
            }
        }
    }

    private static Color GetTierColor(int tier)
    {
        return tier switch
        {
            1 => TierT1,
            2 => TierT2,
            3 => TierT3,
            4 => TierT4,
            5 => TierT5,
            _ => TierT1
        };
    }

    // ==================== MINIMAP ====================

    private void CreateMinimap()
    {
        // Minimap désactivée temporairement pour les performances
    }

    public void InitializeMinimap(WorldSetup worldSetup, FogOfWar fogOfWar)
    {
        // Minimap désactivée temporairement pour les performances
    }

    // ==================== INTERACT HINT ====================

    private void CreateInteractHint()
    {
        _interactHint = MakeLabel("", 13, PalWhiteOff);
        _interactHint.AnchorLeft = 0.5f;
        _interactHint.AnchorRight = 0.5f;
        _interactHint.AnchorTop = 0.72f;
        _interactHint.OffsetLeft = -100;
        _interactHint.OffsetRight = 100;
        _interactHint.HorizontalAlignment = HorizontalAlignment.Center;
        _interactHint.Visible = false;
        _hudRoot.AddChild(_interactHint);
    }

    private void UpdateInteractHint()
    {
        Node playerNode = _groupCache?.GetPlayer() ?? GetTree().GetFirstNodeInGroup("player");
        if (playerNode is not Player player || player.IsDead)
        {
            _interactHint.Visible = false;
            return;
        }

        if (player.IsHarvesting)
        {
            _interactHint.Visible = false;
            return;
        }

        float interactRange = player.InteractRange;
        Vector2 playerPos = player.GlobalPosition;

        foreach (Node node in _groupCache?.GetStructures() ?? GetTree().GetNodesInGroup("structures"))
        {
            if (node is Base.Structure structure && !structure.IsDestroyed && structure.HpRatio < 1f)
            {
                float dist = playerPos.DistanceTo(structure.GlobalPosition);
                if (dist < interactRange)
                {
                    int hpPercent = (int)(structure.HpRatio * 100);
                    _interactHint.Text = $"[E] Réparer ({hpPercent}%)";
                    _interactHint.Visible = true;
                    return;
                }
            }
        }

        foreach (Node node in _groupCache?.GetResources() ?? GetTree().GetNodesInGroup("resources"))
        {
            if (node is Base.ResourceNode res && !res.IsExhausted)
            {
                float dist = playerPos.DistanceTo(res.GlobalPosition);
                if (dist < interactRange)
                {
                    _interactHint.Text = $"[E] Récolter {GetResourceDisplayName(res.ResourceId)}";
                    _interactHint.Visible = true;
                    return;
                }
            }
        }

        _interactHint.Visible = false;
    }

    private void UpdateBiomeLabel()
    {
        if (_worldSetupRef == null || !IsInstanceValid(_worldSetupRef))
            _worldSetupRef = GetNodeOrNull<WorldSetup>("/root/Main");

        if (_worldSetupRef == null)
            return;

        Node playerNode = _groupCache?.GetPlayer() ?? GetTree().GetFirstNodeInGroup("player");
        if (playerNode is not Player player)
            return;

        BiomeData biome = _worldSetupRef.GetBiomeAt(player.GlobalPosition);
        string biomeName = biome?.Name ?? "";

        if (biomeName != _lastBiomeName)
        {
            _lastBiomeName = biomeName;
            _biomeLabel.Text = biomeName;
        }
    }

    // ==================== COMPASS ====================

    private void CreateCompassWidget()
    {
        _compassPanel = new PanelContainer();
        _compassPanel.AnchorLeft = 0.5f;
        _compassPanel.AnchorRight = 0.5f;
        _compassPanel.OffsetLeft = -32;
        _compassPanel.OffsetRight = 32;
        _compassPanel.OffsetTop = 38;
        _compassPanel.OffsetBottom = 80;

        StyleBoxFlat style = new();
        style.BgColor = PalBlackDeep with { A = 0.55f };
        style.ContentMarginLeft = 4;
        style.ContentMarginRight = 4;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 2;
        // No rounded corners — pixel art!
        _compassPanel.AddThemeStyleboxOverride("panel", style);

        VBoxContainer vbox = new();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 2);
        _compassPanel.AddChild(vbox);

        Control arrowHost = new();
        arrowHost.CustomMinimumSize = new Vector2(24f, 24f);
        arrowHost.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        arrowHost.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        vbox.AddChild(arrowHost);

        _compassArrowRoot = new Node2D();
        _compassArrowRoot.Position = new Vector2(12f, 12f);
        arrowHost.AddChild(_compassArrowRoot);

        _compassArrowHead = new Polygon2D();
        _compassArrowHead.Color = PalGoldFoyer with { A = 0.95f };
        _compassArrowHead.Polygon = new Vector2[]
        {
            new(0f, -9f),
            new(6f, 3f),
            new(-6f, 3f)
        };
        _compassArrowRoot.AddChild(_compassArrowHead);

        _compassArrowTail = new Polygon2D();
        _compassArrowTail.Color = PalGoldFoyer with { A = 0.85f };
        _compassArrowTail.Polygon = new Vector2[]
        {
            new(-1.5f, 3f),
            new(1.5f, 3f),
            new(1.5f, 9f),
            new(-1.5f, 9f)
        };
        _compassArrowRoot.AddChild(_compassArrowTail);

        _compassDistanceLabel = MakeLabel("", 7, PalGrayLight, shadow: false);
        _compassDistanceLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_compassDistanceLabel);

        _hudRoot.AddChild(_compassPanel);
    }

    private void ResolveCompassTargets()
    {
        if (_compassPlayer == null || !IsInstanceValid(_compassPlayer))
            _compassPlayer = GetTree().GetFirstNodeInGroup("player") as Player;

        if (_mapCenterAnchor == null || !IsInstanceValid(_mapCenterAnchor))
        {
            Node sceneRoot = GetTree().CurrentScene;
            _mapCenterAnchor = sceneRoot?.GetNodeOrNull<Node2D>("Foyer");
        }
    }

    private void UpdateCompass()
    {
        if (_compassPanel == null || _compassArrowRoot == null) return;

        if (_compassPlayer == null || _mapCenterAnchor == null || !IsInstanceValid(_compassPlayer) || !IsInstanceValid(_mapCenterAnchor))
        {
            ResolveCompassTargets();
            if (_compassPlayer == null || _mapCenterAnchor == null || !IsInstanceValid(_compassPlayer) || !IsInstanceValid(_mapCenterAnchor))
            {
                _compassPanel.Visible = false;
                return;
            }
        }

        _compassPanel.Visible = true;
        Vector2 toCenter = _mapCenterAnchor.GlobalPosition - _compassPlayer.GlobalPosition;
        float distance = toCenter.Length();

        if (distance < 12f)
        {
            _compassArrowRoot.Rotation = 0f;
            _compassArrowHead.Visible = false;
            _compassArrowTail.Visible = false;
            _compassDistanceLabel.Text = "";
            return;
        }

        Vector2 direction = toCenter / distance;
        float angle = Mathf.Atan2(direction.Y, direction.X);
        _compassArrowHead.Visible = true;
        _compassArrowTail.Visible = true;
        _compassArrowRoot.Rotation = angle + (Mathf.Pi * 0.5f);
        _compassDistanceLabel.Text = $"{Mathf.RoundToInt(distance)}";
    }

    // ==================== DAWN SUMMARY ====================

    private void CreateDawnSummaryPanel()
    {
        _dawnSummary = new PanelContainer();
        _dawnSummary.AnchorLeft = 0.5f;
        _dawnSummary.AnchorRight = 0.5f;
        _dawnSummary.AnchorTop = 0.3f;
        _dawnSummary.AnchorBottom = 0.3f;
        _dawnSummary.OffsetLeft = -120;
        _dawnSummary.OffsetRight = 120;
        _dawnSummary.OffsetTop = 0;
        _dawnSummary.OffsetBottom = 70;
        _dawnSummary.Visible = false;

        StyleBoxFlat style = new();
        style.BgColor = PalBlackDeep with { A = 0.85f };
        style.BorderColor = PalGoldFoyer with { A = 0.6f };
        style.BorderWidthBottom = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthRight = 1;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        _dawnSummary.AddThemeStyleboxOverride("panel", style);

        _dawnSummaryLabel = MakeLabel("", 13, PalGoldFoyer);
        _dawnSummaryLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _dawnSummaryLabel.VerticalAlignment = VerticalAlignment.Center;
        _dawnSummary.AddChild(_dawnSummaryLabel);

        _hudRoot.AddChild(_dawnSummary);
    }

    private void OnNightSummary(int nightNumber, int kills, int score)
    {
        _dawnSummaryLabel.Text = $"Nuit {nightNumber} survécue !\nKills : {kills}  —  Score : +{score}";
        _dawnSummary.Visible = true;
        _dawnSummary.Modulate = Colors.White;

        Tween tween = CreateTween();
        tween.TweenInterval(4f);
        tween.TweenProperty(_dawnSummary, "modulate:a", 0f, 1.5f);
        tween.TweenCallback(Callable.From(HideDawnSummary));
    }

    private void HideDawnSummary() => _dawnSummary.Visible = false;

    private static string GetResourceDisplayName(string id)
    {
        return id switch
        {
            "wood" => "Bois",
            "stone" => "Pierre",
            "metal" => "Métal",
            _ => id
        };
    }
}
