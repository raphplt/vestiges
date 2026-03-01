using Godot;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Minimap affichée dans le HUD. Rendu par Image pour la performance.
/// Montre le terrain, le brouillard, le joueur, les ennemis et les structures.
/// </summary>
public partial class Minimap : PanelContainer
{
    private const int MinimapPixelSize = 161; // 1 pixel per cell (-80 to +80)
    private const float EntityUpdateInterval = 0.25f;

    private Image _baseTerrain;
    private Image _composite;
    private ImageTexture _texture;
    private TextureRect _display;

    private WorldGenerator _generator;
    private FogOfWar _fogOfWar;
    private int _mapRadius;
    private float _entityTimer;
    private bool _initialized;

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

        SetupVisuals();
        BuildBaseTerrain();
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
        _display.CustomMinimumSize = new Vector2(140, 140);
        _display.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _display.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        AddChild(_display);

        _baseTerrain = Image.CreateEmpty(MinimapPixelSize, MinimapPixelSize, false, Image.Format.Rgb8);
        _composite = Image.CreateEmpty(MinimapPixelSize, MinimapPixelSize, false, Image.Format.Rgb8);
        _texture = ImageTexture.CreateFromImage(_composite);
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
        _composite.CopyFrom(_baseTerrain);

        // Apply fog of war
        if (_fogOfWar != null)
        {
            for (int x = -_mapRadius; x <= _mapRadius; x++)
            {
                for (int y = -_mapRadius; y <= _mapRadius; y++)
                {
                    if (!_fogOfWar.IsRevealed(new Vector2I(x, y)))
                        _composite.SetPixel(x + _mapRadius, y + _mapRadius, ColorFog);
                }
            }
        }

        // Draw foyer (center)
        DrawDot(_mapRadius, _mapRadius, ColorFoyer, 2);

        // Draw structures
        foreach (Node node in GetTree().GetNodesInGroup("structures"))
        {
            if (node is Node2D structure)
                DrawEntityDot(structure.GlobalPosition, ColorStructure);
        }

        // Draw POIs
        foreach (Node node in GetTree().GetNodesInGroup("pois"))
        {
            if (node is Node2D poi)
                DrawEntityDot(poi.GlobalPosition, ColorPoi);
        }

        // Draw enemies
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Node2D enemy)
                DrawEntityDot(enemy.GlobalPosition, ColorEnemy);
        }

        // Draw player (larger dot for visibility)
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Node2D player)
            DrawEntityDot(player.GlobalPosition, ColorPlayer, 3);

        _texture.Update(_composite);
    }

    private void DrawEntityDot(Vector2 worldPos, Color color, int size = 1)
    {
        // Convert world position to tile coordinates
        // Isometric: world_x = (tile_x - tile_y) * 32, world_y = (tile_x + tile_y) * 16
        // Inverse: tile_x = (world_x/32 + world_y/16) / 2, tile_y = (world_y/16 - world_x/32) / 2
        float tileX = (worldPos.X / 32f + worldPos.Y / 16f) / 2f;
        float tileY = (worldPos.Y / 16f - worldPos.X / 32f) / 2f;

        int px = Mathf.RoundToInt(tileX) + _mapRadius;
        int py = Mathf.RoundToInt(tileY) + _mapRadius;

        DrawDot(px, py, color, size);
    }

    private void DrawDot(int px, int py, Color color, int size = 1)
    {
        for (int dx = -size; dx <= size; dx++)
        {
            for (int dy = -size; dy <= size; dy++)
            {
                int fx = px + dx;
                int fy = py + dy;
                if (fx >= 0 && fx < MinimapPixelSize && fy >= 0 && fy < MinimapPixelSize)
                    _composite.SetPixel(fx, fy, color);
            }
        }
    }
}
