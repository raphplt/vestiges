using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Infrastructure.Steam;

namespace Vestiges.UI;

/// <summary>
/// Classement de l'accueil (plan 43 C2), en éliminations (DECISIONS §155). Trois onglets : la semaine en cours (remise
/// à zéro le lundi, semaine ISO), tous les temps, et les records du profil, au global et par personnage. Les onglets en
/// ligne lisent les classements Steam : les dix premiers, puis la place du joueur et ses voisins ; sans Steam, ils disent
/// pourquoi ils sont vides, et les records du profil restent.
/// </summary>
public partial class HubLeaderboardPanel : MarginContainer
{
    private const int TopCount = 10;
    private const int AroundCount = 5;
    private const float RankWidth = 90f;
    private const float KillsWidth = 220f;

    private static string _tab = "week";

    private HBoxContainer _tabs;
    private Label _subtitle;
    private VBoxContainer _rows;
    private Button _activeTab;
    // Réponses attendues, dans l'ordre des demandes : la file n'en traite qu'une à la fois.
    private readonly List<(string Board, LeaderboardQueue.EntryRange Range)> _awaited = new();
    private readonly List<LeaderboardQueue.Entry> _top = new();
    private readonly List<LeaderboardQueue.Entry> _around = new();
    private string _board;
    private bool _failed;
    // File écoutée : gardée pour s'en désabonner même si Steam s'arrête entre-temps.
    private LeaderboardQueue _queue;
    // Une lecture abandonnée par la file (arrêt de Steam) ne répond jamais : passé ce délai, l'onglet le dit.
    private const double GiveUpSeconds = LeaderboardQueue.MaxAttempts * (LeaderboardQueue.ReplyTimeoutSec + LeaderboardQueue.RetryDelaySec) * 2 + 5;
    private int _request;

    public string CurrentTab => _tab;

    public override void _Ready()
    {
        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 14);
        AddChild(column);
        _tabs = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _tabs.AddThemeConstantOverride("separation", 40);
        column.AddChild(_tabs);
        column.AddChild(new ColorRect { Color = new Color(UITheme.GoldDim, 0.45f), CustomMinimumSize = new Vector2(0f, 4f) });

