using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>Éclat d'un Mémorial à ramasser : sprite qui flotte, petite colonne de lumière pour le trouver.</summary>
public partial class MemoryShard : Node2D
{
    private Sprite2D _sprite;
    private float _time;

    public void Initialize(Texture2D texture)
    {
        LightColumn column = new() { Name = "LightColumn" };
        AddChild(column);
        column.Configure(RarityPalette.Colors("memorial"), 70f, 1f, 7f);

        _sprite = new Sprite2D { Texture = texture, Centered = false, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
        Vector2 pivot = PropManifest.TryGet(texture, out PropManifest.Entry entry)
            ? entry.Pivot
            : new Vector2(texture.GetWidth() * 0.5f, texture.GetHeight());
        _sprite.Offset = -pivot.Round();
        AddChild(_sprite);
        _time = GD.Randf() * Mathf.Tau;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _sprite.Position = new Vector2(0f, Mathf.Round(Mathf.Sin(_time * 2.4f) * 2f - 3f));
    }
}
