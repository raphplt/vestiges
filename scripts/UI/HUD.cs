using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// HUD de run, allégé (plan 24 L2) : l'écran montre la vie, le temps, le score et les éliminations, l'XP et la minimap.
/// Haut-gauche : niveau et PV. Haut-centre : le temps, violet pendant une Résurgence, la Série, et le nom du biome quand
/// on y entre. Haut-droite : score, éliminations, Essence. Bas : armes et objets, puis la barre d'XP sur toute la largeur.
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
    // Case des chiffres de la colonne du haut droit : les icônes restent alignées quand un nombre s'allonge.
    private const float TallyValueWidth = 32f;
    private const float ScoreAreaWidth = 260f;
    // Icônes 32×32 à ×2 (HUD dessiné à ×2, icône de 32 unités) : 2 unités de cadre autour.
    private const float WeaponSlotSize = 36f;
    // Les armes reposent juste au-dessus de la barre d'XP ; les Écussons au-dessus d'elles (plan 37 R3).
    private const float WeaponBarBottom = 6f + XpBar.BarHeight;
    private const float BiomeShowSec = 2f;
    private const float BiomeFadeSec = 0.5f;
    private const float FpsUpdateInterval = 0.25f;
    private const float BiomeUpdateInterval = 0.5f;

    // --- Vitals ---
    private VitalsDisplay _vitals;
    private XpBar _xpBar;
    private EssenceFlights _essenceFlights;
    private StreakHud _streakHud;
    private float _essencePulse;
    private static readonly Color EssencePulseModulate = new(1.8f, 1.8f, 1.8f, 1f);

    // --- Temps ---
    private Label _biomeLabel;
    private Label _timeLabel;
    private Label _perilLabel;
    private float _perilPulse;
    private float _biomeAge = float.MaxValue;
    private Tween _timeColorTween;

    // --- Éliminations, la mesure de la run (DECISIONS §155) ---
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
    private readonly HudSlotLock[] _weaponSlotLocks = new HudSlotLock[Player.MaxWeaponSlots];
    private HudInventoryBar _inventoryBar;
    private Texture2D _slotEmptyTex;
    private Texture2D _slotFilledTex;

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
    private float _runSeconds;
    private RunTracker _runTracker;
    private int _shownSeconds = -1;

    // Palette de la charte graphique
    private static readonly Color PalGrayWarm = new(0x6B / 255f, 0x61 / 255f, 0x61 / 255f);
    private static readonly Color PalGrayLight = new(0x9E / 255f, 0x94 / 255f, 0x94 / 255f);
    private static readonly Color PalWhiteOff = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    private static readonly Color PalGold = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);
    private static readonly Color PalOrangeFlame = new(0xE0 / 255f, 0x7B / 255f, 0x39 / 255f);
    private static readonly Color ResurgenceViolet = new(0.70f, 0.55f, 0.95f);
    // Réserve de Débordement prête : la case de l'arme s'éclaire en bleu pâle, comme le chiffre du coup renforcé.
    private static readonly Color OverflowSlotTint = new(0.75f, 1.05f, 1.45f);
    // Case fermée tant que le Campement ne l'a pas ouverte (plan 36) : cadre éteint, cadenas par-dessus.
    private static readonly Color LockedSlotTint = new(0.45f, 0.43f, 0.48f, 0.7f);
    private readonly System.Collections.Generic.HashSet<string> _overflowReady = new();

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
        _eventBus.PerilChanged += OnPerilChanged;
        _eventBus.RunPhaseChanged += OnRunPhaseChanged;
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.EssenceMultiplierChanged += OnEssenceMultiplierChanged;
        _eventBus.EssenceChanged += OnEssenceChanged;
        _eventBus.WeaponInventoryChanged += OnWeaponInventoryChanged;
        _eventBus.WeaponUpgraded += OnWeaponUpgraded;
        _eventBus.SpecializationGaugeChanged += OnSpecializationGauge;

        _slotEmptyTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_empty.png");
        _slotFilledTex = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_filled.png");

        // Sous tout le HUD : la vignette de blessure ne couvre jamais une jauge ni un texte.
        AddChild(new LowHealthVignette { Name = "LowHealthVignette" });
        AddChild(new HurtVignette { Name = "HurtVignette" });
        _hudRoot = new Control { Name = "HudRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_hudRoot);
        ApplyScale();
        GetViewport().SizeChanged += OnViewportResized;

        BuildVitals();
        _hudRoot.AddChild(new HudLootFlight(WeaponSlotIconOf, id => _inventoryBar?.ItemIconOf(id)) { Name = "LootFlight" });
        _essenceFlights = new EssenceFlights { Name = "EssenceFlights" };
        _essenceFlights.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _essenceFlights.Setup(EssenceTarget, () => _essencePulse = 1f);
        BuildRunClock();
        _streakHud = new StreakHud { Name = "StreakHud" };
        _hudRoot.AddChild(_streakHud);
        BuildScoreArea();
        _hudRoot.AddChild(_essenceFlights);
        BuildWeaponBar();
        BuildInventoryBar();
        _xpBar = new XpBar { Name = "XpBar" };
        _hudRoot.AddChild(_xpBar);

        _hudRoot.AddChild(new ChestPointers { Name = "ChestPointers" });
        _hudRoot.AddChild(new DashHint { Name = "DashHint" });
        _hudRoot.AddChild(new Minimap { Name = "Minimap" });
        _hudRoot.AddChild(new LandObjective { Name = "LandObjective" });
        RunEventHud eventHud = new() { Name = "RunEventHud" };
        _hudRoot.AddChild(eventHud);
        _hudRoot.AddChild(new BossHealthBar { Name = "BossHealthBar" });
        _hudRoot.AddChild(new QuestToast { Name = "QuestToast" });
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
#if TOOLS
        if (_progression != null)
            _progression.DevelopmentLevelChanged -= RefreshDevelopmentLevel;
#endif
        if (_eventBus != null)
        {
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
            _eventBus.PlayerShieldChanged -= OnShieldChanged;
            _eventBus.XpGained -= OnXpChanged;
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.PerilChanged -= OnPerilChanged;
            _eventBus.RunPhaseChanged -= OnRunPhaseChanged;
            _eventBus.EnemyKilled -= OnEnemyKilled;
            _eventBus.EssenceMultiplierChanged -= OnEssenceMultiplierChanged;
            _eventBus.EssenceChanged -= OnEssenceChanged;
            _eventBus.WeaponInventoryChanged -= OnWeaponInventoryChanged;
            _eventBus.WeaponUpgraded -= OnWeaponUpgraded;
            _eventBus.SpecializationGaugeChanged -= OnSpecializationGauge;
        }

        if (GetViewport() != null)
            GetViewport().SizeChanged -= OnViewportResized;
    }

    public void SetProgression(PlayerProgression progression)
    {
#if TOOLS
        if (_progression != null)
            _progression.DevelopmentLevelChanged -= RefreshDevelopmentLevel;
#endif
        _progression = progression;
#if TOOLS
        if (_progression != null)
            _progression.DevelopmentLevelChanged += RefreshDevelopmentLevel;
#endif
    }

