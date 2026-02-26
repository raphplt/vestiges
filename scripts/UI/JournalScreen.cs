using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Journal des Souvenirs — affiche les fragments découverts classés par constellation.
/// Accessible via la touche J pendant la run.
/// Semi-diégétique : parchemin usé, écriture de survivant.
/// </summary>
public partial class JournalScreen : CanvasLayer
{
    private Control _root;
    private VBoxContainer _fragmentList;
    private Label _fragmentTitle;
    private Label _fragmentText;
    private Label _progressLabel;
    private string _selectedConstellation;
    private Dictionary<string, Button> _constellationButtons = new();

    private bool _isVisible;

    public bool IsOpen => _isVisible;

    public override void _Ready()
    {
        Layer = 90;
        BuildUI();
        _root.Visible = false;
    }

    public void Toggle()
    {
        if (_isVisible)
            Hide();
        else
            Show();
    }

    public new void Show()
    {
        _isVisible = true;
        _root.Visible = true;
        RefreshContent();
        GetTree().Paused = true;
    }

    public new void Hide()
    {
        _isVisible = false;
        _root.Visible = false;
        GetTree().Paused = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isVisible)
            return;

        if (@event.IsActionPressed("ui_cancel"))
        {
            Hide();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildUI()
    {
        _root = new Control();
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.ProcessMode = ProcessModeEnum.Always;
        AddChild(_root);

        // Background overlay
        ColorRect overlay = new();
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.Color = new Color(0.02f, 0.02f, 0.05f, 0.88f);
        _root.AddChild(overlay);

        // Main container
        MarginContainer margin = new();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_top", 50);
        margin.AddThemeConstantOverride("margin_bottom", 50);
        margin.AddThemeConstantOverride("margin_left", 80);
        margin.AddThemeConstantOverride("margin_right", 80);
        _root.AddChild(margin);

        VBoxContainer mainVBox = new();
        mainVBox.AddThemeConstantOverride("separation", 16);
        margin.AddChild(mainVBox);

        // Header
        HBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 20);
        mainVBox.AddChild(header);

        Label title = new()
        {
            Text = "SOUVENIRS",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.82f, 0.5f));
        header.AddChild(title);

