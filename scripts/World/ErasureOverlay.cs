using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Overlay simple de lecture des zones effacees.
/// Plus une zone perd de memoire, plus la teinte devient froide et opaque.
/// </summary>
public partial class ErasureOverlay : Node2D
{
    private readonly ErasureManager _manager;
    private readonly int _cellSize;
    private readonly List<(Vector2I cell, float memory)> _visibleCells = new();

    private static readonly Color FragileColor = new(0.34f, 0.38f, 0.49f);
    private static readonly Color FrayedColor = new(0.42f, 0.42f, 0.5f);
    private static readonly Color ErasedColor = new(0.76f, 0.82f, 0.9f);

    public ErasureOverlay(ErasureManager manager, int cellSize)
    {
        _manager = manager;
        _cellSize = cellSize;
    }

    public override void _Draw()
    {
        if (_manager == null)
            return;

        Camera2D camera = GetViewport().GetCamera2D();
        if (camera == null)
            return;

        Vector2 viewportSize = GetViewportRect().Size;
        float zoom = camera.Zoom.X;
        Vector2 cameraPos = camera.GlobalPosition;
        Vector2 halfView = viewportSize / (2f * zoom);

        Rect2 viewRect = new(
            cameraPos - halfView - new Vector2(_cellSize, _cellSize),
            halfView * 2f + new Vector2(_cellSize * 2, _cellSize * 2));

        _manager.GetCellsInRect(viewRect, _visibleCells);

        foreach ((Vector2I cell, float memory) in _visibleCells)
        {
            if (memory >= 0.98f)
                continue;

            float alpha = Mathf.Clamp((1f - memory) * 0.58f, 0f, 0.58f);
            if (alpha <= 0.015f)
                continue;

            Color tint = memory switch
            {
                > 0.50f => FragileColor,
                > 0.25f => FrayedColor,
                _ => ErasedColor
            };

            DrawRect(
                new Rect2(_manager.CellToWorld(cell), new Vector2(_cellSize, _cellSize)),
                new Color(tint, alpha));
        }
    }
}
