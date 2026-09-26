using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Accueil entre les runs : le camp du Foyer, vivant, où veillent les personnages.
/// Menu textuel à gauche, personnage choisi au feu (◀ ▶ ou clic pour changer), Chroniques en surimpression.
/// Espace de conception 1920×1080, au grain ×4 de la peinture du camp ; texte en Saira (lisibilité, retour du 24 septembre).
/// </summary>
public partial class HubScreen : Control
{
	private const string BodyFontPath = "res://assets/fonts/saira/SairaSemiCondensed-Medium.ttf";
	private const string StrongFontPath = "res://assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf";
	private const float MenuLeft = 96f;

	private enum HubState { MainMenu, Chroniques }
	private HubState _currentState = HubState.MainMenu;

	private static readonly Color NameColor = new(0.91f, 0.88f, 0.83f);
	private static readonly Color Tagline = new(0.66f, 0.62f, 0.58f);
	private static readonly Color Locked = new(0.62f, 0.62f, 0.68f);
	private static readonly Color Night = new(0.04f, 0.04f, 0.08f);
	private static readonly Color Outline = new(0.03f, 0.03f, 0.06f, 0.9f);

	private string _selectedCharacterId;
	private Font _bodyFont;
	private Font _strongFont;
	private HubBackdrop _backdrop;
	private HubCamp _camp;
	private Control _mainMenuLayer;
	private Control _chroniquesLayer;
	private HubChroniquesPanel _chroniquesPanel;
	private HubMenuButton _enterVoidButton;
	private HubMenuButton _chroniquesButton;
	private HubMenuButton _chroniquesBackButton;
	private readonly List<HubMenuButton> _menuButtons = new();
	private Label _nameLabel;
	private Label _taglineLabel;
	private Control _nameplate;
	private Label _vestigesLabel;
	private LineEdit _seedInput;
	private SettingsScreen _settingsScreen;
	private VoidTransition _voidTransition;
	private bool _departing;
	private Tween _nameplateTween;
	private ulong _lastCycleMsec;

	public override void _Ready()
	{
		CharacterDataLoader.Load();
		WeaponDataLoader.Load();
		PerkDataLoader.Load();
		QuestDataLoader.Load();
		SouvenirDataLoader.Load();
		RunHistoryManager.Load();
		MetaSaveManager.Load();
		QuestManager.ResolvePendingProgressionQuests();

		GameManager gm = GetNode<GameManager>("/root/GameManager");
		_selectedCharacterId = gm.SelectedCharacterId;
		if (!string.IsNullOrEmpty(_selectedCharacterId) && !MetaSaveManager.IsCharacterUnlocked(_selectedCharacterId))
			_selectedCharacterId = null;
		EnsureDefaultCharacterSelection(gm);

		AudioManager.Instance?.PlayHubMusic();

		_bodyFont = GD.Load<Font>(BodyFontPath);
		_strongFont = GD.Load<Font>(StrongFontPath);
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		BuildUI();
		DevelopmentBadge.AttachTo(this);

		UpdateVestigesDisplay();
		SetState(HubState.MainMenu);
		PlayIntro();

		if (gm.LastRunData != null)
			gm.LastRunData = null;
		if (gm.LastQuestCompletions != null)
			gm.LastQuestCompletions = null;
	}

	public override void _Input(InputEvent @event)
	{
		// Avant la navigation de focus de l'interface : gauche/droite changent de personnage, pas de bouton.
		if (_currentState != HubState.MainMenu || _departing || IsSettingsOpen() || GetViewport().GuiGetFocusOwner() is LineEdit)
			return;
		if (@event.IsActionPressed("ui_left"))
			CycleCharacter(-1);
		else if (@event.IsActionPressed("ui_right"))
			CycleCharacter(1);
		else
			return;
		GetViewport().SetInputAsHandled();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsSettingsOpen() || _departing)
			return;

