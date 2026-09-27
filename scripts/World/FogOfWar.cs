using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Cellules découvertes par le joueur : la découverte alimente les sons et les succès (ZoneDiscovered).
/// Aucun rendu : la couche de brouillard d'origine était transparente depuis mars (masque jamais fourni au shader),
/// elle dessinait 160 000 tuiles pour rien. Le voile de la Stratégie V2 reste à concevoir s'il est voulu.
/// </summary>
public partial class FogOfWar : Node2D
{
    private TileMapLayer _ground;
    private readonly HashSet<Vector2I> _revealedCells = new();
    private Node2D _player;
    private EventBus _eventBus;

    private Vector2I _lastPlayerCell = new(int.MinValue, int.MinValue);

    private int _fogRevealRadius;
    private int _baseRevealRadius;
    private int _revealRevision;

    public int RevealRevision => _revealRevision;

    public bool IsRevealed(Vector2I cell) => _revealedCells.Contains(cell);

    public bool IsRevealed(Vector2 worldPos)
    {
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(worldPos));
        return _revealedCells.Contains(cell);
    }

    public void Initialize(TileMapLayer ground, int fogRevealRadius, int fogInitialClearRadius)
    {
        _ground = ground;
        _fogRevealRadius = fogRevealRadius;
        _baseRevealRadius = fogRevealRadius;
        _eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
        if (_eventBus != null)
            _eventBus.OubliEffectChanged += OnOubliEffectChanged;

        RevealInitialArea(fogInitialClearRadius);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.OubliEffectChanged -= OnOubliEffectChanged;
    }

    /// <summary>Oubli du regard (plan 17 lot 3D) : le brouillard se lève sur un rayon plus court.</summary>
    private void OnOubliEffectChanged(string effect, float total)
    {
        if (effect == "fog_reveal")
            _fogRevealRadius = Mathf.Max(1, Mathf.RoundToInt(_baseRevealRadius * (1f - total)));
    }

    public override void _Process(double delta)
    {
        if (_ground == null)
            return;

        if (_player == null || !IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (_player == null)
                return;
        }

        Vector2I playerCell = _ground.LocalToMap(_ground.ToLocal(_player.GlobalPosition));
        if (playerCell == _lastPlayerCell)
            return;

        Vector2I prevCell = _lastPlayerCell;
        _lastPlayerCell = playerCell;
        RevealAroundPlayer(playerCell, prevCell);
    }

    private void RevealInitialArea(int clearRadius)
    {
        int clearSq = clearRadius * clearRadius;
        for (int x = -clearRadius; x <= clearRadius; x++)
        {
            for (int y = -clearRadius; y <= clearRadius; y++)
            {
                if (x * x + y * y <= clearSq)
                    _revealedCells.Add(new Vector2I(x, y));
            }
        }
    }

    // --- Per-frame reveal (only check newly-entered cells) ---

    private void RevealAroundPlayer(Vector2I center, Vector2I prevCenter)
    {
        int newlyRevealed = 0;
        int radiusSq = _fogRevealRadius * _fogRevealRadius;

        // Only iterate the full circle on first reveal or large movement
        int dx = center.X - prevCenter.X;
        int dy = center.Y - prevCenter.Y;
        bool fullScan = prevCenter.X == int.MinValue || Mathf.Abs(dx) > 2 || Mathf.Abs(dy) > 2;

        if (fullScan)
        {
            for (int cx = -_fogRevealRadius; cx <= _fogRevealRadius; cx++)
            {
                for (int cy = -_fogRevealRadius; cy <= _fogRevealRadius; cy++)
                {
                    if (cx * cx + cy * cy > radiusSq)
                        continue;

                    Vector2I cell = new(center.X + cx, center.Y + cy);
                    if (_revealedCells.Contains(cell))
                        continue;

                    MaterializeCell(cell);
                    newlyRevealed++;
                }
            }
        }
        else
        {
            // Incremental: only check cells in the strip that moved into range
            for (int cx = -_fogRevealRadius; cx <= _fogRevealRadius; cx++)
            {
                for (int cy = -_fogRevealRadius; cy <= _fogRevealRadius; cy++)
                {
                    if (cx * cx + cy * cy > radiusSq)
                        continue;

                    Vector2I cell = new(center.X + cx, center.Y + cy);
                    if (_revealedCells.Contains(cell))
                        continue;

                    // Check if this cell was outside the previous reveal circle
                    int prevDx = cell.X - prevCenter.X;
                    int prevDy = cell.Y - prevCenter.Y;
                    if (prevDx * prevDx + prevDy * prevDy <= radiusSq)
                        continue; // Was already in range

                    MaterializeCell(cell);
                    newlyRevealed++;
                }
            }
        }

        if (newlyRevealed > 0)
            _eventBus?.EmitSignal(EventBus.SignalName.ZoneDiscovered, center.X, center.Y, newlyRevealed);
    }

    private void MaterializeCell(Vector2I cell)
    {
        if (_revealedCells.Add(cell))
            _revealRevision++;
    }
}
