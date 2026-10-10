using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// HUD de run, allégé (plan 24 L2) : l'écran montre la vie, le temps, le score et les éliminations, l'XP et la minimap.
/// Haut-gauche : niveau et PV. Haut-centre : le temps seul, violet pendant une Résurgence, et le nom du biome quand on
/// y entre. Haut-droite : score, éliminations, Essence. Bas : armes et objets, puis la barre d'XP sur toute la largeur.
/// Aucune annonce écrite : les Résurgences se lisent par le présage et la couleur du temps. Les PV sont doublés
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
    private const float ScorePlateWidth = 150f;
    private const float BiomeShowSec = 2f;
    private const float BiomeFadeSec = 0.5f;
    private const float FpsUpdateInterval = 0.25f;
    private const float BiomeUpdateInterval = 0.5f;
    private const float ScoreTickInterval = 0.05f;

    // --- Vitals ---
    private VitalsDisplay _vitals;
    private XpBar _xpBar;
    private EssenceFlights _essenceFlights;
    private float _essencePulse;
    private static readonly Color EssencePulseModulate = new(1.8f, 1.8f, 1.8f, 1f);

    // --- Temps ---
    private Label _biomeLabel;
    private Label _timeLabel;
    private Label _perilLabel;
    private float _perilPulse;
    private float _biomeAge = float.MaxValue;
    private Tween _timeColorTween;

    // --- Score ---
    private Label _scoreLabel;
    private Label _killsLabel;
    private int _kills;
    private Label _essenceLabel;
    private int _essence;
    private float _essenceMultiplier = 1f;
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
    private EssenceTracker _essenceTracker;
    private Player _player;
    private WorldSetup _worldSetup;
    private string _lastBiomeName;
    private float _fpsTimer;
    private float _biomeTimer;
    private float _scoreTimer;
    private float _runSeconds;
    private RunTracker _runTracker;
    private int _shownSeconds = -1;
    private int _targetScore;
    private Label _gainLabel;
    private int _pendingGain;
    private float _gainAge = float.MaxValue;
    private const int MinShownGain = 5;
    private const float GainGroupSec = 0.6f;
    private const float GainShowSec = 0.9f;
    private const float GainFadeSec = 0.4f;
    private float _shownScore;

    // Palette de la charte graphique
    private static readonly Color PalGrayWarm = new(0x6B / 255f, 0x61 / 255f, 0x61 / 255f);
    private static readonly Color PalGrayLight = new(0x9E / 255f, 0x94 / 255f, 0x94 / 255f);
    private static readonly Color PalWhiteOff = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    private static readonly Color PalGold = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);
    private static readonly Color PalOrangeFlame = new(0xE0 / 255f, 0x7B / 255f, 0x39 / 255f);
    private static readonly Color PalCyanEssence = new(0x5E / 255f, 0xC4 / 255f, 0xC4 / 255f);
    private static readonly Color ResurgenceViolet = new(0.70f, 0.55f, 0.95f);
    // Réserve de Débordement prête : la case de l'arme s'éclaire en bleu pâle, comme le chiffre du coup renforcé.
    private static readonly Color OverflowSlotTint = new(0.75f, 1.05f, 1.45f);
    private readonly System.Collections.Generic.HashSet<string> _overflowReady = new();
    private static readonly Color PlateColor = new(0.04f, 0.045f, 0.08f, 0.82f);
    private static readonly Color PlateBorder = new(0.83f, 0.66f, 0.26f, 0.35f);

    public override void _Ready()
    {
        DevelopmentBadge.AttachTo(this);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groupCache = GetNodeOrNull<GroupCache>("/root/GroupCache");
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        _runTracker = GetNodeOrNull<RunTracker>("../RunTracker");
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.PlayerShieldChanged += OnShieldChanged;
        _eventBus.XpGained += OnXpChanged;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.ScoreChanged += OnScoreChanged;
        _eventBus.PerilChanged += OnPerilChanged;
        _eventBus.RunPhaseChanged += OnRunPhaseChanged;
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.EssenceMultiplierChanged += OnEssenceMultiplierChanged;
        _eventBus.EssenceChanged += OnEssenceChanged;
        _eventBus.WeaponInventoryChanged += OnWeaponInventoryChanged;
        _eventBus.WeaponUpgraded += OnWeaponUpgraded;
        _eventBus.PassiveSouvenirSlotsChanged += OnPassiveSlotsChanged;
        _eventBus.SpecializationGaugeChanged += OnSpecializationGauge;

        _slotEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_empty.png");
        _slotFilledTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_filled.png");
        _passiveEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive.png");
        _passiveFilledTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive_filled.png");

        // Sous tout le HUD : la vignette de blessure ne couvre jamais une jauge ni un texte.
        AddChild(new HurtVignette { Name = "HurtVignette" });
        _hudRoot = new Control { Name = "HudRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_hudRoot);
        ApplyScale();
        GetViewport().SizeChanged += OnViewportResized;

        BuildVitals();
        _hudRoot.AddChild(new HudLootFlight(WeaponSlotIconOf) { Name = "LootFlight" });
        _hudRoot.AddChild(new KillStreakDisplay { Name = "KillStreak", Position = new Vector2(PlateMargin + 4f, PlateMargin + _vitals.Size.Y + 6f) });
        _essenceFlights = new EssenceFlights { Name = "EssenceFlights" };
        _essenceFlights.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _essenceFlights.Setup(EssenceTarget, () => _essencePulse = 1f);
        BuildRunClock();
        BuildScoreArea();
        _hudRoot.AddChild(_essenceFlights);
        BuildWeaponBar();
        BuildPassiveBar();
        _xpBar = new XpBar { Name = "XpBar" };
        _hudRoot.AddChild(_xpBar);

        _hudRoot.AddChild(new ChestPointers { Name = "ChestPointers" });
        _hudRoot.AddChild(new Minimap { Name = "Minimap" });
        RunEventHud eventHud = new() { Name = "RunEventHud" };
        _hudRoot.AddChild(eventHud);
        _hudRoot.AddChild(new BossHealthBar { Name = "BossHealthBar" });
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

        // Même horloge que le score et le bilan (plan 02 lot A) ; compteur propre seulement hors run complète.
        if (_runTracker != null)
            _runSeconds = _runTracker.RunDurationSeconds;
        else if (_gameManager == null || _gameManager.CurrentState == GameManager.GameState.Run)
            _runSeconds += dt;
        int seconds = (int)_runSeconds;
        if (seconds != _shownSeconds)
        {
            _shownSeconds = seconds;
            _timeLabel.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        UpdateScoreCounter(dt);
        UpdateGainLabel(dt);
        if (_perilPulse > 0f)
        {
            _perilPulse = Mathf.Max(0f, _perilPulse - dt * 2f);
            _perilLabel.Modulate = Colors.White.Lerp(EssencePulseModulate, _perilPulse);
        }
        if (_essencePulse > 0f)
        {
            _essencePulse = Mathf.Max(0f, _essencePulse - dt * 6f);
            _essenceLabel.Modulate = Colors.White.Lerp(EssencePulseModulate, _essencePulse);
        }
        UpdateBiomeFade(dt);

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
            _eventBus.PlayerShieldChanged -= OnShieldChanged;
            _eventBus.XpGained -= OnXpChanged;
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.ScoreChanged -= OnScoreChanged;
            _eventBus.PerilChanged -= OnPerilChanged;
            _eventBus.RunPhaseChanged -= OnRunPhaseChanged;
            _eventBus.EnemyKilled -= OnEnemyKilled;
            _eventBus.EssenceMultiplierChanged -= OnEssenceMultiplierChanged;
            _eventBus.EssenceChanged -= OnEssenceChanged;
            _eventBus.WeaponInventoryChanged -= OnWeaponInventoryChanged;
            _eventBus.WeaponUpgraded -= OnWeaponUpgraded;
            _eventBus.PassiveSouvenirSlotsChanged -= OnPassiveSlotsChanged;
            _eventBus.SpecializationGaugeChanged -= OnSpecializationGauge;
        }

        if (GetViewport() != null)
            GetViewport().SizeChanged -= OnViewportResized;
    }

    public void SetProgression(PlayerProgression progression) => _progression = progression;

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
        OnShieldChanged(player.Shield, player.MaxShield);
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

    private void BuildVitals()
    {
        _vitals = new VitalsDisplay { Name = "Vitals", Position = new Vector2(PlateMargin, PlateMargin) };
        _hudRoot.AddChild(_vitals);

        _fpsLabel = MakeLabel("", 8, PalGrayWarm, 2);
        _fpsLabel.AnchorTop = 1f;
        _fpsLabel.AnchorBottom = 1f;
        _fpsLabel.OffsetLeft = 6;
        _fpsLabel.OffsetTop = -14 - XpBar.BarHeight;
        _fpsLabel.OffsetRight = 60;
        _fpsLabel.OffsetBottom = -3 - XpBar.BarHeight;
        _hudRoot.AddChild(_fpsLabel);
    }

    /// <summary>
    /// Le temps en haut au centre, le Péril à sa droite dès qu'il dépasse 0 (plan 28 : le joueur le monte lui-même), et
    /// le nom du biome qui passe dessous quand on y entre.
    /// </summary>
    private void BuildRunClock()
    {
        Control anchor = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        anchor.AnchorLeft = 0.5f;
        anchor.AnchorRight = 0.5f;
        _hudRoot.AddChild(anchor);

        _timeLabel = MakeLabel("00:00", 18, PalWhiteOff, 4);
        _timeLabel.Position = new Vector2(-60f, PlateMargin - 2f);
        _timeLabel.Size = new Vector2(120f, 24f);
        _timeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        anchor.AddChild(_timeLabel);

        _perilLabel = MakeLabel("", 10, PlayerSheet.PerilColor, 3);
        _perilLabel.Position = new Vector2(36f, PlateMargin + 4f);
        _perilLabel.Size = new Vector2(90f, 14f);
        _perilLabel.Visible = false;
        anchor.AddChild(_perilLabel);

        _biomeLabel = MakeLabel("", 10, PalGrayLight, 3);
        _biomeLabel.Position = new Vector2(-120f, PlateMargin + 22f);
        _biomeLabel.Size = new Vector2(240f, 14f);
        _biomeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _biomeLabel.Modulate = Colors.Transparent;
        anchor.AddChild(_biomeLabel);
    }

    private void OnPerilChanged(int peril)
    {
        _perilLabel.Visible = peril > 0;
        _perilLabel.Text = string.Format(Tr("UI_HUD_PERIL"), peril);
        _perilPulse = 1f;
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

        _scoreLabel = MakeLabel("0", 18, PalGold, 4);
        _scoreLabel.Position = new Vector2(8, 0);
        _scoreLabel.Size = new Vector2(ScorePlateWidth - 16, 24);
        _scoreLabel.HorizontalAlignment = HorizontalAlignment.Right;
        content.AddChild(_scoreLabel);

        // Gains rapprochés regroupés (plan 02 lot A) : « +120 » à gauche de la plaque, puis s'efface.
        _gainLabel = MakeLabel("", 13, PalGold, 4);
        _gainLabel.Position = new Vector2(-ScorePlateWidth - PlateMargin - 88f, PlateMargin + 10f);
        _gainLabel.Size = new Vector2(80f, 20f);
        _gainLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _gainLabel.Modulate = new Color(1f, 1f, 1f, 0f);
        anchor.AddChild(_gainLabel);

        // Éliminations à gauche, Essence à droite, sous le score.
        PixelSkull skull = new() { Position = new Vector2(7, 26) };
        content.AddChild(skull);
        _killsLabel = MakeLabel("0", 10, PalWhiteOff);
        _killsLabel.Position = new Vector2(19, 22);
        _killsLabel.Size = new Vector2(56, 15);
        content.AddChild(_killsLabel);

        _essenceLabel = MakeLabel("", 10, PalCyanEssence);
        _essenceLabel.Position = new Vector2(70, 22);
        _essenceLabel.Size = new Vector2(ScorePlateWidth - 78, 15);
        _essenceLabel.HorizontalAlignment = HorizontalAlignment.Right;
        content.AddChild(_essenceLabel);

        // Quêtes de run repliées en sceaux, sous la plaque (plan 24 A2).
        anchor.AddChild(new RunQuestSeals { Name = "QuestSeals", Position = new Vector2(-PlateMargin, PlateMargin + 46f) });
    }

    private void BuildWeaponBar()
    {
        // Icônes 32×32 à ×2 (HUD dessiné à ×2, icône de 32 unités) : 2 unités de cadre autour.
        const float slotSize = 36f;
        HBoxContainer bar = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AnchorLeft = 0.5f;
        bar.AnchorRight = 0.5f;
        bar.AnchorTop = 1f;
        bar.AnchorBottom = 1f;
        float totalWidth = Player.MaxWeaponSlots * (slotSize + 3);
        bar.OffsetLeft = -totalWidth / 2;
        bar.OffsetRight = totalWidth / 2;
        bar.OffsetTop = -26 - XpBar.BarHeight - slotSize;
        bar.OffsetBottom = -26 - XpBar.BarHeight;
        bar.AddThemeConstantOverride("separation", 3);
        bar.Alignment = BoxContainer.AlignmentMode.Center;
        _hudRoot.AddChild(bar);

        for (int i = 0; i < Player.MaxWeaponSlots; i++)
        {
            Control slotRoot = new() { CustomMinimumSize = new Vector2(slotSize, slotSize) };
            NinePatchRect frame = MakeSlotFrame(_slotEmptyTex, 4);
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

            Label level = MakeLabel("", 9, PalGold, 3);
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
        bar.OffsetTop = -23 - XpBar.BarHeight;
        bar.OffsetBottom = -5 - XpBar.BarHeight;
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

    private void OnShieldChanged(float shield, float maxShield) => _vitals?.SetShield(shield, maxShield);

    private void UpdateHpDisplay(float currentHp, float maxHp) => _vitals.SetHealth(currentHp, maxHp);

    private void OnXpChanged(float amount)
    {
        if (_progression == null)
            return;
        // Chaque orbe qui arrive fait briller la barre (plan 02 J3).
        float ratio = _progression.XpToNextLevel > 0 ? _progression.CurrentXp / _progression.XpToNextLevel : 0f;
        _xpBar.SetRatio(ratio, amount > 0f);
    }

    private void OnLevelUp(int newLevel)
    {
        _vitals.SetLevel(newLevel);
        // La barre éclate en blanc et repart de zéro (plan 02 J4).
        _xpBar.Flash();
        OnXpChanged(0);

    }

    private void OnScoreChanged(int newScore)
    {
        int gain = newScore - _targetScore;
        _targetScore = newScore;
        // Les petites créatures valent peu : seuls les gains notables s'affichent à côté de la plaque.
        if (gain < MinShownGain)
            return;
        _pendingGain = _gainAge < GainGroupSec ? _pendingGain + gain : gain;
        _gainAge = 0f;
        _gainLabel.Text = $"+{_pendingGain:N0}";
        _gainLabel.Modulate = Colors.White;
    }

    private void UpdateGainLabel(float dt)
    {
        if (_gainAge > GainShowSec + GainFadeSec)
            return;
        _gainAge += dt;
        float fade = Mathf.Clamp((_gainAge - GainShowSec) / GainFadeSec, 0f, 1f);
        _gainLabel.Modulate = new Color(1f, 1f, 1f, 1f - fade);
    }

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

    /// <summary>La phase ne s'écrit plus : seul le temps change de couleur (plan 24 A1, A3).</summary>
    private void OnRunPhaseChanged(string oldPhase, string newPhase)
    {
        Color timeColor = newPhase switch
        {
            "Crisis" => ResurgenceViolet,
            "LateGame" => PalOrangeFlame,
            "Endgame" => PalGold,
            _ => PalWhiteOff
        };
        _timeColorTween?.Kill();
        _timeColorTween = CreateTween();
        _timeColorTween.TweenMethod(Callable.From<Color>(color => _timeLabel.AddThemeColorOverride("font_color", color)),
            _timeLabel.GetThemeColor("font_color"), timeColor, 0.8f);

        if (newPhase == "Death")
            DeathSequence.FadeOutLayer(this);
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        _kills++;
        _killsLabel.Text = _kills.ToString("N0");
    }

    /// <summary>Accalmie après une Résurgence (plan 03 lot C) : le compteur d'Essence affiche le multiplicateur, sans annonce.</summary>
    private void OnEssenceMultiplierChanged(float multiplier, float seconds)
    {
        _essenceMultiplier = multiplier;
        OnEssenceChanged(_essence);
        _essencePulse = 1f;
    }

    /// <summary>Arrivée des grains d'Essence : fin du compteur, dans le repère des vols.</summary>
    private Vector2 EssenceTarget()
    {
        Rect2 rect = _essenceLabel.GetGlobalRect();
        return _essenceFlights.GetGlobalTransform().AffineInverse() * new Vector2(rect.End.X - 16f, rect.GetCenter().Y);
    }

    private void OnEssenceChanged(int amount)
    {
        _essence = amount;
        if (_essenceLabel == null)
            return;
        string text = string.Format(Tr("UI_HUD_ESSENCE"), amount);
        _essenceLabel.Text = _essenceMultiplier > 1f ? $"×{_essenceMultiplier:0.#}  {text}" : text;
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
                // Une case masquée par un vol de butin (HudLootFlight) ne reste pas vide si les armes changent de place.
                _weaponSlotIcons[i].Modulate = Colors.White;
                _weaponSlotFrames[i].Texture = _slotFilledTex;
                _weaponSlotFrames[i].Modulate = _overflowReady.Contains(weapon.Id) ? OverflowSlotTint : Colors.White;
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

    private void OnSpecializationGauge(SpecializationGauge gauge)
    {
        if (gauge.Effect != Progression.SpecializationRuntime.OverflowEffect)
            return;
        bool changed = gauge.Value > 0f ? _overflowReady.Add(gauge.Key) : _overflowReady.Remove(gauge.Key);
        if (!changed || ResolvePlayer() is not Player player)
            return;
        for (int i = 0; i < player.WeaponSlots.Count && i < Player.MaxWeaponSlots; i++)
            _weaponSlotFrames[i].Modulate = _overflowReady.Contains(player.WeaponSlots[i].Id) ? OverflowSlotTint : Colors.White;
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

                string iconPath = passive.Data.IconSmall;
                Texture2D iconTex = string.IsNullOrEmpty(iconPath) ? null : GD.Load<Texture2D>(iconPath);
                _passiveSlotIcons[i].Texture = iconTex;
                _passiveSlotIcons[i].Modulate = Colors.White;
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

    /// <summary>Icône de la case qui porte l'arme <paramref name="weaponId"/>, ou nul.</summary>
    private Control WeaponSlotIconOf(string weaponId)
    {
        Player player = ResolvePlayer();
        if (player == null)
            return null;
        System.Collections.Generic.IReadOnlyList<WeaponInstance> weapons = player.WeaponSlots;
        for (int i = 0; i < weapons.Count && i < Player.MaxWeaponSlots; i++)
        {
            if (weapons[i].Id == weaponId)
                return _weaponSlotIcons[i];
        }
        return null;
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
        if (biomeName == _lastBiomeName)
            return;
        _lastBiomeName = biomeName;
        _biomeLabel.Text = biomeName;
        // Déjà affiché (frontière franchie deux fois) : le nouveau nom reste plein, sans repartir du transparent.
        bool showing = _biomeAge < BiomeFadeSec + BiomeShowSec;
        _biomeAge = biomeName.Length == 0 ? float.MaxValue : showing ? BiomeFadeSec : 0f;
    }

    /// <summary>Le nom du biome apparaît, reste deux secondes puis s'efface.</summary>
    private void UpdateBiomeFade(float dt)
    {
        if (_biomeAge > BiomeShowSec + BiomeFadeSec * 2f)
            return;
        _biomeAge += dt;
        float alpha = _biomeAge < BiomeFadeSec
            ? _biomeAge / BiomeFadeSec
            : 1f - Mathf.Clamp((_biomeAge - BiomeFadeSec - BiomeShowSec) / BiomeFadeSec, 0f, 1f);
        _biomeLabel.Modulate = new Color(1f, 1f, 1f, alpha);
    }
}
