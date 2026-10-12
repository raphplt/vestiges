using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Accueil entre les runs : un camp vivant où veillent les personnages débloqués.
/// Partir ouvre la sélection du personnage ; le camp reste un décor, les catalogues sont reliés. Le Campement dépense les
/// Vestiges en progression permanente (plan 36).
/// Espace de conception 1920×1080, au grain ×4 de la peinture du camp ; texte en Pixel Operator (retour du 12 octobre).
/// </summary>
public partial class HubScreen : Control
{
	private const float MenuLeft = 96f;

	private enum HubState { MainMenu, Characters, Camp, Quests, Collection }
	private HubState _currentState = HubState.MainMenu;

	private static readonly Color Night = new(0.04f, 0.04f, 0.08f);
	private static readonly Color Outline = new(0.03f, 0.03f, 0.06f, 0.9f);

	private string _selectedCharacterId;
	private Font _bodyFont;
	private Font _strongFont;
	private HubBackdrop _backdrop;
	private HubCamp _camp;
	private Control _mainMenuLayer;
	private HubMenuButton _enterVoidButton;
	private HubCollectionPanel _collectionPanel;
	private HubQuestsPanel _questsPanel;
	private HubCharactersPanel _charactersPanel;
	private HubCampUpgradesPanel _campPanel;
	private string _collectionFocus;
	private string _characterFocus;
	private string _questFocus;
	private string _collectionKind = "weapons";
	private readonly Stack<(HubState State, string Focus, string Tab)> _returnStack = new();
	/// <summary>Pages ouvertes depuis le menu : calque, bouton de retour et bouton du menu qui retrouve le focus.</summary>
	private readonly Dictionary<HubState, (Control Layer, Button Back, HubMenuButton Opener)> _pages = new();
	private readonly List<HubMenuButton> _menuButtons = new();
	private readonly Dictionary<HubState, HubMenuButton> _openers = new();
	private Label _vestigesLabel;
	private LineEdit _seedInput;
	private SettingsScreen _settingsScreen;
	private VoidTransition _voidTransition;
	private bool _departing;

