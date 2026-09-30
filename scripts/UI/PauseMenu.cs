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
	private static readonly Color GoldColor = UITheme.GoldColor;
	private static readonly Color GoldBright = UITheme.GoldBright;
	private static readonly Color TextColor = UITheme.TextColor;
	private static readonly Color TextDim = UITheme.TextDim;
	private static readonly Color TextVeryDim = UITheme.TextVeryDim;
	private static readonly Color StatLabelColor = new(0.62f, 0.60f, 0.54f);
	private static readonly Color StatValueColor = new(0.9f, 0.86f, 0.78f);
	private static readonly Color StatBonusColor = new(0.42f, 0.73f, 0.45f);
	private static readonly Color PerilColor = new(0.85f, 0.38f, 0.42f);
	private const string MenusPath = UITheme.MenusPath;

	private Control _root;
	private bool _isPaused;
	private SettingsScreen _settingsScreen;
	private VBoxContainer _loadoutContainer;
	private VBoxContainer _sheetContainer;
	private Texture2D _panelTex;
	private Texture2D _panelSelectedTex;
	private Texture2D _btnNormalTex;
	private Texture2D _btnHoverTex;
	private Texture2D _btnPressedTex;
	private Texture2D _btnDisabledTex;
	private Texture2D _separatorTex;

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

		_settingsScreen = new SettingsScreen();
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

		ColorRect glow = new();
		glow.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		glow.Color = new Color(0.18f, 0.13f, 0.08f, 0.14f);
		_root.AddChild(glow);

		MarginContainer shell = new();
		shell.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		shell.AddThemeConstantOverride("margin_left", 80);
		shell.AddThemeConstantOverride("margin_top", 90);
		shell.AddThemeConstantOverride("margin_right", 80);
		shell.AddThemeConstantOverride("margin_bottom", 90);
		_root.AddChild(shell);

		HBoxContainer hbox = new();
		hbox.Alignment = BoxContainer.AlignmentMode.Center;
		hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		hbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		hbox.AddThemeConstantOverride("separation", 26);
		shell.AddChild(hbox);

		// Armes et passifs à gauche, boutons au centre, fiche du personnage à droite (plan 17 lot 1C).
		_loadoutContainer = BuildInfoPanel(hbox, "ÉQUIPEMENT", 560);

		PanelContainer panel = new();
		panel.CustomMinimumSize = new Vector2(360, 560);
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
		UITheme.SetTextRole(eyebrow, TextRole.Body);
		eyebrow.AddThemeColorOverride("font_color", TextDim);
		vbox.AddChild(eyebrow);

		// Titre
		Label title = new()
		{
			Text = "PAUSE",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		UITheme.SetTextRole(title, TextRole.Banner);
		title.AddThemeColorOverride("font_color", GoldBright);
		vbox.AddChild(title);

		Label subtitle = new()
		{
			Text = "Le monde se fige, mais ta mémoire reste éveillée.",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		UITheme.SetTextRole(subtitle, TextRole.Body);
		subtitle.AddThemeColorOverride("font_color", TextColor);
		vbox.AddChild(subtitle);

		vbox.AddChild(CreateSeparator());

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
			Text = "[Échap] Reprendre la traversée",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		UITheme.SetTextRole(hint, TextRole.Small);
		hint.AddThemeColorOverride("font_color", TextVeryDim);
		vbox.AddChild(hint);

		_sheetContainer = BuildInfoPanel(hbox, "FICHE DU PASSEUR", 360);
	}

	private VBoxContainer BuildInfoPanel(HBoxContainer parent, string titleText, float width)
	{
		PanelContainer panel = new() { CustomMinimumSize = new Vector2(width, 560) };
		ApplyPanelStyle(panel, false);
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
		wrapper.AddChild(CreateSeparator());

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
		content.AddThemeConstantOverride("separation", 6);
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

		AddSectionTitle(_loadoutContainer, "Armes");
		foreach (WeaponInstance weapon in player.WeaponSlots)
			AddWeaponRow(player, weapon);
		AddSectionTitle(_loadoutContainer, "Objets");
		foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
			AddPassiveRow(passive);
		if (player.PassiveSlots.Count == 0)
			AddLine(_loadoutContainer, "Aucun pour l'instant.", "", TextVeryDim);
		AddSectionTitle(_loadoutContainer, "Réminiscences");
		foreach (PerkSpecializationData perk in player.Specializations)
			AddPerkRow(perk, player);
		if (player.Specializations.Count == 0)
			AddLine(_loadoutContainer, "Aucune pour l'instant.", "", TextVeryDim);

		UpdateSheet(player);
	}

	/// <summary>Arme : icône, nom, niveau, stats effectives utiles à son motif, dégâts infligés depuis le début.</summary>
	private void AddWeaponRow(Player player, WeaponInstance weapon)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 10);
		row.AddChild(MakeIcon(weapon.Sprite));

		VBoxContainer text = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		text.AddThemeConstantOverride("separation", 0);
		row.AddChild(text);
		string title = weapon.Ascension != null ? $"{weapon.Name} · {weapon.Ascension.Name}" : weapon.Name;
		text.AddChild(MakeLabel($"{title}   Niv {weapon.Level}", TextRole.Body, StatValueColor));

		List<string> parts = new()
		{
			$"{StatCatalog.Name("damage")} {StatCatalog.Format("damage", player.GetWeaponStatForDisplay(weapon, "damage"))}",
		};
		if (weapon.GetStat("attack_speed", 0f) > 0f)
			parts.Add($"{StatCatalog.Name("attack_speed")} {StatCatalog.Format("attack_speed", player.GetWeaponStatForDisplay(weapon, "attack_speed"))}");
		foreach (string stat in new[] { "projectile_count", "projectile_pierce", "chain_targets", "orbital_count" })
		{
			if (weapon.Base.Stats.ContainsKey(stat) || weapon.Base.Milestones.Contains(stat))
			{
				float value = player.GetWeaponStatForDisplay(weapon, stat);
				// Un seul projectile, aucun perçage : rien à signaler.
				if (value > (stat == "projectile_count" ? 1f : 0f))
					parts.Add($"{StatCatalog.Name(stat)} {StatCatalog.Format(stat, value)}");
			}
		}
		text.AddChild(MakeLabel(string.Join("  ·  ", parts), TextRole.Caption, StatLabelColor));
		if (!string.IsNullOrEmpty(weapon.Base.LoreFlavor))
		{
			Label lore = MakeLabel(weapon.Base.LoreFlavor, TextRole.Caption, TextVeryDim);
			lore.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			lore.CustomMinimumSize = new Vector2(320, 0);
			text.AddChild(lore);
		}

		VBoxContainer dealt = new() { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		dealt.AddChild(MakeLabel(Mathf.RoundToInt(player.GetDamageDealt(weapon.Id)).ToString("N0", French), TextRole.Body, StatBonusColor, HorizontalAlignment.Right));
		dealt.AddChild(MakeLabel("dégâts infligés", TextRole.Caption, TextVeryDim, HorizontalAlignment.Right));
		row.AddChild(dealt);
		_loadoutContainer.AddChild(row);
	}

	private void AddPassiveRow(ActivePassiveSouvenir passive)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 10);
		row.AddChild(MakeIcon(PerkIconResolver.GetPassiveStatIconPath(passive.Data.Stat)));
		Label name = MakeLabel($"{passive.Data.Name}   Niv {passive.Level}/{passive.Data.MaxLevel}", TextRole.Body, StatValueColor);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(name);
		string effects = "";
		for (int i = 0; i < passive.Data.Effects.Count; i++)
		{
			PassiveEffectData effect = passive.Data.Effects[i];
			string line = $"{StatCatalog.Name(effect.Stat)} {StatCatalog.FormatBonus(effect.Stat, passive.Value(i), effect.Multiplicative)}";
			effects = effects.Length == 0 ? line : $"{effects}  ·  {line}";
		}
		row.AddChild(MakeLabel(effects, TextRole.Small, StatBonusColor, HorizontalAlignment.Right));
		_loadoutContainer.AddChild(row);
		// Paliers codés, sous l'objet : dorés une fois atteints, grisés avant.
		foreach (ObjectMilestoneData milestone in passive.Data.Milestones)
		{
			if (!ObjectMilestoneEffects.IsImplemented(milestone.Effect))
				continue;
			MarginContainer indent = new();
			indent.AddThemeConstantOverride("margin_left", 42);
			indent.AddChild(MakeLabel(string.Format(Tr("LEVELUP_MILESTONE"), milestone.Level, milestone.Text), TextRole.Small,
				passive.Reached(milestone) ? GoldBright : TextVeryDim));
			_loadoutContainer.AddChild(indent);
		}
	}

	/// <summary>Perk : nom, règle, puis son état du moment (réserve, fenêtre, cible) ou son inactivité faute d'arme.</summary>
	private void AddPerkRow(PerkSpecializationData perk, Player player)
	{
		VBoxContainer text = new();
		text.AddThemeConstantOverride("separation", 0);
		text.AddChild(MakeLabel(perk.Name, TextRole.Body, StatValueColor));
		Label rule = MakeLabel(perk.Description, TextRole.Small, StatBonusColor);
		rule.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		text.AddChild(rule);
		bool active = PerkSpecializationOffers.IsActive(perk, player);
		string state = active ? PerkState(perk, player) : Tr("PAUSE_PERK_INACTIVE");
		if (state.Length > 0)
			text.AddChild(MakeLabel(state, TextRole.Small, active ? StatValueColor : TextVeryDim));
		_loadoutContainer.AddChild(text);
	}

	private string PerkState(PerkSpecializationData perk, Player player)
	{
		SpecializationRuntime runtime = player.SpecializationRuntime;
		switch (perk.Effect)
		{
			case SpecializationRuntime.OverhealReserveEffect when runtime?.Reserve != null:
				return string.Format(Tr("PAUSE_PERK_RESERVE"), runtime.Reserve.Stock.ToString("0", French), runtime.Reserve.Capacity.ToString("0", French));
			case SpecializationRuntime.RallyEffect when runtime?.Rally != null:
				return runtime.Rally.Recoverable > 0f
					? string.Format(Tr("PAUSE_PERK_RALLY"), runtime.Rally.Recoverable.ToString("0", French), runtime.Rally.Remaining.ToString("0.0", French))
					: Tr("PAUSE_PERK_RALLY_IDLE");
			case SpecializationRuntime.OverflowEffect when runtime?.Overflow != null:
			{
				string ready = "";
				foreach (WeaponInstance weapon in runtime.Overflow.Ready)
				{
					string entry = $"{weapon.Name} +{runtime.Overflow.Amount(weapon).ToString("0", French)}";
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

	/// <summary>Toutes les stats du joueur ; les multiplicateurs se lisent en pourcentage de bonus.</summary>
	private void UpdateSheet(Player player)
	{
		EssenceTracker essenceTracker = GetNodeOrNull<EssenceTracker>("/root/Main/EssenceTracker");
		AddLine(_sheetContainer, Tr("STAT_MAX_HP"), $"{player.CurrentHp:F0} / {player.EffectiveMaxHp:F0}");
		AddLine(_sheetContainer, Tr("STAT_REGEN"), $"{(player.BaseRegenRate + player.BonusRegenRate).ToString("0.0", French)} PV/s");
		// Plus de bouclier de départ (plan 23 R1) : la ligne n'apparaît qu'avec un objet qui en donne.
		if (player.MaxShield > 0f)
			AddLine(_sheetContainer, Tr("STAT_SHIELD"), $"{player.Shield:F0} / {player.MaxShield:F0}");
		AddLine(_sheetContainer, Tr("STAT_ARMOR"), $"{player.Armor:F0}  (−{Percent(player.ArmorReduction)})");
		AddLine(_sheetContainer, Tr("STAT_SPEED"), Bonus(player.SpeedMultiplier));
		AddLine(_sheetContainer, Tr("STAT_DAMAGE"), Bonus(player.DamageMultiplier));
		AddLine(_sheetContainer, Tr("STAT_ATTACK_SPEED"), Bonus(player.AttackSpeedMultiplier));
		AddLine(_sheetContainer, Tr("STAT_CRIT"), $"{Percent(player.CritChance)}  ×{player.CritMultiplier.ToString("0.0", French)}");
		AddLine(_sheetContainer, Tr("STAT_RANGE"), Bonus(player.AttackRangeMultiplier));
		AddLine(_sheetContainer, Tr("STAT_AOE"), Bonus(player.AoeMultiplier));
		AddLine(_sheetContainer, Tr("STAT_STATUS_DURATION"), Bonus(player.StatusDurationMultiplier));
		AddLine(_sheetContainer, Tr("STAT_XP_RANGE"), Bonus(player.XpMagnetMultiplier));
		AddLine(_sheetContainer, Tr("STAT_LUCK"), Percent(player.LuckBonus));
		if (player.AttackCopies > 0)
			AddLine(_sheetContainer, Tr("STAT_ATTACK_COPIES"), $"+{player.AttackCopies}  ({Percent(player.CopyDamageFactor)})");
		if (player.ProjectilePierce > 0)
			AddLine(_sheetContainer, Tr("STAT_PIERCE"), $"+{player.ProjectilePierce}");
		if (essenceTracker != null)
			AddLine(_sheetContainer, "Essence", essenceTracker.CurrentEssence.ToString());
		if (GetNodeOrNull<PerilManager>("/root/Main/PerilManager") is { } peril)
		{
			AddPerilLines(peril.Peril);
			foreach (ActiveOubli oubli in peril.Oublis)
			{
				AddLine(_sheetContainer, "  " + Tr(oubli.Data.NameKey), oubli.Data.Permanent ? Tr("OUBLI_PERMANENT") : "", PerilColor);
				Label effect = MakeLabel("    " + oubli.Data.Describe(), TextRole.Caption, TextVeryDim);
				effect.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				_sheetContainer.AddChild(effect);
			}
		}
	}

	/// <summary>Péril : le niveau, puis ce qu'il coûte et ce qu'il rapporte.</summary>
	private void AddPerilLines(int peril)
	{
		AddLine(_sheetContainer, Tr("STAT_PERIL"), peril.ToString(), peril > 0 ? PerilColor : null);
		if (peril == 0)
			return;
		AddLine(_sheetContainer, "  " + Tr("PERIL_CREATURES"),
			$"{Bonus(PerilDataLoader.EnemyCountMultiplier(peril))} · PV {Bonus(PerilDataLoader.EnemyHpMultiplier(peril))}", TextDim);
		AddLine(_sheetContainer, "  " + Tr("PERIL_REWARDS"),
			$"XP {Bonus(PerilDataLoader.XpMultiplier(peril))} · score {Bonus(PerilDataLoader.ScoreMultiplier(peril))}", TextDim);
	}

	private static readonly System.Globalization.CultureInfo French = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");

	private static string Percent(float fraction) => $"{Mathf.RoundToInt(fraction * 100f)} %";

	/// <summary>Multiplicateur lu en bonus : ×1,15 devient « +15 % », ×1 devient « — ».</summary>
	private static string Bonus(float multiplier)
	{
		int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
		return percent == 0 ? "—" : $"{(percent > 0 ? "+" : "")}{percent} %";
	}

	private static void AddSectionTitle(VBoxContainer container, string text)
	{
		container.AddChild(MakeLabel(text.ToUpper(), TextRole.Caption, TextDim));
	}

	private static void AddLine(VBoxContainer container, string label, string value, Color? color = null)
	{
		HBoxContainer row = new();
		Label name = MakeLabel(label, TextRole.Body, color ?? StatLabelColor);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(name);
		row.AddChild(MakeLabel(value, TextRole.Body, StatValueColor, HorizontalAlignment.Right));
		container.AddChild(row);
	}

	private static Label MakeLabel(string text, TextRole role, Color color, HorizontalAlignment align = HorizontalAlignment.Left) =>
		UITheme.MakeLabel(text, role, color, TextWeight.Regular, align);

	private static Control MakeIcon(string path)
	{
		TextureRect icon = new()
		{
			CustomMinimumSize = new Vector2(32, 32),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		if (!string.IsNullOrEmpty(path))
		{
			string resPath = path.StartsWith("res://") ? path : $"res://{path}";
			if (ResourceLoader.Exists(resPath))
				icon.Texture = GD.Load<Texture2D>(resPath);
		}
		return icon;
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
		UITheme.SetTextRole(btn, TextRole.Subhead);
		btn.AddThemeConstantOverride("h_separation", 6);
		UITheme.ApplyButtonStyle(btn, _btnNormalTex, _btnHoverTex, _btnPressedTex, _btnDisabledTex);
		return btn;
	}
}
