using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Minimap (plan 22 C6, plan 23 R7, plan 24 A6) : un radar rond façon boussole autour du joueur (plan 37 R4), et la
/// carte entière avec sa légende en maintenant « show_map ». Le terrain des biomes, routes et sentiers est échantillonné
/// à la découverte, avec quatre pixels par axe de cellule d'Effacement ; le danger recolore ce fond sans refaire les
/// relevés. Trois calques : un masque (disque de brouillard tramé, ou rectangle de la carte entière) qui découpe le
/// terrain et les repères, puis l'anneau de pierre. Mise à jour quatre fois par seconde, sans parcours des décors ni du
/// monde entier.
/// </summary>
public partial class Minimap : Control
{
    private const float UpdateInterval = 0.25f;
    private const float RadarSize = 112f;
    /// <summary>Cellules de côté que montre le radar : 17 cellules de 128 px, environ deux écrans (×1,5 depuis 26, §117).</summary>
    private const int RadarCells = 17;
    private const int BezelWidth = 6;
    private const float FullMaxHeight = 480f;
    private const float FullMaxWidth = 700f;
    private const float Margin = 10f;
    private const float BottomMargin = 64f;
    private const int LegendFontSize = 8;
    /// <summary>Retrait du bord du disque où s'épinglent les buts lointains de la terre (plan 57 C2).</summary>
    private const float EdgeInset = 5f;
    /// <summary>Battement du repaire prêt : une onde qui s'élargit de LairPulseRadius pixels par période.</summary>
    private const float LairPulseSeconds = 1f;
    private const float LairPulseRadius = 5f;
    /// <summary>
    /// Rayon de découverte, un peu en deçà du bord du radar (8,5 cellules) : le front du brouillard s'y voit avancer.
    /// Sans lien avec la quête « Dessiner la carte », qui compte le brouillard du monde.
    /// </summary>
    private const int RevealRadiusCells = 8;
    /// <summary>Écart du rayon de découverte d'une cellule à l'autre : le front du brouillard s'effiloche.</summary>
    private const float RevealJitterCells = 1.6f;

