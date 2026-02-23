using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Score;

namespace Vestiges.UI;

/// <summary>
/// Écran de game over : score détaillé, record, nuits survécues, bouton relancer.
/// Effet de mort : fade to white avant affichage.
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
    private PanelContainer _panel;
    private Label _titleLabel;
    private Label _nightsLabel;
    private Label _combatLabel;
    private Label _survivalLabel;
    private Label _bonusLabel;
    private Label _totalLabel;
    private Label _recordLabel;
    private Label _killsLabel;
    private Label _vestigesLabel;
    private Label _unlocksLabel;
    private Button _restartButton;
    private Button _hubButton;
    private EventBus _eventBus;
    private ScoreManager _scoreManager;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EntityDied += OnEntityDied;

        BuildUI();
        Hide();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.EntityDied -= OnEntityDied;
    }

    public void SetScoreManager(ScoreManager scoreManager)
    {
        _scoreManager = scoreManager;
    }

    private void BuildUI()
    {
        _panel = new PanelContainer();
        _panel.AnchorLeft = 0.5f;
        _panel.AnchorRight = 0.5f;
        _panel.AnchorTop = 0.5f;
        _panel.AnchorBottom = 0.5f;
        _panel.OffsetLeft = -180;
        _panel.OffsetRight = 180;
        _panel.OffsetTop = -160;
        _panel.OffsetBottom = 160;
        _panel.GrowHorizontal = Control.GrowDirection.Both;
        _panel.GrowVertical = Control.GrowDirection.Both;

        StyleBoxFlat style = new();
        style.BgColor = new Color(0.05f, 0.03f, 0.08f, 0.9f);
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.ContentMarginLeft = 24;
        style.ContentMarginRight = 24;
        style.ContentMarginTop = 20;
        style.ContentMarginBottom = 20;
        _panel.AddThemeStyleboxOverride("panel", style);

        VBoxContainer vbox = new();
        vbox.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(vbox);

        _titleLabel = CreateLabel("GAME OVER", 28, HorizontalAlignment.Center);
        vbox.AddChild(_titleLabel);

        vbox.AddChild(CreateSeparator());

        _nightsLabel = CreateLabel("", 18, HorizontalAlignment.Center);
        vbox.AddChild(_nightsLabel);

        vbox.AddChild(CreateSeparator());

        _combatLabel = CreateLabel("", 14, HorizontalAlignment.Left);
        vbox.AddChild(_combatLabel);

        _survivalLabel = CreateLabel("", 14, HorizontalAlignment.Left);
        vbox.AddChild(_survivalLabel);

        _bonusLabel = CreateLabel("", 14, HorizontalAlignment.Left);
        vbox.AddChild(_bonusLabel);

        _killsLabel = CreateLabel("", 14, HorizontalAlignment.Left);
        vbox.AddChild(_killsLabel);

        vbox.AddChild(CreateSeparator());

        _totalLabel = CreateLabel("", 22, HorizontalAlignment.Center);
        vbox.AddChild(_totalLabel);

        _recordLabel = CreateLabel("", 14, HorizontalAlignment.Center);
        vbox.AddChild(_recordLabel);

        _vestigesLabel = CreateLabel("", 14, HorizontalAlignment.Center);
        _vestigesLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.75f, 0.4f));
        vbox.AddChild(_vestigesLabel);

        _unlocksLabel = CreateLabel("", 14, HorizontalAlignment.Center);
        _unlocksLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.9f, 0.5f));
        vbox.AddChild(_unlocksLabel);

        HBoxContainer buttonRow = new();
        buttonRow.AddThemeConstantOverride("separation", 12);
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(buttonRow);

        _restartButton = new Button();
        _restartButton.Text = "Relancer";
        _restartButton.CustomMinimumSize = new Vector2(130, 38);
        _restartButton.Pressed += OnRestartPressed;
        buttonRow.AddChild(_restartButton);

        _hubButton = new Button();
        _hubButton.Text = "Retour au Hub";
        _hubButton.CustomMinimumSize = new Vector2(130, 38);
        _hubButton.Pressed += OnHubPressed;
        buttonRow.AddChild(_hubButton);

        AddChild(_panel);
    }

    private Label CreateLabel(string text, int fontSize, HorizontalAlignment alignment)
    {
        Label label = new();
        label.Text = text;
        label.HorizontalAlignment = alignment;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private HSeparator CreateSeparator()
    {
        return new HSeparator();
    }

    private void OnEntityDied(Node entity)
    {
        if (entity is not Player)
            return;

        _scoreManager?.SaveEndOfRun();
        StartDeathSequence();
    }

    private void StartDeathSequence()
    {
        CanvasModulate canvasModulate = GetTree().CurrentScene.GetNodeOrNull<CanvasModulate>("CanvasModulate");

        if (canvasModulate != null)
        {
            Tween tween = CreateTween();
            tween.TweenProperty(canvasModulate, "color", new Color(0.95f, 0.93f, 0.9f), 1.5f)
                .SetTrans(Tween.TransitionType.Expo)
                .SetEase(Tween.EaseType.In);
            tween.TweenCallback(Callable.From(ShowGameOver));
        }
        else
        {
            ShowGameOver();
        }
    }

    private void ShowGameOver()
    {
        int total = _scoreManager?.CurrentScore ?? 0;
        int combat = _scoreManager?.CombatScore ?? 0;
        int survival = _scoreManager?.SurvivalScore ?? 0;
        int bonus = _scoreManager?.BonusScore ?? 0;
        int kills = _scoreManager?.TotalKills ?? 0;
        int nights = _scoreManager?.NightsSurvived ?? 0;
        int best = _scoreManager?.BestScore ?? 0;
        bool isRecord = _scoreManager?.IsNewRecord ?? false;

        _nightsLabel.Text = nights > 0
            ? $"{nights} nuit{(nights > 1 ? "s" : "")} survécue{(nights > 1 ? "s" : "")}"
            : "Mort avant la première nuit";

        _combatLabel.Text = $"Combat : {combat}";
        _survivalLabel.Text = $"Survie : {survival}";
        _bonusLabel.Text = $"Bonus (nuits sans dégât) : {bonus}";
        _killsLabel.Text = $"Kills : {kills}";
        _totalLabel.Text = $"SCORE : {total}";

        if (isRecord)
            _recordLabel.Text = "NOUVEAU RECORD !";
        else
            _recordLabel.Text = $"Meilleur : {best}";

        // Vestiges earned
        int vestigesEarned = _scoreManager?.VestigesEarned ?? 0;
        _vestigesLabel.Text = vestigesEarned > 0
            ? $"+{vestigesEarned} Vestiges"
            : "";

        // Character unlocks
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        List<string> unlocks = gm.LastUnlocks;
        if (unlocks != null && unlocks.Count > 0)
        {
            List<string> names = new();
            foreach (string id in unlocks)
            {
                CharacterData data = CharacterDataLoader.Get(id);
                names.Add(data?.Name ?? id);
            }
            _unlocksLabel.Text = string.Join(", ", names) + " débloqué !";
        }
        else
        {
            _unlocksLabel.Text = "";
        }

        _panel.Visible = true;
        Visible = true;
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void OnHubPressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
    }

    private new void Hide()
    {
        _panel.Visible = false;
        Visible = false;
    }
}
