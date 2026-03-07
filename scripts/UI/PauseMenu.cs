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
	private static readonly Color GoldColor = new(0.9f, 0.82f, 0.5f);
	private static readonly Color StatLabelColor = new(0.6f, 0.58f, 0.52f);
	private static readonly Color StatValueColor = new(0.88f, 0.85f, 0.78f);
	private static readonly Color StatBonusColor = new(0.4f, 0.73f, 0.42f);

	private Control _root;
	private bool _isPaused;
	private SettingsScreen _settingsScreen;
	private Button _appelDuVideBtn;
	private PerkManager _perkManager;
	private VBoxContainer _statsContainer;

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
		UpdateStats();
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

		// HBox contenant stats (gauche) + menu (centre)
		HBoxContainer hbox = new();
		hbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		hbox.GrowHorizontal = Control.GrowDirection.Both;
		hbox.GrowVertical = Control.GrowDirection.Both;
		hbox.AddThemeConstantOverride("separation", 16);
		_root.AddChild(hbox);

		// --- Stats panel (gauche) ---
		BuildStatsPanel(hbox);

		// --- Panel central (boutons) ---
		PanelContainer panel = new();
		panel.CustomMinimumSize = new Vector2(280, 280);
		hbox.AddChild(panel);

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
		title.AddThemeColorOverride("font_color", GoldColor);
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

	private void BuildStatsPanel(HBoxContainer parent)
	{
		PanelContainer statsPanel = new();
		statsPanel.CustomMinimumSize = new Vector2(220, 0);

		StyleBoxFlat statsBg = new();
		statsBg.BgColor = new Color(0.06f, 0.06f, 0.1f, 0.9f);
		statsBg.SetBorderWidthAll(1);
		statsBg.BorderColor = new Color(0.3f, 0.28f, 0.22f, 0.6f);
		statsBg.SetCornerRadiusAll(4);
		statsBg.ContentMarginLeft = 14;
		statsBg.ContentMarginRight = 14;
		statsBg.ContentMarginTop = 14;
		statsBg.ContentMarginBottom = 14;
		statsPanel.AddThemeStyleboxOverride("panel", statsBg);

		parent.AddChild(statsPanel);

		VBoxContainer wrapper = new();
		wrapper.AddThemeConstantOverride("separation", 6);
		statsPanel.AddChild(wrapper);

		Label statsTitle = new()
		{
			Text = "STATISTIQUES",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		statsTitle.AddThemeFontSizeOverride("font_size", 14);
		statsTitle.AddThemeColorOverride("font_color", GoldColor);
		wrapper.AddChild(statsTitle);

		wrapper.AddChild(new HSeparator());

		_statsContainer = new VBoxContainer();
		_statsContainer.AddThemeConstantOverride("separation", 3);
		wrapper.AddChild(_statsContainer);
	}

	private void UpdateStats()
	{
		if (_statsContainer == null) return;

		foreach (Node child in _statsContainer.GetChildren())
			child.QueueFree();

		Node playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is not Player player) return;

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
		row.AddThemeConstantOverride("separation", 4);

		Label lbl = new() { Text = label };
		lbl.AddThemeFontSizeOverride("font_size", 12);
		lbl.AddThemeColorOverride("font_color", StatLabelColor);
		lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(lbl);

		Label val = new() { Text = value };
		val.AddThemeFontSizeOverride("font_size", 12);
		val.AddThemeColorOverride("font_color", StatValueColor);
		val.HorizontalAlignment = HorizontalAlignment.Right;
		row.AddChild(val);

		if (!string.IsNullOrEmpty(bonus))
		{
			Label bonusLbl = new() { Text = bonus };
			bonusLbl.AddThemeFontSizeOverride("font_size", 11);
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