	public override void _Ready()
	{
		CharacterDataLoader.Load();
		WeaponDataLoader.Load();
		PerkDataLoader.Load();
		QuestDataLoader.Load();
		SouvenirDataLoader.Load();
		RunHistoryManager.Load();
		MetaSaveManager.Load();
		string repairError = RunSettlement.RepairPendingHistory();

		GameManager gm = GetNode<GameManager>("/root/GameManager");
		gm.ChangeState(GameManager.GameState.Hub);
		_selectedCharacterId = gm.SelectedCharacterId;
		if (!string.IsNullOrEmpty(_selectedCharacterId) && !MetaSaveManager.IsCharacterUnlocked(_selectedCharacterId))
			_selectedCharacterId = null;
		EnsureDefaultCharacterSelection(gm);

		AudioManager.Instance?.RefreshMusic();
		AudioManager.Instance?.PlayHubAmbiance();

		_bodyFont = UITheme.BodyFont;
		_strongFont = UITheme.StrongFont;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		BuildUI();
		DevelopmentBadge.AttachTo(this);

		UpdateVestigesDisplay();
		ShowSaveNotice(repairError.Length > 0);
		SetState(HubState.MainMenu);
		PlayIntro();
		if (gm.OpenCharacterSelection)
		{
			gm.OpenCharacterSelection = false;
			SetState(HubState.Characters);
		}

		if (gm.LastRunData != null)
			gm.LastRunData = null;
		// Depuis le bilan, « Voir dans la Collection » arrive directement sur l'arme débloquée.
		if (!string.IsNullOrEmpty(gm.CollectionFocusWeaponId))
		{
			_collectionFocus = gm.CollectionFocusWeaponId;
			gm.CollectionFocusWeaponId = null;
			SetState(HubState.Collection);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsSettingsOpen() || _departing)
			return;

		if (@event.IsActionPressed("ui_cancel") && _currentState != HubState.MainMenu)
		{
			AudioManager.PlayUI("sfx_menu_confirmer");
			ReturnToPrevious();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Retour de focus après les Paramètres ou un clic dans le vide : la manette doit toujours retrouver le menu.
		if (_currentState == HubState.MainMenu && GetViewport().GuiGetFocusOwner() == null
			&& (@event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_down") || @event.IsActionPressed("ui_accept")))
		{
			_enterVoidButton.GrabFocusSilently();
			GetViewport().SetInputAsHandled();
		}
	}

	// ================================================================
	// CONSTRUCTION
	// ================================================================

	private void BuildUI()
	{
		_backdrop = new HubBackdrop();
		AddChild(_backdrop);

		_mainMenuLayer = new Control { MouseFilter = MouseFilterEnum.Ignore };
		_mainMenuLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_mainMenuLayer);
		BuildTitle();
		BuildMenu();
		BuildVestiges();
		BuildRunOptions();

		_camp = new HubCamp();
		_backdrop.Scene.AddChild(_camp);
		List<CharacterData> characters = CharacterDataLoader.GetAll();
		_camp.Populate(characters, MetaSaveManager.IsCharacterUnlocked);

		BuildPages();

		_settingsScreen = new SettingsScreen();
		AddChild(_settingsScreen);
		_settingsScreen.Opened += () => ShowCurrentPage(false);
		_settingsScreen.Closed += () => ShowCurrentPage(true);
		_voidTransition = new VoidTransition();
		AddChild(_voidTransition);
	}

	private void BuildTitle()
	{
		Texture2D title = UITheme.LoadTex(UITheme.MenusPath + "ui_hub_title.png");
		if (title == null)
			return;

		Vector2 size = title.GetSize() * HubBackdrop.PixelScale;
		TextureRect titleRect = new()
		{
			Name = "Title",
			Texture = title,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			TextureFilter = TextureFilterEnum.Nearest,
			Position = new Vector2(MenuLeft - 16f, 76f),
			Size = size,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_mainMenuLayer.AddChild(titleRect);

		// Les dernières lettres continuent de s'effriter vers la droite.
		CpuParticles2D flakes = new()
		{
			Position = titleRect.Position + new Vector2(size.X * 0.78f, size.Y * 0.62f),
			Amount = 14,
			Lifetime = 2.6,
			Preprocess = 3.0,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
			EmissionRectExtents = new Vector2(size.X * 0.14f, size.Y * 0.2f),
			Direction = new Vector2(1f, -0.6f),
			Spread = 20f,
			Gravity = Vector2.Zero,
			InitialVelocityMin = 14f,
			InitialVelocityMax = 40f,
			ScaleAmountMin = HubBackdrop.PixelScale,
			ScaleAmountMax = HubBackdrop.PixelScale,
			TextureFilter = TextureFilterEnum.Nearest,
			ColorRamp = new Gradient
			{
				Offsets = new[] { 0f, 0.15f, 1f },
				Colors = new[] { new Color(0.96f, 0.94f, 0.92f, 0f), new Color(0.96f, 0.9f, 0.76f, 0.9f), new Color(0.96f, 0.94f, 0.92f, 0f) }
			}
		};
		Image pixel = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
		pixel.Fill(Colors.White);
		flakes.Texture = ImageTexture.CreateFromImage(pixel);
		_mainMenuLayer.AddChild(flakes);

		Label motto = CreateLabel("Le monde s'oublie. Jusqu'où irez-vous ?", TextRole.Heading, UITheme.TextColor, false);
		motto.Position = new Vector2(MenuLeft, titleRect.Position.Y + size.Y + 4f);
		_mainMenuLayer.AddChild(motto);
	}

	private void BuildMenu()
	{
		VBoxContainer menu = new() { Name = "Menu", Position = new Vector2(MenuLeft - 40f, 420f) };
		menu.AddThemeConstantOverride("separation", 4);
		_mainMenuLayer.AddChild(menu);

		_enterVoidButton = AddMenuButton(menu, "Partir", TextRole.Display, UITheme.TitleFont, () => OpenPage(HubState.Characters));
		menu.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 20f), MouseFilter = MouseFilterEnum.Ignore });
		_openers[HubState.Characters] = _enterVoidButton;
		_openers[HubState.Camp] = AddMenuButton(menu, Tr("UI_CAMP_TITLE"), TextRole.Title, UITheme.TitleFont, () => OpenPage(HubState.Camp));
		_openers[HubState.Quests] = AddMenuButton(menu, "Quêtes", TextRole.Title, UITheme.TitleFont, () => OpenPage(HubState.Quests));
		_openers[HubState.Collection] = AddMenuButton(menu, "Collection", TextRole.Title, UITheme.TitleFont, () => OpenPage(HubState.Collection));
		HBoxContainer utilities = new() { Position = new Vector2(MenuLeft - 40, 970) };
		utilities.AddThemeConstantOverride("separation", 16);
		_mainMenuLayer.AddChild(utilities);
		AddMenuButton(utilities, "Paramètres", TextRole.Heading, UITheme.TitleFont, () =>
		{
			AudioManager.PlayUI("sfx_menu_confirmer");
			_settingsScreen?.Open();
		});
		AddMenuButton(utilities, "Quitter", TextRole.Heading, UITheme.TitleFont, () => _ = GameExit.QuitAsync(GetTree()));
	}

