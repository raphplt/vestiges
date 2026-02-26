using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Popup de découverte d'un souvenir pendant la run.
/// S'affiche brièvement avec le nom et un extrait, puis disparaît.
/// </summary>
public partial class SouvenirPopup : CanvasLayer
{
    private PanelContainer _panel;
    private Label _titleLabel;
    private Label _constellationLabel;
    private Label _textLabel;
    private Tween _activeTween;
    private EventBus _eventBus;

    public override void _Ready()
    {
        Layer = 80;

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.SouvenirDiscovered += OnSouvenirDiscovered;

        BuildUI();
        _panel.Visible = false;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.SouvenirDiscovered -= OnSouvenirDiscovered;
    }

    private void BuildUI()
    {
        Control root = new();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _panel.OffsetTop = 40;
        _panel.OffsetLeft = -200;
        _panel.OffsetRight = 200;
        _panel.CustomMinimumSize = new Vector2(400, 0);
        _panel.MouseFilter = Control.MouseFilterEnum.Ignore;

        StyleBoxFlat style = new()
        {
            BgColor = new Color(0.05f, 0.04f, 0.1f, 0.92f),
            BorderColor = new Color(0.85f, 0.75f, 0.4f, 0.7f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        _panel.AddThemeStyleboxOverride("panel", style);
        root.AddChild(_panel);

        VBoxContainer vbox = new();
        vbox.AddThemeConstantOverride("separation", 6);
        _panel.AddChild(vbox);

        _constellationLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _constellationLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_constellationLabel);

        _titleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.82f, 0.5f));
        vbox.AddChild(_titleLabel);

        _textLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(360, 0)
        };
        _textLabel.AddThemeFontSizeOverride("font_size", 12);
        _textLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.68f, 0.6f));
        vbox.AddChild(_textLabel);
    }

    private void OnSouvenirDiscovered(string souvenirId, string souvenirName, string constellationId)
    {
        SouvenirData data = SouvenirDataLoader.Get(souvenirId);
        if (data == null)
            return;

        ConstellationData constellation = SouvenirDataLoader.GetConstellation(constellationId);
        string constellationName = constellation?.Name ?? constellationId;
        Color constellationColor = constellation?.Color ?? new Color(0.7f, 0.7f, 0.7f);

        _constellationLabel.Text = constellationName;
        _constellationLabel.AddThemeColorOverride("font_color", constellationColor);
        _titleLabel.Text = data.Name;

        // Show first 120 chars of text as preview
        string preview = data.Text.Length > 120
            ? data.Text[..120] + "..."
            : data.Text;
        _textLabel.Text = preview;

        ShowPopup();
    }

    private void ShowPopup()
    {
        _activeTween?.Kill();

        _panel.Visible = true;
        _panel.Modulate = new Color(1, 1, 1, 0);

        _activeTween = CreateTween();
        _activeTween.TweenProperty(_panel, "modulate", new Color(1, 1, 1, 1), 0.5f)
            .SetTrans(Tween.TransitionType.Sine);
        _activeTween.TweenInterval(4.0f);
        _activeTween.TweenProperty(_panel, "modulate", new Color(1, 1, 1, 0), 1.0f)
            .SetTrans(Tween.TransitionType.Sine);
        _activeTween.TweenCallback(Callable.From(() => _panel.Visible = false));
    }
}
