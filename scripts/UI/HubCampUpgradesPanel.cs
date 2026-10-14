using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Page Campement de l'accueil (plan 36, lot M2) : les améliorations permanentes achetées en Vestiges, par rayon.
/// Une liste d'objets garde les explications courtes et les rangs visibles. La fiche sélectionnée regroupe le gain,
/// l'achat et le remboursement libre (plan 36 M5), pour ne pas répéter les commandes sur quatorze lignes.
/// </summary>
public partial class HubCampUpgradesPanel : MarginContainer
{
    private const float ListWidth = 780f;
    private const float RowHeight = 104f;
    private const float DetailWidth = 640f;
    private static readonly Color MaxedColor = UITheme.GoldBright;
    private static readonly Color MissingColor = new(0.78f, 0.42f, 0.36f);

    private sealed class Row
    {
        public CampUpgradeData Upgrade;
        public Button Select;
        public CampRankPips Rank;
        public Texture2D Icon;
    }

    private readonly List<Row> _rows = new();
    private Label _balance;
    private Label _status;
    private Button _refundAll;
    private VBoxContainer _list;
    private Row _selected;
    private TextureRect _detailIcon;
    private Label _detailName;
    private Label _detailHint;
    private Label _detailRank;
    private Label _detailEffect;
    private Label _detailPrice;
    private Button _buySelected;
    private Button _refundSelected;
    private Texture2D _currency;
    private TextureRect _priceCurrency;
    private Control _confirmation;
    private Button _confirmCancel;
    private Button _confirmRefund;
    private string _defaultStatus = "";
    private bool _catalogFailed;

    public override void _Ready()
    {
        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 14);
        AddChild(column);

