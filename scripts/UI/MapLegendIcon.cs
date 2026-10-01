using Godot;

namespace Vestiges.UI;

/// <summary>Pictogramme d'une ligne de la légende de la carte (plan 24 A6), le même que sur la carte.</summary>
public partial class MapLegendIcon : Control
{
    private readonly Texture2D _icon;
    private readonly Color _color;
    private readonly bool _hatched;

    public MapLegendIcon() : this(null, Colors.White)
    {
    }

    public MapLegendIcon(Texture2D icon, Color color, bool hatched = false)
    {
        _icon = icon;
        _color = color;
        _hatched = hatched;
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        CustomMinimumSize = new Vector2(8f, 10f);
    }

    public override void _Draw()
    {
        if (_icon != null)
            DrawTexture(_icon, new Vector2(2, 3), _color);
        else
        {
            DrawRect(new Rect2(1, 2, 6, 6), _color);
            if (_hatched)
                for (int i = 0; i < 6; i += 2)
                    DrawLine(new Vector2(1, 2 + i), new Vector2(6 - i, 7), _color.Lightened(0.25f));
        }
    }
}
