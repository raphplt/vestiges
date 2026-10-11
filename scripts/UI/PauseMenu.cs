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
	[Signal] public delegate void JournalRequestedEventHandler();
	private bool _journalOpen;
	private Button _journalButton;

	public void RestoreAfterJournal()
	{
		_journalOpen = false;
		_root.Visible = _isPaused;
		if (_isPaused) _journalButton.GrabFocus();
	}

	private static readonly Color GoldBright = UITheme.GoldBright;
	private static readonly Color TextVeryDim = UITheme.TextVeryDim;
	private static readonly Color StatLabelColor = PlayerSheet.StatLabelColor;
	private static readonly Color StatValueColor = PlayerSheet.StatValueColor;
	private static readonly Color StatBonusColor = PlayerSheet.StatBonusColor;
	private const string MenusPath = UITheme.MenusPath;

	private Control _root;
	private HBoxContainer _content;
	private bool _isPaused;
	private SettingsScreen _settingsScreen;
	private VBoxContainer _loadoutContainer;
	private VBoxContainer _inventoryContainer;
	private Control _confirmation;
	private Control _confirmReturnFocus;
	private System.Action _confirmAction;
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
		_settingsScreen.Opened += () => _root.Visible = false;
		_settingsScreen.Closed += () => _root.Visible = _isPaused;

		BuildUI();
		_root.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!_isPaused || _settingsScreen.IsOpen || _confirmation != null || _journalOpen)
			return;
		float axis = Input.GetAxis("scroll_up", "scroll_down");
		if (axis == 0f)
			return;
		int step = Mathf.RoundToInt(axis * ColumnScrollSpeed * (float)delta);
		Control focus = GetViewport().GuiGetFocusOwner();
		foreach (ScrollContainer scroll in _columnScrolls)
		{
			if (scroll == focus || (focus != null && scroll.IsAncestorOf(focus)))
			{ scroll.ScrollVertical += step; return; }
		}
		foreach (ScrollContainer scroll in _columnScrolls)
		{
			if (scroll.GetGlobalRect().HasPoint(scroll.GetGlobalMousePosition()))
			{ scroll.ScrollVertical += step; return; }
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_journalOpen || !@event.IsActionPressed("ui_cancel"))
			return;

		if (_confirmation != null)
		{
			CloseConfirmation();
			GetViewport().SetInputAsHandled();
			return;
		}

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
		GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Hub);
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

		MenuSkin.AddGameBackdrop(_root, PixelBackdrop.NeutralTint);

		// L’équipement garde l’espace principal ; la fiche conserve une hauteur bornée.
		HBoxContainer hbox = new();
		_content = hbox;
		hbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		hbox.OffsetLeft = -840;
		hbox.OffsetRight = 840;
		hbox.OffsetTop = -410;
		hbox.OffsetBottom = 410;
		hbox.AddThemeConstantOverride("separation", 30);
		_root.AddChild(hbox);

		VBoxContainer vbox = new() { CustomMinimumSize = new Vector2(240, 0) };
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

		_journalButton = CreateButton("Journal");
		_journalButton.Pressed += () =>
		{
			_journalOpen = true;
			_root.Visible = false;
			EmitSignal(SignalName.JournalRequested);
		};
		vbox.AddChild(_journalButton);

		Button hubBtn = CreateButton("Abandonner la run");
		hubBtn.Pressed += () => ConfirmExit(false);
		vbox.AddChild(hubBtn);

		Button quitBtn = CreateButton("Quitter");
		quitBtn.Pressed += () => ConfirmExit(true);
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

		PanelContainer inventoryPanel = new() { CustomMinimumSize = new Vector2(760, 0) };
		inventoryPanel.AddThemeStyleboxOverride("panel", MenuSkin.Panel(24));
		hbox.AddChild(inventoryPanel);
		VBoxContainer inventory = new();
		inventory.AddThemeConstantOverride("separation", 12);
		inventoryPanel.AddChild(inventory);
		inventory.AddChild(UITheme.MakeLabel("ÉQUIPEMENT", TextRole.Subhead, GoldBright));
		_inventoryContainer = new VBoxContainer();
		_inventoryContainer.AddThemeConstantOverride("separation", 10);
		inventory.AddChild(_inventoryContainer);
		ScrollContainer detailScroll = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FocusMode = Control.FocusModeEnum.All };
		inventory.AddChild(detailScroll);
		_columnScrolls.Add(detailScroll);
		_loadoutContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_loadoutContainer.AddThemeConstantOverride("separation", 10);
		detailScroll.AddChild(_loadoutContainer);
		_sheetContainer = BuildInfoPanel(hbox, "STATISTIQUES", 440);
		// Boucle explicite : la géométrie des colonnes ne détourne pas la navigation.
		Button[] buttons = { resumeBtn, settingsBtn, _journalButton, hubBtn, quitBtn };
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
			FocusMode = Control.FocusModeEnum.All,
			FollowFocus = true,
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
		MenuSkin.Clear(_loadoutContainer);
		MenuSkin.Clear(_inventoryContainer);
		MenuSkin.Clear(_sheetContainer);
		if (GetTree().GetFirstNodeInGroup("player") is not Player player) return;
		GridContainer weapons = InventoryGroup("Armes", 4);
		foreach (WeaponInstance weapon in player.WeaponSlots)
			AddInventoryTile(weapons, weapon.Sprite, weapon.Name, $"Niv {weapon.Level}", () => AddWeaponRow(player, weapon));
		FillEmpty(weapons, player.WeaponSlotLimit);
		GridContainer badges = InventoryGroup(Tr("INVENTORY_BADGES"), 4);
		foreach (ActivePassiveSouvenir badge in player.BadgeSlots)
			AddInventoryTile(badges, badge.Data.Icon, badge.Data.Name, $"Niv {badge.Level}", () => AddPassiveRow(badge));
		FillEmpty(badges, InventoryConfig.BadgeSlots);
		// Le sac compte des exemplaires : une tuile par objet, et une rangée au moins pour qu'un sac vide se lise.
		GridContainer objects = InventoryGroup($"{Tr("INVENTORY_BAG")}  {player.BagCount}/{player.BagCapacity}", 6);
		foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
			AddInventoryTile(objects, passive.Data.Icon, passive.Data.Name, $"×{passive.Copies}", () => AddPassiveRow(passive));
		FillEmpty(objects, objects.Columns);
		GridContainer perks = InventoryGroup("Réminiscences", 4);
		foreach (PerkSpecializationData perk in player.Specializations)
			AddInventoryTile(perks, perk.Icon, perk.Name, "", () => AddPerkRow(perk, player));
		FillEmpty(perks, PerkSpecializationDataLoader.Config.MaxEquipped);
		if (player.WeaponSlots.Count > 0) AddWeaponRow(player, player.WeaponSlots[0]);

		UpdateSheet(player);
	}

	private GridContainer InventoryGroup(string title, int columns)
	{
		_inventoryContainer.AddChild(UITheme.MakeLabel(title, TextRole.Body, UITheme.TextColor));
		GridContainer grid = new() { Columns = columns, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		grid.AddThemeConstantOverride("h_separation", 8);
		_inventoryContainer.AddChild(grid);
		return grid;
	}

	private void AddInventoryTile(GridContainer grid, string icon, string name, string level, System.Action detail)
	{
		Button tile = MenuSkin.Button("");
		tile.TooltipText = name;
		tile.AccessibilityName = name;
		tile.CustomMinimumSize = new Vector2(grid.Columns == 6 ? 110 : 169, 92);
		TextureRect picture = new() { Texture = UITheme.LoadTex(icon), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Position = new Vector2((tile.CustomMinimumSize.X - 48) / 2, 8), Size = new Vector2(48, 48), MouseFilter = Control.MouseFilterEnum.Ignore };
		tile.AddChild(picture);
		Label text = UITheme.MakeLabel(level, TextRole.Body, UITheme.TextLight, align: HorizontalAlignment.Center);
		text.Position = new Vector2(0, 62);
		text.Size = new Vector2(tile.CustomMinimumSize.X, 24);
		tile.AddChild(text);
		void Inspect() { MenuSkin.Clear(_loadoutContainer); detail(); }
		tile.MouseEntered += Inspect;
		tile.FocusEntered += Inspect;
		tile.Pressed += Inspect;
		grid.AddChild(tile);
	}

	private static void FillEmpty(GridContainer grid, int count)
	{
		while (grid.GetChildCount() < count)
		{
			Button empty = MenuSkin.Button("—");
			empty.Disabled = true;
			empty.FocusMode = Control.FocusModeEnum.None;
			empty.CustomMinimumSize = new Vector2(grid.Columns == 6 ? 110 : 169, 92);
			grid.AddChild(empty);
		}
	}

	private void ConfirmExit(bool quit)
	{
		_confirmReturnFocus = GetViewport().GuiGetFocusOwner();
		_confirmAction = quit ? QuitGame : ReturnToHub;
		_content.Visible = false;
		_confirmation = new Control { Name = "AbandonConfirmation" };
		_root.AddChild(_confirmation);
		_confirmation.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		ColorRect veil = new() { Color = new Color(0.035f, 0.035f, 0.06f, 0.94f) };
		_confirmation.AddChild(veil);
		veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		PanelContainer panel = new();
		_confirmation.AddChild(panel);
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft = -340; panel.OffsetRight = 340; panel.OffsetTop = -160; panel.OffsetBottom = 160;
		panel.AddThemeStyleboxOverride("panel", MenuSkin.Panel(36));
		VBoxContainer content = new();
		content.AddThemeConstantOverride("separation", 24);
		panel.AddChild(content);
		content.AddChild(UITheme.MakeLabel(quit ? "Quitter le jeu ?" : "Abandonner la run ?", TextRole.Title, UITheme.TextLight));
		Label description = UITheme.MakeLabel("La partie en cours sera abandonnée.", TextRole.Heading, UITheme.TextColor);
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		content.AddChild(description);
		HBoxContainer actions = new(); actions.AddThemeConstantOverride("separation", 20); content.AddChild(actions);
		Button cancel = MenuSkin.Button("Continuer la partie", true);
		cancel.Pressed += CloseConfirmation; actions.AddChild(cancel);
		Button accept = MenuSkin.Button(quit ? "Quitter" : "Abandonner");
		accept.Pressed += () => { System.Action action = _confirmAction; CloseConfirmation(); action?.Invoke(); };
		actions.AddChild(accept);
		cancel.FocusNeighborLeft = cancel.GetPath(); cancel.FocusNeighborRight = accept.GetPath();
		accept.FocusNeighborLeft = cancel.GetPath(); accept.FocusNeighborRight = accept.GetPath();
		foreach (Button button in new[] { cancel, accept })
		{ button.FocusNeighborTop = button.GetPath(); button.FocusNeighborBottom = button.GetPath(); }
		cancel.GrabFocus();
	}

	private void CloseConfirmation()
	{
		_confirmation?.QueueFree(); _confirmation = null; _confirmAction = null;
		_content.Visible = true;
		if (GodotObject.IsInstanceValid(_confirmReturnFocus)) _confirmReturnFocus.GrabFocus();
		_confirmReturnFocus = null;
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
		// Le surplus (cartes prises arme au maximum) est déjà compté dans les dégâts : on le nomme pour qu'il se lise.
		string surplus = weapon.SurplusCards > 0
			? "   " + string.Format(Tr("PAUSE_WEAPON_SURPLUS"), Mathf.RoundToInt(weapon.SurplusDamage * 100f))
			: "";
		Label name = PlayerSheet.MakeLabel($"{title}   Niv {weapon.Level}{surplus}", TextRole.Body, StatValueColor);
		name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		text.AddChild(name);

		List<string> parts = new()
		{
			$"{StatCatalog.Name("damage")} {StatCatalog.Format("damage", player.GetWeaponStatForDisplay(weapon, "damage"))}",
		};
		if (weapon.GetStat("attack_speed") > 0f)
			parts.Add($"{StatCatalog.Name("attack_speed")} {StatCatalog.Format("attack_speed", player.GetWeaponStatForDisplay(weapon, "attack_speed"))}");
		foreach (string stat in new[] { "projectile_count", "projectile_pierce", "chain_targets", "orbital_count" })
		{
			if (weapon.Base.Stats.ContainsKey(stat) || weapon.Base.Growth.ContainsKey(stat))
			{
				float value = player.GetWeaponStatForDisplay(weapon, stat);
				// Un seul projectile, aucun perçage : rien à signaler.
				if (value > (stat == "projectile_count" ? 1f : 0f))
					parts.Add($"{StatCatalog.Name(stat, weapon.Base)} {StatCatalog.Format(stat, value)}");
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
		// Un Écusson n'a pas de plafond à annoncer ; son sous-titre rappelle l'objet d'où il vient.
		string level = passive.Data.IsBadge ? $"Niv {passive.Level}" : $"×{passive.Copies}";
		string title = passive.Data.Subtitle.Length > 0 ? $"{passive.Data.Name} · {passive.Data.Subtitle}" : passive.Data.Name;
		Label name = PlayerSheet.MakeLabel($"{title}   {level}", TextRole.Body, StatValueColor);
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
		// La règle de l'objet, chiffres compris (DECISIONS §53), puis ses paliers.
		MarginContainer ruleIndent = new();
		ruleIndent.AddThemeConstantOverride("margin_left", 42);
		Label rule = PlayerSheet.MakeLabel(passive.Data.RuleText(), TextRole.Small, StatLabelColor);
		rule.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		ruleIndent.AddChild(rule);
		_loadoutContainer.AddChild(ruleIndent);
		// Paliers codés, sous l'objet : dorés une fois atteints, grisés avant.
		foreach (ObjectMilestoneData milestone in passive.Data.Milestones)
		{
			if (!ObjectMilestoneEffects.IsImplemented(milestone.Effect))
				continue;
			MarginContainer indent = new();
			indent.AddThemeConstantOverride("margin_left", 42);
			// Un objet atteint son palier par ses exemplaires, pas par un niveau affiché.
			string text = passive.Data.IsBadge
				? string.Format(Tr("LEVELUP_MILESTONE"), milestone.Level, milestone.Text)
				: string.Format(Tr("ITEM_MILESTONE"), passive.Data.CopiesForLevel(milestone.Level), milestone.Text);
			Label detail = PlayerSheet.MakeLabel(text, TextRole.Small,
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
			GetNodeOrNull<PerilManager>("/root/Main/PerilManager"), withOublis: true, role: TextRole.Subhead);

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
			CustomMinimumSize = new Vector2(240, 56),
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
