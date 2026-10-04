using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Minimap (plan 22 C6, plan 23 R7, plan 24 A6) : un radar zoomé autour du joueur, et la carte entière avec sa légende
/// en maintenant « show_map ». Le terrain des biomes, routes et sentiers est échantillonné à la découverte,
/// avec quatre pixels par axe de cellule d'Effacement ; le danger recolore ce fond sans refaire les relevés.
/// Mise à jour quatre fois par seconde, sans parcours des décors ni du monde entier.
/// </summary>
public partial class Minimap : Control
{
    private const float UpdateInterval = 0.25f;
    private const float RadarSize = 104f;
    /// <summary>Cellules de côté que montre le radar : 26 cellules de 128 px, environ trois écrans, quatre unités chacune.</summary>
    private const int RadarCells = 26;
    private const float FullMaxHeight = 480f;
    private const float FullMaxWidth = 700f;
    private const float Margin = 10f;
    private const float BottomMargin = 64f;
    private const int LegendFontSize = 8;
    private const int RevealRadiusCells = 12;

    private static readonly Color Frame = new(0.05f, 0.05f, 0.09f, 0.78f);
    private static readonly Color Border = new(0.55f, 0.47f, 0.28f, 0.85f);
    private static readonly Color Unknown = new(0.08f, 0.08f, 0.12f, 0.85f);
    private static readonly Color PlayerColor = new(1f, 0.97f, 0.88f);
    private static readonly Color MemorialColor = new(0.95f, 0.82f, 0.45f);
    private static readonly Color RiftColor = new(0.7f, 0.35f, 0.85f);
    private static readonly Color WorkshopColor = new(0.91f, 0.52f, 0.24f);
    private static readonly Color PlaceColor = new(0.78f, 0.86f, 0.92f);
    private ErasureManager _erasure;
    private WorldSetup _world;
    private SmallPlaceDirector _places;
    private GroupCache _groups;
    private EventBus _eventBus;
    private Rect2 _bounds;
    private int _cellSize;
    private Vector2I _origin;
    private CartographyRaster _raster;
    private TileMapLayer _ground;
    private TileMapLayer _roads;
    private MapPalette _palette;
    private ImageTexture _texture;
    private int _width;
    private int _height;
    // Oubli des repères (plan 17 lot 3D) : comme les flèches, les coffres quittent la carte tant qu'il est porté.
    private bool _chestSignalsForgotten;
    private float _timer;
    private bool _full;
    private Rect2 _view;
    private Vector2 _mapSize;
    private VBoxContainer _legend;
    private Vector2 _playerPosition;
    private Texture2D _placeIcon;
    private Texture2D _chestIcon;
    private Texture2D _memorialIcon;
    private Texture2D _riftIcon;
    private Texture2D _workshopIcon;
    private Texture2D _playerIcon;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        const string iconFolder = "res://assets/ui/hud/plan25/";
        _placeIcon = GD.Load<Texture2D>(iconFolder + "minimap_place.png");
        _chestIcon = GD.Load<Texture2D>(iconFolder + "minimap_chest.png");
        _memorialIcon = GD.Load<Texture2D>(iconFolder + "minimap_memorial.png");
        _riftIcon = GD.Load<Texture2D>(iconFolder + "minimap_rift.png");
        _workshopIcon = GD.Load<Texture2D>(iconFolder + "minimap_workshop.png");
        _playerIcon = GD.Load<Texture2D>(iconFolder + "minimap_player.png");
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.ZonePhaseChanged += OnZonePhaseChanged;
        _eventBus.OubliEffectChanged += OnOubliEffectChanged;
        Visible = false;
    }

    /// <summary>
    /// Ce nœud ne tourne pas pendant une pause (level-up, coffre, pause) : la carte entière ouverte à ce moment resterait
    /// figée sous l'écran. Elle se cache le temps de la pause.
    /// </summary>
    public override void _Notification(int what)
    {
        if (what == NotificationPaused && _full)
            Visible = false;
        else if (what == NotificationUnpaused && _raster != null)
            Visible = true;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.ZonePhaseChanged -= OnZonePhaseChanged;
            _eventBus.OubliEffectChanged -= OnOubliEffectChanged;
        }
        _raster?.Dispose();
    }

    private void OnOubliEffectChanged(string effect, float total)
    {
        if (effect == "chest_signals")
            _chestSignalsForgotten = total > 0f;
    }

    /// <summary>La carte n'existe qu'une fois le monde généré : l'image se crée à la première mise à jour possible.</summary>
    private bool EnsureMap()
    {
        if (_raster != null)
            return true;
        _world ??= GetNodeOrNull<WorldSetup>("/root/Main");
        _erasure ??= GetNodeOrNull<ErasureManager>("/root/Main/ErasureManager");
        if (_world is not { IsWorldReady: true } || _erasure == null)
            return false;
        _places = GetNodeOrNull<SmallPlaceDirector>("/root/Main/SmallPlaceDirector");
        _chestSignalsForgotten = (PerilManager.Current?.EffectTotal("chest_signals") ?? 0f) > 0f;
        _bounds = _world.WorldBounds;
        _cellSize = _erasure.CellSize;
        _origin = new Vector2I(Mathf.FloorToInt(_bounds.Position.X / _cellSize), Mathf.FloorToInt(_bounds.Position.Y / _cellSize));
        _width = Mathf.CeilToInt(_bounds.Size.X / _cellSize) + 1;
        _height = Mathf.CeilToInt(_bounds.Size.Y / _cellSize) + 1;
        _ground = _world.GetNode<TileMapLayer>("Ground");
        _roads = _world.GetNodeOrNull<TileMapLayer>("RoadOverlay");
        _palette = MapPalette.Load();
        _raster = new CartographyRaster(_width, _height, _cellSize, _origin, SampleTerrain);
        _texture = ImageTexture.CreateFromImage(_raster.Image);
        // Carte entière : un nombre entier d'unités par cellule, pour des pixels francs.
        int unit = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(FullMaxHeight / _height, FullMaxWidth / _width)));
        _mapSize = new Vector2(_width * unit, _height * unit);
        BuildLegend();
        Visible = true;
        return true;
    }

    private Color SampleTerrain(Vector2 world)
    {
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(world));
        WorldGenerator generator = _world.Generator;
        if (generator.IsErased(cell.X, cell.Y))
            return Colors.Transparent;
        if (_roads != null && _roads.GetCellSourceId(cell) >= 0)
            return _palette.Road;
        if (_world.PathCells?.Contains(cell) == true)
            return _palette.Path;
        return _palette.Terrain(generator.GetBiomeId(cell.X, cell.Y), generator.GetTerrain(cell.X, cell.Y));
    }

    /// <summary>Les mêmes pictogrammes et couleurs que sur la carte ; la légende partage son fond sombre.</summary>
    private void BuildLegend()
    {
        _legend = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _legend.AddThemeConstantOverride("separation", 2);
        AddChild(_legend);
        foreach ((Texture2D icon, Color color, string key) in new (Texture2D, Color, string)[]
        {
            (_playerIcon, PlayerColor, "MAP_LEGEND_YOU"),
            (_chestIcon, RarityPalette.Main("rare"), "MAP_LEGEND_CHEST"),
            (_memorialIcon, MemorialColor, "MAP_LEGEND_MEMORIAL"),
            (_riftIcon, RiftColor, "MAP_LEGEND_RIFT"),
            (_workshopIcon, WorkshopColor, "MAP_LEGEND_WORKSHOP"),
            (_placeIcon, PlaceColor, "MAP_LEGEND_PLACE"),
            (null, _palette.Terrain("forest_reclaimed", TerrainType.Forest), "MAP_BIOME_FOREST"),
            (null, _palette.Terrain("urban_ruins", TerrainType.Concrete), "MAP_BIOME_URBAN"),
            (null, _palette.Terrain("wild_fields", TerrainType.Grass), "MAP_BIOME_FIELDS"),
            (null, _palette.Terrain("swamp", TerrainType.Grass), "MAP_BIOME_SWAMP"),
            (null, _palette.Terrain("collapsed_quarry", TerrainType.Concrete), "MAP_BIOME_QUARRY"),
            (null, _palette.Road, "MAP_LEGEND_PATHS"),
            (null, _palette.Terrain("wild_fields", TerrainType.Water), "MAP_LEGEND_WATER"),
            (null, _palette.PhaseColor(2), "MAP_LEGEND_ERASURE"),
            (null, _palette.PhaseColor(4), "MAP_LEGEND_VOID"),
        })
        {
            HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 4);
            row.AddChild(new MapLegendIcon(icon, color, key == "MAP_LEGEND_VOID"));
            Label label = new() { Text = Tr(key), MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeColorOverride("font_color", new Color(0.88f, 0.84f, 0.76f));
            label.AddThemeFontSizeOverride("font_size", LegendFontSize);
            row.AddChild(label);
            _legend.AddChild(row);
        }
    }

    public override void _Process(double delta)
    {
        // La carte entière s'ouvre tout de suite à l'appui, sans attendre la prochaine repeinte.
        bool full = Input.IsActionPressed("show_map");
        _timer -= (float)delta;
        if (_timer > 0f && full == _full)
            return;
        _timer = UpdateInterval;
        if (!EnsureMap() || _groups.GetPlayer() is not Node2D player)
            return;
        _full = full;
        Control parent = GetParent<Control>();
        if (_full)
        {
            Size = _mapSize;
            Vector2 fullSize = new(Size.X + 8f + _legend.GetCombinedMinimumSize().X, Size.Y);
            Position = ((parent.Size - fullSize) / 2f).Floor();
            _legend.Visible = true;
            _legend.Position = new Vector2(Size.X + 8f, 0f);
        }
        else
        {
            Size = new Vector2(RadarSize, RadarSize);
            Position = new Vector2(parent.Size.X - Size.X - Margin, parent.Size.Y - Size.Y - BottomMargin);
            _legend.Visible = false;
        }
        _playerPosition = player.GlobalPosition;
        // Fenêtre de monde montrée : la carte entière, ou un carré centré sur le joueur, calé sur la grille d'une unité du
        // radar pour que les cellules glissent pixel par pixel.
        float worldSize = RadarCells * _cellSize;
        float step = worldSize / RadarSize;
        Vector2 corner = ((_playerPosition - Vector2.One * worldSize / 2f) / step).Floor() * step;
        _view = _full ? new Rect2(_origin * _cellSize, new Vector2(_width, _height) * _cellSize) : new Rect2(corner, Vector2.One * worldSize);
        RevealAround(_playerPosition);
        if (_raster.Flush())
            _texture.Update(_raster.Image);
        QueueRedraw();
    }

    /// <summary>Les cellules autour du joueur sont vues : elles prennent la couleur de leur phase.</summary>
    private void RevealAround(Vector2 position)
    {
        Vector2I center = new(Mathf.FloorToInt(position.X / _cellSize), Mathf.FloorToInt(position.Y / _cellSize));
        int radiusSq = RevealRadiusCells * RevealRadiusCells;
        for (int dx = -RevealRadiusCells; dx <= RevealRadiusCells; dx++)
        {
            for (int dy = -RevealRadiusCells; dy <= RevealRadiusCells; dy++)
            {
                if (dx * dx + dy * dy > radiusSq)
                    continue;
                Vector2I cell = new(center.X + dx, center.Y + dy);
                if (!_raster.IsKnown(cell))
                    _raster.Paint(cell, (int)_erasure.GetZonePhaseAt(_erasure.CellCenterToWorld(cell)), reveal: true);
            }
        }
    }

    /// <summary>Le front de l'Effacement avance aussi dans les cellules déjà vues.</summary>
    private void OnZonePhaseChanged(int cellX, int cellY, int phase) =>
        _raster?.Paint(new Vector2I(cellX, cellY), phase);

    private bool Known(Vector2 world) =>
        _raster.IsKnown(new Vector2I(Mathf.FloorToInt(world.X / _cellSize), Mathf.FloorToInt(world.Y / _cellSize)));

    private Vector2 ToMap(Vector2 world) => (world - _view.Position) / _view.Size * Size;

    private bool InView(Vector2 world) => _view.HasPoint(world);

    public override void _Draw()
    {
        if (_raster == null)
            return;
        Rect2 area = new(Vector2.Zero, Size);
        if (_full)
            DrawRect(new Rect2(Vector2.Zero, new Vector2(Size.X + 8f + _legend.Size.X, Mathf.Max(Size.Y, _legend.Size.Y))).Grow(6f), Frame with { A = 0.97f });
        DrawRect(area.Grow(2f), Frame);
        DrawRect(area, Unknown);
        // Partie de la carte dans la fenêtre : la région de l'image qui lui correspond, posée à sa place.
        Rect2 visible = _view.Intersection(new Rect2(_origin * _cellSize, new Vector2(_width, _height) * _cellSize));
        if (visible.Size.X > 0f && visible.Size.Y > 0f)
        {
            Rect2 source = new((visible.Position - (Vector2)(_origin * _cellSize)) / _cellSize * CartographyRaster.Detail, visible.Size / _cellSize * CartographyRaster.Detail);
            DrawTextureRectRegion(_texture, new Rect2(ToMap(visible.Position), visible.Size / _view.Size * Size), source);
        }
        DrawRect(area.Grow(2f), Border, false, 1f);
        // Boucles indexées : un foreach sur une IReadOnlyList alloue son énumérateur à chaque repeinte.
        // Un petit lieu servi disparaît de la carte.
        if (_places != null)
        {
            for (int i = 0; i < _places.Places.Count; i++)
            {
                SmallPlace place = _places.Places[i];
                if (place.CanInteract && InView(place.GlobalPosition) && Known(place.GlobalPosition))
                    DrawIcon(place.GlobalPosition, _placeIcon, PlaceColor);
            }
        }
        if (!_chestSignalsForgotten)
        {
            for (int i = 0; i < Chest.Closed.Count; i++)
            {
                Chest chest = Chest.Closed[i];
                if (chest.CanOpen && InView(chest.GlobalPosition) && Known(chest.GlobalPosition))
                    DrawIcon(chest.GlobalPosition, _chestIcon, RarityPalette.Main(chest.Rarity));
            }
        }
        for (int i = 0; i < Memorial.All.Count; i++)
            if (InView(Memorial.All[i].GlobalPosition) && Known(Memorial.All[i].GlobalPosition))
                DrawIcon(Memorial.All[i].GlobalPosition, _memorialIcon, MemorialColor);
        for (int i = 0; i < Rift.All.Count; i++)
            if (InView(Rift.All[i].GlobalPosition) && Known(Rift.All[i].GlobalPosition))
                DrawIcon(Rift.All[i].GlobalPosition, _riftIcon, RiftColor);
        for (int i = 0; i < Workshop.All.Count; i++)
            if (!Workshop.All[i].IsLost && InView(Workshop.All[i].GlobalPosition) && Known(Workshop.All[i].GlobalPosition))
                DrawIcon(Workshop.All[i].GlobalPosition, _workshopIcon, WorkshopColor);
        DrawIcon(_playerPosition, _playerIcon, PlayerColor);
    }

    private void DrawIcon(Vector2 world, Texture2D icon, Color color)
    {
        Vector2 position = ToMap(world).Floor() - new Vector2(2, 2);
        // Un liseré sépare les petits repères du terrain, notamment les coffres des rues claires.
        DrawTexture(icon, position + Vector2.Left, Frame);
        DrawTexture(icon, position + Vector2.Right, Frame);
        DrawTexture(icon, position + Vector2.Up, Frame);
        DrawTexture(icon, position + Vector2.Down, Frame);
        DrawTexture(icon, position, color);
    }
}
