using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Découpe les décors en tronçons spatiaux et masque ceux qui sont loin de la caméra.
/// Le tri en Y de la scène rassemble et trie à chaque frame tous les enfants visibles d'un conteneur trié,
/// même hors écran : un tronçon masqué n'est plus parcouru du tout (≈ 4 ms de rendu CPU sur une carte de 10 000 décors).
/// </summary>
public partial class PropChunks : Node
{
    private const float ChunkSize = 768f;
    // Un décor dépasse de son nœud : immeubles hauts (~250 px), canopées, ombres. La vue est élargie d'autant.
    private const float ViewMargin = 360f;

    private readonly Dictionary<Vector2I, List<Node2D>> _chunks = new();
    private Camera2D _camera;
    private Rect2I _visibleRange;
    private bool _hasRange;

    /// <summary>Répartit les enfants des conteneurs dans des tronçons, qui héritent du tri en Y de leur parent.</summary>
    public void Build(Camera2D camera, params Node2D[] containers)
    {
        _camera = camera;
        int moved = 0;
        foreach (Node2D container in containers)
        {
            Dictionary<Vector2I, Node2D> byCell = new();
            foreach (Node child in container.GetChildren())
            {
                if (child is not Node2D prop)
                    continue;
                Vector2I cell = CellOf(prop.GlobalPosition);
                if (!byCell.TryGetValue(cell, out Node2D chunk))
                {
                    chunk = new Node2D { Name = $"Chunk_{cell.X}_{cell.Y}", YSortEnabled = container.YSortEnabled };
                    container.AddChild(chunk);
                    byCell[cell] = chunk;
                    if (!_chunks.TryGetValue(cell, out List<Node2D> list))
                        _chunks[cell] = list = new List<Node2D>();
                    list.Add(chunk);
                }
                prop.Reparent(chunk);
                moved++;
            }
        }
        foreach (List<Node2D> list in _chunks.Values)
            foreach (Node2D chunk in list)
                chunk.Visible = false;
        GD.Print($"[PropChunks] {moved} décors répartis en {_chunks.Count} tronçons de {ChunkSize} px");
    }

    public override void _Process(double delta)
    {
        if (_camera == null || !IsInstanceValid(_camera))
            return;

        Vector2 half = GetViewport().GetVisibleRect().Size / (2f * _camera.Zoom) + new Vector2(ViewMargin, ViewMargin);
        Vector2 center = _camera.GetScreenCenterPosition();
        Vector2I from = CellOf(center - half);
        Vector2I to = CellOf(center + half);
        Rect2I range = new(from, to - from + Vector2I.One);
        if (_hasRange && range == _visibleRange)
            return;

        if (_hasRange)
            SetRangeVisible(_visibleRange, false);
        SetRangeVisible(range, true);
        _visibleRange = range;
        _hasRange = true;
    }

    private void SetRangeVisible(Rect2I range, bool visible)
    {
        for (int x = range.Position.X; x < range.End.X; x++)
        {
            for (int y = range.Position.Y; y < range.End.Y; y++)
            {
                if (!_chunks.TryGetValue(new Vector2I(x, y), out List<Node2D> list))
                    continue;
                foreach (Node2D chunk in list)
                    chunk.Visible = visible;
            }
        }
    }

    private static Vector2I CellOf(Vector2 position)
    {
        return new Vector2I(Mathf.FloorToInt(position.X / ChunkSize), Mathf.FloorToInt(position.Y / ChunkSize));
    }
}
