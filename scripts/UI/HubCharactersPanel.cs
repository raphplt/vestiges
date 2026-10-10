using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Page Personnages de l'accueil (plan 06 §9.10, lot Q3) : les six personnages, jouables, à débloquer ou à venir. À
/// gauche la liste ; à droite le portrait agrandi (image soignée si elle existe, sinon le sprite de jeu au repos,
/// animé) et la fiche : description, passif, arme de départ, statistiques, ou la quête qui le débloque.
/// </summary>
public partial class HubCharactersPanel : MarginContainer
{
    private const float RowWidth = 420f;
    private const float RowHeight = 96f;
    private const float PortraitScale = 6f;
    private const float IdleFps = 5f;
    private static readonly Color LockedTint = new(0.45f, 0.44f, 0.5f, 1f);

    private readonly struct Entry
    {
        public readonly string Id;
        public readonly string Name;
        public readonly CharacterData Data;
        public readonly bool Unlocked;

        public Entry(string id, string name, CharacterData data, bool unlocked)
        {
            Id = id;
            Name = name;
            Data = data;
            Unlocked = unlocked;
        }

        public bool Upcoming => Data == null;
    }

    private Font _bodyFont;
    private Font _strongFont;
    private VBoxContainer _list;
    private TextureRect _portrait;
    private Label _name;
    private Label _state;
    private Label _description;
    private Label _passive;
    private Label _weapon;
    private Label _stats;
    private Label _quest;
    private readonly Texture2D[] _idle = new Texture2D[CharacterPortrait.IdleFrames];
    private int _idleCount;
    private float _idleTime;

    public override void _Ready()
    {
        _bodyFont = UITheme.BodyFont;
        _strongFont = UITheme.StrongFont;

        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 48);
        AddChild(body);

        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(RowWidth + 24f, 0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        body.AddChild(scroll);
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_list);

