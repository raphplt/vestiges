using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Page Quêtes de l'accueil (plan 06 Q3b) : une grille commune, filtrable par statut. Les personnages, armes et
/// objets sont repérables sur les cartes ; le détail au survol/focus porte la condition et l'avancée utile.
/// </summary>
public partial class HubQuestsPanel : MarginContainer
{
    private enum QuestFilter { All, Open, Done }

    private const int Columns = 4;
    private const float CardWidth = 236f;
    private const float CardHeight = 244f;
    private const float Gap = 12f;
    private const float IconSize = 56f;
    private const float CompletionIconSize = 32f;

    private QuestFilter _filter = QuestFilter.All;
    private readonly List<Button> _filters = new();
    private readonly List<Button> _cards = new();
    private Button _activeFilter;
    private ScrollContainer _scroll;
    private GridContainer _grid;
    private Label _empty;
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
        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 14);
        AddChild(column);

        HBoxContainer filters = new() { Alignment = BoxContainer.AlignmentMode.Center };
        filters.AddThemeConstantOverride("separation", 32);
        column.AddChild(filters);
        AddFilter(filters, QuestFilter.All, "Toutes");
        AddFilter(filters, QuestFilter.Open, "En cours");
        AddFilter(filters, QuestFilter.Done, "Accomplies");
        for (int index = 0; index < _filters.Count; index++)
        {
            Button button = _filters[index];
            button.FocusNeighborLeft = _filters[Mathf.Max(0, index - 1)].GetPath();
            button.FocusNeighborRight = _filters[Mathf.Min(_filters.Count - 1, index + 1)].GetPath();
        }
        column.AddChild(new ColorRect
        {
            Color = new Color(UITheme.GoldDim, 0.45f),
            CustomMinimumSize = new Vector2(0f, 4f),
            MouseFilter = MouseFilterEnum.Ignore,
        });

        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 32);
        column.AddChild(body);
        _scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(Columns * CardWidth + (Columns - 1) * Gap + 24f, 0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        body.AddChild(_scroll);
        VBoxContainer content = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _scroll.AddChild(content);
        _grid = new GridContainer { Columns = Columns };
        _grid.AddThemeConstantOverride("h_separation", (int)Gap);
        _grid.AddThemeConstantOverride("v_separation", (int)Gap);
        content.AddChild(_grid);
        _empty = AddLabel(content, "", TextRole.Heading, UITheme.TextDim);
        _empty.Visible = false;

        _detail = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _detail.AddThemeConstantOverride("separation", 14);
        body.AddChild(_detail);
        _detailName = AddLabel(_detail, "", TextRole.Banner, UITheme.GoldBright, TextWeight.Strong);
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
        label.CustomMinimumSize = new Vector2(420f, 0f);
        parent.AddChild(label);
        return label;
    }

    private void AddFilter(HBoxContainer parent, QuestFilter filter, string text)
    {
        Button button = new() { Text = text, Flat = true, FocusMode = FocusModeEnum.All };
        button.AddThemeFontOverride("font", UITheme.StrongFont);
        UITheme.SetTextRole(button, TextRole.Title);
        UITheme.WireButtonAudio(button);
        button.Pressed += () =>
        {
            if (_filter == filter)
                return;
            _filter = filter;
            Refresh();
            // Le filtre garde le focus : on peut parcourir les trois états sans remonter depuis une carte.
            button.GrabFocus();
        };
        parent.AddChild(button);
        _filters.Add(button);
    }

    /// <summary>Actualise la même grille à l'ouverture ou au changement de filtre ; renvoie la première carte.</summary>
    public Control Refresh()
    {
        UpdateFilters();
        ClearChildren(_grid);
        _cards.Clear();
        _scroll.ScrollVertical = 0;
        List<QuestDefinition> shown = new();
        foreach (QuestDefinition quest in QuestDataLoader.GetAll())
        {
            bool done = MetaSaveManager.HasCompletedQuest(quest.Id);
            if (_filter == QuestFilter.All || done == (_filter == QuestFilter.Done))
                shown.Add(quest);
        }
        // Un ordre stable entre filtres permet de retrouver une récompense, quelle que soit l'avancée.
        shown.Sort((a, b) =>
        {
            int kind = a.Unlocks[0].Kind.CompareTo(b.Unlocks[0].Kind);
            if (kind != 0)
                return kind;
            int difficulty = a.Difficulty.CompareTo(b.Difficulty);
            return difficulty != 0 ? difficulty : string.CompareOrdinal(a.Id, b.Id);
        });
        foreach (QuestDefinition quest in shown)
        {
            Button card = MakeCard(quest);
            _grid.AddChild(card);
            _cards.Add(card);
        }
        WireGridFocus();
        _empty.Visible = shown.Count == 0;
        _detail.Visible = shown.Count > 0;
        if (shown.Count == 0)
        {
            _empty.Text = _filter == QuestFilter.Done ? "Aucune quête accomplie pour le moment." : "Toutes les quêtes sont accomplies.";
            return _activeFilter;
        }
        ShowDetail(shown[0]);
        return _cards[0];
    }

    private void UpdateFilters()
    {
        for (int index = 0; index < _filters.Count; index++)
        {
            Button button = _filters[index];
            bool active = (QuestFilter)index == _filter;
            StyleBoxFlat underline = new()
            {
                BgColor = new Color(0f, 0f, 0f, 0f),
                BorderColor = UITheme.GoldColor,
                BorderWidthBottom = active ? 4 : 0,
                ContentMarginLeft = 12f,
                ContentMarginRight = 12f,
                ContentMarginBottom = 8f,
            };
            foreach (string state in new[] { "normal", "pressed", "hover", "hover_pressed" })
                button.AddThemeStyleboxOverride(state, underline);
            UITheme.ApplyFocusStyle(button);
            button.AddThemeColorOverride("font_color", active ? UITheme.GoldBright : UITheme.TextDim);
            button.AddThemeColorOverride("font_hover_color", UITheme.GoldBright);
            button.AddThemeColorOverride("font_focus_color", UITheme.GoldBright);
            if (active)
                _activeFilter = button;
        }
    }

    private void WireGridFocus()
    {
        for (int index = 0; index < _cards.Count; index++)
        {
            Button card = _cards[index];
            card.FocusNeighborLeft = _cards[index % Columns == 0 ? index : index - 1].GetPath();
            card.FocusNeighborRight = _cards[index % Columns == Columns - 1 || index + 1 == _cards.Count ? index : index + 1].GetPath();
            card.FocusNeighborTop = index < Columns ? _activeFilter.GetPath() : _cards[index - Columns].GetPath();
            int nextRow = (index / Columns + 1) * Columns;
            card.FocusNeighborBottom = nextRow < _cards.Count
                ? _cards[Mathf.Min(index + Columns, _cards.Count - 1)].GetPath() : card.GetPath();
        }
        foreach (Button filter in _filters)
            filter.FocusNeighborBottom = _cards.Count > 0 ? _cards[0].GetPath() : filter.GetPath();
    }

    private Button MakeCard(QuestDefinition quest)
    {
        bool done = MetaSaveManager.HasCompletedQuest(quest.Id);
        Button card = new()
        {
            Name = quest.Id,
            CustomMinimumSize = new Vector2(CardWidth, CardHeight),
            FocusMode = FocusModeEnum.All,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        StyleBoxTexture normal = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + "ui_card_normal.png"), 4, 4, 4, 4);
        StyleBoxTexture lit = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + "ui_card_selected.png"), 4, 4, 4, 4);
        card.AddThemeStyleboxOverride("normal", normal);
        foreach (string state in new[] { "pressed", "hover", "focus", "hover_pressed" })
            card.AddThemeStyleboxOverride(state, lit);
        MarginContainer inset = new() { MouseFilter = MouseFilterEnum.Ignore };
        inset.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        inset.AddThemeConstantOverride("margin_left", 14);
        inset.AddThemeConstantOverride("margin_right", 14);
        inset.AddThemeConstantOverride("margin_top", 12);
        inset.AddThemeConstantOverride("margin_bottom", 12);
        card.AddChild(inset);
        VBoxContainer content = new() { MouseFilter = MouseFilterEnum.Ignore };
        content.AddThemeConstantOverride("separation", 4);
        inset.AddChild(content);
        HBoxContainer header = new() { MouseFilter = MouseFilterEnum.Ignore };
        header.AddThemeConstantOverride("separation", 12);
        content.AddChild(header);
        Texture2D icon = RewardIcon(quest);
        if (icon != null)
            header.AddChild(new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        VBoxContainer category = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        category.AddThemeConstantOverride("separation", 0);
        header.AddChild(category);
        category.AddChild(UITheme.MakeLabel(Stars(quest.Difficulty), TextRole.Heading, UITheme.GoldColor,
            TextWeight.Strong, HorizontalAlignment.Right, outline: 4));
        category.AddChild(UITheme.MakeLabel(KindName(quest.Unlocks[0].Kind), TextRole.Lead, UITheme.TextDim,
            align: HorizontalAlignment.Right, outline: 4));
        AddCardLabel(content, quest.Name, TextRole.Heading, done ? UITheme.GoldBright : UITheme.TextLight, 64f, TextWeight.Strong);
        AddCardLabel(content, QuestBook.RewardSummary(quest), TextRole.Lead, UITheme.TextColor, 48f);
        HBoxContainer status = new()
        {
            CustomMinimumSize = new Vector2(CardWidth - 28f, CompletionIconSize),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        status.AddThemeConstantOverride("separation", 6);
        content.AddChild(status);
        if (done)
        {
            TextureRect completion = CompletionIcon();
            completion.SizeFlagsVertical = SizeFlags.ShrinkEnd;
            status.AddChild(completion);
            Label completed = UITheme.MakeLabel("Accomplie", TextRole.Lead, UITheme.GoldBright, TextWeight.Strong, outline: 4);
            completed.SizeFlagsVertical = SizeFlags.ShrinkEnd;
            status.AddChild(completed);
        }
        float fraction = done ? 1f : QuestBook.Fraction(quest, MetaSaveManager.GetQuestProgress(quest.Id));
        ColorRect track = new()
        {
            Color = new Color(UITheme.BgDark, 0.9f),
            CustomMinimumSize = new Vector2(CardWidth - 28f, 6f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        content.AddChild(track);
        track.AddChild(new ColorRect
        {
            Color = done ? UITheme.GoldColor : UITheme.CyanEssence,
            Size = new Vector2((CardWidth - 28f) * fraction, 6f),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        // Le contenu impose sa hauteur : les noms longs ne peuvent pas empiéter sur la récompense ou la jauge.
        inset.MinimumSizeChanged += () => card.CustomMinimumSize = new Vector2(CardWidth,
            Mathf.Max(CardHeight, inset.GetCombinedMinimumSize().Y));
        card.FocusEntered += () => ShowDetail(quest);
        card.MouseEntered += () => ShowDetail(quest);
        card.Pressed += () => ShowDetail(quest);
        UITheme.WireButtonAudio(card);
        return card;
    }

    private static void AddCardLabel(VBoxContainer parent, string text, TextRole role, Color color, float height,
        TextWeight weight = TextWeight.Regular)
    {
        Label label = UITheme.MakeLabel(text, role, color, weight, outline: 4);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.VerticalAlignment = VerticalAlignment.Top;
        label.CustomMinimumSize = new Vector2(CardWidth - 28f, height);
        parent.AddChild(label);
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
            HBoxContainer line = new();
            line.AddThemeConstantOverride("separation", 12);
            Texture2D icon = UnlockIcon(unlock);
            if (icon != null)
                line.AddChild(new TextureRect
                {
                    Texture = icon,
                    CustomMinimumSize = new Vector2(48f, 48f),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    TextureFilter = TextureFilterEnum.Nearest,
                    MouseFilter = MouseFilterEnum.Ignore,
                });
            Label label = UITheme.MakeLabel(QuestBook.UnlockName(unlock), TextRole.Heading, UITheme.TextLight, TextWeight.Strong, outline: 4);
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            line.AddChild(label);
            _detailRewards.AddChild(line);
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

    private static string KindName(UnlockKind kind) => kind switch
    {
        UnlockKind.Character => "Personnage",
        UnlockKind.Weapon => "Arme",
        UnlockKind.Badge => "Écusson",
        _ => "Objet",
    };
}
