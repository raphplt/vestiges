using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>Sélection avant départ : le portrait prime, la fiche chiffrée est facultative.</summary>
public partial class HubCharactersPanel : MarginContainer
{
    [Signal] public delegate void DepartRequestedEventHandler(string characterId);
    [Signal] public delegate void QuestRequestedEventHandler(string questId);
    [Signal] public delegate void WeaponRequestedEventHandler(string weaponId);
    private VBoxContainer _list;
    private TextureRect _portrait;
    private Label _name;
    private Label _description;
    private Label _signature;
    private Label _condition;
    private Label _details;
    private Button _detailsButton;
    private Button _depart;
    private Button _quest;
    private Button _weapon;
    private CharacterData _examined;
    private readonly Dictionary<string, Button> _rows = new();
    private const float RowHeight = 124f;
    private const float RowInset = 20f;
    // Le personnage se pose sur la pierre, sans cadre : le cadre doré de la carte suffit (DECISIONS §150).
    private const float SpriteWidth = 66f;
    private static readonly Color LockedStone = new(0.62f, 0.6f, 0.66f);
    private static readonly Color LockedSilhouette = new(0.2f, 0.19f, 0.24f);
    private readonly Texture2D[] _idle = new Texture2D[CharacterPortrait.IdleFrames];
    private int _idleCount;
    private float _idleTime;
    public string ExaminedCharacterId => _examined?.Id;
    public bool CanDepart => _examined != null && MetaSaveManager.IsCharacterUnlocked(_examined.Id);

