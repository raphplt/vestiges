using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Meta;

namespace Vestiges.UI;

/// <summary>
/// Page Campement de l'accueil (plan 36, lot M2) : les améliorations permanentes achetées en Vestiges, par rayon.
/// Chaque ligne achète le rang suivant (Valider) ; son bouton « Rendre » rembourse le dernier rang, gratuitement. Le
/// solde se met à jour après chaque achat ; le détail de la ligne active dit l'effet actuel et le suivant.
/// </summary>
public partial class HubCampUpgradesPanel : MarginContainer
{
    private const float RowWidth = 600f;
    private const float RowHeight = 58f;
    private const float RefundWidth = 120f;
    private static readonly Color MaxedColor = UITheme.GoldBright;
    private static readonly Color MissingColor = new(0.78f, 0.42f, 0.36f);

    private sealed class Row
    {
        public CampUpgradeData Upgrade;
        public Button Buy;
        public Button Refund;
        public Label Effect;
        public Label Rank;
        public Label Price;
    }

    private readonly List<Row> _rows = new();
    private Label _balance;
    private Label _status;
    private Label _detail;
    private Button _refundAll;
    private VBoxContainer _left;
    private VBoxContainer _right;

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
        _balance = UITheme.MakeLabel("", TextRole.Title, UITheme.GoldBright, TextWeight.Strong, outline: 6);
        top.AddChild(_balance);
        _status = UITheme.MakeLabel("", TextRole.Lead, UITheme.TextColor, outline: 4);
        _status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _status.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _status.HorizontalAlignment = HorizontalAlignment.Right;
        top.AddChild(_status);
        _refundAll = MakeButton(Tr("UI_CAMP_REFUND_ALL"), new Vector2(200f, 44f));
        _refundAll.Pressed += OnRefundAll;
        top.AddChild(_refundAll);

