using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Menu pause — Escape pour ouvrir/fermer.
/// Garde anti-conflit : ne s'ouvre pas si le jeu est déjà pausé
/// par un autre écran (LevelUpScreen, GameOverScreen, JournalScreen).
/// Les paramètres audio/graphiques sont délégués au SettingsScreen.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
	private Control _root;
	private bool _isPaused;
	private SettingsScreen _settingsScreen;
	private Button _appelDuVideBtn;
	private PerkManager _perkManager;

	public bool IsOpen => _isPaused;

	public override void _Ready()
	{
		Layer = 50;
		ProcessMode = ProcessModeEnum.Always;

		_settingsScreen = new SettingsScreen();
		AddChild(_settingsScreen);

		BuildUI();
		_root.Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel"))
			return;

		if (_settingsScreen.IsOpen)
		{
			_settingsScreen.Close();
			GetViewport().SetInputAsHandled();
			return;
		}

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
		UpdateAppelDuVideButton();
	}

	private void Resume()
	{
		_isPaused = false;
		_root.Visible = false;
		GetTree().Paused = false;
	}

	private void OpenSettings()
	{
		_settingsScreen.Open();
	}

	private void ReturnToHub()
	{
		_isPaused = false;
		_root.Visible = false;
		GetTree().Paused = false;
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

		// Panel central
		PanelContainer panel = new();
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		panel.GrowHorizontal = Control.GrowDirection.Both;
		panel.GrowVertical = Control.GrowDirection.Both;
		panel.OffsetLeft = -140;
		panel.OffsetRight = 140;
		panel.OffsetTop = -140;
		panel.OffsetBottom = 140;
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

		vbox.AddChild(new HSeparator());

		Button resumeBtn = CreateButton("Reprendre");
		resumeBtn.Pressed += Resume;
		vbox.AddChild(resumeBtn);

		Button settingsBtn = CreateButton("Parametres");
		settingsBtn.Pressed += OpenSettings;
		vbox.AddChild(settingsBtn);

		_appelDuVideBtn = CreateButton("Appel du Vide: OFF");
		_appelDuVideBtn.Pressed += ToggleAppelDuVide;
		_appelDuVideBtn.Visible = false;
		vbox.AddChild(_appelDuVideBtn);

		Button hubBtn = CreateButton("Retour au Hub");
		hubBtn.Pressed += ReturnToHub;
		vbox.AddChild(hubBtn);

		Button quitBtn = CreateButton("Quitter");
		quitBtn.Pressed += QuitGame;
		vbox.AddChild(quitBtn);

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

	private void ToggleAppelDuVide()
	{
		CachePerkManager();
		_perkManager?.ToggleAppelDuVide();
		UpdateAppelDuVideButton();
	}

	private void UpdateAppelDuVideButton()
	{
		CachePerkManager();
		if (_perkManager == null || _perkManager.AppelDuVideLevel <= 0)
		{
			_appelDuVideBtn.Visible = false;
			return;
		}

		_appelDuVideBtn.Visible = true;
		bool active = _perkManager.IsAppelDuVideActive;
		int level = _perkManager.AppelDuVideLevel;
		_appelDuVideBtn.Text = $"Appel du Vide Lv{level}: {(active ? "ON" : "OFF")}";

		StyleBoxFlat style = new();
		if (active)
		{
			style.BgColor = new Color(0.4f, 0.1f, 0.15f, 0.8f);
			style.BorderColor = new Color(0.8f, 0.2f, 0.3f);
		}
		else
		{
			style.BgColor = new Color(0.15f, 0.1f, 0.2f, 0.6f);
			style.BorderColor = new Color(0.4f, 0.3f, 0.5f);
		}
		style.BorderWidthBottom = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthLeft = 1;
		style.BorderWidthRight = 1;
		style.CornerRadiusTopLeft = 4;
		style.CornerRadiusTopRight = 4;
		style.CornerRadiusBottomLeft = 4;
		style.CornerRadiusBottomRight = 4;
		_appelDuVideBtn.AddThemeStyleboxOverride("normal", style);
	}

	private void CachePerkManager()
	{
		if (_perkManager != null && IsInstanceValid(_perkManager))
			return;
		_perkManager = GetNodeOrNull<PerkManager>("/root/Main/PerkManager");
	}

	private static Button CreateButton(string text)
	{
		Button btn = new()
		{
			Text = text,
			CustomMinimumSize = new Vector2(200, 34)
		};
		btn.AddThemeFontSizeOverride("font_size", 15);
		return btn;
	}
}
