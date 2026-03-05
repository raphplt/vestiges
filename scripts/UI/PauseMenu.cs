using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Menu pause — Escape pour ouvrir/fermer.
/// Garde anti-conflit : ne s'ouvre pas si le jeu est déjà pausé
/// par un autre écran (LevelUpScreen, GameOverScreen, JournalScreen).
/// </summary>
public partial class PauseMenu : CanvasLayer
{
	private Control _root;
	private bool _isPaused;

	public bool IsOpen => _isPaused;

	public override void _Ready()
	{
		Layer = 50;
		ProcessMode = ProcessModeEnum.Always;
		BuildUI();
		_root.Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel"))
			return;

		if (_isPaused)
		{
			Resume();
			GetViewport().SetInputAsHandled();
		}
		else if (!GetTree().Paused)
		{
			Pause();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Pause()
	{
		_isPaused = true;
		_root.Visible = true;
		GetTree().Paused = true;
	}

	private void Resume()
	{
		_isPaused = false;
		_root.Visible = false;
		GetTree().Paused = false;
		AudioManager.Instance?.SaveSettings();
	}

	private void ReturnToHub()
	{
		_isPaused = false;
		_root.Visible = false;
		GetTree().Paused = false;
		AudioManager.Instance?.SaveSettings();
		GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
	}

	private void QuitGame()
	{
		AudioManager.Instance?.SaveSettings();
		GetTree().Quit();
	}

	private void BuildUI()
	{
		_root = new Control();
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.ProcessMode = ProcessModeEnum.Always;
		AddChild(_root);

		// Overlay sombre
		ColorRect overlay = new();
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		overlay.Color = new Color(0.02f, 0.02f, 0.05f, 0.75f);
		_root.AddChild(overlay);

		// Panel central — élargi pour contenir les sliders
		PanelContainer panel = new();
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		panel.GrowHorizontal = Control.GrowDirection.Both;
		panel.GrowVertical = Control.GrowDirection.Both;
		panel.OffsetLeft = -180;
		panel.OffsetRight = 180;
		panel.OffsetTop = -230;
		panel.OffsetBottom = 230;
		_root.AddChild(panel);

		MarginContainer margin = new();
		margin.LayoutMode = 1;
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 24);
		margin.AddThemeConstantOverride("margin_top", 20);
		margin.AddThemeConstantOverride("margin_right", 24);
		margin.AddThemeConstantOverride("margin_bottom", 20);
		panel.AddChild(margin);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 12);
		vbox.LayoutMode = 1;
		vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddChild(vbox);

		// Titre
		Label title = new()
		{
			Text = "PAUSE",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 22);
		title.AddThemeColorOverride("font_color", new Color(0.9f, 0.82f, 0.5f));
		vbox.AddChild(title);

		// Séparateur visuel
		vbox.AddChild(new HSeparator());

		// --- Boutons ---
		Button resumeBtn = CreateButton("Reprendre");
		resumeBtn.Pressed += Resume;
		vbox.AddChild(resumeBtn);

		Button hubBtn = CreateButton("Retour au Hub");
		hubBtn.Pressed += ReturnToHub;
		vbox.AddChild(hubBtn);

		Button quitBtn = CreateButton("Quitter");
		quitBtn.Pressed += QuitGame;
		vbox.AddChild(quitBtn);

		// --- Section audio ---
		vbox.AddChild(new HSeparator());

		Label audioLabel = new()
		{
			Text = "AUDIO",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		audioLabel.AddThemeFontSizeOverride("font_size", 13);
		audioLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
		vbox.AddChild(audioLabel);

		vbox.AddChild(BuildSlider("Global",   "Master"));
		vbox.AddChild(BuildSlider("Musique",  "Music"));
		vbox.AddChild(BuildSlider("SFX",      "SFX"));
		vbox.AddChild(BuildSlider("Ambiance", "Ambiance"));

		// Hint
		Label hint = new()
		{
			Text = "[Echap] Fermer",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		hint.AddThemeFontSizeOverride("font_size", 11);
		hint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
		vbox.AddChild(hint);
	}

	private static HBoxContainer BuildSlider(string label, string busName)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 8);

		Label lbl = new() { Text = label, CustomMinimumSize = new Vector2(72, 0) };
		lbl.AddThemeFontSizeOverride("font_size", 13);
		lbl.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.8f));
		lbl.VerticalAlignment = VerticalAlignment.Center;
		row.AddChild(lbl);

		HSlider slider = new()
		{
			MinValue = 0.0,
			MaxValue = 1.0,
			Step = 0.01,
			CustomMinimumSize = new Vector2(160, 0),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		slider.ProcessMode = ProcessModeEnum.Always;

		// Initialise depuis l'état courant du bus
		if (AudioManager.Instance != null)
			slider.Value = AudioManager.Instance.GetBusVolumeLinear(busName);
		else
			slider.Value = 1.0;

		slider.ValueChanged += (double v) =>
		{
			AudioManager.Instance?.SetBusVolumeLinear(busName, (float)v);
		};
		row.AddChild(slider);

		// Pourcentage live
		Label pct = new() { CustomMinimumSize = new Vector2(38, 0) };
		pct.AddThemeFontSizeOverride("font_size", 12);
		pct.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.6f));
		pct.VerticalAlignment = VerticalAlignment.Center;
		pct.Text = $"{(int)(slider.Value * 100)}%";

		slider.ValueChanged += (double v) => pct.Text = $"{(int)(v * 100)}%";
		row.AddChild(pct);

		return row;
	}

	private static Button CreateButton(string text)
	{
		Button btn = new()
		{
			Text = text,
			CustomMinimumSize = new Vector2(240, 34)
		};
		btn.AddThemeFontSizeOverride("font_size", 15);
		return btn;
	}
}
