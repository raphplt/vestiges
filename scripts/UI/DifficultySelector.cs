using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Choix du niveau de difficulté avant le départ (plan 50 T3) : gauche et droite le changent quand le bouton a le focus,
/// un clic passe au suivant. Les niveaux fermés restent visibles, avec ce qui les ouvre ; on ne part pas sur l'un d'eux.
/// </summary>
public partial class DifficultySelector : VBoxContainer
{
    [Signal] public delegate void ChangedEventHandler();

    private Button _button;
    private Label _description;
    private int _index;

    public string SelectedId => DifficultyDataLoader.All[_index].Id;
    public bool IsSelectedUnlocked => MetaSaveManager.IsDifficultyUnlocked(SelectedId);
    public Button Button => _button;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        _button = MenuSkin.Button("");
        _button.Name = "DifficultyButton";
        _button.CustomMinimumSize = new Vector2(0, 64);
        _button.Pressed += () => Step(1);
        _button.GuiInput += OnButtonInput;
        AddChild(_button);
        _description = UITheme.MakeLabel("", TextRole.Small, UITheme.TextDim, align: HorizontalAlignment.Center);
        _description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddChild(_description);
        Select(MetaSaveManager.GetLastDifficulty());
    }

    public void Select(string difficultyId)
    {
        _index = Mathf.Max(DifficultyDataLoader.IndexOf(difficultyId), 0);
        Display();
    }

    private void OnButtonInput(InputEvent @event)
    {
        int step = @event.IsActionPressed("ui_left") ? -1 : @event.IsActionPressed("ui_right") ? 1 : 0;
        if (step == 0)
            return;
        Step(step);
        _button.AcceptEvent();
    }

    private void Step(int step)
    {
        int count = DifficultyDataLoader.All.Count;
        _index = (_index + step + count) % count;
        Display();
        EmitSignal(SignalName.Changed);
    }

    private void Display()
    {
        DifficultyLevel level = DifficultyDataLoader.All[_index];
        bool unlocked = MetaSaveManager.IsDifficultyUnlocked(level.Id);
        _button.Text = $"‹   {Tr("UI_DIFFICULTY")} : {Tr(level.NameKey)}   ›";
        _button.SelfModulate = unlocked ? Colors.White : new Color(0.62f, 0.6f, 0.66f);
        if (!unlocked && _index > 0)
            _description.Text = string.Format(Tr("UI_DIFFICULTY_LOCKED"), Tr(DifficultyDataLoader.All[_index - 1].NameKey));
        else
        {
            _description.Text = Tr(level.DescKey);
            if (!Mathf.IsEqualApprox(level.Vestiges, 1f))
                _description.Text += "  ·  " + string.Format(Tr("UI_DIFFICULTY_VESTIGES"),
                    level.Vestiges.ToString("0.##", PlayerSheet.French));
        }
        _description.AddThemeColorOverride("font_color", unlocked ? UITheme.TextDim : UITheme.TextVeryDim);
    }
}
