using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Infrastructure.Steam;
using Vestiges.Progression;
using Vestiges.Score;

namespace Vestiges.UI;

/// <summary>
/// Bilan de l'expédition : score, distance et durée en tête ; portrait, Expédition (dernier coup, détail du score, faits)
/// et Équipement visibles ensemble, puis une rangée de gains (DECISIONS §150). Les colonnes défilent indépendamment. La révélation n'attribue rien : les gains sont enregistrés à la mort.
/// Un premier appui termine la révélation sans activer les boutons ; Rejouer retourne au choix du personnage.
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
    private static readonly Vector2 DesignSize = new(1920f, 1080f);
    private const int KillsFontSize = 110;
    private const float VeilSec = 0.5f;
    private const float ScoreStart = 0.4f;
    private const float ScoreSec = 0.9f;
    private const float BuildStart = 1.2f;
    private const float GainsStart = 2f;
    // Première carte de gain retournée après l'apparition de la rangée, les suivantes à la file.
    private const float FirstCardDelay = 0.25f;
    private const float CardStagger = 0.22f;
    private const float MinRevealEnd = 2.5f;
    private const float ButtonGuardSec = 0.35f;
    private const float SlideIn = 24f;
    private const float MiddleY = 258f;
    private const float ExpeditionWidth = 560f;
    private const float BuildIconSize = 40f;
    private static readonly Color VeilColor = new(0.025f, 0.02f, 0.05f, 0.93f);

    private EventBus _eventBus;
    private ScoreManager _scoreManager;
    // Place de la semaine renvoyée par Steam après l'envoi des éliminations (plan 43 C2), ajoutée à la ligne du record.
    private LeaderboardQueue _rankQueue;
    private string _weeklyBoard;
    private int _weeklyRank;
    private Font _bodyFont;
    private Font _strongFont;
    private Font _boldFont;

    private ColorRect _veil;
    private Control _buttons;
    private Control _root;
    private Label _title;
    private Label _kills;
    private Label _killsCaption;
    private Label _distance;
    private Label _distanceCaption;
    private Label _duration;
    private Label _durationCaption;
    private Label _record;
    private Control _middle;
    private TextureRect _portrait;
    private Label _characterName;
    private VBoxContainer _equipment;
    private VBoxContainer _facts;
    private ScrollContainer _expeditionScroll;
    private ScrollContainer _equipmentScroll;
    private ScrollContainer _gainsScroll;
    private bool _leaving;
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
    private int _finalKills;
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
        if (_rankQueue != null)
            _rankQueue.UploadFinished -= OnUploadFinished;
        // La fenêtre survit au rechargement de la scène : elle ne doit pas garder ce bilan libéré.
        GetViewport().SizeChanged -= FitToViewport;
    }

    public void SetScoreManager(ScoreManager scoreManager)
    {
        _scoreManager = scoreManager;
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
        // Une seule mesure de run, au centre (DECISIONS §155) ; distance et durée l'encadrent.
        _kills = MakeLabel("0", _boldFont, TextRole.Display, UITheme.GoldBright, HorizontalAlignment.Center, 4);
        _kills.AddThemeFontSizeOverride("font_size", KillsFontSize);
        Place(_kills, 660, 64, 600, 120);
        _killsCaption = MakeLabel(Tr("UI_END_KILLS_CAPTION"), _bodyFont, TextRole.Heading, UITheme.TextDim, HorizontalAlignment.Center, 0);
        Place(_killsCaption, 660, 176, 600, 34);
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

        HBoxContainer middle = new();
        middle.AddThemeConstantOverride("separation", 24);
        _middle = middle;
        Place(middle, 80, MiddleY, 1760, 622);
        VBoxContainer character = new() { CustomMinimumSize = new Vector2(200, 0) };
        middle.AddChild(character);
        _portrait = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/menu_portrait.gdshader") } };
        character.AddChild(_portrait);
        _characterName = UITheme.MakeLabel("", TextRole.Heading, UITheme.TextColor, TextWeight.Strong, HorizontalAlignment.Center);
        character.AddChild(_characterName);

        // Expédition et équipement côte à côte : tout le build se lit sans onglet (DECISIONS §150).
        (_expeditionScroll, _facts) = AddColumn(middle, Tr("UI_END_EXPEDITION"), ExpeditionWidth);
        (_equipmentScroll, _equipment) = AddColumn(middle, Tr("UI_END_EQUIPMENT"), 0f);

        // Gains en une rangée, sans titre, centrée ; elle défile de côté s'il y en a beaucoup.
        _gainsScroll = new ScrollContainer { VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever, FocusMode = Control.FocusModeEnum.All };
        UITheme.ApplyFocusStyle(_gainsScroll);
        Place(_gainsScroll, 80, 892, 1760, 62);
        CenterContainer center = new() { CustomMinimumSize = new Vector2(1760, 60), MouseFilter = Control.MouseFilterEnum.Ignore };
        _gainsScroll.AddChild(center);
        HBoxContainer gains = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        gains.AddThemeConstantOverride("separation", 14);
        _gains = gains;
        center.AddChild(gains);

        HBoxContainer buttons = new() { Alignment = BoxContainer.AlignmentMode.Center };
        _buttons = buttons;
        buttons.AddThemeConstantOverride("separation", 24);
        Place(buttons, 80, 962, 1760, 58);
        _restartButton = MenuSkin.Button(Tr("UI_END_REPLAY"), true);
        _restartButton.CustomMinimumSize = new Vector2(350, 58);
        _restartButton.Pressed += OnRestartPressed;
        // Ces départs sonnent leur propre validation : pas de clic par-dessus la cloche.
        UITheme.SetPressSound(_restartButton, null);
        buttons.AddChild(_restartButton);
        _hubButton = MenuSkin.Button(Tr("UI_END_HUB"));
        _hubButton.CustomMinimumSize = new Vector2(300, 58);
        _hubButton.Pressed += OnHubPressed;
        UITheme.SetPressSound(_hubButton, null);
        buttons.AddChild(_hubButton);
        _collectionButton = MenuSkin.Button(Tr("UI_END_COLLECTION"));
        _collectionButton.Visible = false;
        _collectionButton.Pressed += OnCollectionPressed;
        UITheme.SetPressSound(_collectionButton, null);
        buttons.AddChild(_collectionButton);
        _restartButton.FocusNeighborRight = _hubButton.GetPath();
        _hubButton.FocusNeighborLeft = _restartButton.GetPath();
        _expeditionScroll.FocusNeighborRight = _equipmentScroll.GetPath();
        _equipmentScroll.FocusNeighborLeft = _expeditionScroll.GetPath();
        _expeditionScroll.FocusNeighborBottom = _gainsScroll.GetPath();
        _equipmentScroll.FocusNeighborBottom = _gainsScroll.GetPath();
        _gainsScroll.FocusNeighborTop = _equipmentScroll.GetPath();
        _gainsScroll.FocusNeighborBottom = _restartButton.GetPath();
        _restartButton.FocusNeighborTop = _gainsScroll.GetPath();
        _seed = MakeLabel("", _bodyFont, TextRole.Body, UITheme.TextDim, HorizontalAlignment.Left, 0);
        Place(_seed, 80, 1036, 1200, 28);
        GetViewport().SizeChanged += FitToViewport;
        FitToViewport();
    }

    /// <summary>Panneau de pierre titré, au contenu défilant ; largeur 0 : il prend la place qui reste.</summary>
    private (ScrollContainer, VBoxContainer) AddColumn(HBoxContainer parent, string title, float width)
    {
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(width, 0) };
        if (width <= 0f)
            panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        parent.AddChild(panel);
        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 10);
        panel.AddChild(column);
        column.AddChild(UITheme.MakeLabel(title, TextRole.Section, UITheme.GoldBright));
        ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.All };
        UITheme.ApplyFocusStyle(scroll);
        column.AddChild(scroll);
        VBoxContainer content = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(content);
        return (scroll, content);
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
        ListenWeeklyRank();
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
        _finalKills = _scoreManager?.TotalKills ?? 0;
        // Record global ou du personnage : les deux se célèbrent, le texte dit lequel.
        _isRecord = (_scoreManager?.IsNewRecord ?? false) || (_scoreManager?.IsNewCharacterRecord ?? false);
        _finalDistance = record?.MaxDistanceMeters ?? 0f;
        _finalDuration = record?.RunDurationSec ?? 0f;

        _characterName.Text = _build?.CharacterName ?? "";
        _portrait.Texture = _build != null ? CharacterPortrait.Artwork(_build.CharacterId) ?? CharacterPortrait.Idle(_build.CharacterId) : null;
        _record.Text = RecordLine();
        _record.AddThemeColorOverride("font_color", _isRecord ? UITheme.GoldBright : UITheme.TextDim);
        _seed.Text = Tr(DifficultyDataLoader.Get(record?.Difficulty).NameKey);
        if (gm.EffectiveSeed > 0)
            _seed.Text += "  ·  " + string.Format(Tr("UI_END_SEED"), gm.EffectiveSeed);
        string saveNotice = !string.IsNullOrEmpty(gm.LastRunSaveError) ? Tr("UI_END_SAVE_FAILED")
            : gm.LastRunHistoryPending ? Tr("UI_END_HISTORY_PENDING") : "";
        if (saveNotice.Length > 0)
            _seed.Text = _seed.Text.Length > 0 ? $"{saveNotice}  ·  {_seed.Text}" : saveNotice;
        _seed.AddThemeColorOverride("font_color", saveNotice.Length > 0 ? UITheme.GoldBright : UITheme.TextVeryDim);

        BuildMiddle(record);
        BuildGains(gm);
        _expeditionScroll.ScrollVertical = 0;
        _equipmentScroll.ScrollVertical = 0;
        _gainsScroll.ScrollHorizontal = 0;

        _kills.Text = "0";
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

    private void BuildMiddle(RunRecord record)
    {
        MenuSkin.Clear(_facts);
        MenuSkin.Clear(_equipment);
        Control killer = RunSummaryPanels.KillerCard(record?.DeathCause, _finalDuration);
        if (killer != null) _facts.AddChild(killer);
        int totalKills = _scoreManager?.TotalKills ?? 0;
        _facts.AddChild(RunSummaryPanels.Facts(BuildFacts(record), 1, 300f));

        _equipment.AddChild(RunSummaryPanels.WeaponTable(record?.Weapons ?? new List<RunWeaponRecord>(), totalKills, _finalDuration));
        // Écussons et Réminiscences sur une même rangée, le sac dessous : tout le build tient sans défiler.
        HBoxContainer marks = new();
        marks.AddThemeConstantOverride("separation", 40);
        _equipment.AddChild(marks);
        VBoxContainer badgeColumn = BuildGroup(marks, Tr("INVENTORY_BADGES"));
        HBoxContainer badges = MakeSlotRow();
        badgeColumn.AddChild(badges);
        AddBuildSlots(badges, _build?.Badges, InventoryConfig.BadgeSlots, UITheme.GoldColor);
        VBoxContainer perkColumn = BuildGroup(marks, "Réminiscences");
        HBoxContainer perks = MakeSlotRow();
        perkColumn.AddChild(perks);
        if (_build != null)
            foreach ((string icon, string name) in _build.Perks)
            {
                Control slot = RunSummaryPanels.Slot(icon, 0, UITheme.GoldDim, BuildIconSize);
                slot.MouseFilter = Control.MouseFilterEnum.Stop;
                slot.TooltipText = name;
                perks.AddChild(slot);
            }
        if (_build == null || _build.Perks.Count == 0)
            perks.AddChild(UITheme.MakeLabel("Aucune", TextRole.Subhead, UITheme.TextDim));

        VBoxContainer bagColumn = BuildGroup(_equipment, Tr("INVENTORY_BAG"));
        HFlowContainer passives = MakeSlotFlow();
        bagColumn.AddChild(passives);
        if (_build != null)
            foreach ((string icon, string name, int copies, string rarity) in _build.Passives)
            {
                // Le nombre d'exemplaires seulement au-delà d'un, comme au HUD.
                Control slot = RunSummaryPanels.Slot(icon, copies > 1 ? copies : 0, RarityPalette.Main(rarity), BuildIconSize, rarity);
                slot.MouseFilter = Control.MouseFilterEnum.Stop;
                slot.TooltipText = $"{name} · {RarityPalette.DisplayName(rarity)} · ×{copies}";
                passives.AddChild(slot);
            }
        if (_build == null || _build.Passives.Count == 0)
            passives.AddChild(UITheme.MakeLabel("Aucun objet", TextRole.Subhead, UITheme.TextDim));
    }

    private static VBoxContainer BuildGroup(Container parent, string title)
    {
        VBoxContainer group = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        group.AddThemeConstantOverride("separation", 8);
        group.AddChild(UITheme.MakeLabel(title, TextRole.Subhead, UITheme.TextDim));
        parent.AddChild(group);
        return group;
    }

    // Quatre cases au plus : une rangée qui ne passe jamais à la ligne.
    private static HBoxContainer MakeSlotRow()
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        return row;
    }

    private static HFlowContainer MakeSlotFlow()
    {
        HFlowContainer flow = new();
        flow.AddThemeConstantOverride("h_separation", 10);
        flow.AddThemeConstantOverride("v_separation", 10);
        return flow;
    }

    private static void AddBuildSlots(Container row, List<(string Icon, int Level)> carried, int slots, Color frame)
    {
        for (int i = 0; i < slots; i++)
        {
            bool filled = carried != null && i < carried.Count;
            row.AddChild(filled
                ? RunSummaryPanels.Slot(carried[i].Icon, carried[i].Level, frame, BuildIconSize)
                : RunSummaryPanels.Slot(null, 0, UITheme.TextVeryDim, BuildIconSize));
        }
    }

    /// <summary>L'envoi part à la mort, la réponse peut arriver avant comme après l'ouverture du bilan.</summary>
    private void ListenWeeklyRank()
    {
        _rankQueue = SteamManager.Leaderboards;
        if (_rankQueue == null)
            return;
        _weeklyBoard = RunLeaderboards.WeeklyBoard(System.DateTime.UtcNow);
        _rankQueue.UploadFinished += OnUploadFinished;
    }

    private void OnUploadFinished(LeaderboardQueue.UploadResult result)
    {
        if (result.Board != _weeklyBoard || !result.Succeeded || result.GlobalRank <= 0)
            return;
        _weeklyRank = result.GlobalRank;
        if (Visible)
            _record.Text = RecordLine();
    }

    private string RecordLine()
    {
        string record = RecordText(_build?.CharacterName ?? "");
        return _weeklyRank > 0 ? $"{record}  ·  {string.Format(Tr("UI_END_WEEKLY_RANK"), _weeklyRank.ToString("N0"))}" : record;
    }

    /// <summary>
    /// Record global battu, sinon celui du personnage, sinon les deux records à battre (DECISIONS §155).
    /// </summary>
    private string RecordText(string characterName)
    {
        if (_scoreManager == null)
            return "";
        if (_scoreManager.IsNewRecord)
            return Tr("UI_END_NEW_RECORD");
        if (_scoreManager.IsNewCharacterRecord && characterName.Length > 0)
            return string.Format(Tr("UI_END_CHARACTER_RECORD"), characterName);
        string best = string.Format(Tr("UI_END_BEST"), _scoreManager.BestKills.ToString("N0"));
        return characterName.Length > 0
            ? $"{best}  ·  {characterName} : {_scoreManager.CharacterBestKills.ToString("N0")}"
            : best;
    }

    private List<(string Caption, string Value)> BuildFacts(RunRecord record)
    {
        List<(string, string)> facts = new()
        {
            (Tr("UI_END_LEVEL_REACHED"), Mathf.Max(1, record?.MaxLevel ?? 1).ToString()),
            (Tr("UI_END_ELITES"), (record?.ElitesKilled ?? 0).ToString("N0")),
            (Tr("UI_END_SOVEREIGNS"), (record?.SovereignsKilled ?? 0).ToString("N0")),
        };
        if (record?.LandsCrossed is > 0)
            facts.Add((Tr("UI_END_LANDS"), record.BossDefeated
                ? string.Format(Tr("UI_END_LANDS_WON"), record.LandsCrossed.Value)
                : record.LandsCrossed.Value.ToString()));
        if (record?.BossesKilled is > 0)
            facts.Add((Tr("UI_END_BOSSES"), record.BossesKilled.Value.ToString("N0")));
        facts.Add((Tr("UI_END_TRAVELLED"), FormatMeters(record?.TravelledMeters ?? 0f)));
        facts.Add((Tr("UI_END_CRISES"), (record?.CrisesSurvived ?? 0).ToString()));
        facts.Add((Tr("UI_END_CHESTS"), (record?.ChestsOpened ?? 0).ToString("N0")));
        facts.Add((Tr("UI_END_DAMAGE_TAKEN"), Mathf.RoundToInt(record?.TotalDamageTaken ?? 0f).ToString("N0")));
        return facts;
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
            // Le solde renvoie au Campement, où il se dépense (plan 36) ; absent si le règlement n'a pas été écrit.
            string format = Tr("UI_END_VESTIGES");
            if (string.IsNullOrEmpty(gm.LastRunSaveError))
                format += "  ·  " + string.Format(Tr("UI_END_VESTIGES_BALANCE"), MetaSaveManager.GetVestiges().ToString("N0", PlayerSheet.French));
            AddGainCard(format, UITheme.GoldBright, UITheme.LoadTex(UITheme.IconsPath + "ui_icon_vestiges.png")).SetCounter(format, vestiges);
        }
        foreach (string questId in _questsDone)
        {
            QuestDefinition quest = QuestDataLoader.Get(questId);
            if (quest != null)
                AddGainCard(string.Format(Tr("UI_END_QUEST"), quest.Name, QuestBook.RewardSummary(quest)), UITheme.GreenKit,
                    QuestRewardIcon.For(quest), "sfx_souvenir_trouve");
        }
        foreach (WeaponData weapon in _newWeapons)
            AddGainCard(string.Format(Tr("UI_END_NEW_WEAPON"), weapon.Name), UITheme.GoldBright,
                ResourceLoader.Exists(weapon.Sprite) ? GD.Load<Texture2D>(weapon.Sprite) : null, "sfx_souvenir_trouve");
        _collectionButton.Visible = _newWeapons.Count > 0;
        if (_gainCards.Count == 0)
            _gains.AddChild(UITheme.MakeLabel("Aucun nouveau gain.", TextRole.Subhead, UITheme.TextDim));
    }

    private EndGainCard AddGainCard(string text, Color accent, Texture2D icon = null, string flipSound = null)
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
            int step = Mathf.RoundToInt(axis * 850f * (float)delta);
            if (IsActiveColumn(_gainsScroll))
                _gainsScroll.ScrollHorizontal += step;
            else
                (IsActiveColumn(_expeditionScroll) ? _expeditionScroll : _equipmentScroll).ScrollVertical += step;
        }
    }

    /// <summary>La colonne qui a le focus, ou à défaut celle que survole la souris.</summary>
    private bool IsActiveColumn(ScrollContainer scroll)
    {
        Control focus = GetViewport().GuiGetFocusOwner();
        if (focus is ScrollContainer focused && (focused == _gainsScroll || focused == _expeditionScroll || focused == _equipmentScroll))
            return focused == scroll;
        return scroll.GetGlobalRect().HasPoint(scroll.GetGlobalMousePosition());
    }

    /// <summary>Un appui pendant la révélation la termine, gains compris et sans son ; il ne déclenche jamais un bouton.</summary>
    public override void _Input(InputEvent @event)
    {
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

        // Éliminations, distance et durée montent ensemble.
        float count = Mathf.Clamp((_elapsed - ScoreStart) / ScoreSec, 0f, 1f);
        float eased = 1f - Mathf.Pow(1f - count, 3f);
        float heroIn = Mathf.Clamp((_elapsed - ScoreStart) / 0.15f, 0f, 1f);
        _kills.Text = Mathf.RoundToInt(_finalKills * eased).ToString("N0");
        _distance.Text = FormatMeters(_finalDistance * eased);
        _duration.Text = RunSummaryPanels.FormatDuration(_finalDuration * eased);
        Color hero = new(1f, 1f, 1f, heroIn);
        _kills.Modulate = hero;
        _killsCaption.Modulate = hero;
        _distance.Modulate = hero;
        _distanceCaption.Modulate = hero;
        _duration.Modulate = hero;
        _durationCaption.Modulate = hero;

        float recordIn = Mathf.Clamp((_elapsed - ScoreStart - ScoreSec) / 0.2f, 0f, 1f);
        // Le record pulse doucement, une fois révélé : c'est la seule célébration de la run.
        float pulse = _isRecord && recordIn >= 1f ? 0.75f + 0.25f * Mathf.Sin(_elapsed * 4f) : 1f;
        _record.Modulate = new Color(pulse, pulse, pulse, recordIn);

        float middle = Mathf.Clamp((_elapsed - BuildStart) / 0.35f, 0f, 1f);
        _middle.Modulate = new Color(1f, 1f, 1f, middle);
        _middle.Position = new Vector2(80f, MiddleY + (1f - middle) * SlideIn);

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
