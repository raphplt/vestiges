using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Chroniques du Hub : records, statistiques par personnage, endurance et quêtes.
/// Reconstruit son contenu à chaque changement de sous-onglet.
/// </summary>
public partial class HubChroniquesPanel : MarginContainer
{
	private static readonly Color GoldBright = UITheme.GoldBright;
	private static readonly Color GoldDim = UITheme.GoldDim;
	private static readonly Color TextColor = UITheme.TextColor;
	private static readonly Color TextDim = UITheme.TextDim;
	private static readonly Color TextVeryDim = UITheme.TextVeryDim;

	private string _currentChroniquesSubTab = "global";
	private string _selectedCharacterId;
	private Font _tabFont;

	public override void _Ready()
	{
		_tabFont = GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf");
	}

	/// <summary>Reconstruit le panneau ; les lignes du personnage choisi ressortent.</summary>
	public void Refresh(string selectedCharacterId)
	{
		_selectedCharacterId = selectedCharacterId;
		foreach (Node child in GetChildren())
			child.QueueFree();
		AddChild(BuildChroniquesContent());
	}

	/// <summary>Onglet textuel, souligné d'un trait de 4 px quand il est actif.</summary>
	private void ApplyTabStyle(Button btn, bool active)
	{
		StyleBoxFlat underline = new()
		{
			BgColor = new Color(0f, 0f, 0f, 0f),
			BorderColor = UITheme.GoldColor,
			BorderWidthBottom = active ? 4 : 0,
			ContentMarginLeft = 8f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 6f
		};
		StyleBoxFlat hover = (StyleBoxFlat)underline.Duplicate();
		hover.BorderWidthBottom = 4;
		hover.BorderColor = active ? UITheme.GoldColor : new Color(UITheme.GoldDim, 0.6f);
		foreach (string state in new[] { "normal", "pressed", "focus" })
			btn.AddThemeStyleboxOverride(state, underline);
		btn.AddThemeStyleboxOverride("hover", hover);
		btn.AddThemeStyleboxOverride("hover_pressed", hover);
		btn.AddThemeFontOverride("font", _tabFont);
		btn.AddThemeFontSizeOverride("font_size", 26);
		btn.AddThemeColorOverride("font_color", active ? GoldBright : TextDim);
		btn.AddThemeColorOverride("font_hover_color", GoldBright);
		btn.AddThemeColorOverride("font_pressed_color", GoldBright);
		btn.AddThemeColorOverride("font_hover_pressed_color", GoldBright);
		btn.AddThemeColorOverride("font_focus_color", active ? GoldBright : TextDim);
		UITheme.WireButtonAudio(btn);
	}

