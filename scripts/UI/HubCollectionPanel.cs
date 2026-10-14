using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Collection de l'accueil (plan 04 lot C2, première passe) : armes et souvenirs de run en grille d'icônes. Une case
/// verrouillée n'est qu'une silhouette sombre ; le panneau de droite, seul à porter du texte, donne pour la case
/// survolée ou sélectionnée son nom, son état, son effet, ses valeurs et, si elle est verrouillée, la quête qui l'ouvre.
/// La disponibilité suit la même règle que le loot (<see cref="MetaSaveManager.IsUnlocked"/>). L'onglet choisi est
/// conservé d'une ouverture à l'autre.
/// </summary>
public partial class HubCollectionPanel : MarginContainer
{
    [Signal] public delegate void QuestRequestedEventHandler(string questId);
    public string ExaminedEntryId { get; private set; }
    public string CurrentTab => _tab;
    private readonly Dictionary<string, string> _examined = new();
    // Pièces nouvelles déjà regardées pendant cette ouverture : leur badge et leur mention restent jusqu'à la suivante.
    private readonly HashSet<string> _seenNow = new();
    private Button _questLink;
    private string _questId;
    private const int Columns = 6;
    private const float TileSize = 112f;
    private const float IconSize = 64f;
    private static readonly Color LockedTint = new(0.48f, 0.45f, 0.5f, 1f);
    private static readonly Color NewBadgeText = new(0.12f, 0.08f, 0.04f);

    private static string _tab = "weapons";

    private Font _bodyFont;
    private Font _strongFont;
    private HBoxContainer _tabs;
    private Button _activeTab;
    private GridContainer _grid;
    private HBoxContainer _sortRow;
    private ItemSortSelector _itemSort;
    private TextureRect _detailIcon;
    private PanelContainer _detailIconFrame;
    private Label _detailRarity;
    private Label _detailName;
    private Label _detailState;
    private Label _detailText;
    private Label _detailStats;
    private Label _detailCondition;
    private Label _count;

    private readonly struct Entry
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Icon;
        public readonly string Description;
        public readonly string Stats;
        public readonly bool Unlocked;
        public readonly bool Preview;
        public readonly string Condition;

