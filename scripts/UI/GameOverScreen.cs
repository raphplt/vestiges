using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Score;

namespace Vestiges.UI;

/// <summary>
/// Bilan de fin de run (plan 02 lot D, première passe). Trois zones sur un voile sombre : en tête le score final qui
/// défile et, seulement ici, le record ; au centre le personnage, son build complet (armes et souvenirs avec niveau et
/// rareté) et quelques faits ; en bas les gains, puis « Rejouer » et « Retour au camp ». Révélation en trois temps
/// qu'une touche accélère ; les boutons ne s'activent qu'après un court délai, pour qu'un clic de combat ne relance pas
/// la run par accident. Le monde pâlit d'abord, comme effacé. Le build est figé à la mort, avant que la run ne se défasse.
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
    private static readonly Vector2 DesignSize = new(1920f, 1080f);
    private const float VeilSec = 0.5f;
    private const float ScoreStart = 0.4f;
    private const float ScoreSec = 0.9f;
    private const float BuildStart = 1.2f;
    private const float GainsStart = 1.7f;
    private const float RevealEnd = 2.2f;
    private const float ButtonGuardSec = 0.35f;
    private const float IconSize = 64f;
    // Avant le voile, le monde pâlit comme effacé (1,1 s), sans rien retarder d'autre qu'une respiration.
    private const float WashLead = 1.1f;
    private static readonly Color WashColor = new(0.93f, 0.91f, 0.88f, 0.55f);
    private static readonly Color VeilColor = new(0.025f, 0.02f, 0.05f, 0.93f);
    private static readonly Color SlotColor = new(0.08f, 0.07f, 0.12f, 0.9f);

    private EventBus _eventBus;
    private ScoreManager _scoreManager;
    private Font _bodyFont;
    private Font _strongFont;
    private Font _boldFont;

    private ColorRect _wash;
    private ColorRect _veil;
    private Control _buttons;
    private Control _root;
    private Label _title;
    private Label _score;
    private Label _record;
    private Label _detail;
    private Control _middle;
    private Control _gains;
    private HubMenuButton _restartButton;
    private HubMenuButton _hubButton;
    private Label _seed;

    private bool _showing;
    private float _elapsed;
    private float _buttonsAt = float.MaxValue;
    private int _finalScore;
    private bool _isRecord;
    private BuildSnapshot _build;

    /// <summary>Build du joueur relevé à sa mort : ce que le bilan montre ne dépend plus de la run.</summary>
    private sealed class BuildSnapshot
    {
        public string CharacterId;
        public string CharacterName;
        public readonly List<(string Icon, int Level, Color Rarity)> Weapons = new();
        public readonly List<(string Icon, int Level)> Passives = new();
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 50;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EntityDied += OnEntityDied;
        _bodyFont = GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-Medium.ttf");
        _strongFont = GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf");
        _boldFont = GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-Bold.ttf");
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

    // ==============================
    // Construction
    // ==============================

    private void BuildUI()
    {
        _wash = new ColorRect { Color = WashColor, MouseFilter = Control.MouseFilterEnum.Stop };
        _wash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_wash);
        _veil = new ColorRect { Color = VeilColor, MouseFilter = Control.MouseFilterEnum.Ignore };
        _veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_veil);

        _root = new Control { Size = DesignSize, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_root);

        _title = MakeLabel(Tr("UI_END_TITLE"), _strongFont, 34, UITheme.GoldDim, HorizontalAlignment.Center);
        Place(_title, 0f, 70f, DesignSize.X, 50f);

        _score = MakeLabel("0", _boldFont, 120, UITheme.GoldBright, HorizontalAlignment.Center, 10);
        Place(_score, 0f, 120f, DesignSize.X, 150f);

        _record = MakeLabel("", _strongFont, 30, UITheme.TextColor, HorizontalAlignment.Center);
        Place(_record, 0f, 272f, DesignSize.X, 44f);

        _detail = MakeLabel("", _bodyFont, 22, UITheme.TextDim, HorizontalAlignment.Center);
        Place(_detail, 0f, 318f, DesignSize.X, 34f);

        _middle = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        Place(_middle, 0f, 390f, DesignSize.X, 360f);

        _gains = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        _gains.AddThemeConstantOverride("separation", 24);
        Place(_gains, 0f, 790f, DesignSize.X, 90f);

        HBoxContainer buttons = new() { Alignment = BoxContainer.AlignmentMode.Center };
        _buttons = buttons;
        buttons.AddThemeConstantOverride("separation", 40);
        Place(buttons, 0f, 930f, DesignSize.X, 70f);
        _restartButton = new HubMenuButton();
        _restartButton.Setup(Tr("UI_END_REPLAY"), 36, _strongFont);
        _restartButton.Pressed += OnRestartPressed;
        buttons.AddChild(_restartButton);
        _hubButton = new HubMenuButton();
        _hubButton.Setup(Tr("UI_END_HUB"), 36, _strongFont);
        _hubButton.Pressed += OnHubPressed;
        buttons.AddChild(_hubButton);
        _restartButton.FocusNeighborRight = _hubButton.GetPath();
        _hubButton.FocusNeighborLeft = _restartButton.GetPath();

        _seed = MakeLabel("", _bodyFont, 18, UITheme.TextVeryDim, HorizontalAlignment.Left);
        Place(_seed, 40f, 1030f, 600f, 30f);

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

    private static Label MakeLabel(string text, Font font, int size, Color color, HorizontalAlignment alignment, int outline = 6)
    {
        Label label = new() { Text = text, HorizontalAlignment = alignment, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
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
        ShowGameOver();
    }

    private static BuildSnapshot Snapshot(Player player)
    {
        BuildSnapshot build = new() { CharacterId = player.CharacterId };
        build.CharacterName = CharacterDataLoader.Get(player.CharacterId)?.Name ?? player.CharacterId;
        foreach (WeaponInstance weapon in player.WeaponSlots)
            build.Weapons.Add((weapon.Sprite, weapon.Level, weapon.RarityColor));
        foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
            build.Passives.Add((PerkIconResolver.GetPassiveStatIconPath(passive.Data.Stat), passive.Level));
        return build;
    }

    // ==============================
    // Bilan
    // ==============================

    private void ShowGameOver()
    {
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        RunRecord record = gm.LastRunData;
        _finalScore = _scoreManager?.CurrentScore ?? 0;
        _isRecord = _scoreManager?.IsNewRecord ?? false;

        _title.Text = _build != null ? string.Format(Tr("UI_END_TITLE_OF"), _build.CharacterName) : Tr("UI_END_TITLE");
        _record.Text = _isRecord ? Tr("UI_END_NEW_RECORD") : string.Format(Tr("UI_END_BEST"), (_scoreManager?.BestScore ?? 0).ToString("N0"));
        _record.AddThemeColorOverride("font_color", _isRecord ? UITheme.GoldBright : UITheme.TextDim);
        _detail.Text = FormatDetail();
        _seed.Text = gm.RunSeed > 0 ? string.Format(Tr("UI_END_SEED"), gm.RunSeed) : "";

        BuildMiddle(record);
        BuildGains(gm);

        _score.Text = "0";
        // Temps négatif : le monde pâlit d'abord, puis la révélation commence à zéro.
        _elapsed = -WashLead;
        _buttonsAt = RevealEnd;
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
        string detail = string.Format(Tr("UI_END_DETAIL"), _scoreManager.CombatScore.ToString("N0"),
            _scoreManager.SurvivalScore.ToString("N0"), _scoreManager.BonusScore.ToString("N0"),
            _scoreManager.ExplorationScore.ToString("N0"));
        float multiplier = _scoreManager.CharacterMultiplier * _scoreManager.MutatorMultiplier;
        return multiplier > 1.001f ? $"{detail}  ·  ×{multiplier:0.00}" : detail;
    }

    private void BuildMiddle(RunRecord record)
    {
        foreach (Node child in _middle.GetChildren())
            child.QueueFree();

        // Le personnage, de face, à l'échelle ×6 de ses pixels.
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
                    Scale = new Vector2(6f, 6f),
                    Position = new Vector2(420f, 170f),
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                };
                _middle.AddChild(sprite);
                sprite.Play(animation);
            }
        }

        Label weaponsCaption = MakeLabel(Tr("UI_END_WEAPONS"), _strongFont, 20, UITheme.TextDim, HorizontalAlignment.Left, 4);
        weaponsCaption.Position = new Vector2(640f, 20f);
        _middle.AddChild(weaponsCaption);
        HBoxContainer weapons = SlotRow(new Vector2(640f, 52f));
        for (int i = 0; i < Player.MaxWeaponSlots; i++)
        {
            bool filled = _build != null && i < _build.Weapons.Count;
            weapons.AddChild(filled
                ? MakeSlot(_build.Weapons[i].Icon, _build.Weapons[i].Level, _build.Weapons[i].Rarity)
                : MakeSlot(null, 0, UITheme.TextVeryDim));
        }

        Label passivesCaption = MakeLabel(Tr("UI_END_PASSIVES"), _strongFont, 20, UITheme.TextDim, HorizontalAlignment.Left, 4);
        passivesCaption.Position = new Vector2(640f, 170f);
        _middle.AddChild(passivesCaption);
        HBoxContainer passives = SlotRow(new Vector2(640f, 202f));
        for (int i = 0; i < Player.MaxPassiveSlots; i++)
        {
            bool filled = _build != null && i < _build.Passives.Count;
            passives.AddChild(filled
                ? MakeSlot(_build.Passives[i].Icon, _build.Passives[i].Level, UITheme.CyanEssence)
                : MakeSlot(null, 0, UITheme.TextVeryDim));
        }

        VBoxContainer facts = new() { Position = new Vector2(1180f, 30f), Size = new Vector2(520f, 300f) };
        facts.AddThemeConstantOverride("separation", 14);
        _middle.AddChild(facts);
        AddFact(facts, Tr("UI_END_DURATION"), FormatDuration(record?.RunDurationSec ?? 0f));
        AddFact(facts, Tr("UI_END_KILLS"), (_scoreManager?.TotalKills ?? 0).ToString("N0"));
        AddFact(facts, Tr("UI_END_CRISES"), (record?.CrisesSurvived ?? 0).ToString());
        string cause = record?.DeathCause;
        string causeName = string.IsNullOrEmpty(cause) || cause == "unknown" ? "—" : EnemyDataLoader.Get(cause)?.Name ?? cause;
        AddFact(facts, Tr("UI_END_FELL_TO"), causeName);
    }

    private HBoxContainer SlotRow(Vector2 position)
    {
        HBoxContainer row = new() { Position = position };
        row.AddThemeConstantOverride("separation", 16);
        _middle.AddChild(row);
        return row;
    }

    /// <summary>Case du build : icône, cadre à la couleur de la rareté, niveau en coin. Vide : cadre éteint.</summary>
    private Control MakeSlot(string iconPath, int level, Color frame)
    {
        PanelContainer slot = new() { CustomMinimumSize = new Vector2(IconSize + 24f, IconSize + 24f) };
        StyleBoxFlat style = new()
        {
            BgColor = SlotColor,
            BorderColor = frame,
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            BorderWidthTop = 3,
            BorderWidthBottom = 3,
        };
        slot.AddThemeStyleboxOverride("panel", style);
        if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
        {
            TextureRect icon = new()
            {
                Texture = GD.Load<Texture2D>(iconPath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                CustomMinimumSize = new Vector2(IconSize, IconSize),
            };
            slot.AddChild(icon);
        }
        if (level > 0)
        {
            Label badge = MakeLabel(string.Format(Tr("UI_END_LEVEL"), level), _boldFont, 18, UITheme.GoldBright, HorizontalAlignment.Right, 5);
            badge.VerticalAlignment = VerticalAlignment.Bottom;
            slot.AddChild(badge);
        }
        return slot;
    }

    private void AddFact(VBoxContainer facts, string caption, string value)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 16);
        Label captionLabel = MakeLabel(caption, _bodyFont, 24, UITheme.TextDim, HorizontalAlignment.Left, 4);
        captionLabel.CustomMinimumSize = new Vector2(250f, 0f);
        row.AddChild(captionLabel);
        row.AddChild(MakeLabel(value, _strongFont, 28, UITheme.TextColor, HorizontalAlignment.Left, 5));
        facts.AddChild(row);
    }

    private void BuildGains(GameManager gm)
    {
        foreach (Node child in _gains.GetChildren())
            child.QueueFree();
        int vestiges = _scoreManager?.VestigesEarned ?? 0;
        if (vestiges > 0)
            _gains.AddChild(MakeGainCard(string.Format(Tr("UI_END_VESTIGES"), vestiges), UITheme.GoldBright));
        if (gm.LastQuestCompletions != null)
        {
            foreach (string quest in gm.LastQuestCompletions)
                _gains.AddChild(MakeGainCard(quest, UITheme.CyanEssence));
        }
        if (gm.LastUnlocks != null)
        {
            foreach (string id in gm.LastUnlocks)
                _gains.AddChild(MakeGainCard(string.Format(Tr("UI_END_UNLOCK"), CharacterDataLoader.Get(id)?.Name ?? id), UITheme.GreenKit));
        }
    }

    private PanelContainer MakeGainCard(string text, Color accent)
    {
        PanelContainer card = new();
        StyleBoxFlat style = new()
        {
            BgColor = SlotColor,
            BorderColor = accent,
            BorderWidthTop = 3,
            ContentMarginLeft = 22,
            ContentMarginRight = 22,
            ContentMarginTop = 14,
            ContentMarginBottom = 14,
        };
        card.AddThemeStyleboxOverride("panel", style);
        card.AddChild(MakeLabel(text, _strongFont, 24, accent, HorizontalAlignment.Center, 4));
        return card;
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
        if (_elapsed > RevealEnd + ButtonGuardSec && !_isRecord)
            SetProcess(false);
    }

    /// <summary>Un appui pendant la révélation la termine ; il ne déclenche jamais un bouton.</summary>
    public override void _Input(InputEvent @event)
    {
        if (!_showing || _elapsed >= RevealEnd || !@event.IsPressed() || @event.IsEcho())
            return;
        if (@event is not (InputEventKey or InputEventMouseButton or InputEventJoypadButton))
            return;
        _elapsed = RevealEnd;
        _buttonsAt = RevealEnd + ButtonGuardSec;
        ApplyReveal();
        GetViewport().SetInputAsHandled();
    }

    private void ApplyReveal()
    {
        float wash = Mathf.Clamp((_elapsed + WashLead) / WashLead, 0f, 1f);
        _wash.Color = new Color(WashColor, WashColor.A * wash * wash);
        float veil = Mathf.Clamp(_elapsed / VeilSec, 0f, 1f);
        _veil.Color = new Color(VeilColor, VeilColor.A * veil);
        _title.Modulate = new Color(1f, 1f, 1f, veil);

        float count = Mathf.Clamp((_elapsed - ScoreStart) / ScoreSec, 0f, 1f);
        float eased = 1f - Mathf.Pow(1f - count, 3f);
        _score.Text = Mathf.RoundToInt(_finalScore * eased).ToString("N0");
        _score.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp((_elapsed - ScoreStart) / 0.15f, 0f, 1f));
        float recordIn = Mathf.Clamp((_elapsed - ScoreStart - ScoreSec) / 0.2f, 0f, 1f);
        // Le record pulse doucement, une fois révélé : c'est la seule célébration de la run.
        float pulse = _isRecord && recordIn >= 1f ? 0.75f + 0.25f * Mathf.Sin(_elapsed * 4f) : 1f;
        _record.Modulate = new Color(pulse, pulse, pulse, recordIn);
        _detail.Modulate = new Color(1f, 1f, 1f, recordIn);

        float middle = Mathf.Clamp((_elapsed - BuildStart) / 0.35f, 0f, 1f);
        _middle.Modulate = new Color(1f, 1f, 1f, middle);
        _middle.Position = new Vector2(0f, 390f + (1f - middle) * 24f);
        float gains = Mathf.Clamp((_elapsed - GainsStart) / 0.35f, 0f, 1f);
        _gains.Modulate = new Color(1f, 1f, 1f, gains);
        _gains.Position = new Vector2(0f, 790f + (1f - gains) * 24f);
        _buttons.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp((_elapsed - GainsStart) / 0.35f, 0f, 1f));
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _restartButton.Disabled = !enabled;
        _hubButton.Disabled = !enabled;
    }

    private static string FormatDuration(float durationSec)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(durationSec));
        return $"{total / 60:00}:{total % 60:00}";
    }

    private void OnRestartPressed()
    {
        AudioManager.PlayUI("sfx_menu_confirmer");
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void OnHubPressed()
    {
        AudioManager.PlayUI("sfx_menu_confirmer");
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }
}
