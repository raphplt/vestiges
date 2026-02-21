using Godot;
using Vestiges.Core;
using Vestiges.Score;

namespace Vestiges.UI;

/// <summary>
/// Écran de game over : score final, kills, bouton relancer.
/// </summary>
public partial class GameOverScreen : CanvasLayer
{
    private PanelContainer _panel;
    private Label _titleLabel;
    private Label _scoreLabel;
    private Label _killsLabel;
    private Button _restartButton;
    private EventBus _eventBus;
    private ScoreManager _scoreManager;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("Panel");
        _titleLabel = GetNode<Label>("Panel/VBox/Title");
        _scoreLabel = GetNode<Label>("Panel/VBox/Score");
        _killsLabel = GetNode<Label>("Panel/VBox/Kills");
        _restartButton = GetNode<Button>("Panel/VBox/RestartButton");

        _restartButton.Pressed += OnRestartPressed;

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EntityDied += OnEntityDied;

        ProcessMode = ProcessModeEnum.Always;
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

    private void OnEntityDied(Node entity)
    {
        if (entity is not Player)
            return;

        int score = _scoreManager?.CurrentScore ?? 0;
        int kills = _scoreManager?.TotalKills ?? 0;

        _titleLabel.Text = "GAME OVER";
        _scoreLabel.Text = $"Score : {score}";
        _killsLabel.Text = $"Kills : {kills}";

        Show();
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private new void Show()
    {
        _panel.Visible = true;
        Visible = true;
    }

    private new void Hide()
    {
        _panel.Visible = false;
        Visible = false;
    }
}
