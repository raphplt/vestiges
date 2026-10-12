using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Meta;

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
        public Label Rank;
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
        Texture2D icon = UITheme.LoadTex(UITheme.IconsPath + "ui_icon_vestiges.png");
        if (icon != null)
        {
            top.AddChild(new TextureRect
            {
                Texture = icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(32f, 32f),
                TextureFilter = TextureFilterEnum.Nearest,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }
        _balance = CampTypography.Label("", TextRole.Title, UITheme.GoldBright, TextWeight.Strong);
        top.AddChild(_balance);
        _status = CampTypography.Label("", TextRole.Lead, UITheme.TextColor);
        _status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _status.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _status.HorizontalAlignment = HorizontalAlignment.Right;
        _status.ClipText = true;
        _status.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _status.MouseFilter = MouseFilterEnum.Pass;
        top.AddChild(_status);
        _refundAll = MakeButton(Tr("UI_CAMP_REFUND_ALL"), new Vector2(240f, 56f));
        _refundAll.Pressed += OnRefundAll;
        top.AddChild(_refundAll);

        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        body.AddThemeConstantOverride("separation", 40);
        column.AddChild(body);
        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(ListWidth + 16f, 0f), SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true,
        };
        body.AddChild(scroll);
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_list);
        body.AddChild(MakeDetail());
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
                _list.AddChild(CampTypography.Label(Tr(titleKey), TextRole.Subhead, UITheme.GoldColor, TextWeight.Strong));
                any = true;
            }
            _list.AddChild(MakeRow(upgrade));
        }
    }

    private Button MakeRow(CampUpgradeData upgrade)
    {
        Row row = new() { Upgrade = upgrade, Icon = UITheme.LoadTex($"res://assets/ui/camp/{upgrade.Id}.png") };
        row.Select = MakeButton("", new Vector2(ListWidth, RowHeight));
        row.Select.Name = upgrade.Id;
        row.Select.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        Panel iconFrame = new()
        {
            Name = "CampIconFrame", Position = new Vector2(12f, 8f), Size = new Vector2(88f, 88f),
            MouseFilter = MouseFilterEnum.Ignore, TextureFilter = TextureFilterEnum.Nearest,
        };
        iconFrame.AddThemeStyleboxOverride("panel", UITheme.CreateNinePatch(MenuSkin.TextureFor("ui_card_locked.png"), 4, 4, 4, 4));
        row.Select.AddChild(iconFrame);
        TextureRect icon = MakeIcon(row.Icon, 64f);
        icon.Position = new Vector2(12f, 12f);
        iconFrame.AddChild(icon);
        Label name = CampTypography.Label(Tr(upgrade.NameKey), TextRole.Heading, UITheme.TextLight, TextWeight.Strong);
        name.Position = new Vector2(120f, 12f);
        row.Select.AddChild(name);
        Label hint = CampTypography.Label(Tr(upgrade.NameKey + "_HINT"), TextRole.Body, UITheme.TextColor);
        hint.Position = new Vector2(120f, 52f);
        hint.Size = new Vector2(ListWidth - 144f, 40f);
        hint.ClipText = true;
        hint.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        hint.TooltipText = hint.Text;
        row.Select.AddChild(hint);
        row.Rank = CampTypography.Label("", TextRole.Lead, UITheme.TextColor, TextWeight.Strong, HorizontalAlignment.Right);
        row.Rank.Position = new Vector2(ListWidth - 92f, 14f);
        row.Rank.Size = new Vector2(72f, 30f);
        row.Select.AddChild(row.Rank);
        row.Select.FocusEntered += () => SelectRow(row);
        row.Select.Pressed += () => SelectRow(row);
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
        _detailName = CampTypography.Label("", TextRole.Title, UITheme.TextLight, TextWeight.Strong, HorizontalAlignment.Center);
        content.AddChild(_detailName);
        _detailRank = CampTypography.Label("", TextRole.Lead, UITheme.GoldColor, align: HorizontalAlignment.Center);
        content.AddChild(_detailRank);
        _detailHint = CampTypography.Label("", TextRole.Lead, UITheme.TextColor, align: HorizontalAlignment.Center);
        _detailHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailHint.CustomMinimumSize = new Vector2(DetailWidth - 80f, 56f);
        content.AddChild(_detailHint);
        content.AddChild(new HSeparator());
        content.AddChild(CampTypography.Label(Tr("UI_CAMP_EFFECT"), TextRole.Small, UITheme.TextDim, align: HorizontalAlignment.Center));
        _detailEffect = CampTypography.Label("", TextRole.Heading, UITheme.CyanEssence, TextWeight.Strong, HorizontalAlignment.Center);
        _detailEffect.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailEffect.CustomMinimumSize = new Vector2(DetailWidth - 80f, 56f);
        content.AddChild(_detailEffect);
        _detailPrice = CampTypography.Label("", TextRole.Heading, UITheme.GoldBright, TextWeight.Strong, HorizontalAlignment.Center);
        content.AddChild(_detailPrice);
        content.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 16);
        content.AddChild(actions);
        _buySelected = MenuSkin.Button(Tr("UI_CAMP_BUY"), true);
        _buySelected.Name = "BuyCampUpgrade";
        CampTypography.Apply(_buySelected, TextRole.Heading, true);
        _buySelected.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _buySelected.Pressed += () => { if (_selected != null) OnBuy(_selected); };
        actions.AddChild(_buySelected);
        _refundSelected = MenuSkin.Button(Tr("UI_CAMP_REFUND"));
        _refundSelected.Name = "RefundCampUpgrade";
        CampTypography.Apply(_refundSelected, TextRole.Heading, true);
        _refundSelected.SizeFlagsHorizontal = SizeFlags.ExpandFill;
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
        _detailIcon.Texture = row.Icon;
        _detailName.Text = Tr(row.Upgrade.NameKey);
        _detailHint.Text = Tr(row.Upgrade.NameKey + "_HINT");
        SetStatus(_defaultStatus, _catalogFailed ? MissingColor : UITheme.TextColor);
        UpdateDetail();
    }

    private static Button MakeButton(string text, Vector2 size)
    {
        Button button = new() { Text = text, CustomMinimumSize = size, FocusMode = FocusModeEnum.All };
        CampTypography.Apply(button, TextRole.Lead, true);
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

    private void OnRefundAll()
    {
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
            row.Rank.Text = string.Format(Tr("UI_CAMP_RANK"), rank, row.Upgrade.MaxRank);
            row.Rank.AddThemeColorOverride("font_color", rank >= row.Upgrade.MaxRank ? MaxedColor : UITheme.TextColor);
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
        CampUpgradeData upgrade = _selected.Upgrade;
        int rank = MetaSaveManager.GetCampRank(upgrade.Id);
        bool maxed = rank >= upgrade.MaxRank;
        _detailRank.Text = string.Format(Tr("UI_CAMP_RANK"), rank, upgrade.MaxRank);
        _detailEffect.Text = EffectText(upgrade, rank);
        _detailPrice.Text = maxed ? Tr("UI_CAMP_MAXED")
            : string.Format(Tr("UI_CAMP_PRICE"), upgrade.PriceOfRank(rank + 1).ToString("N0", PlayerSheet.French));
        _detailPrice.AddThemeColorOverride("font_color", maxed ? MaxedColor
            : upgrade.PriceOfRank(rank + 1) <= MetaSaveManager.GetVestiges() ? UITheme.GoldBright : MissingColor);
        _buySelected.Disabled = maxed;
        _refundSelected.Disabled = rank == 0;
        _buySelected.FocusNeighborLeft = _refundSelected.FocusNeighborLeft = _selected.Select.GetPath();
        _selected.Select.FocusNeighborRight = (maxed ? _refundSelected : _buySelected).GetPath();
    }

    /// <summary>Effet de la ligne au rang acquis, puis au rang suivant : « +8 % → +12 % », « 3 → 4 emplacements ».</summary>
    private string EffectText(CampUpgradeData upgrade, int rank)
    {
        if (upgrade.Effect == CampEffect.WeaponSlots)
        {
            // L'effet d'une run normale, même en mode dev (qui garde quatre emplacements, dit en tête de page).
            int slots = CampUpgrades.Compute(MetaSaveManager.GetCampRanks(), false).WeaponSlots;
            return rank >= upgrade.MaxRank
                ? string.Format(Tr("UI_CAMP_SLOTS"), slots)
                : string.Format(Tr("UI_CAMP_SLOTS_NEXT"), slots, slots + Mathf.RoundToInt(upgrade.Step));
        }
        if (upgrade.Effect == CampEffect.BagSlots)
        {
            int bag = CampUpgrades.Compute(MetaSaveManager.GetCampRanks(), false).BagSlots;
            return rank >= upgrade.MaxRank
                ? string.Format(Tr("UI_CAMP_BAG"), bag)
                : string.Format(Tr("UI_CAMP_BAG_NEXT"), bag, bag + Mathf.RoundToInt(upgrade.Step));
        }
        string now = "+" + FormatAmount(upgrade, upgrade.TotalAt(rank));
        return rank >= upgrade.MaxRank ? now : $"{now} → +{FormatAmount(upgrade, upgrade.TotalAt(rank + 1))}";
    }

    private static string FormatAmount(CampUpgradeData upgrade, float value)
    {
        return upgrade.Percent
            ? (value * 100f).ToString("0.#", PlayerSheet.French) + " %"
            : value.ToString("0.#", PlayerSheet.French);
    }
}
