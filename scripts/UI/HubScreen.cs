using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Scène Hub entre les runs.
/// - Miroirs : sélection de personnage (verrouillage selon MetaSaveManager)
/// - Chroniques : historique des runs
/// - Établi : achat et sélection de kits de départ
/// - Le Vide : lancer une run
/// </summary>
public partial class HubScreen : Control
{
    private string _selectedCharacterId;
    private Button _enterVoidButton;
    private Label _selectedCharLabel;
    private Label _vestigesLabel;
    private VBoxContainer _characterCards;
    private VBoxContainer _chroniquesContent;
    private VBoxContainer _etabliContent;
    private Dictionary<string, PanelContainer> _cardsByCharacterId = new();

    public override void _Ready()
    {
        CharacterDataLoader.Load();
        WeaponDataLoader.Load();
        PerkDataLoader.Load();
        RunHistoryManager.Load();
        MetaSaveManager.Load();
        StartingKitDataLoader.Load();

        GameManager gm = GetNode<GameManager>("/root/GameManager");
        _selectedCharacterId = gm.SelectedCharacterId;

        // If the selected character is locked, clear selection
        if (!string.IsNullOrEmpty(_selectedCharacterId) && !MetaSaveManager.IsCharacterUnlocked(_selectedCharacterId))
            _selectedCharacterId = null;

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildUI();

        EnsureDefaultCharacterSelection(gm);

        if (!string.IsNullOrEmpty(_selectedCharacterId))
            HighlightSelectedCharacter(_selectedCharacterId);

        UpdateVoidButton();
        UpdateChroniques();
        UpdateVestigesDisplay();

        if (gm.LastRunData != null)
            gm.LastRunData = null;
    }