		if (@event.IsActionPressed("ui_cancel") && _currentState == HubState.Chroniques)
		{
			AudioManager.PlayUI("sfx_menu_confirmer");
			SetState(HubState.MainMenu);
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
		BuildNameplate();
		BuildVestiges();
		BuildRunOptions();

		_camp = new HubCamp();
		_backdrop.Scene.AddChild(_camp);
		_camp.SelectionChanged += OnCharacterSelected;
		List<CharacterData> characters = CharacterDataLoader.GetAll();
		_camp.Populate(characters, MetaSaveManager.IsCharacterUnlocked, _selectedCharacterId);

		BuildChroniques();

		_settingsScreen = new SettingsScreen();
		AddChild(_settingsScreen);
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

		Label motto = CreateLabel("Le monde s'oublie. Jusqu'où irez-vous ?", 22, UITheme.TextColor, false);
		motto.Position = new Vector2(MenuLeft, titleRect.Position.Y + size.Y + 4f);
		_mainMenuLayer.AddChild(motto);
	}

	private void BuildMenu()
	{
		VBoxContainer menu = new() { Name = "Menu", Position = new Vector2(MenuLeft - 40f, 420f) };
		menu.AddThemeConstantOverride("separation", 4);
		_mainMenuLayer.AddChild(menu);

		_enterVoidButton = AddMenuButton(menu, "Partir", 46, _strongFont, OnEnterVoidPressed);
		menu.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 20f), MouseFilter = MouseFilterEnum.Ignore });
		_chroniquesButton = AddMenuButton(menu, "Chroniques", 30, _bodyFont, OpenChroniques);
		AddMenuButton(menu, "Paramètres", 30, _bodyFont, () =>
		{
			AudioManager.PlayUI("sfx_menu_confirmer");
			_settingsScreen?.Open();
		});
		AddMenuButton(menu, "Quitter", 30, _bodyFont, () => GetTree().Quit());
	}

	private HubMenuButton AddMenuButton(VBoxContainer menu, string text, int fontSize, Font font, System.Action onPressed)
	{
		HubMenuButton button = new();
		button.Setup(text, fontSize, font);
		button.Pressed += onPressed;
		menu.AddChild(button);
		_menuButtons.Add(button);
		return button;
	}

	/// <summary>Plaque sous le camp : le nom et une phrase, rien d'autre. Les statistiques restent en jeu.</summary>
	private void BuildNameplate()
	{
		_nameplate = new VBoxContainer { Name = "Nameplate", MouseFilter = MouseFilterEnum.Ignore };
		_nameplate.AddThemeConstantOverride("separation", 0);
		_nameplate.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
		_nameplate.OffsetLeft = -420f;
		_nameplate.OffsetRight = 420f;
		_nameplate.OffsetTop = -150f;
		_nameplate.OffsetBottom = -54f;
		_mainMenuLayer.AddChild(_nameplate);

		HBoxContainer row = new() { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 28);
		_nameplate.AddChild(row);
		row.AddChild(CreateArrow("<", -1));
		_nameLabel = CreateLabel("", 44, NameColor, true);
		_nameLabel.CustomMinimumSize = new Vector2(520f, 0f);
		_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		row.AddChild(_nameLabel);
		row.AddChild(CreateArrow(">", 1));

		_taglineLabel = CreateLabel("", 24, Tagline, false);
		_taglineLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_nameplate.AddChild(_taglineLabel);
	}

	private Button CreateArrow(string glyph, int step)
	{
		Button arrow = new()
		{
			Text = glyph,
			Flat = true,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			CustomMinimumSize = new Vector2(48f, 48f)
		};
		arrow.AddThemeFontOverride("font", _strongFont);
		arrow.AddThemeFontSizeOverride("font_size", 48);
		arrow.AddThemeColorOverride("font_color", UITheme.GoldDim);
		arrow.AddThemeColorOverride("font_hover_color", UITheme.GoldBright);
		arrow.AddThemeColorOverride("font_pressed_color", UITheme.GoldBright);
		arrow.AddThemeConstantOverride("outline_size", 8);
		arrow.AddThemeColorOverride("font_outline_color", Outline);
		StyleBoxEmpty empty = new();
		foreach (string state in new[] { "normal", "hover", "pressed", "focus", "hover_pressed" })
			arrow.AddThemeStyleboxOverride(state, empty);
		arrow.Pressed += () => CycleCharacter(step);
		return arrow;
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
		_vestigesLabel = CreateLabel("0", 30, UITheme.GoldBright, true);
		_vestigesLabel.TooltipText = "Vestiges : monnaie des déblocages permanents";
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
		options.OffsetLeft = MenuLeft;
		options.OffsetBottom = -40f;
		options.OffsetTop = -40f;
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
			developmentToggle.AddThemeFontSizeOverride("font_size", 18);
			developmentToggle.AddThemeColorOverride("font_color", UITheme.TextDim);
			developmentToggle.Toggled += OnDevelopmentModeToggled;
			options.AddChild(developmentToggle);
		}
