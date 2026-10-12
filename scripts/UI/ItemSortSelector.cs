using Godot;

namespace Vestiges.UI;

/// <summary>Sélecteur de tri natif, utilisable au clavier et à la manette, synchronisé avec les autres vues.</summary>
public partial class ItemSortSelector : OptionButton
{
    public TextRole Role { get; set; } = TextRole.Body;

    public override void _Ready()
    {
        Name = "ItemSort";
        FitToLongestItem = false;
        if (CustomMinimumSize.X == 0)
            CustomMinimumSize = new Vector2(420, 48);
        MenuSkin.StyleButton(this);
        UITheme.SetTextRole(this, Role);
        AccessibilityName = Tr("ITEM_SORT_LABEL");
        TooltipText = Tr("ITEM_SORT_LABEL");
        AddItem(Tr("ITEM_SORT_RARITY_ASC"), (int)ItemSortMode.RarityAscending);
        AddItem(Tr("ITEM_SORT_RARITY_DESC"), (int)ItemSortMode.RarityDescending);
        AddItem(Tr("ITEM_SORT_NAME"), (int)ItemSortMode.Name);
        AddItem(Tr("ITEM_SORT_ORIGINAL"), (int)ItemSortMode.Original);
        PopupMenu popup = GetPopup();
        popup.AddThemeFontOverride("font", UITheme.BodyFont);
        popup.AddThemeFontSizeOverride("font_size", UITheme.FontSize(Role));
        popup.AddThemeStyleboxOverride("panel", MenuSkin.Choice(UITheme.GoldDim));
        popup.AddThemeColorOverride("font_color", UITheme.TextLight);
        popup.AddThemeColorOverride("font_hover_color", UITheme.GoldBright);
        UITheme.WireButtonAudio(this);
        ItemSelected += index => ItemSort.Select((ItemSortMode)GetItemId((int)index));
        ItemSort.Changed += Sync;
        Sync();
    }

    private void Sync() => Select((int)ItemSort.Mode);

    public override void _ExitTree() => ItemSort.Changed -= Sync;
}