	private MarginContainer BuildChroniquesContent()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 0);
		margin.AddThemeConstantOverride("margin_right", 0);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);

		VBoxContainer vbox = new();
		vbox.AddThemeConstantOverride("separation", 10);
		vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		margin.AddChild(vbox);

		HBoxContainer subTabRow = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		subTabRow.AddThemeConstantOverride("separation", 40);
		vbox.AddChild(subTabRow);

		CreateChroniquesSubTab(subTabRow, "global", "Global");
		CreateChroniquesSubTab(subTabRow, "personnage", "Personnages");
		CreateChroniquesSubTab(subTabRow, "endurance", "Endurance");
		CreateChroniquesSubTab(subTabRow, "quetes", "Quêtes");

		vbox.AddChild(new ColorRect { Color = new Color(GoldDim, 0.45f), CustomMinimumSize = new Vector2(0f, 4f) });

		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		vbox.AddChild(scroll);

		VBoxContainer scrollContent = new();
		scrollContent.AddThemeConstantOverride("separation", 6);
		scrollContent.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(scrollContent);

		switch (_currentChroniquesSubTab)
		{
			case "global":
				BuildChroniquesGlobal(scrollContent);
				break;
			case "personnage":
				BuildChroniquesPerso(scrollContent);
				break;
			case "endurance":
				BuildChroniquesNuits(scrollContent);
				break;
			case "quetes":
				BuildChroniquesQuetes(scrollContent);
				break;
		}

		return margin;
	}

	private void CreateChroniquesSubTab(HBoxContainer parent, string tabId, string label)
	{
		bool active = tabId == _currentChroniquesSubTab;

		Button btn = new()
		{
			Text = label,
			CustomMinimumSize = new Vector2(140, 36),
			ToggleMode = true,
			ButtonPressed = active,
			Flat = true
		};
		ApplyTabStyle(btn, active);

		string capturedId = tabId;
		btn.Pressed += () =>
		{
			_currentChroniquesSubTab = capturedId;
			Refresh(_selectedCharacterId);
		};

		parent.AddChild(btn);
	}

	private void BuildChroniquesGlobal(VBoxContainer container)
	{
		int bestScore = RunHistoryManager.GetBestScore();
		float longestRun = RunHistoryManager.GetLongestRunDurationSec();
		if (bestScore > 0 || longestRun > 0f)
		{
			Label record = new()
			{
				Text = $"Record : {bestScore:N0}  |  Plus longue run : {FormatDuration(longestRun)}",
				HorizontalAlignment = HorizontalAlignment.Center
			};
			record.AddThemeFontSizeOverride("font_size", 22);
			record.AddThemeColorOverride("font_color", GoldBright);
			container.AddChild(record);
		}

		List<RunRecord> topRuns = RunHistoryManager.GetTopByScore(10);
		if (topRuns.Count == 0)
		{
			AddEmptyLabel(container);
			return;
		}

		for (int i = 0; i < topRuns.Count; i++)
		{
			RunRecord run = topRuns[i];
			string charName = TruncName(run.CharacterName ?? run.CharacterId);

			Label runLabel = new()
			{
				Text = $"#{i + 1}  {charName} — {run.Score:N0} — {FormatDuration(run.RunDurationSec)} — {run.CrisesSurvived} crises"
			};
			runLabel.AddThemeFontSizeOverride("font_size", 16);

			bool isCurrentChar = run.CharacterId == _selectedCharacterId;
			runLabel.AddThemeColorOverride("font_color", isCurrentChar ? GoldDim : TextDim);
			container.AddChild(runLabel);
		}
	}

	private void BuildChroniquesPerso(VBoxContainer container)
	{
		Dictionary<string, RunAnalytics.CharacterRunStats> charStats = RunAnalytics.GetCharacterStats();
		if (charStats.Count == 0)
		{
			AddEmptyLabel(container);
			return;
		}

		foreach (KeyValuePair<string, RunAnalytics.CharacterRunStats> pair in charStats)
		{
			CharacterData charData = CharacterDataLoader.Get(pair.Key);
			string charName = charData?.Name ?? pair.Key;

			Label nameLabel = new()
			{
				Text = charName
			};
			nameLabel.AddThemeFontSizeOverride("font_size", 18);
			nameLabel.AddThemeColorOverride("font_color", charData?.VisualColor ?? TextColor);
			container.AddChild(nameLabel);

			Label statsLabel = new()
			{
				Text = $"  Record: {pair.Value.BestScore:N0} — Moy: {pair.Value.AvgScore:N0} — Durée moy: {FormatDuration(pair.Value.AvgDurationSec)} — Crises moy: {pair.Value.AvgCrises:F1}"
			};
			statsLabel.AddThemeFontSizeOverride("font_size", 14);
			statsLabel.AddThemeColorOverride("font_color", TextDim);
			container.AddChild(statsLabel);
		}
	}

	private void BuildChroniquesNuits(VBoxContainer container)
	{
		float longestRun = RunHistoryManager.GetLongestRunDurationSec();
		int maxCrises = RunHistoryManager.GetMaxCrises();
		if (longestRun > 0f)
		{
			Label record = new()
			{
				Text = $"Endurance : {FormatDuration(longestRun)}  |  Crises max : {maxCrises}",
				HorizontalAlignment = HorizontalAlignment.Center
			};
			record.AddThemeFontSizeOverride("font_size", 22);
			record.AddThemeColorOverride("font_color", GoldBright);
			container.AddChild(record);
		}

		List<RunRecord> topRuns = RunHistoryManager.GetTopByDuration(10);
		if (topRuns.Count == 0)
		{
			AddEmptyLabel(container);
			return;
		}

		for (int i = 0; i < topRuns.Count; i++)
		{
			RunRecord run = topRuns[i];
			string charName = TruncName(run.CharacterName ?? run.CharacterId);

			Label runLabel = new()
			{
				Text = $"#{i + 1}  {charName} — {FormatDuration(run.RunDurationSec)} — {run.CrisesSurvived} crises — {run.Score:N0}"
			};
			runLabel.AddThemeFontSizeOverride("font_size", 16);

			bool isCurrentChar = run.CharacterId == _selectedCharacterId;
			runLabel.AddThemeColorOverride("font_color", isCurrentChar ? GoldDim : TextDim);
			container.AddChild(runLabel);
		}
	}

	private void BuildChroniquesQuetes(VBoxContainer container)
	{
		List<QuestProgressSnapshot> progressionQuests = QuestManager.GetProgressionSnapshots();
		int completedCount = 0;
		foreach (QuestProgressSnapshot snapshot in progressionQuests)
		{
			if (snapshot.IsClaimed)
				completedCount++;
		}

		Label summary = new()
		{
			Text = $"Progression : {completedCount}/{progressionQuests.Count} objectifs gravés dans la pierre",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		summary.AddThemeFontSizeOverride("font_size", 22);
		summary.AddThemeColorOverride("font_color", GoldBright);
		container.AddChild(summary);

		container.AddChild(CreateQuestSectionTitle("Progression permanente"));
		foreach (QuestProgressSnapshot snapshot in progressionQuests)
		{
			Label title = new()
			{
				Text = $"{(snapshot.IsClaimed ? "[Terminée]" : "[En cours]")} {snapshot.Definition.Name}"
			};
			title.AddThemeFontSizeOverride("font_size", 17);
			title.AddThemeColorOverride("font_color", snapshot.IsClaimed ? GoldBright : TextColor);
			container.AddChild(title);

			Label details = new()
			{
				Text = $"{snapshot.Definition.Description}\nAvancement : {snapshot.ProgressLabel}  |  {snapshot.RewardLabel}",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			details.AddThemeFontSizeOverride("font_size", 14);
			details.AddThemeColorOverride("font_color", snapshot.IsClaimed ? GoldDim : TextDim);
			container.AddChild(details);
		}

		container.AddChild(CreateQuestSectionTitle("Quêtes de run"));

		Label intro = new()
		{
			Text = "Trois quêtes sont tirées au hasard au début de chaque run. Elles offrent un coup de pouce immediat en Essence ou en XP.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		intro.AddThemeFontSizeOverride("font_size", 14);
		intro.AddThemeColorOverride("font_color", TextDim);
		container.AddChild(intro);

		foreach (QuestDefinition definition in QuestDataLoader.GetByCategory("run"))
		{
			Label entry = new()
			{
				Text = $"{definition.Name} — {definition.Description}  |  {QuestManager.GetRewardSummary(definition)}",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			entry.AddThemeFontSizeOverride("font_size", 15);
			entry.AddThemeColorOverride("font_color", TextColor);
			container.AddChild(entry);
		}
	}

	private Label CreateQuestSectionTitle(string text)
	{
		Label label = new()
		{
			Text = text
		};
		label.AddThemeFontSizeOverride("font_size", 18);
		label.AddThemeColorOverride("font_color", GoldDim);
		return label;
	}

	private static string FormatDuration(float seconds)
	{
		int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
		int minutes = totalSeconds / 60;
		int remainingSeconds = totalSeconds % 60;
		return $"{minutes}:{remainingSeconds:D2}";
	}

	private static string TruncName(string name)
	{
		if (string.IsNullOrEmpty(name)) return "?";
		return name.Length > 10 ? name[..10] : name;
	}

	private void AddEmptyLabel(VBoxContainer container)
	{
		Label empty = new()
		{
			Text = "Pas encore d'historique.",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		empty.AddThemeFontSizeOverride("font_size", 16);
		empty.AddThemeColorOverride("font_color", TextVeryDim);
		container.AddChild(empty);
	}
}
