using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Settings screen with tabbed navigation (Audio, Graphismes, Controles).
/// Pixel art NinePatch styling. Close button + ESC.
/// Usable both in-game (pause) and from the Hub via gear button.
/// </summary>
public partial class SettingsScreen : CanvasLayer
{
	public bool IsOpen { get; private set; }

	private Control _root;
	private Control _contentArea;
	private string _activeTab = "audio";
	private readonly System.Collections.Generic.Dictionary<string, Button> _tabButtons = new();

	// Textures NinePatch
	private Texture2D _panelTex;
	private Texture2D _tabNormalTex;
	private Texture2D _tabHoverTex;
	private Texture2D _tabActiveTex;
	private Texture2D _btnNormalTex;
	private Texture2D _btnHoverTex;
	private Texture2D _btnPressedTex;
	private Texture2D _btnDisabledTex;

	public override void _Ready()
	{
		Layer = 60;
		ProcessMode = ProcessModeEnum.Always;
		LoadTextures();
		BuildUI();
		_root.Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsOpen)
			return;

		if (@event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public void Open()
	{
		IsOpen = true;
		_root.Visible = true;
		ShowTab(_activeTab);
	}

	public void Close()
	{
		IsOpen = false;
		_root.Visible = false;
		AudioManager.Instance?.SaveSettings();
		VfxFactory.SaveSettings();
	}

	private void LoadTextures()
	{
		_panelTex = UITheme.LoadTex(UITheme.MenusPath + "ui_panel_frame.png");
		_tabNormalTex = UITheme.LoadTex(UITheme.MenusPath + "ui_tab_normal.png");
		_tabHoverTex = UITheme.LoadTex(UITheme.MenusPath + "ui_tab_hover.png");
		_tabActiveTex = UITheme.LoadTex(UITheme.MenusPath + "ui_tab_active.png");
		_btnNormalTex = UITheme.LoadTex(UITheme.MenusPath + "ui_button_normal.png");
		_btnHoverTex = UITheme.LoadTex(UITheme.MenusPath + "ui_button_hover.png");
		_btnPressedTex = UITheme.LoadTex(UITheme.MenusPath + "ui_button_pressed.png");
		_btnDisabledTex = UITheme.LoadTex(UITheme.MenusPath + "ui_button_disabled.png");
	}

	private void BuildUI()
	{
		_root = new Control();
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.ProcessMode = ProcessModeEnum.Always;
		AddChild(_root);

		// Dark overlay (cliquable pour fermer)
		ColorRect overlay = new();
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		overlay.Color = new Color(0.02f, 0.02f, 0.05f, 0.8f);
		overlay.GuiInput += (InputEvent @event) =>
		{
			if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
				Close();
		};
		_root.AddChild(overlay);

		// Main panel (centered, fixed size)
		PanelContainer mainPanel = new();
		mainPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		mainPanel.GrowHorizontal = Control.GrowDirection.Both;
		mainPanel.GrowVertical = Control.GrowDirection.Both;
		mainPanel.OffsetLeft = -360;
		mainPanel.OffsetRight = 360;
		mainPanel.OffsetTop = -280;
		mainPanel.OffsetBottom = 280;

		// NinePatch panel styling
		if (_panelTex != null)
		{
			StyleBoxTexture panelStyle = UITheme.CreateNinePatch(_panelTex, 4, 4, 4, 4);
			panelStyle.ContentMarginLeft = 8;
			panelStyle.ContentMarginRight = 8;
			panelStyle.ContentMarginTop = 8;
			panelStyle.ContentMarginBottom = 8;
			mainPanel.AddThemeStyleboxOverride("panel", panelStyle);
		}
		else
		{
			StyleBoxFlat fallback = new();
			fallback.BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
			fallback.SetCornerRadiusAll(0);
			fallback.SetBorderWidthAll(1);
			fallback.BorderColor = new Color(0.3f, 0.28f, 0.22f, 0.6f);
			mainPanel.AddThemeStyleboxOverride("panel", fallback);
		}
		_root.AddChild(mainPanel);

		VBoxContainer mainVBox = new();
		mainVBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		mainVBox.AddThemeConstantOverride("separation", 0);
		mainPanel.AddChild(mainVBox);

		// Title bar + close button
		mainVBox.AddChild(BuildTitleBar());

		// Tab bar
		mainVBox.AddChild(BuildTabBar());

		// Content area
		_contentArea = new Control
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		mainVBox.AddChild(_contentArea);

		// Bottom hint
		mainVBox.AddChild(BuildBottomHint());
	}

	private MarginContainer BuildTitleBar()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 20);
		margin.AddThemeConstantOverride("margin_top", 16);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_bottom", 8);

		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 0);
		margin.AddChild(row);

		// Spacer gauche
		Control leftSpace = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddChild(leftSpace);

		Label title = new()
		{
			Text = "PARAMETRES",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 22);
		title.AddThemeColorOverride("font_color", UITheme.GoldColor);
		row.AddChild(title);

		// Spacer droit
		Control rightSpace = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddChild(rightSpace);

		// Close button
		Button closeBtn = new()
		{
			Text = "X",
			CustomMinimumSize = new Vector2(36, 36),
			FocusMode = Control.FocusModeEnum.None
		};
		closeBtn.AddThemeFontSizeOverride("font_size", 18);
		UITheme.ApplyButtonStyle(closeBtn, _btnNormalTex, _btnHoverTex, _btnPressedTex, _btnDisabledTex);
		closeBtn.Pressed += Close;
		row.AddChild(closeBtn);

		return margin;
	}

	private HBoxContainer BuildTabBar()
	{
		HBoxContainer tabBar = new();
		tabBar.AddThemeConstantOverride("separation", 4);
		tabBar.CustomMinimumSize = new Vector2(0, 42);

		MarginContainer tabMargin = new();
		tabMargin.AddThemeConstantOverride("margin_left", 16);
		tabMargin.AddThemeConstantOverride("margin_right", 16);
		tabMargin.AddThemeConstantOverride("margin_top", 0);
		tabMargin.AddThemeConstantOverride("margin_bottom", 0);

		HBoxContainer innerBar = new();
		innerBar.AddThemeConstantOverride("separation", 4);
		tabMargin.AddChild(innerBar);

		AddTab(innerBar, "audio", "Audio");
		AddTab(innerBar, "graphismes", "Graphismes");
		AddTab(innerBar, "controles", "Controles");

		tabBar.AddChild(tabMargin);
		return tabBar;
	}

	private void AddTab(HBoxContainer tabBar, string id, string label)
	{
		Button btn = new()
		{
			Text = label,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 38),
			FocusMode = Control.FocusModeEnum.None,
			Flat = true
		};
		btn.AddThemeFontSizeOverride("font_size", 15);

		btn.Pressed += () => ShowTab(id);
		tabBar.AddChild(btn);
		_tabButtons[id] = btn;
	}

	private void ShowTab(string tabId)
	{
		_activeTab = tabId;

		// Update tab styles with NinePatch
		foreach (var (id, btn) in _tabButtons)
		{
			bool active = id == tabId;
			UITheme.ApplyTabStyle(btn, active, _tabNormalTex, _tabHoverTex, _tabActiveTex);
		}

		// Clear content
		foreach (Node child in _contentArea.GetChildren())
			child.QueueFree();

		// Build tab content
		Control content = tabId switch
		{
			"audio" => BuildAudioTab(),
			"graphismes" => BuildGraphicsTab(),
			"controles" => BuildControlsTab(),
			_ => new Control()
		};

		_contentArea.AddChild(content);
	}

	// ================================================================
	// AUDIO TAB
	// ================================================================
	private MarginContainer BuildAudioTab()
	{
		MarginContainer margin = new();
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 30);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_right", 30);
		margin.AddThemeConstantOverride("margin_bottom", 16);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 18);
		vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddChild(vbox);

		vbox.AddChild(BuildVolumeSlider("Volume global", "Master"));
		vbox.AddChild(BuildVolumeSlider("Musique", "Music"));
		vbox.AddChild(BuildVolumeSlider("Effets sonores", "SFX"));
		vbox.AddChild(BuildVolumeSlider("Ambiance", "Ambiance"));

		return margin;
	}

	private VBoxContainer BuildVolumeSlider(string label, string busName)
	{
		VBoxContainer container = new();
		container.AddThemeConstantOverride("separation", 4);

		HBoxContainer headerRow = new();
		headerRow.AddThemeConstantOverride("separation", 0);

		Label lbl = new()
		{
			Text = label,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		lbl.AddThemeFontSizeOverride("font_size", 15);
		lbl.AddThemeColorOverride("font_color", UITheme.TextColor);
		headerRow.AddChild(lbl);

		Label pct = new()
		{
			CustomMinimumSize = new Vector2(48, 0),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		pct.AddThemeFontSizeOverride("font_size", 15);
		pct.AddThemeColorOverride("font_color", UITheme.TextDim);
		headerRow.AddChild(pct);

		container.AddChild(headerRow);

		HSlider slider = new()
		{
			MinValue = 0.0,
			MaxValue = 1.0,
			Step = 0.01,
			CustomMinimumSize = new Vector2(0, 24),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		slider.ProcessMode = ProcessModeEnum.Always;

		// Style pixel art pour le slider
		StyleSlider(slider);

		if (AudioManager.Instance != null)
			slider.Value = AudioManager.Instance.GetBusVolumeLinear(busName);
		else
			slider.Value = 1.0;

		pct.Text = $"{(int)(slider.Value * 100)}%";

		slider.ValueChanged += (double v) =>
		{
			AudioManager.Instance?.SetBusVolumeLinear(busName, (float)v);
			pct.Text = $"{(int)(v * 100)}%";
		};

		container.AddChild(slider);

		return container;
	}

	private void StyleSlider(HSlider slider)
	{
		// Track : StyleBoxFlat pixel art (sombre, pas de radius)
		StyleBoxFlat track = new()
		{
			BgColor = new Color(0.1f, 0.1f, 0.15f),
			BorderColor = new Color(0.25f, 0.22f, 0.18f),
		};
		track.SetBorderWidthAll(1);
		track.SetCornerRadiusAll(0);
		track.ContentMarginTop = 4;
		track.ContentMarginBottom = 4;
		slider.AddThemeStyleboxOverride("slider", track);

		// Grabber area (rempli en gold discret)
		StyleBoxFlat grabberArea = new()
		{
			BgColor = new Color(UITheme.GoldColor, 0.25f)
		};
		grabberArea.SetCornerRadiusAll(0);
		slider.AddThemeStyleboxOverride("grabber_area", grabberArea);
		slider.AddThemeStyleboxOverride("grabber_area_highlight", grabberArea);

		// Grabber (carre gold, style pixel art)
		StyleBoxFlat grabber = new()
		{
			BgColor = UITheme.GoldColor,
			BorderColor = new Color(0.42f, 0.30f, 0.22f),
		};
		grabber.SetBorderWidthAll(1);
		grabber.SetCornerRadiusAll(0);
		grabber.ContentMarginLeft = 6;
		grabber.ContentMarginRight = 6;
		grabber.ContentMarginTop = 8;
		grabber.ContentMarginBottom = 8;
		slider.AddThemeStyleboxOverride("grabber", grabber);

		StyleBoxFlat grabberHl = new()
		{
			BgColor = UITheme.GoldBright,
			BorderColor = new Color(0.42f, 0.30f, 0.22f),
		};
		grabberHl.SetBorderWidthAll(1);
		grabberHl.SetCornerRadiusAll(0);
		grabberHl.ContentMarginLeft = 6;
		grabberHl.ContentMarginRight = 6;
		grabberHl.ContentMarginTop = 8;
		grabberHl.ContentMarginBottom = 8;
		slider.AddThemeStyleboxOverride("grabber_highlight", grabberHl);
	}

	// ================================================================
	// GRAPHICS TAB
	// ================================================================
	private MarginContainer BuildGraphicsTab()
	{
		MarginContainer margin = new();
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 30);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_right", 30);
		margin.AddThemeConstantOverride("margin_bottom", 16);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 16);
		vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddChild(vbox);

		// Fullscreen toggle
		vbox.AddChild(BuildToggleRow("Plein ecran",
			DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed,
			(toggled) =>
			{
				DisplayServer.WindowSetMode(toggled
					? DisplayServer.WindowMode.Fullscreen
					: DisplayServer.WindowMode.Windowed);
			}));

		// Particle level
		HBoxContainer particleRow = new();
		particleRow.AddThemeConstantOverride("separation", 0);

		Label particleLbl = new()
		{
			Text = "Particules",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		particleLbl.AddThemeFontSizeOverride("font_size", 15);
		particleLbl.AddThemeColorOverride("font_color", UITheme.TextColor);
		particleRow.AddChild(particleLbl);

		Button particleBtn = new()
		{
			Text = ParticleLevelLabel(VfxFactory.CurrentParticleLevel),
			CustomMinimumSize = new Vector2(160, 36),
			FocusMode = Control.FocusModeEnum.None
		};
		particleBtn.AddThemeFontSizeOverride("font_size", 14);
		UITheme.ApplyButtonStyle(particleBtn, _btnNormalTex, _btnHoverTex, _btnPressedTex, _btnDisabledTex);
		particleBtn.Pressed += () =>
		{
			VfxFactory.CurrentParticleLevel = VfxFactory.CurrentParticleLevel switch
			{
				ParticleLevel.Full => ParticleLevel.Reduced,
				ParticleLevel.Reduced => ParticleLevel.Off,
				_ => ParticleLevel.Full,
			};
			particleBtn.Text = ParticleLevelLabel(VfxFactory.CurrentParticleLevel);
		};
		particleRow.AddChild(particleBtn);
		vbox.AddChild(particleRow);

		return margin;
	}

	private HBoxContainer BuildToggleRow(string label, bool initialValue, System.Action<bool> onToggle)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 0);

		Label lbl = new()
		{
			Text = label,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		lbl.AddThemeFontSizeOverride("font_size", 15);
		lbl.AddThemeColorOverride("font_color", UITheme.TextColor);
		row.AddChild(lbl);

		// Button toggle pixel art (remplace CheckButton natif)
		Button toggle = new()
		{
			Text = initialValue ? "OUI" : "NON",
			ToggleMode = true,
			ButtonPressed = initialValue,
			CustomMinimumSize = new Vector2(80, 36),
			FocusMode = Control.FocusModeEnum.None
		};
		toggle.AddThemeFontSizeOverride("font_size", 14);
		UITheme.ApplyButtonStyle(toggle, _btnNormalTex, _btnHoverTex, _btnPressedTex, _btnDisabledTex);
		toggle.AddThemeColorOverride("font_color", initialValue ? UITheme.GoldColor : UITheme.TextDim);

		toggle.Toggled += (bool toggled) =>
		{
			toggle.Text = toggled ? "OUI" : "NON";
			toggle.AddThemeColorOverride("font_color", toggled ? UITheme.GoldColor : UITheme.TextDim);
			onToggle(toggled);
		};
		row.AddChild(toggle);

		return row;
	}

	// ================================================================
	// CONTROLS TAB (placeholder)
	// ================================================================
	private MarginContainer BuildControlsTab()
	{
		MarginContainer margin = new();
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 30);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_right", 30);
		margin.AddThemeConstantOverride("margin_bottom", 16);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 16);
		vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddChild(vbox);

		Label placeholder = new()
		{
			Text = "Configuration des controles a venir...",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		placeholder.AddThemeFontSizeOverride("font_size", 15);
		placeholder.AddThemeColorOverride("font_color", UITheme.TextDim);
		vbox.AddChild(placeholder);

		return margin;
	}

	// ================================================================
	// HELPERS
	// ================================================================
	private MarginContainer BuildBottomHint()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 20);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_right", 20);
		margin.AddThemeConstantOverride("margin_bottom", 12);

		Label hint = new()
		{
			Text = "[Echap] ou [X] Fermer",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		hint.AddThemeFontSizeOverride("font_size", 12);
		hint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
		margin.AddChild(hint);

		return margin;
	}

	private static string ParticleLevelLabel(ParticleLevel level)
	{
		return level switch
		{
			ParticleLevel.Full => "Toutes",
			ParticleLevel.Reduced => "Reduites",
			_ => "Desactivees",
		};
	}
}
