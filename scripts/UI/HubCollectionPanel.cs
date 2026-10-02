using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Collection de l'accueil (plan 04 lot C2, première passe) : armes et souvenirs de run en grille d'icônes. Une case
/// verrouillée n'est qu'une silhouette sombre ; le panneau de droite, seul à porter du texte, donne pour la case
/// survolée ou sélectionnée son nom, son état, son effet, ses valeurs et, si elle est verrouillée, la condition directe.
/// La disponibilité suit la même règle que le loot (<see cref="MetaSaveManager.IsWeaponUnlocked"/>). L'onglet choisi
/// est conservé d'une ouverture à l'autre. Les objets du plan 05 rejoindront un troisième onglet quand ils existeront.
/// </summary>
public partial class HubCollectionPanel : MarginContainer
{
    private const int Columns = 6;
    private const float TileSize = 112f;
    private const float IconSize = 64f;
    private static readonly Color LockedTint = new(0.13f, 0.12f, 0.17f, 1f);

    private static string _tab = "weapons";

    private Font _bodyFont;
    private Font _strongFont;
    private HBoxContainer _tabs;
    private Button _activeTab;
    private GridContainer _grid;
    private TextureRect _detailIcon;
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

        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 40);
        column.AddChild(body);

        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(Columns * (TileSize + 12f) + 12f, 0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        body.AddChild(scroll);
        _grid = new GridContainer { Columns = Columns };
        _grid.AddThemeConstantOverride("h_separation", 12);
        _grid.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(_grid);

        VBoxContainer detail = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        detail.AddThemeConstantOverride("separation", 10);
        body.AddChild(detail);
        _detailIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(128f, 128f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        detail.AddChild(_detailIcon);
        _detailName = AddDetailLabel(detail, _strongFont, TextRole.Banner, UITheme.GoldBright);
        _detailState = AddDetailLabel(detail, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _detailText = AddDetailLabel(detail, _bodyFont, TextRole.Heading, UITheme.TextColor);
        _detailStats = AddDetailLabel(detail, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _detailCondition = AddDetailLabel(detail, _strongFont, TextRole.Heading, UITheme.CyanEssence);
        _count = AddDetailLabel(detail, _bodyFont, TextRole.Subhead, UITheme.TextVeryDim);
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
    public Control Refresh(string focusWeaponId = null)
    {
        if (!string.IsNullOrEmpty(focusWeaponId))
            _tab = "weapons";
        foreach (Node child in _tabs.GetChildren())
            child.QueueFree();
        Button weapons = AddTab("weapons", "Armes");
        Button passives = AddTab("passives", "Objets");
        Button perks = AddTab("perks", "Réminiscences");
        // Liens explicites : la recherche géométrique de Godot plongeait dans la grille au lieu de l'onglet voisin.
        weapons.FocusNeighborRight = passives.GetPath();
        passives.FocusNeighborLeft = weapons.GetPath();
        passives.FocusNeighborRight = perks.GetPath();
        perks.FocusNeighborLeft = passives.GetPath();
        return RebuildGrid(focusWeaponId);
    }

    private Button AddTab(string id, string text)
    {
        Button tab = new() { Text = text, Flat = true, FocusMode = FocusModeEnum.All };
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
        tab.AddThemeFontOverride("font", _strongFont);
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
        foreach (Node child in _grid.GetChildren())
            child.QueueFree();
        List<Entry> entries = _tab switch { "weapons" => WeaponEntries(), "perks" => PerkEntries(), _ => PassiveEntries() };
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
                tile.FocusNeighborTop = _activeTab.GetPath();
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
                    ((Control)tab).FocusNeighborBottom = first.GetPath();
            }
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

    private Button MakeTile(Entry entry)
    {
        Button tile = new() { CustomMinimumSize = new Vector2(TileSize, TileSize), FocusMode = FocusModeEnum.All };
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
        tile.FocusEntered += () => ShowDetail(entry);
        tile.MouseEntered += () => ShowDetail(entry);
        return tile;
    }

    private void ShowDetail(Entry entry)
    {
        _detailIcon.Texture = !string.IsNullOrEmpty(entry.Icon) && ResourceLoader.Exists(entry.Icon) ? GD.Load<Texture2D>(entry.Icon) : null;
        _detailIcon.SelfModulate = entry.Unlocked || entry.Preview ? Colors.White : LockedTint;
        _detailName.Text = entry.Name;
        _detailState.Text = entry.Unlocked ? "Disponible en run" : entry.Preview ? "À venir" : "Pas encore disponible";
        _detailText.Text = entry.Description;
        _detailStats.Text = entry.Stats;
        _detailCondition.Text = entry.Unlocked ? "" : entry.Condition;
    }

    private static List<Entry> WeaponEntries()
    {
        List<Entry> entries = new();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            bool unlocked = MetaSaveManager.IsWeaponUnlocked(weapon);
            string condition = "";
            if (!unlocked)
            {
                string souvenir = SouvenirDataLoader.Get(weapon.RequiresSouvenir)?.Name ?? weapon.RequiresSouvenir;
                condition = $"Se débloque en retrouvant le Souvenir « {souvenir} ».";
            }
            entries.Add(new Entry(weapon.Id, weapon.Name, weapon.Sprite, weapon.Description, WeaponStats(weapon), unlocked, condition));
        }
        // Disponibles d'abord : la grille se lit comme « ce que j'ai », puis « ce qui reste à trouver ».
        entries.Sort((a, b) => b.Unlocked.CompareTo(a.Unlocked));
        return entries;
    }

    private static string WeaponStats(WeaponData weapon)
    {
        string pattern = weapon.AttackPattern switch
        {
            "arc" => "Arc",
            "linear" => "Ligne",
            "circular" => "Cercle",
            "orbital" => "Orbital",
            "chain" => "Chaîne",
            "homing" => "Tête chercheuse",
            _ => weapon.AttackPattern,
        };
        string kind = weapon.Type == "melee" ? "Mêlée" : weapon.Type == "ranged" ? "Distance" : "Spéciale";
        weapon.Stats.TryGetValue("damage", out float damage);
        weapon.Stats.TryGetValue("attack_speed", out float speed);
        weapon.Stats.TryGetValue("range", out float range);
        return $"{kind} · {pattern} · {damage:0} dég. · {speed:0.0}/s · portée {range:0}";
    }

    private static List<Entry> PassiveEntries()
    {
        List<Entry> entries = new();
        foreach (PassiveSouvenirData passive in PassiveSouvenirDataLoader.GetAll())
        {
            string stats = passive.MaxLevel > 1 ? $"Jusqu'au niveau {passive.MaxLevel}, cumulable en run" : "";
            entries.Add(new Entry(passive.Id, passive.Name, passive.Icon, passive.RuleText(), stats, true, ""));
        }
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
            entries.Add(new Entry(art.Id, art.Name, data?.Icon ?? art.Icon, data?.Description ?? "Réminiscence",
                "", active, "Cette Réminiscence n'est pas encore proposée en run.", !active));
        }
        return entries;
    }
}
