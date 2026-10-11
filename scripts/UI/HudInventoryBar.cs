using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Inventaire au HUD (plan 35, disposition du plan 37 R3) : les Écussons en grandes cartes au-dessus des armes, cadre
/// doré, niveau et fil d'usage ; les objets du sac en colonne au bord gauche, sans cadre, une ligne par objet avec son
/// nombre d'exemplaires. Rafraîchi sur <see cref="EventBus.PassiveSouvenirSlotsChanged"/>, jamais par image. Sous chaque
/// Écusson, un fil se remplit avec l'usage (plan 35 I7) ; un niveau gagné ainsi fait briller l'Écusson une fois.
/// </summary>
public partial class HudInventoryBar : Control
{
    private const float BadgeSize = 28f;
    private const float BadgeGap = 4f;
    private const float ItemIconSize = 24f;
    private const int ItemsPerColumn = 12;
    private static readonly Color LevelColor = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    // Les Écussons se distinguent des objets par un cadre doré, en attendant leurs icônes brodées (plan 25).
    private static readonly Color BadgeFrameTint = new(1.4f, 1.1f, 0.5f);
    private static readonly Color ThreadBackColor = new(0.04f, 0.06f, 0.09f, 0.8f);
    private static readonly Color ThreadColor = new(0.92f, 0.78f, 0.42f);
    private static readonly Color ShineTint = new(2.2f, 1.9f, 1.2f);
    private static readonly Color CountColor = new(0.83f, 0.8f, 0.74f);
    private const float ThreadHeight = 2f;
    private const float ShineSeconds = 0.5f;

    private sealed class Badge
    {
        public NinePatchRect Frame;
        public TextureRect Icon;
        public Label Level;
        public ColorRect Thread;
        public string BadgeId;
        public Tween Shine;
    }

    private sealed class ItemRow
    {
        public Control Root;
        public TextureRect Icon;
        public Label Copies;
    }

    /// <summary>Bas de la rangée d'Écussons, en unités HUD depuis le bas de l'écran : le haut de la barre d'armes.</summary>
    public float BadgeShelfBottom { get; set; }

    private readonly List<Badge> _badges = new();
    private readonly List<ItemRow> _items = new();
    private GridContainer _bag;
    private Label _bagCount;
    private Texture2D _emptyTexture;
    private Texture2D _filledTexture;
    private EventBus _eventBus;
    private GroupCache _groupCache;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _emptyTexture = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive.png");
        _filledTexture = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive_filled.png");
        BuildBadgeShelf();
        BuildBagColumn();

