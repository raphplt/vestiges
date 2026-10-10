using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Page Quêtes de l'accueil (plan 06 §9.10, lot Q3) : les 36 quêtes de déblocage, toutes ouvertes. À gauche la liste
/// (onglets « En cours » et « Accomplies »), une ligne par quête avec l'icône de ce qu'elle débloque, ses étoiles et
/// sa jauge ; à droite le détail de la ligne survolée : condition, portée, avancée retenue et pièces débloquées.
/// </summary>
public partial class HubQuestsPanel : MarginContainer
{
    private const float RowWidth = 640f;
    private const float RowHeight = 88f;
    private const float IconSize = 48f;

    private static string _tab = "open";

    private Font _bodyFont;
    private Font _strongFont;
    private HBoxContainer _tabs;
    private Button _activeTab;
    private VBoxContainer _list;
    private Label _detailName;
    private Label _detailStars;
    private Label _detailText;
    private Label _detailScope;
    private Label _detailProgress;
    private VBoxContainer _detailRewards;
    private Label _count;

    public override void _Ready()
    {
        _bodyFont = UITheme.BodyFont;
        _strongFont = UITheme.StrongFont;

        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 14);
        AddChild(column);

        _tabs = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _tabs.AddThemeConstantOverride("separation", 40);
        column.AddChild(_tabs);
        column.AddChild(new ColorRect { Color = new Color(UITheme.GoldDim, 0.45f), CustomMinimumSize = new Vector2(0f, 4f) });

        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 40);
        column.AddChild(body);

        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(RowWidth + 24f, 0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        body.AddChild(scroll);
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_list);

        VBoxContainer detail = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        detail.AddThemeConstantOverride("separation", 10);
        body.AddChild(detail);
        _detailName = AddLabel(detail, _strongFont, TextRole.Banner, UITheme.GoldBright);
        _detailStars = AddLabel(detail, _strongFont, TextRole.Heading, UITheme.GoldColor);
        _detailText = AddLabel(detail, _bodyFont, TextRole.Heading, UITheme.TextLight);
        _detailScope = AddLabel(detail, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _detailProgress = AddLabel(detail, _strongFont, TextRole.Heading, UITheme.CyanEssence);
        _detailRewards = new VBoxContainer();
        _detailRewards.AddThemeConstantOverride("separation", 6);
        detail.AddChild(_detailRewards);
        _count = AddLabel(detail, _bodyFont, TextRole.Subhead, UITheme.TextVeryDim);
    }

    private static Label AddLabel(Container parent, Font font, TextRole role, Color color)
    {
        Label label = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(420f, 0f) };
        label.AddThemeFontOverride("font", font);
        UITheme.SetTextRole(label, role);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.05f, 0.85f));
        parent.AddChild(label);
        return label;
    }

    /// <summary>Reconstruit onglets et liste à l'ouverture ; renvoie la ligne qui prend le focus.</summary>
    public Control Refresh()
    {
        foreach (Node child in _tabs.GetChildren())
            child.QueueFree();
        Button open = AddTab("open", "En cours");
        Button done = AddTab("done", "Accomplies");
        open.FocusNeighborRight = done.GetPath();
        done.FocusNeighborLeft = open.GetPath();
        return RebuildList();
    }

    private Button AddTab(string id, string text)
    {
        Button tab = new() { Text = text, Flat = true, FocusMode = FocusModeEnum.All };
        bool active = _tab == id;
        StyleBoxFlat underline = new()
        {
            BgColor = new Color(0f, 0f, 0f, 0f),
            BorderColor = UITheme.GoldColor,
            BorderWidthBottom = active ? 4 : 0,
            ContentMarginLeft = 8f,
            ContentMarginRight = 8f,
            ContentMarginBottom = 6f,
        };
        foreach (string state in new[] { "normal", "pressed", "hover", "focus", "hover_pressed" })
            tab.AddThemeStyleboxOverride(state, underline);
        tab.AddThemeFontOverride("font", _strongFont);
        UITheme.SetTextRole(tab, TextRole.Title);
        tab.AddThemeColorOverride("font_color", active ? UITheme.GoldBright : UITheme.TextDim);
        tab.AddThemeColorOverride("font_hover_color", UITheme.GoldBright);
        tab.AddThemeColorOverride("font_focus_color", UITheme.GoldBright);
        UITheme.WireButtonAudio(tab);
        tab.Pressed += () =>
        {
            if (_tab == id)
                return;
            _tab = id;
            Control first = Refresh();
            first?.GrabFocus();
        };
        _tabs.AddChild(tab);
        if (active)
            _activeTab = tab;
        return tab;
    }

    private Control RebuildList()
    {
        foreach (Node child in _list.GetChildren())
            child.QueueFree();
        IReadOnlyList<QuestDefinition> all = QuestDataLoader.GetAll();
        List<QuestDefinition> shown = new();
        int completed = 0;
        foreach (QuestDefinition quest in all)
        {
            bool done = MetaSaveManager.HasCompletedQuest(quest.Id);
            if (done)
                completed++;
            if (done == (_tab == "done"))
                shown.Add(quest);
        }
        // En cours : les plus proches d'abord, à difficulté égale ; l'ordre du fichier départage.
        if (_tab == "open")
            shown.Sort((a, b) => a.Difficulty != b.Difficulty ? a.Difficulty.CompareTo(b.Difficulty)
                : QuestBook.Fraction(b, MetaSaveManager.GetQuestProgress(b.Id)).CompareTo(QuestBook.Fraction(a, MetaSaveManager.GetQuestProgress(a.Id))));

        Control first = null;
        foreach (QuestDefinition quest in shown)
        {
            Button row = MakeRow(quest);
            _list.AddChild(row);
            if (first == null)
            {
                first = row;
                if (_activeTab != null)
                    row.FocusNeighborTop = _activeTab.GetPath();
            }
        }
        if (first != null)
        {
            foreach (Node tab in _tabs.GetChildren())
                if (!tab.IsQueuedForDeletion())
                    ((Control)tab).FocusNeighborBottom = first.GetPath();
            ShowDetail(shown[0]);
        }
        else
            ShowEmpty();
        _count.Text = $"{completed} / {all.Count} quêtes accomplies";
        return first;
    }

    private Button MakeRow(QuestDefinition quest)
    {
        bool done = MetaSaveManager.HasCompletedQuest(quest.Id);
        Button row = new() { CustomMinimumSize = new Vector2(RowWidth, RowHeight), FocusMode = FocusModeEnum.All };
        row.TextureFilter = TextureFilterEnum.Nearest;
        StyleBoxTexture normal = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + "ui_card_normal.png"), 4, 4, 4, 4);
        StyleBoxTexture lit = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + "ui_card_selected.png"), 4, 4, 4, 4);
        row.AddThemeStyleboxOverride("normal", normal);
        row.AddThemeStyleboxOverride("pressed", normal);
        row.AddThemeStyleboxOverride("hover", lit);
        row.AddThemeStyleboxOverride("focus", lit);
        row.AddThemeStyleboxOverride("hover_pressed", lit);

        Texture2D icon = RewardIcon(quest);
        if (icon != null)
            row.AddChild(new TextureRect
            {
                Texture = icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
                Position = new Vector2(12f, (RowHeight - IconSize) / 2f),
                Size = new Vector2(IconSize, IconSize),
            });

        Label name = RowLabel(quest.Name, _strongFont, TextRole.Heading, done ? UITheme.GoldBright : UITheme.TextLight);
        name.Position = new Vector2(76f, 6f);
        row.AddChild(name);
        Label reward = RowLabel(QuestBook.RewardSummary(quest), _bodyFont, TextRole.Body, UITheme.TextDim);
        reward.Position = new Vector2(76f, 38f);
        row.AddChild(reward);
        Label stars = RowLabel(Stars(quest.Difficulty), _strongFont, TextRole.Heading, UITheme.GoldColor);
        stars.Position = new Vector2(RowWidth - 96f, 8f);
        stars.Size = new Vector2(84f, 28f);
        stars.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(stars);

        float fraction = done ? 1f : QuestBook.Fraction(quest, MetaSaveManager.GetQuestProgress(quest.Id));
        ColorRect track = new() { Color = new Color(UITheme.BgDark, 0.9f), Position = new Vector2(76f, 70f), Size = new Vector2(RowWidth - 100f, 6f), MouseFilter = MouseFilterEnum.Ignore };
        row.AddChild(track);
        track.AddChild(new ColorRect
        {
            Color = done ? UITheme.GoldColor : UITheme.CyanEssence,
            Size = new Vector2((RowWidth - 100f) * fraction, 6f),
            MouseFilter = MouseFilterEnum.Ignore,
        });

        row.FocusEntered += () => ShowDetail(quest);
        row.MouseEntered += () => ShowDetail(quest);
        UITheme.WireButtonAudio(row);
        return row;
    }

    private Label RowLabel(string text, Font font, TextRole role, Color color)
    {
        Label label = new() { Text = text, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", font);
        UITheme.SetTextRole(label, role);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeConstantOverride("outline_size", 4);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.05f, 0.85f));
        return label;
    }

    private void ShowDetail(QuestDefinition quest)
    {
        bool done = MetaSaveManager.HasCompletedQuest(quest.Id);
        IReadOnlyList<float> progress = MetaSaveManager.GetQuestProgress(quest.Id);
        _detailName.Text = quest.Name;
        _detailStars.Text = $"{Stars(quest.Difficulty)}  {DifficultyName(quest.Difficulty)}";
        _detailText.Text = quest.Description;
        _detailScope.Text = quest.Scope == QuestScope.Cumulative ? "Toutes runs confondues" : "En une seule run";
        _detailProgress.Text = done ? "Accomplie"
            : quest.Scope == QuestScope.Cumulative
                ? $"Avancée : {QuestBook.ProgressText(quest, progress)}"
                : $"Meilleure run : {QuestBook.ProgressText(quest, progress)}";
        _detailProgress.AddThemeColorOverride("font_color", done ? UITheme.GoldBright : UITheme.CyanEssence);

        foreach (Node child in _detailRewards.GetChildren())
            child.QueueFree();
        _detailRewards.AddChild(AddLabelTo(done ? "A débloqué :" : "Débloque :", _bodyFont, TextRole.Heading, UITheme.TextDim));
        foreach (QuestUnlock unlock in quest.Unlocks)
        {
            HBoxContainer line = new();
            line.AddThemeConstantOverride("separation", 12);
            Texture2D icon = UnlockIcon(unlock);
            line.AddChild(new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new Vector2(40f, 40f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
            });
            Label label = AddLabelTo($"{KindName(unlock.Kind)} : {QuestBook.UnlockName(unlock)}", _strongFont, TextRole.Heading, UITheme.TextLight);
            label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            line.AddChild(label);
            _detailRewards.AddChild(line);
        }
    }

    private void ShowEmpty()
    {
        _detailName.Text = _tab == "done" ? "Aucune quête accomplie" : "Toutes les quêtes sont accomplies";
        _detailStars.Text = "";
        _detailText.Text = _tab == "done" ? "Les quêtes se remplissent en jouant ; une notification part dès qu'une quête est accomplie." : "";
        _detailScope.Text = "";
        _detailProgress.Text = "";
        foreach (Node child in _detailRewards.GetChildren())
            child.QueueFree();
    }

    private Label AddLabelTo(string text, Font font, TextRole role, Color color)
    {
        Label label = new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(360f, 0f) };
        label.AddThemeFontOverride("font", font);
        UITheme.SetTextRole(label, role);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.05f, 0.85f));
        return label;
    }

    /// <summary>Icône de la ligne : la première pièce qui en a une (l'arme d'un personnage s'il n'a pas de portrait).</summary>
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

    private static string DifficultyName(int difficulty) => difficulty switch
    {
        1 => "en jouant normalement",
        2 => "à chercher",
        _ => "fin de run, risque ou maîtrise",
    };

    private static string KindName(UnlockKind kind) => kind switch
    {
        UnlockKind.Character => "Personnage",
        UnlockKind.Weapon => "Arme",
        _ => "Objet",
    };
}
