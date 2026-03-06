using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Hub screen between runs — two-state menu system.
/// MainMenu: centered title, nav buttons, "Entrer dans le Vide".
/// SubScreen: back button + full-screen tab content (Miroirs, Chroniques, Obélisque, Établi).
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

	// --- Hub state machine ---
	private enum HubState { MainMenu, SubScreen }
	private HubState _currentState = HubState.MainMenu;

	// --- State ---
	private string _selectedCharacterId;
	private string _activeTab = "miroirs";
	private string _currentChroniquesSubTab = "global";
	private Button _enterVoidButton;
	private Button _subScreenVoidButton;
	private Label _selectedCharLabel;
	private Label _vestigesLabel;
	private Label _subScreenVestigesLabel;
	private Label _subScreenTitle;
	private Control _contentArea;
	private Control _mainMenuLayer;
	private Control _subScreenLayer;
	private CenterContainer _subScreenSeedRow;
	private readonly Dictionary<string, PanelContainer> _cardsByCharacterId = new();
	private LineEdit _seedInput;
	private SettingsScreen _settingsScreen;

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
	private Texture2D _cornerTL;
	private Texture2D _cornerTR;
	private Texture2D _cornerBL;
	private Texture2D _cornerBR;

	// --- Colors (from Charte Graphique) ---
	private static readonly Color GoldColor = new(0.83f, 0.66f, 0.26f);
	private static readonly Color GoldBright = new(0.9f, 0.78f, 0.39f);
	private static readonly Color GoldDim = new(0.63f, 0.47f, 0.16f);
	private static readonly Color TextColor = new(0.72f, 0.7f, 0.66f);
	private static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);
	private static readonly Color TextVeryDim = new(0.35f, 0.35f, 0.4f);
	private static readonly Color BgDark = new(0.04f, 0.05f, 0.09f);
	private static readonly Color CyanEssence = new(0.37f, 0.77f, 0.77f);
	private static readonly Color GreenKit = new(0.4f, 0.6f, 0.4f);

	// --- Tab display names ---
	private static readonly Dictionary<string, string> TabDisplayNames = new()
	{
		{ "miroirs", "MIROIRS" },
		{ "chroniques", "CHRONIQUES" },
		{ "obelisque", "OBÉLISQUE" },
		{ "etabli", "ÉTABLI" }
	};

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
		SetState(HubState.MainMenu);

		if (gm.LastRunData != null)
			gm.LastRunData = null;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			if (_settingsScreen != null && _settingsScreen.IsOpen)
				return;

			if (_currentState == HubState.SubScreen)
			{
				SetState(HubState.MainMenu);
				GetViewport().SetInputAsHandled();
			}
		}
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
		_bgTex = LoadTex("res://assets/bg_menu_1920x1080.png");
		_titleTex = LoadTex(MenusPath + "ui_hub_title.png");
		_iconChroniques = LoadTex(IconsPath + "ui_icon_chroniques.png");
		_iconMiroirs = LoadTex(IconsPath + "ui_icon_miroirs.png");
		_iconObelisque = LoadTex(IconsPath + "ui_icon_obelisque.png");
		_iconEtabli = LoadTex(IconsPath + "ui_icon_etabli.png");
		_iconVide = LoadTex(IconsPath + "ui_icon_vide.png");
		_iconVestiges = LoadTex(IconsPath + "ui_icon_vestiges.png");
		_cornerTL = LoadTex(MenusPath + "ui_corner_tl.png");
		_cornerTR = LoadTex(MenusPath + "ui_corner_tr.png");
		_cornerBL = LoadTex(MenusPath + "ui_corner_bl.png");
		_cornerBR = LoadTex(MenusPath + "ui_corner_br.png");
	}

	private static Texture2D LoadTex(string path)
	{
		if (ResourceLoader.Exists(path))
			return GD.Load<Texture2D>(path);

		GD.PushWarning($"[Hub] Missing texture: {path}");
		return null;
	}

	// ================================================================
	// STATE MANAGEMENT
	// ================================================================

	private void SetState(HubState state)
	{
		_currentState = state;
		_mainMenuLayer.Visible = state == HubState.MainMenu;
		_subScreenLayer.Visible = state == HubState.SubScreen;

		if (state == HubState.MainMenu)
		{
			UpdateCharacterSummary();
			UpdateVoidButton();
			UpdateVestigesDisplay();
		}
	}

	private void NavigateToTab(string tabId)
	{
		_activeTab = tabId;
		_subScreenTitle.Text = TabDisplayNames.GetValueOrDefault(tabId, tabId.ToUpper());
		SetState(HubState.SubScreen);
		UpdateSubScreenVestigesDisplay();
		ShowTab(tabId);
		UpdateSubScreenVoidButton();
	}

	// ================================================================
	// BUILD UI — Viewport is 1920×1080
	// ================================================================

	private void BuildUI()
	{
		BuildBackground();
		BuildMainMenu();
		BuildSubScreen();

		_settingsScreen = new SettingsScreen();
		AddChild(_settingsScreen);
	}

	// ----------------------------------------------------------------
	// BACKGROUND + CORNER DECORATIONS
	// ----------------------------------------------------------------
	private void BuildBackground()
	{
		if (_bgTex != null)
		{
			TextureRect bg = new()
			{
				Texture = _bgTex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered
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

		AddCornerDecoration(_cornerTL, 0f, 0f, false, false);
		AddCornerDecoration(_cornerTR, 1f, 0f, true, false);
		AddCornerDecoration(_cornerBL, 0f, 1f, false, true);
		AddCornerDecoration(_cornerBR, 1f, 1f, true, true);
	}

	private void AddCornerDecoration(Texture2D tex, float anchorX, float anchorY, bool flipH, bool flipV)
	{
		if (tex == null) return;

		float w = tex.GetWidth();
		float h = tex.GetHeight();

		TextureRect corner = new()
		{
			Texture = tex,
			ExpandMode = TextureRect.ExpandModeEnum.KeepSize,
			FlipH = flipH,
			FlipV = flipV,
			MouseFilter = MouseFilterEnum.Ignore
		};

		corner.AnchorLeft = anchorX;
		corner.AnchorRight = anchorX;
		corner.AnchorTop = anchorY;
		corner.AnchorBottom = anchorY;
		corner.OffsetLeft = flipH ? -w : 0;
		corner.OffsetRight = flipH ? 0 : w;
		corner.OffsetTop = flipV ? -h : 0;
		corner.OffsetBottom = flipV ? 0 : h;

		AddChild(corner);
	}

	// ----------------------------------------------------------------
	// MAIN MENU LAYER
	// ----------------------------------------------------------------
	private void BuildMainMenu()
	{
		_mainMenuLayer = new Control();
		_mainMenuLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_mainMenuLayer.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(_mainMenuLayer);

		// Centered content with dark backdrop for readability
		CenterContainer center = new();
		center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		center.MouseFilter = MouseFilterEnum.Ignore;
		_mainMenuLayer.AddChild(center);

		PanelContainer backdrop = new();
		StyleBoxFlat backdropStyle = new()
		{
			BgColor = new Color(0.02f, 0.02f, 0.06f, 0.65f),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 60,
			ContentMarginRight = 60,
			ContentMarginTop = 30,
			ContentMarginBottom = 30
		};
		backdrop.AddThemeStyleboxOverride("panel", backdropStyle);
		center.AddChild(backdrop);

		VBoxContainer vbox = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		vbox.AddThemeConstantOverride("separation", 0);
		backdrop.AddChild(vbox);

		// --- Title ---
		if (_titleTex != null)
		{
			CenterContainer titleCenter = new();
			vbox.AddChild(titleCenter);

			TextureRect titleImg = new()
			{
				Texture = _titleTex,
				StretchMode = TextureRect.StretchModeEnum.KeepAspect,
				CustomMinimumSize = new Vector2(_titleTex.GetWidth(), _titleTex.GetHeight())
			};
			titleCenter.AddChild(titleImg);
		}
		else
		{
			Label titleLabel = new()
			{
				Text = "VESTIGES",
				HorizontalAlignment = HorizontalAlignment.Center
			};
			titleLabel.AddThemeFontSizeOverride("font_size", 64);
			titleLabel.AddThemeColorOverride("font_color", GoldColor);
			vbox.AddChild(titleLabel);
		}

		// --- Spacer + Vestiges currency ---
		vbox.AddChild(CreateSpacer(24));

		CenterContainer vestigesCenter = new();
		vbox.AddChild(vestigesCenter);

		HBoxContainer vestigesRow = new();
		vestigesRow.AddThemeConstantOverride("separation", 8);
		vestigesCenter.AddChild(vestigesRow);

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
		_vestigesLabel.AddThemeFontSizeOverride("font_size", 22);
		_vestigesLabel.AddThemeColorOverride("font_color", GoldColor);
		vestigesRow.AddChild(_vestigesLabel);

		// --- Spacer + Selected character summary ---
		vbox.AddChild(CreateSpacer(36));

		_selectedCharLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_selectedCharLabel.AddThemeFontSizeOverride("font_size", 18);
		_selectedCharLabel.AddThemeColorOverride("font_color", TextColor);
		vbox.AddChild(_selectedCharLabel);

		// --- Spacer + Void button ---
		vbox.AddChild(CreateSpacer(16));

		CenterContainer voidCenter = new();
		vbox.AddChild(voidCenter);

		_enterVoidButton = new Button
		{
			Text = "Entrer dans le Vide",
			CustomMinimumSize = new Vector2(360, 60)
		};
		_enterVoidButton.AddThemeFontSizeOverride("font_size", 24);
		if (_iconVide != null)
			_enterVoidButton.Icon = _iconVide;
		ApplyButtonStyle(_enterVoidButton);
		_enterVoidButton.Pressed += OnEnterVoidPressed;
		voidCenter.AddChild(_enterVoidButton);

		// --- Spacer + Separator ---
		vbox.AddChild(CreateSpacer(30));

		if (_separatorTex != null)
		{
			CenterContainer sepCenter = new();
			vbox.AddChild(sepCenter);

			TextureRect sep = new()
			{
				Texture = _separatorTex,
				StretchMode = TextureRect.StretchModeEnum.KeepAspect,
				CustomMinimumSize = new Vector2(340, 6)
			};
			sepCenter.AddChild(sep);
		}

		// --- Spacer + Navigation buttons ---
		vbox.AddChild(CreateSpacer(20));

		VBoxContainer navBox = new();
		navBox.AddThemeConstantOverride("separation", 12);
		vbox.AddChild(navBox);

		CreateNavButton(navBox, "miroirs", "Miroirs", _iconMiroirs);
		CreateNavButton(navBox, "chroniques", "Chroniques", _iconChroniques);
		CreateNavButton(navBox, "obelisque", "Obélisque", _iconObelisque);
		CreateNavButton(navBox, "etabli", "Établi", _iconEtabli);

		// --- Spacer + Paramètres ---
		vbox.AddChild(CreateSpacer(20));

		CenterContainer settingsCenter = new();
		vbox.AddChild(settingsCenter);

		Button settingsBtn = new()
		{
			Text = "Paramètres",
			CustomMinimumSize = new Vector2(320, 50),
			FocusMode = FocusModeEnum.None
		};
		settingsBtn.AddThemeFontSizeOverride("font_size", 20);
		ApplyButtonStyle(settingsBtn);
		settingsBtn.Pressed += () => _settingsScreen?.Open();
		settingsCenter.AddChild(settingsBtn);

		// --- Spacer + Quitter ---
		vbox.AddChild(CreateSpacer(12));

		CenterContainer quitCenter = new();
		vbox.AddChild(quitCenter);

		Button quitBtn = new()
		{
			Text = "Quitter",
			CustomMinimumSize = new Vector2(320, 50),
			FocusMode = FocusModeEnum.None
		};
		quitBtn.AddThemeFontSizeOverride("font_size", 20);
		ApplyButtonStyle(quitBtn);
		quitBtn.Pressed += () => GetTree().Quit();
		quitCenter.AddChild(quitBtn);

	}

	private void CreateNavButton(VBoxContainer parent, string tabId, string label, Texture2D icon)
	{
		CenterContainer btnCenter = new();
		parent.AddChild(btnCenter);

		Button btn = new()
		{
			Text = "  " + label,
			CustomMinimumSize = new Vector2(320, 50),
			Flat = true,
			IconAlignment = HorizontalAlignment.Left,
			FocusMode = FocusModeEnum.None
		};

		if (icon != null)
			btn.Icon = icon;

		btn.AddThemeFontSizeOverride("font_size", 20);
		ApplyButtonStyle(btn);

		string capturedId = tabId;
		btn.Pressed += () => NavigateToTab(capturedId);

		btnCenter.AddChild(btn);
	}

	private void UpdateCharacterSummary()
	{
		CharacterData data = !string.IsNullOrEmpty(_selectedCharacterId)
			? CharacterDataLoader.Get(_selectedCharacterId)
			: null;

		if (data != null)
		{
			CharacterStats stats = data.BaseStats;
			_selectedCharLabel.Text = $"{data.Name}  —  PV:{stats.MaxHp}  ATK:{stats.AttackDamage}  VIT:{stats.Speed}";
		}
		else
		{
			_selectedCharLabel.Text = "Aucun personnage sélectionné";
		}
	}

	// ----------------------------------------------------------------
	// SUB-SCREEN LAYER
	// ----------------------------------------------------------------
	private void BuildSubScreen()
	{
		_subScreenLayer = new Control();
		_subScreenLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_subScreenLayer.MouseFilter = MouseFilterEnum.Ignore;
		_subScreenLayer.Visible = false;
		AddChild(_subScreenLayer);

		VBoxContainer mainVBox = new();
		mainVBox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		mainVBox.AddThemeConstantOverride("separation", 0);
		_subScreenLayer.AddChild(mainVBox);

		// --- TOP BAR ---
		MarginContainer topMargin = new();
		topMargin.AddThemeConstantOverride("margin_top", 16);
		topMargin.AddThemeConstantOverride("margin_bottom", 8);
		topMargin.AddThemeConstantOverride("margin_left", 30);
		topMargin.AddThemeConstantOverride("margin_right", 30);
		mainVBox.AddChild(topMargin);

		HBoxContainer topBar = new();
		topBar.AddThemeConstantOverride("separation", 16);
		topMargin.AddChild(topBar);

		// Back button
		Button backBtn = new()
		{
			Text = "< Retour",
			CustomMinimumSize = new Vector2(140, 44),
			FocusMode = FocusModeEnum.None
		};
		backBtn.AddThemeFontSizeOverride("font_size", 16);
		ApplyButtonStyle(backBtn);
		backBtn.Pressed += () => SetState(HubState.MainMenu);
		topBar.AddChild(backBtn);

		// Spacer
		Control leftSpacer = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		topBar.AddChild(leftSpacer);

		// Sub-screen title
		_subScreenTitle = new Label
		{
			Text = "",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_subScreenTitle.AddThemeFontSizeOverride("font_size", 28);
		_subScreenTitle.AddThemeColorOverride("font_color", GoldColor);
		topBar.AddChild(_subScreenTitle);

		// Spacer
		Control rightSpacer = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		topBar.AddChild(rightSpacer);

		// Vestiges display (small)
		HBoxContainer vestigesRow = new();
		vestigesRow.AddThemeConstantOverride("separation", 6);
		topBar.AddChild(vestigesRow);

		if (_iconVestiges != null)
		{
			TextureRect vestigesIcon = new()
			{
				Texture = _iconVestiges,
				CustomMinimumSize = new Vector2(24, 24),
				StretchMode = TextureRect.StretchModeEnum.KeepAspect
			};
			vestigesRow.AddChild(vestigesIcon);
		}

		_subScreenVestigesLabel = new Label();
		_subScreenVestigesLabel.AddThemeFontSizeOverride("font_size", 18);
		_subScreenVestigesLabel.AddThemeColorOverride("font_color", GoldDim);
		vestigesRow.AddChild(_subScreenVestigesLabel);

		// --- CONTENT AREA ---
		_contentArea = new Control
		{
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		mainVBox.AddChild(_contentArea);

		// --- BOTTOM ACTION BAR (seed + void button) ---
		MarginContainer bottomMargin = new();
		bottomMargin.AddThemeConstantOverride("margin_top", 8);
		bottomMargin.AddThemeConstantOverride("margin_bottom", 20);
		bottomMargin.AddThemeConstantOverride("margin_left", 0);
		bottomMargin.AddThemeConstantOverride("margin_right", 0);
		mainVBox.AddChild(bottomMargin);

		VBoxContainer bottomVBox = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		bottomVBox.AddThemeConstantOverride("separation", 10);
		bottomMargin.AddChild(bottomVBox);

		// Seed input row
		_subScreenSeedRow = new CenterContainer();
		bottomVBox.AddChild(_subScreenSeedRow);

		HBoxContainer seedRow = new();
		seedRow.AddThemeConstantOverride("separation", 10);
		_subScreenSeedRow.AddChild(seedRow);

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

		// Void button
		CenterContainer bottomCenter = new();
		bottomVBox.AddChild(bottomCenter);

		_subScreenVoidButton = new Button
		{
			Text = "Entrer dans le Vide",
			CustomMinimumSize = new Vector2(300, 50),
			Visible = false
		};
		_subScreenVoidButton.AddThemeFontSizeOverride("font_size", 20);
		if (_iconVide != null)
			_subScreenVoidButton.Icon = _iconVide;
		ApplyButtonStyle(_subScreenVoidButton);
		_subScreenVoidButton.Pressed += OnEnterVoidPressed;
		bottomCenter.AddChild(_subScreenVoidButton);
	}

	private void UpdateSubScreenVoidButton()
	{
		bool isMiroirs = _activeTab == "miroirs";
		bool hasChar = !string.IsNullOrEmpty(_selectedCharacterId);
		_subScreenVoidButton.Visible = isMiroirs && hasChar;
		_subScreenVoidButton.Disabled = !hasChar;
		_subScreenSeedRow.Visible = isMiroirs;
	}

	private void UpdateSubScreenVestigesDisplay()
	{
		int vestiges = MetaSaveManager.GetVestiges();
		_subScreenVestigesLabel.Text = $"{vestiges}";
	}

	// ================================================================
	// CONTENT AREA — Show/hide tab content
	// ================================================================
	private void ShowTab(string tabId)
	{
		foreach (Node child in _contentArea.GetChildren())
			child.QueueFree();

		CallDeferred(MethodName.BuildTabContent, tabId);
	}

	private void BuildTabContent(string tabId)
	{
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
			Label descLabel = new()
			{
				Text = character.Description,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			descLabel.AddThemeFontSizeOverride("font_size", 13);
			descLabel.AddThemeColorOverride("font_color", TextDim);
			content.AddChild(descLabel);

			CharacterStats stats = character.BaseStats;
			Label statsLabel = new()
			{
				Text = $"PV:{stats.MaxHp}  ATK:{stats.AttackDamage}  VIT:{stats.Speed}  PORT:{stats.AttackRange}",
				HorizontalAlignment = HorizontalAlignment.Center
			};
			statsLabel.AddThemeFontSizeOverride("font_size", 12);
			statsLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
			content.AddChild(statsLabel);

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

		HBoxContainer subTabRow = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		subTabRow.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(subTabRow);

		CreateChroniquesSubTab(subTabRow, "global", "Global");
		CreateChroniquesSubTab(subTabRow, "personnage", "Par Perso.");
		CreateChroniquesSubTab(subTabRow, "nuits", "Par Nuits");

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

		Label desc = new()
		{
			Text = "Mutateurs de difficulté",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		desc.AddThemeFontSizeOverride("font_size", 16);
		desc.AddThemeColorOverride("font_color", TextDim);
		vbox.AddChild(desc);

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
			Label mutMultLabel = new()
			{
				Text = $"x{mutator.ScoreMultiplier:F2}"
			};
			mutMultLabel.AddThemeFontSizeOverride("font_size", 14);
			mutMultLabel.AddThemeColorOverride("font_color", isActive ? GoldBright : TextDim);
			header.AddChild(mutMultLabel);
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
		UpdateSubScreenVoidButton();
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
		UpdateSubScreenVestigesDisplay();
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
	}

	private void UpdateVestigesDisplay()
	{
		int vestiges = MetaSaveManager.GetVestiges();
		_vestigesLabel.Text = $"{vestiges}";
	}

	private static Control CreateSpacer(int height)
	{
		return new Control { CustomMinimumSize = new Vector2(0, height) };
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

		sbt.TextureMarginLeft = left;
		sbt.TextureMarginTop = top;
		sbt.TextureMarginRight = right;
		sbt.TextureMarginBottom = bottom;

		sbt.ContentMarginLeft = left + 2;
		sbt.ContentMarginTop = top + 2;
		sbt.ContentMarginRight = right + 2;
		sbt.ContentMarginBottom = bottom + 2;

		return sbt;
	}
}
