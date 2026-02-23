using Godot;
using Vestiges.Core;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// HUD en jeu : barre de vie, barre d'XP, indicateur de niveau, score, timer jour/nuit, ressources, résumé d'aube.
/// </summary>
public partial class HUD : CanvasLayer
{
    private ProgressBar _hpBar;
    private Label _hpValueLabel;
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

    // Resource display
    private Label _woodLabel;
    private Label _stoneLabel;
    private Label _metalLabel;
    private Label _capacityLabel;
    private ProgressBar _capacityBar;

    // Contextual hint
    private Label _interactHint;

    private static readonly Color DayBarColor = new(0.9f, 0.75f, 0.2f);
    private static readonly Color DuskBarColor = new(0.55f, 0.35f, 0.7f);
    private static readonly Color NightBarColor = new(0.5f, 0.15f, 0.15f);
    private static readonly Color DawnBarColor = new(0.85f, 0.85f, 0.9f);
    private static readonly Color HpHighColor = Colors.White;
    private static readonly Color HpMediumColor = new(1f, 0.7f, 0.2f);
    private static readonly Color HpLowColor = new(1f, 0.2f, 0.2f);

    public override void _Ready()
    {
        _hpBar = GetNode<ProgressBar>("HpBar");
        _hpValueLabel = GetNode<Label>("HpBar/HpValueLabel");
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
        _eventBus.InventoryChanged += OnInventoryChanged;

        _hpBar.MinValue = 0;
        _hpBar.MaxValue = 100;
        _hpBar.Value = 100;
        UpdateHpDisplay(100f, 100f);

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
        CreateResourcePanel();
        CreateInteractHint();
        UpdateBarColor("Day");
    }

    public override void _Process(double delta)
    {
        if (_dayNightCycle != null)
            _dayNightBar.Value = _dayNightCycle.PhaseProgress;

        UpdateInteractHint();
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
            _eventBus.InventoryChanged -= OnInventoryChanged;
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

    // --- Resource Panel ---

    private void CreateResourcePanel()
    {
        PanelContainer panel = new();
        panel.AnchorLeft = 0f;
        panel.AnchorRight = 0f;
        panel.AnchorTop = 1f;
        panel.AnchorBottom = 1f;
        panel.OffsetLeft = 10;
        panel.OffsetRight = 180;
        panel.OffsetTop = -140;
        panel.OffsetBottom = -10;

        StyleBoxFlat style = new();
        style.BgColor = new Color(0f, 0f, 0f, 0.6f);
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 6;
        style.ContentMarginBottom = 6;
        panel.AddThemeStyleboxOverride("panel", style);

        VBoxContainer vbox = new();
        vbox.AddThemeConstantOverride("separation", 3);
        panel.AddChild(vbox);

        // Title
        Label titleLabel = new();
        titleLabel.Text = "Inventaire";
        titleLabel.AddThemeFontSizeOverride("font_size", 12);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(titleLabel);

        // Resource rows
        _woodLabel = CreateResourceRow("Bois", new Color(0.55f, 0.4f, 0.08f));
        _stoneLabel = CreateResourceRow("Pierre", new Color(0.66f, 0.6f, 0.47f));
        _metalLabel = CreateResourceRow("Métal", new Color(0.44f, 0.53f, 0.63f));

        vbox.AddChild(_woodLabel);
        vbox.AddChild(_stoneLabel);
        vbox.AddChild(_metalLabel);

        // Capacity bar
        _capacityBar = new ProgressBar();
        _capacityBar.CustomMinimumSize = new Vector2(0, 6);
        _capacityBar.ShowPercentage = false;
        _capacityBar.MinValue = 0;
        _capacityBar.MaxValue = Base.Inventory.MaxCapacity;
        _capacityBar.Value = 0;

        StyleBoxFlat fillStyle = new();
        fillStyle.BgColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        fillStyle.CornerRadiusBottomLeft = 2;
        fillStyle.CornerRadiusBottomRight = 2;
        fillStyle.CornerRadiusTopLeft = 2;
        fillStyle.CornerRadiusTopRight = 2;
        _capacityBar.AddThemeStyleboxOverride("fill", fillStyle);

        StyleBoxFlat bgStyle = new();
        bgStyle.BgColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        bgStyle.CornerRadiusBottomLeft = 2;
        bgStyle.CornerRadiusBottomRight = 2;
        bgStyle.CornerRadiusTopLeft = 2;
        bgStyle.CornerRadiusTopRight = 2;
        _capacityBar.AddThemeStyleboxOverride("background", bgStyle);
        vbox.AddChild(_capacityBar);

        _capacityLabel = new Label();
        _capacityLabel.Text = $"0 / {Base.Inventory.MaxCapacity}";
        _capacityLabel.AddThemeFontSizeOverride("font_size", 11);
        _capacityLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        _capacityLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_capacityLabel);

        AddChild(panel);
    }

