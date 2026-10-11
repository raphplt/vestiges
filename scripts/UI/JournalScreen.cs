using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Fragments retrouvés : constellations, index et lecture, avec retour au contexte qui a ouvert le journal.</summary>
public partial class JournalScreen : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private Control _root;
    private VBoxContainer _fragmentList;
    private ScrollContainer _fragmentScroll;
    private ScrollContainer _readingScroll;
    private Label _fragmentTitle;
    private Label _fragmentText;
    private Label _constellationTitle;
    private Label _constellationDescription;
    private Label _progressLabel;
    private MemoryMap _memoryMap;
    private Button _closeButton;
    private Control _returnFocus;
    private string _selectedConstellation;
    private string _selectedFragment;
    private readonly Dictionary<string, Button> _constellationButtons = new();
    private readonly Dictionary<string, Button> _fragmentButtons = new();
    private readonly Dictionary<string, string> _lastFragments = new();
    private bool _wasPaused;
    private bool _isVisible;

    public bool IsOpen => _isVisible;

    public override void _Ready()
    {
        Layer = 90;
        ProcessMode = ProcessModeEnum.Always;
        BuildUI();
        _root.Visible = false;
    }

    public void Toggle()
    {
        if (_isVisible) Hide();
        else Show();
    }

    public new void Show()
    {
        if (_isVisible) return;
        _wasPaused = GetTree().Paused;
        _returnFocus = GetViewport().GuiGetFocusOwner();
        _isVisible = true;
        _root.Visible = true;
        RefreshContent();
        GetTree().Paused = true;
        if (_selectedFragment != null && _fragmentButtons.TryGetValue(_selectedFragment, out Button selected))
            selected.GrabFocus();
        else _closeButton.GrabFocus();
        AudioManager.PlayUI("sfx_inventaire_ouvrir");
    }

    public new void Hide()
    {
        if (!_isVisible) return;
        _isVisible = false;
        _root.Visible = false;
        GetTree().Paused = _wasPaused;
        EmitSignal(SignalName.Closed);
        if (GodotObject.IsInstanceValid(_returnFocus) && _returnFocus.IsVisibleInTree())
            _returnFocus.GrabFocus();
        AudioManager.PlayUI("sfx_inventaire_fermer");
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isVisible || @event.IsEcho()) return;
        if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("journal"))
        {
            Hide();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!_isVisible) return;
        float axis = Input.GetAxis("scroll_up", "scroll_down");
        if (axis == 0f) return;
        Control focus = GetViewport().GuiGetFocusOwner();
        ScrollContainer scroll = focus != null && _fragmentScroll.IsAncestorOf(focus) ? _fragmentScroll : _readingScroll;
        scroll.ScrollVertical += Mathf.RoundToInt(axis * 850f * (float)delta);
    }

    private void BuildUI()
    {
        _root = new Control();
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        MenuSkin.AddGameBackdrop(_root, PixelBackdrop.NeutralTint);
        MarginContainer margin = new();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 110);
        margin.AddThemeConstantOverride("margin_right", 110);
        margin.AddThemeConstantOverride("margin_top", 86);
        margin.AddThemeConstantOverride("margin_bottom", 76);
        _root.AddChild(margin);
        VBoxContainer page = new();
        page.AddThemeConstantOverride("separation", 22);
        margin.AddChild(page);
        HBoxContainer header = new();
        Label title = UITheme.MakeLabel("JOURNAL DES SOUVENIRS", TextRole.Title, UITheme.GoldBright, TextWeight.Bold);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(title);
        _progressLabel = UITheme.MakeLabel("", TextRole.Subhead, UITheme.TextColor);
        header.AddChild(_progressLabel);
        page.AddChild(header);

        HBoxContainer body = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 24);
        page.AddChild(body);
        VBoxContainer constellations = new() { CustomMinimumSize = new Vector2(290, 0) };
        constellations.AddThemeConstantOverride("separation", 8);
        body.AddChild(constellations);
        constellations.AddChild(UITheme.MakeLabel("CONSTELLATIONS", TextRole.Body, UITheme.TextDim));
        foreach (ConstellationData constellation in SouvenirDataLoader.GetAllConstellations())
        {
            Button button = MenuSkin.Button(constellation.Name);
            button.Alignment = HorizontalAlignment.Left;
            UITheme.SetTextRole(button, TextRole.Subhead);
            string id = constellation.Id;
            button.Pressed += () => SelectConstellation(id);
            _constellationButtons.Add(id, button);
            constellations.AddChild(button);
        }
        constellations.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        Label reminder = UITheme.MakeLabel("Retrouver un fragment,\nc’est retenir un peu du monde.", TextRole.Subhead, UITheme.TextDim);
        constellations.AddChild(reminder);

        PanelContainer index = new() { CustomMinimumSize = new Vector2(380, 0) };
        index.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        body.AddChild(index);
        VBoxContainer indexContent = new();
        indexContent.AddThemeConstantOverride("separation", 16);
        index.AddChild(indexContent);
        indexContent.AddChild(UITheme.MakeLabel("FRAGMENTS", TextRole.Heading, UITheme.GoldBright));
        _fragmentScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        indexContent.AddChild(_fragmentScroll);
        _fragmentList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _fragmentList.AddThemeConstantOverride("separation", 8);
        _fragmentScroll.AddChild(_fragmentList);

        PanelContainer reading = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        reading.AddThemeStyleboxOverride("panel", MenuSkin.Panel(42));
        body.AddChild(reading);
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 14);
        reading.AddChild(content);
        _constellationTitle = UITheme.MakeLabel("", TextRole.Body, UITheme.TextDim);
        content.AddChild(_constellationTitle);
        _memoryMap = new MemoryMap { CustomMinimumSize = new Vector2(0, 108), MouseFilter = Control.MouseFilterEnum.Ignore };
        content.AddChild(_memoryMap);
        _fragmentTitle = UITheme.MakeLabel("", TextRole.Title, UITheme.TextLight, TextWeight.Strong);
        _fragmentTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_fragmentTitle);
        _readingScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.All };
        UITheme.ApplyFocusStyle(_readingScroll);
        content.AddChild(_readingScroll);
        _fragmentText = UITheme.MakeLabel("", TextRole.Heading, UITheme.TextColor);
        _fragmentText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _fragmentText.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _readingScroll.AddChild(_fragmentText);
        _constellationDescription = UITheme.MakeLabel("", TextRole.Subhead, UITheme.TextDim);
        _constellationDescription.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_constellationDescription);

        HBoxContainer footer = new();
        page.AddChild(footer);
        _closeButton = MenuSkin.Button("Retour");
        _closeButton.CustomMinimumSize = new Vector2(260, 54);
        _closeButton.Pressed += Hide;
        footer.AddChild(_closeButton);
        Label hint = UITheme.MakeLabel("Page haut / bas · défiler     Échap / B · retour", TextRole.Body, UITheme.TextDim,
            align: HorizontalAlignment.Right);
        hint.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        footer.AddChild(hint);
    }

    private void RefreshContent()
    {
        _progressLabel.Text = $"{MetaSaveManager.GetDiscoveredSouvenirs().Count} / {SouvenirDataLoader.GetAll().Count} fragments retrouvés";
        foreach ((string id, Button button) in _constellationButtons)
        {
            int found = 0;
            List<SouvenirData> fragments = SouvenirDataLoader.GetByConstellation(id);
            foreach (SouvenirData fragment in fragments)
                if (MetaSaveManager.IsSouvenirDiscovered(fragment.Id)) found++;
            button.Text = $"{SouvenirDataLoader.GetConstellation(id).Name}  {found}/{fragments.Count}";
        }
        if (_selectedConstellation == null)
            foreach (string id in _constellationButtons.Keys) { _selectedConstellation = id; break; }
        if (_selectedConstellation != null) SelectConstellation(_selectedConstellation);
    }

    private void SelectConstellation(string id)
    {
        _selectedConstellation = id;
        MenuSkin.Clear(_fragmentList);
        _fragmentButtons.Clear();
        ConstellationData constellation = SouvenirDataLoader.GetConstellation(id);
        _constellationTitle.Text = constellation.Name.ToUpperInvariant();
        _constellationTitle.AddThemeColorOverride("font_color", constellation.Color.Lightened(0.15f));
        _constellationDescription.Text = constellation.Description;
        List<SouvenirData> fragments = SouvenirDataLoader.GetByConstellation(id);
        _memoryMap.Configure(fragments, constellation.Color);
        foreach ((string key, Button button) in _constellationButtons)
            MenuSkin.StyleButton(button, key == id);
        foreach (SouvenirData fragment in fragments)
        {
            bool discovered = MetaSaveManager.IsSouvenirDiscovered(fragment.Id);
            Button button = MenuSkin.Button(discovered ? fragment.Name : "Fragment effacé");
            button.Alignment = HorizontalAlignment.Left;
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.CustomMinimumSize = new Vector2(280, 64);
            UITheme.SetTextRole(button, TextRole.Subhead);
            string fragmentId = fragment.Id;
            button.Pressed += () => SelectFragment(fragmentId);
            button.FocusEntered += () => SelectFragment(fragmentId);
            _fragmentList.AddChild(button);
            _fragmentButtons.Add(fragmentId, button);
            button.FocusNeighborLeft = _constellationButtons[id].GetPath();
            button.FocusNeighborRight = _readingScroll.GetPath();
        }
        string selected = _lastFragments.GetValueOrDefault(id);
        if (selected == null || !_fragmentButtons.ContainsKey(selected))
        {
            selected = null;
            foreach (SouvenirData fragment in fragments)
                if (MetaSaveManager.IsSouvenirDiscovered(fragment.Id)) { selected = fragment.Id; break; }
            selected ??= fragments.Count > 0 ? fragments[0].Id : null;
        }
        if (selected != null)
        {
            SelectFragment(selected);
            _constellationButtons[id].FocusNeighborRight = _fragmentButtons[selected].GetPath();
        }
        else
        {
            _selectedFragment = null;
            _fragmentTitle.Text = "Aucun fragment";
            _fragmentText.Text = "Les souvenirs de cette constellation restent à retrouver.";
        }
    }

    private void SelectFragment(string id)
    {
        _selectedFragment = id;
        _lastFragments[_selectedConstellation] = id;
        SouvenirData fragment = SouvenirDataLoader.Get(id);
        bool discovered = MetaSaveManager.IsSouvenirDiscovered(id);
        _fragmentTitle.Text = discovered ? fragment.Name : "Fragment effacé";
        _fragmentText.Text = discovered ? fragment.Text : "Ce souvenir n’a pas encore été retrouvé.\n\nExplore les vestiges pour en recueillir les fragments.";
        _readingScroll.ScrollVertical = 0;
        _readingScroll.FocusNeighborLeft = _fragmentButtons[id].GetPath();
        _memoryMap.Select(SouvenirDataLoader.GetByConstellation(_selectedConstellation).FindIndex(s => s.Id == id));
        foreach ((string key, Button button) in _fragmentButtons)
        {
            MenuSkin.StyleButton(button, key == id);
            UITheme.SetTextRole(button, TextRole.Subhead);
        }
    }

    /// <summary>La constellation est gravée : points retrouvés lumineux, points perdus creux.</summary>
    private partial class MemoryMap : Control
    {
        private bool[] _known = System.Array.Empty<bool>();
        private Color _color;
        private int _selected;
        public void Configure(List<SouvenirData> fragments, Color color)
        {
            _known = new bool[fragments.Count];
            for (int i = 0; i < fragments.Count; i++) _known[i] = MetaSaveManager.IsSouvenirDiscovered(fragments[i].Id);
            _color = color.Lightened(0.22f);
            QueueRedraw();
        }
        public void Select(int selected) { _selected = selected; QueueRedraw(); }
        public override void _Draw()
        {
            Vector2 previous = Vector2.Zero;
            for (int i = 0; i < _known.Length; i++)
            {
                Vector2 point = new(20 + i * (Size.X - 40) / Mathf.Max(1, _known.Length - 1), 40 + (i * 37 % 47));
                if (i > 0) DrawLine(previous, point, _color with { A = 0.28f }, 2);
                DrawRect(new Rect2(point - Vector2.One * 5, Vector2.One * 10), _known[i] ? _color : UITheme.TextVeryDim, _known[i], 2);
                if (i == _selected) DrawRect(new Rect2(point - Vector2.One * 10, Vector2.One * 20), UITheme.GoldBright, false, 2);
                previous = point;
            }
        }
    }
}
