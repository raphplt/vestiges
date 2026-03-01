using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Base;

/// <summary>
/// Système de placement de structures sur la grille iso.
/// Activé après un craft de structure. Affiche un fantôme de prévisualisation.
/// Clic gauche pour placer (maintenir pour poser en chaîne), clic droit ou Escape pour annuler.
/// </summary>
public partial class StructurePlacer : Node2D
{
    private bool _isPlacing;
    private string _recipeId;
    private RecipeData _recipeData;
    private int _pendingPlacements;
    private bool _waitingForNextCraft;
    private Polygon2D _ghost;
    private TileMapLayer _ground;
    private StructureManager _structureManager;
    private CraftManager _craftManager;
    private Node2D _structureContainer;
    private EventBus _eventBus;
    private CanvasLayer _hintLayer;
    private Label _hintLabel;

    private Vector2I _currentCell;
    private Vector2I _lastPlacedCell;
    private bool _isValidPlacement;
    private bool _hasLastPlacedCell;

    private static readonly Color ValidColor = new(0.2f, 0.8f, 0.2f, 0.5f);
    private static readonly Color InvalidColor = new(0.8f, 0.2f, 0.2f, 0.5f);

    public bool IsPlacing => _isPlacing;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.CraftCompleted += OnCraftCompleted;

        _ground = GetNode<TileMapLayer>("/root/Main/Ground");
        _structureContainer = GetNode<Node2D>("/root/Main/StructureContainer");