    private void EnsureDefaultCharacterSelection(GameManager gm)
    {
        if (!string.IsNullOrEmpty(_selectedCharacterId))
            return;

        foreach (CharacterData character in CharacterDataLoader.GetAll())
        {
            if (!MetaSaveManager.IsCharacterUnlocked(character.Id))
                continue;

            _selectedCharacterId = character.Id;
            gm.SelectedCharacterId = character.Id;
            GD.Print($"[Hub] Default character selected: {character.Id}");
            return;
        }

        GD.PushWarning("[Hub] No unlocked character available for default selection.");
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

        Label title = new()
        {
            Text = "VESTIGES",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 36);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.82f, 0.55f));
        titleBox.AddChild(title);

        // Subtitle + Vestiges on same row
        HBoxContainer subtitleRow = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        subtitleRow.AddThemeConstantOverride("separation", 20);
        titleBox.AddChild(subtitleRow);

        Label subtitle = new()
        {
            Text = "Le Hub"
        };
        subtitle.AddThemeFontSizeOverride("font_size", 14);
        subtitle.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        subtitleRow.AddChild(subtitle);

        _vestigesLabel = new Label();
        _vestigesLabel.AddThemeFontSizeOverride("font_size", 14);
        _vestigesLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.75f, 0.4f));
        subtitleRow.AddChild(_vestigesLabel);

        // Center: 3-column layout
        HBoxContainer columns = new()
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        columns.AddThemeConstantOverride("separation", 20);
        columns.Alignment = BoxContainer.AlignmentMode.Center;
        mainVBox.AddChild(columns);

        // Left column: Chroniques
        columns.AddChild(BuildChroniquesPanel());

        // Center column: Miroirs (character select)
        columns.AddChild(BuildMiroirsPanel());

        // Right column: Etabli
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

        _selectedCharLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _selectedCharLabel.AddThemeFontSizeOverride("font_size", 16);
        _selectedCharLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        bottomBox.AddChild(_selectedCharLabel);

        CenterContainer buttonCenter = new();
        bottomBox.AddChild(buttonCenter);

        _enterVoidButton = new Button
        {
            Text = "Entrer dans le Vide",
            CustomMinimumSize = new Vector2(250, 50)
        };
        _enterVoidButton.AddThemeFontSizeOverride("font_size", 18);
        _enterVoidButton.Pressed += OnEnterVoidPressed;
        buttonCenter.AddChild(_enterVoidButton);
    }

    private void UpdateVestigesDisplay()
    {
        int vestiges = MetaSaveManager.GetVestiges();
        _vestigesLabel.Text = $"Vestiges : {vestiges}";
    }

    // --- Miroirs (Character Selection) ---

    private PanelContainer BuildMiroirsPanel()
    {
        PanelContainer panel = CreateSectionPanel("MIROIRS", 380);

        VBoxContainer content = GetSectionContent(panel);

        Label desc = new()
        {
            Text = "Choisir un personnage",
            HorizontalAlignment = HorizontalAlignment.Center
        };
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
        bool isUnlocked = MetaSaveManager.IsCharacterUnlocked(character.Id);

        PanelContainer card = new()
        {
            CustomMinimumSize = new Vector2(340, 0)
        };

        StyleBoxFlat cardStyle = new()
        {
            BgColor = isUnlocked
                ? new Color(0.06f, 0.06f, 0.1f, 0.9f)
                : new Color(0.04f, 0.04f, 0.06f, 0.7f),
            BorderColor = isUnlocked
                ? new Color(0.2f, 0.2f, 0.25f, 0.6f)
                : new Color(0.15f, 0.15f, 0.18f, 0.4f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);

        VBoxContainer cardContent = new();
        cardContent.AddThemeConstantOverride("separation", 4);
        card.AddChild(cardContent);

        Color textColor = isUnlocked ? character.VisualColor : new Color(0.35f, 0.35f, 0.4f);

        // Header: color + name
        HBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 8);
        cardContent.AddChild(header);

        ColorRect colorIndicator = new()
        {
            Color = isUnlocked ? character.VisualColor : new Color(0.25f, 0.25f, 0.3f),
            CustomMinimumSize = new Vector2(8, 8)
        };
        header.AddChild(colorIndicator);

        Label nameLabel = new()
        {
            Text = character.Name
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", textColor);
        header.AddChild(nameLabel);

        if (isUnlocked)
        {
            // Description
            Label descLabel = new()
            {
                Text = character.Description
            };
            descLabel.AddThemeFontSizeOverride("font_size", 12);
            descLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
            cardContent.AddChild(descLabel);

            // Stats
            CharacterStats stats = character.BaseStats;
            string statsText = $"PV:{stats.MaxHp}  ATK:{stats.AttackDamage}  VIT:{stats.Speed}  PORT:{stats.AttackRange}";
            Label statsLabel = new()
            {
                Text = statsText
            };
            statsLabel.AddThemeFontSizeOverride("font_size", 11);
            statsLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
            cardContent.AddChild(statsLabel);

            // Passive perk
            PerkData passive = PerkDataLoader.Get(character.PassivePerk);
            if (passive != null)
            {
                Label passiveLabel = new()
                {
                    Text = $"Passif : {passive.Name}"
                };
                passiveLabel.AddThemeFontSizeOverride("font_size", 11);
                passiveLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.65f, 0.35f));
                cardContent.AddChild(passiveLabel);
            }

            WeaponData startingWeapon = WeaponDataLoader.Get(character.StartingWeaponId)
                ?? WeaponDataLoader.GetDefaultForCharacter(character.Id);
            if (startingWeapon != null)
            {
                Label weaponLabel = new()
                {
                    Text = $"Arme de départ : {startingWeapon.Name}"
                };
                weaponLabel.AddThemeFontSizeOverride("font_size", 11);
                weaponLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.75f, 0.85f));
                cardContent.AddChild(weaponLabel);
            }

            // Make unlocked card clickable
            string capturedId = character.Id;
            card.GuiInput += (InputEvent @event) =>
            {
                if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
                    OnCharacterClicked(capturedId);
            };
        }
        else
        {
            // Locked: show unlock condition
            Label conditionLabel = new()
            {
                Text = GetUnlockConditionText(character.UnlockCondition)
            };
            conditionLabel.AddThemeFontSizeOverride("font_size", 12);
            conditionLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.35f, 0.3f));
            cardContent.AddChild(conditionLabel);
        }

        _cardsByCharacterId[character.Id] = card;
        _characterCards.AddChild(card);
    }

    private static string GetUnlockConditionText(string condition)
    {
        return condition switch
        {
            "survive_3_nights" => "Survivre 3 nuits",
            "kill_200_in_run" => "200 kills en une run",
            _ => "???"
        };
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
        Color lockedBorder = new(0.15f, 0.15f, 0.18f, 0.4f);

        foreach (KeyValuePair<string, PanelContainer> pair in _cardsByCharacterId)
        {
            bool isUnlocked = MetaSaveManager.IsCharacterUnlocked(pair.Key);
            if (!isUnlocked)
                continue;

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

        Label recordLabel = new()
        {
            Text = bestScore > 0
                ? $"Record : {bestScore}\nNuits max : {maxNights}"
                : "Aucune run enregistrée",
            HorizontalAlignment = HorizontalAlignment.Center
        };
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
            Label emptyLabel = new()
            {
                Text = "Pas encore d'historique."
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 12);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
            _chroniquesContent.AddChild(emptyLabel);
            return;
        }

        Label histTitle = new()
        {
            Text = "Dernières runs"
        };
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

    // --- Établi (Starting Kits) ---

    private PanelContainer BuildEtabliPanel()
    {
        PanelContainer panel = CreateSectionPanel("ÉTABLI", 300);

        _etabliContent = GetSectionContent(panel);

        Label desc = new()
        {
            Text = "Kits de départ",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        desc.AddThemeFontSizeOverride("font_size", 12);
        desc.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        _etabliContent.AddChild(desc);

        RebuildEtabliKits();

        return panel;
    }

    private void RebuildEtabliKits()
    {
        // Remove all kit cards (keep section title + description label)
        List<Node> toRemove = new();
        int index = 0;
        foreach (Node child in _etabliContent.GetChildren())
        {
            // Keep first two children (section title label + description label)
            if (index >= 2)
                toRemove.Add(child);
            index++;
        }
        foreach (Node node in toRemove)
            node.QueueFree();

        // Wait a frame for QueueFree to process, then add new kits
        CallDeferred(MethodName.BuildKitCards);
    }

    private void BuildKitCards()
    {
        // Remove leftover deferred-freed nodes
        while (_etabliContent.GetChildCount() > 2)
            _etabliContent.RemoveChild(_etabliContent.GetChild(_etabliContent.GetChildCount() - 1));

        HashSet<string> purchased = MetaSaveManager.GetPurchasedKits();
        string selectedKit = MetaSaveManager.GetSelectedKit();
        int vestiges = MetaSaveManager.GetVestiges();

        List<StartingKitData> kits = StartingKitDataLoader.GetAll();

        // "None" option to deselect kits
        PanelContainer noneCard = CreateKitCard("Aucun", "Pas de kit de départ", 0, true, string.IsNullOrEmpty(selectedKit));
        noneCard.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
                OnKitSelected("");
        };
        _etabliContent.AddChild(noneCard);

        foreach (StartingKitData kit in kits)
        {
            bool owned = purchased.Contains(kit.Id);
            bool selected = kit.Id == selectedKit;

            PanelContainer kitCard = CreateKitCard(kit.Name, kit.Description, kit.Cost, owned, selected);

            if (owned)
            {
                // Owned: click to select
                string capturedId = kit.Id;
                kitCard.GuiInput += (InputEvent @event) =>
                {
                    if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
                        OnKitSelected(capturedId);
                };
            }
            else
            {
                // Not owned: add buy button
                VBoxContainer content = kitCard.GetChild<VBoxContainer>(0);

                Button buyButton = new()
                {
                    Text = $"Acheter ({kit.Cost} V)",
                    CustomMinimumSize = new Vector2(0, 28),
                    Disabled = vestiges < kit.Cost
                };

                string capturedKitId = kit.Id;
                int capturedCost = kit.Cost;
                buyButton.Pressed += () => OnKitPurchased(capturedKitId, capturedCost);
                content.AddChild(buyButton);
            }

            _etabliContent.AddChild(kitCard);
        }
    }

    private PanelContainer CreateKitCard(string name, string description, int cost, bool owned, bool selected)
    {
        PanelContainer card = new()
        {
            CustomMinimumSize = new Vector2(260, 0)
        };

        StyleBoxFlat style = new();
        if (selected)
        {
            style.BgColor = new Color(0.1f, 0.09f, 0.15f, 0.95f);
            style.BorderColor = new Color(0.85f, 0.75f, 0.4f, 0.8f);
        }
        else if (owned)
        {
            style.BgColor = new Color(0.06f, 0.06f, 0.1f, 0.9f);
            style.BorderColor = new Color(0.2f, 0.2f, 0.25f, 0.6f);
        }
        else
        {
            style.BgColor = new Color(0.04f, 0.04f, 0.06f, 0.7f);
            style.BorderColor = new Color(0.15f, 0.15f, 0.18f, 0.4f);
        }
        style.BorderWidthBottom = 2;
        style.BorderWidthTop = 2;
        style.BorderWidthLeft = 2;
        style.BorderWidthRight = 2;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        card.AddThemeStyleboxOverride("panel", style);

        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 3);
        card.AddChild(content);

        // Header row: name + status
        HBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 8);
        content.AddChild(header);

        Label nameLabel = new()
        {
            Text = name,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color", owned
            ? new Color(0.8f, 0.78f, 0.7f)
            : new Color(0.45f, 0.45f, 0.5f));
        header.AddChild(nameLabel);

        if (selected)
        {
            Label checkLabel = new()
            {
                Text = "ACTIF"
            };
            checkLabel.AddThemeFontSizeOverride("font_size", 11);
            checkLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.75f, 0.4f));
            header.AddChild(checkLabel);
        }
        else if (owned)
        {
            Label ownedLabel = new()
            {
                Text = "Acheté"
            };
            ownedLabel.AddThemeFontSizeOverride("font_size", 11);
            ownedLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.6f, 0.4f));
            header.AddChild(ownedLabel);
        }

        // Description
        Label descLabel = new()
        {
            Text = description
        };
        descLabel.AddThemeFontSizeOverride("font_size", 11);
        descLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        content.AddChild(descLabel);

        return card;
    }

    private void OnKitSelected(string kitId)
    {
        MetaSaveManager.SelectKit(kitId);
        RebuildEtabliKits();
        GD.Print($"[Hub] Kit selected: {(string.IsNullOrEmpty(kitId) ? "none" : kitId)}");
    }

    private void OnKitPurchased(string kitId, int cost)
    {
        if (!MetaSaveManager.PurchaseKit(kitId, cost))
        {
            GD.Print($"[Hub] Cannot purchase kit {kitId} (not enough Vestiges)");
            return;
        }

        // Auto-select the purchased kit
        MetaSaveManager.SelectKit(kitId);
        UpdateVestigesDisplay();
        RebuildEtabliKits();
        GD.Print($"[Hub] Kit purchased and selected: {kitId}");
    }

    // --- Helpers ---

    private PanelContainer CreateSectionPanel(string title, float minWidth)
    {
        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(minWidth, 0),
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };

        StyleBoxFlat style = new()
        {
            BgColor = new Color(0.04f, 0.04f, 0.07f, 0.8f),
            BorderColor = new Color(0.15f, 0.15f, 0.2f, 0.5f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        panel.AddThemeStyleboxOverride("panel", style);

        VBoxContainer vbox = new();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        Label titleLabel = new()
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
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
