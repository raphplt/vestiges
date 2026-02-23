using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Scène Hub entre les runs.
/// Placeholder visuel (fond bleu-noir) avec UI fonctionnelle :
/// - Miroirs : sélection de personnage
/// - Chroniques : historique des runs
/// - Établi : placeholder (Lot 4.3)
/// - Le Vide : lancer une run
/// </summary>
public partial class HubScreen : Control
{
    private string _selectedCharacterId;
    private Button _enterVoidButton;
    private Label _selectedCharLabel;
    private VBoxContainer _characterCards;
    private VBoxContainer _chroniquesContent;
    private Dictionary<string, PanelContainer> _cardsByCharacterId = new();

    public override void _Ready()
    {
        CharacterDataLoader.Load();
        PerkDataLoader.Load();
        RunHistoryManager.Load();

        GameManager gm = GetNode<GameManager>("/root/GameManager");
        _selectedCharacterId = gm.SelectedCharacterId;

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildUI();

        if (!string.IsNullOrEmpty(_selectedCharacterId))
            HighlightSelectedCharacter(_selectedCharacterId);

        UpdateVoidButton();
        UpdateChroniques();

        if (gm.LastRunData != null)
            gm.LastRunData = null;
    }

    private void BuildUI()
    {
        // Background
        ColorRect bg = new();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.Color = new Color(0.02f, 0.03f, 0.08f, 1f);
        AddChild(bg);

        // Main vertical layout
        VBoxContainer mainVBox = new();
        mainVBox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 0);
        AddChild(mainVBox);

        // Top: title area
        MarginContainer titleMargin = new();
        titleMargin.AddThemeConstantOverride("margin_top", 30);
        titleMargin.AddThemeConstantOverride("margin_bottom", 10);
        titleMargin.AddThemeConstantOverride("margin_left", 0);
        titleMargin.AddThemeConstantOverride("margin_right", 0);
        mainVBox.AddChild(titleMargin);

        VBoxContainer titleBox = new();
        titleBox.AddThemeConstantOverride("separation", 4);
        titleMargin.AddChild(titleBox);

