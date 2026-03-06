using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Minimap affichée dans le HUD. Rendu par Image pour la performance.
/// Montre le terrain, le brouillard, le joueur, les ennemis et les structures.
/// Optimisation : les dots d'entités sont effacés/redessinés sans recopier l'image entière.
/// </summary>
public partial class Minimap : PanelContainer
{
    private const float EntityUpdateInterval = 0.4f;

    private Image _baseTerrain;
    private Image _foggedTerrain;
    private ImageTexture _texture;
    private TextureRect _display;

    private WorldGenerator _generator;
    private FogOfWar _fogOfWar;
    private GroupCache _groupCache;
    private int _mapRadius;
    private int _minimapPixelSize;
    private float _entityTimer;
    private bool _initialized;
    private int _lastFogRevision = -1;

    // Tracked dots: pixel positions that were drawn last frame, to erase them efficiently
    private readonly List<(int px, int py, int size)> _drawnDots = new();

    private static readonly Color ColorGrass = new(0.4f, 0.65f, 0.3f);
    private static readonly Color ColorConcrete = new(0.6f, 0.58f, 0.52f);
    private static readonly Color ColorWater = new(0.2f, 0.35f, 0.6f);
    private static readonly Color ColorForest = new(0.2f, 0.5f, 0.2f);
    private static readonly Color ColorFog = new(0.08f, 0.06f, 0.12f);
    private static readonly Color ColorPlayer = new(0.3f, 1f, 0.5f);
    private static readonly Color ColorEnemy = new(1f, 0.25f, 0.25f);
    private static readonly Color ColorStructure = new(0.5f, 0.8f, 1f);
    private static readonly Color ColorFoyer = new(1f, 0.9f, 0.4f);
    private static readonly Color ColorPoi = new(1f, 0.8f, 0.3f);

    public void Initialize(WorldGenerator generator, FogOfWar fogOfWar)
    {
        _generator = generator;
        _fogOfWar = fogOfWar;
        _mapRadius = generator.MapRadius;
        _minimapPixelSize = _mapRadius * 2 + 1;
        _groupCache = GetNodeOrNull<GroupCache>("/root/GroupCache");

        SetupVisuals();
        BuildBaseTerrain();
        _foggedTerrain.CopyFrom(_baseTerrain);
        _initialized = true;
    }

    private void SetupVisuals()
    {
        StyleBoxFlat style = new();
        style.BgColor = new Color(0f, 0f, 0f, 0.7f);
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.ContentMarginLeft = 3;
        style.ContentMarginRight = 3;
        style.ContentMarginTop = 3;
        style.ContentMarginBottom = 3;
        AddThemeStyleboxOverride("panel", style);

        _display = new TextureRect();
        _display.CustomMinimumSize = new Vector2(100, 100);
        _display.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _display.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _display.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        AddChild(_display);

        _baseTerrain = Image.CreateEmpty(_minimapPixelSize, _minimapPixelSize, false, Image.Format.Rgb8);
        _foggedTerrain = Image.CreateEmpty(_minimapPixelSize, _minimapPixelSize, false, Image.Format.Rgb8);
        _texture = ImageTexture.CreateFromImage(_foggedTerrain);
        _display.Texture = _texture;
    }

