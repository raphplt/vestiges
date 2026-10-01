using Godot;

namespace Vestiges.UI;

/// <summary>Pictogramme d'une ligne de la légende de la carte (plan 24 A6), le même que sur la carte.</summary>
public partial class MapLegendIcon : Control
{
    private readonly Texture2D _icon;
    private readonly Color _color;

    public MapLegendIcon() : this(null, Colors.White)
    {
    }

    public MapLegendIcon(Texture2D icon, Color color)
    {
        _icon = icon;
        _color = color;
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        CustomMinimumSize = new Vector2(8f, 10f);
    }

    public override void _Draw()
    {
        if (_icon != null)
            DrawTexture(_icon, new Vector2(2, 3), _color);
    }
}
