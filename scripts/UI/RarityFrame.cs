using Godot;

namespace Vestiges.UI;

/// <summary>Scintillement de la frise : seul l'atlas d'un style existant change, quatre poses mises en cache.</summary>
public partial class RarityFrame : Node
{
    private PanelContainer _panel;
    private StyleBoxTexture _style;
    private Texture2D[] _frames;
    private float _age;
    private int _frame;

    public void Configure(PanelContainer panel, StyleBoxTexture style, Texture2D[] frames)
    {
        _panel = panel;
        _style = style;
        _frames = frames;
        _frame = 0;
        _age = 0f;
        SetProcess(frames.Length > 1);
    }

    public override void _Process(double delta)
    {
        if (!_panel.IsVisibleInTree())
            return;
        _age += (float)delta;
        int frame = (int)(_age * RarityArt.Fps) % _frames.Length;
        if (frame == _frame)
            return;
        _frame = frame;
        _style.Texture = _frames[frame];
    }
}
