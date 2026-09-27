using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Écran de choix commun (plan 17 vague 3) : bénédictions et services du Mémorial, offres de la Faille. Même grammaire
/// que le level-up (<see cref="ChoiceStyle"/>) ; souris, clavier et manette : haut/bas entre les cartes et le bouton
/// de sortie, validation pour choisir, annulation pour sortir quand c'est permis. Fige la run tant qu'il est ouvert.
/// </summary>
public partial class ChoiceScreen : CanvasLayer
{
    // Largeur du texte d'une carte : carte moins marges, icône et écart.
    private const float TextWidth = ChoiceStyle.CardWidth - 110f;

    private ColorRect _overlay;
    private PanelContainer _panel;
    private Label _title;
    private Label _subtitle;
    private VBoxContainer _cardsContainer;
    private HBoxContainer _actions;
    private readonly List<PanelContainer> _cardPanels = new();
    private readonly List<ChoiceCard> _cards = new();
    private Button _cancelButton;
    private Action<int> _onChosen;
    private int _focusIndex;

    public bool IsOpen => Visible;
    public bool CanCancel => _cancelButton != null;

    public override void _Ready()
    {
        Layer = 20;
        ProcessMode = ProcessModeEnum.Always;
        BuildUI();
        Visible = false;
    }

    /// <summary>
    /// Ouvre l'écran. <paramref name="onChosen"/> reçoit l'index de la carte choisie, ou -1 si le joueur sort
    /// (seulement si <paramref name="cancelText"/> est donné). L'écran est fermé quand il est appelé.
    /// </summary>
    public void Open(string title, string subtitle, IReadOnlyList<ChoiceCard> cards, string cancelText, Action<int> onChosen)
    {
        Clear();
        _title.Text = title;
        _subtitle.Text = subtitle ?? "";
        _subtitle.Visible = !string.IsNullOrEmpty(subtitle);
        _onChosen = onChosen;
        foreach (ChoiceCard card in cards)
            AddCard(card);
        if (cancelText != null)
        {
            _cancelButton = new Button
            {
                Text = cancelText,
                CustomMinimumSize = new Vector2(150, 34),
                FocusMode = Control.FocusModeEnum.None,
            };
            UITheme.SetTextRole(_cancelButton, TextRole.Small);
            _cancelButton.AddThemeColorOverride("font_color", ChoiceStyle.TextColor);
            UITheme.WireButtonAudio(_cancelButton);
            _cancelButton.Pressed += () => Close(-1);
            int index = _cards.Count;
            _cancelButton.MouseEntered += () => SetFocus(index);
            _actions.AddChild(_cancelButton);
        }

        SetFocus(FirstEnabled());
        Visible = true;
        GetTree().Paused = true;
    }

    private void BuildUI()
    {
        _overlay = new ColorRect { Color = ChoiceStyle.OverlayColor, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_overlay);

        _panel = new PanelContainer { CustomMinimumSize = new Vector2(ChoiceStyle.CardWidth + 40f, 100) };
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.GrowHorizontal = Control.GrowDirection.Both;
        _panel.GrowVertical = Control.GrowDirection.Both;
        Texture2D frame = ResourceLoader.Exists(UITheme.MenusPath + "ui_panel_frame.png")
            ? GD.Load<Texture2D>(UITheme.MenusPath + "ui_panel_frame.png")
            : null;
        if (frame != null)
        {
            StyleBoxTexture style = UITheme.CreateNinePatch(frame, 6, 6, 6, 6);
            style.ContentMarginLeft = 20;
            style.ContentMarginRight = 20;
            style.ContentMarginTop = 16;
            style.ContentMarginBottom = 18;
            _panel.AddThemeStyleboxOverride("panel", style);
        }
        AddChild(_panel);

        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(inner);

        _title = ChoiceStyle.MakeLabel("", TextRole.Subhead, ChoiceStyle.GoldBright, false, HorizontalAlignment.Center);
        inner.AddChild(_title);
        _subtitle = ChoiceStyle.MakeLabel("", TextRole.Small, ChoiceStyle.TextColor, false, HorizontalAlignment.Center);
        inner.AddChild(_subtitle);

        _cardsContainer = new VBoxContainer();
        _cardsContainer.AddThemeConstantOverride("separation", 8);
        inner.AddChild(_cardsContainer);

        _actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        inner.AddChild(_actions);
    }

