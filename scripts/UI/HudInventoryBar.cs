using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Rangée sous les armes (plan 35) : les Écussons à gauche, cadre doré, puis les objets. Chaque case montre l'icône et
/// le niveau ; une case vide garde son cadre. Rafraîchie sur <see cref="EventBus.PassiveSouvenirSlotsChanged"/>, jamais
/// par image.
/// </summary>
public partial class HudInventoryBar : HBoxContainer
{
    private const float SlotSize = 18f;
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

    public float Width => (InventoryConfig.BadgeSlots + Player.MaxPassiveSlots) * (SlotSize + 3f) + ShelfGap;

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
        for (int i = 0; i < Player.MaxPassiveSlots; i++)
            _items.Add(AddSlot());

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

    private Slot AddSlot()
    {
        Control root = new() { CustomMinimumSize = new Vector2(SlotSize, SlotSize), MouseFilter = MouseFilterEnum.Ignore };
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
        slot.Icon.OffsetLeft = 2;
        slot.Icon.OffsetTop = 2;
        slot.Icon.OffsetRight = -2;
        slot.Icon.OffsetBottom = -2;
        root.AddChild(slot.Icon);
        slot.Level.AddThemeFontSizeOverride("font_size", 8);
        slot.Level.AddThemeColorOverride("font_color", LevelColor);
        slot.Level.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        slot.Level.AddThemeConstantOverride("outline_size", 3);
        slot.Level.Position = new Vector2(SlotSize - 12, SlotSize - 11);
        slot.Level.Size = new Vector2(12, 10);
        root.AddChild(slot.Level);
        AddChild(root);
        return slot;
    }

    private void Refresh()
    {
        if (_groupCache?.GetPlayer() is not Player player)
            return;
        Fill(_badges, player.BadgeSlots, BadgeFrameTint);
        Fill(_items, player.PassiveSlots, Colors.White);
    }

    private void Fill(List<Slot> slots, IReadOnlyList<ActivePassiveSouvenir> carried, Color emptyTint)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            if (i >= carried.Count)
            {
                slot.Frame.Texture = _emptyTexture;
                slot.Frame.Modulate = emptyTint;
                slot.Icon.Visible = false;
                slot.Level.Text = "";
                continue;
            }
            ActivePassiveSouvenir owned = carried[i];
            string iconPath = owned.Data.IconSmall;
            Texture2D icon = string.IsNullOrEmpty(iconPath) ? null : GD.Load<Texture2D>(iconPath);
            slot.Frame.Texture = _filledTexture;
            slot.Frame.Modulate = icon == null ? owned.Data.IconColor : emptyTint;
            slot.Icon.Texture = icon;
            slot.Icon.Visible = icon != null;
            slot.Level.Text = owned.Level > 1 ? $"{owned.Level}" : "";
        }
    }
}