#if TOOLS
    private void RefreshDevelopmentLevel()
    {
        _vitals.SetLevel(_progression.CurrentLevel);
        _xpBar.SetRatio(_progression.XpProgress, false);
    }
#endif

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
        _streakHud?.SetStreak(player.Streak);
        UpdateHpDisplay(player.CurrentHp, player.EffectiveMaxHp);
        OnShieldChanged(player.Shield, player.MaxShield);
    }

    // ==================== CONSTRUCTION ====================

    private static Label MakeLabel(string text, int size, Color color, int outline = 3)
    {
        Label label = new() { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        UITheme.SetCompactText(label, size);
        label.AddThemeColorOverride("font_color", color);
        if (outline > 0)
        {
            label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
            label.AddThemeConstantOverride("outline_size", outline);
        }
        return label;
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
    /// Le temps en haut au centre, la Série dessous (<see cref="StreakHud"/>), puis le nom du biome qui passe quand on y
    /// entre.
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

        _biomeLabel = MakeLabel("", 10, PalGrayLight, 3);
        UITheme.SetCompactTitle(_biomeLabel, 16);
        _biomeLabel.Position = new Vector2(-120f, StreakHud.Bottom + 2f);
        _biomeLabel.Size = new Vector2(240f, 14f);
        _biomeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _biomeLabel.Modulate = Colors.Transparent;
        anchor.AddChild(_biomeLabel);
    }

    private void OnPerilChanged(int peril)
    {
        _perilLabel.Text = peril.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _perilPulse = 1f;
    }

    /// <summary>
    /// Haut droit, sans plaque (plan 37 R2) : les éliminations en grand, la seule mesure de la run (DECISIONS §155), puis
    /// une colonne Essence, Péril (plan 41 R2). Le Péril y reste toujours, 0 compris (plan 34 G0 : il se lit sans pause).
    /// Chaque texte porte son contour sombre.
    /// </summary>
    private void BuildScoreArea()
    {
        Control anchor = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        anchor.AnchorLeft = 1f;
        anchor.AnchorRight = 1f;
        _hudRoot.AddChild(anchor);

        // Le crâne seul dit ce qu'on compte : pas de mot (retour de Raphaël, 13 octobre), doublé au pixel près.
        HBoxContainer killsRow = RightAlignedRow(anchor, PlateMargin - 4f, 6);
        killsRow.AddChild(new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/hud/plan25/kill_skull.png"), TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(20f, 20f), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        _killsLabel = MakeLabel("0", 22, PalGold, 4);
        killsRow.AddChild(_killsLabel);

        // Une colonne sous les éliminations (plan 41 R2) : une valeur par ligne, même taille, chiffres alignés sur le
        // bord droit. Seules les icônes portent une couleur ; les éliminations restent la seule valeur dorée.
        _essenceLabel = AddTallyRow(anchor, PlateMargin + 26f, "res://assets/ui/hud/plan25/essence_crystal.png");
        _perilLabel = AddTallyRow(anchor, PlateMargin + 46f, "res://assets/ui/hud/plan25/peril_stele.png");
    }

    /// <summary>Ligne de la colonne : icône de 10 px puis chiffre, le chiffre dans une case de largeur fixe.</summary>
    private static Label AddTallyRow(Control anchor, float top, string iconPath)
    {
        HBoxContainer row = RightAlignedRow(anchor, top, 6);
        row.AddChild(new TextureRect
        {
            Texture = GD.Load<Texture2D>(iconPath), TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepCentered, CustomMinimumSize = new Vector2(10f, 10f),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        Label value = MakeLabel("0", 15, PalWhiteOff, 4);
        value.HorizontalAlignment = HorizontalAlignment.Right;
        value.CustomMinimumSize = new Vector2(TallyValueWidth, 0f);
        row.AddChild(value);
        return value;
    }

    /// <summary>Rangée collée au bord droit : ses éléments poussent vers la gauche quand un nombre s'allonge.</summary>
    private static HBoxContainer RightAlignedRow(Control anchor, float top, int separation)
    {
        HBoxContainer row = new()
        {
            MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End,
            Position = new Vector2(-ScoreAreaWidth - PlateMargin, top), Size = new Vector2(ScoreAreaWidth, 24f),
        };
        row.AddThemeConstantOverride("separation", separation);
        anchor.AddChild(row);
        return row;
    }

    private void BuildWeaponBar()
    {
        const float slotSize = WeaponSlotSize;
        HBoxContainer bar = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AnchorLeft = 0.5f;
        bar.AnchorRight = 0.5f;
        bar.AnchorTop = 1f;
        bar.AnchorBottom = 1f;
        float totalWidth = Player.MaxWeaponSlots * (slotSize + 3);
        bar.OffsetLeft = -totalWidth / 2;
        bar.OffsetRight = totalWidth / 2;
        bar.OffsetTop = -WeaponBarBottom - slotSize;
        bar.OffsetBottom = -WeaponBarBottom;
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

            HudSlotLock slotLock = new() { Visible = false };
            slotLock.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            slotRoot.AddChild(slotLock);

            _weaponSlotFrames[i] = frame;
            _weaponSlotIcons[i] = icon;
            _weaponSlotLevels[i] = level;
            _weaponSlotLocks[i] = slotLock;
            bar.AddChild(slotRoot);
        }
    }

    /// <summary>
    /// Écussons au-dessus des armes, objets en colonne à gauche (plan 35, plan 37 R3), dans leur propre composant, qui
    /// couvre l'écran sans prendre la souris.
    /// </summary>
    private void BuildInventoryBar()
    {
        HudInventoryBar bar = new() { Name = "InventoryBar", BadgeShelfBottom = WeaponBarBottom + WeaponSlotSize + 5f };
        _inventoryBar = bar;
        bar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _hudRoot.AddChild(bar);
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
        string text = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
                _weaponSlotLocks[i].Visible = false;
            }
            else
            {
                bool locked = i >= player.WeaponSlotLimit;
                _weaponSlotFrames[i].Texture = _slotEmptyTex;
                _weaponSlotFrames[i].Modulate = locked ? LockedSlotTint : Colors.White;
                _weaponSlotIcons[i].Visible = false;
                _weaponSlotLevels[i].Text = "";
                _weaponSlotLocks[i].Visible = locked;
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
