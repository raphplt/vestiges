using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Hub screen between runs — pixel art UI with tabbed navigation.
/// Tabs: Miroirs (character select), Chroniques (leaderboard),
///        Obélisque (mutators), Établi (starting kits).
/// Bottom bar: seed input, selected character summary, "Entrer dans le Vide" button.
///
/// All UI frames use NinePatchRect with pixel art assets from assets/ui/.
/// Viewport: 1920×1080 (Hub runs at full display resolution, NOT 480×270).
/// </summary>
public partial class HubScreen : Control
{
	// --- Paths to UI assets ---
	private const string UiPath = "res://assets/ui/";
	private const string MenusPath = UiPath + "menus/";
	private const string IconsPath = UiPath + "icons/";

	// --- State ---
	private string _selectedCharacterId;
	private string _activeTab = "miroirs";
	private string _currentChroniquesSubTab = "global";
	private Button _enterVoidButton;
	private Label _selectedCharLabel;
	private Label _vestigesLabel;
	private Control _contentArea;
	private readonly Dictionary<string, Button> _tabButtons = new();
	private readonly Dictionary<string, PanelContainer> _cardsByCharacterId = new();
	private LineEdit _seedInput;

	// --- Cached textures ---
	private Texture2D _panelTex;
	private Texture2D _panelSelTex;
	private Texture2D _cardNormalTex;
	private Texture2D _cardSelectedTex;
	private Texture2D _cardLockedTex;
	private Texture2D _tabNormalTex;
	private Texture2D _tabHoverTex;
	private Texture2D _tabActiveTex;
	private Texture2D _btnNormalTex;
	private Texture2D _btnHoverTex;
	private Texture2D _btnPressedTex;
	private Texture2D _btnDisabledTex;
	private Texture2D _separatorTex;
	private Texture2D _bgTex;
	private Texture2D _titleTex;
	private Texture2D _iconChroniques;
	private Texture2D _iconMiroirs;
	private Texture2D _iconObelisque;
	private Texture2D _iconEtabli;
	private Texture2D _iconVide;
	private Texture2D _iconVestiges;

