using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Rendu visuel de la memoire de zone. Dessine des rectangles semi-transparents
/// violaces sur les zones a faible memoire. Plus la memoire est basse, plus l'overlay
/// est opaque et colore.
/// </summary>
public partial class ZoneMemoryOverlay : Node2D
{
	private readonly ZoneMemoryManager _manager;
	private readonly int _cellSize;
	private readonly List<(Vector2I cell, float memory)> _visibleCells = new();

	// Couleur de corruption : violet sombre
	private static readonly Color FadedColor = new(0.15f, 0.05f, 0.2f);

	public ZoneMemoryOverlay(ZoneMemoryManager manager, int cellSize)
	{
		_manager = manager;
		_cellSize = cellSize;
	}

	public override void _Draw()
	{
		if (_manager == null)
			return;

		// Get viewport bounds in world space
		Camera2D camera = GetViewport().GetCamera2D();
		if (camera == null)
			return;

		Vector2 viewportSize = GetViewportRect().Size;
		float zoom = camera.Zoom.X;
		Vector2 cameraPos = camera.GlobalPosition;
		Vector2 halfView = viewportSize / (2f * zoom);

		Rect2 viewRect = new(cameraPos - halfView - new Vector2(_cellSize, _cellSize),
			halfView * 2f + new Vector2(_cellSize * 2, _cellSize * 2));

		_manager.GetCellsInRect(viewRect, _visibleCells);

		float initialMemory = _manager.InitialMemory;

		foreach ((Vector2I cell, float memory) in _visibleCells)
		{
			// Alpha : 0 when memory >= 0.95 (fully remembered), max ~0.55 when memory = 0
			float alpha = (1f - memory) * 0.55f;
			if (alpha < 0.02f)
				continue;

			Color color = new(FadedColor, alpha);
			Vector2 pos = _manager.CellToWorld(cell);
			DrawRect(new Rect2(pos, new Vector2(_cellSize, _cellSize)), color);
		}
	}
}