    private Label CreateResourceRow(string name, Color color)
    {
        Label label = new();
        label.Text = $"{name}  0";
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 14);
        return label;
    }

    private void OnInventoryChanged(string resourceId, int newAmount)
    {
        switch (resourceId)
        {
            case "wood":
                _woodLabel.Text = $"Bois  {newAmount}";
                break;
            case "stone":
                _stoneLabel.Text = $"Pierre  {newAmount}";
                break;
            case "metal":
                _metalLabel.Text = $"Métal  {newAmount}";
                break;
        }

        UpdateCapacityDisplay();
    }

    private void UpdateCapacityDisplay()
    {
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode == null)
            return;

        Base.Inventory inventory = playerNode.GetNodeOrNull<Base.Inventory>("Inventory");
        if (inventory == null)
            return;

        int total = inventory.TotalCount;
        int max = Base.Inventory.MaxCapacity;
        _capacityBar.Value = total;
        _capacityLabel.Text = $"{total} / {max}";

        Color barColor = total >= max
            ? new Color(0.8f, 0.3f, 0.3f, 0.8f)
            : total >= max * 0.8f
                ? new Color(0.8f, 0.6f, 0.2f, 0.8f)
                : new Color(0.6f, 0.6f, 0.6f, 0.8f);

        StyleBoxFlat fillStyle = (StyleBoxFlat)_capacityBar.GetThemeStylebox("fill");
        fillStyle.BgColor = barColor;
    }

    // --- Interact Hint ---

    private void CreateInteractHint()
    {
        _interactHint = new Label();
        _interactHint.AnchorLeft = 0.5f;
        _interactHint.AnchorRight = 0.5f;
        _interactHint.AnchorTop = 0.75f;
        _interactHint.AnchorBottom = 0.75f;
        _interactHint.OffsetLeft = -120;
        _interactHint.OffsetRight = 120;
        _interactHint.HorizontalAlignment = HorizontalAlignment.Center;
        _interactHint.AddThemeFontSizeOverride("font_size", 14);
        _interactHint.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.8f));
        _interactHint.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
        _interactHint.AddThemeConstantOverride("shadow_offset_x", 1);
        _interactHint.AddThemeConstantOverride("shadow_offset_y", 1);
        _interactHint.Visible = false;
        AddChild(_interactHint);
    }

    private void UpdateInteractHint()
    {
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is not Core.Player player || player.IsDead)
        {
            _interactHint.Visible = false;
            return;
        }

        if (player.IsHarvesting)
        {
            _interactHint.Visible = false;
            return;
        }

        float interactRange = player.InteractRange;
        Vector2 playerPos = player.GlobalPosition;

        // Check for damaged structures first
        foreach (Node node in GetTree().GetNodesInGroup("structures"))
        {
            if (node is Base.Structure structure && !structure.IsDestroyed && structure.HpRatio < 1f)
            {
                float dist = playerPos.DistanceTo(structure.GlobalPosition);
                if (dist < interactRange)
                {
                    int hpPercent = (int)(structure.HpRatio * 100);
                    _interactHint.Text = $"[E] Réparer ({hpPercent}%)";
                    _interactHint.Visible = true;
                    return;
                }
            }
        }

        // Check for harvestable resources
        foreach (Node node in GetTree().GetNodesInGroup("resources"))
        {
            if (node is Base.ResourceNode res && !res.IsExhausted)
            {
                float dist = playerPos.DistanceTo(res.GlobalPosition);
                if (dist < interactRange)
                {
                    string resName = GetResourceDisplayName(res.ResourceId);
                    _interactHint.Text = $"[E] Récolter {resName}";
                    _interactHint.Visible = true;
                    return;
                }
            }
        }

        _interactHint.Visible = false;
    }

    // --- Dawn Summary ---

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

    // --- Callbacks ---

    private void OnPlayerDamaged(float currentHp, float maxHp)
    {
        UpdateHpDisplay(currentHp, maxHp);
    }

    private void UpdateHpDisplay(float currentHp, float maxHp)
    {
        float clampedMaxHp = Mathf.Max(1f, maxHp);
        float clampedHp = Mathf.Clamp(currentHp, 0f, clampedMaxHp);

        _hpBar.MaxValue = clampedMaxHp;
        _hpBar.Value = clampedHp;
        _hpValueLabel.Text = $"PV : {Mathf.RoundToInt(clampedHp)} / {Mathf.RoundToInt(clampedMaxHp)}";

        float ratio = clampedHp / clampedMaxHp;
        if (ratio < 0.25f)
            _hpBar.Modulate = HpLowColor;
        else if (ratio < 0.5f)
            _hpBar.Modulate = HpMediumColor;
        else
            _hpBar.Modulate = HpHighColor;
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

    private static string GetResourceDisplayName(string id)
    {
        return id switch
        {
            "wood" => "Bois",
            "stone" => "Pierre",
            "metal" => "Métal",
            _ => id
        };
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
