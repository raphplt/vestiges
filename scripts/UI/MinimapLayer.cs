using System;
using Godot;

namespace Vestiges.UI;

/// <summary>
/// Calque de la minimap : il ne fait que dessiner ce que son propriétaire lui confie. Le masque, le terrain et l'anneau
/// sont des calques distincts pour que le disque du radar découpe le terrain sans découper l'anneau (plan 37 R4).
/// </summary>
public partial class MinimapLayer : Control
{
    public Action<MinimapLayer> Paint { get; set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Draw() => Paint?.Invoke(this);
}