#endif

		HBoxContainer seedRow = new() { MouseFilter = MouseFilterEnum.Ignore };
		seedRow.AddThemeConstantOverride("separation", 12);
		options.AddChild(seedRow);
		Label seedLabel = CreateLabel("Graine", 18, UITheme.TextDim, false);
		seedLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		seedRow.AddChild(seedLabel);

		_seedInput = new LineEdit
		{
			PlaceholderText = "aléatoire",
			CustomMinimumSize = new Vector2(200f, 32f),
			FocusMode = FocusModeEnum.Click
		};
		_seedInput.AddThemeFontOverride("font", _bodyFont);
		_seedInput.AddThemeFontSizeOverride("font_size", 18);
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

	private void BuildChroniques()
	{
		_chroniquesLayer = new Control { Visible = false };
		_chroniquesLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_chroniquesLayer);

		ColorRect veil = new() { Color = new Color(Night, 0.82f) };
		veil.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_chroniquesLayer.AddChild(veil);

		Label header = CreateLabel("Chroniques", 46, UITheme.GoldColor, true);
		header.Position = new Vector2(MenuLeft, 72f);
		_chroniquesLayer.AddChild(header);

		VBoxContainer back = new() { Position = new Vector2(MenuLeft - 40f, 980f) };
		_chroniquesLayer.AddChild(back);
		_chroniquesBackButton = new HubMenuButton();
		_chroniquesBackButton.Setup("Retour", 30, _bodyFont);
		_chroniquesBackButton.Pressed += () =>
		{
			AudioManager.PlayUI("sfx_menu_confirmer");
			SetState(HubState.MainMenu);
		};
		back.AddChild(_chroniquesBackButton);

		_chroniquesPanel = new HubChroniquesPanel();
		_chroniquesPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_chroniquesPanel.OffsetLeft = 320f;
		_chroniquesPanel.OffsetRight = -320f;
		_chroniquesPanel.OffsetTop = 160f;
		_chroniquesPanel.OffsetBottom = -120f;
		_chroniquesLayer.AddChild(_chroniquesPanel);
	}

	private Label CreateLabel(string text, int size, Color color, bool bold)
	{
		Label label = new() { Text = text, MouseFilter = MouseFilterEnum.Ignore };
		label.AddThemeFontOverride("font", bold ? _strongFont : _bodyFont);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeConstantOverride("outline_size", size >= 30 ? 8 : 5);
		label.AddThemeColorOverride("font_outline_color", Outline);
		return label;
	}

	// ================================================================
	// ÉTATS ET ANIMATIONS
	// ================================================================

	private void SetState(HubState state)
	{
		bool fromChroniques = _currentState == HubState.Chroniques;
		_currentState = state;
		_mainMenuLayer.Visible = state == HubState.MainMenu;
		_chroniquesLayer.Visible = state == HubState.Chroniques;

		if (state == HubState.Chroniques)
		{
			_chroniquesPanel.Refresh(_selectedCharacterId);
			_chroniquesBackButton.GrabFocusSilently();
			return;
		}

		UpdateVestigesDisplay();
		if (fromChroniques)
			_chroniquesButton.GrabFocusSilently();
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

	private void OpenChroniques()
	{
		AudioManager.PlayUI("sfx_menu_confirmer");
		SetState(HubState.Chroniques);
	}

	private void CycleCharacter(int step)
	{
		// Un stick tenu émet un événement « pressé » à chaque mouvement : un pas toutes les 180 ms au plus.
		ulong now = Time.GetTicksMsec();
		if (now - _lastCycleMsec < 180)
			return;
		_lastCycleMsec = now;
		AudioManager.PlayUI("sfx_menu_clic");
		_camp.Cycle(step);
	}

	private void OnCharacterSelected(string characterId)
	{
		CharacterData data = CharacterDataLoader.Get(characterId);
		bool unlocked = MetaSaveManager.IsCharacterUnlocked(characterId);
		if (unlocked)
		{
			_selectedCharacterId = characterId;
			GetNode<GameManager>("/root/GameManager").SelectedCharacterId = characterId;
		}

		_enterVoidButton.Disabled = !unlocked;
		_enterVoidButton.QueueRedraw();
		_nameLabel.Text = unlocked ? data?.Name ?? characterId : "???";
		_nameLabel.AddThemeColorOverride("font_color", unlocked ? NameColor : Locked);
		_taglineLabel.Text = unlocked ? data?.Description ?? "" : GetUnlockConditionText(data?.UnlockCondition);

		_nameplateTween?.Kill();
		_nameplate.Modulate = new Color(1f, 1f, 1f, 0.2f);
		_nameplateTween = _nameplate.CreateTween();
		_nameplateTween.TweenProperty(_nameplate, "modulate:a", 1f, 0.25f);
	}

	private void OnEnterVoidPressed()
	{
		if (_departing || string.IsNullOrEmpty(_selectedCharacterId) || !_camp.IsSelectedUnlocked)
			return;

		_departing = true;
		AudioManager.PlayUI("sfx_menu_confirmer");
		foreach (HubMenuButton button in _menuButtons)
			button.Disabled = true;

		GameManager gm = GetNode<GameManager>("/root/GameManager");
		gm.SelectedCharacterId = _selectedCharacterId;
		gm.ActiveMutators = new List<string>();
		string seedText = _seedInput.Text.StripEdges();
		gm.RunSeed = !string.IsNullOrEmpty(seedText) && ulong.TryParse(seedText, out ulong seed) ? seed : 0;
		GD.Print($"[Hub] Entering Void: {_selectedCharacterId}, seed={gm.RunSeed}");

		// Le personnage quitte le feu, le menu s'éteint, puis le Vide se referme.
		_camp.Depart(1.1f);
		Tween leave = CreateTween();
		leave.TweenProperty(_mainMenuLayer, "modulate:a", 0f, 0.45f);
		leave.TweenInterval(0.2f);
		leave.TweenCallback(Callable.From(() => _voidTransition.Play(() =>
		{
			gm.ChangeState(GameManager.GameState.Run);
			GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
		})));
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
		manager.LastUnlocks = null;
		manager.LastQuestCompletions = null;
		manager.LastVestigesEarned = 0;
		manager.ActiveMutators.Clear();
		manager.ChangeState(GameManager.GameState.Hub);
		GetTree().ReloadCurrentScene();
	}
#endif

	// ================================================================
	// AIDES
	// ================================================================

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

	private void UpdateVestigesDisplay()
	{
		_vestigesLabel.Text = $"{MetaSaveManager.GetVestiges()}";
	}

	private static string GetUnlockConditionText(string condition)
	{
		return condition switch
		{
			"survive_3_nights" => "Tenir 12 minutes pour s'en souvenir",
			"kill_200_in_run" => "Abattre 200 créatures en une run pour s'en souvenir",
			_ => "Un souvenir encore effacé"
		};
	}
}