    private static readonly Color Frame = new(0.05f, 0.05f, 0.09f, 0.78f);
    private static readonly Color Border = new(0.55f, 0.47f, 0.28f, 0.85f);
    private static readonly Color Unknown = new(0.08f, 0.08f, 0.12f, 0.85f);
    private static readonly Color PlayerColor = new(1f, 0.97f, 0.88f);
    private static readonly Color MemorialColor = new(0.95f, 0.82f, 0.45f);
    // Mémorial encore endormi (plan 57 C2) : pierre pâle, l'or est pour ceux qu'on a ravivés.
    private static readonly Color DormantMemorialColor = new(0.72f, 0.7f, 0.8f);
    private static readonly Color RiftColor = new(0.7f, 0.35f, 0.85f);
    private static readonly Color WorkshopColor = new(0.91f, 0.52f, 0.24f);
    private static readonly Color WellColor = new(0.55f, 0.72f, 0.83f);
    private static readonly Color PlaceColor = new(0.78f, 0.86f, 0.92f);
    private static readonly Color GuardianColor = new(0.86f, 0.55f, 0.95f);
    // Anneau de terre restante autour du radar (plan 51 P2c) : il se vide avec la terre, jusqu'au cœur.
    private static readonly Color LandRingBack = new(0.08f, 0.07f, 0.11f, 0.85f);
    private static readonly Color LandRingCalm = new(0.92f, 0.78f, 0.45f);
    private static readonly Color LandRingLow = new(0.95f, 0.4f, 0.32f);
    private const float LandRingLowShare = 0.25f;
    private float _landRemaining = -1f;
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
    private Texture2D _steleIcon;
    private Texture2D _workshopIcon;
    private Texture2D _wellIcon;
    private Texture2D _playerIcon;
    private MinimapLayer _lens;
    private MinimapLayer _content;
    private MinimapLayer _bezel;
    private ShaderMaterial _disc;
    // Forme dessinée par le masque et l'anneau : ils ne se redessinent qu'au passage du radar à la carte entière.
    private bool? _shownFull;
    private static Texture2D _fogDisc;
    private static Texture2D _ring;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        const string iconFolder = "res://assets/ui/hud/plan25/";
        _placeIcon = GD.Load<Texture2D>(iconFolder + "minimap_place.png");
        _chestIcon = GD.Load<Texture2D>(iconFolder + "minimap_chest.png");
        _memorialIcon = GD.Load<Texture2D>(iconFolder + "minimap_memorial.png");
        _riftIcon = GD.Load<Texture2D>(iconFolder + "minimap_rift.png");
        _steleIcon = GD.Load<Texture2D>(iconFolder + "minimap_peril_stele.png");
        _workshopIcon = GD.Load<Texture2D>(iconFolder + "minimap_workshop.png");
        _wellIcon = GD.Load<Texture2D>(iconFolder + "minimap_well.png");
        _playerIcon = GD.Load<Texture2D>(iconFolder + "minimap_player.png");
        _fogDisc ??= BuildFogDisc();
        _ring ??= BuildRing();
        _lens = new MinimapLayer { Name = "Lens", Paint = DrawLens };
        AddChild(_lens);
        // Un shader découpe le terrain et les repères au disque : pas de copie d'écran comme avec un masque d'enfants.
        _disc = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/minimap_disc.gdshader") };
        _disc.SetShaderParameter("center", Vector2.One * RadarSize / 2f);
        _content = new MinimapLayer { Name = "Content", Paint = DrawContent, Material = _disc };
        AddChild(_content);
        _bezel = new MinimapLayer { Name = "Bezel", Paint = DrawBezel };
        AddChild(_bezel);
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.ZonePhaseChanged += OnZonePhaseChanged;
        _eventBus.OubliEffectChanged += OnOubliEffectChanged;
        _eventBus.LandChanging += OnLandChanging;
        Visible = false;
    }

    /// <summary>
    /// Changement de terre (plan 51) : la carte de la terre quittée et ses petits lieux sont oubliés ; la suivante se
    /// redessine à la première mise à jour où elle est prête.
    /// </summary>
    private void OnLandChanging(int toIndex)
    {
        _raster?.Dispose();
        _raster = null;
        _places = null;
        _legend?.QueueFree();
        _legend = null;
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
            _eventBus.LandChanging -= OnLandChanging;
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
            (_memorialIcon, DormantMemorialColor, "MAP_LEGEND_MEMORIAL_DORMANT"),
            (_memorialIcon, MemorialColor, "MAP_LEGEND_MEMORIAL"),
            (_memorialIcon, GuardianColor, "MAP_LEGEND_GUARDIAN"),
            (_riftIcon, RiftColor, "MAP_LEGEND_RIFT"),
            (_steleIcon, PlayerSheet.PerilColor, "MAP_LEGEND_PERIL_STELE"),
            (_workshopIcon, WorkshopColor, "MAP_LEGEND_WORKSHOP"),
            (_wellIcon, WellColor, "MAP_LEGEND_WELL"),
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
            // Le gardien n'existe que sur les terres.
            if (key == "MAP_LEGEND_GUARDIAN" && GuardianLair.Current == null)
                continue;
            HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 4);
            row.AddChild(new MapLegendIcon(icon, color, key == "MAP_LEGEND_VOID"));
            Label label = new() { Text = Tr(key), MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeColorOverride("font_color", new Color(0.88f, 0.84f, 0.76f));
            UITheme.SetCompactText(label, LegendFontSize);
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
        if (_shownFull != full)
        {
            _shownFull = full;
            _disc.SetShaderParameter("radius", full ? 0f : RadarSize / 2f);
            _lens.QueueRedraw();
            _bezel.QueueRedraw();
        }
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
        UpdateLandRemaining();
        // Fenêtre de monde montrée : la carte entière, ou un carré centré sur le joueur, calé sur la grille d'une unité du
        // radar pour que les cellules glissent pixel par pixel.
        float worldSize = RadarCells * _cellSize;
        float step = worldSize / RadarSize;
        Vector2 corner = ((_playerPosition - Vector2.One * worldSize / 2f) / step).Floor() * step;
        _view = _full ? new Rect2(_origin * _cellSize, new Vector2(_width, _height) * _cellSize) : new Rect2(corner, Vector2.One * worldSize);
        RevealAround(_playerPosition);
        if (_raster.Flush())
            _texture.Update(_raster.Image);
        _content.QueueRedraw();
    }

    /// <summary>Les cellules autour du joueur sont vues : elles prennent la couleur de leur phase.</summary>
    private void RevealAround(Vector2 position)
    {
        Vector2I center = new(Mathf.FloorToInt(position.X / _cellSize), Mathf.FloorToInt(position.Y / _cellSize));
        for (int dx = -RevealRadiusCells; dx <= RevealRadiusCells; dx++)
        {
            for (int dy = -RevealRadiusCells; dy <= RevealRadiusCells; dy++)
            {
                Vector2I cell = new(center.X + dx, center.Y + dy);
                float radius = RevealRadiusCells - RevealJitterCells * Jitter(cell);
                if (dx * dx + dy * dy > radius * radius)
                    continue;
                if (!_raster.IsKnown(cell))
                    _raster.Paint(cell, (int)_erasure.GetZonePhaseAt(_erasure.CellCenterToWorld(cell)), reveal: true);
            }
        }
    }

    /// <summary>Bruit fixe par cellule, entre 0 et 1 : le même front revient à chaque passage.</summary>
    private static float Jitter(Vector2I cell)
    {
        uint hash = (uint)(cell.X * 73856093) ^ (uint)(cell.Y * 19349663);
        hash = (hash ^ (hash >> 13)) * 1274126177u;
        return (hash & 0xFFFF) / 65535f;
    }

    /// <summary>Le front de l'Effacement avance aussi dans les cellules déjà vues.</summary>
    private void OnZonePhaseChanged(int cellX, int cellY, int phase) =>
        _raster?.Paint(new Vector2I(cellX, cellY), phase);

    private bool Known(Vector2 world) =>
        _raster.IsKnown(new Vector2I(Mathf.FloorToInt(world.X / _cellSize), Mathf.FloorToInt(world.Y / _cellSize)));

    private Vector2 ToMap(Vector2 world) => (world - _view.Position) / _view.Size * Size;

    private bool InView(Vector2 world) => _view.HasPoint(world);

    /// <summary>Masque : disque de brouillard tramé pour le radar, rectangle sombre pour la carte entière.</summary>
    private void DrawLens(MinimapLayer layer)
    {
        if (_raster == null)
            return;
        if (!_full)
        {
            layer.DrawTexture(_fogDisc, Vector2.Zero);
            return;
        }
        Rect2 area = new(Vector2.Zero, Size);
        layer.DrawRect(new Rect2(Vector2.Zero, new Vector2(Size.X + 8f + _legend.Size.X, Mathf.Max(Size.Y, _legend.Size.Y))).Grow(6f), Frame with { A = 0.97f });
        layer.DrawRect(area.Grow(2f), Frame);
        layer.DrawRect(area, Unknown);
    }

    /// <summary>Anneau de pierre du radar, ou simple filet doré autour de la carte entière.</summary>
    /// <summary>Part de la terre encore debout hors du cœur, de 1 à l'arrivée à 0 quand le gardien se lève seul.</summary>
    private void UpdateLandRemaining()
    {
        LandFront front = _erasure.Front;
        float remaining = front == null ? -1f
            : Mathf.Clamp((front.StandingShare() - front.HeartShare) / Mathf.Max(0.0001f, 1f - front.HeartShare), 0f, 1f);
        if (Mathf.IsEqualApprox(remaining, _landRemaining))
            return;
        _landRemaining = remaining;
        _bezel.QueueRedraw();
    }

    private void DrawBezel(MinimapLayer layer)
    {
        if (_raster == null)
            return;
        if (_full)
        {
            layer.DrawRect(new Rect2(Vector2.Zero, Size).Grow(2f), Border, false, 1f);
            return;
        }
        layer.DrawTexture(_ring, -Vector2.One * BezelWidth);
        if (_landRemaining < 0f)
            return;
        // Depuis midi, dans le sens des aiguilles : la terre qui reste ; vide, le gardien se lève seul.
        Vector2 center = Vector2.One * RadarSize / 2f;
        float radius = RadarSize / 2f + BezelWidth + 3f;
        layer.DrawArc(center, radius, 0f, Mathf.Tau, 64, LandRingBack, 3f);
        if (_landRemaining > 0f)
            layer.DrawArc(center, radius, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * _landRemaining, 64,
                _landRemaining <= LandRingLowShare ? LandRingLow : LandRingCalm, 3f);
    }

    private void DrawContent(MinimapLayer layer)
    {
        if (_raster == null)
            return;
        // Partie de la carte dans la fenêtre : la région de l'image qui lui correspond, posée à sa place.
        Rect2 visible = _view.Intersection(new Rect2(_origin * _cellSize, new Vector2(_width, _height) * _cellSize));
        if (visible.Size.X > 0f && visible.Size.Y > 0f)
        {
            Rect2 source = new((visible.Position - (Vector2)(_origin * _cellSize)) / _cellSize * CartographyRaster.Detail, visible.Size / _cellSize * CartographyRaster.Detail);
            layer.DrawTextureRectRegion(_texture, new Rect2(ToMap(visible.Position), visible.Size / _view.Size * Size), source);
        }
        // Boucles indexées : un foreach sur une IReadOnlyList alloue son énumérateur à chaque repeinte.
        // Un petit lieu servi disparaît de la carte.
        if (_places != null)
        {
            for (int i = 0; i < _places.Places.Count; i++)
            {
                SmallPlace place = _places.Places[i];
                if (place.CanInteract && InView(place.GlobalPosition) && Known(place.GlobalPosition))
                    DrawIcon(layer, place.GlobalPosition, _placeIcon, PlaceColor);
            }
        }
        if (!_chestSignalsForgotten)
        {
            for (int i = 0; i < Chest.Closed.Count; i++)
            {
                Chest chest = Chest.Closed[i];
                if (chest.CanOpen && InView(chest.GlobalPosition) && Known(chest.GlobalPosition))
                    DrawIcon(layer, chest.GlobalPosition, _chestIcon, RarityPalette.Main(chest.Rarity));
            }
        }
        // Sur une terre, les Mémoriaux sont les clés du gardien (plan 57 C2) : visibles dès l'arrivée, même dans le
        // brouillard ; les endormis lointains s'épinglent au bord du radar, du côté où les chercher.
        bool land = GuardianLair.Current != null;
        for (int i = 0; i < Memorial.All.Count; i++)
        {
            Memorial memorial = Memorial.All[i];
            if (memorial.State == Memorial.MemorialState.Lost || !land && !Known(memorial.GlobalPosition))
                continue;
            bool dormant = memorial.State is Memorial.MemorialState.Dormant or Memorial.MemorialState.Gathering;
            DrawMarker(layer, memorial.GlobalPosition, _memorialIcon, dormant ? DormantMemorialColor : MemorialColor, land && dormant);
        }
        for (int i = 0; i < Rift.All.Count; i++)
            if (InView(Rift.All[i].GlobalPosition) && Known(Rift.All[i].GlobalPosition))
                DrawIcon(layer, Rift.All[i].GlobalPosition, _riftIcon, RiftColor);
        for (int i = 0; i < PerilStele.All.Count; i++)
        {
            PerilStele stele = PerilStele.All[i];
            if (stele.IsLit && InView(stele.GlobalPosition) && Known(stele.GlobalPosition))
                DrawIcon(layer, stele.GlobalPosition, _steleIcon, PlayerSheet.PerilColor);
        }
        for (int i = 0; i < Workshop.All.Count; i++)
            if (!Workshop.All[i].IsLost && InView(Workshop.All[i].GlobalPosition) && Known(Workshop.All[i].GlobalPosition))
                DrawIcon(layer, Workshop.All[i].GlobalPosition, _workshopIcon, WorkshopColor);
        for (int i = 0; i < Well.All.Count; i++)
            if (!Well.All[i].IsLost && InView(Well.All[i].GlobalPosition) && Known(Well.All[i].GlobalPosition))
                DrawIcon(layer, Well.All[i].GlobalPosition, _wellIcon, WellColor);
        // Le repaire du gardien se voit dès l'arrivée, épinglé au bord s'il est loin : c'est le but de la terre. Prêt à
        // réveiller, il bat.
        if (GuardianLair.Current is { Visible: true } lair && MarkerPosition(lair.GlobalPosition, true) is Vector2 lairAt)
        {
            if (lair.CanInteract)
            {
                float pulse = (float)(Time.GetTicksMsec() / 1000.0 % LairPulseSeconds) / LairPulseSeconds;
                layer.DrawArc(lairAt + Vector2.One * 0.5f, 3f + LairPulseRadius * pulse, 0f, Mathf.Tau, 16,
                    GuardianColor with { A = 1f - pulse }, 1f);
            }
            DrawIconAt(layer, lairAt, _memorialIcon, lair.CanInteract ? GuardianColor.Lightened(0.25f) : GuardianColor);
        }
        DrawIcon(layer, _playerPosition, _playerIcon, PlayerColor);
    }

    private void DrawIcon(MinimapLayer layer, Vector2 world, Texture2D icon, Color color) =>
        DrawIconAt(layer, ToMap(world).Floor(), icon, color);

    /// <summary>Un but de la terre : à sa place s'il tient dans le radar, sinon épinglé au bord si <paramref name="pin"/>.</summary>
    private void DrawMarker(MinimapLayer layer, Vector2 world, Texture2D icon, Color color, bool pin)
    {
        if (MarkerPosition(world, pin) is Vector2 at)
            DrawIconAt(layer, at, icon, color);
    }

    private Vector2? MarkerPosition(Vector2 world, bool pin)
    {
        Vector2 at = ToMap(world);
        if (_full)
            return InView(world) ? at.Floor() : null;
        Vector2 center = Vector2.One * RadarSize / 2f;
        float reach = RadarSize / 2f - EdgeInset;
        Vector2 offset = at - center;
        if (offset.LengthSquared() <= reach * reach)
            return at.Floor();
        return pin ? (center + offset.Normalized() * reach).Floor() : null;
    }

    private void DrawIconAt(MinimapLayer layer, Vector2 map, Texture2D icon, Color color)
    {
        Vector2 position = map - new Vector2(2, 2);
        // Un liseré sépare les petits repères du terrain, notamment les coffres des rues claires.
        layer.DrawTexture(icon, position + Vector2.Left, Frame);
        layer.DrawTexture(icon, position + Vector2.Right, Frame);
        layer.DrawTexture(icon, position + Vector2.Up, Frame);
        layer.DrawTexture(icon, position + Vector2.Down, Frame);
        layer.DrawTexture(icon, position, color);
    }

    /// <summary>Disque du radar au pixel près : deux tons sombres en trame et un grain fixe, le brouillard de l'inconnu.</summary>
    private static Texture2D BuildFogDisc()
    {
        int size = (int)RadarSize;
        float radius = size / 2f;
        using Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Color low = new("13121c");
        Color high = new("1e1c2b");
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = new Vector2(x + 0.5f - radius, y + 0.5f - radius).Length();
            if (distance > radius)
                continue;
            bool lit = ((x + y) & 1) == 0 && Jitter(new Vector2I(x, y)) > 0.45f;
            image.SetPixel(x, y, (lit ? high : low) with { A = 0.94f });
        }
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// Anneau de pierre au pixel : filet d'encre de part et d'autre, pierre éclairée en haut à gauche, quatre repères
    /// cardinaux et le nord doré.
    /// </summary>
    private static Texture2D BuildRing()
    {
        int size = (int)RadarSize + BezelWidth * 2;
        float center = size / 2f;
        float inner = RadarSize / 2f;
        using Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Color ink = new("120f1a");
        Color stone = new("5f5763");
        Color light = new("8d8392");
        Color shadow = new("3b3541");
        Color gold = new("e0b552");
        Color goldBright = new("f6dc92");
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 offset = new(x + 0.5f - center, y + 0.5f - center);
            float distance = offset.Length();
            float depth = distance - inner;
            if (depth < 0f || depth >= BezelWidth)
                continue;
            Vector2 direction = offset / distance;
            Color color;
            if (depth < 1f || depth >= BezelWidth - 1f)
                color = ink;
            else
            {
                float lightness = direction.Dot(new Vector2(-0.7071f, -0.7071f));
                color = lightness > 0.35f ? light : lightness < -0.35f ? shadow : stone;
                if (((x * 7 + y * 13) % 11) == 0)
                    color = color.Darkened(0.12f);
            }
            // Repères cardinaux : une encoche dorée dans la pierre ; le nord, plus large et plus clair.
            bool vertical = Mathf.Abs(offset.X) < 1.5f;
            bool horizontal = Mathf.Abs(offset.Y) < 1.5f;
            if (depth >= 1f && depth < BezelWidth - 1f && (vertical || horizontal))
                color = vertical && offset.Y < 0f ? goldBright : gold;
            if (depth >= 1f && depth < BezelWidth - 1f && offset.Y < 0f && Mathf.Abs(offset.X) < 3f)
                color = goldBright;
            image.SetPixel(x, y, color);
        }
        return ImageTexture.CreateFromImage(image);
    }
}