        _groupCache = GetNodeOrNull<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PassiveSouvenirSlotsChanged += Refresh;
        _eventBus.BadgeThreadChanged += OnBadgeThreadChanged;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.PassiveSouvenirSlotsChanged -= Refresh;
        _eventBus.BadgeThreadChanged -= OnBadgeThreadChanged;
    }

    private void BuildBadgeShelf()
    {
        float width = InventoryConfig.BadgeSlots * (BadgeSize + BadgeGap) - BadgeGap;
        HBoxContainer shelf = new()
        {
            Name = "Badges", MouseFilter = MouseFilterEnum.Ignore,
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 1f, AnchorBottom = 1f,
            OffsetLeft = -width / 2f, OffsetRight = width / 2f,
            OffsetTop = -BadgeShelfBottom - BadgeSize - ThreadHeight - 1f, OffsetBottom = -BadgeShelfBottom,
        };
        shelf.AddThemeConstantOverride("separation", (int)BadgeGap);
        AddChild(shelf);
        for (int i = 0; i < InventoryConfig.BadgeSlots; i++)
            _badges.Add(AddBadge(shelf));
    }

    private Badge AddBadge(HBoxContainer shelf)
    {
        Control root = new() { CustomMinimumSize = new Vector2(BadgeSize, BadgeSize), MouseFilter = MouseFilterEnum.Ignore };
        Badge badge = new()
        {
            Frame = new NinePatchRect
            {
                Texture = _emptyTexture, PatchMarginLeft = 3, PatchMarginRight = 3, PatchMarginTop = 3, PatchMarginBottom = 3,
                TextureFilter = TextureFilterEnum.Nearest,
            },
            Icon = Icon(),
            Level = Text(9, LevelColor),
        };
        badge.Frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddChild(badge.Frame);
        badge.Icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        badge.Icon.OffsetLeft = badge.Icon.OffsetTop = 3f;
        badge.Icon.OffsetRight = badge.Icon.OffsetBottom = -3f;
        root.AddChild(badge.Icon);
        badge.Level.HorizontalAlignment = HorizontalAlignment.Right;
        badge.Level.Position = new Vector2(BadgeSize - 16f, BadgeSize - 13f);
        badge.Level.Size = new Vector2(15f, 12f);
        root.AddChild(badge.Level);
        // Le fil d'usage, juste sous l'Écusson : fond sombre, remplissage doré de gauche à droite.
        root.AddChild(new ColorRect
        {
            Color = ThreadBackColor, Position = new Vector2(1f, BadgeSize + 1f), Size = new Vector2(BadgeSize - 2f, ThreadHeight),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        badge.Thread = new ColorRect
        {
            Color = ThreadColor, Position = new Vector2(1f, BadgeSize + 1f), Size = new Vector2(0f, ThreadHeight),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        root.AddChild(badge.Thread);
        shelf.AddChild(root);
        return badge;
    }

    /// <summary>Colonne du sac au bord gauche, centrée en hauteur : le compte « 7/10 », puis une ligne par objet.</summary>
    private void BuildBagColumn()
    {
        VBoxContainer column = new()
        {
            Name = "Bag", MouseFilter = MouseFilterEnum.Ignore,
            AnchorLeft = 0f, AnchorRight = 0f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = 8f, OffsetRight = 8f, GrowVertical = GrowDirection.Both,
        };
        column.AddThemeConstantOverride("separation", 3);
        AddChild(column);
        _bagCount = Text(8, CountColor);
        column.AddChild(_bagCount);
        _bag = new GridContainer { Columns = 1, MouseFilter = MouseFilterEnum.Ignore };
        _bag.AddThemeConstantOverride("v_separation", 2);
        _bag.AddThemeConstantOverride("h_separation", 6);
        column.AddChild(_bag);
    }

    private ItemRow AddItemRow()
    {
        HBoxContainer root = new() { MouseFilter = MouseFilterEnum.Ignore };
        root.AddThemeConstantOverride("separation", 2);
        ItemRow row = new() { Root = root, Icon = Icon(), Copies = Text(9, CountColor) };
        row.Icon.CustomMinimumSize = new Vector2(ItemIconSize, ItemIconSize);
        root.AddChild(row.Icon);
        row.Copies.CustomMinimumSize = new Vector2(14f, 0f);
        row.Copies.SizeFlagsVertical = SizeFlags.ShrinkEnd;
        root.AddChild(row.Copies);
        _bag.AddChild(root);
        return row;
    }

    private static TextureRect Icon() => new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
        Visible = false,
    };

    private static Label Text(int size, Color color)
    {
        Label label = new() { MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", 3);
        return label;
    }

    private void Refresh()
    {
        if (_groupCache?.GetPlayer() is not Player player)
            return;
        for (int i = 0; i < _badges.Count; i++)
        {
            ActivePassiveSouvenir badge = i < player.BadgeSlots.Count ? player.BadgeSlots[i] : null;
            FillBadge(_badges[i], badge);
            _badges[i].BadgeId = badge?.Id;
            SetThread(_badges[i], badge != null ? player.BadgeUsage?.ProgressOf(badge.Id) ?? 0f : 0f);
        }

        int copies = 0;
        int row = 0;
        foreach (ActivePassiveSouvenir item in player.PassiveSlots)
        {
            if (row == _items.Count)
                _items.Add(AddItemRow());
            FillItem(_items[row++], item);
            copies += item.Copies;
        }
        for (int i = row; i < _items.Count; i++)
            _items[i].Root.Visible = false;
        // Au-delà de douze objets différents, une seconde colonne : le sac des Poches tient sans couvrir le combat.
        _bag.Columns = row > ItemsPerColumn ? 2 : 1;
        _bagCount.Text = $"{copies}/{player.BagCapacity}";
        _bagCount.Visible = row > 0;
    }

    private void FillBadge(Badge slot, ActivePassiveSouvenir owned)
    {
        if (owned == null)
        {
            slot.Frame.Texture = _emptyTexture;
            slot.Frame.Modulate = BadgeFrameTint;
            slot.Icon.Visible = false;
            slot.Level.Text = "";
            return;
        }
        Texture2D icon = LoadIcon(owned);
        slot.Frame.Texture = _filledTexture;
        slot.Frame.Modulate = icon == null ? owned.Data.IconColor : BadgeFrameTint;
        slot.Icon.Texture = icon;
        slot.Icon.Visible = icon != null;
        slot.Level.Text = owned.Level > 1 ? $"{owned.Level}" : "";
    }

    private static void FillItem(ItemRow row, ActivePassiveSouvenir owned)
    {
        row.Root.Visible = true;
        row.Icon.Texture = LoadIcon(owned);
        row.Icon.Visible = row.Icon.Texture != null;
        row.Copies.Text = owned.Copies > 1 ? $"×{owned.Copies}" : "";
    }

    // Grande icône d'abord : les cartes d'Écusson et la colonne du sac l'affichent à 28 et 24 unités, soit 56 et 48 px.
    private static Texture2D LoadIcon(ActivePassiveSouvenir owned)
    {
        string path = !string.IsNullOrEmpty(owned.Data.Icon) ? owned.Data.Icon : owned.Data.IconSmall;
        return string.IsNullOrEmpty(path) ? null : GD.Load<Texture2D>(path);
    }

    private void OnBadgeThreadChanged(string badgeId, float progress, bool leveled)
    {
        foreach (Badge slot in _badges)
        {
            if (slot.BadgeId != badgeId)
                continue;
            SetThread(slot, progress);
            if (leveled)
                Shine(slot);
            return;
        }
    }

    private static void SetThread(Badge slot, float progress) =>
        slot.Thread.Size = new Vector2((BadgeSize - 2f) * Mathf.Clamp(progress, 0f, 1f), ThreadHeight);

    /// <summary>Un niveau gagné par l'usage : l'Écusson brille une fois, puis reprend son cadre doré.</summary>
    private void Shine(Badge slot)
    {
        slot.Shine?.Kill();
        slot.Frame.Modulate = ShineTint;
        slot.Shine = CreateTween();
        slot.Shine.TweenProperty(slot.Frame, "modulate", BadgeFrameTint, ShineSeconds);
    }

    /// <summary>Icône de la ligne du sac qui porte cet objet, ou null : là où atterrit un objet ramassé.</summary>
    public Control ItemIconOf(string itemId)
    {
        if (_groupCache?.GetPlayer() is not Player player)
            return null;
        for (int i = 0; i < player.PassiveSlots.Count && i < _items.Count; i++)
            if (player.PassiveSlots[i].Id == itemId)
                return _items[i].Icon;
        return null;
    }
}
