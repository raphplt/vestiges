using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Base;

/// <summary>
/// Système de placement de structures sur la grille iso.
/// Activé après un craft de structure. Affiche un fantôme de prévisualisation.
/// Click gauche pour placer, Escape pour annuler.
/// </summary>
public partial class StructurePlacer : Node2D
{
    private bool _isPlacing;
    private string _recipeId;
    private RecipeData _recipeData;
    private Polygon2D _ghost;
    private TileMapLayer _ground;
    private StructureManager _structureManager;
    private Node2D _structureContainer;
    private EventBus _eventBus;

    private Vector2I _currentCell;
    private bool _isValidPlacement;

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

    public override void _Process(double delta)
    {
        if (!_isPlacing)
            return;

        Vector2 mousePos = GetGlobalMousePosition();
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(mousePos));
        Vector2 snapPos = _ground.MapToLocal(cell);

        _ghost.GlobalPosition = snapPos;
        _currentCell = cell;

        _isValidPlacement = CheckPlacementValid(cell, snapPos);
        _ghost.Color = _isValidPlacement ? ValidColor : InvalidColor;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isPlacing)
            return;

        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            if (_isValidPlacement)
                PlaceStructure();
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
        if (type == "wall" || type == "trap" || type == "turret" || type == "light")
        {
            StartPlacement(recipeId, recipe);
        }
    }

    private void StartPlacement(string recipeId, RecipeData recipe)
    {
        _recipeId = recipeId;
        _recipeData = recipe;
        _isPlacing = true;
        _ghost.Visible = true;
    }

    private void CancelPlacement()
    {
        _isPlacing = false;
        _ghost.Visible = false;
        _recipeId = null;
        _recipeData = null;
    }

    private void PlaceStructure()
    {
        Vector2 worldPos = _ground.MapToLocal(_currentCell);
        string type = _recipeData.Result.Type;
        float hp = _recipeData.Result.Stats.TryGetValue("hp", out float hpVal) ? hpVal : 50f;

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

        CancelPlacement();
    }

    private void AddStructureChildren(StaticBody2D structure)
    {
        Polygon2D visual = new();
        visual.Name = "Visual";
        structure.AddChild(visual);

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
            _ => new Color(0.5f, 0.5f, 0.5f)
        };
    }

    private void CreateGhost()
    {
        _ghost = new Polygon2D();
        float s = 14f;
        _ghost.Polygon = new Vector2[] { new(-s, 0), new(0, -s * 0.5f), new(s, 0), new(0, s * 0.5f) };
        _ghost.Color = ValidColor;
        _ghost.Visible = false;
        _ghost.ZIndex = 10;
        AddChild(_ghost);
    }
}
