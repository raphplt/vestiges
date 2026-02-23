using System.Collections.Generic;
using Godot;
using Vestiges.Base;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Panneau latéral de craft. Toggle avec la touche C.
/// Affiche les recettes disponibles, permet de lancer un craft.
/// Ne met PAS le jeu en pause.
/// </summary>
public partial class CraftPanel : CanvasLayer
{
    private PanelContainer _panel;
    private VBoxContainer _recipeList;
    private Label _titleLabel;
    private Label _statusLabel;
    private ProgressBar _craftBar;
    private CraftManager _craftManager;
    private Inventory _inventory;
    private StructureManager _structureManager;
    private EventBus _eventBus;
    private bool _isOpen;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.InventoryChanged += OnInventoryChanged;
        _eventBus.CraftCompleted += OnCraftCompleted;

        RecipeDataLoader.Load();
        CreatePanel();
        _panel.Visible = false;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.InventoryChanged -= OnInventoryChanged;
            _eventBus.CraftCompleted -= OnCraftCompleted;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("craft_menu"))
        {
            TogglePanel();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (_isOpen && _craftManager != null && _craftManager.IsCrafting)
        {
            _craftBar.Value = _craftManager.CraftProgress;
            _craftBar.Visible = true;
            RecipeData recipe = RecipeDataLoader.Get(_craftManager.CurrentRecipeId);
            _statusLabel.Text = $"Fabrication : {recipe?.Name ?? ""}...";
        }
        else if (_isOpen)
        {
            _craftBar.Visible = false;
        }
    }

    public void SetCraftManager(CraftManager manager)
    {
        _craftManager = manager;
    }

    public void SetInventory(Inventory inventory)
    {
        _inventory = inventory;
    }

    public void SetStructureManager(StructureManager manager)
    {
        _structureManager = manager;
    }

    private void TogglePanel()
    {
        _isOpen = !_isOpen;
        _panel.Visible = _isOpen;

        if (_isOpen)
            RefreshRecipes();
    }

    private void CreatePanel()
    {
        _panel = new PanelContainer();
        _panel.AnchorLeft = 0f;
        _panel.AnchorRight = 0f;
        _panel.AnchorTop = 0.1f;
        _panel.AnchorBottom = 0.9f;
        _panel.OffsetLeft = 10;
        _panel.OffsetRight = 260;

        StyleBoxFlat style = new();
        style.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 12;
        style.ContentMarginBottom = 12;
        _panel.AddThemeStyleboxOverride("panel", style);

        VBoxContainer mainVbox = new();
        mainVbox.AddThemeConstantOverride("separation", 8);
        _panel.AddChild(mainVbox);

        _titleLabel = new Label();
        _titleLabel.Text = "CRAFT [C]";
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.75f, 0.2f));
        mainVbox.AddChild(_titleLabel);

        _statusLabel = new Label();
        _statusLabel.Text = "";
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        mainVbox.AddChild(_statusLabel);

        _craftBar = new ProgressBar();
        _craftBar.CustomMinimumSize = new Vector2(0, 8);
        _craftBar.ShowPercentage = false;
        _craftBar.MinValue = 0;
        _craftBar.MaxValue = 1;
        _craftBar.Visible = false;

        StyleBoxFlat fillStyle = new();
        fillStyle.BgColor = new Color(0.9f, 0.75f, 0.2f);
        fillStyle.CornerRadiusBottomLeft = 3;
        fillStyle.CornerRadiusBottomRight = 3;
        fillStyle.CornerRadiusTopLeft = 3;
        fillStyle.CornerRadiusTopRight = 3;
        _craftBar.AddThemeStyleboxOverride("fill", fillStyle);

        StyleBoxFlat bgStyle = new();
        bgStyle.BgColor = new Color(0.15f, 0.15f, 0.15f);
        bgStyle.CornerRadiusBottomLeft = 3;
        bgStyle.CornerRadiusBottomRight = 3;
        bgStyle.CornerRadiusTopLeft = 3;
        bgStyle.CornerRadiusTopRight = 3;
        _craftBar.AddThemeStyleboxOverride("background", bgStyle);
        mainVbox.AddChild(_craftBar);

        ScrollContainer scroll = new();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        mainVbox.AddChild(scroll);

        _recipeList = new VBoxContainer();
        _recipeList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_recipeList);

        AddChild(_panel);
    }

    private void RefreshRecipes()
    {
        foreach (Node child in _recipeList.GetChildren())
            child.QueueFree();

        List<RecipeData> recipes = RecipeDataLoader.GetAll();
        bool nearFoyer = _craftManager?.IsPlayerNearFoyer() ?? false;

        _statusLabel.Text = nearFoyer ? "" : "Trop loin du Foyer pour fabriquer";

        string lastCategory = "";
        foreach (RecipeData recipe in recipes)
        {
            string category = GetCategoryDisplayName(recipe.Category);
            if (category != lastCategory)
            {
                _recipeList.AddChild(CreateCategoryHeader(category));
                lastCategory = category;
            }

            PanelContainer entry = CreateRecipeEntry(recipe, nearFoyer);
            _recipeList.AddChild(entry);
        }
    }

    private static Control CreateCategoryHeader(string category)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 6);

        HSeparator sepLeft = new();
        sepLeft.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(sepLeft);

        Label label = new();
        label.Text = category;
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", new Color(0.7f, 0.6f, 0.3f));
        label.HorizontalAlignment = HorizontalAlignment.Center;
        row.AddChild(label);

        HSeparator sepRight = new();
        sepRight.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(sepRight);

        return row;
    }

    private PanelContainer CreateRecipeEntry(RecipeData recipe, bool nearFoyer)
    {
        PanelContainer entry = new();

        bool canAfford = _craftManager?.CanCraft(recipe.Id) ?? false;
        bool isStructure = recipe.Result.Type is "wall" or "trap" or "turret" or "light";
        bool capReached = isStructure && _structureManager != null && !_structureManager.CanPlaceType(recipe.Result.Type);
        bool canCraft = canAfford && nearFoyer && !capReached && !(_craftManager?.IsCrafting ?? false);

        StyleBoxFlat style = new();
        style.BgColor = canCraft ? new Color(0.12f, 0.15f, 0.12f, 0.8f) : new Color(0.12f, 0.1f, 0.1f, 0.6f);
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 6;
        style.ContentMarginBottom = 6;
        entry.AddThemeStyleboxOverride("panel", style);

        VBoxContainer vbox = new();
        vbox.AddThemeConstantOverride("separation", 2);
        entry.AddChild(vbox);

        // Recipe name + structure count
        HBoxContainer nameRow = new();
        vbox.AddChild(nameRow);

        Label nameLabel = new();
        nameLabel.Text = recipe.Name;
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color", canCraft ? Colors.White : new Color(0.5f, 0.5f, 0.5f));
        nameRow.AddChild(nameLabel);

        if (isStructure && _structureManager != null)
        {
            int count = _structureManager.GetCountForType(recipe.Result.Type);
            int max = _structureManager.GetMaxForType(recipe.Result.Type);
            Label countLabel = new();
            countLabel.Text = $"{count}/{max}";
            countLabel.AddThemeFontSizeOverride("font_size", 12);
            countLabel.AddThemeColorOverride("font_color",
                capReached ? new Color(0.8f, 0.4f, 0.2f) : new Color(0.5f, 0.5f, 0.5f));
            nameRow.AddChild(countLabel);
        }

        // Result stats description
        string statsText = FormatResultStats(recipe);
        if (statsText.Length > 0)
        {
            Label statsLabel = new();
            statsLabel.Text = statsText;
            statsLabel.AddThemeFontSizeOverride("font_size", 11);
            statsLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.7f, 0.8f));
            vbox.AddChild(statsLabel);
        }

        // Ingredients
        HBoxContainer ingRow = new();
        ingRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(ingRow);

        foreach (RecipeIngredient ing in recipe.Ingredients)
        {
            int have = _inventory?.GetAmount(ing.Resource) ?? 0;
            bool enough = have >= ing.Amount;

            Label ingLabel = new();
            ingLabel.Text = $"{GetResourceSymbol(ing.Resource)} {have}/{ing.Amount}";
            ingLabel.AddThemeFontSizeOverride("font_size", 12);
            ingLabel.AddThemeColorOverride("font_color",
                enough ? new Color(0.5f, 0.8f, 0.5f) : new Color(0.8f, 0.3f, 0.3f));
            ingRow.AddChild(ingLabel);
        }

        // Action area
        if (capReached)
        {
            Label capLabel = new();
            capLabel.Text = "Limite atteinte";
            capLabel.AddThemeFontSizeOverride("font_size", 11);
            capLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.4f, 0.2f));
            vbox.AddChild(capLabel);
        }
        else if (canCraft)
        {
            Button craftButton = new();
            craftButton.Text = $"Fabriquer ({recipe.BuildTime:0.#}s)";
            craftButton.CustomMinimumSize = new Vector2(0, 28);
            string recipeId = recipe.Id;
            craftButton.Pressed += () => OnCraftPressed(recipeId);
            vbox.AddChild(craftButton);
        }

        return entry;
    }

    private static string FormatResultStats(RecipeData recipe)
    {
        List<string> parts = new();
        Dictionary<string, float> stats = recipe.Result.Stats;

        if (stats.TryGetValue("hp", out float hp))
            parts.Add($"{hp} PV");
        if (stats.TryGetValue("damage", out float dmg))
            parts.Add($"{dmg} dégâts");
        if (stats.TryGetValue("attack_speed", out float atkSpd))
            parts.Add($"{atkSpd:0.#}/s");
        if (stats.TryGetValue("range", out float range))
            parts.Add($"portée {range}");
        if (stats.TryGetValue("uses", out float uses))
            parts.Add($"{uses} utilisations");
        if (stats.TryGetValue("radius", out float radius))
            parts.Add($"rayon {radius}");
        if (stats.TryGetValue("duration", out float dur))
            parts.Add($"{dur}s");
        if (stats.TryGetValue("heal", out float heal))
            parts.Add($"+{heal} PV");

        return string.Join(" · ", parts);
    }

    private void OnCraftPressed(string recipeId)
    {
        if (_craftManager == null)
            return;

        if (_craftManager.StartCraft(recipeId))
        {
            RefreshRecipes();
        }
    }

    private void OnInventoryChanged(string _resourceId, int _newAmount)
    {
        if (_isOpen)
            RefreshRecipes();
    }

    private void OnCraftCompleted(string _recipeId)
    {
        RecipeData recipe = RecipeDataLoader.Get(_recipeId);
        bool isStructure = recipe != null && recipe.Result.Type is "wall" or "trap" or "turret" or "light";
        _statusLabel.Text = isStructure
            ? "Fabrication terminée : place puis maintiens clic pour construire en chaine"
            : "Fabrication terminée !";

        if (_isOpen)
            RefreshRecipes();
    }

    private static string GetResourceSymbol(string id)
    {
        return id switch
        {
            "wood" => "Bois",
            "stone" => "Pierre",
            "metal" => "Métal",
            _ => id
        };
    }

    private static string GetCategoryDisplayName(string category)
    {
        return category switch
        {
            "defense" => "Défense",
            "trap" => "Pièges",
            "utility" => "Utilitaire",
            "consumable" => "Consommables",
            _ => category
        };
    }

}