        CreateGhost();
        CreatePlacementHint();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.CraftCompleted -= OnCraftCompleted;
    }

    public void SetStructureManager(StructureManager manager)
    {
        _structureManager = manager;
    }

    public void SetCraftManager(CraftManager manager)
    {
        _craftManager = manager;
    }

    public override void _Process(double delta)
    {
        if (!_isPlacing)
            return;

        Vector2 mousePos = GetGlobalMousePosition();
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(mousePos));
        Vector2 snapPos = _ground.MapToLocal(cell);

        _ghost.GlobalPosition = snapPos;
        _currentCell = cell;

        if (_pendingPlacements <= 0)
        {
            _isValidPlacement = false;
            _ghost.Visible = false;
            return;
        }

        _ghost.Visible = true;
        _isValidPlacement = CheckPlacementValid(cell, snapPos);
        _ghost.Color = _isValidPlacement ? ValidColor : InvalidColor;

        // Drag-friendly chain placement: hold left click and sweep cells.
        if (Input.IsMouseButtonPressed(MouseButton.Left))
            TryPlaceCurrentCell(continuous: true);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isPlacing)
            return;

        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            TryPlaceCurrentCell(continuous: false);
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventMouseButton rightClick && rightClick.Pressed && rightClick.ButtonIndex == MouseButton.Right)
        {
            CancelPlacement();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            CancelPlacement();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnCraftCompleted(string recipeId)
    {
        RecipeData recipe = RecipeDataLoader.Get(recipeId);
        if (recipe == null)
            return;

        string type = recipe.Result.Type;
        if (type == "wall" || type == "trap" || type == "turret" || type == "light" || type == "memorial")
        {
            if (_structureManager != null && !_structureManager.CanPlaceType(type))
            {
                GD.Print($"[StructurePlacer] Cap atteint pour {type} ({_structureManager.GetCountForType(type)}/{_structureManager.GetMaxForType(type)})");
                if (_isPlacing && _recipeId == recipeId)
                    CancelPlacement();
                return;
            }
            StartPlacement(recipeId, recipe);
        }
    }

    private void StartPlacement(string recipeId, RecipeData recipe)
    {
        if (_isPlacing && _recipeId != null && _recipeId != recipeId)
            CancelPlacement();

        _recipeId = recipeId;
        _recipeData = recipe;
        _isPlacing = true;
        _waitingForNextCraft = false;
        _pendingPlacements++;
        _ghost.Visible = true;
        UpdatePlacementHint();
    }

    private void CancelPlacement()
    {
        if (_waitingForNextCraft
            && _craftManager != null
            && _craftManager.IsCrafting
            && _craftManager.CurrentRecipeId == _recipeId)
        {
            _craftManager.CancelCraft();
        }

        _isPlacing = false;
        _pendingPlacements = 0;
        _waitingForNextCraft = false;
        _ghost.Visible = false;
        _recipeId = null;
        _recipeData = null;
        _hasLastPlacedCell = false;
        UpdatePlacementHint();
    }

    private bool TryPlaceCurrentCell(bool continuous)
    {
        if (_pendingPlacements <= 0 || !_isValidPlacement)
            return false;

        if (continuous && _hasLastPlacedCell && _lastPlacedCell == _currentCell)
            return false;

        PlaceStructure();
        return true;
    }

    private void PlaceStructure()
    {
        Vector2 worldPos = _ground.MapToLocal(_currentCell);
        string type = _recipeData.Result.Type;
        float hp = _recipeData.Result.Stats.TryGetValue("hp", out float hpVal) ? hpVal : 50f;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Core.Player player)
            hp *= player.StructureHpMultiplier;

        Color structureColor = GetStructureColor();

        Structure structure;
        if (type == "trap")
        {
            Trap trap = new();
            float damage = _recipeData.Result.Stats.TryGetValue("damage", out float d) ? d : 10f;
            int uses = _recipeData.Result.Stats.TryGetValue("uses", out float u) ? (int)u : 5;
            structure = trap;
            AddStructureChildren(trap);
            trap.GlobalPosition = worldPos;
            _structureContainer.AddChild(trap);
            trap.Initialize(_recipeId, _recipeData.Id, hp, _currentCell, structureColor);
            trap.SetTrapStats(damage, uses);
        }
        else if (type == "turret")
        {
            Turret turret = new();
            float damage = _recipeData.Result.Stats.TryGetValue("damage", out float td) ? td : 8f;
            float atkSpeed = _recipeData.Result.Stats.TryGetValue("attack_speed", out float tas) ? tas : 1.5f;
            float range = _recipeData.Result.Stats.TryGetValue("range", out float tr) ? tr : 200f;
            structure = turret;
            AddStructureChildren(turret);
            turret.GlobalPosition = worldPos;
            _structureContainer.AddChild(turret);
            turret.Initialize(_recipeId, _recipeData.Id, hp, _currentCell, structureColor);
            turret.SetTurretStats(damage, atkSpeed, range);
        }
        else if (type == "light")
        {
            Torch torch = new();
            float radius = _recipeData.Result.Stats.TryGetValue("radius", out float r) ? r : 80f;
            float duration = _recipeData.Result.Stats.TryGetValue("duration", out float dur) ? dur : 180f;
            structure = torch;
            AddStructureChildren(torch);
            torch.GlobalPosition = worldPos;
            _structureContainer.AddChild(torch);
            torch.Initialize(_recipeId, _recipeData.Id, hp, _currentCell, structureColor);
            torch.SetTorchStats(radius, duration);
        }
        else
        {
            Wall wall = new();
            structure = wall;
            AddStructureChildren(wall);
            wall.GlobalPosition = worldPos;
            _structureContainer.AddChild(wall);
            wall.Initialize(_recipeId, _recipeData.Id, hp, _currentCell, structureColor);
        }

        _structureManager?.Register(_currentCell, structure);

        _eventBus.EmitSignal(EventBus.SignalName.StructurePlaced, _recipeData.Id, worldPos);

        if (type == "memorial")
            _eventBus.EmitSignal(EventBus.SignalName.MemorialActivated);
        _pendingPlacements = Mathf.Max(0, _pendingPlacements - 1);
        _lastPlacedCell = _currentCell;
        _hasLastPlacedCell = true;

        if (_pendingPlacements > 0)
        {
            UpdatePlacementHint();
            return;
        }

        if (TryStartChainCraft())
        {
            if (_pendingPlacements > 0)
            {
                _waitingForNextCraft = false;
                _ghost.Visible = true;
                UpdatePlacementHint();
                return;
            }

            _waitingForNextCraft = true;
            _ghost.Visible = false;
            _isValidPlacement = false;
            UpdatePlacementHint();
            return;
        }

        CancelPlacement();
    }

    private void AddStructureChildren(StaticBody2D structure)
    {
        Polygon2D visual = new();
        visual.Name = "Visual";
        structure.AddChild(visual);

        Polygon2D leftFace = new();
        leftFace.Name = "LeftFace";
        structure.AddChild(leftFace);

        Polygon2D rightFace = new();
        rightFace.Name = "RightFace";
        structure.AddChild(rightFace);

        CircleShape2D shape = new();
        shape.Radius = 14f;
        CollisionShape2D collider = new();
        collider.Shape = shape;
        structure.AddChild(collider);
    }

    private bool CheckPlacementValid(Vector2I cell, Vector2 worldPos)
    {
        if (_structureManager != null && _structureManager.IsOccupied(cell))
            return false;

        float distToFoyer = worldPos.DistanceTo(Vector2.Zero);
        if (distToFoyer > Foyer.SafeRadius * 2.5f)
            return false;

        if (distToFoyer < 30f)
            return false;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Node2D player && player.GlobalPosition.DistanceTo(worldPos) < 20f)
            return false;

        // No building on water
        WorldSetup world = GetNodeOrNull<WorldSetup>("/root/Main");
        if (world != null && world.IsWaterAt(worldPos))
            return false;

        return true;
    }

    private Color GetStructureColor()
    {
        return _recipeId switch
        {
            "wood_wall" => new Color(0.55f, 0.35f, 0.1f),
            "stone_wall" => new Color(0.6f, 0.6f, 0.6f),
            "metal_wall" => new Color(0.7f, 0.7f, 0.8f),
            "barricade" => new Color(0.45f, 0.3f, 0.15f),
            "spike_trap" => new Color(0.5f, 0.35f, 0.2f),
            "turret_basic" => new Color(0.4f, 0.5f, 0.4f),
            "torch" => new Color(1f, 0.8f, 0.3f),
            "memorial" => new Color(0.6f, 0.5f, 0.8f),
            _ => new Color(0.5f, 0.5f, 0.5f)
        };
    }

    private void CreateGhost()
    {
        _ghost = new Polygon2D();
        float s = 14f;
        float h = 7f;
        // Contour hexagonal de la boîte iso (top + sides visibles)
        _ghost.Polygon = new Vector2[]
        {
            new(0, -s * 0.5f), new(s, 0), new(s, h),
            new(0, s * 0.5f + h), new(-s, h), new(-s, 0)
        };
        _ghost.Color = ValidColor;
        _ghost.Visible = false;
        _ghost.ZIndex = 10;
        AddChild(_ghost);
    }

    private bool TryStartChainCraft()
    {
        if (_craftManager == null || string.IsNullOrEmpty(_recipeId) || _recipeData == null)
            return false;

        string type = _recipeData.Result.Type;
        if (_structureManager != null && !_structureManager.CanPlaceType(type))
            return false;

        return _craftManager.StartCraft(_recipeId);
    }

    private void CreatePlacementHint()
    {
        _hintLayer = new CanvasLayer { Layer = 50 };
        AddChild(_hintLayer);

        PanelContainer panel = new();
        panel.AnchorLeft = 0.5f;
        panel.AnchorRight = 0.5f;
        panel.AnchorTop = 1f;
        panel.AnchorBottom = 1f;
        panel.OffsetLeft = -300f;
        panel.OffsetRight = 300f;
        panel.OffsetTop = -62f;
        panel.OffsetBottom = -28f;

        StyleBoxFlat style = new();
        style.BgColor = new Color(0.06f, 0.06f, 0.08f, 0.85f);
        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 6;
        style.ContentMarginBottom = 6;
        panel.AddThemeStyleboxOverride("panel", style);

        _hintLabel = new Label();
        _hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _hintLabel.VerticalAlignment = VerticalAlignment.Center;
        _hintLabel.AddThemeFontSizeOverride("font_size", 12);
        _hintLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.92f));
        panel.AddChild(_hintLabel);

        _hintLayer.AddChild(panel);
        _hintLayer.Visible = false;
    }

    private void UpdatePlacementHint()
    {
        if (_hintLayer == null || _hintLabel == null)
            return;

        if (!_isPlacing)
        {
            _hintLayer.Visible = false;
            return;
        }

        _hintLayer.Visible = true;
        if (_pendingPlacements > 0)
        {
            _hintLabel.Text = "Construction: clic gauche pour poser, maintiens pour poser en chaine, clic droit ou Echap pour annuler";
        }
        else if (_waitingForNextCraft)
        {
            _hintLabel.Text = "Construction en chaine: fabrication suivante en cours...";
        }
        else
        {
            _hintLabel.Text = "Construction active";
        }
    }
}
