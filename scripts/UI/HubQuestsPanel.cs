using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Page Quêtes de l'accueil : une seule liste de lignes compactes, sans filtre (DECISIONS §117). Les quêtes en cours
/// viennent d'abord, les accomplies ensuite, en doré pâle avec leur médaille ; la fiche à droite porte le détail.
/// </summary>
public partial class HubQuestsPanel : MarginContainer
{
    [Signal] public delegate void RewardRequestedEventHandler(int kind, string id);
    public string ExaminedQuestId { get; private set; }
    private bool _needsRefresh = true;

    private const float ListWidth = 1000f;
    private const float RowHeight = 64f;
    private const float IconSize = 44f;
    private const float NameWidth = 290f;
    private const float StarsWidth = 84f;
    private const float GaugeWidth = 120f;
    private const float CompletionIconSize = 32f;
    private static readonly Color DoneTint = new(0.93f, 0.84f, 0.6f);

    private readonly List<Button> _rows = new();
    private ScrollContainer _scroll;
    private VBoxContainer _list;
    private VBoxContainer _detail;
    private Texture2D _completionTexture;
    private HBoxContainer _detailProgressRow;
    private TextureRect _detailCompletion;
    private Label _detailName;
    private Label _detailStars;
    private Label _detailText;
    private Label _detailProgress;
    private VBoxContainer _detailRewards;

    public override void _Ready()
    {
        _completionTexture = UITheme.LoadTex(ScreenArt.Folder + "ui_quest_complete.png");
        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 32);
        AddChild(body);
        _scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(ListWidth + 24f, 0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        body.AddChild(_scroll);
        _list = new VBoxContainer { CustomMinimumSize = new Vector2(ListWidth, 0f) };
        _list.AddThemeConstantOverride("separation", 6);
        _scroll.AddChild(_list);

        _detail = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _detail.AddThemeConstantOverride("separation", 14);
        PanelContainer detailPanel = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        detailPanel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        body.AddChild(detailPanel);
        ScrollContainer detailScroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        detailPanel.AddChild(detailScroll);
        detailScroll.AddChild(_detail);
        _detailName = AddLabel(_detail, "", TextRole.Title, UITheme.TextLight, TextWeight.Strong);
        _detailStars = AddLabel(_detail, "", TextRole.Heading, UITheme.GoldColor, TextWeight.Strong);
        _detailText = AddLabel(_detail, "", TextRole.Heading, UITheme.TextLight);
        _detailProgressRow = new HBoxContainer();
        _detailProgressRow.AddThemeConstantOverride("separation", 10);
        _detail.AddChild(_detailProgressRow);
        _detailCompletion = CompletionIcon();
        _detailCompletion.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _detailProgressRow.AddChild(_detailCompletion);
        _detailProgress = UITheme.MakeLabel("", TextRole.Heading, UITheme.CyanEssence, TextWeight.Strong, outline: 4);
        _detailProgress.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailProgress.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _detailProgressRow.AddChild(_detailProgress);
        _detailRewards = new VBoxContainer();
        _detailRewards.AddThemeConstantOverride("separation", 10);
        _detail.AddChild(_detailRewards);
    }

    private static Label AddLabel(Container parent, string text, TextRole role, Color color, TextWeight weight = TextWeight.Regular)
    {
        Label label = UITheme.MakeLabel(text, role, color, weight, outline: 4);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(360f, 0f);
        parent.AddChild(label);
        return label;
    }

    /// <summary>Reconstruit la liste à l'ouverture ; renvoie la ligne à focaliser.</summary>
    public Control Refresh(string focusId = null)
    {
        if (!_needsRefresh && _rows.Count > 0)
        {
            Button current = _rows.Find(row => row.Name == (focusId ?? ExaminedQuestId)) ?? _rows[0];
            if (focusId != null)
                ShowDetail(QuestDataLoader.Get(focusId));
            return current;
        }
        _needsRefresh = false;
        ClearChildren(_list);
        _rows.Clear();
        _scroll.ScrollVertical = 0;
        List<QuestDefinition> open = new();
        List<QuestDefinition> done = new();
        foreach (QuestDefinition quest in QuestDataLoader.GetAll())
            (MetaSaveManager.HasCompletedQuest(quest.Id) ? done : open).Add(quest);
        open.Sort(CompareQuests);
        done.Sort(CompareQuests);
        foreach (QuestDefinition quest in open)
            AddRow(quest, false);
        if (done.Count > 0)
        {
            Label divider = UITheme.MakeLabel($"Accomplies · {done.Count}", TextRole.Subhead, UITheme.GoldColor, TextWeight.Strong, outline: 4);
            divider.CustomMinimumSize = new Vector2(0f, 48f);
            divider.VerticalAlignment = VerticalAlignment.Bottom;
            _list.AddChild(divider);
            foreach (QuestDefinition quest in done)
                AddRow(quest, true);
        }
        _detail.Visible = _rows.Count > 0;
        if (_rows.Count == 0)
            return null;
        Button selected = _rows.Find(row => row.Name == (focusId ?? ExaminedQuestId)) ?? _rows[0];
        ShowDetail(QuestDataLoader.Get(selected.Name));
        return selected;
    }

    // Un ordre stable (récompense, difficulté, identifiant) permet de retrouver une quête d'une visite à l'autre.
    private static int CompareQuests(QuestDefinition a, QuestDefinition b)
    {
        int kind = a.Unlocks[0].Kind.CompareTo(b.Unlocks[0].Kind);
        if (kind != 0)
            return kind;
        int difficulty = a.Difficulty.CompareTo(b.Difficulty);
        return difficulty != 0 ? difficulty : string.CompareOrdinal(a.Id, b.Id);
    }

