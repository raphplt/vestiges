using Godot;
using Vestiges.Core;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// HUD en jeu : barre de vie, barre d'XP, indicateur de niveau, score.
/// </summary>
public partial class HUD : CanvasLayer
{
    private ProgressBar _hpBar;
    private ProgressBar _xpBar;
    private Label _levelLabel;
    private Label _scoreLabel;
    private EventBus _eventBus;
    private PlayerProgression _progression;

    public override void _Ready()
    {
        _hpBar = GetNode<ProgressBar>("HpBar");
        _xpBar = GetNode<ProgressBar>("XpBar");
        _levelLabel = GetNode<Label>("LevelLabel");
        _scoreLabel = GetNode<Label>("ScoreLabel");

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.XpGained += OnXpChanged;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.ScoreChanged += OnScoreChanged;

        _hpBar.MinValue = 0;
        _hpBar.MaxValue = 100;
        _hpBar.Value = 100;

        _xpBar.MinValue = 0;
        _xpBar.MaxValue = 1;
        _xpBar.Value = 0;

        _levelLabel.Text = "Niv. 1";
        _scoreLabel.Text = "0";
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
            _eventBus.XpGained -= OnXpChanged;
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.ScoreChanged -= OnScoreChanged;
        }
    }

    public void SetProgression(PlayerProgression progression)
    {
        _progression = progression;
    }

    private void OnPlayerDamaged(float currentHp, float maxHp)
    {
        _hpBar.MaxValue = maxHp;
        _hpBar.Value = Mathf.Max(0, currentHp);

        float ratio = currentHp / maxHp;
        if (ratio < 0.25f)
            _hpBar.Modulate = new Color(1f, 0.2f, 0.2f);
        else if (ratio < 0.5f)
            _hpBar.Modulate = new Color(1f, 0.7f, 0.2f);
        else
            _hpBar.Modulate = Colors.White;
    }

    private void OnXpChanged(float _amount)
    {
        if (_progression == null)
            return;

        _xpBar.MaxValue = _progression.XpToNextLevel;
        _xpBar.Value = _progression.CurrentXp;
    }

    private void OnLevelUp(int newLevel)
    {
        _levelLabel.Text = $"Niv. {newLevel}";

        if (_progression != null)
        {
            _xpBar.MaxValue = _progression.XpToNextLevel;
            _xpBar.Value = _progression.CurrentXp;
        }
    }

    private void OnScoreChanged(int newScore)
    {
        _scoreLabel.Text = newScore.ToString();
    }
}
