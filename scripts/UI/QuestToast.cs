using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Notification d'une quête accomplie en pleine run (plan 06 §9.1) : un bandeau court sous la barre de boss, « Quête
/// accomplie : » son nom, puis ce qu'elle débloque. Plusieurs quêtes passent l'une après l'autre ; pendant un écran de
/// choix (arbre en pause) le bandeau se retire et la file attend. Enfant de la racine mise à l'échelle du HUD :
/// coordonnées en unités de référence 960×540.
/// </summary>
public partial class QuestToast : Control
{
    private const float Top = 96f;
    private const float Width = 340f;
    private const float FadeSec = 0.3f;
    private const float HoldSec = 3.2f;

    private static readonly Color Ink = new(0.06f, 0.05f, 0.1f, 0.9f);
    private static readonly Color Gold = new("d4a843");
    private static readonly Color Paper = new("e8e0d4");

    private readonly Queue<string> _pending = new();
    private EventBus _eventBus;
    private PanelContainer _panel;
    private Label _title;
    private Label _detail;
    private Tween _tween;
    private bool _showing;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Pausable;
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        OffsetLeft = -Width / 2f;
        OffsetRight = Width / 2f;
        OffsetTop = Top;
        BuildPanel();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.QuestCompleted += OnQuestCompleted;
        _eventBus.RunPhaseChanged += OnRunPhaseChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.QuestCompleted -= OnQuestCompleted;
        _eventBus.RunPhaseChanged -= OnRunPhaseChanged;
    }

    /// <summary>Un écran de choix met l'arbre en pause : le bandeau se cache et reprend ensuite là où il en était.</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationPaused)
            Visible = false;
        else if (what == NotificationUnpaused)
            Visible = true;
    }

    /// <summary>Bandeau en cours et quêtes en attente (bancs).</summary>
    public string ShownText => _showing ? $"{_title.Text} {_detail.Text}" : "";
    public int PendingCount => _pending.Count;

    private void BuildPanel()
    {
        _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Modulate = Colors.Transparent, Visible = false };
        StyleBoxFlat style = new()
        {
            BgColor = Ink,
            BorderColor = Gold,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 4,
            ContentMarginBottom = 5,
        };
        _panel.AddThemeStyleboxOverride("panel", style);
        _panel.Size = new Vector2(Width, 0f);
        AddChild(_panel);

        VBoxContainer column = new() { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 1);
        _panel.AddChild(column);
        _title = MakeLabel(9, Gold);
        column.AddChild(_title);
        _detail = MakeLabel(11, Paper);
        _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_detail);
    }

    private static Label MakeLabel(int size, Color color)
    {
        Label label = new() { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", 3);
        return label;
    }

    private void OnQuestCompleted(string questId)
    {
        _pending.Enqueue(questId);
        if (!_showing)
            ShowNext();
    }

    private void ShowNext()
    {
        QuestDefinition quest = null;
        while (quest == null && _pending.Count > 0)
            quest = QuestDataLoader.Get(_pending.Dequeue());
        _showing = quest != null;
        if (!_showing)
            return;

        _title.Text = string.Format(Tr("QUEST_DONE_TOAST"), quest.Name);
        _detail.Text = QuestBook.RewardSummary(quest);
        _panel.Visible = true;
        _tween?.Kill();
        _tween = CreateTween();
        // Le son part avec le bandeau, pas pendant l'écran de choix qui le retient.
        _tween.TweenCallback(Callable.From(() => AudioManager.PlayUI("sfx_souvenir_trouve")));
        _tween.TweenProperty(_panel, "modulate", Colors.White, FadeSec);
        _tween.TweenInterval(HoldSec);
        _tween.TweenProperty(_panel, "modulate", Colors.Transparent, FadeSec);
        _tween.TweenCallback(Callable.From(ShowNext));
    }

    private void OnRunPhaseChanged(string oldPhase, string newPhase)
    {
        if (newPhase != GameManager.RunPhase.Death.ToString())
            return;
        // Le bilan reprend les quêtes accomplies : le bandeau se retire avec le reste du HUD.
        _pending.Clear();
        _tween?.Kill();
        _panel.Visible = false;
        _showing = false;
    }
}
