using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Écran de choix de perk au level up.
/// Pause le jeu, affiche 3 choix avec couleur de rarity, reprend après sélection.
/// Affiche les notifications de synergies activées.
/// </summary>
public partial class LevelUpScreen : CanvasLayer
{
    private PanelContainer _panel;
    private VBoxContainer _container;
    private Label _title;
    private Label _synergyNotification;
    private PerkManager _perkManager;
    private string[] _currentChoices;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("Panel");
        _container = GetNode<VBoxContainer>("Panel/Padding/VBox");
        _title = GetNode<Label>("Panel/Padding/VBox/Title");

        CreateSynergyNotification();
        Hide();
    }

    public void SetPerkManager(PerkManager perkManager)
    {
        _perkManager = perkManager;
        _perkManager.PerkChoicesReady += OnPerkChoicesReady;
        _perkManager.SynergyActivated += OnSynergyActivated;
    }

    private void CreateSynergyNotification()
    {
        _synergyNotification = new Label();
        _synergyNotification.HorizontalAlignment = HorizontalAlignment.Center;
        _synergyNotification.VerticalAlignment = VerticalAlignment.Center;
        _synergyNotification.AddThemeFontSizeOverride("font_size", 22);
        _synergyNotification.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        _synergyNotification.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _synergyNotification.OffsetTop = 80;
        _synergyNotification.Visible = false;
        _synergyNotification.ProcessMode = ProcessModeEnum.Always;
        AddChild(_synergyNotification);
    }

    private void OnSynergyActivated(string synergyId, string notification)
    {
        _synergyNotification.Text = notification;
        _synergyNotification.Visible = true;
        _synergyNotification.Modulate = new Color(1f, 1f, 1f, 1f);

        Tween tween = CreateTween();
        tween.TweenInterval(2.0f);
        tween.TweenProperty(_synergyNotification, "modulate:a", 0f, 1.0f);
        tween.TweenCallback(Callable.From(() => _synergyNotification.Visible = false));
    }

    private void OnPerkChoicesReady(string[] perkIds)
    {
        _currentChoices = perkIds;
        if (_currentChoices.Length == 0)
            return;

        ClearButtons();
        BuildButtons();
        Show();
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.Always;
    }

    private void BuildButtons()
    {
        _title.Text = "LEVEL UP !";

        foreach (string perkId in _currentChoices)
        {
            PerkData data = PerkDataLoader.Get(perkId);
            if (data == null)
                continue;

            int currentStacks = _perkManager.GetStacks(perkId);
            string stackText = $"({currentStacks}/{data.MaxStacks})";

            Button button = new()
            {
                CustomMinimumSize = new Vector2(340, 55),
                Text = $"{data.Name} — {data.Description} {stackText}"
            };

            Color rarityColor = GetRarityColor(data.Rarity);
            button.AddThemeColorOverride("font_color", rarityColor);
            button.AddThemeColorOverride("font_hover_color", rarityColor);

            string capturedId = perkId;
            button.Pressed += () => OnPerkSelected(capturedId);

            _container.AddChild(button);
        }
    }

    private static Color GetRarityColor(string rarity)
    {
        return rarity switch
        {
            "common" => new Color(0.9f, 0.9f, 0.9f),
            "uncommon" => new Color(0.4f, 0.73f, 0.42f),
            "rare" => new Color(1f, 0.7f, 0f),
            _ => new Color(0.9f, 0.9f, 0.9f)
        };
    }

    private void OnPerkSelected(string perkId)
    {
        _perkManager.SelectPerk(perkId);
        Hide();
        GetTree().Paused = false;
    }

    private void ClearButtons()
    {
        foreach (Node child in _container.GetChildren())
        {
            if (child is Button)
                child.QueueFree();
        }
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
