using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
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
	private static readonly Color GoldBright = UITheme.GoldBright;
	private static readonly Color TextVeryDim = UITheme.TextVeryDim;
	private static readonly Color StatLabelColor = PlayerSheet.StatLabelColor;
	private static readonly Color StatValueColor = PlayerSheet.StatValueColor;
	private static readonly Color StatBonusColor = PlayerSheet.StatBonusColor;
	private const string MenusPath = UITheme.MenusPath;

	private Control _root;
	private bool _isPaused;
	private SettingsScreen _settingsScreen;
	private VBoxContainer _loadoutContainer;
	private VBoxContainer _sheetContainer;
	private Texture2D _panelTex;
	private Texture2D _btnNormalTex;
	private Texture2D _btnHoverTex;
	private Texture2D _btnPressedTex;
	private Texture2D _btnDisabledTex;

	public bool IsOpen => _isPaused;
	private Button _resumeButton;
	// Colonnes Équipement et Fiche : sans contrôle focalisable, elles défilent au stick droit ou à Page haut/bas.
	private readonly List<ScrollContainer> _columnScrolls = new();
	private const float ColumnScrollSpeed = 900f;

	public override void _Ready()
	{
		Layer = 50;
		ProcessMode = ProcessModeEnum.Always;

		LoadTextures();

		_settingsScreen = new SettingsScreen { Name = "SettingsScreen" };
		AddChild(_settingsScreen);

		BuildUI();
		_root.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!_isPaused || _settingsScreen.IsOpen)
			return;
		float axis = Input.GetAxis("scroll_up", "scroll_down");
		if (axis == 0f)
			return;
		int step = Mathf.RoundToInt(axis * ColumnScrollSpeed * (float)delta);
		foreach (ScrollContainer scroll in _columnScrolls)
			scroll.ScrollVertical += step;
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
		UpdateStats();
		// Clavier et manette : la pause s'ouvre sur « Reprendre ».
		_resumeButton.GrabFocus();
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
		_ = GameExit.QuitAsync(GetTree());
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

		// Une hauteur bornée laisse le monde visible ; l'équipement garde l'espace principal.
		HBoxContainer hbox = new();
		hbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		hbox.OffsetLeft = -770;
		hbox.OffsetRight = 770;
		hbox.OffsetTop = -390;
		hbox.OffsetBottom = 390;
		hbox.AddThemeConstantOverride("separation", 30);
		_root.AddChild(hbox);

		VBoxContainer vbox = new() { CustomMinimumSize = new Vector2(280, 0) };
		vbox.AddThemeConstantOverride("separation", 12);
		hbox.AddChild(vbox);
		Label title = UITheme.MakeLabel("PAUSE", TextRole.Display, GoldBright, TextWeight.Bold);
		vbox.AddChild(title);
		vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 24) });

		Button resumeBtn = CreateButton("Reprendre");
		_resumeButton = resumeBtn;
		resumeBtn.Pressed += Resume;
		vbox.AddChild(resumeBtn);

		Button settingsBtn = CreateButton("Paramètres");
		settingsBtn.Pressed += OpenSettings;
		vbox.AddChild(settingsBtn);

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
			Text = "[Échap] Reprendre",
			HorizontalAlignment = HorizontalAlignment.Left
		};
		UITheme.SetTextRole(hint, TextRole.Small);
		hint.AddThemeColorOverride("font_color", TextVeryDim);
		vbox.AddChild(hint);

		_loadoutContainer = BuildInfoPanel(hbox, "ÉQUIPEMENT", 760);
		_sheetContainer = BuildInfoPanel(hbox, "STATISTIQUES", 440);
		// Boucle explicite : la géométrie des colonnes ne détourne pas la navigation.
		Button[] buttons = { resumeBtn, settingsBtn, hubBtn, quitBtn };
		for (int i = 0; i < buttons.Length; i++)
		{
			Button button = buttons[i];
			button.FocusNeighborTop = button.GetPathTo(buttons[(i + buttons.Length - 1) % buttons.Length]);
			button.FocusNeighborBottom = button.GetPathTo(buttons[(i + 1) % buttons.Length]);
		}
	}

	private VBoxContainer BuildInfoPanel(HBoxContainer parent, string titleText, float width)
	{
		PanelContainer panel = new() { CustomMinimumSize = new Vector2(width, 0) };
		ApplyPanelStyle(panel);
		parent.AddChild(panel);

		MarginContainer frame = new();
		frame.AddThemeConstantOverride("margin_left", 24);
		frame.AddThemeConstantOverride("margin_top", 22);
		frame.AddThemeConstantOverride("margin_right", 24);
		frame.AddThemeConstantOverride("margin_bottom", 22);
		panel.AddChild(frame);

		VBoxContainer wrapper = new();
		wrapper.AddThemeConstantOverride("separation", 8);
		frame.AddChild(wrapper);

		Label title = new() { Text = titleText };
		UITheme.SetTextRole(title, TextRole.Subhead);
		title.AddThemeColorOverride("font_color", GoldBright);
		wrapper.AddChild(title);
		wrapper.AddChild(new ColorRect
		{
			Color = new Color(0.23f, 0.24f, 0.28f),
			CustomMinimumSize = new Vector2(0, 1),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});

		// Quatre armes, des passifs et un texte agrandi dépassent la hauteur de l'écran : la colonne défile.
		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
		};
		wrapper.AddChild(scroll);
		_columnScrolls.Add(scroll);
		// La barre de défilement se pose sur le bord droit : la colonne des valeurs garde sa marge.
		MarginContainer gutter = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		gutter.AddThemeConstantOverride("margin_right", 14);
		scroll.AddChild(gutter);
		VBoxContainer content = new();
		content.AddThemeConstantOverride("separation", 10);
		gutter.AddChild(content);
		return content;
	}

	private void UpdateStats()
	{
		if (_loadoutContainer == null)
			return;
		foreach (Node child in _loadoutContainer.GetChildren())
			child.QueueFree();
		foreach (Node child in _sheetContainer.GetChildren())
			child.QueueFree();

		if (GetTree().GetFirstNodeInGroup("player") is not Player player)
			return;

		PlayerSheet.AddSectionTitle(_loadoutContainer, "Armes");
		foreach (WeaponInstance weapon in player.WeaponSlots)
			AddWeaponRow(player, weapon);
		PlayerSheet.AddSectionTitle(_loadoutContainer, "Objets");
		foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
			AddPassiveRow(passive);
		if (player.PassiveSlots.Count == 0)
			PlayerSheet.AddLine(_loadoutContainer, "Aucun pour l'instant.", "", TextVeryDim);
		PlayerSheet.AddSectionTitle(_loadoutContainer, "Réminiscences");
		foreach (PerkSpecializationData perk in player.Specializations)
			AddPerkRow(perk, player);
		if (player.Specializations.Count == 0)
			PlayerSheet.AddLine(_loadoutContainer, "Aucune pour l'instant.", "", TextVeryDim);

		UpdateSheet(player);
	}

	/// <summary>Arme : icône, nom, niveau, stats effectives utiles à son motif, dégâts infligés depuis le début.</summary>
	private void AddWeaponRow(Player player, WeaponInstance weapon)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 10);
		row.AddChild(PlayerSheet.MakeIcon(weapon.Sprite));

		VBoxContainer text = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		text.AddThemeConstantOverride("separation", 0);
		row.AddChild(text);
		string title = weapon.Ascension != null ? $"{weapon.Name} · {weapon.Ascension.Name}" : weapon.Name;
		Label name = PlayerSheet.MakeLabel($"{title}   Niv {weapon.Level}", TextRole.Body, StatValueColor);
		name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		text.AddChild(name);

		List<string> parts = new()
		{
			$"{StatCatalog.Name("damage")} {StatCatalog.Format("damage", player.GetWeaponStatForDisplay(weapon, "damage"))}",
		};
		if (weapon.GetStat("attack_speed", 0f) > 0f)
			parts.Add($"{StatCatalog.Name("attack_speed")} {StatCatalog.Format("attack_speed", player.GetWeaponStatForDisplay(weapon, "attack_speed"))}");
		foreach (string stat in new[] { "projectile_count", "projectile_pierce", "chain_targets", "orbital_count" })
		{
			if (weapon.Base.Stats.ContainsKey(stat) || weapon.Base.Growth.ContainsKey(stat))
			{
				float value = player.GetWeaponStatForDisplay(weapon, stat);
				// Un seul projectile, aucun perçage : rien à signaler.
				if (value > (stat == "projectile_count" ? 1f : 0f))
					parts.Add($"{StatCatalog.Name(stat)} {StatCatalog.Format(stat, value)}");
			}
		}
		Label stats = PlayerSheet.MakeLabel(string.Join("  ·  ", parts), TextRole.Caption, StatLabelColor);
		stats.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		text.AddChild(stats);

		VBoxContainer dealt = new() { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		dealt.AddChild(PlayerSheet.MakeLabel(Mathf.RoundToInt(player.GetDamageDealt(weapon.Id)).ToString("N0", PlayerSheet.French), TextRole.Body, StatBonusColor, HorizontalAlignment.Right));
		dealt.AddChild(PlayerSheet.MakeLabel("dégâts infligés", TextRole.Caption, TextVeryDim, HorizontalAlignment.Right));
		row.AddChild(dealt);
		_loadoutContainer.AddChild(row);
	}

	private void AddPassiveRow(ActivePassiveSouvenir passive)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 10);
		row.AddChild(PlayerSheet.MakeIcon(passive.Data.Icon));
		Label name = PlayerSheet.MakeLabel($"{passive.Data.Name}   Niv {passive.Level}/{passive.Data.MaxLevel}", TextRole.Body, StatValueColor);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		row.AddChild(name);
		string effects = "";
		for (int i = 0; i < passive.Data.Effects.Count; i++)
		{
			PassiveEffectData effect = passive.Data.Effects[i];
			string line = $"{StatCatalog.Name(effect.Stat)} {StatCatalog.FormatBonus(effect.Stat, passive.Value(i), effect.Multiplicative)}";
			effects = effects.Length == 0 ? line : $"{effects}  ·  {line}";
		}
		row.AddChild(PlayerSheet.MakeLabel(effects, TextRole.Small, StatBonusColor, HorizontalAlignment.Right));
		_loadoutContainer.AddChild(row);
		// Paliers codés, sous l'objet : dorés une fois atteints, grisés avant.
		foreach (ObjectMilestoneData milestone in passive.Data.Milestones)
		{
			if (!ObjectMilestoneEffects.IsImplemented(milestone.Effect))
				continue;
			MarginContainer indent = new();
			indent.AddThemeConstantOverride("margin_left", 42);
			Label detail = PlayerSheet.MakeLabel(string.Format(Tr("LEVELUP_MILESTONE"), milestone.Level, milestone.Text), TextRole.Small,
				passive.Reached(milestone) ? GoldBright : TextVeryDim);
			detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			indent.AddChild(detail);
			_loadoutContainer.AddChild(indent);
		}
	}

	/// <summary>Perk : nom, règle, puis son état du moment (réserve, fenêtre, cible) ou son inactivité faute d'arme.</summary>
	private void AddPerkRow(PerkSpecializationData perk, Player player)
	{
		HBoxContainer row = new();
		row.AddChild(PlayerSheet.MakeIcon(perk.Icon));
		VBoxContainer text = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddChild(text);
		text.AddThemeConstantOverride("separation", 0);
		text.AddChild(PlayerSheet.MakeLabel(perk.Name, TextRole.Body, StatValueColor));
		Label rule = PlayerSheet.MakeLabel(perk.Description, TextRole.Small, StatBonusColor);
		rule.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		text.AddChild(rule);
		bool active = PerkSpecializationOffers.IsActive(perk, player);
		string state = active ? PerkState(perk, player) : Tr("PAUSE_PERK_INACTIVE");
		if (state.Length > 0)
			text.AddChild(PlayerSheet.MakeLabel(state, TextRole.Small, active ? StatValueColor : TextVeryDim));
		_loadoutContainer.AddChild(row);
	}

	private string PerkState(PerkSpecializationData perk, Player player)
	{
		SpecializationRuntime runtime = player.SpecializationRuntime;
		switch (perk.Effect)
		{
			case SpecializationRuntime.OverhealReserveEffect when runtime?.Reserve != null:
				return string.Format(Tr("PAUSE_PERK_RESERVE"), runtime.Reserve.Stock.ToString("0", PlayerSheet.French), runtime.Reserve.Capacity.ToString("0", PlayerSheet.French));
			case SpecializationRuntime.RallyEffect when runtime?.Rally != null:
				return runtime.Rally.Recoverable > 0f
					? string.Format(Tr("PAUSE_PERK_RALLY"), runtime.Rally.Recoverable.ToString("0", PlayerSheet.French), runtime.Rally.Remaining.ToString("0.0", PlayerSheet.French))
					: Tr("PAUSE_PERK_RALLY_IDLE");
			case SpecializationRuntime.OverflowEffect when runtime?.Overflow != null:
			{
				string ready = "";
				foreach (WeaponInstance weapon in runtime.Overflow.Ready)
				{
					string entry = $"{weapon.Name} +{runtime.Overflow.Amount(weapon).ToString("0", PlayerSheet.French)}";
					ready = ready.Length == 0 ? entry : $"{ready}, {entry}";
				}
				return ready.Length > 0 ? string.Format(Tr("PAUSE_PERK_OVERFLOW"), ready) : Tr("PAUSE_PERK_OVERFLOW_IDLE");
			}
			case SpecializationRuntime.PriorityTargetingEffect when runtime?.PriorityTargeting != null:
				return runtime.PriorityTargeting.Current is Enemy target
					? string.Format(Tr("PAUSE_PERK_TARGET"), target.DisplayName)
					: Tr("PAUSE_PERK_TARGET_IDLE");
			default:
				return "";
		}
	}

	/// <summary>Toutes les stats du joueur, Oublis compris : la même fiche que pendant le level-up.</summary>
	private void UpdateSheet(Player player) =>
		PlayerSheet.AddStatLines(_sheetContainer, player, GetNodeOrNull<EssenceTracker>("/root/Main/EssenceTracker"),
			GetNodeOrNull<PerilManager>("/root/Main/PerilManager"), withOublis: true);

	private void LoadTextures()
	{
		_panelTex = UITheme.LoadTex(MenusPath + "ui_panel_frame.png");
		_btnNormalTex = UITheme.LoadTex(MenusPath + "ui_button_normal.png");
		_btnHoverTex = UITheme.LoadTex(MenusPath + "ui_button_hover.png");
		_btnPressedTex = UITheme.LoadTex(MenusPath + "ui_button_pressed.png");
		_btnDisabledTex = UITheme.LoadTex(MenusPath + "ui_button_disabled.png");
	}

	private void ApplyPanelStyle(PanelContainer panel)
	{
		panel.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		panel.AddThemeStyleboxOverride("panel", UITheme.CreateNinePatch(_panelTex, 10, 10, 10, 10));
	}

	private Button CreateButton(string text)
	{
		Button btn = new()
		{
			Text = text,
			Alignment = HorizontalAlignment.Left,
			CustomMinimumSize = new Vector2(280, 56),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		UITheme.SetTextRole(btn, TextRole.Subhead);
		btn.AddThemeConstantOverride("h_separation", 6);
		UITheme.ApplyButtonStyle(btn, _btnNormalTex, _btnHoverTex, _btnPressedTex, _btnDisabledTex);
		foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
		{
			btn.GetThemeStylebox(state).ContentMarginLeft = 18;
			btn.GetThemeStylebox(state).ContentMarginRight = 18;
		}
		return btn;
	}
}
