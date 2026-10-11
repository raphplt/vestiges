using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Rangée sous les armes (plan 35) : les Écussons à gauche, cadre doré et niveau, puis le sac, une petite case par place
/// et un exemplaire par case (les doublons côte à côte). Rafraîchie sur <see cref="EventBus.PassiveSouvenirSlotsChanged"/>,
/// jamais par image ; elle se recentre quand le sac change de taille (Poches du Campement). Sous chaque Écusson, un fil se
/// remplit avec l'usage (plan 35 I7) ; un niveau gagné ainsi fait briller l'Écusson une fois.
/// </summary>
public partial class HudInventoryBar : HBoxContainer
{
    private const float SlotSize = 18f;
    private const float BagSlotSize = 13f;
    private const float ShelfGap = 10f;
    private static readonly Color LevelColor = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    // Les Écussons se distinguent des objets par un cadre doré, en attendant leurs icônes brodées (plan 25).
    private static readonly Color BadgeFrameTint = new(1.4f, 1.1f, 0.5f);
    private static readonly Color ThreadBackColor = new(0.04f, 0.06f, 0.09f, 0.8f);
    private static readonly Color ThreadColor = new(0.92f, 0.78f, 0.42f);
    private static readonly Color ShineTint = new(2.2f, 1.9f, 1.2f);
    private const float ThreadHeight = 2f;
    private const float ShineSeconds = 0.5f;

    private sealed class Slot
    {
        public NinePatchRect Frame;
        public TextureRect Icon;
        public Label Level;
        public ColorRect Thread;
        public string BadgeId;
        public float Width;
        public Tween Shine;
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
            _badges.Add(AddSlot(SlotSize, true));
        AddChild(new Control { CustomMinimumSize = new Vector2(ShelfGap - 3f, 0f), MouseFilter = MouseFilterEnum.Ignore });
        for (int i = 0; i < InventoryConfig.BagSize; i++)
            _items.Add(AddSlot(BagSlotSize));
        Recenter();

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

    private Slot AddSlot(float size = SlotSize, bool threaded = false)
    {
        Control root = new() { CustomMinimumSize = new Vector2(size, size), MouseFilter = MouseFilterEnum.Ignore };
        Slot slot = new()
        {
            Width = size,
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
        if (threaded)
        {
            // Le fil d'usage, juste sous l'Écusson : fond sombre, remplissage doré de gauche à droite.
            root.AddChild(new ColorRect
            {
                Color = ThreadBackColor,
                Position = new Vector2(1f, size + 1f),
                Size = new Vector2(size - 2f, ThreadHeight),
                MouseFilter = MouseFilterEnum.Ignore,
            });
            slot.Thread = new ColorRect
            {
                Color = ThreadColor,
                Position = new Vector2(1f, size + 1f),
                Size = new Vector2(0f, ThreadHeight),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            root.AddChild(slot.Thread);
        }
        AddChild(root);
        return slot;
    }

    private void Refresh()
    {
        if (_groupCache?.GetPlayer() is not Player player)
            return;
        for (int i = 0; i < _badges.Count; i++)
        {
            ActivePassiveSouvenir badge = i < player.BadgeSlots.Count ? player.BadgeSlots[i] : null;
            Fill(_badges[i], badge, BadgeFrameTint, true);
            _badges[i].BadgeId = badge?.Id;
            SetThread(_badges[i], badge != null ? player.BadgeUsage?.ProgressOf(badge.Id) ?? 0f : 0f);
        }
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

    private void OnBadgeThreadChanged(string badgeId, float progress, bool leveled)
    {
        foreach (Slot slot in _badges)
        {
            if (slot.BadgeId != badgeId)
                continue;
            SetThread(slot, progress);
            if (leveled)
                Shine(slot);
            return;
        }
    }

    private static void SetThread(Slot slot, float progress)
    {
        float width = slot.Width - 2f;
        slot.Thread.Size = new Vector2(width * Mathf.Clamp(progress, 0f, 1f), ThreadHeight);
    }

    /// <summary>Un niveau gagné par l'usage : l'Écusson brille une fois, puis reprend son cadre doré.</summary>
    private void Shine(Slot slot)
    {
        slot.Shine?.Kill();
        slot.Frame.Modulate = ShineTint;
        slot.Shine = CreateTween();
        slot.Shine.TweenProperty(slot.Frame, "modulate", BadgeFrameTint, ShineSeconds);
    }

    /// <summary>Icône de la dernière case du sac qui porte cet objet, ou null : là où atterrit un objet ramassé.</summary>
    public Control ItemIconOf(string itemId)
    {
        if (_groupCache?.GetPlayer() is not Player player)
            return null;
        int cell = 0;
        foreach (ActivePassiveSouvenir item in player.PassiveSlots)
        {
            cell += item.Copies;
            if (item.Id == itemId)
                return cell - 1 < _items.Count ? _items[cell - 1].Icon : null;
        }
        return null;
    }

    /// <summary>Centrée sous les armes, quelle que soit la taille du sac.</summary>
    private void Recenter()
    {
        OffsetLeft = -Width / 2f;
        OffsetRight = Width / 2f;
    }
}