        _progressLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _progressLabel.AddThemeFontSizeOverride("font_size", 14);
        _progressLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.6f));
        header.AddChild(_progressLabel);

        // Content: constellation tabs + fragment detail
        HBoxContainer content = new()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 20);
        mainVBox.AddChild(content);

        // Left: constellation list
        content.AddChild(BuildConstellationPanel());

        // Center: fragment list
        ScrollContainer fragmentScroll = new()
        {
            CustomMinimumSize = new Vector2(280, 0),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        content.AddChild(fragmentScroll);

        _fragmentList = new VBoxContainer();
        _fragmentList.AddThemeConstantOverride("separation", 6);
        fragmentScroll.AddChild(_fragmentList);

        // Right: fragment detail
        content.AddChild(BuildDetailPanel());

        // Footer
        Label closeHint = new()
        {
            Text = "[Echap] Fermer",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        closeHint.AddThemeFontSizeOverride("font_size", 12);
        closeHint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
        mainVBox.AddChild(closeHint);
    }

    private VBoxContainer BuildConstellationPanel()
    {
        VBoxContainer panel = new()
        {
            CustomMinimumSize = new Vector2(200, 0)
        };
        panel.AddThemeConstantOverride("separation", 4);

        Label sectionTitle = new()
        {
            Text = "Constellations"
        };
        sectionTitle.AddThemeFontSizeOverride("font_size", 14);
        sectionTitle.AddThemeColorOverride("font_color", new Color(0.65f, 0.6f, 0.5f));
        panel.AddChild(sectionTitle);

        List<ConstellationData> constellations = SouvenirDataLoader.GetAllConstellations();
        foreach (ConstellationData c in constellations)
        {
            Button btn = new()
            {
                Text = c.Name,
                ToggleMode = true,
                CustomMinimumSize = new Vector2(190, 30)
            };
            btn.AddThemeFontSizeOverride("font_size", 13);

            string capturedId = c.Id;
            btn.Pressed += () => OnConstellationSelected(capturedId);

            _constellationButtons[c.Id] = btn;
            panel.AddChild(btn);
        }

        return panel;
    }

    private VBoxContainer BuildDetailPanel()
    {
        VBoxContainer panel = new()
        {
            CustomMinimumSize = new Vector2(350, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        panel.AddThemeConstantOverride("separation", 12);

        _fragmentTitle = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _fragmentTitle.AddThemeFontSizeOverride("font_size", 18);
        _fragmentTitle.AddThemeColorOverride("font_color", new Color(0.9f, 0.82f, 0.5f));
        panel.AddChild(_fragmentTitle);

        _fragmentText = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(340, 0)
        };
        _fragmentText.AddThemeFontSizeOverride("font_size", 14);
        _fragmentText.AddThemeColorOverride("font_color", new Color(0.75f, 0.72f, 0.65f));
        panel.AddChild(_fragmentText);

        return panel;
    }

    private void RefreshContent()
    {
        List<string> discovered = MetaSaveManager.GetDiscoveredSouvenirs();
        int total = SouvenirDataLoader.GetAll().Count;
        _progressLabel.Text = $"{discovered.Count} / {total} fragments";

        // Select first constellation if none selected
        if (string.IsNullOrEmpty(_selectedConstellation))
        {
            List<ConstellationData> constellations = SouvenirDataLoader.GetAllConstellations();
            if (constellations.Count > 0)
                _selectedConstellation = constellations[0].Id;
        }

        RefreshConstellationHighlight();
        RefreshFragmentList();
        ClearDetail();
    }

    private void OnConstellationSelected(string constellationId)
    {
        _selectedConstellation = constellationId;
        RefreshConstellationHighlight();
        RefreshFragmentList();
        ClearDetail();
    }

    private void RefreshConstellationHighlight()
    {
        foreach (KeyValuePair<string, Button> pair in _constellationButtons)
        {
            pair.Value.ButtonPressed = pair.Key == _selectedConstellation;

            ConstellationData c = SouvenirDataLoader.GetConstellation(pair.Key);
            List<SouvenirData> fragments = SouvenirDataLoader.GetByConstellation(pair.Key);
            int discoveredCount = 0;
            foreach (SouvenirData s in fragments)
            {
                if (MetaSaveManager.IsSouvenirDiscovered(s.Id))
                    discoveredCount++;
            }

            pair.Value.Text = $"{c?.Name ?? pair.Key}  ({discoveredCount}/{fragments.Count})";
        }
    }

    private void RefreshFragmentList()
    {
        foreach (Node child in _fragmentList.GetChildren())
            child.QueueFree();

        if (string.IsNullOrEmpty(_selectedConstellation))
            return;

        List<SouvenirData> fragments = SouvenirDataLoader.GetByConstellation(_selectedConstellation);

        foreach (SouvenirData s in fragments)
        {
            bool discovered = MetaSaveManager.IsSouvenirDiscovered(s.Id);

            Button fragmentBtn = new()
            {
                Text = discovered ? s.Name : "???",
                CustomMinimumSize = new Vector2(260, 28),
                Disabled = !discovered
            };
            fragmentBtn.AddThemeFontSizeOverride("font_size", 13);

            if (discovered)
            {
                string capturedId = s.Id;
                fragmentBtn.Pressed += () => ShowFragmentDetail(capturedId);
            }

            _fragmentList.AddChild(fragmentBtn);
        }
    }

    private void ShowFragmentDetail(string souvenirId)
    {
        SouvenirData data = SouvenirDataLoader.Get(souvenirId);
        if (data == null)
            return;

        _fragmentTitle.Text = data.Name;
        _fragmentText.Text = data.Text;
    }

    private void ClearDetail()
    {
        _fragmentTitle.Text = "";
        _fragmentText.Text = "Sélectionner un fragment pour le lire.";
    }
}