    public override void _Ready()
    {
        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 32);
        AddChild(body);
        // La liste a son propre cadre de pierre : elle se détache du mur au lieu de flotter dessus (DECISIONS §117).
        PanelContainer rosterFrame = new() { CustomMinimumSize = new Vector2(372, 0) };
        rosterFrame.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        body.AddChild(rosterFrame);
        ScrollContainer roster = new()
        {
            Name = "CharacterRoster",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true,
        };
        rosterFrame.AddChild(roster);
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 10);
        roster.AddChild(_list);
        _portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(660, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _portrait.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/menu_portrait.gdshader") };
        body.AddChild(_portrait);
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(400, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        body.AddChild(panel);
        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 20);
        panel.AddChild(column);
        ScrollContainer scroll = new()
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true, SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        column.AddChild(scroll);
        VBoxContainer sheet = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        sheet.AddThemeConstantOverride("separation", 22);
        scroll.AddChild(sheet);
        _name = Label(sheet, "", TextRole.Title, UITheme.TextLight);
        _description = Label(sheet, "", TextRole.Heading, UITheme.TextColor);
        _signature = Label(sheet, "", TextRole.Subhead, UITheme.GoldBright);
        _weapon = MenuSkin.Button("");
        _weapon.Alignment = HorizontalAlignment.Left;
        _weapon.Pressed += () => EmitSignal(SignalName.WeaponRequested, _examined.StartingWeaponId);
        MenuSkin.StyleLink(_weapon);
        sheet.AddChild(_weapon);
        _detailsButton = MenuSkin.Button("Détails du personnage");
        _detailsButton.Pressed += () =>
        {
            _details.Visible = !_details.Visible;
            _detailsButton.Text = _details.Visible ? "Masquer les détails" : "Détails du personnage";
        };
        MenuSkin.StyleLink(_detailsButton);
        sheet.AddChild(_detailsButton);
        _details = Label(sheet, "", TextRole.Body, UITheme.TextColor);
        _details.Visible = false;
        _condition = Label(sheet, "", TextRole.Body, UITheme.TextColor);
        _quest = MenuSkin.Button("Voir la quête");
        _quest.Pressed += () =>
        {
            QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Character, _examined.Id);
            if (quest != null) EmitSignal(SignalName.QuestRequested, quest.Id);
        };
        sheet.AddChild(_quest);
        // Partir reste au pied de la fiche, hors du défilement : toujours au même endroit, toujours visible.
        _depart = MenuSkin.Button("Partir");
        MenuSkin.StyleCallToAction(_depart);
        _depart.Name = "DepartButton";
        _depart.CustomMinimumSize = new Vector2(0, 92);
        _depart.Pressed += () => { if (CanDepart) EmitSignal(SignalName.DepartRequested, _examined.Id); };
        column.AddChild(_depart);
    }

    private static Label Label(Container parent, string text, TextRole role, Color color)
    {
        Label label = UITheme.MakeLabel(text, role, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label);
        return label;
    }

    public Control Refresh(string selectedCharacterId = null)
    {
        if (_rows.Count == 0)
        {
            foreach (CharacterData character in CharacterDataLoader.GetAll())
            {
                bool unlocked = MetaSaveManager.IsCharacterUnlocked(character.Id);
                Button row = MenuSkin.Button("");
                MenuSkin.StyleRosterCard(row, false);
                row.Name = character.Id;
                row.AccessibilityName = character.Name;
                row.CustomMinimumSize = new Vector2(300, RowHeight);
                // Verrouillé : la pierre s'éteint, le personnage n'est qu'une ombre, le nom pâlit (DECISIONS §150).
                row.SelfModulate = unlocked ? Colors.White : LockedStone;
                // Sprite et texte en conteneurs, en retrait égal du liseré de pierre : rien ne touche le cadre.
                HBoxContainer content = new() { MouseFilter = MouseFilterEnum.Ignore };
                content.AddThemeConstantOverride("separation", 16);
                row.AddChild(content);
                content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                content.OffsetLeft = RowInset; content.OffsetRight = -RowInset;
                content.OffsetTop = RowInset; content.OffsetBottom = -RowInset;
                TextureRect sprite = new()
                {
                    Texture = CharacterPortrait.Idle(character.Id), CustomMinimumSize = new Vector2(SpriteWidth, 0),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    TextureFilter = TextureFilterEnum.Nearest, MouseFilter = MouseFilterEnum.Ignore,
                    Modulate = unlocked ? Colors.White : LockedSilhouette,
                };
                content.AddChild(sprite);
                VBoxContainer text = new() { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
                text.AddThemeConstantOverride("separation", 2);
                content.AddChild(text);
                text.AddChild(UITheme.MakeLabel(character.Name, TextRole.Subhead, unlocked ? UITheme.TextLight : UITheme.TextVeryDim));
                if (!unlocked)
                    text.AddChild(UITheme.MakeLabel("À débloquer", TextRole.Small, UITheme.TextVeryDim));
                // Seul un clic (ou la validation à la manette) change le personnage choisi : survol et focus ne font que
                // surligner la carte (DECISIONS §154).
                row.Pressed += () => { ShowSheet(character); if (unlocked) _depart.GrabFocus(); else if (_quest.Visible) _quest.GrabFocus(); };
                _list.AddChild(row);
                _rows[character.Id] = row;
            }
        }
        string id = selectedCharacterId ?? _examined?.Id;
        CharacterData selected = (id != null ? CharacterDataLoader.Get(id) : null) ?? CharacterDataLoader.GetAll()[0];
        ShowSheet(selected);
        return _rows.GetValueOrDefault(selected.Id);
    }

    private void ShowSheet(CharacterData character)
    {
        if (_examined == character) return;
        _examined = character;
        _idleTime = 0;
        _idleCount = 0;
        Texture2D art = CharacterPortrait.Artwork(character.Id);
        if (art == null)
            for (int i = 0; i < _idle.Length; i++)
            {
                Texture2D frame = CharacterPortrait.Idle(character.Id, i + 1);
                if (frame == null) break;
                _idle[_idleCount++] = frame;
            }
        _portrait.Texture = art ?? (_idleCount > 0 ? _idle[0] : null);
        _portrait.TextureFilter = art != null ? TextureFilterEnum.Linear : TextureFilterEnum.Nearest;
        _portrait.Modulate = CanDepart ? Colors.White : new Color(0.65f, 0.63f, 0.68f);
        _name.Text = character.Name;
        _description.Text = character.Description;
        PerkData passive = PerkDataLoader.GetAll().Find(perk => perk.Id == character.PassivePerk);
        _signature.Text = passive?.Name ?? "";
        WeaponData weapon = WeaponDataLoader.Get(character.StartingWeaponId);
        _weapon.Text = weapon?.Name ?? "";
        _weapon.Icon = weapon != null ? GD.Load<Texture2D>(weapon.Sprite) : null;
        // Icône bornée plutôt qu'étirée : un lien prend la largeur de son contenu et doit compter l'icône.
        _weapon.AddThemeConstantOverride("icon_max_width", 48);
        _details.Visible = false;
        _detailsButton.Text = "Détails du personnage";
        MobilityConfig mobility = MobilityConfig.Load(character.MobilityProfile);
        _details.Text = $"{passive?.Description}\n\n{weapon?.Summary}\n\n{mobility.Name} — {mobility.Description}";
        QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Character, character.Id);
        _condition.Text = CanDepart ? "" : QuestBook.LockText(UnlockKind.Character, character.Id);
        _condition.Visible = !CanDepart;
        _quest.Visible = !CanDepart && quest != null;
        _depart.Disabled = !CanDepart;
        _depart.Text = CanDepart ? "Partir" : "Personnage verrouillé";
        foreach ((string id, Button row) in _rows)
            MenuSkin.StyleRosterCard(row, id == character.Id);
    }

    public override void _Process(double delta)
    {
        if (_idleCount < 2 || !IsVisibleInTree()) return;
        _idleTime += (float)delta;
        _portrait.Texture = _idle[(int)(_idleTime * 5) % _idleCount];
    }
}
