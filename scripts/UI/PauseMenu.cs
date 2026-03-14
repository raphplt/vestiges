using Godot;
using Vestiges.Core;
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
	private static readonly Color GoldColor = UITheme.GoldColor;
	private static readonly Color GoldBright = UITheme.GoldBright;
	private static readonly Color TextColor = UITheme.TextColor;
	private static readonly Color TextDim = UITheme.TextDim;
	private static readonly Color TextVeryDim = UITheme.TextVeryDim;
	private static readonly Color StatLabelColor = new(0.62f, 0.60f, 0.54f);
	private static readonly Color StatValueColor = new(0.9f, 0.86f, 0.78f);
	private static readonly Color StatBonusColor = new(0.42f, 0.73f, 0.45f);
	private const string MenusPath = UITheme.MenusPath;

	private Control _root;
	private bool _isPaused;
	private SettingsScreen _settingsScreen;
	private Button _appelDuVideBtn;
	private PerkManager _perkManager;
	private VBoxContainer _statsContainer;
	private Texture2D _panelTex;
	private Texture2D _panelSelectedTex;
	private Texture2D _btnNormalTex;
	private Texture2D _btnHoverTex;
	private Texture2D _btnPressedTex;
	private Texture2D _btnDisabledTex;
	private Texture2D _separatorTex;

	public bool IsOpen => _isPaused;

	public override void _Ready()
	{
		Layer = 50;
		ProcessMode = ProcessModeEnum.Always;

		LoadTextures();

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
		UpdateStats();
	}

	private void Resume()
	{
		_isPaused = false;
		_root.Visible = false;
		GetTree().Paused = false;
		AudioManager.PlayUI("sfx_menu_confirmer");
	}

	private void OpenSettings()
	{
		AudioManager.PlayUI("sfx_menu_confirmer");
		_settingsScreen.Open();
	}

	private void ReturnToHub()
	{
		AudioManager.PlayUI("sfx_menu_confirmer");
		_isPaused = false;
		_root.Visible = false;
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/Hub.tscn");
	}

	private void QuitGame()
	{
		AudioManager.PlayUI("sfx_menu_confirmer");
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
		overlay.Color = new Color(0.02f, 0.025f, 0.05f, 0.84f);
		_root.AddChild(overlay);

		ColorRect glow = new();
		glow.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		glow.Color = new Color(0.18f, 0.13f, 0.08f, 0.14f);
		_root.AddChild(glow);

		MarginContainer shell = new();
		shell.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		shell.AddThemeConstantOverride("margin_left", 150);
		shell.AddThemeConstantOverride("margin_top", 110);
		shell.AddThemeConstantOverride("margin_right", 150);
		shell.AddThemeConstantOverride("margin_bottom", 110);
		_root.AddChild(shell);

		HBoxContainer hbox = new();
		hbox.Alignment = BoxContainer.AlignmentMode.Center;
		hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		hbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		hbox.AddThemeConstantOverride("separation", 26);
		shell.AddChild(hbox);

		// --- Stats panel (gauche) ---
		BuildStatsPanel(hbox);

		// --- Panel central (boutons) ---
		PanelContainer panel = new();
		panel.CustomMinimumSize = new Vector2(430, 510);
		ApplyPanelStyle(panel, true);
		hbox.AddChild(panel);

		MarginContainer margin = new();
		margin.LayoutMode = 1;
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 34);
		margin.AddThemeConstantOverride("margin_top", 28);
		margin.AddThemeConstantOverride("margin_right", 34);
		margin.AddThemeConstantOverride("margin_bottom", 28);
		panel.AddChild(margin);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 14);
		vbox.LayoutMode = 1;
		vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddChild(vbox);

		Label eyebrow = new()
		{
			Text = "HALTE DANS LE VIDE",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		eyebrow.AddThemeFontSizeOverride("font_size", 16);
		eyebrow.AddThemeColorOverride("font_color", TextDim);
		vbox.AddChild(eyebrow);

		// Titre
		Label title = new()
		{
			Text = "PAUSE",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 34);
		title.AddThemeColorOverride("font_color", GoldBright);
		vbox.AddChild(title);

		Label subtitle = new()
		{
			Text = "Le monde se fige, mais ta mémoire reste éveillée.",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		subtitle.AddThemeFontSizeOverride("font_size", 16);
		subtitle.AddThemeColorOverride("font_color", TextColor);
		vbox.AddChild(subtitle);

		vbox.AddChild(CreateSeparator());

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

		Control spacer = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		vbox.AddChild(spacer);

		// Hint
		Label hint = new()
		{
			Text = "[Échap] Reprendre la traversée",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		hint.AddThemeFontSizeOverride("font_size", 14);
		hint.AddThemeColorOverride("font_color", TextVeryDim);
		vbox.AddChild(hint);
	}

	private void BuildStatsPanel(HBoxContainer parent)
	{
		PanelContainer statsPanel = new();
		statsPanel.CustomMinimumSize = new Vector2(400, 510);
		ApplyPanelStyle(statsPanel, false);

		parent.AddChild(statsPanel);

		MarginContainer frame = new();
		frame.AddThemeConstantOverride("margin_left", 28);
		frame.AddThemeConstantOverride("margin_top", 26);
		frame.AddThemeConstantOverride("margin_right", 28);
		frame.AddThemeConstantOverride("margin_bottom", 26);
		statsPanel.AddChild(frame);

		VBoxContainer wrapper = new();
		wrapper.AddThemeConstantOverride("separation", 10);
		frame.AddChild(wrapper);

		Label statsTitle = new()
		{
			Text = "ÉTAT DU PASSEUR",
			HorizontalAlignment = HorizontalAlignment.Left
		};
		statsTitle.AddThemeFontSizeOverride("font_size", 20);
		statsTitle.AddThemeColorOverride("font_color", GoldBright);
		wrapper.AddChild(statsTitle);

		Label statsSubtitle = new()
		{
			Text = "Lecture instantanée de la run en cours.",
			HorizontalAlignment = HorizontalAlignment.Left
		};
		statsSubtitle.AddThemeFontSizeOverride("font_size", 14);
		statsSubtitle.AddThemeColorOverride("font_color", TextDim);
		wrapper.AddChild(statsSubtitle);

		wrapper.AddChild(CreateSeparator());

		_statsContainer = new VBoxContainer();
		_statsContainer.AddThemeConstantOverride("separation", 6);
		wrapper.AddChild(_statsContainer);
	}

	private void UpdateStats()
	{
		if (_statsContainer == null) return;

		foreach (Node child in _statsContainer.GetChildren())
			child.QueueFree();

		Node playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is not Player player) return;

		EssenceTracker essenceTracker = GetNodeOrNull<EssenceTracker>("/root/Main/EssenceTracker");
		if (player.EquippedWeapon != null)
		{
			AddStatLine("Arme", player.EquippedWeapon.Name);
			AddStatLine("Rarete", player.EquippedWeapon.RarityDisplayName);
		}
		if (essenceTracker != null)
			AddStatLine("Essence", essenceTracker.CurrentEssence.ToString());

		AddStatLine("PV", $"{player.CurrentHp:F0} / {player.EffectiveMaxHp:F0}");
		AddStatLine("Dégâts", $"{player.AttackDamage:F0}", FormatMult(player.DamageMultiplier));
		AddStatLine("Vit. Attaque", $"{player.AttackSpeed:F1}", FormatMult(player.AttackSpeedMultiplier));
		AddStatLine("Portée", $"{player.EffectiveAttackRange:F0}");
		AddStatLine("Vitesse", $"{player.Speed:F0}", FormatMult(player.SpeedMultiplier));
		AddStatLine("Régen.", $"{player.BaseRegenRate + player.BonusRegenRate:F1}/s");
		AddStatLine("Armure", $"{player.Armor:F0}");
		AddStatLine("Crit", $"{player.CritChance * 100:F0}%  x{player.CritMultiplier:F1}");

		if (player.ExtraProjectiles > 0)
			AddStatLine("Proj. bonus", $"+{player.ExtraProjectiles}");
		if (player.ProjectilePierce > 0)
			AddStatLine("Perçage", $"+{player.ProjectilePierce}");
		if (player.AoeMultiplier > 1f)
			AddStatLine("Zone", FormatMult(player.AoeMultiplier));
		if (player.VampirismPercent > 0f)
			AddStatLine("Vampirisme", $"{player.VampirismPercent * 100:F0}%");
		if (player.DodgeChance > 0f)
			AddStatLine("Esquive", $"{player.DodgeChance * 100:F0}%");
		if (player.ThornsPercent > 0f)
			AddStatLine("Épines", $"{player.ThornsPercent * 100:F0}%");
		if (player.IgniteChance > 0f)
			AddStatLine("Ignition", $"{player.IgniteChance * 100:F0}%");
		if (player.RicochetChance > 0f)
			AddStatLine("Ricochet", $"{player.RicochetChance * 100:F0}%");
		if (player.LuckBonus > 0f)
			AddStatLine("Chance", $"+{player.LuckBonus * 100:F0}%");
	}

	private void AddStatLine(string label, string value, string bonus = null)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 8);

		Label lbl = new() { Text = label };
		lbl.AddThemeFontSizeOverride("font_size", 15);
		lbl.AddThemeColorOverride("font_color", StatLabelColor);
		lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(lbl);

		Label val = new() { Text = value };
		val.AddThemeFontSizeOverride("font_size", 15);
		val.AddThemeColorOverride("font_color", StatValueColor);
		val.HorizontalAlignment = HorizontalAlignment.Right;
		row.AddChild(val);

		if (!string.IsNullOrEmpty(bonus))
		{
			Label bonusLbl = new() { Text = bonus };
			bonusLbl.AddThemeFontSizeOverride("font_size", 14);
			bonusLbl.AddThemeColorOverride("font_color", StatBonusColor);
			row.AddChild(bonusLbl);
		}

		_statsContainer.AddChild(row);
	}

	private static string FormatMult(float mult)
	{
		if (Mathf.IsEqualApprox(mult, 1f))
			return null;
		return $"x{mult:F2}";
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

	private void LoadTextures()
	{
		_panelTex = UITheme.LoadTex(MenusPath + "ui_panel_frame.png");
		_panelSelectedTex = UITheme.LoadTex(MenusPath + "ui_panel_frame_selected.png");
		_btnNormalTex = UITheme.LoadTex(MenusPath + "ui_button_normal.png");
		_btnHoverTex = UITheme.LoadTex(MenusPath + "ui_button_hover.png");
		_btnPressedTex = UITheme.LoadTex(MenusPath + "ui_button_pressed.png");
		_btnDisabledTex = UITheme.LoadTex(MenusPath + "ui_button_disabled.png");
		_separatorTex = UITheme.LoadTex(MenusPath + "ui_separator_wide.png");
	}

	private void ApplyPanelStyle(PanelContainer panel, bool selected)
	{
		Texture2D tex = selected ? (_panelSelectedTex ?? _panelTex) : _panelTex;
		if (tex != null)
		{
			panel.AddThemeStyleboxOverride("panel", UITheme.CreateNinePatch(tex, 10, 10, 10, 10));
			return;
		}

		StyleBoxFlat fallback = new();
		fallback.BgColor = new Color(0.06f, 0.06f, 0.1f, 0.92f);
		fallback.SetBorderWidthAll(1);
		fallback.BorderColor = selected ? GoldColor : new Color(0.26f, 0.24f, 0.18f, 0.85f);
		fallback.SetCornerRadiusAll(4);
		panel.AddThemeStyleboxOverride("panel", fallback);
	}

	private Control CreateSeparator()
	{
		if (_separatorTex != null)
		{
			TextureRect sep = new()
			{
				Texture = _separatorTex,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				CustomMinimumSize = new Vector2(0, 12)
			};
			return sep;
		}

		HSeparator sepFallback = new();
		return sepFallback;
	}

	private Button CreateButton(string text)
	{
		Button btn = new()
		{
			Text = text,
			CustomMinimumSize = new Vector2(320, 48),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		btn.AddThemeFontSizeOverride("font_size", 20);
		btn.AddThemeConstantOverride("h_separation", 6);
		UITheme.ApplyButtonStyle(btn, _btnNormalTex, _btnHoverTex, _btnPressedTex, _btnDisabledTex);
		return btn;
	}
}
