using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Settings screen with tabbed navigation (Audio, Graphismes, Controles).
/// Usable both in-game (pause) and from the Hub via gear button.
/// </summary>
public partial class SettingsScreen : CanvasLayer
{
	public bool IsOpen { get; private set; }

	private Control _root;
	private Control _contentArea;
	private string _activeTab = "audio";
	private readonly System.Collections.Generic.Dictionary<string, Button> _tabButtons = new();

	// Colors
	private static readonly Color GoldColor = new(0.83f, 0.66f, 0.26f);
	private static readonly Color TextColor = new(0.75f, 0.75f, 0.8f);
	private static readonly Color TextDim = new(0.5f, 0.5f, 0.55f);
	private static readonly Color BgOverlay = new(0.02f, 0.02f, 0.05f, 0.8f);
	private static readonly Color PanelBg = new(0.08f, 0.08f, 0.12f, 0.95f);
	private static readonly Color TabActiveBg = new(0.15f, 0.14f, 0.2f);
	private static readonly Color TabInactiveBg = new(0.06f, 0.06f, 0.1f);

	public override void _Ready()
	{
		Layer = 60;
		ProcessMode = ProcessModeEnum.Always;
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

	private void BuildUI()
	{
		_root = new Control();
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.ProcessMode = ProcessModeEnum.Always;
		AddChild(_root);

		// Dark overlay
		ColorRect overlay = new();
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		overlay.Color = BgOverlay;
		_root.AddChild(overlay);

		// Main panel (centered, fixed size)
		PanelContainer mainPanel = new();
		mainPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		mainPanel.GrowHorizontal = Control.GrowDirection.Both;
		mainPanel.GrowVertical = Control.GrowDirection.Both;
		mainPanel.OffsetLeft = -340;
		mainPanel.OffsetRight = 340;
		mainPanel.OffsetTop = -260;
		mainPanel.OffsetBottom = 260;

		StyleBoxFlat panelStyle = new();
		panelStyle.BgColor = PanelBg;
		panelStyle.SetCornerRadiusAll(4);
		panelStyle.SetBorderWidthAll(1);
		panelStyle.BorderColor = new Color(0.3f, 0.28f, 0.22f, 0.6f);
		mainPanel.AddThemeStyleboxOverride("panel", panelStyle);
		_root.AddChild(mainPanel);

		MarginContainer margin = new();
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 0);
		margin.AddThemeConstantOverride("margin_top", 0);
		margin.AddThemeConstantOverride("margin_right", 0);
		margin.AddThemeConstantOverride("margin_bottom", 0);
		mainPanel.AddChild(margin);

		VBoxContainer mainVBox = new();
		mainVBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		mainVBox.AddThemeConstantOverride("separation", 0);
		margin.AddChild(mainVBox);

		// Title bar
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
		margin.AddThemeConstantOverride("margin_right", 20);
		margin.AddThemeConstantOverride("margin_bottom", 8);

		Label title = new()
		{
			Text = "PARAMÈTRES",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 22);
		title.AddThemeColorOverride("font_color", GoldColor);
		margin.AddChild(title);

		return margin;
	}

	private HBoxContainer BuildTabBar()
	{
		HBoxContainer tabBar = new();
		tabBar.AddThemeConstantOverride("separation", 0);
		tabBar.CustomMinimumSize = new Vector2(0, 38);

		AddTab(tabBar, "audio", "Audio");
		AddTab(tabBar, "graphismes", "Graphismes");
		AddTab(tabBar, "controles", "Contrôles");

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
		btn.AddThemeFontSizeOverride("font_size", 14);

		btn.Pressed += () => ShowTab(id);
		tabBar.AddChild(btn);
		_tabButtons[id] = btn;
	}

	private void ShowTab(string tabId)
	{
		_activeTab = tabId;

		// Update tab button styles
		foreach (var (id, btn) in _tabButtons)
		{
			bool active = id == tabId;
			StyleBoxFlat style = new();
			style.BgColor = active ? TabActiveBg : TabInactiveBg;
			style.SetBorderWidthAll(0);
			style.BorderWidthBottom = active ? 2 : 0;
			style.BorderColor = GoldColor;
			btn.AddThemeStyleboxOverride("normal", style);
			btn.AddThemeStyleboxOverride("hover", style);
			btn.AddThemeStyleboxOverride("pressed", style);
			btn.AddThemeColorOverride("font_color", active ? GoldColor : TextDim);
			btn.AddThemeColorOverride("font_hover_color", active ? GoldColor : TextColor);
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

	private static VBoxContainer BuildVolumeSlider(string label, string busName)
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
		lbl.AddThemeFontSizeOverride("font_size", 14);
		lbl.AddThemeColorOverride("font_color", TextColor);
		headerRow.AddChild(lbl);

		Label pct = new()
		{
			CustomMinimumSize = new Vector2(48, 0),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		pct.AddThemeFontSizeOverride("font_size", 14);
		pct.AddThemeColorOverride("font_color", TextDim);
		headerRow.AddChild(pct);

		container.AddChild(headerRow);

		HSlider slider = new()
		{
			MinValue = 0.0,
			MaxValue = 1.0,
			Step = 0.01,
			CustomMinimumSize = new Vector2(0, 20),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		slider.ProcessMode = ProcessModeEnum.Always;

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
		vbox.AddChild(BuildToggleRow("Plein écran",
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
		particleLbl.AddThemeFontSizeOverride("font_size", 14);
		particleLbl.AddThemeColorOverride("font_color", TextColor);
		particleRow.AddChild(particleLbl);

		Button particleBtn = new()
		{
			Text = ParticleLevelLabel(VfxFactory.CurrentParticleLevel),
			CustomMinimumSize = new Vector2(160, 32),
			FocusMode = Control.FocusModeEnum.None
		};
		particleBtn.AddThemeFontSizeOverride("font_size", 13);
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

	private static HBoxContainer BuildToggleRow(string label, bool initialValue, System.Action<bool> onToggle)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 0);

		Label lbl = new()
		{
			Text = label,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		lbl.AddThemeFontSizeOverride("font_size", 14);
		lbl.AddThemeColorOverride("font_color", TextColor);
		row.AddChild(lbl);

		CheckButton toggle = new()
		{
			ButtonPressed = initialValue,
			FocusMode = Control.FocusModeEnum.None
		};
		toggle.Toggled += (bool toggled) => onToggle(toggled);
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
		placeholder.AddThemeFontSizeOverride("font_size", 14);
		placeholder.AddThemeColorOverride("font_color", TextDim);
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
			Text = "[Echap] Fermer",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		hint.AddThemeFontSizeOverride("font_size", 11);
		hint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
		margin.AddChild(hint);

		return margin;
	}

	private static string ParticleLevelLabel(ParticleLevel level)
	{
		return level switch
		{
			ParticleLevel.Full => "Toutes",
			ParticleLevel.Reduced => "Réduites",
			_ => "Désactivées",
		};
	}
}