        // Cadre sombre filet d'or : le portrait ne se perd pas dans le décor du camp derrière le voile.
        PanelContainer frame = new() { SizeFlagsVertical = SizeFlags.ShrinkBegin };
        frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(UITheme.BgDark, 0.92f),
            BorderColor = UITheme.GoldDim,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 24,
            ContentMarginBottom = 24,
        });
        body.AddChild(frame);
        _portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(48f * PortraitScale, 64f * PortraitScale),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        frame.AddChild(_portrait);

        VBoxContainer sheet = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        sheet.AddThemeConstantOverride("separation", 12);
        body.AddChild(sheet);
        _name = AddLabel(sheet, _strongFont, TextRole.Banner, UITheme.GoldBright);
        _state = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _description = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextLight);
        _passive = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextColor);
        _weapon = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextColor);
        _stats = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _quest = AddLabel(sheet, _strongFont, TextRole.Heading, UITheme.CyanEssence);
    }

    /// <summary>Le sprite de repos tourne tant que la page est ouverte ; rien ne tourne sinon.</summary>
    public override void _Process(double delta)
    {
        if (_idleCount < 2 || !IsVisibleInTree())
            return;
        _idleTime += (float)delta;
        _portrait.Texture = _idle[(int)(_idleTime * IdleFps) % _idleCount];
    }

    private static Label AddLabel(Container parent, Font font, TextRole role, Color color)
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

    /// <summary>Reconstruit la liste ; renvoie la ligne du personnage choisi à l'accueil, qui prend le focus.</summary>
    public Control Refresh(string selectedCharacterId)
    {
        foreach (Node child in _list.GetChildren())
            child.QueueFree();
        Control focused = null;
        Entry focusedEntry = default;
        List<Entry> entries = Entries();
        foreach (Entry entry in entries)
        {
            Button row = MakeRow(entry);
            _list.AddChild(row);
            if (focused == null || entry.Id == selectedCharacterId)
            {
                focused = row;
                focusedEntry = entry;
            }
        }
        if (focused != null)
            ShowSheet(focusedEntry);
        return focused;
    }

    /// <summary>Les personnages du catalogue, puis ceux qu'une quête promet sans qu'ils soient encore jouables.</summary>
    private static List<Entry> Entries()
    {
        List<Entry> entries = new();
        HashSet<string> seen = new();
        foreach (CharacterData character in CharacterDataLoader.GetAll())
        {
            seen.Add(character.Id);
            entries.Add(new Entry(character.Id, character.Name, character, MetaSaveManager.IsCharacterUnlocked(character.Id)));
        }
        foreach (QuestDefinition quest in QuestDataLoader.GetAll())
            foreach (QuestUnlock unlock in quest.Unlocks)
                if (unlock.Kind == UnlockKind.Character && seen.Add(unlock.Id))
                    entries.Add(new Entry(unlock.Id, QuestBook.UnlockName(unlock), null, MetaSaveManager.IsCharacterUnlocked(unlock.Id)));
        return entries;
    }

    private Button MakeRow(Entry entry)
    {
        Button row = new() { CustomMinimumSize = new Vector2(RowWidth, RowHeight), FocusMode = FocusModeEnum.All };
        row.TextureFilter = TextureFilterEnum.Nearest;
        string skin = entry.Unlocked && !entry.Upcoming ? "ui_card_normal.png" : "ui_card_locked.png";
        StyleBoxTexture normal = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + skin), 4, 4, 4, 4);
        StyleBoxTexture lit = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + "ui_card_selected.png"), 4, 4, 4, 4);
        row.AddThemeStyleboxOverride("normal", normal);
        row.AddThemeStyleboxOverride("pressed", normal);
        row.AddThemeStyleboxOverride("hover", lit);
        row.AddThemeStyleboxOverride("focus", lit);
        row.AddThemeStyleboxOverride("hover_pressed", lit);

        row.AddChild(new TextureRect
        {
            Texture = CharacterPortrait.Idle(entry.Id),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            Position = new Vector2(10f, 6f),
            Size = new Vector2(63f, 84f),
            SelfModulate = entry.Unlocked ? Colors.White : LockedTint,
        });
        Label name = new() { Text = entry.Name, MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(92f, 18f) };
        name.AddThemeFontOverride("font", _strongFont);
        UITheme.SetTextRole(name, TextRole.Heading);
        name.AddThemeColorOverride("font_color", entry.Unlocked ? UITheme.TextLight : UITheme.TextDim);
        row.AddChild(name);
        Label state = new() { Text = StateText(entry), MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(92f, 52f) };
        state.AddThemeFontOverride("font", _bodyFont);
        UITheme.SetTextRole(state, TextRole.Body);
        state.AddThemeColorOverride("font_color", UITheme.TextDim);
        row.AddChild(state);

        row.FocusEntered += () => ShowSheet(entry);
        row.MouseEntered += () => ShowSheet(entry);
        UITheme.WireButtonAudio(row);
        return row;
    }

    private static string StateText(Entry entry) =>
        entry.Upcoming ? entry.Unlocked ? "Débloqué, jouable bientôt" : "À venir" : entry.Unlocked ? "Jouable" : "À débloquer";

    private void ShowSheet(Entry entry)
    {
        LoadPortrait(entry);
        _name.Text = entry.Name;
        _state.Text = StateText(entry);
        QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Character, entry.Id);
        bool questDone = quest != null && MetaSaveManager.HasCompletedQuest(quest.Id);
        _quest.Text = quest == null ? "" : questDone
            ? $"Débloqué par la quête « {quest.Name} »."
            : $"Quête « {quest.Name} » : {quest.Description}";

        if (entry.Upcoming)
        {
            _description.Text = "Personnage à venir : son passif, sa mobilité et son arme arrivent avec lui.";
            _passive.Text = "";
            _weapon.Text = "";
            _stats.Text = "";
            return;
        }
        CharacterData data = entry.Data;
        _description.Text = data.Description;
        PerkData passive = PerkDataLoader.GetAll().Find(perk => perk.Id == data.PassivePerk);
        _passive.Text = passive == null ? "" : $"Passif — {passive.Name} : {passive.Description}";
        WeaponData weapon = WeaponDataLoader.Get(data.StartingWeaponId);
        _weapon.Text = weapon == null ? "" : $"Arme de départ — {weapon.Name} : {weapon.Summary}";
        CharacterStats stats = data.BaseStats;
        _stats.Text = $"PV {stats.MaxHp:0} · vitesse {stats.Speed:0} · dégâts {stats.AttackDamage:0} · cadence ×{stats.AttackSpeed:0.0} · "
            + $"portée {stats.AttackRange:0} · régénération {stats.RegenRate:0.0}/s";
    }

    private void LoadPortrait(Entry entry)
    {
        _idleTime = 0f;
        _idleCount = 0;
        Texture2D artwork = CharacterPortrait.Artwork(entry.Id);
        if (artwork == null)
        {
            for (int i = 0; i < _idle.Length; i++)
            {
                Texture2D frame = CharacterPortrait.Idle(entry.Id, i + 1);
                if (frame == null)
                    break;
                _idle[_idleCount++] = frame;
            }
        }
        _portrait.Texture = artwork ?? (_idleCount > 0 ? _idle[0] : null);
        _portrait.SelfModulate = entry.Unlocked ? Colors.White : LockedTint;
    }
}
