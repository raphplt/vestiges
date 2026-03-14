using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

public class QuestProgressSnapshot
{
    public QuestDefinition Definition { get; set; }
    public float Progress { get; set; }
    public bool IsComplete { get; set; }
    public bool IsClaimed { get; set; }
    public string ProgressLabel { get; set; }
    public string RewardLabel { get; set; }
}

public partial class QuestManager : CanvasLayer
{
    private sealed class ActiveRunQuest
    {
        public QuestDefinition Definition { get; init; }
        public float Progress { get; set; }
        public bool Completed { get; set; }
    }

    private const int MaxRunQuests = 3;
    private const float RefreshInterval = 0.25f;

    private readonly List<ActiveRunQuest> _activeRunQuests = new();

    private EventBus _eventBus;
    private RunTracker _runTracker;
    private EssenceTracker _essenceTracker;
    private VBoxContainer _questList;
    private Label _toastLabel;
    private float _refreshTimer;
    private float _toastTimer;
    private int _essenceCollectedTotal;

    public override void _Ready()
    {
        QuestDataLoader.Load();
        EnemyDataLoader.Load();
        SouvenirDataLoader.Load();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _runTracker = GetParent().GetNodeOrNull<RunTracker>("RunTracker");
        _essenceTracker = GetParent().GetNodeOrNull<EssenceTracker>("EssenceTracker");

        BuildUi();
        SelectRunQuests();
        RefreshQuestList();

        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.CrisisEnded += OnCrisisEnded;
        _eventBus.PoiExplored += OnPoiExplored;
        _eventBus.ChestOpened += OnChestOpened;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.LootReceived += OnLootReceived;
        _eventBus.RunPhaseChanged += OnRunPhaseChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;

        _eventBus.EnemyKilled -= OnEnemyKilled;
        _eventBus.CrisisEnded -= OnCrisisEnded;
        _eventBus.PoiExplored -= OnPoiExplored;
        _eventBus.ChestOpened -= OnChestOpened;
        _eventBus.LevelUp -= OnLevelUp;
        _eventBus.LootReceived -= OnLootReceived;
        _eventBus.RunPhaseChanged -= OnRunPhaseChanged;
    }

    public override void _Process(double delta)
    {
        _refreshTimer += (float)delta;
        if (_refreshTimer >= RefreshInterval)
        {
            _refreshTimer = 0f;
            float elapsed = _runTracker?.RunDurationSeconds ?? 0f;
            SetAbsoluteProgress("survive_duration_sec", elapsed);
        }

        if (_toastTimer <= 0f || _toastLabel == null)
            return;

        _toastTimer -= (float)delta;
        if (_toastTimer <= 0f)
            _toastLabel.Visible = false;
    }

    public static List<string> ResolvePendingProgressionQuests(RunRecord latestRecord = null)
    {
        QuestDataLoader.Load();
        SouvenirDataLoader.Load();
        CharacterDataLoader.Load();
        MetaSaveManager.Load();
        RunHistoryManager.Load();

        List<string> completions = new();
        foreach (QuestDefinition definition in QuestDataLoader.GetByCategory("progression"))
        {
            if (MetaSaveManager.HasCompletedQuest(definition.Id))
                continue;

            QuestProgressSnapshot snapshot = EvaluateProgressionQuest(definition, latestRecord);
            if (!snapshot.IsComplete)
                continue;

            ApplyProgressionReward(definition);
            MetaSaveManager.CompleteQuest(definition.Id);
            completions.Add($"{definition.Name} — {GetRewardSummary(definition)}");
        }

        return completions;
    }

    public static List<QuestProgressSnapshot> GetProgressionSnapshots(RunRecord latestRecord = null)
    {
        QuestDataLoader.Load();
        List<QuestProgressSnapshot> snapshots = new();
        foreach (QuestDefinition definition in QuestDataLoader.GetByCategory("progression"))
            snapshots.Add(EvaluateProgressionQuest(definition, latestRecord));
        return snapshots;
    }

