using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Score;

namespace Vestiges.UI;

/// <summary>
/// Bilan de l'expédition : score, distance et durée en tête ; portrait, vues Expédition/Équipement et gains.
/// Les colonnes défilent indépendamment. La révélation n'attribue rien : les gains sont enregistrés à la mort.
/// Un premier appui termine la révélation sans activer les boutons ; Rejouer retourne au choix du personnage.
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
    private static readonly Vector2 DesignSize = new(1920f, 1080f);
    private const int ScoreFontSize = 110;
    private const float VeilSec = 0.5f;
    private const float ScoreStart = 0.4f;
    private const float ScoreSec = 0.9f;
    private const float BuildStart = 1.2f;
    private const float TimelineStart = 1.5f;
    private const float TimelineSec = 0.8f;
    private const float GainsStart = 2f;
    // Première carte de gain retournée après l'apparition de la rangée, les suivantes à la file.
    private const float FirstCardDelay = 0.25f;
    private const float CardStagger = 0.22f;
    private const float MinRevealEnd = 2.5f;
    private const float ButtonGuardSec = 0.35f;
    private const float SlideIn = 24f;
    private const float MiddleY = 320f;
    private static readonly Color VeilColor = new(0.025f, 0.02f, 0.05f, 0.93f);

    private EventBus _eventBus;
    private ScoreManager _scoreManager;
    private RunTracker _runTracker;
    private Font _bodyFont;
    private Font _strongFont;
    private Font _boldFont;

    private ColorRect _veil;
    private Control _buttons;
    private Control _root;
    private Label _title;
    private Label _score;
    private Label _distance;
    private Label _distanceCaption;
    private Label _duration;
    private Label _durationCaption;
    private Label _record;
    private Label _detail;
    private Control _middle;
    private TextureRect _portrait;
    private Label _characterName;
    private VBoxContainer _overview;
    private VBoxContainer _equipment;
    private VBoxContainer _facts;
    private ScrollContainer _detailScroll;
    private ScrollContainer _gainsScroll;
    private Button _overviewTab;
    private Button _equipmentTab;
    private bool _equipmentVisible;
    private bool _leaving;
    private Label _timelineCaption;
    private RunTimelineStrip _timeline;
    private Control _gains;
    private Button _restartButton;
    private Button _hubButton;
    private Button _collectionButton;
    private Label _seed;
    // Armes disponibles au départ de la run : celles qui s'y ajoutent au bilan (Souvenir retrouvé) mènent à la Collection.
    private readonly HashSet<string> _weaponsAtStart = new();
    private readonly List<WeaponData> _newWeapons = new();
    private readonly List<string> _questsDone = new();
    private readonly List<EndGainCard> _gainCards = new();

    private bool _showing;
    private float _elapsed;
    private float _buttonsAt = float.MaxValue;
    // Fin de la révélation : après la dernière carte de gain, au plus tôt MinRevealEnd.
    private float _revealEnd = MinRevealEnd;
    private bool _skipped;
    private int _finalScore;
    private float _finalDistance;
    private float _finalDuration;
    private bool _isRecord;
    private BuildSnapshot _build;

    /// <summary>Personnage et souvenirs relevés à la mort : ce que le bilan montre ne dépend plus de la run.</summary>
    private sealed class BuildSnapshot
    {
        public string CharacterId;
        public string CharacterName;
        public readonly List<(string Icon, int Level)> Badges = new();
        public readonly List<(string Icon, string Name, int Copies, string Rarity)> Passives = new();
        public readonly List<(string Icon, string Name)> Perks = new();
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 50;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EntityDied += OnEntityDied;
        _eventBus.QuestCompleted += OnQuestCompleted;
        _weaponsAtStart.UnionWith(AvailableWeaponIds());
        _bodyFont = UITheme.BodyFont;
        _strongFont = UITheme.StrongFont;
        _boldFont = UITheme.BoldFont;
        BuildUI();
        Visible = false;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EntityDied -= OnEntityDied;
            _eventBus.QuestCompleted -= OnQuestCompleted;
        }
        // La fenêtre survit au rechargement de la scène : elle ne doit pas garder ce bilan libéré.
        GetViewport().SizeChanged -= FitToViewport;
    }

    public void SetScoreManager(ScoreManager scoreManager)
    {
        _scoreManager = scoreManager;
    }

    public void SetRunTracker(RunTracker runTracker)
    {
        _runTracker = runTracker;
    }

    // ==============================
    // Construction
    // ==============================

    private void BuildUI()
    {
        _veil = new ColorRect { Color = ChoiceStyle.OverlayColor, MouseFilter = Control.MouseFilterEnum.Stop };
        _veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_veil);
        _root = new Control { Size = DesignSize, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_root);
        MenuSkin.AddBackdrop(_root, subdued: true);

        _title = MakeLabel("FIN D’EXPÉDITION", UITheme.TitleFont, TextRole.Title, UITheme.GoldDim, HorizontalAlignment.Center);
        Place(_title, 0, 34, DesignSize.X, 42);
        _score = MakeLabel("0", _boldFont, TextRole.Display, UITheme.GoldBright, HorizontalAlignment.Center, 4);
        _score.AddThemeFontSizeOverride("font_size", ScoreFontSize);
        Place(_score, 660, 80, 600, 132);
        _distance = MakeLabel("", _boldFont, TextRole.Display, UITheme.TextLight, HorizontalAlignment.Center, 4);
        Place(_distance, 180, 110, 480, 64);
        _distanceCaption = MakeLabel(Tr("UI_END_FARTHEST"), _bodyFont, TextRole.Heading, UITheme.TextDim, HorizontalAlignment.Center, 0);
        Place(_distanceCaption, 180, 176, 480, 34);
        _duration = MakeLabel("", _boldFont, TextRole.Display, UITheme.TextLight, HorizontalAlignment.Center, 4);
        Place(_duration, 1260, 110, 480, 64);
        _durationCaption = MakeLabel(Tr("UI_END_DURATION_CAPTION"), _bodyFont, TextRole.Heading, UITheme.TextDim, HorizontalAlignment.Center, 0);
        Place(_durationCaption, 1260, 176, 480, 34);
        _record = MakeLabel("", _strongFont, TextRole.Heading, UITheme.TextColor, HorizontalAlignment.Center, 0);
        Place(_record, 0, 216, DesignSize.X, 38);
        _detail = MakeLabel("", _bodyFont, TextRole.Subhead, UITheme.TextDim, HorizontalAlignment.Center, 0);
        Place(_detail, 0, 260, DesignSize.X, 30);

        HBoxContainer middle = new();
        middle.AddThemeConstantOverride("separation", 24);
        _middle = middle;
        Place(middle, 80, MiddleY, 1760, 620);
        VBoxContainer character = new() { CustomMinimumSize = new Vector2(260, 0) };
        middle.AddChild(character);
        _portrait = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/menu_portrait.gdshader") } };
        character.AddChild(_portrait);
        _characterName = UITheme.MakeLabel("", TextRole.Heading, UITheme.TextColor, TextWeight.Strong, HorizontalAlignment.Center);
        character.AddChild(_characterName);

        PanelContainer detail = new() { CustomMinimumSize = new Vector2(980, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        detail.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        middle.AddChild(detail);
        VBoxContainer detailContent = new();
        detailContent.AddThemeConstantOverride("separation", 18);
        detail.AddChild(detailContent);
        HBoxContainer tabs = new();
        tabs.AddThemeConstantOverride("separation", 12);
        detailContent.AddChild(tabs);
        _overviewTab = MenuSkin.Button("Expédition", true);
        _equipmentTab = MenuSkin.Button("Équipement");
        _overviewTab.SizeFlagsHorizontal = _equipmentTab.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        tabs.AddChild(_overviewTab);
        tabs.AddChild(_equipmentTab);
        _overviewTab.Pressed += () => SelectDetails(false);
        _equipmentTab.Pressed += () => SelectDetails(true);
        _detailScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.All };
        UITheme.ApplyFocusStyle(_detailScroll);
        detailContent.AddChild(_detailScroll);
        VBoxContainer pages = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _detailScroll.AddChild(pages);
        _overview = new VBoxContainer();
        _overview.AddThemeConstantOverride("separation", 12);
        pages.AddChild(_overview);
        _facts = new VBoxContainer();
        _facts.AddThemeConstantOverride("separation", 12);
        _overview.AddChild(_facts);
        _timelineCaption = UITheme.MakeLabel(Tr("UI_END_TIMELINE"), TextRole.Section, UITheme.GoldBright);
        _overview.AddChild(_timelineCaption);
        _timeline = new RunTimelineStrip { CustomMinimumSize = new Vector2(0, 90), MouseFilter = Control.MouseFilterEnum.Ignore };
        _overview.AddChild(_timeline);
        _equipment = new VBoxContainer { Visible = false };
        _equipment.AddThemeConstantOverride("separation", 18);
        pages.AddChild(_equipment);

        PanelContainer rewards = new() { CustomMinimumSize = new Vector2(450, 0) };
        rewards.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        middle.AddChild(rewards);
        VBoxContainer rewardsContent = new();
        rewardsContent.AddThemeConstantOverride("separation", 18);
        rewards.AddChild(rewardsContent);
        rewardsContent.AddChild(UITheme.MakeLabel("CE QUI DEMEURE", TextRole.Section, UITheme.GoldBright));
        _gainsScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.All };
        UITheme.ApplyFocusStyle(_gainsScroll);
        rewardsContent.AddChild(_gainsScroll);
        _gains = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _gains.AddThemeConstantOverride("separation", 12);
        _gainsScroll.AddChild(_gains);

        HBoxContainer buttons = new() { Alignment = BoxContainer.AlignmentMode.Center };
        _buttons = buttons;
        buttons.AddThemeConstantOverride("separation", 24);
        Place(buttons, 80, 962, 1760, 58);
        _restartButton = MenuSkin.Button(Tr("UI_END_REPLAY"), true);
        _restartButton.CustomMinimumSize = new Vector2(350, 58);
        _restartButton.Pressed += OnRestartPressed;
        buttons.AddChild(_restartButton);
        _hubButton = MenuSkin.Button(Tr("UI_END_HUB"));
        _hubButton.CustomMinimumSize = new Vector2(300, 58);
        _hubButton.Pressed += OnHubPressed;
        buttons.AddChild(_hubButton);
        _collectionButton = MenuSkin.Button(Tr("UI_END_COLLECTION"));
        _collectionButton.Visible = false;
        _collectionButton.Pressed += OnCollectionPressed;
        buttons.AddChild(_collectionButton);
        _restartButton.FocusNeighborRight = _hubButton.GetPath();
        _hubButton.FocusNeighborLeft = _restartButton.GetPath();
        _overviewTab.FocusNeighborBottom = _detailScroll.GetPath();
        _equipmentTab.FocusNeighborBottom = _detailScroll.GetPath();
        _detailScroll.FocusNeighborBottom = _restartButton.GetPath();
        _detailScroll.FocusNeighborRight = _gainsScroll.GetPath();
        _gainsScroll.FocusNeighborLeft = _detailScroll.GetPath();
        _restartButton.FocusNeighborTop = _overviewTab.GetPath();
        _seed = MakeLabel("", _bodyFont, TextRole.Body, UITheme.TextDim, HorizontalAlignment.Left, 0);
        Place(_seed, 80, 1036, 1200, 28);
        GetViewport().SizeChanged += FitToViewport;
        FitToViewport();
    }

    private void SelectDetails(bool equipment)
    {
        _equipmentVisible = equipment;
        _overview.Visible = !equipment;
        _equipment.Visible = equipment;
        MenuSkin.StyleButton(_overviewTab, !equipment);
        MenuSkin.StyleButton(_equipmentTab, equipment);
        _detailScroll.ScrollVertical = 0;
    }

    private void FitToViewport()
    {
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        float scale = Mathf.Min(viewport.X / DesignSize.X, viewport.Y / DesignSize.Y);
        _root.Scale = new Vector2(scale, scale);
        _root.Position = (viewport - DesignSize * scale) / 2f;
    }

    private static Label MakeLabel(string text, Font font, TextRole role, Color color, HorizontalAlignment alignment, int outline = 6)
    {
        Label label = new() { Text = text, HorizontalAlignment = alignment, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", font);
        UITheme.SetTextRole(label, role);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.04f, 0.9f));
        label.AddThemeConstantOverride("outline_size", outline);
        return label;
    }

    private void Place(Control control, float x, float y, float width, float height)
    {
        control.Position = new Vector2(x, y);
        control.Size = new Vector2(width, height);
        _root.AddChild(control);
    }

    // ==============================
    // Mort et relevé
    // ==============================

    private void OnEntityDied(Node entity)
    {
        if (entity is not Player player)
            return;

        _build = Snapshot(player);
        GameManager gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.ChangeState(GameManager.GameState.Death);
        _scoreManager?.SaveEndOfRun();
        _newWeapons.Clear();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            if (!_weaponsAtStart.Contains(weapon.Id) && MetaSaveManager.IsWeaponUnlocked(weapon.Id))
                _newWeapons.Add(weapon);
        }
    }

    private void OnQuestCompleted(string questId) => _questsDone.Add(questId);

    private static BuildSnapshot Snapshot(Player player)
    {
        BuildSnapshot build = new() { CharacterId = player.CharacterId };
        build.CharacterName = CharacterDataLoader.Get(player.CharacterId)?.Name ?? player.CharacterId;
        foreach (ActivePassiveSouvenir badge in player.BadgeSlots)
            build.Badges.Add((badge.Data.Icon, badge.Level));
        foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
            build.Passives.Add((passive.Data.Icon, passive.Data.Name, passive.Copies, passive.Data.Rarity));
        foreach (PerkSpecializationData perk in player.Specializations)
            build.Perks.Add((perk.Icon, perk.Name));
        return build;
    }

    // ==============================
    // Bilan
    // ==============================

    /// <summary>Montre le bilan relevé à la mort ; appelé à la fin de la séquence de mort.</summary>
    public void Reveal()
    {
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        RunRecord record = gm.LastRunData;
        _finalScore = _scoreManager?.CurrentScore ?? 0;
        _isRecord = _scoreManager?.IsNewRecord ?? false;
        _finalDistance = record?.MaxDistanceMeters ?? 0f;
        _finalDuration = record?.RunDurationSec ?? 0f;

        _characterName.Text = _build?.CharacterName ?? "";
        _portrait.Texture = _build != null ? CharacterPortrait.Artwork(_build.CharacterId) ?? CharacterPortrait.Idle(_build.CharacterId) : null;
        SelectDetails(false);
        _record.Text = _isRecord ? Tr("UI_END_NEW_RECORD") : string.Format(Tr("UI_END_BEST"), (_scoreManager?.BestScore ?? 0).ToString("N0"));
        _record.AddThemeColorOverride("font_color", _isRecord ? UITheme.GoldBright : UITheme.TextDim);
        _detail.Text = FormatDetail();
        _seed.Text = gm.EffectiveSeed > 0 ? string.Format(Tr("UI_END_SEED"), gm.EffectiveSeed) : "";
        string saveNotice = !string.IsNullOrEmpty(gm.LastRunSaveError) ? Tr("UI_END_SAVE_FAILED")
            : gm.LastRunHistoryPending ? Tr("UI_END_HISTORY_PENDING") : "";
        if (saveNotice.Length > 0)
            _seed.Text = _seed.Text.Length > 0 ? $"{saveNotice}  ·  {_seed.Text}" : saveNotice;
        _seed.AddThemeColorOverride("font_color", saveNotice.Length > 0 ? UITheme.GoldBright : UITheme.TextVeryDim);

        BuildMiddle(record);
        BuildTimeline();
        BuildGains(gm);

        _score.Text = "0";
        _elapsed = 0f;
        _skipped = false;
        _revealEnd = MinRevealEnd;
        for (int i = 0; i < _gainCards.Count; i++)
            _revealEnd = Mathf.Max(_revealEnd, CardStart(i) + _gainCards[i].Duration);
        _buttonsAt = _revealEnd;
        SetButtonsEnabled(false);
        ApplyReveal();
        Visible = true;
        _showing = true;
        SetProcess(true);
    }

    private string FormatDetail()
    {
        if (_scoreManager == null)
            return "";
        string detail = string.Format(Tr("UI_END_DETAIL"), _scoreManager.TotalKills.ToString("N0"));
        float multiplier = _scoreManager.CharacterMultiplier * _scoreManager.MutatorMultiplier * _scoreManager.PerilMultiplier;
        return multiplier > 1.001f ? $"{detail}  ·  ×{multiplier:0.00}" : detail;
    }

    private void BuildMiddle(RunRecord record)
    {
        MenuSkin.Clear(_facts);
        MenuSkin.Clear(_equipment);
        Control killer = RunSummaryPanels.KillerCard(record?.DeathCause, _finalDuration);
        if (killer != null) _facts.AddChild(killer);
        int totalKills = _scoreManager?.TotalKills ?? 0;
        _facts.AddChild(RunSummaryPanels.Facts(BuildFacts(record, totalKills)));
        _equipment.AddChild(RunSummaryPanels.WeaponTable(record?.Weapons ?? new List<RunWeaponRecord>(), totalKills, _finalDuration));
        // Les Écussons gardent leurs cases, même vides (comme au HUD).
        _equipment.AddChild(UITheme.MakeLabel(Tr("INVENTORY_BADGES"), TextRole.Section, UITheme.GoldBright));
        HFlowContainer badges = MakeSlotFlow();
        _equipment.AddChild(badges);
        AddBuildSlots(badges, _build?.Badges, InventoryConfig.BadgeSlots, UITheme.GoldColor);
        // Les objets du sac, un par fiche avec ses exemplaires.
        _equipment.AddChild(UITheme.MakeLabel(Tr("INVENTORY_BAG"), TextRole.Section, UITheme.GoldBright));
        HFlowContainer passives = MakeSlotFlow();
        _equipment.AddChild(passives);
        if (_build != null)
            foreach ((string icon, string name, int copies, string rarity) in ItemSort.Order(_build.Passives, item => item.Rarity, item => item.Name))
            {
                Control slot = RunSummaryPanels.Slot(icon, copies, RarityPalette.Main(rarity), rarityId: rarity);
                slot.MouseFilter = Control.MouseFilterEnum.Stop;
                slot.TooltipText = $"{name} · {RarityPalette.DisplayName(rarity)} · ×{copies}";
                slot.AddChild(new TextureRect { Texture = RarityArt.Icons(RarityArt.Rank(rarity), 12)[0],
                    CustomMinimumSize = new Vector2(12, 12), SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkBegin, MouseFilter = Control.MouseFilterEnum.Ignore });
                passives.AddChild(slot);
            }
        if (_build == null || _build.Passives.Count == 0)
            passives.AddChild(UITheme.MakeLabel("Aucun objet", TextRole.Subhead, UITheme.TextDim));
        _equipment.AddChild(UITheme.MakeLabel("RÉMINISCENCES", TextRole.Section, UITheme.GoldBright));
        if (_build != null)
            foreach ((string icon, string name) in _build.Perks)
            {
                HBoxContainer row = new();
                row.AddThemeConstantOverride("separation", 14);
                row.AddChild(PlayerSheet.MakeIcon(icon, 40));
                row.AddChild(UITheme.MakeLabel(name, TextRole.Subhead, UITheme.TextColor));
                _equipment.AddChild(row);
            }
        if (_build == null || _build.Perks.Count == 0)
            _equipment.AddChild(UITheme.MakeLabel("Aucune réminiscence", TextRole.Subhead, UITheme.TextDim));
    }

    private static HFlowContainer MakeSlotFlow()
    {
        HFlowContainer flow = new();
        flow.AddThemeConstantOverride("h_separation", 12);
        flow.AddThemeConstantOverride("v_separation", 12);
        return flow;
    }

    private static void AddBuildSlots(Container row, List<(string Icon, int Level)> carried, int slots, Color frame)
    {
        for (int i = 0; i < slots; i++)
        {
            bool filled = carried != null && i < carried.Count;
            row.AddChild(filled
                ? RunSummaryPanels.Slot(carried[i].Icon, carried[i].Level, frame)
                : RunSummaryPanels.Slot(null, 0, UITheme.TextVeryDim));
        }
    }

    private List<(string Caption, string Value)> BuildFacts(RunRecord record, int totalKills)
    {
        List<(string, string)> facts = new()
        {
            (Tr("UI_END_LEVEL_REACHED"), Mathf.Max(1, record?.MaxLevel ?? 1).ToString()),
            (Tr("UI_END_KILLS"), totalKills.ToString("N0")),
            (Tr("UI_END_ELITES"), (record?.ElitesKilled ?? 0).ToString("N0")),
            (Tr("UI_END_SOVEREIGNS"), (record?.SovereignsKilled ?? 0).ToString("N0")),
        };
        if (record?.BossesKilled is > 0)
            facts.Add((Tr("UI_END_BOSSES"), record.BossesKilled.Value.ToString("N0")));
        facts.Add((Tr("UI_END_TRAVELLED"), FormatMeters(record?.TravelledMeters ?? 0f)));
        facts.Add((Tr("UI_END_CRISES"), (record?.CrisesSurvived ?? 0).ToString()));
        facts.Add((Tr("UI_END_CHESTS"), (record?.ChestsOpened ?? 0).ToString("N0")));
        facts.Add((Tr("UI_END_DAMAGE_TAKEN"), Mathf.RoundToInt(record?.TotalDamageTaken ?? 0f).ToString("N0")));
        return facts;
    }

    private void BuildTimeline()
    {
        RunJourney journey = _runTracker?.Journey;
        _timeline.Visible = journey != null && journey.Samples.Count >= 2;
        _timelineCaption.Visible = _timeline.Visible;
        if (!_timeline.Visible)
            return;
        _timeline.SetData(journey.Samples, journey.Markers, _finalDuration, RunSummaryPanels.FormatDuration(0f), RunSummaryPanels.FormatDuration(_finalDuration));
        _timeline.Reveal = 0f;
    }

    private string FormatMeters(float meters) => string.Format(Tr("UI_END_DISTANCE_M"), Mathf.RoundToInt(meters).ToString("N0"));

    /// <summary>Les gains sont déjà acquis (sauvegardés à la mort) : les cartes ne font que les montrer.</summary>
    private void BuildGains(GameManager gm)
    {
        MenuSkin.Clear(_gains);
        _gainCards.Clear();
        int vestiges = _scoreManager?.VestigesEarned ?? 0;
        if (vestiges > 0)
        {
            AddGainCard(string.Format(Tr("UI_END_VESTIGES"), vestiges), UITheme.GoldBright).SetCounter(Tr("UI_END_VESTIGES"), vestiges);
            // Le solde renvoie au Campement, où il se dépense (plan 36) ; faux si le règlement n'a pas été écrit.
            if (string.IsNullOrEmpty(gm.LastRunSaveError))
                AddGainCard(string.Format(Tr("UI_END_VESTIGES_BALANCE"), MetaSaveManager.GetVestiges().ToString("N0", PlayerSheet.French)),
                    UITheme.TextLight);
        }
        foreach (string questId in _questsDone)
        {
            QuestDefinition quest = QuestDataLoader.Get(questId);
            if (quest != null)
                AddGainCard(string.Format(Tr("UI_END_QUEST"), quest.Name, QuestBook.RewardSummary(quest)), UITheme.GreenKit,
                    flipSound: "sfx_souvenir_trouve");
        }
        foreach (WeaponData weapon in _newWeapons)
            AddGainCard(string.Format(Tr("UI_END_NEW_WEAPON"), weapon.Name), UITheme.GoldBright, weapon.Sprite, "sfx_souvenir_trouve");
        _collectionButton.Visible = _newWeapons.Count > 0;
        if (_gainCards.Count == 0)
            _gains.AddChild(UITheme.MakeLabel("Aucun nouveau gain.", TextRole.Subhead, UITheme.TextDim));
    }

    private EndGainCard AddGainCard(string text, Color accent, string icon = null, string flipSound = null)
    {
        EndGainCard card = new();
        card.Setup(text, accent, _strongFont, icon, flipSound);
        _gains.AddChild(card);
        _gainCards.Add(card);
        return card;
    }

    private static float CardStart(int index) => GainsStart + FirstCardDelay + index * CardStagger;

    private static IEnumerable<string> AvailableWeaponIds()
    {
        WeaponDataLoader.Load();
        MetaSaveManager.Load();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            if (MetaSaveManager.IsWeaponUnlocked(weapon.Id))
                yield return weapon.Id;
        }
    }

    // ==============================
    // Révélation
    // ==============================

    public override void _Process(double delta)
    {
        if (!_showing) return;
        _elapsed += (float)delta;
        if (_elapsed <= _revealEnd + 0.1f || _restartButton.Disabled)
            ApplyReveal();
        if (_elapsed >= _buttonsAt && _restartButton.Disabled)
        {
            SetButtonsEnabled(true);
            _restartButton.GrabFocus();
        }
        if (_isRecord && _elapsed > _revealEnd)
        {
            float pulse = 0.87f + 0.13f * Mathf.Sin(_elapsed * 3f);
            _record.Modulate = new Color(pulse, pulse, pulse);
        }
        float axis = Input.GetAxis("scroll_up", "scroll_down");
        if (axis != 0f)
        {
            Control focus = GetViewport().GuiGetFocusOwner();
            ScrollContainer scroll = focus == _gainsScroll || (focus != _detailScroll && _gainsScroll.GetGlobalRect().HasPoint(_gainsScroll.GetGlobalMousePosition()))
                ? _gainsScroll : _detailScroll;
            scroll.ScrollVertical += Mathf.RoundToInt(axis * 850f * (float)delta);
        }
    }

    /// <summary>Un appui pendant la révélation la termine, gains compris et sans son ; il ne déclenche jamais un bouton.</summary>
    public override void _Input(InputEvent @event)
    {
        if (_showing && _elapsed >= _revealEnd && @event.IsActionPressed("ui_cancel") && _equipmentVisible)
        {
            SelectDetails(false);
            _overviewTab.GrabFocus();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!_showing || _elapsed >= _revealEnd || !@event.IsPressed() || @event.IsEcho())
            return;
        if (@event is not (InputEventKey or InputEventMouseButton or InputEventJoypadButton))
            return;
        _elapsed = _revealEnd;
        _skipped = true;
        _buttonsAt = _revealEnd + ButtonGuardSec;
        ApplyReveal();
        GetViewport().SetInputAsHandled();
    }

    private void ApplyReveal()
    {
        float veil = Mathf.Clamp(_elapsed / VeilSec, 0f, 1f);
        _veil.Color = new Color(VeilColor, VeilColor.A * veil);
        _title.Modulate = new Color(1f, 1f, 1f, veil);

        // Score, distance et durée montent ensemble.
        float count = Mathf.Clamp((_elapsed - ScoreStart) / ScoreSec, 0f, 1f);
        float eased = 1f - Mathf.Pow(1f - count, 3f);
        float heroIn = Mathf.Clamp((_elapsed - ScoreStart) / 0.15f, 0f, 1f);
        _score.Text = Mathf.RoundToInt(_finalScore * eased).ToString("N0");
        _distance.Text = FormatMeters(_finalDistance * eased);
        _duration.Text = RunSummaryPanels.FormatDuration(_finalDuration * eased);
        Color hero = new(1f, 1f, 1f, heroIn);
        _score.Modulate = hero;
        _distance.Modulate = hero;
        _distanceCaption.Modulate = hero;
        _duration.Modulate = hero;
        _durationCaption.Modulate = hero;

        float recordIn = Mathf.Clamp((_elapsed - ScoreStart - ScoreSec) / 0.2f, 0f, 1f);
        // Le record pulse doucement, une fois révélé : c'est la seule célébration de la run.
        float pulse = _isRecord && recordIn >= 1f ? 0.75f + 0.25f * Mathf.Sin(_elapsed * 4f) : 1f;
        _record.Modulate = new Color(pulse, pulse, pulse, recordIn);
        _detail.Modulate = new Color(1f, 1f, 1f, recordIn);

        float middle = Mathf.Clamp((_elapsed - BuildStart) / 0.35f, 0f, 1f);
        _middle.Modulate = new Color(1f, 1f, 1f, middle);
        _middle.Position = new Vector2(80f, MiddleY + (1f - middle) * SlideIn);

        float timeline = Mathf.Clamp((_elapsed - TimelineStart) / TimelineSec, 0f, 1f);
        _timelineCaption.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(timeline * 4f, 0f, 1f));
        _timeline.Modulate = _timelineCaption.Modulate;
        _timeline.Reveal = timeline;

        float gains = Mathf.Clamp((_elapsed - GainsStart) / 0.35f, 0f, 1f);
        _gains.Modulate = new Color(1f, 1f, 1f, gains);
        for (int i = 0; i < _gainCards.Count; i++)
            _gainCards[i].Advance(_elapsed - CardStart(i), _skipped);
        _buttons.Modulate = new Color(1f, 1f, 1f, gains);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _restartButton.Disabled = !enabled;
        _hubButton.Disabled = !enabled;
        _collectionButton.Disabled = !enabled;
        _overviewTab.Disabled = !enabled;
        _equipmentTab.Disabled = !enabled;
    }

    private void OnRestartPressed()
    {
        if (_leaving || _restartButton.Disabled) return;
        _leaving = true;
        AudioManager.PlayUI("sfx_menu_confirmer");
        GetTree().Paused = false;
        GetNode<GameManager>("/root/GameManager").OpenCharacterSelection = true;
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }

    private void OnCollectionPressed()
    {
        GetNode<GameManager>("/root/GameManager").CollectionFocusWeaponId = _newWeapons.Count > 0 ? _newWeapons[0].Id : null;
        OnHubPressed();
    }

    private void OnHubPressed()
    {
        if (_leaving || _hubButton.Disabled) return;
        _leaving = true;
        AudioManager.PlayUI("sfx_menu_confirmer");
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }
}
