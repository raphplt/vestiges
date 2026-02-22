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

        foreach (RecipeData recipe in recipes)
        {
            PanelContainer entry = CreateRecipeEntry(recipe, nearFoyer);
            _recipeList.AddChild(entry);
        }

        _statusLabel.Text = nearFoyer ? "" : "Trop loin du Foyer";
    }

    private PanelContainer CreateRecipeEntry(RecipeData recipe, bool nearFoyer)
    {
        PanelContainer entry = new();

        bool canAfford = _craftManager?.CanCraft(recipe.Id) ?? false;
        bool canCraft = canAfford && nearFoyer && !(_craftManager?.IsCrafting ?? false);

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

        Label nameLabel = new();
        nameLabel.Text = recipe.Name;
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color", canCraft ? Colors.White : new Color(0.5f, 0.5f, 0.5f));
        vbox.AddChild(nameLabel);

        string ingredientText = "";
        foreach (RecipeIngredient ing in recipe.Ingredients)
        {
            int have = _inventory?.GetAmount(ing.Resource) ?? 0;
            Color color = have >= ing.Amount ? new Color(0.5f, 0.8f, 0.5f) : new Color(0.8f, 0.3f, 0.3f);
            string colorHex = color.ToHtml(false);
            ingredientText += $"[color=#{colorHex}]{GetResourceName(ing.Resource)}: {have}/{ing.Amount}[/color]  ";
        }

        RichTextLabel ingLabel = new();
        ingLabel.BbcodeEnabled = true;
        ingLabel.Text = ingredientText;
        ingLabel.FitContent = true;
        ingLabel.AddThemeFontSizeOverride("normal_font_size", 11);
        vbox.AddChild(ingLabel);

        if (canCraft)
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
        _statusLabel.Text = "Fabrication terminée !";

        if (_isOpen)
            RefreshRecipes();
    }

    private static string GetResourceName(string id)
    {
        return id switch
        {
            "wood" => "Bois",
            "stone" => "Pierre",
            "metal" => "Métal",
            _ => id
        };
    }
}