	// --- Colors (from Charte Graphique) ---
	private static readonly Color GoldColor = new(0.83f, 0.66f, 0.26f);       // #D4A843
	private static readonly Color GoldBright = new(0.9f, 0.78f, 0.39f);       // bright gold
	private static readonly Color GoldDim = new(0.63f, 0.47f, 0.16f);         // dim gold
	private static readonly Color TextColor = new(0.72f, 0.7f, 0.66f);        // warm grey text
	private static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);           // dim text
	private static readonly Color TextVeryDim = new(0.35f, 0.35f, 0.4f);      // locked text
	private static readonly Color BgDark = new(0.04f, 0.05f, 0.09f);          // dark bg
	private static readonly Color CyanEssence = new(0.37f, 0.77f, 0.77f);     // #5EC4C4
	private static readonly Color GreenKit = new(0.4f, 0.6f, 0.4f);

	public override void _Ready()
	{
		CharacterDataLoader.Load();
		WeaponDataLoader.Load();
		PerkDataLoader.Load();
		RunHistoryManager.Load();
		MetaSaveManager.Load();
		StartingKitDataLoader.Load();
		MutatorDataLoader.Load();

		GameManager gm = GetNode<GameManager>("/root/GameManager");
		_selectedCharacterId = gm.SelectedCharacterId;

		if (!string.IsNullOrEmpty(_selectedCharacterId) && !MetaSaveManager.IsCharacterUnlocked(_selectedCharacterId))
			_selectedCharacterId = null;

		Infrastructure.AudioManager.Instance?.PlayHubMusic();

		LoadTextures();

		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		BuildUI();

		EnsureDefaultCharacterSelection(gm);

		UpdateVoidButton();
		UpdateVestigesDisplay();
		ShowTab(_activeTab);

		if (gm.LastRunData != null)
			gm.LastRunData = null;
	}

	private void LoadTextures()
	{
		_panelTex = LoadTex(MenusPath + "ui_panel_frame.png");
		_panelSelTex = LoadTex(MenusPath + "ui_panel_frame_selected.png");
		_cardNormalTex = LoadTex(MenusPath + "ui_card_normal.png");
		_cardSelectedTex = LoadTex(MenusPath + "ui_card_selected.png");
		_cardLockedTex = LoadTex(MenusPath + "ui_card_locked.png");
		_tabNormalTex = LoadTex(MenusPath + "ui_tab_normal.png");
		_tabHoverTex = LoadTex(MenusPath + "ui_tab_hover.png");
		_tabActiveTex = LoadTex(MenusPath + "ui_tab_active.png");
		_btnNormalTex = LoadTex(MenusPath + "ui_button_normal.png");
		_btnHoverTex = LoadTex(MenusPath + "ui_button_hover.png");
		_btnPressedTex = LoadTex(MenusPath + "ui_button_pressed.png");
		_btnDisabledTex = LoadTex(MenusPath + "ui_button_disabled.png");
		_separatorTex = LoadTex(MenusPath + "ui_separator_simple.png");
		_bgTex = LoadTex(MenusPath + "ui_hub_background.png");
		_titleTex = LoadTex(MenusPath + "ui_hub_title.png");
		_iconChroniques = LoadTex(IconsPath + "ui_icon_chroniques.png");
		_iconMiroirs = LoadTex(IconsPath + "ui_icon_miroirs.png");
		_iconObelisque = LoadTex(IconsPath + "ui_icon_obelisque.png");
		_iconEtabli = LoadTex(IconsPath + "ui_icon_etabli.png");
		_iconVide = LoadTex(IconsPath + "ui_icon_vide.png");
		_iconVestiges = LoadTex(IconsPath + "ui_icon_vestiges.png");
	}

	private static Texture2D LoadTex(string path)
	{
		if (ResourceLoader.Exists(path))
			return GD.Load<Texture2D>(path);

		GD.PushWarning($"[Hub] Missing texture: {path}");
		return null;
	}

	// ================================================================
	// BUILD UI — Viewport is 1920×1080
	// ================================================================

	private void BuildUI()
	{
		// Background
		if (_bgTex != null)
		{
			TextureRect bg = new()
			{
				Texture = _bgTex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale
			};
			bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			AddChild(bg);
		}
		else
		{
			ColorRect bg = new()
			{
				Color = BgDark
			};
			bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			AddChild(bg);
		}

		// Main vertical layout
		VBoxContainer mainVBox = new();
		mainVBox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		mainVBox.AddThemeConstantOverride("separation", 0);
		AddChild(mainVBox);

		// --- TITLE AREA ---
		mainVBox.AddChild(BuildTitleArea());

		// --- TAB BAR ---
		mainVBox.AddChild(BuildTabBar());

		// --- CONTENT AREA (swapped per tab) ---
		_contentArea = new Control
		{
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		mainVBox.AddChild(_contentArea);

		// --- BOTTOM BAR ---
		mainVBox.AddChild(BuildBottomBar());
	}

	// ----------------------------------------------------------------
	// TITLE AREA
	// ----------------------------------------------------------------
	private MarginContainer BuildTitleArea()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_top", 40);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		margin.AddThemeConstantOverride("margin_left", 0);
		margin.AddThemeConstantOverride("margin_right", 0);

		HBoxContainer row = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		row.AddThemeConstantOverride("separation", 30);
		margin.AddChild(row);

		// Title sprite
		if (_titleTex != null)
		{
			TextureRect titleImg = new()
			{
				Texture = _titleTex,
				StretchMode = TextureRect.StretchModeEnum.KeepAspect,
				CustomMinimumSize = new Vector2(_titleTex.GetWidth(), _titleTex.GetHeight())
			};
			row.AddChild(titleImg);
		}
		else
		{
			Label titleLabel = new()
			{
				Text = "VESTIGES",
				HorizontalAlignment = HorizontalAlignment.Center
			};
			titleLabel.AddThemeFontSizeOverride("font_size", 48);
			titleLabel.AddThemeColorOverride("font_color", GoldColor);
			row.AddChild(titleLabel);
		}

		// Vestiges currency
		HBoxContainer vestigesRow = new();
		vestigesRow.AddThemeConstantOverride("separation", 8);
		row.AddChild(vestigesRow);

		if (_iconVestiges != null)
		{
			TextureRect vestigesIcon = new()
			{
				Texture = _iconVestiges,
				CustomMinimumSize = new Vector2(32, 32),
				StretchMode = TextureRect.StretchModeEnum.KeepAspect
			};
			vestigesRow.AddChild(vestigesIcon);
		}

		_vestigesLabel = new Label();
		_vestigesLabel.AddThemeFontSizeOverride("font_size", 20);
		_vestigesLabel.AddThemeColorOverride("font_color", GoldColor);
		vestigesRow.AddChild(_vestigesLabel);

		return margin;
	}

	// ----------------------------------------------------------------
	// TAB BAR
	// ----------------------------------------------------------------
	private MarginContainer BuildTabBar()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_top", 6);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		margin.AddThemeConstantOverride("margin_left", 200);
		margin.AddThemeConstantOverride("margin_right", 200);

		HBoxContainer tabRow = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		tabRow.AddThemeConstantOverride("separation", 12);
		margin.AddChild(tabRow);

		CreateTabButton(tabRow, "miroirs", "Miroirs", _iconMiroirs);
		CreateTabButton(tabRow, "chroniques", "Chroniques", _iconChroniques);
		CreateTabButton(tabRow, "obelisque", "Obélisque", _iconObelisque);
		CreateTabButton(tabRow, "etabli", "Établi", _iconEtabli);

		return margin;
	}

	private void CreateTabButton(HBoxContainer parent, string tabId, string label, Texture2D icon)
	{
		Button btn = new()
		{
			Text = "  " + label,
			CustomMinimumSize = new Vector2(200, 44),
			ToggleMode = true,
			ButtonPressed = tabId == _activeTab,
			Flat = true,
			IconAlignment = HorizontalAlignment.Left
		};

		if (icon != null)
			btn.Icon = icon;

		btn.AddThemeFontSizeOverride("font_size", 18);

		// Style with NinePatchRect textures
		ApplyTabStyle(btn, tabId == _activeTab);

		string capturedId = tabId;
		btn.Pressed += () => OnTabPressed(capturedId);

		_tabButtons[tabId] = btn;
		parent.AddChild(btn);
	}

	private void ApplyTabStyle(Button btn, bool active)
	{
		Texture2D tex = active ? _tabActiveTex : _tabNormalTex;
		if (tex == null) return;

		StyleBoxTexture style = CreateNinePatch(tex, 4, 4, 4, 4);
		btn.AddThemeStyleboxOverride("normal", active ? style : style);
		btn.AddThemeStyleboxOverride("hover", CreateNinePatch(_tabHoverTex ?? tex, 4, 4, 4, 4));
		btn.AddThemeStyleboxOverride("pressed", CreateNinePatch(_tabActiveTex ?? tex, 4, 4, 4, 4));

		Color fontColor = active ? GoldBright : TextDim;
		btn.AddThemeColorOverride("font_color", fontColor);
		btn.AddThemeColorOverride("font_hover_color", GoldColor);
		btn.AddThemeColorOverride("font_pressed_color", GoldBright);
	}

	private void OnTabPressed(string tabId)
	{
		_activeTab = tabId;

		foreach (KeyValuePair<string, Button> pair in _tabButtons)
		{
			pair.Value.ButtonPressed = pair.Key == tabId;
			ApplyTabStyle(pair.Value, pair.Key == tabId);
		}

		ShowTab(tabId);
	}

	// ----------------------------------------------------------------
	// CONTENT AREA — Show/hide tab content
	// ----------------------------------------------------------------
	private void ShowTab(string tabId)
	{
		// Clear current content
		foreach (Node child in _contentArea.GetChildren())
			child.QueueFree();

		CallDeferred(MethodName.BuildTabContent, tabId);
	}

	private void BuildTabContent(string tabId)
	{
		// Clean up deferred
		while (_contentArea.GetChildCount() > 0)
			_contentArea.RemoveChild(_contentArea.GetChild(0));

		Control content = tabId switch
		{
			"miroirs" => BuildMiroirsContent(),
			"chroniques" => BuildChroniquesContent(),
			"obelisque" => BuildObelisqueContent(),
			"etabli" => BuildEtabliContent(),
			_ => new Control()
		};

		content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_contentArea.AddChild(content);
	}

	// ----------------------------------------------------------------
	// BOTTOM BAR
	// ----------------------------------------------------------------
	private MarginContainer BuildBottomBar()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 30);
		margin.AddThemeConstantOverride("margin_left", 0);
		margin.AddThemeConstantOverride("margin_right", 0);

		VBoxContainer bottomBox = new();
		bottomBox.AddThemeConstantOverride("separation", 10);
		margin.AddChild(bottomBox);

		// Selected character label
		_selectedCharLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_selectedCharLabel.AddThemeFontSizeOverride("font_size", 18);
		_selectedCharLabel.AddThemeColorOverride("font_color", TextColor);
		bottomBox.AddChild(_selectedCharLabel);

		// Seed input row
		CenterContainer seedCenter = new();
		bottomBox.AddChild(seedCenter);

		HBoxContainer seedRow = new();
		seedRow.AddThemeConstantOverride("separation", 10);
		seedCenter.AddChild(seedRow);

		Label seedLabel = new()
		{
			Text = "Seed :"
		};
		seedLabel.AddThemeFontSizeOverride("font_size", 14);
		seedLabel.AddThemeColorOverride("font_color", TextDim);
		seedRow.AddChild(seedLabel);

		_seedInput = new LineEdit
		{
			PlaceholderText = "vide = aléatoire",
			CustomMinimumSize = new Vector2(200, 36)
		};
		_seedInput.AddThemeFontSizeOverride("font_size", 14);

		if (_cardNormalTex != null)
		{
			StyleBoxTexture seedStyle = CreateNinePatch(_cardNormalTex, 3, 3, 3, 3);
			seedStyle.ContentMarginLeft = 10;
			seedStyle.ContentMarginRight = 10;
			seedStyle.ContentMarginTop = 6;
			seedStyle.ContentMarginBottom = 6;
			_seedInput.AddThemeStyleboxOverride("normal", seedStyle);
		}
		seedRow.AddChild(_seedInput);

		// Void button with icon
		CenterContainer buttonCenter = new();
		bottomBox.AddChild(buttonCenter);

		_enterVoidButton = new Button
		{
			Text = "Entrer dans le Vide",
			CustomMinimumSize = new Vector2(300, 55)
		};
		_enterVoidButton.AddThemeFontSizeOverride("font_size", 22);

		if (_iconVide != null)
			_enterVoidButton.Icon = _iconVide;

		// Apply pixel art button style
		ApplyButtonStyle(_enterVoidButton);

		_enterVoidButton.Pressed += OnEnterVoidPressed;
		buttonCenter.AddChild(_enterVoidButton);

		return margin;
	}

	private void ApplyButtonStyle(Button btn)
	{
		if (_btnNormalTex != null)
			btn.AddThemeStyleboxOverride("normal", CreateNinePatch(_btnNormalTex, 4, 4, 4, 4));
		if (_btnHoverTex != null)
			btn.AddThemeStyleboxOverride("hover", CreateNinePatch(_btnHoverTex, 4, 4, 4, 4));
		if (_btnPressedTex != null)
			btn.AddThemeStyleboxOverride("pressed", CreateNinePatch(_btnPressedTex, 4, 4, 4, 4));
		if (_btnDisabledTex != null)
			btn.AddThemeStyleboxOverride("disabled", CreateNinePatch(_btnDisabledTex, 4, 4, 4, 4));

		btn.AddThemeColorOverride("font_color", GoldColor);
		btn.AddThemeColorOverride("font_hover_color", GoldBright);
		btn.AddThemeColorOverride("font_pressed_color", GoldBright);
		btn.AddThemeColorOverride("font_disabled_color", TextVeryDim);
	}

	// ================================================================
	// TAB CONTENT: MIROIRS (Character Selection)
	// ================================================================

	private ScrollContainer BuildMiroirsContent()
	{
		ScrollContainer scroll = new();
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 100);
		margin.AddThemeConstantOverride("margin_right", 100);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(margin);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 12);
		vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		margin.AddChild(vbox);

		Label desc = new()
		{
			Text = "Choisir un personnage",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		desc.AddThemeFontSizeOverride("font_size", 16);
		desc.AddThemeColorOverride("font_color", TextDim);
		vbox.AddChild(desc);

		// Character cards in a horizontal flow
		HFlowContainer flow = new();
		flow.AddThemeConstantOverride("h_separation", 16);
		flow.AddThemeConstantOverride("v_separation", 16);
		flow.Alignment = FlowContainer.AlignmentMode.Center;
		vbox.AddChild(flow);

		_cardsByCharacterId.Clear();
		List<CharacterData> characters = CharacterDataLoader.GetAll();
		foreach (CharacterData character in characters)
			CreateCharacterCard(flow, character);

		return scroll;
	}

	private void CreateCharacterCard(HFlowContainer parent, CharacterData character)
	{
		bool isUnlocked = MetaSaveManager.IsCharacterUnlocked(character.Id);
		bool isSelected = character.Id == _selectedCharacterId;

		PanelContainer card = new()
		{
			CustomMinimumSize = new Vector2(280, 0)
		};

		// Apply pixel art card frame
		Texture2D cardTex = isSelected ? _cardSelectedTex : (isUnlocked ? _cardNormalTex : _cardLockedTex);
		if (cardTex != null)
		{
			StyleBoxTexture style = CreateNinePatch(cardTex, 3, 3, 3, 3);
			style.ContentMarginLeft = 16;
			style.ContentMarginRight = 16;
			style.ContentMarginTop = 12;
			style.ContentMarginBottom = 12;
			card.AddThemeStyleboxOverride("panel", style);
		}

		VBoxContainer content = new();
		content.AddThemeConstantOverride("separation", 6);
		card.AddChild(content);

		Color textColor = isUnlocked ? character.VisualColor : TextVeryDim;

		// Name
		Label nameLabel = new()
		{
			Text = character.Name,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		nameLabel.AddThemeFontSizeOverride("font_size", 20);
		nameLabel.AddThemeColorOverride("font_color", textColor);
		content.AddChild(nameLabel);

		if (isUnlocked)
		{
			// Description
			Label descLabel = new()
			{
				Text = character.Description,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			descLabel.AddThemeFontSizeOverride("font_size", 13);
			descLabel.AddThemeColorOverride("font_color", TextDim);
			content.AddChild(descLabel);

			// Stats compact
			CharacterStats stats = character.BaseStats;
			Label statsLabel = new()
			{
				Text = $"PV:{stats.MaxHp}  ATK:{stats.AttackDamage}  VIT:{stats.Speed}  PORT:{stats.AttackRange}",
				HorizontalAlignment = HorizontalAlignment.Center
			};
			statsLabel.AddThemeFontSizeOverride("font_size", 12);
			statsLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
			content.AddChild(statsLabel);

			// Passive
			PerkData passive = PerkDataLoader.Get(character.PassivePerk);
			if (passive != null)
			{
				Label passiveLabel = new()
				{
					Text = $"Passif : {passive.Name}",
					HorizontalAlignment = HorizontalAlignment.Center
				};
				passiveLabel.AddThemeFontSizeOverride("font_size", 12);
				passiveLabel.AddThemeColorOverride("font_color", GoldDim);
				content.AddChild(passiveLabel);
			}

			// Starting weapon
			WeaponData weapon = WeaponDataLoader.Get(character.StartingWeaponId)
				?? WeaponDataLoader.GetDefaultForCharacter(character.Id);
			if (weapon != null)
			{
				Label weaponLabel = new()
				{
					Text = $"Arme : {weapon.Name}",
					HorizontalAlignment = HorizontalAlignment.Center
				};
				weaponLabel.AddThemeFontSizeOverride("font_size", 12);
				weaponLabel.AddThemeColorOverride("font_color", CyanEssence);
				content.AddChild(weaponLabel);
			}

			// Selection indicator
			if (isSelected)
			{
				Label selLabel = new()
				{
					Text = "SÉLECTIONNÉ",
					HorizontalAlignment = HorizontalAlignment.Center
				};
				selLabel.AddThemeFontSizeOverride("font_size", 13);
				selLabel.AddThemeColorOverride("font_color", GoldBright);
				content.AddChild(selLabel);
			}

			// Click handler
			string capturedId = character.Id;
			card.GuiInput += (InputEvent @event) =>
			{
				if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
					OnCharacterClicked(capturedId);
			};
		}
		else
		{
			Label lockLabel = new()
			{
				Text = GetUnlockConditionText(character.UnlockCondition),
				HorizontalAlignment = HorizontalAlignment.Center,
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			lockLabel.AddThemeFontSizeOverride("font_size", 13);
			lockLabel.AddThemeColorOverride("font_color", TextVeryDim);
			content.AddChild(lockLabel);
		}

		_cardsByCharacterId[character.Id] = card;
		parent.AddChild(card);
	}

	// ================================================================
	// TAB CONTENT: CHRONIQUES (Leaderboard)
	// ================================================================

	private MarginContainer BuildChroniquesContent()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 300);
		margin.AddThemeConstantOverride("margin_right", 300);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 10);
		vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		margin.AddChild(vbox);

		// Sub-tab row
		HBoxContainer subTabRow = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		subTabRow.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(subTabRow);

		CreateChroniquesSubTab(subTabRow, "global", "Global");
		CreateChroniquesSubTab(subTabRow, "personnage", "Par Perso.");
		CreateChroniquesSubTab(subTabRow, "nuits", "Par Nuits");

		// Separator
		if (_separatorTex != null)
		{
			TextureRect sep = new()
			{
				Texture = _separatorTex,
				StretchMode = TextureRect.StretchModeEnum.KeepAspect,
				CustomMinimumSize = new Vector2(0, 6),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			vbox.AddChild(sep);
		}

		// Content area for sub-tab
		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		vbox.AddChild(scroll);

		VBoxContainer scrollContent = new();
		scrollContent.AddThemeConstantOverride("separation", 6);
		scrollContent.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(scrollContent);

		// Build sub-tab content
		switch (_currentChroniquesSubTab)
		{
			case "global":
				BuildChroniquesGlobal(scrollContent);
				break;
			case "personnage":
				BuildChroniquesPerso(scrollContent);
				break;
			case "nuits":
				BuildChroniquesNuits(scrollContent);
				break;
		}

		return margin;
	}

	private void CreateChroniquesSubTab(HBoxContainer parent, string tabId, string label)
	{
		bool active = tabId == _currentChroniquesSubTab;

		Button btn = new()
		{
			Text = label,
			CustomMinimumSize = new Vector2(140, 36),
			ToggleMode = true,
			ButtonPressed = active,
			Flat = true
		};
		btn.AddThemeFontSizeOverride("font_size", 15);

		ApplyTabStyle(btn, active);

		string capturedId = tabId;
		btn.Pressed += () =>
		{
			_currentChroniquesSubTab = capturedId;
			ShowTab("chroniques");
		};

		parent.AddChild(btn);
	}

	private void BuildChroniquesGlobal(VBoxContainer container)
	{
		int bestScore = RunHistoryManager.GetBestScore();
		if (bestScore > 0)
		{
			Label record = new()
			{
				Text = $"Record : {bestScore:N0}",
				HorizontalAlignment = HorizontalAlignment.Center
			};
			record.AddThemeFontSizeOverride("font_size", 22);
			record.AddThemeColorOverride("font_color", GoldBright);
			container.AddChild(record);
		}

		List<RunRecord> topRuns = RunHistoryManager.GetTopByScore(10);
		if (topRuns.Count == 0)
		{
			AddEmptyLabel(container);
			return;
		}

		for (int i = 0; i < topRuns.Count; i++)
		{
			RunRecord run = topRuns[i];
			string nights = run.NightsSurvived > 0 ? $"{run.NightsSurvived}N" : "0N";
			string charName = TruncName(run.CharacterName ?? run.CharacterId);

			Label runLabel = new()
			{
				Text = $"#{i + 1}  {charName} — {run.Score:N0} — {nights}"
			};
			runLabel.AddThemeFontSizeOverride("font_size", 16);

			bool isCurrentChar = run.CharacterId == _selectedCharacterId;
			runLabel.AddThemeColorOverride("font_color", isCurrentChar ? GoldDim : TextDim);
			container.AddChild(runLabel);
		}
	}

	private void BuildChroniquesPerso(VBoxContainer container)
	{
		Dictionary<string, RunAnalytics.CharacterRunStats> charStats = RunAnalytics.GetCharacterStats();
		if (charStats.Count == 0)
		{
			AddEmptyLabel(container);
			return;
		}

		foreach (KeyValuePair<string, RunAnalytics.CharacterRunStats> pair in charStats)
		{
			CharacterData charData = CharacterDataLoader.Get(pair.Key);
			string charName = charData?.Name ?? pair.Key;

			Label nameLabel = new()
			{
				Text = charName
			};
			nameLabel.AddThemeFontSizeOverride("font_size", 18);
			nameLabel.AddThemeColorOverride("font_color", charData?.VisualColor ?? TextColor);
			container.AddChild(nameLabel);

			Label statsLabel = new()
			{
				Text = $"  Record: {pair.Value.BestScore:N0} — Moy: {pair.Value.AvgScore:N0} — {pair.Value.RunCount} runs"
			};
			statsLabel.AddThemeFontSizeOverride("font_size", 14);
			statsLabel.AddThemeColorOverride("font_color", TextDim);
			container.AddChild(statsLabel);
		}
	}

	private void BuildChroniquesNuits(VBoxContainer container)
	{
		int maxNights = RunHistoryManager.GetMaxNights();
		if (maxNights > 0)
		{
			Label record = new()
			{
				Text = $"Record : {maxNights} nuits",
				HorizontalAlignment = HorizontalAlignment.Center
			};
			record.AddThemeFontSizeOverride("font_size", 22);
			record.AddThemeColorOverride("font_color", GoldBright);
			container.AddChild(record);
		}

		List<RunRecord> topRuns = RunHistoryManager.GetTopByNights(10);
		if (topRuns.Count == 0)
		{
			AddEmptyLabel(container);
			return;
		}

		for (int i = 0; i < topRuns.Count; i++)
		{
			RunRecord run = topRuns[i];
			string nights = run.NightsSurvived > 0 ? $"{run.NightsSurvived}N" : "0N";
			string charName = TruncName(run.CharacterName ?? run.CharacterId);

			Label runLabel = new()
			{
				Text = $"#{i + 1}  {charName} — {nights} — {run.Score:N0}"
			};
			runLabel.AddThemeFontSizeOverride("font_size", 16);

			bool isCurrentChar = run.CharacterId == _selectedCharacterId;
			runLabel.AddThemeColorOverride("font_color", isCurrentChar ? GoldDim : TextDim);
			container.AddChild(runLabel);
		}
	}

	// ================================================================
	// TAB CONTENT: OBÉLISQUE (Mutators)
	// ================================================================

	private MarginContainer BuildObelisqueContent()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 250);
		margin.AddThemeConstantOverride("margin_right", 250);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 10);
		vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		margin.AddChild(vbox);

		// Description
		Label desc = new()
		{
			Text = "Mutateurs de difficulté",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		desc.AddThemeFontSizeOverride("font_size", 16);
		desc.AddThemeColorOverride("font_color", TextDim);
		vbox.AddChild(desc);

		// Multiplier
		List<string> activeMutators = MetaSaveManager.GetActiveMutators();
		float totalMult = 1f;
		foreach (string id in activeMutators)
		{
			MutatorData mut = MutatorDataLoader.Get(id);
			if (mut != null) totalMult *= mut.ScoreMultiplier;
		}

		Label multLabel = new()
		{
			Text = $"Multiplicateur : x{totalMult:F2}",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		multLabel.AddThemeFontSizeOverride("font_size", 20);
		multLabel.AddThemeColorOverride("font_color", GoldColor);
		vbox.AddChild(multLabel);

		// Separator
		AddSeparator(vbox);

		// Mutator cards
		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		vbox.AddChild(scroll);

		VBoxContainer cardsBox = new();
		cardsBox.AddThemeConstantOverride("separation", 10);
		cardsBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(cardsBox);

		List<MutatorData> mutators = MutatorDataLoader.GetAll();
		List<string> unlocked = MetaSaveManager.GetUnlockedMutators();

		foreach (MutatorData mutator in mutators)
		{
			bool isUnlocked = unlocked.Contains(mutator.Id);
			bool isActive = activeMutators.Contains(mutator.Id);
			CreateMutatorCard(cardsBox, mutator, isUnlocked, isActive);
		}

		return margin;
	}

	private void CreateMutatorCard(VBoxContainer parent, MutatorData mutator, bool isUnlocked, bool isActive)
	{
		PanelContainer card = new()
		{
			CustomMinimumSize = new Vector2(0, 0),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};

		Texture2D tex = isActive ? _cardSelectedTex : (isUnlocked ? _cardNormalTex : _cardLockedTex);
		if (tex != null)
		{
			StyleBoxTexture style = CreateNinePatch(tex, 3, 3, 3, 3);
			style.ContentMarginLeft = 16;
			style.ContentMarginRight = 16;
			style.ContentMarginTop = 10;
			style.ContentMarginBottom = 10;
			card.AddThemeStyleboxOverride("panel", style);
		}

		VBoxContainer content = new();
		content.AddThemeConstantOverride("separation", 4);
		card.AddChild(content);

		// Header
		HBoxContainer header = new();
		header.AddThemeConstantOverride("separation", 10);
		content.AddChild(header);

		Label nameLabel = new()
		{
			Text = mutator.Name,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		nameLabel.AddThemeFontSizeOverride("font_size", 16);
		nameLabel.AddThemeColorOverride("font_color", isUnlocked ? TextColor : TextVeryDim);
		header.AddChild(nameLabel);

		if (isUnlocked)
		{
			Label multLabel = new()
			{
				Text = $"x{mutator.ScoreMultiplier:F2}"
			};
			multLabel.AddThemeFontSizeOverride("font_size", 14);
			multLabel.AddThemeColorOverride("font_color", isActive ? GoldBright : TextDim);
			header.AddChild(multLabel);
		}

		if (isUnlocked)
		{
			Label descLabel = new()
			{
				Text = mutator.Description,
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			descLabel.AddThemeFontSizeOverride("font_size", 13);
			descLabel.AddThemeColorOverride("font_color", TextDim);
			content.AddChild(descLabel);

			string capturedId = mutator.Id;
			card.GuiInput += (InputEvent @event) =>
			{
				if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
					OnMutatorToggled(capturedId);
			};
		}
		else
		{
			Label lockLabel = new()
			{
				Text = $"Survivre {mutator.UnlockNights} nuits"
			};
			lockLabel.AddThemeFontSizeOverride("font_size", 13);
			lockLabel.AddThemeColorOverride("font_color", TextVeryDim);
			content.AddChild(lockLabel);
		}

		parent.AddChild(card);
	}

	// ================================================================
	// TAB CONTENT: ÉTABLI (Starting Kits)
	// ================================================================

	private MarginContainer BuildEtabliContent()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 250);
		margin.AddThemeConstantOverride("margin_right", 250);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 10);
		vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		margin.AddChild(vbox);

		Label desc = new()
		{
			Text = "Kits de départ",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		desc.AddThemeFontSizeOverride("font_size", 16);
		desc.AddThemeColorOverride("font_color", TextDim);
		vbox.AddChild(desc);

		AddSeparator(vbox);

		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		vbox.AddChild(scroll);

		VBoxContainer cardsBox = new();
		cardsBox.AddThemeConstantOverride("separation", 10);
		cardsBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(cardsBox);

		HashSet<string> purchased = MetaSaveManager.GetPurchasedKits();
		string selectedKit = MetaSaveManager.GetSelectedKit();
		int vestiges = MetaSaveManager.GetVestiges();

		// "None" option
		PanelContainer noneCard = CreateKitCard("Aucun", "Pas de kit de départ", 0, true, string.IsNullOrEmpty(selectedKit));
		noneCard.GuiInput += (InputEvent @event) =>
		{
			if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
				OnKitSelected("");
		};
		cardsBox.AddChild(noneCard);

		List<StartingKitData> kits = StartingKitDataLoader.GetAll();
		foreach (StartingKitData kit in kits)
		{
			bool owned = purchased.Contains(kit.Id);
			bool selected = kit.Id == selectedKit;

			PanelContainer kitCard = CreateKitCard(kit.Name, kit.Description, kit.Cost, owned, selected);

			if (owned)
			{
				string capturedId = kit.Id;
				kitCard.GuiInput += (InputEvent @event) =>
				{
					if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
						OnKitSelected(capturedId);
				};
			}
			else
			{
				VBoxContainer cardContent = kitCard.GetChild<VBoxContainer>(0);

				Button buyButton = new()
				{
					Text = $"Acheter ({kit.Cost} V)",
					CustomMinimumSize = new Vector2(0, 36),
					Disabled = vestiges < kit.Cost
				};
				buyButton.AddThemeFontSizeOverride("font_size", 14);
				ApplyButtonStyle(buyButton);

				string capturedKitId = kit.Id;
				int capturedCost = kit.Cost;
				buyButton.Pressed += () => OnKitPurchased(capturedKitId, capturedCost);
				cardContent.AddChild(buyButton);
			}

			cardsBox.AddChild(kitCard);
		}

		return margin;
	}

	private PanelContainer CreateKitCard(string name, string description, int cost, bool owned, bool selected)
	{
		PanelContainer card = new()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};

		Texture2D tex = selected ? _cardSelectedTex : (owned ? _cardNormalTex : _cardLockedTex);
		if (tex != null)
		{
			StyleBoxTexture style = CreateNinePatch(tex, 3, 3, 3, 3);
			style.ContentMarginLeft = 16;
			style.ContentMarginRight = 16;
			style.ContentMarginTop = 10;
			style.ContentMarginBottom = 10;
			card.AddThemeStyleboxOverride("panel", style);
		}

		VBoxContainer content = new();
		content.AddThemeConstantOverride("separation", 4);
		card.AddChild(content);

		// Header
		HBoxContainer header = new();
		header.AddThemeConstantOverride("separation", 12);
		content.AddChild(header);

		Label nameLabel = new()
		{
			Text = name,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		nameLabel.AddThemeFontSizeOverride("font_size", 18);
		nameLabel.AddThemeColorOverride("font_color", owned ? TextColor : TextVeryDim);
		header.AddChild(nameLabel);

		if (selected)
		{
			Label checkLabel = new() { Text = "ACTIF" };
			checkLabel.AddThemeFontSizeOverride("font_size", 14);
			checkLabel.AddThemeColorOverride("font_color", GoldBright);
			header.AddChild(checkLabel);
		}
		else if (owned)
		{
			Label ownedLabel = new() { Text = "Acheté" };
			ownedLabel.AddThemeFontSizeOverride("font_size", 14);
			ownedLabel.AddThemeColorOverride("font_color", GreenKit);
			header.AddChild(ownedLabel);
		}

		Label descLabel = new()
		{
			Text = description,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		descLabel.AddThemeFontSizeOverride("font_size", 13);
		descLabel.AddThemeColorOverride("font_color", TextDim);
		content.AddChild(descLabel);

		return card;
	}

	// ================================================================
	// EVENT HANDLERS
	// ================================================================

	private void OnCharacterClicked(string characterId)
	{
		_selectedCharacterId = characterId;
		GetNode<GameManager>("/root/GameManager").SelectedCharacterId = characterId;
		UpdateVoidButton();
		ShowTab("miroirs");
	}

	private void OnMutatorToggled(string mutatorId)
	{
		MetaSaveManager.ToggleMutator(mutatorId);
		ShowTab("obelisque");
		GD.Print($"[Hub] Mutator toggled: {mutatorId}");
	}

	private void OnKitSelected(string kitId)
	{
		MetaSaveManager.SelectKit(kitId);
		ShowTab("etabli");
		GD.Print($"[Hub] Kit selected: {(string.IsNullOrEmpty(kitId) ? "none" : kitId)}");
	}

	private void OnKitPurchased(string kitId, int cost)
	{
		if (!MetaSaveManager.PurchaseKit(kitId, cost))
		{
			GD.Print($"[Hub] Cannot purchase kit {kitId}");
			return;
		}

		MetaSaveManager.SelectKit(kitId);
		UpdateVestigesDisplay();
		ShowTab("etabli");
		GD.Print($"[Hub] Kit purchased: {kitId}");
	}

	private void OnEnterVoidPressed()
	{
		if (string.IsNullOrEmpty(_selectedCharacterId))
			return;

		GameManager gm = GetNode<GameManager>("/root/GameManager");
		gm.SelectedCharacterId = _selectedCharacterId;
		gm.ActiveMutators = new List<string>(MetaSaveManager.GetActiveMutators());

		string seedText = _seedInput.Text.StripEdges();
		gm.RunSeed = !string.IsNullOrEmpty(seedText) && ulong.TryParse(seedText, out ulong seed) ? seed : 0;

		gm.ChangeState(GameManager.GameState.Run);
		GD.Print($"[Hub] Entering Void: {_selectedCharacterId}, mutators={gm.ActiveMutators.Count}, seed={gm.RunSeed}");
		GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
	}

	// ================================================================
	// HELPERS
	// ================================================================

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
			GD.Print($"[Hub] Default character: {character.Id}");
			return;
		}

		GD.PushWarning("[Hub] No unlocked character.");
	}

	private void UpdateVoidButton()
	{
		bool hasSelection = !string.IsNullOrEmpty(_selectedCharacterId);
		_enterVoidButton.Disabled = !hasSelection;

		CharacterData data = !string.IsNullOrEmpty(_selectedCharacterId)
			? CharacterDataLoader.Get(_selectedCharacterId)
			: null;

		_selectedCharLabel.Text = data != null
			? $"Personnage : {data.Name}"
			: "Aucun personnage sélectionné";
	}

	private void UpdateVestigesDisplay()
	{
		int vestiges = MetaSaveManager.GetVestiges();
		_vestigesLabel.Text = $"{vestiges}";
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

	private static string TruncName(string name)
	{
		if (string.IsNullOrEmpty(name)) return "?";
		return name.Length > 10 ? name[..10] : name;
	}

	private void AddEmptyLabel(VBoxContainer container)
	{
		Label empty = new()
		{
			Text = "Pas encore d'historique.",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		empty.AddThemeFontSizeOverride("font_size", 16);
		empty.AddThemeColorOverride("font_color", TextVeryDim);
		container.AddChild(empty);
	}

	private void AddSeparator(VBoxContainer container)
	{
		if (_separatorTex != null)
		{
			TextureRect sep = new()
			{
				Texture = _separatorTex,
				StretchMode = TextureRect.StretchModeEnum.KeepAspect,
				CustomMinimumSize = new Vector2(0, 6),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			container.AddChild(sep);
		}
		else
		{
			container.AddChild(new HSeparator());
		}
	}

	/// <summary>
	/// Create a StyleBoxTexture configured as a NinePatch from a texture.
	/// Margins define the non-stretchable border regions.
	/// </summary>
	private static StyleBoxTexture CreateNinePatch(Texture2D texture, int left, int top, int right, int bottom)
	{
		StyleBoxTexture sbt = new()
		{
			Texture = texture,
			RegionRect = new Rect2(0, 0, texture.GetWidth(), texture.GetHeight()),
			AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile,
			AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile
		};

		// NinePatch margins (Godot 4.x uses TextureMargin*)
		sbt.TextureMarginLeft = left;
		sbt.TextureMarginTop = top;
		sbt.TextureMarginRight = right;
		sbt.TextureMarginBottom = bottom;

		// Content margins for padding
		sbt.ContentMarginLeft = left + 2;
		sbt.ContentMarginTop = top + 2;
		sbt.ContentMarginRight = right + 2;
		sbt.ContentMarginBottom = bottom + 2;

		return sbt;
	}
}
