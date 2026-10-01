using Godot;

namespace Vestiges.UI;

/// <summary>Pictogramme d'une ligne de la légende de la carte (plan 24 A6), le même que sur la carte.</summary>
public partial class MapLegendIcon : Control
{
    private readonly string[] _icon;
    private readonly Color _color;

    public MapLegendIcon() : this(new[] { "#" }, Colors.White)
    {
    }

    public MapLegendIcon(string[] icon, Color color)
    {
        _icon = icon;
        _color = color;
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(8f, 10f);
    }

    public override void _Draw() =>
        Minimap.DrawPattern(this, new Vector2(4f - _icon[0].Length / 2, 5f - _icon.Length / 2), _icon, _color);
}