    private void BuildBaseTerrain()
    {
        for (int x = -_mapRadius; x <= _mapRadius; x++)
        {
            for (int y = -_mapRadius; y <= _mapRadius; y++)
            {
                int px = x + _mapRadius;
                int py = y + _mapRadius;

                if (!_generator.IsWithinBounds(x, y) || _generator.IsErased(x, y))
                {
                    _baseTerrain.SetPixel(px, py, ColorFog);
                    continue;
                }

                TerrainType terrain = _generator.GetTerrain(x, y);
                Color color = terrain switch
                {
                    TerrainType.Grass => ColorGrass,
                    TerrainType.Concrete => ColorConcrete,
                    TerrainType.Water => ColorWater,
                    TerrainType.Forest => ColorForest,
                    _ => ColorGrass
                };

                _baseTerrain.SetPixel(px, py, color);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!_initialized)
            return;

        _entityTimer += (float)delta;
        if (_entityTimer < EntityUpdateInterval)
            return;

        _entityTimer = 0f;
        UpdateMinimap();
    }

    private void UpdateMinimap()
    {
        bool fogChanged = _fogOfWar == null || _fogOfWar.RevealRevision != _lastFogRevision;
        if (fogChanged)
        {
            _foggedTerrain.CopyFrom(_baseTerrain);

            if (_fogOfWar != null)
            {
                for (int x = -_mapRadius; x <= _mapRadius; x++)
                {
                    for (int y = -_mapRadius; y <= _mapRadius; y++)
                    {
                        if (!_fogOfWar.IsRevealed(new Vector2I(x, y)))
                            _foggedTerrain.SetPixel(x + _mapRadius, y + _mapRadius, ColorFog);
                    }
                }

                _lastFogRevision = _fogOfWar.RevealRevision;
            }

            _drawnDots.Clear();
        }
        else
        {
            // Erase previous entity dots by restoring pixels from _foggedTerrain
            foreach ((int px, int py, int size) in _drawnDots)
            {
                RestoreDot(px, py, size);
            }
        }

        _drawnDots.Clear();

        // Draw foyer (center)
        DrawDot(_mapRadius, _mapRadius, ColorFoyer, 2);

        // Draw structures
        if (_groupCache != null)
        {
            foreach (Node node in _groupCache.GetStructures())
            {
                if (node is Node2D structure)
                    DrawEntityDot(structure.GlobalPosition, ColorStructure);
            }

            foreach (Node node in _groupCache.GetPois())
            {
                if (node is Node2D poi)
                    DrawEntityDot(poi.GlobalPosition, ColorPoi);
            }

            foreach (Node node in _groupCache.GetEnemies())
            {
                if (node is Node2D enemy)
                    DrawEntityDot(enemy.GlobalPosition, ColorEnemy);
            }

            Node playerNode = _groupCache.GetPlayer();
            if (playerNode is Node2D player)
                DrawEntityDot(player.GlobalPosition, ColorPlayer, 3);
        }

        _texture.Update(_foggedTerrain);
    }

    private void DrawEntityDot(Vector2 worldPos, Color color, int size = 1)
    {
        float tileX = (worldPos.X / 32f + worldPos.Y / 16f) / 2f;
        float tileY = (worldPos.Y / 16f - worldPos.X / 32f) / 2f;

        int px = Mathf.RoundToInt(tileX) + _mapRadius;
        int py = Mathf.RoundToInt(tileY) + _mapRadius;

        DrawDot(px, py, color, size);
    }

    private void DrawDot(int px, int py, Color color, int size = 1)
    {
        _drawnDots.Add((px, py, size));
        for (int dx = -size; dx <= size; dx++)
        {
            for (int dy = -size; dy <= size; dy++)
            {
                int fx = px + dx;
                int fy = py + dy;
                if (fx >= 0 && fx < _minimapPixelSize && fy >= 0 && fy < _minimapPixelSize)
                    _foggedTerrain.SetPixel(fx, fy, color);
            }
        }
    }

    private void RestoreDot(int px, int py, int size)
    {
        for (int dx = -size; dx <= size; dx++)
        {
            for (int dy = -size; dy <= size; dy++)
            {
                int fx = px + dx;
                int fy = py + dy;
                if (fx >= 0 && fx < _minimapPixelSize && fy >= 0 && fy < _minimapPixelSize)
                {
                    // Restore from base terrain + fog check
                    Color baseColor = _baseTerrain.GetPixel(fx, fy);
                    if (_fogOfWar != null && !_fogOfWar.IsRevealed(new Vector2I(fx - _mapRadius, fy - _mapRadius)))
                        baseColor = ColorFog;
                    _foggedTerrain.SetPixel(fx, fy, baseColor);
                }
            }
        }
    }
}
