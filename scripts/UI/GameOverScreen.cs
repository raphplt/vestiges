using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Score;

namespace Vestiges.UI;

/// <summary>
/// Bilan de fin de run, en une page dense (plan 02 lot D, M3). En tête, trois chiffres : jusqu'où le joueur est allé,
/// le score (et, seulement ici, le record), la durée. Au centre, le personnage et ce qui l'a emporté, le tableau des
/// armes (dégâts, part, dégâts par seconde, éliminations), les souvenirs et les faits de la run. Dessous, la frise de
/// la run, puis les gains, cartes qui se retournent une à une (M4), et les boutons. Révélation en quelques temps
/// qu'une touche termine (gains compris, sans son) ; les boutons ne
/// s'activent qu'après un court délai, pour qu'un clic de combat ne relance pas la run par accident. Tout est relevé à
/// la mort ; le bilan n'apparaît qu'à la fin de la séquence de mort (<see cref="DeathSequence"/> appelle
/// <see cref="Reveal"/>).
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
    private const float MiddleY = 300f;
    private const float TimelineY = 766f;
    private const float GainsY = 884f;
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
    private Label _timelineCaption;
    private RunTimelineStrip _timeline;
    private Control _gains;
    private HubMenuButton _restartButton;
    private HubMenuButton _hubButton;
    private HubMenuButton _collectionButton;
    private Label _seed;
    // Armes disponibles au départ de la run : celles qui s'y ajoutent au bilan (Souvenir retrouvé) mènent à la Collection.
    private readonly HashSet<string> _weaponsAtStart = new();
    private readonly List<WeaponData> _newWeapons = new();
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
        public readonly List<(string Icon, int Level)> Passives = new();
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 50;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EntityDied += OnEntityDied;
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
            _eventBus.EntityDied -= OnEntityDied;
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
        _veil = new ColorRect { Color = VeilColor, MouseFilter = Control.MouseFilterEnum.Ignore };
        _veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_veil);

        _root = new Control { Size = DesignSize, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_root);

        _title = MakeLabel("", _strongFont, TextRole.Banner, UITheme.GoldDim, HorizontalAlignment.Center);
        Place(_title, 0f, 24f, DesignSize.X, 48f);

        // Trois chiffres en tête : jusqu'où, le score (le héros du bilan, hors échelle), la durée.
        _score = MakeLabel("0", _boldFont, TextRole.Display, UITheme.GoldBright, HorizontalAlignment.Center, 10);
        UITheme.SetFixedTextSize(_score, ScoreFontSize);
        Place(_score, 660f, 70f, 600f, 132f);
        _distance = MakeLabel("", _boldFont, TextRole.Display, UITheme.TextLight, HorizontalAlignment.Center, 8);
        Place(_distance, 180f, 96f, 480f, 64f);
        _distanceCaption = MakeLabel(Tr("UI_END_FARTHEST"), _bodyFont, TextRole.Heading, UITheme.TextDim, HorizontalAlignment.Center, 4);
        Place(_distanceCaption, 180f, 160f, 480f, 34f);
        _duration = MakeLabel("", _boldFont, TextRole.Display, UITheme.TextLight, HorizontalAlignment.Center, 8);
        Place(_duration, 1260f, 96f, 480f, 64f);
        _durationCaption = MakeLabel(Tr("UI_END_DURATION_CAPTION"), _bodyFont, TextRole.Heading, UITheme.TextDim, HorizontalAlignment.Center, 4);
        Place(_durationCaption, 1260f, 160f, 480f, 34f);

        _record = MakeLabel("", _strongFont, TextRole.Title, UITheme.TextColor, HorizontalAlignment.Center);
        Place(_record, 0f, 204f, DesignSize.X, 42f);
        _detail = MakeLabel("", _bodyFont, TextRole.Lead, UITheme.TextDim, HorizontalAlignment.Center, 4);
        Place(_detail, 0f, 246f, DesignSize.X, 30f);

        _middle = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        Place(_middle, 0f, MiddleY, DesignSize.X, 440f);

        _timelineCaption = MakeLabel(Tr("UI_END_TIMELINE"), _bodyFont, TextRole.Small, UITheme.TextDim, HorizontalAlignment.Left, 4);
        Place(_timelineCaption, 60f, TimelineY - 26f, 900f, 24f);
        _timeline = new RunTimelineStrip { MouseFilter = Control.MouseFilterEnum.Ignore };
        Place(_timeline, 60f, TimelineY, DesignSize.X - 120f, 100f);

        _gains = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        _gains.AddThemeConstantOverride("separation", 20);
        Place(_gains, 0f, GainsY, DesignSize.X, 64f);

        HBoxContainer buttons = new() { Alignment = BoxContainer.AlignmentMode.Center };
        _buttons = buttons;
        buttons.AddThemeConstantOverride("separation", 40);
        Place(buttons, 0f, 962f, DesignSize.X, 70f);
        _restartButton = new HubMenuButton();
        _restartButton.Setup(Tr("UI_END_REPLAY"), TextRole.Banner, _strongFont);
        _restartButton.Pressed += OnRestartPressed;
        buttons.AddChild(_restartButton);
        _hubButton = new HubMenuButton();
        _hubButton.Setup(Tr("UI_END_HUB"), TextRole.Banner, _strongFont);
        _hubButton.Pressed += OnHubPressed;
        buttons.AddChild(_hubButton);
        _collectionButton = new HubMenuButton { Visible = false };
        _collectionButton.Setup(Tr("UI_END_COLLECTION"), TextRole.Banner, _strongFont);
        _collectionButton.Pressed += OnCollectionPressed;
        buttons.AddChild(_collectionButton);
        _restartButton.FocusNeighborRight = _hubButton.GetPath();
        _hubButton.FocusNeighborLeft = _restartButton.GetPath();
        _hubButton.FocusNeighborRight = _collectionButton.GetPath();
        _collectionButton.FocusNeighborLeft = _hubButton.GetPath();

        _seed = MakeLabel("", _bodyFont, TextRole.Small, UITheme.TextVeryDim, HorizontalAlignment.Left);
        Place(_seed, 40f, 1040f, 600f, 28f);

        GetViewport().SizeChanged += FitToViewport;
        FitToViewport();
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
        gameManager.LastQuestCompletions = QuestManager.ResolvePendingProgressionQuests(gameManager.LastRunData);
        _newWeapons.Clear();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            if (!_weaponsAtStart.Contains(weapon.Id) && MetaSaveManager.IsWeaponUnlocked(weapon))
                _newWeapons.Add(weapon);
        }
    }

    private static BuildSnapshot Snapshot(Player player)
    {
        BuildSnapshot build = new() { CharacterId = player.CharacterId };
        build.CharacterName = CharacterDataLoader.Get(player.CharacterId)?.Name ?? player.CharacterId;
        foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
            build.Passives.Add((PerkIconResolver.GetPassiveStatIconPath(passive.Data.Stat), passive.Level));
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

        _title.Text = _build?.CharacterName ?? "";
        _record.Text = _isRecord ? Tr("UI_END_NEW_RECORD") : string.Format(Tr("UI_END_BEST"), (_scoreManager?.BestScore ?? 0).ToString("N0"));
        _record.AddThemeColorOverride("font_color", _isRecord ? UITheme.GoldBright : UITheme.TextDim);
        _detail.Text = FormatDetail();
        _seed.Text = gm.RunSeed > 0 ? string.Format(Tr("UI_END_SEED"), gm.RunSeed) : "";

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
        foreach (Node child in _middle.GetChildren())
            child.QueueFree();

        // Colonne de gauche : le personnage, de face, et ce qui l'a emporté.
        if (_build != null)
        {
            CharacterData data = CharacterDataLoader.Get(_build.CharacterId);
            SpriteFrames frames = data != null ? CharacterSpriteLoader.LoadOrGet(data.Id, data.SpriteFolder) : null;
            if (frames != null)
            {
                string animation = frames.HasAnimation("S_idle") ? "S_idle" : "SE_idle";
                AnimatedSprite2D sprite = new()
                {
                    SpriteFrames = frames,
                    Animation = animation,
                    Scale = new Vector2(5f, 5f),
                    Position = new Vector2(300f, 110f),
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                };
                _middle.AddChild(sprite);
                sprite.Play(animation);
            }
        }
        Control killer = RunSummaryPanels.KillerCard(record?.DeathCause, _finalDuration);
        if (killer != null)
        {
            killer.Position = new Vector2(60f, 250f);
            killer.CustomMinimumSize = new Vector2(480f, 0f);
            _middle.AddChild(killer);
        }

        // Colonne centrale : les armes, puis les souvenirs.
        int totalKills = _scoreManager?.TotalKills ?? 0;
        Control weapons = RunSummaryPanels.WeaponTable(record?.Weapons ?? new List<RunWeaponRecord>(), totalKills, _finalDuration);
        weapons.Position = new Vector2(580f, 0f);
        _middle.AddChild(weapons);

        Label passivesCaption = MakeLabel(Tr("UI_END_PASSIVES"), _strongFont, TextRole.Subhead, UITheme.TextDim, HorizontalAlignment.Left, 4);
        passivesCaption.Position = new Vector2(580f, 348f);
        _middle.AddChild(passivesCaption);
        HBoxContainer passives = new() { Position = new Vector2(580f, 376f), MouseFilter = Control.MouseFilterEnum.Ignore };
        passives.AddThemeConstantOverride("separation", 12);
        _middle.AddChild(passives);
        for (int i = 0; i < Player.MaxPassiveSlots; i++)
        {
            bool filled = _build != null && i < _build.Passives.Count;
            passives.AddChild(filled
                ? RunSummaryPanels.Slot(_build.Passives[i].Icon, _build.Passives[i].Level, UITheme.CyanEssence)
                : RunSummaryPanels.Slot(null, 0, UITheme.TextVeryDim));
        }

        // Colonne de droite : les faits de la run.
        Control facts = RunSummaryPanels.Facts(BuildFacts(record, totalKills));
        facts.Position = new Vector2(1480f, 6f);
        _middle.AddChild(facts);
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
        foreach (Node child in _gains.GetChildren())
            child.QueueFree();
        _gainCards.Clear();
        int vestiges = _scoreManager?.VestigesEarned ?? 0;
        if (vestiges > 0)
            AddGainCard(string.Format(Tr("UI_END_VESTIGES"), vestiges), UITheme.GoldBright).SetCounter(Tr("UI_END_VESTIGES"), vestiges);
        if (gm.LastQuestCompletions != null)
        {
            foreach (string quest in gm.LastQuestCompletions)
                AddGainCard(quest, UITheme.CyanEssence);
        }
        if (gm.LastUnlocks != null)
        {
            foreach (string id in gm.LastUnlocks)
                AddGainCard(string.Format(Tr("UI_END_UNLOCK"), CharacterDataLoader.Get(id)?.Name ?? id), UITheme.GreenKit, flipSound: "sfx_souvenir_trouve");
        }
        foreach (WeaponData weapon in _newWeapons)
            AddGainCard(string.Format(Tr("UI_END_NEW_WEAPON"), weapon.Name), UITheme.GoldBright, weapon.Sprite, "sfx_souvenir_trouve");
        _collectionButton.Visible = _newWeapons.Count > 0;
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
            if (MetaSaveManager.IsWeaponUnlocked(weapon))
                yield return weapon.Id;
        }
    }

    // ==============================
    // Révélation
    // ==============================

    public override void _Process(double delta)
    {
        if (!_showing)
            return;
        _elapsed += (float)delta;
        ApplyReveal();
        if (_elapsed >= _buttonsAt && _restartButton.Disabled)
        {
            SetButtonsEnabled(true);
            _restartButton.GrabFocus();
        }
        if (_elapsed > _revealEnd + ButtonGuardSec && !_isRecord)
            SetProcess(false);
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
        _middle.Position = new Vector2(0f, MiddleY + (1f - middle) * SlideIn);

        float timeline = Mathf.Clamp((_elapsed - TimelineStart) / TimelineSec, 0f, 1f);
        _timelineCaption.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(timeline * 4f, 0f, 1f));
        _timeline.Modulate = _timelineCaption.Modulate;
        _timeline.Reveal = timeline;

        float gains = Mathf.Clamp((_elapsed - GainsStart) / 0.35f, 0f, 1f);
        _gains.Modulate = new Color(1f, 1f, 1f, gains);
        _gains.Position = new Vector2(0f, GainsY + (1f - gains) * SlideIn);
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
        AudioManager.PlayUI("sfx_menu_confirmer");
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void OnCollectionPressed()
    {
        GetNode<GameManager>("/root/GameManager").CollectionFocusWeaponId = _newWeapons.Count > 0 ? _newWeapons[0].Id : null;
        OnHubPressed();
    }

    private void OnHubPressed()
    {
        AudioManager.PlayUI("sfx_menu_confirmer");
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }
}
