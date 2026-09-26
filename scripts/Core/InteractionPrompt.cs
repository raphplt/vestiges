using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Invite « [touche] action » posée au-dessus d'un objet à portée. La touche suit la dernière entrée utilisée :
/// clavier ou manette. Texte en police pixel sur un fond sombre, au-dessus des entités et du brouillard.
/// </summary>
public partial class InteractionPrompt : Node2D
{
    private const string Action = "interact";
    private static readonly Color Background = new(0.1f, 0.1f, 0.18f, 0.85f);
    private static readonly Color KeyColor = new("D4A843");
    private static readonly Color TextColor = new("E8E0D4");

    private Label _label;
    private const string VerbKey = "CHEST_OPEN_PROMPT";
    private bool _gamepad;

    public override void _Ready()
    {
        TopLevel = true;
        ZIndex = 30;
        Visible = false;
        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        _label.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/pixel-operator/PixelOperator8.ttf"));
        _label.AddThemeFontSizeOverride("font_size", 8);
        _label.AddThemeColorOverride("font_color", TextColor);
        AddChild(_label);
        Refresh();
    }

    public override void _Input(InputEvent @event)
    {
        bool gamepad = @event is InputEventJoypadButton
            || (@event is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > 0.5f);
        bool keyboard = @event is InputEventKey || @event is InputEventMouseButton;
        if ((gamepad || keyboard) && gamepad != _gamepad)
        {
            _gamepad = gamepad;
            Refresh();
        }
    }

    /// <summary>Affiche l'invite, son bas centré sur <paramref name="anchor"/> (coordonnées monde).</summary>
    public void ShowAt(Vector2 anchor)
    {
        GlobalPosition = anchor.Round();
        Visible = true;
    }

    public void HidePrompt() => Visible = false;

    public override void _Draw()
    {
        Rect2 box = new(_label.Position - new Vector2(3, 1), _label.Size + new Vector2(6, 2));
        DrawRect(box, Background);
        DrawRect(box, KeyColor with { A = 0.6f }, false, 1f);
    }

    private void Refresh()
    {
        string key = _gamepad ? InputRemapManager.GetJoyButtonName(Action) : InputRemapManager.GetKeyName(Action);
        _label.Text = $"[{key}] {Tr(VerbKey)}";
        _label.ResetSize();
        _label.Size = _label.GetMinimumSize();
        _label.Position = new Vector2(-Mathf.Round(_label.Size.X / 2f), -_label.Size.Y);
        QueueRedraw();
    }
}
