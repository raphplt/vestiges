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
    private Label _buildLabel;
    private Label _explorationLabel;
    private Label _multiplierLabel;
    private Label _totalLabel;
    private Label _recordLabel;
    private Label _killsLabel;
    private Label _seedLabel;
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
        _panel.OffsetTop = -210;
        _panel.OffsetBottom = 210;
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

        _buildLabel = CreateLabel("", 14, HorizontalAlignment.Left);
        vbox.AddChild(_buildLabel);

        _explorationLabel = CreateLabel("", 14, HorizontalAlignment.Left);
        vbox.AddChild(_explorationLabel);

        _killsLabel = CreateLabel("", 14, HorizontalAlignment.Left);
        vbox.AddChild(_killsLabel);

        vbox.AddChild(CreateSeparator());

        _multiplierLabel = CreateLabel("", 13, HorizontalAlignment.Center);
        _multiplierLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.45f));
        vbox.AddChild(_multiplierLabel);

        _totalLabel = CreateLabel("", 22, HorizontalAlignment.Center);
        vbox.AddChild(_totalLabel);

        _recordLabel = CreateLabel("", 14, HorizontalAlignment.Center);
        vbox.AddChild(_recordLabel);

        _seedLabel = CreateLabel("", 12, HorizontalAlignment.Center);
        _seedLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
        vbox.AddChild(_seedLabel);

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
        int build = _scoreManager?.BuildScore ?? 0;
        int exploration = _scoreManager?.ExplorationScore ?? 0;
        int kills = _scoreManager?.TotalKills ?? 0;
        int nights = _scoreManager?.NightsSurvived ?? 0;
        int best = _scoreManager?.BestScore ?? 0;
        bool isRecord = _scoreManager?.IsNewRecord ?? false;
        float charMult = _scoreManager?.CharacterMultiplier ?? 1f;
        float mutMult = _scoreManager?.MutatorMultiplier ?? 1f;

        _nightsLabel.Text = nights > 0
            ? $"{nights} nuit{(nights > 1 ? "s" : "")} survécue{(nights > 1 ? "s" : "")}"
            : "Mort avant la première nuit";

        _combatLabel.Text = $"Combat : {combat}";
        _survivalLabel.Text = $"Survie : {survival}";
        _bonusLabel.Text = $"Bonus (nuits sans dégât) : {bonus}";
        _buildLabel.Text = build > 0 ? $"Construction : {build}" : "";
        _explorationLabel.Text = exploration > 0 ? $"Exploration : {exploration}" : "";
        _killsLabel.Text = $"Kills : {kills}";

        // Multiplier line (only if relevant)
        float totalMult = charMult * mutMult;
        if (totalMult > 1f + 0.001f)
        {
            string multParts = $"x{charMult:F2}";
            if (mutMult > 1f + 0.001f)
                multParts += $" x {mutMult:F2} (mutateurs)";
            _multiplierLabel.Text = $"Multiplicateur : {multParts} = x{totalMult:F2}";
        }
        else
        {
            _multiplierLabel.Text = "";
        }

        _totalLabel.Text = $"SCORE : {total}";

        if (isRecord)
            _recordLabel.Text = "NOUVEAU RECORD !";
        else
            _recordLabel.Text = $"Meilleur : {best}";

        // Seed display
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        ulong seed = gm.RunSeed;
        _seedLabel.Text = seed > 0 ? $"Seed : {seed}" : "";

        // Vestiges earned
        int vestigesEarned = _scoreManager?.VestigesEarned ?? 0;
        _vestigesLabel.Text = vestigesEarned > 0
            ? $"+{vestigesEarned} Vestiges"
            : "";

        // Character unlocks
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
