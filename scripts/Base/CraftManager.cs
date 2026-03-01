using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Base;

/// <summary>
/// Gestionnaire du système de craft.
/// Vérifie les ingrédients, consomme les ressources, gère le timer de fabrication.
/// Le joueur doit être dans le rayon du Foyer pour crafter.
/// </summary>
public partial class CraftManager : Node
{
    private Inventory _inventory;
    private EventBus _eventBus;

    private string _currentRecipeId;
    private float _craftProgress;
    private float _craftDuration;
    private bool _isCrafting;

    public bool IsCrafting => _isCrafting;
    public float CraftProgress => _isCrafting ? _craftProgress / _craftDuration : 0f;
    public string CurrentRecipeId => _currentRecipeId;

    [Signal] public delegate void CraftProgressUpdatedEventHandler(float progress);

    public override void _Ready()
    {
        RecipeDataLoader.Load();
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    public override void _Process(double delta)
    {
        if (!_isCrafting)
            return;

        float craftSpeedMult = GetPlayerCraftSpeedMultiplier();
        _craftProgress += (float)delta * craftSpeedMult;
        EmitSignal(SignalName.CraftProgressUpdated, CraftProgress);

        if (_craftProgress >= _craftDuration)
            CompleteCraft();
    }

    private float GetPlayerCraftSpeedMultiplier()
    {
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Player player)
            return player.CraftSpeedMultiplier;
        return 1f;
    }

    public void SetInventory(Inventory inventory)
    {
        _inventory = inventory;
    }

    public bool CanCraft(string recipeId)
    {
        if (_inventory == null)
            return false;

        RecipeData recipe = RecipeDataLoader.Get(recipeId);
        if (recipe == null)
            return false;

        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            if (!_inventory.Has(ingredient.Resource, ingredient.Amount))
                return false;
        }

        return true;
    }

    public bool IsPlayerNearFoyer()
    {
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is not Node2D player)
            return false;

        Node2D foyer = GetNodeOrNull<Node2D>("/root/Main/Foyer");
        if (foyer == null)
            return false;

        float dist = player.GlobalPosition.DistanceTo(foyer.GlobalPosition);
        return dist <= World.Foyer.SafeRadius;
    }

    public bool StartCraft(string recipeId)
    {
        if (_isCrafting)
            return false;

        if (!CanCraft(recipeId))
            return false;

        if (!IsPlayerNearFoyer())
            return false;

        RecipeData recipe = RecipeDataLoader.Get(recipeId);

        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            _inventory.Remove(ingredient.Resource, ingredient.Amount);
        }

        bool isInstantWall = recipe.Result.Type == "wall";
        if (isInstantWall)
        {
            _eventBus.EmitSignal(EventBus.SignalName.CraftStarted, recipeId);
            _eventBus.EmitSignal(EventBus.SignalName.CraftCompleted, recipeId);
            return true;
        }

        _currentRecipeId = recipeId;
        _craftDuration = recipe.BuildTime;
        _craftProgress = 0f;
        _isCrafting = true;

        _eventBus.EmitSignal(EventBus.SignalName.CraftStarted, recipeId);
        return true;
    }

    public void CancelCraft()
    {
        if (!_isCrafting)
            return;

        RecipeData recipe = RecipeDataLoader.Get(_currentRecipeId);
        if (recipe != null)
        {
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                _inventory.Add(ingredient.Resource, ingredient.Amount);
            }
        }

        _isCrafting = false;
        _currentRecipeId = null;
        _craftProgress = 0f;
    }

    private void CompleteCraft()
    {
        string recipeId = _currentRecipeId;
        _isCrafting = false;
        _currentRecipeId = null;
        _craftProgress = 0f;

        RecipeData recipe = RecipeDataLoader.Get(recipeId);
        if (recipe != null && recipe.Result.Type == "consumable")
        {
            ApplyConsumable(recipe);
        }
        else
        {
            _eventBus.EmitSignal(EventBus.SignalName.CraftCompleted, recipeId);
        }

        GD.Print($"[CraftManager] Craft completed: {recipeId}");
    }

    private void ApplyConsumable(RecipeData recipe)
    {
        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is not Player player)
            return;

        if (recipe.Result.Stats.TryGetValue("heal", out float healAmount))
        {
            player.Heal(healAmount);
        }
    }
}
