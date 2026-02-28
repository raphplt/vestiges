using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Fog of War : cache le monde non exploré.
/// Le joueur révèle les cellules en se déplaçant.
/// L'aube repousse la brume autour du territoire révélé.
/// Lore : la réalité n'existe que là où quelqu'un s'en souvient.
/// </summary>
public partial class FogOfWar : Node2D
{
    private TileMapLayer _fogLayer;
    private TileMapLayer _ground;
    private WorldGenerator _generator;
    private HashSet<Vector2I> _revealedCells = new();
    private Node2D _player;
    private EventBus _eventBus;

    private Vector2I _lastPlayerCell = new(int.MinValue, int.MinValue);

    private int _fogRevealRadius;
    private int _dawnFogExpansion;
    private int _fogInitialClearRadius;
    private int _mapRadius;

    // Deferred initialization
    private bool _initPhase;
    private int _initX;
    private int _initY;
    private const int InitBatchSize = 800;

    private static readonly Color FogColor = new(0.04f, 0.02f, 0.08f, 0.85f);

    public bool IsRevealed(Vector2I cell) => _revealedCells.Contains(cell);

    public bool IsRevealed(Vector2 worldPos)
    {
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(worldPos));
        return _revealedCells.Contains(cell);
    }

    public void Initialize(TileMapLayer ground, WorldGenerator generator, int fogRevealRadius, int dawnFogExpansion, int fogInitialClearRadius)
    {
        _ground = ground;
        _generator = generator;
        _fogRevealRadius = fogRevealRadius;
        _dawnFogExpansion = dawnFogExpansion;
        _fogInitialClearRadius = fogInitialClearRadius;
        _mapRadius = generator.MapRadius;

        CreateFogLayer();
        StartDeferredInit();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.DayPhaseChanged += OnDayPhaseChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.DayPhaseChanged -= OnDayPhaseChanged;
    }

    public override void _Process(double delta)
    {
        if (_ground == null)
            return;

        if (_initPhase)
        {
            ProcessDeferredInit();
            return;
        }

        if (_player == null)
        {
            Node playerNode = GetTree().GetFirstNodeInGroup("player");
            if (playerNode is Node2D p)
                _player = p;
            return;
        }

        Vector2I playerCell = _ground.LocalToMap(_ground.ToLocal(_player.GlobalPosition));
        if (playerCell == _lastPlayerCell)
            return;

        Vector2I prevCell = _lastPlayerCell;
        _lastPlayerCell = playerCell;
        RevealAroundPlayer(playerCell, prevCell);
    }

    private void CreateFogLayer()
    {
        _fogLayer = new TileMapLayer();
        _fogLayer.Name = "FogLayer";

        TileSet groundTileSet = _ground.TileSet;
        TileSet fogTileSet = new();
        fogTileSet.TileSize = groundTileSet.TileSize;
        fogTileSet.TileShape = groundTileSet.TileShape;
        fogTileSet.TileLayout = groundTileSet.TileLayout;
        fogTileSet.TileOffsetAxis = groundTileSet.TileOffsetAxis;

        Vector2I tileSize = fogTileSet.TileSize;
        Image fogImage = Image.CreateEmpty(tileSize.X, tileSize.Y, false, Image.Format.Rgba8);
        fogImage.Fill(FogColor);
        ImageTexture fogTexture = ImageTexture.CreateFromImage(fogImage);

        TileSetAtlasSource source = new();
        source.Texture = fogTexture;
        source.TextureRegionSize = tileSize;
        fogTileSet.AddSource(source);
        source.CreateTile(Vector2I.Zero);

        _fogLayer.TileSet = fogTileSet;
        _fogLayer.ZIndex = 10;

        _ground.GetParent().AddChild(_fogLayer);
    }

    // --- Deferred initialization (spread across frames to avoid lag) ---

    private void StartDeferredInit()
    {
        // Pre-populate initial clear area
        int clearR = _fogInitialClearRadius;
        int clearSq = clearR * clearR;
        for (int x = -clearR; x <= clearR; x++)
        {
            for (int y = -clearR; y <= clearR; y++)
            {
                if (x * x + y * y <= clearSq)
                    _revealedCells.Add(new Vector2I(x, y));
            }
        }

        _initX = -_mapRadius;
        _initY = -_mapRadius;
        _initPhase = true;
    }

    private void ProcessDeferredInit()
    {
        int processed = 0;

        while (_initX <= _mapRadius && processed < InitBatchSize)
        {
            while (_initY <= _mapRadius && processed < InitBatchSize)
            {
                int x = _initX;
                int y = _initY;
                _initY++;
                processed++;

                if (!_generator.IsWithinBounds(x, y))
                    continue;
                if (_generator.IsErased(x, y))
                    continue;

                Vector2I cell = new(x, y);

                if (_revealedCells.Contains(cell))
                    continue;

                _fogLayer.SetCell(cell, 0, Vector2I.Zero);
            }

            if (_initY > _mapRadius)
            {
                _initX++;
                _initY = -_mapRadius;
            }
        }

        if (_initX > _mapRadius)
        {
            _initPhase = false;
            GD.Print($"[FogOfWar] Initialization complete — {_revealedCells.Count} cells clear");
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
        _revealedCells.Add(cell);
        _fogLayer.EraseCell(cell);
    }

    private void OnDayPhaseChanged(string phase)
    {
        if (phase != "Dawn")
            return;

        ExpandFogBoundary();
    }

    /// <summary>
    /// À l'aube, repousse la brume de N cellules depuis la frontière du territoire révélé.
    /// La réalité se souvient un peu plus à chaque aube.
    /// </summary>
    private void ExpandFogBoundary()
    {
        Vector2I[] directions =
        {
            new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        List<Vector2I> boundary = new();
        foreach (Vector2I cell in _revealedCells)
        {
            foreach (Vector2I dir in directions)
            {
                Vector2I neighbor = cell + dir;
                if (!_revealedCells.Contains(neighbor))
                {
                    boundary.Add(cell);
                    break;
                }
            }
        }

        HashSet<Vector2I> toReveal = new();
        int expansionSq = _dawnFogExpansion * _dawnFogExpansion;

        foreach (Vector2I bCell in boundary)
        {
            for (int bx = -_dawnFogExpansion; bx <= _dawnFogExpansion; bx++)
            {
                for (int by = -_dawnFogExpansion; by <= _dawnFogExpansion; by++)
                {
                    if (bx * bx + by * by > expansionSq)
                        continue;

                    Vector2I candidate = new(bCell.X + bx, bCell.Y + by);
                    if (!_revealedCells.Contains(candidate))
                        toReveal.Add(candidate);
                }
            }
        }

        foreach (Vector2I cell in toReveal)
            MaterializeCell(cell);

        GD.Print($"[FogOfWar] Dawn expansion: +{toReveal.Count} cells (from {boundary.Count} boundary cells)");
    }
}