    public static string GetRewardSummary(QuestDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.RewardLabel))
            return definition.RewardLabel;

        return definition.RewardType switch
        {
            "vestiges" => $"+{definition.RewardAmount} Vestiges",
            "essence" => $"+{definition.RewardAmount} Essence",
            "xp" => $"+{definition.RewardAmount} XP",
            "souvenir" => $"Souvenir: {SouvenirDataLoader.Get(definition.RewardId)?.Name ?? definition.RewardId}",
            "character_unlock" => $"Personnage: {CharacterDataLoader.Get(definition.RewardId)?.Name ?? definition.RewardId}",
            _ => "Recompense inconnue"
        };
    }

    private void BuildUi()
    {
        PanelContainer panel = new()
        {
            Name = "QuestPanel"
        };
        panel.AnchorLeft = 1f;
        panel.AnchorRight = 1f;
        panel.AnchorTop = 0f;
        panel.AnchorBottom = 0f;
        panel.OffsetLeft = -312;
        panel.OffsetRight = -16;
        panel.OffsetTop = 104;
        panel.OffsetBottom = 260;

        StyleBoxFlat style = new()
        {
            BgColor = new Color(0.04f, 0.06f, 0.11f, 0.84f),
            BorderColor = new Color(0.26f, 0.52f, 0.56f, 0.8f),
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginBottom = 10,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10
        };
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 6);
        panel.AddChild(content);

        Label title = new()
        {
            Text = "QUETES DE RUN"
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        title.AddThemeColorOverride("font_color", new Color(0.82f, 0.94f, 0.98f));
        content.AddChild(title);

        _questList = new VBoxContainer();
        _questList.AddThemeConstantOverride("separation", 4);
        content.AddChild(_questList);

        _toastLabel = new Label
        {
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _toastLabel.AddThemeFontSizeOverride("font_size", 13);
        _toastLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.88f, 0.54f));
        content.AddChild(_toastLabel);
    }

    private void SelectRunQuests()
    {
        List<QuestDefinition> pool = QuestDataLoader.GetByCategory("run");
        if (pool.Count == 0)
            return;

        RandomNumberGenerator rng = new();
        rng.Randomize();

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.RandiRange(0, i);
            (pool[i], pool[swapIndex]) = (pool[swapIndex], pool[i]);
        }

        for (int i = 0; i < Mathf.Min(MaxRunQuests, pool.Count); i++)
        {
            _activeRunQuests.Add(new ActiveRunQuest
            {
                Definition = pool[i],
                Progress = 0f,
                Completed = false
            });
        }
    }

    private void RefreshQuestList()
    {
        foreach (Node child in _questList.GetChildren())
            child.QueueFree();

        foreach (ActiveRunQuest quest in _activeRunQuests)
        {
            Label name = new()
            {
                Text = $"{(quest.Completed ? "[OK]" : "[ ]")} {quest.Definition.Name}"
            };
            name.AddThemeFontSizeOverride("font_size", 14);
            name.AddThemeColorOverride("font_color", quest.Completed
                ? new Color(0.64f, 0.94f, 0.66f)
                : new Color(0.90f, 0.92f, 0.95f));
            _questList.AddChild(name);

            Label details = new()
            {
                Text = $"{quest.Definition.Description}\n{FormatProgress(quest.Definition, quest.Progress)}  |  {GetRewardSummary(quest.Definition)}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            details.AddThemeFontSizeOverride("font_size", 11);
            details.AddThemeColorOverride("font_color", quest.Completed
                ? new Color(0.56f, 0.86f, 0.66f)
                : new Color(0.62f, 0.72f, 0.82f));
            _questList.AddChild(details);
        }
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        AddProgress("kill_total", 1f);

        EnemyData data = EnemyDataLoader.Get(enemyId);
        int essenceGain = data?.Tier switch
        {
            "boss" => 8,
            "elite" => 4,
            _ => 1
        };
        _essenceCollectedTotal += essenceGain;
        SetAbsoluteProgress("collect_essence", _essenceCollectedTotal);

        if (enemyId == "indicible")
            SetAbsoluteProgress("defeat_boss", 1f);
    }

    private void OnCrisisEnded(int crisisNumber)
    {
        SetAbsoluteProgress("survive_crises", crisisNumber);
    }

    private void OnPoiExplored(string poiId, string poiType)
    {
        AddProgress("explore_poi", 1f);
    }

    private void OnChestOpened(string chestId, string rarity, Vector2 position)
    {
        AddProgress("open_chests", 1f);
    }

    private void OnLevelUp(int newLevel)
    {
        SetAbsoluteProgress("reach_level", newLevel);
    }

    private void OnLootReceived(string itemType, string itemId, int amount)
    {
        if (itemType != "essence" || amount <= 0)
            return;

        _essenceCollectedTotal += amount;
        SetAbsoluteProgress("collect_essence", _essenceCollectedTotal);
    }

    private void OnRunPhaseChanged(string oldPhase, string newPhase)
    {
        if (newPhase == GameManager.RunPhase.Endgame.ToString())
            SetAbsoluteProgress("reach_endgame", 1f);
    }

    private void AddProgress(string objectiveType, float delta)
    {
        bool changed = false;
        foreach (ActiveRunQuest quest in _activeRunQuests)
        {
            if (quest.Completed || quest.Definition.ObjectiveType != objectiveType)
                continue;

            changed |= UpdateQuestProgress(quest, quest.Progress + delta);
        }

        if (changed)
            RefreshQuestList();
    }

    private void SetAbsoluteProgress(string objectiveType, float value)
    {
        bool changed = false;
        foreach (ActiveRunQuest quest in _activeRunQuests)
        {
            if (quest.Completed || quest.Definition.ObjectiveType != objectiveType)
                continue;

            changed |= UpdateQuestProgress(quest, value);
        }

        if (changed)
            RefreshQuestList();
    }

    private bool UpdateQuestProgress(ActiveRunQuest quest, float value)
    {
        float clamped = Mathf.Clamp(value, 0f, quest.Definition.Target);
        if (clamped <= quest.Progress + 0.001f && !(clamped >= quest.Definition.Target && !quest.Completed))
            return false;

        quest.Progress = clamped;
        if (!quest.Completed && quest.Progress >= quest.Definition.Target)
            CompleteRunQuest(quest);
        return true;
    }

    private void CompleteRunQuest(ActiveRunQuest quest)
    {
        quest.Completed = true;

        switch (quest.Definition.RewardType)
        {
            case "essence":
                _essenceTracker?.AddEssence(quest.Definition.RewardAmount);
                break;
            case "xp":
                _eventBus?.EmitSignal(EventBus.SignalName.XpGained, (float)quest.Definition.RewardAmount);
                break;
        }

        _toastLabel.Text = $"Quete accomplie: {quest.Definition.Name}\n{GetRewardSummary(quest.Definition)}";
        _toastLabel.Visible = true;
        _toastTimer = 4f;
    }

    private static QuestProgressSnapshot EvaluateProgressionQuest(QuestDefinition definition, RunRecord latestRecord)
    {
        float progress = GetProgressValue(definition, latestRecord);
        bool complete = progress >= definition.Target;
        bool claimed = MetaSaveManager.HasCompletedQuest(definition.Id);

        return new QuestProgressSnapshot
        {
            Definition = definition,
            Progress = Mathf.Min(progress, definition.Target),
            IsComplete = complete,
            IsClaimed = claimed,
            ProgressLabel = FormatProgress(definition, progress),
            RewardLabel = GetRewardSummary(definition)
        };
    }

    private static float GetProgressValue(QuestDefinition definition, RunRecord latestRecord)
    {
        MetaStats stats = MetaSaveManager.GetStats();
        return definition.ObjectiveType switch
        {
            "best_run_duration_sec" => Mathf.Max(stats.BestRunDurationSec, latestRecord?.RunDurationSec ?? 0f),
            "max_kills_in_run" => Mathf.Max(stats.MaxKillsInRun, latestRecord?.TotalKills ?? 0),
            "total_crises_survived" => Mathf.Max(stats.TotalCrisesSurvived, latestRecord?.CrisesSurvived ?? 0),
            "discover_souvenirs" => MetaSaveManager.GetDiscoveredSouvenirCount(),
            "defeat_boss" => HasBossDefeat(latestRecord) ? 1f : 0f,
            "reach_endgame" => HasEndgameReached(latestRecord) ? 1f : 0f,
            _ => 0f
        };
    }

    private static void ApplyProgressionReward(QuestDefinition definition)
    {
        switch (definition.RewardType)
        {
            case "souvenir":
                MetaSaveManager.DiscoverSouvenir(definition.RewardId);
                break;
            case "vestiges":
                MetaSaveManager.AddVestiges(definition.RewardAmount);
                break;
            case "character_unlock":
                MetaSaveManager.UnlockCharacter(definition.RewardId);
                break;
        }
    }

    private static bool HasBossDefeat(RunRecord latestRecord)
    {
        if (latestRecord?.BossDefeated == true)
            return true;

        foreach (RunRecord run in RunHistoryManager.GetHistory())
        {
            if (run.BossDefeated)
                return true;
        }

        return false;
    }

    private static bool HasEndgameReached(RunRecord latestRecord)
    {
        if (latestRecord?.EndgameReached == true)
            return true;

        foreach (RunRecord run in RunHistoryManager.GetHistory())
        {
            if (run.EndgameReached)
                return true;
        }

        return false;
    }

    private static string FormatProgress(QuestDefinition definition, float progress)
    {
        if (definition.ObjectiveType is "defeat_boss" or "reach_endgame")
            return progress >= definition.Target ? "Accomplie" : "Non accomplie";

        if (definition.ObjectiveType is "best_run_duration_sec" or "survive_duration_sec")
            return $"{FormatDuration(progress)} / {FormatDuration(definition.Target)}";

        return $"{Mathf.FloorToInt(progress)} / {Mathf.FloorToInt(definition.Target)}";
    }

    private static string FormatDuration(float durationSec)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(durationSec));
        int minutes = total / 60;
        int seconds = total % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