        public Entry(string id, string name, string icon, string description, string stats, bool unlocked, string condition, bool preview = false)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Description = description;
            Stats = stats;
            Unlocked = unlocked;
            Preview = preview;
            Condition = condition;
        }
    }

    public override void _Ready()
    {
        _bodyFont = UITheme.BodyFont;
        _strongFont = UITheme.StrongFont;

        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 14);
        AddChild(column);

        _tabs = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _tabs.AddThemeConstantOverride("separation", 40);
        column.AddChild(_tabs);
        column.AddChild(new ColorRect { Color = new Color(UITheme.GoldDim, 0.45f), CustomMinimumSize = new Vector2(0f, 4f) });

        _sortRow = new HBoxContainer { Visible = false };
        _sortRow.AddThemeConstantOverride("separation", 16);
        _sortRow.AddChild(UITheme.MakeLabel(Tr("ITEM_SORT_LABEL"), TextRole.Body, UITheme.TextDim));
        _itemSort = new ItemSortSelector();
        _sortRow.AddChild(_itemSort);
        column.AddChild(_sortRow);
        ItemSort.Changed += OnItemSortChanged;

        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 40);
        column.AddChild(body);

        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(Columns * (TileSize + 12f) + 12f, 0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        body.AddChild(scroll);
        _grid = new GridContainer { Columns = Columns };
        _grid.AddThemeConstantOverride("h_separation", 12);
        _grid.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(_grid);

        VBoxContainer detail = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        detail.AddThemeConstantOverride("separation", 10);
        PanelContainer detailPanel = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        detailPanel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        body.AddChild(detailPanel);
        ScrollContainer detailScroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        detailPanel.AddChild(detailScroll);
        detailScroll.AddChild(detail);
        _detailIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(192f, 192f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        _detailIconFrame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkBegin, TextureFilter = TextureFilterEnum.Nearest };
        _detailIconFrame.AddChild(_detailIcon);
        detail.AddChild(_detailIconFrame);
        _detailName = AddDetailLabel(detail, UITheme.TitleFont, TextRole.Title, UITheme.TextLight);
        _detailRarity = AddDetailLabel(detail, _strongFont, TextRole.Heading, UITheme.TextLight);
        _detailState = AddDetailLabel(detail, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _detailText = AddDetailLabel(detail, _bodyFont, TextRole.Heading, UITheme.TextColor);
        // Règles et chiffres toujours affichés : souvent une ligne, jamais derrière un bouton (DECISIONS §117).
        _detailStats = AddDetailLabel(detail, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _detailCondition = AddDetailLabel(detail, _strongFont, TextRole.Heading, UITheme.CyanEssence);
        _questLink = MenuSkin.Button("Voir la quête  →");
        _questLink.Pressed += () => { if (_questId != null) EmitSignal(SignalName.QuestRequested, _questId); };
        detail.AddChild(_questLink);
        _count = AddDetailLabel(detail, _bodyFont, TextRole.Subhead, UITheme.TextDim);
    }

    private static Label AddDetailLabel(VBoxContainer parent, Font font, TextRole role, Color color)
    {
        Label label = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(420f, 0f) };
        label.AddThemeFontOverride("font", font);
        UITheme.SetTextRole(label, role);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.05f, 0.85f));
        parent.AddChild(label);
        return label;
    }

    /// <summary>
    /// Reconstruit onglets et grille à l'ouverture ; renvoie la case qui prend le focus : l'arme
    /// <paramref name="focusWeaponId"/> si elle est donnée (arrivée depuis le bilan), sinon la première.
    /// </summary>
    public Control Refresh(string focusWeaponId = null, string kind = null)
    {
        if (!string.IsNullOrEmpty(focusWeaponId) && kind != null)
            _tab = kind;
        _seenNow.Clear();
        MenuSkin.Clear(_tabs);
        Button[] tabs =
        {
            AddTab("weapons", "Armes"), AddTab("badges", Tr("INVENTORY_BADGES")), AddTab("passives", "Objets"),
            AddTab("perks", "Réminiscences"),
        };
        foreach (Button tab in tabs)
            if (MetaSaveManager.HasNewUnlocks(UnlockKindOf(tab.Name)))
                tab.AddChild(NewMarker(tab));
        // Liens explicites : la recherche géométrique de Godot plongeait dans la grille au lieu de l'onglet voisin.
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].FocusNeighborLeft = tabs[Mathf.Max(0, i - 1)].GetPath();
            tabs[i].FocusNeighborRight = tabs[Mathf.Min(tabs.Length - 1, i + 1)].GetPath();
        }
        return RebuildGrid(focusWeaponId ?? _examined.GetValueOrDefault(_tab));
    }

    private Button AddTab(string id, string text)
    {
        Button tab = new() { Name = id, Text = text, Flat = true, FocusMode = FocusModeEnum.All };
        bool active = _tab == id;
        StyleBoxFlat underline = new()
        {
            BgColor = new Color(0f, 0f, 0f, 0f),
            BorderColor = UITheme.GoldColor,
            BorderWidthBottom = active ? 4 : 0,
            ContentMarginLeft = 8f,
            ContentMarginRight = 8f,
            ContentMarginBottom = 6f,
        };
        foreach (string state in new[] { "normal", "pressed", "hover", "focus", "hover_pressed" })
            tab.AddThemeStyleboxOverride(state, underline);
        tab.AddThemeFontOverride("font", UITheme.TitleFont);
        UITheme.SetTextRole(tab, TextRole.Title);
        tab.AddThemeColorOverride("font_color", active ? UITheme.GoldBright : UITheme.TextDim);
        tab.AddThemeColorOverride("font_hover_color", UITheme.GoldBright);
        tab.AddThemeColorOverride("font_focus_color", UITheme.GoldBright);
        UITheme.WireButtonAudio(tab);
        tab.Pressed += () =>
        {
            if (_tab == id)
                return;
            _tab = id;
            Control first = Refresh();
            first?.GrabFocus();
        };
        _tabs.AddChild(tab);
        if (active)
            _activeTab = tab;
        return tab;
    }

    private Control RebuildGrid(string focusId = null)
    {
        MenuSkin.Clear(_grid);
        _sortRow.Visible = _tab == "passives";
        List<Entry> entries = _tab switch
        {
            "weapons" => WeaponEntries(),
            "perks" => PerkEntries(),
            "badges" => PassiveEntries(true),
            _ => PassiveEntries(false),
        };
        if (_tab == "passives")
            entries = ItemSort.Order(entries, entry => PassiveSouvenirDataLoader.Get(entry.Id)?.Rarity,
                entry => entry.Name, entry => entry.Unlocked);
        int unlocked = 0;
        int index = 0;
        Control first = null;
        Control focused = null;
        Entry focusedEntry = default;
        foreach (Entry entry in entries)
        {
            if (entry.Unlocked)
                unlocked++;
            Control tile = MakeTile(entry);
            _grid.AddChild(tile);
            // Manette : du premier rang, « haut » remonte aux onglets ; des onglets, « bas » redescend.
            if (index++ < Columns && _activeTab != null)
                tile.FocusNeighborTop = (_sortRow.Visible ? (Control)_itemSort : _activeTab).GetPath();
            first ??= tile;
            if (focused == null && entry.Id == focusId)
            {
                focused = tile;
                focusedEntry = entry;
            }
        }
        if (first != null)
        {
            foreach (Node tab in _tabs.GetChildren())
            {
                if (!tab.IsQueuedForDeletion())
                    ((Control)tab).FocusNeighborBottom = _sortRow.Visible ? _itemSort.GetPath() : first.GetPath();
            }
            _itemSort.FocusNeighborTop = _activeTab.GetPath();
            _itemSort.FocusNeighborBottom = (focused ?? first).GetPath();
        }
        _count.Text = $"{unlocked} / {entries.Count} disponibles";
        if (focused != null)
        {
            ShowDetail(focusedEntry);
            return focused;
        }
        if (entries.Count > 0)
            ShowDetail(entries[0]);
        return first;
    }

    private void OnItemSortChanged()
    {
        if (IsVisibleInTree() && _tab == "passives")
            RebuildGrid(ExaminedEntryId);
    }

    public override void _ExitTree() => ItemSort.Changed -= OnItemSortChanged;

    private Button MakeTile(Entry entry)
    {
        Button tile = new() { Name = entry.Id, CustomMinimumSize = new Vector2(TileSize, TileSize), FocusMode = FocusModeEnum.All };
        tile.TextureFilter = TextureFilterEnum.Nearest;
        string skin = entry.Unlocked || entry.Preview ? "ui_card_normal.png" : "ui_card_locked.png";
        StyleBoxTexture normal = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + skin), 4, 4, 4, 4);
        StyleBoxTexture lit = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + "ui_card_selected.png"), 4, 4, 4, 4);
        tile.AddThemeStyleboxOverride("normal", normal);
        tile.AddThemeStyleboxOverride("pressed", normal);
        tile.AddThemeStyleboxOverride("hover", lit);
        tile.AddThemeStyleboxOverride("focus", lit);
        tile.AddThemeStyleboxOverride("hover_pressed", lit);
        if (!string.IsNullOrEmpty(entry.Icon) && ResourceLoader.Exists(entry.Icon))
        {
            TextureRect icon = new()
            {
                Texture = GD.Load<Texture2D>(entry.Icon),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
                Position = new Vector2((TileSize - IconSize) / 2f, (TileSize - IconSize) / 2f),
                Size = new Vector2(IconSize, IconSize),
                // Verrouillée : silhouette sombre, sans badge.
                SelfModulate = entry.Unlocked || entry.Preview ? Colors.White : LockedTint,
            };
            tile.AddChild(icon);
        }
        PassiveSouvenirData item = _tab == "passives" ? PassiveSouvenirDataLoader.Get(entry.Id) : null;
        if (item != null && !item.IsBadge)
        {
            MenuSkin.StyleItemButton(tile, item.Rarity, !entry.Unlocked);
            TextureRect rarity = new() { Texture = RarityArt.Icons(RarityArt.Rank(item.Rarity), 12)[0],
                Position = new Vector2(6, 6), Size = new Vector2(12, 12), MouseFilter = MouseFilterEnum.Ignore };
            tile.AddChild(rarity);
            tile.TooltipText = entry.Name + " · " + ItemPresentation.RarityText(item);
        }
        // Débloquée par une quête et pas encore regardée : le badge reste jusqu'à la prochaine ouverture (DECISIONS §154).
        if (entry.Unlocked && (_seenNow.Contains($"{_tab}:{entry.Id}") || MetaSaveManager.IsNewUnlock(TabUnlockKind(), entry.Id)))
            tile.AddChild(NewBadge());
        tile.FocusEntered += () => ShowDetail(entry);
        tile.MouseEntered += () => ShowDetail(entry);
        tile.Pressed += () => { ShowDetail(entry); if (_questLink.Visible) _questLink.GrabFocus(); };
        UITheme.WireButtonAudio(tile);
        return tile;
    }

    private UnlockKind TabUnlockKind() => UnlockKindOf(_tab);

    private static UnlockKind UnlockKindOf(string tab) => tab switch
    {
        "weapons" => UnlockKind.Weapon,
        "badges" => UnlockKind.Badge,
        "perks" => UnlockKind.Reminiscence,
        _ => UnlockKind.Object,
    };

    /// <summary>Badge « Nouveau » posé en haut à droite d'une case.</summary>
    private static Control NewBadge()
    {
        PanelContainer badge = new() { MouseFilter = MouseFilterEnum.Ignore };
        badge.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = UITheme.GoldBright, ContentMarginLeft = 5f, ContentMarginRight = 5f, ContentMarginTop = 1f, ContentMarginBottom = 1f,
        });
        badge.AddChild(UITheme.MakeLabel("Nouveau", TextRole.Small, NewBadgeText));
        badge.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight, LayoutPresetMode.KeepSize, 4);
        badge.GrowHorizontal = GrowDirection.Begin;
        return badge;
    }

    /// <summary>Point d'or sur un onglet qui contient une pièce nouvelle.</summary>
    private static Control NewMarker(Button tab)
    {
        ColorRect marker = new() { Color = UITheme.GoldBright, CustomMinimumSize = new Vector2(10f, 10f), Size = new Vector2(10f, 10f),
            MouseFilter = MouseFilterEnum.Ignore };
        marker.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight, LayoutPresetMode.KeepSize);
        return marker;
    }

    private void ShowDetail(Entry entry)
    {
        ExaminedEntryId = entry.Id;
        bool isNew = entry.Unlocked && (_seenNow.Contains($"{_tab}:{entry.Id}") || MetaSaveManager.IsNewUnlock(TabUnlockKind(), entry.Id));
        if (isNew && MetaSaveManager.MarkUnlockSeen(TabUnlockKind(), entry.Id))
            _seenNow.Add($"{_tab}:{entry.Id}");
        _examined[_tab] = entry.Id;
        QuestDefinition quest = entry.Preview ? null : QuestDataLoader.FindUnlocking(TabUnlockKind(), entry.Id);
        _questId = quest?.Id;
        _questLink.Visible = !entry.Unlocked && quest != null;
        _detailStats.Visible = entry.Stats.Length > 0;
        _detailIcon.Texture = !string.IsNullOrEmpty(entry.Icon) && ResourceLoader.Exists(entry.Icon) ? GD.Load<Texture2D>(entry.Icon) : null;
        _detailIcon.SelfModulate = entry.Unlocked || entry.Preview ? Colors.White : LockedTint;
        _detailName.Text = entry.Name;
        PassiveSouvenirData item = _tab == "passives" ? PassiveSouvenirDataLoader.Get(entry.Id) : null;
        bool hasRarity = item != null && !item.IsBadge;
        _detailIconFrame.AddThemeStyleboxOverride("panel", hasRarity
            ? MenuSkin.ItemCard(item.Rarity, locked: !entry.Unlocked) : new StyleBoxEmpty());
        _detailRarity.Visible = hasRarity;
        _detailRarity.Text = hasRarity ? ItemPresentation.RarityText(item) : "";
        _detailRarity.AddThemeColorOverride("font_color", hasRarity ? ItemPresentation.Color(item) : UITheme.TextLight);
        _detailName.AddThemeColorOverride("font_color", item != null && !item.IsBadge ? ItemPresentation.Color(item) : UITheme.TextLight);
        _detailState.Text = entry.Unlocked ? (isNew ? "Nouveau · disponible en run" : "Disponible en run")
            : entry.Preview ? "À venir" : "Pas encore disponible";
        _detailText.Text = entry.Description;
        _detailStats.Text = entry.Stats;
        _detailCondition.Text = entry.Unlocked ? "" : entry.Condition;
    }

    private static List<Entry> WeaponEntries()
    {
        List<Entry> entries = new();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            bool unlocked = MetaSaveManager.IsWeaponUnlocked(weapon.Id);
            string condition = unlocked ? "" : QuestBook.LockText(UnlockKind.Weapon, weapon.Id);
            entries.Add(new Entry(weapon.Id, weapon.Name, weapon.Sprite, weapon.Description, WeaponStats(weapon), unlocked, condition));
        }
        // Disponibles d'abord : la grille se lit comme « ce que j'ai », puis « ce qui reste à trouver ».
        // Tri stable : l'ordre du catalogue reste celui des fiches entre cartes de même état.
        return entries.OrderByDescending(entry => entry.Unlocked).ToList();
    }

    private static string WeaponStats(WeaponData weapon)
    {
        string pattern = TranslationServer.Translate(WeaponGrammar.LabelKey(weapon.AttackPattern));
        string kind = TranslationServer.Translate(WeaponGrammar.LabelKey(weapon.Category));
        weapon.Stats.TryGetValue("damage", out float damage);
        weapon.Stats.TryGetValue("attack_speed", out float speed);
        weapon.Stats.TryGetValue("range", out float range);
        return $"{kind} · {pattern} · {damage:0} dég. · {speed:0.0}/s · portée {range:0}";
    }

    /// <summary>Écussons ou objets du sac (plan 35), chacun avec sa règle de montée.</summary>
    private static List<Entry> PassiveEntries(bool badges)
    {
        List<Entry> entries = new();
        foreach (PassiveSouvenirData passive in PassiveSouvenirDataLoader.GetAll())
        {
            if (passive.IsBadge != badges)
                continue;
            string stats;
            if (passive.IsBadge)
                stats = string.Format(TranslationServer.Translate("COLLECTION_BADGE_RULE"), passive.Subtitle,
                    passive.MaxLevel);
            else
                stats = (passive.HasCopyLimit
                    ? string.Format(TranslationServer.Translate("COLLECTION_ITEM_RULE_PLAIN"), passive.MaxCopies)
                    : TranslationServer.Translate("COLLECTION_ITEM_UNLIMITED").ToString());
            bool unlocked = MetaSaveManager.IsObjectUnlocked(passive.Id);
            string condition = unlocked ? "" : QuestBook.LockText(passive.UnlockKind, passive.Id);
            if (!unlocked && condition.Length == 0 && !passive.InitialUnlock)
                condition = "Déblocage à venir.";
            entries.Add(new Entry(passive.Id, passive.Name, passive.Icon, ItemPresentation.CollectionText(passive), stats, unlocked, condition));
        }
        if (badges)
            return entries.OrderByDescending(entry => entry.Unlocked).ToList();
        foreach (CollectionArtDataLoader.Entry art in CollectionArtDataLoader.Items)
            if (art.World)
                entries.Add(new Entry(art.Id, art.Name, art.Icon, "Objet du monde", "", false,
                    "Cet objet attend le Reliquaire.", true));
        return entries;
    }

    private static List<Entry> PerkEntries()
    {
        List<Entry> entries = new();
        foreach (CollectionArtDataLoader.Entry art in CollectionArtDataLoader.Perks)
        {
            PerkSpecializationData data = PerkSpecializationDataLoader.Get(art.Id);
            bool active = data != null && PerkSpecializationEffects.IsImplemented(data.Effect);
            bool unlocked = active && MetaSaveManager.IsReminiscenceUnlocked(art.Id);
            // Une Réminiscence à venir garde sa mention : sa quête est prête mais n'est pas encore ouverte (§117).
            string condition = !active ? "Cette Réminiscence n'est pas encore proposée en run."
                : unlocked ? "" : QuestBook.LockText(UnlockKind.Reminiscence, art.Id);
            entries.Add(new Entry(art.Id, art.Name, data?.Icon ?? art.Icon, data?.Description ?? "Réminiscence",
                "", unlocked, condition, !active));
        }
        // Tri stable : l'ordre du catalogue reste celui des fiches entre cartes de même état.
        return entries.OrderByDescending(entry => entry.Unlocked).ToList();
    }
}
