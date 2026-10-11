using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Rangée sous les armes (plan 35) : les Écussons à gauche, cadre doré et niveau, puis le sac, une petite case par place
/// et un exemplaire par case (les doublons côte à côte). Rafraîchie sur <see cref="EventBus.PassiveSouvenirSlotsChanged"/>,
/// jamais par image ; elle se recentre quand le sac change de taille (Poches du Campement).
/// </summary>
public partial class HudInventoryBar : HBoxContainer
{
    private const float SlotSize = 18f;
    private const float BagSlotSize = 13f;
    private const float ShelfGap = 10f;
    private static readonly Color LevelColor = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    // Les Écussons se distinguent des objets par un cadre doré, en attendant leurs icônes brodées (plan 25).
    private static readonly Color BadgeFrameTint = new(1.4f, 1.1f, 0.5f);

    private sealed class Slot
    {
        public NinePatchRect Frame;
        public TextureRect Icon;
        public Label Level;
    }

    private readonly List<Slot> _badges = new();
    private readonly List<Slot> _items = new();
    private Texture2D _emptyTexture;
    private Texture2D _filledTexture;
    private EventBus _eventBus;
    private GroupCache _groupCache;

    private float Width => InventoryConfig.BadgeSlots * (SlotSize + 3f) + ShelfGap + _items.Count * (BagSlotSize + 3f);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Alignment = AlignmentMode.Center;
        AddThemeConstantOverride("separation", 3);
        _emptyTexture = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive.png");
        _filledTexture = GD.Load<Texture2D>("res://assets/ui/hud/hud_slot_passive_filled.png");
        for (int i = 0; i < InventoryConfig.BadgeSlots; i++)
            _badges.Add(AddSlot());
        AddChild(new Control { CustomMinimumSize = new Vector2(ShelfGap - 3f, 0f), MouseFilter = MouseFilterEnum.Ignore });
        for (int i = 0; i < InventoryConfig.BagSize; i++)
            _items.Add(AddSlot(BagSlotSize));
        Recenter();

        _groupCache = GetNodeOrNull<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PassiveSouvenirSlotsChanged += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.PassiveSouvenirSlotsChanged -= Refresh;
    }

    private Slot AddSlot(float size = SlotSize)
    {
        Control root = new() { CustomMinimumSize = new Vector2(size, size), MouseFilter = MouseFilterEnum.Ignore };
        Slot slot = new()
        {
            Frame = new NinePatchRect
            {
                Texture = _emptyTexture,
                PatchMarginLeft = 3,
                PatchMarginRight = 3,
                PatchMarginTop = 3,
                PatchMarginBottom = 3,
                TextureFilter = TextureFilterEnum.Nearest,
            },
            Icon = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
                Visible = false,
            },
            Level = new Label { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Right },
        };
        slot.Frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddChild(slot.Frame);
        slot.Icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        int inset = size > BagSlotSize ? 2 : 1;
        slot.Icon.OffsetLeft = inset;
        slot.Icon.OffsetTop = inset;
        slot.Icon.OffsetRight = -inset;
        slot.Icon.OffsetBottom = -inset;
        root.AddChild(slot.Icon);
        slot.Level.AddThemeFontSizeOverride("font_size", 8);
        slot.Level.AddThemeColorOverride("font_color", LevelColor);
        slot.Level.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        slot.Level.AddThemeConstantOverride("outline_size", 3);
        slot.Level.Position = new Vector2(size - 12, size - 11);
        slot.Level.Size = new Vector2(12, 10);
        root.AddChild(slot.Level);
        AddChild(root);
        return slot;
    }

    private void Refresh()
    {
        if (_groupCache?.GetPlayer() is not Player player)
            return;
        for (int i = 0; i < _badges.Count; i++)
            Fill(_badges[i], i < player.BadgeSlots.Count ? player.BadgeSlots[i] : null, BadgeFrameTint, true);
        if (_items.Count != player.BagCapacity)
        {
            while (_items.Count < player.BagCapacity)
                _items.Add(AddSlot(BagSlotSize));
            while (_items.Count > player.BagCapacity)
            {
                _items[^1].Frame.GetParent().QueueFree();
                _items.RemoveAt(_items.Count - 1);
            }
            Recenter();
        }
        int cell = 0;
        foreach (ActivePassiveSouvenir item in player.PassiveSlots)
            for (int copy = 0; copy < item.Copies && cell < _items.Count; copy++)
                Fill(_items[cell++], item, Colors.White, false);
        while (cell < _items.Count)
            Fill(_items[cell++], null, Colors.White, false);
    }

    private void Fill(Slot slot, ActivePassiveSouvenir owned, Color tint, bool showLevel)
    {
        if (owned == null)
        {
            slot.Frame.Texture = _emptyTexture;
            slot.Frame.Modulate = tint;
            slot.Icon.Visible = false;
            slot.Level.Text = "";
            return;
        }
        string iconPath = owned.Data.IconSmall;
        Texture2D icon = string.IsNullOrEmpty(iconPath) ? null : GD.Load<Texture2D>(iconPath);
        slot.Frame.Texture = _filledTexture;
        slot.Frame.Modulate = icon == null ? owned.Data.IconColor : tint;
        slot.Icon.Texture = icon;
        slot.Icon.Visible = icon != null;
        slot.Level.Text = showLevel && owned.Level > 1 ? $"{owned.Level}" : "";
    }

    /// <summary>Centrée sous les armes, quelle que soit la taille du sac.</summary>
    private void Recenter()
    {
        OffsetLeft = -Width / 2f;
        OffsetRight = Width / 2f;
    }
}