        HBoxContainer top = new();
        top.AddThemeConstantOverride("separation", 12);
        column.AddChild(top);
        _currency = UITheme.LoadTex(UITheme.IconsPath + "ui_icon_vestiges.png");
        if (_currency != null)
        {
            top.AddChild(new TextureRect
            {
                Texture = _currency,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(48f, 48f),
                TextureFilter = TextureFilterEnum.Nearest,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }
        _balance = UITheme.MakeLabel("", TextRole.Title, UITheme.GoldBright, TextWeight.Strong);
        _balance.AddThemeFontOverride("font", UITheme.StrongFont);
        _balance.TooltipText = Tr("UI_VESTIGES_HINT");
        _balance.MouseFilter = MouseFilterEnum.Pass;
        top.AddChild(_balance);
        _status = UITheme.MakeLabel("", TextRole.Lead, UITheme.TextColor);
        _status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _status.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _status.HorizontalAlignment = HorizontalAlignment.Right;
        _status.ClipText = true;
        _status.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _status.MouseFilter = MouseFilterEnum.Pass;
        top.AddChild(_status);
        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 40);
        column.AddChild(body);
        VBoxContainer left = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        left.AddThemeConstantOverride("separation", 18);
        body.AddChild(left);
        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(ListWidth + 16f, 0f), SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true,
        };
        left.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_list);
        MarginContainer footer = new();
        footer.AddThemeConstantOverride("margin_bottom", 36);
        left.AddChild(footer);
        _refundAll = MenuSkin.Button(Tr("UI_CAMP_REFUND_ALL"));
        _refundAll.Name = "RefundAllCamp";
        _refundAll.CustomMinimumSize = new Vector2(260f, 56f);
        _refundAll.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        _refundAll.Pressed += OpenRefundConfirmation;
        footer.AddChild(_refundAll);
        body.AddChild(MakeDetail());
        VisibilityChanged += () => { if (!IsVisibleInTree()) CloseRefundConfirmation(false); };
    }

    public override void _ExitTree() => CloseRefundConfirmation(false);

    public override void _Input(InputEvent @event)
    {
        if (_confirmation == null || !@event.IsActionPressed("ui_cancel")) return;
        AudioManager.PlayUI("sfx_menu_clic");
        CloseRefundConfirmation();
        GetViewport().SetInputAsHandled();
    }

    /// <summary>Reconstruit la liste et conserve l'objet examiné lors d'une réouverture.</summary>
    public Control Refresh()
    {
        string previous = _selected?.Upgrade.Id;
        MenuSkin.Clear(_list);
        _rows.Clear();
        _selected = null;
        AddBranch(CampBranch.Capacities, "UI_CAMP_BRANCH_CAPACITIES");
        AddBranch(CampBranch.Comfort, "UI_CAMP_BRANCH_COMFORT");
        AddBranch(CampBranch.Stats, "UI_CAMP_BRANCH_STATS");
        _catalogFailed = !CampUpgradeDataLoader.TryLoad(out string catalogError);
        _defaultStatus = _catalogFailed ? catalogError : DevelopmentMode.IsEnabled ? Tr("UI_CAMP_DEV_SLOTS") : "";
        SetStatus(_defaultStatus, _catalogFailed ? MissingColor : UITheme.TextColor);
        for (int i = 0; i < _rows.Count; i++)
        {
            if (i > 0) _rows[i].Select.FocusNeighborTop = _rows[i - 1].Select.GetPath();
            if (i + 1 < _rows.Count) _rows[i].Select.FocusNeighborBottom = _rows[i + 1].Select.GetPath();
        }
        if (_rows.Count > 0)
            SelectRow(_rows.Find(row => row.Upgrade.Id == previous) ?? _rows[0]);
        UpdateAll();
        return _selected?.Select ?? _refundAll;
    }

    private void AddBranch(CampBranch branch, string titleKey)
    {
        bool any = false;
        foreach (CampUpgradeData upgrade in CampUpgradeDataLoader.All)
        {
            if (upgrade.Branch != branch)
                continue;
            if (!any)
            {
                if (_list.GetChildCount() > 0)
                    _list.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 16f), MouseFilter = MouseFilterEnum.Ignore });
                _list.AddChild(UITheme.MakeLabel(Tr(titleKey), TextRole.Section, UITheme.GoldColor));
                any = true;
            }
            _list.AddChild(MakeRow(upgrade));
        }
    }

    private Button MakeRow(CampUpgradeData upgrade)
    {
        Row row = new() { Upgrade = upgrade, Icon = UITheme.LoadTex($"res://assets/ui/camp/{upgrade.Id}.png") };
        row.Select = MakeButton("", new Vector2(ListWidth, RowHeight));
        UITheme.SetPressSound(row.Select, "sfx_menu_select");
        row.Select.Name = upgrade.Id;
        row.Select.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        MarginContainer inset = new() { MouseFilter = MouseFilterEnum.Ignore };
        inset.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        inset.AddThemeConstantOverride("margin_left", 12);
        inset.AddThemeConstantOverride("margin_right", 20);
        inset.AddThemeConstantOverride("margin_top", 8);
        inset.AddThemeConstantOverride("margin_bottom", 8);
        row.Select.AddChild(inset);
        HBoxContainer line = new() { MouseFilter = MouseFilterEnum.Ignore };
        line.AddThemeConstantOverride("separation", 20);
        inset.AddChild(line);
        Panel iconFrame = new()
        {
            Name = "CampIconFrame", CustomMinimumSize = new Vector2(88f, 88f),
            MouseFilter = MouseFilterEnum.Ignore, TextureFilter = TextureFilterEnum.Nearest,
        };
        iconFrame.AddThemeStyleboxOverride("panel", UITheme.CreateNinePatch(MenuSkin.TextureFor("ui_card_locked.png"), 4, 4, 4, 4));
        line.AddChild(iconFrame);
        TextureRect icon = MakeIcon(row.Icon, 64f);
        icon.Position = new Vector2(12f, 12f);
        iconFrame.AddChild(icon);
        VBoxContainer text = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        text.AddThemeConstantOverride("separation", 4);
        line.AddChild(text);
        HBoxContainer heading = new() { MouseFilter = MouseFilterEnum.Ignore };
        heading.AddThemeConstantOverride("separation", 16);
        text.AddChild(heading);
        Label name = UITheme.MakeLabel(Tr(upgrade.NameKey), TextRole.Heading, UITheme.TextLight, TextWeight.Strong);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        name.ClipText = true;
        name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        heading.AddChild(name);
        row.Rank = new CampRankPips { Name = "RankPips" };
        row.Rank.SetRank(0, upgrade.MaxRank);
        heading.AddChild(row.Rank);
        Label hint = UITheme.MakeLabel(Tr(upgrade.NameKey + "_HINT"), TextRole.Body, UITheme.TextColor);
        hint.ClipText = true;
        hint.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        hint.TooltipText = hint.Text;
        text.AddChild(hint);
        row.Select.FocusEntered += () => SelectRow(row);
        row.Select.Pressed += () => SelectRow(row);
        // Survol à la souris : aperçu de la fiche, sans changer l'amélioration choisie ; elle revient à la sortie.
        row.Select.MouseEntered += () => ShowDetail(row);
        row.Select.MouseExited += () => Callable.From(RestoreDetail).CallDeferred();
        _rows.Add(row);
        return row.Select;
    }

    private PanelContainer MakeDetail()
    {
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(DetailWidth, 0f), SizeFlagsVertical = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 12);
        panel.AddChild(content);
        _detailIcon = MakeIcon(null, 128f);
        _detailIcon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        content.AddChild(_detailIcon);
        _detailName = UITheme.MakeLabel("", TextRole.Title, UITheme.TextLight, TextWeight.Strong, HorizontalAlignment.Center);
        content.AddChild(_detailName);
        _detailRank = UITheme.MakeLabel("", TextRole.Lead, UITheme.GoldColor, align: HorizontalAlignment.Center);
        content.AddChild(_detailRank);
        _detailHint = UITheme.MakeLabel("", TextRole.Lead, UITheme.TextColor, align: HorizontalAlignment.Center);
        _detailHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailHint.CustomMinimumSize = new Vector2(DetailWidth - 80f, 56f);
        content.AddChild(_detailHint);
        content.AddChild(new HSeparator());
        _detailEffect = UITheme.MakeLabel("", TextRole.Heading, UITheme.TextLight, TextWeight.Strong, HorizontalAlignment.Center);
        _detailEffect.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailEffect.CustomMinimumSize = new Vector2(DetailWidth - 80f, 56f);
        content.AddChild(_detailEffect);
        HBoxContainer price = new() { Alignment = BoxContainer.AlignmentMode.Center };
        price.AddThemeConstantOverride("separation", 12);
        content.AddChild(price);
        _priceCurrency = MakeIcon(_currency, 40f);
        _priceCurrency.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        price.AddChild(_priceCurrency);
        _detailPrice = UITheme.MakeLabel("", TextRole.Heading, UITheme.GoldBright, TextWeight.Strong, HorizontalAlignment.Center);
        _detailPrice.TooltipText = Tr("UI_VESTIGES_HINT");
        _detailPrice.MouseFilter = MouseFilterEnum.Pass;
        price.AddChild(_detailPrice);
        content.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 16);
        content.AddChild(actions);
        _buySelected = MenuSkin.Button(Tr("UI_CAMP_BUY"), true);
        _buySelected.Name = "BuyCampUpgrade";
        UITheme.ApplyTypography(_buySelected, TextRole.Heading, TextWeight.Strong);
        _buySelected.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        // Achat et remboursement sonnent leur issue (cloche, clic ou refus) : pas de clic d'appui en plus.
        UITheme.SetPressSound(_buySelected, null);
        _buySelected.Pressed += () => { if (_selected != null) OnBuy(_selected); };
        actions.AddChild(_buySelected);
        _refundSelected = MenuSkin.Button(Tr("UI_CAMP_REFUND"));
        _refundSelected.Name = "RefundCampUpgrade";
        UITheme.ApplyTypography(_refundSelected, TextRole.Heading, TextWeight.Strong);
        _refundSelected.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        UITheme.SetPressSound(_refundSelected, null);
        _refundSelected.Pressed += () => { if (_selected != null) OnRefund(_selected); };
        actions.AddChild(_refundSelected);
        return panel;
    }

    private static TextureRect MakeIcon(Texture2D texture, float size) => new()
    {
        Texture = texture, Size = new Vector2(size, size), CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = TextureFilterEnum.Nearest, MouseFilter = MouseFilterEnum.Ignore,
    };

    private void SelectRow(Row row)
    {
        if (_selected == row) return;
        _selected = row;
        foreach (Row other in _rows)
            MenuSkin.StyleRosterCard(other.Select, other == row);
        SetStatus(_defaultStatus, _catalogFailed ? MissingColor : UITheme.TextColor);
        UpdateDetail();
    }

    /// <summary>Fin d'un aperçu : la fiche revient à l'amélioration choisie, sauf si la souris est passée sur une autre carte.</summary>
    private void RestoreDetail()
    {
        foreach (Row row in _rows)
            if (row.Select.IsHovered())
                return;
        if (_selected != null)
            ShowDetail(_selected);
    }

    private static Button MakeButton(string text, Vector2 size)
    {
        Button button = new() { Text = text, CustomMinimumSize = size, FocusMode = FocusModeEnum.All };
        UITheme.ApplyTypography(button, TextRole.Lead, TextWeight.Strong);
        UITheme.ApplyButtonStyle(button, UITheme.LoadTex(UITheme.MenusPath + "ui_card_normal.png"),
            UITheme.LoadTex(UITheme.MenusPath + "ui_card_selected.png"), UITheme.LoadTex(UITheme.MenusPath + "ui_card_selected.png"),
            UITheme.LoadTex(UITheme.MenusPath + "ui_card_locked.png"));
        // Le cadre doré de la carte choisie marque le focus, comme la page Personnages.
        button.AddThemeStyleboxOverride("focus", UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + "ui_card_selected.png"), 4, 4, 4, 4));
        return button;
    }

    private void OnBuy(Row row)
    {
        CampTransaction result = MetaSaveManager.BuyCampRank(row.Upgrade.Id, out SaveFile.WriteResult saved);
        int rank = MetaSaveManager.GetCampRank(row.Upgrade.Id);
        switch (result)
        {
            case CampTransaction.Done:
                AudioManager.PlayUI("sfx_menu_confirmer");
                SetStatus(string.Format(Tr("UI_CAMP_BOUGHT"), Tr(row.Upgrade.NameKey), rank, row.Upgrade.MaxRank), UITheme.GoldBright);
                break;
            case CampTransaction.NotEnoughVestiges:
                AudioManager.PlayUI("sfx_perk_refuse");
                int missing = row.Upgrade.PriceOfRank(rank + 1) - MetaSaveManager.GetVestiges();
                SetStatus(string.Format(Tr("UI_CAMP_NOT_ENOUGH"), missing), MissingColor);
                break;
            default:
                AudioManager.PlayUI("sfx_perk_refuse");
                SetStatus(Tr("UI_CAMP_MAX"), UITheme.TextColor);
                break;
        }
        AfterTransaction(row, saved);
    }

    private void OnRefund(Row row)
    {
        int before = MetaSaveManager.GetVestiges();
        if (MetaSaveManager.RefundCampRank(row.Upgrade.Id, out SaveFile.WriteResult saved) != CampTransaction.Done)
        {
            AudioManager.PlayUI("sfx_perk_refuse");
            return;
        }
        AudioManager.PlayUI("sfx_menu_clic");
        SetStatus(string.Format(Tr("UI_CAMP_REFUNDED"), Tr(row.Upgrade.NameKey), MetaSaveManager.GetVestiges() - before), UITheme.TextLight);
        AfterTransaction(row, saved);
    }

    private void OpenRefundConfirmation()
    {
        if (_refundAll.Disabled || _confirmation != null) return;
        _confirmation = new Control { Name = "CampRefundConfirmation", MouseFilter = MouseFilterEnum.Stop };
        // Sœur de la page : le voile couvre aussi Retour et bloque toute commande derrière le dialogue.
        GetParentControl().AddChild(_confirmation);
        _confirmation.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ColorRect veil = new() { Color = new Color(0.025f, 0.025f, 0.045f, 0.88f) };
        veil.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _confirmation.AddChild(veil);
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _confirmation.AddChild(center);
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(860f, 0f) };
        panel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        center.AddChild(panel);
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 24);
        panel.AddChild(content);
        content.AddChild(UITheme.MakeLabel(Tr("UI_CAMP_REFUND_CONFIRM_TITLE"), TextRole.Title, UITheme.TextLight,
            TextWeight.Strong, HorizontalAlignment.Center));
        Label description = UITheme.MakeLabel(Tr("UI_CAMP_REFUND_CONFIRM_BODY"), TextRole.Body, UITheme.TextColor,
            align: HorizontalAlignment.Center);
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        description.CustomMinimumSize = new Vector2(760f, 0f);
        content.AddChild(description);
        HBoxContainer returned = new() { Alignment = BoxContainer.AlignmentMode.Center };
        returned.AddThemeConstantOverride("separation", 12);
        content.AddChild(returned);
        returned.AddChild(MakeIcon(_currency, 48f));
        Label refundAmount = UITheme.MakeLabel("+" + MetaSaveManager.GetCampRefundAmount().ToString("N0", PlayerSheet.French),
            TextRole.Title, UITheme.GoldBright, TextWeight.Strong);
        refundAmount.AddThemeFontOverride("font", UITheme.StrongFont);
        returned.AddChild(refundAmount);
        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 24);
        content.AddChild(actions);
        _confirmCancel = MenuSkin.Button(Tr("UI_CAMP_REFUND_CANCEL"), true);
        _confirmCancel.Name = "CancelCampRefund";
        _confirmCancel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _confirmCancel.Pressed += () => CloseRefundConfirmation();
        actions.AddChild(_confirmCancel);
        _confirmRefund = MenuSkin.Button(Tr("UI_CAMP_REFUND_ALL"));
        _confirmRefund.Name = "ConfirmCampRefund";
        _confirmRefund.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        UITheme.SetPressSound(_confirmRefund, null);
        _confirmRefund.Pressed += ConfirmRefundAll;
        actions.AddChild(_confirmRefund);
        _confirmCancel.FocusNeighborLeft = _confirmCancel.GetPath();
        _confirmCancel.FocusNeighborRight = _confirmRefund.GetPath();
        _confirmRefund.FocusNeighborLeft = _confirmCancel.GetPath();
        _confirmRefund.FocusNeighborRight = _confirmRefund.GetPath();
        foreach (Button button in new[] { _confirmCancel, _confirmRefund })
        {
            button.FocusNeighborTop = button.FocusNeighborBottom = button.GetPath();
            Button other = button == _confirmCancel ? _confirmRefund : _confirmCancel;
            button.FocusNext = button.FocusPrevious = other.GetPath();
        }
        _confirmCancel.GrabFocus();
    }

    private void CloseRefundConfirmation(bool restoreFocus = true)
    {
        if (_confirmation == null) return;
        _confirmation.Hide();
        _confirmation.QueueFree();
        _confirmation = null;
        _confirmCancel = _confirmRefund = null;
        if (restoreFocus && IsVisibleInTree())
            (_refundAll.Disabled ? _selected?.Select : _refundAll)?.GrabFocus();
    }

    private void ConfirmRefundAll()
    {
        if (_confirmation == null) return;
        CloseRefundConfirmation();
        int refunded = MetaSaveManager.RefundAllCamp(out SaveFile.WriteResult saved);
        AudioManager.PlayUI(refunded > 0 ? "sfx_menu_clic" : "sfx_perk_refuse");
        SetStatus(string.Format(Tr("UI_CAMP_REFUNDED_ALL"), refunded), UITheme.TextLight);
        AfterTransaction(_selected, saved);
    }

    private void AfterTransaction(Row row, SaveFile.WriteResult saved)
    {
        if (!saved.Succeeded)
            SetStatus(string.Format(Tr("UI_CAMP_SAVE_FAILED"), saved.Error), MissingColor);
        Control focused = GetViewport().GuiGetFocusOwner();
        UpdateAll();
        if (row == null)
            return;
        // Un bouton « Rendre » éteint par la transaction ne garde pas le focus : la ligne le reprend.
        if (focused is Button { Disabled: true })
            row.Select.GrabFocus();
    }

    private void SetStatus(string text, Color color)
    {
        _status.Text = text;
        _status.TooltipText = text;
        _status.AddThemeColorOverride("font_color", color);
    }

    private void UpdateAll()
    {
        _balance.Text = MetaSaveManager.GetVestiges().ToString("N0", PlayerSheet.French);
        bool anyBought = false;
        foreach (Row row in _rows)
        {
            int rank = MetaSaveManager.GetCampRank(row.Upgrade.Id);
            anyBought |= rank > 0;
            row.Rank.SetRank(rank, row.Upgrade.MaxRank);
            row.Select.TooltipText = string.Format(Tr("UI_CAMP_RANK"), rank, row.Upgrade.MaxRank);
        }
        _refundAll.Disabled = !anyBought;
        UpdateDetail();
    }

    private void UpdateDetail()
    {
        if (_selected == null)
        {
            _buySelected.Disabled = _refundSelected.Disabled = true;
            return;
        }
        ShowDetail(_selected);
        CampUpgradeData upgrade = _selected.Upgrade;
        int rank = MetaSaveManager.GetCampRank(upgrade.Id);
        bool maxed = rank >= upgrade.MaxRank;
        _buySelected.Disabled = maxed;
        _refundSelected.Disabled = rank == 0;
        _buySelected.FocusNeighborLeft = _refundSelected.FocusNeighborLeft = _selected.Select.GetPath();
        _selected.Select.FocusNeighborRight = (maxed ? _refundSelected : _buySelected).GetPath();
    }

    /// <summary>Remplit la fiche ; les boutons d'achat restent attachés à l'amélioration choisie (<see cref="UpdateDetail"/>).</summary>
    private void ShowDetail(Row row)
    {
        CampUpgradeData upgrade = row.Upgrade;
        int rank = MetaSaveManager.GetCampRank(upgrade.Id);
        bool maxed = rank >= upgrade.MaxRank;
        _detailIcon.Texture = row.Icon;
        _detailName.Text = Tr(upgrade.NameKey);
        _detailHint.Text = Tr(upgrade.NameKey + "_HINT");
        _detailRank.Text = string.Format(Tr("UI_CAMP_RANK"), rank, upgrade.MaxRank);
        _detailEffect.Text = EffectText(upgrade, rank);
        _detailPrice.Text = maxed ? Tr("UI_CAMP_MAXED") : upgrade.PriceOfRank(rank + 1).ToString("N0", PlayerSheet.French);
        _priceCurrency.Visible = !maxed;
        _detailPrice.AddThemeColorOverride("font_color", maxed ? MaxedColor
            : upgrade.PriceOfRank(rank + 1) <= MetaSaveManager.GetVestiges() ? UITheme.GoldBright : MissingColor);
    }

    /// <summary>Gain du prochain achat ; au maximum, bonus total acquis. Les unités viennent du catalogue localisé.</summary>
    private string EffectText(CampUpgradeData upgrade, int rank)
    {
        float amount = rank >= upgrade.MaxRank ? upgrade.TotalAt(rank) : upgrade.Step;
        bool plural = amount > 1f && upgrade.Effect is CampEffect.WeaponSlots or CampEffect.Rerolls or CampEffect.Banishes;
        string key = upgrade.DescKey + (plural ? "_PLURAL" : "");
        return string.Format(Tr(key), FormatAmount(upgrade, amount));
    }

    private static string FormatAmount(CampUpgradeData upgrade, float value)
    {
        if (upgrade.Stat == "regen_rate") value *= 60f;
        return upgrade.Percent
            ? (value * 100f).ToString("0.#", PlayerSheet.French) + " %"
            : value.ToString("0.#", PlayerSheet.French);
    }
}