    private void AddCard(ChoiceCard card)
    {
        int index = _cards.Count;
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(ChoiceStyle.CardWidth, 64), MouseFilter = Control.MouseFilterEnum.Stop };
        panel.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                Activate(index);
                panel.AcceptEvent();
            }
        };
        panel.MouseEntered += () => SetFocus(index);

        MarginContainer margin = new();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", side is "left" or "right" ? 12 : 8);
        panel.AddChild(margin);
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 14);
        margin.AddChild(row);

        if (card.Icon != null)
        {
            row.AddChild(new TextureRect
            {
                Texture = card.Icon,
                // Icônes 32×32 à échelle entière (×2).
                CustomMinimumSize = new Vector2(64, 64),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            });
        }

        VBoxContainer text = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 2);
        row.AddChild(text);

        HBoxContainer header = new();
        text.AddChild(header);
        header.AddChild(ChoiceStyle.MakeLabel(card.Tag, TextRole.Caption, card.Frame, true));
        if (!string.IsNullOrEmpty(card.Price))
            header.AddChild(ChoiceStyle.MakeLabel(card.Price, TextRole.Caption, card.Enabled ? ChoiceStyle.GoldBright : ChoiceStyle.TextDim, false, HorizontalAlignment.Right));
        text.AddChild(ChoiceStyle.MakeLabel(card.Title, TextRole.Body, ChoiceStyle.TextLight, false));
        foreach ((string line, Color color) in card.Lines)
        {
            // Les lignes longues (Oublis) passent à la ligne au lieu de déborder de la carte.
            Label label = ChoiceStyle.MakeLabel(line, TextRole.Small, color, false);
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            // Largeur fixée : sans elle, une étiquette qui passe à la ligne se mesure à zéro et fausse la taille du panneau.
            label.CustomMinimumSize = new Vector2(TextWidth, 0f);
            text.AddChild(label);
        }

        _cards.Add(card);
        _cardPanels.Add(panel);
        _cardsContainer.AddChild(panel);
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;

        int total = _cards.Count + (_cancelButton != null ? 1 : 0);
        if (@event.IsActionPressed("ui_down"))
            SetFocus((_focusIndex + 1) % total);
        else if (@event.IsActionPressed("ui_up"))
            SetFocus((_focusIndex - 1 + total) % total);
        else if (@event.IsActionPressed("ui_accept"))
            Activate(_focusIndex);
        else if (@event.IsActionPressed("ui_cancel"))
        {
            if (_cancelButton != null)
                Close(-1);
        }
        else
            return;
        GetViewport().SetInputAsHandled();
    }

    private void SetFocus(int index)
    {
        _focusIndex = index;
        for (int i = 0; i < _cards.Count; i++)
            ChoiceStyle.StyleCard(_cardPanels[i], _cards[i].Frame, _cards[i].Rank, i == index, _cards[i].Enabled);
        if (_cancelButton != null)
            ChoiceStyle.StyleButton(_cancelButton, index == _cards.Count);
    }

    /// <summary>Choix au clavier ou par un bot de test : même effet qu'un clic.</summary>
    public void Activate(int index)
    {
        if (index >= _cards.Count)
        {
            if (_cancelButton != null)
                Close(-1);
            return;
        }
        if (!_cards[index].Enabled)
        {
            AudioManager.PlayUI("sfx_perk_refuse", 0f);
            return;
        }
        Close(index);
    }

    private int FirstEnabled()
    {
        for (int i = 0; i < _cards.Count; i++)
            if (_cards[i].Enabled)
                return i;
        return _cards.Count;
    }

    private void Close(int choice)
    {
        Action<int> callback = _onChosen;
        _onChosen = null;
        Visible = false;
        GetTree().Paused = false;
        Clear();
        callback?.Invoke(choice);
    }

    private void Clear()
    {
        foreach (Node child in _cardsContainer.GetChildren())
            child.QueueFree();
        foreach (Node child in _actions.GetChildren())
            child.QueueFree();
        _cards.Clear();
        _cardPanels.Clear();
        _cancelButton = null;
    }
}