        Label title = new();
        title.Text = "VESTIGES";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 36);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.82f, 0.55f));
        titleBox.AddChild(title);

        Label subtitle = new();
        subtitle.Text = "Le Hub";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeFontSizeOverride("font_size", 14);
        subtitle.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        titleBox.AddChild(subtitle);

        // Center: 3-column layout
        HBoxContainer columns = new();
        columns.SizeFlagsVertical = SizeFlags.ExpandFill;
        columns.AddThemeConstantOverride("separation", 20);
        columns.Alignment = BoxContainer.AlignmentMode.Center;
        mainVBox.AddChild(columns);

        // Left column: Chroniques
        columns.AddChild(BuildChroniquesPanel());

        // Center column: Miroirs (character select)
        columns.AddChild(BuildMiroirsPanel());

        // Right column: Etabli (placeholder)
        columns.AddChild(BuildEtabliPanel());

        // Bottom: selected character + void button
        MarginContainer bottomMargin = new();
        bottomMargin.AddThemeConstantOverride("margin_top", 10);
        bottomMargin.AddThemeConstantOverride("margin_bottom", 30);
        bottomMargin.AddThemeConstantOverride("margin_left", 0);
        bottomMargin.AddThemeConstantOverride("margin_right", 0);
        mainVBox.AddChild(bottomMargin);

        VBoxContainer bottomBox = new();
        bottomBox.AddThemeConstantOverride("separation", 10);
        bottomMargin.AddChild(bottomBox);

        _selectedCharLabel = new Label();
        _selectedCharLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _selectedCharLabel.AddThemeFontSizeOverride("font_size", 16);
        _selectedCharLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        bottomBox.AddChild(_selectedCharLabel);

        CenterContainer buttonCenter = new();
        bottomBox.AddChild(buttonCenter);

        _enterVoidButton = new Button();
        _enterVoidButton.Text = "Entrer dans le Vide";
        _enterVoidButton.CustomMinimumSize = new Vector2(250, 50);
        _enterVoidButton.AddThemeFontSizeOverride("font_size", 18);
        _enterVoidButton.Pressed += OnEnterVoidPressed;
        buttonCenter.AddChild(_enterVoidButton);
    }

    // --- Miroirs (Character Selection) ---

    private PanelContainer BuildMiroirsPanel()
    {
        PanelContainer panel = CreateSectionPanel("MIROIRS", 380);

        VBoxContainer content = GetSectionContent(panel);

        Label desc = new();
        desc.Text = "Choisir un personnage";
        desc.HorizontalAlignment = HorizontalAlignment.Center;
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        content.AddChild(desc);

        _characterCards = new VBoxContainer();
        _characterCards.AddThemeConstantOverride("separation", 8);
        content.AddChild(_characterCards);

        List<CharacterData> characters = CharacterDataLoader.GetAll();
        foreach (CharacterData character in characters)
            CreateCharacterCard(character);

        return panel;
    }

    private void CreateCharacterCard(CharacterData character)
    {
        PanelContainer card = new();
        card.CustomMinimumSize = new Vector2(340, 0);

        StyleBoxFlat cardStyle = new();
        cardStyle.BgColor = new Color(0.06f, 0.06f, 0.1f, 0.9f);
        cardStyle.BorderColor = new Color(0.2f, 0.2f, 0.25f, 0.6f);
        cardStyle.BorderWidthBottom = 2;
        cardStyle.BorderWidthTop = 2;
        cardStyle.BorderWidthLeft = 2;
        cardStyle.BorderWidthRight = 2;
        cardStyle.CornerRadiusBottomLeft = 6;
        cardStyle.CornerRadiusBottomRight = 6;
        cardStyle.CornerRadiusTopLeft = 6;
        cardStyle.CornerRadiusTopRight = 6;
        cardStyle.ContentMarginLeft = 14;
        cardStyle.ContentMarginRight = 14;
        cardStyle.ContentMarginTop = 10;
        cardStyle.ContentMarginBottom = 10;
        card.AddThemeStyleboxOverride("panel", cardStyle);

        VBoxContainer cardContent = new();
        cardContent.AddThemeConstantOverride("separation", 4);
        card.AddChild(cardContent);

        // Header: color + name
        HBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 8);
        cardContent.AddChild(header);

        ColorRect colorIndicator = new();
        colorIndicator.Color = character.VisualColor;
        colorIndicator.CustomMinimumSize = new Vector2(8, 8);
        header.AddChild(colorIndicator);

        Label nameLabel = new();
        nameLabel.Text = character.Name;
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", character.VisualColor);
        header.AddChild(nameLabel);

        // Description
        Label descLabel = new();
        descLabel.Text = character.Description;
        descLabel.AddThemeFontSizeOverride("font_size", 12);
        descLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        cardContent.AddChild(descLabel);

        // Stats
        CharacterStats stats = character.BaseStats;
        string statsText = $"PV:{stats.MaxHp}  ATK:{stats.AttackDamage}  VIT:{stats.Speed}  PORT:{stats.AttackRange}";
        Label statsLabel = new();
        statsLabel.Text = statsText;
        statsLabel.AddThemeFontSizeOverride("font_size", 11);
        statsLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
        cardContent.AddChild(statsLabel);

        // Passive perk
        PerkData passive = PerkDataLoader.Get(character.PassivePerk);
        if (passive != null)
        {
            Label passiveLabel = new();
            passiveLabel.Text = $"Passif : {passive.Name}";
            passiveLabel.AddThemeFontSizeOverride("font_size", 11);
            passiveLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.65f, 0.35f));
            cardContent.AddChild(passiveLabel);
        }

        // Make the card clickable
        string capturedId = character.Id;
        card.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
                OnCharacterClicked(capturedId);
        };

        _cardsByCharacterId[character.Id] = card;
        _characterCards.AddChild(card);
    }

    private void OnCharacterClicked(string characterId)
    {
        _selectedCharacterId = characterId;

        GameManager gm = GetNode<GameManager>("/root/GameManager");
        gm.SelectedCharacterId = characterId;

        HighlightSelectedCharacter(characterId);
        UpdateVoidButton();
    }

    private void HighlightSelectedCharacter(string selectedId)
    {
        Color goldBorder = new(0.9f, 0.8f, 0.4f, 0.9f);
        Color defaultBorder = new(0.2f, 0.2f, 0.25f, 0.6f);

        foreach (KeyValuePair<string, PanelContainer> pair in _cardsByCharacterId)
        {
            StyleBoxFlat style = pair.Value.GetThemeStylebox("panel") as StyleBoxFlat;
            if (style == null) continue;

            StyleBoxFlat updated = (StyleBoxFlat)style.Duplicate();
            bool isSelected = pair.Key == selectedId;
            updated.BorderColor = isSelected ? goldBorder : defaultBorder;
            updated.BgColor = isSelected
                ? new Color(0.1f, 0.09f, 0.15f, 0.95f)
                : new Color(0.06f, 0.06f, 0.1f, 0.9f);
            pair.Value.AddThemeStyleboxOverride("panel", updated);
        }

        CharacterData data = CharacterDataLoader.Get(selectedId);
        if (data != null)
            _selectedCharLabel.Text = $"Personnage : {data.Name}";
    }

    private void UpdateVoidButton()
    {
        bool hasSelection = !string.IsNullOrEmpty(_selectedCharacterId);
        _enterVoidButton.Disabled = !hasSelection;
        _enterVoidButton.Modulate = hasSelection
            ? new Color(1f, 1f, 1f, 1f)
            : new Color(0.5f, 0.5f, 0.5f, 0.6f);
    }

    private void OnEnterVoidPressed()
    {
        if (string.IsNullOrEmpty(_selectedCharacterId))
            return;

        GameManager gm = GetNode<GameManager>("/root/GameManager");
        gm.SelectedCharacterId = _selectedCharacterId;
        gm.ChangeState(GameManager.GameState.Run);

        GD.Print($"[Hub] Entering the Void with {_selectedCharacterId}");
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }

    // --- Chroniques (Run History) ---

    private PanelContainer BuildChroniquesPanel()
    {
        PanelContainer panel = CreateSectionPanel("CHRONIQUES", 280);

        _chroniquesContent = GetSectionContent(panel);

        return panel;
    }

    private void UpdateChroniques()
    {
        // Clear existing content (keep title)
        foreach (Node child in _chroniquesContent.GetChildren())
        {
            if (child is Label label && label.HasMeta("section_title"))
                continue;
            child.QueueFree();
        }

        // Best score
        int bestScore = RunHistoryManager.GetBestScore();
        int maxNights = RunHistoryManager.GetMaxNights();

        Label recordLabel = new();
        recordLabel.Text = bestScore > 0
            ? $"Record : {bestScore}\nNuits max : {maxNights}"
            : "Aucune run enregistrée";
        recordLabel.HorizontalAlignment = HorizontalAlignment.Center;
        recordLabel.AddThemeFontSizeOverride("font_size", 14);
        recordLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.75f, 0.4f));
        _chroniquesContent.AddChild(recordLabel);

        // Separator
        HSeparator sep = new();
        _chroniquesContent.AddChild(sep);

        // Recent runs
        List<RunRecord> history = RunHistoryManager.GetHistory();
        if (history.Count == 0)
        {
            Label emptyLabel = new();
            emptyLabel.Text = "Pas encore d'historique.";
            emptyLabel.AddThemeFontSizeOverride("font_size", 12);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
            _chroniquesContent.AddChild(emptyLabel);
            return;
        }

        Label histTitle = new();
        histTitle.Text = "Dernières runs";
        histTitle.AddThemeFontSizeOverride("font_size", 12);
        histTitle.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.6f));
        _chroniquesContent.AddChild(histTitle);

        int shown = System.Math.Min(history.Count, 8);
        for (int i = 0; i < shown; i++)
        {
            RunRecord run = history[i];
            Label runLabel = new();
            string nights = run.NightsSurvived > 0
                ? $"{run.NightsSurvived}N"
                : "0N";
            runLabel.Text = $"{run.CharacterName} — {run.Score} pts — {nights}";
            runLabel.AddThemeFontSizeOverride("font_size", 11);
            runLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.6f));
            _chroniquesContent.AddChild(runLabel);
        }
    }

    // --- Établi (Placeholder) ---

    private PanelContainer BuildEtabliPanel()
    {
        PanelContainer panel = CreateSectionPanel("ÉTABLI", 220);

        VBoxContainer content = GetSectionContent(panel);

        Label placeholder = new();
        placeholder.Text = "Kits de départ\n\nBientôt disponible\n(Lot 4.3)";
        placeholder.HorizontalAlignment = HorizontalAlignment.Center;
        placeholder.AddThemeFontSizeOverride("font_size", 13);
        placeholder.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.35f));
        content.AddChild(placeholder);

        panel.Modulate = new Color(0.6f, 0.6f, 0.6f, 0.5f);

        return panel;
    }

    // --- Helpers ---

    private PanelContainer CreateSectionPanel(string title, float minWidth)
    {
        PanelContainer panel = new();
        panel.CustomMinimumSize = new Vector2(minWidth, 0);
        panel.SizeFlagsVertical = SizeFlags.ShrinkCenter;

        StyleBoxFlat style = new();
        style.BgColor = new Color(0.04f, 0.04f, 0.07f, 0.8f);
        style.BorderColor = new Color(0.15f, 0.15f, 0.2f, 0.5f);
        style.BorderWidthBottom = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthRight = 1;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 14;
        style.ContentMarginBottom = 14;
        panel.AddThemeStyleboxOverride("panel", style);

        VBoxContainer vbox = new();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        Label titleLabel = new();
        titleLabel.Text = title;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.7f, 0.55f));
        titleLabel.SetMeta("section_title", true);
        vbox.AddChild(titleLabel);

        return panel;
    }

    private VBoxContainer GetSectionContent(PanelContainer panel)
    {
        return panel.GetChild<VBoxContainer>(0);
    }
}
