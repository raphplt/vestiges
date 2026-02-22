using Godot;
using Vestiges.Core;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// HUD en jeu : barre de vie, barre d'XP, indicateur de niveau, score, timer jour/nuit, résumé d'aube.
/// </summary>
public partial class HUD : CanvasLayer
{
    private ProgressBar _hpBar;
    private ProgressBar _xpBar;
    private Label _levelLabel;
    private Label _scoreLabel;
    private ProgressBar _dayNightBar;
    private Label _phaseLabel;
    private Label _nightLabel;
    private PanelContainer _dawnSummary;
    private Label _dawnSummaryLabel;
    private EventBus _eventBus;
    private PlayerProgression _progression;
    private DayNightCycle _dayNightCycle;

    private static readonly Color DayBarColor = new(0.9f, 0.75f, 0.2f);
    private static readonly Color DuskBarColor = new(0.55f, 0.35f, 0.7f);
    private static readonly Color NightBarColor = new(0.5f, 0.15f, 0.15f);
    private static readonly Color DawnBarColor = new(0.85f, 0.85f, 0.9f);

    public override void _Ready()
    {
        _hpBar = GetNode<ProgressBar>("HpBar");
        _xpBar = GetNode<ProgressBar>("XpBar");
        _levelLabel = GetNode<Label>("LevelLabel");
        _scoreLabel = GetNode<Label>("ScoreLabel");
        _dayNightBar = GetNode<ProgressBar>("DayNightBar");
        _phaseLabel = GetNode<Label>("PhaseLabel");
        _nightLabel = GetNode<Label>("NightLabel");

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.XpGained += OnXpChanged;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.ScoreChanged += OnScoreChanged;
        _eventBus.DayPhaseChanged += OnDayPhaseChanged;
        _eventBus.NightSummary += OnNightSummary;

        _hpBar.MinValue = 0;
        _hpBar.MaxValue = 100;
        _hpBar.Value = 100;

        _xpBar.MinValue = 0;
        _xpBar.MaxValue = 1;
        _xpBar.Value = 0;

        _dayNightBar.MinValue = 0;
        _dayNightBar.MaxValue = 1;
        _dayNightBar.Value = 0;

        _levelLabel.Text = "Niv. 1";
        _scoreLabel.Text = "0";
        _phaseLabel.Text = "Jour";
        _nightLabel.Text = "";

        CreateDawnSummaryPanel();
        UpdateBarColor("Day");
    }

    public override void _Process(double delta)
    {
        if (_dayNightCycle != null)
        {
            _dayNightBar.Value = _dayNightCycle.PhaseProgress;
        }
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
            _eventBus.XpGained -= OnXpChanged;
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.ScoreChanged -= OnScoreChanged;
            _eventBus.DayPhaseChanged -= OnDayPhaseChanged;
            _eventBus.NightSummary -= OnNightSummary;
        }
    }

    public void SetProgression(PlayerProgression progression)
    {
        _progression = progression;
    }

    public void SetDayNightCycle(DayNightCycle cycle)
    {
        _dayNightCycle = cycle;
    }

    private void CreateDawnSummaryPanel()
    {
        _dawnSummary = new PanelContainer();
        _dawnSummary.AnchorLeft = 0.5f;
        _dawnSummary.AnchorRight = 0.5f;
        _dawnSummary.AnchorTop = 0.3f;
        _dawnSummary.AnchorBottom = 0.3f;
        _dawnSummary.OffsetLeft = -140;
        _dawnSummary.OffsetRight = 140;
        _dawnSummary.OffsetTop = 0;
        _dawnSummary.OffsetBottom = 80;
        _dawnSummary.Visible = false;

        StyleBoxFlat style = new();
        style.BgColor = new Color(0f, 0f, 0f, 0.7f);
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 12;
        style.ContentMarginBottom = 12;
        _dawnSummary.AddThemeStyleboxOverride("panel", style);

        _dawnSummaryLabel = new Label();
        _dawnSummaryLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _dawnSummaryLabel.VerticalAlignment = VerticalAlignment.Center;
        _dawnSummary.AddChild(_dawnSummaryLabel);

        AddChild(_dawnSummary);
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

    private void OnDayPhaseChanged(string phase)
    {
        string phaseText = phase switch
        {
            "Day" => "Jour",
            "Dusk" => "Crépuscule",
            "Night" => "Nuit",
            "Dawn" => "Aube",
            _ => phase
        };
        _phaseLabel.Text = phaseText;

        if (_dayNightCycle != null && _dayNightCycle.CurrentNight > 0)
            _nightLabel.Text = $"Nuit {_dayNightCycle.CurrentNight}";

        if (phase == "Day")
            HideDawnSummary();

        UpdateBarColor(phase);
    }

    private void OnNightSummary(int nightNumber, int kills, int score)
    {
        _dawnSummaryLabel.Text = $"Nuit {nightNumber} survécue !\nKills : {kills}  —  Score : +{score}";
        _dawnSummary.Visible = true;
        _dawnSummary.Modulate = Colors.White;

        Tween tween = CreateTween();
        tween.TweenInterval(4f);
        tween.TweenProperty(_dawnSummary, "modulate:a", 0f, 1.5f);
        tween.TweenCallback(Callable.From(HideDawnSummary));
    }

    private void HideDawnSummary()
    {
        _dawnSummary.Visible = false;
    }

    private void UpdateBarColor(string phase)
    {
        Color barColor = phase switch
        {
            "Day" => DayBarColor,
            "Dusk" => DuskBarColor,
            "Night" => NightBarColor,
            "Dawn" => DawnBarColor,
            _ => DayBarColor
        };

        Tween tween = CreateTween();
        tween.TweenProperty(_dayNightBar, "modulate", barColor, 1f);
    }
}
