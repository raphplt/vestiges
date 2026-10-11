using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Découpe les décors en tronçons spatiaux et masque ceux qui sont loin de la caméra.
/// Le tri en Y de la scène rassemble et trie à chaque frame tous les enfants visibles d'un conteneur trié,
/// même hors écran : un tronçon masqué n'est plus parcouru du tout (≈ 4 ms de rendu CPU sur une carte de 10 000 décors).
/// Après <see cref="Build"/>, les enfants directs de <c>PropContainer</c> et <c>GroundDecals</c> sont des tronçons :
/// tout code qui cherche les décors doit les énumérer récursivement, ou s'exécuter avant le découpage.
/// </summary>
public partial class PropChunks : Node
{
    private const float ChunkSize = 768f;
    // Un décor dépasse de son nœud : immeubles hauts (~250 px), canopées, ombres. La vue est élargie d'autant.
    private const float ViewMargin = 360f;

    private readonly Dictionary<Vector2I, List<Node2D>> _chunks = new();
    private readonly List<(Node2D Chunk, Node2D Target)> _pending = new();
    private int _attached;
    private Camera2D _camera;
    private Rect2I _visibleRange;
    private bool _hasRange;

    /// <summary>
    /// Range les décors de chaque source, hors de l'arbre et à l'origine, dans des tronçons masqués, destinés à leur
    /// conteneur final, qui leur transmettra son tri en Y ; <see cref="AttachPending"/> les y fait entrer. Chaque décor
    /// entre une seule fois dans la scène.
    /// </summary>
    public void Build(Camera2D camera, params (Node2D Source, Node2D Target)[] containers)
    {
        _camera = camera;
        int moved = 0;
        foreach ((Node2D source, Node2D target) in containers)
        {
            Dictionary<Vector2I, Node2D> byCell = new();
            foreach (Node child in source.GetChildren())
            {
                if (child is not Node2D prop)
                    continue;
                // Source à l'origine : la position locale est la position monde.
                Vector2I cell = CellOf(prop.Position);
                if (!byCell.TryGetValue(cell, out Node2D chunk))
                {
                    chunk = new Node2D { Name = $"Chunk_{cell.X}_{cell.Y}", YSortEnabled = target.YSortEnabled, Visible = false };
                    byCell[cell] = chunk;
                    if (!_chunks.TryGetValue(cell, out List<Node2D> list))
                        _chunks[cell] = list = new List<Node2D>();
                    list.Add(chunk);
                }
                source.RemoveChild(prop);
                chunk.AddChild(prop);
                moved++;
            }
            foreach (Node2D chunk in byCell.Values)
                _pending.Add((chunk, target));
        }
        GD.Print($"[PropChunks] {moved} décors répartis en {_chunks.Count} tronçons de {ChunkSize} px");
    }

    /// <summary>
    /// Fait entrer les tronçons rangés dans l'arbre jusqu'à épuisement de la tranche de temps : l'entrée de dix mille
    /// décors d'un bloc figeait l'écran de chargement près d'une seconde (plan 37 R5). Vrai s'il en reste.
    /// </summary>
    public bool AttachPending(Func<bool> sliceSpent)
    {
        while (_attached < _pending.Count)
        {
            (Node2D chunk, Node2D target) = _pending[_attached++];
            target.AddChild(chunk);
            if (sliceSpent())
                break;
        }
        if (_attached < _pending.Count)
            return true;
        _pending.Clear();
        _attached = 0;
        return false;
    }

    /// <summary>Scène quittée pendant le chargement : les tronçons pas encore entrés n'ont aucun parent qui les libère.</summary>
    public override void _ExitTree()
    {
        for (int i = _attached; i < _pending.Count; i++)
            _pending[i].Chunk.Free();
        _attached = _pending.Count;
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