        column.AddChild(new ColorRect
        {
            Color = new Color(UITheme.GoldDim, 0.45f),
            CustomMinimumSize = new Vector2(0f, 4f),
            MouseFilter = MouseFilterEnum.Ignore,
        });

        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 56);
        column.AddChild(body);
        _left = new VBoxContainer();
        _left.AddThemeConstantOverride("separation", 8);
        body.AddChild(_left);
        _right = new VBoxContainer();
        _right.AddThemeConstantOverride("separation", 8);
        body.AddChild(_right);

        _detail = UITheme.MakeLabel("", TextRole.Heading, UITheme.TextLight, outline: 5);
        _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_detail);
    }

    /// <summary>Reconstruit les rayons ; renvoie la première ligne, qui prend le focus.</summary>
    public Control Refresh()
    {
        // Retirés tout de suite : l'espaceur entre rayons se décide sur les enfants restants.
        foreach (VBoxContainer columnBox in new[] { _left, _right })
        {
            foreach (Node child in columnBox.GetChildren())
            {
                columnBox.RemoveChild(child);
                child.QueueFree();
            }
        }
        _rows.Clear();

        // Capacités et Confort à gauche, les neuf stats à droite.
        AddBranch(_left, CampBranch.Capacities, "UI_CAMP_BRANCH_CAPACITIES");
        AddBranch(_left, CampBranch.Comfort, "UI_CAMP_BRANCH_COMFORT");
        AddBranch(_right, CampBranch.Stats, "UI_CAMP_BRANCH_STATS");
        if (!CampUpgradeDataLoader.TryLoad(out string catalogError))
            SetStatus(catalogError, MissingColor);
        else
            SetStatus(DevelopmentMode.IsEnabled ? Tr("UI_CAMP_DEV_SLOTS") : "", UITheme.TextColor);
        UpdateAll();
        if (_rows.Count == 0)
            return _refundAll;
        ShowDetail(_rows[0]);
        return _rows[0].Buy;
    }

    private void AddBranch(VBoxContainer parent, CampBranch branch, string titleKey)
    {
        bool any = false;
        foreach (CampUpgradeData upgrade in CampUpgradeDataLoader.All)
        {
            if (upgrade.Branch != branch)
                continue;
            if (!any)
            {
                Label title = UITheme.MakeLabel(Tr(titleKey), TextRole.Title, UITheme.GoldColor, TextWeight.Strong, outline: 6);
                if (parent.GetChildCount() > 0)
                    parent.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 12f), MouseFilter = MouseFilterEnum.Ignore });
                parent.AddChild(title);
                any = true;
            }
            parent.AddChild(MakeRow(upgrade));
        }
    }

    private HBoxContainer MakeRow(CampUpgradeData upgrade)
    {
        Row row = new() { Upgrade = upgrade };
        HBoxContainer line = new();
        line.AddThemeConstantOverride("separation", 8);

        row.Buy = MakeButton("", new Vector2(RowWidth, RowHeight));
        line.AddChild(row.Buy);
        Label name = UITheme.MakeLabel(Tr(upgrade.NameKey), TextRole.Heading, UITheme.TextLight, TextWeight.Strong, outline: 4);
        name.Position = new Vector2(16f, 3f);
        row.Buy.AddChild(name);
        row.Effect = UITheme.MakeLabel("", TextRole.Body, UITheme.TextColor, outline: 3);
        row.Effect.Position = new Vector2(16f, 31f);
        row.Buy.AddChild(row.Effect);
        row.Rank = UITheme.MakeLabel("", TextRole.Lead, UITheme.TextColor, outline: 3);
        row.Rank.Position = new Vector2(330f, 16f);
        row.Buy.AddChild(row.Rank);
        row.Price = UITheme.MakeLabel("", TextRole.Heading, UITheme.GoldBright, TextWeight.Strong, HorizontalAlignment.Right, 4);
        row.Price.Position = new Vector2(420f, 12f);
        row.Price.Size = new Vector2(RowWidth - 436f, 32f);
        row.Buy.AddChild(row.Price);

        row.Refund = MakeButton(Tr("UI_CAMP_REFUND"), new Vector2(RefundWidth, RowHeight));
        line.AddChild(row.Refund);

        row.Buy.Pressed += () => OnBuy(row);
        row.Refund.Pressed += () => OnRefund(row);
        row.Buy.FocusEntered += () => ShowDetail(row);
        row.Buy.MouseEntered += () => ShowDetail(row);
        row.Refund.FocusEntered += () => ShowDetail(row);
        row.Refund.MouseEntered += () => ShowDetail(row);
        _rows.Add(row);
        return line;
    }

    private static Button MakeButton(string text, Vector2 size)
    {
        Button button = new() { Text = text, CustomMinimumSize = size, FocusMode = FocusModeEnum.All };
        button.AddThemeFontOverride("font", UITheme.StrongFont);
        UITheme.SetTextRole(button, TextRole.Lead);
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
        AfterTransaction(_rows.Count > 0 ? _rows[0] : null, saved);
    }

    private void AfterTransaction(Row row, SaveFile.WriteResult saved)
    {
        if (!saved.Succeeded)
            SetStatus(string.Format(Tr("UI_CAMP_SAVE_FAILED"), saved.Error), MissingColor);
        UpdateAll();
        if (row == null)
            return;
        ShowDetail(row);
        // Un bouton « Rendre » éteint par la transaction ne garde pas le focus : la ligne le reprend.
        if (GetViewport().GuiGetFocusOwner() is Button { Disabled: true })
            row.Buy.GrabFocus();
    }

    private void SetStatus(string text, Color color)
    {
        _status.Text = text;
        _status.AddThemeColorOverride("font_color", color);
    }

    private void UpdateAll()
    {
        int vestiges = MetaSaveManager.GetVestiges();
        _balance.Text = vestiges.ToString("N0", PlayerSheet.French);
        bool anyBought = false;
        foreach (Row row in _rows)
        {
            int rank = MetaSaveManager.GetCampRank(row.Upgrade.Id);
            bool maxed = rank >= row.Upgrade.MaxRank;
            anyBought |= rank > 0;
            row.Rank.Text = string.Format(Tr("UI_CAMP_RANK"), rank, row.Upgrade.MaxRank);
            row.Rank.AddThemeColorOverride("font_color", maxed ? MaxedColor : UITheme.TextColor);
            row.Effect.Text = EffectText(row.Upgrade, rank);
            if (maxed)
            {
                row.Price.Text = Tr("UI_CAMP_MAXED");
                row.Price.AddThemeColorOverride("font_color", MaxedColor);
            }
            else
            {
                int price = row.Upgrade.PriceOfRank(rank + 1);
                row.Price.Text = price.ToString("N0", PlayerSheet.French);
                row.Price.AddThemeColorOverride("font_color", price <= vestiges ? UITheme.GoldBright : UITheme.TextVeryDim);
            }
            row.Refund.Disabled = rank == 0;
        }
        _refundAll.Disabled = !anyBought;
    }

    private void ShowDetail(Row row)
    {
        int rank = MetaSaveManager.GetCampRank(row.Upgrade.Id);
        string step = string.Format(Tr(row.Upgrade.DescKey), FormatAmount(row.Upgrade, row.Upgrade.Step));
        _detail.Text = rank >= row.Upgrade.MaxRank
            ? $"{Tr(row.Upgrade.NameKey)} — {step}. {Tr("UI_CAMP_MAX")}"
            : $"{Tr(row.Upgrade.NameKey)} — {step}. "
                + string.Format(Tr("UI_CAMP_NEXT_PRICE"), row.Upgrade.PriceOfRank(rank + 1).ToString("N0", PlayerSheet.French));
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
