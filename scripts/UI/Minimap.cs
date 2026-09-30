using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Minimap (plan 22 C6, plan 23 R7) : la carte vue d'en haut, révélée au fil du chemin. Un pixel par cellule
/// d'Effacement, teinté par sa phase (ancrée, fragile, effilochée, effacée, Néant) ; les lieux découverts en points de
/// leur couleur ; le joueur au centre de sa trace. Coin bas droit du HUD, repeint quatre fois par seconde, sans boucle
/// sur les décors : seulement les cellules autour du joueur et les quelques dizaines de lieux.
/// </summary>
public partial class Minimap : Control
{
    private const float UpdateInterval = 0.25f;
    private const float Width = 180f;
    private const float Margin = 10f;
    private const float BottomMargin = 64f;
    private const int RevealRadiusCells = 12;

    private static readonly Color Frame = new(0.05f, 0.05f, 0.09f, 0.72f);
    private static readonly Color Border = new(0.55f, 0.47f, 0.28f, 0.8f);
    private static readonly Color[] PhaseColors =
    {
        new(0.46f, 0.52f, 0.40f, 0.9f),
        new(0.52f, 0.50f, 0.42f, 0.9f),
        new(0.45f, 0.38f, 0.40f, 0.9f),
        new(0.30f, 0.22f, 0.34f, 0.9f),
        new(0.02f, 0.01f, 0.04f, 0.95f),
    };
    private static readonly Color PlayerColor = new(1f, 0.97f, 0.88f);
    private static readonly Color UsedPlaceColor = new(0.5f, 0.5f, 0.5f, 0.7f);
    private static readonly Color MemorialColor = new(0.95f, 0.82f, 0.45f);
    private static readonly Color RiftColor = new(0.7f, 0.35f, 0.85f);

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
    private float _timer;
    private bool _dirty;
    private Vector2 _playerPosition;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.ZonePhaseChanged += OnZonePhaseChanged;
        Visible = false;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.ZonePhaseChanged -= OnZonePhaseChanged;
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
        _bounds = _world.WorldBounds;
        _cellSize = _erasure.CellSize;
        _origin = new Vector2I(Mathf.FloorToInt(_bounds.Position.X / _cellSize), Mathf.FloorToInt(_bounds.Position.Y / _cellSize));
        int width = Mathf.CeilToInt(_bounds.Size.X / _cellSize) + 1;
        int height = Mathf.CeilToInt(_bounds.Size.Y / _cellSize) + 1;
        _image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        _texture = ImageTexture.CreateFromImage(_image);
        _known = new bool[width * height];
        float scale = Width / width;
        CustomMinimumSize = new Vector2(Width, height * scale);
        Size = CustomMinimumSize;
        Visible = true;
        return true;
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer > 0f)
            return;
        _timer = UpdateInterval;
        if (!EnsureMap() || _groups.GetPlayer() is not Node2D player)
            return;
        Control parent = GetParent<Control>();
        Position = new Vector2(parent.Size.X - Size.X - Margin, parent.Size.Y - Size.Y - BottomMargin);
        _playerPosition = player.GlobalPosition;
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
        return x < 0 || y < 0 || x >= _image.GetWidth() || y >= _image.GetHeight() ? -1 : y * _image.GetWidth() + x;
    }

    private bool Known(Vector2 world)
    {
        int index = Index(new Vector2I(Mathf.FloorToInt(world.X / _cellSize), Mathf.FloorToInt(world.Y / _cellSize)));
        return index >= 0 && _known[index];
    }

    private Vector2 ToMap(Vector2 world) => (world - _bounds.Position) / _bounds.Size * Size;

    public override void _Draw()
    {
        if (_image == null)
            return;
        Rect2 area = new(Vector2.Zero, Size);
        DrawRect(area.Grow(2f), Frame);
        DrawTextureRect(_texture, area, false);
        DrawRect(area.Grow(2f), Border, false, 1f);
        if (_places != null)
            foreach (SmallPlace place in _places.Places)
                if (Known(place.GlobalPosition))
                    DrawDot(place.GlobalPosition, place.CanInteract ? place.Data.Color : UsedPlaceColor, 1.5f);
        foreach (Chest chest in Chest.Closed)
            if (chest.CanOpen && Known(chest.GlobalPosition))
                DrawDot(chest.GlobalPosition, RarityPalette.Main(chest.Rarity), 2f);
        foreach (Memorial memorial in Memorial.All)
            if (Known(memorial.GlobalPosition))
                DrawDot(memorial.GlobalPosition, MemorialColor, 2.5f);
        foreach (Rift rift in Rift.All)
            if (Known(rift.GlobalPosition))
                DrawDot(rift.GlobalPosition, RiftColor, 2.5f);
        Vector2 player = ToMap(_playerPosition);
        DrawCircle(player, 3f, Frame);
        DrawCircle(player, 2f, PlayerColor);
    }

    private void DrawDot(Vector2 world, Color color, float radius) => DrawRect(new Rect2(ToMap(world) - Vector2.One * radius, Vector2.One * radius * 2f), color);
}