	private HubMenuButton AddMenuButton(BoxContainer menu, string text, TextRole role, Font font, System.Action onPressed)
	{
		HubMenuButton button = new();
		button.Setup(text, role, font);
		if (menu is HBoxContainer) button.CustomMinimumSize = new Vector2(210, 48);
		button.Pressed += onPressed;
		menu.AddChild(button);
		_menuButtons.Add(button);
		return button;
	}

	private void BuildVestiges()
	{
		HBoxContainer row = new() { Name = "Vestiges", MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
		row.AddThemeConstantOverride("separation", 12);
		row.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
		row.OffsetLeft = -360f;
		row.OffsetRight = -64f;
		row.OffsetTop = 60f;
		row.OffsetBottom = 108f;
		_mainMenuLayer.AddChild(row);

		Texture2D icon = UITheme.LoadTex(UITheme.IconsPath + "ui_icon_vestiges.png");
		if (icon != null)
		{
			row.AddChild(new TextureRect
			{
				Texture = icon,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = new Vector2(32f, 32f),
				TextureFilter = TextureFilterEnum.Nearest,
				SizeFlagsVertical = SizeFlags.ShrinkCenter,
				MouseFilter = MouseFilterEnum.Ignore
			});
		}
		_vestigesLabel = CreateLabel("0", TextRole.Title, UITheme.GoldBright, true);
		_vestigesLabel.TooltipText = Tr("UI_VESTIGES_HINT");
		_vestigesLabel.MouseFilter = MouseFilterEnum.Pass;
		row.AddChild(_vestigesLabel);
	}

	/// <summary>Réglages de départ discrets en bas à gauche : graine de la run, et bascule du profil dev en éditeur.</summary>
	private void BuildRunOptions()
	{
		VBoxContainer options = new() { Name = "RunOptions", MouseFilter = MouseFilterEnum.Ignore };
		options.AddThemeConstantOverride("separation", 8);
		options.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
		options.GrowVertical = GrowDirection.Begin;
		options.OffsetLeft = 1450;
		options.OffsetBottom = -56f;
		options.OffsetTop = -56f;
		_mainMenuLayer.AddChild(options);

#if TOOLS
		if (DevelopmentMode.IsAvailable)
		{
			CheckButton developmentToggle = new()
			{
				Name = "DevelopmentToggle",
				Text = Tr("UI_DEV_TOGGLE"),
				TooltipText = Tr("UI_DEV_TOGGLE_HINT"),
				ButtonPressed = DevelopmentMode.IsEnabled,
				FocusMode = FocusModeEnum.None
			};
			developmentToggle.AddThemeFontOverride("font", _bodyFont);
			UITheme.SetTextRole(developmentToggle, TextRole.Lead);
			developmentToggle.AddThemeColorOverride("font_color", UITheme.TextDim);
			developmentToggle.Toggled += OnDevelopmentModeToggled;
			options.AddChild(developmentToggle);
		}
#endif

		HBoxContainer seedRow = new() { MouseFilter = MouseFilterEnum.Ignore };
		seedRow.AddThemeConstantOverride("separation", 12);
		options.AddChild(seedRow);
		Label seedLabel = CreateLabel("Graine", TextRole.Lead, UITheme.TextDim, false);
		seedLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		seedRow.AddChild(seedLabel);

		_seedInput = new LineEdit
		{
			PlaceholderText = "aléatoire",
			CustomMinimumSize = new Vector2(200f, 32f),
			FocusMode = FocusModeEnum.Click
		};
		_seedInput.AddThemeFontOverride("font", _bodyFont);
		UITheme.SetTextRole(_seedInput, TextRole.Lead);
		_seedInput.AddThemeColorOverride("font_color", UITheme.TextColor);
		_seedInput.AddThemeColorOverride("font_placeholder_color", UITheme.TextVeryDim);
		StyleBoxFlat seedStyle = new()
		{
			BgColor = new Color(Night, 0.6f),
			BorderColor = new Color(UITheme.GoldDim, 0.5f),
			BorderWidthBottom = 4,
			ContentMarginLeft = 8f,
			ContentMarginRight = 8f
		};
		_seedInput.AddThemeStyleboxOverride("normal", seedStyle);
		_seedInput.AddThemeStyleboxOverride("focus", seedStyle);
		_seedInput.TextSubmitted += _ => _enterVoidButton.GrabFocusSilently();
		seedRow.AddChild(_seedInput);
	}

	/// <summary>
	/// Pages du menu (plan 06 §9.10 pour Personnages et Quêtes) : même voile, même en-tête, même retour ; chacune garde
	/// les marges de son contenu.
	/// </summary>
	private void BuildPages()
	{
		_charactersPanel = new HubCharactersPanel();
		AddPage(HubState.Characters, "Choisir un personnage", _charactersPanel);
		_charactersPanel.DepartRequested += StartRun;
		_charactersPanel.QuestRequested += OpenQuest;
		_charactersPanel.WeaponRequested += id => OpenReward((int)UnlockKind.Weapon, id);
		_campPanel = new HubCampUpgradesPanel();
		AddPage(HubState.Camp, Tr("UI_CAMP_TITLE"), _campPanel);
		_questsPanel = new HubQuestsPanel();
		AddPage(HubState.Quests, "Quêtes", _questsPanel);
		_questsPanel.RewardRequested += OpenReward;
		_collectionPanel = new HubCollectionPanel();
		AddPage(HubState.Collection, "Collection", _collectionPanel);
		_collectionPanel.QuestRequested += OpenQuest;
	}

	private void AddPage(HubState state, string title, Control panel)
	{
		Control layer = new() { Visible = false };
		layer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(layer);
		bool camp = state == HubState.Camp;
		MenuSkin.AddBackdrop(layer, brightness: camp ? 0.52f : 1f);
		// Un même en-tête aligne le titre, l'action de retour et les bords du contenu.
		HBoxContainer header = new() { Name = "PageHeader" };
		header.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
		header.OffsetLeft = 132f;
		header.OffsetRight = -132f;
		header.OffsetTop = 32f;
		header.OffsetBottom = 96f;
		header.AddThemeConstantOverride("separation", 24);
		layer.AddChild(header);
		Label heading = CreateLabel(title, TextRole.Title, UITheme.GoldBright, true);
		heading.Name = "PageTitle";
		heading.AddThemeFontOverride("font", UITheme.TitleFont);
		heading.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		heading.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		header.AddChild(heading);
		Button backButton = MenuSkin.Button(Tr("UI_MENU_BACK"));
		backButton.Name = "PageBack";
		backButton.CustomMinimumSize = new Vector2(220f, 56f);
		backButton.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		UITheme.ApplyTypography(backButton, TextRole.Heading);
		backButton.Pressed += () =>
		{
			AudioManager.PlayUI("sfx_menu_confirmer");
			ReturnToPrevious();
		};
		header.AddChild(backButton);
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		panel.OffsetLeft = 132f;
		panel.OffsetRight = -132f;
		panel.OffsetTop = camp ? 108f : 120f;
		panel.OffsetBottom = -88f;
		layer.AddChild(panel);
		_pages[state] = (layer, backButton, _openers.GetValueOrDefault(state));
	}

	private void OpenPage(HubState state)
	{
		_returnStack.Clear();
		AudioManager.PlayUI("sfx_menu_confirmer");
		SetState(state);
	}

	private Label CreateLabel(string text, TextRole role, Color color, bool bold)
	{
		Label label = new() { Text = text, MouseFilter = MouseFilterEnum.Ignore };
		label.AddThemeFontOverride("font", bold ? _strongFont : _bodyFont);
		UITheme.SetTextRole(label, role);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeConstantOverride("outline_size", role >= TextRole.Title ? 8 : 5);
		label.AddThemeColorOverride("font_outline_color", Outline);
		return label;
	}

	// ================================================================
	// ÉTATS ET ANIMATIONS
	// ================================================================

	private void SetState(HubState state)
	{
		HubState from = _currentState;
		_currentState = state;
		ShowCurrentPage(true);

		if (state != HubState.MainMenu)
		{
			Control first = state switch
			{
				HubState.Characters => _charactersPanel.Refresh(_characterFocus ?? _charactersPanel.ExaminedCharacterId ?? _selectedCharacterId),
				HubState.Camp => _campPanel.Refresh(),
				HubState.Quests => _questsPanel.Refresh(_questFocus),
				HubState.Collection => _collectionPanel.Refresh(_collectionFocus, _collectionKind),
				_ => null,
			};
			_collectionFocus = _characterFocus = _questFocus = null;
			if (first != null)
				first.GrabFocus();
			else
				_pages[state].Back.GrabFocus();
			return;
		}

		UpdateVestigesDisplay();
		if (_pages.TryGetValue(from, out (Control Layer, Button Back, HubMenuButton Opener) previous))
			previous.Opener?.GrabFocusSilently();
	}

	/// <summary>Le camp sort du noir, puis le titre et le menu arrivent l'un après l'autre.</summary>
	private void PlayIntro()
	{
		ColorRect curtain = new() { Color = Night, MouseFilter = MouseFilterEnum.Ignore };
		curtain.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(curtain);
		MoveChild(curtain, _backdrop.GetIndex() + 1);
		Tween fade = curtain.CreateTween();
		fade.TweenProperty(curtain, "color:a", 0f, 1.1f).SetTrans(Tween.TransitionType.Sine);
		fade.TweenCallback(Callable.From(curtain.QueueFree));

		float delay = 0.35f;
		foreach (Node child in _mainMenuLayer.GetChildren())
		{
			if (child is not CanvasItem item)
				continue;
			item.Modulate = new Color(1f, 1f, 1f, 0f);
			Tween reveal = item.CreateTween();
			reveal.TweenInterval(delay);
			reveal.TweenProperty(item, "modulate:a", 1f, 0.5f);
			delay += 0.08f;
		}

		_enterVoidButton.GrabFocusSilently();
	}

	private void OpenQuest(string questId)
	{
		PushOrigin();
		_questFocus = questId;
		SetState(HubState.Quests);
	}

	private void OpenReward(int kind, string id)
	{
		PushOrigin();
		if ((UnlockKind)kind == UnlockKind.Character)
		{
			_characterFocus = id;
			SetState(HubState.Characters);
		}
		else
		{
			_collectionFocus = id;
			_collectionKind = (UnlockKind)kind switch
			{
				UnlockKind.Object => "passives",
				UnlockKind.Badge => "badges",
				UnlockKind.Reminiscence => "perks",
				_ => "weapons",
			};
			SetState(HubState.Collection);
		}
	}

	private void PushOrigin()
	{
		string focus = _currentState switch
		{
			HubState.Characters => _charactersPanel.ExaminedCharacterId,
			HubState.Quests => _questsPanel.ExaminedQuestId,
			HubState.Collection => _collectionPanel.ExaminedEntryId,
			_ => null,
		};
		// La pile est bornée, mais le retour garde toujours la visite la plus récente.
		if (_returnStack.Count == 8)
		{
			(HubState State, string Focus, string Tab)[] recent = _returnStack.ToArray();
			_returnStack.Clear();
			for (int i = recent.Length - 2; i >= 0; i--) _returnStack.Push(recent[i]);
		}
		_returnStack.Push((_currentState, focus, _collectionPanel.CurrentTab));
	}

	private void ReturnToPrevious()
	{
		if (_returnStack.Count == 0) { SetState(HubState.MainMenu); return; }
		(HubState state, string focus, string tab) = _returnStack.Pop();
		_collectionKind = tab;
		if (state == HubState.Characters) _characterFocus = focus;
		if (state == HubState.Quests) _questFocus = focus;
		if (state == HubState.Collection) _collectionFocus = focus;
		SetState(state);
	}

	private void StartRun(string characterId)
	{
		if (_departing || !MetaSaveManager.IsCharacterUnlocked(characterId)) return;
		_departing = true;
		_selectedCharacterId = characterId;
		AudioManager.PlayUI("sfx_menu_confirmer");
		GameManager gm = GetNode<GameManager>("/root/GameManager");
		gm.SelectedCharacterId = characterId;
		gm.ActiveMutators = new List<string>();
		string seedText = _seedInput.Text.StripEdges();
		gm.RunSeed = ulong.TryParse(seedText, out ulong seed) ? seed : 0;
		GD.Print($"[Hub] Entering Void: {characterId}, seed={gm.RunSeed}");
		_voidTransition.Play(() =>
		{
			gm.ChangeState(GameManager.GameState.Run);
			GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
		});
	}

#if TOOLS
	private void OnDevelopmentModeToggled(bool enabled)
	{
		if (!DevelopmentMode.SetEnabled(enabled))
		{
			(FindChild("DevelopmentToggle", true, false) as CheckButton)?.SetPressedNoSignal(DevelopmentMode.IsEnabled);
			return;
		}
		GameManager manager = GetNode<GameManager>("/root/GameManager");
		manager.SelectedCharacterId = null;
		manager.LastRunData = null;
		manager.LastRunSaveError = "";
		manager.LastRunHistoryPending = false;
		manager.LastVestigesEarned = 0;
		manager.ActiveMutators.Clear();
		manager.ChangeState(GameManager.GameState.Hub);
		GetTree().ReloadCurrentScene();
	}
#endif

	// ================================================================
	// AIDES
	// ================================================================

	private void ShowCurrentPage(bool show)
	{
		bool showCamp = show && _currentState == HubState.MainMenu;
		_mainMenuLayer.Visible = showCamp;
		_mainMenuLayer.ProcessMode = showCamp ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
		_backdrop.Visible = showCamp;
		_backdrop.ProcessMode = showCamp ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
		foreach ((HubState state, (Control Layer, Button Back, HubMenuButton Opener) page) in _pages)
			page.Layer.Visible = show && state == _currentState;
	}

	private bool IsSettingsOpen() => _settingsScreen != null && _settingsScreen.IsOpen;

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

	/// <summary>Prévient sous les Vestiges quand le profil n'a pas été lu ou écrit normalement (plan 26 Q2b).</summary>
	private void ShowSaveNotice(bool writeFailed)
	{
		string key = WorstLoadStatus() switch
		{
			SaveFile.ReadStatus.FutureVersion => "UI_SAVE_NOTICE_FUTURE",
			SaveFile.ReadStatus.Inaccessible => "UI_SAVE_NOTICE_LOCKED",
			SaveFile.ReadStatus.Unreadable => "UI_SAVE_NOTICE_RESET",
			SaveFile.ReadStatus.Recovered => "UI_SAVE_NOTICE_RECOVERED",
			_ => writeFailed ? "UI_SAVE_NOTICE_WRITE" : ""
		};
		if (key.Length == 0)
			return;

		Label notice = CreateLabel(Tr(key), TextRole.Small, UITheme.GoldBright, false);
		notice.Name = "SaveNotice";
		notice.AddThemeConstantOverride("outline_size", 6);
		notice.AddThemeColorOverride("font_outline_color", UITheme.OutlineColor);
		notice.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
		notice.OffsetLeft = -900f;
		notice.OffsetRight = -64f;
		notice.OffsetTop = 112f;
		notice.OffsetBottom = 140f;
		notice.HorizontalAlignment = HorizontalAlignment.Right;
		_mainMenuLayer.AddChild(notice);
	}

	private static SaveFile.ReadStatus WorstLoadStatus()
	{
		SaveFile.ReadStatus[] order =
		{
			SaveFile.ReadStatus.FutureVersion, SaveFile.ReadStatus.Inaccessible,
			SaveFile.ReadStatus.Unreadable, SaveFile.ReadStatus.Recovered
		};
		foreach (SaveFile.ReadStatus status in order)
		{
			if (MetaSaveManager.LoadStatus == status || RunHistoryManager.LoadStatus == status)
				return status;
		}
		return SaveFile.ReadStatus.Loaded;
	}

	private void UpdateVestigesDisplay()
	{
		_vestigesLabel.Text = MetaSaveManager.GetVestiges().ToString("N0", PlayerSheet.French);
	}
}
