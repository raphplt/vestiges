using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Minimap (plan 22 C6, plan 23 R7, plan 24 A6) : un radar zoomé autour du joueur, et la carte entière avec sa légende
/// en maintenant « show_map ». Un pixel par cellule d'Effacement, teinté par sa phase (ancrée, fragile, effilochée,
/// effacée, Néant), révélé au fil du chemin ; les lieux en pictogrammes de pixels (coffre, stèle, faille, point pour
/// les petits lieux encore utiles). Repeint quatre fois par seconde, sans boucle sur les décors : seulement les cellules
/// autour du joueur et les quelques dizaines de lieux.
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
    private const int RevealRadiusCells = 12;

    private static readonly Color Frame = new(0.05f, 0.05f, 0.09f, 0.78f);
    private static readonly Color Border = new(0.55f, 0.47f, 0.28f, 0.85f);
    private static readonly Color Unknown = new(0.08f, 0.08f, 0.12f, 0.85f);
    private static readonly Color[] PhaseColors =
    {
        new(0.46f, 0.52f, 0.40f, 0.9f),
        new(0.52f, 0.50f, 0.42f, 0.9f),
        new(0.45f, 0.38f, 0.40f, 0.9f),
        new(0.30f, 0.22f, 0.34f, 0.9f),
        new(0.02f, 0.01f, 0.04f, 0.95f),
    };
    private static readonly Color PlayerColor = new(1f, 0.97f, 0.88f);
    private static readonly Color MemorialColor = new(0.95f, 0.82f, 0.45f);
    private static readonly Color RiftColor = new(0.7f, 0.35f, 0.85f);
    private static readonly Color PlaceColor = new(0.78f, 0.86f, 0.92f);
    private static readonly Color IconOutline = new(0.03f, 0.02f, 0.05f, 0.95f);

    // Pictogrammes en pixels : '#' couleur du lieu, '+' reflet clair.
    private static readonly string[] ChestIcon = { "#####", "#+++#", "#####", "#####" };
    private static readonly string[] MemorialIcon = { " # ", "###", "#+#", "#+#", "###" };
    private static readonly string[] RiftIcon = { "  #", " # ", "#  ", " # ", "  #" };
    private static readonly string[] PlaceIcon = { "##", "##" };
    private static readonly string[] PlayerIcon = { " # ", "###", " # " };

    private ErasureManager _erasure;
    private WorldSetup _world;
    private SmallPlaceDirector _places;
    private GroupCache _groups;
    private EventBus _eventBus;
    private Rect2 _bounds;
    private int _cellSize;
    private Vector2I _origin;
    private Image _image;
    private ImageTexture _texture;
    private bool[] _known;
    private int _width;
    private int _height;
    // Oubli des repères (plan 17 lot 3D) : comme les flèches, les coffres quittent la carte tant qu'il est porté.
    private bool _chestSignalsForgotten;
    private float _timer;
    private bool _dirty;
    private bool _full;
    private Rect2 _view;
    private Vector2 _mapSize;
    private VBoxContainer _legend;
    private Vector2 _playerPosition;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.ZonePhaseChanged += OnZonePhaseChanged;
        _eventBus.OubliEffectChanged += OnOubliEffectChanged;
        Visible = false;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.ZonePhaseChanged -= OnZonePhaseChanged;
            _eventBus.OubliEffectChanged -= OnOubliEffectChanged;
        }
    }

    private void OnOubliEffectChanged(string effect, float total)
    {
        if (effect == "chest_signals")
            _chestSignalsForgotten = total > 0f;
    }

    /// <summary>La carte n'existe qu'une fois le monde généré : l'image se crée à la première mise à jour possible.</summary>
    private bool EnsureMap()
    {
        if (_image != null)
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
        _image = Image.CreateEmpty(_width, _height, false, Image.Format.Rgba8);
        _texture = ImageTexture.CreateFromImage(_image);
        _known = new bool[_width * _height];
        // Carte entière : un nombre entier d'unités par cellule, pour des pixels francs.
        int unit = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(FullMaxHeight / _height, FullMaxWidth / _width)));
        _mapSize = new Vector2(_width * unit, _height * unit);
        BuildLegend();
        Visible = true;
        return true;
    }

    /// <summary>Légende de la carte entière : un pictogramme et un mot par sorte de lieu.</summary>
    private void BuildLegend()
    {
        _legend = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _legend.AddThemeConstantOverride("separation", 2);
        AddChild(_legend);
        foreach ((string[] icon, Color color, string key) in new (string[], Color, string)[]
        {
            (PlayerIcon, PlayerColor, "MAP_LEGEND_YOU"),
            (ChestIcon, RarityPalette.Main("rare"), "MAP_LEGEND_CHEST"),
            (MemorialIcon, MemorialColor, "MAP_LEGEND_MEMORIAL"),
            (RiftIcon, RiftColor, "MAP_LEGEND_RIFT"),
            (PlaceIcon, PlaceColor, "MAP_LEGEND_PLACE"),
        })
        {
            HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 4);
            row.AddChild(new MapLegendIcon(icon, color));
            Label label = new() { Text = Tr(key), MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", 8);
            label.AddThemeColorOverride("font_color", new Color(0.88f, 0.84f, 0.76f));
            label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
            label.AddThemeConstantOverride("outline_size", 3);
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
            Position = ((parent.Size - Size) / 2f).Floor();
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
        if (_dirty)
        {
            _texture.Update(_image);
            _dirty = false;
        }
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
                int index = Index(cell);
                if (index < 0 || _known[index])
                    continue;
                _known[index] = true;
                Paint(cell, (int)_erasure.GetZonePhaseAt(_erasure.CellCenterToWorld(cell)));
            }
        }
    }

    /// <summary>Le front de l'Effacement avance aussi dans les cellules déjà vues.</summary>
    private void OnZonePhaseChanged(int cellX, int cellY, int phase)
    {
        if (_image == null)
            return;
        Vector2I cell = new(cellX, cellY);
        int index = Index(cell);
        if (index >= 0 && _known[index])
            Paint(cell, phase);
    }

    private void Paint(Vector2I cell, int phase)
    {
        _image.SetPixel(cell.X - _origin.X, cell.Y - _origin.Y, PhaseColors[Mathf.Clamp(phase, 0, PhaseColors.Length - 1)]);
        _dirty = true;
    }

    private int Index(Vector2I cell)
    {
        int x = cell.X - _origin.X;
        int y = cell.Y - _origin.Y;
        return x < 0 || y < 0 || x >= _width || y >= _height ? -1 : y * _width + x;
    }

    private bool Known(Vector2 world)
    {
        int index = Index(new Vector2I(Mathf.FloorToInt(world.X / _cellSize), Mathf.FloorToInt(world.Y / _cellSize)));
        return index >= 0 && _known[index];
    }

    private Vector2 ToMap(Vector2 world) => (world - _view.Position) / _view.Size * Size;

    private bool InView(Vector2 world) => _view.HasPoint(world);

    public override void _Draw()
    {
        if (_image == null)
            return;
        Rect2 area = new(Vector2.Zero, Size);
        DrawRect(area.Grow(2f), Frame);
        DrawRect(area, Unknown);
        // Partie de la carte dans la fenêtre : la région de l'image qui lui correspond, posée à sa place.
        Rect2 visible = _view.Intersection(new Rect2(_origin * _cellSize, new Vector2(_width, _height) * _cellSize));
        if (visible.Size.X > 0f && visible.Size.Y > 0f)
        {
            Rect2 source = new((visible.Position - (Vector2)(_origin * _cellSize)) / _cellSize, visible.Size / _cellSize);
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
                    DrawIcon(place.GlobalPosition, PlaceIcon, PlaceColor);
            }
        }
        if (!_chestSignalsForgotten)
        {
            for (int i = 0; i < Chest.Closed.Count; i++)
            {
                Chest chest = Chest.Closed[i];
                if (chest.CanOpen && InView(chest.GlobalPosition) && Known(chest.GlobalPosition))
                    DrawIcon(chest.GlobalPosition, ChestIcon, RarityPalette.Main(chest.Rarity));
            }
        }
        for (int i = 0; i < Memorial.All.Count; i++)
            if (InView(Memorial.All[i].GlobalPosition) && Known(Memorial.All[i].GlobalPosition))
                DrawIcon(Memorial.All[i].GlobalPosition, MemorialIcon, MemorialColor);
        for (int i = 0; i < Rift.All.Count; i++)
            if (InView(Rift.All[i].GlobalPosition) && Known(Rift.All[i].GlobalPosition))
                DrawIcon(Rift.All[i].GlobalPosition, RiftIcon, RiftColor);
        DrawIcon(_playerPosition, PlayerIcon, PlayerColor);
    }

    /// <summary>Pictogramme centré sur la position du lieu, contour sombre d'un pixel dessiné d'abord.</summary>
    private void DrawIcon(Vector2 world, string[] icon, Color color)
    {
        Vector2 center = ToMap(world).Floor();
        Vector2 origin = center - new Vector2(icon[0].Length / 2, icon.Length / 2);
        DrawPattern(this, origin, icon, color);
    }

    /// <summary>Dessine un motif de pixels : contour sombre, couleur, reflet clair. Partagé avec la légende.</summary>
    public static void DrawPattern(CanvasItem canvas, Vector2 origin, string[] icon, Color color)
    {
        for (int y = 0; y < icon.Length; y++)
            for (int x = 0; x < icon[y].Length; x++)
                if (icon[y][x] != ' ')
                    canvas.DrawRect(new Rect2(origin + new Vector2(x - 1, y - 1), new Vector2(3f, 3f)), IconOutline);
        for (int y = 0; y < icon.Length; y++)
            for (int x = 0; x < icon[y].Length; x++)
                if (icon[y][x] != ' ')
                    canvas.DrawRect(new Rect2(origin + new Vector2(x, y), Vector2.One), icon[y][x] == '+' ? color.Lightened(0.45f) : color);
    }
}
