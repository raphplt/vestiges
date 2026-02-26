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

    private static readonly Color FogColor = new(0.04f, 0.02f, 0.08f, 0.95f);

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
        InitializeFog();

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

        _lastPlayerCell = playerCell;
        RevealAroundCell(playerCell, _fogRevealRadius);
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

    private void InitializeFog()
    {
        int fogged = 0;

        for (int x = -_mapRadius; x <= _mapRadius; x++)
        {
            for (int y = -_mapRadius; y <= _mapRadius; y++)
            {
                if (!_generator.IsWithinBounds(x, y))
                    continue;
                if (_generator.IsErased(x, y))
                    continue;

                Vector2I cell = new(x, y);

                if (Mathf.Abs(x) <= _fogInitialClearRadius && Mathf.Abs(y) <= _fogInitialClearRadius)
                {
                    _revealedCells.Add(cell);
                    continue;
                }

                _fogLayer.SetCell(cell, 0, Vector2I.Zero);
                fogged++;
            }
        }

        GD.Print($"[FogOfWar] Initialized — {_revealedCells.Count} cells clear, {fogged} cells fogged");
    }

    private void RevealAroundCell(Vector2I center, int radius)
    {
        int newlyRevealed = 0;
        int radiusSq = radius * radius;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx * dx + dy * dy > radiusSq)
                    continue;

                Vector2I cell = new(center.X + dx, center.Y + dy);

                if (_revealedCells.Contains(cell))
                    continue;

                MaterializeCell(cell);
                newlyRevealed++;
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

        // Trouver les cellules frontières (révélées avec au moins un voisin non révélé)
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

        // Révéler N cellules autour de chaque cellule frontière
        HashSet<Vector2I> toReveal = new();
        int expansionSq = _dawnFogExpansion * _dawnFogExpansion;

        foreach (Vector2I bCell in boundary)
        {
            for (int dx = -_dawnFogExpansion; dx <= _dawnFogExpansion; dx++)
            {
                for (int dy = -_dawnFogExpansion; dy <= _dawnFogExpansion; dy++)
                {
                    if (dx * dx + dy * dy > expansionSq)
                        continue;

                    Vector2I candidate = new(bCell.X + dx, bCell.Y + dy);
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
