using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Fog of War : cache le monde non exploré.
/// Le joueur révèle les cellules en se déplaçant.
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
    private int _fogInitialClearRadius;
    private int _mapRadius;
    private int _revealRevision;

    // Deferred initialization
    private bool _initPhase;
    private int _initX;
    private int _initY;
    private const int InitBatchSize = 800;

    private static readonly Color FogColor = new(0.04f, 0.02f, 0.08f, 0.85f);

    public int RevealRevision => _revealRevision;

    public bool IsRevealed(Vector2I cell) => _revealedCells.Contains(cell);

    public bool IsRevealed(Vector2 worldPos)
    {
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(worldPos));
        return _revealedCells.Contains(cell);
    }

    public void Initialize(TileMapLayer ground, WorldGenerator generator, int fogRevealRadius, int fogInitialClearRadius)
    {
        _ground = ground;
        _generator = generator;
        _fogRevealRadius = fogRevealRadius;
        _fogInitialClearRadius = fogInitialClearRadius;
        _mapRadius = generator.MapRadius;
        _eventBus = GetNodeOrNull<EventBus>("/root/EventBus");

        CreateFogLayer();
        StartDeferredInit();
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

        // Load and assign the Fog of War shader
        if (ResourceLoader.Exists("res://assets/shaders/fog_of_war.gdshader"))
        {
            var shader = GD.Load<Shader>("res://assets/shaders/fog_of_war.gdshader");
            var material = new ShaderMaterial { Shader = shader };
            // Generate a dummy mask texture just to assign the parameter (the real logic is handled elsewhere, or could be passed via ViewportTexture)
            // But we will give it a try with a dummy one first to prevent errors
            _fogLayer.Material = material;
        }

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
        if (!_revealedCells.Add(cell))
            return;

        _fogLayer.EraseCell(cell);
        _revealRevision++;
    }
}
