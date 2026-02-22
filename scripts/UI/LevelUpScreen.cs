using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Écran de choix de perk au level up.
/// Pause le jeu, affiche 3 choix, reprend après sélection.
/// </summary>
public partial class LevelUpScreen : CanvasLayer
{
    private PanelContainer _panel;
    private VBoxContainer _container;
    private Label _title;
    private PerkManager _perkManager;
    private string[] _currentChoices;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("Panel");
        _container = GetNode<VBoxContainer>("Panel/Padding/VBox");
        _title = GetNode<Label>("Panel/Padding/VBox/Title");
        Hide();
    }

    public void SetPerkManager(PerkManager perkManager)
    {
        _perkManager = perkManager;
        _perkManager.PerkChoicesReady += OnPerkChoicesReady;
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
                CustomMinimumSize = new Vector2(300, 50),
                Text = $"{data.Name} — {data.Description} {stackText}"
            };

            string capturedId = perkId;
            button.Pressed += () => OnPerkSelected(capturedId);

            _container.AddChild(button);
        }
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