        PanelContainer frame = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        frame.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        column.AddChild(frame);
        ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        frame.AddChild(scroll);
        VBoxContainer body = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(body);
        _subtitle = UITheme.MakeLabel("", TextRole.Heading, UITheme.TextDim);
        _subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.AddChild(_subtitle);
        _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _rows.AddThemeConstantOverride("separation", 6);
        body.AddChild(_rows);
    }

    public override void _ExitTree() => Unsubscribe();

    /// <summary>Reconstruit onglets et contenu à l'ouverture ; renvoie l'onglet actif, qui prend le focus.</summary>
    public Control Refresh()
    {
        MenuSkin.Clear(_tabs);
        Button[] tabs = { AddTab("week", "Cette semaine"), AddTab("all", "De tous les temps"), AddTab("records", "Mes records") };
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].FocusNeighborLeft = tabs[Mathf.Max(0, i - 1)].GetPath();
            tabs[i].FocusNeighborRight = tabs[Mathf.Min(tabs.Length - 1, i + 1)].GetPath();
        }
        ShowTab();
        return _activeTab;
    }

    private Button AddTab(string id, string text)
    {
        Button tab = new() { Name = id, Text = text, Flat = true, FocusMode = FocusModeEnum.All };
        bool active = _tab == id;
        StyleBoxFlat underline = new()
        {
            BgColor = new Color(0f, 0f, 0f, 0f), BorderColor = UITheme.GoldColor, BorderWidthBottom = active ? 4 : 0,
            ContentMarginLeft = 8f, ContentMarginRight = 8f, ContentMarginBottom = 6f,
        };
        foreach (string state in new[] { "normal", "pressed", "hover", "focus", "hover_pressed" })
            tab.AddThemeStyleboxOverride(state, underline);
        tab.AddThemeFontOverride("font", UITheme.TitleFont);
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
            Refresh()?.GrabFocus();
        };
        _tabs.AddChild(tab);
        if (active)
            _activeTab = tab;
        return tab;
    }

    private void ShowTab()
    {
        Unsubscribe();
        _request++;
        MenuSkin.Clear(_rows);
        if (_tab == "records")
        {
            ShowRecords();
            return;
        }
        bool weekly = _tab == "week";
        string context = weekly ? string.Format("Tous personnages, graines au hasard · remise à zéro dans {0}", UntilReset(DateTime.UtcNow))
            : "Tous personnages, graines au hasard";
        string unavailable = OnlineUnavailableReason();
        if (unavailable != null)
        {
            _subtitle.Text = $"{context}\n{unavailable}";
            return;
        }
        _subtitle.Text = context;
        _board = weekly ? RunLeaderboards.WeeklyBoard(DateTime.UtcNow) : RunLeaderboards.BoardGlobal;
        _top.Clear();
        _around.Clear();
        _failed = false;
        _awaited.Clear();
        LeaderboardQueue queue = SteamManager.Leaderboards;
        _queue = queue;
        queue.DownloadFinished += OnDownloaded;
        AddStatus("Chargement du classement…");
        // Les deux réponses sont attendues avant la première demande : un service peut répondre pendant la demande.
        // Une demande refusée par la file (pleine) n'aura pas de réponse : elle n'est plus attendue.
        (LeaderboardQueue.EntryRange Range, int Count)[] requests =
            { (LeaderboardQueue.EntryRange.Global, TopCount), (LeaderboardQueue.EntryRange.AroundUser, AroundCount) };
        foreach ((LeaderboardQueue.EntryRange range, int _) in requests)
            _awaited.Add((_board, range));
        foreach ((LeaderboardQueue.EntryRange range, int count) in requests)
        {
            if (queue.RequestEntries(_board, range, count))
                continue;
            _awaited.Remove((_board, range));
            _failed = true;
        }
        // Tout est déjà arrivé (réponses immédiates, déjà affichées) ou tout a été refusé.
        if (_awaited.Count == 0)
        {
            if (_queue != null)
            {
                Unsubscribe();
                ShowEntries();
            }
            return;
        }
        int request = ++_request;
        GetTree().CreateTimer(GiveUpSeconds, processAlways: true).Timeout += () =>
        {
            if (request != _request || _awaited.Count == 0 || !IsInsideTree())
                return;
            _awaited.Clear();
            _failed = true;
            Unsubscribe();
            ShowEntries();
        };
    }

    /// <summary>Pourquoi le classement en ligne est vide, ou null s'il peut être lu.</summary>
    private static string OnlineUnavailableReason()
    {
        if (DevelopmentMode.IsEnabled)
            return "Le mode dev ne lit ni n'alimente le classement mondial.";
        if (SteamManager.Leaderboards == null)
            return "Classement mondial indisponible : Steam n'est pas lancé.";
        return null;
    }

    private void OnDownloaded(LeaderboardQueue.DownloadResult result)
    {
        if (_awaited.Count == 0 || _awaited[0].Board != result.Board)
            return;
        LeaderboardQueue.EntryRange range = _awaited[0].Range;
        _awaited.RemoveAt(0);
        if (!result.Succeeded)
            _failed = true;
        else
            (range == LeaderboardQueue.EntryRange.Global ? _top : _around).AddRange(result.Entries);
        if (_awaited.Count == 0)
        {
            Unsubscribe();
            ShowEntries();
        }
    }

    private void ShowEntries()
    {
        MenuSkin.Clear(_rows);
        if (_failed && _top.Count == 0)
        {
            AddStatus("Le classement n'a pas répondu. Réessaie plus tard.");
            return;
        }
        if (_top.Count == 0)
        {
            AddStatus(_tab == "week" ? "Personne n'a encore fini de run cette semaine." : "Personne n'a encore fini de run.");
            return;
        }
        AddHeader();
        HashSet<int> shown = new();
        foreach (LeaderboardQueue.Entry entry in _top)
        {
            AddEntry(entry);
            shown.Add(entry.Rank);
        }
        // Ta place, si elle n'est pas dans les dix premiers : séparée par une coupure.
        bool gap = false;
        foreach (LeaderboardQueue.Entry entry in _around)
        {
            if (shown.Contains(entry.Rank))
                continue;
            if (!gap)
            {
                _rows.AddChild(UITheme.MakeLabel("…", TextRole.Heading, UITheme.TextDim, TextWeight.Regular, HorizontalAlignment.Center));
                gap = true;
            }
            AddEntry(entry);
        }
    }

    private void AddHeader()
    {
        HBoxContainer row = Row();
        row.AddChild(Cell("Rang", RankWidth, UITheme.TextDim, TextRole.Subhead));
        Label name = Cell("Joueur", 0f, UITheme.TextDim, TextRole.Subhead);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(name);
        row.AddChild(Cell("Éliminations", KillsWidth, UITheme.TextDim, TextRole.Subhead, HorizontalAlignment.Right));
        _rows.AddChild(row);
    }

    private void AddEntry(LeaderboardQueue.Entry entry)
    {
        bool mine = entry.SteamId != 0 && entry.SteamId == SteamManager.LocalSteamId;
        Color color = mine ? UITheme.GoldBright : entry.Rank <= 3 ? UITheme.TextLight : UITheme.TextColor;
        HBoxContainer row = Row();
        row.AddChild(Cell(entry.Rank.ToString("N0"), RankWidth, color, TextRole.Heading));
        Label name = Cell(mine ? $"{entry.PlayerName}  (toi)" : entry.PlayerName, 0f, color, TextRole.Heading);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        name.ClipText = true;
        row.AddChild(name);
        row.AddChild(Cell(entry.Score.ToString("N0"), KillsWidth, color, TextRole.Heading, HorizontalAlignment.Right));
        _rows.AddChild(row);
    }

    /// <summary>Records du profil : le global, puis chaque personnage avec la date et la distance de sa meilleure run.</summary>
    private void ShowRecords()
    {
        int best = MetaSaveManager.GetBestKills();
        _subtitle.Text = best > 0 ? $"Record : {best:N0} éliminations" : "Aucune run terminée pour l'instant.";
        HBoxContainer header = Row();
        Label who = Cell("Personnage", 0f, UITheme.TextDim, TextRole.Subhead);
        who.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(who);
        header.AddChild(Cell("Éliminations", KillsWidth, UITheme.TextDim, TextRole.Subhead, HorizontalAlignment.Right));
        header.AddChild(Cell("Au plus loin", 200f, UITheme.TextDim, TextRole.Subhead, HorizontalAlignment.Right));
        header.AddChild(Cell("Date", 260f, UITheme.TextDim, TextRole.Subhead, HorizontalAlignment.Right));
        _rows.AddChild(header);
        foreach (CharacterData character in CharacterDataLoader.GetAll())
        {
            KillRecord record = MetaSaveManager.GetKillRecord(character.Id);
            bool unlocked = MetaSaveManager.IsCharacterUnlocked(character.Id);
            Color color = record != null && record.Kills == best && best > 0 ? UITheme.GoldBright
                : unlocked ? UITheme.TextLight : UITheme.TextVeryDim;
            HBoxContainer row = Row();
            TextureRect portrait = new()
            {
                Texture = CharacterPortrait.Idle(character.Id), CustomMinimumSize = new Vector2(40f, 48f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest, MouseFilter = MouseFilterEnum.Ignore,
                Modulate = unlocked ? Colors.White : new Color(0.2f, 0.19f, 0.24f),
            };
            row.AddChild(portrait);
            Label name = Cell(character.Name, 0f, color, TextRole.Heading);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(name);
            row.AddChild(Cell(record != null ? record.Kills.ToString("N0") : "—", KillsWidth, color, TextRole.Heading, HorizontalAlignment.Right));
            row.AddChild(Cell(record != null ? $"{record.MaxDistanceMeters:N0} m" : "", 200f, UITheme.TextDim, TextRole.Body, HorizontalAlignment.Right));
            row.AddChild(Cell(record?.Date ?? "", 260f, UITheme.TextDim, TextRole.Body, HorizontalAlignment.Right));
            _rows.AddChild(row);
        }
    }

    /// <summary>Temps restant avant lundi 0 h UTC, début de la semaine ISO suivante.</summary>
    public static string UntilReset(DateTime utcNow)
    {
        int days = ((int)DayOfWeek.Monday - (int)utcNow.DayOfWeek + 7) % 7;
        DateTime reset = utcNow.Date.AddDays(days == 0 ? 7 : days);
        TimeSpan left = reset - utcNow;
        return left.TotalHours >= 24 ? $"{left.Days} j {left.Hours} h" : $"{left.Hours} h {left.Minutes:00} min";
    }

    private void AddStatus(string text) =>
        _rows.AddChild(UITheme.MakeLabel(text, TextRole.Heading, UITheme.TextColor, TextWeight.Regular, HorizontalAlignment.Center));

    private static HBoxContainer Row()
    {
        HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 16);
        return row;
    }

    private static Label Cell(string text, float width, Color color, TextRole role, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        Label label = UITheme.MakeLabel(text, role, color, TextWeight.Regular, align);
        if (width > 0f)
            label.CustomMinimumSize = new Vector2(width, 0f);
        label.VerticalAlignment = VerticalAlignment.Center;
        return label;
    }

    private void Unsubscribe()
    {
        if (_queue != null)
            _queue.DownloadFinished -= OnDownloaded;
        _queue = null;
    }
}