    private void AddRow(QuestDefinition quest, bool done)
    {
        Button row = new()
        {
            Name = quest.Id,
            CustomMinimumSize = new Vector2(ListWidth, RowHeight),
            FocusMode = FocusModeEnum.All,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        StyleBoxTexture rest = done ? MenuSkin.AcquiredCard() : UITheme.CreateNinePatch(MenuSkin.TextureFor("ui_card_normal.png"), 8, 8, 8, 8);
        StyleBoxTexture lit = MenuSkin.Choice(done ? UITheme.GoldBright : UITheme.CyanEssence, true);
        row.AddThemeStyleboxOverride("normal", rest);
        foreach (string state in new[] { "pressed", "hover", "focus", "hover_pressed" })
            row.AddThemeStyleboxOverride(state, lit);
        HBoxContainer line = new() { MouseFilter = MouseFilterEnum.Ignore };
        line.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        line.OffsetLeft = 18f;
        line.OffsetRight = -18f;
        line.AddThemeConstantOverride("separation", 16);
        row.AddChild(line);
        line.AddChild(new TextureRect
        {
            Texture = RewardIcon(quest),
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        line.AddChild(RowLabel(quest.Name, TextRole.Heading, done ? DoneTint : UITheme.TextLight, NameWidth, TextWeight.Strong));
        line.AddChild(RowLabel(Stars(quest.Difficulty), TextRole.Heading, UITheme.GoldColor, StarsWidth, TextWeight.Strong));
        Label objective = RowLabel(quest.Description, TextRole.Lead, UITheme.TextColor, 0f);
        objective.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        line.AddChild(objective);
        if (done)
        {
            TextureRect completion = CompletionIcon();
            completion.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            line.AddChild(completion);
        }
        else
            line.AddChild(Gauge(QuestBook.Fraction(quest, MetaSaveManager.GetQuestProgress(quest.Id))));
        row.FocusEntered += () => ShowDetail(quest);
        row.MouseEntered += () => ShowDetail(quest);
        row.Pressed += () => { ShowDetail(quest); if (_detailRewards.GetChildCount() > 1) (_detailRewards.GetChild(1) as Control)?.GrabFocus(); };
        UITheme.WireButtonAudio(row);
        _list.AddChild(row);
        _rows.Add(row);
    }

    private static Label RowLabel(string text, TextRole role, Color color, float width, TextWeight weight = TextWeight.Regular)
    {
        Label label = UITheme.MakeLabel(text, role, color, weight, outline: 4);
        label.CustomMinimumSize = new Vector2(width, 0f);
        label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        label.ClipText = true;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        return label;
    }

    private static Control Gauge(float fraction)
    {
        ColorRect track = new()
        {
            Color = new Color(UITheme.BgDark, 0.9f),
            CustomMinimumSize = new Vector2(GaugeWidth, 8f),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        track.AddChild(new ColorRect
        {
            Color = UITheme.CyanEssence,
            Size = new Vector2(Mathf.Round(GaugeWidth * Mathf.Clamp(fraction, 0f, 1f)), 8f),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        return track;
    }

    private TextureRect CompletionIcon() => new()
    {
        Texture = _completionTexture,
        CustomMinimumSize = new Vector2(CompletionIconSize, CompletionIconSize),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private void ShowDetail(QuestDefinition quest)
    {
        if (quest == null) return;
        ExaminedQuestId = quest.Id;
        bool done = MetaSaveManager.HasCompletedQuest(quest.Id);
        _detailName.Text = quest.Name;
        _detailStars.Text = Stars(quest.Difficulty);
        _detailText.Text = quest.Description;
        bool hasQuantity = quest.Conditions.Exists(condition => condition.Target > 1f);
        _detailProgressRow.Visible = done || hasQuantity;
        _detailCompletion.Visible = done;
        _detailProgress.Text = done ? "Accomplie" : QuestBook.ProgressText(quest, MetaSaveManager.GetQuestProgress(quest.Id));
        _detailProgress.AddThemeColorOverride("font_color", done ? UITheme.GoldBright : UITheme.CyanEssence);
        ClearChildren(_detailRewards);
        AddLabel(_detailRewards, done ? "Récompense débloquée" : "Récompense", TextRole.Subhead, UITheme.TextDim);
        foreach (QuestUnlock unlock in quest.Unlocks)
        {
            Button reward = MenuSkin.Button(QuestBook.UnlockName(unlock) + "  →");
            reward.Alignment = HorizontalAlignment.Left;
            reward.Disabled = unlock.Kind == UnlockKind.Character && CharacterDataLoader.Get(unlock.Id) == null;
            if (reward.Disabled) reward.Text = QuestBook.UnlockName(unlock) + " · À venir";
            reward.Icon = UnlockIcon(unlock);
            reward.ExpandIcon = true;
            reward.AddThemeConstantOverride("icon_max_width", 48);
            reward.Pressed += () => EmitSignal(SignalName.RewardRequested, (int)unlock.Kind, unlock.Id);
            _detailRewards.AddChild(reward);
        }
    }

    private static void ClearChildren(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static Texture2D RewardIcon(QuestDefinition quest)
    {
        foreach (QuestUnlock unlock in quest.Unlocks)
        {
            Texture2D icon = UnlockIcon(unlock);
            if (icon != null)
                return icon;
        }
        return null;
    }

    private static Texture2D UnlockIcon(QuestUnlock unlock)
    {
        if (unlock.Kind == UnlockKind.Character)
            return CharacterPortrait.Idle(unlock.Id);
        string path = QuestBook.UnlockIcon(unlock);
        return !string.IsNullOrEmpty(path) && ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    private static string Stars(int difficulty) => new('★', difficulty);
}
