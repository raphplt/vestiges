using Godot;

namespace Vestiges.UI;

/// <summary>Icône d'éliminations de 10 × 10 pixels, os jauni et contour sel-out (plan 25 S6).</summary>
public partial class PixelSkull : Control
{
    private Texture2D _texture;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        CustomMinimumSize = new Vector2(10, 10);
        _texture = GD.Load<Texture2D>("res://assets/ui/hud/plan25/kill_skull.png");
    }

    public override void _Draw()
    {
        if (_texture != null)
            DrawTexture(_texture, Vector2.Zero);
    }
}
